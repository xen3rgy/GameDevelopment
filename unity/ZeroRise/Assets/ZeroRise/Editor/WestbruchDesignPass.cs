using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    public static class WestbruchDesignPass
    {
        [Serializable] class Result { public int removedLabels,removedBackings,removedCrossingBars,removedDashes,trimmedPavements,removedShopWindowParts,plates,refreshedChunks;public float hiddenFaceArea; }
        static Result result;
        static string Key(MeshFilter f)=>f.GetComponent<MeshRenderer>().sharedMaterial.name+"_"+Mathf.FloorToInt(f.transform.position.x/32)+"_"+Mathf.FloorToInt(f.transform.position.z/32);
        [MenuItem("ZERO RISE/Beschilderung und Strassenflaechen bereinigen")]
        public static void Rebuild()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();AssetDatabase.Refresh();
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            Apply(GameObject.Find("Westbruch \u2013 authored district").transform,plan);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply(Transform root,WestbruchLayout plan)
        {
            result=new Result();WestbruchSignage.Created.Clear();WestbruchSignage.ImportTextures();
            foreach(var oldSign in root.GetComponentsInChildren<MountedSign>())UnityEngine.Object.DestroyImmediate(oldSign.gameObject);
            var oldSignChunks=root.Find("Mounted signage render chunks");if(oldSignChunks)UnityEngine.Object.DestroyImmediate(oldSignChunks.gameObject);
            foreach(var b in plan.buildings){var t=root.Find(b.id);if(t&&PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.UnpackPrefabInstance(t.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}
            var chunks=root.Find("Static render chunks \u2013 sources retained for editing");
            var sources=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.isStatic&&f.sharedMesh&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()&&!f.GetComponent<MeshRenderer>().enabled&&!f.GetComponentInParent<LODGroup>()&&(!chunks||!f.transform.IsChildOf(chunks))).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.ToList());
            var originalCounts=sources.ToDictionary(g=>g.Key,g=>g.Value.Sum(f=>f.sharedMesh.vertexCount));
            var affected=new HashSet<string>();
            void Touch(MeshFilter f){if(f&&!f.GetComponent<MeshRenderer>().enabled&&chunks&&chunks.Find(Key(f)))affected.Add(Key(f));}
            void Remove(GameObject o){foreach(var f in o.GetComponentsInChildren<MeshFilter>())Touch(f);UnityEngine.Object.DestroyImmediate(o);}
            foreach(var b in plan.buildings.Where(b=>b.kind=="shop"))
            {
                var shop=root.Find(b.id);var windowNames=new[]{"Recessed glass","Window jamb","Window lintel","Mullion","Projecting sill"};
                foreach(var f in shop.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform.parent==shop&&windowNames.Contains(f.name)&&f.transform.localPosition.y<2.7f&&f.transform.localPosition.z<-b.d*.5f).ToArray())
                {Remove(f.gameObject);result.removedShopWindowParts++;}
            }
            foreach(var text in root.GetComponentsInChildren<TextMesh>()){result.removedLabels++;Remove(text.gameObject);}
            foreach(var f in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Sign backing"||f.name=="Old road sign post"||f.name=="Worn pedestrian crossing").ToArray())
            {if(f.name=="Sign backing")result.removedBackings++;if(f.name=="Worn pedestrian crossing")result.removedCrossingBars++;Remove(f.gameObject);}
            var bars=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Faded pedestrian crossing").ToArray();
            foreach(var dash in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Worn lane marking").ToArray())
            {
                var bounds=dash.GetComponent<Renderer>().bounds;bounds.Expand(new Vector3(.04f,.1f,.04f));
                if(bars.Any(b=>bounds.Intersects(b.GetComponent<Renderer>().bounds))){Remove(dash.gameObject);result.removedDashes++;}
            }
            var paint=WestbruchSignage.FaceMaterial("road_paint");paint.SetFloat("_Metallic",0);paint.SetFloat("_Smoothness",.12f);HDMaterial.ValidateMaterial(paint);
            foreach(var f in bars)
            {
                Touch(f);var bounds=f.sharedMesh.bounds;float x=bounds.size.x*.5f,z=bounds.size.z*.5f;
                var mesh=new Mesh{vertices=new[]{new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
                mesh.RecalculateNormals();mesh.RecalculateTangents();f.sharedMesh=SaveMesh(mesh,"Crossing_"+x+"_"+z);var p=f.transform.position;p.y=.050f;f.transform.position=p;
                // These bars were authored after the original static batch.
                var renderer=f.GetComponent<MeshRenderer>();renderer.sharedMaterial=paint;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Physics.SyncTransforms();var kerbs=root.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Kerb").Select(c=>c.bounds).ToArray();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Connected pavement").ToArray())if(TrimPavement(f,kerbs)){Touch(f);result.trimmedPavements++;}
            foreach(string materialName in new[]{"Paving"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/"+materialName+".mat");if(!material)continue;
                material.SetFloat("_UVBase",5);material.SetFloat("_TexWorldScale",1f/3);HDMaterial.ValidateMaterial(material);EditorUtility.SetDirty(material);
                foreach(var texture in new[]{material.GetTexture("_BaseColorMap"),material.GetTexture("_NormalMap")})if(texture)
                {var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;if(importer){importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.SaveAndReimport();}}
            }
            foreach(var key in affected)
            {
                var chunk=chunks.Find(key);if(originalCounts[key]!=chunk.GetComponent<MeshFilter>().sharedMesh.vertexCount)throw new Exception("Static source mismatch: "+key);var list=sources[key].Where(f=>f).ToArray();if(list.Length==0){UnityEngine.Object.DestroyImmediate(chunk.gameObject);continue;}
                var mesh=chunk.GetComponent<MeshFilter>().sharedMesh;mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);EditorUtility.SetDirty(mesh);result.refreshedChunks++;
            }
            BuildSigns(root,plan);result.plates=WestbruchSignage.Created.Count;
            // Reusable building assets must contain the same mounted lettering, with
            // visible source meshes. Scene batching stays disabled on these sources.
            foreach(var building in plan.buildings)
            {
                var t=root.Find(building.id);var renderers=t.GetComponentsInChildren<MeshRenderer>();var enabled=renderers.Select(r=>r.enabled).ToArray();
                foreach(var r in renderers)r.enabled=true;
                PrefabUtility.SaveAsPrefabAsset(t.gameObject,Generated+"/Prefabs/"+building.id+".prefab");
                for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
            }
            BatchSigns(root);
            if(root.GetComponentsInChildren<TextMesh>().Length!=0)throw new Exception("Legacy floating labels remain");
            Physics.SyncTransforms();Directory.CreateDirectory("QA");File.WriteAllText("QA/design-repair.json",JsonUtility.ToJson(result,true));Debug.Log("Design repair: "+JsonUtility.ToJson(result));
        }
        static Mesh SaveMesh(Mesh mesh,string key)
        {
            string path=Generated+"/Meshes/Design_"+key.Replace(',','_')+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static bool TrimPavement(MeshFilter f,Bounds[] kerbs)
        {
            // Partition every vertical face at actual kerb edges. Preserve the top and collider.
            var old=f.sharedMesh;var verts=old.vertices;var normals=old.normals;var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();bool changed=false;
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=d;d=swap;}
                int i=vs.Count;foreach(var world in new[]{a,b,c,d}){var p=f.transform.InverseTransformPoint(world);vs.Add(p);uv.Add(Mathf.Abs(normal.y)>.9f?new Vector2(world.x,world.z)/3:Mathf.Abs(normal.x)>.9f?new Vector2(world.z,world.y)/3:new Vector2(world.x,world.y)/3);}ts.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            for(int i=0;i<verts.Length;i+=4)
            {
                var v=Enumerable.Range(i,4).Select(j=>f.transform.TransformPoint(verts[j])).ToArray();var n=f.transform.TransformDirection(normals[i]);
                if(Mathf.Abs(n.y)>.9f){Quad(v[0],v[1],v[2],v[3],n);continue;}
                bool x=Mathf.Abs(n.x)>.9f;float plane=x?v[0].x:v[0].z,lo=v.Min(p=>x?p.z:p.x),hi=v.Max(p=>x?p.z:p.x),bottom=v.Min(p=>p.y),top=v.Max(p=>p.y);
                var covers=kerbs.Where(k=>Mathf.Abs((x?(n.x<0?k.min.x:k.max.x):(n.z<0?k.min.z:k.max.z))-plane)<.0005f&&(x?k.max.z:k.max.x)>lo+.0001f&&(x?k.min.z:k.min.x)<hi-.0001f).ToArray();
                if(covers.Length==0){Quad(v[0],v[1],v[2],v[3],n);continue;}
                var cuts=new SortedSet<float>{lo,hi};foreach(var k in covers){cuts.Add(Mathf.Clamp(x?k.min.z:k.min.x,lo,hi));cuts.Add(Mathf.Clamp(x?k.max.z:k.max.x,lo,hi));}var points=cuts.ToArray();
                for(int j=0;j<points.Length-1;j++)
                {
                    float a=points[j],b=points[j+1],mid=(a+b)*.5f,y=bottom;
                    foreach(var k in covers)if(mid>(x?k.min.z:k.min.x)&&mid<(x?k.max.z:k.max.x))y=Mathf.Max(y,Mathf.Min(top,k.max.y));
                    if(y>bottom+.00001f){changed=true;result.hiddenFaceArea+=(b-a)*(y-bottom);}if(top-y<.00001f)continue;
                    Vector3 V(float along,float height)=>x?new Vector3(plane,height,along):new Vector3(along,height,plane);
                    Quad(V(a,y),V(a,top),V(b,top),V(b,y),n);
                }
            }
            if(!changed)return false;var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            f.sharedMesh=SaveMesh(mesh,FormattableString.Invariant($"Pavement_{f.transform.position.x:0.####}_{f.transform.position.z:0.####}"));return true;
        }
        static void BuildSigns(Transform root,WestbruchLayout plan)
        {
            void Plate(string key,string text,Transform parent,Vector3 p,float w,float h,Transform support,string mount,float yaw=0,bool round=false)=>WestbruchSignage.Plate(key,text,parent,p,new Vector2(w,h),support,mount,yaw,round);
            foreach(var b in plan.buildings)
            {
                var t=root.Find(b.id);var shell=t.Find("Structural shell");string number=b.kind=="shop"?"5":b.kind=="workshop"?"9":b.kind=="garage"?"7":b.id.Split('_').Last();
                float numberX=b.kind=="garage"?0:b.kind=="workshop"?b.w/6:-.98f;
                Plate("number_"+number,number,t,new Vector3(numberX,1.9f,-b.d*.5f-.048f),.24f,.19f,shell,"Facade screw anchors");
                if(b.kind=="shop") {Plate("shop","PFAND & SPAR",t,new Vector3(0,3.16f,-b.d*.5f-.14f),10.5f,.6f,shell,"Shop fascia brackets");Plate("shop_goods","Getraenke - Pfand - Lebensmittel",t,new Vector3(0,3.62f,-b.d*.5f-.14f),10.5f,.28f,shell,"Shop fascia brackets");}
                if(b.kind=="workshop") {Plate("service","Westbruch Service & Reparatur",t,new Vector3(0,3.7f,-b.d*.5f-.14f),5.6f,.5f,shell,"Workshop facade anchors");Plate("deliveries","Anlieferung - Hof freihalten",t,new Vector3(0,2.9f,-b.d*.5f-.14f),1.8f,.28f,shell,"Workshop facade anchors");}
                if(b.kind=="garage")Plate("garage","Nur fuer Mieter",t,new Vector3(0,2.86f,-b.d*.5f-.08f),1.25f,.25f,shell,"Brickwork screw anchors");
            }
            foreach(var t in root.GetComponentsInChildren<Transform>().ToArray())
            {
                if(t.name=="Corrugated parts container")Plate("storage","Teilelager",t,new Vector3(-.99f,1.85f,-3.608f),.5f,.18f,t,"Container door rivets");
                if(t.name=="Next district construction boundary")
                {
                    var p=t.position;p.z=Mathf.Sign(p.z)*113.46f;t.position=p;
                    var fence=root.GetComponentsInChildren<Transform>().Where(c=>c.name=="Fence collider rail").OrderBy(c=>Mathf.Abs(c.position.z-p.z)).First();
                    Plate("building_site","Bauarbeiten - Zutritt verboten",t,new Vector3(0,.85f,-.04f),1.25f,.6f,fence,"Fence mounting clips");
                }
                if(t.name=="Old Westbruch bus shelter")
                {
                    var pole=t.GetComponentsInChildren<Transform>().FirstOrDefault(c=>c.name=="Bus stop signpost")??t;
                    Plate("bus_h","H",t,new Vector3(-2.7f,3.1f,-.035f),.52f,.52f,pole,"Bus pole clamp",0,true);
                    Plate("bus_name","Westbruch",t,new Vector3(-2.7f,2.45f,-.045f),.85f,.17f,pole,"Bus pole clamp");
                    Plate("bus_line","4 - Stadtmitte",t,new Vector3(0,2.4f,.74f),2,.20f,t,"Shelter rear rail");
                    Plate("timetable","Fahrplan Linie 4",t,new Vector3(1.62f,1.6f,.644f),.35f,.5f,t,"Timetable display case");
                }
            }
            // Clamp street plates to existing lighting poles: no new obstacles in walking routes.
            var lamps=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Old European streetlight ")).ToArray();
            var used=new HashSet<Transform>();
            for(int r=0;r<plan.roads.Length;r++)
            {
                var road=plan.roads[r];var target=new Vector3(road.x,0,road.z);var lamp=lamps.Where(l=>!used.Contains(l)&&(road.w>road.d?Mathf.Abs(Mathf.Abs(l.position.z-road.z)-road.d*.5f)<3.1f:Mathf.Abs(Mathf.Abs(l.position.x-road.x)-road.w*.5f)<3.1f)).OrderBy(l=>Vector2.Distance(new Vector2(l.position.x,l.position.z),new Vector2(target.x,target.z))).FirstOrDefault();
                if(!lamp)continue;used.Add(lamp);Plate("street_"+r,r==5?"Gleisdamm":road.id,lamp,new Vector3(0,2.55f,.07f),1.6f,.28f,lamp,"Steel pole clamp",180);
            }
        }
        static void BatchSigns(Transform root)
        {
            var chunks=Group("Mounted signage render chunks",root,Vector3.zero).transform;
            var groups=WestbruchSignage.Created.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>()).GroupBy(Key);
            foreach(var group in groups)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);
                var chunk=Group(group.Key,chunks,Vector3.zero);chunk.isStatic=true;chunk.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,"SignChunk_"+group.Key);var renderer=chunk.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.First().GetComponent<MeshRenderer>().sharedMaterial;
                if(group.Key.StartsWith("Signage_Face"))renderer.shadowCastingMode=ShadowCastingMode.Off;
                foreach(var f in group)f.GetComponent<MeshRenderer>().enabled=false;
            }
        }
        [Serializable] class Label {public string text,parent;public Vector3 position,size;public float characterSize;public int backings;}
        [Serializable] class AuditReport {public List<Label> labels=new List<Label>();public int kerbs,pavements,legacyCrosswalks,residentCrosswalks;}
        public static void Audit()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Physics.SyncTransforms();var report=new AuditReport();
            foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
                report.labels.Add(new Label{text=label.text,parent=label.transform.parent?label.transform.parent.name:"",position=label.transform.position,size=label.GetComponent<Renderer>().bounds.size,characterSize=label.characterSize,backings=label.transform.parent.GetComponentsInChildren<MeshFilter>().Count(f=>f.name=="Sign backing"&&Vector3.Distance(f.transform.position,label.transform.position)<.15f)});
            }
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {if(t.name=="Kerb")report.kerbs++;if(t.name=="Connected pavement")report.pavements++;if(t.name=="Worn pedestrian crossing")report.legacyCrosswalks++;if(t.name=="Faded pedestrian crossing")report.residentCrosswalks++;}
            Directory.CreateDirectory("QA");File.WriteAllText("QA/design-baseline.json",JsonUtility.ToJson(report,true));Debug.Log("Design baseline: "+report.labels.Count+" labels, "+report.legacyCrosswalks+" old crossing bars, "+report.residentCrosswalks+" new bars, "+report.kerbs+" kerbs.");
        }
    }
}
