using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Local authoring only: the existing district terrain and flora are preserved.
    public static class WestbruchOverpassGround
    {
        const float Crest = 5.95f;
        const string Prefix = "OverpassGround_";
        static Terrain terrain;
        static float Ground(float x, float z)
        {
            if(!terrain)return -.025f;
            float bottom=terrain.transform.position.z,top=bottom+terrain.terrainData.size.z;
            float sampled=terrain.SampleHeight(new Vector3(x,0,Mathf.Clamp(z,bottom,top)))+terrain.transform.position.y;
            float outside=Mathf.Max(bottom-z,0,z-top);
            return Mathf.Lerp(sampled,.12f,Smooth(0,40,outside));
        }
        static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, value));

        public static void Build(Transform parent)
        {
            terrain = Terrain.activeTerrain;
            FlattenLocalPavementAndCamp();
            var root = Group("Railway earthworks – continuous vegetated embankments", parent, Vector3.zero).transform;
            Material earth = GroundMaterial();
            Bank(root, -620, 63, "South", earth);
            Bank(root, 97, 620, "North", earth);
            HorizonAprons(root);
            CampSurface(root, earth);
            BankVegetation(root);
            ReclaimedWestGround(root);
        }

        // Update a small heightmap rectangle, rather than recreating the terrain.
        static void FlattenLocalPavementAndCamp()
        {
            if (!terrain) return;
            var data = terrain.terrainData;
            var origin = terrain.transform.position;
            int r = data.heightmapResolution;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((98 - origin.x) / data.size.x * (r - 1)), 0, r - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((165 - origin.x) / data.size.x * (r - 1)), 0, r - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((56 - origin.z) / data.size.z * (r - 1)), 0, r - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((104 - origin.z) / data.size.z * (r - 1)), 0, r - 1);
            var heights = data.GetHeights(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
            for (int iz = 0; iz < heights.GetLength(0); iz++)
            for (int ix = 0; ix < heights.GetLength(1); ix++)
            {
                float x = origin.x + (x0 + ix) * data.size.x / (r - 1);
                float z = origin.z + (z0 + iz) * data.size.z / (r - 1);
                float distance = Mathf.Min(RectDistance(x,z,100,162,72,88), RectDistance(x,z,139,163,58,102));
                distance = Mathf.Min(distance, RectDistance(x,z,113.2f,120.7f,66.4f,72));
                distance = Mathf.Min(distance, Mathf.Max(0, SegmentDistance(new Vector2(x,z),new Vector2(113.2f,70),new Vector2(110,73.5f)) - .6f));
                float blend = 1 - Smooth(0, 2, distance);
                if (blend <= 0) continue;
                heights[iz,ix] = Mathf.Lerp(heights[iz,ix],(-.025f-origin.y)/data.size.y,blend);
            }
            data.SetHeights(x0,z0,heights);
            terrain.Flush();
            EditorUtility.SetDirty(data);
        }

        static float RectDistance(float x,float z,float left,float right,float bottom,float top)
            => new Vector2(Mathf.Max(left-x,0,x-right),Mathf.Max(bottom-z,0,z-top)).magnitude;
        static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
            => Vector2.Distance(p, a+(b-a)*Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude));

        // The front 12 m meet the rising retaining wings. Nothing extends into
        // the 34 m road opening. At the ends only the retained crest remains.
        static void Profile(float z,out float left,out float right,out float toeHeight)
        {
            float end = z < 80 ? Mathf.InverseLerp(51,63,z) : 1-Mathf.InverseLerp(97,109,z);
            float variation = (Mathf.PerlinNoise(z*.041f+19,2.3f)-.5f)*.70f*(1-end);
            left = Mathf.Lerp(103.5f+variation,115.4f,end);
            right = Mathf.Lerp(132.5f+variation,120.6f,end);
            float stop=Smooth(0,10,z)*(1-Smooth(24,34,z));
            left=Mathf.Lerp(left,107.3f,stop);
            toeHeight = Crest*end;
        }
        public static float Surface(float x,float z)
        {
            if(z>63&&z<97)return Ground(x,z);
            Profile(z,out float left,out float right,out _);
            return x>=left&&x<=right?BankHeight(x,z):Ground(x,z);
        }
        static float BankHeight(float x,float z)
        {
            Profile(z,out float left,out float right,out float toe);
            if(x>=115.4f&&x<=120.6f)return Crest;
            bool west=x<115.4f;
            float t=west?Mathf.InverseLerp(left,115.4f,x):Mathf.InverseLerp(right,120.6f,x);
            float baseY=Mathf.Max(Ground(x,z)-.025f,toe-.025f);
            float height=Mathf.Lerp(baseY,Crest,t);
            // Centimetres of natural settlement, never a wavy railway crest.
            height+=(Mathf.PerlinNoise(x*.43f+47,z*.29f+61)-.5f)*.075f*Mathf.Sin(t*Mathf.PI);
            return height;
        }

        static void Bank(Transform root,float start,float end,string id,Material material)
        {
            int rows=Mathf.CeilToInt((end-start)/1.25f);
            const int cols=17;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int row=0;row<=rows;row++)
            {
                float z=Mathf.Lerp(start,end,row/(float)rows);
                Profile(z,out float left,out float right,out _);
                for(int col=0;col<cols;col++)
                {
                    float x=col<7?Mathf.Lerp(left,115.4f,col/6f):col<10?Mathf.Lerp(115.4f,120.6f,(col-6)/4f):Mathf.Lerp(120.6f,right,(col-10)/6f);
                    var p=new Vector3(x,BankHeight(x,z),z);
                    vertices.Add(p);uv.Add(WorldUV(p));
                    if(row==rows||col==cols-1)continue;
                    int a=row*cols+col;triangles.AddRange(new[]{a,a+cols,a+1,a+1,a+cols,a+cols+1});
                }
            }
            MeshPart("Continuous "+id+" railway earth bank",root,vertices,uv,triangles,material,true);
        }
        static Vector2 WorldUV(Vector3 p)=>new Vector2((p.x-101.5f)/33f,(p.z+620)/1240f);

        static void HorizonAprons(Transform root)
        {
            if(!terrain)return;
            var material=Material(Prefix+"DistantGround","grass",new Color(.72f,.71f,.65f),.08f);
            ConfigureGroundDetail(material);
            foreach(int sign in new[]{-1,1})
            {
                float edge=sign<0?terrain.transform.position.z:terrain.transform.position.z+terrain.terrainData.size.z;
                var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
                const int cols=25,rows=100;
                for(int iz=0;iz<=rows;iz++)for(int ix=0;ix<=cols;ix++)
                {
                    float x=Mathf.Lerp(44,192,ix/(float)cols),z=Mathf.Lerp(edge-sign*2,sign*620,iz/(float)rows);
                    var p=new Vector3(x,Ground(x,z)-.035f,z);v.Add(p);uv.Add(new Vector2(x,z)/8);
                    if(iz==rows||ix==cols)continue;int a=iz*(cols+1)+ix;
                    t.AddRange(new[]{a,a+cols+1,a+1,a+1,a+cols+1,a+cols+2});
                }
                if(sign<0)t.Reverse();
                MeshPart("Land below distant railway "+sign,root,v,uv,t,material,true);
            }
        }

        static void CampSurface(Transform root,Material material)
        {
            // Feathered, irregular perimeter sinks into the existing earth.
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            const int spokes=48, rings=9;
            Vector2 center=new Vector2(116.7f,69.15f);
            for(int ring=0;ring<=rings;ring++)
            for(int side=0;side<=spokes;side++)
            {
                float angle=side*Mathf.PI*2/spokes;
                float radius=ring/(float)rings;
                float irregular=1+(Mathf.PerlinNoise(Mathf.Cos(angle)*2+11,Mathf.Sin(angle)*2+17)-.5f)*.18f;
                float x=center.x+Mathf.Cos(angle)*5.0f*radius*irregular,z=center.y+Mathf.Sin(angle)*3.0f*radius*irregular;
                float y=Ground(x,z)+Mathf.Lerp(.014f,-.018f,Smooth(.82f,1,radius));
                var p=new Vector3(x,y,z);v.Add(p);uv.Add(WorldUV(p));
                if(ring==rings||side==spokes)continue;
                int a=ring*(spokes+1)+side;
                // Polar coordinates use the opposite winding to a cartesian grid.
                t.AddRange(new[]{a,a+1,a+spokes+1,a+1,a+spokes+2,a+spokes+1});
            }
            MeshPart("Compacted sheltered sleeping court",root,v,uv,t,material,false);
            v.Clear();uv.Clear();t.Clear();
            Vector2 from=new Vector2(114,69.5f),to=new Vector2(110,73.5f),axis=(to-from).normalized,across=new Vector2(-axis.y,axis.x);
            for(int i=0;i<=16;i++)
            {
                float q=i/16f;Vector2 centerline=Vector2.Lerp(from,to,q)+across*Mathf.Sin(q*Mathf.PI)*.17f;
                for(int j=0;j<5;j++)
                {
                    float lateral=(j-2)*.31f;
                    var point=centerline+across*lateral;
                    float y=Ground(point.x,point.y)+.012f;
                    // The paving overlays the final metre; there is no raised slab.
                    if(j==0||j==4)y-=.025f;
                    var p=new Vector3(point.x,y,point.y);v.Add(p);uv.Add(WorldUV(p));
                    if(i==16||j==4)continue;
                    int a=i*5+j;t.AddRange(new[]{a,a+5,a+1,a+1,a+5,a+6});
                }
            }
            // from-to and its left normal give the reverse winding.
            t.Reverse();
            MeshPart("Worn earth route to Westbruch pavement",root,v,uv,t,material,false);
        }

        static void ReclaimedWestGround(Transform root)
        {
            const string texturePath=Generated+"/Materials/OverpassGround_WestRegrowthV4.png";
            if(!File.Exists(texturePath))BakeMacroTexture(texturePath,28,85,55,75,1024,512);
            var material=Material(Prefix+"WestRegrowth",null,Color.white,.08f);
            material.shader=Shader.Find("HDRP/Lit");material.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));ConfigureGroundDetail(material);
            var flora=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/DistrictFlora.mat");
            var sources=new Mesh[3];for(int i=0;i<3;i++)sources[i]=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_0_"+i+".asset");
            var sites=new[]{new Vector2(35,62),new Vector2(42,69),new Vector2(49,60),new Vector2(54,68),new Vector2(62,62),new Vector2(68,70),new Vector2(75,61),new Vector2(79,68)};
            var random=new System.Random(124038);
            for(int patch=0;patch<sites.Length;patch++)
            {
                Vector2 center=sites[patch];const int spokes=36,rings=7;
                var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
                float rx=patch%3==0?3.8f:3.3f,rz=patch%2==0?2.3f:2.0f;
                for(int ring=0;ring<=rings;ring++)for(int side=0;side<=spokes;side++)
                {
                    float a=side*Mathf.PI*2/spokes,r=ring/(float)rings;
                    float irregular=.85f+Mathf.PerlinNoise(Mathf.Cos(a)*2+patch*8,Mathf.Sin(a)*2+11)*.3f;
                    float x=center.x+Mathf.Cos(a)*rx*r*irregular,z=center.y+Mathf.Sin(a)*rz*r*irregular;
                    float y=Ground(x,z)+Mathf.Lerp(.009f,-.025f,Smooth(.72f,1,r));
                    v.Add(new Vector3(x,y,z));uv.Add(new Vector2((x-28)/57,(z-55)/20));
                    if(ring==rings||side==spokes)continue;int n=ring*(spokes+1)+side;
                    t.AddRange(new[]{n,n+1,n+spokes+1,n+1,n+spokes+2,n+spokes+1});
                }
                MeshPart("Reclaimed bridge footprint soil "+patch,root,v,uv,t,material,false);
                if(!flora||!sources[0]||!sources[1]||!sources[2])continue;
                var parts=new List<CombineInstance>();var heights=new List<Vector2>();var roots=new List<Vector2>();
                for(int plant=0;plant<68;plant++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble())*.91f;
                    float x=center.x+Mathf.Cos(a)*rx*r,z=center.y+Mathf.Sin(a)*rz*r;
                    if(Mathf.PerlinNoise(x*.63f+3,z*.52f+5)<.40f)continue;
                    float y=Ground(x,z)-.017f,scale=.70f+(float)random.NextDouble()*.63f;
                    var mesh=sources[random.Next(3)];
                    parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(x,y,z),Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*scale)});
                    foreach(var vertex in mesh.vertices){heights.Add(new Vector2(y,Mathf.Clamp01(vertex.y/mesh.bounds.max.y)));roots.Add(new Vector2(x,z));}
                }
                PlantChunk(root,"Reclaimed footprint vegetation "+patch,parts,heights,roots,flora);
            }
        }

        static void PlantChunk(Transform root,string name,List<CombineInstance> parts,List<Vector2> heights,List<Vector2> roots,Material material)
        {
            if(parts.Count==0)return;
            string path=Generated+"/Meshes/"+Prefix+name.Replace(' ','_')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(parts.ToArray(),true,true);mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);
            var bounds=mesh.bounds;bounds.Expand(new Vector3(2.6f,1.2f,2.6f));mesh.bounds=bounds;EditorUtility.SetDirty(mesh);
            var obj=Group(name,root,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        static Material GroundMaterial()
        {
            const string texturePath=Generated+"/Materials/OverpassGround_Macro1240V4.png";
            if(!File.Exists(texturePath))BakeMacroTexture(texturePath,101.5f,134.5f,-620,620,512,8192);
            var mat=Material(Prefix+"LivingEarth",null,Color.white,.09f);
            mat.shader=Shader.Find("HDRP/Lit");
            mat.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            mat.SetTextureScale("_BaseColorMap",Vector2.one);
            mat.SetTexture("_NormalMap",null);mat.SetTexture("_MaskMap",null);
            mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.09f);
            ConfigureGroundDetail(mat);return mat;
        }

        static void ConfigureGroundDetail(Material material)
        {
            const string path=Generated+"/Materials/OverpassGround_PhotographicDetail.png";
            if(!File.Exists(path))BakeGroundDetail(path);
            material.SetTexture("_DetailMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            // Player vertex compression packs UV0 to half precision. Multiplying
            // its 1240 m macro axis into detail coordinates causes metre-wide
            // streaks in standalone builds. UV1 is excluded from this project's
            // vertex compression and stores independent metre coordinates.
            material.SetFloat("_UVDetail",1);material.SetVector("_UVDetailsMappingMask",new Vector4(0,1,0,0));
            material.SetFloat("_LinkDetailsWithBase",0);
            material.SetTextureScale("_DetailMap",Vector2.one/3f);
            material.SetFloat("_DetailAlbedoScale",1.45f);material.SetFloat("_DetailNormalScale",.65f);material.SetFloat("_DetailSmoothnessScale",.10f);
            HDMaterial.ValidateMaterial(material);EditorUtility.SetDirty(material);
        }

        static void BakeGroundDetail(string path)
        {
            // HDRP detail packing: R albedo, G normal Y, B smoothness,
            // A normal X. Reuse the project's real yard/grass photography.
            var yard=new ImageSource("yard");var grass=new ImageSource("grass");
            const int size=1024;var pixels=new Color32[size*size];
            float Luma(Color c)=>c.r*.25f+c.g*.60f+c.b*.15f;
            float Value(float u,float v)=>Luma(Color.Lerp(yard.Sample(u,v),grass.Sample(u,v),.22f));
            float mean=0;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)mean+=Value((x+.5f)/64,(y+.5f)/64);
            mean/=4096;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size,v=(y+.5f)/size,step=1f/size;
                float h=Value(u,v),dx=(Value(u+step,v)-Value(u-step,v))*1.6f,dy=(Value(u,v+step)-Value(u,v-step))*1.6f;
                var normal=new Vector3(-dx,-dy,1).normalized;
                pixels[y*size+x]=new Color(Mathf.Clamp01(.5f+(h-mean)*1.15f),normal.y*.5f+.5f,.5f,normal.x*.5f+.5f);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled=true;importer.maxTextureSize=size;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=16;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        sealed class ImageSource
        {
            readonly Color32[] pixels;readonly int width,height;
            public ImageSource(string id)
            {
                string path="Assets/ZeroRise/Art/SurfacesV3/"+id+".jpg";
                if(!File.Exists(path))path="Assets/ZeroRise/Art/Surfaces/"+id+".jpg";
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                ImageConversion.LoadImage(texture,File.ReadAllBytes(path));width=texture.width;height=texture.height;pixels=texture.GetPixels32();UnityEngine.Object.DestroyImmediate(texture);
            }
            public Color Sample(float x,float z)
            {
                int ix=Mathf.FloorToInt(Mathf.Repeat(x,1)*width),iz=Mathf.FloorToInt(Mathf.Repeat(z,1)*height);
                return pixels[iz*width+ix];
            }
        }
        static void BakeMacroTexture(string path,float minX,float maxX,float minZ,float maxZ,int width,int height)
        {
            // Reuse actual project surface photography with rotated secondary
            // sampling and large coherent vegetation/damp fields. The 1240 m
            // map contains no repeated large-scale texture motif.
            var pixels=new Color32[width*height];
            for(int iz=0;iz<height;iz++)for(int ix=0;ix<width;ix++)
            {
                float x=minX+(ix+.5f)*(maxX-minX)/width,z=minZ+(iz+.5f)*(maxZ-minZ)/height;
                float broad=Mathf.PerlinNoise(x*.10f+39,z*.073f+72);
                float detail=Mathf.PerlinNoise(x*.57f+4,z*.41f+19);
                float field=broad*.77f+detail*.23f;
                float mix=Smooth(.40f,.59f,field);
                float slope=Mathf.Clamp01(Mathf.Abs(x-118)/3.3f);
                float grassAmount=Mathf.Lerp(.035f,.91f,mix)*slope;
                // Footwear and dry shelter keep the small sleeping area bare.
                float camp=1-Smooth(0,1.6f,RectDistance(x,z,112.7f,121.2f,66.0f,72.4f));
                float route=1-Smooth(.35f,1.15f,SegmentDistance(new Vector2(x,z),new Vector2(114,69.5f),new Vector2(110,73.5f)));
                grassAmount*=1-Mathf.Max(camp,route)*.97f;
                // Only metre-scale colour lives in this map. Sharp photographic
                // soil grain and normal response come from the independent
                // detail map, avoiding stretched, blurry source motifs.
                Color earth=new Color(.51f,.445f,.35f);
                Color green=new Color(.30f,.335f,.205f);
                Color soil=new Color(.315f,.285f,.235f);
                float damp=Smooth(.43f,.61f,Mathf.PerlinNoise(x*.15f+53,z*.11f+17));
                float mineral=Smooth(.47f,.65f,Mathf.PerlinNoise(x*.085f+18,z*.071f+97));
                earth=Color.Lerp(earth,soil,damp*.64f+camp*.10f);
                earth=Color.Lerp(earth,new Color(.445f,.435f,.375f),mineral*.54f);
                Color result=Color.Lerp(earth,green,grassAmount);
                float macro=Mathf.Lerp(.77f,1.12f,Smooth(.30f,.69f,Mathf.PerlinNoise(x*.031f+71,z*.037f+44)));
                result*=macro;result.a=1;pixels[iz*width+ix]=result;
            }
            var image=new Texture2D(width,height,TextureFormat.RGBA32,true);image.SetPixels32(pixels);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=Mathf.Max(width,height);
            importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }

        static void BankVegetation(Transform root)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/DistrictFlora.mat");
            if(!material)return;
            var sources=new Mesh[3];for(int i=0;i<3;i++)sources[i]=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_0_"+i+".asset");
            if(!sources[0]||!sources[1]||!sources[2])return;
            var random=new System.Random(430291);
            float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            for(int section=-20;section<=19;section++)
            {
                var parts=new List<CombineInstance>();var heights=new List<Vector2>();var roots=new List<Vector2>();
                for(int i=0;i<370;i++)
                {
                    float z=section*32+Range(0,32);if(z< -620||z>620||(z>51&&z<109))continue;
                    Profile(z,out float left,out float right,out _);
                    float x=Range(left+.20f,right-.20f);if(x>114.3f&&x<121.7f)continue;
                    float abundance=Mathf.PerlinNoise(x*.28f+9,z*.21f+43);
                    if(Range(0,1)>abundance*.9f+.12f)continue;
                    var mesh=sources[random.Next(3)];float y=BankHeight(x,z)-.018f;
                    float scale=Range(.80f,1.38f);
                    var planted=new Vector3(x,y,z);
                    // Plants stay upright, rooted a little into the slope.
                    var transform=Matrix4x4.TRS(planted,Quaternion.Euler(0,Range(0,360),0),Vector3.one*scale);
                    parts.Add(new CombineInstance{mesh=mesh,transform=transform});
                    foreach(var vertex in mesh.vertices){heights.Add(new Vector2(y,Mathf.Clamp01(vertex.y/mesh.bounds.max.y)));roots.Add(new Vector2(x,z));}
                }
                if(parts.Count==0)continue;
                string path=Generated+"/Meshes/"+Prefix+"Flora_"+section+".asset";
                var combined=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!combined){combined=new Mesh();AssetDatabase.CreateAsset(combined,path);}else combined.Clear();
                combined.indexFormat=IndexFormat.UInt32;combined.CombineMeshes(parts.ToArray(),true,true);
                combined.SetUVs(2,heights);combined.SetUVs(3,roots);var bounds=combined.bounds;bounds.Expand(new Vector3(2.6f,1.2f,2.6f));combined.bounds=bounds;
                EditorUtility.SetDirty(combined);
                var obj=Group("Rooted embankment grass "+section,root,Vector3.zero);obj.isStatic=true;
                obj.AddComponent<MeshFilter>().sharedMesh=combined;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
        }

        static void MeshPart(string name,Transform parent,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material material,bool solid)
        {
            string path=Generated+"/Meshes/"+Prefix+name.Replace(' ','_')+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            var clean=new List<int>(triangles.Count);
            for(int i=0;i<triangles.Count;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude>1e-10f)clean.AddRange(new[]{a,b,c});
            }
            mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
            mesh.SetUVs(1,vertices.ConvertAll(p=>new Vector2(p.x-118,p.z-80)));
            mesh.SetTriangles(clean,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var obj=Group(name,parent,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
