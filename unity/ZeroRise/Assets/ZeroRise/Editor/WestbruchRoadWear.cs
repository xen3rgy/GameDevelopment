using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchRoadWear
    {
        public static void ConfigureAsphalt(Material mat)
        {
            mat.shader=Shader.Find("HDRP/Lit");
            // One absolute mapping across every road cell, including intersections.
            mat.SetFloat("_UVBase",4);mat.SetFloat("_TexWorldScale",.23f);
            mat.SetFloat("_SmoothnessRemapMin",.08f);mat.SetFloat("_SmoothnessRemapMax",.32f);
            HDMaterial.ValidateMaterial(mat);
            mat.shader=Shader.Find("ZeroRise/UrbanLit");mat.EnableKeyword("_ZR_ROAD");EditorUtility.SetDirty(mat);
        }
        public static void Build(Transform parent,WestbruchLayout layout)
        {
            var root=Group("Unique road wear – curved cracks and irregular repairs",parent,Vector3.zero).transform;
            var crack=Material("RoadFissure",null,new Color(.055f,.058f,.052f),.06f);
            var edge=Material("CrackBrokenEdge",null,new Color(.14f,.135f,.12f),.09f);
            var repairs=new Material[3];
            for(int i=0;i<3;i++)
            {
                repairs[i]=Material("RoadRepairOrganic_"+i,"asphalt",new Color(.35f+i*.045f,.36f+i*.04f,.35f+i*.038f),.17f);
                ConfigureAsphalt(repairs[i]);
            }
            var rng=new System.Random(70421);
            float Random(float min,float max)=>min+(float)rng.NextDouble()*(max-min);
            bool Inside(Vector2 p)=>Array.Exists(layout.roads,r=>Mathf.Abs(p.x-r.x)<r.w*.5f-.15f&&Mathf.Abs(p.y-r.z)<r.d*.5f-.15f);
            foreach(var road in layout.roads)
            {
                bool horizontal=road.w>road.d;float length=horizontal?road.w:road.d,width=horizontal?road.d:road.w;
                Vector2 World(float along,float across)=>horizontal?new Vector2(road.x+along,road.z+across):new Vector2(road.x+across,road.z+along);
                for(int patch=0;patch<Mathf.RoundToInt(length/18);patch++)
                {
                    var center=World(Random(-length*.5f+3,length*.5f-3),Random(-width*.31f,width*.31f));
                    float rx=Random(.5f,1.9f),rz=Random(.35f,1.25f),yaw=Random(0,Mathf.PI);
                    var points=new List<Vector2>();
                    for(int i=0;i<12;i++)
                    {
                        float a=i*Mathf.PI*2/12,r=Random(.83f,1.15f);
                        var delta=new Vector2(Mathf.Cos(a)*rx*r,Mathf.Sin(a)*rz*r);
                        points.Add(center+new Vector2(delta.x*Mathf.Cos(yaw)-delta.y*Mathf.Sin(yaw),delta.x*Mathf.Sin(yaw)+delta.y*Mathf.Cos(yaw)));
                    }
                    if(points.TrueForAll(p=>Inside(p))) Polygon(root,road.id+" irregular repair "+patch,points,.0418f,repairs[patch%3]);
                }
                for(int run=0;run<Mathf.RoundToInt(length/3.4f);run++)
                {
                    var points=new List<Vector2>();
                    var point=World(Random(-length*.5f+1,length*.5f-1),Random(-width*.43f,width*.43f));
                    float angle=Random(0,Mathf.PI*2),turn=Random(-.045f,.045f);int steps=rng.Next(9,31);
                    for(int i=0;i<steps;i++)
                    {
                        if(!Inside(point))break;points.Add(point);
                        angle+=Random(-.25f,.25f)+turn;float step=Random(.14f,.43f);
                        point+=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*step;
                    }
                    if(points.Count<3)continue;
                    float fissure=Random(.013f,.032f);
                    Ribbon(root,road.id+" weathered crack edge "+run,points,fissure*2.3f,.0423f,edge);
                    Ribbon(root,road.id+" unique crack "+run,points,fissure,.043f,crack);
                    // Asymmetric branches follow the same root but diverge differently.
                    for(int branch=0;branch<rng.Next(1,4);branch++)
                    {
                        point=points[rng.Next(1,points.Count-1)];angle=Random(0,Mathf.PI*2);
                        var branchPoints=new List<Vector2>();
                        for(int i=0;i<rng.Next(4,11);i++)
                        {
                            if(!Inside(point))break;branchPoints.Add(point);
                            angle+=Random(-.7f,.7f);point+=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random(.12f,.32f);
                        }
                        if(branchPoints.Count>1)Ribbon(root,road.id+" crack branch "+run+"_"+branch,branchPoints,fissure*.65f,.0431f,crack);
                    }
                }
            }
        }
        static void Ribbon(Transform parent,string name,List<Vector2> points,float width,float y,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<points.Count;i++)
            {
                var direction=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var normal=new Vector2(-direction.y,direction.x)*width;
                // Taper tips, vary thickness without a repeating sawtooth pattern.
                float taper=Mathf.Sin(Mathf.PI*(i+.5f)/points.Count)*(.78f+.22f*Mathf.PerlinNoise(points[i].x,points[i].y));
                foreach(int s in new[]{-1,1}){var p=points[i]+normal*s*taper;v.Add(new Vector3(p.x,y,p.y));uv.Add(p*.23f);}
                if(i>0){int n=i*2;t.AddRange(new[]{n-2,n-1,n,n-1,n+1,n});}
            }
            Save(parent,name,v,uv,t,material);
        }
        static void Polygon(Transform parent,string name,List<Vector2> points,float y,Material material)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            var center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Count;
            v.Add(new Vector3(center.x,y,center.y));uv.Add(center*.23f);
            foreach(var p in points){v.Add(new Vector3(p.x,y,p.y));uv.Add(p*.23f);}
            for(int i=0;i<points.Count;i++)t.AddRange(new[]{0,1+(i+1)%points.Count,1+i});
            Save(parent,name,v,uv,t,material);
        }
        static void Save(Transform parent,string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material material)
        {
            string path=Generated+"/Meshes/Wear_"+name.Replace(' ','_')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
            // Place source at its own bounds center so normal 32 m batching works.
            var center=mesh.bounds.center;
            for(int i=0;i<vertices.Count;i++)vertices[i]-=center;mesh.SetVertices(vertices);mesh.RecalculateBounds();
            var obj=Group(name,parent,center);obj.isStatic=true;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
