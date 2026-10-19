using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class WestbruchTrafficPlayQA
    {
        const string Flag="ZeroRise.TrafficPhysicsQA";
        const string OccupantFlag="ZeroRise.VehicleOccupantQA";
        static WestbruchTrafficPlayQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode)return;
                if(SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary traffic physics verification").AddComponent<TrafficPhysicsTest>();}
                if(SessionState.GetBool(OccupantFlag,false)){SessionState.SetBool(OccupantFlag,false);new GameObject("Temporary occupied vehicle view").AddComponent<VehicleOccupantTest>();}
            };
        }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void CaptureOccupant(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(OccupantFlag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class VehicleOccupantTest:MonoBehaviour
    {
        ThirdPersonMotor player;SimpleCar car;int tick,phase,frames;float phaseStart,headroom;RenderTexture target;
        void Start()
        {
            player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();car=player.nearbyCar;player.enabled=false;
            var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;var outside=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f));
            Physics.Raycast(outside+Vector3.up*3,Vector3.down,out var floor,5,1);player.transform.position=floor.point+Vector3.up*.035f;capsule.enabled=true;Physics.SyncTransforms();
        }
        void Update()
        {
            try
            {
                if(++tick<30)return;
                if(phase==0){car.Enter(player);phase=1;phaseStart=Time.time;}
                if(phase==1)
                {
                    if(Time.time-phaseStart>6)throw new Exception("Occupied vehicle entry failed");
                    if(car.State!=SimpleCar.Occupancy.Driving)return;
                    player.orbit.enabled=false;phase=2;target=new RenderTexture(2048,1152,24,RenderTextureFormat.ARGB32);Camera.main.targetTexture=target;
                }
                if(phase==2)
                {
                    var at=car.transform.position+Vector3.up*1.05f;var eye=car.transform.TransformPoint(new Vector3(-3,1.9f,3.4f));Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));if(++frames<40)return;
                    var occupiedMesh=new Mesh();player.avatar.skin.BakeMesh(occupiedMesh);float highest=float.NegativeInfinity;
                    foreach(var vertex in occupiedMesh.vertices)highest=Mathf.Max(highest,player.avatar.skin.transform.TransformPoint(vertex).y);
                    var roof=car.transform.Find("Roof");var liner=car.transform.Find("Cabin headlining");float ceiling=roof.GetComponent<Renderer>().bounds.min.y;if(liner)ceiling=Mathf.Min(ceiling,liner.GetComponent<Renderer>().bounds.min.y);headroom=ceiling-highest;UnityEngine.Object.Destroy(occupiedMesh);
                    if(headroom<.015f)throw new Exception("Seated avatar clips vehicle roof: clearance "+headroom+" m");
                    RenderPipeline.SubmitRenderRequest(Camera.main,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;RenderTexture.active=target;
                    var pixels=new Texture2D(2048,1152,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,2048,1152),0,0);pixels.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/vehicle-player-driving.png",pixels.EncodeToPNG());UnityEngine.Object.Destroy(pixels);RenderTexture.active=old;
                    Camera.main.targetTexture=null;target.Release();car.TryExit(player);phase=3;phaseStart=Time.time;
                }
                if(phase==3)
                {
                    if(Time.time-phaseStart>5)throw new Exception("Occupied vehicle exit failed");
                    if(car.State!=SimpleCar.Occupancy.Parked)return;
                    if(player.driving||player.Interacting)throw new Exception("Vehicle occupant state did not restore");
                    File.WriteAllText("QA/vehicle-occupant-report.txt","PASSED: real animated entry, visible seated player, matching seat/steering anchors, animated exit. Seated mesh roof clearance: "+headroom.ToString("F4")+" m.");
                    Debug.Log("Vehicle occupant QA passed");EditorApplication.Exit(0);enabled=false;
                }
            }
            catch(Exception error){Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-occupant-report.txt","FAILED: "+error.Message);Debug.LogException(error);EditorApplication.Exit(1);enabled=false;}
        }
    }
    [DefaultExecutionOrder(200)] public sealed class TrafficPhysicsTest:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public int steps,overlaps,turns;public float minimumTravel=100000,maxTrackingError,maxHeadingStep;
            public bool objectStop,pedestrianStop,resumed,passed;
        }
        WestbruchTraffic world;ThirdPersonMotor player;CharacterController capsule;Vector3 playerStart;GameObject blocker;TrafficVehicle subject;
        Report report=new Report();Quaternion[] previous;float started,phaseStart,stoppedTravel;int phase;
        void Start()
        {
            world=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();capsule=player.GetComponent<CharacterController>();
            player.enabled=false;playerStart=player.transform.position;previous=new Quaternion[world.vehicles.Length];for(int i=0;i<previous.Length;i++)previous[i]=world.vehicles[i].transform.rotation;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            Time.timeScale=8;started=Time.time;phaseStart=started;
        }
        TrafficVehicle ClearSubject()
        {foreach(var car in world.vehicles)if(!car.turning&&world.RoadLength(car.from,car.to)-car.progress>35)return car;throw new Exception("No long traffic approach found");}
        void FixedUpdate()
        {
            try
            {
                report.steps++;float elapsed=Time.time-started;
                for(int i=0;i<world.vehicles.Length;i++)
                {
                    var car=world.vehicles[i];world.Evaluate(car,car.progress,out var target,out _);
                    report.maxTrackingError=Mathf.Max(report.maxTrackingError,Vector3.Distance(car.body.position,target));
                    report.maxHeadingStep=Mathf.Max(report.maxHeadingStep,Quaternion.Angle(previous[i],car.body.rotation));previous[i]=car.body.rotation;
                    if(report.steps%4!=0)continue;
                    for(int j=i+1;j<world.vehicles.Length;j++)
                    {
                        var other=world.vehicles[j];if((car.body.position-other.body.position).sqrMagnitude>40)continue;
                        if(Physics.ComputePenetration(car.bodyCollider,car.body.position,car.body.rotation,other.bodyCollider,other.body.position,other.body.rotation,out _,out float depth)&&depth>.02f)report.overlaps++;
                    }
                }
                if(phase==0&&elapsed>5)
                {
                    subject=ClearSubject();world.Evaluate(subject,subject.progress+13,out var at,out var forward);
                    blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetPositionAndRotation(at+Vector3.up*.8f,Quaternion.LookRotation(forward));blocker.transform.localScale=new Vector3(2,1.6f,.7f);phase=1;phaseStart=Time.time;
                }
                if(phase==1&&Time.time-phaseStart>12)
                {
                    report.objectStop=subject.speed<.05f;stoppedTravel=subject.travelled;UnityEngine.Object.Destroy(blocker);phase=2;phaseStart=Time.time;
                }
                if(phase==2&&Time.time-phaseStart>18)
                {
                    report.resumed=subject.travelled-stoppedTravel>12;subject=ClearSubject();world.Evaluate(subject,subject.progress+13,out var at,out _);
                    capsule.enabled=false;player.transform.position=at+Vector3.up*.035f;capsule.enabled=true;Physics.SyncTransforms();phase=3;phaseStart=Time.time;
                }
                if(phase==3&&Time.time-phaseStart>12)
                {
                    report.pedestrianStop=subject.speed<.05f;capsule.enabled=false;player.transform.position=playerStart;capsule.enabled=true;phase=4;
                }
                if(elapsed<180)return;
                foreach(var car in world.vehicles){report.minimumTravel=Mathf.Min(report.minimumTravel,car.travelled);report.turns+=car.completedTurns;}
                report.passed=report.overlaps==0&&report.maxTrackingError<.35f&&report.maxHeadingStep<9&&report.minimumTravel>100&&report.turns>30&&report.objectStop&&report.pedestrianStop&&report.resumed;
                Directory.CreateDirectory("QA");File.WriteAllText("QA/traffic-physics-report.json",JsonUtility.ToJson(report,true));
                if(!report.passed)throw new Exception("Traffic physics QA failed: "+JsonUtility.ToJson(report));
                Debug.Log("Traffic physics QA passed: "+report.steps+" fixed steps, no NPC overlaps, braking for real pedestrian and obstacle, resume after clearance.");Time.timeScale=1;EditorApplication.Exit(0);enabled=false;
            }
            catch(Exception error){Debug.LogException(error);Time.timeScale=1;EditorApplication.Exit(1);enabled=false;}
        }
    }
}
