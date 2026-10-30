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
        [Min(0)] public float nightMoonLux=4,nightFillLux=1.2f;
        [Tooltip("Minimum camera EV at night; the solar exposure curve can remain slightly higher.")]
        public float nightExposureEV=0;
        public int RainDropCount { get; private set; }
        public int WetMaterialCount => wetMaterials.Count;
        public int PuddleCount { get; private set; }
        public int LitWindowCount { get; private set; }
        public float SunIntensity => sun ? sun.intensity : 0;
        public float MoonIntensity => moon ? moon.intensity : 0;
        public float Exposure => exposure != null ? exposure.fixedExposure.value : 0;
        public bool RoofMapReady { get; private set; }
        public bool Initialized { get; private set; }
        public float NightLightBlend { get; private set; }
        public float LightningBoostLux { get; private set; }
        public int ActiveHeadlights { get; private set; }
        const int DropLimit=640, AtlasSize=256;
        static readonly Vector4 AtlasBounds=new Vector4(-160,-150,1f/340,1f/300);
        struct Drop { public Vector3 position;public float floor,speed,length,width,alpha;public bool alive; }
        sealed class WetMaterial
        {
            public Material material; public Color[] colors; public float[] smoothness,min,max;
            public bool shaderWet,layered,soil;public float exposure=1;
        }
        sealed class WindowMaterial { public Material material;public Color emission,color;public int group;public bool lamp; }
        sealed class RendererMaterials { public Renderer renderer;public Material[] original; }
        sealed class LightState { public Light light;public float intensity;public bool enabled;public bool street; }
        sealed class VehicleLight
        {
            public Light light;public Renderer lens;public MaterialPropertyBlock original,block;
            public Transform body;public SimpleCar car;public TrafficVehicle traffic;public float intensity;
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
        Volume atmosphere;VolumeProfile originalProfile,weatherProfile;Light sun,moon,nightFill;Quaternion sunRotation;Color sunColor;float sunOriginalIntensity;bool sunOriginalEnabled;LightShadows sunOriginalShadows;
        Exposure exposure;Fog fog;VolumetricClouds clouds;PhysicallyBasedSky sky;
        Transform listener;Camera view;Terrain terrain;WestbruchWeatherAudio weatherAudio;
        float applyAt,spawnRemainder,lightningUntil,lightningStrength,fogDistance=340,fogHeight=50;int authoredWindowCount;bool moonOwnsShadows;
        VolumetricClouds.CloudPresets currentCloudPreset=(VolumetricClouds.CloudPresets)(-1);

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
                if(clouds!=null){clouds.cloudControl.Override(VolumetricClouds.CloudControl.Simple);clouds.cloudSimpleMode.Override(VolumetricClouds.CloudSimpleMode.Quality);}
                if(sky!=null){sky.updateMode.Override(EnvironmentUpdateMode.Realtime);sky.updatePeriod.Override(1);}
            }
            var sunObject=GameObject.Find("Late afternoon sun");sun=sunObject?sunObject.GetComponent<Light>():null;
            if(sun){sunRotation=sun.transform.rotation;sunColor=sun.color;sunOriginalIntensity=sun.intensity;sunOriginalEnabled=sun.enabled;sunOriginalShadows=sun.shadows;}
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
                    bool wet=IsWeatherSurface(name),window=name.StartsWith("UrbanLife_InteriorWall_",StringComparison.Ordinal)||name.StartsWith("UrbanLife_Curtain_",StringComparison.Ordinal)||name=="StreetlightFrostedGlass"||name=="Atmosphere_WarmBulkheadLens";
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
                            state=new WindowMaterial{material=clone,color=clone.GetColor("_BaseColor"),emission=clone.GetColor("_EmissiveColor"),group=windowClones.Count%3,lamp=name=="StreetlightFrostedGlass"||name=="Atmosphere_WarmBulkheadLens"};
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
                var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.range=15;light.spotAngle=138;light.innerSpotAngle=95;light.shadows=LightShadows.None;light.useColorTemperature=true;light.colorTemperature=3300;light.lightUnit=LightUnit.Lumen;
                obj.AddComponent<HDAdditionalLightData>().volumetricDimmer=0;
                light.intensity=LightUnitUtils.ConvertIntensity(light,3800,LightUnit.Lumen,LightUnit.Candela);
                lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=true,street=true});createdLamps.Add(obj);
            }
            foreach(var light in FindObjectsByType<Light>())
            {
                if(light.type==LightType.Directional||light.name=="Timed existing streetlamp")continue;
                // Existing bridge lamps stay gently lit all day. Timed entrance
                // and street lamps use their original physical intensity.
                if(light.name.Contains("entrance light")||light.name.Contains("Street")||light.name.Contains("street")||light.name.Contains("Lantern")||light.name.Contains("Restrained underbridge"))
                    lamps.Add(new LightState{light=light,intensity=light.intensity,enabled=light.enabled});
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
            // An elevated moon remains independent of the horizon sun during
            // twilight, so shaded streets never lose both light sources.
            var moonObject=new GameObject("Weather moonlight");moonObject.transform.SetParent(transform,false);
            moon=moonObject.AddComponent<Light>();moon.type=LightType.Directional;moon.shadows=LightShadows.None;moon.color=new Color(.53f,.63f,.84f);moon.lightUnit=LightUnit.Lux;moon.intensity=0;
            var hdMoon=moonObject.AddComponent<HDAdditionalLightData>();hdMoon.angularDiameter=.5f;createdLamps.Add(moonObject);
            var fillObject=new GameObject("Weather diffuse urban night fill");fillObject.transform.SetParent(transform,false);
            fillObject.transform.rotation=Quaternion.Euler(65,135,0);
            nightFill=fillObject.AddComponent<Light>();nightFill.type=LightType.Directional;nightFill.shadows=LightShadows.None;nightFill.color=new Color(.45f,.56f,.73f);nightFill.lightUnit=LightUnit.Lux;nightFill.intensity=0;
            var hdFill=fillObject.AddComponent<HDAdditionalLightData>();hdFill.affectSpecular=false;hdFill.volumetricDimmer=0;createdLamps.Add(fillObject);
            trafficBodies=FindObjectsByType<TrafficVehicle>();
            foreach(var car in FindObjectsByType<SimpleCar>())MountHeadlights(car.transform,car,null);
            foreach(var traffic in trafficBodies)MountHeadlights(traffic.transform,null,traffic);
        }
        void MountHeadlights(Transform body,SimpleCar car,TrafficVehicle traffic)
        {
            foreach(var lens in body.GetComponentsInChildren<Renderer>())
            {
                if(lens.name!="Headlamp with inset reflector")continue;
                var obj=new GameObject("Mounted automatic dipped headlamp");obj.transform.SetParent(body,false);
                obj.transform.position=lens.transform.position+body.forward*.045f;
                obj.transform.localRotation=Quaternion.Euler(2.2f,0,0);
                var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.range=42;light.spotAngle=48;light.innerSpotAngle=30;light.color=new Color(1,.91f,.74f);light.shadows=car?LightShadows.Soft:LightShadows.None;light.lightUnit=LightUnit.Lumen;
                var hd=obj.AddComponent<HDAdditionalLightData>();hd.volumetricDimmer=0;hd.fadeDistance=50;
                light.intensity=LightUnitUtils.ConvertIntensity(light,1150,LightUnit.Lumen,LightUnit.Candela);light.enabled=false;
                var original=new MaterialPropertyBlock();lens.GetPropertyBlock(original);var block=new MaterialPropertyBlock();lens.GetPropertyBlock(block);
                vehicleLights.Add(new VehicleLight{light=light,lens=lens,original=original,block=block,body=body,car=car,traffic=traffic,intensity=light.intensity});createdLamps.Add(obj);
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
            if(!puddleMaterial)return;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            for(int z=-90;z<=100;z+=10)for(int x=-110;x<=155;x+=10)
            {
                if(PuddleCount>=80||random.NextDouble()>.35)continue;
                var p=new Vector3(x+Range(-3,3),30,z+Range(-3,3));
                if(!Physics.Raycast(p,Vector3.down,out var hit,45,1,QueryTriggerInteraction.Ignore)||hit.normal.y<.995f||hit.point.y>1.5f||IsSheltered(hit.point))continue;
                var renderer=hit.collider.GetComponent<Renderer>();if(!renderer)continue;
                bool paved=false;foreach(var m in renderer.sharedMaterials)if(m&&(m.name.StartsWith("Asphalt",StringComparison.Ordinal)||m.name.StartsWith("Paving",StringComparison.Ordinal)||m.name.StartsWith("RoadRepair",StringComparison.Ordinal)))paved=true;
                if(!paved)continue;
                const int sides=18;var ring=new Vector3[sides];float rx=Range(.6f,1.75f),rz=Range(.45f,1.25f),phase=Range(0,6.28f);bool flat=true;
                for(int n=0;n<sides;n++)
                {
                    float a=n*Mathf.PI*2/sides,warp=.82f+.13f*Mathf.Sin(a*3+phase)+.07f*Mathf.Sin(a*5-phase);
                    var v=hit.point+new Vector3(Mathf.Cos(a)*rx*warp,0,Mathf.Sin(a)*rz*warp);
                    if(!Physics.Raycast(v+Vector3.up*.2f,Vector3.down,out var edge,.45f,1,QueryTriggerInteraction.Ignore)||Mathf.Abs(edge.point.y-hit.point.y)>.025f||edge.normal.y<.995f){flat=false;break;}
                    ring[n]=v+Vector3.up*.014f;
                }
                if(!flat)continue;
                int start=vertices.Count;vertices.Add(hit.point+Vector3.up*.014f);uv.Add(Vector2.zero);colors.Add(Color.white);
                for(int n=0;n<sides;n++){float a=n*Mathf.PI*2/sides;vertices.Add(ring[n]);uv.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a)));colors.Add(Color.white);triangles.Add(start);triangles.Add(start+1+(n+1)%sides);triangles.Add(start+1+n);}
                PuddleCount++;
            }
            puddleObject=new GameObject("Weather shallow surface water · evaporating");puddleObject.transform.SetParent(transform,false);puddleObject.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            puddleMesh=new Mesh{name="Ground checked shallow puddles"};puddleMesh.SetVertices(vertices);puddleMesh.SetUVs(0,uv);puddleMesh.SetColors(colors);puddleMesh.SetTriangles(triangles,0);puddleMesh.RecalculateBounds();
            puddleObject.AddComponent<MeshFilter>().sharedMesh=puddleMesh;var puddleRenderer=puddleObject.AddComponent<MeshRenderer>();puddleRenderer.sharedMaterial=puddleMaterial;puddleRenderer.shadowCastingMode=ShadowCastingMode.Off;puddleRenderer.receiveShadows=false;
        }
        void Update()
        {
            if(!Initialized)return;
            if(Time.unscaledTime>=applyAt){applyAt=Time.unscaledTime+.15f;ApplyWorld();}
            UpdateRain();
        }
        void ApplyWorld()
        {
            var world=WorldWeatherClock.Instance;if(!world)return;
            float day=world.Daylight,cloud=Mathf.Clamp01(world.CloudCoverage),night=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.40f,day));
            NightLightBlend=night;
            float flash=Time.unscaledTime<lightningUntil?lightningStrength:0;
            float elevation=Mathf.Sin((world.Hour-5.2f)/(20.6f-5.2f)*Mathf.PI)*58;
            float solar=Mathf.Sqrt(Mathf.Clamp01(elevation/58));
            float solarLux=Mathf.Lerp(.55f,85000*Mathf.Pow(day,1.15f)*solar*Mathf.Lerp(1,.17f,cloud),Mathf.Clamp01(day*8));
            float solarEV=Mathf.Clamp(12+.65f*Mathf.Log(Mathf.Max(.55f,solarLux)/85000,2),nightExposureEV,12);
            float moonEV=Mathf.Clamp(12+.65f*Mathf.Log(.55f/85000,2),nightExposureEV,12);
            float adaptedEV=Mathf.Lerp(solarEV,moonEV,night);
            // Exposure adapts with moonlight. Compensate the fading sun by
            // the same EV difference to avoid overexposed dusk-lit facades.
            float solarCompensation=Mathf.Pow(2,adaptedEV-solarEV);
            // Retain an adapted, readable sky-bounce floor as direct sunlight
            // disappears. Both endpoints remain unchanged; the exposure
            // compensation prevents a second dark trough between them.
            float ambientBlend=Mathf.SmoothStep(0,1,Mathf.Clamp01(night*4));
            float moonCompensation=Mathf.Pow(2,adaptedEV-moonEV);
            float visibleSolarLux=solarLux*(1-night)*solarCompensation;
            float visibleMoonLux=nightMoonLux*ambientBlend*moonCompensation;
            if(moonOwnsShadows&&visibleSolarLux>visibleMoonLux*1.2f)moonOwnsShadows=false;
            else if(!moonOwnsShadows&&visibleMoonLux>visibleSolarLux*1.2f)moonOwnsShadows=true;
            float azimuth=world.Hour/24*360-180;
            LightningBoostLux=flash*Mathf.Lerp(28,9000,day);
            if(sun)
            {
                sun.transform.rotation=Quaternion.Euler(Mathf.Max(-15,elevation),azimuth,0);
                sun.color=Color.Lerp(new Color(.53f,.63f,.84f),Color.Lerp(new Color(1,.69f,.43f),new Color(1,.97f,.91f),Mathf.SmoothStep(0,1,day)),Mathf.SmoothStep(0,1,day*4));
                sun.intensity=visibleSolarLux+LightningBoostLux*(1-night);
                // HDRP has one cascaded directional shadow atlas. Transfer
                // it to the moon only once the night light dominates.
                sun.shadows=moonOwnsShadows?LightShadows.None:sunOriginalShadows;
                sun.enabled=true;
            }
            if(moon)
            {
                moon.transform.rotation=Quaternion.Euler(30,azimuth+180,0);moon.intensity=visibleMoonLux+LightningBoostLux*night;moon.enabled=night>.005f;
                moon.shadows=moonOwnsShadows?sunOriginalShadows:LightShadows.None;
            }
            if(sun)Shader.SetGlobalVector("_ZRWeatherSunDirection",moonOwnsShadows&&moon?-moon.transform.forward:-sun.transform.forward);
            if(exposure!=null)exposure.fixedExposure.Override(adaptedEV-flash*.35f);
            if(nightFill){nightFill.enabled=night>.005f;nightFill.intensity=nightFillLux*ambientBlend*moonCompensation;}
            ActiveHeadlights=0;
            foreach(var headlamp in vehicleLights)
            {
                if(!headlamp.body||!headlamp.light)continue;
                bool on=headlamp.car?headlamp.car.State==SimpleCar.Occupancy.Driving:headlamp.traffic&&headlamp.traffic.enabled&&headlamp.traffic.gameObject.activeInHierarchy;
                if(on&&headlamp.traffic&&listener)
                {
                    float distance=(headlamp.body.position-listener.position).sqrMagnitude;int closer=0;
                    foreach(var other in trafficBodies)if(other&&other.enabled&&other.gameObject.activeInHierarchy&&(other.transform.position-listener.position).sqrMagnitude<distance)closer++;
                    on=closer<4&&distance<65*65;
                }
                on&=night>.12f;headlamp.light.enabled=on;
                if(on){headlamp.light.intensity=headlamp.intensity*Mathf.SmoothStep(0,1,night*2);ActiveHeadlights++;}
                if(headlamp.lens){headlamp.block.SetColor("_EmissiveColor",on?new Color(1,.91f,.74f)*85:headlamp.lens.sharedMaterial.GetColor("_EmissiveColor"));headlamp.lens.SetPropertyBlock(headlamp.block);}
            }
            if(fog!=null)
            {
                float targetDistance=world.Weather==WeatherKind.Fog?62:Mathf.Lerp(480,world.IsStorm?115:175,cloud),targetHeight=world.Weather==WeatherKind.Fog?11:Mathf.Lerp(50,65,cloud);
                float blend=1-Mathf.Exp(-.15f/4);fogDistance=Mathf.Lerp(fogDistance,targetDistance,blend);fogHeight=Mathf.Lerp(fogHeight,targetHeight,blend);
                fog.meanFreePath.Override(fogDistance);fog.tint.Override(Color.Lerp(new Color(.79f,.86f,.94f),new Color(.63f,.67f,.70f),cloud));fog.maximumHeight.Override(fogHeight);
            }
            if(clouds!=null)
            {
                // Density alone does not alter the cloud map's spatial
                // coverage. HDRP's actual weather maps supply an overcast
                // ceiling for rain and preserve gaps in fair weather.
                var preset=world.IsStorm&&cloud>.88f?VolumetricClouds.CloudPresets.Stormy:cloud>.72f?VolumetricClouds.CloudPresets.Overcast:cloud>.30f?VolumetricClouds.CloudPresets.Cloudy:VolumetricClouds.CloudPresets.Sparse;
                if(preset!=currentCloudPreset){currentCloudPreset=preset;clouds.cloudPreset=preset;}
                clouds.enable.Override(cloud>.16f);clouds.densityMultiplier.Override(Mathf.Lerp(.10f,.72f,cloud));
            }
            if(sky!=null){sky.aerosolDensity.Override(Mathf.Lerp(.003f,.014f,cloud));sky.airTint.Override(Color.Lerp(Color.white,new Color(.91f,.94f,.98f),cloud));}
            // Keep only eight nearest existing street fixtures active; all
            // lenses still glow. No extra shadows or distant light flood.
            foreach(var state in lamps)if(state.light)
            {
                float factor=state.light.name.Contains("underbridge")?Mathf.Lerp(.65f,1,night):night;
                bool nearby=true;
                if(state.street&&listener)
                {
                    float distance=(state.light.transform.position-listener.position).sqrMagnitude;int closer=0;
                    foreach(var other in lamps)if(other.street&&other.light&&(other.light.transform.position-listener.position).sqrMagnitude<distance)closer++;
                    nearby=closer<8&&distance<65*65;
                }
                state.light.enabled=state.enabled&&factor>.005f&&nearby;state.light.intensity=state.intensity*factor;
            }
            foreach(var state in windowMaterials)
            {
                // Selected existing furnished apartments only. Stagger their
                // evening onset and dim one group as households go to sleep.
                float onset=night*Mathf.SmoothStep(0,1,night*(1.25f-state.group*.12f));
                float bedtime=world.Hour<5||world.Hour>23.5f?(state.group==0?.18f:.60f):1;
                state.material.SetColor("_EmissiveColor",state.lamp?state.emission*night+new Color(1,.70f,.43f)*night*120:state.emission+new Color(1,.63f,.32f)*onset*bedtime*8);
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
            int target=Mathf.RoundToInt(rain*DropLimit);spawnRemainder+=rain*570*dt;
            int budget=Mathf.Min(24,Mathf.FloorToInt(spawnRemainder));spawnRemainder-=budget;
            RainDropCount=0;var cameraRight=listener.right;Vector3 wind=new Vector3(.65f,0,.22f)*world.WindStrength;
            for(int i=0;i<DropLimit;i++)
            {
                var drop=drops[i];int v=i*4;
                if(drop.alive)
                {
                    drop.position+=(Vector3.down*drop.speed+wind)*dt;
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
                        drop=new Drop{position=p,floor=stop,speed=Range(13,18),length=Range(.20f,.48f),width=Range(.007f,.012f),alpha=Range(.10f,.23f),alive=p.y>stop+.7f};
                    }
                }
                if(drop.alive)
                {
                    RainDropCount++;Vector3 half=cameraRight*drop.width,tail=Vector3.up*drop.length-wind*.025f;
                    rainVertices[v]=drop.position-half;rainVertices[v+1]=drop.position+half;rainVertices[v+2]=drop.position+tail+half;rainVertices[v+3]=drop.position+tail-half;
                    float distance=Vector3.Distance(listener.position,drop.position),fade=Mathf.Clamp01((34-distance)/12)*Mathf.Clamp01((drop.position.y-drop.floor-.1f)*2);
                    float illumination=Mathf.Lerp(.12f,1,world.Daylight);
                    var color=new Color(.48f*illumination,.54f*illumination,.59f*illumination,drop.alpha*fade*Mathf.Clamp01(rain*3));for(int n=0;n<4;n++)rainColors[v+n]=color;
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
            StopAllCoroutines();Shader.SetGlobalFloat("_ZRWeatherEnabled",0);Shader.SetGlobalFloat("_ZRWeatherWetness",0);Shader.SetGlobalFloat("_ZRWeatherSoilMoisture",0);Shader.SetGlobalFloat("_ZRWeatherPuddles",0);
            if(weatherAudio)weatherAudio.Lightning-=Flash;
            if(sun){sun.transform.rotation=sunRotation;sun.color=sunColor;sun.intensity=sunOriginalIntensity;sun.enabled=sunOriginalEnabled;sun.shadows=sunOriginalShadows;}
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
