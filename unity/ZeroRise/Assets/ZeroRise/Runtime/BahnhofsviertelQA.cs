using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace ZeroRise
{
    // Explicit standalone integration test; never changes the player's saved world.
    public sealed class BahnhofsviertelQA:MonoBehaviour
    {
        string output;int errors;bool passed=true;readonly List<string> report=new List<string>();Camera camera;ThirdPersonMotor player;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-bahnhof-qa");if(i<0)return;var qa=new GameObject("Bahnhof integration review").AddComponent<BahnhofsviertelQA>();qa.output=i+1<args.Length?args[i+1]:Path.Combine(Application.persistentDataPath,"BahnhofQA");Directory.CreateDirectory(qa.output);Application.runInBackground=true;}
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.Add("ERROR "+text);}}
        void Check(string title,bool value,string detail=""){passed&=value;report.Add((value?"PASS ":"FAIL ")+title+" | "+detail);File.WriteAllLines(Path.Combine(output,"progress.txt"),report);Debug.Log("BAHNHOF "+report[report.Count-1]);}
        IEnumerator Start()
        {
            Time.timeScale=1;yield return new WaitForSeconds(6);camera=Camera.main;player=FindAnyObjectByType<ThirdPersonMotor>();
            var clock=WorldWeatherClock.Instance;clock.SuppressSave=true;clock.enabled=false;clock.SetSpeed(1);clock.SetWeather(WeatherKind.Clear);clock.Advance(14);clock.SetHour(15.5f);
            var orbit=camera.GetComponent<OrbitCamera>();if(orbit)orbit.enabled=false;player.enabled=false;
            var district=GameObject.Find("Bahnhofsviertel – connected station neighbourhood");Check("Saved expansion present",district);
            Check("Original Westbruch retained",GameObject.Find("Uferhof_12")&&GameObject.Find("Westbruch concrete overpass"));
            Check("Two elevated platforms",FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Station platform")==2);
            Check("Station walk-through hall",GameObject.Find("Bahnhof Westbruch – accessible ticket hall"));
            Check("Connected traffic graph",FindAnyObjectByType<WestbruchTraffic>().nodes.Length>=15);
            var world=FindAnyObjectByType<PedestrianWorld>();Check("Expanded resident population",world.people.Length>=28,"Residents "+world.people.Length);
            foreach(var p in new[]{new Vector3(165,1,80),new Vector3(190,1,80),new Vector3(208,1,132),new Vector3(208,1,220),new Vector3(173,1,264),new Vector3(140,1,265)})
            {bool ground=Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,1,QueryTriggerInteraction.Ignore);Check("Ground connection "+p,ground&&hit.point.y<.4f,ground?hit.collider.name+" "+hit.point.y:"missing");}
            // Native character-controller traversal detects fences, raised seams and invisible blockers.
            var controller=player.GetComponent<CharacterController>();Place(new Vector3(151,.20f,80));
            foreach(var target in new[]{new Vector3(180,.20f,80),new Vector3(208,.20f,80),new Vector3(208,.20f,130),new Vector3(208,.20f,210),new Vector3(208,.20f,266),new Vector3(171,.20f,266),new Vector3(140,.20f,265)})
            {
                float until=Time.unscaledTime+18;while(Time.unscaledTime<until)
                {var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.45f)break;controller.Move((delta.normalized*6+Vector3.down*4)*Time.deltaTime);yield return null;}
                Check("Walk route reaches "+target,Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z))<.7f,player.transform.position.ToString());
            }
            // Navigation must connect the old district, square, courtyard and platform ramp.
            foreach(var to in new[]{new Vector3(171,.2f,245),new Vector3(174,.2f,198),new Vector3(140,.2f,265),new Vector3(123,7.1f,250),new Vector3(107,7.1f,250)})
            {
                var path=new NavMeshPath();bool nav=NavMesh.SamplePosition(new Vector3(110,.14f,73),out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var b,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                Check("Navigation connection "+to,nav);
            }
            yield return Shot("01-transition",new Vector3(143,2.3f,78),new Vector3(194,3,83));
            yield return Shot("02-workshop-street",new Vector3(207,2.3f,130),new Vector3(199,4,221));
            yield return Shot("03-station-square",new Vector3(199,2.2f,284),new Vector3(142,4,264));
            yield return Shot("04-ticket-hall",new Vector3(143,1.9f,267),new Vector3(133,2.2f,267));
            yield return Shot("05-platforms",new Vector3(128,9.4f,230),new Vector3(114,8.4f,287));
            yield return Shot("06-courtyard",new Vector3(196,2.2f,196),new Vector3(163,3,199));
            yield return Shot("07-aerial",new Vector3(315,145,374),new Vector3(176,0,199));
            clock.SetHour(0);yield return new WaitForSeconds(5);yield return Shot("08-night-square",new Vector3(199,2.2f,284),new Vector3(142,4,264));
            Check("Station night lamps active",FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.name=="Station street canopy luminaire"&&l.enabled&&l.intensity>0)>=6);
            clock.SetHour(15);clock.SetWeather(WeatherKind.Rain);clock.Advance(20);yield return new WaitForSeconds(5);yield return Shot("09-rain-square",new Vector3(199,2.2f,284),new Vector3(142,4,264));
            var population=world.people.Where(p=>p&&p.name.StartsWith("Station resident")).ToArray();var initial=population.Select(p=>p.totalDistance).ToArray();yield return new WaitForSeconds(15);
            Check("Station residents actually walk",population.Where((p,i)=>p.totalDistance>initial[i]+1).Count()>=3);
            Check("No runtime errors",errors==0,errors.ToString());File.WriteAllText(Path.Combine(output,"bahnhof-validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",report)+"\nRuntime errors: "+errors);Application.Quit(passed?0:1);
        }
        void Place(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;}
        IEnumerator Shot(string name,Vector3 eye,Vector3 target){camera.transform.position=eye;camera.transform.LookAt(target);yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.25f);}
    }
}
