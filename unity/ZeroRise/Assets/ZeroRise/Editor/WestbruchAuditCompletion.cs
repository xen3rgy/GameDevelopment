using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    // Completes the specific locations recorded in MAP-AUDIT-2026-11-02.
    internal static class WestbruchAuditCompletion
    {
        static Transform root,district;
        static Material stone,iron,wood,earth,brick,paint;
        static GameObject[] trees;static GameObject shrub;
        static readonly List<MeshFilter> removedSources=new List<MeshFilter>();
        internal static void Apply(Transform neighbourhood)
        {
            district=neighbourhood;var old=district.Find("BV4_Audit completion");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            root=Group("BV4_Audit completion",district,Vector3.zero).transform;removedSources.Clear();
            stone=Material("BV4_Sandstone","concrete",new Color(.54f,.54f,.47f),.10f);
            iron=Material("BV4_PaintedIron",null,new Color(.15f,.23f,.22f),.22f,.35f);
            wood=Material("BV4_SeatTimber","wood",new Color(.45f,.32f,.20f),.12f);
            earth=Material("BV4_GardenEarth","grass",new Color(.70f,.78f,.61f),.06f);
            brick=Material("BV4_GardenBrick","brick",new Color(.68f,.60f,.48f),.08f);
            paint=Material("BV4_BayPaint",null,new Color(.67f,.67f,.53f),.06f);
            trees=UnityEngine.Object.FindObjectsByType<LODGroup>().Where(t=>t.name.StartsWith("Urban tree ")).GroupBy(t=>t.GetComponentInChildren<MeshFilter>().sharedMesh).Select(g=>g.First().gameObject).ToArray();
            shrub=UnityEngine.Object.FindObjectsByType<LODGroup>().First(t=>t.name.StartsWith("Dense low courtyard shrub")).gameObject;
            RemoveSkyline();OpenPromenadeConnections();ContextFrontages();FinishBankToe();Square();RepairCourt();IndividualAddresses();CorrectSigns();
            RemoveRetainedTriangles();Physics.SyncTransforms();Validate();
        }
        static GameObject B(string n,Transform p,Vector3 at,Vector3 size,Material mat,bool solid=true,float tile=2)
            =>Box("BV4_"+n,p,at,size,mat,solid,tile);
        static void RemoveSkyline()
        {
            var old=district.root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Neighbouring skyline – visual background only");
            if(!old)return;
            // Retain the editable sources (hidden), so rerunning the completion
            // pass can identify exactly the same triangles in the global batches.
            foreach(var f in old.GetComponentsInChildren<MeshFilter>())removedSources.Add(f);
        }
        static string Point(Vector3 p)=>Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.y*1000)+","+Mathf.RoundToInt(p.z*1000);
        static string Tri(Vector3 a,Vector3 b,Vector3 c){var p=new[]{Point(a),Point(b),Point(c)};Array.Sort(p,StringComparer.Ordinal);return string.Join("|",p);}
        static void OpenPromenadeConnections()
        {
            // The former map boundary is retained as a garden fence, with
            // genuine openings at the two new paths, including the old batches.
            foreach(int side in new[]{-1,1})
            {
                Bounds SourceBounds(BoxCollider c)=>c.GetComponent<MeshRenderer>().bounds;
                var panels=district.root.GetComponentsInChildren<BoxCollider>(true)
                    .Where(c=>c.name=="Fence collider rail"&&Mathf.Abs(SourceBounds(c).center.z-side*113.5f)<.1f&&SourceBounds(c).size.z<.3f&&SourceBounds(c).max.x>82&&SourceBounds(c).min.x<86).ToArray();
                if(panels.Length==0)throw new InvalidOperationException("Missing former boundary at promenade connection "+side);
                float left=panels.Min(c=>SourceBounds(c).min.x),right=panels.Max(c=>SourceBounds(c).max.x);
                foreach(var c in panels)c.enabled=false;
                foreach(var f in district.root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(f.name!="Fence post"&&f.name!="Upper rail"&&f.name!="Lower rail"&&f.name!="Chain link diagonal"&&f.name!="Fence collider rail")continue;
                    if(Mathf.Abs(f.transform.position.z-side*113.5f)>.1f||f.transform.position.x<=left+.1f||f.transform.position.x>=right-.1f)continue;
                    removedSources.Add(f);
                    foreach(var c in f.GetComponents<Collider>())c.enabled=false;
                }
            }
        }
        static void RemoveRetainedTriangles()
        {
            var cuts=new Dictionary<Material,HashSet<string>>();
            foreach(var f in removedSources.Distinct())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!f.sharedMesh||!f.sharedMesh.isReadable)continue;
                if(!cuts.TryGetValue(r.sharedMaterial,out var set))cuts[r.sharedMaterial]=set=new HashSet<string>();
                var v=f.sharedMesh.vertices;var t=f.sharedMesh.triangles;
                for(int i=0;i<t.Length;i+=3)set.Add(Tri(f.transform.TransformPoint(v[t[i]]),f.transform.TransformPoint(v[t[i+1]]),f.transform.TransformPoint(v[t[i+2]])));
                r.enabled=false;
            }
            foreach(var f in district.root.GetComponentsInChildren<MeshFilter>().Where(f=>!f.transform.IsChildOf(district)))
            {
                var r=f.GetComponent<MeshRenderer>();var m=f.sharedMesh;
                if(!r||!r.enabled||!r.sharedMaterial||!cuts.TryGetValue(r.sharedMaterial,out var set)||!m||!m.isReadable)continue;
                var v=m.vertices;var t=m.triangles;var keep=new List<int>();
                for(int i=0;i<t.Length;i+=3)
                    if(!set.Contains(Tri(f.transform.TransformPoint(v[t[i]]),f.transform.TransformPoint(v[t[i+1]]),f.transform.TransformPoint(v[t[i+2]]))))keep.AddRange(new[]{t[i],t[i+1],t[i+2]});
                if(keep.Count==t.Length)continue;m.SetTriangles(keep,0);m.RecalculateBounds();EditorUtility.SetDirty(m);
                if(keep.Count==0)r.enabled=false;
            }
        }
        static void ContextFrontages()
        {
            var terrain=Terrain.activeTerrain;var data=terrain.terrainData;int n=data.heightmapResolution;var h=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float wx=origin.x+x/(n-1f)*data.size.x,wz=origin.z+z/(n-1f)*data.size.z;
                if(wx>=-200&&wx<=104&&Mathf.Abs(wz)>=132&&Mathf.Abs(wz)<=280||wx>=82.4f&&wx<=85.6f&&Mathf.Abs(wz)>=110&&Mathf.Abs(wz)<=140)
                    h[z,x]=(-.20f-origin.y)/data.size.y;
            }
            data.SetHeights(0,0,h);EditorUtility.SetDirty(data);
            string[] templates={"BV2_House_Bahnhofstraße_5","BV2_House_Güterstraße_12","BV2_House_Bahnhofstraße_15","BV2_House_Güterstraße_16"};
            for(int side=-1;side<=1;side+=2)
            {
                var frontage=Group("BV4_Connected residential gardens "+side,root,Vector3.zero).transform;
                B("Ground residential continuation",frontage,new Vector3(-48,-.155f,side*206),new Vector3(304,.20f,148),earth,true,2);
                B("Neighbourhood garden promenade",frontage,new Vector3(-4,.04f,side*142),new Vector3(208,.20f,4),BahnhofDetailKit.Load("Paving"));
                B("Promenade connection",frontage,new Vector3(84,.04f,side*125),new Vector3(3.2f,.20f,30),BahnhofDetailKit.Load("Paving"));
                for(int i=0;i<4;i++)
                {
                    float x=-82+i*53;var source=district.Find(templates[(i+(side>0?1:0))%4]);
                    var house=UnityEngine.Object.Instantiate(source.gameObject,frontage);house.name="BV4_Context residence "+side+"_"+i;
                    house.transform.SetPositionAndRotation(new Vector3(x,.14f,side*184),Quaternion.Euler(0,side>0?0:180,0));
                    foreach(var r in house.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
                    foreach(var sign in house.GetComponentsInChildren<FittedWorldSign>())
                    {if(sign.lettering.text.Length<4)sign.lettering.text=(40+i+(side>0?10:0)).ToString();}
                    float threshold=184-source.GetComponent<BoxCollider>().size.z*.5f;
                    B("Private entrance path",frontage,new Vector3(x,.04f,side*(144+threshold)*.5f),new Vector3(2.6f,.20f,threshold-144),BahnhofDetailKit.Load("Paving"));
                    foreach(float half in new[]{-1f,1})
                        Wall(frontage,new Vector3(x+half*9,-.055f,side*158),new Vector3(14,.95f,.24f));
                    foreach(float dx in new[]{-17f,17})
                        Wall(frontage,new Vector3(x+dx,-.055f,side*178),new Vector3(.24f,.90f,40));
                    Wall(frontage,new Vector3(x,-.055f,side*198),new Vector3(34,.95f,.24f));
                    Tree(frontage,x-9,side*165,i+(side>0?3:0),.95f);
                    Tree(frontage,x+12,side*199,i+2,.80f);
                    for(int j=0;j<9;j++)
                    {
                        float offset=-15+j*3.7f;
                        if(Mathf.Abs(offset)<2.2f)continue; // Keep the entrance path and wall opening clear.
                        Shrub(frontage,new Vector3(x+offset,-.055f,side*160),.72f,j+i);
                    }
                    var store=Group("BV4_Garden cycle shelter",frontage,new Vector3(x+11,-.055f,side*174),side>0?0:180).transform;
                    B("Cycle store back",store,new Vector3(0,1.05f,1.3f),new Vector3(4,2.1f,.16f),wood);
                    foreach(float sx in new[]{-1.9f,1.9f})B("Cycle store post",store,new Vector3(sx,1.1f,0),new Vector3(.12f,2.2f,2.8f),wood);
                    B("Cycle store supported roof",store,new Vector3(0,2.22f,0),new Vector3(4.4f,.14f,3.2f),iron);
                    var post=Group("BV4_Promenade lantern",frontage,new Vector3(x,.14f,side*143)).transform;
                    Cylinder("BV4_Promenade light post",post,new Vector3(0,2.25f,0),.055f,4.5f,iron,true);
                    B("Promenade lantern cap",post,new Vector3(0,4.59f,0),new Vector3(.52f,.08f,.52f),iron,false);
                    B("Promenade lantern diffuser",post,new Vector3(0,4.50f,0),new Vector3(.34f,.10f,.34f),BahnhofDetailKit.Load("StreetlightFrostedGlass"),false);
                    Lamp(new Vector3(x,4.60f,side*143),180,17,side>0?"North promenade":"South promenade");
                }
                for(int i=0;i<22;i++)
                {
                    Tree(frontage,-187+i*13,side*(223+Mathf.Sin(i*2.4f)*12),i,.70f+(i%4)*.19f);
                    Tree(frontage,-181+i*13,side*(246+Mathf.Sin(i*1.7f)*8),i+3,1.05f+(i%3)*.16f);
                }
            }
        }
        static void FinishBankToe()
        {
            // The new gardens lie lower than the retained rail embankment.
            // A real soil shoulder supports its original outer edge, without
            // lowering the bank mesh or leaving the existing grass suspended.
            Physics.SyncTransforms();
            foreach(int side in new[]{-1,1})
            {
                var v=new List<Vector3>();var t=new List<int>();const int columns=6,rows=74;
                for(int row=0;row<=rows;row++)
                {
                    float z=side*Mathf.Lerp(132,280,row/(float)rows),edge=-.055f;
                    foreach(var hit in Physics.RaycastAll(new Vector3(103.30f,16,z),Vector3.down,32,1,QueryTriggerInteraction.Ignore))
                        if(hit.collider.name.Contains("railway earth bank"))edge=Mathf.Max(edge,hit.point.y);
                    for(int col=0;col<columns;col++)
                    {
                        float f=col/(columns-1f);v.Add(new Vector3(Mathf.Lerp(100.5f,103.30f,f),Mathf.Lerp(-.055f,edge,f*f*(3-2*f)),z));
                        if(row==rows||col==columns-1)continue;int a=row*columns+col;
                        if(side>0)t.AddRange(new[]{a,a+columns,a+1,a+1,a+columns,a+columns+1});
                        else t.AddRange(new[]{a,a+1,a+columns,a+1,a+columns+1,a+columns});
                    }
                }
                BahnhofDetailKit.Mesh("BV4_Supported bank garden shoulder "+side,root,v,t,earth,true);
            }
        }
        static void Wall(Transform parent,Vector3 bottom,Vector3 size)
        {
            B("Garden enclosure",parent,bottom+Vector3.up*(size.y*.5f),size,brick,true,1.1f);
            B("Garden coping",parent,bottom+Vector3.up*(size.y+.035f),new Vector3(size.x+.10f,.07f,size.z+.10f),stone,false);
        }
        static void Tree(Transform parent,float x,float z,int seed,float scale)
        {
            var source=trees[Math.Abs(seed)%trees.Length];var t=UnityEngine.Object.Instantiate(source,parent);t.name="BV4_Mixed age tree "+seed;
            t.transform.SetPositionAndRotation(new Vector3(x,-.08f,z),Quaternion.Euler(0,seed*137.5f,0));
            t.transform.localScale=Vector3.Scale(source.transform.lossyScale,new Vector3(scale*(.88f+seed%3*.09f),scale,scale*(1.04f-seed%3*.06f)));
        }
        static void Shrub(Transform p,Vector3 at,float scale,int seed)
        {
            var t=UnityEngine.Object.Instantiate(shrub,p);t.name="BV4_Rooted mixed garden shrub";t.transform.localPosition=at-Vector3.up*.025f;
            t.transform.localRotation=Quaternion.Euler(0,seed*137.5f,0);t.transform.localScale=Vector3.one*scale;
        }
        static void PlantedIsland(Vector3 center,Vector2 size)
        {
            var p=Group("BV4_Planting island",root,center).transform;
            B("Raised retained soil",p,new Vector3(0,.13f,0),new Vector3(size.x-.3f,.26f,size.y-.3f),BahnhofDetailKit.Load("GardenEarth"));
            foreach(int side in new[]{-1,1})
            {
                B("Planting edge",p,new Vector3(side*size.x*.5f,.19f,0),new Vector3(.24f,.38f,size.y+.24f),stone);
                B("Planting edge",p,new Vector3(0,.19f,side*size.y*.5f),new Vector3(size.x,.38f,.24f),stone);
            }
            for(int i=0;i<8;i++)Shrub(p,new Vector3(-size.x*.32f+(i%4)*size.x*.21f,.25f,(i<4?-1:1)*size.y*.24f),.67f,i);
        }
        static void Square()
        {
            foreach(float z in new[]{258f,274})PlantedIsland(new Vector3(161,.14f,z),new Vector2(6,5));
            PlantedIsland(new Vector3(180,.14f,257),new Vector2(10,4));
            PlantedIsland(new Vector3(180,.14f,273),new Vector2(10,3));
            foreach(float z in new[]{249f,289})
            {
                var p=Group("BV4_Place lantern standard",root,new Vector3(180,.14f,z)).transform;
                Cylinder("BV4_Lantern iron column",p,new Vector3(0,2.25f,0),.055f,4.5f,iron,true);
                B("Lantern supported shade",p,new Vector3(0,4.5f,0),new Vector3(.55f,.10f,.55f),iron,false);
                B("Lantern diffuser",p,new Vector3(0,4.43f,0),new Vector3(.35f,.09f,.35f),BahnhofDetailKit.Load("StreetlightFrostedGlass"),false);
                Lamp(new Vector3(180,4.48f,z),210,19,"Station square");
            }
            foreach(var point in new[]{new Vector3(157,.14f,253),new Vector3(157,.14f,279),new Vector3(176,.14f,252),new Vector3(176,.14f,285)})
            {
                B("Pedestrian lantern base",root,point+Vector3.up*.46f,new Vector3(.15f,.92f,.15f),iron);
                B("Pedestrian lantern lens",root,point+Vector3.up*.92f,new Vector3(.22f,.06f,.22f),BahnhofDetailKit.Load("StreetlightFrostedGlass"),false);
                Lamp(point+Vector3.up*.95f,24,7,"Garden path");
            }
            foreach(float z in new[]{253f,277})
            {
                B("Station wall light bracket",root,new Vector3(148.5f,4.35f,z),new Vector3(.55f,.12f,.18f),iron,false);
                Lamp(new Vector3(148.8f,4.2f,z),160,9,"Station facade",new Vector3(-.65f,-2,0));
            }
            // Actual waiting space and luggage stand beside the northern garden;
            // taxi access and the straight hall entrance remain free.
            var shelter=Group("BV4_North waiting pergola",root,new Vector3(180,.14f,290)).transform;
            foreach(float x in new[]{-3.2f,3.2f})foreach(float z in new[]{-1.4f,1.4f})B("Pergola column",shelter,new Vector3(x,1.3f,z),new Vector3(.12f,2.6f,.12f),iron);
            B("Waiting pergola roof",shelter,new Vector3(0,2.65f,0),new Vector3(7,.12f,3.3f),iron);
            foreach(float x in new[]{-2.6f,0,2.6f})B("Pergola roof beam",shelter,new Vector3(x,2.52f,0),new Vector3(.10f,.15f,3.1f),wood,false);
            WestbruchProps.Bench(shelter,new Vector3(0,0,.65f),0);
            BahnhofDetailKit.Plate(shelter,"WARTEBEREICH · TAXI",new Vector3(0,2.20f,-1.46f),0,3.8f,.28f,iron,new Color(.88f,.86f,.73f));
            Lamp(new Vector3(180,2.6f,290),65,9,"Waiting shelter");
            foreach(float x in new[]{162f,166})CafeTable(new Vector3(x,.14f,292));
            var sign=Group("BV4_Bus stop display",root,new Vector3(197.5f,.14f,247)).transform;
            Cylinder("BV4_Bus stop support",sign,new Vector3(0,1.7f,0),.035f,3.4f,iron,true);
            BahnhofDetailKit.Plate(sign,"H · BUS 12",new Vector3(0,2.95f,-.055f),0,1.0f,.38f,iron,new Color(.86f,.84f,.65f));
            foreach(float x in new[]{185.2f,188.9f,192.6f})B("Taxi bay inlaid divider",root,new Vector3(x,.047f,280),new Vector3(.065f,.008f,5),paint,false);
            for(int i=0;i<6;i++)B("Bus bay inlaid dash",root,new Vector3(203.3f,.047f,245+i*2.0f),new Vector3(.10f,.008f,.9f),paint,false);
        }
        static void CafeTable(Vector3 at)
        {
            var p=Group("BV4_Cafe terrace set",root,at).transform;
            Cylinder("BV4_Table pedestal",p,new Vector3(0,.36f,0),.045f,.72f,iron,true);
            Cylinder("BV4_Table top",p,new Vector3(0,.74f,0),.58f,.07f,wood,true);
            foreach(float side in new[]{-1f,1})
            {
                var chair=Group("BV4_Terrace chair",p,new Vector3(side*.98f,0,0),side<0?-90:90).transform;
                B("Chair seat",chair,new Vector3(0,.43f,0),new Vector3(.44f,.065f,.44f),wood);
                B("Chair back",chair,new Vector3(0,.67f,.21f),new Vector3(.44f,.42f,.055f),wood);
                foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.17f,.17f})B("Chair leg",chair,new Vector3(x,.20f,z),new Vector3(.035f,.40f,.035f),iron);
            }
        }
        static void RepairCourt()
        {
            PlantedIsland(new Vector3(162,.14f,205),new Vector2(5,3));
            PlantedIsland(new Vector3(168,.14f,193),new Vector2(11,2.5f));
            var p=Group("BV4_Cycle repair courtyard",root,new Vector3(175,.14f,204)).transform;
            foreach(float x in new[]{-4.6f,4.6f})foreach(float z in new[]{-1.4f,1.4f})B("Repair canopy column",p,new Vector3(x,1.5f,z),new Vector3(.14f,3,.14f),iron);
            B("Repair courtyard canopy",p,new Vector3(0,3.08f,0),new Vector3(10,.16f,4),iron);
            B("Repair canopy fascia",p,new Vector3(0,2.80f,-2),new Vector3(10,.4f,.13f),iron,false);
            BahnhofDetailKit.Plate(p,"FAHRRADHOF · REPARATUR",new Vector3(0,2.80f,-2.08f),0,6.2f,.30f,iron,new Color(.88f,.86f,.73f));
            foreach(float x in new[]{-3f,2.5f})
            {
                B("Repair workbench",p,new Vector3(x,.90f,.6f),new Vector3(2.2f,.10f,.8f),wood);
                foreach(float side in new[]{-.85f,.85f})B("Workbench leg",p,new Vector3(x+side,.43f,.6f),new Vector3(.10f,.86f,.65f),iron);
                for(int i=0;i<3;i++)B("Parts storage drawer",p,new Vector3(x-.6f+i*.6f,1.05f,.7f),new Vector3(.45f,.2f,.5f),iron,false);
            }
            var cycle=district.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Station commuter bicycle");
            if(cycle)foreach(float x in new[]{-2f,0,2}){var c=UnityEngine.Object.Instantiate(cycle.gameObject,p);c.name="BV4_Cycle awaiting service";c.transform.localPosition=new Vector3(x,0,-.55f);c.transform.localRotation=Quaternion.Euler(0,70,0);foreach(var r in c.GetComponentsInChildren<MeshRenderer>())r.enabled=true;}
            Lamp(new Vector3(175,3,204),95,11,"Cycle courtyard");
        }
        static void Lamp(Vector3 at,float intensity,float range,string label,Vector3? direction=null)
        {
            var go=Group("BV4 Street light "+label,root,at);go.transform.rotation=Quaternion.LookRotation(direction??Vector3.down,Vector3.forward);
            var l=go.AddComponent<Light>();l.type=LightType.Spot;l.range=range;l.spotAngle=112;l.innerSpotAngle=75;l.shadows=LightShadows.None;l.lightUnit=LightUnit.Candela;l.intensity=intensity;l.useColorTemperature=true;l.colorTemperature=3000;
            var hd=go.AddComponent<HDAdditionalLightData>();hd.fadeDistance=250;hd.volumetricDimmer=0;hd.affectSpecular=false;
        }
        static void IndividualAddresses()
        {
            var original=district.root;var bikes=original.GetComponentsInChildren<Transform>().Where(t=>!t.IsChildOf(district)&&t.name=="Resident bicycle").OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
            for(int i=0;i<bikes.Length;i++)
            {
                removedSources.AddRange(bikes[i].GetComponentsInChildren<MeshFilter>());
                if(i%3==0)continue;
                var copy=UnityEngine.Object.Instantiate(bikes[i].gameObject,root);copy.name="BV4_Individual resident bicycle "+i;
                copy.transform.position=bikes[i].position+bikes[i].right*(i%2==0?.48f:-.40f);copy.transform.rotation=bikes[i].rotation*Quaternion.Euler(0,i%2==0?12:-16,0);
                foreach(var r in copy.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
            }
            var houses=JsonUtility.FromJson<WestbruchLayout>(System.IO.File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json")).buildings;
            int seed=0;
            foreach(var plot in houses)
            {
                var house=GameObject.Find(plot.id).transform;
                var door=house.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.name=="Entrance");
                if(door&&seed%3!=0)
                {
                    removedSources.Add(door);var go=UnityEngine.Object.Instantiate(door.gameObject,root);go.name="BV4_Distinct entrance "+plot.id;go.transform.SetPositionAndRotation(door.transform.position,door.transform.rotation);
                    var color=seed%3==1?new Color(.29f,.25f,.21f):new Color(.24f,.30f,.32f);
                    go.GetComponent<MeshRenderer>().sharedMaterial=Material("BV4_EntrancePaint_"+seed%3,null,color,.21f);go.GetComponent<MeshRenderer>().enabled=true;
                }
                var shutters=house.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("Half lowered roller shutter")).ToArray();
                for(int j=0;j<shutters.Length;j++)if((j+seed)%4==0)
                {
                    var source=shutters[j];removedSources.Add(source);var copy=UnityEngine.Object.Instantiate(source.gameObject,root);copy.name="BV4_Address shutter variation";copy.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                    copy.GetComponent<MeshRenderer>().sharedMaterial=Material("BV4_ShutterPaint_"+seed%3,null,new[]{new Color(.34f,.37f,.30f),new Color(.38f,.32f,.25f),new Color(.30f,.35f,.37f)}[seed%3],.12f);copy.GetComponent<MeshRenderer>().enabled=true;
                }
                var balconies=house.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Balcony slab").ToArray();
                for(int j=0;j<balconies.Length;j++)if((j+seed)%3==0)
                {
                    var floor=balconies[j];var planter=Group("BV4_Resident balcony garden",root,floor.transform.TransformPoint(new Vector3(.55f,.12f,-.44f)),floor.transform.eulerAngles.y).transform;
                    var pot=Material("BV4_ClayPot_"+seed%3,null,new[]{new Color(.37f,.26f,.18f),new Color(.32f,.37f,.30f),new Color(.43f,.36f,.26f)}[seed%3],.10f);
                    B("Balcony plant trough",planter,new Vector3(0,.13f,0),new Vector3(.73f,.26f,.26f),pot,false);
                    Shrub(planter,new Vector3(0,.23f,0),.20f,seed+j);
                }
                seed++;
            }
        }
        static void CorrectSigns()
        {
            // Hofstrasse is x20 in the authored plan; the underpass extension is Nordring.
            foreach(var sign in district.root.GetComponentsInChildren<MountedSign>().Where(s=>s.readableText!=null&&(s.readableText.Contains("Hof")||s.readableText=="Nordring")&&s.transform.position.x>100&&s.transform.position.z>58&&s.transform.position.z<105))
            {
                removedSources.AddRange(sign.GetComponentsInChildren<MeshFilter>());
                var parent=Group("BV4_Corrected Nordring sign",root,sign.transform.position,sign.transform.eulerAngles.y).transform;
                BahnhofDetailKit.Plate(parent,"Nordring",Vector3.zero,0,sign.size.x,sign.size.y,iron,new Color(.9f,.9f,.81f));
                sign.readableText="Nordring";EditorUtility.SetDirty(sign);
            }
        }
        static void Validate()
        {
            var context=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BV4_Context residence ")).ToArray();
            if(context.Length!=8||context.Any(t=>!t.GetComponent<BoxCollider>()))throw new Exception("Context frontage needs eight collidable finished buildings");
            foreach(var p in new[]{new Vector3(161,2,265),new Vector3(175,2,198),new Vector3(84,2,140),new Vector3(0,2,142)})
                if(!Physics.Raycast(p,Vector3.down,out var hit,3,1)||hit.point.y<-.15f)throw new Exception("Audit completion has unsupported ground at "+p);
        }
    }
}
