using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    // Weather voices observe the real listener, roofs and vehicle contacts.
    // Time acceleration affects the weather state, never clip pitch or cooldowns.
    [DefaultExecutionOrder(360)]
    public sealed class WestbruchWeatherAudio : MonoBehaviour
    {
        public AudioClip rainRoad,rainRoof,rainLeaves,wind,thunder,wetTyres;
        public AudioClip[] tailDrips;
        public event Action<float> Lightning;
        public float ShelterBlend { get; private set; }
        public float FoliageBlend { get; private set; }
        public float RainGain=>roadVoice?roadVoice.volume:0;
        public float RoofGain=>roofVoice?roofVoice.volume:0;
        public float LeavesGain=>leavesVoice?leavesVoice.volume:0;
        public float WindGain=>windVoice?windVoice.volume:0;
        public float WetTyreGain { get; private set; }
        public int WetTyreVoices { get; private set; }
        public int ThunderCount { get; private set; }
        public int LightningCount { get; private set; }
        public int TailDripCount { get; private set; }
        public bool Cabin { get; private set; }
        public bool Ready=>roadVoice&&rainRoad&&rainRoof&&rainLeaves&&thunder&&wetTyres;
        public float NextThunderSeconds=>Mathf.Max(0,nextStorm-Time.unscaledTime);

        sealed class TyreVoice
        {
            public Transform body;
            public SimpleCar car;
            public VehicleSoundFx contacts;
            public TrafficVehicle traffic;
            public TrafficSoundFx trafficAudio;
            public AudioSource source;
            public AudioLowPassFilter filter;
            public float exposed=1,phase;
        }
        readonly List<TyreVoice> tyres=new List<TyreVoice>();
        readonly System.Random random=new System.Random(221026);
        AudioSource roadVoice,roofVoice,leavesVoice,windVoice,stormVoice,dripVoice;
        AudioLowPassFilter roadFilter,leavesFilter,windFilter,stormFilter;
        ThirdPersonMotor player;
        Transform listener;
        WestbruchSoundscape mix;
        GameplayAudioDirector director;
        float probeAt,nextDrip,nextStorm,pendingThunder=-1,thunderStrength;
        float shelterTarget,leavesTarget,metalTarget,metalBlend;
        Vector3 roofPoint;
        bool wasStorm,started;

        void Start()
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();
            var found=FindAnyObjectByType<AudioListener>();listener=found?found.transform:null;
            mix=FindAnyObjectByType<WestbruchSoundscape>();director=GameplayAudioDirector.Active;
            roadVoice=Bed("Weather: rain on hard ground",rainRoad,.073f,out roadFilter);
            roofVoice=Bed("Weather: real roof rain",rainRoof,.311f,out _);
            leavesVoice=Bed("Weather: rain on actual nearby leaves",rainLeaves,.613f,out leavesFilter);
            windVoice=Bed("Weather: gusts through vegetation",wind,.427f,out windFilter);
            stormVoice=Bed("Weather: distant thunder",null,0,out stormFilter);
            stormVoice.loop=false;stormVoice.volume=1;
            dripVoice=GameplayAudioDirector.Source(transform,"Weather: water draining after rain",Vector3.zero,null,false,1.2f,13,125);
            dripVoice.volume=1;
            var reflections=GameplayAudioDirector.Reflections(dripVoice);reflections.reverbLevel=-3500;
            foreach(var car in FindObjectsByType<SimpleCar>(FindObjectsInactive.Include))
            {
                var contact=car.GetComponent<VehicleSoundFx>();
                tyres.Add(CreateTyres(car.transform,car,contact,null,null,tyres.Count));
            }
            foreach(var car in FindObjectsByType<TrafficVehicle>(FindObjectsInactive.Include))
                tyres.Add(CreateTyres(car.transform,null,null,car,car.GetComponent<TrafficSoundFx>(),tyres.Count));
            nextDrip=Time.unscaledTime+9;nextStorm=Time.unscaledTime+24+Random01()*18;
            started=true;Probe();
        }
        AudioSource Bed(string title,AudioClip clip,float phase,out AudioLowPassFilter filter)
        {
            var source=GameplayAudioDirector.Source(transform,title,Vector3.zero,clip,true,1,50,85);
            source.spatialBlend=0;source.dopplerLevel=0;source.volume=0;
            filter=source.gameObject.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=15000;
            if(clip){source.Play();source.time=phase*clip.length;}
            return source;
        }
        TyreVoice CreateTyres(Transform body,SimpleCar car,VehicleSoundFx contacts,TrafficVehicle traffic,TrafficSoundFx trafficAudio,int index)
        {
            var source=GameplayAudioDirector.Source(body,"Weather: wet tyre contact",new Vector3(0,.24f,0),wetTyres,true,2.2f,25,135);
            source.dopplerLevel=0;source.volume=0;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.25f,.28f),new Keyframe(.65f,.025f),new Keyframe(1,0)));
            if(wetTyres&&body.gameObject.activeInHierarchy){source.Play();source.time=Mathf.Repeat(index*.317f,.94f)*wetTyres.length;}
            return new TyreVoice{body=body,car=car,contacts=contacts,traffic=traffic,trafficAudio=trafficAudio,source=source,phase=Mathf.Repeat(index*.317f,.94f),filter=source.gameObject.AddComponent<AudioLowPassFilter>()};
        }
        void Update()
        {
            var clock=WorldWeatherClock.Instance;
            if(!started||!clock||!listener)return;
            if(!director)director=GameplayAudioDirector.Active;
            float dt=Time.unscaledDeltaTime,fade=1-Mathf.Exp(-dt/1.3f);
            if(Time.unscaledTime>=probeAt){probeAt=Time.unscaledTime+.24f;Probe();}
            ShelterBlend=Mathf.Lerp(ShelterBlend,shelterTarget,fade);
            FoliageBlend=Mathf.Lerp(FoliageBlend,leavesTarget,fade);
            metalBlend=Mathf.Lerp(metalBlend,metalTarget,fade);
            float ambient=mix?mix.AmbienceGain:.7f;
            float rain=clock.RainIntensity;
            float cabin=Cabin?1:0;
            // Even under cover, rain falling in the adjoining street remains
            // audible, muffled and quieter. Metal taps occur only on real metal
            // shelter or the occupied car, never on a concrete overpass.
            float outside=Mathf.Lerp(1,.28f,ShelterBlend)*Mathf.Lerp(1,.33f,cabin);
            SetBed(roadVoice,rain*.30f*ambient*outside*Mathf.Lerp(1,.67f,FoliageBlend),fade);
            SetBed(roofVoice,rain*.24f*ambient*Mathf.Max(metalBlend,cabin*(1-ShelterBlend)),fade);
            SetBed(leavesVoice,rain*.34f*ambient*FoliageBlend*(1-ShelterBlend)*Mathf.Lerp(1,.22f,cabin),fade);
            float gust=.84f+.16f*Mathf.Sin(Time.unscaledTime*.173f+2.1f);
            SetBed(windVoice,Mathf.Max(0,clock.WindStrength-.25f)*.11f*ambient*outside*gust,fade);
            roadFilter.cutoffFrequency=Mathf.Lerp(10000,2300,Mathf.Max(ShelterBlend,cabin));
            leavesFilter.cutoffFrequency=Mathf.Lerp(11000,2400,cabin);
            windFilter.cutoffFrequency=Mathf.Lerp(7500,1600,Mathf.Max(ShelterBlend,cabin));
            stormFilter.cutoffFrequency=Mathf.Lerp(6200,1700,Mathf.Max(ShelterBlend,cabin));
            UpdateTyres(clock,dt);
            if(clock.IsStorm&&!wasStorm)nextStorm=Time.unscaledTime+24+Random01()*18;
            wasStorm=clock.IsStorm;
            if(!clock.IsStorm){pendingThunder=-1;}
            else if(pendingThunder<0&&Time.unscaledTime>=nextStorm)
            {
                thunderStrength=.40f+Random01()*.50f;
                pendingThunder=Time.unscaledTime+.9f+Random01()*2.6f;
                nextStorm=Time.unscaledTime+45+Random01()*50;
                LightningCount++;Lightning?.Invoke(thunderStrength);
            }
            if(pendingThunder>=0&&Time.unscaledTime>=pendingThunder)
            {
                pendingThunder=-1;
                if(thunder){stormVoice.pitch=.97f+Random01()*.06f;stormVoice.PlayOneShot(thunder,(.21f+thunderStrength*.15f)*ambient*Mathf.Lerp(1,.64f,Mathf.Max(ShelterBlend,cabin)));ThunderCount++;}
            }
            if(Time.unscaledTime>=nextDrip)
            {
                nextDrip=Time.unscaledTime+5+Random01()*10;
                // Draining eaves stay active after rainfall and fade as the
                // clock dries accumulated water. No repeated drip in open air.
                float water=Mathf.Max(rain*.45f,clock.PuddleAmount);
                if(water>.045f&&shelterTarget>.25f&&!Cabin&&tailDrips!=null&&tailDrips.Length>0)
                {
                    var clip=tailDrips[random.Next(tailDrips.Length)];
                    if(clip){dripVoice.transform.position=roofPoint;dripVoice.pitch=.95f+Random01()*.09f;dripVoice.PlayOneShot(clip,.12f*water*ambient);TailDripCount++;}
                }
            }
        }
        void UpdateTyres(WorldWeatherClock clock,float dt)
        {
            WetTyreGain=0;WetTyreVoices=0;
            float fade=1-Mathf.Exp(-dt*6),vehicles=mix?mix.VehicleGain:.85f;
            foreach(var voice in tyres)
            {
                if(!voice.body||!voice.source)continue;
                if(!voice.body.gameObject.activeInHierarchy||(voice.traffic&&!voice.traffic.isActiveAndEnabled))
                {voice.source.Stop();voice.source.volume=0;continue;}
                float distance=Vector3.Distance(listener.position,voice.body.position),motion=0;
                if(distance<25)
                {
                    if(voice.car)
                    {
                        if(!voice.contacts)voice.contacts=voice.car.GetComponent<VehicleSoundFx>();
                        if(voice.contacts&&voice.contacts.GroundedWheels>0)motion=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,18,voice.contacts.PlanarSpeed));
                    }
                    else if(voice.traffic)
                    {
                        if(!voice.trafficAudio)voice.trafficAudio=voice.traffic.GetComponent<TrafficSoundFx>();
                        if(voice.trafficAudio&&voice.trafficAudio.audible)motion=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,18,Mathf.Abs(voice.traffic.speed)));
                    }
                }
                float target=motion*clock.Wetness*.19f*vehicles*voice.exposed;
                if(voice.traffic)target*=.60f*(director?director.OutsideDuck:1);
                bool starting=target>.0005f&&!voice.source.isPlaying;
                SetBed(voice.source,target,fade);voice.source.pitch=1;
                if(starting&&voice.source.isPlaying&&wetTyres)voice.source.time=voice.phase*wetTyres.length;
                voice.filter.cutoffFrequency=voice.car&&Cabin?3800:10000;
                WetTyreGain+=voice.source.volume;
                if(voice.source.volume>.005f)WetTyreVoices++;
            }
        }
        void Probe()
        {
            if(!listener)return;
            Cabin=player&&player.driving&&player.nearbyCar;
            Vector3 anchor=Cabin?player.nearbyCar.transform.position+Vector3.up*2:listener.position;
            shelterTarget=0;metalTarget=0;roofPoint=anchor;
            var offsets=new[]{Vector3.zero,Vector3.left*.85f,Vector3.right*.85f,Vector3.forward*.85f,Vector3.back*.85f};
            foreach(var offset in offsets)
                if(Physics.Raycast(anchor+offset,Vector3.up,out var hit,25,1,QueryTriggerInteraction.Ignore))
                {
                    shelterTarget+=.2f;roofPoint=hit.point-Vector3.up*.15f;
                    string label=hit.collider.name.ToLowerInvariant();
                    var renderer=hit.collider.GetComponent<Renderer>();
                    if(renderer&&renderer.sharedMaterial)label+=" "+renderer.sharedMaterial.name.ToLowerInvariant();
                    if(label.Contains("metal")||label.Contains("corrugat")||label.Contains("steel sheet"))metalTarget+=.2f;
                }
            leavesTarget=0;
            var bank=director?director.library:null;
            if(bank&&bank.foliage!=null)
                foreach(var volume in bank.foliage)
                {
                    var delta=anchor-volume.center;
                    float horizontal=new Vector2(delta.x,delta.z).magnitude-Mathf.Max(volume.radii.x,volume.radii.z);
                    if(Mathf.Abs(delta.y)<5)leavesTarget=Mathf.Max(leavesTarget,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,5,horizontal)));
                }
            foreach(var voice in tyres)
            {
                if(!voice.body||Vector3.Distance(listener.position,voice.body.position)>28){voice.exposed=0;continue;}
                bool cover=Physics.Raycast(voice.body.position+Vector3.up*2,Vector3.up,20,1,QueryTriggerInteraction.Ignore);
                bool hard=Physics.Raycast(voice.body.position+Vector3.up*.4f,Vector3.down,out var ground,2,1,QueryTriggerInteraction.Ignore)
                    &&GameAudioLibrary.SurfaceAt(ground)==GroundSound.Concrete;
                voice.exposed=hard&&!cover?1:0;
            }
        }
        static void SetBed(AudioSource source,float target,float fade)
        {
            if(!source||!source.clip||!source.isActiveAndEnabled)return;
            source.volume=Mathf.Lerp(source.volume,Mathf.Max(0,target),fade);
            // All loops share ordinary real-time playback. Only gain responds
            // to accelerated game time; no speed/pitch changes or rapid starts.
            if(source.volume>.0005f&&!source.isPlaying){source.UnPause();if(!source.isPlaying)source.Play();}
            if(source.volume<.0001f&&target<.0001f&&source.isPlaying)source.Pause();
        }
        float Random01()=>(float)random.NextDouble();
        void OnDisable()
        {
            foreach(var voice in tyres)if(voice.source)voice.source.Stop();
            foreach(var voice in new[]{roadVoice,roofVoice,leavesVoice,windVoice,stormVoice,dripVoice})if(voice)voice.Stop();
        }
        void OnDestroy()
        {
            foreach(var voice in tyres)if(voice.source)Destroy(voice.source.gameObject);
            foreach(var voice in new[]{roadVoice,roofVoice,leavesVoice,windVoice,stormVoice,dripVoice})if(voice)Destroy(voice.gameObject);
        }
    }
}
