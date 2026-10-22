using System;
using UnityEngine;

namespace ZeroRise
{
    // Sound and acoustic transitions only. It observes the existing player and
    // camera without changing locomotion, traffic, navigation or game time.
    [DefaultExecutionOrder(250)]
    public sealed class WestbruchSoundscape : MonoBehaviour
    {
        public AudioSource city,wind,shop,drain,fence,footsteps;
        public AudioClip drainClip,fenceClip;
        public AudioClip[] pavingSteps,soilSteps;
        public AudioLowPassFilter cityMuffle,windMuffle;
        public AudioReverbFilter footReflections;
        public float BridgeBlend { get; private set; }
        public int FootstepCount { get; private set; }
        public int LocalEventCount { get; private set; }
        public float AmbienceGain { get; private set; }
        public float FootstepGain { get; private set; }
        public float MixCityGain=>city?city.volume:0;
        public float MixWindGain=>wind?wind.volume:0;
        ThirdPersonMotor player;Transform listener;
        System.Random random=new System.Random(221022);
        float nextDrain,nextFence,lastPhase,stepCooldown;
        bool initialized,settingsDirty;float saveAt;
        static WestbruchSoundscape active;
        static Rect ControlsRect=>new Rect(Screen.width-246,28,226,154);
        public static bool PointerOverControls=>active&&Cursor.visible&&ControlsRect.Contains(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));
        void OnEnable()=>active=this;
        void OnDisable(){if(active==this)active=null;}

        public static float BridgeWeight(Vector3 p)
        {
            // Actual concrete deck footprint; leave the raised railway dry.
            var bounds=new Bounds(new Vector3(118,2.45f,80),new Vector3(8.4f,5.3f,31));
            float outside=Vector3.Distance(p,bounds.ClosestPoint(p));
            float horizontal=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/2.8f));
            return horizontal*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.9f,6.0f,p.y)));
        }
        void Start()
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();var audioListener=FindAnyObjectByType<AudioListener>();listener=audioListener?audioListener.transform:null;
            AmbienceGain=Mathf.Clamp01(PlayerPrefs.GetFloat("ZeroRise.Sound.Ambience",.70f));
            FootstepGain=Mathf.Clamp01(PlayerPrefs.GetFloat("ZeroRise.Sound.Steps",.75f));
            foreach(var source in new[]{city,wind,shop})if(source&&source.clip){source.volume=0;source.time=(source==shop?3.8f:source==wind?11.3f:0)%source.clip.length;source.Play();}
            nextDrain=Time.time+12;nextFence=Time.time+28;initialized=true;
        }
        void Update()
        {
            if(!initialized||!listener)return;
            float dt=Time.unscaledDeltaTime,fade=1-Mathf.Exp(-dt/1.1f);
            BridgeBlend=Mathf.Lerp(BridgeBlend,BridgeWeight(listener.position),fade);
            if(city)city.volume=Mathf.Lerp(city.volume,.18f*AmbienceGain*Mathf.Lerp(1,.56f,BridgeBlend),fade);
            if(wind)wind.volume=Mathf.Lerp(wind.volume,.105f*AmbienceGain*Mathf.Lerp(1,.22f,BridgeBlend),fade);
            if(shop)shop.volume=Mathf.Lerp(shop.volume,.16f*AmbienceGain,fade);
            if(cityMuffle)cityMuffle.cutoffFrequency=Mathf.Lerp(6500,1250,BridgeBlend);
            if(windMuffle)windMuffle.cutoffFrequency=Mathf.Lerp(8200,1550,BridgeBlend);
            if(footReflections){footReflections.enabled=BridgeBlend>.005f;footReflections.reverbLevel=Mathf.Lerp(-10000,-1750,BridgeBlend);}
            if(Time.time>=nextDrain){nextDrain=Time.time+11+(float)random.NextDouble()*13;if(Vector3.Distance(listener.position,drain.transform.position)<21)PlayDrain();}
            if(Time.time>=nextFence){nextFence=Time.time+32+(float)random.NextDouble()*29;if(Vector3.Distance(listener.position,fence.transform.position)<13)PlayFence();}
            if(settingsDirty&&Time.unscaledTime>=saveAt)SaveSettings();
        }
        void LateUpdate()
        {
            if(!initialized||!player||!player.avatar)return;
            float phase=player.avatar.GaitPhase;bool contact=(lastPhase<.5f&&phase>=.5f)||lastPhase>phase+.1f;lastPhase=phase;
            if(!contact||Time.time<stepCooldown||player.driving||player.Interacting||!player.IsGrounded||player.avatar.ActualSpeed<.35f)return;
            stepCooldown=Time.time+.13f;
            var p=player.transform.position;bool soil=false;
            if(Physics.Raycast(p+Vector3.up*.35f,Vector3.down,out var hit,.9f,1,QueryTriggerInteraction.Ignore))
            {
                string name=hit.collider.name.ToLowerInvariant();soil=hit.collider is TerrainCollider||name.Contains("earth")||name.Contains("soil")||name.Contains("worn earth");p=hit.point;
            }
            EmitFootstep(p,soil);
        }
        public void EmitFootstep(Vector3 point,bool soil)
        {
            var clips=soil?soilSteps:pavingSteps;if(!footsteps||clips==null||clips.Length==0)return;
            footsteps.transform.position=point+Vector3.up*.04f;footsteps.pitch=.96f+(float)random.NextDouble()*.08f;
            footsteps.PlayOneShot(clips[random.Next(clips.Length)],.34f*FootstepGain);FootstepCount++;
        }
        public void PlayDrain()
        {
            if(!drain||!drainClip)return;drain.pitch=.94f+(float)random.NextDouble()*.1f;drain.PlayOneShot(drainClip,.18f*AmbienceGain);LocalEventCount++;
        }
        public void PlayFence()
        {
            if(!fence||!fenceClip)return;fence.pitch=.96f+(float)random.NextDouble()*.08f;fence.PlayOneShot(fenceClip,.10f*AmbienceGain);LocalEventCount++;
        }
        void OnGUI()
        {
            if(!initialized||!Cursor.visible)return;
            // Esc already releases the game's cursor. The compact panel adds
            // audio control without pausing or taking ownership of gameplay.
            GUILayout.BeginArea(ControlsRect,GUI.skin.box);
            GUILayout.Label("TON · WESTBRUCH");GUILayout.Label("Umgebung");
            float ambience=GUILayout.HorizontalSlider(AmbienceGain,0,1);GUILayout.Label("Schritte");float steps=GUILayout.HorizontalSlider(FootstepGain,0,1);
            if(Mathf.Abs(ambience-AmbienceGain)>.001f||Mathf.Abs(steps-FootstepGain)>.001f){AmbienceGain=ambience;FootstepGain=steps;settingsDirty=true;saveAt=Time.unscaledTime+.6f;}
            GUILayout.EndArea();
        }
        void SaveSettings(){if(!settingsDirty)return;PlayerPrefs.SetFloat("ZeroRise.Sound.Ambience",AmbienceGain);PlayerPrefs.SetFloat("ZeroRise.Sound.Steps",FootstepGain);PlayerPrefs.Save();settingsDirty=false;}
        void OnApplicationQuit()=>SaveSettings();
    }
}
