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
    public static class PlayerLocomotionQA
    {
        const string Flag="ZeroRise.LocomotionQA";
        static PlayerLocomotionQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;
                SessionState.SetBool(Flag,false);new GameObject("Temporary locomotion verification").AddComponent<LocomotionTest>();
            };
        }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class LocomotionTest:MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public string gait,route;public int fps,contacts;
            public float hipsBob,maxVerticalSpeed,headingError,plantSpeedMean,plantSpeed95,plantDrift,maxPhaseError,maxGroundError,maxFloorPenetration,maxFootVerticalSpeed;
            public int falseAirStates,groundErrorFrame;public float groundErrorSpeed,groundErrorPhase;public string groundErrorState;public bool passed;
        }
        [Serializable] sealed class Report { public List<Sample> samples=new List<Sample>();public bool passed; }
        ThirdPersonMotor motor;PlayerAvatar avatar;CharacterController capsule;AnimationClip[] clips;
        readonly Vector3 origin=new Vector3(0,30.33f,-40);
        void Reset()
        {
            motor.SetDriving(false);capsule.enabled=false;motor.transform.SetPositionAndRotation(origin,Quaternion.identity);
            motor.visual.localRotation=Quaternion.identity;capsule.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator Start()
        {
            motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();avatar=motor.avatar;capsule=motor.GetComponent<CharacterController>();
            motor.enabled=false;motor.orbit.enabled=false;motor.orbit.transform.rotation=Quaternion.identity;motor.nearbyCar.enabled=false;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.enabled=false;
            var flat=GameObject.CreatePrimitive(PrimitiveType.Cube);flat.name="Temporary flat locomotion test support";
            flat.transform.position=new Vector3(0,30,0);flat.transform.localScale=new Vector3(100,.5f,180);Physics.SyncTransforms();
            clips=avatar.animator.runtimeAnimatorController.animationClips;var report=new Report();
            foreach(int fps in new[]{30,60,144})foreach(string gait in new[]{"Walk","Jog","Run"})
                yield return Route(report,gait,fps,"steady");
            foreach(int fps in new[]{30,60,144})foreach(string gait in new[]{"Walk","Jog","Run"})yield return Route(report,gait,fps,"diagonal-turn-reverse-stop");
            report.passed=report.samples.TrueForAll(sample=>sample.passed);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/player-locomotion-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Locomotion bob/slip/heading/phase QA "+(report.passed?"passed":"FAILED")+": "+JsonUtility.ToJson(report));EditorApplication.Exit(report.passed?0:1);
        }
        IEnumerator Route(Report report,string gait,int fps,string route)
        {
            Time.captureDeltaTime=1f/fps;Reset();
            bool run=gait=="Run",jog=gait=="Jog";
            for(int frame=0;frame<fps/2;frame++){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            for(int frame=0;frame<fps;frame++){motor.MoveStep(Vector2.up,run,false,Time.deltaTime,jog);yield return null;}
            var sample=new Sample{gait=gait,fps=fps,route=route};report.samples.Add(sample);
            float minHips=100,maxHips=-100,previousHips=avatar.hips.position.y-motor.transform.position.y;
            var previousFeet=new[]{avatar.leftFoot.position,avatar.rightFoot.position};var previousWeights=new float[2];var anchors=new Vector3[2];
            var speeds=new List<float>();
            var clip=Array.Find(clips,c=>c.name==gait);
            var curve=AnimationUtility.GetEditorCurve(clip,EditorCurveBinding.FloatCurve("Rig/Hips",typeof(Transform),"m_LocalPosition.y"));
            int frames=route=="steady"?fps*2:fps*5;
            for(int frame=0;frame<frames;frame++)
            {
                Vector2 input=Vector2.up;
                if(route!="steady")input=frame<fps?(Vector2.up+Vector2.right).normalized:frame<fps*2?Vector2.left:frame<fps*3?Vector2.right:frame<fps*4?(Vector2.down+Vector2.left).normalized:Vector2.zero;
                var before=motor.transform.position;motor.MoveStep(input,run,false,Time.deltaTime,jog);yield return null;
                var displacement=Vector3.ProjectOnPlane(motor.transform.position-before,Vector3.up);
                if(displacement.magnitude>.001f)sample.headingError=Mathf.Max(sample.headingError,Vector3.Angle(displacement,motor.visual.forward));
                float hips=avatar.hips.position.y-motor.transform.position.y;
                minHips=Mathf.Min(minHips,hips);maxHips=Mathf.Max(maxHips,hips);
                sample.maxVerticalSpeed=Mathf.Max(sample.maxVerticalSpeed,Mathf.Abs(hips-previousHips)*fps);previousHips=hips;
                var state=avatar.animator.GetCurrentAnimatorStateInfo(0);
                if(state.IsName("Jump")||state.IsName("Fall")||state.IsName("Land"))sample.falseAirStates++;
                if(route=="steady"&&!avatar.animator.IsInTransition(0)&&curve!=null)
                    sample.maxPhaseError=Mathf.Max(sample.maxPhaseError,Mathf.Abs(avatar.hips.localPosition.y-curve.Evaluate(avatar.GaitPhase*clip.length)));
                for(int side=0;side<2;side++)
                {
                    var foot=side==0?avatar.leftFoot:avatar.rightFoot;float weight=side==0?avatar.LeftFootPlantWeight:avatar.RightFootPlantWeight;
                    float sole=Mathf.Min(foot.TransformPoint(new Vector3(0,-.065f,-.065f)).y,foot.TransformPoint(new Vector3(0,-.065f,.145f)).y);
                    sample.maxFloorPenetration=Mathf.Max(sample.maxFloorPenetration,30.25f-sole);
                    sample.maxFootVerticalSpeed=Mathf.Max(sample.maxFootVerticalSpeed,Mathf.Abs(foot.position.y-previousFeet[side].y)*fps);
                    if(weight>.98f)
                    {
                        float error=Mathf.Abs(sole-30.25f);
                        if(error>sample.maxGroundError){sample.maxGroundError=error;sample.groundErrorFrame=frame;sample.groundErrorSpeed=avatar.ActualSpeed;sample.groundErrorPhase=avatar.GaitPhase;sample.groundErrorState=state.IsName("Idle")?"Idle":state.IsName("Locomotion")?"Locomotion":"Other";}
                    }
                    if(weight>.98f&&previousWeights[side]>.98f)
                    {
                        speeds.Add(Vector3.ProjectOnPlane(foot.position-previousFeet[side],Vector3.up).magnitude*fps);
                        sample.plantDrift=Mathf.Max(sample.plantDrift,Vector3.ProjectOnPlane(foot.position-anchors[side],Vector3.up).magnitude);
                    }
                    else anchors[side]=foot.position;
                    previousFeet[side]=foot.position;previousWeights[side]=weight;
                }
            }
            sample.hipsBob=maxHips-minHips;sample.contacts=speeds.Count;
            if(speeds.Count>0){foreach(float speed in speeds)sample.plantSpeedMean+=speed;sample.plantSpeedMean/=speeds.Count;speeds.Sort();sample.plantSpeed95=speeds[Mathf.Min(speeds.Count-1,Mathf.FloorToInt(speeds.Count*.95f))];}
            float bobLimit=gait=="Walk"?.025f:gait=="Jog"?.045f:.06f;
            float slipLimit=gait=="Walk"?.15f:gait=="Jog"?.25f:.45f;
            // Stop/turn transitions may change the base stance height, but cannot
            // introduce a jump-like vertical impulse or sideways skating.
            sample.passed=sample.contacts>2&&sample.headingError<5&&sample.falseAirStates==0&&sample.maxPhaseError<.001f&&sample.plantSpeed95<slipLimit&&sample.plantDrift<.03f&&sample.maxGroundError<.04f&&sample.maxFloorPenetration<.018f&&sample.maxFootVerticalSpeed<(gait=="Walk"?3.5f:gait=="Jog"?5:6)&&sample.maxVerticalSpeed<1.0f&&sample.hipsBob<(route=="steady"?bobLimit:.09f);
        }
    }
}
