using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Individually composed railway-era houses. Shared material language with
    // Westbruch, but independently authored silhouettes, openings and entrances.
    internal static class BahnhofResidentialArchitecture
    {
        public static readonly List<Vector3> EntrancePoints=new List<Vector3>();
        static Material stone,metal,wood,glass,frame,slate,brick,plaster,plasterWarm,plasterCool,shopPaint;
        sealed class House
        {
            public string id,shop;public float x,z,yaw,w,d;public int floors,style;
            public House(string id,float x,float z,float yaw,float w,float d,int floors,int style,string shop=null)
            {this.id=id;this.x=x;this.z=z;this.yaw=yaw;this.w=w;this.d=d;this.floors=floors;this.style=style;this.shop=shop;}
        }
        static readonly House[] Houses={
            new House("Güterstraße_12",180,177,-90,24,14,3,0,"RAD & REPARATUR"),
            new House("Güterstraße_16",180,220,-90,22,16,3,1,"IMBISS AM GLEIS"),
            new House("Bahnhofstraße_3",234,116,90,22,14,3,2,"WASCHSALON"),
            new House("Bahnhofstraße_5",234,147,90,22,14,4,3),
            new House("Bahnhofstraße_9",234,202,90,24,14,3,1,"LEBENSMITTEL"),
            new House("Bahnhofstraße_11",234,233,90,22,16,3,0,"PENSION ADLER"),
            new House("Bahnhofstraße_15",234,275,90,22,14,3,2,"BÄCKEREI KERN"),
            new House("Am_Bahnhof_6",178,314,0,24,14,3,0,"CAFÉ AM BAHNHOF"),
            new House("Am_Bahnhof_8",150,315,0,22,16,3,1),
            new House("Hintere_Gasse_2",281,202,90,22,14,3,3),
            new House("Hintere_Gasse_4",281,235,90,24,14,3,1),
            new House("Hintere_Gasse_6",281,278,90,22,16,3,2)
        };
        static readonly string[] Legacy={"Bahnwerk_Service","Güterhalle_7","Former freight shed – small businesses"};
        static GameObject B(string name,Transform p,Vector3 at,Vector3 size,Material m,bool solid=false,float tile=3)
            =>Box("BV2_"+name,p,at,size,m,solid,tile);

        public static void Rebuild(Transform root)
        {
            EntrancePoints.Clear();
            foreach(var tr in root.Cast<Transform>().Where(t=>t.name.StartsWith("BV2_House_")||t.name.StartsWith("BV2_Industrial_")||Houses.Any(h=>h.id==t.name)||Legacy.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(tr.gameObject);
            stone=BahnhofDetailKit.Load("StoneTrim");metal=BahnhofDetailKit.Load("UrbanLife_PaintedSteel");wood=BahnhofDetailKit.Load("UrbanLife_AgedTimber");glass=BahnhofDetailKit.Load("DarkGlass");slate=BahnhofDetailKit.Load("OldRoof");
            if(!stone||!metal||!wood||!glass||!slate)throw new InvalidOperationException("Expected existing Westbruch architectural materials.");
            frame=Material("BV2_ResidentialPaint",null,new Color(.40f,.43f,.38f),.23f,.15f);
            brick=Material("BV2_ResidentialBrick","brick",new Color(.84f,.79f,.70f),.12f);
            plaster=Material("BV2_ResidentialLime","plaster",new Color(.80f,.79f,.72f),.09f);
            plasterWarm=Material("BV2_ResidentialWarmLime","plaster",new Color(.86f,.80f,.68f),.09f);
            plasterCool=Material("BV2_ResidentialCoolLime","plaster",new Color(.72f,.77f,.74f),.09f);
            shopPaint=Material("BV2_ResidentialShopPaint",null,new Color(.19f,.28f,.25f),.20f,.1f);
            for(int i=0;i<Houses.Length;i++)BuildHouse(root,Houses[i],i);
            Industrial(root,"Bahnwerk_Service",new Vector3(179,.14f,102),180,32,18,4.5f,false);
            Industrial(root,"Güterhalle_7",new Vector3(168,.14f,142),180,30,8,3.3f,true);
            Freight(root);
        }
        static void BuildHouse(Transform root,House h,int seed)
        {
            var p=Group("BV2_House_"+h.id,root,new Vector3(h.x,.14f,h.z),h.yaw).transform;
            float height=h.floors*3.05f;
            var collision=p.gameObject.AddComponent<BoxCollider>();collision.size=new Vector3(h.w,height,h.d);collision.center=Vector3.up*height*.5f;
            Material wall=h.style==0?brick:h.style==1?plaster:h.style==2?plasterWarm:plasterCool;
            BuildFace(p,h.id+"_front",h.w,h.floors,Vector3.back*h.d*.5f,0,wall,h.shop,seed,true);
            BuildFace(p,h.id+"_rear",h.w,h.floors,Vector3.forward*h.d*.5f,180,wall,null,seed+2,false);
            BuildFace(p,h.id+"_left",h.d,h.floors,Vector3.left*h.w*.5f,90,wall,null,seed+3,false);
            BuildFace(p,h.id+"_right",h.d,h.floors,Vector3.right*h.w*.5f,-90,wall,null,seed+4,false);
            foreach(int side in new[]{-1,1})
            {
                if(side==1)B("Continuous stone plinth",p,new Vector3(0,.25f,h.d*.5f+.035f),new Vector3(h.w+.08f,.50f,.12f),stone);
                else foreach(float half in new[]{-1f,1})B("Entrance-cut stone plinth",p,new Vector3(half*(h.w*.25f+.40f),.25f,-h.d*.5f-.035f),new Vector3(h.w*.5f-.80f,.50f,.12f),stone);
                B("Side stone plinth",p,new Vector3(side*(h.w*.5f+.035f),.25f,0),new Vector3(.12f,.50f,h.d),stone);
                B("Eave cornice",p,new Vector3(0,height-.10f,side*(h.d*.5f+.1f)),new Vector3(h.w+.45f,.24f,.40f),stone);
                B("Gable cornice",p,new Vector3(side*(h.w*.5f+.1f),height-.10f,0),new Vector3(.40f,.24f,h.d+.42f),stone);
                if(h.style!=3)
                    for(int floor=1;floor<h.floors;floor++)B("Subtle floor string course",p,new Vector3(0,floor*3.05f-.05f,side*(h.d*.5f+.035f)),new Vector3(h.w+.04f,.09f,.14f),stone);
            }
            if(h.style==3)
            {
                B("Single flat roof weathering slab",p,new Vector3(0,height+.08f,0),new Vector3(h.w+.5f,.18f,h.d+.5f),slate);
                foreach(int side in new[]{-1,1})
                {B("Parapet wall",p,new Vector3(0,height+.38f,side*h.d*.5f),new Vector3(h.w,.55f,.22f),wall);B("Parapet coping",p,new Vector3(0,height+.69f,side*h.d*.5f),new Vector3(h.w+.3f,.09f,.36f),stone);}
                B("Recessed rooftop stair head",p,new Vector3(h.w*.18f,height+.85f,1),new Vector3(3,1.55f,3),wall);
            }
            else
            {
                float rise=h.style==0?2.15f:h.style==1?1.8f:2.6f;
                BahnhofDetailKit.GableRoof(p,"BV2_"+h.id,h.w+.65f,h.d+.65f,height+.08f,rise,slate,wall);
                for(int i=0;i<2;i++)
                {
                    float x=(i==0?-1:1)*h.w*.27f;
                    B("Brick chimney stack",p,new Vector3(x,height+rise*.7f,0),new Vector3(.72f,rise*.9f,.84f),brick);
                    B("Chimney cap",p,new Vector3(x,height+rise*1.15f+.05f,0),new Vector3(.88f,.12f,1),stone);
                }
                foreach(int side in new[]{-1,1})
                    B("Eave gutter",p,new Vector3(0,height+.05f,side*(h.d*.5f+.38f)),new Vector3(h.w+.7f,.11f,.13f),metal);
            }
            foreach(int side in new[]{-1,1})
            {
                float x=side*(h.w*.5f-.48f),z=-h.d*.5f-.15f;
                Cylinder("BV2_Rainwater downpipe",p,new Vector3(x,height*.5f,z),.045f,height,metal);
                foreach(float y in new[]{.6f,3.6f,6.6f,9.2f})if(y<height)B("Pipe wall bracket",p,new Vector3(x,y,z+.04f),new Vector3(.16f,.035f,.16f),metal);
                B("Downpipe ground shoe",p,new Vector3(x,.12f,z-.04f),new Vector3(.15f,.22f,.22f),metal);
            }
            if(h.style==3||h.style==1&&seed%2==0)Balconies(p,h,seed,height);
            // Mounting plates are measured from the actual facade plane.
            BahnhofDetailKit.Plate(p,(seed+3).ToString(),new Vector3(1.13f,1.8f,-h.d*.5f-.047f),0,.29f,.35f,shopPaint,new Color(.86f,.84f,.72f));
            EntrancePoints.Add(p.TransformPoint(new Vector3(0,.015f,-h.d*.5f-1.5f)));
        }
        static void BuildFace(Transform building,string id,float width,int floors,Vector3 at,float yaw,Material finish,string shop,int seed,bool entrance)
        {
            var face=Group("BV2_Facade_"+id,building,at,yaw).transform;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            void Rect(float x0,float x1,float y0,float y1)
            {
                if(x1-x0<.001f||y1-y0<.001f)return;
                AddBox(vertices,uv,triangles,new Vector3((x0+x1)*.5f,(y0+y1)*.5f,.17f),new Vector3(x1-x0,y1-y0,.34f));
            }
            int bays=Mathf.Max(3,Mathf.RoundToInt(width/3.5f));if(entrance&&bays%2==0)bays--;
            float pitch=width/bays;
            for(int floor=0;floor<floors;floor++)for(int i=0;i<bays;i++)
            {
                float x0=-width*.5f+i*pitch,x1=x0+pitch,x=(x0+x1)*.5f,baseY=floor*3.05f;
                bool door=entrance&&floor==0&&i==bays/2;
                bool display=shop!=null&&floor==0&&!door;
                float opening=door?1.4f:display?pitch-.64f:1.35f;
                float sill=door?0:display?.58f:.90f,top=door?2.35f:display?2.34f:2.55f;
                Rect(x0,x-opening*.5f,baseY,baseY+3.05f);Rect(x+opening*.5f,x1,baseY,baseY+3.05f);
                Rect(x-opening*.5f,x+opening*.5f,baseY,baseY+sill);Rect(x-opening*.5f,x+opening*.5f,baseY+top,baseY+3.05f);
                if(door)Door(face,x,seed);
                else Window(face,x,baseY+(sill+top)*.5f,opening,top-sill,seed+i+floor*13,display);
            }
            SaveFacade(face,id,vertices,uv,triangles,finish);
            if(shop!=null)
            {
                // A real wall-backed fascia per side of the residential doorway.
                foreach(int side in new[]{-1,1})
                {
                    float panelWidth=width*.5f-1.10f,x=side*(width*.25f+.30f);
                    BahnhofDetailKit.Plate(face,shop,new Vector3(x,2.73f,-.065f),0,panelWidth,.40f,shopPaint,new Color(.88f,.84f,.70f));
                    B("Shop awning cassette",face,new Vector3(x,2.47f,-.18f),new Vector3(panelWidth,.12f,.28f),metal);
                    var awning=B("Faded shop awning",face,new Vector3(x,2.37f,-.72f),new Vector3(panelWidth,.055f,1.22f),shopPaint);awning.transform.localRotation=Quaternion.Euler(-8,0,0);
                    foreach(int edge in new[]{-1,1})Beam("BV2_Awning bracket",face,new Vector3(x+edge*(panelWidth*.5f-.12f),1.93f,-.06f),new Vector3(x+edge*(panelWidth*.5f-.12f),2.30f,-1.15f),.025f,metal);
                }
            }
        }
        static void Window(Transform face,float x,float y,float w,float h,int seed,bool shop)
        {
            var shadow=Material("BV2_WindowShadow",null,new Color(.065f,.075f,.07f),.05f);
            B("Deep window shadow",face,new Vector3(x,y,.25f),new Vector3(w,h,.045f),shadow);
            var glazing=shop?glass:seed%7==0?Material("BV2_DrawCurtain",null,new Color(.34f,.34f,.29f),.13f):glass;
            B("Inset glazing",face,new Vector3(x,y,.15f),new Vector3(w-.10f,h-.10f,.025f),glazing);
            foreach(int side in new[]{-1,1})
            {
                B("Window side reveal",face,new Vector3(x+side*(w*.5f-.035f),y,.10f),new Vector3(.07f,h,.20f),stone);
                B("Window frame upright",face,new Vector3(x+side*(w*.5f+.005f),y,-.028f),new Vector3(.075f,h,.07f),frame);
                B("Window top and lower reveal",face,new Vector3(x,y+side*(h*.5f-.025f),.10f),new Vector3(w,.05f,.20f),stone);
                B("Window frame horizontal",face,new Vector3(x,y+side*(h*.5f+.0375f),-.028f),new Vector3(w+.15f,.075f,.07f),frame);
            }
            B("Window central mullion",face,new Vector3(x,y,-.035f),new Vector3(.045f,h,.07f),frame);
            if(!shop)B("Window transom",face,new Vector3(x,y+h*.20f,-.035f),new Vector3(w,.035f,.07f),frame);
            B("Stone sill",face,new Vector3(x,y-h*.5f-.10f,-.09f),new Vector3(w+.25f,.11f,.37f),stone);
            if(!shop&&seed%5==0)
            {
                var shutters=Material("BV2_FadedShutters",null,new Color(.32f,.36f,.31f),.12f);
                foreach(int side in new[]{-1,1})
                {
                    float at=x+side*(w*.5f+.26f);
                    B("Folded exterior shutter",face,new Vector3(at,y,-.065f),new Vector3(.38f,h+.08f,.055f),shutters);
                    for(float sy=-h*.42f;sy<h*.45f;sy+=.18f)B("Shutter ventilation slat",face,new Vector3(at,y+sy,-.098f),new Vector3(.32f,.032f,.015f),frame);
                }
            }
        }
        static void Door(Transform face,float x,int seed)
        {
            B("Recessed closed entrance",face,new Vector3(x,1.14f,.17f),new Vector3(1.30f,2.28f,.12f),shopPaint);
            foreach(int side in new[]{-1,1})
            {
                B("Entrance jamb reveal",face,new Vector3(x+side*.70f,1.18f,.08f),new Vector3(.14f,2.36f,.24f),stone);
                B("Door upper pane",face,new Vector3(x+side*.29f,1.70f,.097f),new Vector3(.45f,.65f,.025f),glass);
                B("Door timber panel",face,new Vector3(x+side*.29f,.60f,.097f),new Vector3(.45f,.70f,.025f),frame);
            }
            B("Door lintel",face,new Vector3(x,2.41f,-.025f),new Vector3(1.65f,.16f,.32f),stone);
            B("Door pull",face,new Vector3(x+.44f,1.10f,.08f),new Vector3(.035f,.24f,.06f),metal);
            B("Entrance threshold",face,new Vector3(x,.025f,-.16f),new Vector3(1.55f,.05f,.5f),stone);
            B("Entrance canopy",face,new Vector3(x,2.64f,-.63f),new Vector3(2.20f,.10f,1.30f),slate);
            foreach(int side in new[]{-1,1})Beam("BV2_Entrance canopy bracket",face,new Vector3(x+side*.9f,2.02f,-.015f),new Vector3(x+side*.9f,2.57f,-1.12f),.035f,metal);
            B("Intercom mounting",face,new Vector3(x+.98f,1.40f,-.035f),new Vector3(.16f,.33f,.07f),metal);
            for(int i=0;i<3;i++)B("Intercom button",face,new Vector3(x+.98f,1.29f+i*.09f,-.08f),new Vector3(.055f,.035f,.025f),stone);
            B("Mail collection box",face,new Vector3(x-1.12f,1.25f,-.10f),new Vector3(.53f,.71f,.20f),frame);
            for(int i=0;i<3;i++)B("Mailbox slot",face,new Vector3(x-1.12f,1.05f+i*.20f,-.205f),new Vector3(.40f,.025f,.015f),metal);
        }
        static void Balconies(Transform p,House h,int seed,float height)
        {
            // Narrow rear balconies differentiate postwar houses; no frontage obstruction.
            for(int floor=1;floor<h.floors;floor++)foreach(float x in new[]{-h.w*.25f,h.w*.25f})
            {
                float y=floor*3.05f+.06f,z=h.d*.5f+.65f;
                B("Rear balcony slab",p,new Vector3(x,y,z),new Vector3(3.0f,.16f,1.4f),stone);
                foreach(float side in new[]{-1f,1})B("Balcony side railing",p,new Vector3(x+side*1.43f,y+.58f,z),new Vector3(.045f,1.0f,1.25f),metal);
                B("Balcony top rail",p,new Vector3(x,y+1.08f,z+.65f),new Vector3(2.9f,.055f,.055f),metal);
                for(int n=0;n<12;n++)B("Balcony safety baluster",p,new Vector3(x-1.35f+n*.245f,y+.55f,z+.65f),new Vector3(.025f,1.0f,.025f),metal);
                if((floor+seed)%2==0)B("Balcony planter",p,new Vector3(x+.7f,y+.25f,z+.47f),new Vector3(.8f,.28f,.28f),wood);
            }
        }
        static void Industrial(Transform root,string id,Vector3 at,float yaw,float width,float depth,float height,bool garage)
        {
            var p=Group("BV2_Industrial_"+id,root,at,yaw).transform;
            B("Workshop structural core",p,new Vector3(0,height*.5f,0),new Vector3(width,height,depth),brick,true);
            B("Industrial foundation slab",p,new Vector3(0,-.06f,0),new Vector3(width,.12f,depth),stone);
            int bays=garage?4:3;float pitch=width/bays;
            var doorPaint=Material("BV2_WorkshopDoorPaint",null,new Color(.31f,.35f,.32f),.20f,.35f);
            for(int i=0;i<bays;i++)
            {
                float x=-width*.5f+pitch*(i+.5f),z=-depth*.5f;
                B("Roller door reveal",p,new Vector3(x,1.45f,z-.045f),new Vector3(pitch-.65f,2.9f,.08f),metal);
                B("Roller door face",p,new Vector3(x,1.4f,z-.10f),new Vector3(pitch-.95f,2.8f,.045f),doorPaint);
                for(float y=.22f;y<2.7f;y+=.17f)B("Roller door rib",p,new Vector3(x,y,z-.132f),new Vector3(pitch-1.02f,.025f,.016f),metal);
                foreach(int side in new[]{-1,1})B("Workshop brick pier",p,new Vector3(x+side*(pitch*.5f-.12f),height*.5f,z-.08f),new Vector3(.24f,height,.24f),brick);
                if(!garage)B("Workshop high glazing",p,new Vector3(x,3.65f,z-.045f),new Vector3(pitch-1.2f,.62f,.06f),glass);
            }
            if(garage)
            {
                B("Garage single roof",p,new Vector3(0,height+.08f,0),new Vector3(width+.5f,.16f,depth+.6f),slate);
                B("Garage roof drip edge",p,new Vector3(0,height-.03f,-depth*.5f-.30f),new Vector3(width+.5f,.17f,.05f),metal);
            }
            else
            {
                BahnhofDetailKit.GableRoof(p,"BV2_"+id,width+.65f,depth+.65f,height+.08f,1.65f,slate,brick);
                foreach(float x in new[]{-width*.25f,width*.25f})
                {Cylinder("BV2_Workshop roof vent",p,new Vector3(x,height+1.2f,0),.22f,1.2f,metal);B("Vent rain cap",p,new Vector3(x,height+1.85f,0),new Vector3(.65f,.10f,.65f),metal);}
            }
            BahnhofDetailKit.Plate(p,garage?"GÜTERHOF · MIETERGARAGEN":"BAHNWERK · SERVICE & REPARATUR",new Vector3(0,height-.27f,-depth*.5f-.17f),0,garage?7.8f:12,.45f,shopPaint,new Color(.88f,.84f,.70f));
            foreach(int side in new[]{-1,1})Cylinder("BV2_Workshop downpipe",p,new Vector3(side*(width*.5f-.24f),height*.5f,-depth*.5f-.17f),.05f,height,metal);
            EntrancePoints.Add(p.TransformPoint(new Vector3(garage?width/8:0,.015f,-depth*.5f-1.5f)));
        }
        static void Freight(Transform root)
        {
            var p=Group("BV2_Industrial_Former freight shed",root,new Vector3(160,.14f,163)).transform;
            B("Freight hall brick body",p,new Vector3(0,2.2f,0),new Vector3(20,4.4f,10),brick,true);
            B("Freight loading apron",p,new Vector3(0,.04f,-5.65f),new Vector3(20,.08f,1.3f),stone);
            BahnhofDetailKit.GableRoof(p,"BV2_Freight",21,11,4.5f,1.8f,slate,brick);
            for(int i=0;i<4;i++)
            {
                float x=-7.5f+i*5;
                B("Freight sliding door metal track",p,new Vector3(x,3.1f,-5.12f),new Vector3(4.0f,.10f,.12f),metal);
                B("Freight sliding door",p,new Vector3(x,1.55f,-5.10f),new Vector3(3.2f,2.8f,.12f),wood);
                foreach(float side in new[]{-1f,1})Beam("BV2_Freight diagonal door brace",p,new Vector3(x-1.4f,side<0?.3f:2.8f,-5.18f),new Vector3(x+1.4f,side<0?2.8f:.3f,-5.18f),.07f,metal);
            }
            BahnhofDetailKit.Plate(p,"GÜTERHALLE · ATELIER & LAGER",new Vector3(0,3.75f,-5.09f),0,9,.42f,shopPaint,new Color(.88f,.84f,.70f));
            EntrancePoints.Add(p.TransformPoint(new Vector3(2.5f,.015f,-7)));
        }
        static void AddBox(List<Vector3> v,List<Vector2> uv,List<int> triangles,Vector3 center,Vector3 size)
        {
            var min=center-size*.5f;var max=center+size*.5f;
            void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int n=v.Count;var normal=Vector3.Cross(b-a,c-a).normalized;
                foreach(var point in new[]{a,b,c,d}){v.Add(point);uv.Add(Mathf.Abs(normal.z)>.8f?new Vector2(point.x,point.y)/3:Mathf.Abs(normal.x)>.8f?new Vector2(point.z,point.y)/3:new Vector2(point.x,point.z)/3);}
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            Q(new Vector3(min.x,min.y,min.z),new Vector3(min.x,max.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(max.x,min.y,min.z));
            Q(new Vector3(max.x,min.y,max.z),new Vector3(max.x,max.y,max.z),new Vector3(min.x,max.y,max.z),new Vector3(min.x,min.y,max.z));
            Q(new Vector3(min.x,min.y,max.z),new Vector3(min.x,max.y,max.z),new Vector3(min.x,max.y,min.z),new Vector3(min.x,min.y,min.z));
            Q(new Vector3(max.x,min.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(max.x,max.y,max.z),new Vector3(max.x,min.y,max.z));
            Q(new Vector3(min.x,max.y,min.z),new Vector3(min.x,max.y,max.z),new Vector3(max.x,max.y,max.z),new Vector3(max.x,max.y,min.z));
            Q(new Vector3(min.x,min.y,max.z),new Vector3(min.x,min.y,min.z),new Vector3(max.x,min.y,min.z),new Vector3(max.x,min.y,max.z));
        }
        static void SaveFacade(Transform parent,string id,List<Vector3> v,List<Vector2> uv,List<int> t,Material material)
        {
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Generated+"/Meshes/BV2_Facade_"+id+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous){EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);mesh=previous;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            var obj=Group("BV2_Continuous facade masonry",parent,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
