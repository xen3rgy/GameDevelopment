using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Cuts only the existing crossing landings. Roads, navigation links and
    // their positions remain authoritative; the new ramps meet those links.
    internal static class WestbruchUrbanTransitions
    {
        public const string Name="Westbruch accessible crossings and drainage";
        public static readonly List<Bounds> Cuts=new List<Bounds>();
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            var root=Group(Name,parent,Vector3.zero).transform;Cuts.Clear();
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();
            if(!world||world.crossings==null)throw new Exception("Existing pedestrian crossing links required.");
            var paving=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Paving.mat");
            var concrete=Material("UrbanTransition_TactileConcrete","concrete",new Color(.64f,.63f,.55f),.08f);
            var iron=Material("UrbanTransition_CastIron","steel",new Color(.16f,.18f,.17f),.12f,.62f);
            var dark=Material("UrbanTransition_Recess",null,new Color(.075f,.082f,.075f),.03f);
            int id=0;
            foreach(var crossing in world.crossings)
            {
                bool alongZ=Mathf.Abs(crossing.b.z-crossing.a.z)>Mathf.Abs(crossing.b.x-crossing.a.x);
                foreach(var end in new[]{crossing.a,crossing.b})
                {
                    var middle=(crossing.a+crossing.b)*.5f;
                    var inward=(end-middle);inward.y=0;inward.Normalize();
                    var road=plan.roads.Where(p=>alongZ?p.w>p.d:p.d>p.w)
                        .OrderBy(p=>alongZ?Mathf.Abs(p.z-middle.z):Mathf.Abs(p.x-middle.x)).First();
                    Vector3 edge=alongZ?new Vector3(end.x,0,road.z+Mathf.Sign(inward.z)*road.d*.5f):new Vector3(road.x+Mathf.Sign(inward.x)*road.w*.5f,0,end.z);
                    Vector3 lateral=Vector3.Cross(Vector3.up,inward);
                    var cut=new Bounds(edge+inward*.90f+Vector3.up*.10f,alongZ?new Vector3(3.10f,.24f,1.80f):new Vector3(1.80f,.24f,3.10f));
                    Cuts.Add(cut);
                    var landing=Group("Dropped crossing landing "+id++,root,edge,Quaternion.LookRotation(inward).eulerAngles.y).transform;
                    Ramp(landing,paving);
                    // Tactile ribs sit on the level rear landing, away from wheels.
                    for(int rib=0;rib<10;rib++)Box("Tactile approach rib",landing,new Vector3(-1.05f+rib*.2333f,.145f,1.58f),new Vector3(.025f,.010f,.32f),concrete,false,.6f);
                }
            }
            CutExisting(parent.root,root);
            Physics.SyncTransforms();
            int drainId=0;
            foreach(var road in plan.roads)
            {
                bool horizontal=road.w>road.d;float length=horizontal?road.w:road.d,half=(horizontal?road.d:road.w)*.5f;
                for(float along=-length*.5f+18;along<length*.5f-9;along+=32)
                foreach(int side in new[]{-1,1})
                {
                    Vector3 p=horizontal?new Vector3(road.x+along,0,road.z+side*(half-.30f)):new Vector3(road.x+side*(half-.30f),0,road.z+along);
                    if(plan.roads.Any(r=>r!=road&&Mathf.Abs(p.x-r.x)<r.w*.5f+1&&Mathf.Abs(p.z-r.z)<r.d*.5f+1))continue;
                    if(world.crossings.Any(c=>Distance(p,c.a,c.b)<2.4f))continue;
                    var drain=Group("Seated street inlet "+drainId++,root,p,horizontal?90:0).transform;
                    Box("Flush drain recess",drain,new Vector3(0,.043f,0),new Vector3(.42f,.012f,.62f),dark,false,.6f);
                    foreach(int s in new[]{-1,1})Box("Cast inlet rim",drain,new Vector3(s*.21f,.05f,0),new Vector3(.025f,.018f,.66f),iron,false,.5f);
                    foreach(int s in new[]{-1,1})Box("Cast inlet end",drain,new Vector3(0,.05f,s*.32f),new Vector3(.40f,.018f,.025f),iron,false,.5f);
                    for(int slat=0;slat<9;slat++)Box("Load bearing inlet bar",drain,new Vector3(0,.05f,-.26f+slat*.065f),new Vector3(.39f,.018f,.024f),iron,false,.5f);
                    // Individual gutter stones follow the kerb, with real joints.
                    for(int stone=-4;stone<=4;stone++)if(Mathf.Abs(stone)>1)
                        Box("Gutter setts",drain,new Vector3(0,.042f,stone*.33f),new Vector3(.37f,.012f,.315f),concrete,false,.8f);
                }
            }
            foreach(var p in new[]{new Vector3(-47,0,1.9f),new Vector3(46,0,81.7f),new Vector3(92,0,-45),new Vector3(-92,0,35)})
            {
                var cover=Cylinder("Flush inspection cover",root,p+Vector3.up*.046f,.30f,.014f,iron);
                for(int i=-3;i<=3;i++)Box("Cover anti-slip ribs",root,p+new Vector3(0,.055f,i*.067f),new Vector3(Mathf.Sqrt(.08f-i*i*.0045f)*2,.005f,.014f),iron,false,.5f);
            }
            File.WriteAllText("QA/urban-transitions.txt","Crossing landings: "+id+"\nStreet inlets: "+drainId+"\nOriginal navigation crossings retained: "+world.crossings.Length+"\n");
        }
        static float Distance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
        static void Ramp(Transform root,Material material)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            float[] xs={-1.55f,-1.25f,1.25f,1.55f},zs={0,1.35f,1.80f};
            Vector3 Top(int x,int z)=>new Vector3(xs[x],x==0||x==3?.14f:Mathf.Lerp(.055f,.14f,Mathf.Clamp01(zs[z]/1.35f)),zs[z]);
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});foreach(var p in new[]{a,b,c,d}){var w=root.TransformPoint(p);uv.Add(new Vector2(w.x,w.z)/3);}t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int x=0;x<3;x++)for(int z=0;z<2;z++)Face(Top(x,z),Top(x,z+1),Top(x+1,z+1),Top(x+1,z));
            Face(new Vector3(-1.55f,-.025f,0),new Vector3(-1.55f,.14f,0),new Vector3(1.55f,.14f,0),new Vector3(1.55f,-.025f,0));
            // Replace the front face by individual sections following the lip.
            v.RemoveRange(v.Count-4,4);uv.RemoveRange(uv.Count-4,4);t.RemoveRange(t.Count-6,6);
            for(int x=0;x<3;x++)Face(new Vector3(xs[x],-.025f,0),Top(x,0),Top(x+1,0),new Vector3(xs[x+1],-.025f,0));
            Face(new Vector3(-1.55f,-.025f,1.8f),new Vector3(1.55f,-.025f,1.8f),Top(3,2),Top(0,2));
            Face(new Vector3(-1.55f,-.025f,0),new Vector3(-1.55f,-.025f,1.8f),Top(0,2),Top(0,0));
            Face(new Vector3(1.55f,-.025f,1.8f),new Vector3(1.55f,-.025f,0),Top(3,0),Top(3,2));
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            mesh=Save(mesh,"Ramp_"+root.name.Replace(' ','_'));var o=Group("Continuous flared pavement ramp",root,Vector3.zero);o.isStatic=true;o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;o.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        struct Vertex
        {
            public Vector3 p,n;public Vector2 uv;public Vector4 tangent;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),tangent=Vector4.Lerp(a.tangent,b.tangent,t)};
        }
        static List<Vertex> Clip(List<Vertex> polygon,int axis,float bound,bool positive)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;var a=polygon[polygon.Count-1];float da=(a.p[axis]-bound)*(positive?1:-1);
            foreach(var b in polygon){float db=(b.p[axis]-bound)*(positive?1:-1);if((da>=0)!=(db>=0))result.Add(Vertex.Lerp(a,b,da/(da-db)));if(db>=0)result.Add(b);a=b;da=db;}return result;
        }
        static void CutExisting(Transform district,Transform own)
        {
            foreach(var f in district.GetComponentsInChildren<MeshFilter>().Where(f=>!f.transform.IsChildOf(own)).ToArray())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!f.sharedMesh||!f.sharedMesh.isReadable)continue;
                bool source=f.name=="Connected pavement"||f.name=="Kerb";
                bool batch=r.enabled&&(f.name.StartsWith("Paving_")||f.name.StartsWith("StoneTrim_"));
                if(!source&&!batch)continue;
                var cuts=Cuts.Where(c=>r.bounds.Intersects(c)).ToArray();if(cuts.Length==0)continue;
                var original=f.sharedMesh;var vs=original.vertices;var ns=original.normals;var uvs=original.uv;var tangents=original.tangents;var ts=original.triangles;
                var polygons=new List<List<Vertex>>();
                for(int i=0;i<ts.Length;i+=3){var p=new List<Vertex>();for(int j=0;j<3;j++){int k=ts[i+j];p.Add(new Vertex{p=f.transform.TransformPoint(vs[k]),n=ns.Length==vs.Length?f.transform.TransformDirection(ns[k]):Vector3.up,uv=uvs.Length==vs.Length?uvs[k]:Vector2.zero,tangent=tangents.Length==vs.Length?tangents[k]:new Vector4(1,0,0,1)});}polygons.Add(p);}
                foreach(var cut in cuts)
                {
                    var kept=new List<List<Vertex>>();
                    foreach(var polygon in polygons)
                    {
                        var inside=polygon;
                        foreach(int axis in new[]{0,1,2})foreach(bool minimum in new[]{true,false})
                        {
                            float boundary=minimum?cut.min[axis]:cut.max[axis];
                            // A face on the opening boundary belongs to the cut
                            // too. Keeping that old vertical kerb face leaves a
                            // zero-thickness collision wall at the ramp lip.
                            if(inside.Count>=3&&inside.All(p=>Mathf.Abs(p.p[axis]-boundary)<.00001f))continue;
                            var outside=Clip(inside,axis,boundary,!minimum);if(outside.Count>=3)kept.Add(outside);
                            inside=Clip(inside,axis,boundary,minimum);if(inside.Count<3)break;
                        }
                    }
                    polygons=kept;
                }
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tangent=new List<Vector4>();var indices=new List<int>();
                foreach(var poly in polygons){int n=vertices.Count;foreach(var p in poly){vertices.Add(f.transform.InverseTransformPoint(p.p));normals.Add(f.transform.InverseTransformDirection(p.n));uv.Add(p.uv);tangent.Add(p.tangent);}for(int i=1;i<poly.Count-1;i++){if(Vector3.Cross(poly[i].p-poly[0].p,poly[i+1].p-poly[0].p).sqrMagnitude<1e-12f)continue;indices.AddRange(new[]{n,n+i,n+i+1});}}
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTangents(tangent);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
                string suffix=source?f.name+"_"+f.transform.position.ToString("F4"):f.name;mesh=Save(mesh,"Cut_"+suffix.Replace(' ','_').Replace('(','_').Replace(')','_').Replace(',','_'));
                f.sharedMesh=mesh;
                if(source){foreach(var c in f.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);if(indices.Count>=3)f.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;}
            }
        }
        static Mesh Save(Mesh mesh,string key)
        {
            string path=Generated+"/Meshes/UrbanTransition_"+key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
            mesh.name=key;AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
