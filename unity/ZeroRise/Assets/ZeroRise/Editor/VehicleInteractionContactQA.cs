using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class VehicleInteractionContactQA
    {
        const string Flag="ZeroRise.VehicleInteractionContactQA";
        static VehicleInteractionContactQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary animated vehicle contact verification").AddComponent<VehicleInteractionContactTest>();}};
        }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    [DefaultExecutionOrder(220)] public sealed class VehicleInteractionContactTest:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public bool approachPaused,boardingPaused,exitPaused,approachResumed,boardingResumed,exitResumed,cleanupRestored,retainedApproachRecovered,retainedExitRecovered,passed;
            public int contactFrames;public float maximumPenetration,maximumHeldMovement,maximumHeldProgress;
            public string failure;
        }
        ThirdPersonMotor player;SimpleCar car;CharacterController controller;CapsuleCollider blocker,secondBlocker;IEnumerator exercise;
        readonly Report report=new Report();Vector3 outside,returnPoint;float start;
        void Start(){exercise=Exercise();}
        void Update()
        {
            try
            {
                if(exercise!=null&&!exercise.MoveNext()){exercise=null;Finish();}
                if(!player)return;
                Observe(blocker);Observe(secondBlocker);
            }
            catch(Exception error){report.failure=error.Message;Debug.LogException(error);exercise=null;Finish();}
        }
        IEnumerator Exercise()
        {
            Time.captureDeltaTime=1f/60;
            player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();car=player.nearbyCar;controller=player.GetComponent<CharacterController>();player.enabled=false;player.orbit.enabled=false;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();if(world)foreach(var person in world.people)person.gameObject.SetActive(false);
            var traffic=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();if(traffic)traffic.gameObject.SetActive(false);
            var body=car.GetComponent<Rigidbody>();body.linearVelocity=body.angularVelocity=Vector3.zero;body.constraints=RigidbodyConstraints.FreezeAll;
            outside=Floor(car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f)));returnPoint=Floor(outside-car.transform.forward*1.7f);PlacePlayer(returnPoint);
            for(int i=0;i<12;i++){controller.Move(Vector3.down*.04f);yield return null;}
            car.Enter(player);Require(car.State==SimpleCar.Occupancy.Entering,"Approach did not begin");
            yield return null;blocker=Actor(Floor(outside-car.transform.forward*.8f));
            start=Time.time;while(Time.time-start<1.2f)yield return null;
            var held=player.transform.position;float progress=car.ActionProgress;
            start=Time.time;while(Time.time-start<.5f)yield return null;
            report.approachPaused=Held(held,progress)&&car.State==SimpleCar.Occupancy.Entering&&progress==0;
            Require(report.approachPaused,"Animated approach passed through an actor");Remove(ref blocker);
            start=Time.time;while(car.State!=SimpleCar.Occupancy.Driving&&Time.time-start<8)yield return null;
            report.approachResumed=car.State==SimpleCar.Occupancy.Driving;Require(report.approachResumed,"Approach did not resume after actor cleared");
            car.TryExit(player);start=Time.time;while(car.State!=SimpleCar.Occupancy.Parked&&Time.time-start<6)yield return null;
            Require(car.State==SimpleCar.Occupancy.Parked,"Normal exit failed");
            PlacePlayer(outside);for(int i=0;i<6;i++){controller.Move(Vector3.down*.04f);yield return null;}
            car.Enter(player);start=Time.time;while(car.ActionProgress<.25f&&Time.time-start<5)yield return null;
            Require(car.State==SimpleCar.Occupancy.Entering,"Boarding did not begin");blocker=Actor(Floor(car.transform.TransformPoint(new Vector3(-1.2f,0,-.1f))));
            start=Time.time;while(Time.time-start<1.2f)yield return null;
            held=player.transform.position;progress=car.ActionProgress;
            start=Time.time;while(Time.time-start<.5f)yield return null;
            report.boardingPaused=Held(held,progress)&&car.State==SimpleCar.Occupancy.Entering&&progress<.76f;Require(report.boardingPaused,"Boarding passed through an actor");Remove(ref blocker);
            start=Time.time;while(car.State!=SimpleCar.Occupancy.Driving&&Time.time-start<6)yield return null;
            report.boardingResumed=car.State==SimpleCar.Occupancy.Driving;Require(report.boardingResumed,"Boarding did not resume");
            car.TryExit(player);start=Time.time;while(car.ActionProgress<.15f&&car.State==SimpleCar.Occupancy.Exiting&&Time.time-start<3)yield return null;
            blocker=Actor(Floor(outside+car.transform.forward*.34f));
            start=Time.time;while(Time.time-start<1.2f)yield return null;
            held=player.transform.position;progress=car.ActionProgress;
            start=Time.time;while(Time.time-start<.5f)yield return null;
            report.exitPaused=Held(held,progress)&&car.State==SimpleCar.Occupancy.Exiting&&progress<.72f;Require(report.exitPaused,"Animated exit passed through an actor");Remove(ref blocker);
            start=Time.time;while(car.State!=SimpleCar.Occupancy.Parked&&Time.time-start<6)yield return null;
            report.exitResumed=car.State==SimpleCar.Occupancy.Parked&&controller.enabled&&!player.Interacting&&!player.driving;Require(report.exitResumed,"Exit did not restore on-foot control");
            PlacePlayer(returnPoint);for(int i=0;i<6;i++){controller.Move(Vector3.down*.04f);yield return null;}
            car.Enter(player);start=Time.time;while(car.ActionProgress<.65f&&Time.time-start<6)yield return null;
            Require(car.State==SimpleCar.Occupancy.Entering,"Cleanup fixture did not reach boarding");
            blocker=Actor(returnPoint);secondBlocker=Actor(outside);car.enabled=false;yield return null;
            report.cleanupRestored=car.State==SimpleCar.Occupancy.Parked&&controller.enabled&&!player.Interacting&&!player.driving&&Vector3.Distance(player.transform.position,returnPoint)>.6f;
            Require(report.cleanupRestored,"Disabled vehicle restored player to an occupied origin");Observe(blocker);Observe(secondBlocker);
            Remove(ref blocker);Remove(ref secondBlocker);yield return null;car.enabled=true;
            PlacePlayer(returnPoint);for(int i=0;i<6;i++){controller.Move(Vector3.down*.04f);yield return null;}
            car.Enter(player);yield return null;blocker=Actor(Floor(outside-car.transform.forward*.8f));
            start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;
            report.retainedApproachRecovered=blocker&&car.State==SimpleCar.Occupancy.Parked&&controller.enabled&&!player.Interacting&&!player.driving;
            Require(report.retainedApproachRecovered,"Permanent approach blocker trapped on-foot controls");Remove(ref blocker);yield return null;
            PlacePlayer(outside);for(int i=0;i<6;i++){controller.Move(Vector3.down*.04f);yield return null;}
            car.Enter(player);start=Time.time;while(car.State!=SimpleCar.Occupancy.Driving&&Time.time-start<6)yield return null;
            Require(car.State==SimpleCar.Occupancy.Driving,"Entry before retained exit blocker failed");car.TryExit(player);
            start=Time.time;while(car.ActionProgress<.15f&&car.State==SimpleCar.Occupancy.Exiting&&Time.time-start<3)yield return null;
            blocker=Actor(Floor(outside+car.transform.forward*.34f));start=Time.time;
            while(player.Interacting&&Time.time-start<12)yield return null;
            report.retainedExitRecovered=blocker&&car.State==SimpleCar.Occupancy.Driving&&player.driving&&!player.Interacting&&!controller.enabled&&Vector3.Distance(player.transform.position,car.seatAnchor.position)<.02f;
            Require(report.retainedExitRecovered,"Permanent exit blocker trapped interaction controls");
            Require(report.contactFrames>30&&report.maximumPenetration<.005f,"Animated player penetrated an actor");
            report.passed=true;
        }
        bool Held(Vector3 before,float progress)
        {
            float movement=Vector3.Distance(before,player.transform.position),advance=Mathf.Abs(progress-car.ActionProgress);
            report.maximumHeldMovement=Mathf.Max(report.maximumHeldMovement,movement);report.maximumHeldProgress=Mathf.Max(report.maximumHeldProgress,advance);
            return movement<.002f&&advance<.0001f;
        }
        void Observe(CapsuleCollider actor)
        {
            if(!actor)return;report.contactFrames++;
            if(Physics.ComputePenetration(controller,player.transform.position,player.transform.rotation,actor,actor.transform.position,actor.transform.rotation,out _,out float depth))report.maximumPenetration=Mathf.Max(report.maximumPenetration,depth);
        }
        Vector3 Floor(Vector3 at)
        {
            if(!Physics.Raycast(at+Vector3.up*3,Vector3.down,out var hit,5,1,QueryTriggerInteraction.Ignore))throw new Exception("Contact fixture lacks ground");
            return hit.point+Vector3.up*.035f;
        }
        void PlacePlayer(Vector3 point){controller.enabled=false;player.transform.SetPositionAndRotation(point,Quaternion.identity);player.visual.rotation=car.transform.rotation;controller.enabled=true;Physics.SyncTransforms();}
        CapsuleCollider Actor(Vector3 at)
        {
            var actor=new GameObject("Temporary resident-sized interaction blocker");actor.layer=9;actor.transform.position=at;var capsule=actor.AddComponent<CapsuleCollider>();capsule.radius=.27f;capsule.height=1.8f;capsule.center=Vector3.up*.9f;Physics.SyncTransforms();return capsule;
        }
        void Remove(ref CapsuleCollider actor){if(actor)UnityEngine.Object.Destroy(actor.gameObject);actor=null;}
        static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
        void Finish()
        {
            Time.captureDeltaTime=0;Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-interaction-contact-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Vehicle interaction contact QA: "+JsonUtility.ToJson(report));enabled=false;EditorApplication.Exit(report.passed?0:1);
        }
    }
}
