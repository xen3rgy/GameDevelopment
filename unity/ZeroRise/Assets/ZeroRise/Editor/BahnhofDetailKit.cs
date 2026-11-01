using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;
namespace ZeroRise.Editor
{
    internal static class BahnhofDetailKit
    {
        public static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/"+name+".mat");
        public static Transform Plate(Transform parent,string text,Vector3 localPosition,float yaw,float width,float height,Material board,Color ink)
        {
            var root=Group("BV2_Mounted plate "+text,parent,localPosition,yaw).transform;
            Box("BV2_Sign backing",root,Vector3.zero,new Vector3(width,height,.055f),board,false);
            var lettering=Group("BV2_Fitted lettering",root,new Vector3(0,0,-.034f));
            var label=lettering.AddComponent<TextMesh>();label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=96;label.characterSize=.1f;label.color=ink;label.text=text;
            label.font.RequestCharactersInTexture(text,96,FontStyle.Normal);
            const string path=Generated+"/Materials/BV2_Lettering.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("ZeroRise/WorldLettering"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_MainTex",label.font.material.mainTexture);EditorUtility.SetDirty(material);
            var r=lettering.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            lettering.AddComponent<WorldLabel>();
            var fitted=root.gameObject.AddComponent<FittedWorldSign>();fitted.boardWidth=width;fitted.boardHeight=height;fitted.lettering=label;fitted.Refit();
            var screw=Load("Signage_ScrewSteel")??board;
            foreach(float x in new[]{-1f,1})foreach(float y in new[]{-1f,1})
            {var bolt=Cylinder("BV2_Sign screw",root,new Vector3(x*(width*.5f-.035f),y*(height*.5f-.035f),-.031f),.007f,.006f,screw);bolt.transform.localRotation=Quaternion.Euler(90,0,0);}
            return root;
        }
        // Ridge along local X, eaves along local Z. Gable faces occupy the two X ends.
        public static void GableRoof(Transform parent,string id,float width,float depth,float eave,float rise,Material roof,Material gable)
        {
            float x=width*.5f,z=depth*.5f;
            var vertices=new[]{new Vector3(-x,eave,-z),new Vector3(x,eave,-z),new Vector3(-x,eave,z),new Vector3(x,eave,z),new Vector3(-x,eave+rise,0),new Vector3(x,eave+rise,0)};
            Mesh("BV2_Roof_"+id,parent,vertices,new[]{0,4,1,1,4,5,4,2,5,5,2,3},roof,true);
            Mesh("BV2_Gables_"+id,parent,vertices,new[]{0,2,4,1,5,3},gable,true);
            foreach(float side in new[]{-1f,1})Box("BV2_Roof fascia",parent,new Vector3(0,eave-.055f,side*z),new Vector3(width,.14f,.10f),roof,false);
            Box("BV2_Ridge cap",parent,new Vector3(0,eave+rise+.015f,0),new Vector3(width+.06f,.11f,.20f),roof,false);
        }
        public static GameObject Mesh(string id,Transform parent,IEnumerable<Vector3> source,IEnumerable<int> indices,Material material,bool collision)
        {
            var points=source.ToArray();var tris=indices.ToArray();var flat=tris.Select(i=>points[i]).ToArray();
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.vertices=flat;mesh.triangles=Enumerable.Range(0,flat.Length).ToArray();mesh.RecalculateNormals();
            var uv=new Vector2[flat.Length];for(int i=0;i<flat.Length;i++){var p=flat[i];var n=mesh.normals[i];uv[i]=(Mathf.Abs(n.y)>.65f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.65f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))/2.5f;}
            mesh.uv=uv;mesh.RecalculateTangents();mesh.RecalculateBounds();
            var key=new string((parent.name+"_"+id).Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());
            string path=Generated+"/Meshes/"+key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            var go=Group(id,parent,Vector3.zero);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
        }
    }
}
