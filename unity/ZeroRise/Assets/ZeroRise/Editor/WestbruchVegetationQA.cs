using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    internal static class WestbruchVegetationQA
    {
        [Serializable] class Result
        {
            public float walkingBend,carBend,rootDrift,remoteBend,recoveredBend,maxFrameStep;
            public int contacts;
            public float plantReach,maxFrameAngle;
            public bool passed;
        }
        public static void Run(ThirdPersonMotor player,SimpleCar car)
        {
            var probe=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ZeroRise/Editor/VegetationProbe.compute");
            if(!probe||!SystemInfo.supportsComputeShaders)throw new Exception("GPU plant verification unavailable");
            // Sample the actual baked mesh/UV metadata, not a parallel CPU approximation.
            MeshFilter sample=null;Mesh sampledMesh=null;int tipIndex=0;var heights=new List<Vector2>();var roots=new List<Vector2>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer||!renderer.enabled||!renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE"))continue;
                // Unity's additional play/build static batch is intentionally not
                // CPU-readable. Read the same saved chunk asset before that batch.
                var baked=AssetDatabase.LoadAssetAtPath<Mesh>(WestbruchGeometry.Generated+"/Meshes/Chunk_"+filter.name+".asset");
                if(!baked||!baked.isReadable)continue;
                heights.Clear();roots.Clear();baked.GetUVs(2,heights);baked.GetUVs(3,roots);
                for(int i=0;i<heights.Count;i++)
                {
                    if(heights[i].y<.95f)continue;
                    var p=new Vector3(roots[i].x,heights[i].x,roots[i].y);
                    if(Mathf.Abs(p.x)>85||Mathf.Abs(p.z)>70)continue;
                    if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.5f,.32f,1))continue;
                    if(Physics.CheckBox(p+Vector3.up*.9f,new Vector3(1.9f,.65f,.35f),Quaternion.identity,1))continue;
                    sample=filter;sampledMesh=baked;tipIndex=i;break;
                }
                if(sample)break;
            }
            if(!sample)throw new Exception("No clear baked vegetation test patch");
            var root=new Vector3(roots[tipIndex].x,heights[tipIndex].x,roots[tipIndex].y);
            var tip=sample.transform.TransformPoint(sampledMesh.vertices[tipIndex]);
            // Root, actual top vertex, and remote/elevated controls share the GPU path.
            var points=new[]{(Vector4)root,(Vector4)tip,(Vector4)(tip+Vector3.right*35),(Vector4)(tip+Vector3.up*3)};
            var origins=new[]{new Vector4(root.x,root.y,root.z,0),new Vector4(root.x,root.y,root.z,1),new Vector4(root.x+35,root.y,root.z,1),new Vector4(root.x,root.y+3,root.z,1)};
            var result=new Result{plantReach=Vector3.Distance(root,tip)};var playerStart=player.transform.position;var carStart=car.transform.position;
            var motion=UnityEngine.Object.FindAnyObjectByType<VegetationMotion>();bool motionEnabled=motion.enabled;motion.enabled=false;
            var test=new GameObject("Temporary vegetation GPU test").AddComponent<VegetationMotion>();test.enabled=false;test.player=player;test.car=car;
            using(var input=new ComputeBuffer(4,16))using(var planted=new ComputeBuffer(4,16))using(var output=new ComputeBuffer(4,12))
            {
                input.SetData(points);planted.SetData(origins);int kernel=probe.FindKernel("Probe");
                probe.SetBuffer(kernel,"_Points",input);probe.SetBuffer(kernel,"_Roots",planted);probe.SetBuffer(kernel,"_Output",output);probe.SetInt("_Count",4);
                Vector3[] Evaluate(float now,bool contact)
                {
                    probe.SetVectorArray("_ZRPlantFrom",Shader.GetGlobalVectorArray("_ZRPlantFrom"));
                    probe.SetVectorArray("_ZRPlantTo",Shader.GetGlobalVectorArray("_ZRPlantTo"));
                    probe.SetVectorArray("_ZRPlantBounds",contact?Shader.GetGlobalVectorArray("_ZRPlantBounds"):new Vector4[2]);
                    probe.SetFloat("_ZRPlantTime",now);probe.Dispatch(kernel,1,1,1);var values=new Vector3[4];output.GetData(values);return values;
                }
                try
                {
                    car.transform.position=root+Vector3.right*30;
                    var controller=player.GetComponent<CharacterController>();controller.enabled=false;
                    player.transform.position=root+Vector3.left*1.5f+Vector3.up*.035f;
                    controller.enabled=true;Physics.SyncTransforms();
                    for(int i=0;i<10;i++)
                    {
                        controller.Move(Vector3.right*.3f);
                        test.Step(1000+i*.11f);
                        var actual=Evaluate(1000+i*.11f+.07f,true);var control=Evaluate(1000+i*.11f+.07f,false);
                        result.walkingBend=Mathf.Max(result.walkingBend,Vector3.Distance(actual[1],control[1]));
                        result.rootDrift=Mathf.Max(result.rootDrift,Vector3.Distance(actual[0],control[0]));
                        result.remoteBend=Mathf.Max(result.remoteBend,Vector3.Distance(actual[2],control[2]),Vector3.Distance(actual[3],control[3]));
                    }
                    result.contacts=test.LiveContacts;
                    var previous=Evaluate(1000,true);
                    for(int frame=1;frame<=156;frame++)
                    {
                        var value=Evaluate(1000+frame/60f,true);
                        result.maxFrameStep=Mathf.Max(result.maxFrameStep,Vector3.Distance(value[1],previous[1]));
                        result.maxFrameAngle=Mathf.Max(result.maxFrameAngle,Vector3.Angle(value[1]-root,previous[1]-root));previous=value;
                    }
                    var restored=Evaluate(1002.6f,true);var resting=Evaluate(1002.6f,false);
                    result.recoveredBend=Vector3.Distance(restored[1],resting[1]);
                    player.transform.position=root+Vector3.left*30;
                    for(int i=0;i<10;i++)
                    {
                        car.transform.position=root+new Vector3(-2+i*.45f,.18f,.2f);test.Step(1003+i*.11f);
                        var actual=Evaluate(1003+i*.11f+.07f,true);var control=Evaluate(1003+i*.11f+.07f,false);
                        result.carBend=Mathf.Max(result.carBend,Vector3.Distance(actual[1],control[1]));
                    }
                    result.passed=result.walkingBend>result.plantReach*.14f&&result.carBend>result.plantReach*.18f&&result.rootDrift<.0001f&&result.remoteBend<.0001f&&result.recoveredBend<.005f&&result.maxFrameStep<Mathf.Max(.015f,result.plantReach*.14f)&&result.contacts>0;
                    Directory.CreateDirectory("QA");File.WriteAllText("QA/vegetation-report.json",JsonUtility.ToJson(result,true));
                    if(!result.passed)throw new Exception("Vegetation GPU response failed: "+JsonUtility.ToJson(result));
                    CaptureResponse();
                    Debug.Log("Vegetation GPU QA passed: walking "+result.walkingBend+" m / car "+result.carBend+" m, recovery "+result.recoveredBend);
                }
                finally
                {
                    player.transform.position=playerStart;car.transform.position=carStart;
                    UnityEngine.Object.DestroyImmediate(test.gameObject);motion.enabled=motionEnabled;Physics.SyncTransforms();
                }
            }
        }
        static void CaptureResponse()
        {
            // Render the shipped plant meshes/materials with the shipped HDRP
            // vertex shader, including real contact deformation, depth and shadows.
            var camera=Camera.main;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            int mask=camera.cullingMask;var oldTarget=camera.targetTexture;
            var hd=camera.GetComponent<HDAdditionalCameraData>();var oldAA=hd.antialiasing;
            var stage=new GameObject("Temporary plant render verification");var root=new Vector3(150,2,0);
            var transient=new List<Mesh>();
            var target=new RenderTexture(1000,700,24,RenderTextureFormat.ARGB32);
            try
            {
                int display=0;
                foreach(int kind in new[]{0,3,4})
                {
                    string meshName="UrbanFlora_"+kind+"_1";
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(WestbruchGeometry.Generated+"/Meshes/"+meshName+".asset");
                    var mesh=UnityEngine.Object.Instantiate(original);transient.Add(mesh);
                    var planted=root+Vector3.right*(display++-1)*.75f;var h=new List<Vector2>();var r=new List<Vector2>();
                    foreach(var v in mesh.vertices){h.Add(new Vector2(planted.y,Mathf.Clamp01(v.y/mesh.bounds.max.y)));r.Add(new Vector2(planted.x,planted.z));}
                    mesh.SetUVs(2,h);mesh.SetUVs(3,r);var bounds=mesh.bounds;bounds.Expand(2);mesh.bounds=bounds;
                    var plant=new GameObject("Shipped plant type "+kind);plant.layer=30;plant.transform.SetParent(stage.transform);plant.transform.position=planted;
                    plant.AddComponent<MeshFilter>().sharedMesh=mesh;
                    plant.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(WestbruchGeometry.Generated+"/Materials/DistrictFlora.mat");
                }
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=30;floor.transform.SetParent(stage.transform);
                floor.transform.position=root+Vector3.down*.055f;floor.transform.localScale=new Vector3(6,.08f,5);
                floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(WestbruchGeometry.Generated+"/Materials/CompactedSoil.mat");
                camera.cullingMask=1<<30;camera.targetTexture=target;hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                camera.transform.SetPositionAndRotation(root+new Vector3(2.1f,1.15f,-2.8f),Quaternion.LookRotation(new Vector3(-2.1f,-.62f,2.8f)));
                var from=new Vector4[24];var to=new Vector4[24];var region=new[]{new Vector4(root.x,root.z,4,1),Vector4.zero};
                from[0]=new Vector4(root.x-.8f,root.y,root.z-.18f,100);
                to[0]=new Vector4(root.x+.8f,root.y,root.z-.18f,1.1f);
                Shader.SetGlobalVectorArray("_ZRPlantFrom",from);Shader.SetGlobalVectorArray("_ZRPlantTo",to);
                foreach(string state in new[]{"rest","contact","recovered"})
                {
                    Shader.SetGlobalVectorArray("_ZRPlantBounds",state=="contact"?region:new Vector4[2]);
                    // Identical wind/lighting during all three snapshots.
                    Shader.SetGlobalFloat("_ZRPlantTime",100.1f);
                    for(int warmup=0;warmup<5;warmup++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;
                    var image=new Texture2D(1000,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,700),0,0);image.Apply();
                    File.WriteAllBytes("QA/plants-"+state+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
                }
                foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("ZeroRise/UrbanLit")))
                    if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
            }
            finally
            {
                camera.targetTexture=oldTarget;camera.cullingMask=mask;hd.antialiasing=oldAA;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(stage);
                foreach(var mesh in transient)UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}
