using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchVehicleArt
    {
        static Material Trim=>Material("VehicleRubberTrim",null,new Color(.035f,.041f,.039f),.23f);
        static Material Alloy=>Material("VehicleBrushedAlloy","steel",new Color(.64f,.67f,.66f),.52f,.75f);
        internal static Material Lamp(string id,Color color,float intensity)
        {
            var mat=Material("VehicleLamp"+id,null,color,.65f);
            mat.SetColor("_EmissiveColor",color*intensity);HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);return mat;
        }
        public static GameObject Create(Transform parent,int kind,int color,bool driveable,bool npc,Vector3 position,float yaw)
        {
            float length=kind==3?4.65f:kind==2?4.35f:kind==1?4.2f:3.8f;
            float width=kind==3?1.90f:1.72f,height=driveable?1.80f:kind==3?1.90f:kind==0?1.65f:1.59f;
            float frontAxle=driveable?1.23f:length*.315f,rearAxle=driveable?-1.22f:-length*.305f;
            var root=Group(driveable?"Driveable compact":npc?"NPC vehicle "+kind:"Parked vehicle "+kind,parent,position,yaw);
            Color[] colors={new Color(.18f,.25f,.18f),new Color(.29f,.10f,.075f),new Color(.16f,.22f,.27f),new Color(.57f,.55f,.47f),new Color(.25f,.28f,.29f),new Color(.40f,.33f,.19f)};
            var paint=Material("VehiclePaint"+color,null,colors[color%colors.Length],.43f,.35f);
            var glass=Material("VehicleClearGlass",null,new Color(.12f,.18f,.21f,.13f),.83f,0);
            glass.SetFloat("_SurfaceType",1);glass.SetFloat("_DoubleSidedEnable",0);HDMaterial.ValidateMaterial(glass);EditorUtility.SetDirty(glass);
            var cabin=Material("VehicleSeatFabric","yard",new Color(.12f,.14f,.14f),.07f);
            float roofFront=kind==3?.81f:.25f,roofBack=kind==2?-1.40f:kind==3?-1.98f:kind==1?-.90f:-1.12f;
            float screenFront=kind==3?1.15f:.72f,screenBack=kind==1?-1.43f:kind==3?-2.08f:kind==2?-1.92f:-1.50f;
            float roofWidth=width*.405f,beltWidth=width*.47f;
            var chassis=Group("Coachwork – shaped panels and wheel arches",root.transform,Vector3.zero).transform;
            MeshPart("Body lower shell",chassis,ProfileShell(kind,length,width,frontAxle,rearAxle),paint);
            Box("Cabin floor",root.transform,new Vector3(0,.37f,-.12f),new Vector3(width-.14f,.11f,2.14f),Trim,false);
            MeshPart("Closed engine shoulder and rear deck",root.transform,Deck(kind,length,width,screenFront,screenBack),paint);
            Interior(root.transform,kind,length,width,height,frontAxle,rearAxle,screenFront,screenBack,roofFront,roofBack,roofWidth);
            SoftBox("Hood",root.transform,new Vector3(0,1.035f,(screenFront+length*.5f-.06f)*.5f),new Vector3(width*.84f,.04f,length*.5f-screenFront-.10f),.015f,paint);
            SoftBox("Roof",root.transform,new Vector3(0,height,(roofFront+roofBack)*.5f),new Vector3(roofWidth*2,.085f,roofFront-roofBack+.12f),.025f,paint);
            Panel("Sloped windshield",root.transform,new Vector3(-beltWidth,1.04f,screenFront),new Vector3(-roofWidth,height-.045f,roofFront),new Vector3(roofWidth,height-.045f,roofFront),new Vector3(beltWidth,1.04f,screenFront),glass);
            Panel("Rear glass",root.transform,new Vector3(beltWidth,1.04f,screenBack),new Vector3(roofWidth,height-.045f,roofBack),new Vector3(-roofWidth,height-.045f,roofBack),new Vector3(-beltWidth,1.04f,screenBack),kind==3?paint:glass);
            if(kind==3)
            {
                Beam("Cargo rear door seam",root.transform,new Vector3(0,1.03f,screenBack-.025f),new Vector3(0,height-.06f,roofBack-.025f),.018f,Trim);
                foreach(int side in new[]{-1,1})Box("Van rear handle",root.transform,new Vector3(side*.14f,1.18f,screenBack-.04f),new Vector3(.04f,.14f,.035f),Trim,false);
            }
            if(kind==1)SoftBox("Separate luggage deck",root.transform,new Vector3(0,1.035f,(screenBack-length*.5f)*.5f),new Vector3(width*.84f,.04f,length*.5f-Mathf.Abs(screenBack)-.08f),.015f,paint);
            var doors=new Transform[2];
            for(int s=-1;s<=1;s+=2)
            {
                var door=Group(s<0?"Driver hinged door":"Passenger hinged door",root.transform,new Vector3(s*.83f,0,.60f)).transform;doors[s<0?0:1]=door;
                // Hinge and seating coordinates stay compatible with the player rig.
                SoftBox("Sculpted door panel",door,new Vector3(s*(width*.5f-.83f-.026f),.77f,-.62f),new Vector3(.08f,.49f,1.22f),.019f,paint);
                Box("Door inner card",door,new Vector3(-s*.064f,.78f,-.62f),new Vector3(.028f,.37f,1.14f),Trim,false);
                SoftBox("Interior door armrest",door,new Vector3(-s*.095f,.84f,-.64f),new Vector3(.07f,.055f,.32f),.018f,Trim);
                float sx=s*(beltWidth-.83f),rx=s*(roofWidth-.83f);
                Panel("Door glass",door,new Vector3(sx,1.04f,.09f),new Vector3(rx,height-.06f,roofFront-.60f),new Vector3(rx,height-.06f,-1.13f),new Vector3(sx,1.04f,-1.20f),glass);
                Beam("A pillar",door,new Vector3(sx,1.025f,.105f),new Vector3(rx,height-.015f,roofFront-.6f),.055f,paint);
                Beam("Door upper seal",door,new Vector3(rx,height-.02f,roofFront-.6f),new Vector3(rx,height-.02f,-1.15f),.038f,Trim);
                Beam("Door rear pillar",door,new Vector3(rx,height-.03f,-1.15f),new Vector3(sx,1.015f,-1.21f),.058f,Trim);
                Box("Recessed door handle",door,new Vector3(s*(width*.5f-.83f+.026f),.95f,-1.02f),new Vector3(.035f,.034f,.16f),Trim,false);
                SoftBox("Door rubbing strip",door,new Vector3(s*(width*.5f-.83f+.018f),.69f,-.62f),new Vector3(.028f,.045f,1.14f),.010f,Trim);
                if(kind==1||kind==2){Box("Rear door handle",root.transform,new Vector3(s*(width*.5f+.014f),.95f,-1.17f),new Vector3(.028f,.034f,.14f),Trim,false);Beam("Rear door shut line",root.transform,new Vector3(s*(width*.5f+.01f),.53f,-1.31f),new Vector3(s*(width*.5f+.01f),1.005f,-1.31f),.009f,Trim);}
                var mirror=SoftBox("Rounded mirror housing",door,new Vector3(s*(width*.5f-.83f+.10f),1.12f,-.03f),new Vector3(.18f,.11f,.23f),.032f,paint);mirror.transform.localRotation=Quaternion.Euler(0,s*9,0);
                Box("Mirror lens",mirror.transform,new Vector3(0,0,-.121f),new Vector3(.14f,.075f,.012f),Alloy,false);
                if(roofBack<-.69f)
                    Panel("Rear quarter window",root.transform,new Vector3(s*beltWidth,1.04f,-.66f),new Vector3(s*roofWidth,height-.05f,-.55f),new Vector3(s*roofWidth,height-.05f,roofBack),new Vector3(s*beltWidth,1.04f,screenBack),kind==3?paint:glass);
                Beam("Rear roof pillar",root.transform,new Vector3(s*beltWidth,1.02f,screenBack),new Vector3(s*roofWidth,height,roofBack),.085f,paint);
                Beam("Fixed windshield pillar",root.transform,new Vector3(s*beltWidth,1.025f,screenFront),new Vector3(s*roofWidth,height-.015f,roofFront),.055f,paint);
                if(kind==3)
                {
                    // The van's screen extends ahead of its door hinge. This
                    // fixed quarter pane closes the triangular cabin corner;
                    // the door keeps its original animation pivot and frame.
                    Panel("Van front quarter glazing",root.transform,new Vector3(s*beltWidth,1.04f,.705f),new Vector3(s*roofWidth,height-.045f,roofFront-.015f),new Vector3(s*roofWidth,height-.045f,roofFront),new Vector3(s*beltWidth,1.04f,screenFront),glass);
                    Beam("Quarter window lower seal",root.transform,new Vector3(s*beltWidth,1.032f,.705f),new Vector3(s*beltWidth,1.032f,screenFront),.030f,Trim);
                }
                Beam("Roof side rail",root.transform,new Vector3(s*roofWidth,height,roofBack),new Vector3(s*roofWidth,height,roofFront),.065f,paint);
                Beam("Door belt trim",root.transform,new Vector3(s*(width*.5f+.012f),1.013f,screenBack),new Vector3(s*(width*.5f+.012f),1.013f,screenFront),.035f,Trim);
                Box("Lower sill",root.transform,new Vector3(s*(width*.5f-.025f),.47f,-.12f),new Vector3(.08f,.12f,1.95f),Trim,false);
                for(int axle=0;axle<2;axle++)WheelArch(root.transform,s,axle==0?frontAxle:rearAxle,width,Trim);
                if(kind==2){Beam("Roof rail",root.transform,new Vector3(s*.55f,height+.095f,-1.25f),new Vector3(s*.55f,height+.095f,-.15f),.038f,Trim);foreach(float foot in new[]{-1.18f,-.22f})Beam("Roof rail mounting foot",root.transform,new Vector3(s*.55f,height+.025f,foot),new Vector3(s*.55f,height+.095f,foot),.028f,Trim);}
                if(kind==3)Box("Van cargo pressed inset",root.transform,new Vector3(s*(width*.5f+.008f),1.38f,-1.22f),new Vector3(.018f,.63f,1.20f),paint,false);
                var seat=Box("Seat back",root.transform,new Vector3(s*.37f,1.04f,-.59f),new Vector3(.50f,.65f,.11f),cabin,false);seat.transform.localRotation=Quaternion.Euler(-7,0,0);
                Box("Seat cushion",root.transform,new Vector3(s*.37f,.68f,-.25f),new Vector3(.52f,.13f,.61f),cabin,false);
                Box("Headrest",root.transform,new Vector3(s*.37f,1.43f,-.65f),new Vector3(.28f,.18f,.11f),cabin,false);
            }
            Box("Dashboard",root.transform,new Vector3(0,.99f,.54f),new Vector3(1.55f,.15f,.33f),Trim,false);
            SoftBox("Center console",root.transform,new Vector3(0,.67f,.01f),new Vector3(.22f,.24f,.65f),.035f,Trim);
            Beam("Gear lever",root.transform,new Vector3(0,.78f,.18f),new Vector3(0,.92f,.15f),.025f,Trim);
            for(int vent=0;vent<3;vent++)Box("Dash ventilation grille",root.transform,new Vector3(-.54f+vent*.54f,1.071f,.48f),new Vector3(.22f,.012f,.075f),Alloy,false);
            if(kind!=3)
            {
                SoftBox("Rear bench cushion",root.transform,new Vector3(0,.63f,-1.0f),new Vector3(1.35f,.15f,.48f),.045f,cabin);
                SoftBox("Rear bench back",root.transform,new Vector3(0,.95f,-1.28f),new Vector3(1.34f,.55f,.13f),.045f,cabin);
                foreach(int side in new[]{-1,1})SoftBox("Rear head restraint",root.transform,new Vector3(side*.42f,1.29f,-1.28f),new Vector3(.26f,.19f,.12f),.04f,cabin);
            }
            else{Box("Cargo load floor",root.transform,new Vector3(0,.56f,-1.3f),new Vector3(1.65f,.08f,1.5f),Trim,false);Box("Cabin cargo bulkhead",root.transform,new Vector3(0,1.13f,-.87f),new Vector3(1.65f,1.06f,.06f),paint,false);}
            var white=Lamp("Head",new Color(.67f,.70f,.64f),.65f);var tail=Lamp("Tail",new Color(.40f,.018f,.009f),.5f);
            var brake=Lamp("Brake",new Color(.70f,.025f,.012f),2.2f);var amber=Lamp("Amber",new Color(.75f,.30f,.025f),2.5f);
            var brakeRenderers=new List<Renderer>();var indicators=new List<Renderer>();
            foreach(int s in new[]{-1,1})
            {
                float z=s*length*.5f;
                SoftBox("Contoured bumper",root.transform,new Vector3(0,.57f,z),new Vector3(width*.98f,.17f,.17f),.048f,Trim);
                Box("Unbranded registration plate",root.transform,new Vector3(0,.59f,z+s*.091f),new Vector3(.43f,.105f,.015f),Material("VehiclePlate",null,new Color(.68f,.69f,.61f),.23f),false);
                foreach(int side in new[]{-1,1})
                {
                    SoftBox("Inset lamp surround",root.transform,new Vector3(side*width*.34f,.88f,z+s*.052f),new Vector3(kind==0?.43f:.50f,.19f,.05f),.018f,Trim);
                    var lamp=Box(s>0?"Headlamp with inset reflector":"Rear lamp cluster",root.transform,new Vector3(side*width*.34f,.88f,z+s*.065f),new Vector3(kind==0?.39f:.46f,.15f,.045f),s>0?white:tail,false);
                    if(s>0)for(int rib=0;rib<3;rib++)Box("Headlamp lens rib",root.transform,new Vector3(side*width*.34f+(rib-1)*.105f,.88f,z+.090f),new Vector3(.008f,.13f,.005f),Alloy,false);
                    if(s<0)brakeRenderers.Add(lamp.GetComponent<Renderer>());
                    if(s<0)Box("Rear reversing lens",root.transform,new Vector3(side*width*.27f,.86f,z+s*.093f),new Vector3(.065f,.095f,.012f),Material("VehicleReverseLens",null,new Color(.54f,.56f,.48f),.45f),false);
                    var signal=Box("Amber corner indicator",root.transform,new Vector3(side*width*.438f,.85f,z+s*.068f),new Vector3(.08f,.10f,.047f),Trim,false);indicators.Add(signal.GetComponent<Renderer>());
                }
            }
            for(int i=0;i<4;i++)Box("Front grille slat",root.transform,new Vector3(0,.83f+i*.031f,length*.5f+.069f),new Vector3(.52f,.014f,.025f),Trim,false);
            Box("Lower air intake",root.transform,new Vector3(0,.44f,length*.5f+.03f),new Vector3(.72f,.10f,.05f),Trim,false);
            Beam("Left wiper",root.transform,new Vector3(-.64f,1.045f,screenFront+.025f),new Vector3(-.12f,1.09f,screenFront-.025f),.014f,Trim);
            Beam("Right wiper",root.transform,new Vector3(.10f,1.045f,screenFront+.025f),new Vector3(.58f,1.09f,screenFront-.025f),.014f,Trim);
            var exhaust=Cylinder("Exhaust outlet",root.transform,new Vector3(-.51f,.39f,-length*.5f-.035f),.043f,.14f,Alloy);exhaust.transform.localRotation=Quaternion.Euler(90,0,0);
            // Keep a disabled clearance envelope for conservative traffic QA.
            // Physical contact follows the low body and tapered cabin instead
            // of an invisible full-height box above the bonnet and boot.
            var collision=root.AddComponent<BoxCollider>();collision.center=new Vector3(0,driveable?.85f:(height+.38f)*.5f,-.02f);collision.size=driveable?new Vector3(1.75f,1.3f,3.9f):new Vector3(width,height-.38f,length+.12f);collision.enabled=false;
            ContactHull("Lower rounded body collision",root.transform,BodyContactMesh(length,width));
            ContactHull("Sloped cabin and roof collision",root.transform,CabinContactMesh(width,height,screenFront,screenBack,roofFront,roofBack));
            SimpleCar car=null;
            if(driveable)
            {
                var body=root.AddComponent<Rigidbody>();body.mass=1120;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                car=root.AddComponent<SimpleCar>();car.doors=doors;car.wheels=new WheelCollider[4];car.wheelVisuals=new Transform[4];
                var navObstacle=root.AddComponent<UnityEngine.AI.NavMeshObstacle>();navObstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;navObstacle.center=Vector3.up*.9f;navObstacle.size=new Vector3(1.85f,1.7f,4.1f);navObstacle.carving=false;
                car.seatAnchor=Group("Driver animation seat",root.transform,new Vector3(-.37f,.28f,-.13f)).transform;
                car.steeringWheel=Group("Steering wheel pivot",root.transform,new Vector3(-.37f,1.14f,.18f)).transform;
                WestbruchProps.CarInteractions(car);
            }
            var steering=new List<Transform>();var rolling=new List<Transform>();int index=0;
            foreach(float z in new[]{frontAxle,rearAxle})foreach(float side in new[]{-1f,1f})
            {
                float x=side*(driveable?.86f:width*.5f-.025f);
                var swivel=Group("Wheel steering",root.transform,new Vector3(x,.34f,z)).transform;
                var wheel=Group("Wheel rotating pivot",swivel,Vector3.zero).transform;steering.Add(swivel);rolling.Add(wheel);
                MeshPart("Rounded tyre with sidewall",wheel,Tyre(),Trim);
                var rim=Group("Recessed alloy wheel",wheel,new Vector3(side*.103f,0,0)).transform;
                MeshPart("Open alloy rim lip",rim,RimRing(.245f,.019f),Alloy);
                CylinderPart("Deep wheel hub",rim,Vector3.zero,.067f,.023f,Alloy);
                CylinderPart("Recessed brake disc",rim,new Vector3(-side*.030f,0,0),.202f,.012f,Material("VehicleBrakeDisc",null,new Color(.29f,.31f,.30f),.27f,.68f));
                for(int bolt=0;bolt<5;bolt++){float a=bolt*Mathf.PI*2/5;CylinderPart("Wheel lug bolt",rim,new Vector3(side*.018f,Mathf.Cos(a)*.042f,Mathf.Sin(a)*.042f),.009f,.013f,Alloy);}
                for(int spoke=0;spoke<(kind==3?8:5);spoke++)
                {
                    float a=spoke*Mathf.PI*2/(kind==3?8:5);var v=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));
                    Beam("Shaped alloy spoke",rim,new Vector3(-side*.018f,0,0)+v*.055f,new Vector3(0,0,0)+v*.226f,kind==3?.032f:.040f,Alloy);
                }
                if(car)
                {
                    var physics=Group("Wheel physics",root.transform,new Vector3(x,.34f,z)).AddComponent<WheelCollider>();physics.radius=.34f;physics.mass=20;physics.suspensionDistance=.18f;
                    var spring=physics.suspensionSpring;spring.spring=30000;spring.damper=4200;spring.targetPosition=.5f;physics.suspensionSpring=spring;
                    car.wheels[index]=physics;car.wheelVisuals[index]=swivel;
                }
                index++;
            }
            if(npc)
            {
                var agent=root.AddComponent<TrafficVehicle>();agent.bodyCollider=collision;agent.length=length+.12f;agent.width=width;agent.kind=kind;
                agent.steering=steering.ToArray();agent.rolling=rolling.ToArray();agent.rearLamps=brakeRenderers.ToArray();agent.indicators=indicators.ToArray();agent.tailMaterial=tail;agent.brakeMaterial=brake;agent.amberMaterial=amber;agent.offMaterial=Trim;
                var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                Driver(root.transform,color,agent);
            }
            var moving=new List<Transform>();
            if(car){moving.AddRange(car.doors);moving.AddRange(car.wheelVisuals);moving.Add(car.steeringWheel);}
            if(npc)moving.AddRange(rolling);
            if(npc)moving.Add(root.GetComponentInChildren<VehicleDriver>().wheel);
            BakeParts(root.transform,moving,npc?root.GetComponent<TrafficVehicle>():null,kind+"_"+color+"_"+driveable+"_"+npc);
            foreach(var child in root.GetComponentsInChildren<Transform>()){child.gameObject.isStatic=!driveable&&!npc;if(npc)child.gameObject.layer=8;}
            return root;
        }
        // Re-author only the vehicles after art/traffic changes. Terrain, flora,
        // buildings, their placement and their render chunks remain untouched.
        public static void RebuildAndCapture()
        {RebuildVehicles();WestbruchTrafficQA.Run();}
        public static void RebuildVehicles()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();
            var plan=JsonUtility.FromJson<WestbruchLayout>(System.IO.File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var root=GameObject.Find("Westbruch – authored district").transform;
            var traffic=Object.FindAnyObjectByType<WestbruchTraffic>();if(traffic)Object.DestroyImmediate(traffic.gameObject);
            foreach(var item in root.GetComponentsInChildren<MeshRenderer>())
                if(item.enabled&&item.transform.parent&&item.transform.parent.name=="Static render chunks – sources retained for editing"&&item.sharedMaterial.name.StartsWith("Vehicle"))Object.DestroyImmediate(item.gameObject);
            var remove=new List<GameObject>();foreach(Transform item in root)
                if(item.name=="Driveable compact"||item.name.StartsWith("Parked vehicle "))remove.Add(item.gameObject);
            foreach(var item in remove)Object.DestroyImmediate(item);
            var player=Object.FindAnyObjectByType<ThirdPersonMotor>();
            foreach(var p in plan.cars)
            {
                int kind=p.driveable?0:p.x< -40?1:p.x<0?2:p.z<0?3:0,color=p.driveable?0:Mathf.Abs(Mathf.RoundToInt(p.x+p.z))%6;
                var built=Create(root,kind,color,p.driveable,false,p.Position(.15f),p.yaw);if(p.driveable)player.nearbyCar=built.GetComponent<SimpleCar>();
            }
            Object.FindAnyObjectByType<VegetationMotion>().car=player.nearbyCar;player.orbit.obstacles=1|(1<<8);
            WestbruchTrafficBuilder.Build(root,plan);Physics.SyncTransforms();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),WestbruchBuilder.ScenePath);AssetDatabase.SaveAssets();
        }
        static void BakeParts(Transform root,List<Transform> moving,TrafficVehicle agent,string variant)
        {
            var islands=new List<Transform>{root};islands.AddRange(moving);
            var batches=new Dictionary<string,List<MeshFilter>>();var roots=new Dictionary<string,Transform>();
            foreach(var part in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=part.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;
                if(agent&&(System.Array.IndexOf(agent.rearLamps,renderer)>=0||System.Array.IndexOf(agent.indicators,renderer)>=0))continue;
                int island=0;for(int i=1;i<islands.Count;i++)if(part.transform.IsChildOf(islands[i])){island=i;break;}
                string key=island+"_"+renderer.sharedMaterial.name;
                if(!batches.TryGetValue(key,out var list)){list=new List<MeshFilter>();batches[key]=list;roots[key]=islands[island];}list.Add(part);
            }
            foreach(var pair in batches)
            {
                var origin=roots[pair.Key];var combine=new CombineInstance[pair.Value.Count];
                for(int i=0;i<combine.Length;i++)combine[i]=new CombineInstance{mesh=pair.Value[i].sharedMesh,transform=origin.worldToLocalMatrix*pair.Value[i].transform.localToWorldMatrix};
                string path=Generated+"/Meshes/VehicleCombined_"+variant+"_"+pair.Key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(combine,true,true);EditorUtility.SetDirty(mesh);
                MeshPart("Baked vehicle panels "+pair.Key,origin,mesh,pair.Value[0].GetComponent<Renderer>().sharedMaterial);
                foreach(var part in pair.Value)part.GetComponent<Renderer>().enabled=false;
            }
        }
        static void CylinderPart(string name,Transform parent,Vector3 p,float radius,float depth,Material material)
        {var o=Cylinder(name,parent,p,radius,depth,material);o.transform.localRotation=Quaternion.Euler(0,0,90);}
        static void ContactHull(string name,Transform parent,Mesh mesh)
        {var shape=Group(name,parent,Vector3.zero).AddComponent<MeshCollider>();shape.sharedMesh=mesh;shape.convex=true;}
        static Mesh BodyContactMesh(float length,float width)=>Cached("ContactBodyV2_"+length+"_"+width,()=>
        {
            float x=width*.5f+.012f,z=length*.5f+.075f;
            var outline=new[]{new Vector2(-x+.12f,-z),new Vector2(x-.12f,-z),new Vector2(x,-z+.27f),new Vector2(x,z-.27f),new Vector2(x-.12f,z),new Vector2(-x+.12f,z),new Vector2(-x,z-.27f),new Vector2(-x,-z+.27f)};
            var lower=new Vector3[8];var upper=new Vector3[8];for(int i=0;i<8;i++){lower[i]=new Vector3(outline[i].x,.43f,outline[i].y);upper[i]=new Vector3(outline[i].x,1.035f,outline[i].y);}return SolidHull(lower,upper);
        });
        static Mesh CabinContactMesh(float width,float height,float front,float rear,float roofFront,float roofRear)=>Cached("ContactCabinV2_"+width+"_"+height+"_"+front+"_"+rear,()=>
            SolidHull(new[]{new Vector3(-width*.47f,1.015f,rear),new Vector3(width*.47f,1.015f,rear),new Vector3(width*.47f,1.015f,front),new Vector3(-width*.47f,1.015f,front)},new[]{new Vector3(-width*.405f,height+.04f,roofRear),new Vector3(width*.405f,height+.04f,roofRear),new Vector3(width*.405f,height+.04f,roofFront),new Vector3(-width*.405f,height+.04f,roofFront)}));
        static Mesh SolidHull(Vector3[] lower,Vector3[] upper)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();Vector3 center=Vector3.zero;foreach(var p in lower)center+=p;foreach(var p in upper)center+=p;center/=lower.Length*2;
            void Face(params Vector3[] source){var points=(Vector3[])source.Clone();if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),points[0]-center)<0)System.Array.Reverse(points);int n=vertices.Count;vertices.AddRange(points);for(int i=1;i<points.Length-1;i++)triangles.AddRange(new[]{n,n+i,n+i+1});}
            Face(lower);Face(upper);for(int i=0;i<lower.Length;i++){int next=(i+1)%lower.Length;Face(lower[i],lower[next],upper[next],upper[i]);}
            var result=new Mesh{vertices=vertices.ToArray(),triangles=triangles.ToArray(),uv=new Vector2[vertices.Count]};result.RecalculateNormals();return result;
        }
        static void Interior(Transform root,int kind,float length,float width,float height,float front,float rear,float screenFront,float screenBack,float roofFront,float roofBack,float roofWidth)
        {
            var lining=Material("VehicleInteriorLining",null,new Color(.105f,.112f,.103f),.16f);
            MeshPart("Inset inner metal shell",root,Cached("InnerShellV1_"+kind+"_"+front+"_"+rear,()=>
            {
                var mesh=Object.Instantiate(ProfileShell(kind,length,width,front,rear));var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){var p=vertices[i];p.x-=Mathf.Sign(p.x)*.028f;p.z*=1-.028f/(length*.5f);vertices[i]=p;}
                var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();return mesh;
            }),lining);
            MeshPart("Continuous formed floor pan",root,Cached("FloorPanV1_"+kind,()=>
            {
                var v=new List<Vector3>();var t=new List<int>();float half=length*.5f;
                void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal){int n=v.Count;v.AddRange(new[]{a,b,c,d});if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0)t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});else t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
                float W(float z)=>width*.5f-.028f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half-.36f,half,Mathf.Abs(z)))*.095f;
                for(int i=0;i<56;i++)
                {
                    float a=Mathf.Lerp(-half+.025f,half-.025f,i/56f),b=Mathf.Lerp(-half+.025f,half-.025f,(i+1)/56f);
                    Quad(new Vector3(-W(a),.47f,a),new Vector3(-W(b),.47f,b),new Vector3(W(b),.47f,b),new Vector3(W(a),.47f,a),Vector3.up);
                    Quad(new Vector3(-W(a),.42f,a),new Vector3(-W(b),.42f,b),new Vector3(W(b),.42f,b),new Vector3(W(a),.42f,a),Vector3.down);
                    foreach(int side in new[]{-1,1})Quad(new Vector3(side*W(a),.42f,a),new Vector3(side*W(a),.47f,a),new Vector3(side*W(b),.47f,b),new Vector3(side*W(b),.42f,b),Vector3.right*side);
                }
                foreach(int end in new[]{-1,1}){float z=end*(half-.025f);Quad(new Vector3(-W(z),.42f,z),new Vector3(-W(z),.47f,z),new Vector3(W(z),.47f,z),new Vector3(W(z),.42f,z),Vector3.forward*end);}
                var mesh=new Mesh{vertices=v.ToArray(),triangles=t.ToArray(),uv=new Vector2[v.Count]};mesh.RecalculateNormals();return mesh;
            }),lining);
            MeshPart("Engine and luggage deck inner skin",root,Cached("InnerDeckV1_"+kind,()=>
            {
                var mesh=Object.Instantiate(Deck(kind,length,width,screenFront,screenBack));var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){vertices[i].y-=.014f;vertices[i].x-=Mathf.Sign(vertices[i].x)*.02f;}var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}mesh.vertices=vertices;mesh.triangles=t;mesh.RecalculateNormals();return mesh;
            }),lining);
            SoftBox("Cabin headlining",root,new Vector3(0,height-.048f,(roofFront+roofBack)*.5f),new Vector3(roofWidth*2-.055f,.012f,roofFront-roofBack+.025f),.003f,lining);
            Box("Full footwell firewall",root,new Vector3(0,.74f,screenFront-.035f),new Vector3(width-.18f,.55f,.07f),lining,false);
            if(kind!=3)SoftBox("Rear parcel shelf",root,new Vector3(0,.995f,(screenBack-1.28f)*.5f),new Vector3(width-.18f,.045f,Mathf.Max(.13f,-1.28f-screenBack)),.015f,lining);
            foreach(int side in new[]{-1,1})foreach(float axle in new[]{front,rear})
                MeshPart("Closed inner wheel tub",root,Cached("WheelTubV1_"+width+"_"+side+"_"+axle,()=>
                {
                    const int count=24;var v=new List<Vector3>();var t=new List<int>();float inner=side*(width*.5f-.22f),outer=side*(width*.5f-.026f);
                    void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 normal){if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=c;c=swap;}int n=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
                    for(int i=0;i<count;i++)
                    {
                        float a=i*Mathf.PI/count,b=(i+1)*Mathf.PI/count;var p=new Vector3(inner,.34f+Mathf.Sin(a)*.425f,axle+Mathf.Cos(a)*.425f);var q=new Vector3(inner,.34f+Mathf.Sin(b)*.425f,axle+Mathf.Cos(b)*.425f);
                        Tri(new Vector3(inner,.34f,axle),p,q,Vector3.left*side);var po=new Vector3(outer,p.y,p.z);var qo=new Vector3(outer,q.y,q.z);var normal=new Vector3(0,Mathf.Sin((a+b)*.5f),Mathf.Cos((a+b)*.5f));Tri(p,po,qo,normal);Tri(p,qo,q,normal);
                    }
                    var mesh=new Mesh{vertices=v.ToArray(),triangles=t.ToArray(),uv=new Vector2[v.Count]};mesh.RecalculateNormals();return mesh;
                }),lining);
        }
        static void Driver(Transform parent,int variation,TrafficVehicle vehicle)
        {
            var root=Group("Visible seated NPC driver",parent,new Vector3(-.37f,.28f,-.13f)).transform;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAvatarBuilder.Folder+"/StreetwearHero.prefab");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);visual.transform.localScale=Vector3.one*.84f;var avatar=visual.GetComponent<PlayerAvatar>();
            avatar.skin.sharedMesh=WithoutBackpack(avatar.skin.sharedMesh,avatar.skin.sharedMaterials);
            var materials=avatar.skin.sharedMaterials;for(int i=0;i<materials.Length;i++)
            {
                if(!materials[i].name.Contains("cloth")&&!materials[i].name.Contains("skin")&&!materials[i].name.Contains("hair"))continue;
                string path=Generated+"/Materials/DriverRefined_"+variation+"_"+i+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(materials[i]);AssetDatabase.CreateAsset(material,path);}
                Color tint=materials[i].name.Contains("skin")?new[]{new Color(.68f,.49f,.36f),new Color(.42f,.29f,.21f),new Color(.74f,.58f,.44f)}[variation%3]:materials[i].name.Contains("hair")?new[]{new Color(.14f,.10f,.07f),new Color(.27f,.24f,.19f),new Color(.12f,.12f,.10f)}[variation%3]:new[]{new Color(.20f,.25f,.28f),new Color(.32f,.24f,.17f),new Color(.24f,.28f,.18f)}[variation%3];material.SetColor("_BaseColor",tint);HDMaterial.ValidateMaterial(material);EditorUtility.SetDirty(material);materials[i]=material;
            }
            avatar.skin.sharedMaterials=materials;var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(PlayerAvatarBuilder.Folder+"/Drive.anim");clip.SampleAnimation(visual,0);
            var wheel=Group("NPC steering pivot",parent,new Vector3(-.37f,1.14f,.18f)).transform;wheel.localRotation=Quaternion.Euler(55,0,0);
            MeshPart("NPC steering rim",wheel,RimRing(.17f,.012f),Trim).transform.localRotation=Quaternion.Euler(0,0,90);
            for(int i=0;i<3;i++){float a=(30+i*120)*Mathf.Deg2Rad;Beam("NPC steering spoke",wheel,Vector3.zero,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.16f,.02f,Trim);}
            var driver=root.gameObject.AddComponent<VehicleDriver>();driver.avatar=avatar;driver.wheel=wheel;driver.vehicle=vehicle;
        }
        internal static Mesh WithoutBackpack(Mesh source,Material[] materials)
        {
            string path=Generated+"/Meshes/ResidentStreetwearWithoutBackpack.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
            var mesh=Object.Instantiate(source);mesh.name="Resident streetwear without backpack";var weights=source.boneWeights;
            bool Backpack(int vertex)=>weights[vertex].boneIndex0==5&&weights[vertex].weight0>.45f||weights[vertex].boneIndex1==5&&weights[vertex].weight1>.45f;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var output=new List<int>();var triangles=source.GetTriangles(sub);
                if(!materials[sub].name.ToLowerInvariant().Contains("pack"))for(int i=0;i<triangles.Length;i+=3)if(!Backpack(triangles[i])&&!Backpack(triangles[i+1])&&!Backpack(triangles[i+2]))output.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                mesh.SetTriangles(output,sub);
            }
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static GameObject MeshPart(string name,Transform parent,Mesh mesh,Material material)
        {var o=Group(name,parent,Vector3.zero);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;return o;}
        static GameObject SoftBox(string name,Transform parent,Vector3 position,Vector3 size,float bevel,Material material)
        {
            var mesh=Cached("Bevel_"+size+"_"+bevel,()=>
            {
                var v=new List<Vector3>();var triangles=new List<int>();var h=size*.5f;float b=bevel;
                void Face(Vector3[] p,Vector3 normal)
                {
                    if(Vector3.Dot(Vector3.Cross(p[1]-p[0],p[2]-p[0]),normal)<0)System.Array.Reverse(p);
                    int n=v.Count;v.AddRange(p);for(int i=1;i<p.Length-1;i++)triangles.AddRange(new[]{n,n+i,n+i+1});
                }
                for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1})
                {
                    int u=(axis+1)%3,w=(axis+2)%3;var p=new Vector3[4];
                    for(int i=0;i<4;i++){var at=Vector3.zero;at[axis]=sign*h[axis];at[u]=(i==0||i==3?-1:1)*(h[u]-b);at[w]=(i<2?-1:1)*(h[w]-b);p[i]=at;}
                    var normal=Vector3.zero;normal[axis]=sign;Face(p,normal);
                }
                for(int a=0;a<3;a++)for(int c=a+1;c<3;c++)foreach(int sa in new[]{-1,1})foreach(int sc in new[]{-1,1})
                {
                    int along=3-a-c;var p=new Vector3[4];
                    for(int i=0;i<4;i++){var at=Vector3.zero;bool first=i<2;at[a]=sa*(h[a]-(first?0:b));at[c]=sc*(h[c]-(first?b:0));at[along]=(i==0||i==3?-1:1)*(h[along]-b);p[i]=at;}
                    var normal=Vector3.zero;normal[a]=sa;normal[c]=sc;Face(p,normal);
                }
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var s=new Vector3(x,y,z);Face(new[]{Vector3.Scale(s,new Vector3(h.x,h.y-b,h.z-b)),Vector3.Scale(s,new Vector3(h.x-b,h.y,h.z-b)),Vector3.Scale(s,new Vector3(h.x-b,h.y-b,h.z))},s);
                }
                var result=new Mesh{vertices=v.ToArray(),triangles=triangles.ToArray(),uv=new Vector2[v.Count]};result.RecalculateNormals();return result;
            });
            var obj=MeshPart(name,parent,mesh,material);obj.transform.localPosition=position;return obj;
        }
        static void Panel(string name,Transform parent,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material mat)
        {
            string key="PanelV2_"+name+"_"+a+"_"+b+"_"+c+"_"+d;
            MeshPart(name,parent,Cached(key,()=>{var m=new Mesh{vertices=new[]{a,b,c,d,a,b,c,d},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right,Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3,6,5,4,7,6,4}};m.RecalculateNormals();return m;}),mat);
        }
        static Mesh Cached(string key,System.Func<Mesh> create)
        {
            // Numeric vertex coordinates form a stable asset key without names
            // containing punctuation unsupported by Windows paths.
            string path=Generated+"/Meshes/Vehicle_"+Hash128.Compute(key)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            mesh=create();mesh.name="Vehicle "+key.Split('_')[0];mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh ProfileShell(int kind,float length,float width,float front,float rear)
        {
            return Cached("BodyV6_"+kind+"_"+front+"_"+rear,()=>
            {
                var v=new List<Vector3>();var t=new List<int>();
                void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
                {
                    int n=v.Count;v.AddRange(new[]{a,b,c,d});
                    if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)>0)t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                    else t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                }
                float half=length*.5f;
                // Cut at the actual moving door edges. Skipping whole regular
                // segments previously left several-centimetre gaps at the jambs.
                const float doorBack=-.638f,doorFront=.598f;
                var sections=new List<float>{doorBack,doorFront};for(int i=0;i<=56;i++)sections.Add(-half+length*i/56);sections.Sort();
                for(int i=0;i<sections.Count-1;i++)
                {
                    float z0=sections[i],z1=sections[i+1];
                    if((z0+z1)*.5f>doorBack&&(z0+z1)*.5f<doorFront)continue;
                    float W(float z)=>width*.5f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half-.36f,half,Mathf.Abs(z)))*.095f;
                    float Bottom(float z){float dz=Mathf.Min(Mathf.Abs(z-front),Mathf.Abs(z-rear));return dz<.395f?Mathf.Max(.43f,.34f+Mathf.Sqrt(.395f*.395f-dz*dz)):.43f;}
                    foreach(int side in new[]{-1,1})
                    {
                        var a=new Vector3(side*W(z0),Bottom(z0),z0);var b=new Vector3(side*W(z0),.91f,z0);var c=new Vector3(side*W(z1),.91f,z1);var d=new Vector3(side*W(z1),Bottom(z1),z1);
                        Quad(a,b,c,d,Vector3.right*side);
                        // Both shoulders face out and up. Mirroring positions
                        // without mirroring winding made the entire right upper
                        // fender disappear under normal backface culling.
                        Quad(new Vector3(side*W(z0),.91f,z0),new Vector3(side*(W(z0)-.055f),1.015f,z0),new Vector3(side*(W(z1)-.055f),1.015f,z1),new Vector3(side*W(z1),.91f,z1),new Vector3(side,1,0));
                    }
                }
                foreach(float jamb in new[]{doorBack,doorFront})foreach(int side in new[]{-1,1})
                {
                    // Recessed metal returns behind the narrow door shut line
                    // close the opening without joining the animated door mesh.
                    Quad(new Vector3(side*width*.5f,.53f,jamb),new Vector3(side*width*.5f,.91f,jamb),new Vector3(side*(width*.5f-.10f),1.015f,jamb),new Vector3(side*(width*.5f-.10f),.53f,jamb),Vector3.forward*(jamb<0?1:-1));
                }
                foreach(int side in new[]{-1,1})
                {
                    float outer=width*.5f-.095f,inner=outer-.055f;int start=v.Count;
                    v.AddRange(new[]{new Vector3(-outer,.43f,side*half),new Vector3(-outer,.91f,side*half),new Vector3(-inner,1.015f,side*half),new Vector3(inner,1.015f,side*half),new Vector3(outer,.91f,side*half),new Vector3(outer,.43f,side*half)});
                    for(int i=1;i<5;i++)if(side>0)t.AddRange(new[]{start,start+i+1,start+i});else t.AddRange(new[]{start,start+i,start+i+1});
                }
                var mesh=new Mesh{vertices=v.ToArray(),triangles=t.ToArray()};mesh.uv=new Vector2[v.Count];mesh.RecalculateNormals();return mesh;
            });
        }
        static Mesh Deck(int kind,float length,float width,float front,float rear)
        {
            return Cached("ClosedDeckV1_"+kind,()=>
            {
                var v=new List<Vector3>();var indices=new List<int>();float half=length*.5f;
                foreach(var span in new[]{new Vector2(front,half),new Vector2(-half,rear)})for(int i=0;i<12;i++)
                {
                    float z0=Mathf.Lerp(span.x,span.y,i/12f),z1=Mathf.Lerp(span.x,span.y,(i+1)/12f);
                    float W(float z)=>width*.5f-.055f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half-.36f,half,Mathf.Abs(z)))*.095f;
                    int start=v.Count;v.AddRange(new[]{new Vector3(-W(z0),1.015f,z0),new Vector3(-W(z1),1.015f,z1),new Vector3(W(z1),1.015f,z1),new Vector3(W(z0),1.015f,z0)});indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                }
                var mesh=new Mesh{vertices=v.ToArray(),triangles=indices.ToArray()};mesh.uv=new Vector2[v.Count];mesh.RecalculateNormals();return mesh;
            });
        }
        static void WheelArch(Transform root,int side,float z,float width,Material material)
        {for(int i=0;i<14;i++){float a=i*Mathf.PI/14,b=(i+1)*Mathf.PI/14;Beam("Wheel arch lip",root,new Vector3(side*(width*.5f+.013f),.34f+Mathf.Sin(a)*.402f,z+Mathf.Cos(a)*.402f),new Vector3(side*(width*.5f+.013f),.34f+Mathf.Sin(b)*.402f,z+Mathf.Cos(b)*.402f),.026f,material);}}
        static Mesh Tyre()=>Cached("TyreV1",()=>RingMesh(.282f,.058f,.115f));
        static Mesh RimRing(float radius,float tube)=>Cached("Ring_"+radius+"_"+tube,()=>RingMesh(radius,tube,tube));
        static Mesh RingMesh(float radius,float radial,float axial)
        {
            const int around=36,tube=10;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int i=0;i<=around;i++)for(int j=0;j<=tube;j++)
            {
                float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube;float r=radius+Mathf.Cos(b)*radial;
                v.Add(new Vector3(Mathf.Sin(b)*axial,Mathf.Cos(a)*r,Mathf.Sin(a)*r));uv.Add(new Vector2(i/(float)around,j/(float)tube));
                if(i<around&&j<tube){int k=i*(tube+1)+j;t.AddRange(new[]{k,k+tube+1,k+1,k+1,k+tube+1,k+tube+2});}
            }
            var mesh=new Mesh{vertices=v.ToArray(),uv=uv.ToArray(),triangles=t.ToArray()};mesh.RecalculateNormals();return mesh;
        }
    }
}
