using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class PlayerIdleQA
    {
        const string Flag="ZeroRise.IdleQA";
        static PlayerIdleQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;
                SessionState.SetBool(Flag,false);new GameObject("Temporary long idle verification").AddComponent<IdleTest>();
            };
        }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class IdleTest:MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public string setup;public int fps,unexpectedStates,footLiftFrames;
            public float leftFootTravel,rightFootTravel,maxFootSpeed,maxFloorError,rootTravel,chestTravel;
            public bool idleLook,passed;
        }
        [Serializable] sealed class Report { public List<Sample> samples=new List<Sample>();public bool passed; }
        ThirdPersonMotor motor;PlayerAvatar avatar;CharacterController capsule;GameObject flat;
        IEnumerator Start()
        {
            motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();avatar=motor.avatar;capsule=motor.GetComponent<CharacterController>();
            motor.enabled=false;motor.orbit.enabled=false;motor.nearbyCar.enabled=false;motor.orbit.transform.rotation=Quaternion.identity;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            var report=new Report();var savedPosition=motor.transform.position;
            yield return Check(report,60,"saved district spawn",savedPosition,false);
            flat=GameObject.CreatePrimitive(PrimitiveType.Cube);flat.name="Temporary idle support";flat.transform.position=new Vector3(0,30,0);flat.transform.localScale=new Vector3(100,.5f,180);Physics.SyncTransforms();
            foreach(int fps in new[]{30,60,144})
            {
                yield return Check(report,fps,"fresh flat idle",new Vector3(0,30.36f,0),false);
                yield return Check(report,fps,"idle after sprint",new Vector3(0,30.36f,-10),true);
            }
            flat.transform.rotation=Quaternion.Euler(0,0,8);Physics.SyncTransforms();
            yield return Check(report,60,"idle on 8 degree side slope",new Vector3(0,30.36f,0),false);
            report.passed=report.samples.TrueForAll(s=>s.passed);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/player-idle-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Long idle QA "+(report.passed?"passed":"FAILED")+": "+JsonUtility.ToJson(report));EditorApplication.Exit(report.passed?0:1);
        }
        IEnumerator Check(Report report,int fps,string setup,Vector3 position,bool afterRun)
        {
            Time.captureDeltaTime=1f/fps;motor.SetDriving(false);capsule.enabled=false;motor.transform.SetPositionAndRotation(position,Quaternion.identity);motor.visual.localRotation=Quaternion.identity;capsule.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<fps*2;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            if(afterRun)
            {
                for(int i=0;i<fps;i++){motor.MoveStep(Vector2.up,true,false,Time.deltaTime);yield return null;}
                for(int i=0;i<fps*2;i++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            }
            var sample=new Sample{setup=setup,fps=fps};report.samples.Add(sample);
            var a=avatar.leftFoot.position;var b=avatar.rightFoot.position;var previousA=a;var previousB=b;var root=motor.transform.position;
            float chestMin=100,chestMax=-100;
            // Includes several breathing loops and both sides of the 18 s
            // look-around trigger. Measurements include EVERY foot frame,
            // including recovery/swing frames that contact tests exclude.
            for(int i=0;i<fps*24;i++)
            {
                motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;
                var state=avatar.animator.GetCurrentAnimatorStateInfo(0);
                if(!state.IsName("Idle")&&!state.IsName("IdleLook"))sample.unexpectedStates++;
                sample.idleLook|=state.IsName("IdleLook");
                sample.leftFootTravel=Mathf.Max(sample.leftFootTravel,Vector3.Distance(a,avatar.leftFoot.position));
                sample.rightFootTravel=Mathf.Max(sample.rightFootTravel,Vector3.Distance(b,avatar.rightFoot.position));
                sample.maxFootSpeed=Mathf.Max(sample.maxFootSpeed,Vector3.Distance(previousA,avatar.leftFoot.position)*fps,Vector3.Distance(previousB,avatar.rightFoot.position)*fps);
                previousA=avatar.leftFoot.position;previousB=avatar.rightFoot.position;
                sample.rootTravel=Mathf.Max(sample.rootTravel,Vector3.Distance(root,motor.transform.position));
                float chestY=avatar.chest.position.y-motor.transform.position.y;chestMin=Mathf.Min(chestMin,chestY);chestMax=Mathf.Max(chestMax,chestY);
                foreach(var foot in new[]{avatar.leftFoot,avatar.rightFoot})
                {
                    var heel=foot.TransformPoint(new Vector3(0,-.065f,-.065f));var toe=foot.TransformPoint(new Vector3(0,-.065f,.145f));
                    float sole=Mathf.Min(heel.y,toe.y);
                    if(Physics.Raycast(foot.position+Vector3.up*.25f,Vector3.down,out var ground,.6f,1))
                    {
                        float error=Mathf.Abs(sole-ground.point.y);sample.maxFloorError=Mathf.Max(sample.maxFloorError,error);
                        if(error>.015f)sample.footLiftFrames++;
                    }
                }
            }
            sample.chestTravel=chestMax-chestMin;
            sample.passed=sample.unexpectedStates==0&&sample.footLiftFrames==0&&sample.leftFootTravel<.005f&&sample.rightFootTravel<.005f&&sample.maxFootSpeed<.08f&&sample.rootTravel<.002f&&sample.chestTravel>.0002f&&sample.chestTravel<.012f&&sample.maxFloorError<.015f&&sample.idleLook;
        }
    }
}
