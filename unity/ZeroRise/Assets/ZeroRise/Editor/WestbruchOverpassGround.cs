using System.Collections.Generic;
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
        sealed class BankPatch
        {
            public float start,end;public int rows,cols;public List<Vector3> vertices;
        }
        static readonly List<BankPatch> bankPatches=new List<BankPatch>();
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
            bankPatches.Clear();
            FlattenLocalPavementAndCamp();
            var root = Group("Railway earthworks – continuous vegetated embankments", parent, Vector3.zero).transform;
            Material earth = GroundMaterial();
            Bank(root, -620, 63, "South", earth);
            Bank(root, 97, 620, "North", earth);
            HorizonAprons(root);
            BlendTerrainFeet();
            CampSurface(root,GroundMaterial("ShelteredSoil"));
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
            // The retaining wings start at EXACT authored positions. Organic
            // variation fades out before those interfaces rather than leaving
            // little triangular holes between wall and earth.
            float wings=1-Smooth(45,51,z)* (1-Smooth(109,115,z));
            float variation = (Mathf.PerlinNoise(z*.041f+19,2.3f)-.5f)*.70f*(1-end)*wings;
            left = Mathf.Lerp(103.5f+variation,115.4f,end);
            right = Mathf.Lerp(132.5f+variation,120.6f,end);
            float stop=Smooth(0,10,z)*(1-Smooth(24,34,z));
            left=Mathf.Lerp(left,107.3f,stop);
            toeHeight = Crest*end;
        }
        static float FootWidth(float z)
        {
            float wings=1-Smooth(45,51,z)*(1-Smooth(109,115,z));
            float stop=Smooth(0,10,z)*(1-Smooth(24,34,z));
            return 2*wings*(1-stop);
        }
        static void Extents(float z,out float left,out float right)
        {
            Profile(z,out left,out right,out _);
            float foot=FootWidth(z);left-=foot;right+=foot;
            left=Mathf.Max(103.25f,left);right=Mathf.Min(134.5f,right);
        }
        public static float Surface(float x,float z)
        {
            if(z>63&&z<97)return Ground(x,z);
            return TryBankSurface(x,z,out float height,out _)?Mathf.Max(Ground(x,z),height):Ground(x,z);
        }
        static float BankHeight(float x,float z)
        {
            Profile(z,out float left,out float right,out float toe);
            if(x>=115.4f&&x<=120.6f)return Crest;
            Extents(z,out float outerLeft,out float outerRight);
            bool west=x<115.4f;
            float t=west?Mathf.InverseLerp(outerLeft,115.4f,x):Mathf.InverseLerp(outerRight,120.6f,x);
            float baseY=Mathf.Max(Ground(x,z)-.035f,toe-.025f);
            // C1-continuous slope: zero derivative at the buried outer foot
            // and at the horizontal rail crest. No planar wedge or sharp lip.
            float height=Mathf.Lerp(baseY,Crest,t*t*(3-2*t));
            float settlement=Mathf.Pow(Mathf.Sin(t*Mathf.PI),2);
            height+=(Mathf.PerlinNoise(x*.29f+47,z*.19f+61)-.5f)*.12f*settlement;
            return height;
        }

        static bool TryBankSurface(float x,float z,out float height,out Vector3 normal)
        {
            height=Ground(x,z);normal=Vector3.up;
            Extents(z,out float left,out float right);
            if((z>63&&z<97)||x<left||x>right)return false;
            foreach(var patch in bankPatches)
            {
                if(z<patch.start||z>patch.end)continue;
                int row=Mathf.Clamp(Mathf.FloorToInt((z-patch.start)/(patch.end-patch.start)*patch.rows),0,patch.rows-1);
                for(int col=0;col<patch.cols-1;col++)
                {
                    int a=row*patch.cols+col,b=a+patch.cols;
                    if(TriangleHeight(patch.vertices[a],patch.vertices[b],patch.vertices[a+1],x,z,out height,out normal)||
                       TriangleHeight(patch.vertices[a+1],patch.vertices[b],patch.vertices[b+1],x,z,out height,out normal))return true;
                }
            }
            height=BankHeight(x,z);return true;
        }
        static bool TriangleHeight(Vector3 a,Vector3 b,Vector3 c,float x,float z,out float height,out Vector3 normal)
        {
            height=0;normal=Vector3.up;
            float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
            if(Mathf.Abs(denominator)<1e-8f)return false;
            float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/denominator;
            float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/denominator;
            if(u<-.0001f||v<-.0001f||u+v>1.0001f)return false;
            height=u*a.y+v*b.y+(1-u-v)*c.y;normal=Vector3.Cross(b-a,c-a).normalized;return true;
        }

        static void Bank(Transform root,float start,float end,string id,Material material)
        {
            int rows=Mathf.CeilToInt((end-start)/.65f);
            const int cols=65;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int row=0;row<=rows;row++)
            {
                float z=Mathf.Lerp(start,end,row/(float)rows);
                Extents(z,out float left,out float right);
                for(int col=0;col<cols;col++)
                {
                    float x=col<=28?Mathf.Lerp(left,115.4f,col/28f):col<=36?Mathf.Lerp(115.4f,120.6f,(col-28)/8f):Mathf.Lerp(120.6f,right,(col-36)/28f);
                    var p=new Vector3(x,BankHeight(x,z),z);
                    vertices.Add(p);uv.Add(WorldUV(p));
                    if(row==rows||col==cols-1)continue;
                    int a=row*cols+col;triangles.AddRange(new[]{a,a+cols,a+1,a+1,a+cols,a+cols+1});
                }
            }
            bankPatches.Add(new BankPatch{start=start,end=end,rows=rows,cols=cols,vertices=vertices});
            MeshPart("Continuous "+id+" railway earth bank",root,vertices,uv,triangles,material,true);
        }
        static Vector2 WorldUV(Vector3 p)=>new Vector2((p.x-101.5f)/33f,(p.z+620)/1240f);

        static void HorizonAprons(Transform root)
        {
            if(!terrain)return;
            var material=GroundMaterial("DistantGround");
            foreach(int sign in new[]{-1,1})
            {
                float edge=sign<0?terrain.transform.position.z:terrain.transform.position.z+terrain.terrainData.size.z;
                var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
                const int cols=25,rows=100;
                for(int iz=0;iz<=rows;iz++)for(int ix=0;ix<=cols;ix++)
                {
                    // Starts on the terrain boundary, with NO overlapping
                    // coplanar strip. Both sides share the exact edge height.
                    float x=Mathf.Lerp(44,192,ix/(float)cols),z=Mathf.Lerp(edge,sign*620,iz/(float)rows);
                    var p=new Vector3(x,Ground(x,z),z);v.Add(p);uv.Add(WorldUV(p));
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
            MeshPart("Compacted sheltered sleeping court",root,v,uv,t,material,true);
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
            var material=GroundMaterial("WestRegrowth");
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
                    float y=Ground(x,z)-.042f,scale=.70f+(float)random.NextDouble()*.63f;
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

        static Material GroundMaterial(string id="LivingEarth")
        {
            // Actual coloured photography is blended in the stock HDRP ground
            // shader. No bright greyscale pebble overlay or stretched 1240 m
            // photographic atlas is involved. UV1 remains full precision in
            // Windows builds; UV0 is only a harmless blend-mask coordinate.
            var material=Material(Prefix+id,null,Color.white,.06f);
            material.shader=Shader.Find("HDRP/LayeredLit");
            material.SetFloat("_LayerCount",3);
            material.SetFloat("_VertexColorMode",1);
            material.SetFloat("_UseHeightBasedBlend",0);
            material.SetFloat("_UseMainLayerInfluence",0);
            material.SetFloat("_UVBlendMask",0);
            material.SetVector("_UVMappingMaskBlendMask",new Vector4(1,0,0,0));
            material.SetTexture("_LayerMaskMap",Texture2D.whiteTexture);
            string folder="Assets/ZeroRise/Art/SurfacesV3/";
            string[] surfaces={"mud","grass","mud"};
            float[] tiles={1.65f,1.17f,2.79f};
            var tints=new[]{new Color(.86f,.84f,.80f),new Color(.86f,.88f,.77f),new Color(1,.97f,.89f)};
            for(int layer=0;layer<3;layer++)
            {
                string suffix=layer.ToString();
                material.SetColor("_BaseColor"+suffix,tints[layer]);
                material.SetTexture("_BaseColorMap"+suffix,AssetDatabase.LoadAssetAtPath<Texture2D>(folder+surfaces[layer]+".jpg"));
                material.SetTextureScale("_BaseColorMap"+suffix,Vector2.one/tiles[layer]);
                material.SetTextureOffset("_BaseColorMap"+suffix,new Vector2(layer*.373f,layer*.619f));
                material.SetFloat("_UVBase"+suffix,1);
                material.SetVector("_UVMappingMask"+suffix,new Vector4(0,1,0,0));
                material.SetFloat("_NormalMapSpace"+suffix,0);
                material.SetTexture("_NormalMap"+suffix,AssetDatabase.LoadAssetAtPath<Texture2D>(folder+surfaces[layer]+"-height.png"));
                material.SetFloat("_NormalScale"+suffix,.19f);
                material.SetTexture("_MaskMap"+suffix,null);
                material.SetTexture("_DetailMap"+suffix,null);
                material.SetFloat("_Smoothness"+suffix,.055f);
                material.SetFloat("_Metallic"+suffix,0);
            }
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);return material;
        }

        static Color CoverWeights(float x,float z)
        {
            // Warp the field before sampling: organic grass islands rather than
            // regular longitudinal bands or identical round texture stamps.
            float warp=(Mathf.PerlinNoise(x*.047f+91,z*.043f+17)-.5f)*3.3f;
            float broad=Mathf.PerlinNoise((x+warp)*.19f+39,z*.139f+72);
            float fine=Mathf.PerlinNoise(x*.59f+4,z*.47f+19);
            float grass=Smooth(.37f,.66f,broad*.80f+fine*.20f)*.94f;
            float camp=1-Smooth(0,1.4f,RectDistance(x,z,112.7f,121.2f,66,72.4f));
            float path=1-Smooth(.35f,1.05f,SegmentDistance(new Vector2(x,z),new Vector2(114,69.5f),new Vector2(110,73.5f)));
            grass*=1-Mathf.Max(camp,path)*.97f;
            if(x>114.2f&&x<121.8f&&(z<63||z>97))grass*=.55f;
            float dry=Smooth(.34f,.67f,Mathf.PerlinNoise(x*.23f+53,z*.171f+17))*.66f*(1-grass);
            return new Color(grass,dry,0,1);
        }

        static void BlendTerrainFeet()
        {
            if(!terrain)return;
            var data=terrain.terrainData;int resolution=data.alphamapResolution;
            if(data.alphamapLayers<3)return;
            Vector3 origin=terrain.transform.position;
            int x0=Mathf.Clamp(Mathf.FloorToInt((99-origin.x)/data.size.x*resolution),0,resolution-1);
            int x1=Mathf.Clamp(Mathf.CeilToInt((137-origin.x)/data.size.x*resolution),0,resolution-1);
            var alpha=data.GetAlphamaps(x0,0,x1-x0+1,resolution);
            for(int row=0;row<resolution;row++)for(int col=0;col<alpha.GetLength(1);col++)
            {
                float x=origin.x+(x0+col+.5f)*data.size.x/resolution;
                float z=origin.z+(row+.5f)*data.size.z/resolution;
                if(z>51&&z<109)continue;
                Extents(z,out float left,out float right);
                float distance=Mathf.Min(Mathf.Abs(x-left),Mathf.Abs(x-right));
                float blend=1-Smooth(.30f,2.35f,distance);
                // The bus shelter and the road pavement retain their materials.
                if(x<107.3f&&z>0&&z<34)blend*=Smooth(105.8f,107.3f,x);
                if(blend<=0)continue;
                var cover=CoverWeights(x,z);
                for(int layer=0;layer<data.alphamapLayers;layer++)
                {
                    float target=layer==1?cover.r:layer==2?1-cover.r:0;
                    alpha[row,col,layer]=Mathf.Lerp(alpha[row,col,layer],target,blend);
                }
            }
            data.SetAlphamaps(x0,0,alpha);EditorUtility.SetDirty(data);
        }
        static void BankVegetation(Transform root)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/DistrictFlora.mat");
            if(!material)return;
            var sources=new Mesh[4,3];
            for(int kind=0;kind<4;kind++)for(int i=0;i<3;i++)
                sources[kind,i]=AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/UrbanFlora_"+kind+"_"+i+".asset");
            if(!sources[0,0])return;
            var random=new System.Random(430291);
            float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            int plantedCount=0;
            for(int section=-20;section<=19;section++)
            {
                var parts=new List<CombineInstance>();var heights=new List<Vector2>();var roots=new List<Vector2>();
                int attempts=section>=-5&&section<=5?1900:330;
                for(int i=0;i<attempts;i++)
                {
                    float z=section*32+Range(0,32);
                    if(z< -620||z>620||(z>62.75f&&z<97.25f))continue;
                    Extents(z,out float left,out float right);
                    float x=Range(left+.3f,right-.3f);
                    if(x>114.5f&&x<121.5f)continue;
                    float grass=CoverWeights(x,z).r;
                    if(Range(0,1)>.04f+grass*.94f)continue;
                    if(!TryBankSurface(x,z,out float surface,out Vector3 normal)||normal.y<.3f)continue;
                    float terrainHeight=Ground(x,z);
                    if(surface<terrainHeight+.06f)continue;
                    float choice=Range(0,1);
                    int kind=choice<.82f?0:choice<.90f?1:choice<.98f?2:3;
                    var mesh=sources[kind,random.Next(3)];if(!mesh)continue;
                    float scale=Range(.78f,1.34f);
                    // Roots use the actual rendered/collision triangle, not a
                    // different analytic/bilinear surface. Tilt the root plane
                    // into the slope and bury it 4.5 cm, covering blade spread.
                    var planted=new Vector3(x,surface-.045f,z);
                    Quaternion rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Lerp(Vector3.up,normal,.72f).normalized)*Quaternion.Euler(0,Range(0,360),0);
                    parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(planted,rotation,Vector3.one*scale)});
                    foreach(var vertex in mesh.vertices)
                    {
                        heights.Add(new Vector2(planted.y,Mathf.Clamp01(vertex.y/mesh.bounds.max.y)));
                        roots.Add(new Vector2(x,z));
                    }
                    plantedCount++;
                }
                PlantChunk(root,"Rooted embankment grass "+section,parts,heights,roots,material);
            }
            Debug.Log("Overpass earthworks: "+plantedCount+" slope-conforming grass/sedge/rosette clumps; every root anchored to its collision triangle.");
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
            mesh.SetColors(vertices.ConvertAll(p=>CoverWeights(p.x,p.z)));
            mesh.SetTriangles(clean,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var obj=Group(name,parent,Vector3.zero);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=name.Contains("earth bank")?ShadowCastingMode.On:ShadowCastingMode.Off;
            if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
