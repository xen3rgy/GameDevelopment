using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    // Explicit release QA only. These controls and the temporary test barrier
    // never run in a normal session and nothing is written to the game scene.
    public sealed class GameplaySoundSmoke : MonoBehaviour
    {
        string output;bool failed,drive;int errors;
        float throttle,steer;bool brake;
        ThirdPersonMotor player;SimpleCar car;Rigidbody body;Camera camera;
        GameplayAudioDirector sound;
        readonly List<string> lines=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-game-sfx-qa");if(i<0)return;
            Launch(i+1<args.Length?args[i+1]:Application.persistentDataPath);
        }
        public static void Launch(string folder){var test=new GameObject("Explicit gameplay sound release QA").AddComponent<GameplaySoundSmoke>();test.output=folder;Directory.CreateDirectory(folder);}
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
        void Check(string label,bool pass,string detail)
        {
            failed|=!pass;lines.Add((pass?"PASS: ":"FAIL: ")+label+". "+detail);
            File.WriteAllText(Path.Combine(output,"game-sfx-windows-validation.txt"),"RUNNING\n"+string.Join("\n",lines));
        }
        IEnumerator Start()
        {
            for(int i=0;i<12;i++)yield return null;
            sound=GameplayAudioDirector.Active;player=FindAnyObjectByType<ThirdPersonMotor>();camera=Camera.main;
            Check("Full recorded sound library bootstraps in the Windows player",sound&&sound.library&&player&&camera,"Director, library, listener and real player");
            if(!sound||!player||!camera){Finish();yield break;}
            player.enabled=false;player.orbit.enabled=false;
            car=player.nearbyCar?player.nearbyCar:FindAnyObjectByType<SimpleCar>();body=car?car.GetComponent<Rigidbody>():null;
            if(!car||!body){Check("Actual driveable car exists",false,"");Finish();yield break;}
            var fx=car.GetComponent<VehicleSoundFx>();
            Check("Parked player car is silent before ignition",fx&&fx.Starts==0&&fx.EngineGain<.001f,"Ignitions "+(fx?fx.Starts:-1));
            yield return GameplaySoundFeedbackChecks.Run(player,camera,sound,Check,output);

            var recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,80);
            int steps=sound.PlayerSteps,jumps=sound.Jumps,landings=sound.Landings;
            foreach(var waypoint in new[]{new Vector2(111,70),new Vector2(110,73.5f),new Vector2(104,73.5f)})
            {
                float began=Time.time;
                while(Time.time-began<18)
                {
                    var delta=waypoint-new Vector2(player.transform.position.x,player.transform.position.z);if(delta.magnitude<.3f)break;
                    FollowPlayer();Move(delta.normalized,false);yield return null;
                }
            }
            for(int i=0;i<8;i++){FollowPlayer();Move(Vector2.zero,false);yield return null;}
            bool jump=true;float jumpAt=Time.time;
            while(Time.time-jumpAt<2.2f){FollowPlayer();Move(Vector2.zero,jump);jump=false;yield return null;}
            Check("Real walking and jump/landing actions are audible",sound.PlayerSteps-steps>8&&sound.Jumps>jumps&&sound.Landings>landings,
                "Steps "+(sound.PlayerSteps-steps)+"; jumps "+(sound.Jumps-jumps)+"; landings "+(sound.Landings-landings));
            EndTake(recorder,"sfx-walking-and-landing.wav","Walking/landing mixer capture");

            // Use the same authored road/lane as the established vehicle QA.
            drive=false;body.linearVelocity=body.angularVelocity=Vector3.zero;
            body.position=new Vector3(103,.32f,77.5f);body.rotation=Quaternion.Euler(0,90,0);
            car.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            var outside=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f));
            Physics.Raycast(outside+Vector3.up*3,Vector3.down,out var ground,6,1,QueryTriggerInteraction.Ignore);
            player.transform.position=ground.point+Vector3.up*.04f;controller.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<8;i++){FollowPlayer();Move(Vector2.zero,false);yield return null;}
            recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,100);
            car.Enter(player);float entryAt=Time.time;
            while(player.Interacting&&Time.time-entryAt<18){FollowVehicle();yield return null;}
            bool entered=player.driving&&car.State==SimpleCar.Occupancy.Driving;
            Check("Actual animated entry triggers both door edges and ignition",entered&&fx.Opens>0&&fx.Closes>0,"State "+car.State+"; doors "+fx.Opens+"/"+fx.Closes);
            if(!entered){EndTake(recorder,"sfx-car-actions.wav","Vehicle mixer capture");Finish();yield break;}
            float idleAt=Time.time;while(Time.time-idleAt<2.2f){FollowVehicle();yield return null;}
            Check("Stationary occupied car has audible idle and silent rolling tyres",fx.Starts>0&&fx.EngineGain>.05f&&fx.RollingGain<.005f,
                "Engine gain "+fx.EngineGain.ToString("F3")+"; rolling "+fx.RollingGain.ToString("F4"));
            fx.PlayHorn();
            drive=true;float beganDriving=Time.time;
            while(Time.time-beganDriving<24&&body.position.x<145)
            {
                float speed=body.linearVelocity.magnitude;throttle=speed<8.5f?.82f:0;brake=false;
                steer=Mathf.Clamp((77.5f-body.position.z)*.3f+Mathf.DeltaAngle(car.transform.eulerAngles.y,90)/25,-1,1);
                FollowVehicle();yield return null;
            }
            throttle=0;brake=true;
            float stopAt=Time.time;while(Time.time-stopAt<5&&body.linearVelocity.magnitude>.10f){FollowVehicle();yield return null;}
            Check("Driving excites recorded engine and contact-dependent tyre layers",fx.HighestRpm>1500&&fx.Shifts>0&&fx.RollingFrames>10&&body.position.x>140,
                "x "+body.position.x.ToString("F2")+"; highest audio RPM "+fx.HighestRpm.ToString("F0")+"; audio shifts "+fx.Shifts+"; rolling frames "+fx.RollingFrames+"; slip frames "+fx.SlipFrames);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"sfx-windows-driving.png"));yield return null;

            // A controlled, disposable collision checks the actual physics
            // event rather than directly calling an impact playback function.
            var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="QA-only disposable impact barrier";
            barrier.transform.position=body.position+car.transform.forward*6+Vector3.up*.7f;barrier.transform.localScale=new Vector3(.35f,1.5f,3.5f);
            int impacts=fx.Impacts;float impactBegan=Time.time;
            throttle=.5f;brake=false;steer=0;
            while(Time.time-impactBegan<6&&fx.Impacts==impacts){FollowVehicle();yield return null;}
            throttle=0;brake=true;
            stopAt=Time.time;while(Time.time-stopAt<3&&body.linearVelocity.magnitude>.1f){FollowVehicle();yield return null;}
            Check("Real vehicle collision produces body/seat foley",fx.Impacts>impacts,"New collision events "+(fx.Impacts-impacts));
            Destroy(barrier);drive=false;
            yield return SteeringFeedback(fx);
            car.TryExit(player);float exitAt=Time.time;
            while(player.Interacting&&Time.time-exitAt<18){FollowVehicle();yield return null;}
            for(int i=0;i<8;i++)yield return null;
            Check("Real exit stops engine and closes the actual door",!player.driving&&!player.Interacting&&fx.Stops>0&&fx.Opens>=2&&fx.Closes>=2&&fx.Horns>0,
                "Ignitions/stops "+fx.Starts+"/"+fx.Stops+"; doors "+fx.Opens+"/"+fx.Closes+"; horns "+fx.Horns);
            EndTake(recorder,"sfx-car-actions.wav","Vehicle mixer capture");

            recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,40);
            var cars=FindObjectsByType<TrafficSoundFx>();var residents=FindObjectsByType<PedestrianAgent>();
            var passing=cars.Length>0?cars[0]:null;
            if(passing)
            {
                float nearGain=0,middleGain=0,farGain=0;int middleSelected=0;
                foreach(float distance in new[]{5f,15f,30f})
                {
                    float until=Time.time+2.5f,peak=0;int selected=0;
                    while(Time.time<until)
                    {
                        camera.transform.position=passing.transform.position+passing.transform.right*distance+Vector3.up*1.8f;
                        camera.transform.LookAt(passing.transform.position+Vector3.up);
                        if(until-Time.time<1.3f){peak=Mathf.Max(peak,passing.EstimatedListenerGain);if(passing.audible)selected++;}
                        yield return null;
                    }
                    if(distance==5)nearGain=peak;if(distance==15){middleGain=peak;middleSelected=selected;}if(distance==30)farGain=peak;
                    lines.Add("Traffic listener probe "+distance+" m: maximum gain "+peak.ToString("F5")+"; selected voice frames "+selected);
                }
                Check("Nearby traffic is restrained and falls away with real distance",nearGain>.004f&&nearGain<.21f&&middleSelected>0&&middleGain<nearGain*.50f&&farGain<nearGain*.25f&&passing.engine.dopplerLevel==0,
                    "5m "+nearGain.ToString("F4")+"; 15m voiced "+middleGain.ToString("F5")+"; 30m "+farGain.ToString("F5")+"; engine range "+passing.engine.maxDistance+"m; Doppler "+passing.engine.dopplerLevel);
            }
            PedestrianAgent walker=null;foreach(var person in residents)if(person.avatar&&person.avatar.ActualSpeed>.4f){walker=person;break;}
            int npcBefore=sound.NpcSteps;
            if(walker)
            {
                float until=Time.time+9;
                while(Time.time<until){camera.transform.position=walker.transform.position-walker.transform.forward*3+Vector3.up*1.8f;camera.transform.LookAt(walker.transform.position+Vector3.up);yield return null;}
            }
            int trafficFrames=0,activeVoices=0;
            foreach(var moving in cars){trafficFrames+=moving.AudibleFrames;if(moving.audible)activeVoices++;}
            Check("Real traffic and nearby walking residents have spatial sound",trafficFrames>0&&sound.NpcSteps>npcBefore&&activeVoices<=4,
                "Traffic audible frames "+trafficFrames+"; NPC steps "+(sound.NpcSteps-npcBefore)+"; traffic voice budget "+activeVoices+"/4");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"sfx-windows-neighbourhood.png"));yield return null;
            EndTake(recorder,"sfx-traffic-and-residents.wav","Neighbourhood mixer capture");
            string release=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Westbruch-build.json");
            if(!Application.isEditor)Check("Launcher loads the corrected sound release",File.Exists(release)&&File.ReadAllText(release).Contains("contact sound feedback revision"),File.Exists(release)?File.ReadAllText(release):"Missing manifest");
            Finish();
        }
        IEnumerator SteeringFeedback(VehicleSoundFx fx)
        {
            var savedPosition=body.position;var savedRotation=body.rotation;
            int playerSteps=sound.PlayerSteps;
            var trace=new List<string>{"turn,time,x,z,speed,lateralSpeed,contactAngle,contacts,scrub,gain,impacts"};
            var recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,100);
            foreach(float turn in new[]{0f,-1f,1f})
            {
                drive=true;throttle=steer=0;brake=true;
                body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=new Vector3(69,.32f,turn>0?2.5f:-2.5f);body.rotation=Quaternion.Euler(0,90,0);
                car.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();
                for(int i=0;i<40;i++){FollowVehicle();yield return new WaitForFixedUpdate();}
                Check("Stationary tyres remain quiet before steering case "+turn,fx.SlipGain<.002f&&fx.RollingGain<.002f,"Slip gain "+fx.SlipGain.ToString("F4"));
                float until=Time.time+8;throttle=.8f;brake=false;
                while(Time.time<until&&body.position.x<84){FollowVehicle();yield return null;}
                int before=fx.LateralSlipFrames,impactBefore=fx.Impacts;float speed=fx.PlanarSpeed,peakGain=0,peakAngle=0,peakSide=0,peakScrub=0;int contacts=0;
                steer=turn==0?.055f:turn;throttle=turn==0?.12f:.48f;until=Time.time+2.2f;
                while(Time.time<until)
                {
                    if(turn==0&&body.position.x>96.5f)break;
                    peakGain=Mathf.Max(peakGain,fx.SlipGain);peakAngle=Mathf.Max(peakAngle,fx.WheelSlipAngleDegrees);
                    peakSide=Mathf.Max(peakSide,fx.LateralSpeed);peakScrub=Mathf.Max(peakScrub,fx.LateralScrub);contacts=Mathf.Max(contacts,fx.GroundedWheels);
                    trace.Add(FormattableString.Invariant($"{turn},{Time.time:F3},{body.position.x:F3},{body.position.z:F3},{fx.PlanarSpeed:F3},{fx.LateralSpeed:F3},{fx.WheelSlipAngleDegrees:F3},{fx.GroundedWheels},{fx.LateralScrub:F4},{fx.SlipGain:F4},{fx.Impacts-impactBefore}"));
                    FollowVehicle();yield return null;
                }
                string detail="Entry speed "+speed.ToString("F2")+" m/s; contacts "+contacts+"; contact angle "+peakAngle.ToString("F2")+"; lateral speed "+peakSide.ToString("F2")+"; scrub "+peakScrub.ToString("F3")+"; gain "+peakGain.ToString("F3")+"; lateral sound frames "+(fx.LateralSlipFrames-before)+"; body impacts "+(fx.Impacts-impactBefore)+"; end "+body.position.ToString("F2");
                if(turn==0)Check("Gentle real steering avoids tyre squeal",peakGain<.025f,detail);
                else Check("Actual "+(turn<0?"left":"right")+" hard steering produces lateral tyre feedback",speed>5&&contacts>=2&&peakScrub>.08f&&peakGain>.015f&&fx.LateralSlipFrames>before+2,detail);
                throttle=0;steer=0;brake=true;until=Time.time+2;
                while(Time.time<until){FollowVehicle();yield return null;}
                ScreenCapture.CaptureScreenshot(Path.Combine(output,turn<0?"sfx-hard-left.png":turn>0?"sfx-hard-right.png":"sfx-gentle-steering.png"));yield return null;
            }
            // Actual air gap, confined to the disposable QA session; no force,
            // tyre or grip settings are changed to manufacture a drift.
            drive=false;car.DriveStep(0,0,false);body.linearVelocity=body.angularVelocity=Vector3.zero;
            body.position=new Vector3(75,12,-2.5f);car.transform.position=body.position;Physics.SyncTransforms();
            for(int i=0;i<6;i++)yield return new WaitForFixedUpdate();
            Check("No tyre sound without physical wheel contacts",fx.GroundedWheels==0&&fx.SlipGain<.001f&&fx.RollingGain<.001f,"Contacts "+fx.GroundedWheels+"; slip "+fx.SlipGain.ToString("F4")+"; roll "+fx.RollingGain.ToString("F4"));
            Check("Seated driver emits no walking sounds",sound.PlayerSteps==playerSteps,"Additional footsteps "+(sound.PlayerSteps-playerSteps));
            body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=savedPosition;body.rotation=savedRotation;
            car.transform.SetPositionAndRotation(body.position,body.rotation);car.DriveStep(0,0,true);Physics.SyncTransforms();
            for(int i=0;i<40;i++){FollowVehicle();yield return new WaitForFixedUpdate();}
            EndTake(recorder,"sfx-corrected-steering.wav","Actual gentle/left/right steering capture");
            File.WriteAllLines(Path.Combine(output,"sfx-steering-contacts.csv"),trace);
        }
        void EndTake(GameplayAudioRecording recorder,string filename,string label){if(recorder)ValidateTake(recorder.End(Path.Combine(output,filename)),label);}
        void ValidateTake(GameplayAudioRecording.Stats stats,string label)=>Check(label,stats.frames>48000&&stats.channels==2&&stats.peak>.001f&&stats.peak<.93f&&stats.rms>.0001f,
            stats.seconds.ToString("F2")+"s; peak "+stats.peak.ToString("F5")+"; RMS "+stats.rms.ToString("F6")+"; channels "+stats.channels);
        void Finish(){lines.Add("Runtime errors: "+errors);failed|=errors>0;File.WriteAllText(Path.Combine(output,"game-sfx-windows-validation.txt"),"Passed: "+!failed+"\n"+string.Join("\n",lines));Application.Quit(failed?1:0);}
        void FollowPlayer(){camera.transform.position=player.transform.position+new Vector3(-3,2.6f,4);camera.transform.LookAt(player.transform.position+Vector3.up*1.1f);}
        void FollowVehicle(){camera.transform.position=car.transform.position-car.transform.forward*6.3f+car.transform.right*2.3f+Vector3.up*2.7f;camera.transform.LookAt(car.transform.position+Vector3.up*.8f);}
        void Move(Vector2 direction,bool jump)
        {
            var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,forward);
            var world=new Vector3(direction.x,0,direction.y);var input=new Vector2(Vector3.Dot(world,right),Vector3.Dot(world,forward));
            float remaining=Mathf.Min(Time.deltaTime,.5f);bool first=true;
            while(remaining>.00001f){float dt=Mathf.Min(.02f,remaining);player.MoveStep(input,false,jump&&first,dt);first=false;remaining-=dt;}
        }
        void FixedUpdate(){if(drive&&car)car.DriveStep(throttle,steer,brake);}
    }
}
