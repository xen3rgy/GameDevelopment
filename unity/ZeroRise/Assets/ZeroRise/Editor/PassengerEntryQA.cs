using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class PassengerEntryQA
    {
        const string Flag="ZeroRise.PassengerEntryQA";
        static PassengerEntryQA(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary passenger entry verification").AddComponent<PassengerEntryTest>();}};}
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    [DefaultExecutionOrder(220)] public sealed class PassengerEntryTest:MonoBehaviour
    {
        [Serializable] class Case {public string name;public bool entered,rightDoor,transfer,rightExit,reentered;}
        [Serializable] class Report
        {
            public List<Case> cases=new List<Case>();public bool bothBlocked,blockedAnimationWaited,blockedAnimationResumed,cancelRestored,exitCancelRestored,passed;
            public int transferFrames,contactFrames,consoleVertices;public float maximumPenetration,minimumHeadroom=100,maximumRootStep;public string failure;
        }
        readonly Report report=new Report();IEnumerator exercise;readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();readonly HashSet<string> consoleBones=new HashSet<string>();ThirdPersonMotor player;SimpleCar car;CharacterController cc;GameObject obstacle,second,lateBlocker;TrafficVehicle neighbour;Camera camera;Mesh baked;Vector3 last;bool observing;int capture;float clock;string pendingCapture;RenderTexture captureTarget;int captureFrames;float captureTimeScale;
        void Start(){exercise=Exercise();routines.Push(exercise);}
        void Update()
        {
            try
            {
                while(routines.Count>0)
                {
                    var top=routines.Peek();
                    if(!top.MoveNext()){routines.Pop();continue;}
                    if(top.Current is IEnumerator nested){routines.Push(nested);continue;}
                    return;
                }
                Finish();
            }
            catch(Exception e){report.failure=e.ToString();Debug.LogException(e);routines.Clear();Finish();}
        }
        void LateUpdate()
        {
            if(pendingCapture!=null){CaptureFrame();return;}
            if(!observing||!player)return;
            if(player.Interacting)
            {
                report.maximumRootStep=Mathf.Max(report.maximumRootStep,Vector3.Distance(last,player.transform.position));
                foreach(var obj in new[]{obstacle,second,lateBlocker})if(obj)foreach(var collider in obj.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger)
                {report.contactFrames++;if(Physics.ComputePenetration(cc,player.transform.position,player.transform.rotation,collider,collider.transform.position,collider.transform.rotation,out _,out float depth))report.maximumPenetration=Mathf.Max(report.maximumPenetration,depth);}
            }
            last=player.transform.position;
            if(car.SeatTransferProgress>=0)
            {
                report.transferFrames++;player.avatar.skin.BakeMesh(baked);
                float ceiling=car.transform.Find("Cabin headlining").GetComponent<Renderer>().bounds.min.y,highest=float.NegativeInfinity;
                var console=car.transform.Find("Center console").GetComponent<Renderer>().bounds;console.Expand(-.008f);
                var vertices=baked.vertices;var weights=player.avatar.skin.sharedMesh.boneWeights;
                for(int i=0;i<vertices.Length;i++)
                {
                    var world=player.avatar.skin.transform.TransformPoint(vertices[i]);highest=Mathf.Max(highest,world.y);
                    if(console.Contains(world))
                    {
                        report.consoleVertices++;string bone=player.avatar.skin.bones[weights[i].boneIndex0].name;
                        if(consoleBones.Add(bone))Debug.Log("CONSOLE CONTACT: "+bone+" progress="+car.SeatTransferProgress+" point="+car.transform.InverseTransformPoint(world));
                    }
                }
                report.minimumHeadroom=Mathf.Min(report.minimumHeadroom,ceiling-highest);
                if(capture==1&&car.SeatTransferProgress>.30f){Capture("passenger-transfer-outside",false);capture++;}
                if(capture==2&&car.SeatTransferProgress>.52f){Capture("passenger-transfer-inside",true);capture++;}
            }
            else if(capture==0&&car.InteractionSide>0&&car.State==SimpleCar.Occupancy.Entering&&car.ActionProgress>.45f){Capture("passenger-door-entry",false);capture++;}
        }
        IEnumerator Exercise()
        {
            Time.captureDeltaTime=1f/60;Time.timeScale=2;
            player=FindAnyObjectByType<ThirdPersonMotor>();car=player.nearbyCar;cc=player.GetComponent<CharacterController>();player.enabled=false;player.orbit.enabled=false;camera=Camera.main;
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
            foreach(var person in FindObjectsByType<PedestrianAgent>(FindObjectsSortMode.None))person.gameObject.SetActive(false);
            foreach(var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))foreach(var setting in volume.profile.components)if(setting is MotionBlur||setting is DepthOfField)setting.active=false;
            var traffic=FindAnyObjectByType<WestbruchTraffic>();neighbour=traffic.vehicles[1];traffic.enabled=false;
            foreach(var vehicle in traffic.vehicles)vehicle.gameObject.SetActive(false);
            var body=car.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=new Vector3(-65,.04f,0);body.rotation=Quaternion.Euler(0,90,0);car.transform.SetPositionAndRotation(body.position,body.rotation);baked=new Mesh();
            foreach(string kind in new[]{"wall","vehicle","crate","resident"})
            {
                Cleanup();yield return null;obstacle=Blocker(kind,-1);Place(1);yield return Grounded();observing=true;
                var row=new Case{name=kind};report.cases.Add(row);car.Enter(player);clock=Time.time;
                while(player.Interacting&&Time.time-clock<12){row.rightDoor|=car.InteractionSide>0&&car.DoorAngle>60;row.transfer|=car.SeatTransferProgress>.2f;yield return null;}
                row.entered=player.driving&&car.State==SimpleCar.Occupancy.Driving;Require(row.entered&&row.rightDoor&&row.transfer,"Passenger entry failed for "+kind+": "+player.InteractionHint);
                car.TryExit(player);clock=Time.time;bool exitTransfer=false;
                while(player.Interacting&&Time.time-clock<10){exitTransfer|=car.SeatTransferProgress>.2f;yield return null;}
                row.rightExit=!player.driving&&!player.Interacting&&cc.enabled&&car.InteractionSide>0&&exitTransfer;Require(row.rightExit,"Passenger exit failed for "+kind);
                yield return Grounded();car.Enter(player);clock=Time.time;while(player.Interacting&&Time.time-clock<12)yield return null;
                row.reentered=player.driving;Require(row.reentered,"Re-entry failed for "+kind);observing=false;
                car.enabled=false;yield return null;car.enabled=true;
            }
            Cleanup();yield return null;obstacle=Blocker("wall",-1);second=Blocker("wall",1);Place(1,2.8f);yield return Grounded();car.Enter(player);
            report.bothBlocked=car.State==SimpleCar.Occupancy.Parked&&!player.Interacting&&cc.enabled;Require(report.bothBlocked,"Both blocked doors disabled controls");
            Destroy(second);second=null;yield return null;Place(1);yield return Grounded();observing=true;car.Enter(player);
            clock=Time.time;while(car.ActionProgress<.18f&&Time.time-clock<5)yield return null;
            lateBlocker=Blocker("resident",1);lateBlocker.transform.position=car.transform.TransformPoint(new Vector3(1.15f,0,-.08f));Physics.SyncTransforms();clock=Time.time;while(Time.time-clock<1)yield return null;
            float progress=car.ActionProgress;var held=player.transform.position;clock=Time.time;while(Time.time-clock<.4f)yield return null;
            report.blockedAnimationWaited=player.Interacting&&Mathf.Abs(progress-car.ActionProgress)<.0001f&&Vector3.Distance(held,player.transform.position)<.002f;Require(report.blockedAnimationWaited,"Passenger door did not wait for arriving resident");
            Destroy(lateBlocker);lateBlocker=null;yield return null;clock=Time.time;while(player.Interacting&&Time.time-clock<10)yield return null;
            report.blockedAnimationResumed=player.driving;Require(report.blockedAnimationResumed,"Passenger boarding did not resume");
            car.TryExit(player);clock=Time.time;while(player.Interacting&&Time.time-clock<10)yield return null;yield return Grounded();car.Enter(player);
            clock=Time.time;while(car.SeatTransferProgress<.25f&&Time.time-clock<8)yield return null;
            Require(car.SeatTransferProgress>=.25f,"Cancellation never reached cabin transfer");typeof(SimpleCar).GetField("cancelRequested",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,true);
            clock=Time.time;while(player.Interacting&&Time.time-clock<8)yield return null;
            report.cancelRestored=!player.Interacting&&(player.driving||cc.enabled);Require(report.cancelRestored,"Cancelling cabin transfer trapped controls");
            Require(player.driving,"Cancelled seated entry did not restore driving");car.TryExit(player);
            clock=Time.time;while(car.SeatTransferProgress<.25f&&Time.time-clock<8)yield return null;
            Require(car.SeatTransferProgress>=.25f,"Exit cancellation never reached transfer");typeof(SimpleCar).GetField("cancelRequested",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,true);
            clock=Time.time;while(player.Interacting&&Time.time-clock<8)yield return null;
            report.exitCancelRestored=player.driving&&!player.Interacting&&!cc.enabled&&Vector3.Distance(player.transform.position,car.seatAnchor.position)<.02f;Require(report.exitCancelRestored,"Exit transfer cancellation failed");
            Require(report.consoleVertices==0,"Transfer feet clip centre console: "+report.consoleVertices);
            Require(report.maximumPenetration<.005f,"Passenger interaction penetrated external blocker");
            Require(report.minimumHeadroom>.005f,"Transfer clips headlining: "+report.minimumHeadroom);
            report.passed=true;
        }
        IEnumerator Grounded(){for(int i=0;i<12;i++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}}
        void Place(float side,float offset=1.68f)
        {
            observing=false;cc.enabled=false;var at=car.transform.TransformPoint(new Vector3(side*offset,0,-.44f));
            if(Physics.Raycast(at+Vector3.up*3,Vector3.down,out var hit,6,1,QueryTriggerInteraction.Ignore))at=hit.point+Vector3.up*.035f;
            player.transform.SetPositionAndRotation(at,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();last=at;
        }
        GameObject Blocker(string kind,float side)
        {
            GameObject go;
            if(kind=="vehicle"){go=Instantiate(neighbour.gameObject);go.SetActive(true);var rb=go.GetComponent<Rigidbody>();rb.isKinematic=true;go.GetComponent<TrafficVehicle>().enabled=false;go.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(side*2.03f,0,0)),car.transform.rotation);}
            else{go=new GameObject("Temporary "+kind);go.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(side*(kind=="wall"?1.5f:1.4f),0,-.44f)),car.transform.rotation);
                if(kind=="resident"){go.layer=9;var cap=go.AddComponent<CapsuleCollider>();cap.height=1.8f;cap.radius=.27f;cap.center=Vector3.up*.9f;}
                else{var box=go.AddComponent<BoxCollider>();box.center=Vector3.up*.9f;box.size=kind=="wall"?new Vector3(.2f,1.8f,2):new Vector3(.8f,1.8f,1.3f);}}
            Physics.SyncTransforms();return go;
        }
        void Cleanup(){if(obstacle)Destroy(obstacle);if(second)Destroy(second);if(lateBlocker)Destroy(lateBlocker);obstacle=second=lateBlocker=null;}
        void Capture(string name,bool inside)
        {
            // Settle rendering over real frames at an unchanged sampled pose.
            // Repeated requests in one frame keep stale motion vectors and
            // falsely smear the stopped car in these diagnostic images.
            pendingCapture=name;captureFrames=0;captureTimeScale=Time.timeScale;Time.timeScale=0;
            player.avatar.enabled=false;player.avatar.animator.enabled=false;
            captureTarget=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);
            camera.nearClipPlane=inside?.025f:.1f;camera.fieldOfView=inside?76:48;
            var eye=car.transform.TransformPoint(inside?new Vector3(.65f,1.32f,-.20f):new Vector3(3.5f,2.1f,3.8f));var aim=car.transform.TransformPoint(inside?new Vector3(-.22f,1.10f,-.15f):new Vector3(0,.95f,0));camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(aim-eye));
            camera.targetTexture=captureTarget;var data=camera.GetComponent<HDAdditionalCameraData>();data.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;
            data.customRenderingSettings=true;
            foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.DepthOfField})
            {data.renderingPathCustomFrameSettings.SetEnabled(field,false);data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
        }
        void CaptureFrame()
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=captureTarget});if(++captureFrames<10)return;
            var old=RenderTexture.active;RenderTexture.active=captureTarget;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/"+pendingCapture+".png",pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;camera.targetTexture=null;captureTarget.Release();Destroy(captureTarget);
            pendingCapture=null;player.avatar.animator.enabled=true;player.avatar.enabled=true;Time.timeScale=captureTimeScale;
        }
        static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
        void Finish(){Time.captureDeltaTime=0;Time.timeScale=1;Directory.CreateDirectory("QA");File.WriteAllText("QA/passenger-entry-report.json",JsonUtility.ToJson(report,true));Debug.Log("PASSENGER ENTRY QA: "+JsonUtility.ToJson(report));enabled=false;EditorApplication.Exit(report.passed?0:1);}
    }
}
