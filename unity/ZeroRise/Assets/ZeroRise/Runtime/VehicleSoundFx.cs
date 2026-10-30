using UnityEngine;

namespace ZeroRise
{
    // Audio-only powertrain model. Gear ratios/pitch never feed the vehicle's
    // WheelColliders, torque, braking, steering or occupancy state.
    [DefaultExecutionOrder(320)]
    public sealed class VehicleSoundFx : MonoBehaviour
    {
        public int Starts { get; private set; }
        public int Stops { get; private set; }
        public int Opens { get; private set; }
        public int Closes { get; private set; }
        public int Shifts { get; private set; }
        public int Horns { get; private set; }
        public int Impacts { get; private set; }
        public int RollingFrames { get; private set; }
        public int SlipFrames { get; private set; }
        public int LateralSlipFrames { get; private set; }
        public int GroundedWheels { get; private set; }
        public float PlanarSpeed { get; private set; }
        public float LateralSpeed { get; private set; }
        public float SlipAngleDegrees { get; private set; }
        public float WheelSlipAngleDegrees { get; private set; }
        public float WheelSidewaysSlip { get; private set; }
        public float WheelForwardSlip { get; private set; }
        public float LateralScrub { get; private set; }
        public float LongitudinalScrub { get; private set; }
        public float PeakLateralSpeed { get; private set; }
        public float PeakSlipAngleDegrees { get; private set; }
        public float PeakLateralScrub { get; private set; }
        public float Rpm { get; private set; }=850;
        public float HighestRpm { get; private set; }=850;
        public float EngineGain=>idle?idle.volume+load.volume:0;
        public float RollingGain=>road?road.volume:0;
        public float SlipGain=>slip?slip.volume:0;
        public AudioSource idle,load,road,slip,mechanical;
        GameplayAudioDirector director;
        SimpleCar car;Rigidbody body;
        AudioSource[] doorSounds;
        AudioLowPassFilter[] filters;
        AudioReverbFilter[] reflections;
        readonly bool[] openDoors=new bool[2];
        bool running;
        int gear=1;
        float startedAt,shiftUntil,impactAt,filterAt,occlusion=1,targetOcclusion=1,cutoff=18000;
        float voicePitch=1;

        public void Initialize(GameplayAudioDirector owner,SimpleCar vehicle,int index)
        {
            director=owner;car=vehicle;body=GetComponent<Rigidbody>();var bank=director.library;
            voicePitch=.985f+(index%4)*.01f;
            idle=GameplayAudioDirector.Source(transform,"Recorded engine idle",new Vector3(0,.65f,1),bank.engineIdle,true,4.5f,38,45);
            load=GameplayAudioDirector.Source(transform,"Recorded engine under load",new Vector3(0,.65f,1),bank.engineLoad,true,4.5f,42,45);
            road=GameplayAudioDirector.Source(transform,"Tyres on actual road",new Vector3(0,.22f,0),bank.roadTyres,true,4,32,65);
            slip=GameplayAudioDirector.Source(transform,"Wheel contact slip",new Vector3(0,.22f,0),bank.tyreSlip,true,4,36,60);
            mechanical=GameplayAudioDirector.Source(transform,"Ignition and cabin mechanisms",new Vector3(-.3f,.8f,.4f),null,false,4,38,40);
            mechanical.volume=1;
            doorSounds=new AudioSource[2];
            for(int i=0;i<2;i++)
            {
                var hinge=car.doors!=null&&car.doors.Length>i?car.doors[i]:transform;
                doorSounds[i]=GameplayAudioDirector.Source(hinge,"Actual door latch and slam",Vector3.zero,null,false,3.8f,32,35);doorSounds[i].volume=1;
            }
            filters=new AudioLowPassFilter[7];reflections=new AudioReverbFilter[7];
            var sources=new[]{idle,load,road,slip,mechanical,doorSounds[0],doorSounds[1]};
            for(int i=0;i<sources.Length;i++){filters[i]=sources[i].gameObject.AddComponent<AudioLowPassFilter>();reflections[i]=GameplayAudioDirector.Reflections(sources[i]);}
        }
        void Update()
        {
            if(!director||!body||!car)return;
            float dt=Time.unscaledDeltaTime,fade=1-Mathf.Exp(-dt*8),speed=PlanarSpeed;
            occlusion=Mathf.Lerp(occlusion,targetOcclusion,fade);
            bool shouldRun=car.State==SimpleCar.Occupancy.Driving;
            if(shouldRun&&!running)
            {
                running=true;startedAt=Time.time;gear=1;Starts++;
                mechanical.pitch=voicePitch;mechanical.PlayOneShot(director.library.engineStart,.85f);
            }
            if(!shouldRun&&running)
            {
                running=false;Stops++;mechanical.pitch=voicePitch;mechanical.PlayOneShot(director.library.engineStop,.52f);
            }
            if(shouldRun&&Input.GetKeyDown(KeyCode.H))PlayHorn();
            for(int i=0;i<2;i++)
            {
                if(car.doors==null||car.doors.Length<=i||!car.doors[i])continue;
                float angle=Mathf.Abs(Mathf.DeltaAngle(0,car.doors[i].localEulerAngles.y));
                if(!openDoors[i]&&angle>=2){openDoors[i]=true;doorSounds[i].pitch=director.Pitch(.02f);doorSounds[i].PlayOneShot(director.library.doorOpen,.70f);Opens++;}
                if(openDoors[i]&&angle<=.8f){openDoors[i]=false;doorSounds[i].pitch=director.Pitch(.02f);doorSounds[i].PlayOneShot(director.library.doorClose,.84f);Closes++;}
            }
            float throttle=Mathf.Abs(car.AudioThrottle);
            int wanted=gear;
            if(car.AudioThrottle<-.1f&&Vector3.Dot(body.linearVelocity,transform.forward)<-.2f)wanted=0;
            else
            {
                if(gear<=0)wanted=1;
                if(speed>6.8f&&wanted==1)wanted=2;else if(speed>13.0f&&wanted==2)wanted=3;else if(speed>20&&wanted==3)wanted=4;
                else if(speed<5.4f&&wanted>1)wanted=1;else if(speed<11.3f&&wanted>2)wanted=2;else if(speed<18.3f&&wanted>3)wanted=3;
            }
            if(running&&wanted!=gear&&Time.time>=shiftUntil)
            {
                gear=wanted;shiftUntil=Time.time+.22f;
                if(speed>2){mechanical.PlayOneShot(director.library.cabinClick,.09f);Shifts++;}
            }
            float ratio=gear<=1?270:gear==2?168:gear==3?113:86;
            float targetRpm=running?850+speed*ratio+throttle*(speed<1?700:300):850;
            Rpm=Mathf.Lerp(Rpm,Mathf.Clamp(targetRpm,800,3800),1-Mathf.Exp(-dt*(targetRpm>Rpm?5:3)));
            if(running)HighestRpm=Mathf.Max(HighestRpm,Rpm);
            float shift=Time.time<shiftUntil?.60f:1;
            float opening=Mathf.Clamp01((Time.time-startedAt-.42f)/.65f);
            float drive=running?opening:0,blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1150,2150,Rpm));
            float amount=director.VehicleLevel*occlusion;
            // Both clips now share one stationary engine's reference speed;
            // load is its warmer timbre, not an unrelated high-RPM recording.
            // One modest pitch envelope prevents two detuned engines during
            // the crossfade without inventing a fundamental reference RPM.
            idle.pitch=load.pitch=voicePitch*Mathf.Lerp(.98f,1.22f,Mathf.InverseLerp(850,3800,Rpm));
            SetLoop(idle,Mathf.Lerp(idle.volume,drive*amount*(1-blend)*.30f,fade));
            SetLoop(load,Mathf.Lerp(load.volume,drive*amount*blend*(.25f+throttle*.11f)*shift,fade));
            float rolling=GroundedWheels>0?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,22,speed)):0;
            road.pitch=Mathf.Lerp(.94f,1.09f,rolling);
            SetLoop(road,Mathf.Lerp(road.volume,rolling*.27f*amount,fade));if(road.volume>.015f)RollingFrames++;
            float scrub=Mathf.Max(LateralScrub,LongitudinalScrub*.85f);
            // A short release joins consecutive physics contacts, while no
            // contact or a stopped vehicle immediately silences the tyre loop.
            float slipGain=GroundedWheels>0&&speed>1.5f?Mathf.Lerp(slip.volume,scrub*.30f*amount,1-Mathf.Exp(-dt*(scrub>.05f?17:9))):0;
            slip.pitch=Mathf.Lerp(.96f,1.06f,Mathf.Clamp01(speed/22));
            SetLoop(slip,slipGain);if(slip.volume>.025f)SlipFrames++;
            if(LateralScrub>.08f&&slip.volume>.015f)LateralSlipFrames++;
            if(Time.time>=filterAt)
            {
                filterAt=Time.time+.18f;
                bool blocked=director.listener&&Physics.Linecast(transform.position+Vector3.up*1.2f,director.listener.position,out _,1,QueryTriggerInteraction.Ignore);
                targetOcclusion=blocked?.62f:1;cutoff=blocked?1700:running?10500:18000;
            }
            float bridge=Mathf.Min(WestbruchSoundscape.BridgeWeight(transform.position+Vector3.up),director.mix.BridgeBlend);
            for(int i=0;i<filters.Length;i++){filters[i].cutoffFrequency=Mathf.Lerp(filters[i].cutoffFrequency,cutoff,fade);reflections[i].reverbLevel=Mathf.Lerp(-10000,-2200,bridge);}
            mechanical.volume=director.VehicleLevel*occlusion;
            foreach(var door in doorSounds)door.volume=director.VehicleLevel*occlusion;
        }
        void FixedUpdate()
        {
            if(!body||!car)return;
            var velocity=Vector3.ProjectOnPlane(body.linearVelocity,transform.up);
            PlanarSpeed=velocity.magnitude;
            LateralSpeed=Mathf.Abs(Vector3.Dot(velocity,transform.right));
            float forwardSpeed=Mathf.Abs(Vector3.Dot(velocity,transform.forward));
            SlipAngleDegrees=Mathf.Atan2(LateralSpeed,Mathf.Max(.5f,forwardSpeed))*Mathf.Rad2Deg;
            GroundedWheels=0;WheelSidewaysSlip=WheelForwardSlip=WheelSlipAngleDegrees=0;
            float contactLateral=0,normalisedSideSlip=0;
            if(car.wheels!=null)foreach(var wheel in car.wheels)
            {
                if(!wheel||!wheel.GetGroundHit(out var hit))continue;
                GroundedWheels++;
                var contactVelocity=Vector3.ProjectOnPlane(body.GetPointVelocity(hit.point),hit.normal);
                float lateral=Mathf.Abs(Vector3.Dot(contactVelocity,hit.sidewaysDir));
                float longitudinal=Mathf.Abs(Vector3.Dot(contactVelocity,hit.forwardDir));
                float angle=Mathf.Atan2(lateral,Mathf.Max(.5f,longitudinal))*Mathf.Rad2Deg;
                contactLateral=Mathf.Max(contactLateral,lateral);
                WheelSlipAngleDegrees=Mathf.Max(WheelSlipAngleDegrees,angle);
                WheelSidewaysSlip=Mathf.Max(WheelSidewaysSlip,Mathf.Abs(hit.sidewaysSlip));
                WheelForwardSlip=Mathf.Max(WheelForwardSlip,Mathf.Abs(hit.forwardSlip));
                normalisedSideSlip=Mathf.Max(normalisedSideSlip,Mathf.Abs(hit.sidewaysSlip)/Mathf.Max(.05f,wheel.sidewaysFriction.extremumSlip));
            }
            // Chassis sideslip catches a real slide even if the wheel model
            // reports a small sidewaysSlip value. The contact angle also hears
            // tyre scrub during a hard steering arc before the whole car drifts.
            LateralScrub=Mathf.Max(EvaluateLateralScrub(PlanarSpeed,LateralSpeed,SlipAngleDegrees,0,GroundedWheels),
                EvaluateLateralScrub(PlanarSpeed,contactLateral,WheelSlipAngleDegrees,normalisedSideSlip,GroundedWheels));
            LongitudinalScrub=GroundedWheels>0&&PlanarSpeed>2.5f&&(car.AudioBraking||Mathf.Abs(car.AudioThrottle)>.15f)
                ?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,1.65f,WheelForwardSlip))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,6,PlanarSpeed)):0;
            if(GroundedWheels>0)
            {
                PeakLateralSpeed=Mathf.Max(PeakLateralSpeed,LateralSpeed);
                PeakSlipAngleDegrees=Mathf.Max(PeakSlipAngleDegrees,SlipAngleDegrees);
                PeakLateralScrub=Mathf.Max(PeakLateralScrub,LateralScrub);
            }
        }
        // Pure, read-only counterpart of the live contact model for focused QA.
        // Speed is m/s, angle degrees, and wheelSlip is relative to peak grip.
        public static float EvaluateLateralScrub(float speed,float lateralSpeed,float angle,float wheelSlip,int contacts)
        {
            if(contacts<=0||speed<=2.5f||lateralSpeed<=.55f)return 0;
            float motion=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,7,speed));
            float sideways=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,2.3f,lateralSpeed));
            float angleScrub=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,18,angle));
            float gripScrub=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1.8f,wheelSlip))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,7,angle));
            return motion*sideways*Mathf.Max(angleScrub,gripScrub*.85f);
        }
        public void PlayHorn()
        {
            if(!car||car.State!=SimpleCar.Occupancy.Driving)return;
            mechanical.PlayOneShot(director.library.horn,.65f);Horns++;
        }
        void OnCollisionEnter(Collision collision)
        {
            if(!director||Time.time<impactAt||collision.relativeVelocity.magnitude<1.7f)return;
            var contact=collision.GetContact(0);float speed=collision.relativeVelocity.magnitude;
            if(contact.normal.y>.65f&&speed<3.4f)return;
            impactAt=Time.time+.25f;
            director.World(director.library.bodyImpact,contact.point,.24f+Mathf.Clamp01(speed/10)*.45f);
            director.World(director.library.seatImpact,contact.point,.16f);Impacts++;
        }
        internal static void SetLoop(AudioSource source,float gain)
        {
            source.volume=Mathf.Clamp01(gain);
            if(gain>.0008f&&!source.isPlaying)source.Play();
            else if(gain<.0004f&&source.isPlaying)source.Stop();
        }
        void OnDisable()
        {
            foreach(var source in new[]{idle,load,road,slip,mechanical})if(source)source.Stop();
            if(doorSounds!=null)foreach(var source in doorSounds)if(source)source.Stop();
        }
    }
}
