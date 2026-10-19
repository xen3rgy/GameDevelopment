using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchDressing
    {
        static Material metal=>Material("DressingMetal","steel",new Color(.4f,.46f,.43f),.28f,.55f);
        static Material wood=>Material("DressingTimber","wood",new Color(.75f,.66f,.52f));
        static Material concrete=>Material("DressingConcrete","concrete",new Color(.85f,.86f,.84f));
        public static void Build(Transform parent,WestbruchLayout data)
        {
            var root=Group("Neighbourhood life – purposeful details",parent,Vector3.zero).transform;
            int index=0;
            foreach(var b in data.buildings)
            {
                var building=GameObject.Find(b.id).transform;
                Architecture(building,b,index);
                float sign=b.yaw==180?1:-1;
                if(b.kind=="housing"||b.kind=="shop")
                {
                    var bicycle=Group("Resident bicycle",root,new Vector3(b.x+b.w*.5f-6,.14f,b.z+sign*(b.d*.5f+.42f)),b.yaw).transform;
                    Bicycle(bicycle);
                    var lamp=Group("Entrance bulkhead",building,new Vector3(1.3f,2.28f,-b.d*.5f-.16f));
                    Box("Bulkhead casing",lamp.transform,Vector3.zero,new Vector3(.18f,.12f,.12f),metal,false);
                    Box("Frosted diffuser",lamp.transform,new Vector3(0,-.07f,0),new Vector3(.14f,.025f,.1f),Material("FrostedLamp",null,new Color(.88f,.78f,.55f),.2f),false);
                }
                index++;
            }
            Gardens(root);
            Industrial(root);
            RoadDetails(root);
            Skyline(root);
        }
        static void Architecture(Transform root,BuildingPlot b,int index)
        {
            float h=b.kind=="workshop"?4.6f:b.kind=="garage"?3:b.floors*3.05f;
            var faded=Material("FadedPaint"+(index%4),null,new[]{new Color(.58f,.55f,.48f),new Color(.61f,.62f,.59f),new Color(.40f,.47f,.43f),new Color(.57f,.48f,.40f)}[index%4]);
            foreach(int s in new[]{-1,1})
            {
                Box("Corner brick reveal",root,new Vector3(s*(b.w*.5f-.18f),h*.5f,-b.d*.5f-.035f),new Vector3(.32f,h,.08f),Material("RevealedBrick","brick",new Color(.83f,.82f,.78f)),false,2.5f);
                Box("Roof fascia",root,new Vector3(0,h+.28f,s*(b.d*.5f+.18f)),new Vector3(b.w+.5f,.11f,.07f),metal,false);
            }
            if(b.kind=="garage"||b.kind=="workshop")
            {
                Sign(b.kind=="garage"?"GARAGEN · NUR FÜR MIETER":"ANLIEFERUNG · HOF FREIHALTEN",root,new Vector3(0,2.9f,-b.d*.5f-.17f),b.w*.6f,metal,new Color(.9f,.86f,.74f));
                if(b.kind=="workshop")
                {
                    var roof=Group("Rooftop extractors",root,new Vector3(0,h+.2f,0)).transform;
                    foreach(float x in new[]{-8f,8f})
                    {
                        Cylinder("Ventilation duct",roof,new Vector3(x,.3f,2),.25f,.6f,metal);
                        Box("Vent cap",roof,new Vector3(x,.65f,2),new Vector3(.8f,.12f,.8f),metal,false);
                    }
                }
                return;
            }
            for(int side=-1;side<=1;side+=2)
            {
                var face=side==-1?root:Group("Rear facade detailing",root,Vector3.zero,180).transform;
                for(int floor=0;floor<b.floors;floor++)
                for(float x=-b.w*.5f+2.5f;x<b.w*.5f-1.6f;x+=3.8f)
                {
                    if(floor==0&&Mathf.Abs(x)<2)continue;
                    int seed=Mathf.Abs(Mathf.RoundToInt(x*10)+index*31+floor*7);
                    float y=1.7f+floor*3.05f,z=-b.d*.5f-.07f;
                    if(seed%4==0)
                    {
                        Box("Half lowered roller shutter",face,new Vector3(x,y+.46f,z-.015f),new Vector3(1.28f,.65f,.045f),faded,false);
                        for(int l=0;l<5;l++)Box("Shutter louvre",face,new Vector3(x,y+.18f+l*.12f,z-.045f),new Vector3(1.29f,.025f,.02f),metal,false);
                    }
                    else if(seed%3==0)
                    {
                        var curtain=Material("Curtain"+seed%3,null,new[]{new Color(.38f,.36f,.31f),new Color(.29f,.32f,.34f),new Color(.42f,.37f,.30f)}[seed%3],.35f);
                        Box("Drawn curtain",face,new Vector3(x-.36f,y,z),new Vector3(.49f,1.57f,.018f),curtain,false);
                    }
                    if(floor==0&&seed%7==0)Box("Window security bars",face,new Vector3(x,y,z-.08f),new Vector3(1.4f,.04f,.04f),metal,false);
                }
                // Worn service panels are attached to the wall, not floating signs.
                Box("Utility connection",face,new Vector3(b.w*.5f-1.4f,.48f,-b.d*.5f-.09f),new Vector3(.4f,.5f,.15f),metal,false);
            }
            if(index%5==0)
            {
                var roof=Model("Low pitched roof",root,new Vector3(0,h+.18f,0),RoofMesh(b.w+.5f,b.d+.5f),Material("OldRoof", "asphalt",new Color(.55f,.56f,.53f)),true);
                roof.GetComponent<MeshCollider>().convex=false;
            }
            if(b.kind=="shop") Shopfront(root,b);
        }
        static void Shopfront(Transform root,BuildingPlot b)
        {
            var green=Material("ShopGreen",null,new Color(.10f,.23f,.18f),.25f);
            var window=Material("ShopWindow",null,new Color(.17f,.24f,.23f),.82f,.22f);
            foreach(float x in new[]{-8f,-4.3f,4.3f,8f})
            {
                Box("Shop display window",root,new Vector3(x,1.35f,-b.d*.5f-.095f),new Vector3(3.05f,2.15f,.08f),window,false);
                foreach(int s in new[]{-1,1})Box("Shopframe",root,new Vector3(x+s*1.55f,1.35f,-b.d*.5f-.17f),new Vector3(.07f,2.3f,.08f),green,false);
                Box("Display sill",root,new Vector3(x,.3f,-b.d*.5f-.16f),new Vector3(3.15f,.1f,.2f),green,false);
            }
            Sign("GETRÄNKE · PFAND · LEBENSMITTEL",root,new Vector3(0,3.62f,-b.d*.5f-.18f),b.w*.8f,green,new Color(.89f,.84f,.67f));
            foreach(float x in new[]{-9.8f,9.7f})
            {
                Box("Return crate",root,new Vector3(x,.26f,-b.d*.5f-.85f),new Vector3(.7f,.5f,.5f),green,false);
                Box("Crate rim",root,new Vector3(x,.54f,-b.d*.5f-.85f),new Vector3(.76f,.06f,.55f),wood,false);
            }
        }
        static void Bicycle(Transform root)
        {
            var tyre=Material("BicycleRubber",null,new Color(.06f,.06f,.05f),.1f);
            foreach(float x in new[]{-.63f,.63f})Model("Bicycle tyre",root,new Vector3(x,.35f,0),Ring(.33f,.022f),tyre);
            foreach(float x in new[]{-.63f,.63f})
                for(int s=0;s<10;s++)
                {
                    float a=s*Mathf.PI/5;
                    Beam("Wheel spoke",root,new Vector3(x,.35f,0),new Vector3(x+Mathf.Cos(a)*.29f,.35f+Mathf.Sin(a)*.29f,0),.008f,metal);
                }
            Vector3 rear=new Vector3(-.63f,.35f,0),front=new Vector3(.63f,.35f,0),crank=new Vector3(-.1f,.29f,0),seat=new Vector3(-.27f,.84f,0),bar=new Vector3(.35f,.86f,0);
            foreach(var edge in new[]{(rear,crank),(rear,seat),(seat,crank),(seat,bar),(crank,bar),(bar,front)})Beam("Bicycle frame",root,edge.Item1,edge.Item2,.028f,metal);
            Box("Bicycle seat",root,new Vector3(-.28f,.92f,0),new Vector3(.25f,.05f,.17f),tyre,false);
            Beam("Handlebar",root,new Vector3(.38f,.98f,-.23f),new Vector3(.38f,.98f,.23f),.025f,metal);
            Beam("Kickstand",root,crank,new Vector3(-.12f,0,.14f),.02f,metal);
            var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.5f,0);collider.size=new Vector3(1.85f,.9f,.32f);
        }
        static void Gardens(Transform parent)
        {
            foreach(var p in new[]{new Vector3(-60,0,44),new Vector3(-15,0,46),new Vector3(34,0,-62)})
            {
                var root=Group("Shared garden corner",parent,new Vector3(p.x,WestbruchLandscape.Ground(p.x,p.z),p.z)).transform;
                var soil=Material("GardenEarth","mud",Color.white,.1f);
                Box("Raised bed soil",root,new Vector3(0,.09f,0),new Vector3(3.8f,.18f,1.1f),soil,false);
                foreach(int side in new[]{-1,1})Box("Old raised bed board",root,new Vector3(0,.14f,side*.6f),new Vector3(4,.28f,.055f),wood,false);
                foreach(int side in new[]{-1,1})Box("End board",root,new Vector3(side*2,.14f,0),new Vector3(.06f,.28f,1.2f),wood,false);
                WestbruchProps.Pallet(root,new Vector3(2.8f,0,.2f));
                Cylinder("Rain barrel",root,new Vector3(2.7f,.5f,1.3f),.31f,1,metal,true);
                WestbruchProps.Fence(root,new Vector3(-2.3f,0,1.5f),new Vector3(1.5f,0,1.5f));
            }
            var line=Group("Shared washing line",parent,new Vector3(-53,.14f,37)).transform;
            foreach(int side in new[]{-1,1})Cylinder("Washing post",line,new Vector3(side*3,1.05f,0),.035f,2.1f,metal);
            Beam("Clothesline",line,new Vector3(-3,2.02f,0),new Vector3(3,1.96f,0),.015f,metal);
            for(int i=0;i<5;i++)Box("Drying laundry",line,new Vector3(-2+i*.9f,1.65f,0),new Vector3(.52f,.6f,.018f),Material("Laundry"+i,null,new Color(.35f+i*.05f,.37f+i*.04f,.34f+i*.04f)),false);
            var ball=Cylinder("Lost playground ball",parent,new Vector3(-51.3f,.34f,-52),.2f,.34f,Material("FadedBall",null,new Color(.28f,.31f,.27f)));
            ball.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        static void Industrial(Transform parent)
        {
            foreach(var p in new[]{new Vector3(110,0,51),new Vector3(34,0,-44)})
            {
                float y=WestbruchLandscape.Ground(p.x,p.z);var root=Group("Refuse and salvage alcove",parent,new Vector3(p.x,y,p.z),15).transform;
                Box("Large refuse container",root,new Vector3(0,.63f,0),new Vector3(2.05f,1.05f,1.1f),metal);
                Box("Sloping hinged lid",root,new Vector3(0,1.19f,0),new Vector3(2.13f,.12f,1.18f),metal,false);
                foreach(float x in new[]{-.82f,.82f})foreach(float z in new[]{-.39f,.39f})Cylinder("Bin caster",root,new Vector3(x,.1f,z),.10f,.1f,metal);
                WestbruchProps.Pallet(root,new Vector3(1.7f,0,1));
                for(int i=0;i<3;i++)
                {
                    var bag=Group("Tied refuse bag",root,new Vector3(-1.5f-i*.3f,.23f,i*.3f));
                    var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.transform.SetParent(bag.transform,false);obj.transform.localScale=new Vector3(.42f,.5f,.37f);obj.GetComponent<Renderer>().sharedMaterial=Material("RefusePlastic",null,new Color(.045f,.047f,.044f),.42f);UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.isStatic=true;
                    Box("Bag tie",bag.transform,new Vector3(.02f,.3f,0),new Vector3(.055f,.07f,.055f),metal,false);
                }
                for(int i=0;i<3;i++){var ring=Model("Salvaged tyre",root,new Vector3(2,.13f+i*.23f,-.4f),Ring(.34f,.095f),Material("Rubber",null,new Color(.05f,.05f,.05f)));ring.transform.localRotation=Quaternion.Euler(90,0,0);}
            }
        }
        static void RoadDetails(Transform parent)
        {
            var paint=Material("CrosswalkPaint",null,new Color(.64f,.65f,.61f));
            // The pedestrian navigation builder owns all designated crossing markings.
            foreach(var p in new[]{new Vector3(84,0,-13),new Vector3(104,0,12),new Vector3(-84,0,7)})
            {
                float y=WestbruchLandscape.Ground(p.x,p.z);Cylinder("Old road sign post",parent,p+Vector3.up*(y+1.55f),.027f,3.1f,metal);
                Sign("30",parent,p+new Vector3(0,y+2.8f,0),.6f,Material("SignWhite",null,new Color(.74f,.76f,.72f)),new Color(.12f,.16f,.17f));
            }
            var kiosk=GameObject.Find("Old Westbruch bus shelter");
            if(kiosk)
            {
                var window=Material("ShelterClearGlass",null,new Color(.41f,.49f,.49f,.22f),.7f);
                window.SetFloat("_SurfaceType",1);HDMaterial.ValidateMaterial(window);
                var panel=kiosk.transform.Find("Weathered back panel");panel.GetComponent<MeshRenderer>().sharedMaterial=window;
                Box("Timetable case",kiosk.transform,new Vector3(1.62f,1.6f,.70f),new Vector3(.42f,.57f,.08f),metal,false);
                Sign("4 · STADTMITTE",kiosk.transform,new Vector3(1.62f,1.6f,.64f),.4f,Material("TimetablePaper",null,Color.gray),Color.white);
            }
        }
        static void Skyline(Transform parent)
        {
            var root=Group("Neighbouring skyline – visual background only",parent,Vector3.zero).transform;
            int n=0;
            foreach(float z in new[]{-153f,-194,153,194})foreach(float x in new[]{-85f,-44,-4,42,83,166})
            {
                float h=10+(n%4)*3,w=20+(n%3)*4;var b=Group("Distant residential block",root,new Vector3(x,WestbruchLandscape.Ground(x,z)-.03f,z)).transform;
                var mat=Material("BackdropFacade"+n%3,n%2==0?"brick":"plaster",new Color(.71f,.73f,.70f));
                Box("Backdrop body",b,new Vector3(0,h*.5f,0),new Vector3(w,h,17),mat,false);
                Box("Backdrop roof",b,new Vector3(0,h+.08f,0),new Vector3(w+.4f,.16f,17.4f),metal,false);
                for(float y=1.8f;y<h-1;y+=3.2f)for(float xx=-w*.5f+2;xx<w*.5f-1;xx+=3.5f)foreach(int s in new[]{-1,1})
                    Box("Distant window",b,new Vector3(xx,y,s*8.55f),new Vector3(1.2f,1.5f,.05f),Material("BackdropGlass",null,new Color(.15f,.23f,.25f),.55f),false);
                n++;
            }
        }
        static GameObject Model(string name,Transform parent,Vector3 p,Mesh mesh,Material mat,bool collider=false)
        {
            var obj=Group(name,parent,p);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collider)obj.AddComponent<MeshCollider>().sharedMesh=mesh;return obj;
        }
        static Mesh Ring(float radius,float thickness)
        {
            string path=Generated+"/Meshes/Ring_"+Mathf.RoundToInt(radius*1000)+"_"+Mathf.RoundToInt(thickness*1000)+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int sides=24,tube=8;
            for(int i=0;i<=sides;i++)for(int j=0;j<=tube;j++)
            {
                float a=i*Mathf.PI*2/sides,b=j*Mathf.PI*2/tube,r=radius+Mathf.Cos(b)*thickness;
                vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*thickness));uv.Add(new Vector2(i/(float)sides,j/(float)tube));
                if(i<sides&&j<tube){int k=i*(tube+1)+j;t.AddRange(new[]{k,k+tube+1,k+1,k+1,k+tube+1,k+tube+2});}
            }
            mesh=new Mesh{vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=t.ToArray()};mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh RoofMesh(float width,float depth)
        {
            string path=Generated+"/Meshes/Gable_"+width+"_"+depth+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            float x=width*.5f,z=depth*.5f;
            var v=new[]{new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,-z),new Vector3(x,0,z),new Vector3(0,1.3f,-z),new Vector3(0,1.3f,z)};
            mesh=new Mesh{vertices=v,triangles=new[]{0,1,4,1,5,4,4,5,2,5,3,2,0,4,2,1,3,5},uv=new[]{new Vector2(0,0),new Vector2(0,depth/3),new Vector2(width/3,0),new Vector2(width/3,depth/3),new Vector2(width/6,0),new Vector2(width/6,depth/3)}};
            mesh.RecalculateNormals();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
