using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class WestbruchBuilder
    {
        public const string ScenePath = "Assets/ZeroRise/Scenes/Westbruch.unity";
        static WestbruchLayout layout;
        static Material asphalt, paving, concrete, steel, wood, dirt, glass, trim;
        static WestbruchBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isBatchMode && !File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Build();
            };
        }
        [MenuItem("ZERO RISE/Westbruch bauen oder neu generieren")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            layout = JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            foreach (string sub in new[] { "Meshes", "Materials", "Prefabs" }) Directory.CreateDirectory(Generated + "/" + sub);
            Directory.CreateDirectory("Assets/ZeroRise/Scenes"); Directory.CreateDirectory("Assets/ZeroRise/Settings");
            AssetDatabase.Refresh(); Clear(); ConfigurePipeline();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Westbruch – authored district").transform;
            asphalt = Material("Asphalt", "asphalt", new Color(.48f,.51f,.54f), .22f);
            WestbruchRoadWear.ConfigureAsphalt(asphalt);
            paving = Material("Paving", "paving", new Color(.88f,.87f,.84f));
            concrete = Material("Concrete", "concrete", Color.white);
            steel = Material("AgedSteel", "steel", new Color(.55f,.61f,.55f), .3f, .65f);
            wood = Material("Timber", "wood", new Color(.85f,.76f,.62f));
            dirt = Material("CompactedSoil", "yard", new Color(.8f,.78f,.72f));
            glass = Material("DarkGlass", null, new Color(.16f,.23f,.27f), .82f, .25f);
            trim = Material("StoneTrim", "concrete", new Color(.77f,.76f,.69f));
            WestbruchLandscape.TerrainGround(root,layout);
            Surfaces(root);
            WestbruchRoadWear.Build(root,layout);
            foreach (var plot in layout.buildings) Building(plot, root);
            WestbruchProps.Build(root, layout);
            WestbruchDressing.Build(root,layout);
            WestbruchEverydayDetails.BuildYards(root,layout);
            WestbruchStreetFurniture.BuildLights(root,layout);
            WestbruchLandscape.Nature(root,layout);
            WestbruchCourtyardDetails.Build(root,layout);
            WestbruchFlora.Build(root,layout);
            WestbruchYardLitter.Build(root,layout);
            WestbruchEverydayDetails.BuildLitter(root,layout);
            Physics.SyncTransforms();WestbruchGroundDetails.Build(root,layout);
            var groundAsset=Group("Ground asset – sculpted terrain and embedded relief",root,Vector3.zero).transform;
            Terrain.activeTerrain.transform.SetParent(groundAsset,true);
            root.Find("Embedded ground relief – gravel, soil clods and edge rubble").SetParent(groundAsset,true);
            PrefabUtility.SaveAsPrefabAsset(groundAsset.gameObject,Generated+"/Prefabs/WestbruchGroundAsset.prefab");
            Lighting();
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .1f; camera.farClipPlane = 500; camera.fieldOfView = 58;
            cameraObject.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            cameraObject.AddComponent<AudioListener>(); var orbit = cameraObject.AddComponent<OrbitCamera>();
            orbit.obstacles=1|(1<<8);
            var player = Group("Player", null, layout.spawn.Position(.2f)); player.layer = 2;
            var controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f;
            controller.center = Vector3.up * .9f; controller.stepOffset = .26f; controller.slopeLimit = 45;
            var motor = player.AddComponent<ThirdPersonMotor>(); motor.orbit = orbit;
            PlayerAvatarBuilder.Build(motor);orbit.Follow(player.transform, 1.5f, 5.5f);
            foreach (var car in layout.cars)
            {
                var built = WestbruchProps.Car(root, car);
                if (car.driveable) motor.nearbyCar = built.GetComponent<SimpleCar>();
            }
            WestbruchTrafficBuilder.Build(root,layout);
            var vegetationMotion=new GameObject("Vegetation wind and contact response").AddComponent<VegetationMotion>();
            vegetationMotion.player=motor;vegetationMotion.car=motor.nearbyCar;
            CombineStaticGeometry(root);
            WestbruchPedestrianBuilder.Build(root,layout);
            WestbruchDesignPass.Apply(root,layout);
            WestbruchCanalBuilder.Build(root);
            cameraObject.transform.position = layout.spawn.Position(4) + Vector3.back * 6;
            cameraObject.transform.LookAt(layout.spawn.Position(1.4f));
            Physics.SyncTransforms();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Westbruch created. Play: WASD / mouse, E car. ZERO RISE menu: validate and build Windows.");
        }
        static void ConfigurePipeline()
        {
            PlayerSettings.companyName = "ZERO RISE"; PlayerSettings.productName = "ZERO RISE – Westbruch";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var settingsObjects = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settingsObjects.Length > 0)
            {
                var settings = new SerializedObject(settingsObjects[0]);
                var input = settings.FindProperty("activeInputHandler");
                if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            }
            string path = "Assets/ZeroRise/Settings/WindowsHDRP.asset";
            var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>(); AssetDatabase.CreateAsset(asset, path); }
            var pipelineSettings=asset.currentPlatformRenderPipelineSettings;
            pipelineSettings.supportVolumetricClouds=true;
            pipelineSettings.supportSSAO=true;
            asset.currentPlatformRenderPipelineSettings=pipelineSettings;
            EditorUtility.SetDirty(asset);
            GraphicsSettings.defaultRenderPipeline = asset; QualitySettings.renderPipeline = asset;
            // HDRP 17's Ensure is internal. Invoke the pinned package's initializer so
            // resources and its default volume profile are populated, not empty YAML.
            var globalType = typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings");
            var ensure = globalType?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);
            if (ensure == null || ensure.Invoke(null, new object[] { true }) == null)
                throw new InvalidOperationException("HDRP global settings unavailable. Open Window > Rendering > HDRP Wizard and Fix All, then rebuild Westbruch.");
        }
        static void CombineStaticGeometry(Transform root)
        {
            var batches=new Dictionary<string,List<MeshFilter>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer||!renderer.enabled||!filter.gameObject.isStatic||!filter.sharedMesh.isReadable||filter.GetComponent<TextMesh>())continue;
                // Imported tree geometry is instanced separately and keeps its LOD.
                if(filter.GetComponentInParent<LODGroup>())continue;
                var p=filter.transform.position;
                string key=renderer.sharedMaterial.name+"_"+Mathf.FloorToInt(p.x/32)+"_"+Mathf.FloorToInt(p.z/32);
                if(!batches.TryGetValue(key,out var list)) { list=new List<MeshFilter>();batches.Add(key,list); }
                list.Add(filter);
            }
            var parent=Group("Static render chunks – sources retained for editing",root,Vector3.zero).transform;
            foreach(var batch in batches)
            {
                string path=Generated+"/Meshes/Chunk_"+batch.Key+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;
                var parts=batch.Value.Select(f=>new CombineInstance { mesh=f.sharedMesh, transform=f.transform.localToWorldMatrix }).ToArray();
                mesh.CombineMeshes(parts,true,true);EditorUtility.SetDirty(mesh);
                bool vegetation=batch.Value[0].GetComponent<MeshRenderer>().sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE");
                if(vegetation)
                {
                    var roots=new List<Vector2>(mesh.vertexCount);var heights=new List<Vector2>(mesh.vertexCount);
                    foreach(var source in batch.Value)
                    {
                        var planted=source.transform.position;float height=source.sharedMesh.bounds.max.y;
                        foreach(var vertex in source.sharedMesh.vertices)
                        {
                            roots.Add(new Vector2(planted.x,planted.z));
                            heights.Add(new Vector2(planted.y,Mathf.Clamp01(vertex.y/height)));
                        }
                    }
                    mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);
                    // Conservative render bounds cover wind/contact bending.
                    var expanded=mesh.bounds;expanded.Expand(new Vector3(2.6f,1.2f,2.6f));mesh.bounds=expanded;
                }
                var chunk=Group(batch.Key,parent,Vector3.zero);chunk.isStatic=true;
                chunk.AddComponent<MeshFilter>().sharedMesh=mesh;
                chunk.AddComponent<MeshRenderer>().sharedMaterial=batch.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
                foreach(var source in batch.Value) source.GetComponent<MeshRenderer>().enabled=false;
            }
        }
        static bool Contains(Plot p, float x, float z, float margin = 0)
            => Mathf.Abs(x-p.x) < p.w*.5f+margin && Mathf.Abs(z-p.z) < p.d*.5f+margin;
        static void Surfaces(Transform root)
        {
            var surfaces = Group("Connected roads and footpaths",root,Vector3.zero).transform;
            var plots=WalkPlan(layout);
            WestbruchLandscape.SetWalks(plots);
            BuildSurfaces(surfaces,plots);
        }
        internal static List<Plot> WalkPlan(WestbruchLayout plan)
        {
            var plots = new List<Plot>(plan.paths);
            foreach (var b in plan.buildings)
            {
                float sign = b.yaw == 180 ? 1 : -1;
                float doorZ = b.z + sign * (b.d*.5f+.05f);
                float edge = doorZ;
                float nearest = float.PositiveInfinity;
                foreach(var surface in plan.paths.Concat(plan.roads))
                {
                    float margin = plan.roads.Contains(surface) ? 3 : 0;
                    if(Mathf.Abs(b.x-surface.x)>surface.w*.5f+margin)continue;
                    if(Contains(surface,b.x,doorZ,margin)) { nearest=0;edge=doorZ;break; }
                    float targetEdge=surface.z-sign*(surface.d*.5f+margin);
                    float ahead=(targetEdge-doorZ)*sign;
                    if(ahead>=0&&ahead<nearest) { nearest=ahead;edge=targetEdge; }
                }
                if(float.IsPositiveInfinity(nearest)) throw new InvalidOperationException("No connected entrance surface: "+b.id);
                plots.Add(new Plot { id=b.id+" approach", x=b.x, z=(doorZ+edge)*.5f, w=2.2f, d=Mathf.Abs(doorZ-edge)+.2f });
            }
            foreach(var b in plan.buildings)
            {
                float sign=b.yaw==180?1:-1;
                plots.Add(new Plot{id=b.id+" facade walk",x=b.x,z=b.z+sign*(b.d*.5f+1),w=b.w-1.2f,d=2.3f});
            }
            return plots;
        }
        static void BuildSurfaces(Transform surfaces,List<Plot> plots)
        {
            var xs = new SortedSet<float> { -layout.width*.5f, layout.width*.5f };
            var zs = new SortedSet<float> { -layout.depth*.5f, layout.depth*.5f };
            foreach (var p in layout.roads.Concat(plots))
            {
                xs.Add(p.x-p.w*.5f); xs.Add(p.x+p.w*.5f); zs.Add(p.z-p.d*.5f); zs.Add(p.z+p.d*.5f);
                if (layout.roads.Contains(p))
                { xs.Add(p.x-p.w*.5f-3); xs.Add(p.x+p.w*.5f+3); zs.Add(p.z-p.d*.5f-3); zs.Add(p.z+p.d*.5f+3); }
            }
            var x = xs.ToArray(); var z = zs.ToArray();
            for(int i=0;i<x.Length-1;i++) for(int j=0;j<z.Length-1;j++)
            {
                float cx=(x[i]+x[i+1])*.5f, cz=(z[j]+z[j+1])*.5f;
                bool road=layout.roads.Any(p=>Contains(p,cx,cz));
                bool sidewalk=!road && (layout.roads.Any(p=>Contains(p,cx,cz,3)) || plots.Any(p=>Contains(p,cx,cz)));
                if(!road&&!sidewalk) continue;
                float height=road?.04f:.14f;
                Box(road?"Road cell":"Connected pavement",surfaces,new Vector3(cx,height*.5f,cz),new Vector3(x[i+1]-x[i],height,z[j+1]-z[j]),road?asphalt:paving);
            }
            // Kerbs follow only the exposed road edge; no bars across junction mouths.
            foreach(var r in layout.roads)
            {
                bool horizontal=r.w>r.d; float length=horizontal?r.w:r.d;
                for(float along=-length*.5f+.5f;along<length*.5f;along+=1)
                foreach(int side in new[]{-1,1})
                {
                    float cx=r.x+(horizontal?along:side*(r.w*.5f+.09f));
                    float cz=r.z+(horizontal?side*(r.d*.5f+.09f):along);
                    if(layout.roads.Any(p=>p!=r && Contains(p,cx,cz,.55f))) continue;
                    // Workshop entries and courtyard paths have dropped kerbs.
                    float height=plots.Any(p=>Contains(p,cx,cz,.65f))?.055f:.17f;
                    Box("Kerb",surfaces,new Vector3(cx,height*.5f,cz),horizontal?new Vector3(.97f,height,.18f):new Vector3(.18f,height,.97f),trim);
                }
            }
            var paint=Material("FadedRoadPaint",null,new Color(.62f,.59f,.48f));
            foreach(var r in layout.roads)
            {
                bool horizontal=r.w>r.d; float length=horizontal?r.w:r.d;
                for(float a=-length*.5f+6;a<length*.5f-4;a+=8)
                {
                    float cx=r.x+(horizontal?a:0),cz=r.z+(horizontal?0:a);
                    if(layout.roads.Any(p=>p!=r&&Contains(p,cx,cz,3))) continue;
                    Box("Worn lane marking",surfaces,new Vector3(cx,.043f,cz),horizontal?new Vector3(2.8f,.004f,.1f):new Vector3(.1f,.004f,2.8f),paint,false);
                }
            }
        }
        static void Building(BuildingPlot b, Transform parent)
        {
            var root=Group(b.id,parent,b.Position(.14f),b.yaw).transform;
            float height=b.kind=="workshop"?4.6f:b.kind=="garage"?3:b.floors*3.05f;
            var facade=Material(b.id+"_Facade",b.surface,Tint(b.tone));
            int facadeSeed=Array.IndexOf(layout.buildings,b);
            facade.SetTextureOffset("_BaseColorMap",new Vector2((facadeSeed*.271f)%1,(facadeSeed*.163f)%1));
            facade.SetTextureScale("_BaseColorMap",Vector2.one*(b.surface=="brick"?1.5f:1+(facadeSeed%3)*.13f));
            Box("Structural shell",root,new Vector3(0,height*.5f,0),new Vector3(b.w,height,b.d),facade);
            Box("Foundation course",root,new Vector3(0,.10f,0),new Vector3(b.w+.08f,.55f,b.d+.08f),concrete,false);
            Box("Roof slab",root,new Vector3(0,height+.10f,0),new Vector3(b.w+.45f,.2f,b.d+.45f),asphalt);
            foreach(int side in new[]{-1,1})
            {
                Box("Roof parapet",root,new Vector3(0,height+.34f,side*(b.d*.5f+.1f)),new Vector3(b.w+.6f,.36f,.2f),trim,false);
                Box("Roof parapet",root,new Vector3(side*(b.w*.5f+.1f),height+.34f,0),new Vector3(.2f,.36f,b.d),trim,false);
                Cylinder("Rainwater pipe",root,new Vector3(side*(b.w*.5f-.35f),height*.5f,-b.d*.5f-.12f),.055f,height,steel);
                Box("Chimney",root,new Vector3(side*b.w*.25f,height+.6f,1),new Vector3(.8f,1.2f,.7f),concrete,false);
            }
            if(b.kind=="garage"||b.kind=="workshop")
            {
                int n=b.kind=="garage"?4:3;
                for(int i=0;i<n;i++)
                {
                    float xx=(i-(n-1)*.5f)*(b.w/n);
                    Box("Recessed roller door",root,new Vector3(xx,1.4f,-b.d*.5f-.04f),new Vector3(b.w/n-.55f,2.65f,.12f),steel,false);
                    for(float y=.28f;y<2.7f;y+=.25f) Box("Roller seam",root,new Vector3(xx,y,-b.d*.5f-.115f),new Vector3(b.w/n-.6f,.024f,.022f),trim,false);
                }
                if(b.kind=="workshop") Sign("WESTBRUCH · SERVICE & REPARATUR",root,new Vector3(0,3.3f,-b.d*.5f-.15f),b.w*.82f,steel,new Color(.9f,.85f,.66f));
            }
            else
            {
                for(int side=-1;side<=1;side+=2) for(int floor=0;floor<b.floors;floor++)
                for(float xx=-b.w*.5f+2.5f;xx<b.w*.5f-1.6f;xx+=3.8f)
                {
                    if(floor==0&&Mathf.Abs(xx)<2) continue;
                    Window(root,xx,1.7f+floor*3.05f,side*(b.d*.5f+.035f));
                    if(floor>0&&Mathf.Abs(xx)>2&&Mathf.RoundToInt(xx)%2==0)
                    {
                        var balconyRoot=side==-1?root:Group("Rear balcony",root,Vector3.zero,180).transform;
                        WestbruchProps.Balcony(balconyRoot,side==-1?xx:-xx,floor*3.05f+.55f,-b.d*.5f-.8f);
                    }
                }
                // Side elevations carry windows too, not blank colored boxes.
                foreach(int side in new[]{-1,1})
                {
                    var elevation=Group("Side elevation",root,new Vector3(side*b.w*.5f,0,0),side==1?-90:90).transform;
                    for(int floor=0;floor<b.floors;floor++) for(float xx=-b.d*.5f+2.2f;xx<b.d*.5f-1;xx+=3.8f) Window(elevation,xx,1.7f+floor*3.05f,-.035f);
                }
                Box("Entrance",root,new Vector3(0,1.12f,-b.d*.5f-.09f),new Vector3(1.35f,2.24f,.18f),steel,false);
                Box("Entrance canopy",root,new Vector3(0,2.5f,-b.d*.5f-.72f),new Vector3(2.5f,.16f,1.4f),concrete);
                Box("Mailboxes",root,new Vector3(1.05f,1.25f,-b.d*.5f-.12f),new Vector3(.45f,.7f,.12f),steel,false);
                Sign(b.id.Replace('_',' '),root,new Vector3(0,2.82f,-b.d*.5f-.09f),3.2f,steel,Color.white);
                if(b.kind=="shop")
                {
                    var green=Material("ShopGreen",null,new Color(.12f,.24f,.18f));
                    Sign("PFAND & SPAR",root,new Vector3(0,3,-b.d*.5f-.2f),b.w*.8f,green,new Color(.91f,.86f,.68f));
                    Box("Shop awning",root,new Vector3(0,2.7f,-b.d*.5f-.8f),new Vector3(b.w*.8f,.18f,1.6f),green);
                }
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Generated+"/Prefabs/"+b.id+".prefab",InteractionMode.AutomatedAction);
        }
        static void Window(Transform parent,float x,float y,float z)
        {
            float sign=Mathf.Sign(z);
            Box("Recessed glass",parent,new Vector3(x,y,z),new Vector3(1.3f,1.65f,.045f),glass,false);
            foreach(int s in new[]{-1,1}) Box("Window jamb",parent,new Vector3(x+s*.7f,y,z+sign*.025f),new Vector3(.1f,1.85f,.09f),trim,false);
            foreach(int s in new[]{-1,1}) Box("Window lintel",parent,new Vector3(x,y+s*.87f,z+sign*.04f),new Vector3(1.5f,.1f,.14f),trim,false);
            Box("Mullion",parent,new Vector3(x,y,z+sign*.05f),new Vector3(.045f,1.65f,.07f),trim,false);
            Box("Projecting sill",parent,new Vector3(x,y-.91f,z+sign*.11f),new Vector3(1.62f,.1f,.28f),concrete,false);
        }
        static void Lighting()
        {
            var sun=new GameObject("Late afternoon sun"); sun.transform.rotation=Quaternion.Euler(36,-42,0);
            var light=sun.AddComponent<Light>(); light.type=LightType.Directional; light.color=new Color(1,.98f,.94f); light.shadows=LightShadows.Soft;
            sun.AddComponent<HDAdditionalLightData>(); light.intensity=65000;
            string path="Assets/ZeroRise/Settings/WestbruchAtmosphere.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile) { profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,path); }
            T Add<T>() where T:VolumeComponent { if(profile.TryGet<T>(out var found))return found; var value=profile.Add<T>(true); AssetDatabase.AddObjectToAsset(value,profile); return value; }
            Add<VisualEnvironment>().skyType.Override((int)SkyType.PhysicallyBased);
            Add<VisualEnvironment>().skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            Add<PhysicallyBasedSky>();
            Add<VolumetricClouds>().enable.Override(true);
            var exposure=Add<Exposure>(); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(12);
            Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var fog=Add<Fog>(); fog.enabled.Override(true); fog.meanFreePath.Override(340); fog.baseHeight.Override(0); fog.maximumHeight.Override(50);
            Add<Bloom>().intensity.Override(.12f);
            Add<ScreenSpaceAmbientOcclusion>().intensity.Override(.65f);
            var volume=new GameObject("Westbruch atmosphere").AddComponent<Volume>(); volume.isGlobal=true; volume.sharedProfile=profile;
            EditorUtility.SetDirty(profile);
        }
        [MenuItem("ZERO RISE/Windows-Prototyp bauen")]
        public static void BuildWindows()
        {
            if(!File.Exists(ScenePath)) Build();
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath}, locationPathName="Builds/Windows/ZeroRise.exe", target=BuildTarget.StandaloneWindows64, options=BuildOptions.None });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed: "+report.summary.result);
        }
    }
}
