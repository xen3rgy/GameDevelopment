using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class WestbruchGroundQA
    {
        const string PlayFlag="ZeroRise.GroundPlayQA";
        static WestbruchGroundQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(PlayFlag,false))return;
                SessionState.SetBool(PlayFlag,false);new GameObject("Temporary terrain traversal verification").AddComponent<GroundTraversalTest>();
            };
        }
        public static void Play(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(PlayFlag,true);EditorApplication.EnterPlaymode();}
        [Serializable] sealed class Report
        {
            public int resolution,layers,detailTriangles,detailChunks,clearanceSamples;
            public int floraPlants,floraTriangles,floraVariants,floraChunks;
            public int detailedTrees,treeLODs,litterTriangles,litterChunks,courtyardShrubs;
            public int[] floraKinds=new int[6];
            public float maxPlantHeight,maxPlantRootError;
            public float minHeight=100,maxHeight=-100,maxWalkSlope,maxRoadIntrusion;
            public bool reusableAsset,stochasticShader,heightBlend;
            public List<string> failures=new List<string>();
        }
        public static void RebuildAndCapture(){WestbruchBuilder.Build();Capture();}
        public static void Validate()
        {
            WestbruchQA.Validate();
            var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var report=new Report{resolution=data.heightmapResolution,layers=data.terrainLayers.Length,
                reusableAsset=AssetDatabase.LoadAssetAtPath<GameObject>(WestbruchGeometry.Generated+"/Prefabs/WestbruchGroundAsset.prefab"),
                stochasticShader=terrain.materialTemplate.shader.name=="ZeroRise/UrbanGround",heightBlend=terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_BLEND_HEIGHT")};
            if(report.resolution<1025||!report.reusableAsset||!report.stochasticShader||!report.heightBlend)report.failures.Add("Ground asset/material setup missing");
            float Height(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            foreach(var road in plan.roads)
                for(float x=road.x-road.w*.5f+.3f;x<road.x+road.w*.5f;x+=1.25f)
                for(float z=road.z-road.d*.5f+.3f;z<road.z+road.d*.5f;z+=1.25f)
                {report.clearanceSamples++;report.maxRoadIntrusion=Mathf.Max(report.maxRoadIntrusion,Height(x,z)-.04f);}
            foreach(var walk in WestbruchBuilder.WalkPlan(plan))
                for(float x=walk.x-walk.w*.5f+.2f;x<walk.x+walk.w*.5f;x+=.75f)
                for(float z=walk.z-walk.d*.5f+.2f;z<walk.z+walk.d*.5f;z+=.75f)
                {report.clearanceSamples++;if(Height(x,z)>.13f)report.failures.Add("Terrain covers paved walk: "+walk.id);}
            for(float x=-108;x<110;x+=1.5f)for(float z=-105;z<105;z+=1.5f)
            {
                float h=Height(x,z);report.minHeight=Mathf.Min(report.minHeight,h);report.maxHeight=Mathf.Max(report.maxHeight,h);
                float nx=(x-terrain.transform.position.x)/data.size.x,nz=(z-terrain.transform.position.z)/data.size.z;
                report.maxWalkSlope=Mathf.Max(report.maxWalkSlope,data.GetSteepness(nx,nz));
            }
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(!filter.name.StartsWith("GroundRelief_"))continue;
                report.detailTriangles+=filter.sharedMesh.triangles.Length/3;report.detailChunks++;
                if(filter.GetComponent<Collider>())report.failures.Add("Small ground detail blocks movement");
            }
            if(report.maxRoadIntrusion>.001f)report.failures.Add("Terrain protrudes through road");
            if(report.maxHeight-report.minHeight<.35f)report.failures.Add("Ground relief still too flat");
            if(report.maxWalkSlope>35)report.failures.Add("Unwalkable ground slope");
            if(report.detailTriangles<25000||report.detailTriangles>220000||report.detailChunks>48)report.failures.Add("Ground mesh density/budget invalid");
            var floraVariants=new HashSet<Mesh>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(filter.name.StartsWith("DistrictFlora_"))report.floraChunks++;
                if(!filter.name.StartsWith("Ground flora "))continue;
                report.floraPlants++;report.floraTriangles+=(int)filter.sharedMesh.GetIndexCount(0)/3;floraVariants.Add(filter.sharedMesh);
                int kind=int.Parse(filter.sharedMesh.name.Split('_')[1]);report.floraKinds[kind]++;
                var p=filter.transform.position;float h=filter.GetComponent<Renderer>().bounds.max.y-p.y;
                report.maxPlantHeight=Mathf.Max(report.maxPlantHeight,h);
                report.maxPlantRootError=Mathf.Max(report.maxPlantRootError,Mathf.Abs(p.y-Height(p.x,p.z)));
                if(filter.GetComponent<Collider>())report.failures.Add("Ground flora adds physical obstruction");
                foreach(var road in plan.roads)
                    if(Mathf.Abs(p.x-road.x)<road.w*.5f+3.1f&&Mathf.Abs(p.z-road.z)<road.d*.5f+3.1f)report.failures.Add("Plant on circulation surface");
            }
            report.floraVariants=floraVariants.Count;
            if(report.floraPlants<4000||report.floraPlants>WestbruchFlora.MaximumPlants||report.floraTriangles>WestbruchFlora.MaximumTriangles||report.floraChunks>80)report.failures.Add("Ground flora density/budget invalid");
            if(report.maxPlantHeight>1.05f||report.maxPlantRootError>.035f||report.floraVariants<15||Array.Exists(report.floraKinds,n=>n==0))report.failures.Add("Ground flora scale/support/diversity invalid");
            if(report.floraKinds[4]>report.floraPlants*.025f||report.floraKinds[5]>report.floraPlants*.04f)report.failures.Add("District looks like a planted garden");
            foreach(var lod in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                if(lod.name.StartsWith("Dense low courtyard shrub"))
                {
                    report.courtyardShrubs++;
                    if(lod.GetComponentsInChildren<Collider>().Length>0)report.failures.Add("Soft shrub blocks player or camera");
                    if(lod.gameObject.isStatic)report.failures.Add("Interactive shrub can lose its root in static batching");
                    foreach(var renderer in lod.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)
                        if(!mat.IsKeywordEnabled("_ZR_SHRUB"))report.failures.Add("Shrub contact shader missing");
                    continue;
                }
                if(!lod.name.StartsWith("Urban tree "))continue;
                report.detailedTrees++;var levels=lod.GetLODs();report.treeLODs+=levels.Length;
                if(levels.Length!=2)report.failures.Add("Detailed tree LODs missing");
                foreach(var level in levels)foreach(var renderer in level.renderers)
                {
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    if(mesh.subMeshCount!=2)report.failures.Add("Tree bark/foliage submeshes missing");
                    foreach(var mat in renderer.sharedMaterials)
                    {var tex=mat.GetTexture("_BaseColorMap");if(!tex||tex.width!=2048||tex.height!=2048)report.failures.Add("Tree material is not imported at 2K");}
                }
            }
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(filter.name.StartsWith("YardLitter_")){report.litterTriangles+=(int)filter.sharedMesh.GetIndexCount(0)/3;report.litterChunks++;if(filter.GetComponent<Collider>())report.failures.Add("Yard litter blocks movement");}
            if(report.detailedTrees<30||report.courtyardShrubs<6||report.litterTriangles<10000||report.litterTriangles>250000||report.litterChunks>64)report.failures.Add("Detailed tree/yard assets missing or over budget");
            foreach(var message in ShaderUtil.GetShaderMessages(terrain.materialTemplate.shader))
                if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)report.failures.Add(message.message);
            WestbruchStreetFurnitureQA.Validate(plan);
            WestbruchEverydayDetails.Validate();
            WestbruchEarthCoverageQA.Validate(plan);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/ground-asset-report.json",JsonUtility.ToJson(report,true));
            if(report.failures.Count>0)throw new Exception("Ground QA: "+string.Join("; ",report.failures));
            Debug.Log("Ground asset QA passed: "+report.detailTriangles+" detail triangles; relief "+(report.maxHeight-report.minHeight)+"m; "+report.clearanceSamples+" circulation samples.");
        }
        public static void Capture()
        {
            Validate();var camera=Camera.main;var terrain=Terrain.activeTerrain;
            const int width=2048,height=1152;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.fieldOfView=60;
            Vector3 patch=new Vector3(-106,0,-30);float nearest=1000;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(filter.name.StartsWith("Ground flora 0"))
                {
                    float distance=Vector3.Distance(filter.transform.position,new Vector3(68,0,31));
                    if(distance<nearest){nearest=distance;patch=filter.transform.position;}
                }
            Vector3[] eyes={new Vector3(72,2.7f,26),new Vector3(72,.65f,29),new Vector3(-48,2.5f,29),new Vector3(-108,1.4f,-36),new Vector3(60,12,19),new Vector3(patch.x+1.1f,.55f,patch.z-1.6f),new Vector3(15,3.2f,-74),new Vector3(-99,3,-44),new Vector3(-103.7f,2.14f,-74),new Vector3(-16,1.8f,-53.5f)};
            Vector3[] aims={new Vector3(60,.1f,34),new Vector3(61,.12f,33),new Vector3(-56,.1f,35),new Vector3(-107,.1f,-20),new Vector3(62,.1f,31),patch+Vector3.up*.09f,new Vector3(-15,1.4f,-52),new Vector3(-106,4,-30),new Vector3(-99.7f,3.14f,-69),new Vector3(-13,.7f,-56.5f)};
            Array.Resize(ref eyes,eyes.Length+9);Array.Resize(ref aims,aims.Length+9);
            eyes[10]=new Vector3(-60,2.1f,8.8f);aims[10]=new Vector3(-49,.25f,4.8f);
            eyes[11]=new Vector3(-43,2.2f,44.7f);aims[11]=new Vector3(-48,.5f,49);
            eyes[12]=new Vector3(10,3.6f,-75);aims[12]=new Vector3(-20,.7f,-65);
            string[] objects={"Wheeled communal dumpster body","Dumped upholstered mattress"};
            for(int i=0;i<objects.Length;i++)
            {
                var prop=GameObject.Find(objects[i]);var at=prop.transform.parent.position;
                eyes[13+i]=new Vector3(at.x+3.3f,2.1f,at.z-4.2f);aims[13+i]=at+Vector3.up*.45f;
            }
            eyes[15]=new Vector3(-91,2.5f,-88);aims[15]=new Vector3(-105,.3f,-100);
            eyes[16]=new Vector3(-49,2.4f,-38);aims[16]=new Vector3(-54,.3f,-25);
            eyes[17]=new Vector3(-85,2.8f,-88);aims[17]=new Vector3(-76,.3f,-92);
            eyes[18]=new Vector3(7,2.6f,-73);aims[18]=new Vector3(-17,.4f,-55);
            int view=0,frames=0;
            EditorApplication.CallbackFunction capture=null;
            capture=()=>
            {
                try
                {
                    var eye=eyes[view];eye.y+=terrain.SampleHeight(new Vector3(eye.x,0,eye.z))+terrain.transform.position.y;
                    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(aims[view]-eye));EditorApplication.QueuePlayerLoopUpdate();if(++frames<35)return;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes("QA/ground-"+view+".png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
                    frames=0;if(++view<eyes.Length)return;
                    foreach(var message in ShaderUtil.GetShaderMessages(terrain.materialTemplate.shader))if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
                    camera.targetTexture=null;target.Release();EditorApplication.update-=capture;Debug.Log("Ground HDRP gameplay/close-up renders complete");EditorApplication.Exit(0);
                }
                catch(Exception error){Debug.LogException(error);EditorApplication.update-=capture;EditorApplication.Exit(1);}
            };
            EditorApplication.update+=capture;
        }
    }
    public sealed class GroundTraversalTest:MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public string route;public int fps;public bool running,passed;
            public float distance,verticalTravel,maxGroundGap,maxCentreRayGap,maxPenetration,maxCameraPenetration,idleRootDrift;
            public int unsupportedFrames;
        }
        [Serializable] sealed class Report { public List<Sample> samples=new List<Sample>();public bool passed; }
        IEnumerator Start()
        {
            var motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();var capsule=motor.GetComponent<CharacterController>();var orbit=motor.orbit;
            motor.enabled=false;orbit.enabled=false;motor.nearbyCar.enabled=false;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            var terrain=Terrain.activeTerrain;var report=new Report();
            Vector2[] starts={new Vector2(60,24),new Vector2(70,27),new Vector2(-62,30),new Vector2(-107,-32),new Vector2(-35,-58),new Vector2(-14,-61)};
            Vector2[] ends={new Vector2(64,36),new Vector2(76,35),new Vector2(-50,34),new Vector2(-107,-18),new Vector2(-15,-58),new Vector2(-8,-62)};
            string[] names={"shop mound and paving","shop drainage hollow","courtyard soil bank","canal verge","new worn courtyard access","shared seating clearance"};
            foreach(int fps in new[]{30,60})for(int route=0;route<starts.Length;route++)
            {
                Time.captureDeltaTime=1f/fps;motor.SetDriving(false);capsule.enabled=false;
                var start=new Vector3(starts[route].x,0,starts[route].y);start.y=terrain.SampleHeight(start)+terrain.transform.position.y+.20f;
                var direction=new Vector3(ends[route].x-start.x,0,ends[route].y-start.z).normalized;
                motor.transform.position=start;motor.visual.rotation=Quaternion.LookRotation(direction);capsule.enabled=true;Physics.SyncTransforms();
                orbit.transform.rotation=Quaternion.identity;
                for(int i=0;i<fps;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                var sample=new Sample{route=names[route],fps=fps,running=fps==30};report.samples.Add(sample);
                var origin=motor.transform.position;float low=origin.y,high=origin.y;
                float length=Vector2.Distance(starts[route],ends[route]);float speed=sample.running?PlayerGaitProfile.RunSpeed:PlayerGaitProfile.WalkSpeed;
                int frames=Mathf.CeilToInt((length/speed+.3f)*fps);
                for(int frame=0;frame<frames;frame++)
                {
                    orbit.transform.rotation=Quaternion.identity;
                    motor.MoveStep(new Vector2(direction.x,direction.z),sample.running,false,Time.deltaTime);yield return null;
                    var pos=motor.transform.position;low=Mathf.Min(low,pos.y);high=Mathf.Max(high,pos.y);
                    if(Physics.Raycast(pos+Vector3.up*.5f,Vector3.down,out var ground,2,1,QueryTriggerInteraction.Ignore))
                    {
                        float centreGap=pos.y-ground.point.y;
                        sample.maxCentreRayGap=Mathf.Max(sample.maxCentreRayGap,centreGap);
                        sample.maxPenetration=Mathf.Max(sample.maxPenetration,-centreGap);
                        // At a kerb the capsule still rests on the upper ledge
                        // while its centre ray already sees the lower soil. Probe
                        // the physical footprint, retaining the centre metric too.
                        float support=ground.point.y;
                        foreach(var offset in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                            if(Physics.Raycast(pos+offset*capsule.radius*.9f+Vector3.up*.5f,Vector3.down,out var edge,2,1,QueryTriggerInteraction.Ignore))support=Mathf.Max(support,edge.point.y);
                        // A neighbouring upper kerb is support for the rounded
                        // capsule, not the floor directly beneath its centre.
                        float gap=pos.y-support;sample.maxGroundGap=Mathf.Max(sample.maxGroundGap,gap);
                    }
                    else sample.unsupportedFrames++;
                    // Execute the real orbit collision solver while circling the
                    // actual terrain. The main camera is disabled only for speed.
                    orbit.Rotate(3,0);orbit.SendMessage("LateUpdate");
                    var eye=orbit.transform.position;
                    float floor=terrain.SampleHeight(eye)+terrain.transform.position.y;
                    sample.maxCameraPenetration=Mathf.Max(sample.maxCameraPenetration,floor+.20f-eye.y);
                }
                sample.distance=Vector3.ProjectOnPlane(motor.transform.position-origin,Vector3.up).magnitude;sample.verticalTravel=high-low;
                for(int i=0;i<fps;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                var resting=motor.transform.position;
                for(int i=0;i<fps*3;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;sample.idleRootDrift=Mathf.Max(sample.idleRootDrift,Vector3.Distance(resting,motor.transform.position));}
                sample.passed=sample.distance>length*.88f&&sample.unsupportedFrames==0&&sample.maxGroundGap<.12f&&sample.maxPenetration<.03f&&sample.maxCameraPenetration<.001f&&sample.idleRootDrift<.005f;
            }
            report.passed=report.samples.TrueForAll(s=>s.passed);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/ground-traversal-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Ground traversal QA "+(report.passed?"passed":"FAILED")+": "+JsonUtility.ToJson(report));EditorApplication.Exit(report.passed?0:1);
        }
    }
}
