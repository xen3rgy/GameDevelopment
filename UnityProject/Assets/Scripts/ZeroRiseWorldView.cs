using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    public sealed class BottlePickup : MonoBehaviour
    {
        public int id;
        private float baseY;
        public void Init(int bottleId){id=bottleId;baseY=transform.position.y;}
        private void Update()
        {
            transform.Rotate(0,25*Time.deltaTime,0);
            var p=transform.position;p.y=baseY+Mathf.Sin(Time.time*2+id)*.035f;transform.position=p;
        }
    }
    public sealed class RoamingResident : MonoBehaviour
    {
        private Vector3 home, target;
        private CitizenVisual visual;
        private float rest, speed;
        private System.Random random;
        public void Init(int id)
        {
            home=transform.position;target=home;random=new System.Random(id*977+2);
            speed=.6f+(float)random.NextDouble()*.5f;
            rest=(float)random.NextDouble()*5;
            visual=GetComponent<CitizenVisual>();
        }
        private void Update()
        {
            if(rest>0){rest-=Time.deltaTime;visual.Animate(Time.deltaTime,0,false);return;}
            Vector3 delta=target-transform.position;delta.y=0;
            if(delta.sqrMagnitude<.5f)
            {
                rest=2f+(float)random.NextDouble()*4f;
                target=home+new Vector3((float)(random.NextDouble()-.5)*8,0,(float)(random.NextDouble()-.5)*8);
                return;
            }
            Vector3 next=delta.normalized * (speed*Time.deltaTime);
            // Simple cast avoids walking into nearby walls; no expensive city pathfinding in this prototype.
            if(Physics.Raycast(transform.position+Vector3.up*.6f,next.normalized,out var hit,1.0f) && !hit.collider.transform.IsChildOf(transform))
            {target=home;rest=2;visual.Animate(Time.deltaTime,0,false);return;}
            transform.position+=next;
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(next),Time.deltaTime*5);
            visual.Animate(Time.deltaTime,1,false);
        }
    }

    public sealed class ZeroRiseWorldView : MonoBehaviour
    {
        public readonly List<BottlePickup> Bottles = new List<BottlePickup>();
        private readonly List<Transform> staticRoots = new List<Transform>();
        private static readonly Color Grass=Art.Hex(0x778668),Asphalt=Art.Hex(0x494f53),
            Pavement=Art.Hex(0x939992),Rail=Art.Hex(0x3c4847);
        public GameObject Player { get; private set; }
        public CharacterController Controller { get; private set; }
        public CitizenVisual PlayerVisual { get; private set; }
        public GameObject Vehicle { get; private set; }
        public CarVisual VehicleVisual { get; private set; }
        public Camera Camera { get; private set; }
        public Transform WorldRoot { get; private set; }
        public Transform ContentRoot { get; private set; }

        private Transform Parent(string name, Transform owner)
        {
            var go=new GameObject(name);go.transform.SetParent(owner,false);return go.transform;
        }
        public void Build(GameState state)
        {
            UnityEngine.Random.InitState(71231);
            WorldRoot=Parent("Lindenstadt (static world)",transform);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=Art.Hex(0xa3afac);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=Art.Hex(0x9caab1);RenderSettings.fogDensity=.0027f;
            Camera=new GameObject("Third-person camera").AddComponent<Camera>();
            Camera.tag="MainCamera";Camera.fieldOfView=60;Camera.nearClipPlane=.08f;Camera.farClipPlane=310;
            Camera.backgroundColor=Art.Hex(0x93aab9);Camera.clearFlags=CameraClearFlags.Skybox;
            Camera.gameObject.AddComponent<AudioListener>();
            var sun=new GameObject("Directional daylight").AddComponent<Light>();
            sun.type=LightType.Directional;sun.color=Art.Hex(0xffe4ba);
            sun.intensity=1.15f;sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(52,-34,0);
            MakeGround();
            MakeStreets();
            MakeCoreBuildings();
            MakeOuterBuildings();
            MakeLandmarks();
            MakeTrees();
            MakeSites();
            MakeResidents();
            MakePickups(state);
            MakePlayer(state);
            if(state.hasVehicle) MakeVehicle(state);
        }

        private void MakeGround()
        {
            Art.Block(WorldRoot,"Terrain",90,-.6f,0,735,1.2f,540,Grass,true);
        }
        private void MakeStreets()
        {
            var root=Parent("Roads & sidewalks",WorldRoot);
            foreach (var road in ZeroRiseWorldData.Roads)
            {
                bool horizontal=road.width>road.depth;
                Art.Block(root,"Road • "+road.name,road.x,.055f,road.z,road.width,.11f,road.depth,Asphalt,true);
                if(horizontal)
                    foreach(int s in new[]{-1,1})
                        Art.Block(root,"Footpath",road.x,.095f,road.z+s*(road.depth/2+2.45f),road.width,.14f,4.6f,Pavement,true);
                else
                    foreach(int s in new[]{-1,1})
                        Art.Block(root,"Footpath",road.x+s*(road.width/2+2.45f),.095f,road.z,4.6f,.14f,road.depth,Pavement,true);
                for(int n=0;n<(horizontal?road.width:road.depth)/9;n++)
                {
                    float v=(n+.5f)*9 - (horizontal?road.width:road.depth)*.5f;
                    Art.Block(root,"Road markings",road.x+(horizontal?v:0),.114f,road.z+(horizontal?0:v),
                        horizontal?3.5f:.11f,.009f,horizontal?.11f:3.5f,Art.Hex(0xc8c4af));
                }
            }
            // Existing station viaduct: a known, navigable landmark.
            for(float z=-77;z<=77;z+=21)
                foreach(float x in new[]{-149f,-141f})
                    Art.Block(root,"Viaduct pillar",x,3.1f,z,1.5f,6.2f,2.0f,Art.Hex(0x837e75),true);
            Art.Block(root,"Elevated station viaduct",-145,6.4f,0,14,1.0f,164,Art.Hex(0x727b7b),true);
            Art.Block(root,"Rail tracks",-145,7.00f,0,9,.08f,164,Rail);
        }
        private static uint WallColor(string district, int i)
        {
            if(district=="Südwerk"||district=="Westhafen") return new uint[]{0xb4aa91,0x9a9b8d,0xb6a18a}[i%3];
            if(district=="Ostkontor"||district=="Techpark") return new uint[]{0xabb9b5,0xb8c1bc,0x9daead}[i%3];
            if(district=="Bahnhofsviertel")return new uint[]{0x955f4a,0x9c7463,0xa88c77}[i%3];
            if(district=="Höhenviertel")return new uint[]{0xcec5af,0xc4cabd,0xd3cdba}[i%3];
            return new uint[]{0xb8b1a0,0xc1b9ab,0xa4a89b}[i%3];
        }
        private void BuildingMesh(Transform root,Building b,int index)
        {
            var g=Parent(string.IsNullOrEmpty(b.name)?"Residential block":b.name,root);
            Color baseColor=Art.Hex(WallColor(b.district,index));
            Art.Block(g,"Walls",b.x,b.height/2+.17f,b.z,b.width,b.height,b.depth,baseColor,true);
            Art.Block(g,"Roof edge",b.x,b.height+.25f,b.z,b.width+.4f,.23f,b.depth+.4f,Art.Hex(0x535f5d));
            Art.Block(g,"Street-facing plinth",b.x,.43f,b.z+b.depth*.5f+.04f,b.width,.55f,.12f,Art.Hex(0x68706b));
            int floors=Mathf.Min(7,Mathf.Max(1,Mathf.FloorToInt((b.height-3)/3.6f)));
            int cols=Mathf.Clamp(Mathf.FloorToInt((b.width-1)/4.2f),2,7);
            for(int floor=0;floor<floors;floor++)for(int col=0;col<cols;col++)
            {
                float px=b.x+(-b.width/2+(col+.5f)*b.width/cols);
                float py=4.6f+floor*3.45f;
                if(py>=b.height-.8f)continue;
                var glass=Art.Hex((index+floor+col)%5==0?0x9ba5a0:0x314c56);
                Art.Block(g,"Window",px,py,b.z+b.depth*.5f+.07f,1.65f,1.65f,.08f,glass);
                Art.Block(g,"Sill",px,py-.9f,b.z+b.depth*.5f+.16f,1.95f,.16f,.26f,Art.Hex(0xbcb9a8));
            }
            if(!string.IsNullOrEmpty(b.name))
            {
                Art.Block(g,"Shop/door opening",b.x,1.4f,b.z+b.depth*.5f+.08f,2.6f,2.8f,.10f,Art.Hex(0x263f48));
                var text=Text(g,b.name,new Vector3(b.x,3.45f,b.z+b.depth*.5f+.15f),.34f,Art.Hex(0xe1d8bb));
                text.transform.localRotation=Quaternion.identity;
            }
        }
        private TextMesh Text(Transform root,string caption,Vector3 worldPosition,float scale,Color tint)
        {
            var go=new GameObject("Sign: "+caption);go.transform.SetParent(root,false);
            go.transform.position=worldPosition;
            var text=go.AddComponent<TextMesh>();text.text=caption;text.fontSize=36;text.characterSize=scale/3f;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=tint;
            return text;
        }
        private void MakeCoreBuildings()
        {
            var root=Parent("Core architecture",WorldRoot);
            int index=0;
            foreach(var b in ZeroRiseWorldData.Core) BuildingMesh(root,b,index++);
        }
        private void MakeOuterBuildings()
        {
            var root=Parent("District blocks",WorldRoot);
            int index=0;
            foreach(var b in ZeroRiseWorldData.OuterBuildings())BuildingMesh(root,b,index++);
        }
        private void MakeLandmarks()
        {
            var root=Parent("Landmark plazas",WorldRoot);
            foreach(var place in ZeroRiseWorldData.Places)
            {
                if(place.kind!="landmark")continue;
                float x=place.position.x,z=place.position.y;
                Art.Block(root,place.name+" plaza",x,.14f,z,19,.22f,18,Art.Hex(0xaaa898),true);
                Art.Part(root,PrimitiveType.Cylinder,place.name+" monument",
                    new Vector3(x,1.35f,z),new Vector3(2.2f,1.1f,2.2f),Art.Hex(0x607a73),true);
                Text(root,place.name,new Vector3(x,2.7f,z),.42f,Art.Hex(0xf1d6a5));
            }
        }
        private void MakeTrees()
        {
            var root=Parent("Low-cost city trees",WorldRoot);
            var random=new System.Random(531);
            for(int i=0;i<175;i++)
            {
                float x=(float)(ZeroRiseWorldData.MinX+10+random.NextDouble()*680);
                float z=(float)(ZeroRiseWorldData.MinZ+10+random.NextDouble()*490);
                if(ZeroRiseWorldData.OnRoad(x,z)||ZeroRiseWorldData.InExistingBuilding(x,z,6))continue;
                Art.Part(root,PrimitiveType.Cylinder,"Trunk",new Vector3(x,1.75f,z),
                    new Vector3(.30f,1.75f,.30f),Art.Hex(0x665542));
                Art.Part(root,PrimitiveType.Sphere,"Canopy",new Vector3(x,4.4f,z),
                    new Vector3(3.3f,3.9f,3.2f),Art.Hex(new uint[]{0x47644b,0x5a7855,0x66774b}[i%3]));
            }
        }
        private void MakeSites()
        {
            var root=Parent("Location interaction signs",WorldRoot);
            foreach(var p in ZeroRiseWorldData.Places)
            {
                if(p.kind=="landmark"||p.kind=="delivery")continue;
                float x=p.position.x,z=p.position.y;
                Art.Part(root,PrimitiveType.Cylinder,"Marker "+p.name,
                    new Vector3(x,.14f,z),new Vector3(.95f,.02f,.95f),Art.Hex(0xd5af79));
                Text(root,p.name,new Vector3(x,1.93f,z),.38f,Art.Hex(0xf1dfb7));
            }
        }
        private void MakeResidents()
        {
            var root=Parent("Pedestrians (prototype)",WorldRoot);
            int index=0;
            foreach(var p in ZeroRiseWorldData.Places)
            {
                if(index>=18)break;
                var pos=p.position;
                if(ZeroRiseWorldData.InExistingBuilding(pos.x+4,pos.y+5,1))continue;
                var go=new GameObject("Citizen "+index+" near "+p.name);go.transform.SetParent(root);
                go.transform.position=new Vector3(pos.x+4,.20f,pos.y+5);
                go.AddComponent<CitizenVisual>().Build(false,index+2);
                go.AddComponent<RoamingResident>().Init(index);
                index++;
            }
        }
        public void MakePickups(GameState state)
        {
            var root=Parent("Scavenging objects",WorldRoot);
            var collected=new HashSet<int>(state.collectedBottleIds??Array.Empty<int>());
            var random=new System.Random(71923);
            for(int i=0;i<55;i++)
            {
                float x,z;
                if(i<15){x=-200+(float)random.NextDouble()*83;z=-70+(float)random.NextDouble()*140;}
                else {x=ZeroRiseWorldData.MinX+(float)random.NextDouble()*690;z=ZeroRiseWorldData.MinZ+(float)random.NextDouble()*500;}
                if(ZeroRiseWorldData.OnRoad(x,z)||ZeroRiseWorldData.InExistingBuilding(x,z,3)||collected.Contains(i))continue;
                var go=new GameObject("Pfandflasche "+i);go.transform.SetParent(root);go.transform.position=new Vector3(x,.36f,z);
                var glass=Art.Part(go.transform,PrimitiveType.Cylinder,"Green bottle",Vector3.zero,new Vector3(.16f,.26f,.16f),Art.Hex(0x3e916e));
                Art.Part(go.transform,PrimitiveType.Cylinder,"Bottle neck",new Vector3(0,.20f,0),new Vector3(.07f,.10f,.07f),Art.Hex(0x65a789));
                var pickup=go.AddComponent<BottlePickup>();pickup.Init(i);Bottles.Add(pickup);
            }
        }
        private void MakePlayer(GameState state)
        {
            Player=new GameObject("Player character");
            Player.transform.SetParent(transform);
            Player.transform.position=new Vector3(state.x,.35f,state.z);
            Player.transform.rotation=Quaternion.Euler(0,state.yaw,0);
            Controller=Player.AddComponent<CharacterController>();
            Controller.height=1.92f;Controller.radius=.32f;Controller.center=new Vector3(0,.96f,0);
            Controller.stepOffset=.30f;Controller.slopeLimit=50f;
            var visuals=Parent("Visual rig",Player.transform);
            PlayerVisual=visuals.gameObject.AddComponent<CitizenVisual>();
            PlayerVisual.Build(true,99);
        }
        public void MakeVehicle(GameState state)
        {
            if(Vehicle!=null)Destroy(Vehicle);
            Vehicle=new GameObject("Owned vehicle");
            Vehicle.transform.SetParent(transform);
            Vehicle.transform.position=new Vector3(state.vehicleX,.23f,state.vehicleZ);
            Vehicle.transform.rotation=Quaternion.Euler(0,state.vehicleYaw,0);
            VehicleVisual=Vehicle.AddComponent<CarVisual>();
            VehicleVisual.Build(state.vehicleType);
            VehicleVisual.UpdateCondition(state.vehicleCondition);
            // Root collision only: decorative parts do not interfere with the camera.
            var box=Vehicle.AddComponent<BoxCollider>();
            box.center=new Vector3(0,.85f,0);
            box.size=state.vehicleType=="bike"?new Vector3(.9f,1.6f,2.1f):
                state.vehicleType=="van"?new Vector3(2f,2.15f,4.65f):new Vector3(1.85f,1.6f,4f);
        }
    }
}
