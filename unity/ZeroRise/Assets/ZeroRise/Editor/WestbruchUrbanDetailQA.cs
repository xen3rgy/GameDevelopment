using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Inspect the actual indexed render batches as well as disabled editing
    // sources. Physics alone cannot reveal an old kerb left in a render chunk.
    internal static class WestbruchUrbanDetailQA
    {
        public static void Validate(Action<string,bool,string> record)
        {
            var root=GameObject.Find(WestbruchUrbanRenewal.RootName);if(!root)return;
            Physics.SyncTransforms();
            VisibleCrossingCuts(root.transform,record);
            RestingDetails(root.transform,record);
            ExistingFurnitureClearance(root.transform,record);
            LegacyRearWindowFaces(root.transform,record);
            Railway(root.transform,record);
        }
        static List<Vector3> Clip(List<Vector3> p,int axis,float boundary,bool positive)
        {
            var result=new List<Vector3>();if(p.Count==0)return result;
            var a=p[p.Count-1];float da=(a[axis]-boundary)*(positive?1:-1);
            foreach(var b in p)
            {
                float db=(b[axis]-boundary)*(positive?1:-1);
                if((da>=0)!=(db>=0))result.Add(Vector3.Lerp(a,b,da/(da-db)));
                if(db>=0)result.Add(b);a=b;da=db;
            }
            return result;
        }
        static void VisibleCrossingCuts(Transform root,Action<string,bool,string> record)
        {
            var landings=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Dropped crossing landing ")).ToArray();
            var batches=root.root.GetComponentsInChildren<MeshFilter>().Where(f=>
            {
                var r=f.GetComponent<MeshRenderer>();return r&&r.enabled&&f.sharedMesh&&f.sharedMesh.isReadable&&
                    (f.name.StartsWith("Paving_")||f.name.StartsWith("StoneTrim_"));
            }).ToArray();
            int bad=0,checkedTriangles=0;string example="";
            foreach(var landing in landings)
            {
                var region=new Bounds(landing.TransformPoint(new Vector3(0,.12f,.67f)),new Vector3(3.2f,.24f,3.2f));
                foreach(var f in batches)
                {
                    var renderer=f.GetComponent<MeshRenderer>();if(!renderer.bounds.Intersects(region))continue;
                    var vertices=f.sharedMesh.vertices;var ts=f.sharedMesh.triangles;
                    for(int i=0;i<ts.Length;i+=3)
                    {
                        var a=landing.InverseTransformPoint(f.transform.TransformPoint(vertices[ts[i]]));
                        var b=landing.InverseTransformPoint(f.transform.TransformPoint(vertices[ts[i+1]]));
                        var c=landing.InverseTransformPoint(f.transform.TransformPoint(vertices[ts[i+2]]));
                        // Only upward road-level faces can cover the new slope.
                        if(Mathf.Max(a.y,b.y,c.y)>.23f||Mathf.Min(a.y,b.y,c.y)<.03f||Vector3.Cross(b-a,c-a).y<.0000001f)continue;
                        var p=new List<Vector3>{a,b,c};p=Clip(p,0,-1.15f,true);p=Clip(p,0,1.15f,false);p=Clip(p,2,.025f,true);p=Clip(p,2,1.30f,false);
                        if(p.Count<3)continue;
                        float area=0;for(int n=1;n<p.Count-1;n++)area+=Mathf.Abs(Vector3.Cross(p[n]-p[0],p[n+1]-p[0]).y)*.5f;
                        if(area<.000001f)continue;checkedTriangles++;
                        if(p.Any(v=>v.y>Mathf.Lerp(.055f,.14f,Mathf.Clamp01(v.z/1.35f))+.004f))
                        {bad++;if(example.Length==0)example=landing.name+" / "+f.name+" / triangle "+i/3;}
                    }
                }
            }
            record("Visible paving and stone kerb batches exclude the dropped crossing openings",bad==0&&landings.Length==36,
                "Landings "+landings.Length+"; old high-face overlaps "+bad+"; candidate clipped faces "+checkedTriangles+"; "+example);
            var ribs=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Tactile approach rib").ToArray();int floating=0;float maxGap=0;
            foreach(var f in ribs)
            {
                var bounds=f.GetComponent<MeshRenderer>().bounds;
                foreach(float z in new[]{bounds.min.z+.015f,bounds.center.z,bounds.max.z-.015f})
                foreach(float x in new[]{bounds.min.x+.005f,bounds.center.x,bounds.max.x-.005f})
                {
                    if(!Physics.Raycast(new Vector3(x,bounds.min.y+.001f,z),Vector3.down,out var hit,.2f,1,QueryTriggerInteraction.Ignore)){floating++;continue;}
                    float gap=bounds.min.y-hit.point.y;maxGap=Mathf.Max(maxGap,gap);if(gap>.004f)floating++;
                }
            }
            record("Tactile ribs rest on the finished pavement",ribs.Length==360&&floating==0,"Ribs "+ribs.Length+"; unsupported samples "+floating+"; maximum gap "+maxGap.ToString("F4")+"m");
        }
        static float Floor(float x,float z)
        {
            float floor=float.NegativeInfinity;
            foreach(var h in Physics.RaycastAll(new Vector3(x,12,z),Vector3.down,24,1,QueryTriggerInteraction.Ignore))
            {
                string n=h.collider.name;
                bool allowed=h.collider is TerrainCollider||n=="Road cell"||n=="Connected pavement"||n=="Continuous flared pavement ramp"||n.Contains("hardstanding");
                if(allowed&&h.normal.y>.7f)floor=Mathf.Max(floor,h.point.y);
            }
            return floor;
        }
        static bool OverlapXZ(Bounds a,Bounds b)=>Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x)>.005f&&Mathf.Min(a.max.z,b.max.z)-Mathf.Max(a.min.z,b.min.z)>.005f;
        static void ExistingFurnitureClearance(Transform root,Action<string,bool,string> record)
        {
            int tested=0;var collisions=new HashSet<string>();
            var checkedNames=new HashSet<string>{"Delivery crate base","Crate end","Crate upper rim","Crate open side slat","Delivery carton body","Grounded tied refuse sack","Flowerpot body","Stored workshop offcut","Folded gardening board"};
            foreach(var pocket in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("UrbanLife site - ")))
            foreach(var f in pocket.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&checkedNames.Contains(f.name)))
            {
                tested++;var meshBounds=f.sharedMesh.bounds;
                var center=f.transform.TransformPoint(meshBounds.center);
                var scale=f.transform.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
                var half=Vector3.Scale(meshBounds.extents,scale)-Vector3.one*.008f;
                half=new Vector3(Mathf.Max(.002f,half.x),Mathf.Max(.002f,half.y),Mathf.Max(.002f,half.z));
                var renderBounds=f.GetComponent<MeshRenderer>().bounds;
                foreach(var c in Physics.OverlapBox(center,half,f.transform.rotation,1,QueryTriggerInteraction.Ignore))
                {
                    if(!c.enabled||c is TerrainCollider||c.transform.IsChildOf(root))continue;
                    string n=c.name;
                    if(n=="Road cell"||n=="Connected pavement"||n=="Continuous flared pavement ramp"||n.Contains("hardstanding")||n.Contains("pavement")||n.Contains("ground apron"))continue;
                    // Resting on an old platform is allowed. Intersecting a bench
                    // seat, back, legs, old bin or building shell is a real clash.
                    if(c.bounds.max.y<=renderBounds.min.y+.008f)continue;
                    collisions.Add(pocket.name+" / "+f.name+" intersects "+c.name+" at "+c.transform.position.ToString("F2"));
                }
            }
            record("New delivery and household props clear existing benches and static furniture",tested>=40&&collisions.Count==0,
                "Authored part shapes checked "+tested+"; existing collider clashes "+collisions.Count+"; "+string.Join("; ",collisions.Take(6)));
        }
        static void LegacyRearWindowFaces(Transform root,Action<string,bool,string> record)
        {
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var legacy=root.root.Find("Static render chunks – sources retained for editing");
            if(!legacy){record("Legacy rear shutter and curtain faces removed from visible static batches",false,"Original render chunk root missing");return;}
            var batches=legacy.GetComponentsInChildren<MeshFilter>().Where(f=>
            {
                var r=f.GetComponent<MeshRenderer>();
                return r&&r.enabled&&f.sharedMesh&&f.sharedMesh.isReadable&&r.sharedMaterial&&
                    (r.sharedMaterial.name.StartsWith("FadedPaint")||r.sharedMaterial.name.StartsWith("Curtain"));
            }).ToArray();
            int houses=0,leftovers=0;string example="";
            foreach(var b in plan.buildings.Where(b=>b.kind=="housing"||b.kind=="shop"))
            {
                var house=GameObject.Find(b.id);if(!house)continue;houses++;
                foreach(var f in batches)
                {
                    var vs=f.sharedMesh.vertices;var ts=f.sharedMesh.triangles;
                    for(int i=0;i<ts.Length;i+=3)
                    {
                        var p=new List<Vector3>();
                        for(int j=0;j<3;j++)p.Add(house.transform.InverseTransformPoint(f.transform.TransformPoint(vs[ts[i+j]])));
                        if(p.Max(q=>q.z)<b.d*.5f+.05f||p.Min(q=>q.z)>b.d*.5f+.14f||p.Max(q=>q.x)<-b.w*.5f||p.Min(q=>q.x)>b.w*.5f)continue;
                        p=Clip(p,0,-b.w*.5f,true);p=Clip(p,0,b.w*.5f,false);
                        p=Clip(p,1,.2f,true);p=Clip(p,1,b.floors*3.05f,false);
                        p=Clip(p,2,b.d*.5f+.05f,true);p=Clip(p,2,b.d*.5f+.14f,false);
                        if(p.Count<3)continue;
                        float area=0;for(int j=1;j<p.Count-1;j++)area+=Vector3.Cross(p[j]-p[0],p[j+1]-p[0]).magnitude*.5f;
                        if(area<.000001f)continue;
                        leftovers++;if(example.Length==0)example=b.id+" / "+f.name+" / triangle "+i/3;
                    }
                }
            }
            record("Legacy rear shutter and curtain faces removed from visible static batches",houses==18&&batches.Length>0&&leftovers==0,
                "Housing/shop facades "+houses+"; legacy paint/curtain batches "+batches.Length+"; residual rear faces "+leftovers+"; "+example);
        }
        static void RestingDetails(Transform root,Action<string,bool,string> record)
        {
            var pockets=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("UrbanLife site - ")).ToArray();
            int bad=0,details=0,bottleIntersections=0;float maxGap=0;string example="";
            foreach(var pocket in pockets)
            {
                var p=pocket.position;float floor=Floor(p.x,p.z);
                if(float.IsNegativeInfinity(floor)||p.y-floor>.012f||p.y-floor<-.06f){bad++;if(example.Length==0)example=pocket.name+" root";}
                var parts=pocket.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()).ToArray();
                foreach(var f in parts.Where(f=>f.name=="Folded gardening board"||f.name=="Stored workshop offcut"||f.name=="Delivery crate base"||f.name=="Delivery carton body"||f.name=="Grounded tied refuse sack"||f.name=="Bicycle grounded tyre"||f.name=="Flowerpot body"))
                {
                    details++;var b=f.GetComponent<MeshRenderer>().bounds;
                    float support=Floor(b.center.x,b.center.z);
                    foreach(var other in parts)
                    {
                        if(other==f)continue;var ob=other.GetComponent<MeshRenderer>().bounds;
                        if(ob.max.y<=b.min.y+.004f&&OverlapXZ(b,ob))support=Mathf.Max(support,ob.max.y);
                    }
                    float gap=b.min.y-support;maxGap=Mathf.Max(maxGap,gap);
                    if(float.IsNegativeInfinity(support)||gap>.012f){bad++;if(example.Length==0)example=pocket.name+" / "+f.name+" gap "+gap.ToString("F4");}
                    if(f.name=="Delivery crate base")
                    foreach(var bottle in parts.Where(q=>q.name=="Returned bottle neck"))
                    {
                        var neck=bottle.GetComponent<MeshRenderer>().bounds;
                        if(OverlapXZ(b,neck)&&neck.min.y<b.max.y-.003f&&neck.max.y>b.min.y+.003f)bottleIntersections++;
                    }
                }
            }
            record("Everyday props and stacked items sit on actual supporting surfaces",pockets.Length==8&&details>=25&&bad==0&&bottleIntersections==0,
                "Sites "+pockets.Length+"; resting details "+details+"; unsupported "+bad+"; bottle/solid crate-base intersections "+bottleIntersections+"; maximum gap "+maxGap.ToString("F4")+"m; "+example);
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();
            record("Existing NPC activity places and crossing links remain present",world&&world.navigation&&world.people.Length==18&&world.crossings.Length==18&&world.places.Length>=25,
                world?"Residents "+world.people.Length+"; crossings "+world.crossings.Length+"; places "+world.places.Length:"Missing PedestrianWorld");
        }
        static void Railway(Transform root,Action<string,bool,string> record)
        {
            var signals=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Freight stop signal ")).ToArray();
            var markers=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Railway hectometre marker ")).ToArray();
            var foundations=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Embedded signal foundation").ToArray();
            var feet=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Embedded signal foundation"||f.name=="Embedded kilometre post"||f.name=="Relay cabinet seated concrete plinth").ToArray();
            int unsupported=0,gauge=0;float highestGap=0;
            foreach(var f in feet)
            {
                var b=f.GetComponent<MeshRenderer>().bounds;
                foreach(float x in new[]{b.min.x+.01f,b.center.x,b.max.x-.01f})foreach(float z in new[]{b.min.z+.01f,b.center.z,b.max.z-.01f})
                {float gap=b.min.y-WestbruchRailwayDetails.Support(x,z);highestGap=Mathf.Max(highestGap,gap);if(gap>.006f)unsupported++;}
            }
            foreach(var f in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Seated inspection bench slab"||f.name=="Seated covered cable trough"||f.name=="Signal head backplate"))
            {
                var b=f.GetComponent<MeshRenderer>().bounds;
                // Keep railway detail solids out of the freight loading envelope.
                if(b.max.x>116.20f&&b.min.x<119.80f&&b.max.y>6.43f)gauge++;
            }
            record("Signals and kilometre markers are supported clear of the freight loading gauge",signals.Length==2&&markers.Length==3&&foundations.Length==2&&unsupported==0&&gauge==0,
                "Signals "+signals.Length+"; hectometre markers "+markers.Length+"; unsupported foundation samples "+unsupported+"; gauge infringements "+gauge+"; maximum foundation gap "+highestGap.ToString("F4")+"m");
        }
    }
}
