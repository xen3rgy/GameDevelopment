using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    public static class WestbruchBridgeStartQA
    {
        public static void CheckSaved()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
                Physics.SyncTransforms();
                var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
                for(float x=26;x<=70;x+=2)foreach(float side in new[]{-1f,1f})
                    if(Physics.Raycast(new Vector3(x,3,65+side*10.1f),Vector3.down,out var hit,4,1,QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(bridge))
                        throw new Exception("Approach intrudes into the existing street corridor.");
                var camp=GameObject.Find(WestbruchBridgeStart.CampName).transform;
                var flora=camp.Find("Local render batch DistrictFlora").GetComponent<MeshFilter>().sharedMesh;
                if(flora.uv3.Length!=flora.vertexCount||flora.uv4.Length!=flora.vertexCount||flora.bounds.size.y>3)
                    throw new Exception("Foliage root data or height bounds incorrect.");
                File.WriteAllText("QA/bridge-start-geometry.txt","Saved scene reopened: PASS\nExisting street corridors remain clear: PASS\nLocal foliage wind roots and height bounds: PASS\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("QA/bridge-start-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory("QA");
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
                var cars=UnityEngine.Object.FindObjectsByType<SimpleCar>().Length;
                WestbruchBridgeStart.Apply();
                if(cars!=UnityEngine.Object.FindObjectsByType<SimpleCar>().Length)throw new Exception("Vehicle count changed.");
                if(GameObject.Find("Rail underpass – east connection"))throw new Exception("Legacy bridge remains.");
                var player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();
                var p=player.transform.position;
                if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(83,66))>.01f)throw new Exception("New game spawn incorrect.");
                Physics.SyncTransforms();
                if(!Physics.Raycast(p+Vector3.up*.4f,Vector3.down,out var hit,1,1,QueryTriggerInteraction.Ignore)||!(hit.collider is TerrainCollider))throw new Exception("Spawn is not on terrain.");
                if(Physics.CheckCapsule(p+Vector3.up*.34f,p+Vector3.up*1.45f,.28f,1,QueryTriggerInteraction.Ignore))throw new Exception("Spawn capsule obstructed.");
                if(!Physics.Raycast(new Vector3(94,4,65),Vector3.down,4,1,QueryTriggerInteraction.Ignore))throw new Exception("Underpass road is missing.");
                SessionState.SetInt("BridgeStart.Cars",cars);
                EditorApplication.playModeStateChanged+=Entered;
                EditorApplication.EnterPlaymode();
            }
            catch(Exception e){File.WriteAllText("QA/bridge-start-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [InitializeOnLoadMethod] static void Register(){EditorApplication.playModeStateChanged-=Entered;EditorApplication.playModeStateChanged+=Entered;}
        static void Entered(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetInt("BridgeStart.Cars",-1)>=0)
            {SessionState.SetInt("BridgeStart.Cars",-1);new GameObject("Focused bridge and story start verification").AddComponent<BridgeStartCapture>();}
        }
    }
    public sealed class BridgeStartCapture:MonoBehaviour
    {
        Camera camera;OrbitCamera orbit;ThirdPersonMotor player;CharacterController cc;RenderTexture target;
        int frame,view,errors;bool settled,walked;Vector3 spawn;string report;
        void Start()
        {
            Application.logMessageReceived+=Log;
            player=FindAnyObjectByType<ThirdPersonMotor>();cc=player.GetComponent<CharacterController>();orbit=player.orbit;camera=Camera.main;
            spawn=player.transform.position;player.enabled=false;
            target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var hd=camera.GetComponent<HDAdditionalCameraData>();hd.customRenderingSettings=true;
            foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.DepthOfField}){hd.renderingPathCustomFrameSettings.SetEnabled(field,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
        }
        void Log(string msg,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
        void LateUpdate()
        {
            try
            {
                if(!settled)
                {
                    cc.Move(Vector3.down*.035f);
                    if(++frame<90)return;
                    settled=cc.isGrounded&&Mathf.Abs(player.transform.position.y-spawn.y)<.13f;
                    if(!settled)throw new Exception("Player failed to settle on the camp terrain.");
                    report="Spawn terrain contact: PASS\nSpawn capsule clear: PASS\nVehicles preserved: PASS\n";
                    frame=0;
                }
                // First view is the real orbit camera at the authored new-game spawn.
                if(view==1){orbit.enabled=false;camera.transform.position=new Vector3(82.9f,2.2f,70.8f);camera.transform.LookAt(new Vector3(81.5f,.7f,64.5f));}
                if(view==2){camera.transform.position=new Vector3(105,8,44);camera.transform.LookAt(new Vector3(91,3.3f,65));}
                if(++frame<50)return;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                var old=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
                var name=new[]{"spawn","shelter","overpass"}[view];File.WriteAllBytes("QA/bridge-start-"+name+".png",pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;
                frame=0;if(++view<3)return;
                // Exercise the actual controller through the fence gap and onto the sidewalk.
                cc.enabled=false;player.transform.position=spawn;cc.enabled=true;Physics.SyncTransforms();
                for(int i=0;i<110;i++)cc.Move(new Vector3(0,-.035f,.06f));
                walked=player.transform.position.z>72;
                for(int i=0;i<60;i++)cc.Move(new Vector3(.06f,-.035f,0));
                walked&=player.transform.position.x>86;
                report+="Grounded after settling: "+settled+"\nFence opening to sidewalk traversal: "+walked+"\nRuntime errors: "+errors+"\nActual HDRP runtime captures: 3\n";
                bool pass=walked&&errors==0;File.WriteAllText("QA/bridge-start-validation.txt",report+"Passed: "+pass+"\n");
                EditorApplication.Exit(pass?0:1);
            }
            catch(Exception e){File.WriteAllText("QA/bridge-start-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        void OnDestroy(){Application.logMessageReceived-=Log;if(target){camera.targetTexture=null;target.Release();Destroy(target);}}
    }
}
