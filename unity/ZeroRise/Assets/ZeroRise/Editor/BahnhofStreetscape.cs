using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Authored, baked streetscape assets. All geometry uses real surface heights.
    internal static class BahnhofStreetscape
    {
        const string Art="Assets/ZeroRise/Art/Streetscape2K/";
        static Transform district,root;static Material asphalt,paving,soil,mulch,stone,iron,edge,repair;
        static readonly List<CombineInstance> plants=new List<CombineInstance>();
        static readonly List<Vector2> plantRoots=new List<Vector2>(),plantHeights=new List<Vector2>();
        static readonly Dictionary<string,Mesh> assets=new Dictionary<string,Mesh>();
        static System.Random rng;
        static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
        internal static void Apply(Transform neighbourhood)
        {
            district=neighbourhood;var old=district.Find("BV5_Streetscape");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            root=Group("BV5_Streetscape",district,Vector3.zero).transform;rng=new System.Random(11022026);plants.Clear();plantRoots.Clear();plantHeights.Clear();assets.Clear();
            foreach(var id in new[]{"asphalt_02","pavement_05","brown_mud_leaves_01","forest_ground_04","leafy_grass"})Import(id);
            asphalt=Surface("Road asphalt","asphalt_02",new Color(.74f,.77f,.79f),.32f,true);
            paving=Surface("Station stone setts","pavement_05",new Color(.70f,.76f,.80f),.45f);
            mulch=Surface("Planted soil","brown_mud_leaves_01",new Color(.66f,.63f,.58f),.25f);
            stone=Material("BV5_KerbGranite","concrete",new Color(.62f,.63f,.60f),.12f);
            iron=Material("BV5_OxidizedIron",null,new Color(.17f,.19f,.18f),.16f,.65f);
            edge=Material("BV5_DarkBasalt","concrete",new Color(.25f,.27f,.27f),.10f);
            repair=Surface("Road repairs","asphalt_02",new Color(.53f,.56f,.57f),.3f,true);
            soil=Ground();BuildUnderstory();ReplaceSurfaces();StreetEdges();RoadDetails();SquareBands();TreeGrilles();PlantBeds();GardenMargins();ReplaceTrees();BakePlants();
            Physics.SyncTransforms();
            Debug.Log("BV5 streetscape complete: PBR roads and setts, chamfered stone kerbs, drainage, tree grilles, layered soil, rooted planting and new tree meshes; plants "+plantRoots.Count);
        }
        static void Import(string id)
        {
            foreach(var kind in new[]{"diff","nor_gl","rough"})
            {
                string p=Art+id+"_"+kind+".jpg";AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
                var imp=AssetImporter.GetAtPath(p) as TextureImporter;if(!imp)throw new FileNotFoundException(p);
                imp.textureType=kind=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;imp.convertToNormalmap=false;
                imp.sRGBTexture=kind=="diff";imp.mipmapEnabled=true;imp.anisoLevel=8;imp.wrapMode=TextureWrapMode.Repeat;imp.maxTextureSize=2048;
                imp.isReadable=kind=="rough";imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
            }
            string mask=Art+id+"_mask.png";
            if(!File.Exists(mask))
            {
                var rough=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+id+"_rough.jpg");var pixels=rough.GetPixels32();
                for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,240,128,(byte)(255-pixels[i].r));
                var packed=new Texture2D(rough.width,rough.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(mask,packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);
            }
            AssetDatabase.ImportAsset(mask,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(mask);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        static Material Surface(string label,string id,Color color,float normal,bool road=false)
        {
            var mat=Material("BV5_"+label.Replace(' ','_'),null,color,.15f);
            mat.shader=Shader.Find("HDRP/Lit");mat.SetTexture("_BaseColorMap",Tex(id,"diff"));mat.SetTexture("_NormalMap",Tex(id,"nor_gl"));mat.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+id+"_mask.png"));
            mat.SetFloat("_NormalScale",normal);mat.SetFloat("_SmoothnessRemapMin",.02f);mat.SetFloat("_SmoothnessRemapMax",.32f);HDMaterial.ValidateMaterial(mat);
            if(road){mat.shader=Shader.Find("ZeroRise/UrbanLit");mat.EnableKeyword("_ZR_ROAD");}EditorUtility.SetDirty(mat);return mat;
        }
        static Texture2D Tex(string id,string kind)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Art+id+"_"+kind+".jpg");
        static Material Ground()
        {
            var mat=Material("BV5_LayeredGardenGround",null,Color.white,.05f);mat.shader=Shader.Find("HDRP/LayeredLit");mat.SetFloat("_LayerCount",3);mat.SetFloat("_VertexColorMode",1);mat.SetFloat("_UseHeightBasedBlend",0);mat.SetTexture("_LayerMaskMap",Texture2D.whiteTexture);
            mat.SetFloat("_UVBlendMask",0);mat.SetVector("_UVMappingMaskBlendMask",new Vector4(1,0,0,0));
            var ids=new[]{"forest_ground_04","leafy_grass","brown_mud_leaves_01"};var tiles=new[]{3.1f,2f,2.7f};
            for(int i=0;i<3;i++)
            {
                mat.SetColor("_BaseColor"+i,new[]{new Color(.72f,.70f,.64f),new Color(.57f,.73f,.54f),new Color(.62f,.61f,.56f)}[i]);
                mat.SetTexture("_BaseColorMap"+i,Tex(ids[i],"diff"));mat.SetTextureScale("_BaseColorMap"+i,Vector2.one/tiles[i]);mat.SetTextureOffset("_BaseColorMap"+i,new Vector2(i*.38f,i*.73f));
                mat.SetFloat("_UVBase"+i,1);mat.SetVector("_UVMappingMask"+i,new Vector4(0,1,0,0));mat.SetTexture("_NormalMap"+i,Tex(ids[i],"nor_gl"));mat.SetFloat("_NormalScale"+i,.25f);
                mat.SetTexture("_MaskMap"+i,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+ids[i]+"_mask.png"));mat.SetFloat("_SmoothnessRemapMin"+i,.015f);mat.SetFloat("_SmoothnessRemapMax"+i,.16f);mat.SetFloat("_Metallic"+i,0);
            }
            HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);return mat;
        }
        static void ReplaceSurfaces()
        {
            foreach(var r in district.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.transform.IsChildOf(root)||!r.sharedMaterial)continue;var name=r.name;string material=r.sharedMaterial.name;
                bool ground=name.StartsWith("BV2_Surface earth")||name.StartsWith("BV3_Ground ")||name=="BV4_Ground residential continuation"||name.StartsWith("BV4_Supported bank garden shoulder");
                if(ground)
                {
                    var f=r.GetComponent<MeshFilter>();var m=name=="BV4_Ground residential continuation"?GardenGrid():UnityEngine.Object.Instantiate(f.sharedMesh);var v=m.vertices;var uv=new Vector2[v.Length];var colors=new Color[v.Length];
                    for(int i=0;i<v.Length;i++)
                    {
                        var p=f.transform.TransformPoint(v[i]);uv[i]=new Vector2(p.x,p.z);
                        float n=Mathf.PerlinNoise(p.x*.077f+53,p.z*.061f+41),detail=Mathf.PerlinNoise(p.x*.31f+4,p.z*.28f);
                        colors[i]=new Color(Mathf.Clamp01(.48f+n*.34f+detail*.13f),.10f+(.5f-n)*.12f,0,1);
                    }
                    m.uv2=uv;m.colors=colors;m=Store(m,"Ground_"+name+"_"+f.transform.position.z);f.sharedMesh=m;var c=f.GetComponent<MeshCollider>();if(c)c.sharedMesh=m;r.sharedMaterial=soil;
                }
                else if(name.StartsWith("BV2_Surface road"))r.sharedMaterial=asphalt;
                else if(name.StartsWith("BV2_Surface walk")||material=="Paving")
                {
                    r.sharedMaterial=paving;
                    var f=r.GetComponent<MeshFilter>();if(f)
                    {
                        var m=UnityEngine.Object.Instantiate(f.sharedMesh);var v=m.vertices;var uv=new Vector2[v.Length];
                        for(int i=0;i<v.Length;i++){var p=f.transform.TransformPoint(v[i]);var normal=f.transform.TransformDirection(m.normals[i]);uv[i]=(Mathf.Abs(normal.y)>.65f?new Vector2(p.x,p.z):Mathf.Abs(normal.x)>.65f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/3.4f;}
                        m.uv=uv;m.RecalculateTangents();m=Store(m,"Paving_"+name+"_"+f.transform.position.ToString("F2"));f.sharedMesh=m;var c=f.GetComponent<MeshCollider>();if(c)c.sharedMesh=m;
                    }
                }
                else if(name.Contains("retained soil")||name.Contains("Raised bed soil")||name.Contains("garden soil"))r.sharedMaterial=mulch;
            }
            foreach(var t in district.GetComponentsInChildren<Transform>().Where(t=>t.name=="BV2_Kerb"||t.name=="BV2_Drain surround"||t.name=="BV2_Drain slot").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        static Mesh Store(Mesh mesh,string key)
        {
            string clean=new string(key.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());mesh.name="BV5_"+clean;string path=Generated+"/Meshes/"+mesh.name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior){EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);mesh=prior;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh GardenGrid()
        {
            const int nx=152,nz=74;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                v.Add(new Vector3(x*2-152,.10f,z*2-74));uv.Add(new Vector2(x,z));
                if(x==nx||z==nz)continue;int a=z*(nx+1)+x;t.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});
            }
            var m=new Mesh{indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        static GameObject BoxAt(string name,Vector3 at,Vector3 size,Material mat,bool solid=false,float tile=2)=>Box("BV5_"+name,root,at,size,mat,solid,tile);
        static void Kerb(Vector3 a,Vector3 b)
        {
            float length=Vector3.Distance(a,b);var direction=(b-a).normalized;var right=Vector3.Cross(Vector3.up,direction);int count=Mathf.CeilToInt(length/.99f);float step=length/count;
            // Bevelled top nose, vertical face and buried heel, one metre units.
            // Five millimetres above the retained paving avoids coincident
            // surfaces along the small buried heel overlap.
            var profile=new[]{new Vector2(-.09f,-.10f),new Vector2(.09f,-.10f),new Vector2(.09f,.145f),new Vector2(-.055f,.145f),new Vector2(-.09f,.105f)};
            for(int n=0;n<count;n++)
            {
                var v=new List<Vector3>();var t=new List<int>();float start=n*step+.004f,end=(n+1)*step-.004f;
                foreach(float s in new[]{start,end})foreach(var p in profile)v.Add(a+direction*s+right*p.x+Vector3.up*p.y);
                for(int i=0;i<5;i++){int j=(i+1)%5;t.AddRange(new[]{i,j,5+i,j,5+j,5+i});}
                t.AddRange(new[]{0,2,1,0,3,2,0,4,3,5,6,7,5,7,8,5,8,9});
                BahnhofDetailKit.Mesh("BV5_Chamfered kerb "+a.x+"_"+a.z+"_"+n,root,v,t,stone,false);
            }
            // Two orderly rows of small dark stones form the real road gutter.
            var roadSide=right;var probe=(a+b)*.5f+right*.3f+Vector3.up*2;
            if(!Physics.Raycast(probe,Vector3.down,out var support,4,1)||!support.collider.name.StartsWith("BV2_Surface road"))roadSide=-right;
            for(float s=.16f;s<length;s+=.33f)
            {
                var at=a+direction*s+roadSide*.20f;at.y=.047f;var stoneBlock=BoxAt("Gutter sett",at,new Vector3(.24f,.012f,.31f),edge);stoneBlock.transform.rotation=Quaternion.LookRotation(direction);
            }
        }
        static void StreetEdges()
        {
            foreach(float x in new[]{202.98f,213.02f})foreach(var span in new[]{new Vector2(98,146),new Vector2(156,168),new Vector2(184,223),new Vector2(233,258),new Vector2(274,289),new Vector2(299,303)})
                Kerb(new Vector3(x,0,x<208?span.x:span.y),new Vector3(x,0,x<208?span.y:span.x));
            foreach(float z in new[]{75.02f,84.98f})Kerb(new Vector3(z<80?168:199,0,z),new Vector3(z<80?199:168,0,z));
            foreach(float z in new[]{171.52f,180.48f,301.52f,310.48f})Kerb(new Vector3(z%10<5?218:256,0,z),new Vector3(z%10<5?256:218,0,z));
            foreach(float z in new[]{133f,188,218,248,284})foreach(float x in new[]{203.38f,212.62f})Drain(new Vector3(x,.043f,z));
        }
        static void Drain(Vector3 p)
        {
            BoxAt("Recessed drain pan",p,new Vector3(.56f,.006f,.36f),edge);
            foreach(float x in new[]{-.27f,.27f})BoxAt("Cast drain rim",p+new Vector3(x,.006f,0),new Vector3(.025f,.012f,.38f),iron);
            foreach(float z in new[]{-.18f,.18f})BoxAt("Cast drain rim",p+new Vector3(0,.006f,z),new Vector3(.56f,.012f,.025f),iron);
            for(int i=0;i<9;i++)BoxAt("Drain grate bar",p+new Vector3(-.23f+i*.0575f,.003f,0),new Vector3(.023f,.008f,.34f),iron);
        }
        static void RoadDetails()
        {
            foreach(var at in new[]{new Vector3(207,.047f,118),new Vector3(209,.047f,244),new Vector3(235,.047f,176),new Vector3(263,.047f,283)})
            {
                var c=Cylinder("BV5_Flush service cover",root,at,.33f,.006f,iron);c.transform.Rotate(0,27,0);
                for(int i=-3;i<=3;i++)BoxAt("Service cover ribs",at+new Vector3(0,.005f,i*.075f),new Vector3(.43f,.004f,.018f),edge);
                foreach(float x in new[]{-.15f,.15f})BoxAt("Service cover lifting slot",at+new Vector3(x,.008f,0),new Vector3(.055f,.004f,.028f),iron);
            }
            foreach(var tuple in new[]{new Vector4(205.8f,137,1.3f,2.7f),new Vector4(210.5f,213,1.5f,3.2f),new Vector4(206.3f,288,1.2f,2.2f),new Vector4(244,177,2.5f,1.25f)})
            {
                var patch=BoxAt("Inset asphalt service repair",new Vector3(tuple.x,.0435f,tuple.y),new Vector3(tuple.z,.003f,tuple.w),repair);
                for(int side=-1;side<=1;side+=2)
                {BoxAt("Tar repair seam",new Vector3(tuple.x+side*tuple.z*.5f,.046f,tuple.y),new Vector3(.018f,.003f,tuple.w+.02f),edge);BoxAt("Tar repair seam",new Vector3(tuple.x,.046f,tuple.y+side*tuple.w*.5f),new Vector3(tuple.z,.003f,.018f),edge);}
            }
        }
        static void SquareBands()
        {
            // Flush stone bands frame pedestrian rooms without adding steps.
            foreach(float z in new[]{247f,283f,295f})for(float x=149;x<199;x+=.50f)BoxAt("Square basalt band",new Vector3(x,.143f,z),new Vector3(.485f,.006f,.22f),edge);
            foreach(float x in new[]{155f,168f})for(float z=233;z<296;z+=.50f)
            {if(z>261&&z<270)continue;BoxAt("Square paving cross band",new Vector3(x,.143f,z),new Vector3(.22f,.006f,.485f),stone);}
            // Existing square tree gardens remain; their soil now receives understory.
        }
        static bool SoilAt(float x,float z,out float y)
        {
            y=0;foreach(var h in Physics.RaycastAll(new Vector3(x,35,z),Vector3.down,70,1,QueryTriggerInteraction.Ignore).OrderByDescending(h=>h.point.y))
            {
                var c=h.collider;var r=c.GetComponent<MeshRenderer>();string n=c.name;
                if(c is TerrainCollider)continue;
                if(r&&r.sharedMaterial&&(r.sharedMaterial==soil||r.sharedMaterial==mulch||r.sharedMaterial.name=="GardenEarth"||n.Contains("retained soil")||n.Contains("Raised bed soil"))){y=h.point.y;return true;}
                if(h.point.y>-.1f)return false; // Never grow through paving, buildings or props.
            }
            return false;
        }
        static void TreeGrilles()
        {
            foreach(float z in new[]{242f,288})
            {
                var v=new List<Vector3>();var t=new List<int>();
                foreach(var ring in new[]{new Vector2(.38f,.42f),new Vector2(.80f,.86f)})for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                    Vector3 P(float radius,float y,float angle)=>new Vector3(171+Mathf.Cos(angle)*radius,y,z+Mathf.Sin(angle)*radius);
                    void Q(Vector3 a0,Vector3 b0,Vector3 c0,Vector3 d0){int s=v.Count;v.AddRange(new[]{a0,b0,c0,d0});t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});}
                    Q(P(ring.x,.37f,a),P(ring.x,.37f,b),P(ring.y,.37f,b),P(ring.y,.37f,a));
                    Q(P(ring.y,.34f,a),P(ring.y,.37f,a),P(ring.y,.37f,b),P(ring.y,.34f,b));
                    Q(P(ring.x,.34f,b),P(ring.x,.37f,b),P(ring.x,.37f,a),P(ring.x,.34f,a));
                }
                BahnhofDetailKit.Mesh("BV5_Cast tree grille "+z,root,v,t,iron,false);
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI/12;var bar=BoxAt("Tree grille radial bar",new Vector3(171+Mathf.Cos(a)*.61f,.355f,z+Mathf.Sin(a)*.61f),new Vector3(.40f,.03f,.022f),iron);bar.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                }
            }
        }
        static void Plant(float x,float z,float scale,int kind)
        {
            if(!SoilAt(x,z,out float y))return;
            var mesh=kind==0?assets["tuft"]:kind==5?assets["perennial"]:AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_"+kind+"_"+(plantRoots.Count%3)+".asset");if(!mesh)return;
            var pos=new Vector3(x,y-.014f,z);plants.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(pos,Quaternion.Euler(0,R(0,360),0),new Vector3(scale,scale,scale))});plantRoots.Add(new Vector2(x,z));plantHeights.Add(new Vector2(pos.y,1));
        }
        static void PlantBeds()
        {
            Physics.SyncTransforms();
            foreach(var bed in district.GetComponentsInChildren<Transform>().Where(t=>t.name=="BV4_Planting island"||t.name.StartsWith("BV3_Circular station tree garden ")||t.name=="BV2_Raised front planting"))
            {
                var soilCollider=bed.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.name.Contains("soil",StringComparison.OrdinalIgnoreCase));if(!soilCollider)continue;var b=soilCollider.bounds;
                for(float x=b.min.x+.24f;x<b.max.x-.18f;x+=.36f)for(float z=b.min.z+.24f;z<b.max.z-.18f;z+=.39f)
                {if(bed.name.StartsWith("BV3_Circular")&&Vector2.Distance(new Vector2(x,z),new Vector2(bed.position.x,bed.position.z))<.98f)continue;Plant(x+R(-.07f,.07f),z+R(-.07f,.07f),R(.45f,.80f),rng.NextDouble()<.26?5:0);}
            }
        }
        static void GardenMargins()
        {
            // Coherent planted ribbons in soil, set back from entrances and crossings.
            foreach(float z in new[]{116f,147f,202f,233f,275f})
                for(float dz=-10;dz<=10;dz+=.40f)for(float dx=0;dx<1.4f;dx+=.40f)
                {if(Mathf.Abs(dz)<2.8f)continue;Plant(228.2f+dx,z+dz,R(.65f,1.05f),rng.NextDouble()<.18?5:0);}
            foreach(float x in new[]{145.2f,146.0f,297.8f,299.0f})for(float z=125;z<330;z+=.48f)
                Plant(x+R(-.2f,.2f),z+R(-.2f,.2f),R(.65f,1.1f),rng.NextDouble()<.18?3:0);
            foreach(int side in new[]{-1,1})for(int h=0;h<4;h++)
            {
                float x=-82+h*53;
                for(float dx=-15;dx<16;dx+=.55f)for(float dz=0;dz<1.6f;dz+=.5f)
                {if(Mathf.Abs(dx)<2.2f)continue;Plant(x+dx,side*(160+dz),R(.55f,.85f),rng.NextDouble()<.28?5:0);}
            }
        }
        static void ReplaceTrees()
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_Leaves.mat");var bark=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_Bark.mat");
            var treeMeshes=new Mesh[3,2];for(int type=0;type<3;type++)for(int lod=0;lod<2;lod++)treeMeshes[type,lod]=BuildTree(type,lod);
            int count=0;
            foreach(var group in district.GetComponentsInChildren<LODGroup>().Where(g=>g.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh&&f.sharedMesh.name.StartsWith("UrbanTree_"))).ToArray())
            {
                int type=count++%3;var filters=group.GetComponentsInChildren<MeshFilter>();
                for(int i=0;i<filters.Length;i++){filters[i].sharedMesh=treeMeshes[type,Mathf.Min(i,1)];filters[i].GetComponent<MeshRenderer>().sharedMaterials=new[]{bark,source};}
                var rs=filters.Select(f=>f.GetComponent<Renderer>()).ToArray();if(rs.Length>=2)group.SetLODs(new[]{new LOD(.10f,new[]{rs[0]}),new LOD(.006f,new[]{rs[1]})});group.RecalculateBounds();
            }
        }
        static Mesh BuildTree(int type,int lod)
        {
            var random=new System.Random(91031+type*419);float height=new[]{7.8f,9.2f,6.8f}[type],radius=new[]{2.45f,2.6f,2.25f}[type];
            var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var trunk=new List<int>();var leaves=new List<int>();
            float Rand(float a,float b)=>a+(float)random.NextDouble()*(b-a);
            void Tube(Vector3 a,Vector3 b,float ra,float rb)
            {
                var axis=(b-a).normalized;var right=Vector3.Cross(axis,Vector3.forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;var side=Vector3.Cross(axis,right);int start=v.Count,n=lod==0?9:6;
                for(int j=0;j<2;j++)for(int i=0;i<=n;i++){float angle=i*Mathf.PI*2/n;v.Add((j==0?a:b)+(right*Mathf.Cos(angle)+side*Mathf.Sin(angle))*(j==0?ra:rb));uv.Add(new Vector2(i/(float)n,(j==0?a.y:b.y)*.6f));colors.Add(Color.white);}
                for(int i=0;i<n;i++)trunk.AddRange(new[]{start+i,start+i+n+1,start+i+1,start+i+1,start+i+n+1,start+i+n+2});
            }
            Tube(new Vector3(0,-.08f,0),new Vector3(.12f,height*.70f,.06f),.20f,.045f);
            var tips=new List<Vector3>();
            for(int i=0;i<11;i++)
            {
                float a=i*2.39996f;var start=new Vector3(.06f,height*Rand(.29f,.60f),.03f);var end=new Vector3(Mathf.Cos(a)*radius*Rand(.55f,.90f),height*Rand(.63f,.89f),Mathf.Sin(a)*radius*Rand(.55f,.9f));
                var mid=Vector3.Lerp(start,end,.5f)+Vector3.up*.22f;Tube(start,mid,.075f,.045f);Tube(mid,end,.045f,.014f);
                for(int j=0;j<3;j++){var tip=end+new Vector3(Rand(-.55f,.55f),Rand(.20f,.65f),Rand(-.55f,.55f));Tube(Vector3.Lerp(mid,end,.7f),tip,.018f,.003f);tips.Add(tip);}
            }
            int leafCount=lod==0?4800:1100;
            for(int i=0;i<leafCount;i++)
            {
                var tip=tips[i%tips.Count];var pos=tip+new Vector3(Rand(-.85f,.85f),Rand(-.55f,.60f),Rand(-.85f,.85f));
                var rotation=Quaternion.Euler(Rand(-65,65),Rand(0,360),Rand(-50,50));float size=Rand(.12f,.18f)*(lod==0?1:1.30f);int start=v.Count;
                int species=type==0?0:type==1?2:3;var offset=new Vector2(species%2*.5f,species/2*.5f);var tint=new Color(Rand(.78f,.98f),Rand(.85f,1),Rand(.70f,.88f)).linear;
                foreach(var p in new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)}){v.Add(pos+rotation*new Vector3(p.x*size*.6f,p.y*size,0));uv.Add(offset+new Vector2((p.x+1)*.25f,(p.y+1)*.25f));colors.Add(tint);}
                leaves.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var m=new Mesh{indexFormat=IndexFormat.UInt32};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(colors);m.subMeshCount=2;m.SetTriangles(trunk,0);m.SetTriangles(leaves,1);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return Store(m,"StreetTree_"+type+"_"+lod);
        }
        static void BuildUnderstory()
        {
            for(int kind=0;kind<2;kind++)
            {
                var random=new System.Random(55177+kind);var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var t=new List<int>();
                float Rand(float a,float b)=>a+(float)random.NextDouble()*(b-a);
                void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
                {int s=v.Count;v.AddRange(new[]{a,b,c});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one});colors.AddRange(new[]{color*.70f,color,color});t.AddRange(new[]{s,s+1,s+2});}
                for(int i=0;i<(kind==0?60:36);i++)
                {
                    float a=i*2.39996f,height=Rand(.22f,.52f);var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,dir);
                    var start=dir*Rand(.01f,.05f);var middle=start+Vector3.up*height*.60f+dir*.06f;
                    var bend=start+Vector3.up*height*.95f+dir*.17f;var tip=start+Vector3.up*height*.83f+dir*Rand(.25f,.32f);float width=Rand(.009f,.018f);
                    var color=new Color(Rand(.24f,.36f),Rand(.36f,.49f),Rand(.14f,.23f)).linear;
                    Triangle(start,middle-side*width,middle+side*width,color);
                    Triangle(middle-side*width,bend-side*width*.55f,bend+side*width*.55f,color);
                    Triangle(middle-side*width,bend+side*width*.55f,middle+side*width,color);
                    Triangle(bend-side*width*.55f,tip,bend+side*width*.55f,color*1.13f);
                }
                if(kind==1)for(int stem=0;stem<5;stem++)
                {
                    float a=stem*2.39996f;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var center=dir*.11f;float height=Rand(.47f,.64f);var side=Vector3.Cross(dir,Vector3.up)*.006f;
                    Triangle(center-side,center+Vector3.up*height,center+side,new Color(.25f,.35f,.17f).linear);
                    for(int flower=0;flower<9;flower++)
                    {
                        var p=center+Vector3.up*(height-.17f+flower*.019f);float radius=Mathf.Lerp(.042f,.009f,flower/9f);var c=new Color(.48f+Rand(-.06f,.06f),.35f,.61f).linear;
                        for(int petal=0;petal<5;petal++){float angle=petal*Mathf.PI*.4f;var d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;Triangle(p,p+d+Vector3.up*.019f,p+Quaternion.Euler(0,45,0)*d,c);}
                    }
                }
                var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();assets[kind==0?"tuft":"perennial"]=Store(mesh,kind==0?"OrnamentalTuft":"FloweringPerennial");
            }
        }
        static void BakePlants()
        {
            if(plants.Count==0)return;
            var material=BahnhofDetailKit.Load("DistrictFlora");
            foreach(var group in Enumerable.Range(0,plants.Count).GroupBy(i=>new Vector2Int(Mathf.FloorToInt(plantRoots[i].x/20),Mathf.FloorToInt(plantRoots[i].y/20))))
            {
                var chunk=Group("BV5_Planting LOD "+group.Key.x+"_"+group.Key.y,root,Vector3.zero);var indices=group.ToArray();var renderers=new Renderer[2];
                for(int level=0;level<2;level++)
                {
                    var selected=indices.Where((index,order)=>level==0||order%3==0).ToArray();
                    var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(selected.Select(i=>plants[i]).ToArray(),true,true);
                    var heights=new List<Vector2>();var roots=new List<Vector2>();
                    foreach(int p in selected)for(int i=0;i<plants[p].mesh.vertexCount;i++){heights.Add(new Vector2(plantHeights[p].x,Mathf.Clamp01(plants[p].mesh.vertices[i].y)));roots.Add(plantRoots[p]);}
                    mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);mesh=Store(mesh,"RootedPlanting_"+group.Key.x+"_"+group.Key.y+"_"+level);
                    var go=Group("BV5_Rooted planting detail "+level,chunk.transform,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=level==0?ShadowCastingMode.On:ShadowCastingMode.Off;renderers[level]=renderer;
                }
                var lod=chunk.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;
                lod.SetLODs(new[]{new LOD(.40f,new[]{renderers[0]}),new LOD(.008f,new[]{renderers[1]})});lod.RecalculateBounds();
            }
        }
    }
}
