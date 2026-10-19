using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    // Real metre-sized plants. These source assets are shared by the district
    // baker; this class creates no scene objects and contains no runtime work.
    internal static class WestbruchFloraMeshes
    {
        static readonly Dictionary<int,Mesh> built=new Dictionary<int,Mesh>();
        const string Folder=WestbruchGeometry.Generated+"/Meshes";
        public static Mesh Get(int kind,int variant)
        {
            if(kind<0||kind>5)throw new ArgumentOutOfRangeException(nameof(kind));
            if(variant<0||variant>2)throw new ArgumentOutOfRangeException(nameof(variant));
            int key=kind*3+variant;
            if(built.TryGetValue(key,out var cached)&&cached)return cached;
            if(!AssetDatabase.IsValidFolder(Folder)){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();}
            var geometry=new Plant(kind,variant);geometry.Generate();
            string path=Folder+"/UrbanFlora_"+kind+"_"+variant+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name="UrbanFlora_"+kind+"_"+variant;mesh.indexFormat=IndexFormat.UInt16;
            mesh.SetVertices(geometry.vertices);mesh.SetColors(geometry.colors);mesh.SetUVs(0,geometry.uv);mesh.SetTriangles(geometry.triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);built[key]=mesh;return mesh;
        }

        sealed class Plant
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Color> colors=new List<Color>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> triangles=new List<int>();
            readonly System.Random random;
            readonly int kind,variant;
            readonly float height;
            // Mesh vertex data is linear. Neutral material multiplication keeps
            // these restrained artist-space olive/straw/bark tones predictable.
            static readonly Color Olive=new Color(.38f,.41f,.25f).linear;
            static readonly Color Fresh=new Color(.32f,.40f,.23f).linear;
            static readonly Color Straw=new Color(.51f,.47f,.32f).linear;
            static readonly Color Dry=new Color(.42f,.37f,.25f).linear;
            static readonly Color Bark=new Color(.35f,.28f,.20f).linear;
            static readonly Color Pale=new Color(.73f,.71f,.59f).linear;
            float F(float a,float b)=>a+(b-a)*(float)random.NextDouble();
            Vector3 Out(float angle)=>new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            public Plant(int kind,int variant)
            {
                this.kind=kind;this.variant=variant;random=new System.Random(61309+kind*1543+variant*313);
                height=kind==0?F(.23f,.36f):kind==1?F(.32f,.48f):kind==2?F(.065f,.12f):kind==3?F(.28f,.43f):kind==4?F(.58f,.88f):F(.17f,.24f);
            }
            public void Generate()
            {
                switch(kind)
                {
                    case 0:Grass();break;case 1:Sedge();break;case 2:Rosette();break;
                    case 3:Weed();break;case 4:Shrub();break;default:Flowers();break;
                }
                float low=float.PositiveInfinity,high=float.NegativeInfinity;
                foreach(var point in vertices){low=Mathf.Min(low,point.y);high=Mathf.Max(high,point.y);}
                // Keep source units reliable for placement and bend-height metadata.
                float sy=height/Mathf.Max(.001f,high-low);
                for(int i=0;i<vertices.Count;i++){var p=vertices[i];p.y=(p.y-low)*sy;vertices[i]=p;}
                int budget=kind==0?121:kind==2?450:kind==1||kind==3?1000:kind==4?2500:500;
                if(triangles.Count/3>=budget)throw new InvalidOperationException("Urban flora triangle budget exceeded: "+kind+"/"+variant);
            }
            int Vertex(Vector3 p,Color color,Vector2 texture)
            {int index=vertices.Count;color.a=1;vertices.Add(p);colors.Add(color);uv.Add(texture);return index;}
            void Tri(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            void Quad(int a,int b,int c,int d){Tri(a,b,c);Tri(c,b,d);}
            void Face(Vector3 a,Vector3 b,Vector3 c,Color color)
            {int start=vertices.Count;Vertex(a,color,new Vector2(0,0));Vertex(b,color,new Vector2(0,1));Vertex(c,color,new Vector2(1,0));Tri(start,start+1,start+2);}

            // Blades retain a curved, folded three-dimensional lamina, rendered
            // double-sided; sub-millimetre thickness needs no duplicate faces.
            // Broader leaves retain separate surfaces and closed edges.
            void Leaf(Func<float,Vector3> spine,Vector3 side,float width,Color tone,int segments=3,bool blade=false,float twist=0)
            {
                int start=vertices.Count;
                for(int section=0;section<=segments;section++)
                {
                    float t=section/(float)segments;
                    var tangent=(spine(Mathf.Min(1,t+.01f))-spine(Mathf.Max(0,t-.01f))).normalized;
                    var across=Vector3.ProjectOnPlane(side,tangent).normalized;
                    if(across.sqrMagnitude<.01f)across=Vector3.Cross(tangent,Vector3.forward).normalized;
                    across=Quaternion.AngleAxis(twist*t,tangent)*across;
                    var normal=Vector3.Cross(tangent,across).normalized;
                    float profile=blade?Mathf.Pow(1-t,.72f)*Mathf.Lerp(.70f,1,Mathf.Min(1,t*5)):Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.72f);
                    float breadth=Mathf.Max(.00012f,width*profile*F(.82f,1.06f));
                    float l=breadth*F(.85f,1.02f),r=breadth*F(.85f,1.02f),fold=breadth*(blade?.23f:.15f);
                    float thickness=blade?0:.00040f;
                    var center=spine(t);
                    var leafTone=blade?Color.Lerp(tone,Straw,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,1,t))*.65f):tone;
                    var shade=leafTone*Mathf.Lerp(.58f,1.05f,Mathf.SmoothStep(0,1,t));shade.a=1;
                    Vertex(center-across*l+normal*thickness,shade,new Vector2(0,t));
                    Vertex(center+normal*(fold+thickness),shade*1.035f,new Vector2(.5f,t));
                    Vertex(center+across*r+normal*thickness,shade,new Vector2(1,t));
                    if(!blade)
                    {
                        Vertex(center-across*l-normal*thickness,shade*.86f,new Vector2(0,t));
                        Vertex(center+normal*(fold-thickness),shade*.90f,new Vector2(.5f,t));
                        Vertex(center+across*r-normal*thickness,shade*.86f,new Vector2(1,t));
                    }
                    if(section==0)continue;
                    int stride=blade?3:6,a=start+(section-1)*stride,b=a+stride;
                    Quad(a,b,a+1,b+1);Quad(a+1,b+1,a+2,b+2);
                    if(!blade)
                    {
                        Quad(a+3,a+4,b+3,b+4);Quad(a+4,a+5,b+4,b+5);
                        Quad(a+3,b+3,a,b);Quad(a+2,b+2,a+5,b+5);
                    }
                }
                if(!blade)
                {
                    Quad(start,start+1,start+3,start+4);Quad(start+1,start+2,start+4,start+5);
                    int end=start+segments*6;
                    Quad(end,end+3,end+1,end+4);Quad(end+1,end+4,end+2,end+5);
                }
            }
            void Tube(Vector3[] points,float radius,Color tone,int sides=3)
            {
                int start=vertices.Count;
                for(int ring=0;ring<points.Length;ring++)
                {
                    float t=ring/(float)(points.Length-1);
                    var tangent=(points[Mathf.Min(points.Length-1,ring+1)]-points[Mathf.Max(0,ring-1)]).normalized;
                    var right=Vector3.Cross(Mathf.Abs(tangent.y)>.9f?Vector3.forward:Vector3.up,tangent).normalized;
                    var forward=Vector3.Cross(tangent,right).normalized;
                    float r=radius*Mathf.Lerp(1,.24f,t);
                    for(int side=0;side<sides;side++)
                    {
                        float a=side*Mathf.PI*2/sides;
                        var shade=tone*Mathf.Lerp(.57f,1,t);shade.a=1;
                        Vertex(points[ring]+(right*Mathf.Cos(a)+forward*Mathf.Sin(a))*r,shade,new Vector2(side/(float)sides,t));
                        if(ring>0)
                        {
                            int old=start+(ring-1)*sides+side,next=start+(ring-1)*sides+(side+1)%sides;
                            Quad(old,old+sides,next,next+sides);
                        }
                    }
                }
                int bottom=Vertex(points[0],tone*.55f,new Vector2(.5f,0));
                int top=Vertex(points[points.Length-1],tone,new Vector2(.5f,1));
                for(int side=0;side<sides;side++)
                {
                    Tri(bottom,start+(side+1)%sides,start+side);
                    int end=start+(points.Length-1)*sides;Tri(top,end+side,end+(side+1)%sides);
                }
            }
            void Octa(Vector3 center,Vector3 size,Color tone)
            {
                var a=center+Vector3.up*size.y;var b=center-Vector3.up*size.y;
                var ring=new[]{center+Vector3.right*size.x,center+Vector3.forward*size.z,center-Vector3.right*size.x,center-Vector3.forward*size.z};
                for(int i=0;i<4;i++){Face(a,ring[i],ring[(i+1)%4],tone);Face(b,ring[(i+1)%4],ring[i],tone*.84f);}
            }
            void Grass()
            {
                int count=8+variant;float lean=F(0,Mathf.PI*2),spread=F(.13f,.20f);
                for(int i=0;i<count;i++)
                {
                    float a=i*2.399963f+F(-.4f,.4f);
                    var direction=Out(a);var root=direction*F(.012f,spread);
                    float h=height*F(.56f,1),bend=F(.08f,.19f);
                    var wind=Out(lean)*F(.01f,.035f);
                    Leaf(t=>root+Vector3.up*(h*t-bend*.55f*t*t*t)+direction*(bend*t*t)+wind*t*t,
                        Out(a+Mathf.PI*.5f),F(.0070f,.0130f),Color.Lerp(Olive,Straw,F(.20f,.80f)),2,true,F(-30,35));
                }
            }
            void Sedge()
            {
                for(int i=0;i<6;i++)
                {
                    float a=i*2.399963f+F(-.3f,.3f),h=height*(i<3?F(.76f,.98f):F(.35f,.61f));
                    var direction=Out(a);var root=direction*F(.005f,.025f);float bend=F(.025f,.075f);
                    Leaf(t=>root+Vector3.up*(h*t-bend*.3f*t*t*t)+direction*(bend*t*t),Out(a+1.57f),F(.0026f,.0045f),Color.Lerp(Olive,Straw,F(.2f,.7f)),4,true,F(-20,28));
                }
                for(int stalk=0;stalk<2;stalk++)
                {
                    var direction=Out(F(0,Mathf.PI*2));float h=height*F(.91f,1.02f);
                    var top=direction*.035f+Vector3.up*h;
                    Tube(new[]{direction*.008f,Vector3.up*(h*.35f),direction*.012f+Vector3.up*(h*.70f),top},.0015f,Dry);
                    for(int n=0;n<5;n++)
                    {
                        float a=n*2.4f+stalk;var center=top+Out(a)*.0045f-Vector3.up*(n*.008f);
                        Octa(center,new Vector3(.0028f,.0065f,.0028f),Color.Lerp(Dry,Straw,.45f));
                    }
                }
            }
            void Rosette()
            {
                int count=6+variant;
                for(int i=0;i<count;i++)
                {
                    float a=i*2.399963f+F(-.25f,.25f),length=F(.10f,.17f),arch=height*F(.65f,1);
                    var direction=Out(a);var bend=Out(a+.6f)*F(.008f,.025f);
                    Leaf(t=>Vector3.up*(.006f+Mathf.Sin(t*Mathf.PI)*arch)+direction*(t*length)+bend*t*t,
                        Out(a+1.57f),F(.008f,.014f),Color.Lerp(Fresh,Olive,F(.1f,.65f)),3,false,F(-20,24));
                }
            }
            void Weed()
            {
                var lean=Out(F(0,Mathf.PI*2))*F(.018f,.038f);
                Vector3 Stem(float t)=>Vector3.up*(height*t)+lean*t*t;
                Tube(new[]{Stem(0),Stem(.32f),Stem(.68f),Stem(1)},.0028f,Color.Lerp(Dry,Olive,.55f),4);
                for(int level=0;level<3;level++)
                {
                    float f=.24f+level*.23f,a=level*2.25f+variant*.7f;
                    var start=Stem(f);var branch=start+Out(a)*F(.045f,.075f)+Vector3.up*F(.025f,.055f);
                    Tube(new[]{start,Vector3.Lerp(start,branch,.55f)+Vector3.up*.008f,branch},.0015f,Olive);
                    for(int pair=0;pair<2;pair++)
                    {
                        float angle=a+pair*Mathf.PI;
                        WeedLeaf(start,angle,F(.055f,.083f));WeedLeaf(branch,angle+.7f,F(.037f,.060f));
                    }
                }
                WeedLeaf(Stem(.89f),variant+.5f,.034f);WeedLeaf(Stem(.91f),variant+3.3f,.029f);
            }
            void WeedLeaf(Vector3 root,float a,float length)
            {
                var direction=Out(a);
                Leaf(t=>root+direction*(length*t)+Vector3.up*(length*.25f*t+Mathf.Sin(t*Mathf.PI)*.009f),
                    Out(a+1.57f),length*F(.19f,.25f),Color.Lerp(Fresh,Olive,F(.15f,.8f)),3,false,F(-25,25));
            }
            void Shrub()
            {
                var lean=Out(F(0,Mathf.PI*2))*.035f;
                Vector3 Trunk(float t)=>Vector3.up*(height*.70f*t)+lean*t*t;
                Tube(new[]{Trunk(0),Trunk(.3f),Trunk(.66f),Trunk(1)},F(.008f,.012f),Bark,4);
                int branches=6+variant%2;
                for(int branch=0;branch<branches;branch++)
                {
                    float a=branch*2.399963f+F(-.55f,.55f),level=F(.29f,.86f);
                    var start=Trunk(level);var direction=Out(a);
                    var end=direction*F(.20f,.36f)+Vector3.up*(height*F(.58f,.94f));
                    Tube(new[]{start,Vector3.Lerp(start,end,.32f),Vector3.Lerp(start,end,.72f)+Vector3.up*.025f,end},F(.003f,.005f),Bark,4);
                    for(int twig=0;twig<2;twig++)
                    {
                        float spread=a+(twig==0?-.60f:.68f)+F(-.2f,.2f);
                        var origin=Vector3.Lerp(start,end,twig==0?.58f:.82f);
                        var tip=origin+Out(spread)*F(.07f,.13f)+Vector3.up*F(.035f,.085f);
                        Tube(new[]{origin,Vector3.Lerp(origin,tip,.5f)+Vector3.up*.008f,tip},.0018f,Bark);
                        for(int leaf=0;leaf<3;leaf++)
                        {
                            var root=Vector3.Lerp(origin,tip,.42f+leaf*.26f);float angle=spread+(leaf%2==0?-.9f:1.0f);
                            float length=F(.043f,.065f);var outward=Out(angle);
                            Leaf(t=>root+outward*(length*t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.007f+t*.006f),
                                Out(angle+1.57f),F(.012f,.018f),Color.Lerp(Olive,Dry,F(.04f,.35f)),3,false,F(-40,40));
                        }
                    }
                }
            }
            void Flowers()
            {
                for(int leaf=0;leaf<3;leaf++)
                {
                    float a=leaf*2.4f,length=F(.045f,.075f);var direction=Out(a);
                    Leaf(t=>direction*(length*t)+Vector3.up*(.005f+Mathf.Sin(t*Mathf.PI)*.022f),Out(a+1.57f),.0045f,Olive,3);
                }
                int stalks=variant==1?1:2;
                for(int stalk=0;stalk<stalks;stalk++)
                {
                    float a=stalk*2.7f+variant;var outward=Out(a);float h=height*F(.78f,1);
                    var top=outward*.025f+Vector3.up*h;
                    Tube(new[]{outward*.008f,Vector3.up*(h*.34f),outward*.012f+Vector3.up*(h*.7f),top},.0012f,Color.Lerp(Dry,Olive,.5f),4);
                    for(int head=0;head<2;head++)
                    {
                        var root=Vector3.Lerp(Vector3.zero,top,.82f+head*.08f);
                        var tip=root+Out(a+head*2.1f)*.018f+Vector3.up*.018f;
                        Tube(new[]{root,tip},.0007f,Dry);Flower(tip,F(.006f,.0085f),a+head);
                    }
                }
            }
            void Flower(Vector3 center,float radius,float yaw)
            {
                Octa(center,new Vector3(.0023f,.0020f,.0023f),Dry);
                for(int petal=0;petal<5;petal++)
                {
                    float angle=yaw+petal*Mathf.PI*2/5;var direction=Out(angle);var across=Out(angle+1.57f);
                    var a=center+direction*.0012f;var b=center+direction*(radius*.55f)-across*radius*.30f;
                    var c=center+direction*radius+Vector3.up*.0012f;var d=center+direction*(radius*.55f)+across*radius*.30f;
                    var ridge=center+direction*(radius*.52f)+Vector3.up*.0018f;
                    foreach(var pair in new[]{new[]{a,b},new[]{b,c},new[]{c,d},new[]{d,a}})
                    {
                        Face(pair[0],ridge,pair[1],Pale);Face(pair[1]-Vector3.up*.0003f,ridge-Vector3.up*.0003f,pair[0]-Vector3.up*.0003f,Pale*.87f);
                    }
                }
            }
        }
    }
}
