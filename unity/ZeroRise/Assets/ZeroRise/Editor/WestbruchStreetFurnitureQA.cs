using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    internal static class WestbruchStreetFurnitureQA
    {
        [Serializable] sealed class Placement
        {
            public int trees,shrubs,flora,lights,benches;
            public int[] shrubQuadrants=new int[4];
            public List<string> failures=new List<string>();
        }
        public static void Validate(WestbruchLayout plan)
        {
            WestbruchLandscape.InspectScene(plan);Physics.SyncTransforms();var report=new Placement();
            void Failure(string message){if(report.failures.Count<20)report.failures.Add(message);}
            foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                bool tree=group.name.StartsWith("Urban tree "),shrub=group.name.StartsWith("Dense low courtyard shrub");
                if(!tree&&!shrub)continue;var at=group.transform.position;
                if(tree)report.trees++;else {report.shrubs++;report.shrubQuadrants[(at.x>=0?1:0)+(at.z>=0?2:0)]++;}
                if(!WestbruchLandscape.OnEarth(at.x,at.z,tree?.44f:.25f,group.transform))Failure("Vegetation is not supported on earth: "+group.name+" "+at);
                var terrain=Terrain.activeTerrain;float floor=terrain.SampleHeight(at)+terrain.transform.position.y;
                if(Mathf.Abs(at.y-floor)>.035f)Failure("Vegetation root floats: "+group.name);
                if(shrub&&group.GetLODs().Length!=2)Failure("Map-wide shrubs lack reduced geometry");
            }
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(!filter.name.StartsWith("Ground flora "))continue;report.flora++;
                var at=filter.transform.position;
                if(!WestbruchLandscape.OnEarth(at.x,at.z,.05f))Failure("Ground plant on hard surface: "+at);
            }
            foreach(var at in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(at.name=="Worn public bench")
                {
                    report.benches++;
                    if(at.GetComponent<Collider>())Failure("Bench retains oversized root collider");
                    var support=at.Find("Bench seat support");
                    if(!support||!support.GetComponent<BoxCollider>())Failure("Bench seat collision missing");
                    else
                    {
                        float top=support.position.y+support.GetComponent<BoxCollider>().size.y*.5f;
                        foreach(var slat in at.GetComponentsInChildren<MeshRenderer>())
                            if(slat.name=="Seat slat"&&Mathf.Abs(slat.bounds.max.y-top)>.002f)Failure("Seat collision differs from visible timber");
                    }
                }
                if(!at.name.StartsWith("Old European streetlight "))continue;report.lights++;
                var hits=Physics.RaycastAll(at.position+Vector3.up*8,Vector3.down,9,1,QueryTriggerInteraction.Ignore);
                Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));Collider floor=null;
                foreach(var hit in hits)if(!hit.collider.transform.IsChildOf(at)){floor=hit.collider;break;}
                if(!floor||floor.name!="Connected pavement")Failure("Streetlight is not on supported pavement: "+at.position);
                if(!at.GetComponent<CapsuleCollider>()||at.GetComponentsInChildren<Light>().Length>0)Failure("Streetlight collision/light budget invalid");
            }
            if(report.lights<16||report.benches<18||report.shrubs<30||Array.Exists(report.shrubQuadrants,n=>n<3))Failure("District dressing coverage incomplete");
            Directory.CreateDirectory("QA");File.WriteAllText("QA/street-dressing-report.json",JsonUtility.ToJson(report,true));
            if(report.failures.Count>0)throw new Exception("Street dressing QA: "+string.Join("; ",report.failures));
            Debug.Log("Street dressing QA passed: "+report.trees+" tree bases, "+report.shrubs+" shrubs, "+report.flora+" plant roots, "+report.lights+" pavement lamps, "+report.benches+" bench seats.");
        }
        [Serializable] sealed class Standing
        {
            public int benches;public float maxSeatError,maxStandingError,maxDrift;public bool passed;
        }
        public static void Stand(ThirdPersonMotor player)
        {
            var controller=player.GetComponent<CharacterController>();var saved=player.transform.position;var report=new Standing();
            try
            {
                foreach(var bench in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if(bench.name!="Worn public bench")continue;
                    var support=bench.Find("Bench seat support");var box=support.GetComponent<BoxCollider>();
                    float top=support.position.y+box.size.y*.5f;
                    var position=bench.TransformPoint(new Vector3(0,0,-.13f));position.y=top+.45f;
                    controller.enabled=false;player.transform.position=position;controller.enabled=true;Physics.SyncTransforms();
                    for(int frame=0;frame<75;frame++)controller.Move(Vector3.down*(2f/60));
                    var resting=player.transform.position;
                    report.maxStandingError=Mathf.Max(report.maxStandingError,Mathf.Abs(resting.y-top-controller.skinWidth));
                    for(int frame=0;frame<60;frame++){controller.Move(Vector3.down*(2f/60));report.maxDrift=Mathf.Max(report.maxDrift,Vector3.Distance(resting,player.transform.position));}
                    foreach(var visual in bench.GetComponentsInChildren<MeshRenderer>())if(visual.name=="Seat slat")report.maxSeatError=Mathf.Max(report.maxSeatError,Mathf.Abs(visual.bounds.max.y-top));
                    report.benches++;
                }
                report.passed=report.benches>=18&&report.maxSeatError<.002f&&report.maxStandingError<.015f&&report.maxDrift<.002f;
                Directory.CreateDirectory("QA");File.WriteAllText("QA/bench-standing-report.json",JsonUtility.ToJson(report,true));
                if(!report.passed)throw new Exception("Bench standing QA failed: "+JsonUtility.ToJson(report));
                Capture(player);
                Debug.Log("Bench standing QA passed: "+report.benches+" real capsule landings, standing error "+report.maxStandingError+"m.");
            }
            finally{controller.enabled=false;player.transform.position=saved;controller.enabled=true;Physics.SyncTransforms();}
        }
        static void Capture(ThirdPersonMotor player)
        {
            var camera=Camera.main;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;
            var hd=camera.GetComponent<HDAdditionalCameraData>();var oldAA=hd.antialiasing;
            var renderers=player.GetComponentsInChildren<Renderer>();var visible=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++){visible[i]=renderers[i].enabled;renderers[i].enabled=false;}
            var target=new RenderTexture(2048,1152,24,RenderTextureFormat.ARGB32);
            try
            {
                var pole=new Vector3(-99.7f,.14f,-69);float nearest=10000;
                foreach(var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if(item.name.StartsWith("Old European streetlight "))
                    {float distance=Vector3.Distance(item.position,new Vector3(-99.7f,.14f,-69));if(distance<nearest){nearest=distance;pole=item.position;}}
                var eyes=new[]{pole+new Vector3(-4,2,-5),new Vector3(-16,1.8f,-53.5f)};
                var aims=new[]{pole+Vector3.up*3,new Vector3(-13,.7f,-56.5f)};
                camera.targetTexture=target;hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                for(int view=0;view<2;view++)
                {
                    camera.transform.SetPositionAndRotation(eyes[view],Quaternion.LookRotation(aims[view]-eyes[view]));
                    for(int warmup=0;warmup<5;warmup++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(2048,1152,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,2048,1152),0,0);pixels.Apply();File.WriteAllBytes(view==0?"QA/streetlight-detail.png":"QA/bench-detail.png",pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
                }
            }
            finally
            {
                camera.targetTexture=oldTarget;hd.antialiasing=oldAA;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=visible[i];
                target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
        }

    }
}
