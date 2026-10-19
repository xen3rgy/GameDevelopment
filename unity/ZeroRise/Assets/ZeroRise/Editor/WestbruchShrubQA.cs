using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    internal static class WestbruchShrubQA
    {
        [Serializable] sealed class Walk
        {
            public string shrub;
            public float distance,groundGap;
            public bool passed;
        }
        [Serializable] sealed class Report
        {
            public List<Walk> walks=new List<Walk>();
            public float contactBend,rootDrift,remoteBend,recoveredBend,maxFrameStep,maxFrameAngle,plantReach;
            public bool passed;
        }
        public static void Run(ThirdPersonMotor player,SimpleCar car)
        {
            var shrubs=new List<MeshFilter>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if(filter.name.StartsWith("Dense low courtyard shrub"))shrubs.Add(filter);
            if(shrubs.Count<6)throw new Exception("Interactive shrubs missing");
            var result=new Report();var controller=player.GetComponent<CharacterController>();
            var oldPosition=player.transform.position;var oldRotation=player.transform.rotation;
            var motion=UnityEngine.Object.FindAnyObjectByType<VegetationMotion>();bool wasEnabled=motion.enabled;
            motion.enabled=false;var test=new GameObject("Temporary shrub contact verification").AddComponent<VegetationMotion>();
            test.enabled=false;test.player=player;
            try
            {
                foreach(var shrub in shrubs)
                {
                    if(shrub.GetComponent<Collider>()||shrub.gameObject.isStatic)throw new Exception("Shrub collision/static root regression");
                    foreach(var mat in shrub.GetComponent<Renderer>().sharedMaterials)
                        if(!mat.IsKeywordEnabled("_ZR_SHRUB"))throw new Exception("Shrub material lost contact variant");
                    var root=shrub.transform.position;Vector3 direction=Vector3.zero;
                    foreach(var axis in new[]{Vector3.right,Vector3.forward,new Vector3(.7071f,0,.7071f),new Vector3(.7071f,0,-.7071f)})
                    {
                        if(!Physics.CheckBox(root+Vector3.up*.98f,new Vector3(.32f,.60f,1.55f),Quaternion.LookRotation(axis),1,QueryTriggerInteraction.Ignore))
                        {direction=axis;break;}
                    }
                    if(direction==Vector3.zero)throw new Exception("No clear through-shrub test route: "+shrub.name);
                    var start=root-direction*1.50f;start.y=Floor(start)+.12f;
                    controller.enabled=false;player.transform.position=start;controller.enabled=true;Physics.SyncTransforms();
                    var sample=new Walk{shrub=shrub.name};
                    // Exercise the real player capsule, including gravity and terrain
                    // support. These soft plants must neither block nor lift it.
                    for(int frame=0;frame<132;frame++)
                    {
                        controller.Move((direction*1.5f+Vector3.down*2)/60f);
                        var at=player.transform.position;
                        sample.groundGap=Mathf.Max(sample.groundGap,at.y-Floor(at));
                    }
                    sample.distance=Vector3.Dot(player.transform.position-start,direction);
                    sample.passed=sample.distance>3.15f&&sample.groundGap<.14f;result.walks.Add(sample);
                }
                var source=shrubs[0];var planted=source.transform.position;var mesh=source.sharedMesh;
                var vertices=mesh.vertices;int top=0;
                for(int i=1;i<vertices.Length;i++)if(vertices[i].y>vertices[top].y)top=i;
                var tip=source.transform.TransformPoint(vertices[top]);result.plantReach=Vector3.Distance(tip,planted);
                var points=new[]{(Vector4)planted,(Vector4)tip,(Vector4)(tip+Vector3.right*35),(Vector4)(tip+Vector3.up*3)};
                var roots=new[]{(Vector4)planted,(Vector4)planted,(Vector4)(planted+Vector3.right*35),(Vector4)(planted+Vector3.up*3)};
                var probe=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ZeroRise/Editor/VegetationProbe.compute");
                using(var input=new ComputeBuffer(4,16))using(var origin=new ComputeBuffer(4,16))using(var output=new ComputeBuffer(4,12))
                {
                    input.SetData(points);origin.SetData(roots);int kernel=probe.FindKernel("ShrubProbe");
                    probe.SetBuffer(kernel,"_Points",input);probe.SetBuffer(kernel,"_Roots",origin);probe.SetBuffer(kernel,"_Output",output);probe.SetInt("_Count",4);
                    Vector3[] Evaluate(float now,bool contact)
                    {
                        probe.SetVectorArray("_ZRPlantFrom",Shader.GetGlobalVectorArray("_ZRPlantFrom"));
                        probe.SetVectorArray("_ZRPlantTo",Shader.GetGlobalVectorArray("_ZRPlantTo"));
                        probe.SetVectorArray("_ZRPlantBounds",contact?Shader.GetGlobalVectorArray("_ZRPlantBounds"):new Vector4[2]);
                        probe.SetFloat("_ZRPlantTime",now);probe.Dispatch(kernel,1,1,1);var values=new Vector3[4];output.GetData(values);return values;
                    }
                    controller.enabled=false;Vector3 previous=tip;
                    for(int frame=0;frame<=210;frame++)
                    {
                        float t=frame/60f,now=2000+t;
                        player.transform.position=planted+Vector3.right*Mathf.Lerp(-1.7f,1.7f,Mathf.Clamp01(t/1.35f))+Vector3.up*.035f;
                        test.Step(now);var actual=Evaluate(now,true);var wind=Evaluate(now,false);
                        result.contactBend=Mathf.Max(result.contactBend,Vector3.Distance(actual[1],wind[1]));
                        result.rootDrift=Mathf.Max(result.rootDrift,Vector3.Distance(actual[0],wind[0]));
                        result.remoteBend=Mathf.Max(result.remoteBend,Vector3.Distance(actual[2],wind[2]),Vector3.Distance(actual[3],wind[3]));
                        if(frame>0)
                        {
                            result.maxFrameStep=Mathf.Max(result.maxFrameStep,Vector3.Distance(actual[1],previous));
                            result.maxFrameAngle=Mathf.Max(result.maxFrameAngle,Vector3.Angle(actual[1]-planted,previous-planted));
                        }
                        if(frame==210)result.recoveredBend=Vector3.Distance(actual[1],wind[1]);previous=actual[1];
                    }
                }
                result.passed=result.walks.TrueForAll(s=>s.passed)&&result.contactBend>.18f&&result.rootDrift<.0001f&&result.remoteBend<.0001f&&result.recoveredBend<.005f&&result.maxFrameStep<Mathf.Max(.015f,result.plantReach*.05f)&&result.maxFrameAngle<3;
                Directory.CreateDirectory("QA");File.WriteAllText("QA/shrub-contact-report.json",JsonUtility.ToJson(result,true));
                if(!result.passed)throw new Exception("Shrub contact QA failed: "+JsonUtility.ToJson(result));
                Capture(source,player,test);
                Debug.Log("Shrub contact QA passed: "+result.walks.Count+" actual capsule crossings; "+result.contactBend+"m GPU bend; "+result.maxFrameStep+"m maximum frame step.");
            }
            finally
            {
                controller.enabled=false;player.transform.SetPositionAndRotation(oldPosition,oldRotation);controller.enabled=true;
                UnityEngine.Object.DestroyImmediate(test.gameObject);motion.enabled=wasEnabled;Physics.SyncTransforms();
            }
        }
        static float Floor(Vector3 p)
        {
            return Physics.Raycast(p+Vector3.up*1.5f,Vector3.down,out var hit,4,1,QueryTriggerInteraction.Ignore)?hit.point.y:Terrain.activeTerrain.SampleHeight(p)+Terrain.activeTerrain.transform.position.y;
        }
        static void Capture(MeshFilter source,ThirdPersonMotor player,VegetationMotion test)
        {
            var camera=Camera.main;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;int oldMask=camera.cullingMask;
            var hd=camera.GetComponent<HDAdditionalCameraData>();var oldAA=hd.antialiasing;
            var stage=new GameObject("Temporary interactive shrub render");var root=new Vector3(150,2,0);
            var target=new RenderTexture(960,720,24,RenderTextureFormat.ARGB32);
            var floorMaterial=new Material(Shader.Find("HDRP/Lit"));floorMaterial.SetColor("_BaseColor",new Color(.27f,.23f,.17f));floorMaterial.SetFloat("_Smoothness",.08f);
            HDMaterial.ValidateMaterial(floorMaterial);
            try
            {
                var shrub=new GameObject("Actual shared shrub asset");shrub.layer=30;shrub.transform.SetParent(stage.transform);shrub.transform.position=root;
                shrub.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                shrub.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=30;floor.transform.SetParent(stage.transform);
                floor.transform.position=root+Vector3.down*.065f;floor.transform.localScale=new Vector3(5,.08f,5);
                floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
                camera.cullingMask=1<<30;camera.targetTexture=target;hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                camera.transform.SetPositionAndRotation(root+new Vector3(1.9f,1.25f,-2.4f),Quaternion.LookRotation(new Vector3(-1.9f,-.80f,2.4f)));
                for(int frame=0;frame<=36;frame++)
                {
                    float t=frame/12f;
                    player.transform.position=root+Vector3.right*Mathf.Lerp(-1.7f,1.7f,Mathf.Clamp01(t/1.35f))+Vector3.up*.035f;
                    test.Step(3000+t);
                    for(int warmup=0;warmup<3;warmup++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(960,720,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();File.WriteAllBytes("QA/shrub-motion-"+frame.ToString("D2")+".png",pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
                }
            }
            finally
            {
                camera.targetTexture=oldTarget;camera.cullingMask=oldMask;hd.antialiasing=oldAA;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(stage);UnityEngine.Object.DestroyImmediate(floorMaterial);
            }
        }
    }
}
