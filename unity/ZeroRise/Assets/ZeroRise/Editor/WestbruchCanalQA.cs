using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class WestbruchCanalQA
    {
        const string Flag="ZeroRise.CanalQA";
        static WestbruchCanalQA(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Canal verification").AddComponent<CanalCapture>();}};}
        public static void Run(){WestbruchCanalBuilder.Rebuild();CaptureSaved();}
        public static void CaptureSaved()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);var root=GameObject.Find(WestbruchCanalBuilder.RootName);var water=root.GetComponentInChildren<WaterSurface>();
            if(!water||water.surfaceType!=WaterSurfaceType.River||water.GetComponent<Collider>())throw new Exception("Native canal water missing or blocking movement");
            if(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Canal"))throw new Exception("Legacy water box remains");
            var terrain=Terrain.activeTerrain;int trees=0;foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Far bank pioneer tree"))
            {if(Mathf.Abs(t.position.y-(terrain.SampleHeight(t.position)+terrain.transform.position.y-.015f))>.003f)throw new Exception("Floating bank tree");trees++;}
            if(trees!=25)throw new Exception("Far bank tree distribution incomplete");
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>().enabled&&f.GetComponent<MeshRenderer>().sharedMaterial.name=="Canal_AlgaeStains"))
                if(mesh.sharedMesh.normals.Any(n=>n.x>-.9f))throw new Exception("Water-facing masonry stains face away from channel");
            for(float z=-110;z<=110;z+=11)
            {
                var at=water.transform.position+new Vector3(-water.transform.localScale.x*.5f,0,z);
                if(terrain.SampleHeight(at)+terrain.transform.position.y<water.transform.position.y)throw new Exception("Exposed rectangular western water edge");
            }
            var blades=root.GetComponentsInChildren<MeshFilter>().First(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>().enabled&&f.GetComponent<MeshRenderer>().sharedMaterial.name=="Canal_ReedBlades").sharedMesh;
            var roots=blades.uv4;var heights=blades.uv3;int samples=0;var unique=new System.Collections.Generic.HashSet<Vector2>();for(int i=0;i<roots.Length;i++)
            {if(!unique.Add(roots[i]))continue;var p=new Vector3(roots[i].x,0,roots[i].y);float floor=terrain.SampleHeight(p)+terrain.transform.position.y;if(Mathf.Abs(heights[i].x-(floor-.015f))>.004f)throw new Exception("Reed roots not embedded in canal bed");samples++;}
            File.WriteAllText("QA/canal-geometry.txt","Native river surface: 1\nLegacy water boxes: 0\nSupported far bank trees: "+trees+"\nEmbedded reed roots checked: "+samples+"\n");
            SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
        }
    }
    public sealed class CanalCapture:MonoBehaviour
    {
        Camera camera;RenderTexture target;WaterSurface water;int view,frames,errors;float started,firstSimulation;Texture2D first;float waterDifference;bool barrier;
        readonly Vector3[] eyes={new Vector3(-109.2f,2.8f,-26),new Vector3(-109.2f,2.6f,40),new Vector3(-111,1.1f,29),new Vector3(-116,5,-66),new Vector3(-100,8,2),new Vector3(-109.2f,2.8f,-26)};
        readonly Vector3[] aims={new Vector3(-121,0,13),new Vector3(-120,0,-29),new Vector3(-116,-.1f,27),new Vector3(-113,0,-60),new Vector3(-135,.5f,20),new Vector3(-121,0,13)};
        readonly string[] names={"north","south","water-close","ladder","overview","north-motion"};
        void Start()
        {
            Application.logMessageReceived+=(text,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors++;};
            water=FindAnyObjectByType<WaterSurface>();var player=FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;player.orbit.enabled=false;
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-109,.2f,-5);cc.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<50;i++)cc.Move(new Vector3(-.10f,-.08f,0));barrier=player.transform.position.x> -111;
            if(!barrier)throw new Exception("Canal edge barrier permits walking into water");
            camera=Camera.main;camera.enabled=false;camera.fieldOfView=60;camera.nearClipPlane=.08f;
            var hd=camera.GetComponent<HDAdditionalCameraData>();hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;hd.customRenderingSettings=true;
            foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.DepthOfField}){hd.renderingPathCustomFrameSettings.SetEnabled(field,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
            target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;started=Time.time;
        }
        void LateUpdate()
        {
            try
            {
                camera.transform.SetPositionAndRotation(eyes[view],Quaternion.LookRotation(aims[view]-eyes[view]));RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                if(++frames<35||Time.time-started<1)return;
                var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes("QA/canal-"+names[view]+".png",pixels.EncodeToPNG());RenderTexture.active=old;
                if(view==0){first=pixels;firstSimulation=water.simulationTime;}
                else {if(view==5){double difference=0;int count=0;for(int y=280;y<520;y+=2)for(int x=540;x<1020;x+=2){var a=first.GetPixel(x,y);var b=pixels.GetPixel(x,y);difference+=Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);count++;}waterDifference=(float)(difference/count);}Destroy(pixels);}
                frames=0;started=Time.time;
                if(++view==eyes.Length)
                {
                    float advance=water.simulationTime-firstSimulation;bool passed=errors==0&&advance>.1f&&waterDifference>.002f&&barrier;
                    File.WriteAllText("QA/canal-native.txt","Native HDRP views: 6\nWater simulation advance: "+advance+"\nWater image motion difference: "+waterDifference+"\nEdge barrier: "+barrier+"\nRuntime errors: "+errors+"\nPassed: "+passed+"\n");EditorApplication.Exit(passed?0:1);
                }
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
