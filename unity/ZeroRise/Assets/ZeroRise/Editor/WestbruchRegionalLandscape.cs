using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
using static ZeroRise.WestbruchLandscapeProfile;

namespace ZeroRise.Editor
{
    internal static class WestbruchRegionalLandscape
    {
        const string Art = "Assets/ZeroRise/Art/Streetscape2K/";
        static Transform root;
        static System.Random random;
        static float R(float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        static Material ground, rock, bark, needles, leaves;
        static readonly List<MeshCollider> land = new List<MeshCollider>();

        internal static void Apply(Transform district)
        {
            var old = district.Find("BV6_Regional landscape");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            root = Group("BV6_Regional landscape",district,Vector3.zero).transform;
            random = new System.Random(6112026); land.Clear();
            foreach (string id in new[]{"rock_face_03","snow_02"}) BahnhofStreetscape.Import(id);
            ground = BiomeMaterial();
            rock = BahnhofStreetscape.Surface("Regional weathered rock","rock_face_03",new Color(.28f,.34f,.38f),.85f);
            bark = AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_Bark.mat");
            leaves = AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_Leaves.mat");
            needles = Material("BV6_SpruceNeedles",null,new Color(.13f,.22f,.12f),.08f);
            bark.enableInstancing=true;leaves.enableInstancing=true;needles.enableInstancing=true;
            needles.SetFloat("_DoubleSidedEnable",1); HDMaterial.ValidateMaterial(needles);
            foreach (var f in district.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("BV3_Ground ")))
            {
                var mesh = f.sharedMesh; Paint(mesh,f.transform); f.GetComponent<MeshRenderer>().sharedMaterial = ground;
                land.Add(f.GetComponent<MeshCollider>());
            }
            // Distant continuation closes the skyline behind the explorable foothills.
            Horizon("East",1800,10000,-1800,10000);
            Horizon("North",-1800,1800,1800,10000);
            Horizon("West",-10000,-1800,-1800,10000);
            Horizon("South",-10000,10000,-10000,-1800);
            Physics.SyncTransforms();
            Lake(); Woodland(); ShoreAndMeadows(); LakesideTrail();
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.farClipPlane = Mathf.Max(camera.farClipPlane,8000);
            Debug.Log("BV6 regional landscape complete: continuous relief, basin, four biomes, grounded woodland, shoreline and distant mountain horizon.");
        }
        static Material BiomeMaterial()
        {
            var mat = Material("BV6_AltitudeAndSlopeBiomes",null,Color.white,.12f);
            mat.shader = Shader.Find("HDRP/LayeredLit"); mat.SetFloat("_LayerCount",4); mat.SetFloat("_VertexColorMode",1); mat.SetFloat("_UseHeightBasedBlend",0);
            mat.SetTexture("_LayerMaskMap",Texture2D.whiteTexture);
            var ids = new[]{"forest_ground_04","leafy_grass","rock_face_03","snow_02"};
            var tints = new[]{new Color(.39f,.40f,.34f),new Color(.32f,.46f,.27f),new Color(.28f,.34f,.38f),new Color(.76f,.81f,.86f)};
            for(int i=0;i<4;i++)
            {
                mat.SetColor("_BaseColor"+i,tints[i]);
                mat.SetTexture("_BaseColorMap"+i,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+ids[i]+"_diff.jpg"));
                mat.SetTexture("_NormalMap"+i,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+ids[i]+"_nor_gl.jpg"));
                mat.SetTexture("_MaskMap"+i,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+ids[i]+"_mask.png"));
                // World triplanar coordinates avoid stretched rock on cliff faces.
                mat.SetFloat("_UVBase"+i,5);mat.SetFloat("_TexWorldScale"+i,1);
                mat.SetTextureScale("_BaseColorMap"+i,Vector2.one/(i==2?6.5f:i==3?3.1f:2.8f));
                mat.SetFloat("_NormalScale"+i,i==2?.8f:.42f);mat.SetFloat("_SmoothnessRemapMin"+i,.02f);mat.SetFloat("_SmoothnessRemapMax"+i,i==3?.36f:.19f);
            }
            HDMaterial.ValidateMaterial(mat);mat.shader=Shader.Find("ZeroRise/LandscapeLayeredLit");EditorUtility.SetDirty(mat);return mat;
        }
        static void Paint(Mesh mesh,Transform transform)
        {
            var v=mesh.vertices;var normals=mesh.normals;var colors=new Color[v.Length];
            for(int i=0;i<v.Length;i++)
            {
                Vector3 p=transform.TransformPoint(v[i]),n=transform.TransformDirection(normals[i]);
                float variation=Noise(p.x,p.z,.033f)*.6f+Noise(p.x,p.z,.16f,8)*.4f;
                float forest=Fade(.42f,.67f,Noise(p.x,p.z,.008f,12));
                float grass=Mathf.Lerp(.38f,.98f,Fade(.27f,.69f,variation))*(1-forest*.50f);
                float shore=1-Fade(1.0f,1.18f,LakeRadius(p.x,p.z));
                float stone=Mathf.Max(Fade(.20f,.56f,1-n.y),Fade(100,235,p.y));
                stone=Mathf.Max(stone,shore*.73f);
                float distance=Mathf.Max(p.x-300,-200-p.x,p.z-364,-240-p.z);
                float wilderness=Fade(0,160,distance);
                var oldSoil=BahnhofGroundSurface.LayersAt(p.x,p.z);
                colors[i]=new Color(Mathf.Lerp(oldSoil.grass*.9f,grass,wilderness),stone*wilderness,SnowCover(p,n),1);
            }
            mesh.colors=colors;EditorUtility.SetDirty(mesh);
        }
        static Mesh Store(Mesh mesh,string id)
        {
            mesh.name="BV6_"+id;string path=Generated+"/Meshes/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material[] materials,Vector3 position)
        {
            var go=Group(name,parent,position);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;return go;
        }
        static void Horizon(string id,float x0,float x1,float z0,float z1)
        {
            // The seam itself is sampled from the exact saved ground collider;
            // interior vertices use the common height profile.
            float[] Axis(float a,float b,float step) => Enumerable.Range(0,Mathf.CeilToInt((b-a)/step)+1).Select(i=>Mathf.Lerp(a,b,i/(float)Mathf.CeilToInt((b-a)/step))).ToArray();
            var xs=new SortedSet<float>(Axis(x0,x1,75));var zs=new SortedSet<float>(Axis(z0,z1,75));
            foreach(var c in land.ToArray())foreach(var vertex in c.sharedMesh.vertices)
            {
                var p=c.transform.TransformPoint(vertex);
                if(id=="East"&&Mathf.Abs(p.x-x0)<.01f&&p.z>=z0&&p.z<=z1)zs.Add(p.z);
                if(id=="North"&&Mathf.Abs(p.z-z0)<.01f&&p.x>=x0&&p.x<=x1)xs.Add(p.x);
                if(id=="West"&&Mathf.Abs(p.x-x1)<.01f&&p.z>=z0&&p.z<=z1)zs.Add(p.z);
                if(id=="South"&&Mathf.Abs(p.z-z1)<.01f&&p.x>=x0&&p.x<=x1)xs.Add(p.x);
            }
            var xx=xs.ToArray();var zz=zs.ToArray();int nx=xx.Length-1,nz=zz.Length-1;
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                float x=xx[i],z=zz[j];float y=BahnhofLandscapeFinish.ContextHeight(x,z);
                y=Mathf.Lerp(8+Noise(x,z,.0015f)*38+Noise(x,z,.006f,8)*9,y,Fade(200,600,x));
                if(id=="East"&&i==0&&z<=1800&&Ground(x-.001f,z,out var edge))y=edge.point.y;
                if(id=="North"&&j==0&&Ground(x,z-.001f,out edge))y=edge.point.y;
                if(id=="West"&&i==nx&&Ground(x+.001f,z,out edge))y=edge.point.y;
                if(id=="South"&&j==nz&&Ground(x,z+.001f,out edge))y=edge.point.y;
                v.Add(new Vector3(x,y,z));uv.Add(new Vector2(x,z));
                if(i==nx||j==nz)continue;int a=j*(nx+1)+i;t.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});
            }
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            Paint(mesh,root);mesh=Store(mesh,"Horizon"+id);
            var go=MeshObject("BV6_Continuous horizon "+id,root,mesh,new[]{ground},Vector3.zero);land.Add(go.AddComponent<MeshCollider>());land.Last().sharedMesh=mesh;
        }
        static bool Ground(float x,float z,out RaycastHit result)
        {
            var ray=new Ray(new Vector3(x,1200,z),Vector3.down);result=default;bool found=false;
            foreach(var collider in land)if(collider&&collider.Raycast(ray,out var hit,1400)&&(!found||hit.point.y>result.point.y)){result=hit;found=true;}
            return found;
        }
        static void Lake()
        {
            var go=Group("Westbruch Waldsee – sheltered freshwater lake",root,new Vector3(LakeCentre.x,LakeLevel,LakeCentre.y));go.transform.localScale=new Vector3(340,1,260);
            var water=go.AddComponent<WaterSurface>();water.surfaceType=WaterSurfaceType.River;water.geometryType=WaterGeometryType.InstancedQuads;
            water.repetitionSize=80;water.largeWindSpeed=8;water.largeChaos=.6f;water.largeBand0Multiplier=.12f;water.largeBand1Multiplier=.04f;water.largeCurrentSpeedValue=0;
            water.ripples=true;water.ripplesMotionMode=WaterPropertyOverrideMode.Custom;water.ripplesWindSpeed=4;water.ripplesChaos=.6f;water.ripplesCurrentSpeedValue=0;
            water.refractionColor=new Color(.29f,.42f,.37f).linear;water.scatteringColor=new Color(.12f,.24f,.21f).linear;water.absorptionDistance=3.8f;water.maxRefractionDistance=1;
            water.ambientScattering=.22f;water.heightScattering=.02f;water.displacementScattering=.06f;water.startSmoothness=.96f;water.endSmoothness=.85f;
            water.tessellation=true;water.maxTessellationFactor=3;water.tessellationFactorFadeStart=35;water.tessellationFactorFadeRange=150;water.foam=true;water.simulationFoamAmount=.015f;water.caustics=false;water.underWater=false;
            go.AddComponent<RegionalLakeWeather>();
        }
        static void Woodland()
        {
            var conifers=new Mesh[3,3];var broadleaf=new Mesh[3,3];
            for(int variant=0;variant<3;variant++)for(int lod=0;lod<3;lod++)
            {
                conifers[variant,lod]=Conifer(variant,lod);
                broadleaf[variant,lod]=lod<2?AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/BV5_StreetTree_"+variant+"_"+lod+".asset"):DistantCrown(broadleaf[variant,1],variant);
            }
            int count=0;
            // Jittered cells and broad density fields create groves and clearings.
            for(float z=-70;z<1700;z+=12)for(float x=440;x<1750;x+=12)
            {
                float px=x+R(-5,5),pz=z+R(-5,5),density=Noise(px,pz,.008f,12);
                if(density<R(.24f,.59f)||!Ground(px,pz,out var hit)||hit.point.y>238||hit.normal.y<.74f||LakeRadius(px,pz)<1.13f)continue;
                bool spruce=hit.point.y>65||R(0,1)<.62f;int variant=count%3;float scale=R(1.05f,1.7f);
                var go=Group("BV6_Rooted "+(spruce?"spruce ":"broadleaf ")+count++,root,hit.point-Vector3.up*.10f,R(0,360));go.transform.localScale=Vector3.one*scale;
                var rs=new Renderer[3];
                for(int lod=0;lod<3;lod++)
                {
                    var mesh=spruce?conifers[variant,lod]:broadleaf[variant,lod];
                    rs[lod]=MeshObject("Crown LOD "+lod,go.transform,mesh,new[]{bark,spruce?needles:leaves},Vector3.zero).GetComponent<Renderer>();
                    // Shared meshes can be instanced; static batching would duplicate
                    // every high-detail crown in the player data and GPU buffers.
                    rs[lod].gameObject.isStatic=false;
                    if(lod==2)rs[lod].shadowCastingMode=ShadowCastingMode.Off;
                }
                var group=go.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(new[]{new LOD(.09f,new[]{rs[0]}),new LOD(.045f,new[]{rs[1]}),new LOD(.004f,new[]{rs[2]})});group.RecalculateBounds();
                var trunk=go.AddComponent<CapsuleCollider>();trunk.radius=.23f;trunk.height=spruce?10:5;trunk.center=Vector3.up*(trunk.height*.5f-.05f);
            }
            Debug.Log("BV6 grounded woodland trees: "+count);
        }
        static void LakesideTrail()
        {
            var waypoints=new[]{new Vector2(440,200),new Vector2(478,221),new Vector2(511,248),new Vector2(536,270),new Vector2(558,293),new Vector2(578,318),new Vector2(588,347)};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();float distance=0;
            for(int segment=1;segment<waypoints.Length;segment++)
            {
                var a=waypoints[segment-1];var b=waypoints[segment];var direction=(b-a).normalized;var across=new Vector2(-direction.y,direction.x);int count=Mathf.CeilToInt(Vector2.Distance(a,b));
                for(int j=0;j<count;j++)
                {
                    var centre=Vector2.Lerp(a,b,j/(float)count);float width=.68f+Noise(centre.x,centre.y,.3f)*.34f;
                    int n=vertices.Count;
                    foreach(int sign in new[]{-1,1})
                    {
                        var p=centre+across*width*sign;
                        if(Ground(p.x,p.y,out var h))vertices.Add(h.point+Vector3.up*.018f);else vertices.Add(new Vector3(p.x,0,p.y));
                        uv.Add(new Vector2(sign==1?1:0,distance*.45f));
                    }
                    if(n>0)indices.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});distance+=Vector2.Distance(a,b)/count;
                }
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=Store(mesh,"LakeFootpath");
            var earth=BahnhofStreetscape.Surface("Lake footpath","brown_mud_leaves_01",new Color(.50f,.48f,.42f),.4f);
            MeshObject("BV6_Winding woodland footpath",root,mesh,new[]{earth},Vector3.zero);
        }
        static Mesh DistantCrown(Mesh source,int variant)
        {
            var input=source.vertices;var tex=source.uv;var color=source.colors;
            var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var trunk=new List<int>();var foliage=new List<int>();
            // Retain the main trunk only; twigs are subpixel at this distance.
            foreach(int index in source.GetTriangles(0).Take(36)){trunk.Add(v.Count);v.Add(input[index]);uv.Add(tex[index]);colors.Add(Color.white);}
            var triangles=source.GetTriangles(1);
            for(int i=0;i+5<triangles.Length;i+=24)
            {
                Vector3 centre=(input[triangles[i]]+input[triangles[i+2]])*.5f;
                for(int j=0;j<6;j++){int index=triangles[i+j];foliage.Add(v.Count);v.Add(centre+(input[index]-centre)*1.85f);uv.Add(tex[index]);colors.Add(color.Length==input.Length?color[index]:Color.white);}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.subMeshCount=2;mesh.SetTriangles(trunk,0);mesh.SetTriangles(foliage,1);mesh.RecalculateNormals();mesh.RecalculateTangents();return Store(mesh,"FarBroadleaf_"+variant);
        }
        static Mesh Conifer(int variant,int lod)
        {
            var saved=random;random=new System.Random(81273+variant*173);
            float height=12+variant*1.8f;var v=new List<Vector3>();var uv=new List<Vector2>();var trunk=new List<int>();var foliage=new List<int>();
            void Tri(Vector3 a,Vector3 b,Vector3 c,List<int> indices)
            {int n=v.Count;v.AddRange(new[]{a,b,c});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one});indices.AddRange(new[]{n,n+1,n+2});}
            int sides=lod==0?9:6;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;var p=new Vector3(Mathf.Cos(a)*.22f,-.2f,Mathf.Sin(a)*.22f);var q=new Vector3(Mathf.Cos(b)*.22f,-.2f,Mathf.Sin(b)*.22f);
                Tri(p,new Vector3(.09f,height,0),q,trunk);
            }
            if(lod==2)
            {
                // Subpixel needles need an opaque crown envelope at long range;
                // otherwise mip/coverage loss turns a forest into bare trunks.
                for(int tier=0;tier<7;tier++)
                {
                    float y=Mathf.Lerp(1.9f,height-1.2f,tier/7f),r=3.5f*(1-y/height);
                    var tip=new Vector3(.12f,y+2.5f,0);
                    for(int sector=0;sector<9;sector++)
                    {
                        float a=sector*Mathf.PI*2/9+tier*.7f,b=(sector+1)*Mathf.PI*2/9+tier*.7f;
                        float ra=r*(sector%2==0?1:.78f),rb=r*(sector%2==0?.78f:1);
                        Tri(new Vector3(Mathf.Cos(a)*ra,y,Mathf.Sin(a)*ra),tip,new Vector3(Mathf.Cos(b)*rb,y,Mathf.Sin(b)*rb),foliage);
                    }
                }
            }
            // Individual drooping branch sprays and crossed narrow needles in the near views.
            int tiers=lod==0?15:lod==1?10:7;
            for(int tier=0;tier<tiers;tier++)
            {
                float y=Mathf.Lerp(1.6f,height-.3f,tier/(float)tiers);float radius=3.5f*(1-y/height)*R(.8f,1.15f);
                int branches=lod==0?8:lod==1?7:5;
                for(int branch=0;branch<branches;branch++)
                {
                    float angle=branch*Mathf.PI*2/branches+tier*2.39f;var dir=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var side=Vector3.Cross(dir,Vector3.up);
                    var a=new Vector3(.05f,y,0);var b=a+dir*radius-Vector3.up*.42f;Tri(a-side*.055f,b,a+side*.055f,trunk);
                    int sprays=lod==0?7:lod==1?5:3;
                    for(int k=0;k<sprays;k++)
                    {
                        float f=(k+1)/(float)sprays;var p=Vector3.Lerp(a,b,f);float w=(1-f*.7f)*radius*.31f;
                        for(int s=-1;s<=1;s+=2)
                        {
                            var end=p+side*s*w+dir*.30f+Vector3.up*R(-.12f,.18f);
                            int count=lod==0?5:lod==1?3:1;
                            for(int needle=0;needle<count;needle++)
                            {
                                var stem=Vector3.Lerp(p,end,needle/(float)count);float width=lod==0?.095f:lod==1?.22f:.42f;float length=lod==0?.35f:lod==1?.58f:.90f;
                                Tri(stem-dir*width,stem+dir*length+Vector3.up*.11f,stem+side*s*width,foliage);
                                Tri(stem-Vector3.up*width,stem+dir*length+side*s*.14f,stem+Vector3.up*width,foliage);
                            }
                        }
                    }
                }
            }
            random=saved;var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(trunk,0);mesh.SetTriangles(foliage,1);mesh.RecalculateNormals();mesh.RecalculateTangents();return Store(mesh,"Spruce_"+variant+"_"+lod);
        }
        static Mesh Boulder(int variant)
        {
            int rings=8,sides=13;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=j*Mathf.PI/rings;
                var p=new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b)*.65f,Mathf.Sin(a)*Mathf.Sin(b));
                float distortion=.82f+Noise(p.x*4+variant*7,p.z*4+p.y*9,.7f)*.38f;p*=distortion;
                v.Add(p);uv.Add(new Vector2(a*.45f,b*.45f));
                if(j==rings||i==sides)continue;int n=j*(sides+1)+i;t.AddRange(new[]{n,n+1,n+sides+1,n+1,n+sides+2,n+sides+1});
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();return Store(mesh,"ErodedBoulder_"+variant);
        }
        static void ShoreAndMeadows()
        {
            var rocks=Enumerable.Range(0,4).Select(Boulder).ToArray();var plantMaterial=BahnhofDetailKit.Load("DistrictFlora");
            var batches=new Dictionary<Vector2Int,List<CombineInstance>>();int stones=0,plants=0;
            for(int i=0;i<14000;i++)
            {
                float x=R(448,1020),z=R(135,630);
                if(i<9500)
                {
                    float angle=R(0,Mathf.PI*2),shoreRadius=R(1.03f,1.19f);
                    float shape=1+.10f*Mathf.Sin(angle*3+.8f)+.055f*Mathf.Sin(angle*7);
                    x=730+Mathf.Cos(angle)*130*shoreRadius*shape;z=360+Mathf.Sin(angle)*96*shoreRadius*shape;
                }
                if(!Ground(x,z,out var hit)||hit.normal.y<.82f||hit.point.y<LakeLevel+.18f)continue;
                float radius=LakeRadius(x,z),pocket=Noise(x,z,.045f,18);
                if(radius>1.65f&&pocket<.55f)continue;
                if(i%13==0&&(radius<1.21f||hit.normal.y<.92f)&&Noise(x,z,.035f,38)>.49f)
                {
                    float scale=R(.32f,1.85f);var go=MeshObject("BV6_Ufer rock "+stones++,root,rocks[i%4],new[]{rock},hit.point+Vector3.up*scale*.12f);
                    go.transform.rotation=Quaternion.Euler(R(-20,20),R(0,360),R(-20,20));go.transform.localScale=new Vector3(scale,R(.6f,1)*scale,scale*R(.7f,1.2f));go.AddComponent<MeshCollider>().sharedMesh=rocks[i%4];
                }
                if(radius<.99f||radius>1.45f&&pocket<.60f)continue;
                Mesh plant=radius<1.12f?WestbruchFloraMeshes.Get(1,i%3):AssetDatabase.LoadAssetAtPath<Mesh>(Generated+"/Meshes/BV5_MeadowGrass_"+(i%3)+".asset");
                float size=radius<1.12f?R(.85f,1.4f):R(.8f,1.5f);
                var cell=new Vector2Int(Mathf.FloorToInt(x/24),Mathf.FloorToInt(z/24));if(!batches.ContainsKey(cell))batches[cell]=new List<CombineInstance>();
                batches[cell].Add(new CombineInstance{mesh=plant,transform=Matrix4x4.TRS(hit.point-Vector3.up*.025f,Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,R(0,360),0),Vector3.one*size)});plants++;
            }
            foreach(var entry in batches)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);
                var heights=new List<Vector2>();var roots=new List<Vector2>();
                foreach(var instance in entry.Value)foreach(var v in instance.mesh.vertices){Vector3 p=instance.transform.MultiplyPoint3x4(Vector3.zero);heights.Add(new Vector2(p.y,Mathf.Clamp01(v.y)));roots.Add(new Vector2(p.x,p.z));}
                mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);mesh=Store(mesh,"ShorePlants_"+entry.Key.x+"_"+entry.Key.y);
                var go=MeshObject("BV6_Rooted shore plants",root,mesh,new[]{plantMaterial},Vector3.zero);var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.035f,new[]{go.GetComponent<Renderer>()})});lod.RecalculateBounds();
            }
            Debug.Log($"BV6 shoreline: {stones} embedded rocks, {plants} rooted grass and reed clumps.");
        }
    }
}
