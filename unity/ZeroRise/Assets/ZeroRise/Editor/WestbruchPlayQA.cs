using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class WestbruchPlayQA
    {
        const string Flag="ZeroRise.PlayQA";
        const string VegetationFlag="ZeroRise.VegetationQA";
        const string IsolationFlag="ZeroRise.DrivingQAIsolatedActors";
        static WestbruchPlayQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;
                SessionState.SetBool(Flag,false);
                var test=new GameObject("Temporary play QA").AddComponent<WestbruchDrivingTest>();
                test.vegetationOnly=SessionState.GetBool(VegetationFlag,false);SessionState.SetBool(VegetationFlag,false);
                test.isolateActors=SessionState.GetBool(IsolationFlag,false);SessionState.SetBool(IsolationFlag,false);
            };
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
        public static void RunVegetation() { SessionState.SetBool(VegetationFlag,true);Run(); }
        public static void RunWithoutResidents(){SessionState.SetBool(IsolationFlag,true);Run();}
    }
    public sealed class WestbruchDrivingTest:MonoBehaviour
    {
        public bool vegetationOnly;
        public bool isolateActors;
        SimpleCar car; ThirdPersonMotor player; Rigidbody body;
        int tick,driveTick;float peak,stopped;
        bool exitStarted;
        Vector3 started;bool entered,exited;float maxWheelRpm;
        [Serializable] class Result { public float speed,brakingSpeed,distance,wheelRpm;public bool enter,exit;public string result; }
        void Start()
        {
            if(isolateActors){foreach(var resident in UnityEngine.Object.FindObjectsByType<PedestrianAgent>())resident.gameObject.SetActive(false);var traffic=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();if(traffic)traffic.gameObject.SetActive(false);}
            car=UnityEngine.Object.FindAnyObjectByType<SimpleCar>();
            player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();
            body=car.GetComponent<Rigidbody>();car.enabled=false;
            // Use an unobstructed real road; exercise the actual colliders/wheels.
            car.transform.SetPositionAndRotation(new Vector3(-65,.5f,0),Quaternion.Euler(0,90,0));
            Physics.SyncTransforms();
        }
        void FixedUpdate()
        {
            try
            {
                tick++;
                if(vegetationOnly){WestbruchVegetationQA.Run(player,car);WestbruchShrubQA.Run(player,car);EditorApplication.Exit(0);enabled=false;return;}
                if(tick==40)
                {
                    var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;
                    var point=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f));
                    if(Physics.Raycast(point+Vector3.up*3,Vector3.down,out var ground,5,1))point=ground.point+Vector3.up*.035f;
                    player.transform.position=point;capsule.enabled=true;Physics.SyncTransforms();
                }
                if(tick<80){car.DriveStep(0,0,true);return;}
                if(tick==80){started=car.transform.position;car.Enter(player);}
                if(!entered)
                {
                    car.DriveStep(0,0,true);entered=player.driving&&car.State==SimpleCar.Occupancy.Driving&&player.orbit.target==car.transform;
                    if(tick>400&&!entered)throw new Exception("Animated car entry timed out");return;
                }
                driveTick++;
                if(driveTick<180)
                {
                    car.DriveStep(.7f,0,false);peak=Mathf.Max(peak,body.linearVelocity.magnitude);
                    foreach(var wheel in car.wheels)maxWheelRpm=Mathf.Max(maxWheelRpm,Mathf.Abs(wheel.rpm));
                    return;
                }
                car.DriveStep(0,0,true);
                if(driveTick<300)return;
                if(!exitStarted){stopped=body.linearVelocity.magnitude;car.TryExit(player);exitStarted=true;}
                if(car.State==SimpleCar.Occupancy.Exiting){if(driveTick>520)throw new Exception("Animated car exit timed out");return;}
                exited=!player.driving&&player.orbit.target==player.transform;
                var report=new Result { speed=peak,brakingSpeed=stopped,distance=Vector3.Distance(started,car.transform.position),wheelRpm=maxWheelRpm,enter=entered,exit=exited };
                bool pass=peak>2&&stopped<.8f&&maxWheelRpm>10&&entered&&exited;
                report.result=pass?"passed":"failed";
                Directory.CreateDirectory("QA");File.WriteAllText("QA/driving-report.json",JsonUtility.ToJson(report,true));
                Debug.Log("Westbruch driving QA "+report.result+": "+peak+" m/s, brake "+stopped+" m/s.");
                if(pass){WestbruchStreetFurnitureQA.Stand(player);WestbruchVegetationQA.Run(player,car);WestbruchShrubQA.Run(player,car);}
                EditorApplication.Exit(pass?0:1);enabled=false;
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);enabled=false;}
        }
    }
}
