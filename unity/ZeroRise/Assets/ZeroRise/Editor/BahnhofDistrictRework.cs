using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    // Rebuild only the extension's visual sources; preserve the original district and its railway.
    public static class BahnhofDistrictRework
    {
        static bool buildAfterApply;
        public static void ApplyAndBuild(){buildAfterApply=true;Apply();}
        static Transform root;static readonly List<string> checks=new List<string>();static bool passed;
        static readonly List<Rect> roads=new List<Rect>(),walks=new List<Rect>();
        static Rect R(float x,float z,float w,float d)=>new Rect(x-w*.5f,z-d*.5f,w,d);
        [MenuItem("ZERO RISE/Bahnhofsviertel/Rebuild finished architecture and streets")]
        public static void Apply()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();checks.Clear();passed=true;
                root=GameObject.Find(BahnhofsviertelBuilder.RootName).transform;
                UnpackSources();RemoveConflictingBackdrop();ClearLegacySigns();ContinuousSurfaces();
                BahnhofResidentialArchitecture.Rebuild(root);BahnhofStationArchitecture.Rebuild(root);
                DressStreet();BahnhofLandscapeFinish.Apply(root);BahnhofSquareFinish.Apply(root);WestbruchAuditCompletion.Apply(root);FixVehicles();RefreshNavigation();Physics.SyncTransforms();
                foreach(var fitted in root.GetComponentsInChildren<FittedWorldSign>())fitted.Refit();
                BahnhofVisualQualityQA.Validate(root,Record);BahnhofPlacementQA.Validate(root,Record);if(!passed)throw new Exception("Visual authoring checks failed: QA/bahnhof-rework-authoring.txt");
                Bake();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                File.WriteAllText("QA/bahnhof-rework-authoring.txt","Passed: "+passed+"\n"+string.Join("\n",checks));
                Debug.Log("Bahnhof rework saved: individual architecture, supported lettering and continuous street surfaces");
                if(buildAfterApply)WestbruchMacBuild.Build();else if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("QA/bahnhof-rework-authoring.txt","Passed: False\n"+string.Join("\n",checks)+"\n"+e);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        public static void FinishDetailsAndBuild()=>ApplyAndBuild();
        public static void ValidateSavedScene()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);checks.Clear();passed=true;
                root=GameObject.Find(BahnhofsviertelBuilder.RootName).transform;Physics.SyncTransforms();
                BahnhofVisualQualityQA.Validate(root,Record);BahnhofPlacementQA.Validate(root,Record);
                File.WriteAllText("QA/bahnhof-complete-placement.txt","Passed: "+passed+"\n"+string.Join("\n",checks));
                EditorApplication.Exit(passed?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Record(string title,bool ok,string detail){passed&=ok;checks.Add((ok?"PASS ":"FAIL ")+title+" | "+detail);}
        static void RemoveConflictingBackdrop()
        {
            var town=root.root;var skyline=town.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Neighbouring skyline – visual background only");
            if(!skyline)return;
            var blocks=skyline.Cast<Transform>().Where(t=>t.position.x>140&&t.position.z>118&&t.position.z<364).ToArray();
            var cuts=new List<Bounds>();
            foreach(var block in blocks)
            {
                var renderers=block.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)continue;
                var bound=renderers[0].bounds;foreach(var r in renderers)bound.Encapsulate(r.bounds);bound.Expand(.06f);cuts.Add(bound);
            }
            // These old non-colliding skyline blocks had already been baked into
            // global render chunks. Deleting only their editable sources leaves
            // the visible blocks intersecting the new freight hall and houses.
            var chunks=town.Find("Static render chunks – sources retained for editing");
            if(chunks)foreach(var f in chunks.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.sharedMaterial||!cuts.Any(c=>c.Intersects(r.bounds)))continue;
                if(!r.sharedMaterial.name.StartsWith("Backdrop")&&r.sharedMaterial.name!="DressingMetal")continue;
                var mesh=f.sharedMesh;var v=mesh.vertices;var old=mesh.triangles;var keep=new List<int>();
                for(int i=0;i<old.Length;i+=3)
                {var at=f.transform.TransformPoint((v[old[i]]+v[old[i+1]]+v[old[i+2]])/3);if(!cuts.Any(c=>c.Contains(at)))keep.AddRange(new[]{old[i],old[i+1],old[i+2]});}
                if(keep.Count==old.Length)continue;mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                if(keep.Count==0)UnityEngine.Object.DestroyImmediate(f.gameObject);
            }
            foreach(var block in blocks)UnityEngine.Object.DestroyImmediate(block.gameObject);
        }
        static void DestroyNamed(params string[] names)
        {
            var set=new HashSet<string>(names);foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>set.Contains(t.name)).ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        static void UnpackSources()
        {
            DestroyNamed("Bahnhofsplatz parked car","Resident parked car","BV2_Parked station car","BV2_Parked resident car");
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Bahnhof render chunk ")||t.name.StartsWith("BV2_Render chunk ")).ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
                if(f.GetComponent<MeshRenderer>()&&!f.GetComponentInParent<LODGroup>()&&!f.GetComponentInParent<PedestrianAgent>()&&!f.GetComponentInParent<SimpleCar>())f.GetComponent<MeshRenderer>().enabled=true;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Rear property fence collision"))r.enabled=false;
            DestroyNamed("BV2_Street life","BV2_Continuous surfaces","BV2_Surface markings","BV2_Street plants");
        }
        static void ClearLegacySigns()
        {
            foreach(var fitted in root.GetComponentsInChildren<FittedWorldSign>().ToArray())if(fitted)UnityEngine.Object.DestroyImmediate(fitted.gameObject);
            foreach(var sign in root.GetComponentsInChildren<MountedSign>().ToArray())if(sign)UnityEngine.Object.DestroyImmediate(sign.gameObject);
            foreach(var label in root.GetComponentsInChildren<TextMesh>().Where(t=>!t.GetComponentInParent<FittedWorldSign>()).ToArray())if(label)UnityEngine.Object.DestroyImmediate(label.gameObject);
            DestroyNamed("Sign backing","Station kiosk","Bahnhofsplatz bus shelter","Wayfinding signpost");
        }
        static bool Has(List<Rect> list,Vector2 p)=>list.Any(r=>r.Contains(p));
        static bool Development(Vector2 p)=>p.x>=166&&p.x<=300&&p.y>=58&&p.y<=364||p.x>=132&&p.x<=300&&p.y>=118&&p.y<=364;
        static void ContinuousSurfaces()
        {
            DestroyNamed("District supporting earth","Eastern connecting ground","Bahnhof connected pavement","Bahnhof connected carriageway","Flush junction","Station square paving","Bahnhofsplatz access carriageway","Taxi waiting bay","Güterhof accessible courtyard","Courtyard east entrance","Rail bank pedestrian shortcut","Shortcut south link","Shortcut station link","Station road kerb","Worn centre marking","Station pedestrian crossing","Taxi bay separator","Ramp lower approach","Station rear access");
            roads.Clear();walks.Clear();
            roads.AddRange(new[]{R(184,80,48,10),R(208,193,10,226),R(236,176,56,9),R(236,306,56,9),R(264,241,9,130),R(189,266,38,9),R(190,280,13,10),R(278.5f,176,15,8),R(264,326.5f,9,27)});
            foreach(var p in new[]{new Vector2(208,80),new Vector2(208,176),new Vector2(208,266),new Vector2(208,306),new Vector2(264,176),new Vector2(264,306)})roads.Add(R(p.x,p.y,14,14));
            walks.AddRange(new[]{R(184,80,48,16),R(208,193,16,226),R(236,176,56,15),R(236,306,56,15),R(264,241,15,130),R(174,264,52,64),R(174,198,42,24),R(196,198,12,5),R(140,174,3.2f,108),R(161,120,45,3.2f),R(145,227,12,3.2f),R(135,130,8,4),R(129,263,6,42),R(278.5f,176,15,13),R(264,326.5f,15,27),R(219.5f,80,9,22)});
            // Close the two returns beside the hall, stopping exactly at its
            // z248/282 floor edges instead of leaving a strip open to the sky.
            walks.AddRange(new[]{R(133,243.25f,2,9.5f),R(133,283,2,2)});
            // Front zones are deliberately paved up to shopfronts, with planting pockets instead of empty eight-metre strips.
            foreach(var p in new[]{new Vector2(234,116),new Vector2(234,147),new Vector2(234,202),new Vector2(234,233),new Vector2(234,275)})walks.Add(R(223,p.y,10,24));
            foreach(float z in new[]{177f,220})walks.Add(R(194,z,14,24));
            var terrain=Terrain.activeTerrain;var data=terrain.terrainData;int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;
            for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
            {float x=origin.x+ix/(n-1f)*data.size.x,z=origin.z+iz/(n-1f)*data.size.z;if(Development(new Vector2(x,z)))heights[iz,ix]=(-.12f-origin.y)/data.size.y;}
            data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);
            // One disjoint planar subdivision, not stacked pavement rectangles.
            var xs=new SortedSet<float>{132,134,166,300};var zs=new SortedSet<float>{58,118,364};
            foreach(var r in roads.Concat(walks)){xs.Add(r.xMin);xs.Add(r.xMax);zs.Add(r.yMin);zs.Add(r.yMax);}
            for(float x=132;x<300;x+=4)xs.Add(x);for(float z=58;z<364;z+=4)zs.Add(z);
            var xx=xs.ToArray();var zz=zs.ToArray();var cells=new Dictionary<string,List<Rect>>();
            for(int ix=0;ix<xx.Length-1;ix++)for(int iz=0;iz<zz.Length-1;iz++)
            {
                var cell=Rect.MinMaxRect(xx[ix],zz[iz],xx[ix+1],zz[iz+1]);var p=cell.center;
                // Leave the old underpass/court surfaces in charge west of x163.
                if(p.x<160&&p.y<118||!Development(p)&&!Has(roads,p)&&!Has(walks,p))continue;
                string type=Has(roads,p)?"road":Has(walks,p)?"walk":"earth";
                string key=type+"_"+Mathf.FloorToInt(p.x/40)+"_"+Mathf.FloorToInt(p.y/40);
                if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<Rect>();list.Add(cell);
            }
            var surfaces=Group("BV2_Continuous surfaces",root,Vector3.zero).transform;
            foreach(var pair in cells)
            {
                string type=pair.Key.Split('_')[0];float top=type=="road"?.04f:type=="walk"?.14f:-.055f;
                var mat=BahnhofDetailKit.Load(type=="road"?"Asphalt":type=="walk"?"Paving":"OverpassGround_LivingEarth");
                var v=new List<Vector3>();var uv=new List<Vector2>();var uv1=new List<Vector2>();var colors=new List<Color>();var tri=new List<int>();
                foreach(var r in pair.Value)
                {
                    int a=v.Count;
                    foreach(var p in new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin)})
                    {v.Add(new Vector3(p.x,top,p.y));uv.Add(p/3);uv1.Add(p);float grass=Mathf.SmoothStep(0,.85f,Mathf.PerlinNoise(p.x*.09f,p.y*.12f));colors.Add(new Color(grass,.25f*(1-grass),0,1));}
                    tri.AddRange(new[]{a,a+1,a+2,a,a+2,a+3});
                    // Close the visible step between the disjoint height levels.
                    float Neighbour(Vector2 p)
                    {if(p.x<160&&p.y<118)return -.12f;if(!Development(p)&&!Has(roads,p)&&!Has(walks,p))return -.25f;return Has(roads,p)?.04f:Has(walks,p)?.14f:-.055f;}
                    var corners=new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin)};
                    for(int edge=0;edge<4;edge++)
                    {
                        var from=corners[edge];var to=corners[(edge+1)%4];var delta=to-from;var outward=new Vector2(-delta.y,delta.x).normalized;
                        float below=Neighbour((from+to)*.5f+outward*.001f);if(top<=below+.001f)continue;
                        int b=v.Count;
                        foreach(var point in new[]{new Vector3(from.x,top,from.y),new Vector3(to.x,top,to.y),new Vector3(to.x,below,to.y),new Vector3(from.x,below,from.y)})
                        {v.Add(point);uv.Add(new Vector2(point.x+point.z,point.y)/3);uv1.Add(new Vector2(point.x,point.z));colors.Add(new Color(.2f,.2f,0,1));}
                        tri.AddRange(new[]{b,b+2,b+1,b,b+3,b+2});
                    }
                }
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv1);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                string path=Generated+"/Meshes/BV2_Surface_"+pair.Key+".asset";SaveMesh(mesh,path,out mesh);
                var go=Group("BV2_Surface "+pair.Key,surfaces,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;go.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
            SurfaceDetails();
        }
        static void SaveMesh(Mesh source,string path,out Mesh result)
        {var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(source,old);UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(old);result=old;}else{AssetDatabase.CreateAsset(source,path);result=source;}}
        static void SurfaceDetails()
        {
            var detail=Group("BV2_Surface markings",root,Vector3.zero).transform;var paint=BahnhofDetailKit.Load("FadedRoadPaint");var kerb=BahnhofDetailKit.Load("PropConcrete");
            // Broken lines terminate before crossings/junctions.
            for(float z=100;z<300;z+=6)if(new[]{151f,176,228,266,294}.All(j=>Mathf.Abs(j-z)>7))Box("BV2_Centre paint marking",detail,new Vector3(208,.046f,z),new Vector3(.10f,.006f,2.4f),paint,false);
            for(float x=169;x<200;x+=6)Box("BV2_Approach paint marking",detail,new Vector3(x,.046f,80),new Vector3(2.3f,.006f,.1f),paint,false);
            foreach(float z in new[]{151f,228,294})for(int i=0;i<9;i++)Box("BV2_Crossing paint marking",detail,new Vector3(204+i*.95f,.046f,z),new Vector3(.44f,.006f,2.5f),paint,false);
            foreach(float x in new[]{202.92f,213.08f})foreach(var range in new[]{new Vector2(98,146),new Vector2(156,168),new Vector2(184,223),new Vector2(233,258),new Vector2(274,289),new Vector2(299,303)})Box("BV2_Kerb",detail,new Vector3(x,.08f,(range.x+range.y)*.5f),new Vector3(.16f,.16f,range.y-range.x),kerb,true);
            var metal=BahnhofDetailKit.Load("DressingMetal");
            foreach(float z in new[]{133f,188,218,248,284})foreach(float x in new[]{202.65f,213.35f})
            {
                Box("BV2_Drain surround",detail,new Vector3(x,.146f,z),new Vector3(.62f,.012f,.36f),metal,false);
                for(int i=0;i<7;i++)Box("BV2_Drain slot",detail,new Vector3(x-.24f+i*.08f,.153f,z),new Vector3(.035f,.006f,.29f),BahnhofDetailKit.Load("Rubber"),false);
            }
            var plan=new WestbruchLayout{roads=new[]{new Plot{id="Bahnhofstrasse",x=208,z=193,w=10,d=226},new Plot{id="Gueterstrasse",x=184,z=80,w=48,d=10},new Plot{id="HintereGasse",x=264,z=241,w=9,d=130}}};
            WestbruchRoadWear.Build(detail,plan);
        }
        static void DressStreet()
        {
            var life=Group("BV2_Street life",root,Vector3.zero).transform;
            var metal=BahnhofDetailKit.Load("DressingMetal");var wood=BahnhofDetailKit.Load("UrbanLife_AgedTimber");var stone=BahnhofDetailKit.Load("PropConcrete");
            var green=Material("BV2_KioskGreen",null,new Color(.23f,.29f,.24f),.17f,.10f);
            var glass=Material("BV2_KioskGlass",null,new Color(.15f,.22f,.22f),.48f,.15f);
            var kiosk=Group("BV2_Station kiosk",life,new Vector3(188,.14f,239),90).transform;
            Box("BV2_Kiosk back",kiosk,new Vector3(0,1.4f,1.9f),new Vector3(6,2.8f,.2f),green);
            foreach(float x in new[]{-2.9f,2.9f})Box("BV2_Kiosk side",kiosk,new Vector3(x,1.4f,0),new Vector3(.2f,2.8f,4),green);
            Box("BV2_Kiosk counter base",kiosk,new Vector3(0,.5f,-1.9f),new Vector3(6,1,.2f),green);
            Box("BV2_Kiosk front beam",kiosk,new Vector3(0,2.5f,-1.9f),new Vector3(6,.6f,.2f),green);
            Box("BV2_Kiosk service recess",kiosk,new Vector3(0,1.6f,-1.72f),new Vector3(5.5f,1.2f,.035f),glass,false);
            Box("BV2_Kiosk counter sill",kiosk,new Vector3(0,1.05f,-2.08f),new Vector3(5.7f,.10f,.45f),wood);
            Box("BV2_Kiosk roof",kiosk,new Vector3(0,2.94f,0),new Vector3(6.5f,.2f,4.7f),metal);
            BahnhofDetailKit.Plate(kiosk,"KIOSK · ZEITUNGEN",new Vector3(0,2.52f,-2.035f),0,4.8f,.43f,green,new Color(.83f,.79f,.65f));
            var bus=Group("BV2_Bus shelter",life,new Vector3(199,.14f,251),90).transform;
            foreach(float x in new[]{-2.5f,2.5f})foreach(float z in new[]{-.7f,.7f})Cylinder("BV2_Shelter steel post",bus,new Vector3(x,1.25f,z),.048f,2.5f,metal,true);
            Box("BV2_Shelter folded roof",bus,new Vector3(0,2.55f,0),new Vector3(5.7f,.15f,2.2f),metal);
            foreach(float x in new[]{-2.5f,2.5f})Beam("BV2_Shelter diagonal brace",bus,new Vector3(x,1.9f,.7f),new Vector3(x,2.45f,-.4f),.045f,metal);
            Box("BV2_Shelter sign mounting rail",bus,new Vector3(0,2.25f,.70f),new Vector3(5.0f,.08f,.10f),metal);
            WestbruchProps.Bench(bus,Vector3.zero,0);
            BahnhofDetailKit.Plate(bus,"BAHNHOF WESTBRUCH",new Vector3(0,2.25f,.63f),0,2.8f,.32f,green,new Color(.86f,.83f,.73f));
            var timetable=Group("BV2_Timetable support",bus,new Vector3(2.15f,0,.70f)).transform;
            foreach(float y in new[]{1.2f,1.65f})Box("BV2_Timetable mounting console",bus,new Vector3(2.38f,y,.70f),new Vector3(.3f,.05f,.07f),metal);
            Box("BV2_Timetable cabinet",timetable,new Vector3(0,1.4f,0),new Vector3(.48f,.70f,.10f),metal,false);
            BahnhofDetailKit.Plate(timetable,"BUS 12\nSTADTMITTE",new Vector3(0,1.4f,-.07f),0,.41f,.59f,stone,new Color(.10f,.15f,.14f));
            foreach(var p in new[]{new Vector3(198,.14f,110),new Vector3(200,.14f,234)})
            {
                var post=Group("BV2_Street directions",life,p).transform;Cylinder("BV2_Direction post",post,Vector3.up*1.55f,.04f,3.1f,metal,true);
                BahnhofDetailKit.Plate(post,p.z<200?"BAHNHOF ↑":"WESTBRUCH ↓",new Vector3(0,2.7f,-.05f),0,1.6f,.32f,green,new Color(.84f,.84f,.76f));
            }
            foreach(var lamp in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Old European streetlight ")))
            {
                if(Mathf.Abs(lamp.position.z-132)<.1f&&lamp.position.x<208)
                    BahnhofDetailKit.Plate(lamp,"Bahnhofstraße",new Vector3(0,2.7f,-.12f),0,1.2f,.22f,green,new Color(.87f,.88f,.81f));
            }
            // Low planted forecourts give each frontage a clear address and keep the entrances open.
            foreach(float z in new[]{116f,147,202,233,275})foreach(float dz in new[]{-8f,8})
            {
                var bed=Group("BV2_Raised front planting",life,new Vector3(223,.14f,z+dz)).transform;
                var shrub=UnityEngine.Object.FindObjectsByType<LODGroup>().FirstOrDefault(g=>g.name.StartsWith("Dense low courtyard shrub"));
                if(shrub)foreach(float offset in new[]{-1f,0,1})
                {
                    var planted=UnityEngine.Object.Instantiate(shrub.gameObject,bed);planted.name="BV2_Front garden shrub";planted.transform.localPosition=new Vector3(offset,.30f,0);planted.transform.localScale=Vector3.one*.48f;
                }
                Box("BV2_Planter retained soil",bed,new Vector3(0,.15f,0),new Vector3(3.3f,.3f,2.6f),BahnhofDetailKit.Load("GardenEarth"),true);
                foreach(float x in new[]{-1.8f,1.8f})Box("BV2_Planter stone end",bed,new Vector3(x,.25f,0),new Vector3(.23f,.5f,3),stone);
                foreach(float zz in new[]{-1.4f,1.4f})Box("BV2_Planter wall",bed,new Vector3(0,.25f,zz),new Vector3(3.8f,.5f,.23f),stone);
            }
            // The courtyard has a use: small repair work, household storage and a shared seating corner.
            var yard=Group("BV2_Gueterhof repair corner",life,new Vector3(157,.14f,202)).transform;
            foreach(float x in new[]{-.65f,.65f})Box("BV2_Workbench leg",yard,new Vector3(x,.43f,0),new Vector3(.10f,.86f,.65f),metal);
            Box("BV2_Workbench top",yard,new Vector3(0,.9f,0),new Vector3(1.7f,.09f,.8f),wood);
            for(int i=0;i<3;i++)Box("BV2_Repair tool case",yard,new Vector3(-.5f+i*.4f,1.03f,.08f),new Vector3(.27f,.17f,.3f),metal,false);
            WestbruchProps.Pallet(life,new Vector3(158,.14f,190));WestbruchProps.Bin(life,new Vector3(185,.14f,205),0);
            var bike=root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Station commuter bicycle");
            if(bike){var clone=UnityEngine.Object.Instantiate(bike.gameObject,yard);clone.name="BV2_Repair bicycle";clone.transform.SetPositionAndRotation(new Vector3(159,.14f,202),Quaternion.Euler(0,30,0));}
            RearStationApproach(life);Physics.SyncTransforms();GreenPockets();TrimDistantApron();
        }
        static void RearStationApproach(Transform life)
        {
            // Cut a retained passenger approach into the bank, so the hall exit
            // opens onto pavement and a real stairway instead of straight into earth.
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Continuous North railway earth bank"||f.name=="Local render batch OverpassGround_LivingEarth"))
            {
                var mesh=f.sharedMesh;var vertices=mesh.vertices;var old=mesh.triangles;var keep=new List<int>();
                for(int i=0;i<old.Length;i+=3)
                {
                    var at=f.transform.TransformPoint((vertices[old[i]]+vertices[old[i+1]]+vertices[old[i+2]])/3);
                    if(at.x>=125.8f&&at.z>238.5f&&at.z<284.5f)continue;
                    keep.AddRange(new[]{old[i],old[i+1],old[i+2]});
                }
                mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);var c=f.GetComponent<MeshCollider>();if(c){c.sharedMesh=null;c.sharedMesh=mesh;}
            }
            var concrete=BahnhofDetailKit.Load("Overpass_RetainingConcrete");var cap=BahnhofDetailKit.Load("StoneTrim");
            Box("BV2_Station approach retaining wall",life,new Vector3(126,2.1f,261.5f),new Vector3(.45f,4.2f,46),concrete,true,2.5f);
            Box("BV2_Retaining wall stone cap",life,new Vector3(126,4.24f,261.5f),new Vector3(.60f,.15f,46.2f),cap,false);
            Box("BV2_Approach north retained end",life,new Vector3(129.5f,2.1f,284.3f),new Vector3(7.5f,4.2f,.45f),concrete,true,2.5f);
        }
        static void GreenPockets()
        {
            var parent=Group("BV2_Street plants",root,Vector3.zero).transform;
            var material=BahnhofDetailKit.Load("DistrictFlora");var source=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_0_0.asset");if(!source||!material)return;
            var random=new System.Random(261101);var batches=new Dictionary<string,List<Vector3>>();
            for(int i=0;i<6000;i++)
            {
                var p=new Vector2(144+(float)random.NextDouble()*150,100+(float)random.NextDouble()*252);
                if(Has(roads,p)||Has(walks,p)||!Development(p)||Mathf.PerlinNoise(p.x*.19f,p.y*.19f)<.46f)continue;
                if(Physics.CheckCapsule(new Vector3(p.x,.35f,p.y),new Vector3(p.x,1.5f,p.y),.20f,1,QueryTriggerInteraction.Ignore))continue;
                string key=Mathf.FloorToInt(p.x/40)+"_"+Mathf.FloorToInt(p.y/40);if(!batches.TryGetValue(key,out var list))batches[key]=list=new List<Vector3>();if(list.Count<100)list.Add(new Vector3(p.x,-.06f,p.y));
            }
            foreach(var batch in batches)
            {
                var parts=new List<CombineInstance>();var heights=new List<Vector2>();var roots=new List<Vector2>();
                foreach(var p in batch.Value)
                {
                    float scale=.7f+(float)random.NextDouble()*.55f;parts.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*scale)});
                    foreach(var vertex in source.vertices){heights.Add(new Vector2(p.y,Mathf.Clamp01(vertex.y/source.bounds.max.y)));roots.Add(new Vector2(p.x,p.z));}
                }
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);mesh.RecalculateBounds();var b=mesh.bounds;b.Expand(new Vector3(2.6f,1.2f,2.6f));mesh.bounds=b;
                SaveMesh(mesh,Generated+"/Meshes/BV2_Plants_"+batch.Key+".asset",out mesh);
                var go=Group("BV2_Plant patch "+batch.Key,parent,Vector3.zero);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
        static void TrimDistantApron()
        {
            // The old far-land apron must not remain a higher second skin under the new neighbourhood.
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Land below distant railway 1"||f.name=="Local render batch OverpassGround_DistantGround"))
            {
                var mesh=f.sharedMesh;var v=mesh.vertices;var old=mesh.triangles;var keep=new List<int>();
                for(int i=0;i<old.Length;i+=3){var center=(v[old[i]]+v[old[i+1]]+v[old[i+2]])/3;var world=f.transform.TransformPoint(center);if(Development(new Vector2(world.x,world.z)))continue;keep.AddRange(new[]{old[i],old[i+1],old[i+2]});}
                mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);var c=f.GetComponent<MeshCollider>();if(c){c.sharedMesh=null;c.sharedMesh=mesh;}
            }
        }
        static void FixVehicles()
        {
            WestbruchVehicleArt.Create(root,0,2,false,false,new Vector3(190,.04f,281),0).name="BV2_Parked station car";
            WestbruchVehicleArt.Create(root,1,4,false,false,new Vector3(186,.04f,281),0).name="BV2_Parked resident car";
        }
        static void RefreshNavigation()
        {
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();
            // All activity points in the extension are rebuilt from real entrances, rather than copied facade offsets.
            var places=world.places.Where(p=>p.label==null||!p.label.StartsWith("Bahnhofsviertel ")).ToList();
            foreach(var point in BahnhofResidentialArchitecture.EntrancePoints)
                places.Add(new PedestrianWorld.Place{label="Bahnhofsviertel "+places.Count,activity=PedestrianWorld.Activity.Shop,approach=point,facing=Quaternion.identity});
            foreach(var p in new[]{new Vector3(156,.14f,247),new Vector3(156,.14f,281),new Vector3(140,.14f,265),new Vector3(175,.14f,199),new Vector3(196,.14f,251)})
                places.Add(new PedestrianWorld.Place{label="Bahnhofsviertel "+places.Count,activity=PedestrianWorld.Activity.Wait,approach=p,facing=Quaternion.Euler(0,90,0)});
            foreach(float z in new[]{-142f,142})foreach(float x in new[]{-29f,24})places.Add(new PedestrianWorld.Place{label="Bahnhofsviertel promenade "+x+" "+z,activity=PedestrianWorld.Activity.Wait,approach=new Vector3(x,.14f,z),facing=Quaternion.identity});
            world.places=places.ToArray();
            BahnhofsviertelBuilder.Navigation(false);
            // Fit to actual baked walkable ground without silently moving people into buildings.
            foreach(var place in world.places)if(place.label!=null&&place.label.StartsWith("Bahnhofsviertel ")&&NavMesh.SamplePosition(place.approach,out var hit,1.4f,NavMesh.AllAreas))place.approach=hit.position;
            foreach(var person in world.people.Where(p=>p.name.StartsWith("Station resident")))
                if(NavMesh.SamplePosition(person.transform.position,out var hit,5,NavMesh.AllAreas))person.transform.position=hit.position;
            EditorUtility.SetDirty(world);
        }
        static void Bake()
        {
            // Parked vehicles already own baked panels. Never combine them with their retained raw sources.
            bool Vehicle(Transform t)=>t.GetComponentsInParent<Transform>().Any(p=>p.name.StartsWith("BV2_Parked ")||p.name=="Regional train – parked at platform one");
            var sources=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.isStatic&&f.sharedMesh&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&!f.GetComponent<TextMesh>()&&!f.GetComponentInParent<LODGroup>()&&!f.GetComponentInParent<PedestrianAgent>()&&!f.GetComponentInParent<StationClockHands>()&&!f.GetComponentInParent<SimpleCar>()&&!Vehicle(f.transform)&&!f.transform.GetComponentsInParent<Transform>().Any(t=>t.name=="BV2_Continuous surfaces")).ToArray();
            foreach(var group in sources.GroupBy(f=>new{mat=f.GetComponent<MeshRenderer>().sharedMaterial,x=Mathf.FloorToInt(f.transform.position.x/40),z=Mathf.FloorToInt(f.transform.position.z/40),shadow=f.GetComponent<MeshRenderer>().shadowCastingMode}))
            {
                var list=group.ToArray();var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                SaveMesh(mesh,Generated+"/Meshes/BV2_Chunk_"+group.Key.mat.name+"_"+group.Key.x+"_"+group.Key.z+"_"+group.Key.shadow+".asset",out mesh);
                var go=Group("BV2_Render chunk "+group.Key.mat.name,root,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=group.Key.mat;r.shadowCastingMode=group.Key.shadow;
                foreach(var f in list)f.GetComponent<MeshRenderer>().enabled=false;
            }
        }
    }
}
