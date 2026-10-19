using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise.Editor
{
    // One authoring field for both terrain paint and vegetation placement.
    // Moisture, foot wear and protected edges form coherent patches rather
    // than two unrelated random distributions on top of each other.
    internal static class WestbruchGroundCover
    {
        internal struct Cover
        {
            public float gravel,grass,soil,sand,shelter,damp,wear;
        }
        static WestbruchLayout plan;
        static Plot[] walks;
        static readonly List<Vector2> trees=new List<Vector2>();
        // Deliberate rough-grass islands make neglected yards readable from
        // normal camera distance, while the shared wear field leaves routes bare.
        public static readonly Vector4[] RoughGrassSites={
            new Vector4(-25,-70,5,3),new Vector4(-1,-69,4,2.4f),
            new Vector4(-17,-48,4,4.5f),new Vector4(10,-51,4.5f,3),
            new Vector4(-45,34,3.7f,2),new Vector4(61,31,4.2f,2.4f),
            new Vector4(101,44,3.5f,2.2f),new Vector4(-106,18,1.4f,6),
            new Vector4(-10,45,3.4f,2.4f),new Vector4(35,31,3.4f,2.3f),
            new Vector4(-70,28,3,2),new Vector4(8,-67,3,2.2f),
            new Vector4(-105,-94,2,4),new Vector4(-105,88,2,4),
            new Vector4(-45,94,2.8f,2),new Vector4(5,94,3,2),
            new Vector4(-45,-102,2.8f,3),new Vector4(6,-103,3,3),
            new Vector4(34,12,3,2.5f),new Vector4(80,-10,2,3),
            new Vector4(119,34,5,6),new Vector4(118,-19,5,6),
            new Vector4(117,-93,4,6),new Vector4(120,91,4,6)};
        public static float RoughGrass(float x,float z)
        {
            float result=0;
            foreach(var site in RoughGrassSites)
            {
                float dx=(x-site.x)/site.z,dz=(z-site.y)/site.w;
                float edge=Mathf.Sqrt(dx*dx+dz*dz)+(Mathf.PerlinNoise((x+74)*.65f,(z+91)*.65f)-.5f)*.33f;
                result=Mathf.Max(result,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1.1f,edge)));
            }
            return result;
        }
        public static void Configure(WestbruchLayout layout,Plot[] paths)
        {
            plan=layout;walks=paths;trees.Clear();
            foreach(var tree in plan.trees)trees.Add(new Vector2(tree.x,tree.z));
        }
        public static void SetTrees(IEnumerable<Vector3> positions)
        {
            trees.Clear();foreach(var p in positions)trees.Add(new Vector2(p.x,p.z));
        }
        internal static float Distance<T>(T[] plots,float x,float z,float margin=0) where T:Plot
        {
            float nearest=1000;
            foreach(var p in plots)
            {
                float dx=Mathf.Max(0,Mathf.Abs(x-p.x)-p.w*.5f-margin),dz=Mathf.Max(0,Mathf.Abs(z-p.z)-p.d*.5f-margin);
                nearest=Mathf.Min(nearest,Mathf.Sqrt(dx*dx+dz*dz));
            }
            return nearest;
        }
        static float Smooth(float low,float high,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(low,high,value));
        public static float CourtyardPathDistance(float x,float z)
        {
            float t=Mathf.Clamp01((x+35)/20),px=-35+t*20,pz=-58+Mathf.Sin(t*Mathf.PI)*.85f;
            return Vector2.Distance(new Vector2(x,z),new Vector2(px,pz));
        }
        public static Cover Sample(float x,float z)
        {
            float walk=Distance(walks,x,z),road=Distance(plan.roads,x,z,3.05f),building=Distance(plan.buildings,x,z);
            float edge=Mathf.Min(walk,road),wear=1-Smooth(.15f,1.4f,edge);
            wear=Mathf.Max(wear,1-Smooth(.55f,1.15f,CourtyardPathDistance(x,z)));
            float tree=0;
            foreach(var p in trees)tree=Mathf.Max(tree,1-Smooth(.4f,4.2f,Vector2.Distance(p,new Vector2(x,z))));
            float shelter=Mathf.Max(1-Smooth(.9f,4.8f,building),tree*.82f);
            float broad=Mathf.PerlinNoise((x+360)*.115f,(z+198)*.115f);
            float fine=Mathf.PerlinNoise((x+841)*.43f,(z+326)*.43f);
            float island=Smooth(.44f,.65f,broad*.78f+fine*.22f);
            float damp=Smooth(.43f,.66f,Mathf.PerlinNoise((x+112)*.14f,(z+350)*.14f));
            damp=Mathf.Max(damp,Hollow(x,z,73,30,5,7)*.95f+Hollow(x,z,-45,34,5,3)*.8f);
            float grass=Mathf.Clamp01(island*.90f+shelter*.22f)*(1-wear*.94f)*(1-tree*.32f);
            float soil=Mathf.Clamp01(.14f+damp*.44f+tree*.48f+shelter*.17f)*(1-wear*.68f);
            grass=Mathf.Max(grass,RoughGrass(x,z)*.72f*(1-wear));
            // The unpaved plots share a dry, colonised base, with stronger
            // growth in irregular islands. Bare circulation is still worn out.
            grass=Mathf.Max(grass,(.25f+island*.28f)*(1-wear)*(1-tree*.22f));
            if(building<.55f){grass*=.1f;soil=Mathf.Max(soil,.62f);}
            if(x< -103){grass=Mathf.Max(grass,.48f*(1-wear));soil=Mathf.Max(soil,.24f);}
            float sand=x> -81&&x< -49&&z> -57&&z< -41?.94f:0;
            grass*=1-soil*.46f;
            grass=Smooth(.12f,.54f,grass)*.86f;
            float gravel=Mathf.Max(.045f,1-grass-soil-sand),sum=gravel+grass+soil+sand;
            return new Cover{gravel=gravel/sum,grass=grass/sum,soil=soil/sum,sand=sand/sum,shelter=shelter,damp=damp,wear=wear};
        }
        static float Hollow(float x,float z,float cx,float cz,float rx,float rz)
        {
            float q=(x-cx)*(x-cx)/(rx*rx)+(z-cz)*(z-cz)/(rz*rz);
            return Mathf.Pow(Mathf.Max(0,1-q),2);
        }
    }
}
