using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    // Explicit standalone release smoke mode; dormant during every normal game launch.
    public sealed class OverpassBuildSmoke : MonoBehaviour
    {
        string output;int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-overpass-smoke");if(at<0)return;
            var test=new GameObject("Standalone overpass release smoke").AddComponent<OverpassBuildSmoke>();
            test.output=at+1<args.Length?args[at+1]:Path.Combine(Application.persistentDataPath,"OverpassQA");Directory.CreateDirectory(test.output);
        }
        void OnEnable(){Application.logMessageReceived+=Log;}
        void OnDisable(){Application.logMessageReceived-=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
        IEnumerator Start()
        {
            var player=FindAnyObjectByType<ThirdPersonMotor>();var camera=Camera.main;
            if(!player||!camera){Complete(false,"Missing player/camera.");yield break;}
            for(int i=0;i<180;i++)yield return null;
            var position=player.transform.position;
            bool current=GameObject.Find("Continuous rail head")&&GameObject.Find("Connected Nordring east carriageway")&&Vector2.Distance(new Vector2(position.x,position.z),new Vector2(116,70))<.3f&&player.IsGrounded;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"overpass-windows-spawn.png"));
            for(int i=0;i<30;i++)yield return null;
            player.enabled=false;player.orbit.enabled=false;camera.transform.position=new Vector3(103,2.0f,77.5f);camera.transform.LookAt(new Vector3(124,2.8f,80));
            for(int i=0;i<90;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"overpass-windows-underpass.png"));
            for(int i=0;i<30;i++)yield return null;
            camera.transform.position=new Vector3(81,9,93);camera.transform.LookAt(new Vector3(114,1,80));
            for(int i=0;i<90;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"overpass-windows-junction.png"));
            for(int i=0;i<30;i++)yield return null;
            string releasePath=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Westbruch-build.json");
            bool stamp=File.Exists(releasePath);string release=stamp?File.ReadAllText(releasePath):"MISSING";
            Complete(current&&stamp&&errors==0,"Spawn: "+position.ToString("F3")+"\nCurrent engineered overpass scene and grounded spawn: "+current+"\nRelease manifest present: "+stamp+"\nRuntime errors: "+errors+"\nApplication data: "+Application.dataPath+"\nRelease: "+release);
        }
        void Complete(bool pass,string details){File.WriteAllText(Path.Combine(output,"overpass-windows-smoke.txt"),"Passed: "+pass+"\n"+details);Application.Quit(pass?0:1);}
    }
}
