using UnityEngine;

namespace ZeroRise
{
    // Four nearby traffic vehicles are voiced at once. Sources follow the real
    // moving cars, including waiting at junctions; no canned phantom pass-bys.
    [DefaultExecutionOrder(320)]
    public sealed class TrafficSoundFx : MonoBehaviour
    {
        public bool audible;
        public AudioSource engine,rolling;
        public int AudibleFrames { get; private set; }
        public float ListenerDistance { get; private set; }
        public float EstimatedListenerGain { get; private set; }
        GameplayAudioDirector director;TrafficVehicle car;
        AudioLowPassFilter engineFilter,roadFilter;
        AudioReverbFilter engineReflections,roadReflections;
        float fade,pitch,occlusion=1,targetOcclusion=1,targetCutoff=12000,probeAt;
        float enginePhase,roadPhase;
        static readonly AnimationCurve DistanceCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.08f,.86f),
            new Keyframe(.20f,.38f),new Keyframe(.45f,.06f),new Keyframe(.72f,.008f),new Keyframe(1,0));
        public void Initialize(GameplayAudioDirector owner,TrafficVehicle vehicle,int index)
        {
            director=owner;car=vehicle;pitch=(vehicle.kind==0?.98f:.88f)+(index%5)*.01f;
            engine=GameplayAudioDirector.Source(transform,"Actual traffic engine",new Vector3(0,.8f,1),owner.library.engineIdle,true,2.2f,34,115);
            rolling=GameplayAudioDirector.Source(transform,"Actual traffic tyres",new Vector3(0,.25f,0),owner.library.roadTyres,true,2.2f,29,125);
            engine.SetCustomCurve(AudioSourceCurveType.CustomRolloff,DistanceCurve);rolling.SetCustomCurve(AudioSourceCurveType.CustomRolloff,DistanceCurve);
            // Traffic uses kinematic MovePosition. Doppler estimated from its
            // stepped transform compounded the pitched idle into a siren-like
            // pass-by. Actual distance/panning already follows every vehicle.
            engine.dopplerLevel=rolling.dopplerLevel=0;
            enginePhase=Mathf.Repeat(.137f+index*.373f,1);roadPhase=Mathf.Repeat(.417f+index*.271f,1);
            engine.pitch=pitch;rolling.pitch=1;probeAt=Time.time+(index%7)*.025f;
            engineFilter=engine.gameObject.AddComponent<AudioLowPassFilter>();roadFilter=rolling.gameObject.AddComponent<AudioLowPassFilter>();
            engineReflections=GameplayAudioDirector.Reflections(engine);roadReflections=GameplayAudioDirector.Reflections(rolling);
        }
        void Update()
        {
            if(!director||!car)return;
            ListenerDistance=director.listener?Vector3.Distance(transform.position,director.listener.position):float.PositiveInfinity;
            float dt=Time.unscaledDeltaTime;fade=Mathf.Lerp(fade,audible&&ListenerDistance<34?1:0,1-Mathf.Exp(-dt*4));
            if(fade<.0004f){VehicleSoundFx.SetLoop(engine,0);VehicleSoundFx.SetLoop(rolling,0);EstimatedListenerGain=0;return;}
            float speed=Mathf.Abs(car.speed),motion=Mathf.Clamp01(speed/15);
            float smooth=1-Mathf.Exp(-dt*7);
            occlusion=Mathf.Lerp(occlusion,targetOcclusion,smooth);
            engineFilter.cutoffFrequency=roadFilter.cutoffFrequency=Mathf.Lerp(engineFilter.cutoffFrequency,targetCutoff,smooth);
            float bridge=Mathf.Min(WestbruchSoundscape.BridgeWeight(transform.position+Vector3.up),director.mix.BridgeBlend);
            engineReflections.reverbLevel=roadReflections.reverbLevel=Mathf.Lerp(-10000,-2500,bridge);
            // Rolling noise carries a road car's pass-by; idle is a quiet
            // mechanical bed, never transposed up almost an octave with speed.
            engine.pitch=Mathf.Lerp(engine.pitch,pitch*Mathf.Lerp(.98f,1.13f,motion),smooth);
            rolling.pitch=Mathf.Lerp(.96f,1.07f,motion);
            float gain=fade*director.VehicleLevel*director.OutsideDuck*occlusion;
            SetVoice(engine,gain*Mathf.Lerp(.105f,.16f,motion),enginePhase);
            SetVoice(rolling,gain*Mathf.SmoothStep(0,.17f,Mathf.InverseLerp(.8f,18,speed)),roadPhase);
            EstimatedListenerGain=engine.volume*DistanceCurve.Evaluate(Mathf.Clamp01(ListenerDistance/engine.maxDistance))
                +rolling.volume*DistanceCurve.Evaluate(Mathf.Clamp01(ListenerDistance/rolling.maxDistance));
            if(EstimatedListenerGain>.003f)AudibleFrames++;
            if(Time.time>=probeAt)
            {
                probeAt=Time.time+.24f;
                bool blocked=director.listener&&Physics.Linecast(transform.position+Vector3.up*1.2f,director.listener.position,out _,1,QueryTriggerInteraction.Ignore);
                targetOcclusion=blocked?.48f:1;
                targetCutoff=blocked?1450:Mathf.Lerp(12000,4500,Mathf.Clamp01(ListenerDistance/34));
            }
        }
        static void SetVoice(AudioSource source,float gain,float phase)
        {
            bool starting=gain>.0008f&&!source.isPlaying;
            VehicleSoundFx.SetLoop(source,gain);
            // Identical source recordings must not restart phase-locked when
            // several cars enter the nearest-four budget on the same frame.
            if(starting&&source.isPlaying&&source.clip)source.time=source.clip.length*phase;
        }
        void OnDisable(){if(engine)engine.Stop();if(rolling)rolling.Stop();EstimatedListenerGain=0;}
    }
}
