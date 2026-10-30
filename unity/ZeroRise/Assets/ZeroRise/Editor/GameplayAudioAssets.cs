using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class GameplayAudioAssets
    {
        public const string Folder="Assets/ZeroRise/Art/Audio/GameSfx/";
        public static GameAudioLibrary Library=>AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(Folder+"GameplayAudio.asset");
        static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+name);

        [MenuItem("ZERO RISE/Westbruch/Install recorded gameplay sound FX")]
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(Folder).Where(p=>p.EndsWith(".ogg")||p.EndsWith(".wav")))Import(path);
            for(int i=0;i<5;i++)
            {
                foreach(string surface in new[]{"concrete","grass","wood"})
                    PrepareStep("footstep_"+surface+"_"+i.ToString("000")+".ogg","Step"+surface+i,null);
                PrepareStep("footstep_concrete_"+i.ToString("000")+".ogg","Stepmetal"+i,"impactPlate_heavy_000.ogg");
            }
            PrepareStep("impactMetal_medium_000.ogg","ImpactBody",null);
            PrepareStep("impactMetal_light_000.ogg","ImpactLight",null);
            PrepareStep("impactSoft_heavy_000.ogg","SeatSettle",null);
            for(int i=0;i<5;i++)
            {
                PrepareStep("footstep_grass_"+i.ToString("000")+".ogg","ShoeEarth"+i,null,.050f);
                PrepareStep("footstep_wood_"+i.ToString("000")+".ogg","ShoeWood"+i,null,.055f);
                PrepareStep("footstep_concrete_"+i.ToString("000")+".ogg","ShoeMetal"+i,"impactPlate_heavy_000.ogg",.060f);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(Folder,"*.wav"))Import(path);
            var bank=Library;
            if(!bank){bank=ScriptableObject.CreateInstance<GameAudioLibrary>();AssetDatabase.CreateAsset(bank,Folder+"GameplayAudio.asset");}
            AudioClip W(string name)=>Clip(name+".wav");
            bank.engineIdle=W("EngineIdle");bank.engineLoad=W("EngineLoad");bank.engineStart=W("EngineStart");bank.engineStop=W("EngineStop");
            bank.roadTyres=W("RoadTyres");bank.tyreSlip=W("TyreSlip");bank.doorOpen=W("DoorOpen");bank.doorClose=W("DoorClose");bank.cabinClick=W("CabinClick");bank.horn=W("Horn");
            bank.bodyImpact=W("ImpactBody");bank.lightMetal=W("ImpactLight");bank.seatImpact=W("SeatSettle");
            bank.concrete=Enumerable.Range(0,8).Select(i=>W("ShoeConcrete"+i)).ToArray();bank.soil=Enumerable.Range(0,5).Select(i=>W("ShoeEarth"+i)).ToArray();
            bank.gravel=Enumerable.Range(0,8).Select(i=>W("ShoeGravel"+i)).ToArray();bank.grass=Enumerable.Range(0,8).Select(i=>W("ShoeGrass"+i)).ToArray();
            bank.bushRustles=Enumerable.Range(0,3).Select(i=>W("BushRustle"+i)).ToArray();
            bank.wood=Enumerable.Range(0,5).Select(i=>W("ShoeWood"+i)).ToArray();bank.metal=Enumerable.Range(0,5).Select(i=>W("ShoeMetal"+i)).ToArray();
            bank.clothing=Enumerable.Range(0,3).Select(i=>W("Clothing"+i)).ToArray();bank.birds=Enumerable.Range(0,3).Select(i=>W("Sparrow"+i)).ToArray();bank.drips=Enumerable.Range(0,3).Select(i=>W("DrainDrop"+i)).ToArray();
            bank.leaves=W("LeavesAir");bank.fence=W("FenceCreak");EditorUtility.SetDirty(bank);
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var sound=UnityEngine.Object.FindAnyObjectByType<WestbruchSoundscape>();
            if(!sound)throw new Exception("Expected the existing authored atmosphere scene.");
            Bind(sound);
            EditorUtility.SetDirty(sound);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Validate((label,pass,detail)=>{if(!pass)throw new Exception(label+": "+detail);Debug.Log("SFX PASS: "+label+". "+detail);});
        }
        public static void InstallAndValidate(){try{Install();WestbruchOverpassQA.CaptureSaved();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void InstallAndActionQA(){try{Install();GameplayAudioActionQA.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void InstallAndBuild(){try{Install();WestbruchOverpassBuild.Windows();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void Bind(WestbruchSoundscape sound)
        {
            var bank=Library;if(!bank)return;
            sound.gameAudio=bank;sound.wind.clip=bank.leaves;sound.drainClip=bank.drips[0];sound.fenceClip=bank.fence;
            sound.pavingSteps=bank.concrete.Take(3).ToArray();sound.soilSteps=bank.soil.Take(3).ToArray();EditorUtility.SetDirty(sound);
            BakeFoliage(bank);
        }
        static void BakeFoliage(GameAudioLibrary bank)
        {
            // Keep the original small source plants after rendering is batched.
            // An aggregate renderer's district-wide bounds are not a contact area.
            var volumes=new System.Collections.Generic.List<GameAudioLibrary.FoliageVolume>();
            var crowns=new System.Collections.Generic.Dictionary<Mesh,Bounds>();int denseCount=0;
            var filters=UnityEngine.Object.FindObjectsByType<MeshFilter>();
            // Put the actual dense courtyard shrubs first, so release traversal
            // QA exercises the leafy plants pictured in the feedback, too.
            foreach(var filter in filters.OrderBy(f=>f.name.StartsWith("Dense low courtyard shrub")?0:1))
            {
                bool dense=filter.name.StartsWith("Dense low courtyard shrub");
                if(!filter.sharedMesh||!dense&&(!filter.sharedMesh.name.StartsWith("UrbanFlora_4_")||!filter.name.StartsWith("Ground flora 4")))continue;
                var mesh=filter.sharedMesh;
                if(!crowns.TryGetValue(mesh,out var local))
                {
                    // Dense meshes have enlarged wind-culling bounds and a
                    // separate far LOD. Use only the rendered near leaf faces.
                    var vertices=mesh.vertices;var indices=mesh.GetTriangles(dense?1:0);
                    if(indices.Length==0)continue;
                    local=new Bounds(vertices[indices[0]],Vector3.zero);
                    foreach(int index in indices)local.Encapsulate(vertices[index]);
                    crowns[mesh]=local;
                }
                var matrix=filter.transform.localToWorldMatrix;
                Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
                var extents=Abs(matrix.MultiplyVector(Vector3.right*local.extents.x))+Abs(matrix.MultiplyVector(Vector3.up*local.extents.y))+Abs(matrix.MultiplyVector(Vector3.forward*local.extents.z));
                var center=matrix.MultiplyPoint3x4(local.center);
                // Exclude the bare lowest stem and the empty corners of the AABB.
                if(!dense)center.y+=extents.y*.12f;
                extents.Scale(dense?new Vector3(.94f,.97f,.94f):new Vector3(.88f,.85f,.88f));
                if(extents.x>.03f&&extents.y>.05f&&extents.x<2&&extents.z<2)
                {volumes.Add(new GameAudioLibrary.FoliageVolume{center=center,radii=extents});if(dense)denseCount++;}
            }
            bank.foliage=volumes.ToArray();EditorUtility.SetDirty(bank);
            if(volumes.Count==0||denseCount<6)throw new Exception("Missing visible dense courtyard shrub crowns for contact sound.");
            Debug.Log("Baked sound contact crowns for "+volumes.Count+" existing shrubs, including "+denseCount+" dense courtyard shrubs; no physics or navigation objects added.");
        }
        static void Import(string path)
        {
            path=path.Replace('\\','/');var importer=AssetImporter.GetAtPath(path) as AudioImporter;if(!importer)return;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=48000;
            importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();
        }
        static void PrepareStep(string name,string destination,string resonance,float targetRms=.17f)
        {
            if(File.Exists(Folder+destination+".wav"))return;
            var clip=Clip(name);clip.LoadAudioData();var samples=new float[clip.samples];if(!clip.GetData(samples,0))throw new Exception("Cannot decode recorded step "+name);
            if(resonance!=null)
            {
                var plate=Clip(resonance);plate.LoadAudioData();var ring=new float[plate.samples];plate.GetData(ring,0);
                Array.Resize(ref samples,Math.Max(samples.Length,Math.Min(ring.Length,14400)));
                for(int i=0;i<samples.Length&&i<ring.Length;i++)samples[i]+=ring[i]*.16f;
            }
            double energy=0;float peak=0;foreach(float x in samples){energy+=x*x;peak=Mathf.Max(peak,Mathf.Abs(x));}
            float gain=Mathf.Min(targetRms/(float)Math.Sqrt(energy/samples.Length),.78f/Mathf.Max(.001f,peak));
            for(int i=0;i<samples.Length;i++)samples[i]*=gain;
            int fade=Math.Min(72,samples.Length/4);for(int i=0;i<fade;i++){float f=Mathf.SmoothStep(0,1,i/(float)(fade-1));samples[i]*=f;samples[samples.Length-1-i]*=f;}
            using(var writer=new BinaryWriter(File.Create(Folder+destination+".wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(48000);writer.Write(96000);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float x in samples)writer.Write((short)Mathf.RoundToInt(x*32767));
            }
        }
        public static void Validate(Action<string,bool,string> record)
        {
            var bank=Library;if(!bank){record("Recorded gameplay sound library exists",false,"Missing library asset");return;}
            var serialized=new SerializedObject(bank);var property=serialized.GetIterator();int count=0;float peak=0;bool valid=true;
            while(property.NextVisible(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue is AudioClip clip)
            {
                count++;clip.LoadAudioData();var data=new float[clip.samples*clip.channels];valid&=clip.GetData(data,0)&&clip.channels==1;
                foreach(float x in data){valid&=!float.IsNaN(x)&&!float.IsInfinity(x);peak=Mathf.Max(peak,Mathf.Abs(x));}
                valid&=data.Any(x=>Mathf.Abs(x)>.001f);
            }
            record("Recorded vehicle, foley and surface clips decode and have safe headroom",valid&&count>=40&&peak<1,"References "+count+"; decoded maximum peak "+peak.ToString("F4"));
            record("Distinct pavement, earth, grass, gravel and contact-rustle banks",bank.concrete.Length>=8&&bank.soil.Length>=5&&bank.grass.Length>=8&&bank.gravel.Length>=8&&bank.bushRustles.Length>=3&&bank.foliage.Length>0,"Individual shrub crowns "+bank.foliage.Length);
            var sound=UnityEngine.Object.FindAnyObjectByType<WestbruchSoundscape>();
            record("Saved scene references the full gameplay sound library",sound&&sound.gameAudio==bank&&sound.wind.clip==bank.leaves&&sound.drainClip==bank.drips[0]&&sound.fenceClip==bank.fence,"Player/traffic audio bootstrap uses this scene reference; recorded wind, drain and fence active");
        }
    }
}
