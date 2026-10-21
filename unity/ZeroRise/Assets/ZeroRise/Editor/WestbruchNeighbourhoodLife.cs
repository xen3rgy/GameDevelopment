using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // A local, repeatable art pass. The district shell, navigation and entrances
    // stay authored by the existing systems; this adds selected signs of use.
    internal static class WestbruchNeighbourhoodLife
    {
        internal const string RootName="Westbruch inhabited facades and everyday life";
        static Material Metal=>Material("UrbanLife_PaintedSteel","steel",new Color(.27f,.31f,.28f),.23f,.40f);
        static Material Wood=>Material("UrbanLife_AgedTimber","wood",new Color(.65f,.56f,.42f),.09f);
        static Material Rubber=>Material("UrbanLife_Rubber",null,new Color(.045f,.05f,.045f),.08f);
        static Material Sack=>Material("UrbanLife_CreasedRefuse",null,new Color(.075f,.085f,.075f),.21f);
        static readonly Color[] Paint={new Color(.22f,.32f,.29f),new Color(.37f,.27f,.22f),new Color(.29f,.34f,.38f),new Color(.43f,.40f,.31f)};
        static Material Finish(int i)=>Material("UrbanLife_EntrancePaint_"+(i%4),null,Paint[i%4],.21f,.12f);
        static Material Plaster(int i)=>Material("UrbanLife_RepairPlaster_"+(i%3),"plaster",new[]{new Color(.69f,.65f,.55f),new Color(.61f,.62f,.57f),new Color(.72f,.68f,.60f)}[i%3],.06f);

        public static void Build(Transform parent,WestbruchLayout layout)
        {
            var previous=GameObject.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous);
            var root=Group(RootName,parent,Vector3.zero).transform;
            int index=0;
            foreach(var b in layout.buildings)
            {
                var source=GameObject.Find(b.id);if(!source){index++;continue;}
                var facade=Group("Inhabited facade "+b.id,root,root.InverseTransformPoint(source.transform.position)).transform;
                facade.rotation=source.transform.rotation;
                if(b.kind=="housing"||b.kind=="shop")
                {
                    Entrance(facade,b,index);
                    // Two selected apartments per block. Most windows stay quiet.
                    float first=-b.w*.5f+2.5f;
                    WindowRoom(facade,b,first+3.8f,1.7f+3.05f,index);
                    float last=first;
                    while(last+3.8f<b.w*.5f-1.6f)last+=3.8f;
                    if(index%2==0&&b.kind!="shop"&&Mathf.Abs(last)>2)WindowRoom(facade,b,last,1.7f,index+1);
                    if(index%3==0)RepairedWindow(facade,b,first,1.7f,index);
                    WallRepairs(facade,b,index);
                }
                if(index==1||index==4||b.kind=="workshop"||b.kind=="garage")Graffiti(facade,b,index);
                index++;
            }
            Physics.SyncTransforms();
            Pocket(root,layout,"Uferhof_12",new Vector3(-7.1f,0,7.78f),"Resident refuse collection",0);
            Pocket(root,layout,"Uferhof_14",new Vector3(6.4f,0,7.78f),"Locked courtyard bicycle",1);
            Pocket(root,layout,"Nordhof_8",new Vector3(-6.1f,0,7.80f),"Resident potting corner",2);
            Pocket(root,layout,"Suedhof_4",new Vector3(-6.1f,0,7.78f),"Household moving cartons",3);
            Pocket(root,layout,"Pfand_und_Spar",new Vector3(14.1f,0,-3.8f),"Grocer bottle delivery",4,90);
            Pocket(root,layout,"Werkstatt",new Vector3(16.80f,0,4.0f),"Workshop salvage corner",5);
            Pocket(root,layout,"Nordring_33",new Vector3(7.2f,0,7.78f),"North block recycling",0);
            Pocket(root,layout,"Stadthof_25",new Vector3(8.8f,0,8.78f),"Resident bicycle repair",6);
        }

        static void Entrance(Transform root,BuildingPlot b,int seed)
        {
            float wall=-b.d*.5f;var paint=Finish(seed);
            // Raised panels sit forward of the existing door, never coplanar.
            Box("Renewed resident entrance door",root,new Vector3(0,1.11f,wall-.205f),new Vector3(1.23f,2.13f,.035f),paint,false,.6f);
            for(int s=-1;s<=1;s+=2)
            {
                Box("Entrance frosted upper pane",root,new Vector3(s*.285f,seed%3==0?1.77f:1.65f,wall-.229f),new Vector3(.46f,seed%3==0?.38f:.63f,.013f),Material("UrbanLife_FrostedDoor",null,new Color(.27f,.34f,.32f),.53f),false);
                Box("Entrance lower timber panel",root,new Vector3(s*.285f,.60f,wall-.231f),new Vector3(.46f,.65f,.018f),paint,false);
                Box("Repainted door surround",root,new Vector3(s*.735f,1.13f,wall-.17f),new Vector3(.095f,2.32f,.075f),paint,false);
            }
            Box("Resident door pull",root,new Vector3(.43f,1.01f,wall-.274f),new Vector3(.028f,.22f,.04f),Metal,false);
            Box("Door brass letter slot",root,new Vector3(-.27f,1.11f,wall-.243f),new Vector3(.30f,.045f,.018f),Metal,false);
            float canopyWidth=2.56f+seed%3*.05f;
            var cap=Box("Entrance canopy metal weather cap",root,new Vector3(0,seed%3==2?2.68f:2.62f,wall-.72f),new Vector3(canopyWidth,.06f,1.44f),paint,false);
            if(seed%3==2)cap.transform.localRotation=Quaternion.Euler(-3,0,0);
            Box("Canopy drip edge",root,new Vector3(0,2.57f,wall-1.455f),new Vector3(canopyWidth,.11f,.03f),Metal,false);
            if(seed%3==1)
                foreach(int side in new[]{-1,1})Beam("Entrance canopy iron bracket",root,new Vector3(side*1.13f,2.05f,wall-.14f),new Vector3(side*1.13f,2.42f,wall-1.22f),.035f,Metal);
            if(seed%3==0)
                Box("Old entrance timber canopy fascia",root,new Vector3(0,2.50f,wall-1.484f),new Vector3(canopyWidth,.13f,.025f),Wood,false,.75f);
            for(int i=0;i<3;i++)
            {
                Box("Individual mailbox name card",root,new Vector3(1.05f,1.06f+i*.18f,wall-.19f),new Vector3(.32f,.075f,.018f),Material("UrbanLife_NameCards",null,new Color(.65f,.64f,.55f),.12f),false);
                Box("Resident intercom button",root,new Vector3(.80f,1.14f+i*.12f,wall-.21f),new Vector3(.05f,.045f,.04f),Metal,false);
            }
            // Fixed, wall-mounted details leave the 2.2 m entrance approach free.
            if(seed%3==1)
            {
                Box("Residents noticeboard frame",root,new Vector3(-1.45f,1.40f,wall-.075f),new Vector3(.67f,.83f,.08f),paint,false);
                Box("Residents noticeboard cork",root,new Vector3(-1.45f,1.40f,wall-.123f),new Vector3(.57f,.73f,.012f),Wood,false,.4f);
                for(int n=0;n<3;n++)Box("Faded local notice",root,new Vector3(-1.61f+(n%2)*.28f,1.57f-(n/2)*.29f,wall-.133f-n*.002f),new Vector3(.21f,.25f,.004f),Material("UrbanLife_NoticePaper_"+n,null,new[]{new Color(.65f,.65f,.57f),new Color(.57f,.61f,.56f),new Color(.66f,.60f,.48f)}[n],.04f),false);
            }
        }

        static void WindowRoom(Transform root,BuildingPlot b,float x,float y,int seed)
        {
            // A shallow geometric room insert covers the opaque original glass.
            // Reveals, furniture, curtain and glazing have separate depth planes.
            float wall=-b.d*.5f;
            var room=Group("Visible apartment interior "+seed,root,new Vector3(x,y,wall)).transform;
            var dark=Material("UrbanLife_RoomShadow",null,new Color(.085f,.095f,.086f),.05f);
            var lining=Material("UrbanLife_InteriorWall_"+(seed%3),null,new[]{new Color(.38f,.35f,.29f),new Color(.31f,.35f,.33f),new Color(.40f,.37f,.30f)}[seed%3],.09f);
            Box("Apartment rear wall",room,new Vector3(0,0,-.151f),new Vector3(1.30f,1.65f,.018f),dark,false);
            Box("Inner room paint",room,new Vector3(0,.14f,-.164f),new Vector3(1.20f,1.30f,.012f),lining,false);
            foreach(int side in new[]{-1,1})
            {
                Box("Window deep side reveal",room,new Vector3(side*.64f,0,-.225f),new Vector3(.07f,1.68f,.14f),Plaster(seed),false,.5f);
                Box("Window renewed vertical frame",room,new Vector3(side*.68f,0,-.307f),new Vector3(.065f,1.76f,.035f),Finish(seed),false);
                Box("Window room floor and ceiling",room,new Vector3(0,side*.81f,-.225f),new Vector3(1.27f,.045f,.14f),lining,false);
                Box("Window renewed horizontal frame",room,new Vector3(0,side*.865f,-.307f),new Vector3(1.43f,.065f,.035f),Finish(seed),false);
            }
            Box("Window central glazing bar",room,new Vector3(0,0,-.311f),new Vector3(.038f,1.66f,.035f),Finish(seed),false);
            Box("Interior room shelf",room,new Vector3(.30f,-.39f,-.227f),new Vector3(.53f,.05f,.09f),Wood,false,.5f);
            Box("Interior room cupboard",room,new Vector3(.31f,-.60f,-.205f),new Vector3(.46f,.36f,.065f),Wood,false,.5f);
            var curtain=Material("UrbanLife_Curtain_"+(seed%3),null,new[]{new Color(.50f,.44f,.33f),new Color(.34f,.41f,.40f),new Color(.50f,.49f,.42f)}[seed%3],.05f);
            for(int fold=0;fold<5;fold++)Box("Gathered curtain fold",room,new Vector3(-.52f+fold*.044f,.02f,-.249f-(fold%2)*.014f),new Vector3(.045f,1.48f,.022f),curtain,false);
            if(seed%2==0)
            {
                for(int n=0;n<4;n++)Box("Books visible behind glazing",room,new Vector3(.15f+n*.065f,-.27f,-.225f),new Vector3(.045f,.19f+n%2*.04f,.05f),Finish(seed+n),false);
                Box("Interior picture frame",room,new Vector3(.28f,.37f,-.180f),new Vector3(.32f,.38f,.016f),Wood,false);
                Box("Muted framed picture",room,new Vector3(.28f,.37f,-.192f),new Vector3(.25f,.31f,.008f),Finish(seed+1),false);
            }
            else
            {
                Cylinder("Apartment plant pot",room,new Vector3(.28f,-.33f,-.232f),.065f,.11f,Material("UrbanLife_Terracotta","brick",new Color(.55f,.34f,.22f),.08f));
                var foliage=Material("UrbanLife_IndoorLeaves",null,new Color(.18f,.27f,.16f),.08f);
                for(int leaf=0;leaf<5;leaf++)Beam("Plant behind resident window",room,new Vector3(.28f,-.28f,-.231f),new Vector3(.28f+Mathf.Sin(leaf*2.4f)*.11f,-.07f+leaf%2*.05f,-.231f),.025f,foliage);
            }
            Box("Sealed apartment glazing",room,new Vector3(0,0,-.292f),new Vector3(1.26f,1.62f,.004f),Glazing(),false);
            Box("Replaced window drainage sill",room,new Vector3(0,-.93f,-.245f),new Vector3(1.65f,.07f,.34f),Plaster(seed),false,.65f);
        }
        static Material Glazing()
        {
            var glass=Material("UrbanLife_TransparentGlazing",null,new Color(.38f,.48f,.45f,.08f),.72f);
            glass.SetFloat("_SurfaceType",1);glass.SetFloat("_BlendMode",0);glass.SetFloat("_ZWrite",0);glass.SetFloat("_TransparentZWrite",0);
            glass.SetFloat("_EnableBlendModePreserveSpecularLighting",0);glass.renderQueue=(int)RenderQueue.Transparent;
            HDMaterial.ValidateMaterial(glass);EditorUtility.SetDirty(glass);return glass;
        }
        static void RepairedWindow(Transform root,BuildingPlot b,float x,float y,int seed)
        {
            float wall=-b.d*.5f;
            var repair=Group("Temporary repaired apartment window "+seed,root,new Vector3(x,y,wall)).transform;
            Box("Single replaced lower plywood pane",repair,new Vector3(-.32f,-.30f,-.137f),new Vector3(.58f,.78f,.025f),Wood,false,.65f);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Window repair timber cleat",repair,new Vector3(-.32f,side==1?.08f:-.68f,-.163f),new Vector3(.63f,.045f,.024f),Wood,false,.65f);
                foreach(float dx in new[]{-.23f,.23f})
                {
                    var screw=Cylinder("Repair screw",repair,new Vector3(-.32f+dx,side==1?.08f:-.68f,-.180f),.008f,.009f,Metal);
                    screw.transform.localRotation=Quaternion.Euler(90,0,0);
                }
            }
        }
        static void WallRepairs(Transform root,BuildingPlot b,int seed)
        {
            float wall=-b.d*.5f;
            // Localised repairs occupy the blank foundation zone below windows.
            foreach(int side in new[]{-1,1})
            {
                float x=side*(b.w*.5f-2.5f);
                Box("Repointed foundation patch",root,new Vector3(x,.40f,wall-.0515f),new Vector3(seed%2==0?1.35f:.8f,.22f,.025f),Plaster(seed+side+3),false,.7f);
            }
            if(seed%4==2)
            {
                var face=Group("Side gable render repair",root,new Vector3(b.w*.5f,0,0),-90).transform;
                Box("Older lime render repair",face,new Vector3(0,.49f,-.008f),new Vector3(1.8f,.39f,.018f),Plaster(seed),false,.75f);
            }
        }
        static void Graffiti(Transform root,BuildingPlot b,int seed)
        {
            // Painted strokes are thin attached strips, with deliberate breaks.
            // No hovering decal card or large opaque rectangle covers the wall.
            var tag=Group("Weathered small Westbruch graffiti "+b.id,root,new Vector3(b.w*.5f+.002f,3.2f,0),-90).transform;
            var paint=Material("UrbanLife_FadedGraffiti_"+(seed%2),null,seed%2==0?new Color(.20f,.23f,.20f):new Color(.35f,.23f,.20f),.02f);
            var points=new[]{new[]{new Vector2(-1, .45f),new Vector2(-.88f,-.28f),new Vector2(-.62f,.11f),new Vector2(-.45f,-.26f),new Vector2(-.30f,.45f)},
                new[]{new Vector2(-.05f,.40f),new Vector2(-.17f,-.25f),new Vector2(.24f,-.21f)},
                new[]{new Vector2(-.08f,.39f),new Vector2(.29f,.38f)},new[]{new Vector2(-.12f,.08f),new Vector2(.18f,.07f)},
                new[]{new Vector2(.80f,.37f),new Vector2(.47f,.40f),new Vector2(.40f,.05f),new Vector2(.76f,-.05f),new Vector2(.72f,-.25f),new Vector2(.34f,-.25f)},
                new[]{new Vector2(.85f,.40f),new Vector2(1.34f,.40f)},new[]{new Vector2(1.12f,.40f),new Vector2(1.03f,-.25f)}};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var line in points)for(int i=1;i<line.Length;i++)
            {
                Vector2 a=line[i-1],c=line[i],delta=c-a;int count=Mathf.CeilToInt(delta.magnitude/.10f);
                for(int step=0;step<count;step++)
                {
                    if((step+i+seed)%7==3)continue;
                    var start=Vector2.Lerp(a,c,(float)step/count);var end=Vector2.Lerp(a,c,(step+.91f)/count);
                    var n=new Vector2(-delta.y,delta.x).normalized*.019f;int at=vertices.Count;
                    foreach(var p in new[]{start-n,start+n,end+n,end-n}){vertices.Add(new Vector3(p.x,p.y,-.001f));uv.Add(p);}
                    triangles.AddRange(new[]{at,at+1,at+2,at,at+2,at+3});
                }
            }
            var mesh=SaveMesh("UrbanLife_Graffiti_"+seed,vertices,uv,triangles);
            Model("Faded paint attached to brickwork",tag,Vector3.zero,mesh,paint).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        static void Pocket(Transform parent,WestbruchLayout layout,string id,Vector3 at,string name,int type,float extraYaw=0)
        {
            var b=layout.buildings.FirstOrDefault(p=>p.id==id);var house=GameObject.Find(id);if(b==null||!house)return;
            var world=house.transform.TransformPoint(at);float floor=Floor(world.x,world.z);
            float padBottom=0,padTop=0;
            if(type==4)
            {
                var rotation=house.transform.rotation*Quaternion.Euler(0,extraYaw,0);
                float low=floor,high=floor;
                foreach(float x in new[]{-.80f,0,.80f})foreach(float z in new[]{-.40f,0,.40f})
                {
                    var sample=world+rotation*new Vector3(x,0,z);float height=Floor(sample.x,sample.z);
                    low=Mathf.Min(low,height);high=Mathf.Max(high,height);
                }
                if(high-low>.35f)throw new InvalidOperationException("Grocer side delivery footing requires more than35cm of terrain correction.");
                padBottom=low-.04f;padTop=high+.018f;floor=padTop;
                var occupied=Physics.OverlapBox(new Vector3(world.x,padTop+.37f,world.z),new Vector3(.68f,.35f,.25f),rotation,1,QueryTriggerInteraction.Ignore)
                    .Where(c=>!(c is TerrainCollider)&&!(c is CharacterController)).ToArray();
                if(occupied.Length>0)throw new InvalidOperationException("Grocer delivery site intersects existing scene furniture: "+string.Join(", ",occupied.Select(c=>c.name)));
            }
            // Rigid small objects are placed only on an effectively level foot.
            if(type!=4&&(Mathf.Abs(Floor(world.x-.65f,world.z)-floor)>.045f||Mathf.Abs(Floor(world.x+.65f,world.z)-floor)>.045f))return;
            var root=Group("UrbanLife site - "+name,parent,parent.InverseTransformPoint(new Vector3(world.x,floor-.008f,world.z))).transform;
            root.rotation=house.transform.rotation*Quaternion.Euler(0,extraYaw,0);
            if(type==4)
            {
                Box("Grocer delivery hardstanding",root,new Vector3(0,(padBottom+padTop)*.5f-root.position.y,0),new Vector3(1.6f,padTop-padBottom,.8f),Material("UrbanLife_DeliveryConcrete","concrete",new Color(.62f,.62f,.57f),.08f),true,1.5f);
                Physics.SyncTransforms();
            }
            switch(type)
            {
                case 0:
                    RefuseBag(root,new Vector3(-.32f,0,.03f),type+1);
                    RefuseBag(root,new Vector3(.16f,0,-.06f),type+2);
                    Crate(root,new Vector3(.67f,0,.02f),0,false);
                    break;
                case 1:
                case 6:
                    Bicycle(root,type==6);break;
                case 2:
                    Crate(root,new Vector3(-.52f,0,0),0,false);
                    FlowerPot(root,new Vector3(.10f,0,0),.17f);
                    FlowerPot(root,new Vector3(.53f,0,.02f),.12f);
                    Box("Folded gardening board",root,Seated(root,new Vector3(-.50f,.3375f,0)),new Vector3(.73f,.035f,.38f),Wood,false,.6f);
                    break;
                case 3:
                    Carton(root,new Vector3(-.39f,0,0),new Vector3(.59f,.39f,.44f),-7);
                    Carton(root,new Vector3(.27f,0,0),new Vector3(.47f,.32f,.42f),10);
                    Carton(root,new Vector3(-.36f,.39f,.02f),new Vector3(.44f,.27f,.36f),-3);
                    break;
                case 4:
                    Crate(root,new Vector3(-.35f,0,0),1,true);
                    Crate(root,new Vector3(-.35f,.32f,0),1,true);
                    Crate(root,new Vector3(.34f,0,0),1,false);
                    break;
                case 5:
                    Crate(root,new Vector3(0,0,0),0,false);
                    for(int i=0;i<4;i++)Box("Stored workshop offcut",root,Seated(root,new Vector3(-.12f+i*.075f,.3325f+i*.026f,0)),new Vector3(.72f,.025f,.095f),Wood,false,.65f);
                    Carton(root,new Vector3(.66f,0,.06f),new Vector3(.42f,.24f,.39f),9);
                    break;
            }
        }
        static float Floor(float x,float z)
        {
            float floor=Terrain.activeTerrain?Terrain.activeTerrain.SampleHeight(new Vector3(x,0,z))+Terrain.activeTerrain.transform.position.y:0;
            foreach(var h in Physics.RaycastAll(new Vector3(x,8,z),Vector3.down,12,1,QueryTriggerInteraction.Ignore))
                if(h.normal.y>.95f&&(h.collider.name=="Connected pavement"||h.collider.name=="Road cell"||h.collider.name.Contains("hardstanding")))floor=Mathf.Max(floor,h.point.y);
            return floor;
        }
        static void Carton(Transform parent,Vector3 at,Vector3 size,float yaw)
        {
            var root=Group("Taped delivery carton",parent,Seated(parent,at),yaw).transform;
            var board=Material("UrbanLife_UsedCardboard","wood",new Color(.65f,.54f,.36f),.04f);
            var tape=Material("UrbanLife_CartonTape",null,new Color(.47f,.39f,.26f),.19f);
            Box("Delivery carton body",root,Vector3.up*size.y*.5f,size,board,false,.5f);
            Box("Carton parcel tape",root,new Vector3(0,size.y+.003f,0),new Vector3(.06f,.005f,size.z+.004f),tape,false);
            Box("Carton address label",root,new Vector3(.12f,size.y+.006f,.01f),new Vector3(.13f,.003f,.10f),Material("UrbanLife_ParcelLabel",null,new Color(.73f,.72f,.64f),.02f),false);
        }
        static void Crate(Transform parent,Vector3 at,int color,bool bottles)
        {
            var root=Group("Slotted reusable delivery crate",parent,Seated(parent,at)).transform;var material=color==0?Wood:Finish(0);
            Box("Delivery crate base",root,new Vector3(0,.025f,0),new Vector3(.60f,.05f,.40f),material,false,.5f);
            foreach(int side in new[]{-1,1})
            {
                Box("Crate end",root,new Vector3(side*.285f,.17f,0),new Vector3(.03f,.29f,.40f),material,false,.5f);
                Box("Crate upper rim",root,new Vector3(0,.30f,side*.195f),new Vector3(.60f,.04f,.03f),material,false,.5f);
                for(int n=0;n<6;n++)Box("Crate open side slat",root,new Vector3(-.255f+n*.102f,.17f,side*.195f),new Vector3(.028f,.23f,.025f),material,false,.5f);
            }
            if(bottles)for(int x=0;x<4;x++)for(int z=0;z<2;z++)
            {
                var glass=Material("UrbanLife_ReturnBottle",null,new Color(.12f,.19f,.11f),.57f,.10f);
                Cylinder("Returned bottle body",root,new Vector3(-.20f+x*.13f,.17f,-.085f+z*.17f),.044f,.23f,glass);
                Cylinder("Returned bottle neck",root,new Vector3(-.20f+x*.13f,.30f,-.085f+z*.17f),.018f,.03f,glass);
            }
        }
        static void FlowerPot(Transform parent,Vector3 at,float radius)
        {
            var pot=Group("Resident courtyard flowerpot",parent,Seated(parent,at)).transform;var terracotta=Material("UrbanLife_Terracotta","brick",new Color(.55f,.34f,.22f),.08f);
            Cylinder("Flowerpot body",pot,new Vector3(0,radius*.75f,0),radius,radius*1.5f,terracotta);
            Cylinder("Flowerpot rim",pot,new Vector3(0,radius*1.43f,0),radius*1.09f,radius*.17f,terracotta);
            Cylinder("Flowerpot dark soil",pot,new Vector3(0,radius*1.52f,0),radius*.91f,.012f,Material("UrbanLife_PotSoil","mud",new Color(.35f,.29f,.22f),.02f));
            var leaf=Material("UrbanLife_IndoorLeaves",null,new Color(.18f,.27f,.16f),.08f);
            for(int n=0;n<7;n++)
            {
                float a=n*2.39996f;
                Beam("Potted plant stem",pot,new Vector3(0,radius*1.5f,0),new Vector3(Mathf.Sin(a)*radius*.7f,radius*(2.2f+n%3*.2f),Mathf.Cos(a)*radius*.7f),.018f,leaf);
            }
        }
        static void RefuseBag(Transform parent,Vector3 at,int seed)
        {
            string path=Generated+"/Meshes/UrbanLife_RefuseBag_"+seed+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                float[] heights={0,.035f,.17f,.33f,.43f,.475f,.50f};float[] radii={.145f,.19f,.22f,.19f,.09f,.027f,.055f};const int sides=16;
                for(int ring=0;ring<heights.Length;ring++)for(int n=0;n<=sides;n++)
                {
                    float angle=n*Mathf.PI*2/sides,r=radii[ring]*(1+Mathf.Sin(n*2.7f+seed+ring*.8f)*.075f);
                    vertices.Add(new Vector3(Mathf.Sin(angle)*r,heights[ring],Mathf.Cos(angle)*r*.85f));uv.Add(new Vector2(n/(float)sides,heights[ring]));
                    if(ring>0&&n<sides){int v=ring*(sides+1)+n;triangles.AddRange(new[]{v,v-sides-1,v+1,v+1,v-sides-1,v-sides});}
                }
                mesh=SaveMesh("UrbanLife_RefuseBag_"+seed,vertices,uv,triangles);
            }
            var bag=Model("Grounded tied refuse sack",parent,Seated(parent,at),mesh,Sack);bag.transform.localRotation=Quaternion.Euler(0,seed*43,0);
        }
        static void Bicycle(Transform parent,bool repair)
        {
            var bike=Group("Parked resident utility bicycle",parent,Vector3.zero).transform;var frame=Finish(repair?2:1);
            foreach(float x in new[]{-.61f,.61f})
            {
                var ring=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/Ring_330_22.asset");
                if(ring)Model("Bicycle grounded tyre",bike,new Vector3(x,.352f,0),ring,Rubber);
                for(int n=0;n<12;n++)
                {
                    float a=n*Mathf.PI/6;Beam("Bicycle wheel spoke",bike,new Vector3(x,.352f,0),new Vector3(x+Mathf.Cos(a)*.305f,.352f+Mathf.Sin(a)*.305f,0),.006f,Metal);
                }
            }
            var rear=new Vector3(-.61f,.352f,0);var front=new Vector3(.61f,.352f,0);var crank=new Vector3(-.10f,.30f,0);var seat=new Vector3(-.26f,.85f,0);var bar=new Vector3(.33f,.88f,0);
            foreach(var pair in new[]{(rear,crank),(rear,seat),(seat,crank),(seat,bar),(crank,bar),(bar,front)})Beam("Resident bicycle frame",bike,pair.Item1,pair.Item2,.029f,frame);
            Box("Bicycle saddle",bike,new Vector3(-.27f,.91f,0),new Vector3(.26f,.055f,.15f),Rubber,false);
            Beam("Bicycle handlebar",bike,new Vector3(.36f,.97f,-.23f),new Vector3(.36f,.97f,.23f),.024f,Metal);
            Beam("Bicycle kickstand on ground",bike,crank,new Vector3(-.16f,0,.20f),.018f,Metal);
            Beam("Cycle lock to rack",bike,new Vector3(-.5f,.40f,0),new Vector3(-.5f,.33f,.23f),.015f,Rubber);
            foreach(int side in new[]{-1,1})Beam("Anchored bicycle parking hoop",bike,new Vector3(side*.52f,0,.24f),new Vector3(side*.52f,.64f,.24f),.035f,Metal);
            Beam("Bicycle parking hoop top",bike,new Vector3(-.52f,.64f,.24f),new Vector3(.52f,.64f,.24f),.035f,Metal);
            Box("Utility bicycle luggage rack",bike,new Vector3(-.60f,.72f,0),new Vector3(.51f,.035f,.24f),Metal,false);
            if(repair)Crate(parent,new Vector3(1.20f,0,0),0,false);
        }
        static GameObject Model(string name,Transform parent,Vector3 at,Mesh mesh,Material material)
        {
            var model=Group(name,parent,at);model.isStatic=true;model.AddComponent<MeshFilter>().sharedMesh=mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial=material;return model;
        }
        static Vector3 Seated(Transform parent,Vector3 at)
        {
            // Preserve deliberately stacked height while seating each footprint,
            // rather than assuming an entire courtyard is one flat elevation.
            var world=parent.TransformPoint(new Vector3(at.x,0,at.z));
            at.y+=Floor(world.x,world.z)-parent.position.y-.008f;return at;
        }
        static Mesh SaveMesh(string id,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            string path=Generated+"/Meshes/"+id+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
    }
}
