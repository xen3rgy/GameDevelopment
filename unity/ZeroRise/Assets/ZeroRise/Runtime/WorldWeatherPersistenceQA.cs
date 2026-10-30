using System;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    // Explicit two-process verification uses an isolated PlayerPrefs slot.
    // It never reads, saves or deletes the player's ordinary world state.
    public sealed class WorldWeatherPersistenceQA : MonoBehaviour
    {
        const string Flag="-weather-persistence-qa";
        const string VerificationKey="ZeroRise.World.Weather.v1.Verification";
        string stage,output;
        WorldWeatherClock clock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,Flag);
            if(index<0)return;
            var world=WorldWeatherClock.Instance;
            if(world){world.enabled=false;world.SuppressSave=true;}
            if(index+2>=args.Length||(args[index+1]!="write"&&args[index+1]!="read"))
            {Debug.LogError("Weather persistence QA expects write|read and an output directory.");Application.Quit(1);return;}
            var qa=new GameObject("Explicit isolated weather restart verification").AddComponent<WorldWeatherPersistenceQA>();
            qa.clock=world;qa.stage=args[index+1];qa.output=args[index+2];
        }

        void Start()
        {
            bool passed=false;string detail;
            try
            {
                Directory.CreateDirectory(output);
                if(!clock)throw new InvalidOperationException("Saved scene has no world clock.");
                if(!string.Equals(clock.StorageKey,VerificationKey,StringComparison.Ordinal))
                    throw new InvalidOperationException("Refusing persistence QA outside the isolated verification slot.");
                string expectedPath=Path.Combine(output,"expected-world-state.json");
                if(stage=="write")
                {
                    clock.state=new WorldWeatherClock.WorldState
                    {
                        minutes=3*1440+22.75*60,nextWeather=3*1440+23.75*60,
                        weather=(int)WeatherKind.Rain,automatic=true,random=919191,
                        rain=.61f,cloud=.87f,wind=.43f,wetness=.78f,puddles=.54f,soil=.89f
                    };
                    clock.SetSpeed(20);
                    string expected=clock.ExportState();
                    File.WriteAllText(expectedPath,expected);
                    clock.SuppressSave=false;
                    try{clock.Save();}finally{clock.SuppressSave=true;}
                    passed=PlayerPrefs.HasKey(clock.StorageKey)&&PlayerPrefs.GetString(clock.StorageKey)==expected;
                    detail="Wrote day, clock, rain, automatic schedule, random state and three water reservoirs to isolated PlayerPrefs. Test speed "+clock.SpeedMultiplier+"x. State: "+expected;
                }
                else
                {
                    if(!File.Exists(expectedPath))throw new FileNotFoundException("Run the write stage first.",expectedPath);
                    string expected=File.ReadAllText(expectedPath),actual=clock.ExportState();
                    bool restored=clock.LoadedSavedWorld&&string.Equals(actual,expected,StringComparison.Ordinal);
                    bool normalSpeed=Mathf.Approximately(clock.SpeedMultiplier,1);
                    passed=restored&&normalSpeed;
                    detail="Restart loaded saved verification world: "+clock.LoadedSavedWorld+"; exact state restored: "+restored+"; test speed resets to 1x: "+normalSpeed+". State: "+actual;
                    // This exact key has already been checked above. Leave the
                    // player's world slot and all audio preferences untouched.
                    PlayerPrefs.DeleteKey(clock.StorageKey);PlayerPrefs.Save();
                }
            }
            catch(Exception exception){detail=exception.ToString();}
            string report="Passed: "+passed+"\nStage: "+stage+"\n"+detail+"\n";
            try{File.WriteAllText(Path.Combine(output,"weather-persistence-"+stage+".txt"),report);}
            catch(Exception exception){passed=false;Debug.LogError("Could not write weather persistence report: "+exception.Message);}
            if(passed)Debug.Log("WEATHER PERSISTENCE "+report);else Debug.LogError("WEATHER PERSISTENCE "+report);
            Application.Quit(passed?0:1);
        }
    }
}
