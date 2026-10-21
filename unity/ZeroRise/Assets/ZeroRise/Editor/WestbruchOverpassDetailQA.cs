using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Deliberately checks authored triangles and real supporting colliders.
    // Broad lane/spawn checks cannot expose floating small props or bad cap UVs.
    internal static class WestbruchOverpassDetailQA
    {
        struct Face {public Vector3 a,b,c,normal;public float plane;public MeshFilter owner;public bool local;}
        sealed class Piece {public Bounds bounds;public Vector3 lowest;public bool ready;}
        static Transform ground;
        internal static Vector3 Eye(float x,float z,float eyeHeight)
        {
            var p=new Vector3(x,0,z);return Support(p,out float y)?new Vector3(x,y+eyeHeight,z):new Vector3(x,eyeHeight,z);
        }
        static bool InArea(Vector3 p)=>p.x>=70&&p.x<=167&&p.z>=45&&p.z<=116;
        internal static void Validate(Action<string,bool,string> record)
        {
            Physics.SyncTransforms();
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName);
            var camp=GameObject.Find(WestbruchBridgeStart.CampName);
            ground=GameObject.Find("Railway earthworks – continuous vegetated embankments")?.transform;
            if(!bridge){record("Focused detail scene exists",false,"Missing bridge root");return;}
            CapUVAndCoplanarFaces(bridge.transform,record);
            RailSafetyJoins(bridge.transform,record);
            Fences(bridge.transform,camp?camp.transform:null,record);
            TreesAndProps(bridge.transform,camp?camp.transform:null,record);
            Rubble(record);
            PavementCorners(record);
            OriginalPavementClearance(record);
        }
        static bool Support(Vector3 p,out float height)
        {
            if(!ground)ground=GameObject.Find("Railway earthworks – continuous vegetated embankments")?.transform;
            height=float.NegativeInfinity;
            foreach(var h in Physics.RaycastAll(new Vector3(p.x,20,p.z),Vector3.down,40,1,QueryTriggerInteraction.Ignore))
            {
                if(h.normal.y<.3f)continue;
                var c=h.collider;string name=c.name.ToLowerInvariant();
                bool floor=c is TerrainCollider||(ground&&c.transform.IsChildOf(ground))||name.Contains("pavement")||name.Contains("carriageway")||name.Contains("turning court")||name.Contains("footing")||name.Contains("ground apron")||name.Contains("hardstanding")||name.Contains("maintenance approach ramp")||name.Contains("maintenance walkway");
                // A maintenance deck above a camp fence is a ceiling, while
                // that same surface supports a rail post standing near y6.
                bool elevated=name.Contains("maintenance approach ramp")||name.Contains("maintenance walkway");
                if(elevated&&h.point.y>p.y+.5f)continue;
                if(floor)height=Mathf.Max(height,h.point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
        static void AddExample(List<string> list,string item){if(list.Count<8)list.Add(item);}
        static Bounds AuthoredBounds(Transform transform)
        {
            bool ready=false;var bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var f in transform.GetComponentsInChildren<MeshFilter>())
            {
                if(!f.sharedMesh||f.name.StartsWith("Local render batch")||f.name.Contains("collision envelope"))continue;
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.sharedMaterial||r.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE"))continue;
                // Retained per-prop sources are disabled after local batching.
                var local=f.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var v=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=f.transform.TransformPoint(v);if(!ready){bounds=new Bounds(p,Vector3.zero);ready=true;}else bounds.Encapsulate(p);
                }
            }
            return bounds;
        }
        static void TreesAndProps(Transform bridge,Transform camp,Action<string,bool,string> record)
        {
            int trees=0,treeFailures=0,props=0,propFailures=0;float maxTreeGap=0,maxPropGap=0;var treeExamples=new List<string>();var propExamples=new List<string>();
            foreach(var tree in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                if(!tree.name.StartsWith("Urban tree ")||!InArea(tree.transform.position))continue;
                trees++;var p=tree.transform.position;
                if(!Support(p,out float floor)){treeFailures++;AddExample(treeExamples,"No tree support at "+p.ToString("F2"));continue;}
                float gap=p.y+.02f-floor;maxTreeGap=Mathf.Max(maxTreeGap,Mathf.Abs(gap));
                if(Mathf.Abs(gap)>.075f){treeFailures++;AddExample(treeExamples,"Tree root gap "+gap.ToString("F3")+" at "+p.ToString("F2"));}
            }
            var names=new HashSet<string>{"Worn mattress and bedding","Worn backpack","Salvaged shopping cart","Tied refuse bag","Wheelie bin","Gewerbehof utilities and loading","Gewerbehof railway maintenance garage","Railway lineside cabinet","Reused district streetlight"};
            var parents=camp?new[]{bridge,camp}:new[]{bridge};
            foreach(var parent in parents)foreach(var t in parent.GetComponentsInChildren<Transform>())
            {
                if(!names.Contains(t.name)||!InArea(t.position))continue;
                props++;var bounds=AuthoredBounds(t);var at=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                if(!Support(at,out float floor)){propFailures++;propExamples.Add("No prop support: "+t.name+" "+at.ToString("F2"));continue;}
                float gap=at.y-floor;maxPropGap=Mathf.Max(maxPropGap,gap);
                // Foundations can be embedded; loose props cannot levitate.
                if(gap>.015f)propExamples.Add(t.name+" foot gap "+gap.ToString("F3")+" at "+at.ToString("F2")+", supporting floor "+floor.ToString("F3"));
                if(gap>.02f)propFailures++;
            }
            record("Trees follow actual new ground rather than stale terrain heights",trees>0&&treeFailures==0,"Trees "+trees+", failures "+treeFailures+", maximum absolute root gap "+maxTreeGap.ToString("F3")+" m; "+string.Join("; ",treeExamples));
            record("Loose camp/yard props have actual support within 0.020 m",props>0&&propFailures==0,"Props "+props+", failures "+propFailures+", maximum foot gap "+maxPropGap.ToString("F3")+" m; gaps above 0.015 m: "+string.Join("; ",propExamples));
        }
        static void Fences(Transform bridge,Transform camp,Action<string,bool,string> record)
        {
            int posts=0,badPosts=0,rails=0,badRails=0;float maxGap=0;var examples=new List<string>();
            foreach(var parent in camp?new[]{bridge,camp}:new[]{bridge})foreach(var f in parent.GetComponentsInChildren<MeshFilter>())
            {
                if(!f.sharedMesh)continue;
                if(f.name=="Fence post")
                {
                    var b=AuthoredBounds(f.transform);var p=new Vector3(b.center.x,b.min.y,b.center.z);if(!InArea(p))continue;
                    posts++;bool supported=Support(p,out float y);float gap=supported?p.y-y:99;maxGap=Mathf.Max(maxGap,Mathf.Abs(gap));
                    if(!supported||Mathf.Abs(gap)>.075f){badPosts++;AddExample(examples,"Post gap "+gap.ToString("F3")+" at "+p.ToString("F2"));}
                }
                if(f.name=="Lower rail")
                {
                    var bounds=f.sharedMesh.bounds;bool bad=false;
                    for(int sample=0;sample<=8;sample++)
                    {
                        var p=f.transform.TransformPoint(bounds.center+Vector3.up*Mathf.Lerp(-bounds.extents.y,bounds.extents.y,sample/8f));if(!InArea(p))continue;
                        bool supported=Support(p,out float floor);float gap=supported?p.y-floor:99;
                        if(!supported||gap<.10f||gap>.36f){bad=true;AddExample(examples,"Rail sample "+p.ToString("F3")+", supporting floor "+floor.ToString("F3")+", clearance "+gap.ToString("F3")+" m (span "+f.transform.position.ToString("F2")+")");}
                    }
                    rails++;if(bad){badRails++;AddExample(examples,"Lower rail loses grade near "+f.transform.position.ToString("F2"));}
                }
            }
            record("Fence post feet and lower rails track actual embankment grade",posts>0&&badPosts==0&&badRails==0,"Posts "+posts+", unsupported/buried "+badPosts+", rail spans "+rails+", bad spans "+badRails+", maximum post gap "+maxGap.ToString("F3")+" m; "+string.Join("; ",examples));
        }
        static void RailSafetyJoins(Transform bridge,Action<string,bool,string> record)
        {
            var filters=bridge.GetComponentsInChildren<MeshFilter>();var postBounds=new List<Bounds>();var examples=new List<string>();
            int checkedPosts=0,badPosts=0,endpoints=0,unattached=0,badTerminalHeights=0;float maxPostGap=0,maxAttachment=0,maxWireHeight=0;
            foreach(var f in filters)
            {
                if(f.name!="Grounded rail boundary post"&&f.name!="Maintenance parapet post")continue;
                var bounds=AuthoredBounds(f.transform);postBounds.Add(bounds);
                if(f.name!="Grounded rail boundary post"||bounds.center.z<45||bounds.center.z>115)continue;
                checkedPosts++;var foot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                bool supported=Support(foot,out float floor);float gap=supported?foot.y-floor:99;maxPostGap=Mathf.Max(maxPostGap,Mathf.Abs(gap));
                if(!supported||Mathf.Abs(gap)>.07f){badPosts++;AddExample(examples,"Rail post foot gap "+gap.ToString("F3")+" at "+foot.ToString("F2"));}
            }
            var terminalLevels=new HashSet<string>();
            foreach(var f in filters)
            {
                if(f.name!="Terminated railway safety wire"||!f.sharedMesh)continue;
                var bounds=f.sharedMesh.bounds;
                foreach(float sign in new[]{-1f,1f})
                {
                    // Geometry.Beam's local Y axis is the complete endpoint span.
                    var point=f.transform.TransformPoint(bounds.center+Vector3.up*bounds.extents.y*sign);
                    if(point.z<45||point.z>115)continue;endpoints++;maxWireHeight=Mathf.Max(maxWireHeight,point.y);
                    float nearest=float.PositiveInfinity;
                    foreach(var post in postBounds)
                    {
                        var axis=new Vector3(post.center.x,Mathf.Clamp(point.y,post.min.y,post.max.y),post.center.z);
                        nearest=Mathf.Min(nearest,Vector3.Distance(axis,point));
                    }
                    maxAttachment=Mathf.Max(maxAttachment,nearest);
                    if(nearest>.07f){unattached++;AddExample(examples,"Wire misses post by "+nearest.ToString("F3")+" at "+point.ToString("F2"));}
                    bool terminal=(Mathf.Abs(point.z-63)<.02f||Mathf.Abs(point.z-97)<.02f)&&(Mathf.Abs(point.x-114.05f)<.02f||Mathf.Abs(point.x-121.95f)<.02f);
                    if(!terminal)continue;
                    var levels=new[]{6.72f,7.12f,7.60f};int best=0;for(int i=1;i<levels.Length;i++)if(Mathf.Abs(point.y-levels[i])<Mathf.Abs(point.y-levels[best]))best=i;
                    terminalLevels.Add(Mathf.RoundToInt(point.x*100)+"/"+Mathf.RoundToInt(point.z*100)+"/"+best);
                    if(Mathf.Abs(point.y-levels[best])>.035f){badTerminalHeights++;AddExample(examples,"Wrong terminal wire height at "+point.ToString("F3"));}
                }
            }
            record("Rail boundary post feet rest on actual soil or maintenance ramps",checkedPosts>0&&badPosts==0,"Posts "+checkedPosts+", unsupported "+badPosts+", maximum absolute foot gap "+maxPostGap.ToString("F3")+" m; "+string.Join("; ",examples));
            record("All railway wires terminate on actual posts at the correct parapet rail levels",endpoints>0&&unattached==0&&badTerminalHeights==0&&terminalLevels.Count==12,"Endpoints "+endpoints+", unattached "+unattached+", incorrect terminal heights "+badTerminalHeights+", distinct terminal connections "+terminalLevels.Count+"/12; maximum endpoint/post distance "+maxAttachment.ToString("F3")+" m, maximum wire height "+maxWireHeight.ToString("F3")+" m; "+string.Join("; ",examples));
        }
        static void PavementCorners(Action<string,bool,string> record)
        {
            int samples=0,failures=0;var examples=new List<string>();
            foreach(float z0 in new[]{72.1f,85.1f})for(float x=139.1f;x<=141.91f;x+=.35f)for(float z=z0;z<=z0+2.8f;z+=.35f)
            {
                samples++;bool found=false;
                foreach(var hit in Physics.RaycastAll(new Vector3(x,1,z),Vector3.down,2,1,QueryTriggerInteraction.Ignore))
                    if(hit.collider.name.ToLowerInvariant().Contains("pavement")&&hit.normal.y>.95f&&Mathf.Abs(hit.point.y-.14f)<.025f)found=true;
                if(!found){failures++;AddExample(examples,new Vector3(x,0,z).ToString("F2"));}
            }
            record("Both courtyard pavement corners are completely filled at a consistent level",failures==0,"Samples "+samples+", missing pavement "+failures+"; "+string.Join("; ",examples));
        }
        static void OriginalPavementClearance(Action<string,bool,string> record)
        {
            int samples=0,soilIntrusions=0,fenceContacts=0;var examples=new List<string>();
            for(float z=-100;z<=110;z+=2)
            {
                // The new Nordring crossing intentionally replaces this short
                // part of the original north/south sidewalk with carriageway.
                if(z>=72&&z<=88)continue;
                for(int column=0;column<5;column++)
                {
                    float x=Mathf.Lerp(100.2f,102.9f,column/4f);samples++;
                    foreach(var hit in Physics.RaycastAll(new Vector3(x,10,z),Vector3.down,20,1,QueryTriggerInteraction.Ignore))
                    {
                        bool soil=hit.collider is TerrainCollider||(ground&&hit.collider.transform.IsChildOf(ground));
                        if(soil&&hit.normal.y>.25f&&hit.point.y>.155f){soilIntrusions++;AddExample(examples,"Soil above original pavement at "+hit.point.ToString("F3"));break;}
                    }
                    foreach(var collider in Physics.OverlapBox(new Vector3(x,1,z),new Vector3(.09f,.7f,.09f),Quaternion.identity,1,QueryTriggerInteraction.Ignore))
                        if(collider.name=="Fence collider rail"){fenceContacts++;AddExample(examples,"Fence crosses original sidewalk near "+new Vector3(x,.14f,z).ToString("F2"));break;}
                }
            }
            record("Original Stadtanschluss pedestrian pavement remains clear of new fill and fencing",soilIntrusions==0&&fenceContacts==0,"Samples "+samples+" at x100.2–102.9, z−100–110 excluding Nordring crossing; soil intrusions "+soilIntrusions+", fence contacts "+fenceContacts+"; "+string.Join("; ",examples));
        }
        static void Rubble(Action<string,bool,string> record)
        {
            int pieces=0,floating=0;float maxGap=0;var examples=new List<string>();
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;
                if(!renderer||!renderer.enabled||!renderer.sharedMaterial||!renderer.sharedMaterial.name.StartsWith("GroundDetail_")||!mesh||!mesh.isReadable)continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;var parent=new int[vertices.Length];for(int i=0;i<parent.Length;i++)parent[i]=i;
                int Root(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
                void Join(int a,int b){a=Root(a);b=Root(b);if(a!=b)parent[a]=b;}
                var used=new HashSet<int>(indices);var weld=new Dictionary<Vector3Int,int>();var world=new Vector3[vertices.Length];
                foreach(int index in used)
                {
                    var p=f.transform.TransformPoint(vertices[index]);world[index]=p;
                    var key=new Vector3Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000));
                    if(weld.TryGetValue(key,out int previous))Join(index,previous);else weld.Add(key,index);
                }
                for(int i=0;i<indices.Length;i+=3){Join(indices[i],indices[i+1]);Join(indices[i],indices[i+2]);}
                var groups=new Dictionary<int,Piece>();
                foreach(int index in used)
                {
                    int key=Root(index);if(!groups.TryGetValue(key,out var piece)){piece=new Piece();groups.Add(key,piece);}
                    var p=world[index];if(!piece.ready){piece.bounds=new Bounds(p,Vector3.zero);piece.lowest=p;piece.ready=true;}else{piece.bounds.Encapsulate(p);if(p.y<piece.lowest.y)piece.lowest=p;}
                }
                foreach(var piece in groups.Values)
                {
                    if(!InArea(piece.bounds.center))continue;pieces++;
                    bool supported=Support(piece.lowest,out float floor);float gap=supported?piece.lowest.y-floor:99;maxGap=Mathf.Max(maxGap,gap);
                    if(!supported||gap>.04f){floating++;AddExample(examples,"Loose piece gap "+gap.ToString("F3")+" at "+piece.lowest.ToString("F2"));}
                }
            }
            record("Rendered gravel/rubble pieces beside new piers and earthworks are supported",floating==0,"Rendered welded pieces "+pieces+", floating "+floating+", maximum gap "+maxGap.ToString("F3")+" m; "+string.Join("; ",examples));
        }
        static bool SurfaceCandidate(string name)
        {
            name=name.ToLowerInvariant();
            return !name.StartsWith("local render batch")&&(name.Contains("pavement")||name.Contains("carriageway")||name.Contains("turning court")||name.Contains("retaining wing")||name.Contains("bridge deck")||name.Contains("approach apron")||name.Contains("maintenance approach ramp")||name.Contains("maintenance walkway")||name.Contains("edge cornice")||name.Contains("ballast backwall"));
        }
        static void CapUVAndCoplanarFaces(Transform bridge,Action<string,bool,string> record)
        {
            var faces=new List<Face>();var examples=new List<string>();int caps=0,collapsed=0,rampMeshes=0,rampTopTriangles=0;
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>())
            {
                if(!SurfaceCandidate(f.name)||!f.sharedMesh||!f.sharedMesh.isReadable)continue;
                bool ramp=f.name.ToLowerInvariant().Contains("maintenance approach ramp");if(ramp)rampMeshes++;
                var mesh=f.sharedMesh;var positions=mesh.vertices;var uv=mesh.uv;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    int ia=indices[i],ib=indices[i+1],ic=indices[i+2];var a=f.transform.TransformPoint(positions[ia]);var b=f.transform.TransformPoint(positions[ib]);var c=f.transform.TransformPoint(positions[ic]);
                    var cross=Vector3.Cross(b-a,c-a);float area=cross.magnitude*.5f;if(area<.00001f)continue;var normal=cross.normalized;
                    if((f.name.ToLowerInvariant().Contains("retaining wing")||f.name.ToLowerInvariant().Contains("maintenance approach ramp"))&&normal.y>.15f)
                    {
                        if(ramp)rampTopTriangles++;
                        caps++;float texelArea=uv.Length==positions.Length?Mathf.Abs(Cross(uv[ib]-uv[ia],uv[ic]-uv[ia]))*.5f:0;
                        if(texelArea/area<.000001f){collapsed++;AddExample(examples,"Collapsed cap UV: "+f.name+" near "+((a+b+c)/3).ToString("F2"));}
                    }
                    faces.Add(new Face{a=a,b=b,c=c,normal=normal,plane=Vector3.Dot(normal,a),owner=f,local=true});
                }
            }
            // The retained legacy sources can contain faces deliberately removed
            // from their render chunks. Indexed visible chunks are authoritative.
            var footprint=new Bounds(new Vector3(131,4,80),new Vector3(70,20,50));
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(f.transform.IsChildOf(bridge))continue;
                var r=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;
                if(!r||!r.enabled||!r.sharedMaterial||!r.bounds.Intersects(footprint)||!mesh||!mesh.isReadable)continue;
                string material=r.sharedMaterial.name;
                if(material!="Paving"&&material!="Asphalt")continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=f.transform.TransformPoint(vertices[indices[i]]);var b=f.transform.TransformPoint(vertices[indices[i+1]]);var c=f.transform.TransformPoint(vertices[indices[i+2]]);
                    var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);if(!bounds.Intersects(footprint))continue;
                    var cross=Vector3.Cross(b-a,c-a);if(cross.sqrMagnitude<.000000001f)continue;var normal=cross.normalized;
                    faces.Add(new Face{a=a,b=b,c=c,normal=normal,plane=Vector3.Dot(normal,a),owner=f,local=false});
                }
            }
            record("Visible retaining-wall caps and maintenance ramps have non-collapsed texture coordinates",caps>0&&collapsed==0&&rampMeshes==4&&rampTopTriangles>0,"Upward cap/ramp triangles "+caps+", collapsed UV triangles "+collapsed+", approach ramps "+rampMeshes+"/4, ramp top triangles "+rampTopTriangles+"; "+string.Join("; ",examples));
            int overlaps=0;float largest=0;examples.Clear();var overlapKinds=new Dictionary<string,int>();
            for(int i=0;i<faces.Count;i++)for(int j=i+1;j<faces.Count;j++)
            {
                var a=faces[i];var b=faces[j];if((!a.local&&!b.local)||a.owner==b.owner||Vector3.Dot(a.normal,b.normal)<.99999f||Mathf.Abs(a.plane-b.plane)>.0008f)continue;
                float area=OverlapArea(a,b);if(area<.0005f)continue;
                overlaps++;largest=Mathf.Max(largest,area);
                var names=new[]{a.owner.name,b.owner.name};Array.Sort(names,StringComparer.Ordinal);string kind=names[0]+" / "+names[1];
                if(overlapKinds.ContainsKey(kind))overlapKinds[kind]++;else{overlapKinds.Add(kind,1);AddExample(examples,kind+" near "+((a.a+a.b+a.c)/3).ToString("F2"));}
            }
            record("Bridge caps and paving have no positive-area coplanar face overlaps",overlaps==0,"Overlapping triangle pairs "+overlaps+", largest projected area "+largest.ToString("F4")+" m²; kinds: "+string.Join("; ",overlapKinds.Select(p=>p.Key+" ("+p.Value+")"))+"; examples: "+string.Join("; ",examples));
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static Vector2 Project(Vector3 p,int drop)=>drop==0?new Vector2(p.y,p.z):drop==1?new Vector2(p.x,p.z):new Vector2(p.x,p.y);
        static float OverlapArea(Face a,Face b)
        {
            var n=a.normal;int drop=Mathf.Abs(n.x)>Mathf.Abs(n.y)?(Mathf.Abs(n.x)>Mathf.Abs(n.z)?0:2):(Mathf.Abs(n.y)>Mathf.Abs(n.z)?1:2);
            var polygon=new List<Vector2>{Project(a.a,drop),Project(a.b,drop),Project(a.c,drop)};
            var clip=new[]{Project(b.a,drop),Project(b.b,drop),Project(b.c,drop)};float sign=Mathf.Sign(Cross(clip[1]-clip[0],clip[2]-clip[0]));
            for(int edge=0;edge<3&&polygon.Count>0;edge++)
            {
                var next=new List<Vector2>();var start=clip[edge];var direction=clip[(edge+1)%3]-start;
                var previous=polygon[polygon.Count-1];float d0=Cross(direction,previous-start)*sign;
                foreach(var current in polygon)
                {
                    float d1=Cross(direction,current-start)*sign;bool in0=d0>=-.00001f,in1=d1>=-.00001f;
                    if(in0!=in1){float t=d0/(d0-d1);next.Add(Vector2.LerpUnclamped(previous,current,t));}
                    if(in1)next.Add(current);previous=current;d0=d1;
                }
                polygon=next;
            }
            float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);return Mathf.Abs(area)*.5f;
        }
    }
}
