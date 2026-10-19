using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class VehicleBodyShowcase
    {
        const string Flag="ZeroRise.VehicleBodyShowcase";
        static VehicleBodyShowcase(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary both-side vehicle body verification").AddComponent<VehicleBodyCapture>();}};}
        public static void Capture(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class VehicleBodyCapture:MonoBehaviour
    {
        Camera camera;RenderTexture target;TrafficVehicle[] cars=new TrafficVehicle[4];SimpleCar playerCar;bool frozen;float started;int view,frames;
        void Start()
        {
            foreach(var car in FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None))if(!cars[car.kind])cars[car.kind]=car;
            playerCar=FindAnyObjectByType<SimpleCar>();var player=FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;player.orbit.enabled=false;camera=Camera.main;started=Time.time;
        }
        void Update()
        {
            try
            {
                if(!frozen)
                {
                    if(Time.time-started<7)return;
                    Time.timeScale=.001f;FindAnyObjectByType<WestbruchTraffic>().enabled=false;
                    foreach(var person in FindObjectsByType<PedestrianAgent>()){person.agent.isStopped=true;person.enabled=false;}
                    foreach(var avatar in FindObjectsByType<PlayerAvatar>()){avatar.enabled=false;avatar.animator.enabled=false;}
                    foreach(var volume in FindObjectsByType<Volume>())foreach(var setting in volume.profile.components)if(setting is MotionBlur||setting is DepthOfField)setting.active=false;
                    camera.GetComponent<HDAdditionalCameraData>().antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
                    target=new RenderTexture(2048,1152,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;frozen=true;
                }
                bool interior=view>=20;int kind=interior?(view-20)/3:view/4,angle=interior?(view-20)%3:view%4,side=angle%2==0?-1:1,end=angle<2?1:-1;
                var subject=kind<4?cars[kind].transform:playerCar.transform;
                var at=subject.TransformPoint(new Vector3(0,.92f,0));var eye=subject.TransformPoint(new Vector3(side*3.7f,1.85f,end*3.6f));
                if(interior)
                {
                    float rear=kind==3?-2.08f:kind==2?-1.92f:kind==1?-1.43f:-1.50f;
                    if(angle==0){eye=subject.TransformPoint(new Vector3(.34f,1.32f,-.20f));at=subject.TransformPoint(new Vector3(.28f,.58f,.50f));}
                    else if(angle==1){eye=subject.TransformPoint(new Vector3(0,.82f,rear+.025f));at=subject.TransformPoint(new Vector3(.64f,.63f,rear-.12f));}
                    else{eye=subject.TransformPoint(new Vector3(.25f,1.30f,-.20f));at=subject.TransformPoint(new Vector3(-.5f,1.53f,-.65f));}
                }
                camera.nearClipPlane=interior?.025f:.1f;camera.fieldOfView=interior?83:43;camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(++frames<8)return;
                var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(2048,1152,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,2048,1152),0,0);pixels.Apply();
                Directory.CreateDirectory("QA");File.WriteAllBytes("QA/"+(interior?"interior-":"body-")+(kind<4?"type-"+kind:"player")+"-"+angle+".png",pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;frames=0;
                if(++view<35)return;camera.targetTexture=null;target.Release();Time.timeScale=1;Debug.Log("Thirty-five native vehicle views captured: exterior both sides/front/rear plus cabin/trunk/headliner.");EditorApplication.Exit(0);enabled=false;
            }
            catch(System.Exception error){Debug.LogException(error);Time.timeScale=1;EditorApplication.Exit(1);enabled=false;}
        }
    }
}
