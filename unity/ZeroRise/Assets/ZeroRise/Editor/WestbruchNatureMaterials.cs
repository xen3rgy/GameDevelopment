using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchNatureMaterials
    {
        public const string Folder="Assets/ZeroRise/Art/Nature2K/";
        static bool prepared;
        public static Material Bark,Leaves,FallenLeaves,ShrubBark,ShrubLeaves;
        public static void Prepare()
        {
            if(prepared)return;
            foreach(string id in new[]{"Bark","Leaves","Earth"})
            {
                string path=Folder+id+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=id=="Leaves";importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=id=="Leaves";importer.alphaTestReferenceValue=.34f;
                importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.ToLarger;importer.isReadable=true;
                importer.wrapMode=id=="Leaves"?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Texture2D packed=id=="Leaves"?PackLeaves(source):null;
                var working=packed?packed:source;int width=working.width,height=working.height;var pixels=working.GetPixels32();
                var heights=new Color32[pixels.Length];var masks=new Color32[pixels.Length];
                for(int i=0;i<pixels.Length;i++)
                {
                    var c=pixels[i];byte value=(byte)Mathf.Clamp((c.r*.25f+c.g*.6f+c.b*.15f),0,255);
                    heights[i]=new Color32(value,value,value,255);
                    masks[i]=new Color32(0,(byte)(190+value*.25f),value,(byte)(id=="Leaves"?58:id=="Bark"?27:24));
                }
                Write(id+"-height",width,height,heights,true,id=="Leaves"?.018f:id=="Bark"?.09f:.045f);
                Write(id+"-mask",width,height,masks,false,0);
                if(packed)Object.DestroyImmediate(packed);
                importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            Bark=Surface("Nature_Bark","Bark",Color.white,false,.14f,"_ZR_TREE");
            Leaves=Surface("Nature_Leaves","Leaves",new Color(.94f,.98f,.90f),true,.23f,"_ZR_TREE");
            FallenLeaves=Surface("Nature_FallenLeaves","Leaves",new Color(.62f,.34f,.21f),true,.10f,null);
            ShrubBark=Surface("Nature_ShrubBark","Bark",new Color(.86f,.84f,.77f),false,.12f,"_ZR_SHRUB");
            ShrubLeaves=Surface("Nature_ShrubLeaves","Leaves",new Color(.84f,.88f,.70f),true,.18f,"_ZR_SHRUB");
            prepared=true;
        }
        static Texture2D PackLeaves(Texture2D source)
        {
            // Technical atlas packing preserves the generated pixels and gives
            // every species a real transparent gutter. The generated sheet did
            // not honour the requested tile padding; cropping UVs alone cuts lobes.
            const int size=2048,tile=1024,padding=80;
            int sw=source.width,sh=source.height;var input=source.GetPixels32();var output=new Color32[size*size];
            for(int row=0;row<2;row++)for(int col=0;col<2;col++)
            {
                int x0=col*sw/2,y0=row*sh/2,x1=(col+1)*sw/2,y1=(row+1)*sh/2;
                int left=x1,right=x0,bottom=y1,top=y0;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(input[y*sw+x].a>86)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
                int w=right-left+1,h=top-bottom+1;float scale=Mathf.Min((tile-padding*2f)/w,(tile-padding*2f)/h);
                int dw=Mathf.RoundToInt(w*scale),dh=Mathf.RoundToInt(h*scale),ox=col*tile+(tile-dw)/2,oy=row*tile+(tile-dh)/2;
                for(int y=0;y<dh;y++)for(int x=0;x<dw;x++)
                {
                    int sx=Mathf.Clamp(left+Mathf.FloorToInt((x+.5f)/scale),left,right),sy=Mathf.Clamp(bottom+Mathf.FloorToInt((y+.5f)/scale),bottom,top);
                    output[(oy+y)*size+ox+x]=input[sy*sw+sx];
                }
            }
            var image=new Texture2D(size,size,TextureFormat.RGBA32,false);image.SetPixels32(output);image.Apply();
            string path=Folder+"Leaves-Atlas.png";File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.34f;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return image;
        }
        static void Write(string id,int width,int height,Color32[] colors,bool normal,float scale)
        {
            var image=new Texture2D(width,height,TextureFormat.RGBA32,false,true);image.SetPixels32(colors);image.Apply();
            string path=Folder+id+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=false;
            importer.convertToNormalmap=normal;importer.heightmapScale=scale;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=true;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.wrapMode=id.StartsWith("Leaves")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.SaveAndReimport();
        }
        static Material Surface(string name,string image,Color tint,bool cutout,float smoothness,string motion)
        {
            var mat=Material(name,null,tint,smoothness);
            mat.shader=Shader.Find("HDRP/Lit");mat.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+(image=="Leaves"?"Leaves-Atlas":image)+".png"));
            mat.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+image+"-height.png"));mat.SetFloat("_NormalScale",image=="Leaves"?.32f:.65f);
            mat.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+image+"-mask.png"));
            mat.SetFloat("_AlphaCutoffEnable",cutout?1:0);mat.SetFloat("_AlphaCutoff",.34f);mat.SetFloat("_AlphaCutoffShadow",.34f);
            mat.SetFloat("_DoubleSidedEnable",cutout?1:0);mat.SetFloat("_DoubleSidedNormalMode",0);
            HDMaterial.ValidateMaterial(mat);
            mat.DisableKeyword("_ZR_TREE");mat.DisableKeyword("_ZR_SHRUB");mat.DisableKeyword("_ZR_FOLIAGE");
            if(motion!=null){mat.shader=Shader.Find("ZeroRise/UrbanLit");mat.EnableKeyword(motion);}
            EditorUtility.SetDirty(mat);return mat;
        }
    }
}
