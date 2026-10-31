using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise
{
    // Runtime weather uses private profiles/materials. No authored asset, mesh,
    // collision or navigation data is modified by a weather or time change.
    [DefaultExecutionOrder(310)]
    public sealed class WestbruchWeatherVisuals : MonoBehaviour
    {
        public Material rainMaterial, puddleMaterial;
        [Min(0)] public float nightMoonLux=.22f,nightFillLux=1.2f;
        [Range(0,12)] public float urbanBounceLux=6;
        [Tooltip("Minimum camera EV at night; the solar exposure curve can remain slightly higher.")]
        public float nightExposureEV=0;
        public int RainDropCount { get; private set; }
        public int WetMaterialCount => wetMaterials.Count;
        WeatherSurfaceWater surfaceWater;
        public int PuddleCount => surfaceWater?surfaceWater.PuddleCount:0;
        public int SplashCount => surfaceWater?surfaceWater.SplashCount:0;
        public bool ReflectivePuddles => surfaceWater&&surfaceWater.UsesLitReflections;
        public Vector3 PuddleInspectionPoint => surfaceWater?surfaceWater.InspectionPoint:Vector3.zero;
        public int LitWindowCount { get; private set; }
        public float SunIntensity => sun ? sun.intensity : 0;
        public float MoonIntensity => moon ? moon.intensity : 0;
        public float Exposure => exposure != null ? exposure.fixedExposure.value : 0;
        public bool RoofMapReady { get; private set; }
        public bool Initialized { get; private set; }
        public float NightLightBlend { get; private set; }
        public float LightningBoostLux { get; private set; }
        public int ActiveHeadlights { get; private set; }
        public int ActiveStreetLights { get; private set; }
        public int HeadlampEmitterMaterials { get; private set; }
        public float StarVisibility { get; private set; }
        public bool StarMapLoaded { get; private set; }
        public static float HeadlightVisibility(float metres)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(140,240,metres));
        public static float StreetlightVisibility(float metres)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(170,270,metres));
        Texture2D dippedBeamCookie,streetCookie;
        const int DropLimit=1024, AtlasSize=256;
        static readonly Vector4 AtlasBounds=new Vector4(-160,-150,1f/340,1f/300);
        struct Drop { public Vector3 position;public float floor,speed,length,width,alpha;public bool alive; }
        sealed class WetMaterial
        {
            public Material material; public Color[] colors; public float[] smoothness,min,max;
            public bool shaderWet,layered,soil;public float exposure=1;
        }
        sealed class WindowMaterial { public Material material;public Color emission,color;public int group;public bool lamp,shop; }
        sealed class RendererMaterials { public Renderer renderer;public Material[] original; }
        sealed class LightState { public Light light;public float intensity;public bool enabled;public bool street,bounce;public float fade=1;public Material emitter;public Color emission; }
        sealed class VehicleLight
        {
            public Light light;public Renderer lens;public MaterialPropertyBlock original,block;
            public Transform body;public SimpleCar car;public TrafficVehicle traffic;public float intensity,fade;public bool rear;public Material emitter;public Color idleEmission;
        }
        readonly List<WetMaterial> wetMaterials=new List<WetMaterial>();
        readonly List<WindowMaterial> windowMaterials=new List<WindowMaterial>();
        readonly List<RendererMaterials> renderers=new List<RendererMaterials>();
        readonly List<LightState> lamps=new List<LightState>();
        readonly List<GameObject> createdLamps=new List<GameObject>();
        readonly List<VehicleLight> vehicleLights=new List<VehicleLight>();
        TrafficVehicle[] trafficBodies;
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<VolumeComponent> ownedComponents=new List<VolumeComponent>();
        readonly System.Random random=new System.Random(261022);
        readonly Drop[] drops=new Drop[DropLimit];
        Vector3[] rainVertices;Color[] rainColors;
        Mesh rainMesh,puddleMesh;GameObject rainObject,puddleObject;Texture2D roofMap;
        Volume atmosphere;VolumeProfile originalProfile,weatherProfile;Light sun,moon,nightFill,dayFill,nightReturn,skySun;HDAdditionalLightData sunData;bool originalSunSky;Quaternion sunRotation;Color sunColor;float sunOriginalIntensity;bool sunOriginalEnabled;LightShadows sunOriginalShadows;
        Exposure exposure;Fog fog;VolumetricClouds clouds;PhysicallyBasedSky sky;
        Transform listener;Camera view;Terrain terrain;WestbruchWeatherAudio weatherAudio;
        float applyAt,spawnRemainder,lightningUntil,lightningStrength,fogDistance=340,fogHeight=50;int authoredWindowCount;bool moonOwnsShadows;


        IEnumerator Start()
        {
            view=Camera.main;if(!view)view=FindAnyObjectByType<Camera>();
            listener=view?view.transform:null;
            terrain=Terrain.activeTerrain;
            var atmosphereObject=GameObject.Find("Westbruch atmosphere");
            atmosphere=atmosphereObject?atmosphereObject.GetComponent<Volume>():null;
            if(atmosphere&&atmosphere.sharedProfile)
            {
                originalProfile=atmosphere.sharedProfile;
                weatherProfile=ScriptableObject.CreateInstance<VolumeProfile>();weatherProfile.name="Runtime weather atmosphere";
                foreach(var component in originalProfile.components)
                {var copy=Instantiate(component);weatherProfile.components.Add(copy);ownedComponents.Add(copy);}
                atmosphere.sharedProfile=weatherProfile;
                weatherProfile.TryGet(out exposure);weatherProfile.TryGet(out fog);weatherProfile.TryGet(out clouds);weatherProfile.TryGet(out sky);
                if(clouds!=null){clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Quality);clouds.cloudPreset=VolumetricClouds.CloudPresets.Custom;}
                if(sky!=null)
                {
                    sky.updateMode.Override(EnvironmentUpdateMode.Realtime);sky.updatePeriod.Override(.25f);
                    var stars=Resources.Load<Cubemap>("WestbruchStars");StarMapLoaded=stars;
                    if(stars)sky.spaceEmissionTexture.Override(stars);
                }
            }
            var sunObject=GameObject.Find("Late afternoon sun");sun=sunObject?sunObject.GetComponent<Light>():null;
            if(sun){sunRotation=sun.transform.rotation;sunColor=sun.color;sunOriginalIntensity=sun.intensity;sunOriginalEnabled=sun.enabled;sunOriginalShadows=sun.shadows;sunData=sun.GetComponent<HDAdditionalLightData>();if(sunData){originalSunSky=sunData.interactsWithSky;sunData.interactsWithSky=false;}}
            dippedBeamCookie=BeamCookie(true);streetCookie=BeamCookie(false);
            CaptureMaterialsAndLights();CreateNightLighting();CreateRain();
            weatherAudio=GetComponent<WestbruchWeatherAudio>();if(weatherAudio)weatherAudio.Lightning+=Flash;
            Initialized=true;ApplyWorld();
            yield return BakeRoofMap();
            BuildPuddles();
        }
        void CaptureMaterialsAndLights()
        {
            var wetClones=new Dictionary<Material,WetMaterial>();
            var windowClones=new Dictionary<Material,WindowMaterial>();
            foreach(var renderer in FindObjectsByType<Renderer>())
            {
                if(renderer is not MeshRenderer)continue;
                var materials=renderer.sharedMaterials;Material[] changed=null;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(!source)continue;string name=source.name;
                    bool wet=IsWeatherSurface(name),window=name.StartsWith("UrbanLife_InteriorWall_",StringComparison.Ordinal)||name.StartsWith("UrbanLife_Curtain_",StringComparison.Ordinal)||name=="StreetlightFrostedGlass"||name=="Atmosphere_WarmBulkheadLens"||name=="ShopWindow";
                    if(!wet&&!window)continue;
                    changed??=(Material[])materials.Clone();
                    if(wet)
                    {
                        if(!wetClones.TryGetValue(source,out var state))
                        {
                            var clone=new Material(source){name=source.name+" (weather instance)"};ownedMaterials.Add(clone);
                            bool layered=source.shader.name.Contains("LayeredLit");
                            // Preserve stock shader/keywords to retain the
                            // exact variants already shipped for these assets.
                            bool shaderWet=clone.shader.name=="ZeroRise/UrbanLit"&&clone.IsKeywordEnabled("_ZR_ROAD");
                            int count=layered?4:1;
                            state=new WetMaterial{material=clone,shaderWet=shaderWet,layered=layered,soil=layered||name.Contains("Earth")||name.Contains("Soil"),colors=new Color[count],smoothness=new float[count],min=new float[count],max=new float[count]};
                            for(int n=0;n<count;n++)
                            {string suffix=layered?n.ToString():"";state.colors[n]=clone.GetColor("_BaseColor"+suffix);state.smoothness[n]=clone.GetFloat("_Smoothness"+suffix);state.min[n]=clone.GetFloat("_SmoothnessRemapMin"+suffix);state.max[n]=clone.GetFloat("_SmoothnessRemapMax"+suffix);}
                            // Sheltered earth is a dedicated existing material.
                            if(name.Contains("Sheltered"))state.exposure=.10f;
                            wetClones.Add(source,state);wetMaterials.Add(state);
                        }
                        changed[i]=state.material;
                    }
                    else
                    {
                        if(!windowClones.TryGetValue(source,out var state))
                        {
                            var clone=new Material(source){name=source.name+" (evening instance)"};ownedMaterials.Add(clone);
                            state=new WindowMaterial{material=clone,color=clone.GetColor("_BaseColor"),emission=clone.GetColor("_EmissiveColor"),group=windowClones.Count%3,shop=name=="ShopWindow",lamp=name=="StreetlightFrostedGlass"||name=="Atmosphere_WarmBulkheadLens"};
                            windowClones.Add(source,state);windowMaterials.Add(state);
                        }
                        changed[i]=state.material;
                    }
                }
                if(changed!=null){renderers.Add(new RendererMaterials{renderer=renderer,original=materials});renderer.sharedMaterials=changed;}
            }
            foreach(var t in FindObjectsByType<Transform>())
            {
                if(t.name.StartsWith("Visible apartment interior ",StringComparison.Ordinal))authoredWindowCount++;
                if(!t.name.StartsWith("Old European streetlight ",StringComparison.Ordinal))continue;
                var obj=new GameObject("Timed existing streetlamp");obj.transform.SetParent(t,false);obj.transform.localPosition=new Vector3(0,5.62f,1.12f);obj.transform.rotation=Quaternion.Euler(90,0,0);
                var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.range=23;light.spotAngle=112;light.innerSpotAngle=82;light.shadows=LightShadows.None;light.useColorTemperature=true;light.colorTemperature=t.position.x< -80?2700:t.position.x>60&&t.position.z<45?4000:3100;light.lightUnit=LightUnit.Candela;
                var hd=obj.AddComponent<HDAdditionalLightData>();hd.volumetricDimmer=.08f;hd.fadeDistance=320;hd.SetCookie(streetCookie);
                light.intensity=Mathf.Abs(t.position.x-94)<15||Mathf.Abs(t.position.z)<12?1300:1000;
                lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=true,street=true});createdLamps.Add(obj);
                // Approximate warm light reflected by the pavement onto nearby facades.
                // Diffuse-only: this is bounced light, not a second visible bulb or specular source.
                var bounceObject=new GameObject("Night bounce street");bounceObject.transform.SetParent(t,false);
                bounceObject.transform.position=new Vector3(light.transform.position.x,t.position.y+.85f,light.transform.position.z);
                var bounce=bounceObject.AddComponent<Light>();bounce.type=LightType.Point;bounce.range=13;bounce.lightUnit=LightUnit.Candela;bounce.intensity=26;bounce.shadows=LightShadows.None;bounce.useColorTemperature=true;bounce.colorTemperature=light.colorTemperature;
                var hdBounce=bounceObject.AddComponent<HDAdditionalLightData>();hdBounce.affectSpecular=false;hdBounce.volumetricDimmer=0;hdBounce.fadeDistance=320;
                lamps.Add(new LightState{light=bounce,intensity=bounce.intensity,enabled=true,bounce=true});createdLamps.Add(bounceObject);
            }
            AddNeighbourhoodLights();
            foreach(var light in FindObjectsByType<Light>())
            {
                if(light.type==LightType.Directional||light.name=="Timed existing streetlamp"||light.name.StartsWith("Night spill")||light.name.StartsWith("Night bounce"))continue;
                // Existing bridge lamps stay gently lit all day. Timed entrance
                // and street lamps use their original physical intensity.
                if(light.name.Contains("entrance light")||light.name.Contains("Street")||light.name.Contains("street")||light.name.Contains("Lantern")||light.name.Contains("Restrained underbridge"))
                    lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=light.enabled});
            }
        }
        Texture2D BeamCookie(bool headlamp)
        {
            const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=headlamp?"Soft asymmetric dipped beam":"Soft street luminaire distribution",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1)*2-1,v=y/(float)(size-1)*2-1;
                float value;
                if(headlamp)
                {
                    // Right-hand traffic: a low horizontal cut-off, rising gently toward the verge.
                    float cutoff=.015f+.11f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.1f,.75f,u));
                    float top=1-Mathf.SmoothStep(cutoff-.035f,cutoff+.025f,v);
                    float hot=Mathf.Exp(-u*u*2.8f-(v+.11f)*(v+.11f)*15);
                    float foreground=.24f*Mathf.Exp(-u*u*1.4f-(v+.43f)*(v+.43f)*3);
                    value=Mathf.Clamp01((hot+foreground)*top)*(1-Mathf.SmoothStep(.82f,1,Mathf.Abs(u)));
                }
                else value=Mathf.Exp(-(u*u*.8f+v*v*1.5f)*1.2f)*(1-Mathf.SmoothStep(.65f,1,new Vector2(u,v).magnitude));
                texture.SetPixel(x,y,new Color(value,value,value,1));
            }
            texture.Apply(true,true);return texture;
        }
        void AddNeighbourhoodLights()
        {
            foreach(var fixture in FindObjectsByType<Transform>())
            {
                bool entrance=fixture.name=="Entrance bulkhead",window=fixture.name.StartsWith("Visible apartment interior ",StringComparison.Ordinal),shop=fixture.name=="Shop display window";
                if(!entrance&&!window&&!shop)continue;
                // Small pools originate at existing physical fixtures and furnished windows.
                var obj=new GameObject(shop?"Night spill shop":entrance?"Night spill entrance":"Night spill apartment");obj.transform.SetParent(fixture,false);
                obj.transform.position=fixture.position-fixture.forward*(window?.40f:.08f)+Vector3.down*(window?.2f:.1f);
                obj.transform.rotation=Quaternion.LookRotation(-fixture.forward*(window?.85f:.35f)+Vector3.down);
                var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.range=shop?9:window?6:8;
                light.spotAngle=window?85:105;light.innerSpotAngle=window?45:65;light.shadows=LightShadows.None;
                light.useColorTemperature=true;light.colorTemperature=shop?4100:window?2700:3000;light.lightUnit=LightUnit.Candela;light.intensity=shop?200:window?65:230;
                var hd=obj.AddComponent<HDAdditionalLightData>();hd.fadeDistance=240;hd.volumetricDimmer=0;hd.SetCookie(streetCookie);
                lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=true});createdLamps.Add(obj);
            }
            var shelter=GameObject.Find("Old Westbruch bus shelter");
            if(shelter)
            {
                var diffuser=GameObject.CreatePrimitive(PrimitiveType.Cube);diffuser.name="Attached night shelter light";
                diffuser.transform.SetParent(shelter.transform,false);diffuser.transform.localPosition=new Vector3(0,2.545f,0);diffuser.transform.localScale=new Vector3(1.5f,.035f,.12f);
                Destroy(diffuser.GetComponent<Collider>());
                var material=new Material(Shader.Find("HDRP/Lit")){name="Night shelter frosted diffuser"};material.SetColor("_BaseColor",new Color(.65f,.75f,.80f));material.SetColor("_EmissiveColor",Color.black);HDMaterial.ValidateMaterial(material);ownedMaterials.Add(material);
                var renderer=diffuser.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;createdLamps.Add(diffuser);
                var lamp=new GameObject("Night spill shelter");lamp.transform.SetParent(shelter.transform,false);lamp.transform.localPosition=new Vector3(0,2.49f,0);lamp.transform.rotation=Quaternion.Euler(90,0,0);
                var light=lamp.AddComponent<Light>();light.type=LightType.Spot;light.range=8;light.spotAngle=110;light.innerSpotAngle=80;light.lightUnit=LightUnit.Candela;light.intensity=240;light.useColorTemperature=true;light.colorTemperature=4200;light.shadows=LightShadows.None;
                var hd=lamp.AddComponent<HDAdditionalLightData>();hd.fadeDistance=240;hd.volumetricDimmer=0;hd.SetCookie(streetCookie);
                lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=true,emitter=material,emission=new Color(.70f,.84f,1)*100});createdLamps.Add(lamp);
            }
        }
        static bool IsWeatherSurface(string name)
        {
            return name=="Asphalt"||name=="Paving"||name=="GardenEarth"||name=="RoadRepair"||name.StartsWith("RoadRepairOrganic_",StringComparison.Ordinal)
                ||name.StartsWith("OverpassGround_",StringComparison.Ordinal)&&!name.Contains("Distant")
                ||name.StartsWith("GroundDetail_",StringComparison.Ordinal)||name=="UrbanYard";
        }
        void CreateNightLighting()
        {
            // The atmospheric sun follows solar altitude. Its camera-normalized
            // radiance must not inherit ground-fill or moon adaptation curves.
            var skyObject=new GameObject("Weather atmospheric sun");skyObject.transform.SetParent(transform,false);
            skySun=skyObject.AddComponent<Light>();skySun.type=LightType.Directional;skySun.shadows=LightShadows.None;skySun.lightUnit=LightUnit.Lux;skySun.color=Color.white;skySun.intensity=0;
            var hdSky=skyObject.AddComponent<HDAdditionalLightData>();hdSky.affectDiffuse=false;hdSky.affectSpecular=false;hdSky.interactsWithSky=true;hdSky.angularDiameter=.55f;hdSky.flareSize=.75f;hdSky.flareFalloff=8;createdLamps.Add(skyObject);
            // An elevated moon remains independent of the horizon sun during
            // twilight, so shaded streets never lose both light sources.
            var moonObject=new GameObject("Weather moonlight");moonObject.transform.SetParent(transform,false);
            moon=moonObject.AddComponent<Light>();moon.type=LightType.Directional;moon.shadows=LightShadows.None;moon.color=new Color(.53f,.63f,.84f);moon.lightUnit=LightUnit.Lux;moon.intensity=0;
            var hdMoon=moonObject.AddComponent<HDAdditionalLightData>();hdMoon.angularDiameter=.5f;hdMoon.interactsWithSky=true;hdMoon.surfaceTexture=Resources.Load<Texture2D>("WestbruchMoon");hdMoon.surfaceTint=new Color(.76f,.78f,.82f);hdMoon.flareSize=.3f;hdMoon.flareTint=new Color(.18f,.20f,.23f);createdLamps.Add(moonObject);
            var fillObject=new GameObject("Weather diffuse urban night fill");fillObject.transform.SetParent(transform,false);
            fillObject.transform.rotation=Quaternion.Euler(65,135,0);
            nightFill=fillObject.AddComponent<Light>();nightFill.type=LightType.Directional;nightFill.shadows=LightShadows.None;nightFill.color=new Color(.45f,.56f,.73f);nightFill.lightUnit=LightUnit.Lux;nightFill.intensity=0;
            var hdFill=fillObject.AddComponent<HDAdditionalLightData>();hdFill.affectSpecular=false;hdFill.volumetricDimmer=0;hdFill.interactsWithSky=false;createdLamps.Add(fillObject);
            // A weak opposite diffuse return keeps back-facing facades readable.
            var returnObject=new GameObject("Weather diffuse city return");returnObject.transform.SetParent(transform,false);returnObject.transform.rotation=Quaternion.Euler(35,315,0);
            nightReturn=returnObject.AddComponent<Light>();nightReturn.type=LightType.Directional;nightReturn.shadows=LightShadows.None;nightReturn.lightUnit=LightUnit.Lux;nightReturn.color=new Color(.46f,.51f,.60f);nightReturn.intensity=0;
            var hdReturn=returnObject.AddComponent<HDAdditionalLightData>();hdReturn.affectSpecular=false;hdReturn.volumetricDimmer=0;hdReturn.interactsWithSky=false;createdLamps.Add(returnObject);
            // Keep the established daylight sky bounce separate from the night-only fill.
            var dayObject=new GameObject("Weather daylight sky bounce");dayObject.transform.SetParent(transform,false);dayObject.transform.rotation=Quaternion.Euler(65,135,0);
            dayFill=dayObject.AddComponent<Light>();dayFill.type=LightType.Directional;dayFill.shadows=LightShadows.None;dayFill.lightUnit=LightUnit.Lux;dayFill.color=new Color(.80f,.87f,1);dayFill.intensity=0;
            var hdDay=dayObject.AddComponent<HDAdditionalLightData>();hdDay.affectSpecular=false;hdDay.volumetricDimmer=0;hdDay.interactsWithSky=false;hdDay.angularDiameter=0;createdLamps.Add(dayObject);
            trafficBodies=FindObjectsByType<TrafficVehicle>();
            foreach(var car in FindObjectsByType<SimpleCar>())MountHeadlights(car.transform,car,null);
            foreach(var traffic in trafficBodies)MountHeadlights(traffic.transform,null,traffic);
        }
        void MountHeadlights(Transform body,SimpleCar car,TrafficVehicle traffic)
        {
            // Front lenses can be merged into the vehicle's material batches.
            // Replace their material slot, not only the disabled source renderer's block.
            Material frontEmitter=null;
            foreach(var renderer in body.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;Material[] replacement=null;
                for(int i=0;i<materials.Length;i++)if(materials[i]&&(materials[i].name=="VehicleLampHead"||materials[i].name=="HeadlightLens"))
                {
                    if(!frontEmitter){frontEmitter=new Material(materials[i]){name="Weather headlamp emitter"};ownedMaterials.Add(frontEmitter);HeadlampEmitterMaterials++;}
                    replacement??=(Material[])materials.Clone();replacement[i]=frontEmitter;
                }
                if(replacement!=null){renderers.Add(new RendererMaterials{renderer=renderer,original=materials});renderer.sharedMaterials=replacement;}
            }
            foreach(var lens in body.GetComponentsInChildren<Renderer>())
            {
                bool rear=lens.name=="Rear lamp cluster";
                if(lens.name!="Headlamp with inset reflector"&&!rear)continue;
                var obj=new GameObject(rear?"Mounted rear running lamp":"Mounted automatic dipped headlamp");obj.transform.SetParent(body,false);
                obj.transform.position=lens.transform.position+body.forward*(rear?-.045f:.045f);
                obj.transform.localRotation=rear?Quaternion.Euler(4,180,0):Quaternion.Euler(1.1f,0,0);
                var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.range=rear?4.8f:62;light.spotAngle=rear?100:52;light.innerSpotAngle=rear?60:45;light.color=rear?new Color(1,.035f,.012f):new Color(1,.95f,.86f);light.shadows=rear?LightShadows.None:LightShadows.Soft;light.lightUnit=LightUnit.Candela;
                var hd=obj.AddComponent<HDAdditionalLightData>();hd.volumetricDimmer=.06f;hd.fadeDistance=300;hd.shadowFadeDistance=65;hd.SetShadowResolution(256);hd.SetCookie(rear?streetCookie:dippedBeamCookie);
                light.intensity=rear?8:6500;light.enabled=false;
                var original=new MaterialPropertyBlock();lens.GetPropertyBlock(original);var block=new MaterialPropertyBlock();lens.GetPropertyBlock(block);
                vehicleLights.Add(new VehicleLight{light=light,lens=lens,original=original,block=block,body=body,car=car,traffic=traffic,intensity=light.intensity,rear=rear,emitter=rear?null:frontEmitter,idleEmission=frontEmitter?frontEmitter.GetColor("_EmissiveColor"):Color.black});createdLamps.Add(obj);
            }
        }
        IEnumerator BakeRoofMap()
        {
            // 1.3 m atlas cells over the actual playable district; amortized so
            // shader weather never stalls a frame for a complete roof scan.
            var data=new Color[AtlasSize*AtlasSize];
            var terrainOrigin=terrain?terrain.transform.position:Vector3.zero;
            Shader.SetGlobalVector("_ZRWeatherTerrainOrigin",new Vector4(terrainOrigin.x,terrainOrigin.z,0,0));
            for(int row=0;row<AtlasSize;row++)
            {
                for(int col=0;col<AtlasSize;col++)
                {
                    float x=AtlasBounds.x+(col+.5f)/(AtlasSize*AtlasBounds.z),z=AtlasBounds.y+(row+.5f)/(AtlasSize*AtlasBounds.w);
                    float top=-10,ground=terrain?terrain.SampleHeight(new Vector3(x,0,z))+terrainOrigin.y:0;
                    if(Physics.Raycast(new Vector3(x,100,z),Vector3.down,out var hit,160,1,QueryTriggerInteraction.Ignore))top=hit.point.y;
                    data[row*AtlasSize+col]=new Color(top,ground,0,1);
                }
                if(row%4==3)yield return null;
            }
            roofMap=new Texture2D(AtlasSize,AtlasSize,TextureFormat.RGBAFloat,false,true){name="Runtime static rain exposure heights",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            roofMap.SetPixels(data);roofMap.Apply(false,true);Shader.SetGlobalTexture("_ZRWeatherHeightMap",roofMap);Shader.SetGlobalVector("_ZRWeatherBounds",AtlasBounds);Shader.SetGlobalFloat("_ZRWeatherEnabled",1);RoofMapReady=true;
        }
        public bool IsSheltered(Vector3 point)
        {
            // An actual collision roof rather than a hand-authored region.
            return Physics.Raycast(point+Vector3.up*.15f,Vector3.up,80,1,QueryTriggerInteraction.Ignore);
        }
        void CreateRain()
        {
            if(!rainMaterial)return;
            rainObject=new GameObject("Weather rain · roof clipped");rainObject.transform.SetParent(transform,false);
            rainObject.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            rainMesh=new Mesh{name="Bounded live rain streaks"};rainMesh.MarkDynamic();
            rainVertices=new Vector3[DropLimit*4];rainColors=new Color[DropLimit*4];var uv=new Vector2[DropLimit*4];var triangles=new int[DropLimit*6];
            for(int i=0;i<DropLimit;i++){int v=i*4,t=i*6;uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(0,1);triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}
            rainMesh.vertices=rainVertices;rainMesh.uv=uv;rainMesh.colors=rainColors;rainMesh.triangles=triangles;
            rainObject.AddComponent<MeshFilter>().sharedMesh=rainMesh;var renderer=rainObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=rainMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
        }
        void BuildPuddles()
        {
            if(!puddleMaterial||!rainMaterial)return;
            surfaceWater=gameObject.AddComponent<WeatherSurfaceWater>();surfaceWater.Initialize(puddleMaterial,rainMaterial);
        }
        void Update()
        {
            if(!Initialized)return;
            if(Time.unscaledTime>=applyAt){applyAt=Time.unscaledTime+.05f;ApplyWorld();}
            UpdateRain();
        }
        public static float NightBlendAt(float hour)
        {
            hour=Mathf.Repeat(hour,24);
            float dawn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.15f,6.3f,hour));
            float dusk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(19.5f,20+40f/60,hour));
            return 1-dawn*(1-dusk);
        }
        void ApplyWorld()
        {
            var world=WorldWeatherClock.Instance;if(!world)return;
            float day=world.Daylight,cloud=Mathf.Clamp01(world.CloudCoverage),night=NightBlendAt(world.Hour);
            NightLightBlend=night;
            float flash=Time.unscaledTime<lightningUntil?lightningStrength:0;
            float lowLight=Mathf.Max(night,world.RainIntensity*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,cloud))*.65f);
            Shader.SetGlobalFloat("_ZRWeatherWind",world.WindStrength*world.GustStrength);
            // Sunset at 20:00; civil twilight continues while the sun descends
            // below the horizon, reaching about -8 degrees around 20:40.
            float elevation=Mathf.Sin((world.Hour-5.2f)/(20f-5.2f)*Mathf.PI)*58;
            float solar=Mathf.Sqrt(Mathf.Clamp01(elevation/58));
            float weatherAttenuation=Mathf.Lerp(1,.012f,Mathf.SmoothStep(0,1,cloud));
            float moonEV=Mathf.Clamp(12+.65f*Mathf.Log(.55f/85000,2),nightExposureEV,12);
            float daylightReferenceEV=Mathf.Clamp(12+.65f*Mathf.Log(weatherAttenuation,2),moonEV,12);
            float adaptedEV=Mathf.Lerp(daylightReferenceEV,moonEV,night);
            float daylightCompensation=Mathf.Pow(2,adaptedEV-daylightReferenceEV);
            float moonCompensation=Mathf.Pow(2,adaptedEV-moonEV);
            float ambientBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(night*4));
            float visibleSolarLux=85000*Mathf.Pow(day,1.15f)*solar*weatherAttenuation*daylightCompensation;
            float visibleMoonLux=Mathf.Min(.3f,nightMoonLux)*ambientBlend*moonCompensation*Mathf.Lerp(1,.18f,cloud);
            if(moonOwnsShadows&&visibleSolarLux>visibleMoonLux*1.2f)moonOwnsShadows=false;
            else if(!moonOwnsShadows&&visibleMoonLux>visibleSolarLux*1.2f)moonOwnsShadows=true;
            float azimuth=world.Hour/24*360-180;
            LightningBoostLux=flash*Mathf.Lerp(28,9000,day);
            if(skySun)
            {
                skySun.transform.rotation=Quaternion.Euler(elevation,azimuth,0);
                float atmosphereEnvelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-16,-10,elevation));
                skySun.intensity=85000*weatherAttenuation*daylightCompensation*atmosphereEnvelope;
                skySun.enabled=atmosphereEnvelope>.0001f;
            }
            if(sun)
            {
                sun.transform.rotation=Quaternion.Euler(elevation,azimuth,0);
                sun.color=Color.Lerp(Color.Lerp(new Color(1,.73f,.52f),new Color(1,.97f,.94f),Mathf.SmoothStep(0,1,day)),new Color(.89f,.94f,1),cloud*.85f);
                sun.intensity=visibleSolarLux+LightningBoostLux*(1-night);
                // HDRP has one cascaded directional shadow atlas. Transfer
                // it to the moon only once the night light dominates.
                sun.shadows=moonOwnsShadows?LightShadows.None:sunOriginalShadows;
                sun.enabled=visibleSolarLux>0||LightningBoostLux>0;
            }
            if(moon)
            {
                moon.transform.rotation=Quaternion.Euler(30,azimuth+180,0);moon.intensity=visibleMoonLux+LightningBoostLux*night;moon.enabled=night>.005f;
                moon.shadows=moonOwnsShadows?sunOriginalShadows:LightShadows.None;
            }
            if(sun)Shader.SetGlobalVector("_ZRWeatherSunDirection",moonOwnsShadows&&moon?-moon.transform.forward:-sun.transform.forward);
            if(exposure!=null)exposure.fixedExposure.Override(adaptedEV-flash*.35f);
            if(nightFill)
            {
                // Broad sky bounce maintains readable shadows under an overcast sky.
                nightFill.enabled=true;
                nightFill.color=Color.Lerp(new Color(.58f,.62f,.68f),new Color(.80f,.87f,1),day);
                nightFill.intensity=Mathf.Max(nightFillLux,urbanBounceLux)*ambientBlend*moonCompensation;
            }
            if(nightReturn){nightReturn.enabled=night>.001f;nightReturn.intensity=Mathf.Max(nightFillLux,urbanBounceLux)*.5f*ambientBlend*moonCompensation;}
            if(dayFill){dayFill.enabled=day>.0001f;dayFill.intensity=(1100+6500*cloud)*day*daylightCompensation;}
            ActiveHeadlights=0;
            foreach(var headlamp in vehicleLights)
            {
                if(!headlamp.body||!headlamp.light)continue;
                bool on=headlamp.car?headlamp.car.State==SimpleCar.Occupancy.Driving:headlamp.traffic&&headlamp.traffic.enabled&&headlamp.traffic.gameObject.activeInHierarchy;
                float distance=listener?Vector3.Distance(headlamp.body.position,listener.position):0;
                float target=on&&lowLight>.12f?HeadlightVisibility(distance)*Mathf.SmoothStep(0,1,lowLight*2):0;
                headlamp.fade=on?Mathf.Lerp(headlamp.fade,target,1-Mathf.Exp(-.05f/.25f)):0;
                headlamp.light.enabled=headlamp.fade>.001f;
                bool braking=headlamp.rear&&headlamp.traffic&&headlamp.traffic.desiredSpeed<headlamp.traffic.speed-.2f;
                headlamp.light.intensity=headlamp.intensity*headlamp.fade*(braking?3:1);
                if(!headlamp.rear&&headlamp.light.enabled)ActiveHeadlights++;
                // The visible lens stays lit even when its distant contribution fades away.
                if(headlamp.emitter)headlamp.emitter.SetColor("_EmissiveColor",on&&lowLight>.12f?new Color(1,.95f,.86f)*750:headlamp.idleEmission);
                if(headlamp.lens){headlamp.block.SetColor("_EmissiveColor",on&&lowLight>.12f?(headlamp.rear?new Color(1,.035f,.012f)*(braking?6:2):new Color(1,.95f,.86f)*750):headlamp.lens.sharedMaterial.GetColor("_EmissiveColor"));headlamp.lens.SetPropertyBlock(headlamp.block);}
            }
            if(fog!=null)
            {
                float targetDistance=Mathf.Lerp(Mathf.Lerp(600,240,cloud),85,world.FogBlend),targetHeight=Mathf.Lerp(55,9,world.FogBlend);
                float blend=1-Mathf.Exp(-.05f/4);fogDistance=Mathf.Lerp(fogDistance,targetDistance,blend);fogHeight=Mathf.Lerp(fogHeight,targetHeight,blend);
                fog.meanFreePath.Override(fogDistance);fog.tint.Override(Color.Lerp(new Color(.79f,.86f,.94f),new Color(.63f,.67f,.70f),cloud));fog.maximumHeight.Override(fogHeight);
            }
            if(clouds!=null)
            {
                // A single custom cloud field keeps spatial identity across weather changes.
                clouds.enable.Override(true);
                clouds.densityMultiplier.Override(Mathf.Lerp(.28f,.52f,cloud));
                clouds.shapeFactor.Override(Mathf.Lerp(.96f,.48f,Mathf.SmoothStep(0,1,cloud)));
                clouds.erosionFactor.Override(Mathf.Lerp(.84f,.58f,cloud));
                clouds.shapeScale.Override(5);
                clouds.bottomAltitude.Override(1600);
                clouds.altitudeRange.Override(2200);
                clouds.multiScattering.Override(.65f);
            }
            if(sky!=null)
            {
                sky.aerosolDensity.Override(Mathf.Lerp(.0025f,.0045f,cloud));
                sky.aerosolTint.Override(new Color(.88f,.92f,1));
                sky.airTint.Override(Color.white);
                float stellarNight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-2,-8,elevation));
                StarVisibility=stellarNight*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.80f,cloud)))*(1-world.FogBlend);
                sky.spaceEmissionMultiplier.Override(.22f*StarVisibility);
            }
            ActiveStreetLights=0;
            foreach(var state in lamps)if(state.light)
            {
                float factor=state.light.name.Contains("underbridge")?Mathf.Lerp(.65f,1,lowLight):lowLight;
                float distance=listener?Vector3.Distance(state.light.transform.position,listener.position):0;
                float visibility=state.street||state.bounce?StreetlightVisibility(distance):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(110,210,distance));
                state.fade=Mathf.Lerp(state.fade,state.enabled?factor*visibility:0,1-Mathf.Exp(-.05f/.3f));
                state.light.enabled=state.fade>.001f;state.light.intensity=state.intensity*state.fade;
                if(state.emitter)state.emitter.SetColor("_EmissiveColor",state.emission*state.fade);
                if(state.street&&state.light.enabled)ActiveStreetLights++;
            }
            foreach(var state in windowMaterials)
            {
                // Selected existing furnished apartments only. Stagger their
                // evening onset and dim one group as households go to sleep.
                float onset=night*Mathf.SmoothStep(0,1,night*(1.25f-state.group*.12f));
                float bedtime=world.Hour<5||world.Hour>23.5f?(state.group==0?.18f:.60f):1;
                state.material.SetColor("_EmissiveColor",state.lamp?state.emission*night+new Color(1,.70f,.43f)*night*120:state.emission+(state.shop?new Color(.70f,.83f,1)*.65f:new Color(1,.63f,.32f)*8)*onset*bedtime);
                state.material.SetColor("_BaseColor",Color.Lerp(state.color,state.color*new Color(1.10f,.95f,.73f),onset));
            }
            LitWindowCount=night>.20f?authoredWindowCount:0;
            foreach(var state in wetMaterials)
            {
                if(state.shaderWet)continue;
                float w=world.Wetness*state.exposure;
                float damp=(state.soil?Mathf.Max(world.Wetness,world.SoilMoisture*.85f):world.Wetness)*state.exposure;
                for(int n=0;n<state.colors.Length;n++){string suffix=state.layered?n.ToString():"";state.material.SetColor("_BaseColor"+suffix,state.colors[n]*Mathf.Lerp(1,.74f,damp));state.material.SetFloat("_Smoothness"+suffix,Mathf.Lerp(state.smoothness[n],Mathf.Max(state.smoothness[n],state.layered ? .32f : .65f),w));state.material.SetFloat("_SmoothnessRemapMin"+suffix,Mathf.Lerp(state.min[n],.20f,w));state.material.SetFloat("_SmoothnessRemapMax"+suffix,Mathf.Lerp(state.max[n],.46f,w));}
            }
            Shader.SetGlobalFloat("_ZRWeatherWetness",world.Wetness);Shader.SetGlobalFloat("_ZRWeatherSoilMoisture",world.SoilMoisture);Shader.SetGlobalFloat("_ZRWeatherRain",world.RainIntensity);Shader.SetGlobalFloat("_ZRWeatherPuddles",world.PuddleAmount);Shader.SetGlobalFloat("_ZRWeatherDaylight",day);
        }
        void UpdateRain()
        {
            var world=WorldWeatherClock.Instance;if(!rainMesh||!listener||!world)return;
            float dt=Mathf.Min(.08f,Time.unscaledDeltaTime),rain=world.RainIntensity;
            int target=Mathf.RoundToInt(rain*DropLimit);spawnRemainder+=rain*900*dt;
            int budget=Mathf.Min(24,Mathf.FloorToInt(spawnRemainder));spawnRemainder-=budget;
            RainDropCount=0;var cameraRight=listener.right;Vector3 wind=new Vector3(.65f,0,.22f)*world.WindStrength*world.GustStrength;
            for(int i=0;i<DropLimit;i++)
            {
                var drop=drops[i];int v=i*4;
                if(drop.alive)
                {
                    drop.position+=(Vector3.down*drop.speed+wind)*dt;
                    if(drop.position.y<drop.floor+drop.length&&rain>.05f&&surfaceWater)surfaceWater.Impact(new Vector3(drop.position.x,drop.floor,drop.position.z));
                    if(drop.position.y<drop.floor+drop.length||Vector3.SqrMagnitude(drop.position-listener.position)>45*45||rain<.005f)drop.alive=false;
                }
                if(!drop.alive&&budget>0&&RainDropCount<target)
                {
                    budget--;Vector3 p=listener.position+new Vector3(Range(-23,23),Range(3,14),Range(-23,23));
                    if(Physics.Raycast(new Vector3(p.x,100,p.z),Vector3.down,out var hit,160,1,QueryTriggerInteraction.Ignore)&&p.y>hit.point.y+.7f)
                    {
                        float stop=hit.point.y;
                        // Conservative second sample clips wind drift entering
                        // a roof edge before its next particle respawn.
                        Vector3 destination=p+wind*1.5f;
                        if(Physics.Raycast(new Vector3(destination.x,100,destination.z),Vector3.down,out var ahead,160,1,QueryTriggerInteraction.Ignore))stop=Mathf.Max(stop,ahead.point.y);
                        drop=new Drop{position=p,floor=stop,speed=Range(13,18),length=Range(.12f,.32f),width=Range(.005f,.010f),alpha=Range(.18f,.38f),alive=p.y>stop+.7f};
                    }
                }
                if(drop.alive)
                {
                    RainDropCount++;Vector3 half=cameraRight*drop.width,tail=Vector3.up*drop.length-wind*.025f;
                    rainVertices[v]=drop.position-half;rainVertices[v+1]=drop.position+half;rainVertices[v+2]=drop.position+tail+half;rainVertices[v+3]=drop.position+tail-half;
                    float distance=Vector3.Distance(listener.position,drop.position),fade=Mathf.Clamp01((34-distance)/12)*Mathf.Clamp01((drop.position.y-drop.floor-.1f)*2);
                    float illumination=Mathf.Lerp(.38f,1,world.Daylight);
                    var color=new Color(.62f*illumination,.70f*illumination,.78f*illumination,drop.alpha*fade*Mathf.Clamp01(rain*3));for(int n=0;n<4;n++)rainColors[v+n]=color;
                }
                else for(int n=0;n<4;n++){rainVertices[v+n]=listener.position;rainColors[v+n]=Color.clear;}
                drops[i]=drop;
            }
            rainMesh.vertices=rainVertices;rainMesh.colors=rainColors;rainMesh.bounds=new Bounds(listener.position,Vector3.one*90);
        }
        void Flash(float strength){lightningUntil=Time.unscaledTime+.16f;lightningStrength=Mathf.Clamp01(strength);applyAt=0;}
        float Range(float min,float max)=>min+(max-min)*(float)random.NextDouble();
        void OnDisable()
        {
            StopAllCoroutines();if(dippedBeamCookie)Destroy(dippedBeamCookie);if(streetCookie)Destroy(streetCookie);if(surfaceWater)Destroy(surfaceWater);Shader.SetGlobalFloat("_ZRWeatherWind",0);Shader.SetGlobalFloat("_ZRWeatherEnabled",0);Shader.SetGlobalFloat("_ZRWeatherWetness",0);Shader.SetGlobalFloat("_ZRWeatherSoilMoisture",0);Shader.SetGlobalFloat("_ZRWeatherPuddles",0);
            if(weatherAudio)weatherAudio.Lightning-=Flash;
            if(sun){sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=sunOriginalIntensity;sun.enabled=sunOriginalEnabled;sun.shadows=sunOriginalShadows;if(sunData)sunData.interactsWithSky=originalSunSky;}
            foreach(var lamp in lamps)if(lamp.light){lamp.light.intensity=lamp.intensity;lamp.light.enabled=lamp.enabled;}
            foreach(var lamp in createdLamps)if(lamp)Destroy(lamp);
            foreach(var lamp in vehicleLights)if(lamp.lens)lamp.lens.SetPropertyBlock(lamp.original);
            foreach(var state in renderers)if(state.renderer)state.renderer.sharedMaterials=state.original;
            if(atmosphere&&originalProfile)atmosphere.sharedProfile=originalProfile;
            foreach(var material in ownedMaterials)if(material)Destroy(material);
            foreach(var component in ownedComponents)if(component)Destroy(component);
            if(weatherProfile)Destroy(weatherProfile);if(roofMap)Destroy(roofMap);if(rainMesh)Destroy(rainMesh);if(puddleMesh)Destroy(puddleMesh);if(rainObject)Destroy(rainObject);if(puddleObject)Destroy(puddleObject);
            Initialized=false;
        }
    }
}
