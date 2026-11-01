using System;
using UnityEngine;

namespace ZeroRise
{
    public enum WeatherKind { Clear, Cloudy, Rain, Storm, Fog }

    // The calendar advances independently of Unity's physics/audio clocks.
    // Test speed changes days and drying, while walking and driving keep their pace.
    [DefaultExecutionOrder(-350)]
    public sealed class WorldWeatherClock : MonoBehaviour
    {
        [Serializable] public sealed class WorldState
        {
            public int version=1;
            public double minutes=15.5*60, nextWeather=17*60;
            public int weather;
            public bool automatic=true;
            public uint random=572193;
            public float rain,cloud=.12f,wind=.15f,wetness,puddles,soil;
        }
        const string SaveKey="ZeroRise.World.Weather.v1";
        internal string StorageKey { get; private set; }=SaveKey;
        public static WorldWeatherClock Instance { get; private set; }
        public const float GameMinutesPerRealSecond=.5f; // One day takes 48 real minutes at 1x.
        public WorldState state=new WorldState();
        public bool SuppressSave { get; set; }
        public float SpeedMultiplier { get; private set; }=1;
        public double TotalGameMinutes=>state.minutes;
        public int DayNumber=>(int)(state.minutes/1440)+1;
        public float Hour=>(float)(state.minutes%1440)/60;
        public float Daylight=>DaylightAt(Hour);
        public float RainIntensity=>state.rain;
        public float CloudCoverage=>state.cloud;
        public float WindStrength=>state.wind;
        public float GustStrength=>.84f+.12f*Mathf.Sin(Time.unscaledTime*.173f+2.1f)+.04f*Mathf.Sin(Time.unscaledTime*.71f);
        public float Wetness=>state.wetness;
        public float PuddleAmount=>state.puddles;
        public float SoilMoisture=>state.soil;
        public WeatherKind Weather=>(WeatherKind)state.weather;
        public bool IsStorm=>Weather==WeatherKind.Storm;
        public bool Automatic=>state.automatic;
        public bool LoadedSavedWorld { get; private set; }
        float nextSave;
        public float FogBlend { get; private set; }
        GUIStyle clockStyle,smallStyle;
        public static Rect ControlsRect=>new Rect(Mathf.Max(8,Screen.width-622),28,340,456);
        public static bool PointerOverControls=>Instance&&Cursor.visible&&ControlsRect.Contains(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));

        void Awake()
        {
            if(Instance&&Instance!=this){enabled=false;return;}
            Instance=this;
            var args=Environment.GetCommandLineArgs();
            StorageKey=Array.IndexOf(args,"-weather-persistence-qa")>=0?SaveKey+".Verification":SaveKey;
            SuppressSave=Array.IndexOf(args,"-bahnhof-qa")>=0||Array.IndexOf(args,"-world-weather-qa")>=0||Array.IndexOf(args,"-world-twilight-qa")>=0||Array.IndexOf(args,"-world-weather-visual-review")>=0||Array.IndexOf(args,"-night-lighting-qa")>=0||Array.IndexOf(args,"-night-sky-qa")>=0||Array.IndexOf(args,"-sunset-transition-qa")>=0;
            if(!SuppressSave&&PlayerPrefs.HasKey(StorageKey))
            {
                try { var saved=JsonUtility.FromJson<WorldState>(PlayerPrefs.GetString(StorageKey));if(IsValid(saved)){state=saved;LoadedSavedWorld=true;} }
                catch(Exception e){Debug.LogWarning("World clock save could not be read: "+e.Message);}
            }
            if(!IsValid(state))state=new WorldState();
            nextSave=Time.unscaledTime+30;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
        void Update()
        {
            Advance(Time.unscaledDeltaTime);
            if(Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+30;Save();}
        }
        public static float DaylightAt(float hour)
        {
            float sunrise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.2f,7.4f,hour));
            float sunset=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(18.3f,20.6f,hour));
            return sunrise*sunset;
        }
        public void SetSpeed(float value)=>SpeedMultiplier=Mathf.Clamp(value,1,20);
        public void SetHour(float hour)
        {
            state.minutes=Math.Floor(state.minutes/1440)*1440+Mathf.Repeat(hour,24)*60;
            state.nextWeather=state.minutes+60+Next01()*90;
        }
        public void SetWeather(WeatherKind kind,bool automatic=false)
        {
            state.weather=Mathf.Clamp((int)kind,0,4);state.automatic=automatic;
            state.nextWeather=state.minutes+70+Next01()*110;
        }
        public void UseAutomaticWeather(){state.automatic=true;state.nextWeather=state.minutes+35+Next01()*55;}
        public void Advance(float realSeconds)
        {
            if(!float.IsFinite(realSeconds)||realSeconds<=0)return;
            // Catch up bounded stalls; suspended/background time beyond two minutes is skipped.
            float remaining=Mathf.Min(realSeconds,120);
            while(remaining>0){float dt=Mathf.Min(.25f,remaining);Integrate(dt);remaining-=dt;}
        }
        void Integrate(float dt)
        {
            float minutes=dt*GameMinutesPerRealSecond*SpeedMultiplier;state.minutes+=minutes;
            if(Automatic&&state.minutes>=state.nextWeather)ChooseWeather();
            float targetRain=Weather==WeatherKind.Rain?.58f:IsStorm?1:0;
            float targetCloud=Weather==WeatherKind.Clear?.10f:Weather==WeatherKind.Cloudy?.70f:Weather==WeatherKind.Fog?.82f:1;
            float targetWind=IsStorm?.85f:Weather==WeatherKind.Rain?.42f:Weather==WeatherKind.Cloudy?.30f:.15f;
            // Cloud cover leads rainfall; clearing leaves wet surfaces behind.
            float cloudBlend=1-Mathf.Exp(-dt/8f);
            state.cloud=Mathf.Lerp(state.cloud,targetCloud,cloudBlend);
            float rainGate=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,.85f,state.cloud));
            state.rain=Mathf.Lerp(state.rain,targetRain*rainGate,1-Mathf.Exp(-dt/5f));
            state.wind=Mathf.Lerp(state.wind,targetWind,1-Mathf.Exp(-dt/9f));
            FogBlend=Mathf.Lerp(FogBlend,Weather==WeatherKind.Fog?1:0,1-Mathf.Exp(-dt/10f));
            float evaporation=Mathf.Lerp(.38f,1,Daylight)*Mathf.Lerp(.85f,1.18f,WindStrength);
            float rain=RainIntensity;
            state.wetness=Mathf.Clamp01(state.wetness+minutes*(rain/24-(1-rain)*evaporation/95));
            // Puddles need sustained rainfall/saturation; drainage continues in shade.
            state.puddles=Mathf.Clamp01(state.puddles+minutes*(rain*rain*Mathf.InverseLerp(.15f,.7f,Wetness)/42-(1-rain)*(.30f+.70f*evaporation)/155));
            state.soil=Mathf.Clamp01(state.soil+minutes*(rain/58-(1-rain)*evaporation/240));
        }
        void ChooseWeather()
        {
            float choice=Next01();WeatherKind next;
            switch(Weather)
            {
                case WeatherKind.Clear: next=choice<.76f?WeatherKind.Cloudy:WeatherKind.Fog;break;
                case WeatherKind.Cloudy: next=choice<.44f?WeatherKind.Rain:choice<.86f?WeatherKind.Clear:WeatherKind.Fog;break;
                case WeatherKind.Rain: next=choice<.22f?WeatherKind.Storm:WeatherKind.Cloudy;break;
                case WeatherKind.Storm: next=WeatherKind.Rain;break;
                default:next=WeatherKind.Cloudy;break;
            }
            SetWeather(next,true);
        }
        float Next01(){uint x=state.random==0?572193:state.random;x^=x<<13;x^=x>>17;x^=x<<5;state.random=x;return (x&0xFFFFFF)/16777216f;}
        public string ExportState()=>JsonUtility.ToJson(state);
        public bool ImportState(string json)
        {
            try { var candidate=JsonUtility.FromJson<WorldState>(json);if(!IsValid(candidate))return false;state=candidate;return true; }
            catch{return false;}
        }
        public static bool IsValid(WorldState value)
        {
            if(value==null||value.version!=1||double.IsNaN(value.minutes)||double.IsInfinity(value.minutes)||value.minutes<0||value.minutes>5e8||double.IsNaN(value.nextWeather)||double.IsInfinity(value.nextWeather)||value.weather<0||value.weather>4)return false;
            foreach(float f in new[]{value.rain,value.cloud,value.wind,value.wetness,value.puddles,value.soil})if(!float.IsFinite(f)||f<0||f>1)return false;
            return true;
        }
        public void Save()
        {
            if(SuppressSave||!Application.isPlaying)return;
            PlayerPrefs.SetString(StorageKey,ExportState());PlayerPrefs.Save();
        }
        void OnApplicationQuit()=>Save();
        void OnApplicationPause(bool paused){if(paused)Save();}
        public static string WeatherName(WeatherKind kind)=>kind==WeatherKind.Clear?"Heiter":kind==WeatherKind.Cloudy?"Bewölkt":kind==WeatherKind.Rain?"Regen":kind==WeatherKind.Storm?"Gewitter":"Nebel";
        void OnGUI()
        {
            if(clockStyle==null){clockStyle=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter};smallStyle=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.MiddleCenter};}
            int minute=(int)(TotalGameMinutes%1440);
            var hud=Cursor.visible?new Rect(22,58,256,58):new Rect(Screen.width*.5f-128,10,256,58);GUI.Box(hud,GUIContent.none);
            GUI.Label(new Rect(hud.x,hud.y+2,hud.width,28),"Tag "+DayNumber+" · "+(minute/60).ToString("00")+":"+(minute%60).ToString("00"),clockStyle);
            GUI.Label(new Rect(hud.x,hud.y+29,hud.width,22),WeatherName(Weather)+" · "+SpeedMultiplier.ToString("0")+"×",smallStyle);
            if(!Cursor.visible)return;
            GUILayout.BeginArea(ControlsRect,GUI.skin.box);
            GUILayout.Label("WETTER & TAGESZEIT");
            GUILayout.Label("Zeittempo (Uhr, Wetter und Trocknung)");
            GUILayout.BeginHorizontal();foreach(float speed in new[]{1f,2f,5f,10f,20f}){GUI.enabled=SpeedMultiplier!=speed;if(GUILayout.Button(speed+"×",GUILayout.Height(27)))SetSpeed(speed);}GUI.enabled=true;GUILayout.EndHorizontal();
            GUILayout.Label("1×: Ein Spieltag dauert 48 Minuten.");
            GUILayout.Space(7);GUILayout.Label("Wetter · "+(Automatic?"automatisch":"manuell"));
            GUILayout.BeginHorizontal();foreach(var kind in new[]{WeatherKind.Clear,WeatherKind.Cloudy,WeatherKind.Rain})if(GUILayout.Button(WeatherName(kind),GUILayout.Height(27)))SetWeather(kind);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();foreach(var kind in new[]{WeatherKind.Storm,WeatherKind.Fog})if(GUILayout.Button(WeatherName(kind),GUILayout.Height(27)))SetWeather(kind);if(GUILayout.Button("Automatisch",GUILayout.Height(27)))UseAutomaticWeather();GUILayout.EndHorizontal();
            GUILayout.Space(7);GUILayout.Label("Tageszeit wählen");
            GUILayout.BeginHorizontal();foreach(float hour in new[]{7f,12f,20f,0f})if(GUILayout.Button(((int)hour).ToString("00")+":00",GUILayout.Height(27)))SetHour(hour);GUILayout.EndHorizontal();
            GUILayout.Space(6);GUILayout.Label("Nasse Flächen  "+Mathf.RoundToInt(Wetness*100)+" %");
            GUILayout.Label("Pfützen  "+Mathf.RoundToInt(PuddleAmount*100)+" % · Bodenfeuchte  "+Mathf.RoundToInt(SoilMoisture*100)+" %");
            GUILayout.Label("Nach Regen trocknen Flächen allmählich ab.");
            GUILayout.Label("Uhrzeit und Nässe werden gespeichert.");
            GUILayout.Space(6);GUILayout.Label("Esc: Einstellungen · Klick ins Spiel: zurück");
            GUILayout.EndArea();
        }
    }
}
