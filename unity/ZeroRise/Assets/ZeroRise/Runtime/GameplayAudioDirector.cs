using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    // Observes real gameplay; audio never moves actors or changes decisions.
    [DefaultExecutionOrder(300)]
    public sealed class GameplayAudioDirector : MonoBehaviour
    {
        public static GameplayAudioDirector Active { get; private set; }
        public GameAudioLibrary library;
        public WestbruchSoundscape mix;
        public Transform listener;
        public int PlayerSteps { get; private set; }
        public int NpcSteps { get; private set; }
        public int Jumps { get; private set; }
        public int Landings { get; private set; }
        public int BirdCalls { get; private set; }
        public int OccludedVoices { get; private set; }
        public int BushRustles { get; private set; }
        public int ClothingCues { get; private set; }
        public int FoliageVolumeCount=>library&&library.foliage!=null?library.foliage.Length:0;
        public int LastFoliageIndex { get; private set; }=-1;
        public GroundSound LastSurface { get; private set; }
        public string LastFootstepClip { get; private set; }
        public int[] FootstepSurfaceCounts { get; }=new int[6];
        public float VehicleLevel=>mix?mix.VehicleGain:.85f;
        public float EffectsLevel=>mix?mix.EffectsGain:.85f;
        public float StepsLevel=>mix?mix.FootstepGain:.75f;
        public float AmbientLevel=>mix?mix.AmbienceGain:.70f;
        public float OutsideDuck { get; private set; }=1;
        readonly System.Random random=new System.Random(221024);
        ThirdPersonMotor player;
        VehicleSoundFx[] vehicles;
        TrafficSoundFx[] traffic;
        PedestrianAgent[] people;
        float[] npcPhases,npcCooldown;
        PedestrianAgent.Behaviour[] npcStates;
        readonly Dictionary<AudioClip[],int> lastClips=new Dictionary<AudioClip[],int>();
        AudioClip lastPlayerStep;
        Voice[] pool;
        float lastPhase,footCooldown,cullAt,occlusionAt,birdAt;
        Vector3 lastFoliagePosition;
        bool foliageInitialized;
        float foliageTravel,foliageCooldown,foliageTravelTarget=.08f;
        CharacterController body;
        readonly Vector3[] birdPlaces={new Vector3(-39,4.1f,42),new Vector3(-38,4.1f,-43),new Vector3(82,4.2f,54),new Vector3(98,3.8f,56)};

        sealed class Voice
        {
            public AudioSource source;
            public AudioLowPassFilter filter;
            public AudioReverbFilter reflections;
            public float amount,occlusion=1,cutoff=18000;
            public bool ambient,step;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Active)return;
            var sound=FindAnyObjectByType<WestbruchSoundscape>();
            if(!sound||!sound.gameAudio)return;
            var director=new GameObject("ZERO RISE gameplay sound FX").AddComponent<GameplayAudioDirector>();
            director.library=sound.gameAudio;director.mix=sound;
        }
        void Awake()=>Active=this;
        void OnDestroy(){if(player){player.Jumped-=Jumped;player.Landed-=Landed;}if(Active==this)Active=null;}
        void Start()
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();
            var ears=FindAnyObjectByType<AudioListener>();listener=ears?ears.transform:null;
            if(!player||!listener||!library){enabled=false;return;}
            player.Jumped+=Jumped;player.Landed+=Landed;
            body=player.GetComponent<CharacterController>();
            if(!ears.GetComponent<GameAudioLimiter>())ears.gameObject.AddComponent<GameAudioLimiter>();
            var cars=FindObjectsByType<SimpleCar>();vehicles=new VehicleSoundFx[cars.Length];
            for(int i=0;i<cars.Length;i++)
            {
                var fx=cars[i].gameObject.AddComponent<VehicleSoundFx>();fx.Initialize(this,cars[i],i);vehicles[i]=fx;
            }
            var moving=FindObjectsByType<TrafficVehicle>();traffic=new TrafficSoundFx[moving.Length];
            for(int i=0;i<moving.Length;i++)
            {
                var fx=moving[i].gameObject.AddComponent<TrafficSoundFx>();fx.Initialize(this,moving[i],i);traffic[i]=fx;
            }
            people=FindObjectsByType<PedestrianAgent>();npcPhases=new float[people.Length];npcCooldown=new float[people.Length];npcStates=new PedestrianAgent.Behaviour[people.Length];
            pool=new Voice[10];
            for(int i=0;i<pool.Length;i++)
            {
                var source=Source(transform,"Pooled world foley "+i,Vector3.zero,null,false,1.8f,26,130);
                var voice=new Voice{source=source,filter=source.gameObject.AddComponent<AudioLowPassFilter>(),reflections=Reflections(source)};
                pool[i]=voice;
            }
            // Preserve the original QA-facing source, now with near-field
            // perspective appropriate to the character in a third-person game.
            mix.footsteps.spatialBlend=.35f;mix.footsteps.minDistance=4.5f;mix.footsteps.maxDistance=20;mix.footsteps.priority=50;
            birdAt=Time.time+8;
        }
        public static AudioSource Source(Transform parent,string name,Vector3 local,AudioClip clip,bool loop,float near,float far,int priority)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=local;
            var source=obj.AddComponent<AudioSource>();source.clip=clip;source.loop=loop;source.playOnAwake=false;source.volume=0;
            source.spatialBlend=1;source.minDistance=near;source.maxDistance=far;source.priority=priority;source.dopplerLevel=0;
            source.rolloffMode=AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.16f,.74f),new Keyframe(.48f,.20f),new Keyframe(1,0)));
            source.bypassReverbZones=true;return source;
        }
        public static AudioReverbFilter Reflections(AudioSource source)
        {
            var filter=source.gameObject.AddComponent<AudioReverbFilter>();filter.reverbPreset=AudioReverbPreset.User;
            filter.dryLevel=0;filter.room=-2000;filter.roomHF=-1800;filter.decayTime=.64f;filter.decayHFRatio=.55f;
            filter.reflectionsLevel=-1900;filter.reflectionsDelay=.018f;filter.reverbLevel=-10000;filter.reverbDelay=.029f;filter.diffusion=76;filter.density=84;
            return filter;
        }
        public AudioClip Variation(AudioClip[] clips,int surface=-1,AudioClip exclude=null)
        {
            if(clips==null||clips.Length==0)return null;
            int chosen;
            // Remember each actual bank, including NPC feet and shrub foley,
            // so the same take never repeats immediately within that bank.
            if(clips.Length>1&&lastClips.TryGetValue(clips,out int previous))
            {chosen=random.Next(clips.Length-1);if(chosen>=previous)chosen++;}
            else chosen=random.Next(clips.Length);
            AudioClip fallback=null;
            for(int i=0;i<clips.Length;i++)
            {
                int index=(chosen+i)%clips.Length;
                if(!clips[index])continue;
                if(clips.Length>1&&clips[index]==exclude){fallback=clips[index];continue;}
                lastClips[clips]=index;return clips[index];
            }
            return fallback;
        }
        public float Pitch(float range=.035f)=>1-range+(float)random.NextDouble()*range*2;
        public bool World(AudioClip clip,Vector3 position,float gain,bool ambient=false,bool step=false)
        {
            if(!clip||pool==null||!listener||Vector3.SqrMagnitude(listener.position-position)>32*32)return false;
            Voice voice=null;
            foreach(var candidate in pool)if(!candidate.source.isPlaying){voice=candidate;break;}
            // Quiet ambience never cuts off a close footstep or interaction.
            if(voice==null)return false;
            voice.source.transform.position=position;voice.source.pitch=Pitch();voice.source.clip=clip;
            voice.amount=gain;voice.ambient=ambient;voice.step=step;UpdateOcclusion(voice);
            voice.source.volume=gain*(ambient?AmbientLevel:step?StepsLevel:EffectsLevel)*voice.occlusion*(ambient?OutsideDuck:1);
            voice.source.Play();return true;
        }
        public void PlayerStep(Vector3 point,GroundSound surface,float strength=1)
        {
            if(!library||!mix||!mix.footsteps)return;
            var clip=Variation(library.Steps(surface),(int)surface,lastPlayerStep);if(!clip)return;
            lastPlayerStep=clip;
            mix.footsteps.transform.position=point+Vector3.up*.07f;mix.footsteps.pitch=Pitch(.025f);
            mix.footsteps.PlayOneShot(clip,Mathf.Clamp(strength,.35f,1.5f)*.48f*Pitch(.06f)*StepsLevel);
            LastSurface=surface;LastFootstepClip=clip.name;
            if((int)surface>=0&&(int)surface<FootstepSurfaceCounts.Length)FootstepSurfaceCounts[(int)surface]++;
            PlayerSteps++;mix.RegisterFootstep();
        }
        void Update()
        {
            if(pool==null||!listener)return;
            OutsideDuck=Mathf.Lerp(OutsideDuck,player&&player.driving?.67f:1,1-Mathf.Exp(-Time.unscaledDeltaTime*3));
            if(Time.time>=cullAt)
            {
                cullAt=Time.time+.30f;
                for(int i=0;i<traffic.Length;i++)
                {
                    var fx=traffic[i];
                    if(!fx||!fx.gameObject.activeInHierarchy){if(fx)fx.audible=false;continue;}
                    float distance=Vector3.SqrMagnitude(listener.position-fx.transform.position);int closer=0;
                    for(int j=0;j<traffic.Length;j++)if(j!=i&&traffic[j]&&traffic[j].gameObject.activeInHierarchy)
                    {float other=Vector3.SqrMagnitude(listener.position-traffic[j].transform.position);if(other<distance||other==distance&&j<i)closer++;}
                    fx.audible=distance<70*70&&closer<4;
                }
            }
            bool refresh=Time.time>=occlusionAt;
            if(refresh){occlusionAt=Time.time+.16f;OccludedVoices=0;}
            foreach(var voice in pool)
            {
                if(!voice.source.isPlaying)continue;
                if(refresh)UpdateOcclusion(voice);
                if(refresh&&voice.occlusion<.99f)OccludedVoices++;
                float gain=voice.ambient?AmbientLevel:voice.step?StepsLevel:EffectsLevel;
                voice.source.volume=Mathf.Lerp(voice.source.volume,voice.amount*gain*voice.occlusion*(voice.ambient?OutsideDuck:1),1-Mathf.Exp(-Time.unscaledDeltaTime*9));
                voice.filter.cutoffFrequency=Mathf.Lerp(voice.filter.cutoffFrequency,voice.cutoff,1-Mathf.Exp(-Time.unscaledDeltaTime*8));
                voice.reflections.reverbLevel=Mathf.Lerp(-10000,-2200,Mathf.Min(WestbruchSoundscape.BridgeWeight(voice.source.transform.position),mix.BridgeBlend));
            }
            NearbyPeople();
            if(Time.time>=birdAt)
            {
                birdAt=Time.time+24+(float)random.NextDouble()*34;
                var closest=birdPlaces[0];foreach(var p in birdPlaces)if((p-listener.position).sqrMagnitude<(closest-listener.position).sqrMagnitude)closest=p;
                var world=WorldWeatherClock.Instance;
                float birds=world?world.Daylight*(1-world.RainIntensity):1;
                if(birds>.30f&&(closest-listener.position).sqrMagnitude<30*30&&World(Variation(library.birds),closest,.34f*birds,true))BirdCalls++;
            }
        }
        void UpdateOcclusion(Voice voice)
        {
            bool blocked=Physics.Linecast(voice.source.transform.position+Vector3.up*.2f,listener.position,out _,1,QueryTriggerInteraction.Ignore);
            voice.occlusion=blocked?.57f:1;voice.cutoff=blocked?1800:18000;
        }
        void LateUpdate()
        {
            if(pool==null||!player||!player.avatar)return;
            var avatar=player.avatar;float phase=avatar.GaitPhase;
            bool contact=lastPhase<.5f&&phase>=.5f||lastPhase>phase+.1f;lastPhase=phase;
            bool grounded=player.IsGrounded||player.Interacting&&avatar.Grounded;
            if(player.driving){foliageInitialized=false;foliageTravel=0;foliageTravelTarget=.08f;LastFoliageIndex=-1;return;}
            var point=player.transform.position;
            FoliageMovement(point,avatar.ActualSpeed,player.Interacting);
            if(contact&&grounded&&Time.time>=footCooldown&&avatar.ActualSpeed>.35f)
            {
                footCooldown=Time.time+.12f;
                PlayerStep(point,GameAudioLibrary.SurfaceAt(point),Mathf.Lerp(.62f,1.13f,Mathf.InverseLerp(.8f,6,avatar.ActualSpeed)));
            }
        }
        void FoliageMovement(Vector3 point,float speed,bool interacting)
        {
            if(!foliageInitialized||interacting)
            {lastFoliagePosition=point;foliageInitialized=true;foliageTravel=0;foliageTravelTarget=.08f;LastFoliageIndex=-1;return;}
            var previous=lastFoliagePosition;lastFoliagePosition=point;
            float distance=Vector3.Distance(previous,point);
            // Teleports and a stationary character are silent. Accumulation
            // follows distance travelled inside leaves, never a walking timer.
            if(distance>2.5f){foliageTravel=0;foliageTravelTarget=.08f;LastFoliageIndex=-1;return;}
            if(distance<.001f||speed<.12f)return;
            if(!TouchesFoliage(previous,point,out int index,out var contact))
            {foliageTravel=0;foliageTravelTarget=.08f;LastFoliageIndex=-1;return;}
            LastFoliageIndex=index;foliageTravel+=distance;
            if(foliageTravel<foliageTravelTarget||Time.time<foliageCooldown)return;
            var clip=Variation(library.bushRustles);if(!clip)return;
            if(!World(clip,contact,Mathf.Lerp(.16f,.25f,Mathf.InverseLerp(.8f,5.8f,speed))))return;
            BushRustles++;foliageTravel=0;
            foliageTravelTarget=.65f+(float)random.NextDouble()*.35f;
            foliageCooldown=Time.time+.68f+(float)random.NextDouble()*.24f;
        }
        public bool TouchesFoliage(Vector3 from,Vector3 to,out int index,out Vector3 contact)
        {
            index=-1;contact=to;
            if(!library||library.foliage==null)return false;
            // Capsule-to-crown sweep. The padding is the actual character's
            // contact radius, not an audible proximity zone around the plant.
            float radius=body?body.radius:.27f,height=body?body.height:1.75f;
            float low=Mathf.Min(radius,.25f),high=Mathf.Max(low,height-radius);
            float nearest=float.PositiveInfinity;
            for(int i=0;i<library.foliage.Length;i++)
            {
                var volume=library.foliage[i];var r=volume.radii;
                if(r.x<=0||r.y<=0||r.z<=0)continue;
                if(Mathf.Min(from.y,to.y)+low>volume.center.y+r.y||Mathf.Max(from.y,to.y)+high<volume.center.y-r.y)continue;
                r.x+=radius;r.z+=radius;
                var a=new Vector3((from.x-volume.center.x)/r.x,(Mathf.Clamp(volume.center.y,from.y+low,from.y+high)-volume.center.y)/r.y,(from.z-volume.center.z)/r.z);
                var b=new Vector3((to.x-volume.center.x)/r.x,(Mathf.Clamp(volume.center.y,to.y+low,to.y+high)-volume.center.y)/r.y,(to.z-volume.center.z)/r.z);
                var delta=b-a;float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(-Vector3.Dot(a,delta)/delta.sqrMagnitude):0;
                float distance=(a+delta*t).sqrMagnitude;
                if(distance>=1||distance>=nearest)continue;
                nearest=distance;index=i;
                var point=Vector3.Lerp(from,to,t);
                contact=new Vector3(Mathf.Clamp(point.x,volume.center.x-volume.radii.x,volume.center.x+volume.radii.x),Mathf.Clamp(volume.center.y,point.y+low,point.y+high),Mathf.Clamp(point.z,volume.center.z-volume.radii.z,volume.center.z+volume.radii.z));
            }
            return index>=0;
        }
        void Jumped()
        {
            if(pool==null)return;
            World(Variation(library.clothing),player.transform.position+Vector3.up*.9f,.09f);ClothingCues++;Jumps++;
        }
        void Landed(float speed)
        {
            if(pool==null)return;var point=player.transform.position;
            PlayerStep(point,GameAudioLibrary.SurfaceAt(point),Mathf.Clamp(speed/5,.65f,1.4f));
            // Landing is a weighted contact with the ground, not a car-seat thump.
            Landings++;footCooldown=Time.time+.16f;
        }
        void NearbyPeople()
        {
            for(int i=0;i<people.Length;i++)
            {
                var person=people[i];if(!person||!person.isActiveAndEnabled||!person.avatar)continue;
                float phase=person.avatar.GaitPhase;bool contact=npcPhases[i]<.5f&&phase>=.5f||npcPhases[i]>phase+.1f;npcPhases[i]=phase;
                float distance=(person.transform.position-listener.position).sqrMagnitude;
                bool near=distance<14*14;
                int closer=0;
                if(near)foreach(var other in people)if(other&&other.isActiveAndEnabled&&other!=person&&(other.transform.position-listener.position).sqrMagnitude<distance)closer++;
                if(near&&closer<4)
                {
                    if(contact&&Time.time>=npcCooldown[i]&&person.avatar.ActualSpeed>.35f&&person.avatar.Grounded)
                    {
                        npcCooldown[i]=Time.time+.18f;var surface=GameAudioLibrary.SurfaceAt(person.transform.position);
                        World(Variation(library.Steps(surface)),person.transform.position+Vector3.up*.07f,.22f,false,true);NpcSteps++;
                    }
                    if(npcStates[i]!=person.state&&(person.state==PedestrianAgent.Behaviour.Arriving||person.state==PedestrianAgent.Behaviour.Leaving))
                        World(Variation(library.clothing),person.transform.position+Vector3.up*.7f,.075f);
                    if(npcStates[i]!=person.state&&person.state==PedestrianAgent.Behaviour.Activity&&person.world&&person.destination>=0&&person.destination<person.world.places.Length&&person.world.places[person.destination].activity==PedestrianWorld.Activity.Sit)
                        World(library.seatImpact,person.transform.position+Vector3.up*.45f,.08f);
                }
                npcStates[i]=person.state;
            }
        }
    }
}
