using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Structural checks for the player-visible failures in the supplied images.
    // Source geometry remains available after batching, so roof tests never
    // mistake a solid house collider for a complete, rendered roof.
    internal static class BahnhofPlacementQA
    {
        sealed class Footprint
        {
            public string name;
            public Vector2[] corners;
            public float bottom,top;
        }
        struct Triangle
        {
            public Vector3 a,b,c;
        }
        public static void Validate(Transform root,Action<string,bool,string> record)
        {
            if(!root){record("Structural placement has a district",false,"Missing district root");return;}
            Physics.SyncTransforms();
            var houses=root.Cast<Transform>().Where(t=>t.name.StartsWith("BV2_House_",StringComparison.Ordinal)).ToArray();
            var industry=root.Cast<Transform>().Where(t=>t.name.StartsWith("BV2_Industrial_",StringComparison.Ordinal)).ToArray();
            Foundations(houses,industry,record);
            var footprints=Footprints(root,houses,industry,record);
            BackdropGeometry(footprints,record);
            RoofCoverage(houses,record);
            RootedFoliage(record);
            BuriedPiers(root,record);
            LandscapeContinuity(record);
        }
        static void Foundations(Transform[] houses,Transform[] industry,Action<string,bool,string> record)
        {
            var faults=new List<string>();int inspected=0;
            foreach(var building in houses.Concat(industry))
            {
                var foundation=building.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.IndexOf("foundation",StringComparison.OrdinalIgnoreCase)>=0);
                if(!foundation||!foundation.sharedMesh||!foundation.sharedMesh.isReadable||foundation.sharedMesh.vertexCount==0)
                {faults.Add(building.name+": no readable continuous foundation");continue;}
                inspected++;var bounds=WorldBounds(foundation);
                if(bounds.min.y>-.20f||Mathf.Abs(bounds.max.y-.14f)>.025f)
                    faults.Add(building.name+": foundation spans y="+bounds.min.y.ToString("F3")+" to "+bounds.max.y.ToString("F3"));
                BoxCollider core=building.GetComponent<BoxCollider>();
                if(!core)core=building.GetComponentsInChildren<BoxCollider>(true).FirstOrDefault(c=>c.name.Contains("structural core")||c.name.Contains("brick body"));
                if(core)
                {
                    var body=ColliderBounds(core);
                    if(bounds.min.x>body.min.x+.025f||bounds.max.x<body.max.x-.025f||bounds.min.z>body.min.z+.025f||bounds.max.z<body.max.z-.025f)
                        faults.Add(building.name+": foundation does not cover its main wall footprint");
                }
            }
            record("Main buildings have buried, flush continuous foundations",houses.Length==12&&industry.Length>=3&&faults.Count==0,Details(faults,inspected+" foundation meshes measured; bottom ≤ -0.20 m, top 0.14 m"));
        }
        static List<Footprint> Footprints(Transform root,Transform[] houses,Transform[] industry,Action<string,bool,string> record)
        {
            var list=new List<Footprint>();var faults=new List<string>();
            foreach(var building in houses.Concat(industry))
            {
                var core=building.GetComponent<BoxCollider>();
                if(!core)core=building.GetComponentsInChildren<BoxCollider>(true).FirstOrDefault(c=>c.name.Contains("structural core")||c.name.Contains("brick body"));
                if(!core){faults.Add(building.name+": missing structural footprint");continue;}
                list.Add(FromCollider(building.name,core));
            }
            var station=root.Find("Bahnhof Westbruch – accessible ticket hall");
            var stationFloor=station?station.GetComponentsInChildren<BoxCollider>(true).FirstOrDefault(c=>c.name=="BV2_Terrazzo hall floor"):null;
            if(stationFloor)
            {
                var item=FromCollider(station.name,stationFloor);item.top=station.position.y+8;list.Add(item);
            }
            else faults.Add("Station: missing physical main floor footprint");
            for(int i=0;i<list.Count;i++)for(int j=i+1;j<list.Count;j++)
                if(Overlap(list[i].corners,list[j].corners,.025f))faults.Add(list[i].name+" intersects "+list[j].name);
            record("Main building footprints do not intersect",faults.Count==0&&list.Count>=16,Details(faults,list.Count+" oriented structural footprints checked pairwise"));
            return list;
        }
        static Footprint FromCollider(string name,BoxCollider collider)
        {
            var c=collider.center;var half=collider.size*.5f;var points=new List<Vector2>();
            foreach(var offset in new[]{new Vector3(-half.x,0,-half.z),new Vector3(half.x,0,-half.z),new Vector3(half.x,0,half.z),new Vector3(-half.x,0,half.z)})
            {var p=collider.transform.TransformPoint(c+offset);points.Add(new Vector2(p.x,p.z));}
            var bounds=ColliderBounds(collider);
            return new Footprint{name=name,corners=points.ToArray(),bottom=bounds.min.y,top=bounds.max.y};
        }
        static bool Overlap(Vector2[] a,Vector2[] b,float penetration)
        {
            foreach(var polygon in new[]{a,b})for(int i=0;i<polygon.Length;i++)
            {
                var edge=polygon[(i+1)%polygon.Length]-polygon[i];if(edge.sqrMagnitude<1e-12f)continue;
                var axis=new Vector2(-edge.y,edge.x).normalized;
                float amin=float.PositiveInfinity,amax=float.NegativeInfinity,bmin=float.PositiveInfinity,bmax=float.NegativeInfinity;
                foreach(var p in a){float d=Vector2.Dot(p,axis);amin=Mathf.Min(amin,d);amax=Mathf.Max(amax,d);}
                foreach(var p in b){float d=Vector2.Dot(p,axis);bmin=Mathf.Min(bmin,d);bmax=Mathf.Max(bmax,d);}
                if(Mathf.Min(amax,bmax)-Mathf.Max(amin,bmin)<=penetration)return false;
            }
            return true;
        }
        static void BackdropGeometry(List<Footprint> footprints,Action<string,bool,string> record)
        {
            var faults=new List<string>();int triangles=0,objects=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!mesh||!mesh.isReadable)continue;
                var materials=renderer.sharedMaterials;var v=mesh.vertices;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    if(materials.Length==0)continue;
                    var material=materials[Mathf.Min(sub,materials.Length-1)];
                    if(!material||!material.name.StartsWith("BackdropFacade",StringComparison.Ordinal))continue;
                    objects++;var indices=mesh.GetTriangles(sub);
                    for(int i=0;i+2<indices.Length;i+=3)
                    {
                        triangles++;var a=filter.transform.TransformPoint(v[indices[i]]);var b=filter.transform.TransformPoint(v[indices[i+1]]);var c=filter.transform.TransformPoint(v[indices[i+2]]);
                        float minY=Mathf.Min(a.y,b.y,c.y),maxY=Mathf.Max(a.y,b.y,c.y);
                        var triangle=new[]{new Vector2(a.x,a.z),new Vector2(b.x,b.z),new Vector2(c.x,c.z)};
                        foreach(var building in footprints)
                        {
                            if(maxY<building.bottom+.10f||minY>building.top+.20f||!TrianglePenetrates(building,triangle))continue;
                            string fault=building.name+" overlaps active backdrop triangles in "+filter.name;
                            if(!faults.Contains(fault)&&faults.Count<12)faults.Add(fault);
                        }
                    }
                }
            }
            record("Rendered original skyline does not penetrate the new buildings",faults.Count==0,Details(faults,triangles+" rendered BackdropFacade triangles across "+objects+" submeshes checked"));
        }
        static bool TrianglePenetrates(Footprint building,Vector2[] triangle)
        {
            // Vertical wall triangles collapse to a segment when projected on
            // the ground. Positive-area polygon SAT would wrongly skip them.
            for(int i=0;i<triangle.Length;i++)if(SegmentInside(building,triangle[i],triangle[(i+1)%triangle.Length],.025f))return true;
            return false;
        }
        static bool SegmentInside(Footprint building,Vector2 a,Vector2 b,float clearance)
        {
            var start=building.corners[0];var x=building.corners[1]-start;var z=building.corners[3]-start;
            float near=0,far=1;
            foreach(var edge in new[]{x,z})
            {
                float length=edge.magnitude;if(length<clearance*2)return false;var axis=edge/length;
                float origin=Vector2.Dot(a-start,axis),delta=Vector2.Dot(b-a,axis);
                if(Mathf.Abs(delta)<1e-8f)
                {if(origin<clearance||origin>length-clearance)return false;continue;}
                float t0=(clearance-origin)/delta,t1=(length-clearance-origin)/delta;
                if(t0>t1){float swap=t0;t0=t1;t1=swap;}
                near=Mathf.Max(near,t0);far=Mathf.Min(far,t1);if(far<near)return false;
            }
            return far>=near;
        }
        static void RoofCoverage(Transform[] houses,Action<string,bool,string> record)
        {
            var faults=new List<string>();int samples=0;
            foreach(var house in houses)
            {
                var core=house.GetComponent<BoxCollider>();if(!core){faults.Add(house.name+": no core for roof probe layout");continue;}
                var triangles=new List<Triangle>();
                foreach(var filter in house.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.name.StartsWith("BV2_Roof_",StringComparison.Ordinal))continue;
                    var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable)continue;var v=mesh.vertices;var t=mesh.triangles;
                    for(int i=0;i+2<t.Length;i+=3)
                    {
                        var a=filter.transform.TransformPoint(v[t[i]]);var b=filter.transform.TransformPoint(v[t[i+1]]);var c=filter.transform.TransformPoint(v[t[i+2]]);
                        if(Vector3.Cross(b-a,c-a).y>.001f)triangles.Add(new Triangle{a=a,b=b,c=c});
                    }
                }
                // Flat roofs are actual weathering slabs. Do not use ceiling,
                // cornice, parapet, collider, or decorative seam geometry instead.
                foreach(var filter in house.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name=="BV2_Single flat roof weathering slab"))
                {
                    var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable)continue;var v=mesh.vertices;var t=mesh.triangles;
                    for(int i=0;i+2<t.Length;i+=3)
                    {
                        var a=filter.transform.TransformPoint(v[t[i]]);var b=filter.transform.TransformPoint(v[t[i+1]]);var c=filter.transform.TransformPoint(v[t[i+2]]);
                        if(Vector3.Cross(b-a,c-a).y>.001f)triangles.Add(new Triangle{a=a,b=b,c=c});
                    }
                }
                float wallTop=ColliderBounds(core).max.y;
                foreach(float x in new[]{-.32f,0,.32f})foreach(float z in new[]{-.32f,0,.32f})
                {
                    if(x==0&&z==0)continue;samples++;
                    var point=house.TransformPoint(new Vector3(core.center.x+core.size.x*x,0,core.center.z+core.size.z*z));
                    bool covered=triangles.Any(t=>HeightAt(t,point,out float y)&&y>=wallTop-.06f&&y<=wallTop+7);
                    if(!covered)faults.Add(house.name+": open rendered roof above "+point.ToString("F2"));
                }
            }
            record("House roofs cover their interiors with upward rendered surfaces",houses.Length==12&&samples==96&&faults.Count==0,Details(faults,samples+" interior roof projections on original roof meshes"));
        }
        static void RootedFoliage(Action<string,bool,string> record)
        {
            var faults=new List<string>();var rootsTested=new HashSet<Vector3Int>();int meshes=0;float maximumError=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||!renderer.sharedMaterial||!renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE")||!mesh||!mesh.isReadable)continue;
                var roots=mesh.uv4;var heights=mesh.uv3;
                if(roots.Length!=mesh.vertexCount||heights.Length!=mesh.vertexCount)continue;
                meshes++;
                // Meshes trimmed at the excavation retain unused vertex slots.
                // Only indices still referenced by visible triangles are plants.
                foreach(int i in mesh.triangles.Distinct())
                {
                    float x=roots[i].x,z=roots[i].y,y=heights[i].x;
                    if(x<125.6f||x>600||z<58||z>600)continue;
                    var key=new Vector3Int(Mathf.RoundToInt(x*1000),Mathf.RoundToInt(y*1000),Mathf.RoundToInt(z*1000));if(!rootsTested.Add(key))continue;
                    if(!LandHeight(x,z,out float ground))
                    {if(faults.Count<12)faults.Add("No land supports plant root "+new Vector3(x,y,z).ToString("F3"));continue;}
                    float delta=y-ground;maximumError=Mathf.Max(maximumError,Mathf.Abs(delta));
                    if((delta>.06f||delta<-.12f)&&faults.Count<12)faults.Add("Root "+new Vector3(x,y,z).ToString("F3")+" is "+delta.ToString("F3")+" m above its land support");
                }
            }
            record("Referenced grass roots are supported by actual authored land",rootsTested.Count>0&&faults.Count==0,Details(faults,rootsTested.Count+" used metadata roots / "+meshes+" foliage meshes; max root-ground offset "+maximumError.ToString("F3")+" m"));
        }
        static bool LandHeight(float x,float z,out float height)
        {
            height=float.NegativeInfinity;
            foreach(var hit in Physics.RaycastAll(new Vector3(x,30,z),Vector3.down,80,~0,QueryTriggerInteraction.Ignore))
            {
                string name=hit.collider.name;
                bool land=hit.collider is TerrainCollider||name.StartsWith("BV2_Surface ",StringComparison.Ordinal)||name.StartsWith("BV3_Ground ",StringComparison.Ordinal)||name.Contains("retained soil",StringComparison.OrdinalIgnoreCase)||name.Contains("garden soil",StringComparison.OrdinalIgnoreCase)||name.Contains("Raised bed soil",StringComparison.OrdinalIgnoreCase)||name=="Continuous North railway earth bank"||name=="Passing loop earth formation"||name=="Land below distant railway 1";
                if(land)height=Mathf.Max(height,hit.point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
        static void BuriedPiers(Transform root,Action<string,bool,string> record)
        {
            var faults=new List<string>();int count=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.Contains("Ramp supporting pier")||f.name.Contains("Entrance landing supported pier")))
            {
                count++;var b=WorldBounds(filter);
                foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    float px=b.center.x+x*b.extents.x,pz=b.center.z+z*b.extents.z;
                    if(!LandHeight(px,pz,out float y)||b.min.y>y-.04f)
                        faults.Add(filter.name+" at "+b.center.ToString("F2")+": lower edge is not buried at footprint corner");
                }
            }
            record("Ramp and landing piers penetrate their actual ground",count>0&&faults.Count==0,Details(faults,count+" pier bases checked at all four footprint corners"));
        }
        static void LandscapeContinuity(Action<string,bool,string> record)
        {
            var faults=new List<string>();int probes=0;
            foreach(var point in new[]{new Vector2(80,260),new Vector2(0,300),new Vector2(-120,340),new Vector2(345,210),new Vector2(270,390),new Vector2(400,450)})
            {probes++;if(!LandHeight(point.x,point.y,out _))faults.Add("Missing surrounding land at "+point);}
            foreach(float z in new[]{100f,210,300,352})
            {
                probes+=2;
                if(!LandHeight(299.95f,z,out float a)||!LandHeight(300.05f,z,out float b)||Mathf.Abs(a-b)>.08f)faults.Add("Open east land seam at z="+z);
            }
            foreach(float x in new[]{155f,210,280})
            {
                probes+=2;
                if(!LandHeight(x,363.95f,out float a)||!LandHeight(x,364.05f,out float b)||Mathf.Abs(a-b)>.08f)faults.Add("Open north land seam at x="+x);
            }
            record("Surrounding land continues across station-quarter edges",faults.Count==0,Details(faults,probes+" land and boundary probes including west of railway"));
        }
        static bool HeightAt(Triangle t,Vector3 point,out float height)
        {
            float x1=t.b.x-t.a.x,z1=t.b.z-t.a.z,x2=t.c.x-t.a.x,z2=t.c.z-t.a.z;
            float denominator=x1*z2-z1*x2;height=0;if(Mathf.Abs(denominator)<1e-8f)return false;
            float x=point.x-t.a.x,z=point.z-t.a.z;
            float b=(x*z2-z*x2)/denominator,c=(x1*z-z1*x)/denominator,a=1-b-c;
            if(a<-.0001f||b<-.0001f||c<-.0001f)return false;
            height=a*t.a.y+b*t.b.y+c*t.c.y;return true;
        }
        static Bounds WorldBounds(MeshFilter filter)
        {
            var points=filter.sharedMesh.vertices;var bounds=new Bounds(filter.transform.TransformPoint(points[0]),Vector3.zero);
            for(int i=1;i<points.Length;i++)bounds.Encapsulate(filter.transform.TransformPoint(points[i]));return bounds;
        }
        static Bounds ColliderBounds(BoxCollider collider)
        {
            var bounds=new Bounds(collider.transform.TransformPoint(collider.center),Vector3.zero);var half=collider.size*.5f;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})bounds.Encapsulate(collider.transform.TransformPoint(collider.center+Vector3.Scale(half,new Vector3(x,y,z))));return bounds;
        }
        static string Details(List<string> faults,string success)=>faults.Count==0?success:string.Join("; ",faults.Take(12));
    }
}
