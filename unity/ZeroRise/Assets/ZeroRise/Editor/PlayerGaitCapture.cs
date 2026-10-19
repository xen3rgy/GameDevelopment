using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class PlayerGaitCapture
    {
        const string Flag="ZeroRise.GaitCapture";
        static PlayerGaitCapture()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;
                SessionState.SetBool(Flag,false);var capture=new GameObject("Temporary gait render capture").AddComponent<GaitRenderCapture>();capture.steeringOnly=SessionState.GetBool(Flag+"Steering",false);capture.directionOnly=SessionState.GetBool(Flag+"Direction",false);capture.idleOnly=SessionState.GetBool(Flag+"Idle",false);SessionState.SetBool(Flag+"Steering",false);SessionState.SetBool(Flag+"Direction",false);SessionState.SetBool(Flag+"Idle",false);
            };
        }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void RunSteering(){SessionState.SetBool(Flag+"Steering",true);Run();}
        public static void RunDirection(){SessionState.SetBool(Flag+"Direction",true);Run();}
        public static void RunIdle(){SessionState.SetBool(Flag+"Idle",true);Run();}
    }
    public sealed class GaitRenderCapture:MonoBehaviour
    {
        public bool steeringOnly,directionOnly,idleOnly;
        Camera cameraView;RenderTexture render;Texture2D pixels;
        void Save(string folder,int index)
        {
            Directory.CreateDirectory(folder);RenderPipeline.SubmitRenderRequest(cameraView,new RenderPipeline.StandardRequest{destination=render});
            var old=RenderTexture.active;RenderTexture.active=render;pixels.ReadPixels(new Rect(0,0,840,900),0,0);pixels.Apply();
            File.WriteAllBytes(folder+"/frame-"+index.ToString("000")+".png",pixels.EncodeToPNG());RenderTexture.active=old;
        }
        IEnumerator Start()
        {
            Time.captureDeltaTime=1f/60;
            var motor=Object.FindAnyObjectByType<ThirdPersonMotor>();var car=motor.nearbyCar;var avatar=motor.avatar;
            motor.enabled=false;motor.orbit.enabled=false;car.enabled=false;cameraView=Camera.main;cameraView.fieldOfView=36;
            render=new RenderTexture(840,900,24,RenderTextureFormat.ARGB32);cameraView.targetTexture=render;pixels=new Texture2D(840,900,TextureFormat.RGB24,false);
            var capsule=motor.GetComponent<CharacterController>();
            foreach(string gait in steeringOnly?System.Array.Empty<string>():idleOnly?new[]{"idle"}:directionOnly?new[]{"direction"}:new[]{"walk","jog","run","direction"})
            {
                motor.SetDriving(false);capsule.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(-65,.25f,-3),Quaternion.identity);motor.visual.localRotation=Quaternion.identity;capsule.enabled=true;avatar.ResetMotion();Physics.SyncTransforms();
                cameraView.transform.rotation=Quaternion.identity;
                for(int i=0;i<36;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                int frames=idleOnly?480:gait=="direction"?360:gait=="walk"?144:gait=="jog"?108:90,index=0;
                for(int i=0;i<frames;i++)
                {
                    var forward=Vector3.ProjectOnPlane(cameraView.transform.forward,Vector3.up).normalized;
                    var desired=Vector3.forward;
                    if(gait=="direction")desired=i<60?(Vector3.forward+Vector3.right).normalized:i<120?Vector3.left:i<180?Vector3.right:i<240?(Vector3.back+Vector3.left).normalized:i<300?Vector3.forward:Vector3.zero;
                    if(idleOnly)desired=i>=180&&i<240?Vector3.forward:Vector3.zero;
                    var input=new Vector2(Vector3.Dot(desired,cameraView.transform.right),Vector3.Dot(desired,forward));
                    motor.MoveStep(input,gait=="run"||(gait=="direction"&&i>=240&&i<300),false,Time.deltaTime,gait=="jog");yield return null;
                    var target=motor.transform.position+Vector3.up*.91f;var eye=motor.transform.position+new Vector3(2.4f,1.3f,2.2f);
                    cameraView.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
                    if(directionOnly||idleOnly||i%3==0)Save("QA/gait-"+gait+(directionOnly||idleOnly?"60":""),index++);
                }
            }
            if(directionOnly||idleOnly){Debug.Log("Actual 60 Hz "+(idleOnly?"idle":"direction-change")+" HDRP capture complete");cameraView.targetTexture=null;render.Release();EditorApplication.Exit(0);yield break;}
            motor.SetDriving(true);motor.transform.SetPositionAndRotation(car.seatAnchor.position,car.transform.rotation);motor.visual.localRotation=Quaternion.identity;
            avatar.BindSteeringWheel(car.steeringWheel);avatar.VehiclePose("Drive");car.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
            for(int i=0;i<30;i++){car.DriveStep(0,0,true);yield return null;}
            for(int i=0;i<144;i++)
            {
                car.DriveStep(0,Mathf.Sin(i/143f*Mathf.PI*2),true);motor.transform.SetPositionAndRotation(car.seatAnchor.position,car.transform.rotation);yield return null;
                var eye=car.transform.TransformPoint(new Vector3(-1.5f,1.6f,.75f));var target=car.transform.TransformPoint(new Vector3(-.35f,1.15f,.1f));
                cameraView.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
                if(i%3==0)Save("QA/gait-steering",i/3);
            }
            Debug.Log("Actual gait/steering HDRP motion captures complete");cameraView.targetTexture=null;render.Release();EditorApplication.Exit(0);
        }
    }
}
