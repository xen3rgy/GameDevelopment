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
    [InitializeOnLoad] public static class WestbruchDesignQA
    {
        const string Flag="ZeroRise.DesignQA";
        static WestbruchDesignQA(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Design verification camera").AddComponent<DesignCapture>();}};}
        public static void Run(){WestbruchDesignPass.Rebuild();Validate();SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void CaptureSaved(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Validate();SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
        public static void Validate()
        {
            var root=GameObject.Find("Westbruch \u2013 authored district").transform;Physics.SyncTransforms();
            if(root.GetComponentsInChildren<TextMesh>().Length!=0)throw new Exception("Floating text remains");
            var signs=root.GetComponentsInChildren<MountedSign>();if(signs.Length<38)throw new Exception("Expected house, shop, bus and street signage missing: "+signs.Length);
            foreach(var sign in signs)
            {
                if(!sign.support)throw new Exception("Unattached sign: "+sign.name);
                var face=sign.transform.Find("Printed enamel face").GetComponent<MeshRenderer>();
                if(!face.sharedMaterial.GetTexture("_BaseColorMap"))throw new Exception("Missing printed sign texture: "+sign.name);
                if(face.sharedMaterial.shader.name!="HDRP/Lit")throw new Exception("Sign must use scene lighting");
            }
            var filters=root.GetComponentsInChildren<MeshFilter>();var bars=filters.Where(f=>f.name=="Faded pedestrian crossing").ToArray();
            if(filters.Any(f=>f.name=="Worn pedestrian crossing"||f.name=="Sign backing"))throw new Exception("Obsolete overlapping geometry remains");
            foreach(var f in bars)
            {
                if(f.sharedMesh.vertexCount!=4||f.GetComponent<Collider>())throw new Exception("Crossing is not a single paint surface");
                foreach(var v in f.sharedMesh.vertices)if(Mathf.Abs(f.transform.TransformPoint(v).y-.050f)>.0001f)throw new Exception("Paint depth incorrect");
            }
            var kerbs=root.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Kerb").Select(c=>c.bounds).ToArray();int faces=0;float overlap=0;
            foreach(var f in filters.Where(f=>f.name=="Connected pavement"))
            {
                var mesh=f.sharedMesh;var v=mesh.vertices;var n=mesh.normals;
                for(int i=0;i<v.Length;i+=4)
                {
                    var normal=f.transform.TransformDirection(n[i]);if(Mathf.Abs(normal.y)>.9f)continue;faces++;
                    var points=Enumerable.Range(i,4).Select(j=>f.transform.TransformPoint(v[j])).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);bool x=Mathf.Abs(normal.x)>.9f;float plane=x?b.center.x:b.center.z;
                    foreach(var k in kerbs)
                    {
                        float other=x?(normal.x<0?k.min.x:k.max.x):(normal.z<0?k.min.z:k.max.z);if(Mathf.Abs(plane-other)>.0005f)continue;
                        float along=Mathf.Min(x?b.max.z:b.max.x,x?k.max.z:k.max.x)-Mathf.Max(x?b.min.z:b.min.x,x?k.min.z:k.min.x),height=Mathf.Min(b.max.y,k.max.y)-Mathf.Max(b.min.y,k.min.y);
                        if(along>.0001f&&height>.0001f)overlap+=along*height;
                    }
                }
            }
            if(overlap>.001f)throw new Exception("Coplanar kerb/pavement faces remain: "+overlap+" m2");
            string report="Mounted printed plates: "+signs.Length+"\nFloating labels: 0\nSingle-face crossing bars: "+bars.Length+"\nVertical pavement faces checked: "+faces+"\nCoplanar curb overlap: "+overlap+" m2\n";
            Directory.CreateDirectory("QA");File.WriteAllText("QA/design-geometry.txt",report);Debug.Log(report);
        }
    }
    public sealed class DesignCapture:MonoBehaviour
    {
        Camera camera;RenderTexture target;int view,frames;int failures;
        readonly Vector3[] eyes={new Vector3(97,2.5f,12),new Vector3(-72.7f,2.1f,12.5f),new Vector3(90,.9f,-18),new Vector3(85,9,-27),new Vector3(57,3,29),new Vector3(94,1.5f,109),new Vector3(56,2.5f,-41)};
        readonly Vector3[] aims={new Vector3(105,2.15f,17),new Vector3(-70.8f,2,15.9f),new Vector3(96,.05f,-13),new Vector3(94,1,-10),new Vector3(57,3.5f,39),new Vector3(94,.85f,113.42f),new Vector3(56,2.8f,-48)};
        readonly string[] names={"bus","house-number","crossing-kerb","junction","shop","boundary","workshop"};
        void Start()
        {
            Application.logMessageReceived+=(text,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)failures++;};
            Time.timeScale=0;var player=FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;player.orbit.enabled=false;camera=Camera.main;camera.enabled=false;camera.nearClipPlane=.08f;camera.fieldOfView=60;
            target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var hd=camera.GetComponent<HDAdditionalCameraData>();hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.None;hd.customRenderingSettings=true;
            foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.DepthOfField}){hd.renderingPathCustomFrameSettings.SetEnabled(field,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
        }
        void LateUpdate()
        {
            try
            {
                camera.transform.SetPositionAndRotation(eyes[view],Quaternion.LookRotation(aims[view]-eyes[view]));
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});if(++frames<12)return;
                var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();File.WriteAllBytes("QA/design-"+names[view]+".png",pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;frames=0;
                if(++view==eyes.Length){File.WriteAllText("QA/design-native.txt","Native HDRP views: 7\nRuntime errors: "+failures);EditorApplication.Exit(failures==0?0:1);}
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
