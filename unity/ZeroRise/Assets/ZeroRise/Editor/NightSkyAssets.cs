using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class NightSkyAssets
    {
        public static void Prepare()
        {
            const string folder="Assets/ZeroRise/Resources";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            const string starsPath=folder+"/WestbruchStars.asset";
            var existingStars=AssetDatabase.LoadAssetAtPath<Cubemap>(starsPath);
            if(!existingStars||existingStars.name!="Westbruch clear-night stars v2")
            {
                const int size=512;var map=existingStars?existingStars:new Cubemap(size,TextureFormat.RGBAHalf,true);map.name="Westbruch clear-night stars v2";map.filterMode=FilterMode.Trilinear;map.wrapMode=TextureWrapMode.Clamp;
                var random=new System.Random(801733);
                var tones=new[]{new Color(.88f,.95f,1),new Color(1,.99f,.96f),new Color(1,.90f,.78f)};
                for(int face=0;face<6;face++)
                {
                    var pixels=new Color[size*size];
                    for(int star=0;star<650;star++)
                    {
                        float x=2+(float)random.NextDouble()*(size-5),y=2+(float)random.NextDouble()*(size-5);
                        float brightness=.65f+Mathf.Pow((float)random.NextDouble(),5)*10;
                        int type=random.Next(10);Color tint=tones[type<3?0:type<9?1:2];
                        for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                        {
                            int px=(int)x+dx,py=(int)y+dy;float distance=(px-x)*(px-x)+(py-y)*(py-y);
                            float profile=Mathf.Exp(-distance/ .20f);
                            pixels[py*size+px]+=tint*(brightness*profile);
                        }
                    }
                    map.SetPixels(pixels,(CubemapFace)face);
                }
                map.Apply(true,false);if(!existingStars)AssetDatabase.CreateAsset(map,starsPath);else EditorUtility.SetDirty(map);
            }
            const string moonPath=folder+"/WestbruchMoon.asset";
            if(!AssetDatabase.LoadAssetAtPath<Texture2D>(moonPath))
            {
                const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGB24,true,true){name="Subtle lunar surface",wrapMode=TextureWrapMode.Clamp};
                var random=new System.Random(7751);var craters=new Vector3[34];
                for(int i=0;i<craters.Length;i++)craters[i]=new Vector3((float)random.NextDouble(),(float)random.NextDouble(),.02f+(float)random.NextDouble()*.09f);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    var uv=new Vector2(x/(float)size,y/(float)size);float value=.67f+.09f*Mathf.Sin(uv.x*17+Mathf.Sin(uv.y*23))+.05f*Mathf.Sin(uv.y*37-uv.x*15);
                    foreach(var crater in craters){float r=Vector2.Distance(uv,new Vector2(crater.x,crater.y))/crater.z;value-=.17f*Mathf.Exp(-r*r*2);value+=.045f*Mathf.Exp(-Mathf.Pow((r-.85f)*9,2));}
                    texture.SetPixel(x,y,new Color(value,value,value));
                }
                texture.Apply(true,false);AssetDatabase.CreateAsset(texture,moonPath);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
