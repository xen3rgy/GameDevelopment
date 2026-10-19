using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.AI;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    // A targeted waterfront pass. All existing inland dressing and navigation remain.
    public static class WestbruchCanalBuilder
    {
        public const string RootName="Westbruch canal - flowing water and working banks";
        [Serializable] class Report {public int reedColonies,trees,shrubs,wallPanels,drains,ladders,bollards,stones,renderChunks;public float waterWidth=19.2f,waterLength=428;}
        static Report report;static System.Random rng;static Transform root;static Terrain terrain;
        static float R(float lo,float hi)=>lo+(float)rng.NextDouble()*(hi-lo);
        static Material concrete,wet,algae,metal,rust,wood;
        [MenuItem("ZERO RISE/Kanal und Ufer aktualisieren")]
        public static void Rebuild()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();
            Build(GameObject.Find("Westbruch \u2013 authored district").transform);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Build(Transform parent)
        {
            var previous=parent.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            RemoveLegacy(parent);report=new Report();rng=new System.Random(191026);terrain=Terrain.activeTerrain;
            root=Group(RootName,parent,Vector3.zero).transform;ConfigureWater();ShapeBanks();
            concrete=Surface("Canal_WeatheredConcrete","concrete",new Color(.58f,.58f,.51f),.2f);
            wet=Surface("Canal_DampConcrete","concrete",new Color(.22f,.26f,.21f),.58f);
            algae=Surface("Canal_AlgaeStains","concrete",new Color(.23f,.28f,.14f),.38f);
            metal=Surface("Canal_WornIron","steel",new Color(.20f,.24f,.23f),.26f,.7f);
            rust=Surface("Canal_RustIron","steel",new Color(.36f,.23f,.13f),.18f,.55f);
            wood=Surface("Canal_OldTimber","wood",new Color(.48f,.43f,.32f),.13f);
            RetainingWall();BankDetails();Reeds();FarBank();PumpHouse();BoundaryFences(parent);Batch();Physics.SyncTransforms();RefreshNavigation();
            Directory.CreateDirectory("QA");File.WriteAllText("QA/canal-build.json",JsonUtility.ToJson(report,true));Debug.Log("Canal authored: "+JsonUtility.ToJson(report));
        }
        static Material Surface(string id,string source,Color tint,float smooth,float metallic=0)
        {
            var mat=Material(id,source,tint,smooth,metallic);mat.SetFloat("_UVBase",5);mat.SetFloat("_TexWorldScale",.65f);HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);return mat;
        }
        static void RemoveLegacy(Transform parent)
        {
            bool Boundary(MeshFilter f)=>Mathf.Abs(Mathf.Abs(f.transform.position.z)-113.5f)<.01f&&new[]{"Fence post","Upper rail","Lower rail","Chain link diagonal","Fence collider rail"}.Contains(f.name)&&!(f.name=="Fence post"&&f.transform.position.x< -131.4f);
            var old=parent.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Canal"||f.name=="Canal retaining wall"||Boundary(f)).ToArray();
            var chunks=parent.Find("Static render chunks \u2013 sources retained for editing");
            foreach(var material in old.Select(f=>f.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToArray())
            {
                var affected=old.Where(f=>f.GetComponent<MeshRenderer>().sharedMaterial==material).Select(f=>material.name+"_"+Mathf.FloorToInt(f.transform.position.x/32)+"_"+Mathf.FloorToInt(f.transform.position.z/32)).Distinct();
                foreach(string key in affected)
                {
                    var chunk=chunks?chunks.Find(key):null;if(!chunk)continue;
                    var remaining=parent.GetComponentsInChildren<MeshFilter>().Where(f=>!old.Contains(f)&&f.name!="Fence collider rail"&&!f.transform.IsChildOf(chunks)&&f.gameObject.isStatic&&f.sharedMesh&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()&&!f.GetComponent<MeshRenderer>().enabled&&f.GetComponent<MeshRenderer>().sharedMaterial==material&&!f.GetComponentInParent<LODGroup>()&&material.name+"_"+Mathf.FloorToInt(f.transform.position.x/32)+"_"+Mathf.FloorToInt(f.transform.position.z/32)==key).ToArray();
                    if(remaining.Length==0)UnityEngine.Object.DestroyImmediate(chunk.gameObject);
                    else {var mesh=chunk.GetComponent<MeshFilter>().sharedMesh;mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(remaining.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);EditorUtility.SetDirty(mesh);}
                }
            }
            foreach(var f in old)UnityEngine.Object.DestroyImmediate(f.gameObject);
        }
        static void ConfigureWater()
        {
            var asset=GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;var settings=asset.currentPlatformRenderPipelineSettings;
            settings.supportWater=true;settings.waterSimulationResolution=WaterSimulationResolution.Medium128;settings.supportSSR=true;settings.supportSSRTransparent=true;asset.currentPlatformRenderPipelineSettings=settings;EditorUtility.SetDirty(asset);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/ZeroRise/Settings/WestbruchAtmosphere.asset");
            T Volume<T>() where T:VolumeComponent {if(profile.TryGet<T>(out var old))return old;var v=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(v,profile);return v;}
            Volume<WaterRendering>().enable.Override(true);Volume<WaterRendering>().triangleSize.Override(24);
            var reflection=Volume<ScreenSpaceReflection>();reflection.enabled.Override(true);reflection.enabledTransparent.Override(true);reflection.depthBufferThickness.Override(.07f);EditorUtility.SetDirty(profile);
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data=camera.GetComponent<HDAdditionalCameraData>();if(!data)continue;data.customRenderingSettings=true;
                foreach(var field in new[]{FrameSettingsField.Water,FrameSettingsField.Refraction,FrameSettingsField.SSR,FrameSettingsField.TransparentSSR}){data.renderingPathCustomFrameSettings.SetEnabled(field,true);data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
            }
            // The western edge extends under the sloping bank, so terrain depth
            // clips a natural shoreline instead of exposing the edge of a quad.
            var obj=Group("Canal current - native HDRP river surface",root,new Vector3(-122.05f,-.12f,0));obj.transform.localScale=new Vector3(19.2f,1,428);
            var water=obj.AddComponent<WaterSurface>();water.surfaceType=WaterSurfaceType.River;water.geometryType=WaterGeometryType.InstancedQuads;
            water.repetitionSize=36;water.largeWindSpeed=10;water.largeChaos=.65f;water.largeOrientationValue=90;water.largeBand0Multiplier=.24f;water.largeBand1Multiplier=.12f;water.largeCurrentSpeedValue=.65f;
            water.ripples=true;water.ripplesMotionMode=WaterPropertyOverrideMode.Custom;water.ripplesWindSpeed=7;water.ripplesChaos=.7f;water.ripplesOrientationValue=73;water.ripplesCurrentSpeedValue=.4f;
            water.refractionColor=new Color(.31f,.39f,.29f).linear;water.scatteringColor=new Color(.16f,.24f,.19f).linear;water.absorptionDistance=.85f;water.maxRefractionDistance=.3f;
            water.ambientScattering=.25f;water.heightScattering=.03f;water.displacementScattering=.08f;water.startSmoothness=.94f;water.endSmoothness=.82f;
            water.tessellation=true;water.maxTessellationFactor=3;water.tessellationFactorFadeStart=25;water.tessellationFactorFadeRange=100;
            water.foam=true;water.simulationFoamAmount=.05f;water.foamColor=new Color(.70f,.72f,.62f);water.foamPersistenceMultiplier=.15f;water.foamTextureTiling=.65f;water.caustics=false;water.scriptInteractions=false;water.underWater=false;
        }
        static void ShapeBanks()
        {
            var data=terrain.terrainData;int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float wx=origin.x+x/(n-1f)*data.size.x,wz=origin.z+z/(n-1f)*data.size.z;
                if(wx> -166&&wx< -132.5f)
                {
                    float bump=0;foreach(var p in new[]{new Vector2(-144,-73),new Vector2(-149,2),new Vector2(-143,83),new Vector2(-147,145),new Vector2(-147,-146)})bump+=Mathf.Pow(Mathf.Max(0,1-(new Vector2(wx,wz)-p).sqrMagnitude/180),2)*.9f;
                    // Absolute target makes repeated application idempotent.
                    h[z,x]=(.11f+bump+Mathf.PerlinNoise((wx+300)*.1f,(wz+300)*.1f)*.12f-origin.y)/data.size.y;
                }
                if(wx> -131.5f&&wx< -111.98f)
                {
                    float bedBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-131.5f,-129.4f,wx));
                    float bed=-.97f+.06f*Mathf.PerlinNoise((wx+400)*.31f,(wz+400)*.12f);
                    h[z,x]=(Mathf.Lerp(.08f,bed,bedBlend)-origin.y)/data.size.y;
                }
            }
            data.SetHeights(0,0,h);terrain.Flush();EditorUtility.SetDirty(data);
            int a=data.alphamapWidth;var splats=data.GetAlphamaps(0,0,a,a);
            for(int iz=0;iz<a;iz++)for(int ix=0;ix<a;ix++)
            {
                float x=origin.x+ix/(a-1f)*data.size.x,z=origin.z+iz/(a-1f)*data.size.z;
                if(x< -132&&x> -159){float grass=.38f+Mathf.PerlinNoise((x+321)*.25f,(z+600)*.15f)*.37f;splats[iz,ix,0]=1-grass-.12f;splats[iz,ix,1]=grass;splats[iz,ix,2]=.12f;splats[iz,ix,3]=0;}
                if(x> -131.5f&&x< -112.5f){splats[iz,ix,0]=.15f;splats[iz,ix,1]=0;splats[iz,ix,2]=.72f;splats[iz,ix,3]=.13f;}
            }
            data.SetAlphamaps(0,0,splats);terrain.Flush();
        }
        static float Ground(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        static void RetainingWall()
        {
            Box("Continuous canal retaining core",root,new Vector3(-112,-.25f,0),new Vector3(1,1.5f,228),concrete);
            for(float z=-113;z<114;z+=2)
            {
                Box("Jointed coping stone",root,new Vector3(-112,.55f,z),new Vector3(1.22f,.10f,1.976f),concrete);report.wallPanels++;
                Box("Waterline damp masonry",root,new Vector3(-112.513f,-.24f,z),new Vector3(.024f,.80f,1.96f),wet,false);
                if(rng.NextDouble()<.72) Stain(z,R(.6f,1.9f),R(.16f,.43f));
            }
            // A dark sediment shelf and sloped rubble replace the former soil/water gap.
            for(int i=0;i<42;i++)
            {
                float z=R(-110,110),x=R(-114.5f,-113.1f);Rock(new Vector3(x,-.48f,z),new Vector3(R(.15f,.45f),R(.12f,.35f),R(.2f,.6f)));report.stones++;
            }
        }
        static void Stain(float z,float length,float height)
        {
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<=12;i++){float a=z-length*.5f+i*length/12;float top=-.12f+height*(.35f+.65f*Mathf.PerlinNoise(a*4,17));v.Add(new Vector3(-112.528f,-.35f,a));v.Add(new Vector3(-112.528f,top,a));uv.Add(new Vector2(a,.1f));uv.Add(new Vector2(a,top));if(i>0){int k=i*2;t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();Model("Irregular algae and tide staining",mesh,algae);
        }
        static void BankDetails()
        {
            foreach(float z in new[]{-91f,-34,27,89})
            {
                var at=new Vector3(-112.62f,-.02f,z);var pipe=Group("Masonry storm drain",root,at).transform;
                Ring(pipe,Vector3.zero,.25f,.065f,.32f,concrete,Quaternion.Euler(0,0,90));
                var dark=Material("Canal_DrainDark",null,new Color(.035f,.045f,.037f),.1f);var hole=Cylinder("Recessed outfall throat",pipe,new Vector3(.03f,0,0),.19f,.015f,dark);hole.transform.localRotation=Quaternion.Euler(0,0,90);
                foreach(float y in new[]{-.13f,0,.13f}) Beam("Outfall protective grille",pipe,new Vector3(-.18f,y,-.16f),new Vector3(-.18f,y,.16f),.022f,rust);
                report.drains++;
            }
            foreach(float z in new[]{-61f,56})
            {
                var ladder=Group("Fixed canal maintenance ladder",root,new Vector3(-112.76f,0,z)).transform;
                foreach(float side in new[]{-.24f,.24f}){Beam("Ladder upright",ladder,new Vector3(0,-.73f,side),new Vector3(0,.93f,side),.04f,metal);Beam("Ladder return anchor",ladder,new Vector3(0,.93f,side),new Vector3(.35f,.58f,side),.04f,metal);}
                for(float y=-.63f;y<.8f;y+=.25f)Beam("Serrated ladder rung",ladder,new Vector3(0,y,-.24f),new Vector3(0,y,.24f),.035f,rust);
                report.ladders++;
            }
            foreach(float z in new[]{-97f,-67,-15,18,62,98})
            {
                var post=Group("Old mooring bollard",root,new Vector3(-112, .6f,z)).transform;
                Cylinder("Bolted base",post,new Vector3(0,.025f,0),.19f,.05f,metal);Cylinder("Mooring neck",post,new Vector3(0,.19f,0),.085f,.34f,rust);
                Beam("Bollard cross head",post,new Vector3(-.21f,.34f,0),new Vector3(.21f,.34f,0),.11f,metal);report.bollards++;
            }
            foreach(float z in new[]{-20f,22})
            {
                float x=-107.5f;var p=new Vector3(x,Ground(x,z),z);
                if(!Physics.CheckBox(p+Vector3.up*.6f,new Vector3(.85f,.5f,1.5f),Quaternion.identity,1,QueryTriggerInteraction.Ignore)){WestbruchProps.Bench(root,p,90);}
            }
            foreach(float z in new[]{-145f,145})
            {
                var bridge=Group("Distant canal service bridge",root,new Vector3(-121.5f,0,z)).transform;
                Box("Concrete bridge deck",bridge,new Vector3(0,1.8f,0),new Vector3(24,.36f,3.3f),concrete,false);
                foreach(int side in new[]{-1,1})
                {
                    Box("Bridge bearing",bridge,new Vector3(side*10.7f,.4f,0),new Vector3(1.8f,2.45f,3.6f),concrete,false);
                    Beam("Bridge handrail",bridge,new Vector3(-12,2.8f,side*1.5f),new Vector3(12,2.8f,side*1.5f),.055f,metal);
                    for(float x=-11.8f;x<12;x+=1.6f)Beam("Bridge railing upright",bridge,new Vector3(x,1.98f,side*1.5f),new Vector3(x,2.8f,side*1.5f),.035f,metal);
                }
            }
        }
        static void Ring(Transform parent,Vector3 p,float radius,float thickness,float depth,Material material,Quaternion rotation)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                Vector3 P(float angle,float r,float y)=>new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
                void Q(Vector3 x,Vector3 y,Vector3 z,Vector3 w){int k=v.Count;v.AddRange(new[]{x,y,z,w});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
                Q(P(a,radius,-depth*.5f),P(a,radius,depth*.5f),P(b,radius,depth*.5f),P(b,radius,-depth*.5f));
                Q(P(b,radius-thickness,-depth*.5f),P(b,radius-thickness,depth*.5f),P(a,radius-thickness,depth*.5f),P(a,radius-thickness,-depth*.5f));
                foreach(int side in new[]{-1,1})
                    if(side==1)Q(P(a,radius,side*depth*.5f),P(a,radius-thickness,side*depth*.5f),P(b,radius-thickness,side*depth*.5f),P(b,radius,side*depth*.5f));
                    else Q(P(a,radius,side*depth*.5f),P(b,radius,side*depth*.5f),P(b,radius-thickness,side*depth*.5f),P(a,radius-thickness,side*depth*.5f));
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();var obj=Model("Hollow concrete pipe collar",mesh,material,parent);obj.transform.localPosition=p;obj.transform.localRotation=rotation;
        }
        static void Rock(Vector3 p,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Embedded irregular bank stone";obj.transform.SetParent(root,false);obj.transform.localPosition=p;obj.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=concrete;obj.isStatic=true;obj.transform.localRotation=Quaternion.Euler(R(-20,20),R(0,360),R(-20,20));
        }
        static GameObject Model(string name,Mesh mesh,Material material,Transform parent=null)
        {var obj=Group(name,parent?parent:root,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;return obj;}
        static void Reeds()
        {
            var mat=Material("Canal_ReedBlades",null,new Color(.48f,.55f,.35f),.13f);mat.SetFloat("_DoubleSidedEnable",1);HDMaterial.ValidateMaterial(mat);mat.shader=Shader.Find("ZeroRise/UrbanLit");mat.EnableKeyword("_ZR_FOLIAGE");EditorUtility.SetDirty(mat);
            var v=new List<Vector3>();var uv=new List<Vector2>();var roots=new List<Vector2>();var heights=new List<Vector2>();var colors=new List<Color>();var tri=new List<int>();
            void Blade(Vector3 planted,Vector3 start,Vector3 direction,float length,float width,Color tint)
            {
                var side=Vector3.Cross(direction.normalized,Vector3.up).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;
                int first=v.Count;
                for(int s=0;s<=6;s++)
                {
                    float t=s/6f;var centre=start+Vector3.up*(length*(t*.7f-t*t*.4f))+direction*(length*(t*t*.55f+t*.12f));float w=width*Mathf.Sin(Mathf.PI*Mathf.Pow(t,.7f));
                    foreach(int a in new[]{-1,0,1}){var p=centre+side*w*a+Vector3.up*(a==0?width*.22f:0);v.Add(p);uv.Add(new Vector2((a+1)*.5f,t));roots.Add(new Vector2(planted.x,planted.z));heights.Add(new Vector2(planted.y,Mathf.Clamp01((p.y-planted.y)/1.9f)));colors.Add(tint*(1-t*.12f));}
                    if(s>0){int k=first+s*3;tri.AddRange(new[]{k-3,k-2,k,k-2,k+1,k,k-2,k-1,k+1,k-1,k+2,k+1});}
                }
            }
            void Culm(Vector3 planted,float height,float radius,Vector3 lean,Color tint,float bottom=0)
            {
                for(int ring=0;ring<=5;ring++)for(int side=0;side<=6;side++)
                {
                    float y=Mathf.Lerp(bottom,height,ring/5f),t=y/height,angle=side*Mathf.PI/3;var p=planted+Vector3.up*y+lean*t*t+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                    v.Add(p);uv.Add(new Vector2(side/6f,t));roots.Add(new Vector2(planted.x,planted.z));heights.Add(new Vector2(planted.y,Mathf.Clamp01(y/1.9f)));colors.Add(tint);
                    if(ring>0&&side>0){int k=v.Count-1;tri.AddRange(new[]{k-8,k-1,k-7,k-1,k,k-7});}
                }
            }
            for(int colony=0;colony<190;colony++)
            {
                float z=R(-196,196);bool east=colony%3==0;float x=east?R(-114.6f,-113.2f):R(-131.1f,-129.3f);float y=Ground(x,z)-.015f;
                var planted=new Vector3(x,y,z);report.reedColonies++;
                for(int stem=0;stem<rng.Next(7,15);stem++)
                {
                    float angle=R(0,Mathf.PI*2);var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var at=planted+direction*R(0,.38f);at.y=Ground(at.x,at.z)-.015f;float h=R(1.1f,1.9f);var tint=Color.Lerp(new Color(.25f,.30f,.12f),new Color(.44f,.37f,.19f),R(0,1));
                    var lean=direction*R(.03f,.15f);Culm(at,h,.006f,lean,tint);
                    if(stem%4==0)Culm(at,h+.12f,.018f,lean,new Color(.32f,.23f,.12f),h-.03f);
                    for(int leaf=0;leaf<3;leaf++){float atHeight=h*R(.42f,.78f);Blade(at,at+Vector3.up*atHeight+lean*Mathf.Pow(atHeight/h,2),Quaternion.Euler(0,R(0,360),0)*Vector3.forward,R(.3f,.7f),R(.009f,.018f),tint);}
                }
            }
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();var b=mesh.bounds;b.Expand(2);mesh.bounds=b;
            var obj=Model("Rooted reed colonies - wind and player response",mesh,mat);obj.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.TwoSided;
        }
        static void FarBank()
        {
            for(int i=0;i<25;i++)
            {
                float x=R(-156,-136),z=-190+i*15.5f+R(-5,5);int kind=i%4;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/Prefabs/UrbanTree_"+kind+".prefab");
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);PrefabUtility.UnpackPrefabInstance(tree,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);tree.name="Far bank pioneer tree "+i;tree.transform.SetPositionAndRotation(new Vector3(x,Ground(x,z)-.015f,z),Quaternion.Euler(0,R(0,360),0));tree.transform.localScale=Vector3.one*R(.68f,1.15f);report.trees++;
                foreach(var c in tree.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            }
            // Reuse the established 3D shrub asset, preserving its root-bending shader.
            var source=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).FirstOrDefault(l=>l.name.Contains("shrub")||l.name.Contains("Shrub"));
            if(source)for(int i=0;i<80;i++)
            {
                float x=R(-144,-133),z=R(-185,185);var shrub=UnityEngine.Object.Instantiate(source.gameObject,root);shrub.name="Far bank bramble colony "+i;shrub.transform.SetPositionAndRotation(new Vector3(x,Ground(x,z)-.02f,z),Quaternion.Euler(0,R(0,360),0));shrub.transform.localScale=Vector3.one*R(.7f,1.3f);foreach(var c in shrub.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);report.shrubs++;
            }
        }
        static void BoundaryFences(Transform parent)
        {
            // The old rectangular map fence crossed the open channel in mid-air.
            // Keep the land boundary, leaving a continuous, unobstructed waterway.
            foreach(int side in new[]{-1,1})WestbruchProps.Fence(root,new Vector3(-111,0,side*113.5f),new Vector3(131.5f,0,side*113.5f));
            foreach(var sign in parent.GetComponentsInChildren<MountedSign>().Where(s=>s.faceKey=="building_site"))
                sign.support=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fence collider rail").OrderBy(t=>Mathf.Abs(t.position.z-sign.transform.position.z)).First();
        }
        static void PumpHouse()
        {
            var pump=Group("Disused municipal pump house",root,new Vector3(-141.5f,Ground(-141.5f,42),42),-90).transform;
            var brick=Surface("Canal_PumpBrick","brick",new Color(.59f,.48f,.35f),.16f);
            var roofing=Surface("Canal_PumpRoof","steel",new Color(.24f,.27f,.24f),.2f,.55f);
            var shell=Box("Pump house masonry",pump,new Vector3(0,1.6f,0),new Vector3(6,3.2f,5.2f),brick,false);
            Box("Damp stone footing",pump,new Vector3(0,.025f,0),new Vector3(6.2f,.35f,5.4f),wet,false);
            foreach(int side in new[]{-1,1})
            {
                var roof=Box("Pitched corrugated roof",pump,new Vector3(0,3.50f,side*1.4f),new Vector3(6.5f,.10f,2.88f),roofing,false);roof.transform.localRotation=Quaternion.Euler(side*13,0,0);
                Beam("Rain gutter",pump,new Vector3(-3.25f,3.19f,side*2.82f),new Vector3(3.25f,3.19f,side*2.82f),.085f,rust);
                var mesh=new Mesh{vertices=new[]{new Vector3(side*3,3.2f,-2.6f),new Vector3(side*3,3.81f,0),new Vector3(side*3,3.2f,2.6f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.right},triangles=side==1?new[]{0,1,2}:new[]{0,2,1}};mesh.RecalculateNormals();mesh.RecalculateTangents();Model("Brick gable",mesh,brick,pump);
            }
            Box("Weathered service door",pump,new Vector3(1.25f,1.07f,-2.635f),new Vector3(1.05f,2.14f,.08f),metal,false);
            foreach(int side in new[]{-1,1})Box("Door jamb",pump,new Vector3(1.25f+side*.57f,1.09f,-2.70f),new Vector3(.08f,2.2f,.1f),concrete,false);
            Box("Door lintel",pump,new Vector3(1.25f,2.23f,-2.70f),new Vector3(1.26f,.1f,.12f),concrete,false);
            Beam("Door pull handle",pump,new Vector3(1.58f,.95f,-2.71f),new Vector3(1.58f,1.15f,-2.71f),.025f,rust);
            Box("Worn entrance step",pump,new Vector3(1.25f,.03f,-2.95f),new Vector3(1.45f,.13f,.65f),concrete,false);
            var glass=Material("Canal_PumpGlass",null,new Color(.11f,.17f,.16f),.77f,.1f);
            Box("Inset pump house window",pump,new Vector3(-1.5f,1.9f,-2.63f),new Vector3(1.05f,1.12f,.06f),glass,false);
            foreach(int side in new[]{-1,1})
            {
                Box("Window side frame",pump,new Vector3(-1.5f+side*.57f,1.9f,-2.69f),new Vector3(.08f,1.3f,.10f),wood,false);
                Box("Window cross frame",pump,new Vector3(-1.5f,1.9f+side*.61f,-2.69f),new Vector3(1.2f,.08f,.10f),wood,false);
            }
            for(float x=-1.85f;x<-1.1f;x+=.24f)Beam("Old window security bar",pump,new Vector3(x,1.32f,-2.76f),new Vector3(x,2.48f,-2.76f),.022f,rust);
            Box("Louvred ventilation frame",pump,new Vector3(.05f,2.8f,-2.65f),new Vector3(1,.38f,.08f),metal,false);
            for(int i=0;i<4;i++){var slat=Box("Angled vent louvre",pump,new Vector3(.05f,2.67f+i*.085f,-2.71f),new Vector3(.88f,.055f,.065f),rust,false);slat.transform.localRotation=Quaternion.Euler(-25,0,0);}
            Beam("Drain downpipe",pump,new Vector3(2.8f,.15f,-2.78f),new Vector3(2.8f,3.20f,-2.78f),.075f,rust);
            foreach(float y in new[]{.6f,2.5f})Box("Pipe mounting strap",pump,new Vector3(2.8f,y,-2.78f),new Vector3(.16f,.045f,.1f),metal,false);
            WestbruchSignage.Plate("number_7","7",pump,new Vector3(2.03f,1.85f,-2.66f),new Vector2(.19f,.16f),shell.transform,"Facade screw anchors");
            // The discharge structure is founded in the bank, behind the visible
            // pipe collar; its throat is dark and recessed rather than capped flat.
            var discharge=Group("Old pump discharge headwall",root,new Vector3(-130.9f,-.03f,42)).transform;
            Box("Bank outfall headwall",discharge,new Vector3(-.12f,.08f,0),new Vector3(.48f,1.3f,1.55f),wet,false);
            Ring(discharge,new Vector3(.18f,.07f,0),.29f,.065f,.32f,concrete,Quaternion.Euler(0,0,90));
            var throat=Cylinder("Dark discharge recess",discharge,new Vector3(.16f,.07f,0),.22f,.02f,Material("Canal_DrainDark",null,new Color(.035f,.045f,.037f),.1f));throat.transform.localRotation=Quaternion.Euler(0,0,90);
        }
        static void Batch()
        {
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.isStatic&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&!f.GetComponentInParent<LODGroup>()&&f.sharedMesh.subMeshCount==1).ToArray();
            var chunks=Group("Canal render chunks",root,Vector3.zero).transform;
            foreach(var g in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial.name+"_"+Mathf.FloorToInt(f.transform.position.z/32)))
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(g.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);
                string path=Generated+"/Meshes/Canal_"+g.Key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(mesh,path);
                var obj=Group(g.Key,chunks,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=g.First().GetComponent<MeshRenderer>().sharedMaterial;
                if(g.Key.StartsWith("Canal_Reed")){var b=mesh.bounds;b.Expand(2);mesh.bounds=b;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;}
                foreach(var f in g){f.GetComponent<MeshRenderer>().enabled=false;if(!AssetDatabase.Contains(f.sharedMesh)){f.sharedMesh=null;UnityEngine.Object.DestroyImmediate(f.gameObject);}}
                report.renderChunks++;
            }
        }
        static void RefreshNavigation()
        {
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();if(!world)return;
            var sources=new List<NavMeshBuildSource>();var markups=UnityEngine.Object.FindObjectsByType<SimpleCar>(FindObjectsSortMode.None).Select(c=>new NavMeshBuildMarkup{root=c.transform,ignoreFromBuild=true}).ToList();
            var bounds=new Bounds(new Vector3(0,1,0),new Vector3(224,7,224));NavMeshBuilder.CollectSources(bounds,1,NavMeshCollectGeometry.PhysicsColliders,0,markups,sources);
            for(int i=0;i<sources.Count;i++){var s=sources[i];if(s.sourceObject is TerrainData){s.area=4;sources[i]=s;}}
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            foreach(var road in plan.roads)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(road.x,.5f,road.z),Quaternion.identity,Vector3.one),size=new Vector3(road.w,4,road.d),area=1});
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.34f;settings.agentHeight=1.8f;settings.agentClimb=.27f;settings.agentSlope=40;settings.overrideVoxelSize=true;settings.voxelSize=.09f;settings.minRegionArea=1;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)throw new Exception("Canal navigation refresh failed");
            world.UnloadNavigation();EditorUtility.CopySerialized(data,world.navigation);UnityEngine.Object.DestroyImmediate(data);EditorUtility.SetDirty(world.navigation);world.LoadNavigation();
        }
    }
}
