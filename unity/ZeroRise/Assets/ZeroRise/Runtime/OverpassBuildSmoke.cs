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
            int approachRamps=0;
            foreach(var name in new[]{"False_False","False_True","True_False","True_True"})
                if(GameObject.Find("Seated maintenance approach ramp "+name))approachRamps++;
            var bank=GameObject.Find("Continuous South railway earth bank");
            var bankRenderer=bank?bank.GetComponent<MeshRenderer>():null;
            string groundShader=bankRenderer&&bankRenderer.sharedMaterial&&bankRenderer.sharedMaterial.shader?bankRenderer.sharedMaterial.shader.name:"MISSING";
            bool layeredGround=groundShader=="HDRP/LayeredLit";
            int crossingLandings=0,roomInserts=0,freightSignals=0;
            foreach(var t in FindObjectsByType<Transform>())
            {
                if(t.name.StartsWith("Dropped crossing landing "))crossingLandings++;
                if(t.name.StartsWith("Visible apartment interior "))roomInserts++;
                if(t.name.StartsWith("Freight stop signal "))freightSignals++;
            }
            bool neighbourhood=GameObject.Find("Westbruch inhabited neighbourhood update")&&crossingLandings==36&&roomInserts==26&&freightSignals==2;
            bool releaseFeatures=approachRamps==4&&layeredGround&&neighbourhood;
            bool current=GameObject.Find("Continuous rail head")&&GameObject.Find("Connected Nordring east carriageway")&&Vector2.Distance(new Vector2(position.x,position.z),new Vector2(116,70))<.3f&&player.IsGrounded&&releaseFeatures;
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
            // Detail captures use the actual standalone renderer and supporting
            // colliders, including the joins missed by district-wide views.
            yield return CaptureDetail(camera,"wall-cap",new Vector3(111,7.8f,59),new Vector3(116,5.7f,63));
            yield return CaptureDetail(camera,"slope-toe",Eye(132,52,1.2f),new Vector3(124,3.2f,58));
            yield return CaptureDetail(camera,"south-corner",new Vector3(137,1.5f,70.5f),new Vector3(141,.14f,74));
            yield return CaptureDetail(camera,"north-corner",new Vector3(137,1.5f,90),new Vector3(141,.14f,86));
            yield return CaptureDetail(camera,"fence",Eye(138,55,1.4f),Eye(136,57.5f,.65f));
            yield return CaptureDetail(camera,"pier-rubble",Eye(125,67,.75f),Eye(122.8f,65.8f,.08f));
            yield return CaptureDetail(camera,"urban-housing",new Vector3(-76,2.1f,10),new Vector3(-73,3,16));
            yield return CaptureDetail(camera,"urban-shop",new Vector3(74,1.5f,41),new Vector3(71.1f,.5f,43.2f));
            yield return CaptureDetail(camera,"urban-graffiti",new Vector3(-22,3.7f,24),new Vector3(-26.99f,3.34f,22));
            yield return CaptureDetail(camera,"urban-rear",new Vector3(-32,1.7f,34),new Vector3(-31.6f,.6f,29.8f));
            yield return CaptureDetail(camera,"urban-crossing",new Vector3(75,1.2f,72),new Vector3(72,.1f,75));
            yield return CaptureDetail(camera,"urban-drain",new Vector3(47.5f,1.0f,82),new Vector3(46,.05f,84.7f));
            yield return CaptureDetail(camera,"urban-railway",new Vector3(124,10,36),new Vector3(118,8,51));
            yield return CaptureDetail(camera,"urban-railway-deck",new Vector3(124,8.5f,72),new Vector3(119,6.7f,94));
            string releasePath=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Westbruch-build.json");
            bool stamp=File.Exists(releasePath);string release=stamp?File.ReadAllText(releasePath):"MISSING";
            Complete(current&&stamp&&errors==0,"Spawn: "+position.ToString("F3")+"\nCurrent engineered overpass scene and grounded spawn: "+current+"\nFinal release geometry/material features: "+releaseFeatures+"\nNeighbourhood update: "+neighbourhood+"; crossing landings "+crossingLandings+"; apartment interiors "+roomInserts+"; freight signals "+freightSignals+"\nSeated approach ramp objects: "+approachRamps+"/4\nHDRP layered ground present: "+layeredGround+" ("+groundShader+")\nRelease manifest present: "+stamp+"\nRuntime errors: "+errors+"\nApplication data: "+Application.dataPath+"\nRelease: "+release);
        }
        IEnumerator CaptureDetail(Camera camera,string name,Vector3 eye,Vector3 target)
        {
            camera.transform.position=eye;camera.transform.LookAt(target);
            for(int i=0;i<45;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"overpass-windows-detail-"+name+".png"));
            // Screenshot is written at frame end. Retain this pose until then.
            for(int i=0;i<5;i++)yield return null;
        }
        static Vector3 Eye(float x,float z,float eyeHeight)
        {
            float floor=float.NegativeInfinity;
            var ground=GameObject.Find("Railway earthworks – continuous vegetated embankments");
            foreach(var hit in Physics.RaycastAll(new Vector3(x,20,z),Vector3.down,40,1,QueryTriggerInteraction.Ignore))
            {
                if(hit.normal.y<.3f)continue;
                string name=hit.collider.name.ToLowerInvariant();
                bool supporting=hit.collider is TerrainCollider||(ground&&hit.collider.transform.IsChildOf(ground.transform))||name.Contains("pavement")||name.Contains("carriageway")||name.Contains("turning court")||name.Contains("footing")||name.Contains("ground apron")||name.Contains("hardstanding")||name.Contains("maintenance approach ramp")||name.Contains("maintenance walkway");
                if((name.Contains("maintenance approach ramp")||name.Contains("maintenance walkway"))&&hit.point.y>.5f)continue;
                if(supporting)floor=Mathf.Max(floor,hit.point.y);
            }
            if(float.IsNegativeInfinity(floor))floor=0;
            return new Vector3(x,floor+eyeHeight,z);
        }
        void Complete(bool pass,string details){File.WriteAllText(Path.Combine(output,"overpass-windows-smoke.txt"),"Passed: "+pass+"\n"+details);Application.Quit(pass?0:1);}
    }
}
