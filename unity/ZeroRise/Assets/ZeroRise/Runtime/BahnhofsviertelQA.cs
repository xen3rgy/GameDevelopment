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
            var clock=WorldWeatherClock.Instance;clock.SuppressSave=true;clock.enabled=false;clock.SetWeather(WeatherKind.Clear);clock.SetSpeed(20);clock.Advance(120);clock.SetSpeed(1);clock.SetHour(15.5f);
            var orbit=camera.GetComponent<OrbitCamera>();if(orbit)orbit.enabled=false;player.enabled=false;
            if (Environment.GetCommandLineArgs().Contains("-map-support-views-only"))
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                yield return SupportSurvey();
                Check("No runtime errors", errors == 0, errors.ToString());
                File.WriteAllText(Path.Combine(output, "bahnhof-validation.txt"), "Passed: " + passed + "\n" + string.Join("\n", report));
                Application.Quit(passed ? 0 : 1);
                yield break;
            }
            if(Environment.GetCommandLineArgs().Contains("-whole-map-routes-only"))
            {
                foreach(var r in player.GetComponentsInChildren<Renderer>())r.enabled=false;
                yield return WholeMapSurvey();Check("No runtime errors",errors==0,errors.ToString());
                File.WriteAllText(Path.Combine(output,"bahnhof-validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",report));Application.Quit(passed?0:1);yield break;
            }
            var district=GameObject.Find("Bahnhofsviertel – connected station neighbourhood");Check("Saved expansion present",district);
            Check("Original Westbruch retained",GameObject.Find("Uferhof_12")&&GameObject.Find("Westbruch concrete overpass"));
            Check("Two elevated platforms",FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Station platform")==2);
            Check("Station walk-through hall",GameObject.Find("Bahnhof Westbruch – accessible ticket hall"));
            Check("Connected traffic graph",FindAnyObjectByType<WestbruchTraffic>().nodes.Length>=15);
            var world=FindAnyObjectByType<PedestrianWorld>();Check("Expanded resident population",world.people.Length>=28,"Residents "+world.people.Length);
            var completion=district.transform.Find("BV4_Audit completion");
            Check("Audit completion installed",completion);
            if(completion)
            {
                var homes=completion.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BV4_Context residence ")).ToArray();
                Check("Eight finished collidable context homes",homes.Length==8&&homes.All(t=>t.GetComponent<BoxCollider>()));
                var backdrop=FindObjectsByType<MeshRenderer>().Where(r=>r.enabled&&r.sharedMaterial&&r.sharedMaterial.name.StartsWith("BackdropFacade")&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh.triangles.Length>0).ToArray();
                Check("No nearby primitive backdrop geometry remains rendered",backdrop.Length==0,backdrop.Length.ToString());
                foreach(float side in new[]{-1f,1f})
                {bool floor=Physics.Raycast(new Vector3(0,2,side*142),Vector3.down,out var ground,3,1)&&Mathf.Abs(ground.point.y-.14f)<.03f;Check("Context promenade ground "+side,floor);}
            }
            foreach(float z in new[]{-142f,142})
            {
                var path=new NavMeshPath();bool connected=NavMesh.SamplePosition(new Vector3(84,.14f,z),out var edge,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(new Vector3(80,.14f,39),out var town,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(town.position,edge.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                Check("Context promenade connected to neighbourhood "+z,connected);
            }
            var signs=district.GetComponentsInChildren<FittedWorldSign>();int outside=0;
            foreach(var sign in signs)
            {
                sign.Refit();var renderer=sign.lettering.GetComponent<MeshRenderer>();var bounds=renderer.localBounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
                {
                    var at=sign.transform.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center+new Vector3(bounds.extents.x*x,bounds.extents.y*y,0)));
                    if(Mathf.Abs(at.x)>sign.boardWidth*.46f+.006f||Mathf.Abs(at.y)>sign.boardHeight*.42f+.006f)outside++;
                }
            }
            Check("Rendered lettering fits physical sign plates",signs.Length>=15&&outside==0,"Signs "+signs.Length+"; escaped corners "+outside);
            Check("No loose legacy labels in playable district",district.GetComponentsInChildren<TextMesh>().All(t=>t.GetComponentInParent<FittedWorldSign>()));
            foreach(var p in new[]{new Vector3(152,1,265),new Vector3(140,1,210),new Vector3(140,1,225),new Vector3(161.5f,1,80),new Vector3(165,1,80),new Vector3(190,1,80),new Vector3(208,1,132),new Vector3(208,1,220),new Vector3(173,1,264),new Vector3(140,1,265),new Vector3(133,1,244),new Vector3(133,1,283)})
            {bool ground=Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,1,QueryTriggerInteraction.Ignore);Check("Ground connection "+p,ground&&hit.point.y<.4f,ground?hit.collider.name+" "+hit.point.y:"missing");}
            // Native character-controller traversal detects fences, raised seams and invisible blockers.
            var controller=player.GetComponent<CharacterController>();Place(new Vector3(151,.20f,80));
            foreach(var target in new[]{new Vector3(180,.20f,80),new Vector3(208,.20f,80),new Vector3(208,.20f,130),new Vector3(208,.20f,210),new Vector3(208,.20f,266),new Vector3(171,.20f,266),new Vector3(140,.20f,265)})
            {
                float until=Time.unscaledTime+18;while(Time.unscaledTime<until)
                {var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.45f)break;controller.Move((delta.normalized*6+Vector3.down*4)*Time.deltaTime);yield return null;}
                bool grounded=Physics.Raycast(new Vector3(target.x,3,target.z),Vector3.down,out var support,6,1,QueryTriggerInteraction.Ignore)&&Mathf.Abs(player.transform.position.y-support.point.y)<.4f;
                Check("Walk route reaches "+target,grounded&&Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(target.x,target.z))<.7f,player.transform.position+"; grounded height "+grounded);
            }
            // Navigation must connect the old district, square, courtyard and platform ramp.
            foreach(var to in new[]{new Vector3(171,.2f,245),new Vector3(174,.2f,198),new Vector3(140,.2f,265),new Vector3(123,7.1f,250),new Vector3(107,7.1f,250)})
            {
                var path=new NavMeshPath();bool nav=NavMesh.SamplePosition(new Vector3(110,.14f,73),out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(to,out var b,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                Check("Navigation connection "+to,nav);
            }
            // The ramp-only navigation check missed the overlapping upper
            // landing. Walk the actual main stairs in both directions too.
            Place(new Vector3(128,.20f,256));
            yield return Traverse("Main station stairs ascent",new Vector3(128,7.05f,239.6f));
            yield return Traverse("Main station stairs descent",new Vector3(128,.14f,256));
            foreach(float side in new[]{-1f,1f})
            {
                Place(new Vector3(84,.22f,side*110));
                yield return Traverse("Garden promenade physical connection "+side,new Vector3(84,.14f,side*142));
                yield return Traverse("Garden promenade physical frontage "+side,new Vector3(24,.14f,side*142));
                yield return Traverse("Garden residence physical entrance "+side,new Vector3(24,.14f,side*171));
            }
            foreach(var renderer in player.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            yield return Shot("01-transition",new Vector3(143,2.3f,78),new Vector3(194,3,83));
            yield return Shot("02-workshop-street",new Vector3(207,2.3f,130),new Vector3(199,4,221));
            yield return Shot("03-station-square",new Vector3(184,2.2f,252),new Vector3(140,4,265));
            yield return Shot("04-ticket-hall",new Vector3(143,1.9f,267),new Vector3(133,2.2f,267));
            yield return Shot("05-platforms",new Vector3(128,9.4f,230),new Vector3(114,8.4f,287));
            yield return Shot("06-courtyard",new Vector3(196,2.2f,196),new Vector3(163,3,199));
            yield return Shot("06b-residential-east",new Vector3(220,1.9f,260),new Vector3(233,4,275));
            yield return Shot("06c-residential-west",new Vector3(199,1.9f,182),new Vector3(179,4,177));
            yield return Shot("06d-workshop-close",new Vector3(184,1.9f,119),new Vector3(179,2,102));
            yield return Shot("06e-station-front",new Vector3(160,1.9f,265),new Vector3(141,4,265));
            yield return Shot("06f-platform-bridge",new Vector3(123,8.8f,300),new Vector3(114,11,323));
            yield return Shot("27-south-garden-frontage",new Vector3(27,1.9f,-142),new Vector3(24,3,-184));
            yield return Shot("28-north-garden-frontage",new Vector3(27,1.9f,142),new Vector3(24,3,184));
            yield return Shot("29-square-waiting",new Vector3(182,1.9f,272),new Vector3(169,2.5f,290));
            yield return Shot("30-courtyard-repair",new Vector3(187,1.9f,196),new Vector3(173,2,204));
            yield return Shot("31-kerb-and-drainage",new Vector3(201.7f,.85f,131),new Vector3(204,.15f,140));
            yield return Shot("32-new-street-surface",new Vector3(208,1.8f,110),new Vector3(208,1.1f,145));
            yield return Shot("33-planted-soil-close",new Vector3(159.2f,.95f,254),new Vector3(161,.6f,259));
            yield return Shot("34-garden-ground-detail",new Vector3(29,1.4f,-154),new Vector3(34,.4f,-165));
            yield return Shot("07-aerial",new Vector3(315,145,374),new Vector3(176,0,199));
            if(Environment.GetCommandLineArgs().Contains("-bahnhof-survey"))
            {
                yield return Shot("10-arrival-reverse",new Vector3(196,1.9f,83),new Vector3(150,2.8f,80));
                yield return Shot("11-arrival-south",new Vector3(209,1.9f,101),new Vector3(221,3,64));
                yield return Shot("12-workshop-yard",new Vector3(196,1.9f,148),new Vector3(167,3,143));
                yield return Shot("13-former-overlap",new Vector3(197,1.9f,191),new Vector3(162,5,185));
                yield return Shot("14-rear-house",new Vector3(248,1.9f,269),new Vector3(236,3,275));
                yield return Shot("15-east-street-context",new Vector3(273,1.9f,185),new Vector3(315,3,150));
                yield return Shot("16-north-context",new Vector3(268,1.9f,320),new Vector3(256,4,354));
                yield return Shot("17-station-rear",new Vector3(130,1.9f,274),new Vector3(132,3,262));
                yield return Shot("18-stair-construction",new Vector3(130,2,252),new Vector3(128,5,240));
                yield return Shot("19-bank-toe",new Vector3(140,1.9f,290),new Vector3(131,2,295));
                yield return Shot("20-ramp-bank",new Vector3(139,1.9f,234),new Vector3(136,4,190));
                yield return Shot("21-platform-context",new Vector3(111,13.4f,323),new Vector3(235,7,256));
                yield return Shot("22-roof-construction",new Vector3(248,16,278),new Vector3(233,12,267));
                yield return Shot("23-east-landscape",new Vector3(287,1.9f,301),new Vector3(350,3,331));
                yield return Shot("24-street-termination",new Vector3(208,1.9f,302),new Vector3(208,3,347));
                yield return Shot("25-underpass-approach",new Vector3(111,1.9f,80),new Vector3(197,2,80));
                yield return Shot("26-courtyard-reverse",new Vector3(165,1.9f,198),new Vector3(185,4,210));
            }
            if(Environment.GetCommandLineArgs().Contains("-whole-map-survey"))yield return WholeMapSurvey();
            Check("Additional practical lamps are off in daylight",FindObjectsByType<Light>().Where(l=>l.name.StartsWith("BV4 Street light")).All(l=>!l.enabled||l.intensity<.001f));
            var stationPaving=FindObjectsByType<MeshRenderer>().FirstOrDefault(r=>r.name.StartsWith("BV2_Surface walk")&&r.sharedMaterial&&r.sharedMaterial.name.StartsWith("BV5_Station_stone_setts"));
            float dryPavingBrightness=stationPaving?stationPaving.sharedMaterial.GetColor("_BaseColor").grayscale:0;
            float dryPavingSmoothness=stationPaving?stationPaving.sharedMaterial.GetFloat("_SmoothnessRemapMax"):0;
            if(Environment.GetCommandLineArgs().Contains("-whole-map-survey"))yield return OrbitBankSurvey();
            clock.SetHour(0);yield return new WaitForSeconds(5);yield return Shot("08-night-square",new Vector3(184,2.2f,252),new Vector3(140,4,265));
            var nearbyPractical=FindObjectsByType<Light>().Where(l=>l.name.StartsWith("BV4 Street light")&&Vector3.Distance(l.transform.position,camera.transform.position)<100).ToArray();
            Check("Additional nearby practical lamps illuminate at night",nearbyPractical.Length>=10&&nearbyPractical.All(l=>l.enabled&&l.intensity>0),"Nearby "+nearbyPractical.Length+"; active "+nearbyPractical.Count(l=>l.enabled&&l.intensity>0));
            Check("Station night lamps active",FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.name=="Station street canopy luminaire"&&l.enabled&&l.intensity>0)>=6);
            // Clouds lead rainfall and hard surfaces accumulate water over game
            // time. Compare a genuinely dry state with sustained rainfall.
            clock.SetWeather(WeatherKind.Rain);clock.SetSpeed(20);clock.Advance(60);clock.SetSpeed(1);clock.SetHour(15);yield return new WaitForSeconds(5);yield return Shot("09-rain-square",new Vector3(184,2.2f,252),new Vector3(140,4,265));
            if(stationPaving)Check("New station paving reacts to rain",stationPaving.sharedMaterial.GetColor("_BaseColor").grayscale<dryPavingBrightness*.97f&&stationPaving.sharedMaterial.GetFloat("_SmoothnessRemapMax")>dryPavingSmoothness+.03f,"Wetness "+clock.Wetness+"; brightness "+dryPavingBrightness+" -> "+stationPaving.sharedMaterial.GetColor("_BaseColor").grayscale+"; smoothness "+dryPavingSmoothness+" -> "+stationPaving.sharedMaterial.GetFloat("_SmoothnessRemapMax"));
            int residentsInBeds=0;
            foreach(var bed in district.GetComponentsInChildren<Transform>().Where(t=>t.name=="BV4_Planting island"))
            {
                var soil=bed.GetComponentsInChildren<BoxCollider>().First(c=>c.name=="BV4_Raised retained soil");var bounds=soil.bounds;
                residentsInBeds+=world.people.Count(p=>p&&p.transform.position.x>bounds.min.x+.2f&&p.transform.position.x<bounds.max.x-.2f&&p.transform.position.z>bounds.min.z+.2f&&p.transform.position.z<bounds.max.z-.2f);
            }
            Check("Residents stay off planted islands",residentsInBeds==0,residentsInBeds.ToString());
            var population=world.people.Where(p=>p&&p.name.StartsWith("Station resident")).ToArray();var initial=population.Select(p=>p.totalDistance).ToArray();yield return new WaitForSeconds(15);
            Check("Station residents actually walk",population.Where((p,i)=>p.totalDistance>initial[i]+1).Count()>=3);
            var frameTimes=new List<float>();float sampleEnd=Time.unscaledTime+4;
            while(Time.unscaledTime<sampleEnd){yield return null;frameTimes.Add(Time.unscaledDeltaTime*1000);}
            frameTimes.Sort();if(frameTimes.Count>0)report.Add("INFO Native frame sample: mean "+frameTimes.Average().ToString("F1")+" ms; 95th percentile "+frameTimes[Mathf.Min(frameTimes.Count-1,Mathf.FloorToInt(frameTimes.Count*.95f))].ToString("F1")+" ms");
            Check("No runtime errors",errors==0,errors.ToString());File.WriteAllText(Path.Combine(output,"bahnhof-validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",report)+"\nRuntime errors: "+errors);Application.Quit(passed?0:1);
        }
        IEnumerator WholeMapSurvey()
        {
            // The original district's retained building roots supply real facade dimensions.
            string[] ids={"Uferhof_12","Uferhof_14","Hofstrasse_3","Nordhof_8","Nordhof_10","Nordhof_16","Suedhof_2","Suedhof_4","Suedhof_6","Garagen_West","Pfand_und_Spar","Stadthof_21","Werkstatt","Stadthof_25","Nordring_31","Nordring_33","Nordring_35","Suedring_41","Suedring_43","Suedring_45"};
            foreach(var id in Environment.GetCommandLineArgs().Contains("-whole-map-routes-only")?new string[0]:ids)
            {
                var house=GameObject.Find(id);if(!house)continue;
                var shell=house.transform.Find("Structural shell");if(!shell)continue;
                var mesh=shell.GetComponent<MeshFilter>().sharedMesh;float depth=mesh.bounds.size.z;
                foreach(int side in new[]{-1,1})
                {
                    var eye=house.transform.TransformPoint(new Vector3(5,1.9f,side*(depth*.5f+9)));
                    var target=house.transform.TransformPoint(new Vector3(0,3,0));
                    yield return Shot("whole-"+id+(side<0?"-front":"-rear"),eye,target);
                }
            }
            Place(new Vector3(110,.22f,73));
            foreach(var target in new[]{new Vector3(80,.14f,39),new Vector3(-35,.14f,42),new Vector3(-86,.14f,12),new Vector3(-35,.14f,-40),new Vector3(80,.14f,-39),new Vector3(80,.14f,-86),new Vector3(-84,.14f,-86),new Vector3(-84,.14f,86)})
            {
                var path=new NavMeshPath();bool connected=NavMesh.SamplePosition(player.transform.position,out var a,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(target,out var b,3,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                Check("Whole map navigation "+target,connected);
                if(connected)foreach(var corner in path.corners.Skip(1))yield return Traverse("Whole map physical route "+corner,corner);
            }
            var eyes=new[]{new Vector3(-110,2,0),new Vector3(0,2,-114),new Vector3(0,2,114),new Vector3(99,2,-110),new Vector3(116,2,70),new Vector3(-40,2,-55)};
            var aims=new[]{new Vector3(-145,2,0),new Vector3(0,2,-180),new Vector3(0,2,180),new Vector3(118,4,-180),new Vector3(118,4,80),new Vector3(-60,2,-55)};
            for(int i=0;i<eyes.Length;i++)yield return Shot("whole-boundary-"+i,eyes[i],aims[i]);
            yield return SupportSurvey();
        }
        IEnumerator SupportSurvey()
        {
            yield return Shot("whole-canal-north",new Vector3(-123,7,153),new Vector3(-123,-.1f,205));
            yield return Shot("whole-canal-south",new Vector3(-123,7,-153),new Vector3(-123,-.1f,-205));
            yield return Shot("whole-south-landscape",new Vector3(-75,40,-275),new Vector3(-145,0,-445));
            yield return Shot("whole-north-landscape",new Vector3(15,35,215),new Vector3(-100,0,295));
            yield return Shot("whole-bus-bench-feet",new Vector3(103.2f,.55f,17.8f),new Vector3(105.4f,.2f,17));
            yield return Shot("whole-lamp-footing",new Vector3(198.6f,.65f,130.5f),new Vector3(200,.25f,132));
            yield return Shot("whole-freight-pallet-feet",new Vector3(149,1.1f,151),new Vector3(152,.25f,153));
            yield return Shot("whole-platform-board-mounts",new Vector3(123,8.7f,250),new Vector3(123,9.9f,258));
            yield return Shot("whole-salvage-pallet-feet", new Vector3(34.4f,.6f,-41.7f), new Vector3(35.9f,.12f,-43.47f));
            var balcony = FindObjectsByType<Transform>().FirstOrDefault(t => t.name == "BV4_Resident balcony garden");
            if (balcony)
                yield return Shot("whole-balcony-planter-foot", balcony.position - balcony.forward * 2.6f + Vector3.up * .32f, balcony.position + Vector3.up * .13f);
        }
        IEnumerator OrbitBankSurvey()
        {
            var orbit=camera.GetComponent<OrbitCamera>();if(!orbit)yield break;
            Place(new Vector3(100,.22f,-110));orbit.Follow(player.transform,1.5f,5.5f);orbit.enabled=true;Cursor.lockState=CursorLockMode.None;
            for(int i=0;i<8;i++)
            {
                orbit.Rotate(45,0);yield return new WaitForSeconds(1);
                var at=camera.transform.position;
                bool buried=Physics.RaycastAll(at+Vector3.up*15,Vector3.down,30,1,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name.Contains("earth bank")&&h.point.y>at.y+.05f);
                Check("Normal orbit stays outside railway earth "+i,!buried,at.ToString("F2"));
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"whole-orbit-bank-"+i+".png"));yield return new WaitForSeconds(.3f);
            }
            orbit.enabled=false;
        }
        void Place(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.transform.position=p;c.enabled=true;}
        IEnumerator Traverse(string title,Vector3 target)
        {
            var controller=player.GetComponent<CharacterController>();float until=Time.unscaledTime+Mathf.Max(15,Vector3.Distance(player.transform.position,target)/3.2f+12);
            while(Time.unscaledTime<until)
            {
                var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.35f)break;
                controller.Move((delta.normalized*3.2f+Vector3.down*4)*Time.deltaTime);yield return null;
            }
            Check(title,Vector2.Distance(new Vector2(target.x,target.z),new Vector2(player.transform.position.x,player.transform.position.z))<.6f&&Mathf.Abs(player.transform.position.y-target.y)<.4f,player.transform.position.ToString("F2"));
        }
        IEnumerator Shot(string name,Vector3 eye,Vector3 target){camera.transform.position=eye;camera.transform.LookAt(target);yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.25f);}
    }
}
