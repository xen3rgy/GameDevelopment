using System.IO;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class WeatherSurfaceAssets
    {
        public static Material Prepare()
        {
            NightSkyAssets.Prepare();
            const string folder="Assets/ZeroRise/Settings/Weather/";
            Directory.CreateDirectory(folder);
            const string texturePath=folder+"WaterRipples.png";
            if(!File.Exists(texturePath))
            {
                const int size=128;
                var texture=new Texture2D(size,size,TextureFormat.RGB24,false,true);
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=x/(float)size*Mathf.PI*2,v=y/(float)size*Mathf.PI*2;
                    float dx=Mathf.Cos(u*4+Mathf.Sin(v*3))*.12f+Mathf.Cos(u*9-v*7)*.04f;
                    float dy=Mathf.Cos(v*3+Mathf.Sin(u*4))*.12f-Mathf.Cos(u*9-v*7)*.04f;
                    var n=new Vector3(-dx,-dy,1).normalized;
                    texture.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
                }
                texture.Apply();File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;
                importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            }
            string path=folder+"Puddle.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("HDRP/Lit");
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;
            material.SetFloat("_SurfaceType",1);material.SetFloat("_BlendMode",0);
            material.SetFloat("_EnableBlendModePreserveSpecularLighting",1);
            material.SetFloat("_TransparentDepthPrepassEnable",1);
            material.SetFloat("_ReceivesSSRTransparent",1);
            material.SetFloat("_Smoothness",.96f);material.SetFloat("_Metallic",0);
            material.SetFloat("_DoubleSidedEnable",1);material.SetFloat("_NormalScale",.25f);
            material.SetColor("_BaseColor",new Color(.06f,.085f,.10f,.24f));
            material.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            HDShaderUtils.ResetMaterialKeywords(material);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();return material;
        }
    }
}
