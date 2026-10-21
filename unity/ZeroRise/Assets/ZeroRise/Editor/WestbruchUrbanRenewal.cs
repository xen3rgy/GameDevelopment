using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    public static class WestbruchUrbanRenewal
    {
        public const string RootName="Westbruch inhabited neighbourhood update";
        [MenuItem("ZERO RISE/Westbruch/Apply neighbourhood and railway update")]
        public static void Apply()
        {
            Directory.CreateDirectory("QA");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit play mode before patching.");
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var district=GameObject.Find("Westbruch – authored district");if(!district)throw new Exception("Expected authored Westbruch scene.");
            var old=GameObject.Find(RootName);if(old)UnityEngine.Object.DestroyImmediate(old);
            var root=Group(RootName,district.transform,Vector3.zero).transform;
            Clear();Physics.SyncTransforms();
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();
            string preserved="Navigation asset: "+AssetDatabase.GetAssetPath(world.navigation)+"\nNPC count: "+world.people.Length+"\nCrossings: "+world.crossings.Length+"\nCars: "+UnityEngine.Object.FindObjectsByType<SimpleCar>(FindObjectsSortMode.None).Length+"\n";
            WestbruchUrbanTransitions.Build(root,plan);
            WestbruchNeighbourhoodLife.Build(root,plan);
            WestbruchRearFacadeRepair.Apply(district.transform,root);
            WestbruchRailwayDetails.Build(root);
            Physics.SyncTransforms();
            Bake(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            File.WriteAllText("QA/urban-update-preserved.txt",preserved+"Existing gameplay and navigation data retained. Local authored patch only.\n");
        }
        public static void RebuildAndValidate(){Apply();WestbruchOverpassQA.CaptureSaved();}
        public static void DiagnoseSaved()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);var lines=new List<string>();
            Validate((label,pass,detail)=>lines.Add((pass?"PASS: ":"FAIL: ")+label+". "+detail));
            File.WriteAllLines("QA/urban-diagnostics.txt",lines);EditorApplication.Exit(0);
        }
        static void Bake(Transform root)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&!f.GetComponent<TextMesh>()).ToArray();
            int number=0;
            // Small spatial batches preserve culling on streets and on the long railway.
            foreach(var batch in filters.GroupBy(f=>new{material=f.GetComponent<MeshRenderer>().sharedMaterial,x=Mathf.FloorToInt(f.transform.position.x/64),z=Mathf.FloorToInt(f.transform.position.z/64),shadow=f.GetComponent<MeshRenderer>().shadowCastingMode}))
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batch.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                string key="UrbanUpdate_"+batch.Key.material.name+"_"+batch.Key.x+"_"+batch.Key.z+"_"+batch.Key.shadow;
                string path=Generated+"/Meshes/"+key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(mesh,path);
                var o=Group("Urban render batch "+number++,root,Vector3.zero);o.isStatic=true;o.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=batch.Key.material;r.shadowCastingMode=batch.Key.shadow;
                foreach(var f in batch)f.GetComponent<MeshRenderer>().enabled=false;
            }
        }
        public static void Validate(Action<string,bool,string> record)
        {
            var root=GameObject.Find(RootName);if(!root)return;
            Physics.SyncTransforms();
            var landings=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Dropped crossing landing ")).ToArray();
            int misses=0,samples=0;float biggest=0;var errors=new List<string>();
            foreach(var landing in landings)
            {
                float last=0;
                for(int i=0;i<=22;i++)
                {
                    var p=landing.TransformPoint(new Vector3(0,1,-.2f+i*.1f));samples++;
                    if(!Physics.Raycast(p,Vector3.down,out var hit,2,1,QueryTriggerInteraction.Ignore)){misses++;errors.Add("No support "+p);continue;}
                    if(i>0&&Mathf.Abs(last-hit.point.y)>.025f)errors.Add("Step at "+p.ToString("F3")+" "+last+" -> "+hit.point.y+" "+hit.collider.name);
                    if(i>0)biggest=Mathf.Max(biggest,Mathf.Abs(last-hit.point.y));last=hit.point.y;
                    // Exactly at the shared road/ramp edge either adjacent
                    // triangle can own the ray. Both heights meet the 15mm lip.
                    if(i>=2&&i<=20){float expected=Mathf.Lerp(.055f,.14f,Mathf.Clamp01((-.2f+i*.1f)/1.35f));bool roadEdge=i==2&&Mathf.Abs(hit.point.y-.04f)<.004f;if(!roadEdge&&Mathf.Abs(hit.point.y-expected)>.004f){misses++;errors.Add("Wrong height "+p.ToString("F3")+" expected "+expected+" got "+hit.point.y+" "+hit.collider.name);}}
                }
            }
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();
            record("Designated pedestrian crossings have continuous flared landings",landings.Length==world.crossings.Length*2&&misses==0&&biggest<.025f,"Landings "+landings.Length+"; samples "+samples+"; missing/wrong-height "+misses+"; maximum 10cm step "+biggest.ToString("F4")+"m; "+string.Join("; ",errors));
            int invalid=0,triangles=0;
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||f.GetComponent<TextMesh>())continue;
                if(!f.sharedMesh||!f.sharedMesh.isReadable||!r.sharedMaterial||!r.sharedMaterial.shader||!r.sharedMaterial.shader.isSupported){invalid++;continue;}
                triangles+=f.sharedMesh.triangles.Length/3;
                if(f.sharedMesh.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))invalid++;
            }
            record("Neighbourhood update meshes and HDRP materials valid",invalid==0&&triangles>0,"Invalid renderers "+invalid+"; triangles "+triangles);
            record("Existing residents and navigation retained",world.navigation&&world.people.Length==18&&world.crossings.Length>=15,"NPCs "+world.people.Length+"; navigation "+world.navigation.name+"; crossings "+world.crossings.Length);
            WestbruchUrbanDetailQA.Validate(record);
        }
    }
}
