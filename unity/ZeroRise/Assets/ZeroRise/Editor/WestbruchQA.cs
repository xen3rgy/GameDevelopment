using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    public static class WestbruchQA
    {
        [Serializable] class Report
        {
            public int buildings, renderers, enabledRenderers, colliders, entranceProbes, roadProbes, cameraProbes;
            public int terrainLayers,treeCount,treeVariants;
            public float groundRelief;
            public float grassWeight;
            public int uniqueCracks,irregularRepairs,vegetationChunks;
            public List<string> failures = new List<string>();
        }
        [MenuItem("ZERO RISE/Westbruch pruefen")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var data=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var report=new Report { buildings=data.buildings.Length };
            var colliders=UnityEngine.Object.FindObjectsByType<Collider>();
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>();
            report.colliders=colliders.Length;report.renderers=renderers.Length;
            Physics.SyncTransforms();
            var terrain=Terrain.activeTerrain;
            report.terrainLayers=terrain?terrain.terrainData.terrainLayers.Length:0;
            if(report.terrainLayers<4)report.failures.Add("Ground material blending missing");
            if(terrain)
            {
                var splats=terrain.terrainData.GetAlphamaps(0,0,terrain.terrainData.alphamapWidth,terrain.terrainData.alphamapHeight);
                float total=0;for(int z=0;z<splats.GetLength(0);z++)for(int x=0;x<splats.GetLength(1);x++)total+=splats[z,x,1];
                report.grassWeight=total/(splats.GetLength(0)*splats.GetLength(1));if(report.grassWeight<.025f||report.grassWeight>.75f)report.failures.Add("Sparse district ground blend invalid");
                float min=100,max=-100;
                for(float x=-83;x<83;x+=5)for(float z=-72;z<72;z+=5){float h=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;min=Mathf.Min(min,h);max=Mathf.Max(max,h);}
                report.groundRelief=max-min;if(report.groundRelief<.08f)report.failures.Add("Terrain has no meaningful relief");
            }
            var crowns=new HashSet<Mesh>();
            foreach(var lod in UnityEngine.Object.FindObjectsByType<LODGroup>())
            {
                if(!lod.name.StartsWith("Urban tree "))continue;
                var filter=lod.GetComponentInChildren<MeshFilter>();if(!filter)continue;
                crowns.Add(filter.sharedMesh);report.treeCount++;
                var treeBounds=filter.GetComponent<Renderer>().bounds;
                foreach(var b in data.buildings)
                {
                    var body=GameObject.Find(b.id).transform.Find("Structural shell").GetComponent<Renderer>().bounds;
                    if(body.Intersects(treeBounds))report.failures.Add("Tree crown at "+lod.transform.position+" intersects "+b.id);
                }
            }
            report.treeVariants=crowns.Count;if(crowns.Count<4)report.failures.Add("Distinct tree crown meshes missing");
            foreach(var r in renderers)
            {
                if(r.enabled)report.enabledRenderers++;
                foreach(var material in r.sharedMaterials)
                    {
                    if(!material||!material.shader||!material.shader.isSupported) report.failures.Add("Invalid material: "+r.name);
                    else foreach(var message in ShaderUtil.GetShaderMessages(material.shader))
                        if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)report.failures.Add("Shader error: "+message.message);
                    }
            }
            var motion=UnityEngine.Object.FindAnyObjectByType<VegetationMotion>();
            if(!motion||!motion.player||!motion.car)report.failures.Add("Vegetation actor references missing");
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer)continue;
                if(filter.name.Contains(" unique crack "))
                {
                    report.uniqueCracks++;
                    if(Array.Exists(filter.sharedMesh.normals,n=>n.y<.9f))report.failures.Add("Crack faces below road: "+filter.name);
                }
                if(filter.name.Contains(" irregular repair "))report.irregularRepairs++;
                if(!renderer.enabled||!renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE"))continue;
                report.vegetationChunks++;
                if(!filter.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord2)||!filter.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord3))report.failures.Add("Plant root/bend metadata missing: "+filter.name);
            }
            var asphalt=AssetDatabase.LoadAssetAtPath<Material>(WestbruchGeometry.Generated+"/Materials/Asphalt.mat");
            if(!asphalt||!asphalt.IsKeywordEnabled("_ZR_ROAD")||asphalt.GetFloat("_UVBase")!=4)report.failures.Add("Continuous nonrepeating asphalt missing");
            if(report.uniqueCracks<100||report.irregularRepairs<20||report.vegetationChunks<1)report.failures.Add("Road/plant detail meshes missing");
            foreach(var b in data.buildings)
            {
                var obj=GameObject.Find(b.id);
                if(!obj) { report.failures.Add("Missing building: "+b.id);continue; }
                var p=b.Position(.16f)+Quaternion.Euler(0,b.yaw,0)*new Vector3(0,0,-b.d*.5f-1.15f);
                if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.5f,.31f,1)) report.failures.Add("Blocked entrance: "+b.id);
                if(!Physics.Raycast(p+Vector3.up*.4f,Vector3.down,out var ground,1,1)||ground.point.y<.1f) report.failures.Add("Entrance has no paved support: "+b.id);
                report.entranceProbes++;
            }
            foreach(var road in data.roads)
            {
                bool horizontal=road.w>road.d;float length=horizontal?road.w:road.d;
                // The prototype's city connection ends at signed construction fences.
                float endMargin=road.id=="Stadtanschluss"?5:2;
                for(float a=-length*.5f+endMargin;a<length*.5f-endMargin;a+=2)
                {
                    var p=road.Position(.82f)+(horizontal?Vector3.right:Vector3.forward)*a;
                    if(Physics.CheckBox(p,new Vector3(.95f,.64f,2.0f),horizontal?Quaternion.Euler(0,90,0):Quaternion.identity,1))
                        report.failures.Add("Road obstructed: "+road.id+" at "+a);
                    report.roadProbes++;
                }
            }
            var car=UnityEngine.Object.FindAnyObjectByType<SimpleCar>();
            foreach(float z in new[]{-113.5f,113.5f})
                if(!Physics.CheckBox(new Vector3(94,.65f,z),new Vector3(.5f,.2f,.3f),Quaternion.identity,1))report.failures.Add("Missing construction boundary at "+z);
            if(!car||car.wheels.Length!=4||car.wheelVisuals.Length!=4)report.failures.Add("Driveable car/wheels missing");
            if(car&&car.GetComponent<Rigidbody>().mass<900)report.failures.Add("Car mass invalid");
            // Car camera origin is above its roof. Sample full orbit beneath the bridge
            // using the same swept camera radius and obstacle clearance as OrbitCamera.
            foreach(float z in new[]{61f,65f,69f})foreach(float pitch in new[]{19f,45f,64f})
            for(float yaw=0;yaw<360;yaw+=15)
            {
                var aim=new Vector3(94,2.2f,z);
                var direction=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
                float boom=7;
                if(Physics.SphereCast(aim,.24f,direction,out var hit,boom,1,QueryTriggerInteraction.Ignore))boom=Mathf.Max(.35f,hit.distance-.18f);
                var candidate=aim+direction*boom;
                if(boom<1.8f||Physics.CheckSphere(candidate,.22f,1,QueryTriggerInteraction.Ignore))report.failures.Add("Underpass camera clearance at "+z+" / "+yaw+" / "+pitch);
                report.cameraProbes++;
            }
            var spawn=data.spawn.Position(.2f);
            if(Physics.CheckCapsule(spawn+Vector3.up*.4f,spawn+Vector3.up*1.5f,.31f,1)) report.failures.Add("Blocked player spawn");
            Directory.CreateDirectory("QA");File.WriteAllText("QA/placement-report.json",JsonUtility.ToJson(report,true));
            if(report.failures.Count>0) throw new Exception("Westbruch QA: "+string.Join("; ",report.failures));
            Debug.Log("Westbruch QA passed: "+report.entranceProbes+" entrances, "+report.roadProbes+" road probes.");
        }
        public static void Capture()
        {
            Validate();
            var camera=Camera.main;
            var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);
            camera.targetTexture=target;
            int frames=0,view=0;
            Vector3[] positions={ new Vector3(-99,37,-104),new Vector3(-32,5,-67),new Vector3(86,4,40),new Vector3(-30,3,-33),new Vector3(80,3,28),new Vector3(-40,2.2f,-1),new Vector3(-20,.9f,-46) };
            Vector3[] aims={ new Vector3(-4,0,8),new Vector3(-66,1.7f,-47),new Vector3(94,2.2f,65),new Vector3(-70,1.5f,-23),new Vector3(57,1.6f,42),new Vector3(-27,.04f,1),new Vector3(-22,.32f,-48) };
            EditorApplication.CallbackFunction capture=null;
            capture=()=>
            {
                try
                {
                    camera.transform.SetPositionAndRotation(positions[view],Quaternion.LookRotation(aims[view]-positions[view]));
                    EditorApplication.QueuePlayerLoopUpdate();
                    if(++frames<30)return;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
                    var old=RenderTexture.active;RenderTexture.active=target;
                    var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
                    File.WriteAllBytes("QA/westbruch-"+view+".png",pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
                    frames=0;
                    if(++view<positions.Length)return;
                    foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("ZeroRise/UrbanLit")))
                        if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception("Urban shader: "+message.message);
                    camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
                    EditorApplication.update-=capture;EditorApplication.Exit(0);
                }
                catch(Exception error) { Debug.LogException(error);EditorApplication.update-=capture;EditorApplication.Exit(1); }
            };
            EditorApplication.update+=capture;
        }
        public static void RebuildAndCapture() { WestbruchBuilder.Build();Capture(); }
    }
}
