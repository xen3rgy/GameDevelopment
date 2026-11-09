using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
using static ZeroRise.WestbruchLakeAccess;

namespace ZeroRise.Editor
{
    internal static class WestbruchLakeAccessBuilder
    {
        static Transform root;
        static Collider[] supports;
        static Material timber,iron,concrete;
        internal static void Build(Transform regional,Material landscape)
        {
            root=Group("BV8_Waldsee access and arrival",regional,Vector3.zero).transform;
            Physics.SyncTransforms();
            supports=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>
                c.name.StartsWith("BV3_Ground ")||c.name.StartsWith("BV2_Surface ")||c.name.StartsWith("BV3_Court paving")).ToArray();
            timber=BahnhofDetailKit.Load("UrbanLife_AgedTimber");iron=BahnhofDetailKit.Load("BV5_OxidizedIron");
            concrete=Material("BV8_WeatheredLandingConcrete","concrete",new Color(.47f,.46f,.41f),.08f);
            BahnhofStreetscape.Import("gravelly_sand");
            Surface(landscape);
            // Modest trailhead markers attached to timber posts, never loose labels.
            Sign(new Vector2(275,309),90,"WALDSEE  →\nUferweg",1.6f);
            Sign(new Vector2(329,179.5f),90,"WALDSEE  →\nKiesweg · langsam fahren",2.1f);
            Sign(new Vector2(557,332),25,"WALDSEE\nParken · Ufer zu Fuß",1.8f);
            Sign(new Vector2(579,356),70,"ZUM UFER  →",1.4f);
            // Parking faces the woodland. The turning area remains free.
            for(int i=0;i<4;i++)
            {
                float x=558+i*3.3f;var p=At(x,355);
                Box("BV8_Parking timber wheel stop",root,p+Vector3.up*.12f,new Vector3(2.25f,.24f,.24f),timber,true,1.5f);
            }
            // A pedestrian opening at the exit keeps cars back from the water.
            foreach(var p in new[]{new Vector2(578,353),new Vector2(580,358.5f)})
            {var at=At(p.x,p.y);Cylinder("BV8_Ufer timber bollard",root,at+Vector3.up*.45f,.14f,.9f,timber,true);}
            Bench(new Vector2(577,342),75);
            Bench(new Vector2(601,363),80);
            Landing();
            Verges();
            Physics.SyncTransforms();
            Validate();
        }
        static Vector3 At(float x,float z)
        {
            var ray=new Ray(new Vector3(x,1000,z),Vector3.down);bool found=false;RaycastHit best=default;
            foreach(var c in supports)if(c&&c.Raycast(ray,out var hit,1400)&&(!found||hit.point.y>best.point.y)){found=true;best=hit;}
            if(!found)throw new InvalidOperationException("No lake approach ground at "+x+", "+z);
            return best.point;
        }
        static float Coverage(Vector2 p)
        {
            float d=Distance(Road,p,out _),f=Distance(Footpath,p,out _),s=Distance(ShorePath,p,out _);
            float edgeNoise=(WestbruchLandscapeProfile.Noise(p.x,p.y,.34f)-.5f)*.3f;
            float road=1-WestbruchLandscapeProfile.Fade(2.35f+edgeNoise,3.4f+edgeNoise,d);
            float foot=1-WestbruchLandscapeProfile.Fade(.85f+edgeNoise,1.9f+edgeNoise,f);
            float shore=1-WestbruchLandscapeProfile.Fade(.65f+edgeNoise,1.6f+edgeNoise,s);
            float park=1-WestbruchLandscapeProfile.Fade(.83f,1.12f+edgeNoise*.025f,ParkingRadius(p));
            float bay=1-WestbruchLandscapeProfile.Fade(.65f,1.08f+edgeNoise*.05f,BayRadius(p));
            return Mathf.Max(road,foot,shore,park,bay);
        }
        static void Surface(Material landscape)
        {
            // A single welded surface joins road, paths, parking and beach. No
            // intersecting strips at junctions; a metre-wide irregular soil blend.
            var mat=Material("BV8_CompactedGravelAndForest",null,Color.white,.1f);
            mat.CopyPropertiesFromMaterial(landscape);mat.shader=Shader.Find("HDRP/LayeredLit");
            string art="Assets/ZeroRise/Art/Streetscape2K/";
            foreach(string prop in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})
            {
                string file=prop=="_BaseColorMap"?"gravelly_sand_diff.jpg":prop=="_NormalMap"?"gravelly_sand_nor_gl.jpg":"gravelly_sand_mask.png";
                mat.SetTexture(prop+"3",AssetDatabase.LoadAssetAtPath<Texture2D>(art+file));
            }
            mat.SetColor("_BaseColor3",new Color(.78f,.76f,.70f));mat.SetTextureScale("_BaseColorMap3",Vector2.one/2.5f);
            mat.SetFloat("_NormalScale3",.46f);mat.SetFloat("_SmoothnessRemapMax3",.12f);
            mat.SetFloat("_AlphaCutoffEnable",1);mat.SetFloat("_AlphaCutoff",.5f);
            HDMaterial.ValidateMaterial(mat);mat.shader=Shader.Find("ZeroRise/LakeAccessLayeredLit");EditorUtility.SetDirty(mat);
            // Fine sampling keeps the curved shoulders smooth at walking height.
            const float step=.25f;const int nx=1560,nz=1122;var v=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var tris=new List<int>();var lookup=new Dictionary<int,int>();
            int Vertex(int ix,int iz)
            {
                int key=iz*(nx+1)+ix;if(lookup.TryGetValue(key,out int known))return known;
                float x=268+ix*step,z=163+iz*step;var p=new Vector2(x,z);float cover=Coverage(p);var at=At(x,z);
                // Thin terrain-conforming gravel; its collider matches the visible
                // triangles and edge colours blend into the surrounding soil.
                at.y+=.032f;
                lookup[key]=v.Count;v.Add(at);uv.Add(p);colors.Add(new Color(0,0,cover,1));return v.Count-1;
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                var p=new Vector2(268+(x+.5f)*step,163+(z+.5f)*step);if(Coverage(p)<.015f)continue;
                // The depot asphalt ends at x=325, on a grid boundary. Keep its
                // straight construction joint instead of feathering soil over it.
                if(p.x<325f&&p.y<200f)continue;
                int a=Vertex(x,z),b=Vertex(x,z+1),c=Vertex(x+1,z+1),d=Vertex(x+1,z);tris.AddRange(new[]{a,b,c,a,c,d});
            }
            var mesh=new Mesh{name="BV8_JoinedLakeAccess",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            // Identical base soil, rock and macro weights as the adjoining terrain;
            // only the fourth layer is replaced by gravel in this lowland material.
            WestbruchRegionalLandscape.Paint(mesh,root);var baseColors=mesh.colors;
            for(int i=0;i<baseColors.Length;i++)baseColors[i].b=colors[i].b;
            mesh.colors=baseColors;
            string path=Generated+"/Meshes/BV8_JoinedLakeAccess.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}else{saved=mesh;AssetDatabase.CreateAsset(saved,path);}
            var go=Group("BV8_Continuous gravel access",root,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=saved;go.AddComponent<MeshRenderer>().sharedMaterial=mat;go.AddComponent<MeshCollider>().sharedMesh=saved;
        }
        static void Sign(Vector2 pos,float yaw,string text,float width)
        {
            var p=At(pos.x,pos.y);var sign=Group("BV8_Trailhead",root,p,yaw).transform;
            Box("BV8_Sign timber post",sign,new Vector3(0,1.1f,0),new Vector3(.12f,2.2f,.12f),timber);
            BahnhofDetailKit.Plate(sign,text,new Vector3(0,1.78f,-.09f),0,width,.48f,timber,new Color(.9f,.88f,.77f));
        }
        static void Bench(Vector2 pos,float yaw)
        {
            var p=At(pos.x,pos.y);var parent=Group("BV8_Lakeside timber bench",root,p,yaw).transform;
            // Each foot independently reaches the real ground below the seat.
            foreach(float x in new[]{-.73f,.73f})foreach(float z in new[]{-.24f,.24f})
            {
                var world=parent.TransformPoint(new Vector3(x,0,z));float foot=At(world.x,world.z).y-p.y-.06f;
                Box("BV8_Bench foot",parent,new Vector3(x,(foot+.44f)*.5f,z),new Vector3(.10f,.44f-foot,.10f),iron);
            }
            foreach(float z in new[]{-.22f,0,.22f})Box("BV8_Bench seat slat",parent,new Vector3(0,.46f,z),new Vector3(1.95f,.07f,.19f),timber);
            foreach(float x in new[]{-.73f,.73f})Box("BV8_Bench back support",parent,new Vector3(x,.72f,.28f),new Vector3(.08f,.6f,.08f),iron);
            foreach(float y in new[]{.73f,.94f})Box("BV8_Bench back slat",parent,new Vector3(0,y,.3f),new Vector3(1.95f,.16f,.06f),timber);
        }
        static void Verges()
        {
            var rng=new System.Random(71126);float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
            var batches=new Dictionary<Vector2Int,List<CombineInstance>>();
            foreach(var route in new[]{Road,Footpath,ShorePath})for(int i=6;i<route.Length-1;i+=7)
            {
                if(rng.NextDouble()<.3)continue;
                var direction=(route[i+1]-route[i]).normalized;var side=new Vector3(-direction.z,0,direction.x)*(rng.NextDouble()<.5?-1:1);
                float offset=route==Road?Rand(4.2f,6):Rand(2.3f,4.4f);var centre=route[i]+side*offset;
                for(int j=0;j<12;j++)
                {
                    float angle=Rand(0,Mathf.PI*2),radius=Rand(.1f,1.45f);float x=centre.x+Mathf.Cos(angle)*radius,z=centre.z+Mathf.Sin(angle)*radius;
                    if(Cleared(new Vector2(x,z),.25f))continue;var at=At(x,z);if(at.y<14.4f&&WestbruchLandscapeProfile.LakeRadius(x,z)<1.08f)continue;
                    int kind=j==0?4:0;var mesh=WestbruchFloraMeshes.Get(kind,j%3);float scale=kind==4?Rand(.35f,.65f):Rand(.65f,1.1f);
                    var cell=new Vector2Int(Mathf.FloorToInt(x/24),Mathf.FloorToInt(z/24));if(!batches.ContainsKey(cell))batches[cell]=new List<CombineInstance>();
                    batches[cell].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(at-Vector3.up*.025f,Quaternion.Euler(0,Rand(0,360),0),Vector3.one*scale)});
                }
            }
            foreach(var entry in batches)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);
                var heights=new List<Vector2>();var roots=new List<Vector2>();
                foreach(var item in entry.Value)foreach(var v in item.mesh.vertices)
                {var at=item.transform.MultiplyPoint3x4(Vector3.zero);heights.Add(new Vector2(at.y,Mathf.Clamp01(v.y)));roots.Add(new Vector2(at.x,at.z));}
                mesh.SetUVs(2,heights);mesh.SetUVs(3,roots);
                mesh.name="BV8_Verge_"+entry.Key.x+"_"+entry.Key.y;
                string path=Generated+"/Meshes/BV8_Verge_"+entry.Key.x+"_"+entry.Key.y+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}else{saved=mesh;AssetDatabase.CreateAsset(saved,path);}
                var go=Group("BV8_Grounded verge pockets",root,Vector3.zero);go.AddComponent<MeshFilter>().sharedMesh=saved;go.AddComponent<MeshRenderer>().sharedMaterial=BahnhofDetailKit.Load("DistrictFlora");
            }
        }
        static void Landing()
        {
            // Remnant of the former gravel works: short segmented retaining edge,
            // founded below ground rather than a suspended waterfront platform.
            for(int i=0;i<5;i++)
            {
                float z=403+i*1.55f,x=598+(z-403)*.8f;var at=At(x,z);float top=Mathf.Max(15.75f,at.y+.10f);
                var block=Box("BV8_Old loading edge",root,new Vector3(x,top-.7f,z),new Vector3(1.8f,1.4f,1.65f),concrete,true,2);
                block.transform.localRotation=Quaternion.Euler(0,38,0);
                if(i==1||i==4)Cylinder("BV8_Old mooring post",root,new Vector3(x,top+.15f,z),.09f,.3f,iron,true);
            }
        }
        static void Validate()
        {
            float maxGrade=0;int unsupported=0;
            foreach(var route in new[]{Road,Footpath,ShorePath})for(int i=1;i<route.Length;i++)
            {
                var a=At(route[i-1].x,route[i-1].z);var b=At(route[i].x,route[i].z);
                float grade=Mathf.Abs(a.y-b.y)/Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));maxGrade=Mathf.Max(maxGrade,grade);
                if(grade>.30f){unsupported++;Debug.LogWarning("BV8 excessive slope from "+a+" to "+b+" grade "+grade);}
            }
            if(unsupported>0)throw new InvalidOperationException("Lake access has excessive ground slope: "+unsupported+" maximum "+maxGrade);
            Debug.Log("BV8 lake access: continuous graded routes; maximum sampled grade "+maxGrade);
        }
    }
}
