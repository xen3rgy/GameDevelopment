using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    // One authored alignment drives terrain grading, surfacing, vegetation clearance
    // and the standalone traversal audit. Heights are metres above city datum.
    public static class WestbruchLakeAccess
    {
        public static readonly Vector3[] Road = Smooth(new[] {
            new Vector3(320,.02f,176), new Vector3(350,.15f,176), new Vector3(389,2.2f,184),
            new Vector3(430,5.6f,201),new Vector3(470,9.2f,222),new Vector3(505,13,249),
            new Vector3(531,16.5f,281),new Vector3(548,18.3f,311),new Vector3(563,18.4f,340)});
        public static readonly Vector3[] Footpath = Smooth(new[] {
            new Vector3(270,.12f,306),new Vector3(299,-.02f,306),new Vector3(331,.5f,310),
            new Vector3(365,2.4f,322),new Vector3(406,5.6f,340),new Vector3(450,9,355),
            new Vector3(489,12.8f,375),new Vector3(525,16.6f,381),new Vector3(551,18.4f,371),
            new Vector3(574,18.3f,358),new Vector3(593,16.5f,353),new Vector3(611,15.15f,354)});
        public static readonly Vector3[] ShorePath = Smooth(new[] {
            new Vector3(611,15.15f,354),new Vector3(599,15.8f,361),new Vector3(589,16,377),
            new Vector3(588,16,388),new Vector3(593,16.2f,402),new Vector3(605,16.4f,416)});
        public static readonly Vector3 Parking = new Vector3(566,18.4f,344);
        public static Vector3[] Smooth(Vector3[] controls)
        {
            var result = new List<Vector3>();
            for(int i=0;i<controls.Length-1;i++)
            {
                var a=controls[Mathf.Max(0,i-1)];var b=controls[i];var c=controls[i+1];var d=controls[Mathf.Min(i+2,controls.Length-1)];
                int count=Mathf.CeilToInt(Vector3.Distance(b,c)/1.5f);
                for(int j=0;j<count;j++) {float t=j/(float)count;
                    result.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}
            }
            result.Add(controls[controls.Length-1]);return result.ToArray();
        }
        public static float Distance(Vector3[] route,Vector2 p,out float height)
        {
            float best=float.MaxValue;height=0;
            for(int i=1;i<route.Length;i++)
            {
                Vector2 a=new Vector2(route[i-1].x,route[i-1].z),b=new Vector2(route[i].x,route[i].z);
                float t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);float sq=(p-Vector2.Lerp(a,b,t)).sqrMagnitude;
                if(sq<best){best=sq;height=Mathf.Lerp(route[i-1].y,route[i].y,t);}
            }
            return Mathf.Sqrt(best);
        }
        public static float ParkingRadius(Vector2 p) => new Vector2((p.x-Parking.x)/12,(p.y-Parking.z)/14).magnitude;
        public static float BayRadius(Vector2 p) => new Vector2((p.x-612)/17,(p.y-354)/17).magnitude;
        public static bool Cleared(Vector2 p,float margin=0)
        {
            if(p.x<268||p.x>675||p.y<160||p.y>440)return false;
            return Distance(Road,p,out _) < 3.3f+margin || Distance(Footpath,p,out _) < 1.7f+margin ||
                Distance(ShorePath,p,out _) < 1.5f+margin || ParkingRadius(p)<1.05f+margin/12 || BayRadius(p)<1+margin/17 || Vector2.Distance(p,new Vector2(600,406))<6+margin;
        }
        public static float Grade(float x,float z,float original)
        {
            if(x<300||x>680||z<155||z>450)return original;
            var p=new Vector2(x,z);float result=original;
            void Blend(Vector3[] route,float inner,float outer)
            {float distance=Distance(route,p,out float h);result=Mathf.Lerp(result,h,1-WestbruchLandscapeProfile.Fade(inner,outer,distance));}
            Blend(Road,5,15);Blend(Footpath,2.6f,10);Blend(ShorePath,2.2f,8);
            result=Mathf.Lerp(result,Parking.y,1-WestbruchLandscapeProfile.Fade(1,1.6f,ParkingRadius(p)));
            // The arrival cove slopes continuously into the water; no raised slab.
            float beach=15.25f-(x-610)*.105f + Mathf.Sin(z*.27f)*.075f;
            result=Mathf.Lerp(result,beach,1-WestbruchLandscapeProfile.Fade(.7f,1.35f,BayRadius(p)));
            return result;
        }
    }
}
