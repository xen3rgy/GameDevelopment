using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Authored neighbourhood use and wind-blown waste, baked once into spatial
    // meshes. Small litter has no physics objects or per-frame behaviours.
    internal static class WestbruchEverydayDetails
    {
        const string Manifest=Generated+"/StreetLifePlacement.json";
        const int Families=8;
        [Serializable] sealed class Piece { public Vector3 p;public float radius;public int family;public string support; }
        [Serializable] sealed class Record
        {
            public int sites,triangles,chunks;public List<Piece> pieces=new List<Piece>();
        }
        sealed class Shape
        {
            public readonly List<Vector3> v=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> t=new List<int>();
            public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector2? ua=null,Vector2? ub=null,Vector2? uc=null)
            {
                int i=v.Count;v.Add(a);v.Add(b);v.Add(c);
                uv.Add(ua??Vector2.zero);uv.Add(ub??Vector2.up);uv.Add(uc??Vector2.one);t.Add(i);t.Add(i+1);t.Add(i+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {Tri(a,b,c,Vector2.zero,Vector2.up,Vector2.one);Tri(a,c,d,Vector2.zero,Vector2.one,Vector2.right);}
        }
        static readonly Dictionary<int,Mesh> variants=new Dictionary<int,Mesh>();
        static readonly Vector2[] preferred={new Vector2(-80,49),new Vector2(-48,49),new Vector2(-16,49),new Vector2(7,45),
            new Vector2(-82,-59),new Vector2(-52,-58),new Vector2(-19,-67),new Vector2(7,-63),
            new Vector2(36,33),new Vector2(76,30),new Vector2(77,-29),new Vector2(36,-71),
            new Vector2(-77,89),new Vector2(-9,89),new Vector2(76,89),new Vector2(-79,-90),new Vector2(-7,-90),new Vector2(76,-90),
            new Vector2(113,28),new Vector2(114,-28)};
        static readonly List<Vector2> placed=new List<Vector2>();
        static Material[] Palette()
        {
            var paper=Material("StreetLife_Newsprint",null,new Color(.68f,.65f,.56f),.06f);
            paper.SetTexture("_BaseColorMap",PrintTexture());EditorUtility.SetDirty(paper);
            var cardboard=Material("StreetLife_Cardboard",null,new Color(.76f,.65f,.49f),.04f);
            var fabric=Material("StreetLife_FadedFabric",null,new Color(.61f,.59f,.48f),.035f);
            foreach(var mat in new[]{cardboard,fabric})
            {
                mat.SetTexture("_BaseColorMap",FibreTexture(mat==fabric));mat.SetTexture("_NormalMap",null);mat.SetTexture("_MaskMap",null);
                UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);
            }
            return new[]{paper,cardboard,
                Material("StreetLife_OxidisedTin","steel",new Color(.54f,.53f,.46f),.32f,.65f),
                Material("StreetLife_GreenBottle",null,new Color(.095f,.19f,.10f),.62f,.18f),
                Material("StreetLife_FadedPackaging",null,new Color(.39f,.26f,.16f),.12f),
                Material("StreetLife_BlackPlastic",null,new Color(.04f,.047f,.044f),.31f),
                Material("StreetLife_Rubber",null,new Color(.055f,.057f,.051f),.05f),
                fabric};
        }
        static Texture2D FibreTexture(bool cloth)
        {
            string path=Generated+"/Materials/StreetLife_"+(cloth?"WovenFabric":"CardboardFibre")+".asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture)return texture;
            const int size=512;var pixels=new Color[size*size];var random=new System.Random(cloth?912:183);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float noise=(float)random.NextDouble(),broad=Mathf.PerlinNoise(x*.017f+31,y*.017f+79);
                float fibre=cloth?((x%4==0||y%4==0)?-.045f:.018f):Mathf.Sin(x*.21f+y*.006f)*.013f;
                float tone=.73f+noise*.085f+fibre+(broad-.5f)*.07f;
                // Faint water/touch staining, with no masonry or wood grain.
                float stain=Mathf.SmoothStep(.57f,.77f,Mathf.PerlinNoise(x*.009f,y*.009f));tone-=stain*(cloth?.14f:.055f);
                pixels[y*size+x]=new Color(tone,tone*(cloth?.99f:.95f),tone*(cloth?.93f:.85f));
            }
            texture=new Texture2D(size,size,TextureFormat.RGB24,true){name=cloth?"Worn woven upholstery":"Uncoated packing cardboard",wrapMode=TextureWrapMode.Repeat};
            texture.SetPixels(pixels);texture.Apply(true);AssetDatabase.CreateAsset(texture,path);return texture;
        }
        static Texture2D PrintTexture()
        {
            const string path=Generated+"/Materials/StreetLife_Newsprint.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture)return texture;
            const int size=512;texture=new Texture2D(size,size,TextureFormat.RGB24,true){name="Unbranded weathered print",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];var rng=new System.Random(76103);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float n=(float)rng.NextDouble()*.08f;
                bool margin=x<24||x>488||y<24||y>488;
                bool text=!margin&&((y>430&&y<455)||(y<408&&y>50&&y%17<3&&x%151<135));
                bool photo=x>30&&x<225&&y>235&&y<387;
                float value=text?.22f+n:photo?.38f+n+Mathf.PerlinNoise(x*.04f,y*.04f)*.23f:.85f+n;
                pixels[y*size+x]=new Color(value,value*.97f,value*.88f);
            }
            texture.SetPixels(pixels);texture.Apply(true);AssetDatabase.CreateAsset(texture,path);return texture;
        }
        public static void BuildYards(Transform parent,WestbruchLayout plan)
        {
            variants.Clear();placed.Clear();Physics.SyncTransforms();
            var root=Group("Everyday yard pockets – refuse, storage and repairs",parent,Vector3.zero).transform;
            var palette=Palette();var timber=Material("StreetLife_Offcuts","wood",new Color(.58f,.49f,.35f),.07f);
            var steel=Material("StreetLife_WornBin","steel",new Color(.22f,.28f,.23f),.16f,.35f);
            for(int site=0;site<preferred.Length;site++)
            {
                bool found=false;Vector2 at=preferred[site];float y=0;
                for(int attempt=0;attempt<81&&!found;attempt++)
                {
                    float a=attempt*2.39996f,r=attempt==0?0:Mathf.Sqrt(attempt)*.85f;
                    at=preferred[site]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                    // Whole pocket stays off circulation. Test low objects as well
                    // as buildings, and reserve a capsule-wide approach in front.
                    bool clear=true;float low=100,high=-100;
                    for(int ix=-2;ix<=2&&clear;ix++)for(int iz=-1;iz<=1;iz++)
                    {
                        float x=at.x+ix*.85f,z=at.y+iz*1.2f;
                        if(!WestbruchLandscape.OnEarth(x,z,.14f)){clear=false;break;}
                        float floor=Terrain.activeTerrain.SampleHeight(new Vector3(x,0,z))+Terrain.activeTerrain.transform.position.y;
                        low=Mathf.Min(low,floor);high=Mathf.Max(high,floor);
                    }
                    if(!clear||high-low>.095f)continue;
                    if(!Support(at.x,at.y,.1f,out var hit,out _)||!(hit.collider is TerrainCollider))continue;
                    y=hit.point.y;
                    if(Physics.CheckBox(new Vector3(at.x,y+.83f,at.y),new Vector3(1.85f,.82f,1.35f),Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                    // Existing soil traversal and seating routes remain open.
                    if(at.x> -65&&at.x< -47&&at.y>28&&at.y<37)continue;
                    if(at.x> -38&&at.x< -7&&at.y> -64&&at.y< -50)continue;
                    found=true;
                }
                if(!found)continue;
                placed.Add(at);
                var pocket=Group("Yard pocket "+site,root,new Vector3(at.x,y-.018f,at.y),0).transform;
                // Four visibly different uses, with circulation-facing open space.
                if(site%4==0)
                {
                    Dumpster(pocket,steel,palette[5]);
                    for(int i=0;i<3;i++)Item("Wrinkled tied refuse sack",pocket,new Vector3(-1.12f+i*.52f,0,-.75f),Source(5,i),palette[5],new Vector3(.85f,1,.85f),i*37,true);
                    Item("Open discarded carton",pocket,new Vector3(1.02f,0,.15f),Source(8,0),palette[1],Vector3.one,17,true);
                }
                else if(site%4==1)
                {
                    Mattress(pocket,palette[7]);Chair(pocket,new Vector3(-1.2f,0,.35f),timber,steel);
                    Item("Open moving box",pocket,new Vector3(.95f,0,.2f),Source(8,1),palette[1],new Vector3(1.15f,1.3f,1.15f),-16,true);
                    WestbruchProps.Pallet(pocket,new Vector3(.9f,0,-.8f));
                }
                else if(site%4==2)
                {
                    WestbruchProps.Pallet(pocket,new Vector3(-.55f,0,0));
                    WestbruchProps.Pallet(pocket,new Vector3(-.5f,.17f,.06f));
                    for(int i=0;i<3;i++)Item("Discarded tyre stack",pocket,new Vector3(.8f,i*.18f,.2f),Source(6,0),palette[6],new Vector3(1.35f,1,1.35f),i*29,i==0);
                    for(int i=0;i<4;i++)Box("Salvaged timber offcut",pocket,new Vector3(-.55f,.37f+i*.055f,0),new Vector3(1.35f,.05f,.15f),timber,false,.6f).transform.localRotation=Quaternion.Euler(0,i*8,0);
                    Item("Empty parts carton",pocket,new Vector3(-1.15f,0,-.8f),Source(8,2),palette[1],new Vector3(.8f,.6f,.9f),12,true);
                }
                else
                {
                    WestbruchProps.Bin(pocket,new Vector3(-.75f,0,.3f),7);
                    WestbruchProps.Bin(pocket,new Vector3(0,0,.4f),-9);
                    for(int i=0;i<2;i++)Item("Tied bin-side sack",pocket,new Vector3(.7f+i*.5f,0,.1f-i*.25f),Source(5,i+1),palette[5],Vector3.one,site*17+i*29,true);
                    Crate(pocket,new Vector3(-.8f,0,-.65f),steel);
                }
                Physics.SyncTransforms();
            }
            Debug.Log("Everyday details: "+placed.Count+" supported yard pockets; no additions to paths or roads.");
        }
        static void Dumpster(Transform p,Material steel,Material rubber)
        {
            Box("Wheeled communal dumpster body",p,new Vector3(0,.63f,.35f),new Vector3(1.65f,.94f,.86f),steel);
            foreach(int side in new[]{-1,1})
            {
                Box("Moulded dumpster lid",p,new Vector3(side*.42f,1.13f,.35f),new Vector3(.83f,.09f,.97f),rubber,false);
                Beam("Bin side lift handle",p,new Vector3(side*.89f,.78f,.13f),new Vector3(side*.89f,.78f,.58f),.045f,steel);
                for(int i=0;i<4;i++)Box("Pressed bin reinforcing rib",p,new Vector3(-.59f+i*.39f,.65f,-.1f),new Vector3(.025f,.68f,.025f),steel,false);
                foreach(float z in new[]{.04f,.67f})
                {
                    var wheel=Cylinder("Dumpster caster tyre",p,new Vector3(side*.65f,.095f,z),.095f,.065f,rubber);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    Box("Caster fork",p,new Vector3(side*.65f,.19f,z),new Vector3(.08f,.13f,.08f),steel,false);
                }
            }
        }
        static void Mattress(Transform p,Material fabric)
        {
            var body=Box("Dumped upholstered mattress",p,new Vector3(0,.11f,0),new Vector3(.86f,.22f,1.65f),fabric);
            foreach(float z in new[]{-.67f,0,.67f})foreach(float x in new[]{-.28f,.28f})
                Box("Depressed mattress tuft",p,new Vector3(x,.221f,z),new Vector3(.035f,.006f,.035f),Material("StreetLife_Seam",null,new Color(.16f,.17f,.14f)),false);
            foreach(int side in new[]{-1,1})Box("Mattress sewn edge",p,new Vector3(side*.427f,.18f,0),new Vector3(.014f,.017f,1.6f),fabric,false);
        }
        static void Chair(Transform p,Vector3 at,Material timber,Material steel)
        {
            var chair=Group("Discarded kitchen chair",p,at,-14).transform;
            Box("Chair seat",chair,new Vector3(0,.43f,0),new Vector3(.44f,.055f,.43f),timber);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.16f,.16f})Box("Chair leg",chair,new Vector3(x,.21f,z),new Vector3(.035f,.42f,.035f),steel);
            foreach(float x in new[]{-.17f,.17f})Box("Chair back upright",chair,new Vector3(x,.68f,.16f),new Vector3(.035f,.53f,.035f),steel);
            Box("Chair back panel",chair,new Vector3(0,.79f,.17f),new Vector3(.43f,.2f,.045f),timber);
        }
        static void Crate(Transform p,Vector3 at,Material steel)
        {
            var root=Group("Empty slotted delivery crate",p,at,12).transform;
            Box("Crate base",root,new Vector3(0,.025f,0),new Vector3(.62f,.05f,.42f),steel);
            foreach(int s in new[]{-1,1})
            {
                Box("Crate top rail",root,new Vector3(0,.32f,s*.205f),new Vector3(.64f,.045f,.025f),steel,false);
                for(int i=0;i<7;i++)Box("Crate side slat",root,new Vector3(-.28f+i*.093f,.17f,s*.205f),new Vector3(.027f,.27f,.025f),steel,false);
                Box("Crate end panel",root,new Vector3(s*.3f,.17f,0),new Vector3(.025f,.29f,.42f),steel);
            }
        }
        static void Item(string name,Transform parent,Vector3 p,Mesh mesh,Material material,Vector3 scale,float yaw,bool solid=false)
        {
            var obj=Group(name,parent,p,yaw);obj.transform.localScale=scale;obj.isStatic=true;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        static bool Floor(RaycastHit hit)=>hit.collider&&(hit.collider is TerrainCollider||hit.collider.name=="Road cell"||hit.collider.name=="Connected pavement");
        static bool Support(float x,float z,float radius,out RaycastHit center,out float range)
        {
            range=0;center=default;
            if(!Physics.Raycast(new Vector3(x,8,z),Vector3.down,out center,10,1,QueryTriggerInteraction.Ignore)||!Floor(center)||center.normal.y<.94f)return false;
            float low=center.point.y,high=low;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f;
                if(!Physics.Raycast(new Vector3(x+Mathf.Cos(a)*radius,8,z+Mathf.Sin(a)*radius),Vector3.down,out var hit,10,1,QueryTriggerInteraction.Ignore)||!Floor(hit)||hit.normal.y<.94f)return false;
                low=Mathf.Min(low,hit.point.y);high=Mathf.Max(high,hit.point.y);
            }
            range=high-low;return true;
        }
        public static void BuildLitter(Transform parent,WestbruchLayout plan)
        {
            Physics.SyncTransforms();var root=Group("Street life litter – baked windrows and bin spill",parent,Vector3.zero).transform;
            var palette=Palette();var record=new Record{sites=placed.Count};var rng=new System.Random(2026101701);
            float F(float a,float b)=>a+(b-a)*(float)rng.NextDouble();
            var groups=new Dictionary<int,List<CombineInstance>>();
            void Add(float x,float z,int family)
            {
                if(x< -109||x>124||Mathf.Abs(z)>109)return;
                foreach(var car in plan.cars)
                {
                    bool sideways=Mathf.Abs(Mathf.Sin(car.yaw*Mathf.Deg2Rad))>.5f;
                    if(Mathf.Abs(x-car.x)<(sideways?2.9f:1.8f)&&Mathf.Abs(z-car.z)<(sideways?1.8f:2.9f))return;
                }
                float scale=F(.8f,1.55f);int variant=rng.Next(3);var mesh=Source(family,variant);
                float radius=new Vector2(mesh.bounds.extents.x,mesh.bounds.extents.z).magnitude*scale+.014f;
                if(!Support(x,z,radius,out var support,out float range)||range>.045f)return;
                var p=new Vector3(x,support.point.y+.006f,z);
                var rotation=Quaternion.FromToRotation(Vector3.up,support.normal)*Quaternion.Euler(0,F(0,360),0);
                int cell=Mathf.Clamp(Mathf.FloorToInt((x+132)/44),0,5)+Mathf.Clamp(Mathf.FloorToInt((z+114)/38),0,5)*6;
                int key=cell*Families+family;if(!groups.TryGetValue(key,out var list)){list=new List<CombineInstance>();groups[key]=list;}
                var origin=new Vector3((cell%6+.5f)*44-132,0,(cell/6+.5f)*38-114);
                list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p-origin,rotation,Vector3.one*scale)});
                record.pieces.Add(new Piece{p=p,radius=radius,family=family,support=support.collider is TerrainCollider?"earth":support.collider.name});
            }
            // Broken windrows accumulate on both sides of each kerb, with gaps
            // and pockets. Traffic lanes receive only occasional lightweight waste.
            foreach(var road in plan.roads)
            {
                bool horizontal=road.w>road.d;float length=horizontal?road.w:road.d,half=(horizontal?road.d:road.w)*.5f;
                foreach(int side in new[]{-1,1})
                for(float along=-length*.5f+3;along<length*.5f-3;along+=F(2.6f,5.7f))
                {
                    if(F(0,1)<.26f)continue;
                    float across=side*(half+F(-.65f,1.65f));int count=rng.Next(9,24);
                    for(int i=0;i<count;i++)
                    {
                        float a=along+F(-1.65f,1.65f),b=across+F(-.29f,.29f);
                        int roll=rng.Next(100),family=roll<39?0:roll<55?1:roll<65?2:roll<72?3:roll<83?4:7;
                        Add(road.x+(horizontal?a:b),road.z+(horizontal?b:a),family);
                    }
                }
                for(int i=0;i<25;i++)
                {
                    float a=F(-length*.45f,length*.45f),b=F(-half*.75f,half*.75f);
                    Add(road.x+(horizontal?a:b),road.z+(horizontal?b:a),rng.NextDouble()<.7?0:1);
                }
            }
            var pockets=new List<Vector2>(placed);
            foreach(var item in parent.GetComponentsInChildren<Transform>())
                if(item.name=="Wheelie bin"||item.name=="Refuse and salvage alcove"||item.name=="Old Westbruch bus shelter")pockets.Add(new Vector2(item.position.x,item.position.z));
            // Visible larger scraps around actual uses, rather than a random
            // district-wide scatter. Fences and building rears trap lighter waste.
            foreach(var at in pockets)
                for(int i=0;i<46;i++)
                {
                    float angle=F(0,Mathf.PI*2),r=F(.55f,3.2f);
                    Add(at.x+Mathf.Cos(angle)*r,at.y+Mathf.Sin(angle)*r,rng.Next(5));
                }
            foreach(var b in plan.buildings)
                for(int i=0;i<36;i++)
                {
                    float side=b.yaw==180?-1:1;
                    Add(b.x+F(-b.w*.46f,b.w*.46f),b.z+side*(b.d*.5f+F(.4f,2.7f)),rng.NextDouble()<.6?0:1);
                }
            foreach(var entry in groups)
            {
                string name="StreetLifeChunk_"+entry.Key,path=Generated+"/Meshes/"+name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                int cell=entry.Key/Families;var origin=new Vector3((cell%6+.5f)*44-132,0,(cell/6+.5f)*38-114);
                Item(name,root,origin,mesh,palette[entry.Key%Families],Vector3.one,0);
                record.triangles+=mesh.triangles.Length/3;record.chunks++;
            }
            File.WriteAllText(Manifest,JsonUtility.ToJson(record,true));AssetDatabase.ImportAsset(Manifest);
            Debug.Log("Street litter baked: "+record.pieces.Count+" varied scraps, "+record.triangles+" triangles, "+record.chunks+" source chunks; zero small-litter colliders.");
        }
        static Mesh Source(int family,int variant)
        {
            int key=family*3+variant;if(variants.TryGetValue(key,out var cached))return cached;
            string path=Generated+"/Meshes/StreetLifeSourceV2_"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh){variants[key]=mesh;return mesh;}
            var s=new Shape();
            if(family==0||family==1)
            {
                float w=family==0?.14f:.22f,l=family==0?.21f:.29f,bend=.014f+variant*.018f;
                var a=new Vector3(-w,0,-l);var b=new Vector3(-w*.85f,bend,l);var c=new Vector3(w*.18f,.006f,l);var d=new Vector3(w*.05f,.004f,-l);
                s.Quad(a,b,c,d);s.Quad(d,c,new Vector3(w,bend*.4f,l*.86f),new Vector3(w*.87f,bend,-l*.9f));
                if(family==1)s.Quad(new Vector3(-w*.7f,.01f,0),new Vector3(-w*.65f,.035f,l*.6f),new Vector3(w*.7f,.05f,l*.6f),new Vector3(w*.65f,.025f,0));
            }
            else if(family==5)
            {
                // Uneven bulging bag body, gathered shoulder and twisted neck.
                const int sides=24;float[] heights={0,.06f,.23f,.43f,.56f,.61f,.66f};float[] radii={.17f,.27f,.31f,.25f,.08f,.035f,.055f};
                for(int ring=0;ring<heights.Length-1;ring++)for(int j=0;j<sides;j++)
                {
                    Vector3 P(int k,int r){k%=sides;float a=k*Mathf.PI*2/sides;float crinkle=1+Mathf.Sin(k*2.3f+r*1.9f+variant)*.065f;return new Vector3(Mathf.Cos(a)*radii[r]*crinkle+.025f*r,heights[r],Mathf.Sin(a)*radii[r]*crinkle);}
                    s.Quad(P(j,ring),P(j,ring+1),P(j+1,ring+1),P(j+1,ring));
                }
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;s.Tri(Vector3.zero,new Vector3(Mathf.Cos(b)*.17f,0,Mathf.Sin(b)*.17f),new Vector3(Mathf.Cos(a)*.17f,0,Mathf.Sin(a)*.17f));}
            }
            else if(family==6)
            {
                const int sides=20,tube=8;
                Vector3 P(int i,int j){float a=i*Mathf.PI*2/sides,b=j*Mathf.PI*2/tube;return new Vector3(Mathf.Cos(a)*(.22f+Mathf.Cos(b)*.069f),.088f+Mathf.Sin(b)*.088f,Mathf.Sin(a)*(.22f+Mathf.Cos(b)*.069f));}
                for(int i=0;i<sides;i++)for(int j=0;j<tube;j++)s.Quad(P(i,j),P(i,j+1),P(i+1,j+1),P(i+1,j));
            }
            else if(family==8)
            {
                float w=.30f,d=.24f,h=.39f;
                s.Quad(new Vector3(-w,0,-d),new Vector3(-w,0,d),new Vector3(w,0,d),new Vector3(w,0,-d));
                for(int side=0;side<4;side++)
                {
                    var q=Quaternion.Euler(0,side*90,0);float span=side%2==0?w:d,depth=side%2==0?d:w;
                    Vector3 P(float x,float y,float z)=>q*new Vector3(x,y,z);
                    s.Quad(P(-span,0,-depth),P(span,0,-depth),P(span,h,-depth),P(-span,h,-depth));
                    s.Quad(P(-span,h,-depth),P(span,h,-depth),P(span,h+.07f,-depth-.12f-variant*.035f),P(-span,h+.03f,-depth-.14f));
                }
                // Open boxes need both the inner cardboard and outer walls.
                int count=s.t.Count;for(int i=0;i<count;i+=3){s.t.Add(s.t[i+2]);s.t.Add(s.t[i+1]);s.t.Add(s.t[i]);}
            }
            else if(family==7)
            {
                // Curled brown leaves on paving and gutter gravel, not flowers.
                s.Tri(new Vector3(-.07f,.006f,0),new Vector3(0,.012f,.15f),new Vector3(.065f,.025f,.02f));
                s.Tri(new Vector3(-.07f,.006f,0),new Vector3(.065f,.025f,.02f),new Vector3(.015f,0,-.11f));
            }
            else
            {
                // Closed, rotated profiles distinguish crushed cans, bottles and
                // tapered takeaway cups. Each variant has a different dented rim.
                const int sides=12;int rings=family==3?7:4;
                var rotation=Quaternion.Euler(family==4?78:90,0,variant*23);
                var points=new Vector3[rings,sides];float minimum=100;
                for(int r=0;r<rings;r++)for(int j=0;j<sides;j++)
                {
                    float t=r/(float)(rings-1),a=j*Mathf.PI*2/sides;
                    float radius=family==3?(r<4?.038f:r==4?.024f:.014f):family==4?Mathf.Lerp(.03f,.052f,t):.032f*(r==0||r==rings-1?1:.8f);
                    radius*=1+Mathf.Sin(j*2.1f+r+variant)*.12f;
                    var p=rotation*new Vector3(Mathf.Cos(a)*radius,t*(family==3?.23f:.125f),Mathf.Sin(a)*radius);
                    points[r,j]=p;minimum=Mathf.Min(minimum,p.y);
                }
                Vector3 P(int r,int j)=>points[r,j%sides]-Vector3.up*minimum;
                for(int r=0;r<rings-1;r++)for(int j=0;j<sides;j++)s.Quad(P(r,j),P(r+1,j),P(r+1,j+1),P(r,j+1));
                for(int j=1;j<sides-1;j++){s.Tri(P(0,0),P(0,j+1),P(0,j));s.Tri(P(rings-1,0),P(rings-1,j),P(rings-1,j+1));}
            }
            mesh=new Mesh{name="Cached street-life variant "+key};mesh.SetVertices(s.v);mesh.SetUVs(0,s.uv);mesh.SetTriangles(s.t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(family==2||family==3||family==4||family==5)
            {
                // Weld only shading across shared corners. Geometric creases and
                // the gathered sack profile remain, without diamond-like faces.
                var normals=mesh.normals;var sums=new Dictionary<Vector3Int,Vector3>();
                for(int i=0;i<s.v.Count;i++){var corner=Vector3Int.RoundToInt(s.v[i]*100000);sums.TryGetValue(corner,out var n);sums[corner]=n+normals[i];}
                for(int i=0;i<s.v.Count;i++)normals[i]=sums[Vector3Int.RoundToInt(s.v[i]*100000)].normalized;
                mesh.normals=normals;mesh.RecalculateTangents();
            }
            AssetDatabase.CreateAsset(mesh,path);variants[key]=mesh;return mesh;
        }
        public static void Validate()
        {
            var report=JsonUtility.FromJson<Record>(File.ReadAllText(Manifest));var kinds=new int[Families];var quadrants=new int[4];int roads=0,paving=0,earth=0;
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            var walks=WestbruchBuilder.WalkPlan(plan);
            var failures=new List<string>();Physics.SyncTransforms();
            foreach(var piece in report.pieces)
            {
                kinds[piece.family]++;quadrants[(piece.p.x>=0?1:0)+(piece.p.z>=0?2:0)]++;
                if(piece.support=="earth")earth++;else if(piece.support=="Road cell")roads++;else paving++;
                if(!Support(piece.p.x,piece.p.z,piece.radius,out var floor,out float range)||Mathf.Abs(piece.p.y-floor.point.y-.006f)>.009f||range>.046f)
                    if(failures.Count<12)failures.Add("Unsupported/overlapping litter at "+piece.p);
            }
            int sites=0,chunks=0;
            foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(tr.name.StartsWith("Yard pocket "))
                {
                    sites++;
                    foreach(var collider in tr.GetComponentsInChildren<Collider>())
                    {
                        var bounds=collider.bounds;
                        foreach(var walk in walks)
                            if(Mathf.Abs(bounds.center.x-walk.x)<bounds.extents.x+walk.w*.5f+.12f&&Mathf.Abs(bounds.center.z-walk.z)<bounds.extents.z+walk.d*.5f+.12f)
                                failures.Add("New solid blocks connected walk: "+collider.name+" / "+walk.id);
                        foreach(var road in plan.roads)
                            if(Mathf.Abs(bounds.center.x-road.x)<bounds.extents.x+road.w*.5f+3&&Mathf.Abs(bounds.center.z-road.z)<bounds.extents.z+road.d*.5f+3)
                                failures.Add("New solid blocks roadside circulation: "+collider.name);
                    }
                }
                if(tr.name.StartsWith("StreetLifeChunk_")){chunks++;if(tr.GetComponent<Collider>())failures.Add("Small litter collision");}
            }
            if(sites<12||report.pieces.Count<2500||report.triangles>500000||chunks>288||Array.Exists(quadrants,n=>n<300)||roads<500||paving<500||earth<300)failures.Add("District detail coverage/budget invalid");
            foreach(int kind in new[]{0,1,2,3,4,7})if(kinds[kind]<40)failures.Add("Missing litter shape family "+kind);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/everyday-details-report.txt","Sites: "+sites+"\nPieces: "+report.pieces.Count+"\nRoad / paving / earth: "+roads+" / "+paving+" / "+earth+"\nQuadrants: "+string.Join(", ",quadrants)+"\nTriangles: "+report.triangles+"\nSource chunks: "+chunks+"\nFailures: "+string.Join("; ",failures));
            if(failures.Count>0)throw new Exception("Everyday detail QA: "+string.Join("; ",failures));
            Debug.Log("Everyday detail QA passed: "+sites+" yard pockets; "+report.pieces.Count+" supported scraps on roads, paving and earth; all four quadrants.");
        }
    }
}
