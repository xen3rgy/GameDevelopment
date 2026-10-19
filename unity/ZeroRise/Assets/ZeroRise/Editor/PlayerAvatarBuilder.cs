using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Our exported browser hero, refined and bound once in the Editor. No runtime
    // geometry generation, primitive colliders or downloaded character assets.
    public static class PlayerAvatarBuilder
    {
        public const string Folder=Generated+"/Player";
        [Serializable] class Source { public Bone[] bones;public Surface[] materials;public Part[] parts; }
        [Serializable] class Bone { public string name;public int parent;public float[] p; }
        [Serializable] class Surface { public string name,kind;public float[] color;public float roughness; }
        [Serializable] class Part { public int bone,material;public float[] positions,normals,uv;public int[] indices; }
        static Vector3 Vector(float[] p,int i=0)=>new Vector3(p[i],p[i+1],p[i+2]);

        public static PlayerAvatar Build(ThirdPersonMotor motor)
        {
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Generated+"/Materials");
            AssetDatabase.Refresh();
            var source=JsonUtility.FromJson<Source>(File.ReadAllText("Assets/ZeroRise/Art/Player/BrowserHero.json"));
            var visual=new GameObject("Player visual – rigged streetwear");visual.transform.SetParent(motor.transform,false);
            var bones=new List<Transform>();var positions=new List<Vector3>();
            foreach(var bone in source.bones)
            {
                var obj=new GameObject(bone.name);obj.transform.SetParent(bone.parent<0?visual.transform:bones[bone.parent],false);
                var position=Vector(bone.p);obj.transform.localPosition=position-(bone.parent<0?Vector3.zero:positions[bone.parent]);
                bones.Add(obj.transform);positions.Add(position);
            }
            for(int side=-1;side<=1;side+=2)
            {
                var eye=new GameObject(side<0?"Eye_L":"Eye_R");eye.transform.SetParent(bones[4],false);
                var position=new Vector3(side*.046f,1.618f,.111f);eye.transform.localPosition=position-positions[4];bones.Add(eye.transform);positions.Add(position);
            }
            var mats=new List<Material>();
            foreach(var surface in source.materials)
            {
                // Three exports linear RGB; Unity's Color property/inspector uses
                // the artist-facing gamma color, as the existing district does.
                var tint=new Color(surface.color[0],surface.color[1],surface.color[2]).gamma;
                var mat=Material("Hero_"+surface.name,null,tint,1-surface.roughness);
                if(surface.kind=="cloth"||surface.kind=="denim"||surface.kind=="pack")
                {
                    mat.SetTexture("_NormalMap",Fabric(surface.kind));mat.SetFloat("_NormalScale",.13f);
                    mat.SetTextureScale("_NormalMap",Vector2.one);mat.SetTextureScale("_BaseColorMap",Vector2.one*7);
                }
                HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);mats.Add(mat);
            }
            int AddMaterial(string name,Color tint,float smooth=.15f){mats.Add(Material("Hero_"+name,null,tint,smooth));return mats.Count-1;}
            int whites=AddMaterial("EyeIvory",new Color(.62f,.65f,.61f),.5f),iris=AddMaterial("HazelIris",new Color(.055f,.095f,.058f),.38f);
            int pupil=AddMaterial("Pupil",new Color(.008f,.01f,.009f),.45f),thread=AddMaterial("SeamAndCord",new Color(.21f,.25f,.25f));
            var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<BoneWeight>();
            var indices=new List<int>[mats.Count];for(int i=0;i<indices.Length;i++)indices[i]=new List<int>();
            BoneWeight Weight(int bone,Vector3 p)
            {
                int other=bone;float amount=0;
                if(bone==1){other=2;amount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.96f,1.28f,p.y));}
                else if(bone==6||bone==12){other=bone+1;amount=Mathf.InverseLerp(1.09f,.98f,p.y)*.45f;}
                else if(bone==7||bone==13){other=bone-1;amount=Mathf.InverseLerp(.95f,1.07f,p.y)*.32f;}
                else if(bone==9||bone==15){other=bone+1;amount=Mathf.InverseLerp(.55f,.44f,p.y)*.4f;}
                else if(bone==10||bone==16){other=bone-1;amount=Mathf.InverseLerp(.42f,.52f,p.y)*.3f;}
                return new BoneWeight{boneIndex0=bone,weight0=1-amount,boneIndex1=other,weight1=amount};
            }
            int Vertex(Vector3 p,Vector3 n,Vector2 tex,int bone)
            {int id=verts.Count;verts.Add(p);normals.Add(n.normalized);uv.Add(tex);weights.Add(Weight(bone,p));return id;}
            void Triangle(int mat,int a,int b,int c){indices[mat].Add(a);indices[mat].Add(b);indices[mat].Add(c);}
            foreach(var part in source.parts)
            {
                if(source.materials[part.material].name=="hair_494139")continue; // Replace the browser's tiny eye rectangles.
                if(source.materials[part.material].kind=="denim"&&(part.bone==9||part.bone==10||part.bone==15||part.bone==16))continue;
                if(source.materials[part.material].name=="cloth_303e4b"&&(part.bone==6||part.bone==7||part.bone==12||part.bone==13))continue;
                if(source.materials[part.material].kind=="skin"&&(part.bone==6||part.bone==7||part.bone==12||part.bone==13))continue; // Skin beneath the fixed long sleeves.
                var remap=new int[part.positions.Length/3];
                for(int i=0;i<remap.Length;i++)remap[i]=Vertex(Vector(part.positions,i*3),Vector(part.normals,i*3),new Vector2(part.uv[i*2],part.uv[i*2+1]),part.bone);
                var edges=new Dictionary<ulong,int>();
                int Midpoint(int a,int b)
                {
                    uint lo=(uint)Mathf.Min(a,b),hi=(uint)Mathf.Max(a,b);ulong key=((ulong)lo<<32)|hi;
                    if(edges.TryGetValue(key,out int found))return found;
                    var center=(verts[a]+verts[b])*.5f;
                    // Phong surface refinement rounds the original silhouette while
                    // preserving flat panels/seams and their original UVs.
                    var projected=center-(normals[a]*Vector3.Dot(center-verts[a],normals[a])+normals[b]*Vector3.Dot(center-verts[b],normals[b]))*.35f;
                    int id=Vertex(projected,(normals[a]+normals[b]).normalized,(uv[a]+uv[b])*.5f,part.bone);edges.Add(key,id);return id;
                }
                for(int i=0;i<part.indices.Length;i+=3)
                {
                    int a=remap[part.indices[i]],b=remap[part.indices[i+1]],c=remap[part.indices[i+2]];
                    int ab=Midpoint(a,b),bc=Midpoint(b,c),ca=Midpoint(c,a);
                    Triangle(part.material,a,ab,ca);Triangle(part.material,ab,b,bc);Triangle(part.material,ca,bc,c);Triangle(part.material,ab,bc,ca);
                }
            }
            void Ellipsoid(Vector3 center,Vector3 size,int material,int bone,int rings=10,int segments=20)
            {
                int start=verts.Count;
                for(int j=0;j<=rings;j++)for(int i=0;i<=segments;i++)
                {
                    float a=i*Mathf.PI*2/segments,t=j*Mathf.PI/rings;
                    var p=new Vector3(Mathf.Sin(t)*Mathf.Sin(a),Mathf.Cos(t),Mathf.Sin(t)*Mathf.Cos(a));
                    Vertex(center+Vector3.Scale(p,size),new Vector3(p.x/size.x,p.y/size.y,p.z/size.z),new Vector2(i/(float)segments,j/(float)rings),bone);
                }
                for(int j=0;j<rings;j++)for(int i=0;i<segments;i++)
                {int a=start+j*(segments+1)+i,b=a+segments+1;Triangle(material,a,b,a+1);Triangle(material,a+1,b,b+1);}
            }
            void Garment(float x,Vector3[] rings,int material,int upper,int lower,float jointY,float blendRadius)
            {
                const int segments=32;int start=verts.Count;
                for(int j=0;j<rings.Length;j++)for(int i=0;i<=segments;i++)
                {
                    float angle=i*Mathf.PI*2/segments;var ring=rings[j];
                    var before=rings[Mathf.Max(0,j-1)];var after=rings[Mathf.Min(rings.Length-1,j+1)];
                    float drx=(after.y-before.y)/(after.x-before.x),drz=(after.z-before.z)/(after.x-before.x);
                    var around=new Vector3(Mathf.Cos(angle)*ring.y,0,-Mathf.Sin(angle)*ring.z);
                    var up=new Vector3(Mathf.Sin(angle)*drx,1,Mathf.Cos(angle)*drz);
                    float centerZ=(upper==6||upper==12)?Mathf.Lerp(0,.013f,Mathf.InverseLerp(1.04f,.92f,ring.x)):0;
                    int v=Vertex(new Vector3(x+Mathf.Sin(angle)*ring.y,ring.x,Mathf.Cos(angle)*ring.z+centerZ),Vector3.Cross(around,up),new Vector2(i/(float)segments,j/(float)(rings.Length-1)),upper);
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(jointY+blendRadius,jointY-blendRadius,ring.x));
                    weights[v]=new BoneWeight{boneIndex0=upper,weight0=1-blend,boneIndex1=lower,weight1=blend};
                }
                for(int j=0;j<rings.Length-1;j++)for(int i=0;i<segments;i++)
                {int a=start+j*(segments+1)+i,b=a+segments+1;Triangle(material,a,b,a+1);Triangle(material,a+1,b,b+1);}
            }
            void Cord(Vector3[] points,float radius,int material,int bone)
            {
                int start=verts.Count;
                for(int j=0;j<points.Length;j++)
                {
                    var direction=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(j-1,0)]).normalized;
                    var side=Vector3.Cross(direction,Mathf.Abs(direction.y)>.8f?Vector3.forward:Vector3.up).normalized;
                    var up=Vector3.Cross(direction,side);
                    for(int i=0;i<=6;i++)
                    {float a=i*Mathf.PI/3;var n=side*Mathf.Cos(a)+up*Mathf.Sin(a);Vertex(points[j]+n*radius,n,new Vector2(i/6f,j*.15f),bone);}
                }
                for(int j=0;j<points.Length-1;j++)for(int i=0;i<6;i++)
                {int a=start+j*7+i,b=a+7;Triangle(material,a,a+1,b);Triangle(material,a+1,b+1,b);}
            }
            for(int side=-1;side<=1;side+=2)
            {
                int eye=side<0?18:19,hand=side<0?8:14,foot=side<0?11:17,thigh=side<0?9:15;
                int arm=side<0?6:12;
                Garment(side*.26f,new[]{new Vector3(1.323f,.063f,.066f),new Vector3(1.295f,.084f,.084f),new Vector3(1.255f,.082f,.081f),new Vector3(1.18f,.077f,.074f),new Vector3(1.095f,.063f,.064f),new Vector3(1.01f,.06f,.06f),new Vector3(.95f,.058f,.056f),new Vector3(.86f,.047f,.046f),new Vector3(.815f,.046f,.046f)},6,arm,arm+1,1.01f,.065f);
                Garment(side*.115f,new[]{new Vector3(.881f,.079f,.088f),new Vector3(.825f,.093f,.1f),new Vector3(.73f,.083f,.088f),new Vector3(.58f,.065f,.07f),new Vector3(.47f,.063f,.068f),new Vector3(.4f,.068f,.066f),new Vector3(.28f,.056f,.056f),new Vector3(.15f,.05f,.052f),new Vector3(.09f,.049f,.05f)},7,thigh,thigh+1,.47f,.07f);
                Ellipsoid(new Vector3(side*.26f,1.302f,0),new Vector3(.082f,.052f,.082f),6,arm);
                Ellipsoid(new Vector3(side*.115f,.866f,0),new Vector3(.085f,.04f,.095f),7,thigh);
                Ellipsoid(new Vector3(side*.046f,1.618f,.111f),new Vector3(.014f,.0055f,.005f),whites,eye);
                Ellipsoid(new Vector3(side*.046f,1.618f,.1158f),new Vector3(.0048f,.0046f,.0015f),iris,eye);
                Ellipsoid(new Vector3(side*.046f,1.618f,.117f),new Vector3(.0024f,.0037f,.001f),pupil,eye);
                // Soft brow contour and small sideburns follow the animated head.
                Cord(new[]{new Vector3(side*.027f,1.638f,.100f),new Vector3(side*.043f,1.645f,.104f),new Vector3(side*.067f,1.641f,.098f)},.0032f,4,4);
                Cord(new[]{new Vector3(side*.105f,1.685f,-.006f),new Vector3(side*.112f,1.651f,-.004f),new Vector3(side*.11f,1.621f,-.006f)},.008f,4,4);
                // The original mitten silhouette gains separated curled fingers.
                for(int finger=0;finger<4;finger++)Ellipsoid(new Vector3(side*.26f+(finger-1.5f)*.014f,.734f,.028f),new Vector3(.008f,.027f,.014f),2,hand,6,10);
                Cord(new[]{new Vector3(side*.062f,1.363f,.142f),new Vector3(side*.062f,1.281f,.158f),new Vector3(side*.07f,1.196f,.164f)},.0033f,thread,2);
                Cord(new[]{new Vector3(side*.087f,1.03f,.139f),new Vector3(side*.103f,1.004f,.141f),new Vector3(side*.017f,1.004f,.148f)},.0014f,thread,1);
                // Jeans side stitching and curved rear pocket seams.
                Cord(new[]{new Vector3(side*.18f,.835f,-.008f),new Vector3(side*.191f,.747f,-.008f),new Vector3(side*.179f,.65f,-.005f),new Vector3(side*.168f,.55f,-.003f)},.0012f,thread,thigh);
                Cord(new[]{new Vector3(side*.063f,.865f,-.106f),new Vector3(side*.06f,.811f,-.107f),new Vector3(side*.113f,.793f,-.106f),new Vector3(side*.164f,.816f,-.103f),new Vector3(side*.16f,.866f,-.103f)},.0014f,thread,1);
                // Four raised laces on each sneaker, with a rounded heel tab.
                for(int lace=0;lace<4;lace++)
                {float z=.042f+lace*.019f;Cord(new[]{new Vector3(side*.115f-.022f,.11f,z),new Vector3(side*.115f,.117f,z+.004f),new Vector3(side*.115f+.022f,.11f,z)},.0022f,thread,foot);}
                Ellipsoid(new Vector3(side*.115f,.093f,-.069f),new Vector3(.021f,.022f,.008f),9,foot,6,12);
                Cord(new[]{new Vector3(side*.075f,1.335f,-.322f),new Vector3(side*.104f,1.293f,-.343f),new Vector3(side*.105f,1.12f,-.361f)},.0025f,thread,5);
            }
            Cord(new[]{new Vector3(-.022f,1.516f,.1f),new Vector3(0,1.513f,.104f),new Vector3(.022f,1.516f,.1f)},.0017f,5,4);
            // Zipper pull, stitched backpack pocket and a fabric handle.
            Cord(new[]{new Vector3(-.107f,1.192f,-.398f),new Vector3(0,1.197f,-.405f),new Vector3(.107f,1.192f,-.398f)},.0023f,thread,5);
            Ellipsoid(new Vector3(.084f,1.183f,-.408f),new Vector3(.004f,.011f,.003f),1,5,6,10);
            var meshPath=Folder+"/StreetwearHero.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
            mesh.name="ZERO RISE refined streetwear hero";mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();
            mesh.bindposes=bones.ConvertAll(b=>b.worldToLocalMatrix*visual.transform.localToWorldMatrix).ToArray();
            mesh.subMeshCount=indices.Length;for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
            mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var renderer=visual.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=bones.ToArray();renderer.rootBone=bones[1];renderer.sharedMaterials=mats.ToArray();
            renderer.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(1.6f,2.1f,1.35f));renderer.updateWhenOffscreen=false;
            var animator=visual.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var rig=AvatarBuilder.BuildGenericAvatar(visual,"Rig");rig.name="ZERO RISE generic player rig";
            if(!rig.isValid)throw new Exception("Player skeleton is invalid");
            string rigPath=Folder+"/StreetwearHeroRig.asset";
            if(File.Exists(Folder+"/StreetwearHero.avatar")&&!File.Exists(rigPath))AssetDatabase.MoveAsset(Folder+"/StreetwearHero.avatar",rigPath);
            var savedRig=AssetDatabase.LoadAssetAtPath<Avatar>(rigPath);
            if(!savedRig){AssetDatabase.CreateAsset(rig,rigPath);savedRig=rig;}else{EditorUtility.CopySerialized(rig,savedRig);UnityEngine.Object.DestroyImmediate(rig);EditorUtility.SetDirty(savedRig);}
            animator.avatar=savedRig;animator.runtimeAnimatorController=PlayerAnimationBuilder.Build(visual.transform,bones.ToArray());animator.applyRootMotion=false;
            var avatar=visual.AddComponent<PlayerAvatar>();avatar.animator=animator;avatar.skin=renderer;avatar.leftEye=bones[18];avatar.rightEye=bones[19];avatar.hips=bones[1];
            avatar.chest=bones[2];avatar.backpack=bones[5];avatar.leftArm=bones[6];avatar.rightArm=bones[12];
            avatar.head=bones[4];avatar.leftForearm=bones[7];avatar.rightForearm=bones[13];avatar.leftHand=bones[8];avatar.rightHand=bones[14];
            avatar.leftThigh=bones[9];avatar.leftCalf=bones[10];avatar.leftFoot=bones[11];avatar.rightThigh=bones[15];avatar.rightCalf=bones[16];avatar.rightFoot=bones[17];
            motor.visual=visual.transform;motor.avatar=avatar;
            foreach(var t in visual.GetComponentsInChildren<Transform>()){t.gameObject.layer=2;t.gameObject.isStatic=false;}
            PrefabUtility.SaveAsPrefabAsset(visual,Folder+"/StreetwearHero.prefab");AssetDatabase.SaveAssets();
            Debug.Log("Player built: "+verts.Count+" vertices, "+bones.Count+" bones, "+mats.Count+" material slots.");return avatar;
        }
        static Texture2D Fabric(string kind)
        {
            string path=Folder+"/Fabric_"+kind+"_normal.png";
            if(!File.Exists(path))
            {
                const int size=256;var image=new Texture2D(size,size,TextureFormat.RGB24,false,true);var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float a=x*Mathf.PI*.25f,b=y*Mathf.PI*.25f;
                    float nx=Mathf.Cos(a)*.22f,ny=Mathf.Sin(b)*.18f;
                    if(kind=="denim"){nx=Mathf.Cos(a+b)*.2f;ny=nx*.7f;}
                    var normal=new Vector3(nx,ny,1).normalized;pixels[y*size+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f);
                }
                image.SetPixels(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.NormalMap;importer.convertToNormalmap=false;importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        [MenuItem("ZERO RISE/Spieler-Modell und Animationen aktualisieren")]
        public static void Upgrade()
        {
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            var motor=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();
            if(!motor)throw new Exception("Existing player missing");
            if(motor.visual)UnityEngine.Object.DestroyImmediate(motor.visual.gameObject);
            Build(motor);
            foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>(FindObjectsSortMode.None))WestbruchProps.CarInteractions(car);
            EditorSceneManager.SaveScene(motor.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
