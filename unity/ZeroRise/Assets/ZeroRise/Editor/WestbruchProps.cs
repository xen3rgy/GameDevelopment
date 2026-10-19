using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchProps
    {
        static Material metal => Material("PropMetal","steel",new Color(.46f,.51f,.46f),.32f,.6f);
        static Material timber => Material("PropWood","wood",new Color(.74f,.63f,.47f));
        static Material stone => Material("PropConcrete","concrete",new Color(.8f,.8f,.77f));
        public static void Build(Transform root,WestbruchLayout layout)
        {
            var props=Group("Street and courtyard details",root,Vector3.zero).transform;
            Playground(props);
            BusStop(props);
            Bridge(props);
            WorkshopYard(props);
            // Temporary construction fences delimit this first playable district.
            // They sit beyond all road/entrance probes and stop players falling off Terrain.
            Fence(props,new Vector3(-131.5f,0,-113.5f),new Vector3(131.5f,0,-113.5f));
            Fence(props,new Vector3(-131.5f,0,113.5f),new Vector3(131.5f,0,113.5f));
            Fence(props,new Vector3(-131.5f,0,-113.5f),new Vector3(-131.5f,0,113.5f));
            Fence(props,new Vector3(131.5f,0,-113.5f),new Vector3(131.5f,0,113.5f));
            foreach(int s in new[]{-1,1})
            {
                var signRoot=Group("Next district construction boundary",props,new Vector3(94,0,s*112.8f),s==1?0:180).transform;
                Sign("BAUABSCHNITT · TESTGEBIET",signRoot,new Vector3(0,.85f,0),4,metal,new Color(.94f,.83f,.56f));
            }
            foreach(var b in layout.buildings)
            {
                float front=b.z+(b.yaw==180?1:-1)*(b.d*.5f+1.2f);
                Bin(props,new Vector3(b.x-b.w*.5f+1.2f,.14f,front),b.yaw);
                Bin(props,new Vector3(b.x-b.w*.5f+2.3f,.14f,front+.2f),b.yaw);
                if(b.kind=="housing"||b.kind=="shop")
                {
                    Bench(props,new Vector3(b.x+b.w*.5f-2,.14f,front),b.yaw);
                    Pallet(props,new Vector3(b.x-b.w*.5f+3.5f,.14f,front+.3f));
                }
            }
            // Old electrical poles along the waterfront, well clear of roads.
            for(float z=-76;z<=80;z+=39)
            {
                Cylinder("Timber utility pole",props,new Vector3(-108,4,z),.13f,8,timber,true);
                Box("Cross arm",props,new Vector3(-108,7.4f,z),new Vector3(2.4f,.12f,.16f),timber,false);
                if(z<80) for(int wire=-1;wire<=1;wire+=2)
                {
                    Vector3 prev=new Vector3(-108+wire*.8f,7.5f,z);
                    for(int step=1;step<=8;step++)
                    {
                        float f=step/8f;var next=new Vector3(prev.x,7.5f-Mathf.Sin(f*Mathf.PI)*.6f,z+39*f);
                        Beam("Sagging cable",props,prev,next,.018f,metal);prev=next;
                    }
                }
            }
            var water=Material("CanalWater",null,new Color(.06f,.16f,.19f),.9f,.1f);
            Box("Canal",props,new Vector3(-122,-.18f,0),new Vector3(16,.12f,228),water,false);
            Box("Canal retaining wall",props,new Vector3(-112,.18f,0),new Vector3(1,.7f,228),stone);
            Fence(props,new Vector3(-111,.14f,-112),new Vector3(-111,.14f,112),false);
            // Detailed vegetation is baked by WestbruchLandscape after all paths exist.
            var wet=Material("ShallowPuddle",null,new Color(.13f,.17f,.18f),.91f,.25f);
            for(int i=0;i<22;i++)
            {
                float x=-80+i*7.3f,z=i%2==0?2.8f:-2.9f;
                if(i%4==0)
                {
                    var pool=Cylinder("Shallow puddle",props,new Vector3(x+1,.045f,z-.35f),.8f,.006f,wet);
                    pool.transform.localScale=new Vector3(1.6f,.003f,.7f);
                }
                if(i%5==0)
                {
                    Cylinder("Drain cover",props,new Vector3(x,.048f,-3.4f),.32f,.015f,metal);
                    Box("Gully grate",props,new Vector3(x+3,.15f,-5.4f),new Vector3(.45f,.025f,.28f),metal,false);
                }
            }
        }
        public static void Balcony(Transform root,float x,float y,float z)
        {
            Box("Balcony slab",root,new Vector3(x,y,z),new Vector3(2.3f,.16f,1.6f),stone);
            for(float xx=-1.05f;xx<1.1f;xx+=.23f)
                Beam("Balcony railing",root,new Vector3(x+xx,y+.12f,z-.72f),new Vector3(x+xx,y+1.08f,z-.72f),.035f,metal);
            Box("Balcony handrail",root,new Vector3(x,y+1.08f,z-.72f),new Vector3(2.3f,.045f,.045f),metal,false);
            foreach(int s in new[]{-1,1}) Box("Side handrail",root,new Vector3(x+s*1.08f,y+1.08f,z),new Vector3(.045f,.045f,1.5f),metal,false);
            Box("Planter",root,new Vector3(x+.65f,y+.27f,z-.4f),new Vector3(.58f,.28f,.22f),timber,false);
            if(Mathf.RoundToInt(x+y)%3==0)
                Box("Laundry over railing",root,new Vector3(x-.4f,y+.65f,z-.76f),new Vector3(.65f,.8f,.018f),Material("FadedLaundry",null,new Color(.44f,.39f,.32f)),false);
        }
        internal static void Bin(Transform parent,Vector3 p,float yaw)
        {
            var root=Group("Wheelie bin",parent,p,yaw).transform;
            Box("Bin body",root,new Vector3(0,.48f,0),new Vector3(.55f,.84f,.55f),metal);
            Box("Hinged lid",root,new Vector3(0,.93f,0),new Vector3(.62f,.075f,.63f),metal,false);
            foreach(int s in new[]{-1,1})
            {
                var wheel=Cylinder("Bin wheel",root,new Vector3(s*.25f,.1f,.19f),.1f,.08f,Material("Rubber",null,new Color(.06f,.06f,.06f)));
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
            }
        }
        internal static void Bench(Transform parent,Vector3 p,float yaw)
        {
            var root=Group("Worn public bench",parent,p,yaw).transform;
            for(int i=0;i<3;i++)
            {
                Box("Seat slat",root,new Vector3(0,.46f,(i-1)*.15f),new Vector3(1.75f,.055f,.12f),timber,false);
                Box("Back slat",root,new Vector3(0,.71f+i*.14f,.23f),new Vector3(1.75f,.1f,.05f),timber,false);
            }
            foreach(int s in new[]{-1,1})
            {
                Box("Steel leg",root,new Vector3(s*.63f,.24f,0),new Vector3(.08f,.48f,.5f),metal,false);
                Beam("Bench back upright",root,new Vector3(s*.63f,.27f,.19f),new Vector3(s*.63f,1.045f,.265f),.045f,metal);
                Box("Steel seat bearer",root,new Vector3(s*.63f,.425f,0),new Vector3(.09f,.045f,.48f),metal,false);
            }
            void Support(string name,Vector3 center,Vector3 size)
            {
                var part=Group(name,root,center);part.AddComponent<BoxCollider>().size=size;
            }
            Support("Bench seat support",new Vector3(0,.46f,0),new Vector3(1.75f,.055f,.42f));
            for(int i=0;i<3;i++)Support("Bench back support",new Vector3(0,.71f+i*.14f,.23f),new Vector3(1.75f,.1f,.05f));
            foreach(int side in new[]{-1,1})Support("Bench leg support",new Vector3(side*.63f,.24f,0),new Vector3(.08f,.48f,.5f));
        }
        internal static void Pallet(Transform parent,Vector3 p)
        {
            var root=Group("Discarded pallet",parent,p,8).transform;
            for(int i=0;i<3;i++) Box("Runner",root,new Vector3((i-1)*.38f,.055f,0),new Vector3(.1f,.11f,.8f),timber,false);
            for(int i=0;i<5;i++) Box("Slat",root,new Vector3(0,.14f,(i-2)*.17f),new Vector3(1.1f,.06f,.12f),timber,false);
        }
        internal static void Fence(Transform parent,Vector3 a,Vector3 b,bool mesh=true)
        {
            var delta=b-a; int n=Mathf.CeilToInt(delta.magnitude/2.4f);
            for(int i=0;i<=n;i++)
            {
                var p=Vector3.Lerp(a,b,i/(float)n);
                Cylinder("Fence post",parent,p+Vector3.up*.58f,.035f,1.16f,metal);
            }
            Beam("Upper rail",parent,a+Vector3.up*1.04f,b+Vector3.up*1.04f,.035f,metal);
            Beam("Lower rail",parent,a+Vector3.up*.2f,b+Vector3.up*.2f,.03f,metal);
            if(mesh)
            {
                int count=Mathf.CeilToInt(delta.magnitude/.32f);
                for(int i=0;i<count;i++)
                {
                    var start=Vector3.Lerp(a,b,i/(float)count)+Vector3.up*.22f;
                    var end=Vector3.Lerp(a,b,Mathf.Min(1,(i+3)/(float)count))+Vector3.up*1.02f;
                    Beam("Chain link diagonal",parent,start,end,.01f,metal);
                    Beam("Chain link diagonal",parent,start+Vector3.up*.8f,end-Vector3.up*.8f,.01f,metal);
                }
            }
            // Actual collision is one clean volume per fence span, not the tiny wires.
            var box=Box("Fence collider rail",parent,(a+b)*.5f+Vector3.up*.6f,new Vector3(.06f,1.2f,delta.magnitude),metal);
            box.transform.localRotation=Quaternion.LookRotation(delta);
            box.GetComponent<MeshRenderer>().enabled=false;
        }
        static void Playground(Transform parent)
        {
            var root=Group("Neighbourhood playground",parent,new Vector3(-65,.01f,-49)).transform;
            Box("Compacted play surface",root,new Vector3(0,.055f,0),new Vector3(30,.11f,14),Material("PlaySoil","sand",Color.white));
            Fence(root,new Vector3(-15,.1f,-7),new Vector3(15,.1f,-7));
            Fence(root,new Vector3(-15,.1f,7),new Vector3(15,.1f,7));
            Fence(root,new Vector3(-15,.1f,-7),new Vector3(-15,.1f,7));
            // Two openings meet the east access path, never a closed fence at its end.
            Fence(root,new Vector3(15,.1f,-7),new Vector3(15,.1f,-2));
            Fence(root,new Vector3(15,.1f,2),new Vector3(15,.1f,7));
            for(int s=-1;s<=1;s+=2)
            {
                Beam("Swing A-frame",root,new Vector3(-6+s*2.1f,.12f,-2),new Vector3(-6+s*2.1f,2.6f,0),.12f,timber,true);
                Beam("Swing A-frame",root,new Vector3(-6+s*2.1f,.12f,2),new Vector3(-6+s*2.1f,2.6f,0),.12f,timber,true);
            }
            Beam("Swing beam",root,new Vector3(-8.2f,2.6f,0),new Vector3(-3.8f,2.6f,0),.15f,timber);
            foreach(float x in new[]{-7f,-5})
            {
                foreach(float side in new[]{-.22f,.22f}) Beam("Swing chain",root,new Vector3(x+side,2.55f,0),new Vector3(x+side,.65f,0),.018f,metal);
                Box("Swing seat",root,new Vector3(x,.64f,0),new Vector3(.62f,.08f,.3f),timber);
            }
            Box("Slide platform",root,new Vector3(4,1.7f,2),new Vector3(1.3f,.15f,1.3f),timber);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}) Box("Slide post",root,new Vector3(4+x*.58f,.87f,2+z*.58f),new Vector3(.12f,1.74f,.12f),timber);
            var slideTop=new Vector3(4,1.77f,1.4f);var slideEnd=new Vector3(4,.2f,-1.5f);
            var slide=Box("Formed metal slide",root,(slideTop+slideEnd)*.5f,new Vector3(.74f,.045f,Vector3.Distance(slideTop,slideEnd)),metal);
            slide.transform.localRotation=Quaternion.LookRotation(slideEnd-slideTop);
            foreach(float s in new[]{-.36f,.36f})Beam("Slide side lip",root,slideTop+new Vector3(s,.035f,0),slideEnd+new Vector3(s,.035f,0),.04f,metal);
            foreach(float x in new[]{3.54f,4.46f}) Beam("Ladder rail",root,new Vector3(x,.12f,4.1f),new Vector3(x,1.8f,2.65f),.08f,timber);
            for(int i=0;i<5;i++) Beam("Ladder rung",root,new Vector3(3.54f,.3f+i*.3f,3.94f-i*.26f),new Vector3(4.46f,.3f+i*.3f,3.94f-i*.26f),.07f,timber);
            Box("Sandbox soil",root,new Vector3(9,.1f,-2),new Vector3(3.8f,.15f,3),Material("Sand", "sand",Color.white));
            foreach(int s in new[]{-1,1})
            {
                Box("Sandbox border",root,new Vector3(9+s*2,.2f,-2),new Vector3(.16f,.32f,3.3f),timber);
                Box("Sandbox border",root,new Vector3(9,.2f,-2+s*1.6f),new Vector3(4,.32f,.16f),timber);
            }
            Bench(root,new Vector3(-11,.1f,4),180);Bench(root,new Vector3(10,.1f,4),180);
        }
        static void BusStop(Transform parent)
        {
            var root=Group("Old Westbruch bus shelter",parent,new Vector3(105,.14f,17),90).transform;
            foreach(int s in new[]{-1,1}) Box("Shelter post",root,new Vector3(s*2,1.3f,.8f),new Vector3(.065f,2.6f,.065f),metal);
            Box("Shelter roof",root,new Vector3(0,2.63f,0),new Vector3(4.6f,.12f,2.5f),metal);
            Box("Weathered back panel",root,new Vector3(0,1.4f,.83f),new Vector3(4,2.2f,.06f),Material("ShelterGlass",null,new Color(.20f,.26f,.25f),.6f),true);
            Bench(root,new Vector3(0,0,.4f),0);
            Cylinder("Bus stop signpost",root,new Vector3(-2.7f,1.7f,0),.035f,3.4f,metal,true);
            Sign("H · WESTBRUCH",root,new Vector3(-2.7f,3.1f,0),1.3f,Material("BusYellow",null,new Color(.62f,.57f,.18f)),new Color(.04f,.22f,.13f));
            Sign("LINIE 4 · STADTMITTE",root,new Vector3(0,2.15f,.77f),3,metal,Color.white);
        }
        static void Bridge(Transform parent)
        {
            var root=Group("Rail underpass – east connection",parent,new Vector3(94,0,65)).transform;
            Box("Bridge deck",root,new Vector3(0,6.2f,0),new Vector3(44,.9f,8),stone);
            foreach(int s in new[]{-1,1})
            {
                Box("Abutment",root,new Vector3(s*16,2.9f,0),new Vector3(2.2f,5.8f,8),stone);
                Box("Railway parapet",root,new Vector3(0,7,s*3.8f),new Vector3(44,.7f,.35f),stone);
                Box("Rail",root,new Vector3(0,6.76f,s*.72f),new Vector3(44,.12f,.09f),metal,false);
                Box("Bridge girder",root,new Vector3(0,5.56f,s*2.8f),new Vector3(40,.45f,.32f),metal);
            }
            for(float x=-21;x<22;x+=.6f) Box("Rail sleeper",root,new Vector3(x,6.69f,0),new Vector3(.19f,.12f,2.1f),timber,false);
            foreach(int s in new[]{-1,1})
            {
                var lightObj=Group("Underpass warm lamp",root,new Vector3(s*10,5.45f,0));
                var light=lightObj.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.38f);light.range=10;
                lightObj.AddComponent<HDAdditionalLightData>();light.intensity=500;
                Box("Lamp housing",root,new Vector3(s*10,5.58f,0),new Vector3(.65f,.1f,.35f),metal,false);
            }
            Sign("WESTBRUCH",root,new Vector3(0,5.9f,-4.06f),5,metal,Color.white);
        }
        static void WorkshopYard(Transform parent)
        {
            var root=Group("Workshop forecourt props",parent,new Vector3(55,.14f,-44)).transform;
            for(int i=0;i<3;i++) Pallet(root,new Vector3(18,.19f*i,2));
            var rubber=Material("Rubber",null,new Color(.055f,.055f,.052f),.13f);
            for(int i=0;i<4;i++) Cylinder("Used tyre stack",root,new Vector3(20,.14f+i*.28f,2),.39f,.26f,rubber,true);
            var container=Group("Corrugated parts container",parent,new Vector3(75,.14f,-60),0).transform;
            Box("Container shell",container,new Vector3(0,1.25f,0),new Vector3(3,2.5f,7),metal);
            for(float z=-3.3f;z<3.5f;z+=.28f)foreach(int s in new[]{-1,1}) Box("Container corrugation",container,new Vector3(s*1.52f,1.25f,z),new Vector3(.08f,2.44f,.07f),metal,false);
            foreach(int s in new[]{-1,1})
            {
                Box("Container door",container,new Vector3(s*.72f,1.25f,-3.55f),new Vector3(1.4f,2.4f,.08f),metal,false);
                Box("Door locking bar",container,new Vector3(s*.65f,1.25f,-3.63f),new Vector3(.04f,2.25f,.04f),stone,false);
            }
            Sign("TEILELAGER",container,new Vector3(0,1.9f,-3.65f),2,metal,Color.white);
        }
        static void Vegetation(Transform parent,WestbruchLayout layout)
        {
            var rng=new System.Random(13026);
            var grass=Material("DryGrass",null,new Color(.27f,.30f,.14f));
            var leaf=Material("ShrubLeaf",null,new Color(.21f,.29f,.14f));
            for(int i=0;i<650;i++)
            {
                float x=(float)rng.NextDouble()*202-105,z=(float)rng.NextDouble()*156-78;
                if(Array.Exists(layout.roads,r=>Mathf.Abs(x-r.x)<r.w*.5f+3.4f&&Mathf.Abs(z-r.z)<r.d*.5f+3.4f))continue;
                if(Array.Exists(layout.paths,r=>Mathf.Abs(x-r.x)<r.w*.5f+1&&Mathf.Abs(z-r.z)<r.d*.5f+1))continue;
                if(Array.Exists(layout.buildings,b=>Mathf.Abs(x-b.x)<b.w*.5f+1.8f&&Mathf.Abs(z-b.z)<b.d*.5f+1.8f))continue;
                if(x>-82&&x<-32&&z>-72&&z<-39)continue;
                float height=.12f+(float)rng.NextDouble()*.28f;
                var clump=Group("Sparse verge weeds",parent,new Vector3(x,0,z),(float)rng.NextDouble()*360).transform;
                for(int blade=0;blade<3;blade++)
                    Beam("Grass blade",clump,new Vector3(blade*.045f,0,0),new Vector3(blade*.075f,height,blade*.045f),.017f,grass);
                if(i%17==0)
                {
                    Beam("Shrub stem",clump,Vector3.zero,new Vector3(.05f,.75f,0),.025f,timber);
                    for(int n=0;n<7;n++)
                    {
                        var l=Box("Branch leaf spray",clump,new Vector3(Mathf.Sin(n*2.4f)*.3f,.25f+n*.075f,Mathf.Cos(n*2.4f)*.2f),new Vector3(.22f,.035f,.12f),leaf,false);
                        l.transform.localRotation=Quaternion.Euler(15+n*5,n*137,25);
                    }
                }
                if(i%11==0)
                {
                    Box("Windblown paper",parent,new Vector3(x+.3f,.008f,z+.1f),new Vector3(.14f,.012f,.2f),Material("LitterPaper",null,new Color(.6f,.55f,.43f)),false);
                    Cylinder("Discarded can",parent,new Vector3(x-.2f,.055f,z),.035f,.11f,metal);
                }
            }
        }
        public static GameObject Car(Transform parent,CarPlot p)
        {
            var bay=Box("Gravel parking bay",parent,p.Position(.075f),new Vector3(2.65f,.15f,5.15f),Material("ParkingGravel","yard",new Color(.87f,.84f,.77f)));
            bay.transform.localRotation=Quaternion.Euler(0,p.yaw,0);
            int variant=p.driveable?0:p.x< -40?1:p.x<0?2:p.z<0?3:0;
            int color=p.driveable?0:Mathf.Abs(Mathf.RoundToInt(p.x+p.z))%6;
            return WestbruchVehicleArt.Create(parent,variant,color,p.driveable,false,p.Position(.15f),p.yaw);
        }
        public static void CarInteractions(SimpleCar car)
        {
            if(car.seatAnchor)
            {
                car.steeringWheel.localPosition=new Vector3(-.37f,1.14f,.18f);car.ResetSteeringVisual();SteeringDetails(car);return;
            }
            var root=car.transform;var paint=root.Find("Hood").GetComponent<Renderer>().sharedMaterial;
            var trim=Material("CabinTrim",null,new Color(.045f,.052f,.048f),.24f);
            var upholstery=Material("SeatFabric","yard",new Color(.18f,.19f,.17f),.12f);
            var glass=Material("CabinTransparentGlass",null,new Color(.15f,.21f,.23f,.17f),.91f,.05f);
            glass.SetFloat("_SurfaceType",1);glass.SetFloat("_DoubleSidedEnable",1);UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(glass);EditorUtility.SetDirty(glass);
            foreach(var child in root.GetComponentsInChildren<MeshRenderer>())
                if(child.name=="Body"||child.name=="Side window"||child.name=="Window pillar"||child.name=="Mirror")UnityEngine.Object.DestroyImmediate(child.gameObject);
            Box("Cabin floor",root,new Vector3(0,.31f,-.05f),new Vector3(1.65f,.1f,2.15f),trim,false);
            Box("Rear body",root,new Vector3(0,.68f,-1.4f),new Vector3(1.72f,.53f,1),paint,false);
            Box("Front body",root,new Vector3(0,.68f,1.26f),new Vector3(1.72f,.53f,1.22f),paint,false);
            root.Find("Roof").localPosition=new Vector3(0,1.73f,-.3f);
            var windshield=root.Find("Sloped windshield");windshield.localPosition=new Vector3(0,1.35f,.56f);windshield.GetComponent<MeshRenderer>().sharedMaterial=glass;
            var rear=root.Find("Rear glass");rear.localPosition=new Vector3(0,1.37f,-1.08f);rear.GetComponent<MeshRenderer>().sharedMaterial=glass;
            car.doors=new Transform[2];
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;var door=Group(i==0?"Driver hinged door":"Passenger hinged door",root,new Vector3(side*.83f,0,.6f)).transform;car.doors[i]=door;
                Box("Door panel",door,new Vector3(0,.72f,-.62f),new Vector3(.09f,.58f,1.24f),paint,false);
                Box("Door interior",door,new Vector3(-side*.058f,.76f,-.62f),new Vector3(.025f,.43f,1.16f),trim,false);
                Box("Door glass",door,new Vector3(-side*.04f,1.32f,-.62f),new Vector3(.022f,.6f,1.16f),glass,false);
                Box("Front window frame",door,new Vector3(-side*.025f,1.31f,-.04f),new Vector3(.06f,.66f,.055f),paint,false);
                Box("Rear window frame",door,new Vector3(-side*.025f,1.31f,-1.2f),new Vector3(.065f,.66f,.065f),paint,false);
                Box("Window top frame",door,new Vector3(-side*.025f,1.64f,-.62f),new Vector3(.06f,.05f,1.24f),paint,false);
                Box("Door handle",door,new Vector3(side*.065f,.94f,-1),new Vector3(.025f,.038f,.16f),trim,false);
                Box("Mirror",door,new Vector3(side*.1f,1.09f,-.03f),new Vector3(.18f,.12f,.24f),paint,false);
                Box("Rear side glass",root,new Vector3(side*.75f,1.35f,-.88f),new Vector3(.025f,.54f,.40f),glass,false);
                Box("Rear pillar",root,new Vector3(side*.77f,1.34f,-1.12f),new Vector3(.075f,.67f,.09f),paint,false);
                Box("B pillar",root,new Vector3(side*.79f,1.33f,-.66f),new Vector3(.07f,.7f,.08f),trim,false);
                Box("Lower sill",root,new Vector3(side*.81f,.45f,-.12f),new Vector3(.09f,.13f,2.13f),paint,false);
                Box("Seat cushion",root,new Vector3(side*.37f,.68f,-.25f),new Vector3(.53f,.13f,.62f),upholstery,false);
                var seat=Box("Seat back",root,new Vector3(side*.37f,1.04f,-.59f),new Vector3(.51f,.65f,.1f),upholstery,false);seat.transform.localRotation=Quaternion.Euler(-7,0,0);
                Box("Headrest",root,new Vector3(side*.37f,1.43f,-.65f),new Vector3(.29f,.19f,.1f),upholstery,false);
            }
            Box("Dashboard",root,new Vector3(0,.99f,.54f),new Vector3(1.55f,.15f,.33f),trim,false);
            car.seatAnchor=Group("Driver animation seat",root,new Vector3(-.37f,.28f,-.13f)).transform;
            car.steeringWheel=Group("Steering wheel pivot",root,new Vector3(-.37f,1.14f,.18f)).transform;
            car.ResetSteeringVisual();
            SteeringDetails(car);
            foreach(var child in root.GetComponentsInChildren<Transform>()){child.gameObject.isStatic=false;child.gameObject.layer=0;}
        }
        static void SteeringDetails(SimpleCar car)
        {
            var trim=Material("CabinTrim",null,new Color(.045f,.052f,.048f),.24f);
            foreach(var part in car.steeringWheel.GetComponentsInChildren<MeshRenderer>())
                if(part.name=="Steering rim"||part.name=="Steering hub")UnityEngine.Object.DestroyImmediate(part.gameObject);
            if(!car.steeringWheel.Find("Continuous leather steering rim"))
            {
                const int around=48,tube=8;string path=Generated+"/Meshes/CompactSteeringRing.asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh)
                {
                    var vertices=new Vector3[(around+1)*(tube+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var triangles=new int[around*tube*6];int n=0;
                    for(int i=0;i<=around;i++)for(int j=0;j<=tube;j++)
                    {
                        float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));int v=i*(tube+1)+j;
                        normals[v]=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);vertices[v]=radial*.18f+normals[v]*.0135f;uv[v]=new Vector2(i/(float)around,j/(float)tube);
                        if(i==around||j==tube)continue;int next=v+tube+1;
                        triangles[n++]=v;triangles[n++]=v+1;triangles[n++]=next;triangles[n++]=v+1;triangles[n++]=next+1;triangles[n++]=next;
                    }
                    mesh=new Mesh{name="Cached continuous leather steering rim",vertices=vertices,normals=normals,uv=uv,triangles=triangles};mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
                }
                var ring=Group("Continuous leather steering rim",car.steeringWheel,Vector3.zero);ring.AddComponent<MeshFilter>().sharedMesh=mesh;ring.AddComponent<MeshRenderer>().sharedMaterial=trim;
                Cylinder("Padded steering hub",car.steeringWheel,Vector3.zero,.065f,.045f,trim);
            }
            if(!car.transform.Find("Steering column"))Beam("Steering column",car.transform,new Vector3(-.37f,.99f,.48f),new Vector3(-.37f,1.14f,.18f),.045f,trim);
            if(!car.steeringWheel.Find("Steering spoke 0"))for(int i=0;i<3;i++)
            {
                float angle=(30+i*120)*Mathf.Deg2Rad;Beam("Steering spoke "+i,car.steeringWheel,Vector3.zero,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.165f,.025f,trim);
            }
            foreach(var child in car.GetComponentsInChildren<Transform>())child.gameObject.isStatic=false;
        }

    }
}
