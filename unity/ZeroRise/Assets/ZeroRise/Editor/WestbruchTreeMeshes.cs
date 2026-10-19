using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    // Authored branch topology and individual photographic leaf laminae. Source
    // assets are shared by all tree instances; no canopy hulls or runtime work.
    internal static class WestbruchTreeMeshes
    {
        const string Folder=WestbruchGeometry.Generated+"/Meshes";
        static readonly Dictionary<int,Mesh> built=new Dictionary<int,Mesh>();
        public static Mesh Get(int kind,int lod)
        {
            if(kind<0||kind>3)throw new ArgumentOutOfRangeException(nameof(kind));
            if(lod<0||lod>1)throw new ArgumentOutOfRangeException(nameof(lod));
            int key=kind*2+lod;if(built.TryGetValue(key,out var cached)&&cached)return cached;
            if(!AssetDatabase.IsValidFolder(Folder)){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();}
            var geometry=new Tree(kind,lod);geometry.Generate();
            string path=Folder+"/UrbanTree_"+kind+"_"+lod+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name="UrbanTree_"+kind+"_"+lod;mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(geometry.vertices);mesh.SetUVs(0,geometry.uv);mesh.SetColors(geometry.colors);mesh.subMeshCount=2;
            mesh.SetTriangles(geometry.bark,0);mesh.SetTriangles(geometry.leaves,1);
            mesh.RecalculateNormals();geometry.SmoothSeams(mesh);mesh.RecalculateTangents();mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);built[key]=mesh;return mesh;
        }

        sealed class Tree
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<Color> colors=new List<Color>();
            public readonly List<int> bark=new List<int>(),leaves=new List<int>();
            readonly List<Vector2Int> seams=new List<Vector2Int>();
            readonly int kind,lod,seed;
            readonly float height,crown,trunkRadius;
            Vector3[] trunk;
            // Photography supplies the actual albedo. These near-neutral tints
            // vary foliage gently without multiplying the 2K photographs dark.
            static readonly Color[] LeafTints={
                new Color(.88f,.95f,.83f).linear,new Color(.96f,.98f,.89f).linear,
                new Color(.83f,.92f,.80f).linear,new Color(.93f,.96f,.82f).linear};
            static readonly Color BarkTint=new Color(.96f,.94f,.90f).linear;
            float F(System.Random random,float a,float b)=>a+(b-a)*(float)random.NextDouble();
            System.Random Random(int id)=>new System.Random(unchecked(seed+id*7919));
            Vector3 Out(float angle)=>new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            public Tree(int kind,int lod)
            {
                this.kind=kind;this.lod=lod;seed=81539+kind*17957;
                height=new[]{7.05f,7.55f,6.35f,7.20f}[kind];
                crown=new[]{2.05f,1.14f,2.48f,2.08f}[kind];
                trunkRadius=new[]{.235f,.135f,.24f,.265f}[kind];
            }
            public void Generate()
            {
                var random=Random(1);float phase=F(random,0,Mathf.PI*2);
                var lean=Out(phase)*new[]{.19f,.12f,.24f,.34f}[kind];
                float leader=height*new[]{.67f,.83f,.61f,.70f}[kind];
                trunk=new Vector3[12];
                for(int i=0;i<trunk.Length;i++)
                {
                    float t=i/(float)(trunk.Length-1);
                    trunk[i]=Vector3.up*(leader*t)+lean*t*t+Out(phase+1.2f)*(Mathf.Sin(t*4.3f)*.13f*t);
                }
                Tube(trunk,trunkRadius,lod==0?14:8,0,true);
                // A street tree's root collar is one rounded continuous surface;
                // separate root cylinders looked like sharp pegs on the terrain.
                int primaryCount=kind==1?8:kind==2?10:9;
                const int secondaryCount=4,tertiaryCount=4;
                for(int primary=0;primary<primaryCount;primary++)
                {
                    int id=100+primary;var rng=Random(id);
                    // Fork heights are independent of azimuth and crown height.
                    // A broadleaf crown must not become a stack of conifer tiers.
                    float fraction=F(rng,kind==1?.45f:.58f,.97f);
                    if(primary==0)fraction=kind==1?.43f:.55f;if(primary==1)fraction=.91f;
                    var start=Along(trunk,fraction);float angle=phase+primary*2.399963f+F(rng,-.32f,.32f);
                    float targetY=height*F(rng,kind==1?.48f:.51f,kind==1?.92f:.86f);
                    targetY=Mathf.Max(start.y+height*.075f,targetY);
                    float latitude=Mathf.Clamp((targetY-height*.67f)/(height*.38f),-.9f,.9f);
                    float profile=Mathf.Sqrt(1-latitude*latitude);
                    float reach=crown*profile*F(rng,.73f,1.07f);
                    if(kind==3)reach*=1+.26f*Mathf.Cos(angle-phase-.5f);
                    var end=Out(angle)*reach+lean*fraction+Vector3.up*targetY;
                    var main=ForkCurve(start,end,Tangent(trunk,fraction),id,6,.24f);
                    float radius=trunkRadius*F(rng,.30f,.45f)*(1-fraction*.32f);
                    Tube(main,radius,lod==0?8:5,id);
                    for(int secondary=0;secondary<secondaryCount;secondary++)
                    {
                        int branchId=1000+primary*100+secondary;var branchRandom=Random(branchId);
                        float f=secondary==secondaryCount-1?.965f:F(branchRandom,.24f+secondary*.18f,.48f+secondary*.18f);
                        var origin=Along(main,f);float side=secondary%2==0?-1:1;
                        float a=angle+side*F(branchRandom,.48f,1.55f);
                        float spread=(kind==1?.67f:1)*F(branchRandom,.48f,.91f);
                        var tip=origin+Out(a)*spread+Vector3.up*F(branchRandom,kind==1?-.12f:-.40f,.67f);
                        var second=ForkCurve(origin,tip,Tangent(main,f),branchId,4,.10f);
                        Tube(second,radius*F(branchRandom,.27f,.42f),lod==0?6:4,branchId);
                        for(int tertiary=0;tertiary<tertiaryCount;tertiary++)
                        {
                            int shootId=10000+primary*1000+secondary*100+tertiary;var shootRandom=Random(shootId);
                            float tf=tertiary==tertiaryCount-1?.965f:F(shootRandom,.22f+tertiary*.19f,.43f+tertiary*.19f);
                            var joint=Along(second,tf);float ta=a+(tertiary%2==0?-1:1)*F(shootRandom,.40f,1.28f);
                            var shootEnd=joint+Out(ta)*F(shootRandom,kind==1?.20f:.28f,kind==1?.43f:.57f)+Vector3.up*F(shootRandom,-.36f,.44f);
                            var shoot=Curve(joint,shootEnd,shootId,3,.045f);
                            if(lod==0||tertiary%2==0)Tube(shoot,F(shootRandom,.0048f,.0080f),lod==0?4:3,shootId);
                            Spray(shoot,ta,12,shootId+170000);
                            for(int twig=0;twig<6;twig++)
                            {
                                int twigId=100000+primary*10000+secondary*1000+tertiary*10+twig;var twigRandom=Random(twigId);
                                var basePoint=Along(shoot,.19f+twig*.145f+F(twigRandom,-.025f,.025f));
                                float wa=ta+(twig%2==0?-1:1)*F(twigRandom,.45f,1.72f);
                                var twigEnd=basePoint+Out(wa)*F(twigRandom,.20f,.38f)+Vector3.up*F(twigRandom,-.27f,.29f);
                                var fine=Curve(basePoint,twigEnd,twigId,3,.025f);
                                // Subpixel twigs disappear in LOD1; the retained
                                // leaves keep their real three-dimensional shoots.
                                if(lod==0)Tube(fine,F(twigRandom,.0018f,.0031f),3,twigId);
                                Spray(fine,wa,14,twigId+370000);
                            }
                        }
                    }
                }
                int count=(bark.Count+leaves.Count)/3;
                if(count>(lod==0?90000:32000)||count<(lod==0?30000:10000))
                    throw new InvalidOperationException("Tree source triangle budget invalid: "+kind+"/"+lod+" = "+count);
            }

            Vector3[] Curve(Vector3 start,Vector3 end,int id,int count,float bow)
            {
                var random=Random(id+31);var direction=end-start;
                var sideways=Vector3.Cross(Vector3.up,direction).normalized;
                var depth=Vector3.Cross(direction.normalized,sideways).normalized;
                float bend=F(random,-bow,bow),rise=F(random,-.25f,.75f)*bow;
                var points=new Vector3[count];
                for(int i=0;i<count;i++)
                {
                    float t=i/(float)(count-1),arch=Mathf.Sin(t*Mathf.PI);
                    points[i]=Vector3.Lerp(start,end,t)+sideways*(bend*arch+Mathf.Sin(t*Mathf.PI*2)*bow*.16f)+depth*(rise*arch);
                }
                points[0]=start;points[count-1]=end;return points;
            }
            Vector3[] ForkCurve(Vector3 start,Vector3 end,Vector3 parentTangent,int id,int count,float bow)
            {
                var random=Random(id+31);var delta=end-start;float length=delta.magnitude;
                var heading=delta.normalized;
                var initial=(parentTangent*.76f+heading*.24f).normalized;
                var controlA=start+initial*(length*.27f);
                var controlB=end-(heading*.86f+Vector3.up*.14f).normalized*(length*.23f);
                var sideways=Vector3.Cross(Vector3.up,heading).normalized;float bend=F(random,-bow,bow);
                var points=new Vector3[count];
                for(int i=0;i<count;i++)
                {
                    float t=i/(float)(count-1),s=1-t;
                    points[i]=start*(s*s*s)+controlA*(3*s*s*t)+controlB*(3*s*t*t)+end*(t*t*t)+sideways*(Mathf.Sin(t*Mathf.PI)*bend);
                }
                points[0]=start;points[count-1]=end;return points;
            }
            static Vector3 Tangent(Vector3[] points,float fraction)
                =>(Along(points,Mathf.Min(1,fraction+.015f))-Along(points,Mathf.Max(0,fraction-.015f))).normalized;
            static Vector3 Along(Vector3[] points,float fraction)
            {
                float t=Mathf.Clamp01(fraction)*(points.Length-1);int segment=Mathf.Min(points.Length-2,Mathf.FloorToInt(t));
                return Vector3.Lerp(points[segment],points[segment+1],t-segment);
            }
            int Vertex(Vector3 p,Vector2 texture,Color shade)
            {
                int index=vertices.Count;p.y=Mathf.Max(0,p.y);shade.a=1;
                vertices.Add(p);uv.Add(texture);colors.Add(shade);return index;
            }
            void Tri(List<int> target,int a,int b,int c){target.Add(a);target.Add(b);target.Add(c);}
            void Tube(Vector3[] points,float radius,int sides,int id,bool leader=false)
            {
                int start=vertices.Count;var random=Random(id+719);float distance=0;
                float phase=F(random,0,Mathf.PI*2),circumference=radius*Mathf.PI*2;
                for(int ring=0;ring<points.Length;ring++)
                {
                    if(ring>0)distance+=Vector3.Distance(points[ring-1],points[ring]);
                    float t=ring/(float)(points.Length-1);
                    var tangent=(points[Mathf.Min(points.Length-1,ring+1)]-points[Mathf.Max(0,ring-1)]).normalized;
                    var across=Vector3.Cross(Mathf.Abs(tangent.y)>.91f?Vector3.forward:Vector3.up,tangent).normalized;
                    var other=Vector3.Cross(tangent,across).normalized;
                    float taper=leader?(Mathf.Pow(1-t,.78f)*.913f+.007f)*(1+Mathf.Exp(-t*16)*.22f):Mathf.Pow(1-t,1.08f)*.976f+.024f;
                    for(int side=0;side<=sides;side++)
                    {
                        float u=side/(float)sides,a=u*Mathf.PI*2+phase;
                        float flute=1+Mathf.Sin(a*3+id*.31f+t*2.1f)*(leader?.045f:.025f);
                        var radial=across*Mathf.Cos(a)+other*Mathf.Sin(a);
                        var shade=BarkTint*Mathf.Lerp(leader?.72f:.91f,1.04f,t);
                        Vertex(points[ring]+radial*(radius*taper*flute),new Vector2(u*circumference/.50f,distance/.65f),shade);
                        if(ring>0&&side<sides)
                        {
                            int previous=start+(ring-1)*(sides+1)+side,current=previous+sides+1;
                            Tri(bark,previous,previous+1,current);Tri(bark,previous+1,current+1,current);
                        }
                    }
                    seams.Add(new Vector2Int(start+ring*(sides+1),start+ring*(sides+1)+sides));
                }
                int bottom=Vertex(points[0],new Vector2(.5f,0),BarkTint*.72f),top=Vertex(points[points.Length-1],new Vector2(.5f,distance/.65f),BarkTint);
                int last=start+(points.Length-1)*(sides+1);
                for(int side=0;side<sides;side++){Tri(bark,bottom,start+side+1,start+side);Tri(bark,top,last+side,last+side+1);}
            }
            void Spray(Vector3[] twig,float angle,int count,int id)
            {
                // Leaf positions and random draws are identical at both LODs;
                // the reduced LOD keeps a subset of the actual same leaves.
                var random=Random(id);
                for(int leaf=0;leaf<count;leaf++)
                {
                    float t=(leaf+.40f+F(random,-.16f,.16f))/count;
                    var root=Along(twig,t);float a=angle+(leaf%2==0?-1:1)*F(random,.72f,1.34f);
                    bool broad=kind==0||kind==3;
                    float length=F(random,broad?.105f:.075f,broad?.18f:.13f);
                    if(lod==1)length*=1.30f;
                    float width=length*F(random,broad?.31f:.25f,broad?.42f:.34f);
                    var tangent=(twig[twig.Length-1]-twig[0]).normalized;
                    var direction=(Out(a)*.74f+tangent*.24f+Vector3.up*F(random,-.74f,.66f)).normalized;
                    // Broad, varied leaf pitches break the previous edge-on
                    // shelves and expose real laminae from gameplay cameras.
                    float normalAngle=F(random,0,Mathf.PI*2);
                    var normal=(Vector3.up*F(random,.12f,.95f)+Out(normalAngle)*F(random,.32f,1.08f)).normalized;
                    float twist=F(random,-34,34),curl=F(random,.007f,.026f);
                    var tint=LeafTints[random.Next(LeafTints.Length)]*F(random,.91f,1.035f);
                    // A few sun-stressed leaves add age without turning the
                    // neglected district's established trees into dead skeletons.
                    if(F(random,0,1)<.035f)tint*=new Color(1.03f,.95f,.80f,1).linear;
                    if(lod==1&&(leaf%2!=1||(kind==2&&leaf==count-1)))continue;
                    Leaf(root,direction,normal,length,width,twist,curl,tint);
                }
            }
            void Leaf(Vector3 root,Vector3 direction,Vector3 normal,float length,float width,float twist,float curl,Color shade)
            {
                var across=Vector3.Cross(normal,direction).normalized;
                if(across.sqrMagnitude<.01f)across=Vector3.Cross(Mathf.Abs(direction.y)<.9f?Vector3.up:Vector3.right,direction).normalized;
                normal=Vector3.Cross(direction,across).normalized;
                var tip=root+direction*length-normal*curl;
                var tipAcross=Quaternion.AngleAxis(twist,direction)*across;float fold=width*.17f;
                int start=vertices.Count,col=kind%2,row=kind<2?1:0;
                // Each image occupies about85% of its tile. Crop the empty border
                // while retaining padding against neighbour mip/atlas bleeding.
                const float padding=.0325f;float x=col*.5f+padding,y=row*.5f+padding,span=.5f-padding*2;
                Vertex(root-across*width-normal*fold,new Vector2(x,y),shade*.94f);
                Vertex(root,new Vector2(x+span*.5f,y),shade);
                Vertex(root+across*width-normal*fold,new Vector2(x+span,y),shade*.96f);
                Vertex(tip-tipAcross*width-normal*(fold*.75f),new Vector2(x,y+span),shade*.97f);
                Vertex(tip,new Vector2(x+span*.5f,y+span),shade*1.025f);
                Vertex(tip+tipAcross*width-normal*(fold*.75f),new Vector2(x+span,y+span),shade*.99f);
                Tri(leaves,start,start+3,start+1);Tri(leaves,start+1,start+3,start+4);
                Tri(leaves,start+1,start+4,start+2);Tri(leaves,start+2,start+4,start+5);
            }
            public void SmoothSeams(Mesh mesh)
            {
                var normals=mesh.normals;
                foreach(var pair in seams)
                {
                    var normal=(normals[pair.x]+normals[pair.y]).normalized;normals[pair.x]=normal;normals[pair.y]=normal;
                }
                mesh.normals=normals;
            }
        }
    }
}
