using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // A local, repeatable scene patch. The rest of Westbruch is never regenerated.
    public static class WestbruchBridgeStart
    {
        public const string BridgeName = "Westbruch concrete overpass";
        public const string CampName = "Sheltered Westbruch story start";
        static Material Existing(string name) => AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/"+name+".mat");
        static Material Concrete => Existing("PropConcrete");
        static Material Metal => Existing("PropMetal");

        static float Ground(float x,float z) => Terrain.activeTerrain ? Terrain.activeTerrain.SampleHeight(new Vector3(x,0,z))+Terrain.activeTerrain.transform.position.y : 0;
        static GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false) => Box(name,parent,p,size,mat,solid);
        static void Rod(Transform parent,Vector3 a,Vector3 b,float width=.025f) => Beam("Galvanised worn wire",parent,a,b,width,Metal);

        [MenuItem("ZERO RISE/Bridge and story start/Apply local patch")]
        public static void Apply()
        {
            Directory.CreateDirectory("QA");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit play mode before applying the local patch.");
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var old=GameObject.Find("Rail underpass – east connection");
            var current=GameObject.Find(BridgeName);
            var parent=(old?old:current)?.transform.parent;
            if(!parent)throw new InvalidOperationException("Expected the existing east bridge in Westbruch.");
            // Original geometry is also present in render chunks. Rebuild ONLY
            // the material/spatial batches that contain old bridge sources.
            if(old)RemoveLegacyChunks(old.transform);
            if(old)UnityEngine.Object.DestroyImmediate(old);
            if(current)UnityEngine.Object.DestroyImmediate(current);
            var camp=GameObject.Find(CampName);if(camp)UnityEngine.Object.DestroyImmediate(camp);
            WestbruchOverpassArchitecture.ClearLegacy(parent.root);
            Clear();Build(parent);
            PlacePlayer();
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Bridge and secluded story start saved; district, traffic and navigation preserved.");
        }

        static void RemoveLegacyChunks(Transform old)
        {
            var district=old.root;
            var chunks=district.Find("Static render chunks – sources retained for editing");
            if(!chunks)throw new InvalidOperationException("Original render chunk root missing.");
            string Key(MeshFilter f) {var p=f.transform.position;return f.GetComponent<MeshRenderer>().sharedMaterial.name+"_"+Mathf.FloorToInt(p.x/32)+"_"+Mathf.FloorToInt(p.z/32);}
            var affected=new HashSet<string>(old.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()&&!f.GetComponent<TextMesh>()).Select(Key));
            foreach(string key in affected)
            {
                var filter=chunks.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==Generated+"/Meshes/Chunk_"+key+".asset");
                if(!filter)continue;
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var removed=new HashSet<int>();
                foreach(var source in old.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()&&!f.GetComponent<TextMesh>()&&Key(f)==key&&!f.GetComponent<MeshRenderer>().enabled))
                {
                    var expected=source.sharedMesh.vertices.Select(p=>source.transform.TransformPoint(p)).ToArray();bool found=false;
                    for(int start=0;start<=vertices.Length-expected.Length;start++)
                    {
                        if((vertices[start]-expected[0]).sqrMagnitude>.0000001f)continue;
                        bool matches=true;for(int i=1;i<expected.Length;i++)if((vertices[start+i]-expected[i]).sqrMagnitude>.0000001f){matches=false;break;}
                        if(!matches)continue;
                        for(int i=0;i<expected.Length;i++)removed.Add(start+i);found=true;break;
                    }
                    if(!found)throw new InvalidOperationException("Cannot safely remove the original bridge batch: "+source.name);
                }
                var original=mesh.triangles;var keep=new List<int>(original.Length);
                for(int i=0;i<original.Length;i+=3)if(!removed.Contains(original[i])&&!removed.Contains(original[i+1])&&!removed.Contains(original[i+2]))keep.AddRange(new[]{original[i],original[i+1],original[i+2]});
                mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
        }
        public static void Build(Transform parent,bool bakeLocal=true)
        {
            WestbruchOverpassArchitecture.Build(parent,bakeLocal);
        }
        internal static void BuildRelocatedCamp(Transform parent,bool bakeLocal)
        {
            Camp(parent,false);
            var camp=GameObject.Find(CampName).transform;
            var ground=camp.Cast<Transform>().ToDictionary(t=>t,t=>Ground(t.position.x,t.position.z));
            camp.SetPositionAndRotation(new Vector3(182,0,-13),Quaternion.Euler(0,-90,0));
            foreach(var item in ground){var p=item.Key.position;item.Key.position+=Vector3.up*(Ground(p.x,p.z)-item.Value);}
            var fenceParts=new HashSet<string>{"Fence post","Upper rail","Lower rail","Chain link diagonal","Fence collider rail"};
            foreach(var t in camp.GetComponentsInChildren<Transform>().Where(t=>fenceParts.Contains(t.name)).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            var fence=Group("Grounded shelter perimeter with west exit",camp,Vector3.zero).transform;
            fence.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            WestbruchOverpassArchitecture.GroundFence(fence,new Vector2(120.7f,66.4f),new Vector2(120.7f,71.9f));
            WestbruchOverpassArchitecture.GroundFence(fence,new Vector2(120.7f,71.9f),new Vector2(113.2f,71.9f));
            WestbruchOverpassArchitecture.GroundFence(fence,new Vector2(113.2f,66.4f),new Vector2(113.2f,69.1f));
            WestbruchOverpassArchitecture.GroundFence(fence,new Vector2(113.2f,70.9f),new Vector2(113.2f,71.9f));
            if(bakeLocal)BakeLocal(camp,"RelocatedCamp");
        }
        static void Quad(List<int> t,int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
        internal static GameObject MeshPart(string name,Transform parent,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material mat,bool solid=false)
        {
            string path=Generated+"/Meshes/BridgeStart_"+name.Replace(' ','_')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=Group(name,parent,Vector3.zero);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;return go;
        }

        static void Camp(Transform parent,bool bakeLocal)
        {
            var root=Group(CampName,parent,Vector3.zero).transform;
            Vector3 P(float x,float z,float y=0)=>new Vector3(x,Ground(x,z)+y,z);
            // The perimeter is grounded against finished world surfaces after
            // relocation, including the 1.8 m west exit and pavement level.
            var fabric=Existing("StreetLife_FadedFabric");var cardboard=Existing("StreetLife_Cardboard");
            var blanket=new Material(fabric){name="BridgeStart_OliveBlanket"};
            const string blanketPath=Generated+"/Materials/BridgeStart_OliveBlanket.mat";
            if(Existing("BridgeStart_OliveBlanket")){UnityEngine.Object.DestroyImmediate(blanket);blanket=Existing("BridgeStart_OliveBlanket");}
            else AssetDatabase.CreateAsset(blanket,blanketPath);
            blanket.SetColor("_BaseColor",new Color(.25f,.28f,.19f));EditorUtility.SetDirty(blanket);
            var bed=Group("Worn mattress and bedding",root,P(80.65f,65.25f,.014f),-8).transform;
            Part("Flattened cardboard under bedding",bed,new Vector3(0,.012f,0),new Vector3(1.55f,.022f,2.4f),cardboard);
            Mattress(bed,fabric);
            Cushion("Flattened pillow",bed,new Vector3(0,.35f,.72f),new Vector3(.65f,.14f,.38f),fabric);
            Cloth(bed,"Rumpled olive blanket",new Vector3(0,.30f,-.29f),1.12f,1.35f,blanket,false);
            for(int i=0;i<6;i++)Part("Mattress edge stitching",bed,new Vector3(-.587f,.19f,-.8f+i*.32f),new Vector3(.012f,.013f,.18f),cardboard);
            var bag=Group("Worn backpack",root,P(79.8f,63.6f,-.015f),20).transform;
            Cushion("Canvas pack",bag,new Vector3(0,.35f,0),new Vector3(.44f,.64f,.28f),blanket,true);
            Cushion("Front pocket",bag,new Vector3(0,.24f,-.16f),new Vector3(.33f,.28f,.09f),fabric);
            foreach(float x in new[]{-.13f,.13f})Rod(bag,new Vector3(x,.05f,.16f),new Vector3(x,.61f,.16f),.035f);
            Rod(bag,new Vector3(-.09f,.66f,0),new Vector3(.09f,.66f,0),.03f);
            ShoppingCart(root,P(84.1f,66.2f),-14,fabric);
            WestbruchProps.Pallet(root,P(80.6f,62.1f));
            WestbruchProps.Pallet(root,P(80.6f,62.1f,.17f));
            // Small salvaged windbreak at the back, no theatrical shelter or fire.
            var windbreak=Group("Salvaged canvas windbreak",root,P(81.1f,61.35f)).transform;
            Cloth(windbreak,"Sagging faded windbreak",new Vector3(0,.76f,0),2.3f,1.15f,fabric,true);
            var rng=new System.Random(8072);
            for(int i=0;i<30;i++)
            {
                float x=79.5f+(float)rng.NextDouble()*5.3f,z=61.6f+(float)rng.NextDouble()*7;
                if((x>81.65f&&x<84.25f&&z>63)||Vector2.Distance(new Vector2(x,z),new Vector2(80.65f,65.25f))<1.5f)continue;
                var piece=Part(i%3==0?"Discarded packing cardboard":"Weathered paper scrap",root,P(x,z,.015f),new Vector3(.15f+(float)rng.NextDouble()*.23f,.009f,.18f+(float)rng.NextDouble()*.24f),i%3==0?cardboard:Existing("StreetLife_Newsprint"));piece.transform.localRotation=Quaternion.Euler(0,i*137,0);
            }
            foreach(var at in new[]{new Vector2(81.8f,62.2f),new Vector2(84.5f,62),new Vector2(79.7f,67.5f)})
            {
                var sack=Group("Tied refuse bag",root,P(at.x,at.y,.010f)).transform;
                Cushion("Bag of belongings",sack,new Vector3(0,.22f,0),new Vector3(.42f,.43f,.4f),Existing("StreetLife_BlackPlastic"));
                Cushion("Tied bag neck",sack,new Vector3(0,.46f,0),new Vector3(.09f,.10f,.08f),Existing("StreetLife_BlackPlastic"));
            }
            foreach(var at in new[]{new Vector2(81.55f,64),new Vector2(84.5f,65),new Vector2(80.1f,67)})
            {
                var p=P(at.x,at.y);Cylinder("Discarded green bottle",root,p+Vector3.up*.11f,.035f,.21f,Existing("StreetLife_GreenBottle"));
                Cylinder("Bottle neck",root,p+Vector3.up*.24f,.018f,.065f,Existing("StreetLife_GreenBottle"));
            }
            // Reuse district flora meshes, keeping the bed and escape route bare.
            var flora=Existing("DistrictFlora");
            for(int i=0;i<115;i++)
            {
                float x=79.45f+(float)rng.NextDouble()*6.4f,z=60.8f+(float)rng.NextDouble()*9;
                if(x>81.7f&&x<84.25f&&z>63)continue;
                if(x<81.5f&&z>63.9f&&z<66.8f)continue;
                var plant=Group("Sheltered edge weed",root,P(x,z,-.015f),i*137).transform;
                plant.gameObject.isStatic=true;
                plant.localScale=Vector3.one*(.55f+(float)rng.NextDouble()*.4f);
                plant.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_"+(i%9==0?3:0)+"_"+(i%3)+".asset");
                plant.gameObject.AddComponent<MeshRenderer>().sharedMaterial=flora;
            }
            var marker=Group("New game spawn – future 0 euro story",root,P(83,66,.035f));
            marker.transform.rotation=Quaternion.identity;
            if(bakeLocal)BakeLocal(root,"Camp");
        }
        static void Mattress(Transform parent,Material fabric)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int rings=12,segments=48;
            float Shape(float x,float exponent)=>Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),exponent);
            for(int y=0;y<=rings;y++)
            {
                float phi=-Mathf.PI*.5f+y/(float)rings*Mathf.PI;
                float radius=Mathf.Pow(Mathf.Max(0,Mathf.Cos(phi)),.22f);
                for(int x=0;x<=segments;x++)
                {
                    float a=x/(float)segments*Mathf.PI*2;
                    v.Add(new Vector3(Shape(Mathf.Cos(a),.22f)*radius*.59f,.16f+Shape(Mathf.Sin(phi),.35f)*.13f,Shape(Mathf.Sin(a),.22f)*radius*1.025f));
                    uv.Add(new Vector2(x/(float)segments*3,y/(float)rings));
                    if(y<rings&&x<segments){int n=y*(segments+1)+x;Quad(triangles,n,n+segments+1,n+segments+2,n+1);}
                }
            }
            var mattress=MeshPart("Stained rounded mattress",parent,v,uv,triangles,fabric);
            var collider=mattress.AddComponent<BoxCollider>();collider.center=new Vector3(0,.16f,0);collider.size=new Vector3(1.12f,.24f,1.99f);
        }

        internal static void BakeLocal(Transform root,string label)
        {
            // Adjacent fence spans share one real post, never two coincident
            // cylinders fighting for depth at every joint and corner.
            var posts=new Dictionary<Vector3Int,Transform>();
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Fence post").ToArray())
            {
                var bounds=t.GetComponent<MeshRenderer>().bounds;var p=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);var key=new Vector3Int(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000));
                if(posts.TryGetValue(key,out var previous))
                {
                    if(previous.GetComponent<MeshRenderer>().bounds.max.y>=bounds.max.y)UnityEngine.Object.DestroyImmediate(t.gameObject);
                    else {UnityEngine.Object.DestroyImmediate(previous.gameObject);posts[key]=t;}
                }
                else posts[key]=t;
            }
            var sources=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).ToArray();
            foreach(var batch in sources.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mat=batch.Key;string path=Generated+"/Meshes/BridgeStart_"+label+"_"+mat.name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;
                var parts=batch.ToArray();mesh.CombineMeshes(parts.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
                if(mat.IsKeywordEnabled("_ZR_FOLIAGE"))
                {
                    var heights=new List<Vector2>();var roots=new List<Vector2>();
                    foreach(var source in parts)
                    {
                        var planted=source.transform.position;float height=source.sharedMesh.bounds.max.y;
                        if(source.sharedMesh.uv3.Length==source.sharedMesh.vertexCount&&source.sharedMesh.uv4.Length==source.sharedMesh.vertexCount){heights.AddRange(source.sharedMesh.uv3);roots.AddRange(source.sharedMesh.uv4);}
                        else foreach(var vertex in source.sharedMesh.vertices){heights.Add(new Vector2(planted.y,Mathf.Clamp01(vertex.y/height)));roots.Add(new Vector2(planted.x,planted.z));}
                    }
                    mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);var bounds=mesh.bounds;bounds.Expand(new Vector3(2.6f,1.2f,2.6f));mesh.bounds=bounds;
                }
                EditorUtility.SetDirty(mesh);var chunk=Group("Local render batch "+mat.name,root,Vector3.zero);chunk.isStatic=true;
                chunk.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=chunk.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.receiveShadows=parts[0].GetComponent<MeshRenderer>().receiveShadows;renderer.shadowCastingMode=parts[0].GetComponent<MeshRenderer>().shadowCastingMode;
                foreach(var source in parts)source.GetComponent<MeshRenderer>().enabled=false;
            }
        }
        static void Cushion(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            if(solid){var c=obj.AddComponent<BoxCollider>();c.size=new Vector3(.9f,.85f,.9f);}obj.isStatic=true;
        }
        static void Cloth(Transform parent,string name,Vector3 offset,float width,float length,Material mat,bool vertical)
        {
            const int nx=18,nz=22;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float u=x/(float)nx,w=z/(float)nz;
                float fold=Mathf.Sin(u*32+w*11)*.025f+Mathf.Sin(w*25+u*8)*.018f;
                var point=offset+new Vector3((u-.5f)*width,0,(w-.5f)*length);
                if(vertical)point=offset+new Vector3((u-.5f)*width,(w-.5f)*length-.12f*Mathf.Sin(u*Mathf.PI),fold);
                else
                {
                    // Follow the mattress surface so folds cannot disappear into it.
                    float top=.16f+.13f*Mathf.Pow(Mathf.Max(0,1-Mathf.Pow(Mathf.Abs(point.x)/.59f,9.09f)-Mathf.Pow(Mathf.Abs(point.z)/1.025f,9.09f)),.175f);
                    point.y=top+.045f+fold*.65f;
                }
                v.Add(point);
                uv.Add(new Vector2(u*2,w*2));
                if(z<nz&&x<nx){int a=z*(nx+1)+x;Quad(t,a,a+nx+1,a+nx+2,a+1);}
            }
            if(vertical)t.Reverse();
            MeshPart(name,parent,v,uv,t,mat);
        }
        static void ShoppingCart(Transform parent,Vector3 position,float yaw,Material cloth)
        {
            var root=Group("Salvaged shopping cart",parent,position,yaw).transform;
            foreach(float x in new[]{-.28f,.28f})
            {
                Rod(root,new Vector3(x,.18f,-.42f),new Vector3(x,.18f,.42f),.035f);
                Rod(root,new Vector3(x,.18f,.32f),new Vector3(x,.96f,.5f),.035f);
                foreach(float z in new[]{-.38f,.36f})
                {
                    var wheel=Cylinder("Cart castor",root,new Vector3(x,.095f,z),.085f,.055f,Existing("StreetLife_Rubber"));wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    Rod(root,new Vector3(x,.12f,z),new Vector3(x,.25f,z),.035f);
                }
            }
            foreach(float y in new[]{.43f,.57f,.71f,.85f})
            {
                float w=Mathf.Lerp(.23f,.32f,(y-.43f)/.42f);
                foreach(int side in new[]{-1,1})Rod(root,new Vector3(side*w,y,-.4f),new Vector3(side*w,y,.38f),.013f);
                foreach(float z in new[]{-.4f,.38f})Rod(root,new Vector3(-w,y,z),new Vector3(w,y,z),.013f);
            }
            for(float z=-.4f;z<.4f;z+=.10f)
            {
                foreach(int s in new[]{-1,1})Rod(root,new Vector3(s*.23f,.43f,z),new Vector3(s*.32f,.85f,z),.012f);
                Rod(root,new Vector3(-.23f,.43f,z),new Vector3(.23f,.43f,z),.012f);
            }
            for(float x=-.25f;x<.27f;x+=.10f)foreach(float z in new[]{-.4f,.38f})Rod(root,new Vector3(x*.85f,.43f,z),new Vector3(x,.85f,z),.012f);
            Beam("Faded red trolley handle",root,new Vector3(-.31f,.97f,.5f),new Vector3(.31f,.97f,.5f),.045f,Existing("StreetLife_FadedPackaging"));
            Part("Cart collision envelope",root,new Vector3(0,.49f,0),new Vector3(.62f,.86f,.88f),Metal,true).GetComponent<Renderer>().enabled=false;
            Cloth(root,"Blanket over trolley rim",new Vector3(.33f,.65f,.06f),.44f,.49f,cloth,true);
        }
        public static void PlacePlayer()
        {
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();if(!player)throw new InvalidOperationException("Existing player missing.");
            var cc=player.GetComponent<CharacterController>();bool enabled=cc.enabled;cc.enabled=false;
            player.transform.SetPositionAndRotation(new Vector3(plan.spawn.x,Ground(plan.spawn.x,plan.spawn.z)+.035f,plan.spawn.z),Quaternion.Euler(0,-90,0));cc.enabled=enabled;
            if(player.visual)player.visual.rotation=Quaternion.Euler(0,-90,0);
            if(player.orbit)player.orbit.initialYaw=-90;
            if(Camera.main){Camera.main.transform.position=player.transform.position+new Vector3(5.2f,3.3f,0);Camera.main.transform.LookAt(player.transform.position+Vector3.up*1.5f);}
        }
    }
}
