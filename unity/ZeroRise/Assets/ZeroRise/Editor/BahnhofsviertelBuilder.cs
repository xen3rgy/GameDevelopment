using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Additive district authoring. Never regenerate the original Westbruch map.
    public static class BahnhofsviertelBuilder
    {
        public const string RootName="Bahnhofsviertel – connected station neighbourhood";
        static Transform root;static Material asphalt,paving,stone,brick,metal,glass,roof,paint,wood;
        static readonly List<Vector3> visits=new List<Vector3>();
        static GameObject B(string n,Transform p,Vector3 at,Vector3 size,Material m,bool solid=true)=>Box(n,p,at,size,m,solid);
        static Material Load(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/"+n+".mat");
        [MenuItem("ZERO RISE/Bahnhofsviertel/Build connected expansion")]
        public static void Build()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();visits.Clear();
                if(GameObject.Find(RootName)){Debug.Log("Bahnhofsviertel is already installed; use Refine for targeted updates.");if(Application.isBatchMode)EditorApplication.Exit(0);return;}
                var district=GameObject.Find("Westbruch – authored district");if(!district)throw new Exception("Saved Westbruch district missing");
                root=Group(RootName,district.transform,Vector3.zero).transform;
                asphalt=Load("Asphalt");paving=Load("Paving");stone=Load("PropConcrete");metal=Load("DressingMetal");wood=Load("PropWood");
                brick=Material("Bahnhof_OldBrick","brick",new Color(.65f,.48f,.35f),.12f);
                glass=Material("Bahnhof_RecessedGlass",null,new Color(.10f,.16f,.18f),.55f,.15f);
                roof=Material("Bahnhof_Slate","asphalt",new Color(.27f,.29f,.29f),.15f);
                paint=Material("Bahnhof_FadedRoadPaint",null,new Color(.69f,.67f,.55f),.05f);
                OpenConnection();Ground();Streets();Buildings();Station();Railway();StreetLife();Navigation();Traffic();
                Physics.SyncTransforms();Validate();RefineLoaded();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                File.WriteAllText("QA/bahnhofsviertel-authoring.txt","Passed: True\nAdditive expansion saved. Original Westbruch retained.\nStation lobby, two platforms, access ramp, pedestrian bridge, shops, courtyard and connected traffic/navigation.\n");
                Debug.Log("Bahnhofsviertel extension saved successfully");if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        static void OpenConnection()
        {
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
            // Recombine the bridge's retained sources after cutting only the east exit.
            foreach(var batch in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Local render batch ")).ToArray())UnityEngine.Object.DestroyImmediate(batch.gameObject);
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<MeshRenderer>())f.GetComponent<MeshRenderer>().enabled=true;
            var remove=new List<GameObject>();
            foreach(var t in bridge.GetComponentsInChildren<Transform>())
            {
                var p=t.position;var r=t.GetComponent<Renderer>();if(!r)continue;
                bool fence=(t.name.Contains("Fence")||t.name.Contains("fence")||t.name.Contains("rail")||t.name=="Chain link diagonal")&&Mathf.Abs(p.x-164)<.8f&&p.z>70&&p.z<90;
                if(fence||t.name=="Court east pavement")remove.Add(t.gameObject);
            }
            foreach(var o in remove.Distinct())if(o)UnityEngine.Object.DestroyImmediate(o);
            foreach(float z in new[]{66.5f,93.5f})B("Retained court east pavement",bridge,new Vector3(161.5f,.0675f,z),new Vector3(3,.145f,11),paving);
            WestbruchBridgeStart.BakeLocal(bridge,"EngineeredOverpass");
        }
        static void Ground()
        {
            // Lower only new development ground; keep the rail bank and original town intact.
            var terrain=Terrain.activeTerrain;var data=terrain.terrainData;int r=data.heightmapResolution;
            var h=data.GetHeights(0,0,r,r);var origin=terrain.transform.position;
            for(int z=0;z<r;z++)for(int x=0;x<r;x++)
            {
                float wx=origin.x+x/(r-1f)*data.size.x,wz=origin.z+z/(r-1f)*data.size.z;
                if((wx>166&&wz>58||wx>134&&wz>118)&&wx<300&&wz<370)h[z,x]=Mathf.Max(0,(-.035f-origin.y)/data.size.y);
            }
            data.SetHeights(0,0,h);EditorUtility.SetDirty(data);
            // The distant railway apron overlaps the development. Trim its retained
            // source and rebake together with the bridge, avoiding coplanar surfaces.
            var ground=Material("Bahnhof_Earth","yard",new Color(.54f,.53f,.42f),.08f);
            B("District supporting earth",root,new Vector3(218,-.16f,228),new Vector3(168,.25f,290),ground);
            B("Eastern connecting ground",root,new Vector3(198,-.16f,71),new Vector3(64,.25f,26),ground);
            // Existing distant apron is at .12m: move only its development-side
            // vertices down, and update the collider before rebaking bridge sources.
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Land below distant railway 1"))
            {
                var mesh=f.sharedMesh;var v=mesh.vertices;
                for(int i=0;i<v.Length;i++)if(v[i].x>134&&v[i].z<374)v[i].y=-.04f;
                mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var c=f.GetComponent<MeshCollider>();if(c){c.sharedMesh=null;c.sharedMesh=mesh;}
            }
            foreach(var batch in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Local render batch ")).ToArray())UnityEngine.Object.DestroyImmediate(batch.gameObject);
            foreach(var f in bridge.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<MeshRenderer>())f.GetComponent<MeshRenderer>().enabled=true;
            WestbruchBridgeStart.BakeLocal(bridge,"EngineeredOverpass");
        }
        static readonly List<Rect> roadCuts=new List<Rect>();
        static void Cut(float x,float z,float w,float d)=>roadCuts.Add(new Rect(x-w/2,z-d/2,w,d));
        static void Slab(string name,float x,float z,float w,float d,Material m,float top=.14f)
        {
            var pieces=new List<Rect>{new Rect(x-w/2,z-d/2,w,d)};
            if(m==paving&&Mathf.Abs(top-.14f)<.001f)foreach(var cut in roadCuts)
            {
                var next=new List<Rect>();foreach(var a in pieces)
                {
                    if(!a.Overlaps(cut)){next.Add(a);continue;}
                    float lx=Mathf.Max(a.xMin,cut.xMin),hx=Mathf.Min(a.xMax,cut.xMax),lz=Mathf.Max(a.yMin,cut.yMin),hz=Mathf.Min(a.yMax,cut.yMax);
                    if(lx>a.xMin)next.Add(Rect.MinMaxRect(a.xMin,a.yMin,lx,a.yMax));
                    if(hx<a.xMax)next.Add(Rect.MinMaxRect(hx,a.yMin,a.xMax,a.yMax));
                    if(lz>a.yMin)next.Add(Rect.MinMaxRect(lx,a.yMin,hx,lz));
                    if(hz<a.yMax)next.Add(Rect.MinMaxRect(lx,hz,hx,a.yMax));
                }pieces=next;
            }
            foreach(var a in pieces)if(a.width>.015f&&a.height>.015f)B(name,root,new Vector3(a.center.x,top-.08f,a.center.y),new Vector3(a.width,.16f,a.height),m);
        }
        static void Street(float x,float z,float w,float d)
        {
            Slab("Bahnhof connected pavement",x,z,w+6,d+6,paving);
            Slab("Bahnhof connected carriageway",x,z,w,d,asphalt,.04f);
            bool along=w>d;float length=along?w:d;
            for(float t=-length/2+4;t<length/2-2;t+=6)
                B("Worn centre marking",root,new Vector3(x+(along?t:0),.044f,z+(along?0:t)),along?new Vector3(2.4f,.005f,.10f):new Vector3(.10f,.005f,2.4f),paint,false);
        }
        static void Streets()
        {
            roadCuts.Clear();Cut(184,80,48,10);Cut(208,193,10,226);Cut(236,176,56,9);Cut(236,306,56,9);Cut(264,241,9,130);Cut(189,266,38,9);Cut(190,280,13,10);
            foreach(var p in new[]{new Vector2(208,80),new Vector2(208,176),new Vector2(208,266),new Vector2(208,306),new Vector2(264,176),new Vector2(264,306)})Cut(p.x,p.y,14,14);
            Street(184,80,48,10);Street(208,193,10,226);Street(236,176,56,9);Street(236,306,56,9);Street(264,241,9,130);
            // Junctions have flush road surfaces and continuous walking corners.
            foreach(var p in new[]{new Vector2(208,80),new Vector2(208,176),new Vector2(208,266),new Vector2(208,306),new Vector2(264,176),new Vector2(264,306)})Slab("Flush junction",p.x,p.y,14,14,asphalt,.041f);
            Slab("Station square paving",173,264,54,64,paving);
            Slab("Bahnhofsplatz access carriageway",189,266,38,9,asphalt,.04f);
            // Short terminating parking court on the square; no traffic through pedestrians.
            Slab("Taxi waiting bay",190,280,13,10,asphalt,.04f);
            for(int i=0;i<3;i++)B("Taxi bay separator",root,new Vector3(185+i*3.7f,.047f,280),new Vector3(.08f,.006f,5.5f),paint,false);
            Slab("Güterhof accessible courtyard",174,198,42,24,paving);
            Slab("Courtyard east entrance",196,198,12,5,paving);
            Slab("Rail bank pedestrian shortcut",140,174,3.2f,108,paving);
            Slab("Shortcut south link",161,120,45,3.2f,paving);
            Slab("Shortcut station link",145,227,12,3.2f,paving);
            foreach(float z in new[]{151f,228,294})for(int i=0;i<9;i++)
                B("Station pedestrian crossing",root,new Vector3(204+i*.95f,.047f,z),new Vector3(.44f,.007f,2.5f),paint,false);
            // Actual curb faces; continuous 10cm steps stay within controller climb.
            foreach(float x in new[]{202.9f,213.1f})foreach(var span in new[]{new Vector2(97,143),new Vector2(155,168),new Vector2(184,220),new Vector2(232,258),new Vector2(274,286),new Vector2(298,303)})
                B("Station road kerb",root,new Vector3(x,.09f,(span.x+span.y)/2),new Vector3(.16f,.18f,span.y-span.x),stone);
        }
        static Transform House(string id,string prefab,float x,float z,float yaw,string shop=null)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/Prefabs/"+prefab+".prefab");if(!asset)throw new Exception("Missing existing house "+prefab);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            go.name=id;go.transform.SetPositionAndRotation(new Vector3(x,.14f,z),Quaternion.Euler(0,yaw,0));
            foreach(var label in go.GetComponentsInChildren<TextMesh>())label.text=id.Replace('_',' ');
            var shell=go.transform.Find("Structural shell").GetComponent<MeshFilter>().sharedMesh.bounds.size;
            float h=shell.y;
            // Local roof profiles distinguish this railway-era street from Westbruch.
            PitchedRoof(go.transform,shell.x+.65f,shell.z+.75f,h+.3f,1.9f);
            if(shop!=null)Shop(go.transform,shop,shell.x,shell.z);
            visits.Add(go.transform.TransformPoint(new Vector3(0,.05f,-shell.z*.5f-1.5f)));
            return go.transform;
        }
        static void Buildings()
        {
            House("Bahnwerk_Service","Werkstatt",179,102,180);
            House("Güterhalle_7","Garagen_West",168,142,180);
            House("Güterstraße_12","Nordhof_8",180,177,-90,"RAD & REPARATUR");
            House("Güterstraße_16","Hofstrasse_3",180,220,-90,"IMBISS AM GLEIS");
            House("Bahnhofstraße_3","Nordhof_10",234,116,90,"WASCHSALON");
            House("Bahnhofstraße_5","Nordhof_16",234,147,90);
            House("Bahnhofstraße_9","Nordhof_8",234,202,90,"LEBENSMITTEL");
            House("Bahnhofstraße_11","Hofstrasse_3",234,233,90,"PENSION ADLER");
            House("Bahnhofstraße_15","Nordhof_10",234,275,90,"BÄCKEREI KERN");
            House("Am_Bahnhof_6","Nordhof_8",178,314,0,"CAFÉ AM BAHNHOF");
            House("Am_Bahnhof_8","Hofstrasse_3",150,315,0);
            House("Hintere_Gasse_2","Nordhof_10",281,202,90);
            House("Hintere_Gasse_4","Nordhof_8",281,235,90);
            House("Hintere_Gasse_6","Hofstrasse_3",281,278,90);
            // A repaired freight building belongs to the railway, with believable loading apron.
            var shed=Group("Former freight shed – small businesses",root,new Vector3(160,.14f,163)).transform;
            B("Freight shed brick shell",shed,new Vector3(0,2.2f,0),new Vector3(20,4.4f,10),brick);
            PitchedRoof(shed,21,11,4.5f,1.7f);Shop(shed,"GÜTERHOF · ATELIER & LAGER",20,10);
            for(int i=0;i<3;i++)WestbruchProps.Pallet(root,new Vector3(150+i*2,.14f,153));
            // Courtyard stays open from the main street and the rail shortcut.
            foreach(float x in new[]{158f,187})WestbruchProps.Bin(root,new Vector3(x,.14f,207),0);
            WestbruchProps.Bench(root,new Vector3(168,.14f,205),180);
        }
        static void PitchedRoof(Transform p,float w,float d,float y,float rise)
        {
            var v=new List<Vector3>{new Vector3(-w/2,y,-d/2),new Vector3(w/2,y,-d/2),new Vector3(-w/2,y,d/2),new Vector3(w/2,y,d/2),new Vector3(-w/2,y+rise,0),new Vector3(w/2,y+rise,0)};
            Mesh("Pitched slate roof",p,v,new[]{0,4,1,1,4,5,4,2,5,5,2,3,0,2,4,1,5,3},roof,true);
        }
        static void Shop(Transform p,string title,float w,float d)
        {
            float z=-d*.5f-.13f;var green=Material("Bahnhof_ShopGreen",null,new Color(.16f,.23f,.20f),.18f);
            for(int s=-1;s<=1;s+=2)
            {
                B("Shop recessed display",p,new Vector3(s*w*.24f,1.25f,z),new Vector3(w*.32f,1.9f,.13f),glass,false);
                foreach(float yy in new[]{.28f,2.22f})B("Shop display frame",p,new Vector3(s*w*.24f,yy,z-.08f),new Vector3(w*.33f,.08f,.10f),metal,false);
            }
            Sign(title,p,new Vector3(0,2.75f,z-.09f),w*.86f,green,new Color(.88f,.83f,.66f));
            B("Faded canvas awning",p,new Vector3(0,2.48f,z-.63f),new Vector3(w*.84f,.12f,1.25f),green,false);
        }
        static void Mesh(string name,Transform p,List<Vector3> vertices,int[] triangles,Material m,bool solid)
        {
            // Separate vertices give hard architectural edges and stable texture projection.
            var v=triangles.Select(i=>vertices[i]).ToArray();var mesh=new Mesh();mesh.vertices=v;mesh.triangles=Enumerable.Range(0,v.Length).ToArray();mesh.RecalculateNormals();
            var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++){var n=mesh.normals[i];uv[i]=Mathf.Abs(n.y)>.7f?new Vector2(v[i].x,v[i].z)/3:Mathf.Abs(n.x)>.7f?new Vector2(v[i].z,v[i].y)/3:new Vector2(v[i].x,v[i].y)/3;}
            mesh.uv=uv;mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=Generated+"/Meshes/Bahnhof_"+name.Replace(' ','_')+"_"+p.name.Replace(' ','_')+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=Group(name,p,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=m;if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        static void Station()
        {
            var s=Group("Bahnhof Westbruch – accessible ticket hall",root,new Vector3(140,.14f,265),-90).transform;
            // True walk-through hall with front and rear doors, not a solid box.
            B("Station floor",s,new Vector3(0,-.08f,0),new Vector3(34,.16f,16),paving);
            foreach(float x in new[]{-16.8f,16.8f})B("Station end wall",s,new Vector3(x,3.1f,0),new Vector3(.4f,6.2f,16),brick);
            foreach(float z in new[]{-7.8f,7.8f})
            {
                foreach(float x in new[]{-10f,10f})B("Station facade wing",s,new Vector3(x,3.1f,z),new Vector3(14,6.2f,.4f),brick);
                B("Station door lintel",s,new Vector3(0,4.8f,z),new Vector3(6,2.8f,.4f),brick);
                foreach(float x in new[]{-12f,-7,7,12})B("Tall station window",s,new Vector3(x,2.7f,z+Mathf.Sign(z)*.22f),new Vector3(1.65f,3.3f,.06f),glass,false);
                B("Station stone cornice",s,new Vector3(0,5.85f,z),new Vector3(34.5f,.24f,.7f),stone,false);
            }
            PitchedRoof(s,35,17,6.3f,2.4f);
            Sign("WESTBRUCH",s,new Vector3(0,4.05f,-8.07f),5.2f,metal,new Color(.91f,.89f,.79f));
            B("Station clock dormer",s,new Vector3(0,7.0f,-7.45f),new Vector3(2.6f,2.0f,1.2f),brick);
            B("Clock dormer coping",s,new Vector3(0,8.06f,-7.45f),new Vector3(2.85f,.16f,1.5f),roof);
            var face=Cylinder("Station clock face",s,new Vector3(0,7.1f,-7.9f),.82f,.08f,Material("Bahnhof_ClockFace",null,new Color(.78f,.77f,.66f),.1f));face.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;B("Clock index",s,new Vector3(Mathf.Sin(a)*.67f,7.1f+Mathf.Cos(a)*.67f,-7.96f),new Vector3(.055f,.09f,.015f),metal,false);}
            var hands=Group("Station clock hands",s,new Vector3(0,7.1f,-7.99f));hands.AddComponent<StationClockHands>();
            B("Hour hand",hands.transform,new Vector3(0,.19f,0),new Vector3(.075f,.40f,.025f),metal,false);
            B("Minute hand",hands.transform,new Vector3(0,.28f,-.02f),new Vector3(.045f,.59f,.025f),metal,false);
            foreach(float x in new[]{-11f,11})WestbruchProps.Bench(s,new Vector3(x,0,3),0);
            foreach(float x in new[]{-8f,-5})
            {
                B("Ticket machine",s,new Vector3(x,1.02f,6.8f),new Vector3(.8f,2.04f,.65f),metal);
                B("Ticket machine screen",s,new Vector3(x,1.42f,6.46f),new Vector3(.51f,.40f,.025f),glass,false);
            }
            Sign("FAHRKARTEN     ·     ZU DEN GLEISEN →",s,new Vector3(-7,2.65f,6.3f),7,metal,Color.white);
            Sign("GLEIS 1 / 2",s,new Vector3(0,2.7f,7.55f),3.5f,metal,Color.white);
            Slab("Station rear access",130,265,4,34,paving);
            // Ground-to-platform ramp: 94m at 7.3%, with protected resting landings.
            Ramp("Platform access ramp",new Vector3(136,.14f,130),new Vector3(136,7.05f,224),3.2f);
            Slab("Ramp lower approach",135,130,8,4,paving);
            B("Platform entrance landing",root,new Vector3(129.5f,6.96f,224),new Vector3(17,.18f,4),paving);
            Stair("Main platform stairs",new Vector3(128,.14f,254),Vector3.back,38,2.5f,6.91f);
            B("Main stairs upper landing",root,new Vector3(125.8f,6.96f,240.9f),new Vector3(6.8f,.18f,3),paving);
            visits.Add(new Vector3(156,.15f,247));visits.Add(new Vector3(156,.15f,281));visits.Add(new Vector3(140,.15f,263));
        }
        static void Ramp(string name,Vector3 a,Vector3 b,float width)
        {
            var side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized*width*.5f;
            var v=new List<Vector3>{a-side,a+side,b-side,b+side,a-side-Vector3.up*.20f,a+side-Vector3.up*.20f,b-side-Vector3.up*.20f,b+side-Vector3.up*.20f};
            Mesh(name,root,v,new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,4,2,2,4,6,1,3,5,3,7,5},paving,true);
            for(int s=-1;s<=1;s+=2){var off=side*s;Beam(name+" handrail",root,a+off+Vector3.up,b+off+Vector3.up,.055f,metal);for(int i=0;i<=24;i++){var p=Vector3.Lerp(a,b,i/24f)+off;Cylinder("Ramp railing post",root,p+Vector3.up*.5f,.027f,1,metal);}}
        }
        static void Stair(string name,Vector3 at,Vector3 direction,int count,float width,float rise)
        {
            float step=rise/count;
            for(int i=0;i<count;i++)B(name,root,at+direction*(i*.34f+.17f)+Vector3.up*((i+1)*step*.5f),new Vector3(width,(i+1)*step,.34f),stone);
            var side=Vector3.Cross(Vector3.up,direction)*(width*.5f-.08f);var end=at+direction*(count*.34f)+Vector3.up*rise;
            foreach(int sign in new[]{-1,1})Beam(name+" handrail",root,at+side*sign+Vector3.up,end+side*sign+Vector3.up,.05f,metal);
        }
        static void Railway()
        {
            // Existing track at x118 remains continuous. Add a passing loop with
            // a gradual 65m turnout, returning to the original branch beyond station.
            B("Passing loop supporting retained fill",root,new Vector3(111.5f,3,274),new Vector3(8,6,150),Load("Overpass_RetainingConcrete"));
            var rail=Load("Overpass_PolishedRailHead");var ballast=Load("Overpass_RailBallast");var sleeper=Load("Overpass_ConcreteSleeper");
            Vector3 Track(float z){float offset=6.5f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(140,205,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(345,410,z)));return new Vector3(118-offset,6.66f,z);}
            for(float z=140;z<410;z+=2)
            {
                var a=Track(z);var b=Track(z+2);var t=Group("Passing loop track segment",root,(a+b)*.5f,Quaternion.LookRotation(b-a).eulerAngles.y).transform;
                B("Passing loop ballast",t,new Vector3(0,-.51f,0),new Vector3(4.5f,.48f,2.02f),ballast);
                foreach(float x in new[]{-.7515f,.7515f})B("Passing loop rail",t,new Vector3(x,0,0),new Vector3(.068f,.08f,2.02f),rail,false);
                for(float zz=-.8f;zz<1;zz+=.62f)B("Passing loop sleeper",t,new Vector3(0,-.21f,zz),new Vector3(2.58f,.16f,.24f),sleeper,false);
            }
            foreach(float x in new[]{106.9f,123f})
            {
                B("Station platform",root,new Vector3(x,3.5f,274),new Vector3(4.6f,7.1f,108),paving);
                float edge=x<118?x+2.25f:x-2.25f;
                B("Platform tactile warning strip",root,new Vector3(edge,7.058f,274),new Vector3(.45f,.018f,108),paint,false);
                for(float z=242;z<=306;z+=16)
                {
                    foreach(float side in new[]{-1f,1})Cylinder("Platform canopy column",root,new Vector3(x+side*1.65f,8.7f,z),.065f,3.3f,metal,true);
                }
                B("Platform canopy",root,new Vector3(x,10.38f,274),new Vector3(5.2f,.18f,72),roof);
                for(int i=0;i<4;i++)WestbruchProps.Bench(root,new Vector3(x,7.05f,246+i*17),90);
                var label=Group("Platform name board",root,new Vector3(x,0,270),90).transform;
                Sign(x<118?"WESTBRUCH  ·  2":"WESTBRUCH  ·  1",label,new Vector3(0,8.7f,0),3.8f,metal,Color.white);
            }
            // Passenger footbridge reaches both platforms without walking across rails.
            B("Passenger overbridge deck",root,new Vector3(115,11.75f,323),new Vector3(23,.24f,3),paving);
            foreach(float x in new[]{106.9f,123f})Stair("Platform footbridge stairs",new Vector3(x,7.05f,313),Vector3.forward,26,2.1f,4.82f);
            foreach(float z in new[]{321.55f,324.45f}){Beam("Passenger bridge handrail",root,new Vector3(103.5f,12.85f,z),new Vector3(126.5f,12.85f,z),.06f,metal);for(float x=104;x<127;x+=1.4f)Cylinder("Bridge baluster",root,new Vector3(x,12.3f,z),.025f,1.1f,metal);}
            foreach(float x in new[]{105.3f,124.5f})B("Passenger bridge support",root,new Vector3(x,9.2f,323),new Vector3(.25f,5.1f,.25f),metal);
        }
        static void StreetLife()
        {
            var template=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name.StartsWith("Old European streetlight "));
            int n=100;
            foreach(float z in new[]{103f,132,162,194,224,255,286,311})foreach(float x in new[]{200f,216})
            {var lamp=UnityEngine.Object.Instantiate(template.gameObject,root);lamp.name="Old European streetlight "+n++;lamp.transform.SetPositionAndRotation(new Vector3(x,.14f,z),Quaternion.Euler(0,x<208?90:-90,0));}
            foreach(var p in new[]{new Vector3(165,.14f,87),new Vector3(184,.14f,88),new Vector3(156,.14f,239),new Vector3(156,.14f,290),new Vector3(186,.14f,290),new Vector3(255,.14f,195),new Vector3(255,.14f,278)})
            {var lamp=UnityEngine.Object.Instantiate(template.gameObject,root);lamp.name="Old European streetlight "+n++;lamp.transform.SetPositionAndRotation(p,Quaternion.Euler(0,0,0));}
            var tree=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(t=>t.name.StartsWith("Urban tree "));
            foreach(var p in new[]{new Vector3(171,.14f,242),new Vector3(171,.14f,288),new Vector3(192,.14f,234),new Vector3(194,.14f,298),new Vector3(251,-.03f,218),new Vector3(251,-.03f,250),new Vector3(154,-.03f,185),new Vector3(190,-.03f,131),new Vector3(225,-.03f,320),new Vector3(278,-.03f,319)})
            {var t=UnityEngine.Object.Instantiate(tree.gameObject,root);t.name="Bahnhof street tree";t.transform.position=p;t.transform.Rotate(0,n++*73,0);}
            foreach(var p in new[]{new Vector3(169,.14f,245),new Vector3(169,.14f,285),new Vector3(190,.14f,290)})WestbruchProps.Bench(root,p,90);
            var bike=GameObject.Find("Resident bicycle");if(bike)for(int i=0;i<7;i++){var b=UnityEngine.Object.Instantiate(bike,root);b.name="Station commuter bicycle";b.transform.SetPositionAndRotation(new Vector3(154+i*1.1f,.14f,292),Quaternion.Euler(0,90+i%2*8,0));}
            foreach(var p in new[]{new Vector3(166,.14f,245),new Vector3(187,.14f,290)})WestbruchProps.Bin(root,p,0);
            var kiosk=Group("Station kiosk",root,new Vector3(188,.14f,239),90).transform;
            B("Kiosk shell",kiosk,new Vector3(0,1.5f,0),new Vector3(6,3,4),brick);B("Kiosk roof",kiosk,new Vector3(0,3.1f,0),new Vector3(6.6f,.2f,4.8f),roof);Shop(kiosk,"KIOSK · ZEITUNGEN",6,4);
            var shelter=Group("Bahnhofsplatz bus shelter",root,new Vector3(199,.14f,251),90).transform;
            foreach(float x in new[]{-2.5f,2.5f})foreach(float z in new[]{-.7f,.7f})Cylinder("Bus shelter post",shelter,new Vector3(x,1.25f,z),.045f,2.5f,metal,true);
            B("Shelter roof",shelter,new Vector3(0,2.55f,0),new Vector3(5.7f,.16f,2.2f),metal);B("Shelter back glazing",shelter,new Vector3(0,1.3f,.75f),new Vector3(5.2f,2.2f,.045f),glass);
            WestbruchProps.Bench(shelter,Vector3.zero,0);Sign("H  BAHNHOFSPLATZ",shelter,new Vector3(0,2.2f,-.83f),3.7f,metal,Color.white);
            Sign("WESTBRUCH  ←     BAHNHOF  ↑",root,new Vector3(198,2.5f,110),5,metal,new Color(.91f,.88f,.7f));
            Cylinder("Wayfinding signpost",root,new Vector3(198,1.25f,110),.045f,2.5f,metal,true);
            // Existing authored cars, already compatible with weather and headlights.
            WestbruchVehicleArt.Create(root,0,2,false,false,new Vector3(190,.14f,281),0).name="Bahnhofsplatz parked car";
            WestbruchVehicleArt.Create(root,1,4,false,false,new Vector3(220,.14f,258),180).name="Resident parked car";
            visits.AddRange(new[]{new Vector3(196,.15f,251),new Vector3(169,.15f,244),new Vector3(169,.15f,284),new Vector3(188,.15f,245),new Vector3(175,.15f,199),new Vector3(155,.15f,292)});
            foreach(var at in new[]{new Vector3(145,4.8f,257),new Vector3(145,4.8f,274),new Vector3(123,10.15f,250),new Vector3(123,10.15f,280),new Vector3(123,10.15f,304),new Vector3(107,10.15f,250),new Vector3(107,10.15f,280),new Vector3(107,10.15f,304)})
            {
                var go=Group("Station street canopy luminaire",root,at);go.transform.rotation=Quaternion.Euler(90,0,0);
                var l=go.AddComponent<Light>();l.type=LightType.Spot;l.range=18;l.spotAngle=115;l.innerSpotAngle=85;l.lightUnit=LightUnit.Candela;l.intensity=650;l.useColorTemperature=true;l.colorTemperature=3400;l.shadows=LightShadows.None;
                var hd=go.AddComponent<HDAdditionalLightData>();hd.fadeDistance=270;hd.volumetricDimmer=0;
            }
            // Modest boundary is formed by rear yards and railway fencing, rather than a district gate.
            foreach(float z in new[]{338f,350})B("Rail service storage",root,new Vector3(160,1.5f,z),new Vector3(18,3,7),brick);
        }
        internal static void Navigation(bool addResidents=true)
        {
            var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();world.UnloadNavigation();
            Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();var ignore=new List<NavMeshBuildMarkup>();
            foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>())ignore.Add(new NavMeshBuildMarkup{root=car.transform,ignoreFromBuild=true});
            foreach(var person in world.people)ignore.Add(new NavMeshBuildMarkup{root=person.transform,ignoreFromBuild=true});
            var bounds=new Bounds(new Vector3(80,6,116),new Vector3(450,30,485));
            UnityEngine.AI.NavMeshBuilder.CollectSources(bounds,1,NavMeshCollectGeometry.PhysicsColliders,0,ignore,sources);
            var originalPlan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            foreach(var road in originalPlan.roads)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(road.x,.5f,road.z),Quaternion.identity,Vector3.one),size=new Vector3(road.w,4,road.d),area=1});
            foreach(var road in new[]{new Vector4(184,80,48,10),new Vector4(208,193,10,226),new Vector4(236,176,56,9),new Vector4(236,306,56,9),new Vector4(264,241,9,130)})sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(road.x,.5f,road.y),Quaternion.identity,Vector3.one),size=new Vector3(road.z,4,road.w),area=1});
            if(!world.crossings.Any(c=>c.a.x>190)){var list=world.crossings.ToList();foreach(float z in new[]{151f,228,294})list.Add(new PedestrianWorld.Crossing{a=new Vector3(201,.14f,z),b=new Vector3(215,.14f,z)});world.crossings=list.ToArray();}
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.34f;settings.agentHeight=1.8f;settings.agentClimb=.27f;settings.agentSlope=40;settings.overrideVoxelSize=true;settings.voxelSize=.12f;settings.minRegionArea=1;
            var data=UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)throw new Exception("Expanded navigation failed");
            const string path=Generated+"/BahnhofNavigation.asset";var prior=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);if(prior){EditorUtility.CopySerialized(data,prior);UnityEngine.Object.DestroyImmediate(data);data=prior;}else AssetDatabase.CreateAsset(data,path);world.navigation=data;world.LoadNavigation();
            var places=world.places.ToList();foreach(var p in visits)
                if(NavMesh.SamplePosition(p,out var hit,1.4f,NavMesh.AllAreas)&&Mathf.Abs(hit.position.y-p.y)<.6f)
                    places.Add(new PedestrianWorld.Place{label="Bahnhofsviertel "+places.Count,activity=places.Count%3==0?PedestrianWorld.Activity.Visit:PedestrianWorld.Activity.Shop,approach=hit.position,facing=Quaternion.Euler(0,90,0)});
            world.places=places.ToArray();var people=world.people.ToList();var added=places.Skip(places.Count-Mathf.Min(visits.Count,places.Count-25)).ToArray();
            for(int i=0;addResidents&&i<10;i++)
            {
                var place=added[(i*3)%added.Length];var clone=UnityEngine.Object.Instantiate(world.people[i%world.people.Length].gameObject,root);clone.name="Station resident "+i;clone.transform.position=place.approach;
                var person=clone.GetComponent<PedestrianAgent>();person.world=world;person.personality=18+i;person.initialPlace=-1;person.destination=person.previousPlace=-1;person.agent.enabled=false;people.Add(person);
            }
            world.people=people.ToArray();EditorUtility.SetDirty(world);world.UnloadNavigation();
        }
        static void Traffic()
        {
            var world=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();var nodes=world.nodes.ToList();var links=world.links.ToList();
            int old=nodes.FindIndex(p=>Mathf.Abs(p.x-94)<.1f&&Mathf.Abs(p.z-80)<.1f);if(old<0)throw new Exception("Nordring traffic junction missing");
            int Add(float x,float z){nodes.Add(new Vector3(x,.04f,z));return nodes.Count-1;}
            void Link(int a,int b){links.Add(new WestbruchTraffic.Link{a=a,b=b,offset=2.2f,speed=5.2f});}
            int a1=Add(151,80),a2=Add(208,80),a3=Add(208,176),a4=Add(208,306),a5=Add(264,306),a6=Add(264,176);
            Link(old,a1);Link(a1,a2);Link(a2,a3);Link(a3,a4);Link(a4,a5);Link(a5,a6);Link(a6,a3);
            world.nodes=nodes.ToArray();world.links=links.ToArray();EditorUtility.SetDirty(world);
        }
        public static void Refine()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
                if(GameObject.Find(RootName).transform.Find("BV2_Continuous surfaces")){BahnhofDistrictRework.Apply();return;}
                RefineLoaded();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void RefineLoaded()
        {
                Clear();root=GameObject.Find(RootName).transform;
                paving=Load("Paving");metal=Load("DressingMetal");
                foreach(var t in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Bahnhof render chunk ")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(!f.GetComponentInParent<LODGroup>()&&!f.GetComponentInParent<PedestrianAgent>()&&!f.GetComponentInParent<SimpleCar>()&&f.GetComponent<MeshRenderer>())f.GetComponent<MeshRenderer>().enabled=true;
                foreach(var f in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Pitched slate roof"))
                {
                    var m=f.sharedMesh;if(m.normals[0].y>=0)continue;var t=m.triangles;
                    for(int i=0;i<t.Length;i+=3){int a=t[i+1];t[i+1]=t[i+2];t[i+2]=a;}m.triangles=t;m.RecalculateNormals();m.RecalculateTangents();EditorUtility.SetDirty(m);
                    var c=f.GetComponent<MeshCollider>();if(c){c.sharedMesh=null;c.sharedMesh=m;}
                }
                if(!root.Find("Passing loop earth formation"))
                {
                    var v=new List<Vector3>();var t=new List<int>();const int rows=135;
                    for(int i=0;i<=rows;i++)
                    {
                        float z=140+i*2,offset=6.5f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(140,205,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(345,410,z))),x=118-offset;
                        v.AddRange(new[]{new Vector3(x-8,-.04f,z),new Vector3(x-2.5f,5.95f,z),new Vector3(x+2.5f,5.95f,z),new Vector3(x+8,-.04f,z)});
                        if(i==rows)continue;int n=i*4;for(int j=0;j<3;j++)t.AddRange(new[]{n+j,n+j+4,n+j+1,n+j+1,n+j+4,n+j+5});
                    }
                    Mesh("Passing loop earth formation",root,v,t.ToArray(),Material("Bahnhof_RailEarth","grass",new Color(.47f,.48f,.36f),.08f),true);
                }
                foreach(var lamp in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Old European streetlight ")&&Mathf.Abs(t.position.x-165)<.1f&&Mathf.Abs(t.position.z-80)<.1f))lamp.position+=Vector3.forward*7;
                if(!root.Find("Platform edge alignment complete"))
                {
                    foreach(var tr in root.Cast<Transform>().ToArray())
                    {
                        if(!(tr.name.StartsWith("Platform")||tr.name=="Station platform"||tr.name=="Worn public bench"||tr.name=="Station street canopy luminaire"))continue;
                        var p=tr.position;if(p.z<220||p.z>329||p.y<3)continue;
                        if(p.x>119&&p.x<126)p.x-=1;else if(p.x>103&&p.x<110)p.x+=.6f;tr.position=p;
                    }
                    Group("Platform edge alignment complete",root,Vector3.zero);
                }
                var terrain=Terrain.activeTerrain;var td=terrain.terrainData;int resolution=td.heightmapResolution;var heights=td.GetHeights(0,0,resolution,resolution);var origin=terrain.transform.position;
                for(int iz=0;iz<resolution;iz++)for(int ix=0;ix<resolution;ix++)
                {
                    float x=origin.x+ix/(resolution-1f)*td.size.x,z=origin.z+iz/(resolution-1f)*td.size.z;
                    if(x>156&&x<173&&z>70&&z<90)heights[iz,ix]=(-.035f-origin.y)/td.size.y;
                }
                td.SetHeights(0,0,heights);EditorUtility.SetDirty(td);
                if(!root.Find("Station rear property boundary"))
                {
                    var boundary=Group("Station rear property boundary",root,Vector3.zero).transform;
                    foreach(var line in new[]{new Vector4(300,84,300,364),new Vector4(134,364,300,364),new Vector4(134,118,164,118)})
                    {
                        var a=new Vector3(line.x,0,line.y);var b=new Vector3(line.z,0,line.w);int count=Mathf.CeilToInt(Vector3.Distance(a,b)/2.4f);
                        for(int i=0;i<=count;i++)Cylinder("Rear yard fence post",boundary,Vector3.Lerp(a,b,i/(float)count)+Vector3.up*.65f,.035f,1.3f,metal);
                        foreach(float y in new[]{.3f,.75f,1.25f})Beam("Rear yard fence rail",boundary,a+Vector3.up*y,b+Vector3.up*y,.025f,metal);
                        var barrier=B("Rear property fence collision",boundary,(a+b)/2+Vector3.up*.65f,new Vector3(.09f,1.3f,Vector3.Distance(a,b)),metal);barrier.transform.rotation=Quaternion.LookRotation(b-a);barrier.GetComponent<Renderer>().enabled=false;
                    }
                    var tree=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).First(t=>t.name.StartsWith("Urban tree "));
                    for(int i=0;i<15;i++)
                    {
                        var clone=UnityEngine.Object.Instantiate(tree.gameObject,root);clone.name="Railway neighbourhood perimeter tree";
                        clone.transform.position=i<8?new Vector3(294,-.03f,112+i*32):new Vector3(145+(i-8)*23,-.03f,358);clone.transform.Rotate(0,i*83,0);
                    }
                }
                if(!root.Find("Regional train – parked at platform one"))
                {
                    var train=Group("Regional train – parked at platform one",root,new Vector3(118,6.66f,263)).transform;
                    var body=Material("Bahnhof_RegionalCream",null,new Color(.66f,.65f,.56f),.32f,.25f);
                    var stripe=Material("Bahnhof_RegionalGreen",null,new Color(.12f,.26f,.24f),.3f,.3f);
                    var window=Material("Bahnhof_TrainGlass",null,new Color(.07f,.12f,.14f),.66f,.25f);
                    var rubber=Material("Bahnhof_TrainBogie",null,new Color(.075f,.08f,.07f),.15f,.4f);
                    foreach(float z in new[]{-11.3f,11.3f})
                    {
                        var car=Group("Regional diesel railcar",train,new Vector3(0,0,z)).transform;
                        B("Train body",car,new Vector3(0,2.13f,0),new Vector3(2.85f,2.8f,21.5f),body);
                        B("Train roof",car,new Vector3(0,3.58f,0),new Vector3(2.75f,.16f,21),roof=Load("Bahnhof_Slate"));
                        B("Underframe",car,new Vector3(0,.65f,0),new Vector3(2.5f,.38f,19),rubber);
                        foreach(float x in new[]{-1.435f,1.435f})
                        {
                            B("Regional livery stripe",car,new Vector3(x,1.35f,0),new Vector3(.025f,.45f,21),stripe,false);
                            for(float z1=-8;z1<=8;z1+=2.25f)B("Passenger train window",car,new Vector3(x,2.55f,z1),new Vector3(.035f,.95f,1.65f),window,false);
                            foreach(float z1 in new[]{-6.8f,6.8f})B("Closed passenger door",car,new Vector3(x,1.8f,z1),new Vector3(.045f,2.05f,1.1f),stripe,false);
                        }
                        foreach(float z1 in new[]{-10.77f,10.77f})B("Driver windscreen",car,new Vector3(0,2.6f,z1),new Vector3(2.3f,1.0f,.035f),window,false);
                        foreach(float z1 in new[]{-7.4f,-5.6f,5.6f,7.4f})foreach(float x in new[]{-1.18f,1.18f})
                        {var wheel=Cylinder("Rail wheel",car,new Vector3(x,.43f,z1),.43f,.18f,rubber);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
                        foreach(float z1 in new[]{-3f,3f})B("Roof ventilation",car,new Vector3(0,3.78f,z1),new Vector3(1.8f,.28f,2.5f),metal);
                    }
                    B("Flexible train coupling",train,new Vector3(0,1.8f,0),new Vector3(2.4f,2.5f,1.2f),rubber);
                }
                var stationFace=GameObject.Find("Bahnhof Westbruch – accessible ticket hall").transform;
                foreach(var tr in stationFace.GetComponentsInChildren<Transform>())
                {
                    var p=tr.localPosition;
                    if(tr.name=="Station clock face")p.z=-8.12f;else if(tr.name=="Clock index")p.z=-8.18f;else if(tr.name=="Station clock hands")p.z=-8.21f;else continue;tr.localPosition=p;
                }
                foreach(var tr in root.GetComponentsInChildren<Transform>())
                {
                    var p=tr.localPosition;if(tr.name=="Train body")p.y=1.93f;else if(tr.name=="Train roof")p.y=3.4f;else if(tr.name=="Roof ventilation"){p.y=3.54f;tr.localScale=new Vector3(1,.45f,1);}else continue;tr.localPosition=p;
                }
                foreach(var hidden in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Rear property fence collision"))hidden.enabled=false;
                Navigation(false);Batch();Physics.SyncTransforms();        }
        public static void AuditConnection()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Physics.SyncTransforms();var lines=new List<string>();
            foreach(var c in Physics.OverlapBox(new Vector3(165,1,80),new Vector3(2,1,3),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                lines.Add(c.name+" | "+c.GetType().Name+" | "+c.bounds+" | parent "+c.transform.parent.name);
            File.WriteAllLines("QA/bahnhof-connection-colliders.txt",lines);EditorApplication.Exit(0);
        }
        public static void Polish()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();visits.Clear();root=GameObject.Find(RootName).transform;
                paving=Load("Paving");brick=Load("Bahnhof_OldBrick");roof=Load("Bahnhof_Slate");stone=Load("PropConcrete");metal=Load("DressingMetal");
                var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName).transform;
                foreach(var batch in bridge.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Local render batch ")).ToArray())UnityEngine.Object.DestroyImmediate(batch.gameObject);
                foreach(var f in bridge.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<MeshRenderer>())f.GetComponent<MeshRenderer>().enabled=true;
                foreach(var t in bridge.GetComponentsInChildren<Transform>().Where(t=>t.name=="Chain link diagonal"&&Mathf.Abs(t.position.x-164)<.8f&&t.position.z>70&&t.position.z<90).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                WestbruchBridgeStart.BakeLocal(bridge,"EngineeredOverpass");
                var station=GameObject.Find("Bahnhof Westbruch – accessible ticket hall").transform;
                if(!station.Find("Station clock dormer")){B("Station clock dormer",station,new Vector3(0,7,-7.45f),new Vector3(2.6f,2,1.2f),brick);B("Clock dormer coping",station,new Vector3(0,8.06f,-7.45f),new Vector3(2.85f,.16f,1.5f),roof);}
                Navigation(false);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Validate()
        {
            Physics.SyncTransforms();
            foreach(var p in new[]{new Vector3(165,1,80),new Vector3(190,1,80),new Vector3(208,1,140),new Vector3(173,1,265),new Vector3(140,1,265)})
                if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out _,5,1,QueryTriggerInteraction.Ignore))throw new Exception("No ground at "+p);
            if(root.GetComponentsInChildren<LODGroup>().Length<8)throw new Exception("Street trees missing");
            var nav=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();if(nav.people.Length<28)throw new Exception("Station residents missing");
        }
        static void Batch()
        {
            // Spatial chunks retain culling at street scale. Keep LOD trees,
            // characters, cars, labels and animated clock hands independent.
            var sources=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.isStatic&&f.sharedMesh&&f.sharedMesh.isReadable&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&!f.GetComponent<TextMesh>()&&!f.GetComponentInParent<LODGroup>()&&!f.GetComponentInParent<SimpleCar>()&&!f.GetComponentInParent<PedestrianAgent>()&&!f.GetComponentInParent<StationClockHands>()).ToArray();
            foreach(var group in sources.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial.name+"_"+Mathf.FloorToInt(f.transform.position.x/40)+"_"+Mathf.FloorToInt(f.transform.position.z/40)))
            {
                var list=group.ToArray();var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);
                string path=Generated+"/Meshes/BahnhofBatch_"+group.Key+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(previous){EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);mesh=previous;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
                var chunk=Group("Bahnhof render chunk "+group.Key,root,Vector3.zero);chunk.isStatic=true;chunk.AddComponent<MeshFilter>().sharedMesh=mesh;chunk.AddComponent<MeshRenderer>().sharedMaterial=list[0].GetComponent<MeshRenderer>().sharedMaterial;
                foreach(var f in list)f.GetComponent<MeshRenderer>().enabled=false;
            }
        }
    }
}
