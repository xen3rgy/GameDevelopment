using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Repair the existing baked rear elevations without rebuilding the district.
    // The original source is retained, disabled and marked once it is aligned.
    internal static class WestbruchRearFacadeRepair
    {
        const string Prefix="Aligned rear ";
        const string RootName="Rear facade alignment repairs";
        static readonly HashSet<string> Names=new HashSet<string>{"Half lowered roller shutter","Shutter louvre","Drawn curtain","Window security bars"};

        public static void Apply(Transform district,Transform newRoot)
        {
            var previous=newRoot.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var sources=district.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.parent&&t.parent.name=="Rear facade detailing"&&Names.Contains(OriginalName(t.name)))
                .Select(t=>t.gameObject).ToArray();
            // Some earlier scenes retain a stale baked shutter body even when
            // its source renderer was visible. Match both old and aligned poses,
            // independent of source visibility, before creating the new copies.
            foreach(var source in sources)
                if(source.GetComponent<MeshRenderer>())source.GetComponent<MeshRenderer>().enabled=false;
            WestbruchOverpassArchitecture.RemoveBakedSources(district,sources);
            var poses=sources.Select(o=>o.transform.localPosition).ToArray();
            var envelopes=new List<Envelope>();
            foreach(var source in sources)AddEnvelope(source,envelopes);
            try
            {
                for(int i=0;i<sources.Length;i++)sources[i].transform.localPosition=new Vector3(-poses[i].x,poses[i].y,poses[i].z);
                foreach(var source in sources)AddEnvelope(source,envelopes);
                WestbruchOverpassArchitecture.RemoveBakedSources(district,sources);
            }
            finally{for(int i=0;i<sources.Length;i++)sources[i].transform.localPosition=poses[i];}
            RemoveRoundingResiduals(district,envelopes);
            var repairs=Group(RootName,newRoot,Vector3.zero).transform;
            foreach(var source in sources)
            {
                var t=source.transform;
                if(!source.name.StartsWith(Prefix,StringComparison.Ordinal))
                {
                    var p=t.localPosition;p.x=-p.x;t.localPosition=p;
                    source.name=Prefix+source.name;
                }
                var originalRenderer=source.GetComponent<MeshRenderer>();
                if(!originalRenderer)continue;
                var aligned=UnityEngine.Object.Instantiate(source,repairs,false);
                aligned.name=source.name;
                aligned.transform.position=t.position;aligned.transform.rotation=t.rotation;
                var scale=repairs.lossyScale;var wanted=t.lossyScale;
                aligned.transform.localScale=new Vector3(wanted.x/scale.x,wanted.y/scale.y,wanted.z/scale.z);
                aligned.GetComponent<MeshRenderer>().enabled=true;aligned.isStatic=true;
                originalRenderer.enabled=false;
            }
            Debug.Log("Rear facade alignment: "+sources.Length+" shutters, louvres, curtains and security rails reseated over their actual windows.");
        }
        static string OriginalName(string name)=>name.StartsWith(Prefix,StringComparison.Ordinal)?name.Substring(Prefix.Length):name;
        sealed class Envelope
        {
            public Material material;
            public Matrix4x4 inverse;
            public Bounds local,world;
            public bool Contains(Vector3 point)=>local.Contains(inverse.MultiplyPoint3x4(point));
        }
        static void AddEnvelope(GameObject source,List<Envelope> envelopes)
        {
            var mesh=source.GetComponent<MeshFilter>();var renderer=source.GetComponent<MeshRenderer>();
            if(!mesh||!mesh.sharedMesh||!renderer||!renderer.sharedMaterial)return;
            var bounds=mesh.sharedMesh.bounds;bounds.Expand(.002f);
            var world=new Bounds(source.transform.TransformPoint(bounds.center),Vector3.zero);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                world.Encapsulate(source.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
            envelopes.Add(new Envelope{material=renderer.sharedMaterial,inverse=source.transform.worldToLocalMatrix,local=bounds,world=world});
        }
        static void RemoveRoundingResiduals(Transform district,List<Envelope> envelopes)
        {
            var chunks=district.Find("Static render chunks – sources retained for editing");if(!chunks)return;
            var updates=new List<KeyValuePair<Mesh,List<int>>>();int removed=0;
            foreach(var filter in chunks.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(!renderer||!mesh||!mesh.isReadable)continue;
                var candidates=envelopes.Where(e=>e.material==renderer.sharedMaterial&&e.world.Intersects(renderer.bounds)).ToArray();
                if(candidates.Length==0)continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;var keep=new List<int>(indices.Length);int changed=0;
                for(int n=0;n<indices.Length;n+=3)
                {
                    var a=filter.transform.TransformPoint(vertices[indices[n]]);var b=filter.transform.TransformPoint(vertices[indices[n+1]]);var c=filter.transform.TransformPoint(vertices[indices[n+2]]);
                    if(candidates.Any(e=>e.Contains(a)&&e.Contains(b)&&e.Contains(c))){changed++;continue;}
                    keep.Add(indices[n]);keep.Add(indices[n+1]);keep.Add(indices[n+2]);
                }
                if(changed==0)continue;
                removed+=changed;updates.Add(new KeyValuePair<Mesh,List<int>>(mesh,keep));
            }
            // Stage all edits before mutating any shared legacy chunk asset.
            foreach(var update in updates){update.Key.SetTriangles(update.Value,0);update.Key.RecalculateBounds();EditorUtility.SetDirty(update.Key);}
            Debug.Log("Rear facade residual cleanup: "+removed+" stale triangles removed using source-local bounds.");
        }
        public static void Dump()
        {
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var lines=new List<string>();
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=f.GetComponent<MeshRenderer>();var mesh=f.sharedMesh;if(!renderer||!mesh)continue;
                if(f.name.Contains("Half lowered")&&renderer.bounds.center.x> -50&&renderer.bounds.center.x< -26&&renderer.bounds.center.z>28&&renderer.bounds.center.z<30)
                    lines.Add("SOURCE "+f.name+" / parent "+f.transform.parent.name+" / enabled "+renderer.enabled+" / position "+f.transform.position.ToString("F4")+" / local "+f.transform.localPosition.ToString("F4")+" / bounds "+renderer.bounds+" / material "+renderer.sharedMaterial.name);
                if(!renderer.enabled||!mesh.isReadable||!renderer.sharedMaterial||!renderer.sharedMaterial.name.StartsWith("FadedPaint"))continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;int found=0;var bounds=new Bounds();
                for(int n=0;n<indices.Length;n+=3)
                {
                    var a=f.transform.TransformPoint(vertices[indices[n]]);var b=f.transform.TransformPoint(vertices[indices[n+1]]);var c=f.transform.TransformPoint(vertices[indices[n+2]]);var p=(a+b+c)/3;
                    if(p.x< -50||p.x> -26||p.z<29.03f||p.z>29.15f||p.y<.8f||p.y>3.1f)continue;
                    if(found++==0)bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(a);bounds.Encapsulate(b);bounds.Encapsulate(c);
                    lines.Add("TRI "+f.name+" / material "+renderer.sharedMaterial.name+" / "+a.ToString("F4")+" "+b.ToString("F4")+" "+c.ToString("F4"));
                }
                if(found>0)lines.Add("VISIBLE "+f.name+" / triangles "+found+" / bounds "+bounds);
            }
            Directory.CreateDirectory("QA");File.WriteAllLines("QA/urban-rear-diagnostic.txt",lines);Debug.Log("Rear diagnostic written with "+lines.Count+" lines.");EditorApplication.Exit(0);
        }
    }
}
