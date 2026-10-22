using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // An additive local lighting pass. The existing afternoon sky, street
    // layout and fixed exposure remain authoritative; there is no time jump.
    internal static class WestbruchAtmosphereLighting
    {
        public const string RootName="Westbruch lighting moods";
        public const int VolumeLayer=10,LocalVolumeCount=3,EntranceLightCount=4,BridgeLightCount=6;
        const string Settings="Assets/ZeroRise/Settings/";

        public static void Build(Transform parent)
        {
            var old=parent.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=Group(RootName,parent,Vector3.zero).transform;

            // The global trim is small enough to preserve building paint and
            // daylight visibility. The covered street and courts then blend
            // gently toward cooler shade, rather than switching exposure.
            var baseMood=Profile("Atmosphere_DaylightTrim",.025f,1.5f,-2,2);
            var global=Group("Restrained afternoon colour trim",root,Vector3.zero).AddComponent<Volume>();
            global.gameObject.layer=VolumeLayer;global.isGlobal=true;global.priority=2;global.sharedProfile=baseMood;global.weight=1;
            var bridgeMood=Profile("Atmosphere_BridgeShade",.20f,-2,-3,-6);
            Zone(root,"Soft underbridge light transition",new Vector3(118,2.6f,80),new Vector3(8,5.2f,27),2.8f,bridgeMood,6);
            var courtMood=Profile("Atmosphere_CourtyardShade",.08f,-1,-2,-3);
            Zone(root,"Nordhof sheltered daylight",new Vector3(-39,2.3f,42),new Vector3(95,5,18),5, courtMood,4);
            Zone(root,"Suedhof sheltered daylight",new Vector3(-38,2.3f,-43),new Vector3(94,5,18),5,courtMood,4);

            foreach(var camera in UnityEngine.Object.FindObjectsByType<HDAdditionalCameraData>())
            {
                // Dedicated trigger volumes never join the Default layer used
                // by walking, vehicle suspension or the camera's obstacle cast.
                camera.volumeLayerMask=camera.volumeLayerMask.value|(1<<VolumeLayer);
                EditorUtility.SetDirty(camera);
            }

            var sun=GameObject.Find("Late afternoon sun");
            if(sun&&sun.TryGetComponent<Light>(out var daylight))
            {
                daylight.useColorTemperature=false;daylight.color=new Color(1,.970f,.925f);
                if(sun.TryGetComponent<HDAdditionalLightData>(out var hdSun))hdSun.angularDiameter=.70f;
                EditorUtility.SetDirty(daylight);
            }
            Underbridge();
            Entrances(root);
        }

        static VolumeProfile Profile(string name,float postExposure,float contrast,float saturation,float temperature)
        {
            string path=Settings+name+".asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name=name;AssetDatabase.CreateAsset(profile,path);}
            T Component<T>() where T:VolumeComponent
            {
                if(profile.TryGet<T>(out var component))return component;
                component=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(component,profile);return component;
            }
            var colour=Component<ColorAdjustments>();colour.postExposure.Override(postExposure);colour.contrast.Override(contrast);colour.saturation.Override(saturation);
            var white=Component<WhiteBalance>();white.temperature.Override(temperature);white.tint.Override(0);
            EditorUtility.SetDirty(colour);EditorUtility.SetDirty(white);EditorUtility.SetDirty(profile);return profile;
        }

        static void Zone(Transform parent,string name,Vector3 center,Vector3 size,float blend,VolumeProfile profile,float priority)
        {
            var o=Group(name,parent,center);o.layer=VolumeLayer;
            var box=o.AddComponent<BoxCollider>();box.isTrigger=true;box.size=size;
            var volume=o.AddComponent<Volume>();volume.isGlobal=false;volume.priority=priority;volume.blendDistance=blend;volume.weight=1;volume.sharedProfile=profile;
        }

        static void Underbridge()
        {
            int count=0;
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
            {
                if(light.name!="Restrained underbridge illumination")continue;
                var position=light.transform.position;position.y=5.515f;light.transform.position=position;
                light.transform.rotation=Quaternion.Euler(90,0,0);
                SoftSpot(light,1800,3500,6.8f,110,78,32);
                count++;
            }
            if(count!=BridgeLightCount)throw new InvalidOperationException("Expected the six existing mounted bridge luminaires; found "+count);
            // This material is already shared by the visible bridge batches.
            // It belongs only to these six sealed lenses, so emission and the
            // real downward light now agree without duplicate fixture meshes.
            var lenses=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Overpass_SealedLampDiffuser.mat");
            if(!lenses)throw new InvalidOperationException("Existing sealed bridge diffuser material missing.");
            lenses.SetColor("_EmissiveColor",new Color(.91f,.80f,.61f)*1300);
            HDMaterial.ValidateMaterial(lenses);EditorUtility.SetDirty(lenses);
        }

        static void Entrances(Transform parent)
        {
            var inlay=Material("Atmosphere_WarmBulkheadLens",null,new Color(.83f,.75f,.56f),.16f);
            inlay.SetColor("_EmissiveColor",new Color(.94f,.73f,.46f)*650);
            HDMaterial.ValidateMaterial(inlay);EditorUtility.SetDirty(inlay);
            var ids=new[]{"Uferhof_12","Nordhof_10","Suedhof_4","Pfand_und_Spar"};
            float[] temperatures={3100,3300,3000,3500};
            for(int i=0;i<ids.Length;i++)
            {
                var house=GameObject.Find(ids[i]);var fixture=house?house.transform.Find("Entrance bulkhead"):null;
                if(!fixture)throw new InvalidOperationException("Mounted entrance bulkhead missing: "+ids[i]);
                var lamp=Group("Lit existing entrance bulkhead "+ids[i],parent,parent.InverseTransformPoint(fixture.position)).transform;
                lamp.rotation=fixture.rotation;
                // A thin luminous insert overlaps the existing frosted lens by
                // one millimetre; its bottom is physically attached, not afloat.
                Box("Attached warm bulkhead lens insert",lamp,new Vector3(0,-.0835f,-.0005f),new Vector3(.124f,.004f,.084f),inlay,false,.3f);
                var emitter=Group("Warm entrance light "+ids[i],lamp,new Vector3(0,-.095f,-.025f)).AddComponent<Light>();
                emitter.transform.rotation=Quaternion.LookRotation(fixture.TransformDirection(new Vector3(0,-1,-.25f)),Vector3.up);
                SoftSpot(emitter,240,temperatures[i],3.8f,105,72,24);
            }
        }

        static void SoftSpot(Light light,float lumens,float temperature,float range,float outer,float inner,float fade)
        {
            light.type=LightType.Spot;light.range=range;light.spotAngle=outer;light.innerSpotAngle=inner;
            light.shadows=LightShadows.None;light.useColorTemperature=true;light.colorTemperature=temperature;light.color=Color.white;
            light.enableSpotReflector=true;
            var hd=light.GetComponent<HDAdditionalLightData>();if(!hd)hd=light.gameObject.AddComponent<HDAdditionalLightData>();
            // Unity 6 stores punctual intensity in candela internally even when
            // the inspector displays lumens. Do not assign lumen values to cd.
            light.lightUnit=LightUnit.Lumen;
            light.intensity=LightUnitUtils.ConvertIntensity(light,lumens,LightUnit.Lumen,LightUnit.Candela);
            hd.lightDimmer=1;hd.volumetricDimmer=0;hd.fadeDistance=fade;hd.affectDiffuse=true;hd.affectSpecular=true;
            EditorUtility.SetDirty(light);EditorUtility.SetDirty(hd);
        }

        public static void Validate(Action<string,bool,string> record)
        {
            var root=GameObject.Find(RootName);
            if(!root){record("Westbruch lighting patch is present",false,"Missing "+RootName);return;}
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName);
            var lenses=bridge?bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Sealed warm white diffuser").ToArray():Array.Empty<MeshFilter>();
            var bridgeLights=UnityEngine.Object.FindObjectsByType<Light>().Where(l=>l.name=="Restrained underbridge illumination").ToArray();
            int badBridge=0;float highestMountGap=0,largestLumenError=0;
            foreach(var light in bridgeLights)
            {
                var nearest=lenses.OrderBy(f=>Vector3.SqrMagnitude(f.transform.position-light.transform.position)).FirstOrDefault();
                float gap=nearest?Vector3.Distance(nearest.transform.position,light.transform.position):100;
                highestMountGap=Mathf.Max(highestMountGap,gap);
                bool physical=PhysicalSpot(light,1800,out float error);
                if(gap>.05f||Vector3.Dot(light.transform.forward,Vector3.down)<.999f||!physical)badBridge++;
                largestLumenError=Mathf.Max(largestLumenError,error);
            }
            record("Bridge lights are attached to the existing lenses and emit downward in physical units",bridgeLights.Length==BridgeLightCount&&lenses.Length==BridgeLightCount&&badBridge==0,
                "Lights "+bridgeLights.Length+"; lenses "+lenses.Length+"; bad mounts/settings "+badBridge+"; maximum centre offset "+highestMountGap.ToString("F4")+"m; lumen error "+largestLumenError.ToString("F3"));

            var entrances=root.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Warm entrance light ",StringComparison.Ordinal)).ToArray();
            int badEntrances=0;
            foreach(var light in entrances)
            {
                string id=light.name.Substring("Warm entrance light ".Length);var house=GameObject.Find(id);var fixture=house?house.transform.Find("Entrance bulkhead"):null;
                if(!fixture||Vector3.Distance(light.transform.position,fixture.TransformPoint(new Vector3(0,-.095f,-.025f)))>.002f||Vector3.Dot(light.transform.forward,Vector3.down)<.95f||!PhysicalSpot(light,240,out _))badEntrances++;
            }
            int inserts=root.GetComponentsInChildren<MeshFilter>().Count(f=>f.name=="Attached warm bulkhead lens insert");
            record("Selected entrance lights belong to real mounted bulkheads",entrances.Length==EntranceLightCount&&inserts==EntranceLightCount&&badEntrances==0,
                "Warm lights "+entrances.Length+"; attached inserts "+inserts+"; bad mounts/settings "+badEntrances);

            var volumes=root.GetComponentsInChildren<Volume>();var local=volumes.Where(v=>!v.isGlobal).ToArray();
            bool volumeSettings=volumes.Length==LocalVolumeCount+1&&local.Length==LocalVolumeCount;
            foreach(var volume in volumes)
            {
                volumeSettings&=volume.gameObject.layer==VolumeLayer&&volume.sharedProfile&&volume.weight==1;
                if(!volume.sharedProfile)continue;
                bool colourFound=volume.sharedProfile.TryGet<ColorAdjustments>(out var colour),whiteFound=volume.sharedProfile.TryGet<WhiteBalance>(out var white);
                volumeSettings&=colourFound&&whiteFound;
                if(colourFound)volumeSettings&=colour.postExposure.overrideState&&colour.postExposure.value>=0&&colour.postExposure.value<=.25f&&Mathf.Abs(colour.contrast.value)<=5;
                if(whiteFound)volumeSettings&=Mathf.Abs(white.temperature.value)<=8;
                if(!volume.isGlobal){var box=volume.GetComponent<BoxCollider>();volumeSettings&=box&&box.isTrigger&&volume.blendDistance>=2.5f;}
            }
            var colliders=root.GetComponentsInChildren<Collider>();
            bool collisionSafe=colliders.Length==LocalVolumeCount&&colliders.All(c=>c.isTrigger&&c.gameObject.layer==VolumeLayer&&(1&(1<<c.gameObject.layer))==0);
            var cameras=UnityEngine.Object.FindObjectsByType<HDAdditionalCameraData>();
            bool masks=cameras.Length>0&&cameras.All(c=>(c.volumeLayerMask.value&1)!=0&&(c.volumeLayerMask.value&(1<<VolumeLayer))!=0);
            record("Lighting zones blend through dedicated nonblocking HDRP volume triggers",volumeSettings&&collisionSafe&&masks,
                "Local/global volumes "+local.Length+"/"+(volumes.Length-local.Length)+"; layer "+VolumeLayer+"; trigger-only colliders "+collisionSafe+"; default and lighting camera masks "+masks);

            var sun=GameObject.Find("Late afternoon sun");var sourceAtmosphere=GameObject.Find("Westbruch atmosphere");
            var original=sourceAtmosphere?sourceAtmosphere.GetComponent<Volume>():null;
            bool timePreserved=sun&&sun.TryGetComponent<Light>(out var daylight)&&Mathf.Abs(daylight.intensity-65000)<1&&Quaternion.Angle(sun.transform.rotation,Quaternion.Euler(36,-42,0))<.01f;
            bool exposurePreserved=original&&original.sharedProfile&&original.sharedProfile.TryGet<Exposure>(out var exposure)&&exposure.mode.value==ExposureMode.Fixed&&Mathf.Abs(exposure.fixedExposure.value-12)<.001f;
            record("Afternoon sun and stable global exposure are preserved",timePreserved&&exposurePreserved,"Sun 36/-42 degrees and 65000 lux "+timePreserved+"; original fixed EV12 "+exposurePreserved);
        }

        static bool PhysicalSpot(Light light,float lumens,out float error)
        {
            error=100000;
            if(light.type!=LightType.Spot)return false;
            error=Mathf.Abs(LightUnitUtils.ConvertIntensity(light,light.intensity,LightUnit.Candela,LightUnit.Lumen)-lumens);
            var hd=light.GetComponent<HDAdditionalLightData>();
            return hd&&light.lightUnit==LightUnit.Lumen&&error<1&&light.useColorTemperature&&light.colorTemperature>=2800&&light.colorTemperature<=3700&&light.shadows==LightShadows.None&&hd.volumetricDimmer==0&&light.innerSpotAngle<light.spotAngle;
        }
    }
}
