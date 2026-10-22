using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    public static class WestbruchAtmospherePass
    {
        public const string RootName="Westbruch light and acoustic atmosphere";
        [MenuItem("ZERO RISE/Westbruch/Apply light and sound atmosphere")]
        public static void Apply()
        {
            Directory.CreateDirectory("QA");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit play mode before applying atmosphere.");
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var district=GameObject.Find("Westbruch – authored district");if(!district)throw new Exception("Expected saved Westbruch scene.");
            var old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);
            WestbruchAudioAssets.Prepare();
            var root=Group(RootName,district.transform,Vector3.zero).transform;
            var sound=root.gameObject.AddComponent<WestbruchSoundscape>();
            sound.city=Source("Distant urban traffic bed",root,Vector3.zero,"CityDistant",false,0,0,true);
            sound.wind=Source("Courtyard air and foliage bed",root,Vector3.zero,"CourtyardWind",false,0,0,true);
            // A quiet fridge compressor belongs to the existing grocer, with a
            // closed facade between the listener and its appliances.
            sound.shop=Source("Grocer refrigeration behind facade",root,new Vector3(65,1.2f,41),"ShopCompressor",true,1.5f,19,true);
            sound.drain=Source("Drain water inside existing catchpit",root,new Vector3(114.05f,.30f,65.8f),null,true,1.2f,20,false);
            sound.fence=Source("Occasional wind movement at railway fence",root,new Vector3(136,.75f,55),null,true,1,13,false);
            sound.footsteps=Source("Surface aware player footsteps",root,Vector3.zero,null,true,1.7f,15,false);
            sound.drainClip=WestbruchAudioAssets.Clip("DrainResonance");sound.fenceClip=WestbruchAudioAssets.Clip("FenceTick");
            sound.pavingSteps=Enumerable.Range(0,3).Select(i=>WestbruchAudioAssets.Clip("FootstepPaving"+i)).ToArray();
            sound.soilSteps=Enumerable.Range(0,3).Select(i=>WestbruchAudioAssets.Clip("FootstepSoil"+i)).ToArray();
            sound.cityMuffle=sound.city.gameObject.AddComponent<AudioLowPassFilter>();sound.windMuffle=sound.wind.gameObject.AddComponent<AudioLowPassFilter>();
            var shopMuffle=sound.shop.gameObject.AddComponent<AudioLowPassFilter>();shopMuffle.cutoffFrequency=1200;
            sound.footReflections=Reflections(sound.footsteps);sound.footReflections.enabled=false;
            Reflections(sound.drain);
            WestbruchAtmosphereLighting.Build(root);
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static AudioSource Source(string name,Transform parent,Vector3 position,string clip,bool spatial,float near,float far,bool loop)
        {
            var source=Group(name,parent,position).AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.volume=0;source.spatialBlend=spatial?1:0;
            source.dopplerLevel=0;source.priority=spatial?150:200;source.spread=spatial?35:0;source.bypassReverbZones=true;
            source.clip=clip==null?null:WestbruchAudioAssets.Clip(clip);
            if(spatial){source.minDistance=near;source.maxDistance=far;source.rolloffMode=AudioRolloffMode.Custom;source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.15f,.72f),new Keyframe(.5f,.17f),new Keyframe(1,0)));}
            // One-shot volumes are set through PlayOneShot; bed loops fade in.
            if(!loop)source.volume=1;
            return source;
        }
        static AudioReverbFilter Reflections(AudioSource source)
        {
            var filter=source.gameObject.AddComponent<AudioReverbFilter>();filter.reverbPreset=AudioReverbPreset.User;
            filter.dryLevel=0;filter.room=-2100;filter.roomHF=-1700;filter.roomLF=-1200;filter.decayTime=.66f;filter.decayHFRatio=.58f;
            filter.reflectionsLevel=-1800;filter.reflectionsDelay=.018f;filter.reverbLevel=-1750;filter.reverbDelay=.031f;filter.diffusion=78;filter.density=86;return filter;
        }
        public static void RebuildAndValidate(){Apply();WestbruchOverpassQA.CaptureSaved();}
        public static void Validate(Action<string,bool,string> record)
        {
            var root=GameObject.Find(RootName);if(!root)return;
            WestbruchAudioAssets.Validate(record);var sound=root.GetComponent<WestbruchSoundscape>();
            var sources=root.GetComponentsInChildren<AudioSource>();
            bool routed=sound&&sources.Length==6&&sources.Count(s=>s.spatialBlend==0)==2&&sources.All(s=>!s.playOnAwake&&s.dopplerLevel==0)&&sound.pavingSteps.Length==3&&sound.soilSteps.Length==3;
            record("Soundscape has bounded spatial sources and preserves the sole camera listener",routed&&UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"Sources "+sources.Length+"; listeners "+UnityEngine.Object.FindObjectsByType<AudioListener>().Length);
            record("Bridge acoustics distinguish sheltered road, open street and raised railway",WestbruchSoundscape.BridgeWeight(new Vector3(118,1.8f,80))>.99f&&WestbruchSoundscape.BridgeWeight(new Vector3(100,1.8f,80))==0&&WestbruchSoundscape.BridgeWeight(new Vector3(118,8,80))==0,"Analytic weights checked at underpass, street and railway");
            WestbruchAtmosphereLighting.Validate(record);
        }
    }
}
