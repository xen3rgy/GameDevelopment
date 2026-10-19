using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    internal static class WestbruchSignage
    {
        public const string Art="Assets/ZeroRise/Art/Signs/";
        public static readonly List<Transform> Created=new List<Transform>();
        static Material frame=>Material("Signage_AgedGalvanized",null,new Color(.34f,.36f,.32f),.25f,.65f);
        static Material screws=>Material("Signage_ScrewSteel",null,new Color(.23f,.25f,.23f),.3f,.7f);
        public static Material FaceMaterial(string key)
        {
            var mat=Material("Signage_Face_"+key,null,Color.white,.18f,.12f);mat.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key+".png"));HDMaterial.ValidateMaterial(mat);EditorUtility.SetDirty(mat);return mat;
        }
        public static Transform Plate(string key,string text,Transform parent,Vector3 at,Vector2 size,Transform support,string mount,float yaw=0,bool round=false,bool collision=false)
        {
            var root=Group("Mounted sign - "+key,parent,at,yaw).transform;Created.Add(root);
            var meta=root.gameObject.AddComponent<MountedSign>();meta.faceKey=key;meta.readableText=text;meta.mount=mount;meta.size=size;meta.support=support;
            if(round)
            {
                var back=Cylinder("Pressed circular steel plate",root,Vector3.zero,size.x*.5f,.028f,frame);back.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            else Box("Folded metal sign plate",root,Vector3.zero,new Vector3(size.x,size.y,.028f),frame,collision);
            float depth=mount.Contains("Facade")||mount.Contains("fascia")||mount.Contains("facade")?.15f:mount.Contains("Brickwork")?.09f:.05f;
            foreach(int side in new[]{-1,1}) Box("Rear mounting spacer",root,new Vector3(side*size.x*.32f,0,.014f+depth*.5f),new Vector3(.024f,.024f,depth),frame,false);
            var mesh=new Mesh();var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            if(round)
            {
                vertices.Add(new Vector3(0,0,-.016f));uv.Add(new Vector2(.5f,.5f));
                for(int i=0;i<=64;i++){float a=-i*Mathf.PI/32;vertices.Add(new Vector3(Mathf.Cos(a)*size.x*.5f,Mathf.Sin(a)*size.y*.5f,-.016f));uv.Add(new Vector2(.5f+Mathf.Cos(a)*.5f,.5f+Mathf.Sin(a)*.5f));if(i>0)indices.AddRange(new[]{0,i,i+1});}
            }
            else
            {
                vertices.AddRange(new[]{new Vector3(-size.x*.5f,-size.y*.5f,-.016f),new Vector3(-size.x*.5f,size.y*.5f,-.016f),new Vector3(size.x*.5f,size.y*.5f,-.016f),new Vector3(size.x*.5f,-size.y*.5f,-.016f)});
                uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});indices.AddRange(new[]{0,1,2,0,2,3});
            }
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            string path=Generated+"/Meshes/SignFace_"+key+"_"+size.x.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+"_"+size.y.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var face=Group("Printed enamel face",root,Vector3.zero);face.isStatic=true;face.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=face.AddComponent<MeshRenderer>();renderer.sharedMaterial=FaceMaterial(key);renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(!round)foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {
                float radius=Mathf.Min(.008f,size.y*.025f);var bolt=Cylinder("Recessed mounting screw",root,new Vector3(x*(size.x*.5f-.025f),y*(size.y*.5f-.025f),-.019f),radius,.006f,screws);bolt.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            return root;
        }
        public static Transform Post(Transform parent,Vector3 floor)
        {
            var root=Group("Bolted street signpost",parent,floor).transform;
            Cylinder("Galvanized support pole",root,new Vector3(0,1.45f,0),.035f,2.9f,frame);
            var collision=root.gameObject.AddComponent<CapsuleCollider>();collision.radius=.045f;collision.height=2.9f;collision.center=Vector3.up*1.45f;
            Cylinder("Pole cap",root,new Vector3(0,2.91f,0),.041f,.025f,frame);
            return root;
        }
        public static void ImportTextures()
        {
            foreach(string path in System.IO.Directory.GetFiles(Art,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));if(!importer)continue;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.sRGBTexture=true;importer.SaveAndReimport();
            }
        }
    }
}
