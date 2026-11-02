using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    // Explicit native smoke test. No scene, player preferences or map changes are saved.
    public sealed class AdminFlightQA:MonoBehaviour
    {
        string output;bool passed=true;int errors;ThirdPersonMotor player;
        readonly List<string> results=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-admin-flight-qa");if(i<0)return;
            var qa=new GameObject("Admin flight native review").AddComponent<AdminFlightQA>();
            qa.output=i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"AdminFlightQA");Directory.CreateDirectory(qa.output);Application.runInBackground=true;
        }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Check(string label,bool value){passed&=value;results.Add((value?"PASS ":"FAIL ")+label);Debug.Log("ADMIN FLIGHT "+results[results.Count-1]);}
        void Place(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;}
        IEnumerator Fly(Vector2 input,float vertical,bool fast,float seconds)
        {float end=Time.time+seconds;while(Time.time<end){player.FlyStep(input,vertical,fast,Time.deltaTime);yield return null;}}
        IEnumerator Start()
        {
            yield return new WaitForSeconds(6);player=FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;
            var clock=WorldWeatherClock.Instance;clock.SuppressSave=true;clock.enabled=false;clock.SetHour(15);
            Check("Flight defaults off",!player.Flying);
            Place(new Vector3(170,45,265));Check("Flight can be enabled",player.SetFlying(true));
            Check("Flying cannot enter a car",!player.CanInteract&&!player.IsGrounded);
            var at=player.transform.position;yield return new WaitForSeconds(1);Check("Hover has no gravity drift",Vector3.Distance(at,player.transform.position)<.01f);
            yield return Fly(Vector2.zero,1,false,1);Check("Ascend moves character",player.transform.position.y>at.y+7.5f);
            at=player.transform.position;yield return Fly(Vector2.zero,-1,false,.5f);Check("Descend moves character",player.transform.position.y<at.y-3.5f);
            at=player.transform.position;yield return Fly(Vector2.up,0,false,.5f);float normal=Vector3.Distance(at,player.transform.position);Check("Camera relative flight moves",normal>3.5f&&normal<4.6f);
            at=player.transform.position;yield return Fly(Vector2.up,0,true,.5f);Check("Shift flight is faster",Vector3.Distance(at,player.transform.position)>normal*2.6f);
            at=player.transform.position;yield return Fly(Vector2.one,1,false,.5f);Check("Three axis diagonal is speed limited",Vector3.Distance(at,player.transform.position)<4.7f);
            var forward=Vector3.ProjectOnPlane(player.orbit.transform.forward,Vector3.up).normalized;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary flight collision probe";wall.transform.SetPositionAndRotation(player.transform.position+forward*3+Vector3.up,Quaternion.LookRotation(forward));wall.transform.localScale=new Vector3(20,20,.3f);Physics.SyncTransforms();
            at=player.transform.position;yield return Fly(Vector2.up,0,false,1);Check("Flight respects wall collision",Vector3.Distance(at,player.transform.position)<3);
            Destroy(wall);yield return null;
            player.ToggleFlight();Check("Shared F and menu toggle disables flight",!player.Flying&&player.VerticalSpeed==0);
            at=player.transform.position;float end=Time.time+4;
            while(Time.time<end){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
            Check("Normal gravity resumes",player.transform.position.y<at.y-10);
            Check("Controller lands on map",player.IsGrounded);
            player.driving=true;Check("Cannot enable while driving",!player.SetFlying(true)&&!player.Flying);player.driving=false;
            player.BeginInteraction();Check("Cannot enable during entry or exit",!player.SetFlying(true));player.SetDriving(false);
            player.ToggleFlight();Check("Flight can be enabled again",player.Flying);
            yield return Fly(Vector2.zero,1,false,5);player.enabled=true;player.orbit.Rotate(0,45);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-admin-flight.png"));yield return new WaitForSeconds(.4f);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-escape-flight-controls.png"));yield return new WaitForSeconds(.4f);
            Check("No runtime errors",errors==0);File.WriteAllText(Path.Combine(output,"validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",results));Application.Quit(passed?0:1);
        }
    }
}
