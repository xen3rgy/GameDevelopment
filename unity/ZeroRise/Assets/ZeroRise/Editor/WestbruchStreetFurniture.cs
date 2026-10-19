using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchStreetFurniture
    {
        public static void BuildLights(Transform parent,WestbruchLayout plan)
        {
            Physics.SyncTransforms();
            var root=Group("Street lighting - pavement edge layout",parent,Vector3.zero).transform;
            var steel=Material("StreetlightWeatheredSteel","steel",new Color(.33f,.37f,.34f),.28f,.60f);
            var rim=Material("StreetlightCastHousing","steel",new Color(.20f,.24f,.23f),.27f,.55f);
            var lens=Material("StreetlightFrostedGlass",null,new Color(.69f,.63f,.45f),.24f);
            lens.SetColor("_EmissiveColor",new Color(.85f,.64f,.30f)*1.8f);HDMaterial.ValidateMaterial(lens);EditorUtility.SetDirty(lens);
            var concrete=Material("StreetlightFooting","concrete",new Color(.59f,.59f,.54f),.10f);
            var pole=Tube("StreetlightTaperedPole",new[]{Vector3.zero,new Vector3(0,.12f,0),new Vector3(0,1.2f,0),new Vector3(0,4.9f,0),new Vector3(0,5.66f,0)},new[]{.092f,.085f,.069f,.047f,.045f});
            var arm=Tube("StreetlightCurvedArm",new[]{new Vector3(0,4.98f,0),new Vector3(0,5.25f,.035f),new Vector3(0,5.49f,.14f),new Vector3(0,5.67f,.34f),new Vector3(0,5.75f,.62f),new Vector3(0,5.77f,1.03f)},new[]{.047f,.046f,.044f,.041f,.037f,.032f});
            var head=Housing("StreetlightOvalHousing",.43f,.17f,.14f);
            var diffuser=Housing("StreetlightOvalDiffuser",.37f,.135f,.025f);
            var placed=new List<Vector3>();
            var approaches=WestbruchBuilder.WalkPlan(plan).Where(p=>p.id.EndsWith(" approach")).ToArray();
            bool Inside(Plot p,float x,float z,float margin)=>Mathf.Abs(x-p.x)<p.w*.5f+margin&&Mathf.Abs(z-p.z)<p.d*.5f+margin;
            int count=0;
            foreach(var road in plan.roads)
            {
                bool horizontal=road.w>road.d;float length=horizontal?road.w:road.d,halfWidth=(horizontal?road.d:road.w)*.5f;
                var along=horizontal?Vector3.right:Vector3.forward;var normal=horizontal?Vector3.forward:Vector3.right;
                int slot=0;
                for(float distance=-length*.5f+16;distance<length*.5f-9;distance+=29.5f)
                {
                    int side=slot++%2==0?-1:1;
                    foreach(float adjustment in new[]{0f,3f,-3f,6f,-6f})
                    {
                        var p=new Vector3(road.x,0,road.z)+along*(distance+adjustment)+normal*(side*(halfWidth+.70f));
                        if(plan.roads.Any(r=>r!=road&&Inside(r,p.x,p.z,5))||approaches.Any(r=>Inside(r,p.x,p.z,1.1f)))continue;
                        if(p.x>59&&p.x<132&&p.z>55&&p.z<75)continue;
                        if(placed.Any(v=>Vector3.ProjectOnPlane(v-p,Vector3.up).sqrMagnitude<18*18))continue;
                        if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var floor,4,1,QueryTriggerInteraction.Ignore)||floor.collider.name!="Connected pavement")continue;
                        p.y=floor.point.y;
                        if(Physics.CheckCapsule(p+Vector3.up*.32f,p+Vector3.up*5.75f,.14f,1,QueryTriggerInteraction.Ignore))continue;
                        var light=Group("Old European streetlight "+count,root,p,Quaternion.LookRotation(-normal*side).eulerAngles.y).transform;
                        Model("Tapered round steel pole",light,Vector3.zero,pole,steel);
                        Model("Swept round outreach arm",light,Vector3.zero,arm,steel);
                        Model("Rounded cast lamp housing",light,new Vector3(0,5.73f,1.12f),head,rim);
                        Model("Warm frosted lamp diffuser",light,new Vector3(0,5.649f,1.12f),diffuser,lens);
                        Cylinder("Pole foot collar",light,new Vector3(0,.13f,0),.105f,.24f,rim);
                        Box("Set flush concrete footing",light,new Vector3(0,.008f,0),new Vector3(.28f,.025f,.28f),concrete,false);
                        Box("Recessed maintenance cover",light,new Vector3(0,.67f,-.073f),new Vector3(.095f,.31f,.012f),rim,false,.7f);
                        var collider=light.gameObject.AddComponent<CapsuleCollider>();collider.radius=.105f;collider.height=5.7f;collider.center=Vector3.up*2.85f;
                        placed.Add(p);count++;Physics.SyncTransforms();break;
                    }
                }
            }
            Debug.Log("Street lighting placed: "+count+" supported pavement-edge poles; no extra dynamic lights.");
        }
        static void Model(string name,Transform parent,Vector3 position,Mesh mesh,Material material)
        {
            var obj=Group(name,parent,position);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;obj.isStatic=true;
        }
        static Mesh Save(string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            string path=Generated+"/Meshes/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=name;mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        static Mesh Tube(string name,Vector3[] centres,float[] radii)
        {
            var smoothCentres=new List<Vector3>();var smoothRadii=new List<float>();
            for(int segment=0;segment<centres.Length-1;segment++)for(int sample=0;sample<4;sample++)
            {
                float t=sample/4f,t2=t*t,t3=t2*t;
                var a=centres[Mathf.Max(0,segment-1)];var b=centres[segment];var c=centres[segment+1];var d=centres[Mathf.Min(centres.Length-1,segment+2)];
                smoothCentres.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3));
                smoothRadii.Add(Mathf.Lerp(radii[segment],radii[segment+1],t));
            }
            smoothCentres.Add(centres[centres.Length-1]);smoothRadii.Add(radii[radii.Length-1]);centres=smoothCentres.ToArray();radii=smoothRadii.ToArray();
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();const int sides=12;float distance=0;
            for(int ring=0;ring<centres.Length;ring++)
            {
                if(ring>0)distance+=Vector3.Distance(centres[ring],centres[ring-1]);
                var tangent=(centres[Mathf.Min(centres.Length-1,ring+1)]-centres[Mathf.Max(0,ring-1)]).normalized;
                var x=Vector3.right;var z=Vector3.Cross(x,tangent).normalized;
                for(int side=0;side<=sides;side++)
                {
                    float angle=side*Mathf.PI*2/sides;
                    vertices.Add(centres[ring]+(x*Mathf.Cos(angle)+z*Mathf.Sin(angle))*radii[ring]);uv.Add(new Vector2(side/(float)sides,distance/.65f));
                    if(ring==0||side==sides)continue;
                    int a=(ring-1)*(sides+1)+side,b=a+sides+1;
                    indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});
                }
            }
            int first=vertices.Count;vertices.Add(centres[0]);uv.Add(Vector2.zero);vertices.Add(centres[centres.Length-1]);uv.Add(Vector2.one);
            int end=(centres.Length-1)*(sides+1);
            for(int side=0;side<sides;side++)indices.AddRange(new[]{first,side,side+1,first+1,end+side+1,end+side});
            return Save(name,vertices,uv,indices);
        }
        static Mesh Housing(string name,float length,float width,float height)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();const int sides=16;
            for(int ring=0;ring<3;ring++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,scale=ring==1?1:.84f;
                var p=new Vector3(Mathf.Cos(a)*width*scale,(ring-1)*height*.5f,Mathf.Sin(a)*length*scale);
                vertices.Add(p);uv.Add(new Vector2(p.x/.3f,p.z/.3f));
                if(ring==0)continue;int old=(ring-1)*sides+side,next=(ring-1)*sides+(side+1)%sides;
                indices.AddRange(new[]{old,old+sides,next,next,old+sides,next+sides});
            }
            vertices.Add(Vector3.down*height*.5f);uv.Add(Vector2.zero);vertices.Add(Vector3.up*height*.5f);uv.Add(Vector2.one);
            for(int side=0;side<sides;side++)indices.AddRange(new[]{48,side,(side+1)%sides,49,32+(side+1)%sides,32+side});
            return Save(name,vertices,uv,indices);
        }
    }
}
