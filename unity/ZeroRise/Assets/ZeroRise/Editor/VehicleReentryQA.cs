using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class VehicleReentryQA
    {
        const string Flag="ZeroRise.VehicleReentryQA";
        static VehicleReentryQA(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary curb re-entry regression").AddComponent<VehicleReentryTest>();}};}
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class VehicleReentryTest:MonoBehaviour
    {
        [Serializable] public class Case {public string name,hint;public bool grounded,entered,exited,reentered;public float rootHeight;public Vector3 stoppedAt;}
        [Serializable] class Report {public List<Case> cases=new List<Case>();public bool passed,actualDriveReentry,residentYielded,trafficProtectsBoarding,trafficProtectsExiting,wallRespected,residentAlternativeDoor;public float maximumApproachPenetration;public float drivenDistance,residentYieldDistance;public string failure,realEntryHint,residentDiagnostic;}
        Report report=new Report();IEnumerator routine;ThirdPersonMotor player;SimpleCar car;CharacterController cc;Rigidbody body;GameObject curb,actor;float start;bool driveControl;float throttle;
        void Start(){routine=Run();}
        void FixedUpdate(){if(driveControl)car.DriveStep(throttle,0,throttle==0);}
        void Update(){try{
            if(player&&car&&player.Interacting&&car.State==SimpleCar.Occupancy.Entering&&car.ActionProgress==0)
                foreach(var collider in car.GetComponentsInChildren<MeshCollider>())
                    if(collider.enabled&&Physics.ComputePenetration(cc,player.transform.position,player.transform.rotation,collider,collider.transform.position,collider.transform.rotation,out _,out float depth))report.maximumApproachPenetration=Mathf.Max(report.maximumApproachPenetration,depth);
            if(routine!=null&&!routine.MoveNext()){routine=null;Finish();}}catch(Exception e){report.failure=e.ToString();Debug.LogException(e);routine=null;Finish();}}
        IEnumerator Run()
        {
            Time.captureDeltaTime=1f/60;Time.timeScale=4;
            player=FindAnyObjectByType<ThirdPersonMotor>();car=player.nearbyCar;cc=player.GetComponent<CharacterController>();body=car.GetComponent<Rigidbody>();player.enabled=false;player.orbit.enabled=false;
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            foreach(var person in FindObjectsByType<PedestrianAgent>(FindObjectsSortMode.None))person.gameObject.SetActive(false);
            FindAnyObjectByType<WestbruchTraffic>().gameObject.SetActive(false);
            Box("Test road",new Vector3(0,19.9f,0),new Vector3(30,.2f,30));
            curb=Box("Test pavement",new Vector3(2.55f,20.09f,0),new Vector3(2.5f,.18f,14));curb.SetActive(false);
            // Grounded capsule starts, both sides and both ends. A pedestrian
            // on the opposite pavement must not prevent using the driver door.
            var starts=new[]{new Vector3(-1.68f,0,-.44f),new Vector3(1.68f,0,-.44f),new Vector3(-1.15f,0,2.3f),new Vector3(-1.15f,0,-2.3f),new Vector3(-1.68f,0,-.44f),new Vector3(-1.68f,0,-.44f),new Vector3(-1.68f,0,-.44f),new Vector3(-1.68f,0,-.44f)};
            var names=new[]{"flat-driver","flat-passenger","front-corner","rear-corner","curb-opposite-resident","rolled-road","pitched-road","resident-beside-bonnet"};
            for(int i=0;i<starts.Length;i++)
            {
                car.enabled=false;yield return null;car.enabled=true;
                body.constraints=RigidbodyConstraints.FreezeAll;body.linearVelocity=body.angularVelocity=Vector3.zero;
                var rotation=Quaternion.Euler(i==6?4:0,0,i==5?5:i==4?4:0);body.position=new Vector3(0,20.04f,0);body.rotation=rotation;car.transform.SetPositionAndRotation(body.position,rotation);
                curb.SetActive(i==4);
                if(actor)Destroy(actor);if(i==4||i==7){actor=new GameObject("Opposite-side resident");actor.layer=9;actor.transform.position=i==7?new Vector3(-1.46f,20.04f,.98f):new Vector3(1.68f,20.2f,-.44f);var c=actor.AddComponent<CapsuleCollider>();c.height=1.8f;c.radius=.27f;c.center=Vector3.up*.9f;}
                cc.enabled=false;player.transform.SetPositionAndRotation(new Vector3(starts[i].x,20.08f,starts[i].z),Quaternion.identity);player.visual.rotation=Quaternion.identity;cc.enabled=true;Physics.SyncTransforms();
                for(int f=0;f<15;f++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                var row=new Case{name=names[i],grounded=player.CanInteract,rootHeight=player.transform.position.y-20};report.cases.Add(row);
                car.Enter(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;row.entered=player.driving;row.hint=player.InteractionHint;row.stoppedAt=player.transform.position;
                if(row.entered){car.TryExit(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;row.exited=!player.driving&&!player.Interacting&&cc.enabled;
                    if(row.exited){for(int f=0;f<12;f++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}car.Enter(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;row.reentered=player.driving;row.hint=player.InteractionHint;}}
                Debug.Log("REENTRY CASE: "+JsonUtility.ToJson(row));
            }
            // The original fixtures froze the car. Also drive the actual
            // WheelColliders along the authored kerb, stop, exit and re-enter
            // from the far side with a real resident initially at the door.
            car.enabled=false;yield return null;car.enabled=true;if(actor)Destroy(actor);curb.SetActive(false);
            body.constraints=RigidbodyConstraints.None;body.linearVelocity=body.angularVelocity=Vector3.zero;
            body.position=new Vector3(-42,.35f,-84.3f);body.rotation=Quaternion.Euler(0,270,0);car.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
            start=Time.time;while(Time.time-start<2)yield return null;
            Place(Ground(car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f))));
            for(int f=0;f<12;f++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            car.Enter(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;
            if(!player.driving)throw new Exception("Actual kerb-side entry failed: "+player.InteractionHint);
            var began=body.position;driveControl=true;throttle=.8f;start=Time.time;while(Time.time-start<2.5f)yield return null;
            throttle=0;start=Time.time;while(Time.time-start<2)yield return null;driveControl=false;report.drivenDistance=Vector3.Distance(began,body.position);
            if(report.drivenDistance<2)throw new Exception("Actual wheel-driven fixture did not drive");
            car.TryExit(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;
            if(player.driving||player.Interacting)throw new Exception("Actual post-drive exit failed: "+player.InteractionHint);
            var doorway=Ground(car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f)));
            var resident=FindAnyObjectByType<PedestrianWorld>().people[0];resident.gameObject.SetActive(true);yield return null;
            if(!NavMesh.SamplePosition(doorway,out var nav,.8f,NavMesh.AllAreas))throw new Exception("Kerb fixture has no resident pavement at "+doorway);
            resident.agent.Warp(nav.position);resident.transform.position=nav.position;resident.destination=-1;resident.state=PedestrianAgent.Behaviour.WaitingForRoute;
            typeof(PedestrianAgent).GetMethod("AnchorHere",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(resident,null);
            typeof(PedestrianAgent).GetField("chooseAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(resident,Time.time+100);
            var residentFrom=resident.transform.position;
            Place(Ground(car.transform.TransformPoint(new Vector3(1.68f,0,-.44f))));
            for(int f=0;f<12;f++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            report.residentDiagnostic="Resident="+resident.transform.position+", car="+car.transform.position+", player="+player.transform.position+", grounded="+player.CanInteract+", onNav="+resident.agent.isOnNavMesh;
            car.Enter(player);report.realEntryHint=player.InteractionHint;start=Time.time;bool yielding=resident.state==PedestrianAgent.Behaviour.Yielding;
            while(player.Interacting&&Time.time-start<14){yielding|=resident.state==PedestrianAgent.Behaviour.Yielding;yield return null;}
            report.residentYieldDistance=Vector3.Distance(residentFrom,resident.transform.position);report.residentYielded=yielding&&report.residentYieldDistance>.8f;
            report.actualDriveReentry=player.driving&&!player.Interacting;report.residentAlternativeDoor=report.actualDriveReentry&&car.InteractionSide>0&&report.residentYieldDistance<.02f;
            car.enabled=false;yield return null;car.enabled=true;resident.gameObject.SetActive(false);
            Place(new Vector3(0,20.08f,9));cc.enabled=false;Physics.SyncTransforms();
            var traffic=FindAnyObjectByType<WestbruchTraffic>(FindObjectsInactive.Include);traffic.Initialize();
            var occupied=typeof(WestbruchTraffic).GetMethod("Occupied",BindingFlags.Instance|BindingFlags.NonPublic);
            var center=new Vector3(0,20,9);var args=new object[]{traffic.vehicles[0],center,Vector3.forward,2f};
            bool empty=!(bool)occupied.Invoke(traffic,args);player.BeginInteraction();bool protectedActor=(bool)occupied.Invoke(traffic,args);
            report.trafficProtectsBoarding=empty&&protectedActor;player.driving=true;report.trafficProtectsExiting=(bool)occupied.Invoke(traffic,args);player.SetDriving(false);
            body.constraints=RigidbodyConstraints.FreezeAll;body.position=new Vector3(0,20.04f,0);body.rotation=Quaternion.identity;car.transform.SetPositionAndRotation(body.position,body.rotation);
            Box("Blocked driver door wall",new Vector3(-1.5f,21,-.44f),new Vector3(.2f,2,2));Place(new Vector3(1.68f,20.08f,-.44f));
            for(int f=0;f<12;f++){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            car.Enter(player);bool rightDoor=false;start=Time.time;
            while(player.Interacting&&Time.time-start<12){rightDoor|=car.InteractionSide>0&&car.DoorAngle>60;yield return null;}
            report.wallRespected=player.driving&&rightDoor&&Mathf.Abs(Mathf.DeltaAngle(0,car.doors[0].localEulerAngles.y))<.01f;
            report.passed=report.cases.TrueForAll(c=>c.entered&&c.exited&&c.reentered)&&report.actualDriveReentry&&(report.residentYielded||report.residentAlternativeDoor)&&report.trafficProtectsBoarding&&report.trafficProtectsExiting&&report.wallRespected&&report.maximumApproachPenetration<.005f;
        }
        Vector3 Ground(Vector3 at)
        {
            var hits=Physics.RaycastAll(at+Vector3.up*3,Vector3.down,6,1,QueryTriggerInteraction.Ignore);float best=float.PositiveInfinity;var point=at;bool found=false;
            foreach(var h in hits)if(!h.transform.IsChildOf(car.transform)&&h.normal.y>.75f&&h.distance<best){best=h.distance;point=h.point+Vector3.up*.035f;found=true;}
            if(!found)throw new Exception("No real ground at "+at);return point;
        }
        void Place(Vector3 point){cc.enabled=false;player.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();}
        GameObject Box(string name,Vector3 at,Vector3 size){var go=new GameObject(name);go.transform.position=at;go.AddComponent<BoxCollider>().size=size;return go;}
        void Finish(){Time.captureDeltaTime=0;Time.timeScale=1;Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-reentry-report.json",JsonUtility.ToJson(report,true));Debug.Log("VEHICLE REENTRY QA: "+JsonUtility.ToJson(report));enabled=false;EditorApplication.Exit(report.passed?0:1);}
    }
}

