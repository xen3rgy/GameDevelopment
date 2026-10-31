using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise
{
    // Only runs through the explicit editor command or -world-weather-qa path.
    public sealed class WorldWeatherQA : MonoBehaviour
    {
        string output;bool failed,drive,visualOnly,nightOnly,twilightOnly,skyOnly,sunsetOnly;int errors;float throttle;WorldWeatherClock clock;
        ThirdPersonMotor player;SimpleCar car;Camera camera;Rigidbody body;
        readonly List<string> report=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-world-weather-qa");
            if(index>=0)Launch(index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"WorldWeatherQA"));
            else if(Array.IndexOf(args,"-sunset-transition-qa") is int sunsetReview && sunsetReview>=0)Launch(args[sunsetReview+1],true);
            else if(Array.IndexOf(args,"-night-sky-qa") is int skyReview && skyReview>=0)Launch(args[skyReview+1],true,true);
            else if(Array.IndexOf(args,"-night-lighting-qa") is int nightReview && nightReview>=0)Launch(args[nightReview+1],true,true);
            else if(Array.IndexOf(args,"-world-weather-visual-review") is int review && review>=0)Launch(args[review+1],true);
            else {index=Array.IndexOf(args,"-world-twilight-qa");if(index>=0)Launch(index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"WorldTwilightQA"),false,false,true);}
        }
        public static void Launch(string folder,bool visualsOnly=false,bool nightsOnly=false,bool twilight=false)
        {
            Application.runInBackground=true;
            var qa=new GameObject("Explicit world weather verification").AddComponent<WorldWeatherQA>();qa.output=folder;qa.visualOnly=visualsOnly;qa.nightOnly=nightsOnly;qa.twilightOnly=twilight;qa.skyOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-night-sky-qa")>=0;qa.sunsetOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-sunset-transition-qa")>=0;Directory.CreateDirectory(folder);
            var world=WorldWeatherClock.Instance;if(world)world.SuppressSave=true;
        }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;report.Add("ERROR "+message+"\n"+stack);}}
        void Check(string label,bool pass,string detail=""){failed|=!pass;report.Add((pass?"PASS ":"FAIL ")+label+" | "+detail);File.WriteAllText(Path.Combine(output,"world-weather-progress.txt"),string.Join("\n",report));Debug.Log("WORLD WEATHER "+(pass?"PASS ":"FAIL ")+label+" | "+detail);}
        IEnumerator Start()
        {
            // Allow existing scene bootstraps, runtime navmesh and roof caches to initialize.
            StartCoroutine(Watchdog());yield return null;clock=WorldWeatherClock.Instance;player=FindAnyObjectByType<ThirdPersonMotor>();car=FindAnyObjectByType<SimpleCar>();camera=Camera.main;
            var visual=FindAnyObjectByType<WestbruchWeatherVisuals>();var weatherSound=FindAnyObjectByType<WestbruchWeatherAudio>();var rhythm=CityDailyRhythm.Instance;
            bool systems=clock&&player&&car&&camera&&rhythm&&visual&&weatherSound&&GameplayAudioDirector.Active&&GameplayAudioDirector.Active.library;
            Check("Saved scene contains all world systems",systems);
            if(!systems){Finish();yield break;}
            clock.SuppressSave=true;clock.enabled=false;player.enabled=false;player.orbit.enabled=false;car.enabled=false;body=car.GetComponent<Rigidbody>();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
            CoreChecks();
            clock.state=new WorldWeatherClock.WorldState();clock.SetHour(12);clock.SetWeather(WeatherKind.Clear);clock.SetSpeed(1);clock.Advance(20);
            Place(new Vector3(111,0,70));View(new Vector3(103,2.8f,67),new Vector3(123,3,81));
            yield return Hold(12);
            float readyUntil=Time.unscaledTime+20;while(!visual.RoofMapReady&&Time.unscaledTime<readyUntil)yield return null;
            Check("Runtime weather geometry and roof cache initialize",visual.Initialized&&visual.RoofMapReady&&visual.PuddleCount>0&&visual.WetMaterialCount>0&&weatherSound.Ready,"Puddles "+visual.PuddleCount+"; materials "+visual.WetMaterialCount);
            Check("Puddles use HDRP environment reflections",visual.ReflectivePuddles,"Native HDRP/Lit shallow water");
            float daySun=SunIntensity();
            Check("Weather materials survive runtime shader stripping",visual.rainMaterial&&visual.puddleMaterial&&visual.rainMaterial.shader.isSupported&&visual.puddleMaterial.shader.isSupported);
            if(sunsetOnly){weatherSound.enabled=false;yield return SunsetTransitionReview(visual);Finish();yield break;}
            if(twilightOnly){yield return TwilightRegression(visual);Finish();yield break;}
            if(nightOnly){if(skyOnly)yield return NightSkyReview(visual);else yield return NightLightingReview(visual);Finish();yield break;}
            yield return Capture("01-clear-noon.png");
            LogSystem("Noon",visual,weatherSound,rhythm);
            Check("Noon schedule increases population",rhythm.TargetPedestrians==18&&rhythm.TargetTraffic>=9,"Residents "+rhythm.ActivePedestrians+" / target "+rhythm.TargetPedestrians+"; traffic "+rhythm.ActiveTraffic+" / target "+rhythm.TargetTraffic);

            clock.SetWeather(WeatherKind.Rain);clock.SetSpeed(20);clock.Advance(60);clock.SetHour(12);
            yield return Hold(7);
            Check("Rain mesh is visible in outdoor weather",visual.RainDropCount>10,"Streaks "+visual.RainDropCount);
            Check("Rain leaves persistent surface water",clock.RainIntensity>.5f&&clock.Wetness>.8f&&clock.PuddleAmount>.35f,"Rain "+clock.RainIntensity+"; wet "+clock.Wetness+"; puddles "+clock.PuddleAmount);
            var take=Application.isEditor?null:GameplayAudioRecording.Begin(camera,32);
            yield return Hold(visualOnly?1:10);yield return Capture("02-rain-wet-street.png");EndTake(take,"rain-outside.wav");
            Check("Rain impacts create bounded surface splashes",visual.SplashCount>0&&visual.SplashCount<=256,"Live impact particles "+visual.SplashCount);
            var puddle=visual.PuddleInspectionPoint;
            if(puddle!=Vector3.zero)
            {
                View(puddle+new Vector3(-2,.35f,-2),puddle+Vector3.up*.08f);yield return Hold(3);yield return Capture("02b-reflective-puddle.png");
                View(new Vector3(103,2.8f,67),new Vector3(123,3,81));
            }
            LogSystem("Rain outdoors",visual,weatherSound,rhythm);
            float outsideRain=weatherSound.RainGain;
            Place(new Vector3(118,0,80));View(new Vector3(118,2.1f,80),new Vector3(108,2,79));
            yield return Hold(5);
            take=Application.isEditor?null:GameplayAudioRecording.Begin(camera,20);yield return Hold(7);EndTake(take,"rain-under-bridge.wav");
            yield return Capture("03-rain-under-bridge.png");LogSystem("Rain under bridge",visual,weatherSound,rhythm);
            float shelter=weatherSound.ShelterBlend,insideRain=weatherSound.RainGain;
            Check("Real bridge roof changes rain perspective",shelter>.5f&&insideRain<outsideRain,"Roof shelter "+shelter+"; outdoor rain "+outsideRain+"; covered rain "+insideRain);

            // Reuse the actual locomotion routes to catch regressions in wet weather.
            if(!visualOnly){yield return GameplaySoundFeedbackChecks.Run(player,camera,GameplayAudioDirector.Active,Check,output);yield return DriveInRain();}

            clock.SetWeather(WeatherKind.Storm);clock.Advance(14);clock.SetHour(17);
            Place(new Vector3(111,0,70));View(new Vector3(103,2.8f,67),new Vector3(123,3,81));
            take=Application.isEditor?null:GameplayAudioRecording.Begin(camera,58);yield return Hold(visualOnly?7:49);EndTake(take,"storm-thunder.wav");
            if(!visualOnly)Check("Storm produces delayed thunder and lightning",weatherSound.ThunderCount>0&&weatherSound.LightningCount>=weatherSound.ThunderCount,"Lightning "+weatherSound.LightningCount+"; thunder "+weatherSound.ThunderCount);
            yield return Capture("04-storm.png");LogSystem("Storm",visual,weatherSound,rhythm);
            clock.SetWeather(WeatherKind.Fog);clock.Advance(14);clock.SetHour(7);yield return Hold(7);
            yield return Capture("05-fog-morning.png");LogSystem("Morning fog",visual,weatherSound,rhythm);
            clock.SetWeather(WeatherKind.Clear);clock.Advance(14);clock.SetHour(20);yield return Hold(7);
            yield return Capture("06-evening.png");
            clock.SetHour(0);yield return Hold(8);yield return Capture("07-night.png");
            yield return NightWindow("10-night-window.png");
            LogSystem("Night",visual,weatherSound,rhythm);
            Check("Night sun is substantially dimmer",SunIntensity()<daySun*.12f,"Noon "+daySun+"; night "+SunIntensity());
            Check("Evening lamps and furnished windows are lit",visual.NightLightBlend>.8f&&visual.LitWindowCount>0,"Light blend "+visual.NightLightBlend+"; windows "+visual.LitWindowCount);
            TrafficVehicle nightTraffic=null;foreach(var vehicle in FindAnyObjectByType<WestbruchTraffic>().vehicles)if(vehicle.DailyActive){nightTraffic=vehicle;break;}
            if(nightTraffic)
            {
                float watchUntil=Time.unscaledTime+2;while(Time.unscaledTime<watchUntil){View(nightTraffic.transform.position-nightTraffic.transform.forward*7+nightTraffic.transform.right*3+Vector3.up*2.3f,nightTraffic.transform.position+nightTraffic.transform.forward*8);yield return null;}
                int beams=0;foreach(var lamp in nightTraffic.GetComponentsInChildren<Light>())if(lamp.name=="Mounted automatic dipped headlamp"&&lamp.enabled)beams++;
                Check("Active traffic has mounted night headlights",beams==2,"Watched vehicle lamps "+beams+"; active headlight beams "+visual.ActiveHeadlights);
                yield return Capture("11-night-traffic.png");
            }
            else Check("Active traffic remains present at night",false);
            Check("Night schedule calms the neighbourhood",rhythm.TargetPedestrians<=6&&rhythm.TargetTraffic<=7,"Residents target "+rhythm.TargetPedestrians+"; traffic target "+rhythm.TargetTraffic);
            if(!visualOnly)yield return NightStorm(weatherSound,visual);
            if(!visualOnly)yield return DriveInRain(true);
            Place(new Vector3(111,0,70));View(new Vector3(103,2.8f,67),new Vector3(123,3,81));
            float until=Time.unscaledTime+(visualOnly?0:120);while(Time.unscaledTime<until&&(rhythm.ParkedVehicles==0||rhythm.RetiredResidents==0)){yield return null;}
            LogSystem("Night after safe departures",visual,weatherSound,rhythm);
            if(!visualOnly)Check("Daily population actually departs safely",rhythm.ParkedVehicles>0&&rhythm.RetiredResidents>0,"Parked vehicles "+rhythm.ParkedVehicles+"; residents home "+rhythm.RetiredResidents);
            int returned=rhythm.ReturnedVehicles,deliveryChoices=rhythm.MorningVanRouteChoices;
            clock.SetHour(8);rhythm.RefreshPopulation();until=Time.unscaledTime+(visualOnly?0:45);
            while(Time.unscaledTime<until&&(rhythm.ActiveDeliveryVans<1||rhythm.ReturnedVehicles==returned&&rhythm.MorningVanRouteChoices==deliveryChoices))yield return null;
            if(!visualOnly)Check("Morning delivery schedule uses existing vans",rhythm.MorningDeliveries&&rhythm.TargetTraffic==12&&rhythm.ActiveDeliveryVans>=1&&(rhythm.ReturnedVehicles>returned||rhythm.MorningVanRouteChoices>deliveryChoices),"Active vans "+rhythm.ActiveDeliveryVans+"; returned vehicles "+(rhythm.ReturnedVehicles-returned)+"; morning route choices "+(rhythm.MorningVanRouteChoices-deliveryChoices));

            clock.SetWeather(WeatherKind.Clear);clock.state.rain=0;clock.state.wetness=1;clock.state.puddles=1;clock.state.soil=1;clock.SetHour(12);clock.SetSpeed(20);
            clock.Advance(5);clock.SetHour(12);yield return Hold(4);yield return Capture("08-after-rain.png");
            float wetBefore=clock.Wetness,puddleBefore=clock.PuddleAmount;
            clock.Advance(7);clock.SetHour(12);yield return Hold(.5f);
            Check("Earth retains moisture after hard surfaces dry",clock.Wetness<.01f&&clock.SoilMoisture>.4f&&Mathf.Abs(Shader.GetGlobalFloat("_ZRWeatherSoilMoisture")-clock.SoilMoisture)<.001f,"Wet surface "+clock.Wetness+"; soil "+clock.SoilMoisture+"; shader soil "+Shader.GetGlobalFloat("_ZRWeatherSoilMoisture"));
            View(new Vector3(-46,2.6f,31),new Vector3(-46,.1f,36));yield return Hold(2);yield return Capture("08b-residual-ground-moisture.png");
            View(new Vector3(103,2.8f,67),new Vector3(123,3,81));
            clock.Advance(65);clock.SetHour(12);yield return Hold(5);yield return Capture("09-dried.png");
            Check("Remaining water evaporates and drains in game time",clock.Wetness<.01f&&clock.PuddleAmount<.01f&&wetBefore>.1f&&puddleBefore>.1f,"Before "+wetBefore+" / "+puddleBefore+"; after "+clock.Wetness+" / "+clock.PuddleAmount);
            Check("Time acceleration keeps original physics clocks",Mathf.Approximately(Time.timeScale,1)&&Mathf.Approximately(Time.fixedDeltaTime,.02f),"Time scale "+Time.timeScale+"; fixed delta "+Time.fixedDeltaTime);
            string manifestRoot=Application.platform==RuntimePlatform.OSXPlayer
                ?Path.GetFullPath(Path.Combine(Application.dataPath,"../.."))
                :Path.GetDirectoryName(Application.dataPath);
            string manifest=Path.Combine(manifestRoot,"Westbruch-build.json");
            if(!Application.isEditor)
            {
                string json=File.Exists(manifest)?File.ReadAllText(manifest):"";
                Check("Actual launcher loads new weather release",json.Contains("weather and daily rhythm")&&json.Contains(Application.buildGUID),"Running build GUID "+Application.buildGUID+"; manifest "+json);
            }
            Finish();
        }
        void CoreChecks()
        {
            clock.state=new WorldWeatherClock.WorldState();clock.SetWeather(WeatherKind.Clear);clock.SetHour(23.9f);clock.SetSpeed(20);int day=clock.DayNumber;clock.Advance(1);
            Check("Clock wraps midnight into the next day",clock.DayNumber==day+1&&clock.Hour<.1f,"Day "+clock.DayNumber+"; hour "+clock.Hour);
            foreach(float speed in new[]{1f,2f,5f,10f,20f})
            {clock.SetSpeed(speed);double before=clock.TotalGameMinutes;clock.Advance(2);Check("World clock "+speed+"x",Math.Abs(clock.TotalGameMinutes-before-speed)<.001,"Game minutes "+(clock.TotalGameMinutes-before));}
            clock.SetSpeed(20);clock.SetHour(12);clock.SetWeather(WeatherKind.Rain);clock.Advance(60);
            string saved=clock.ExportState();float wet=clock.Wetness,puddle=clock.PuddleAmount,soil=clock.SoilMoisture;
            Check("Rain saturates different surface reservoirs",wet>.8f&&puddle>.3f&&soil>.4f,saved);
            clock.SetWeather(WeatherKind.Clear);clock.Advance(1);
            Check("Rain stopping keeps water on the ground",clock.Wetness>.5f&&clock.PuddleAmount>.2f);
            Check("World save restores clock, weather and residual water",clock.ImportState(saved)&&Mathf.Approximately(clock.Wetness,wet)&&Mathf.Approximately(clock.PuddleAmount,puddle)&&Mathf.Approximately(clock.SoilMoisture,soil),clock.ExportState());
            Check("Broken future save is rejected",!clock.ImportState("{\"version\":999}"));
            clock.state=new WorldWeatherClock.WorldState();clock.SetWeather(WeatherKind.Cloudy,true);clock.state.nextWeather=clock.TotalGameMinutes+.1;clock.Advance(1);
            Check("Automatic weather chooses a next state and schedules its duration",clock.Automatic&&clock.Weather!=WeatherKind.Cloudy&&clock.state.nextWeather>clock.TotalGameMinutes,clock.ExportState());
            Check("Daylight and density schedules differ",WorldWeatherClock.DaylightAt(12)>.99f&&WorldWeatherClock.DaylightAt(0)<.01f&&CityDailyRhythm.PedestrianDemand(12)>CityDailyRhythm.PedestrianDemand(0)*2&&CityDailyRhythm.TrafficDemand(8)>CityDailyRhythm.TrafficDemand(0)*1.8f);
        }
        IEnumerator DriveInRain(bool night=false)
        {
            drive=false;body.linearVelocity=body.angularVelocity=Vector3.zero;body.position=new Vector3(103,.32f,77.5f);body.rotation=Quaternion.Euler(0,90,0);
            car.transform.SetPositionAndRotation(body.position,body.rotation);Physics.SyncTransforms();for(int i=0;i<40;i++)yield return new WaitForFixedUpdate();
            Place(car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f)));FollowVehicle();
            car.Enter(player);float deadline=Time.unscaledTime+18;while(player.Interacting&&Time.unscaledTime<deadline){FollowVehicle();yield return null;}
            Check((night?"Night":"Wet weather")+" preserves animated vehicle entry",player.driving&&car.State==SimpleCar.Occupancy.Driving);
            if(!player.driving)yield break;
            yield return Hold(.5f);
            if(night)Check("Occupied player's mounted headlights switch on",PlayerHeadlights()==2,"Enabled player lamps "+PlayerHeadlights());
            var take=Application.isEditor?null:GameplayAudioRecording.Begin(camera,42);
            drive=true;float start=body.position.x,peak=0;deadline=Time.unscaledTime+24;
            while(body.position.x<145&&Time.unscaledTime<deadline)
            {peak=Mathf.Max(peak,body.linearVelocity.magnitude);throttle=body.linearVelocity.magnitude<7.5f?.65f:0;FollowVehicle();yield return null;}
            throttle=0;drive=false;car.DriveStep(0,0,true);deadline=Time.unscaledTime+4;while(body.linearVelocity.magnitude>.15f&&Time.unscaledTime<deadline){car.DriveStep(0,0,true);FollowVehicle();yield return null;}
            Check("Real vehicle drives through "+(night?"night":"wet")+" bridge road",body.position.x>140&&peak>4,"Distance "+(body.position.x-start)+" m; peak speed "+peak+" m/s");
            yield return Capture(night?"night-driving.png":"wet-weather-driving.png");EndTake(take,night?"night-driving.wav":"rain-driving.wav");
            car.TryExit(player);deadline=Time.unscaledTime+18;while(player.Interacting&&Time.unscaledTime<deadline){FollowVehicle();yield return null;}
            Check((night?"Night":"Wet weather")+" preserves real exit",!player.driving&&!player.Interacting);
            if(night){yield return Hold(.5f);Check("Parked player car switches headlamps off",PlayerHeadlights()==0);}
        }
        int PlayerHeadlights(){int count=0;foreach(var lamp in car.GetComponentsInChildren<Light>())if(lamp.name=="Mounted automatic dipped headlamp"&&lamp.enabled)count++;return count;}
        IEnumerator NightStorm(WestbruchWeatherAudio audio,WestbruchWeatherVisuals visual)
        {
            bool flashed=false;Action<float> observed=_=>flashed=true;audio.Lightning+=observed;
            clock.SetWeather(WeatherKind.Storm);clock.Advance(14);clock.SetHour(0);
            var take=Application.isEditor?null:GameplayAudioRecording.Begin(camera,56);
            float deadline=Time.unscaledTime+47,peak=0;int thunder=audio.ThunderCount;
            while(!flashed&&Time.unscaledTime<deadline){peak=Mathf.Max(peak,visual.LightningBoostLux);yield return null;}
            if(flashed){yield return null;peak=Mathf.Max(peak,visual.LightningBoostLux);yield return Capture("night-storm-flash.png");}
            yield return Hold(4);EndTake(take,"night-storm.wav");audio.Lightning-=observed;
            Check("Night lightning respects adapted illumination",flashed&&peak>1&&peak<=28.01f&&audio.ThunderCount>thunder,"Peak added lux "+peak+"; thunder events "+(audio.ThunderCount-thunder));
            clock.SetWeather(WeatherKind.Clear);clock.Advance(14);clock.SetHour(0);yield return Hold(4);
        }
        void FixedUpdate(){if(drive&&car)car.DriveStep(throttle,Mathf.Clamp((77.5f-body.position.z)*.3f+Mathf.DeltaAngle(car.transform.eulerAngles.y,90)/25,-1,1),false);}
        void FollowVehicle()=>View(car.transform.position-car.transform.forward*6.5f+car.transform.right*2+Vector3.up*2.7f,car.transform.position+Vector3.up);
        void Place(Vector3 point)
        {
            var control=player.GetComponent<CharacterController>();control.enabled=false;
            if(Physics.Raycast(point+Vector3.up*3,Vector3.down,out var ground,7,1,QueryTriggerInteraction.Ignore))point=ground.point;
            player.transform.position=point+Vector3.up*.035f;player.SetDriving(false);Physics.SyncTransforms();
        }
        void View(Vector3 at,Vector3 target){camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(target-at));}
        static float SunIntensity(){var sun=GameObject.Find("Late afternoon sun");return sun?sun.GetComponent<Light>().intensity:0;}
        IEnumerator Watchdog(){yield return new WaitForSecondsRealtime(600);Check("QA completes within ten minutes",false);Finish();}
        void LogSystem(string phase,params Component[] systems)
        {
            foreach(var system in systems)
            {
                var values=new List<string>();foreach(var p in system.GetType().GetProperties())if(p.DeclaringType==system.GetType()&&p.GetIndexParameters().Length==0&&(p.PropertyType.IsPrimitive||p.PropertyType==typeof(string)))values.Add(p.Name+"="+p.GetValue(system));
                report.Add(phase+" "+system.GetType().Name+": "+string.Join("; ",values));
            }
        }
        IEnumerator Hold(float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until)yield return null;}
        IEnumerator SunsetTransitionReview(WestbruchWeatherVisuals visual)
        {
            // HDRP advances its sky environment with scaled renderer time.
            // Freeze scene activity and cloud drift, never the renderer clock.
            Time.timeScale=1;
            var trafficSystem=FindAnyObjectByType<WestbruchTraffic>();if(trafficSystem)trafficSystem.enabled=false;
            if(CityDailyRhythm.Instance)CityDailyRhythm.Instance.enabled=false;
            var atmosphere=GameObject.Find("Westbruch atmosphere").GetComponent<UnityEngine.Rendering.Volume>();
            if(atmosphere.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.VolumetricClouds>(out var cloudField))
            {
                cloudField.globalWindSpeed.Override(new UnityEngine.Rendering.HighDefinition.WindParameter.WindParamaterValue{mode=UnityEngine.Rendering.HighDefinition.WindParameter.WindOverrideMode.Custom,customValue=0});
                cloudField.verticalShapeWindSpeed.Override(0);cloudField.verticalErosionWindSpeed.Override(0);
            }
            var csv=new List<string>{"mode,weather,hour,skyMean,exposureEV,sunLux"};
            var skyAt=new Vector3(100,28,70);var skyTarget=skyAt+new Vector3(-86.6f,35,50);
            foreach(var weather in new[]{WeatherKind.Clear,WeatherKind.Cloudy,WeatherKind.Storm})
            {
                clock.SetSpeed(1);clock.SetWeather(weather);clock.Advance(90);clock.SetHour(12);
                View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(2);yield return Capture("sunset-noon-"+weather+".png");
                View(skyAt,skyTarget);float previous=-1,largestRise=0;bool monotone=true;
                for(int step=0;step<=12;step++)
                {
                    float hour=19.75f+step/12f;clock.SetHour(hour);yield return Hold(1.3f);
                    string filename="sunset-"+weather+"-"+Mathf.RoundToInt(hour*60).ToString("0000")+".png";
                    yield return Capture(filename);
                    var shot=new Texture2D(2,2);shot.LoadImage(File.ReadAllBytes(Path.Combine(output,filename)));float mean=UpperSkyMean(shot);Destroy(shot);
                    csv.Add(SunsetRow("fixed",weather,hour,mean,visual));
                    if(hour>=20&&hour<=20.667f)
                    {
                        if(previous>=0){float rise=mean-previous;largestRise=Mathf.Max(largestRise,rise);monotone&=rise<=Mathf.Max(.0015f,previous*.03f);}
                        previous=mean;
                    }
                }
                Check("Evening sky fades without rebrightening in "+weather,monotone,"Largest 5-minute increase "+largestRise);
            }
            clock.SetWeather(WeatherKind.Clear);clock.Advance(90);
            foreach(float speed in new[]{1f,20f})
            {
                clock.SetHour(20);clock.SetSpeed(speed);View(skyAt,skyTarget);yield return Hold(2);
                clock.enabled=true;float nextHour=20,previous=-1,largestRise=0,largestDrop=0,first=-1;bool monotone=true;int samples=0;
                while(clock.Hour<20.69f)
                {
                    if(clock.Hour>=nextHour)
                    {
                        yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();float mean=UpperSkyMean(shot);float hour=clock.Hour;
                        if(samples%5==0||hour>20.65f)File.WriteAllBytes(Path.Combine(output,"sunset-live-"+speed+"x-"+Mathf.RoundToInt(hour*60)+".png"),shot.EncodeToPNG());Destroy(shot);
                        csv.Add(SunsetRow("live-"+speed+"x",WeatherKind.Clear,hour,mean,visual));
                        if(first<0)first=mean;
                        if(previous>=0){float rise=mean-previous;largestRise=Mathf.Max(largestRise,rise);largestDrop=Mathf.Max(largestDrop,-rise);monotone&=rise<=Mathf.Max(.0015f,previous*.03f);}
                        previous=mean;nextHour=hour+2f/60;samples++;
                    }
                    yield return null;
                }
                clock.enabled=false;
                Check("Running "+speed+"x sunset has no sky brightness reversal",monotone&&samples>=12,"Samples "+samples+"; maximum rise "+largestRise);
                Check("Running "+speed+"x sunset has no abrupt sky blackout",largestDrop<=Mathf.Max(.015f,first*.45f),"Largest 2-minute drop "+largestDrop+"; initial sky mean "+first);
            }
            clock.SetSpeed(1);clock.SetHour(20+40f/60);View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(2);yield return Capture("sunset-final-street.png");
            Time.timeScale=1;File.WriteAllLines(Path.Combine(output,"sunset-luminance.csv"),csv);
        }
        static string SunsetRow(string mode,WeatherKind weather,float hour,float mean,WestbruchWeatherVisuals visual)
        {
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            return mode+","+weather+","+hour.ToString("F5",culture)+","+mean.ToString("F7",culture)+","+visual.Exposure.ToString("F5",culture)+","+visual.SunIntensity.ToString("F5",culture);
        }
        static float UpperSkyMean(Texture2D image)
        {
            // Average an unobstructed sky region above the sun disk and horizon; individual stars do not dominate it.
            var pixels=image.GetPixels32();float sum=0;int count=0;
            for(int y=image.height/2;y<image.height-8;y+=4)for(int x=8;x<image.width-8;x+=4)
            {sum+=SkyLuma(pixels[y*image.width+x]);count++;}
            return sum/Mathf.Max(1,count);
        }
        IEnumerator NightSkyReview(WestbruchWeatherVisuals visual)
        {
            clock.SetWeather(WeatherKind.Clear);clock.Advance(90);clock.SetHour(0);yield return Hold(8);
            View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(3);yield return Capture("night-map-overview.png");
            View(new Vector3(94,1.8f,42),new Vector3(94,2.5f,80));yield return Hold(3);yield return Capture("night-main-street.png");
            var shelter=GameObject.Find("Old Westbruch bus shelter");
            if(shelter){View(shelter.transform.position-shelter.transform.forward*6+Vector3.up*1.6f,shelter.transform.position+Vector3.up*1.4f);yield return Hold(3);yield return Capture("night-bus-shelter.png");}
            foreach(var fixture in FindObjectsByType<Transform>())if(fixture.name=="Shop display window")
            {View(fixture.position-fixture.forward*8+Vector3.up*.6f,fixture.position);yield return Hold(3);yield return Capture("night-shopfront.png");break;}
            View(new Vector3(100,28,70),new Vector3(100,78,-16.6f));yield return Hold(5);yield return Capture("night-clear-sky.png");
            Check("Native star cubemap is included",visual.StarMapLoaded);
            Check("Clear weather reveals stars",visual.StarVisibility>.9f,"Star visibility "+visual.StarVisibility);
            Check("Moon illumination remains subtle",visual.MoonIntensity>0&&visual.MoonIntensity<=.31f,"Moon lux "+visual.MoonIntensity);
            var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(Path.Combine(output,"night-clear-sky.png")));var pixels=image.GetPixels32();
            float sum=0;int count=0,peaks=0;int width=image.width,height=image.height;
            for(int y=height/3;y<height-5;y++)for(int x=5;x<width-5;x++)
            {
                float value=SkyLuma(pixels[y*width+x]);sum+=value;count++;
                if(value<.035f)continue;
                if(value<SkyLuma(pixels[y*width+x-1])||value<SkyLuma(pixels[y*width+x+1])||value<SkyLuma(pixels[(y-1)*width+x])||value<SkyLuma(pixels[(y+1)*width+x]))continue;
                float surround=(SkyLuma(pixels[y*width+x-4])+SkyLuma(pixels[y*width+x+4])+SkyLuma(pixels[(y-4)*width+x])+SkyLuma(pixels[(y+4)*width+x]))*.25f;
                if(value>surround*1.8f+.005f)peaks++;
            }
            Destroy(image);
            Check("Rendered midnight sky stays dark",sum/Mathf.Max(1,count)<.025f,"Mean sky luminance "+sum/Mathf.Max(1,count));
            Check("Rendered clear sky contains distinct stars",peaks>20,"Bright local peaks "+peaks);
            clock.SetWeather(WeatherKind.Storm);clock.Advance(90);clock.SetHour(0);yield return Hold(8);yield return Capture("night-overcast-sky.png");
            Check("Overcast weather hides stars",visual.StarVisibility<.01f,"Star visibility "+visual.StarVisibility);
            View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(3);yield return Capture("night-overcast-street.png");
            clock.SetWeather(WeatherKind.Clear);clock.Advance(90);clock.SetHour(0);yield return Hold(5);yield return Capture("07-night.png");
            LogSystem("Night atmosphere",visual);
        }
        static float SkyLuma(Color32 p)=>(p.r*.2126f+p.g*.7152f+p.b*.0722f)/255f;
        IEnumerator NightLightingReview(WestbruchWeatherVisuals visual)
        {
            clock.SetHour(0);clock.SetWeather(WeatherKind.Clear);clock.Advance(30);clock.SetHour(0);
            yield return Hold(5);yield return Capture("night-overview.png");
            float measuredUntil=Time.unscaledTime+3,frameTotal=0;int frameSamples=0;
            while(Time.unscaledTime<measuredUntil){yield return null;frameTotal+=Time.unscaledDeltaTime;frameSamples++;}
            report.Add("Stable night overview frame time: "+(frameTotal/Mathf.Max(1,frameSamples)*1000).ToString("F2")+" ms; samples "+frameSamples+"; resolution "+Screen.width+"x"+Screen.height);
            Check("Combined vehicle lenses have individual emissive materials",visual.HeadlampEmitterMaterials>1,"Emitter groups "+visual.HeadlampEmitterMaterials);
            Check("Street lighting extends beyond nearest-eight budget",visual.ActiveStreetLights>8,"Active street lights "+visual.ActiveStreetLights);
            foreach(float distance in new[]{30f,64f,66f,90f,130f})
            {
                Check("No headlamp cutoff at "+distance+" metres",WestbruchWeatherVisuals.HeadlightVisibility(distance)>.99f);
                Check("No streetlamp cutoff at "+distance+" metres",WestbruchWeatherVisuals.StreetlightVisibility(distance)>.99f);
            }
            Check("Light distance tails fade gradually",WestbruchWeatherVisuals.HeadlightVisibility(180)>.3f&&WestbruchWeatherVisuals.HeadlightVisibility(180)<.9f&&WestbruchWeatherVisuals.StreetlightVisibility(220)>.3f&&WestbruchWeatherVisuals.StreetlightVisibility(220)<.9f);
            Time.timeScale=0;
            var trafficSystem=FindAnyObjectByType<WestbruchTraffic>();if(trafficSystem)trafficSystem.enabled=false;
            if(CityDailyRhythm.Instance)CityDailyRhythm.Instance.enabled=false;
            TrafficVehicle subject=null;float best=float.PositiveInfinity;
            foreach(var vehicle in FindObjectsByType<TrafficVehicle>())if(vehicle.DailyActive)
            {float d=(vehicle.transform.position-new Vector3(85,0,77.5f)).sqrMagnitude;if(d<best){best=d;subject=vehicle;}}
            Check("Night review has an active traffic car",subject);
            if(subject)
            {
                var reviewPosition=new Vector3(85,subject.transform.position.y,77.5f);var reviewRotation=Quaternion.Euler(0,90,0);
                if(subject.TryGetComponent<Rigidbody>(out var reviewBody)){reviewBody.interpolation=RigidbodyInterpolation.None;reviewBody.position=reviewPosition;reviewBody.rotation=reviewRotation;}
                subject.transform.SetPositionAndRotation(reviewPosition,reviewRotation);Physics.SyncTransforms();
                var front=subject.transform.forward;var center=subject.transform.position;
                var headlamps=subject.GetComponentsInChildren<Light>();
                View(center+front*11+Vector3.up*1.3f+subject.transform.right*2,center+Vector3.up*.6f);yield return Hold(3);yield return Capture("night-dipped-beam-front.png");
                foreach(float distance in new[]{15f,35f,64f,66f,95f,150f})
                {
                    View(center-front*distance+Vector3.up*2.4f+subject.transform.right*3,center+front*15);yield return Hold(2);
                    int active=0;float intensity=0;
                    foreach(var lamp in headlamps)if(lamp.name=="Mounted automatic dipped headlamp"&&lamp.enabled){active++;intensity+=lamp.intensity;}
                    Check("Actual traffic headlamps remain present at "+distance+"m",active==2,"Enabled "+active+"; total intensity "+intensity);
                    string shot="night-range-"+distance.ToString("000")+"m.png";
                    yield return Capture(shot);
                    if(distance==64||distance==66||distance==95)
                    {
                        var sample=camera.WorldToScreenPoint(center+front*18+Vector3.up*.05f);
                        float on=GroundPatch(Path.Combine(output,shot),sample);
                        foreach(var lamp in headlamps)if(lamp.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var data)){data.affectDiffuse=false;data.affectSpecular=false;}
                        yield return Hold(1);string control="night-control-"+distance.ToString("000")+"m.png";yield return Capture(control);
                        float off=GroundPatch(Path.Combine(output,control),sample);
                        Check("Rendered road receives headlamp light at "+distance+"m",on-off>.0001f,"On "+on+"; off "+off+"; difference "+(on-off));
                        foreach(var lamp in headlamps)if(lamp.TryGetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>(out var data)){data.affectDiffuse=true;data.affectSpecular=true;}
                        yield return Hold(1);
                    }
                }
                View(center-front*7+Vector3.up*2+subject.transform.right*3,center+front*20);yield return Hold(3);yield return Capture("night-dry-road-beams.png");
                clock.state.wetness=1;clock.state.puddles=1;
                yield return Hold(4);yield return Capture("night-wet-road-beams.png");
                var puddle=visual.PuddleInspectionPoint;View(puddle+new Vector3(-2,.35f,-2),puddle+Vector3.up*.08f);yield return Hold(3);yield return Capture("night-water-reflections.png");
            }
            Time.timeScale=1;
            View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(3);yield return Capture("07-night.png");
            yield return NightWindow("night-window-spill.png");
            LogSystem("Final night lighting",visual);
        }
        static float GroundPatch(string path,Vector3 sample)
        {
            var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));float sum=0;int count=0;
            int cx=Mathf.RoundToInt(sample.x),cy=Mathf.RoundToInt(sample.y);
            // Integrate the road region: a tiny patch is dominated by temporal jitter on gravel and painted markings.
            for(int y=cy-40;y<=cy+40;y++)for(int x=cx-40;x<=cx+40;x++)
            {var p=texture.GetPixel(Mathf.Clamp(x,0,texture.width-1),Mathf.Clamp(y,0,texture.height-1));sum+=p.r*.2126f+p.g*.7152f+p.b*.0722f;count++;}
            Destroy(texture);return sum/count;
        }
        IEnumerator TwilightRegression(WestbruchWeatherVisuals visual)
        {
            // Intermediate twilight states must remain playable; noon and
            // midnight alone miss a mismatch between moonlight and exposure.
            foreach(float hour in new[]{0f,19.5f,19.8f,19.9f,20f,20.2f,20.3f,20.5f,5.2f,5.4f,5.6f,5.8f,6f})
            {
                clock.SetHour(hour);yield return Hold(4);
                string name="twilight-"+hour.ToString("00.00",System.Globalization.CultureInfo.InvariantCulture)+".png";
                yield return Capture(name);CheckTwilightImage(name,"Twilight visibility at "+hour.ToString("F2"));
                LogSystem("Twilight "+hour,visual);
            }
            // Exercise the normal Update clock, HUD and interpolated rendering
            // together, with no manual Advance calls during either transition.
            foreach(float start in new[]{19.5f,5.2f})
            {
                clock.SetHour(start);clock.SetSpeed(20);clock.enabled=true;
                float end=Time.unscaledTime+10;int samples=0;float minimumFraction=1;
                while(Time.unscaledTime<end)
                {
                    yield return Hold(.8f);Texture2D shot;
                    if(Application.isEditor)
                    {
                        yield return Capture("twilight-live-sample.png");shot=new Texture2D(2,2);shot.LoadImage(File.ReadAllBytes(Path.Combine(output,"twilight-live-sample.png")));
                    }
                    else {yield return new WaitForEndOfFrame();shot=ScreenCapture.CaptureScreenshotAsTexture();}
                    minimumFraction=Mathf.Min(minimumFraction,TwilightReadableFraction(shot));Destroy(shot);samples++;
                }
                Check("Enabled 20x world clock stays readable through "+(start>12?"sunset":"dawn"),minimumFraction>.12f&&clock.Hour>start+1.4f,"Samples "+samples+"; minimum ground visibility "+minimumFraction+"; end hour "+clock.Hour);
                yield return Capture(start>12?"twilight-live-sunset.png":"twilight-live-dawn.png");clock.enabled=false;
            }
            clock.SetSpeed(1);clock.SetHour(12);
        }
        void CheckTwilightImage(string filename,string label)
        {
            var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(Path.Combine(output,filename)));
            float fraction=TwilightReadableFraction(image);Destroy(image);Check(label,fraction>.12f,"Readable ground pixel fraction "+fraction);
        }
        static float TwilightReadableFraction(Texture2D image)
        {
            // Lower left/middle: street and ground, excluding sky and all UI.
            var pixels=image.GetPixels32();int count=0,readable=0;
            for(int y=image.height/10;y<image.height*45/100;y+=2)for(int x=image.width/20;x<image.width*55/100;x+=2)
            {var p=pixels[y*image.width+x];if((p.r*.2126f+p.g*.7152f+p.b*.0722f)/255>.025f)readable++;count++;}
            return readable/(float)count;
        }
        IEnumerator NightWindow(string filename)
        {
            Transform room=null;float score=float.PositiveInfinity;
            foreach(var t in FindObjectsByType<Transform>())if(t.name.StartsWith("Visible apartment interior ")&&t.position.y>2)
            {float distance=Vector3.Distance(t.position,player.transform.position)+t.position.y*3;if(distance<score){score=distance;room=t;}}
            if(!room)yield break;
            var at=room.position-room.forward*9+room.right*3;at.y=2.1f;View(at,room.position);yield return Hold(4);yield return Capture(filename);
            View(new Vector3(103,2.8f,67),new Vector3(123,3,81));yield return Hold(2);
        }
        IEnumerator Capture(string filename)
        {
            if(Application.isEditor)
            {
                var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
                for(int i=0;i<8;i++){RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});yield return null;}
                var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,filename),image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Destroy(image);Destroy(rt);
            }
            else {ScreenCapture.CaptureScreenshot(Path.Combine(output,filename));yield return Hold(.25f);}
            if(filename=="07-night.png")
            {
                var diagnostic=new Texture2D(2,2);diagnostic.LoadImage(File.ReadAllBytes(Path.Combine(output,filename)));int readable=0;double sum=0;
                foreach(var pixel in diagnostic.GetPixels32()){float light=(pixel.r*.2126f+pixel.g*.7152f+pixel.b*.0722f)/255;sum+=light;if(light>.025f)readable++;}
                float fraction=readable/(float)(diagnostic.width*diagnostic.height);double mean=sum/(diagnostic.width*diagnostic.height);Destroy(diagnostic);
                Check("Night scene retains visible streets and silhouettes",fraction>.20f,"Readable pixel fraction "+fraction+"; mean sRGB luma "+mean);
            }
        }
        void EndTake(GameplayAudioRecording take,string filename)
        {
            if(!take)return;var stats=take.End(Path.Combine(output,filename));Check("Real listener recording "+filename,stats.frames>48000&&stats.peak>.001f&&stats.peak<.93f,"Seconds "+stats.seconds+"; peak "+stats.peak+"; RMS "+stats.rms);
        }
        void Finish(){failed|=errors>0;report.Add("Runtime errors: "+errors);File.WriteAllText(Path.Combine(output,"world-weather-validation.txt"),"Passed: "+!failed+"\n"+string.Join("\n",report));Application.Quit(failed?1:0);}
    }
}
