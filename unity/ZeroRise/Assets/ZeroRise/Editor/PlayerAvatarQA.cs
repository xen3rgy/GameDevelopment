using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class PlayerAvatarQA
    {
        const string Flag="ZeroRise.PlayerQA";
        static PlayerAvatarQA()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Flag,false))return;
                SessionState.SetBool(Flag,false);new GameObject("Temporary avatar play verification").AddComponent<PlayerAvatarPlayTest>();
            };
        }
        [Serializable] class ModelReport
        {
            public int bones,vertices,triangles,clips,visualColliders;
            public float maxPlantedFootError,maxLoopPositionError,maxLoopRotationError;
            public float walkPelvisTravel,jogPelvisTravel,runPelvisTravel;
            public string worstFootClip;public float worstFootPhase;
            public bool passed;
        }
        public static void Capture()
        {
            PlayerAvatarBuilder.Upgrade();
            var motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();var avatar=motor.avatar;var skin=avatar.skin;
            var report=new ModelReport{bones=skin.bones.Length,vertices=skin.sharedMesh.vertexCount,triangles=skin.sharedMesh.triangles.Length/3,visualColliders=avatar.GetComponentsInChildren<Collider>().Length};
            var controller=motor.GetComponent<CharacterController>();
            if(controller.height!=1.8f||controller.radius!=.3f||controller.center!=Vector3.up*.9f||avatar.animator.applyRootMotion)throw new Exception("Player movement/collision dimensions changed");
            if(!avatar.animator.avatar||!avatar.animator.avatar.isValid)throw new Exception("Generic player rig invalid");
            foreach(var weight in skin.sharedMesh.boneWeights)
                if(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)>.001f||weight.boneIndex0>=report.bones||weight.boneIndex1>=report.bones)throw new Exception("Player skin weights invalid");
            foreach(var mat in skin.sharedMaterials)
                if(!mat||!mat.shader||!mat.shader.isSupported)throw new Exception("Player material invalid");
            var clips=avatar.animator.runtimeAnimatorController.animationClips;report.clips=new HashSet<AnimationClip>(clips).Count;
            foreach(string name in new[]{"Walk","Jog","Run"})
            {
                var clip=Array.Find(clips,c=>c.name==name);float stance=name=="Walk"?PlayerGaitProfile.WalkStance:name=="Jog"?PlayerGaitProfile.JogStance:PlayerGaitProfile.RunStance;
                var footL=Array.Find(skin.bones,b=>b.name=="Foot_L");var footR=Array.Find(skin.bones,b=>b.name=="Foot_R");
                float minimumPelvis=float.PositiveInfinity,maximumPelvis=float.NegativeInfinity;
                for(int frame=0;frame<=120;frame++)
                {
                    float phase=frame/120f;clip.SampleAnimation(avatar.gameObject,clip.length*phase);
                    minimumPelvis=Mathf.Min(minimumPelvis,avatar.hips.localPosition.y);maximumPelvis=Mathf.Max(maximumPelvis,avatar.hips.localPosition.y);
                    for(int side=0;side<2;side++)
                    {
                        float f=Mathf.Repeat(phase+side*.5f,1);if(f>=stance)continue;
                        var foot=side==0?footL:footR;
                        float soleY=Mathf.Min(foot.TransformPoint(new Vector3(0,-.065f,-.065f)).y,foot.TransformPoint(new Vector3(0,-.065f,.145f)).y);
                        float error=Mathf.Abs(soleY-motor.transform.position.y);
                        if(error>report.maxPlantedFootError){report.maxPlantedFootError=error;report.worstFootClip=name;report.worstFootPhase=phase;}
                    }
                }
                float pelvisTravel=maximumPelvis-minimumPelvis;
                if(name=="Walk")report.walkPelvisTravel=pelvisTravel;else if(name=="Jog")report.jogPelvisTravel=pelvisTravel;else report.runPelvisTravel=pelvisTravel;
            }
            foreach(string name in new[]{"Idle","Walk","Jog","Run","Fall","TurnLeft","TurnRight","Drive"})
            {
                var clip=Array.Find(clips,c=>c.name==name);clip.SampleAnimation(avatar.gameObject,0);
                var p=new Vector3[skin.bones.Length];var q=new Quaternion[skin.bones.Length];
                for(int i=0;i<p.Length;i++){p[i]=skin.bones[i].localPosition;q[i]=skin.bones[i].localRotation;}
                clip.SampleAnimation(avatar.gameObject,clip.length-.00001f);
                for(int i=0;i<p.Length;i++)
                {
                    report.maxLoopPositionError=Mathf.Max(report.maxLoopPositionError,Vector3.Distance(p[i],skin.bones[i].localPosition));
                    report.maxLoopRotationError=Mathf.Max(report.maxLoopRotationError,Quaternion.Angle(q[i],skin.bones[i].localRotation));
                }
            }
            report.passed=report.bones>=20&&report.vertices>10000&&report.triangles<50000&&report.clips==15&&report.visualColliders==0&&report.maxPlantedFootError<.018f&&report.maxLoopPositionError<.001f&&report.maxLoopRotationError<.15f&&report.walkPelvisTravel<.025f&&report.jogPelvisTravel<.045f&&report.runPelvisTravel<.060f;
            Directory.CreateDirectory("QA");File.WriteAllText("QA/player-model-report.json",JsonUtility.ToJson(report,true));
            if(!report.passed)throw new Exception("Player model QA: "+JsonUtility.ToJson(report));
            var camera=Camera.main;var render=new RenderTexture(1200,1100,24,RenderTextureFormat.ARGB32);camera.targetTexture=render;
            var origin=motor.transform.position;
            if(Physics.Raycast(origin+Vector3.up*3,Vector3.down,out var ground,5,1))motor.transform.position=ground.point+Vector3.up*.008f;
            var position=motor.transform.position;
            string[] poses={"Idle","Idle","Idle","Walk","Run","Jump","EnterCar","Drive","ExitCar"};
            Vector3[] offsets={new Vector3(1.45f,1.32f,2.8f),new Vector3(-1.45f,1.35f,-2.8f),new Vector3(.30f,1.69f,.91f),new Vector3(2,1.2f,2.5f),new Vector3(-2,1.25f,2.4f),new Vector3(1.9f,1.2f,2.6f)};
            float[] times={0,0,0,.16f,.14f,.21f,.95f,0,.95f};int frames=0,view=0;
            var car=motor.nearbyCar;
            EditorApplication.CallbackFunction capture=null;
            capture=()=>
            {
                try
                {
                    var clip=Array.Find(clips,c=>c.name==poses[view]);clip.SampleAnimation(avatar.gameObject,times[view]);
                    if(view<6)
                    {
                        var target=position+Vector3.up*(view==2?1.61f:.92f);
                        camera.transform.SetPositionAndRotation(position+offsets[view],Quaternion.LookRotation(target-position-offsets[view]));
                    }
                    else
                    {
                        float t=times[view]/Array.Find(clips,c=>c.name==poses[view]).length;
                        float seated=view==7?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(view==8?.28f:.34f,view==8?.72f:.76f,t));if(view==8)seated=1-seated;
                        var outside=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f));if(Physics.Raycast(outside+Vector3.up*3,Vector3.down,out var floor,5,1))outside=floor.point+Vector3.up*.035f;
                        motor.transform.SetPositionAndRotation(Vector3.Lerp(outside,car.seatAnchor.position,seated),car.transform.rotation);motor.visual.localRotation=Quaternion.identity;
                        car.doors[0].localRotation=Quaternion.Euler(0,view==7?0:70,0);
                        if(view==7)
                        {
                            void Grip(Transform arm,Transform forearm,Transform hand,float side)
                            {
                                var grip=car.steeringWheel.TransformPoint(new Vector3(side*.19f,-.022f,0));var pole=arm.position+avatar.transform.right*(side*.3f)-avatar.transform.forward*.18f-Vector3.up*.2f;
                                AvatarLimbIK.Solve(arm,forearm,hand,grip,pole,car.steeringWheel.rotation*Quaternion.Euler(0,0,-side*90),1);
                            }
                            Grip(avatar.leftArm,avatar.leftForearm,avatar.leftHand,-1);Grip(avatar.rightArm,avatar.rightForearm,avatar.rightHand,1);
                        }
                        var target=car.transform.position+Vector3.up*1.0f;var eye=car.transform.TransformPoint(new Vector3(-3.5f,1.8f,2.9f));
                        camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
                    }
                    EditorApplication.QueuePlayerLoopUpdate();if(++frames<25)return;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=render});
                    var old=RenderTexture.active;RenderTexture.active=render;var image=new Texture2D(1200,1100,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,1200,1100),0,0);image.Apply();File.WriteAllBytes("QA/player-"+view+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
                    frames=0;if(++view<poses.Length)return;
                    camera.targetTexture=null;render.Release();UnityEngine.Object.DestroyImmediate(render);motor.transform.position=origin;
                    foreach(var mat in skin.sharedMaterials)foreach(var msg in ShaderUtil.GetShaderMessages(mat.shader))
                        if(msg.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(msg.message);
                    Debug.Log("Player model/pose QA passed: "+report.triangles+" triangles, foot error "+report.maxPlantedFootError);
                    EditorApplication.update-=capture;EditorApplication.Exit(0);
                }
                catch(Exception error){Debug.LogException(error);EditorApplication.update-=capture;EditorApplication.Exit(1);}
            };
            EditorApplication.update+=capture;
        }
        public static void Play()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
        public static void BuildWindows() { WestbruchQA.Validate();WestbruchBuilder.BuildWindows(); }
    }
    public sealed class PlayerAvatarPlayTest:MonoBehaviour
    {
        [Serializable] class Report
        {
            public float walkSpeed,jogSpeed,runSpeed,highFrameSpeed,jumpRise,blockedSpeed,idleMotion,walkLegMotion;
            public float maxGripError,maxShaftNormalError,maxContactSoleError;
            public bool contactCurveLow,contactCurveHigh;
            public bool jump,fall,land,idleAfterStop,idleAfterLanding,startUsesGait,stopSettlesIdle,frozenFeetTakeover,turning,visibleDriver,restoredAfterCar,blockedEntry,blockedExit,movingExitRejected,inputLocked,reentryRejected,cleanupRestored,passed;
            public float maxDoorAngle,seatError,minCameraDistance=100;
            public float minExitCameraDistance=100;
            public bool passengerExit;
            public string entryHint;
            public bool groundedBeforeJump;public float jumpFrameTime;
        }
        IEnumerator Start()
        {
            Time.captureDeltaTime=1f/60;
            var motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();var avatar=motor.avatar;var car=motor.nearbyCar;
            var report=new Report();motor.enabled=false;motor.orbit.enabled=false;car.enabled=false;
            void ObserveLocomotion()
            {
                if(avatar.ActualSpeed<=.1f)return;
                var state=avatar.animator.GetCurrentAnimatorStateInfo(0);var next=avatar.animator.GetNextAnimatorStateInfo(0);
                report.frozenFeetTakeover|=state.IsName("StartMove")||state.IsName("StopMove")||
                    (avatar.animator.IsInTransition(0)&&(next.IsName("StartMove")||next.IsName("StopMove")));
                report.startUsesGait|=state.IsName("Locomotion")&&avatar.ActualSpeed>.35f;
            }
            var controller=motor.GetComponent<CharacterController>();controller.enabled=false;
            motor.transform.position=new Vector3(-65,.08f,0);controller.enabled=true;Physics.SyncTransforms();
            motor.orbit.transform.rotation=Quaternion.identity;motor.visual.rotation=Quaternion.identity;avatar.ResetMotion();
            yield return null;
            float start=Time.time;
            while(Time.time-start<.3f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            var chest=Array.Find(avatar.skin.bones,b=>b.name=="Chest");float chestY=chest.localPosition.y;
            start=Time.time;
            // Observe a complete quiet breathing cycle; a short sample near
            // its turning point cannot distinguish subtle breathing from stillness.
            while(Time.time-start<3.5f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);report.idleMotion=Mathf.Max(report.idleMotion,Mathf.Abs(chest.localPosition.y-chestY));yield return null;}
            var thigh=Array.Find(avatar.skin.bones,b=>b.name=="Thigh_L");var oldLeg=thigh.localRotation;
            start=Time.time;
            while(Time.time-start<.8f)
            {
                motor.MoveStep(Vector2.up,false,false,Time.deltaTime);report.walkSpeed=Mathf.Max(report.walkSpeed,avatar.ActualSpeed);
                report.walkLegMotion=Mathf.Max(report.walkLegMotion,Quaternion.Angle(oldLeg,thigh.localRotation));yield return null;
                ObserveLocomotion();
                float contact=avatar.animator.GetFloat("LeftPlant");report.contactCurveLow|=contact<.1f;report.contactCurveHigh|=contact>.9f;
                if(avatar.LeftFootPlantWeight>.85f&&avatar.ActualSpeed>1.3f)
                {
                    var foot=avatar.leftFoot;float sole=Mathf.Min(foot.TransformPoint(new Vector3(0,-.065f,-.065f)).y,foot.TransformPoint(new Vector3(0,-.065f,.145f)).y);
                    if(Physics.Raycast(foot.position+Vector3.up*.3f,Vector3.down,out var floor,.7f,1))report.maxContactSoleError=Mathf.Max(report.maxContactSoleError,Mathf.Abs(sole-floor.point.y));
                }
            }
            start=Time.time;while(Time.time-start<.6f){motor.MoveStep(Vector2.up,false,false,Time.deltaTime,true);report.jogSpeed=Mathf.Max(report.jogSpeed,avatar.ActualSpeed);yield return null;ObserveLocomotion();}
            start=Time.time;
            while(Time.time-start<.6f){motor.MoveStep(Vector2.up,true,false,Time.deltaTime);report.runSpeed=Mathf.Max(report.runSpeed,avatar.ActualSpeed);yield return null;ObserveLocomotion();}
            Time.captureDeltaTime=1f/240;start=Time.time;
            while(Time.time-start<.5f){motor.MoveStep(Vector2.up,false,false,Time.deltaTime);report.highFrameSpeed=avatar.ActualSpeed;yield return null;ObserveLocomotion();}
            Time.captureDeltaTime=1f/60;
            start=Time.time;
            while(Time.time-start<.6f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;ObserveLocomotion();}
            report.stopSettlesIdle=avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")&&!avatar.animator.IsInTransition(0);
            report.idleAfterStop=report.stopSettlesIdle&&avatar.ActualSpeed<.05f&&avatar.animator.GetFloat("Speed")<.05f;
            // A key press is valid only after the controller has settled on the
            // kerb/pavement reached during the preceding movement checks.
            start=Time.time;
            while(!motor.IsGrounded&&Time.time-start<1){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            report.groundedBeforeJump=motor.IsGrounded;report.jumpFrameTime=Time.deltaTime;
            float beforeJump=motor.transform.position.y;motor.MoveStep(Vector2.zero,false,true,Time.deltaTime);
            start=Time.time;
            while(Time.time-start<1.15f)
            {
                motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);
                report.jumpRise=Mathf.Max(report.jumpRise,motor.transform.position.y-beforeJump);
                var state=avatar.animator.GetCurrentAnimatorStateInfo(0);report.jump|=state.IsName("Jump");report.fall|=state.IsName("Fall");report.land|=state.IsName("Land");yield return null;
            }
            report.idleAfterLanding=avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")&&avatar.animator.GetFloat("Speed")<.05f;
            // Walk into a temporary physical obstacle; animation must follow the
            // resulting speed, not the fact that a movement key remains held.
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary player movement obstacle";
            wall.transform.position=motor.transform.position+new Vector3(0,.9f,.4f);wall.transform.localScale=new Vector3(3,2,.1f);Physics.SyncTransforms();
            start=Time.time;
            while(Time.time-start<.4f){motor.MoveStep(Vector2.up,true,false,Time.deltaTime);yield return null;}
            report.blockedSpeed=avatar.ActualSpeed;UnityEngine.Object.Destroy(wall);yield return null;
            start=Time.time;
            while(Time.time-start<.6f){avatar.Motion(0,0,true,Time.deltaTime,.8f);report.turning|=avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Turning");yield return null;}
            controller.enabled=false;motor.transform.position=new Vector3(-65,0,5);controller.enabled=true;
            car.transform.SetPositionAndRotation(new Vector3(-65,.5f,0),Quaternion.Euler(0,90,0));var body=car.GetComponent<Rigidbody>();body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
            start=Time.time;while(Time.time-start<1.5f){car.DriveStep(0,0,true);yield return null;}
            controller.enabled=false;motor.transform.position=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f))+Vector3.up*.15f;controller.enabled=true;Physics.SyncTransforms();
            start=Time.time;while(Time.time-start<.35f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Temporary door blocker";blocker.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(-1.4f,.95f,.36f)),car.transform.rotation);blocker.transform.localScale=new Vector3(.4f,1.8f,.4f);Physics.SyncTransforms();
            var rightBlocker=UnityEngine.Object.Instantiate(blocker);rightBlocker.transform.position=car.transform.TransformPoint(new Vector3(1.4f,.95f,.36f));Physics.SyncTransforms();
            car.Enter(motor);report.blockedEntry=car.State==SimpleCar.Occupancy.Parked&&!motor.Interacting;
            UnityEngine.Object.Destroy(rightBlocker);
            UnityEngine.Object.Destroy(blocker);yield return null;Physics.SyncTransforms();
            motor.orbit.enabled=true;car.Enter(motor);report.entryHint=motor.InteractionHint;report.reentryRejected=car.State==SimpleCar.Occupancy.Entering;car.Enter(motor);
            start=Time.time;
            while(car.State==SimpleCar.Occupancy.Entering&&Time.time-start<5)
            {
                var position=motor.transform.position;motor.MoveStep(Vector2.up,true,true,Time.deltaTime);report.inputLocked|=motor.Interacting&&motor.transform.position==position;
                car.DriveStep(1,1,false);report.maxDoorAngle=Mathf.Max(report.maxDoorAngle,car.DoorAngle);
                report.minCameraDistance=Mathf.Min(report.minCameraDistance,Vector3.Distance(motor.orbit.transform.position,car.transform.position));yield return null;
            }
            yield return new WaitForSeconds(.25f);
            report.seatError=Vector3.Distance(motor.transform.position,car.seatAnchor.position);
            report.visibleDriver=motor.driving&&motor.visual.gameObject.activeSelf&&avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Drive")&&!controller.enabled&&car.DoorAngle<.01f;
            var shaft=car.transform.rotation*Quaternion.Euler(65,0,0)*Vector3.up;
            foreach(float steer in new[]{-1f,0f,1f,0f})
            {
                start=Time.time;
                while(Time.time-start<.6f){car.DriveStep(0,steer,true);yield return null;}
                yield return null;
                report.maxShaftNormalError=Mathf.Max(report.maxShaftNormalError,Vector3.Angle(shaft,car.steeringWheel.up));
                report.maxGripError=Mathf.Max(report.maxGripError,Vector3.Distance(avatar.leftHand.position,car.steeringWheel.TransformPoint(new Vector3(-.19f,-.022f,0))),Vector3.Distance(avatar.rightHand.position,car.steeringWheel.TransformPoint(new Vector3(.19f,-.022f,0))));
            }
            for(int i=0;i<24;i++)
            {
                motor.orbit.Rotate(15,0);yield return null;yield return null;
                report.minCameraDistance=Mathf.Min(report.minCameraDistance,Vector3.Distance(motor.orbit.transform.position,car.transform.position));
            }
            body.linearVelocity=car.transform.forward*2;car.TryExit(motor);report.movingExitRejected=car.State==SimpleCar.Occupancy.Driving;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            var barriers=new GameObject[2];
            for(int i=0;i<2;i++){barriers[i]=GameObject.CreatePrimitive(PrimitiveType.Cube);barriers[i].transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(i==0?-1.6f:1.6f,.95f,-.2f)),car.transform.rotation);barriers[i].transform.localScale=new Vector3(.45f,1.8f,1.5f);}
            Physics.SyncTransforms();car.TryExit(motor);report.blockedExit=car.State==SimpleCar.Occupancy.Driving&&motor.driving;
            foreach(var barrier in barriers)UnityEngine.Object.Destroy(barrier);yield return null;Physics.SyncTransforms();car.TryExit(motor);
            start=Time.time;while(car.State==SimpleCar.Occupancy.Exiting&&Time.time-start<4){car.DriveStep(0,0,true);yield return null;}
            start=Time.time;while(Time.time-start<.5f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            report.restoredAfterCar=!motor.driving&&!motor.Interacting&&motor.visual.gameObject.activeSelf&&controller.enabled&&motor.orbit.target==motor.transform&&avatar.animator.isActiveAndEnabled;
            for(int i=0;i<24;i++){motor.orbit.Rotate(15,0);yield return null;yield return null;report.minExitCameraDistance=Mathf.Min(report.minExitCameraDistance,Vector3.Distance(motor.orbit.transform.position,motor.transform.position));}
            car.Enter(motor);start=Time.time;while(car.State==SimpleCar.Occupancy.Entering&&Time.time-start<5){car.DriveStep(0,0,true);yield return null;}
            var leftBlock=GameObject.CreatePrimitive(PrimitiveType.Cube);leftBlock.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(-1.6f,.95f,-.2f)),car.transform.rotation);leftBlock.transform.localScale=new Vector3(.45f,1.8f,1.5f);Physics.SyncTransforms();car.TryExit(motor);
            start=Time.time;while(car.State==SimpleCar.Occupancy.Exiting&&Time.time-start<4){car.DriveStep(0,0,true);yield return null;}
            report.passengerExit=!motor.driving&&car.transform.InverseTransformPoint(motor.transform.position).x>1.3f;
            UnityEngine.Object.Destroy(leftBlock);yield return null;
            // Return to the driver side for interruption cleanup verification.
            controller.enabled=false;motor.transform.position=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f))+Vector3.up*.08f;controller.enabled=true;Physics.SyncTransforms();
            start=Time.time;while(Time.time-start<.4f){motor.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            car.Enter(motor);yield return null;car.enabled=true;car.enabled=false;yield return null;
            report.cleanupRestored=!motor.driving&&!motor.Interacting&&controller.enabled&&car.State==SimpleCar.Occupancy.Parked;
            report.passed=report.walkSpeed>1.35f&&report.walkSpeed<1.55f&&report.jogSpeed>3&&report.jogSpeed<3.4f&&report.runSpeed>5.6f&&report.runSpeed<6&&report.highFrameSpeed>1.35f&&report.highFrameSpeed<1.55f&&report.jumpRise>.35f&&report.jump&&report.fall&&report.land&&report.idleAfterStop&&report.idleAfterLanding&&report.visibleDriver&&report.restoredAfterCar&&report.blockedSpeed<.05f&&report.idleMotion>.0003f&&report.walkLegMotion>15&&report.startUsesGait&&report.stopSettlesIdle&&!report.frozenFeetTakeover&&report.turning&&report.maxDoorAngle>60&&report.seatError<.06f&&report.minCameraDistance>2.5f&&report.minExitCameraDistance>1.6f&&report.blockedEntry&&report.blockedExit&&report.movingExitRejected&&report.inputLocked&&report.reentryRejected&&report.cleanupRestored&&report.passengerExit&&report.maxGripError<.015f&&report.maxShaftNormalError<.1f&&report.maxContactSoleError<.035f&&report.contactCurveLow&&report.contactCurveHigh;
            Directory.CreateDirectory("QA");File.WriteAllText("QA/player-motion-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Player motion QA "+(report.passed?"passed":"FAILED")+": "+JsonUtility.ToJson(report));EditorApplication.Exit(report.passed?0:1);
        }
    }
}
