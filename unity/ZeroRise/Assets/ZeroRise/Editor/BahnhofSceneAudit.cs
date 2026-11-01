using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ZeroRise.Editor
{
    public static class BahnhofSceneAudit
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var lines=new List<string>();
            string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
            var district=GameObject.Find(BahnhofsviertelBuilder.RootName).transform;
            lines.Add("DISTRICT DIRECT CHILDREN (source geometry bounds)");
            foreach(Transform t in district)
            {
                var rr=t.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();
                if(rr.Length==0)continue;var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
                lines.Add(t.name+" pos="+t.position.ToString("F2")+" min="+b.min.ToString("F2")+" max="+b.max.ToString("F2"));
            }
            lines.Add("EXTERNAL VISIBLE GEOMETRY IN NEW DISTRICT");
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!r.transform.IsChildOf(district)&&r.bounds.max.x>132&&r.bounds.min.x<310&&r.bounds.max.z>65&&r.bounds.min.z<370).OrderBy(r=>Path(r.transform)))
                lines.Add(Path(r.transform)+" min="+r.bounds.min.ToString("F2")+" max="+r.bounds.max.ToString("F2")+" mat="+(r.sharedMaterial?r.sharedMaterial.name:"null"));
            lines.Add("ROOT OBJECTS");foreach(var t in EditorSceneManager.GetActiveScene().GetRootGameObjects())lines.Add(t.name);
            File.WriteAllLines("QA/bahnhof-scene-audit.txt",lines);Debug.Log("Bahnhof scene audit written");EditorApplication.Exit(0);
        }
    }
}
