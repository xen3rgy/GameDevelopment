using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class WorldWeatherAssets
    {
        const string RootName="Westbruch weather and daily rhythm";
        const string Materials="Assets/ZeroRise/Settings/Weather/";
        [MenuItem("ZERO RISE/Westbruch/Install weather and daily rhythm")]
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var root=GameObject.Find(RootName);if(!root)root=new GameObject(RootName);
            T Component<T>() where T:Component=>root.GetComponent<T>()??root.AddComponent<T>();
            Component<WorldWeatherClock>();Component<CityDailyRhythm>();
            var visuals=Component<WestbruchWeatherVisuals>();var audio=Component<WestbruchWeatherAudio>();
            Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
            visuals.rainMaterial=Material("Rain","ZeroRise/WeatherRain");visuals.puddleMaterial=WeatherSurfaceAssets.Prepare();
            AudioClip Clip(string path)
            {
                string full="Assets/ZeroRise/Art/Audio/"+path+".wav";
                if(AssetImporter.GetAtPath(full) is AudioImporter importer)
                {
                    var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;
                    importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=false;importer.SaveAndReimport();
                }
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(full);if(!clip)throw new Exception("Weather clip missing: "+full);return clip;
            }
            audio.rainRoad=Clip("WeatherSfx/RainRoad");audio.rainRoof=Clip("WeatherSfx/RainRoof");audio.rainLeaves=Clip("WeatherSfx/RainLeaves");
            audio.thunder=Clip("WeatherSfx/Thunder");audio.wetTyres=Clip("WeatherSfx/WetTyres");audio.wind=Clip("GameSfx/LeavesAir");
            audio.tailDrips=new[]{Clip("GameSfx/DrainDrop0"),Clip("GameSfx/DrainDrop1"),Clip("GameSfx/DrainDrop2")};
            EditorUtility.SetDirty(visuals);EditorUtility.SetDirty(audio);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("Weather/daily rhythm installed into existing Westbruch scene; no district regeneration.");
        }
        static Material Material(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);if(!shader||!shader.isSupported)throw new Exception("Weather shader unavailable: "+shaderName);
            string path=Materials+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}else material.shader=shader;
            EditorUtility.SetDirty(material);return material;
        }
        public static void InstallAndQA(){try{Install();WorldWeatherEditorQA.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void VisualQA(){try{WorldWeatherEditorQA.RunVisual();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void NightQA(){try{WorldWeatherEditorQA.RunNight();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void TwilightQA(){try{WorldWeatherEditorQA.RunTwilight();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void Build(){try{WestbruchOverpassBuild.Windows();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
    [InitializeOnLoad]
    public static class WorldWeatherEditorQA
    {
        const string Pending="ZeroRise.WorldWeatherQA",Visual="ZeroRise.WorldVisualQA",Night="ZeroRise.WorldNightQA",Twilight="ZeroRise.WorldTwilightQA";
        static string Folder=>SessionState.GetBool(Twilight,false)?"QA/WorldWeatherTwilightProof":SessionState.GetBool(Night,false)?"QA/WorldWeatherNightProof":SessionState.GetBool(Visual,false)?"QA/WorldWeatherVisualProof":"QA/WorldWeatherEditor";
        static string Report=>Folder+"/world-weather-validation.txt";
        static WorldWeatherEditorQA(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
        public static void Run(){SessionState.SetBool(Twilight,false);SessionState.SetBool(Night,false);SessionState.SetBool(Visual,false);Begin();}
        public static void RunVisual(){SessionState.SetBool(Twilight,false);SessionState.SetBool(Night,false);SessionState.SetBool(Visual,true);Begin();}
        public static void RunNight(){SessionState.SetBool(Twilight,false);SessionState.SetBool(Night,true);SessionState.SetBool(Visual,true);Begin();}
        public static void RunTwilight(){SessionState.SetBool(Twilight,true);SessionState.SetBool(Night,false);SessionState.SetBool(Visual,true);Begin();}
        static void Begin()
        {
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            Directory.CreateDirectory(Folder);File.WriteAllText(Report,"RUNNING\n");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))WorldWeatherQA.Launch(Folder,SessionState.GetBool(Visual,false),SessionState.GetBool(Night,false),SessionState.GetBool(Twilight,false));}
        static void Poll()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||!File.Exists(Report))return;
            string report=File.ReadAllText(Report);if(!report.StartsWith("Passed:"))return;
            SessionState.SetBool(Pending,false);EditorApplication.Exit(report.StartsWith("Passed: True")?0:1);
        }
    }
}
