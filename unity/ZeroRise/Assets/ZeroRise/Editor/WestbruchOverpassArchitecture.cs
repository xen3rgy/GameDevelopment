using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // One engineered rail corridor and a real street connection, authored locally.
    internal static class WestbruchOverpassArchitecture
    {
        const string Assets=Generated+"/Meshes/";
        static Material Load(string id)=>AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/"+id+".mat");
        static Material concrete,metal,ballast,rail,sleeper,paint,paving,asphalt;
        static GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,Material m,bool solid=false,float tile=4)=>Box(name,parent,p,size,m,solid,tile);
        public static void ClearLegacy(Transform district)
        {
            var names=new HashSet<string>{"Railway east retaining fill","Railway west retaining fill","Ballast atop abutment","Rail continuation","Rail continuation sleeper"};
            var targets=district.GetComponentsInChildren<Transform>().Where(t=>names.Contains(t.name)).Select(t=>t.gameObject).ToList();
            var oldBin=district.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Refuse and salvage alcove"&&Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(110,51))<1);
            if(oldBin)targets.Add(oldBin.gameObject);
            var fenceNames=new HashSet<string>{"Fence post","Upper rail","Lower rail","Chain link diagonal","Fence collider rail"};
            foreach(var f in district.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r)continue;
                if(fenceNames.Contains(f.name)&&Mathf.Abs(r.bounds.center.x-131.5f)<.15f&&r.bounds.size.x<.35f)targets.Add(f.gameObject);
                if((f.name=="Connected pavement"||f.name=="Kerb")&&r.bounds.center.x>=99.85f&&r.bounds.center.x<=103.05f&&r.bounds.center.z>75&&r.bounds.center.z<85)targets.Add(f.gameObject);
            }
            RemoveBakedSources(district,targets.Distinct().ToArray());
            foreach(var o in targets.Distinct())if(o)UnityEngine.Object.DestroyImmediate(o);
            int removedPlants=ClearPavedVegetation(district);
            var trees=district.GetComponentsInChildren<LODGroup>().Where(t=>t.name.StartsWith("Urban tree ")).ToArray();int moved=0;
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var processed=new List<LODGroup>();
            foreach(var tree in trees)
            {
                var p=tree.transform.position;
                bool conflict=p.x>104&&p.x<133&&(p.z<64||p.z>96)||processed.Any(t=>Vector2.Distance(new Vector2(t.transform.position.x,t.transform.position.z),new Vector2(p.x,p.z))<2);
                if(conflict)
                {
                    bool placed=false;
                    foreach(float offset in new[]{0f,7,-7,14,-14,21,-21,28,-28})foreach(float x in new[]{79f,139,35,166})
                    {
                        if(placed)continue;float z=p.z+offset;
                        if(Mathf.Abs(z)>110||x>137&&z>54&&z<116)continue;
                        if(plan.buildings.Any(b=>Mathf.Abs(x-b.x)<b.w*.5f+3&&Mathf.Abs(z-b.z)<b.d*.5f+3))continue;
                        if(plan.roads.Any(r=>Mathf.Abs(x-r.x)<r.w*.5f+5&&Mathf.Abs(z-r.z)<r.d*.5f+5))continue;
                        if(trees.Any(t=>t!=tree&&Vector2.Distance(new Vector2(t.transform.position.x,t.transform.position.z),new Vector2(x,z))<5.5f))continue;
                        tree.transform.position=new Vector3(x,Floor(x,z)-.02f,z);moved++;placed=true;
                    }
                    if(!placed)throw new InvalidOperationException("No clear verge position for "+tree.name);
                }
                processed.Add(tree);
            }
            File.WriteAllText("QA/overpass-cleanup.txt","Legacy isolated rail/retaining sources removed: "+targets.Distinct().Count()+"\nPaving foliage triangles removed: "+removedPlants+"\nConflicting LOD trees relocated: "+moved+"\n");
        }
        public static bool IsPaved(float x,float z,float margin=0)
            =>x>100-margin&&x<160+margin&&z>72-margin&&z<88+margin||x>139-margin&&x<163+margin&&z>58-margin&&z<102+margin;
        static int ClearPavedVegetation(Transform district)
        {
            int removed=0;
            foreach(var f in district.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;
                if(!r||!r.enabled||!r.sharedMaterial||!r.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE")||!mesh||!mesh.isReadable)continue;
                var positions=mesh.vertices;var roots=mesh.uv4;var triangles=mesh.triangles;var clear=new bool[positions.Length];
                for(int i=0;i<positions.Length;i++){var p=roots.Length==positions.Length?roots[i]:new Vector2(f.transform.TransformPoint(positions[i]).x,f.transform.TransformPoint(positions[i]).z);clear[i]=IsPaved(p.x,p.y,.55f);}
                var keep=new List<int>(triangles.Length);int count=0;
                for(int i=0;i<triangles.Length;i+=3){if(clear[triangles[i]]||clear[triangles[i+1]]||clear[triangles[i+2]])count++;else keep.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
                if(count==0)continue;removed+=count;mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
            return removed;
        }
        static float Floor(float x,float z)=>Terrain.activeTerrain.SampleHeight(new Vector3(x,0,z))+Terrain.activeTerrain.transform.position.y;
        static string Key(MeshFilter f){var p=f.transform.position;return f.GetComponent<MeshRenderer>().sharedMaterial.name+"_"+Mathf.FloorToInt(p.x/32)+"_"+Mathf.FloorToInt(p.z/32);}
        static void RemoveBakedSources(Transform district,GameObject[] objects)
        {
            var chunks=district.Find("Static render chunks – sources retained for editing");if(!chunks)return;
            var sources=objects.SelectMany(o=>o.GetComponentsInChildren<MeshFilter>()).Distinct().Where(f=>f.GetComponent<MeshRenderer>()&&!f.GetComponent<MeshRenderer>().enabled&&f.name!="Fence collider rail").ToArray();
            string Vertex(Vector3 p)=>Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.y*1000)+","+Mathf.RoundToInt(p.z*1000);
            string Triangle(Vector3 a,Vector3 b,Vector3 c){var keys=new[]{Vertex(a),Vertex(b),Vertex(c)};Array.Sort(keys,StringComparer.Ordinal);return keys[0]+"|"+keys[1]+"|"+keys[2];}
            var updates=new List<KeyValuePair<Mesh,List<int>>>();
            foreach(var batch in sources.GroupBy(Key))
            {
                var f=chunks.Find(batch.Key)?.GetComponent<MeshFilter>();if(!f)continue;
                var mesh=f.sharedMesh;var vertices=mesh.vertices;var remove=new HashSet<string>();
                foreach(var source in batch)
                {
                    var positions=source.sharedMesh.vertices.Select(p=>source.transform.TransformPoint(p)).ToArray();var triangles=source.sharedMesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)remove.Add(Triangle(positions[triangles[i]],positions[triangles[i+1]],positions[triangles[i+2]]));
                }
                var indices=mesh.triangles;var keep=new List<int>();int removed=0;
                for(int i=0;i<indices.Length;i+=3)
                {
                    if(remove.Contains(Triangle(f.transform.TransformPoint(vertices[indices[i]]),f.transform.TransformPoint(vertices[indices[i+1]]),f.transform.TransformPoint(vertices[indices[i+2]]))))removed++;
                    else keep.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
                }
                if(removed==0)Debug.Log("Legacy batch already excludes target triangles: "+batch.Key);
                updates.Add(new KeyValuePair<Mesh,List<int>>(mesh,keep));
            }
            // Validate every batch before changing any shared mesh asset.
            foreach(var update in updates){update.Key.SetTriangles(update.Value,0);update.Key.RecalculateBounds();EditorUtility.SetDirty(update.Key);}
        }
        public static void Build(Transform parent,bool bakeLocal)
        {
            var root=Group(WestbruchBridgeStart.BridgeName,parent,Vector3.zero).transform;
            concrete=Material("Overpass_CastConcrete","concrete",new Color(.72f,.74f,.72f),.13f);
            metal=Material("Overpass_GalvanizedSteel","steel",new Color(.38f,.41f,.40f),.24f,.7f);
            ballast=Material("Overpass_RailBallast","yard",new Color(.54f,.56f,.52f),.06f);
            rail=Material("Overpass_RailWeb","steel",new Color(.29f,.24f,.19f),.24f,.75f);
            sleeper=Material("Overpass_ConcreteSleeper","concrete",new Color(.66f,.67f,.63f),.08f);
            paint=Load("Signage_Face_road_paint");paving=Load("Paving");asphalt=Load("Asphalt");
            WestbruchOverpassGround.Build(root);
            Structure(root);Track(root);Streets(root);Boundaries(root);Details(root);
            WestbruchBridgeStart.BuildRelocatedCamp(parent,bakeLocal);
            if(bakeLocal)WestbruchBridgeStart.BakeLocal(root,"EngineeredOverpass");
        }
        static void Structure(Transform root)
        {
            Part("Prestressed concrete bridge deck",root,new Vector3(118,5.92f,80),new Vector3(8.4f,.56f,34),concrete,true,5);
            foreach(float x in new[]{114.55f,116.85f,119.15f,121.45f})
            {
                Part("Prestressed longitudinal girder",root,new Vector3(x,5.36f,80),new Vector3(.42f,.56f,32),concrete,true,5);
                Part("Girder lower flange",root,new Vector3(x,5.13f,80),new Vector3(.68f,.12f,32),concrete,false,5);
            }
            foreach(float z in new[]{62.2f,97.8f})Part("Retained maintenance approach apron",root,new Vector3(118,6.175f,z),new Vector3(8.6f,.45f,2.2f),concrete,true);
            foreach(float z in new[]{64f,96})
            {
                Part("Embedded reinforced footing",root,new Vector3(118,.09f,z),new Vector3(9.2f,.5f,3.4f),concrete,true);
                Part("Abutment stem",root,new Vector3(118,2.4f,z),new Vector3(8.4f,4.64f,2.2f),concrete,true,5);
                Part("Bearing shelf",root,new Vector3(118,4.67f,z),new Vector3(8.4f,.50f,2.5f),concrete,true);
                foreach(float x in new[]{114.55f,116.85f,119.15f,121.45f})
                    Part("Replaceable elastomer bearing",root,new Vector3(x,5.0f,z),new Vector3(.72f,.16f,.65f),Load("StreetLife_Rubber"));
                Part("Ballast backwall",root,new Vector3(118,5.47f,z+(z<80?-1.02f:1.02f)),new Vector3(8.4f,1.76f,.32f),concrete,true);
                foreach(int side in new[]{-1,1})
                {
                    Vector2 a=new Vector2(118+side*14.5f,z<80?51:109),b=new Vector2(118+side*2.6f,z<80?63:97);
                    Wing(root,a,b);
                }
                var dark=Material("Overpass_FormJoint",null,new Color(.29f,.30f,.28f),.05f);
                float face=z<80?z+1.105f:z-1.105f;
                foreach(float x in new[]{115.9f,118,120.1f})Part("Recessed vertical casting joint",root,new Vector3(x,2.4f,face),new Vector3(.012f,4.2f,.008f),dark);
                for(float y=.9f;y<4.6f;y+=1.35f)Part("Horizontal concrete lift seam",root,new Vector3(118,y,face),new Vector3(8.2f,.012f,.008f),dark);
                // Form-tie holes follow actual casting panels, not random large stains.
                foreach(float x in new[]{114.8f,116.9f,119.1f,121.2f})foreach(float y in new[]{1.6f,3.6f})
                {var hole=Cylinder("Recessed form tie",root,new Vector3(x,y,face),.019f,.009f,dark);hole.transform.rotation=Quaternion.Euler(90,0,0);}
            }
            foreach(float x in new[]{114.05f,121.95f})
            {
                Part("Bridge edge cornice",root,new Vector3(x,6.21f,80),new Vector3(.30f,.3f,34),concrete,true);
                Part("Maintenance walkway",root,new Vector3(x+(x<118?.28f:-.28f),6.33f,80),new Vector3(.75f,.16f,34),concrete,true);
                for(float z=63;z<=97;z+=2)
                {
                    Part("Parapet base plate",root,new Vector3(x,6.45f,z),new Vector3(.19f,.035f,.19f),metal);
                    Part("Maintenance parapet post",root,new Vector3(x,7.03f,z),new Vector3(.055f,1.15f,.055f),metal,true);
                }
                foreach(float h in new[]{6.72f,7.12f,7.60f})Part("Bridge continuous parapet",root,new Vector3(x,h,80),new Vector3(.055f,.055f,34),metal,true);
                for(float z=63.4f;z<97;z+=.20f)Part("Safety infill upright",root,new Vector3(x,7.05f,z),new Vector3(.018f,1.08f,.018f),metal);
                foreach(float z in new[]{65.8f,94.2f})
                {
                    Cylinder("Sealed rainwater downpipe",root,new Vector3(x,2.73f,z),.06f,5.35f,metal);
                    foreach(float y in new[]{.5f,2.4f,4.4f})Part("Drainpipe bracket",root,new Vector3(x,y,z),new Vector3(.22f,.04f,.22f),metal);
                    Part("Rainwater catchpit",root,new Vector3(x,.05f,z),new Vector3(.48f,.12f,.48f),concrete,true);
                }
            }
        }
        static void Wing(Transform parent,Vector2 a,Vector2 b)
        {
            var direction=(b-a).normalized;var side=new Vector2(-direction.y,direction.x)*.18f;
            var v=new List<Vector3>();
            foreach(var p in new[]{a-side,a+side,b+side,b-side})v.Add(new Vector3(p.x,-.12f,p.y));
            foreach(var p in new[]{a-side,a+side,b+side,b-side})v.Add(new Vector3(p.x,Vector2.Distance(p,a)<1?.30f:6.05f,p.y));
            var t=new List<int>{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            // Hard concrete faces must not average normals across prism corners.
            var flat=new List<Vector3>();foreach(int index in t)flat.Add(v[index]);v=flat;t=Enumerable.Range(0,flat.Count).ToList();
            var uv=v.Select(p=>new Vector2(Vector2.Dot(new Vector2(p.x,p.z)-a,direction),p.y)/4).ToList();
            var wingMaterial=Material("Overpass_RetainingConcrete","concrete",new Color(.72f,.74f,.72f),.13f);
            var wing=WestbruchBridgeStart.MeshPart("Rising retaining wing "+a.x+"_"+a.y,parent,v,uv,t,wingMaterial,true);
            wing.GetComponent<MeshRenderer>().receiveShadows=true;
        }
        static void Track(Transform root)
        {
            var railHead=Material("Overpass_PolishedRailHead",null,new Color(.51f,.54f,.54f),.58f,.86f);
            // A continuous non-electrified urban freight branch, standard European gauge.
            foreach(float x in new[]{117.2485f,118.7515f})
            {
                Part("Continuous rail foot",root,new Vector3(x,6.53f,0),new Vector3(.125f,.025f,1240),rail);
                Part("Continuous rail web",root,new Vector3(x,6.59f,0),new Vector3(.018f,.10f,1240),rail);
                Part("Continuous rail head",root,new Vector3(x,6.66f,0),new Vector3(.068f,.04f,1240),railHead);
            }
            // Ballast is a closed trapezoid, including bridge joins. Surface collision
            // is continuous rather than hundreds of little sleeper colliders.
            var v=new List<Vector3>();var t=new List<int>();const int sections=80;
            for(int i=0;i<=sections;i++)
            {
                float z=Mathf.Lerp(-620,620,i/(float)sections);
                v.AddRange(new[]{new Vector3(115.55f,5.95f,z),new Vector3(120.45f,5.95f,z),new Vector3(119.8f,6.43f,z),new Vector3(116.2f,6.43f,z)});
                if(i==sections)continue;int n=i*4;
                t.AddRange(new[]{n,n+4,n+7,n,n+7,n+3,n+3,n+7,n+6,n+3,n+6,n+2,n+2,n+6,n+5,n+2,n+5,n+1});
            }
            t.AddRange(new[]{0,3,2,0,2,1});int last=sections*4;t.AddRange(new[]{last,last+1,last+2,last,last+2,last+3});
            WestbruchBridgeStart.MeshPart("Continuous supported ballast formation",root,v,v.Select(p=>new Vector2(p.x*.6f,p.z*.6f)).ToList(),t,ballast,true);
            for(float z=-619.8f;z<620;z+=.62f)
            {
                Part("Prestressed rail sleeper",root,new Vector3(118,6.45f,z),new Vector3(2.58f,.16f,.24f),sleeper,false,1.3f);
                if(z<45||z>115)continue;
                foreach(float x in new[]{117.2485f,118.7515f})foreach(int side in new[]{-1,1})
                    Part("Elastic rail fastening",root,new Vector3(x+side*.10f,6.54f,z),new Vector3(.06f,.04f,.12f),rail);
            }
            for(float z=-608;z<620;z+=24)
            {
                if(z>60&&z<100)continue;
                var cabinet=Group("Railway lineside cabinet",root,new Vector3(120.3f,5.95f,z)).transform;
                Part("Weatherproof signal cabinet",cabinet,new Vector3(0,.48f,0),new Vector3(.42f,.90f,.34f),metal);
                Part("Cabinet concrete plinth",cabinet,new Vector3(0,.045f,0),new Vector3(.58f,.09f,.48f),concrete);
            }
            foreach(float x in new[]{115f,121f})foreach(var segment in new[]{new Vector2(-620,62.8f),new Vector2(97.2f,620)})
            {
                Part("Railway access barrier",root,new Vector3(x,6.5f,(segment.x+segment.y)*.5f),new Vector3(.05f,1.8f,segment.y-segment.x),metal,true).GetComponent<Renderer>().enabled=false;
                for(float z=segment.x;z<=segment.y;z+=3)Cylinder("Rail boundary post",root,new Vector3(x,6.45f,z),.027f,1.8f,metal);
                foreach(float h in new[]{6.3f,6.7f,7.1f})Part("Railway boundary tension wire",root,new Vector3(x,h,(segment.x+segment.y)*.5f),new Vector3(.014f,.014f,segment.y-segment.x),metal);
            }
        }
        static void Streets(Transform root)
        {
            Part("Connected Nordring east carriageway",root,new Vector3(128,.015f,80),new Vector3(56,.05f,10),asphalt,true);
            foreach(float z in new[]{73.5f,86.5f})Part("Continuous underpass pavement",root,new Vector3(119.5f,.0675f,z),new Vector3(39,.145f,3),paving,true);
            // A paved turning court makes a deliberate destination, with a rounded
            // usable turning envelope and no road ending in bare dirt.
            Part("Gewerbehof turning court",root,new Vector3(151,.015f,80),new Vector3(18,.05f,38),asphalt,true);
            Part("Court east pavement",root,new Vector3(161.5f,.0675f,80),new Vector3(3,.145f,40),paving,true);
            foreach(float z in new[]{67,93})Part("Court west pavement",root,new Vector3(140.5f,.0675f,z),new Vector3(3,.145f,14),paving,true);
            foreach(float z in new[]{59.5f,100.5f})Part("Court end pavement",root,new Vector3(151,.0675f,z),new Vector3(24,.145f,3),paving,true);
            // Kerbs stop at the open junction and at the turning court.
            foreach(float z in new[]{74.91f,85.09f})Part("Underpass kerb",root,new Vector3(120,.10f,z),new Vector3(40,.20f,.18f),concrete,true);
            foreach(float x in new[]{142.09f,159.91f})foreach(var range in new[]{new Vector2(61,74.85f),new Vector2(85.15f,99)})
                Part("Turning court kerb",root,new Vector3(x,.10f,(range.x+range.y)*.5f),new Vector3(.18f,.20f,range.y-range.x),concrete,true);
            foreach(float z in new[]{61.09f,98.91f})Part("Court end kerb",root,new Vector3(151,.10f,z),new Vector3(18,.20f,.18f),concrete,true);
            for(float x=104;x<140;x+=5)Part("Faded lane centre dash",root,new Vector3(x,.0425f,80),new Vector3(2.5f,.005f,.11f),paint);
            for(float z=75.5f;z<85;z+=1.0f)Part("Nordring pedestrian crossing paint",root,new Vector3(104,.044f,z),new Vector3(2.2f,.004f,.48f),paint);
            for(float x=146;x<156;x+=2.6f)
                foreach(float z in new[]{64f,96f})Part("Faded service parking bay",root,new Vector3(x,.043f,z),new Vector3(.07f,.004f,4),paint);
            foreach(var p in new[]{new Vector2(111,74.65f),new Vector2(126,74.65f),new Vector2(111,85.35f),new Vector2(126,85.35f)})
            {
                Part("Flush storm drain surround",root,new Vector3(p.x,.043f,p.y),new Vector3(.55f,.008f,.38f),metal);
                for(int i=0;i<7;i++)Part("Gully grate slot",root,new Vector3(p.x-.21f+i*.07f,.049f,p.y),new Vector3(.027f,.006f,.30f),rail);
            }
        }
        static void Boundaries(Transform root)
        {
            GroundFence(root,new Vector2(131.5f,-113.5f),new Vector2(131.5f,71.8f));
            GroundFence(root,new Vector2(131.5f,88.2f),new Vector2(131.5f,113.5f));
            GroundFence(root,new Vector2(131.5f,57.5f),new Vector2(164,57.5f));
            GroundFence(root,new Vector2(164,57.5f),new Vector2(164,112));
            GroundFence(root,new Vector2(164,112),new Vector2(131.5f,112));
        }
        static void GroundFence(Transform root,Vector2 a,Vector2 b)
        {
            int n=Mathf.CeilToInt(Vector2.Distance(a,b)/4.8f);
            for(int i=0;i<n;i++)
            {
                var p=Vector2.Lerp(a,b,i/(float)n);var q=Vector2.Lerp(a,b,(i+1)/(float)n);
                WestbruchProps.Fence(root,new Vector3(p.x,WestbruchOverpassGround.Surface(p.x,p.y),p.y),new Vector3(q.x,WestbruchOverpassGround.Surface(q.x,q.y),q.y));
            }
        }
        static void MaintenanceGarage(Transform root)
        {
            float floor=Floor(151,106)+.04f;
            var building=Group("Gewerbehof railway maintenance garage",root,new Vector3(151,floor,106)).transform;
            var brick=Material("Overpass_ServiceBrick","brick",new Color(.65f,.61f,.52f),.12f);
            var dark=Load("DressingMetal");
            Part("Garage reinforced slab",building,new Vector3(0,-.13f,0),new Vector3(10.4f,.30f,7.4f),concrete,true);
            Part("Service shed rear wall",building,new Vector3(0,1.65f,3.5f),new Vector3(10,3.3f,.32f),brick,true,3);
            foreach(float x in new[]{-4.85f,4.85f})Part("Service shed side wall",building,new Vector3(x,1.65f,0),new Vector3(.30f,3.3f,7),brick,true,3);
            foreach(float x in new[]{-3.5f,3.5f})Part("Garage entrance brick pier",building,new Vector3(x,1.65f,-3.5f),new Vector3(3,3.3f,.32f),brick,true,3);
            Part("Garage lintel",building,new Vector3(0,3.03f,-3.5f),new Vector3(4,.55f,.35f),concrete,true);
            Part("Closed corrugated roller door",building,new Vector3(0,1.36f,-3.56f),new Vector3(3.9f,2.72f,.09f),metal,true);
            for(float y=.15f;y<2.7f;y+=.16f)Part("Roller door seam",building,new Vector3(0,y,-3.615f),new Vector3(3.82f,.018f,.018f),dark);
            Part("Galvanised shed roof",building,new Vector3(0,3.38f,0),new Vector3(10.5f,.16f,7.6f),metal,true);
            foreach(float x in new[]{-4.95f,4.95f})Part("Roof rain gutter",building,new Vector3(x,3.29f,0),new Vector3(.10f,.10f,7.5f),metal);
            Cylinder("Workshop rainwater downpipe",building,new Vector3(-4.99f,1.65f,-3.4f),.045f,3.3f,metal);
            Part("Service entrance step",building,new Vector3(0,.02f,-3.9f),new Vector3(4,.08f,.65f),concrete,true);
            WestbruchSignage.Plate("deliveries","Anlieferung",building,new Vector3(3.5f,2.1f,-3.69f),new Vector2(1.05f,.38f),building,"Brickwork");
        }
        static void Details(Transform root)
        {
            var glow=Material("Overpass_SealedLampDiffuser",null,new Color(.86f,.84f,.74f),.22f);
            glow.SetColor("_EmissiveColor",new Color(3.2f,2.8f,2.1f));HDMaterial.ValidateMaterial(glow);EditorUtility.SetDirty(glow);
            foreach(float x in new[]{115.1f,120.9f})foreach(float z in new[]{70f,80,90})
            {
                Part("Flush vandal resistant luminaire",root,new Vector3(x,5.61f,z),new Vector3(.25f,.10f,.8f),metal);
                Part("Sealed warm white diffuser",root,new Vector3(x,5.55f,z),new Vector3(.18f,.018f,.66f),glow);
                var lamp=Group("Restrained underbridge illumination",root,new Vector3(x,5.35f,z)).AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.88f,.70f);lamp.range=7;lamp.shadows=LightShadows.None;lamp.gameObject.AddComponent<HDAdditionalLightData>();lamp.intensity=190;
            }
            // Reuse the district's authored curved streetlight rather than a second style.
            var prototype=GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name.StartsWith("Old European streetlight "));
            if(prototype)foreach(var p in new[]{new Vector3(106,.14f,86),new Vector3(136,.14f,73.9f),new Vector3(161,.14f,93)})
            {var light=UnityEngine.Object.Instantiate(prototype.gameObject,root);light.name="Reused district streetlight";foreach(var r in light.GetComponentsInChildren<MeshRenderer>())r.enabled=true;light.transform.position=p;light.transform.rotation=Quaternion.Euler(0,p.z>80?180:0,0);}
            MaintenanceGarage(root);
            var service=Group("Gewerbehof utilities and loading",root,new Vector3(161,0,105)).transform;
            Part("Recessed service cabinet",service,new Vector3(0,1,0),new Vector3(4.8f,2,.55f),metal,true);
            for(int i=0;i<3;i++)WestbruchProps.Pallet(root,new Vector3(157,.17f*i,92));
            WestbruchProps.Bin(root,new Vector3(157,.10f,67),180);
            WestbruchProps.Bin(root,new Vector3(158,.10f,67),180);
            Part("Crash bollard",root,new Vector3(157,.60f,90),new Vector3(.16f,1.2f,.16f),metal,true);
            var post=WestbruchSignage.Post(root,new Vector3(106,.14f,87.1f));
            WestbruchSignage.Plate("street_1","Nordring",post,new Vector3(0,2.55f,0),new Vector2(1.05f,.24f),post,"Post",90);
        }
    }
}
