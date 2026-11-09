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
            if (Environment.GetCommandLineArgs().Contains("-lake-access-review") || Environment.GetCommandLineArgs().Contains("-lake-access-views"))
            {
                foreach(var renderer in player.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                if(Environment.GetCommandLineArgs().Contains("-lake-access-views"))yield return LakeAccessViews();else yield return LakeAccessReview();
                Check("No runtime errors",errors==0,errors.ToString());
                File.WriteAllText(Path.Combine(output,"bahnhof-validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",report));
                Application.Quit(passed?0:1);yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-core-review"))
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                yield return CoreReview();
                Check("No runtime errors", errors == 0, errors.ToString());
                File.WriteAllText(Path.Combine(output, "bahnhof-validation.txt"), "Passed: " + passed + "\n" + string.Join("\n", report));
                Application.Quit(passed ? 0 : 1); yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-landscape-review"))
            {
                foreach(var renderer in player.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                yield return LandscapeReview();
                Check("No runtime errors",errors==0,errors.ToString());
                File.WriteAllText(Path.Combine(output,"bahnhof-validation.txt"),"Passed: "+passed+"\n"+string.Join("\n",report));
                Application.Quit(passed?0:1);yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-meadow-review"))
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                yield return MeadowReview();
                Check("No runtime errors", errors == 0, errors.ToString());
                File.WriteAllText(Path.Combine(output, "bahnhof-validation.txt"), "Passed: " + passed + "\n" + string.Join("\n", report));
                Application.Quit(passed ? 0 : 1);
                yield break;
            }
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
        IEnumerator MeadowReview()
        {
            bool SoilHeight(float x, float z, out float y)
            {
                bool hit = Physics.Raycast(new Vector3(x, 3, z), Vector3.down, out var floor, 5, 1, QueryTriggerInteraction.Ignore);
                y = hit ? floor.point.y : 0;
                return hit && (floor.collider.name.StartsWith("BV2_Surface earth") || floor.collider.name.StartsWith("BV3_Ground "));
            }

            var heights = new List<float>();
            for (int x = 257; x <= 283; x += 2)
            for (int z = 105; z <= 165; z += 2)
                if (SoilHeight(x, z, out float y)) heights.Add(y);
            float relief = heights.Count > 0 ? heights.Max() - heights.Min() : 0;
            Check("Meadow has continuous walkable soil and real relief", heights.Count == 434 && relief >= .25f, $"{heights.Count}/434 samples, {relief:F2} m relief");
            foreach (float z in new[] { 100f, 119f, 210f, 300f, 352f })
            {
                bool west = SoilHeight(299.95f, z, out float yWest);
                bool east = SoilHeight(300.05f, z, out float yEast);
                Check("Meadow joins east landscape at " + z, west && east && Mathf.Abs(yWest - yEast) < .08f, $"{yWest:F3} / {yEast:F3} m");
            }
            SoilHeight(257, 108, out float start);
            Place(new Vector3(257, start + .08f, 108));
            foreach (var point in new[] { new Vector2(271, 123), new Vector2(278, 150), new Vector2(265, 166) })
            {
                SoilHeight(point.x, point.y, out float y);
                yield return Traverse("Character traverses meadow " + point, new Vector3(point.x, y + .08f, point.y));
            }
            camera.fieldOfView = 58;
            yield return Shot("meadow-from-flight", new Vector3(249.95f, 15.91f, 87.24f), new Vector3(264, 0, 109.4f));
            yield return Shot("meadow-at-eye-level", new Vector3(274, 1.85f, 165), new Vector3(263, .15f, 127));
            yield return Shot("meadow-footpath", new Vector3(258, 1.65f, 122), new Vector3(264, .28f, 154));
            yield return Shot("meadow-east-join", new Vector3(318, 2.1f, 115), new Vector3(288, .05f, 123));
            yield return Shot("meadow-north", new Vector3(284, 5, 322), new Vector3(316, .25f, 392));
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
        IEnumerator LandscapeFrameTiming()
        {
            // Measure after shaders, visibility and reflection history have settled.
            yield return new WaitForSeconds(3);
            var frameTimes=new List<float>();float end=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<end){yield return null;frameTimes.Add(Time.unscaledDeltaTime);}
            frameTimes.Sort();float average=frameTimes.Average();float p95=frameTimes[Mathf.Min(frameTimes.Count-1,Mathf.FloorToInt(frameTimes.Count*.95f))];
            report.Add($"MEASURE Lake overlook {Screen.width}x{Screen.height}: {1/average:F1} FPS average, p95 {p95*1000:F1} ms, {frameTimes.Count} frames");
            Debug.Log(report[report.Count-1]);
        }

        Vector3 LakeFloor(Vector3 p)
        {
            var hits=Physics.RaycastAll(new Vector3(p.x,500,p.z),Vector3.down,600,1,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.name.StartsWith("BV3_Ground ")||h.collider.name.StartsWith("BV8_Continuous")||h.collider.name.StartsWith("BV2_Surface ")||h.collider.name.StartsWith("BV3_Court paving"))
                .OrderByDescending(h=>h.point.y).ToArray();
            if(hits.Length==0)throw new Exception("Missing lake ground "+p);
            return hits[0].point;
        }
        IEnumerator DriveLakeRoad()
        {
            var car=FindAnyObjectByType<SimpleCar>();Check("Driveable test car present",car);if(!car)yield break;
            car.enabled=false;var body=car.GetComponent<Rigidbody>();
            var route=new[]{new Vector3(279,0,176),new Vector3(294,0,176),new Vector3(310,0,176)}.Concat(WestbruchLakeAccess.Road).ToArray();
            body.position=LakeFloor(route[0])+Vector3.up*.20f;body.rotation=Quaternion.LookRotation(Vector3.right);body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();yield return new WaitForSeconds(.7f);
            int nearest=0;float until=Time.unscaledTime+115,maxDeviation=0;bool arrived=false;
            while(Time.unscaledTime<until)
            {
                for(int i=nearest;i<Mathf.Min(route.Length,nearest+16);i++)
                    if(new Vector2(body.position.x-route[i].x,body.position.z-route[i].z).sqrMagnitude < new Vector2(body.position.x-route[nearest].x,body.position.z-route[nearest].z).sqrMagnitude)nearest=i;
                int target=Mathf.Min(route.Length-1,nearest+5);var local=car.transform.InverseTransformPoint(LakeFloor(route[target]));
                float steering=Mathf.Atan2(5.2f*local.x,Mathf.Max(4,local.x*local.x+local.z*local.z))*Mathf.Rad2Deg/29;
                float speed=body.linearVelocity.magnitude;car.DriveStep(Mathf.Clamp01((6-speed)*.55f),Mathf.Clamp(steering,-1,1),speed>7);
                if(nearest>3)maxDeviation=Mathf.Max(maxDeviation,WestbruchLakeAccess.Distance(WestbruchLakeAccess.Road,new Vector2(body.position.x,body.position.z),out _));
                camera.transform.position=body.position-car.transform.forward*7+Vector3.up*3;camera.transform.LookAt(body.position+Vector3.up);
                if(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(563,340))<4){arrived=true;break;}
                if(nearest>3&&maxDeviation>9)break;
                yield return new WaitForFixedUpdate();
            }
            car.DriveStep(0,0,true);
            Check("Wheel-physics drive from depot to lake parking",arrived&&maxDeviation<4,"Arrived "+arrived+"; max lateral deviation "+maxDeviation+"; final "+body.position);
            yield return Shot("00-test-car-arrival",body.position+new Vector3(-7,4,-6),body.position+Vector3.up);
        }
        IEnumerator LakeAccessReview()
        {
            Check("Lake access saved",GameObject.Find("BV8_Waldsee access and arrival"));
            Check("Shore path follows dry natural bank",WestbruchLakeAccess.ShorePath.All(p=>WestbruchLandscapeProfile.LakeRadius(p.x,p.z)>1.01f));
            var controller=player.GetComponent<CharacterController>();
            foreach(var route in new[]{WestbruchLakeAccess.Footpath,WestbruchLakeAccess.ShorePath})
            {
                Place(LakeFloor(route[0])+Vector3.up*.1f);int missed=0;float worst=0;
                for(int i=1;i<route.Length;i+=2)
                {
                    var target=LakeFloor(route[i]);float until=Time.unscaledTime+1.5f;
                    while(Time.unscaledTime<until)
                    {
                        var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.25f)break;
                        controller.Move((delta.normalized*5+Vector3.down*9)*Time.deltaTime);yield return null;
                    }
                    var difference=player.transform.position-target;difference.y=0;worst=Mathf.Max(worst,difference.magnitude);
                    if(difference.magnitude>.65f){missed++;if(missed>3)break;}
                }
                Check("Character traverses "+(route==WestbruchLakeAccess.Footpath?"district to lake":"shore path"),missed==0,"Failed segments "+missed+"; worst error "+worst);
            }
            int blocked=0,rough=0;float maxGrade=0;
            var road=WestbruchLakeAccess.Road;
            var blockers=new HashSet<string>();
            for(int i=1;i<road.Length;i++)
            {
                var a=LakeFloor(road[i-1]);var b=LakeFloor(road[i]);var delta=b-a;
                maxGrade=Mathf.Max(maxGrade,Mathf.Abs(delta.y)/new Vector2(delta.x,delta.z).magnitude);
                var direction=new Vector3(delta.x,0,delta.z).normalized;var across=Vector3.Cross(Vector3.up,direction);
                foreach(int side in new[]{-1,1})
                    if(Mathf.Abs(LakeFloor(b+across*1.3f*side).y-b.y)>.22f)rough++;
                foreach(var c in Physics.OverlapBox(b+Vector3.up*1.1f,new Vector3(1.15f,.75f,2.2f),Quaternion.LookRotation(direction),1,QueryTriggerInteraction.Ignore))
                {
                    if(c==controller||c.name.StartsWith("BV3_Ground ")||c.name.StartsWith("BV8_Continuous")||c.name.StartsWith("BV2_Surface ")||c.name.StartsWith("BV3_Court paving"))continue;
                    blocked++;blockers.Add(c.name+" at "+c.transform.position);
                }
            }
            Check("Vehicle envelope clear along gravel road",blocked==0,string.Join("; ",blockers));
            Check("Road grade and crossfall suitable for car",maxGrade<.16f&&rough==0,"Maximum grade "+maxGrade+"; crossfall failures "+rough);
            // Existing street and yard must also provide a clear vehicle entrance.
            int entryBlockers=0;
            for(float x=277;x<328;x+=1.5f)
                foreach(var c in Physics.OverlapBox(new Vector3(x,1.1f,176),new Vector3(2,.7f,1.1f),Quaternion.identity,1,QueryTriggerInteraction.Ignore))
                    if(c!=controller&&!c.name.Contains("paving")&&!c.name.StartsWith("BV2_Surface")&&!c.name.StartsWith("BV8_Continuous")&&!c.name.StartsWith("BV3_Ground")){entryBlockers++;blockers.Add(c.name+" "+c.transform.position);}
            Check("Existing depot entrance and new exit clear",entryBlockers==0,string.Join("; ",blockers));
            yield return DriveLakeRoad();
            yield return LakeAccessViews();
        }
        IEnumerator LakeAccessViews()
        {
            yield return Shot("09-depot-exit",new Vector3(313,2,176),new Vector3(344,2,176));
            yield return Shot("10-parking-wayfinding",LakeFloor(new Vector3(553,0,325))+Vector3.up*1.7f,new Vector3(557,20,332));
            yield return Shot("01-lake-connection-overview",new Vector3(425,155,110),new Vector3(484,9,320));
            yield return Shot("02-footpath-leaves-town",LakeFloor(new Vector3(284,0,306))+Vector3.up*1.8f,new Vector3(350,2,315));
            yield return Shot("03-woodland-road",LakeFloor(new Vector3(456,0,214))+Vector3.up*1.7f,new Vector3(510,15,253));
            yield return Shot("04-arrival-and-parking",new Vector3(540,39,322),new Vector3(595,15,359));
            yield return Shot("05-lake-arrival-eye-level",LakeFloor(new Vector3(605,0,354))+Vector3.up*1.75f,new Vector3(797,22,375));
            yield return Shot("06-old-loading-edge",LakeFloor(new Vector3(588,0,395))+Vector3.up*1.75f,new Vector3(604,17,413));
            yield return Shot("07-shore-and-woodland",new Vector3(673,64,307),new Vector3(581,16,369));
            var clock=WorldWeatherClock.Instance;clock.SetWeather(WeatherKind.Rain);clock.Advance(120);clock.SetHour(17);
            yield return Shot("08-lakeside-rain",LakeFloor(new Vector3(605,0,354))+Vector3.up*1.75f,new Vector3(797,22,375));
        }

        IEnumerator LandscapeReview()
        {
            var region=GameObject.Find("BV6_Regional landscape");Check("Regional landscape saved",region);
            var trees=region.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BV6_Rooted spruce ")||t.name.StartsWith("BV6_Rooted broadleaf ")).ToArray();
            int floating=0;foreach(var tree in trees)
            {
                var hits=Physics.RaycastAll(tree.position+Vector3.up*25,Vector3.down,40,1).Where(h=>h.collider is MeshCollider&&(h.collider.name.StartsWith("BV3_Ground")||h.collider.name.StartsWith("BV6_Continuous"))).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0||Mathf.Abs(tree.position.y+.10f-hits[0].point.y)>.025f)floating++;
            }
            Check("Woodland roots meet terrain",trees.Length>300&&floating==0,$"Trees {trees.Length}; floating {floating}");
            var lake=FindAnyObjectByType<RegionalLakeWeather>();Check("Lake with weather response",lake);
            int uncovered=0;for(int i=0;i<=32;i++)foreach(int side in new[]{-1,1})
            {
                float fraction=i/32f;
                foreach(var point in new[]{new Vector3(730+170*side,14,Mathf.Lerp(230,490,fraction)),new Vector3(Mathf.Lerp(560,900,fraction),14,360+130*side)})
                    if(!Physics.Raycast(point+Vector3.up*800,Vector3.down,out var h,1000,1)||h.point.y<14.35f)uncovered++;
            }
            Check("Lake perimeter buried beneath natural banks",uncovered==0,uncovered.ToString());
            int seams=0;foreach(float z in new[]{40f,120,240,360,480,600,800,1200,1700})
            {if(!Physics.Raycast(new Vector3(599.99f,900,z),Vector3.down,out var a,1000,1)||!Physics.Raycast(new Vector3(600.01f,900,z),Vector3.down,out var b,1000,1)||Mathf.Abs(a.point.y-b.point.y)>.10f)seams++;}
            Check("Continuous district to mountain ground seam",seams==0,seams.ToString());
            Check("Distant landscape visible",camera.farClipPlane>=3500,camera.farClipPlane.ToString());
            int routeSamples=0,routeMissing=0;
            foreach(var p in new[]{new Vector3(440,900,200),new Vector3(478,900,221),new Vector3(511,900,248),new Vector3(536,900,270),new Vector3(558,900,293),new Vector3(578,900,318)})
            {
                if(!Physics.Raycast(p,Vector3.down,out var support,1000,1)||support.normal.y<.71f){routeMissing++;continue;}
                Place(support.point+Vector3.up*.1f);float until=Time.unscaledTime+.5f;
                while(Time.unscaledTime<until){player.GetComponent<CharacterController>().Move(Vector3.down*4*Time.deltaTime);yield return null;}
                if(Mathf.Abs(player.transform.position.y-support.point.y)>.3f)routeMissing++;routeSamples++;
            }
            Check("Walking surface supports lake approach",routeMissing==0&&routeSamples==6,$"Samples {routeSamples}; failures {routeMissing}");
            yield return Shot("01-city-to-foothills",new Vector3(277,7,166),new Vector3(1150,120,650));
            yield return Shot("02-lake-overlook",new Vector3(515,105,195),new Vector3(790,25,445));
            yield return LandscapeFrameTiming();
            yield return Shot("03-region-aerial",new Vector3(470,410,40),new Vector3(1120,120,820));
            Physics.Raycast(new Vector3(620,900,335),Vector3.down,out var bank,1000,1);
            yield return Shot("04-shore-at-eye-level",bank.point+Vector3.up*1.8f,new Vector3(850,24,385));
            Physics.Raycast(new Vector3(1490,900,440),Vector3.down,out var summit,1000,1);
            Check("High mountain has snow biome",summit.point.y>290,summit.point.ToString());
            yield return Shot("05-high-country",summit.point+new Vector3(-15,35,-20),new Vector3(1100,220,1100));
            var clock=WorldWeatherClock.Instance;clock.SetWeather(WeatherKind.Storm);clock.Advance(120);clock.SetHour(15.5f);
            yield return Shot("06-lake-storm",bank.point+Vector3.up*2,new Vector3(850,25,385));
            yield return Shot("07-mountain-snowfall",summit.point+Vector3.up*3,summit.point+new Vector3(30,4,40));
            var weather=FindAnyObjectByType<WestbruchWeatherVisuals>();
            Check("Snow replaces rain at high altitude",weather.SnowBlend>.95f&&weather.RainDropCount>0,$"Snow {weather.SnowBlend}; particles {weather.RainDropCount}");
            var water=lake.GetComponent<UnityEngine.Rendering.HighDefinition.WaterSurface>();Check("Storm increases lake wind",water.largeWindSpeed>15,water.largeWindSpeed.ToString());
        }

        IEnumerator CoreReview()
        {
            var core = GameObject.Find("BV7_District ground renewal");
            Check("Saved playable district renewal present", core);
            var rails = FindObjectsByType<MeshRenderer>().Where(r => r.name == "Passing loop rail").ToArray();
            var fences = FindObjectsByType<MeshRenderer>().Where(r => r.name == "Terminated railway safety wire" || r.name == "Grounded rail boundary post" || r.name == "BV7_Outside track safety wire" || r.name == "BV7_Grounded rail perimeter post").ToArray();
            int conflicts = 0;
            foreach (var rail in rails)
            { var b = rail.bounds; b.Expand(new Vector3(.6f, 1.6f, .02f)); conflicts += fences.Count(f => f.bounds.Intersects(b)); }
            Check("Saved rail corridor clear of fences", rails.Length == 270 && conflicts == 0, conflicts.ToString());
            var beds = FindObjectsByType<Collider>().Where(c=>c.name=="BV3_Retained tree garden soil").ToArray();
            Check("Round planter collisions match visible earth",beds.Length==2&&beds.All(c=>c is MeshCollider&&Mathf.Abs(c.bounds.max.y-c.GetComponent<MeshRenderer>().bounds.max.y)<.005f));
            yield return Shot("00a-tree-bed-ground",new Vector3(181,1.8f,243),new Vector3(171,.9f,242));
            yield return Shot("00b-square-overhead",new Vector3(148,36,265),new Vector3(184,0,265));
            yield return Shot("00c-garden-relief",new Vector3(281,16,145),new Vector3(269,0,129));
            yield return Shot("00d-north-soil-seam",new Vector3(119,24,386),new Vector3(151,0,350));
            yield return Shot("00e-planting-distribution",new Vector3(131,20,224),new Vector3(151,0,182));
            yield return Shot("01-arrival-continuity", new Vector3(143, 1.8f, 78), new Vector3(190, 1.4f, 81));
            yield return Shot("02-road-edge-close", new Vector3(201.7f, .85f, 131), new Vector3(204, .15f, 140));
            yield return Shot("03-side-street-junction", new Vector3(217, 1.8f, 171), new Vector3(248, .6f, 177));
            yield return Shot("04-bank-footpath", new Vector3(139, 1.9f, 234), new Vector3(136, 3, 190));
            yield return Shot("05-bank-eye-level", new Vector3(140, 1.9f, 290), new Vector3(130, 2.5f, 305));
            yield return Shot("06-station-planting", new Vector3(159.2f, .95f, 254), new Vector3(161, .6f, 259));
            yield return Shot("07-south-turnout", new Vector3(109, 8.8f, 150), new Vector3(115, 6.5f, 181));
            yield return Shot("08-north-turnout", new Vector3(110, 8.8f, 410), new Vector3(114, 6.5f, 378));
            yield return Shot("09-west-platform-boundary", new Vector3(99, 3.5f, 278), new Vector3(111, 7, 300));
            yield return Shot("10-residential-edge", new Vector3(220, 1.9f, 260), new Vector3(232, 1.1f, 275));
            yield return Shot("11-station-square", new Vector3(184, 2.2f, 252), new Vector3(140, 4, 265));
            var clock = WorldWeatherClock.Instance;
            clock.SetWeather(WeatherKind.Rain); clock.Advance(120); clock.SetHour(15.5f);
            yield return new WaitForSeconds(4);
            yield return Shot("12-rain-on-soil-and-stone", new Vector3(139, 1.9f, 234), new Vector3(136, 3, 190));
            clock.SetWeather(WeatherKind.Clear); clock.Advance(120); clock.SetHour(0);
            yield return new WaitForSeconds(4);
            yield return Shot("13-night-station-square", new Vector3(184, 2.2f, 252), new Vector3(140, 4, 265));
        }
        IEnumerator Shot(string name,Vector3 eye,Vector3 target){camera.transform.position=eye;camera.transform.LookAt(target);yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.25f);}
    }
}
