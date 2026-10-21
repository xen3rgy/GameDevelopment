using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Re-seat only existing detail inside the edited footprint, after its final
    // ground exists. Baked meshes retain all geometry outside that footprint.
    internal static class WestbruchOverpassPlacement
    {
        static bool Local(Vector3 p)=>p.x>97&&p.x<168&&p.z>53&&p.z<115;
        internal static float Surface(float x,float z)
        {
            if(x>=100&&x<=142&&(z>=72&&z<=75||z>=85&&z<=88)||x>=139&&x<=163&&(z>=58&&z<=61||z>=99&&z<=102)||x>=139&&x<=142&&z>=61&&z<=99||x>=160&&x<=163&&z>=61&&z<=99)return .14f;
            if(x>=100&&x<=142&&z>=75&&z<=85||x>=142&&x<=160&&z>=61&&z<=99)return .04f;
            return WestbruchOverpassGround.Surface(x,z);
        }
        internal static void Repair(Transform district,Transform newBridge)
        {
            int pieces=0,removed=0,trees=0,plants=0;
            foreach(var tree in district.GetComponentsInChildren<LODGroup>())
            {
                if(!tree.name.StartsWith("Urban tree ")||!Local(tree.transform.position))continue;
                var p=tree.transform.position;p.y=Surface(p.x,p.z)-.035f;tree.transform.position=p;trees++;
            }
            foreach(var filter in district.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(filter.transform.IsChildOf(newBridge)||!renderer||!renderer.enabled||!mesh||!mesh.isReadable||!renderer.sharedMaterial)continue;
                var material=renderer.sharedMaterial;
                if(material.IsKeywordEnabled("_ZR_FOLIAGE"))
                {
                    var v=mesh.vertices;var heights=mesh.uv3;var roots=mesh.uv4;if(roots.Length!=v.Length||heights.Length!=v.Length)continue;
                    bool changed=false;
                    for(int i=0;i<v.Length;i++)
                    {
                        var at=new Vector3(roots[i].x,0,roots[i].y);if(!Local(at))continue;
                        float target=Surface(at.x,at.z)-.018f,delta=target-heights[i].x;
                        if(Mathf.Abs(delta)<.001f)continue;
                        var world=filter.transform.TransformPoint(v[i]);world.y+=delta;v[i]=filter.transform.InverseTransformPoint(world);heights[i].x=target;plants++;changed=true;
                    }
                    if(changed){mesh.vertices=v;mesh.uv3=heights;mesh.RecalculateBounds();var b=mesh.bounds;b.Expand(new Vector3(2.6f,1.2f,2.6f));mesh.bounds=b;EditorUtility.SetDirty(mesh);}
                    continue;
                }
                if(!filter.name.StartsWith("StreetLifeChunk_")&&!filter.name.StartsWith("GroundRelief_")&&!material.name.StartsWith("GroundDetail_"))continue;
                if(!renderer.bounds.Intersects(new Bounds(new Vector3(132,3,84),new Vector3(72,16,64))))continue;
                var vertices=mesh.vertices;var indices=mesh.triangles;var parent=Enumerable.Range(0,vertices.Length).ToArray();
                int Root(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
                void Union(int a,int b){a=Root(a);b=Root(b);if(a!=b)parent[b]=a;}
                var weld=new Dictionary<Vector3Int,int>();
                foreach(int index in new HashSet<int>(indices))
                {
                    var p=vertices[index];var key=new Vector3Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000));
                    if(weld.TryGetValue(key,out int other))Union(index,other);else weld[key]=index;
                }
                for(int i=0;i<indices.Length;i+=3){Union(indices[i],indices[i+1]);Union(indices[i],indices[i+2]);}
                var groups=new Dictionary<int,List<int>>();foreach(int index in new HashSet<int>(indices)){int id=Root(index);if(!groups.TryGetValue(id,out var list))groups[id]=list=new List<int>();list.Add(index);}
                var discard=new HashSet<int>();bool dirty=false;
                foreach(var pair in groups)
                {
                    var points=pair.Value.Select(i=>filter.transform.TransformPoint(vertices[i])).ToArray();var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;
                    if(!Local(center))continue;
                    float low=points.Min(p=>p.y);
                    float lowestFloor=float.PositiveInfinity,highestFloor=float.NegativeInfinity;
                    foreach(var p in points){float h=Surface(p.x,p.z);lowestFloor=Mathf.Min(lowestFloor,h);highestFloor=Mathf.Max(highestFloor,h);}
                    // Old waste inside newly laid paving or newly raised fill is
                    // removed, rather than transplanted implausibly onto it.
                    if(WestbruchOverpassArchitecture.IsPaved(center.x,center.z,.1f)||Surface(center.x,center.z)>low+.6f||highestFloor-lowestFloor>.10f)
                    {discard.Add(pair.Key);removed++;dirty=true;continue;}
                    float shift=float.NegativeInfinity;
                    foreach(var p in points)if(p.y<=low+.018f)shift=Mathf.Max(shift,Surface(p.x,p.z)-p.y-.012f);
                    if(Mathf.Abs(shift)<.001f)continue;
                    foreach(int index in pair.Value){var p=filter.transform.TransformPoint(vertices[index]);p.y+=shift;vertices[index]=filter.transform.InverseTransformPoint(p);}
                    pieces++;dirty=true;
                }
                if(!dirty)continue;
                var keep=new List<int>();for(int i=0;i<indices.Length;i+=3)if(!discard.Contains(Root(indices[i])))keep.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
                mesh.vertices=vertices;mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
            File.WriteAllText("QA/overpass-placement-repair.txt",$"Reseated existing ground pieces: {pieces}\nRemoved conflicting old pieces: {removed}\nRegrounded trees: {trees}\nRegrounded flora vertices: {plants}\n");
        }
    }
}
