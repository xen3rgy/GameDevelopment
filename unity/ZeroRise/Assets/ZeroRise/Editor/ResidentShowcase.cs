using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class ResidentShowcase
    {
        const string Flag="ZeroRise.ResidentShowcase";
        static ResidentShowcase(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary native resident and vehicle showcase").AddComponent<ResidentShowcaseCapture>();}};}
        public static void RebuildVehiclesAndCapture(){WestbruchVehicleArt.RebuildVehicles();Capture();}
        public static void Capture(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class ResidentShowcaseCapture:MonoBehaviour
    {
        PedestrianWorld world;WestbruchTraffic traffic;Camera camera;RenderTexture target;float started;bool frozen;int view,frames;
        void Start(){world=FindAnyObjectByType<PedestrianWorld>();traffic=FindAnyObjectByType<WestbruchTraffic>();var player=FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;player.orbit.enabled=false;camera=Camera.main;started=Time.time;}
        void Update()
        {
            try
            {
                if(Time.time-started<7&&!frozen)return;
                if(!frozen)
                {
                    Time.timeScale=.001f;foreach(var person in world.people){person.agent.isStopped=true;person.enabled=false;}
                    foreach(var avatar in FindObjectsByType<PlayerAvatar>()){avatar.enabled=false;avatar.animator.enabled=false;}
                    foreach(var volume in FindObjectsByType<Volume>())foreach(var setting in volume.profile.components)if(setting is MotionBlur||setting is DepthOfField)setting.active=false;
                    camera.GetComponent<HDAdditionalCameraData>().antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                    target=new RenderTexture(2048,1152,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;frozen=true;
                }
                Transform subject=view<3?world.people[view==0?0:view==1?3:4].transform:view<7?traffic.vehicles[view-3].transform:view==7?traffic.vehicles[0].transform:FindAnyObjectByType<SimpleCar>().transform;
                Vector3 at=subject.position+Vector3.up*(view<3?.95f:.85f),eye=subject.TransformPoint(view<3?new Vector3(-2.3f,1.9f,3.1f):new Vector3(-3.2f,2.3f,3.8f));
                if(view==4)eye=subject.TransformPoint(new Vector3(-3.2f,2.2f,-3.8f));
                if(view==7){at=subject.GetComponentInChildren<VehicleDriver>().avatar.head.position;eye=subject.TransformPoint(new Vector3(-2.1f,1.6f,.3f));}
                camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(++frames<8)return;
                var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(2048,1152,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,2048,1152),0,0);pixels.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/resident-vehicle-"+view+".png",pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;frames=0;
                if(++view<9)return;camera.targetTexture=null;target.Release();Time.timeScale=1;Debug.Log("Nine settled native resident/vehicle detail views captured");EditorApplication.Exit(0);enabled=false;
            }
            catch(System.Exception error){Debug.LogException(error);Time.timeScale=1;EditorApplication.Exit(1);enabled=false;}
        }
    }
}
