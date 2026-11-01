using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Geometric acceptance checks for the rendered district, independent of
    // object counts and route reachability in the gameplay smoke test.
    internal static class BahnhofVisualQualityQA
    {
        struct TopFace
        {
            public Vector3 a,b,c;
            public string owner;
            public float minX,maxX,minZ,maxZ;
        }

        public static void Validate(Transform root,Action<string,bool,string> record)
        {
            if(!root){record("Visual review district exists",false,"Missing district root");return;}
            Signs(root,record);
            var hallExit=root.GetComponentsInChildren<FittedWorldSign>().FirstOrDefault(sign=>sign.lettering&&sign.lettering.text=="ZU DEN GLEISEN");
            bool attached=hallExit&&Physics.Raycast(hallExit.transform.position-hallExit.transform.forward*.05f,hallExit.transform.forward,out var mountHit,.35f,1,QueryTriggerInteraction.Ignore);
            record("Hall exit sign is fixed to the physical lintel",attached,"Rear direction plate mounting ray");
            foreach(var point in new[]{new Vector3(152,2,265),new Vector3(140,2,210),new Vector3(140,2,225),new Vector3(161.5f,2,80),new Vector3(133,2,244),new Vector3(133,2,283)})
            {
                bool supported=Physics.Raycast(point,Vector3.down,out var hit,3,1,QueryTriggerInteraction.Ignore)&&hit.point.y>=.025f&&hit.point.y<.25f;
                record("Actual ground supports the connecting route "+point,supported,supported?hit.collider.name:"Missing walkable floor");
            }
            ParkedCars(root,record);
            MeshesAndRoofs(root,record);
            ContinuousSurfaces(root,record);
        }

        static void Signs(Transform root,Action<string,bool,string> record)
        {
            var signs=root.GetComponentsInChildren<FittedWorldSign>(true);
            record("Physical sign system installed",signs.Length>0,"Mounted signs: "+signs.Length);
            var faults=new List<string>();
            foreach(var sign in signs)
            {
                var backing=sign.transform.Find("BV2_Sign backing");
                if(!backing){faults.Add(sign.name+": missing direct backing");continue;}
                if(!sign.lettering||sign.boardWidth<=0||sign.boardHeight<=0)
                {faults.Add(sign.name+": missing lettering or invalid board dimensions");continue;}
                sign.Refit();
                var renderer=sign.lettering.GetComponent<MeshRenderer>();
                if(!renderer){faults.Add(sign.name+": missing lettering renderer");continue;}
                var bounds=renderer.localBounds;var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
                var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var local=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                    var at=sign.transform.InverseTransformPoint(renderer.transform.TransformPoint(local));
                    min=Vector3.Min(min,at);max=Vector3.Max(max,at);
                }
                float halfWidth=sign.boardWidth*.90f*.5f,halfHeight=sign.boardHeight*.82f*.5f;
                const float tolerance=.006f;
                if(!Finite(min)||!Finite(max)||min.x< -halfWidth-tolerance||max.x>halfWidth+tolerance||min.y< -halfHeight-tolerance||max.y>halfHeight+tolerance)
                    faults.Add(sign.name+": lettering bounds "+min.ToString("F3")+" / "+max.ToString("F3")+" exceed board "+sign.boardWidth+" × "+sign.boardHeight);
            }
            record("Lettering stays within its physical sign backing",faults.Count==0,Details(faults,signs.Length+" signs measured in their own mounting frame"));
            var loose=root.GetComponentsInChildren<TextMesh>(true).Where(t=>!t.GetComponentInParent<FittedWorldSign>()).Select(t=>t.name).ToList();
            record("No loose legacy world lettering remains",loose.Count==0,Details(loose,"All world text belongs to fitted sign boards"));
        }

        static void ParkedCars(Transform root,Action<string,bool,string> record)
        {
            var cars=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.IndexOf("parked car",StringComparison.OrdinalIgnoreCase)>=0||t.name.StartsWith("Parked vehicle ",StringComparison.Ordinal)||t.name.StartsWith("BV2_Parked ",StringComparison.Ordinal)).ToArray();
            var faults=new List<string>();
            foreach(var car in cars)
            {
                var renderers=car.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                bool hasBatch=renderers.Any(r=>r.name.StartsWith("Baked vehicle panels ",StringComparison.Ordinal));
                if(!hasBatch)continue;
                var raw=renderers.Where(r=>!r.name.StartsWith("Baked vehicle panels ",StringComparison.Ordinal)&&!r.GetComponent<TextMesh>()).ToArray();
                if(raw.Length>0)faults.Add(car.name+": baked panels and "+raw.Length+" live individual sources ("+raw[0].name+")");
            }
            record("Parked vehicles render each surface once",faults.Count==0,Details(faults,"Checked "+cars.Length+" parked vehicle roots"));
        }

        static void MeshesAndRoofs(Transform root,Action<string,bool,string> record)
        {
            int meshes=0,vertices=0,roofs=0;var invalid=new List<string>();var badRoofs=new List<string>();
            var tested=new HashSet<Mesh>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable)continue;
                var world=filter.transform.localToWorldMatrix;
                if(!Finite(world.MultiplyPoint3x4(Vector3.zero))||!Finite(filter.transform.lossyScale))invalid.Add(filter.name+": invalid transform");
                if(tested.Add(mesh))
                {
                    meshes++;var v=mesh.vertices;vertices+=v.Length;
                    for(int i=0;i<v.Length;i++)if(!Finite(v[i])){invalid.Add(filter.name+": non-finite vertex "+i);break;}
                    var normals=mesh.normals;
                    for(int i=0;i<normals.Length;i++)if(!Finite(normals[i])){invalid.Add(filter.name+": non-finite normal "+i);break;}
                }
                string name=filter.name.ToLowerInvariant();
                if(name.StartsWith("bv2_gables_"))continue;
                if(!(name.Contains("roof")||name.Contains("dach"))||name.Contains("vent")||name.Contains("pipe")||name.Contains("gutter")||name.Contains("chunk")||name.Contains("batch"))continue;
                roofs++;float upwardArea=0;var points=mesh.vertices;var triangles=mesh.triangles;
                for(int i=0;i+2<triangles.Length;i+=3)
                {
                    var a=world.MultiplyPoint3x4(points[triangles[i]]);var b=world.MultiplyPoint3x4(points[triangles[i+1]]);var c=world.MultiplyPoint3x4(points[triangles[i+2]]);
                    upwardArea+=Mathf.Max(0,Vector3.Cross(b-a,c-a).y)*.5f;
                }
                if(upwardArea<.015f)badRoofs.Add(filter.name+": no upward-facing roof surface");
            }
            record("Architectural mesh vertices and normals are finite",invalid.Count==0,Details(invalid,meshes+" unique readable meshes / "+vertices+" vertices"));
            record("Roofs have visible upward-facing surfaces",roofs>0&&badRoofs.Count==0,Details(badRoofs,"Checked "+roofs+" roof pieces"));
        }

        static void ContinuousSurfaces(Transform root,Action<string,bool,string> record)
        {
            var group=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="BV2_Continuous surfaces");
            if(!group){record("Continuous street and pavement surfaces exist",false,"Missing BV2_Continuous surfaces group");return;}
            var faces=new List<TopFace>();
            foreach(var f in group.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=f.sharedMesh;if(!mesh||!mesh.isReadable)continue;
                string name=f.name.ToLowerInvariant();
                if(new[]{"marking","paint","drain","kerb","curb","tactile","chunk","batch"}.Any(name.Contains))continue;
                var v=mesh.vertices;var t=mesh.triangles;var matrix=f.transform.localToWorldMatrix;
                for(int i=0;i+2<t.Length;i+=3)
                {
                    var a=matrix.MultiplyPoint3x4(v[t[i]]);var b=matrix.MultiplyPoint3x4(v[t[i+1]]);var c=matrix.MultiplyPoint3x4(v[t[i+2]]);
                    var normal=Vector3.Cross(b-a,c-a);
                    // Vertical walls and down-facing undersides cannot cause a
                    // duplicate top pavement. Sloped crossing landings can.
                    if(normal.y<.0001f||normal.normalized.y<.35f)continue;
                    faces.Add(new TopFace{a=a,b=b,c=c,owner=f.name,minX=Mathf.Min(a.x,b.x,c.x),maxX=Mathf.Max(a.x,b.x,c.x),minZ=Mathf.Min(a.z,b.z,c.z),maxZ=Mathf.Max(a.z,b.z,c.z)});
                }
            }
            record("Continuous surfaces contain usable top geometry",faces.Count>0,"Top faces: "+faces.Count);
            int stride=Mathf.Max(1,Mathf.CeilToInt(faces.Count/240f)),samples=0;var faults=new List<string>();
            for(int i=0;i<faces.Count;i+=stride)
            {
                var face=faces[i];var at=(face.a+face.b+face.c)/3;samples++;int hits=0;string other="";
                foreach(var test in faces)
                {
                    if(at.x<test.minX||at.x>test.maxX||at.z<test.minZ||at.z>test.maxZ)continue;
                    if(HeightAt(test,at,out float height)&&Mathf.Abs(height-at.y)<.0008f){hits++;if(test.owner!=face.owner)other=test.owner;}
                }
                if(hits>1&&faults.Count<12)faults.Add(at.ToString("F3")+": "+hits+" coincident top faces at "+face.owner+(other.Length>0?" / "+other:""));
            }
            record("Street and pavement top surfaces do not overlap",faces.Count>0&&faults.Count==0,Details(faults,"Checked "+samples+" interior top-face probes"));
        }

        static bool HeightAt(TopFace face,Vector3 p,out float height)
        {
            float x1=face.b.x-face.a.x,z1=face.b.z-face.a.z,x2=face.c.x-face.a.x,z2=face.c.z-face.a.z;
            float denominator=x1*z2-z1*x2;height=0;if(Mathf.Abs(denominator)<1e-9f)return false;
            float x=p.x-face.a.x,z=p.z-face.a.z;
            float b=(x*z2-z*x2)/denominator,c=(x1*z-z1*x)/denominator,a=1-b-c;
            // Ignore shared edges of neighbouring, non-overlapping triangles.
            if(a<.00001f||b<.00001f||c<.00001f)return false;
            height=face.a.y*a+face.b.y*b+face.c.y*c;return true;
        }
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
        static string Details(List<string> faults,string success)=>faults.Count==0?success:string.Join("; ",faults.Take(12));
    }
}
