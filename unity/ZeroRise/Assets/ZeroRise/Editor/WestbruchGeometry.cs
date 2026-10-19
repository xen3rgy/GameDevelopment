using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Geometry and materials are baked as assets, never rebuilt during gameplay.
    internal static class WestbruchGeometry
    {
        public const string Generated = "Assets/ZeroRise/Generated";
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static void Clear() { meshes.Clear(); materials.Clear(); }
        public static Color Tint(string html) { ColorUtility.TryParseHtmlString(html, out var c); return c; }
        public static Material Material(string id, string surface, Color tint, float smoothness = .15f, float metallic = 0)
        {
            if (materials.TryGetValue(id, out var cached)) return cached;
            string path = Generated + "/Materials/" + id + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smoothness); mat.SetFloat("_Metallic", metallic);
            if (!string.IsNullOrEmpty(surface))
            {
                string folder=System.IO.File.Exists("Assets/ZeroRise/Art/SurfacesV3/"+surface+".jpg")?"Assets/ZeroRise/Art/SurfacesV3/":"Assets/ZeroRise/Art/Surfaces/";
                mat.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + surface + ".jpg"));
                string normal=folder+surface+(folder.Contains("V3")?"-height.png":"-height.jpg");
                mat.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                mat.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+surface+"-mask.png"));
                mat.SetFloat("_NormalScale", .25f); mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);
            EditorUtility.SetDirty(mat); materials[id] = mat; return mat;
        }
        public static GameObject Group(string name, Transform parent, Vector3 position, float yaw = 0)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localRotation = Quaternion.Euler(0, yaw, 0); return obj;
        }
        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material, bool collision = true, float tile = 3)
        {
            string key = FormattableString.Invariant($"{size.x:0.###}_{size.y:0.###}_{size.z:0.###}_{tile:0.###}");
            if (!meshes.TryGetValue(key, out var mesh))
            {
                string path = Generated + "/Meshes/Box_" + key + ".asset";
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (!mesh)
                {
                    mesh = CreateBox(size, tile); mesh.name = key; AssetDatabase.CreateAsset(mesh, path);
                }
                var vertices=mesh.vertices;var normals=mesh.normals;var mapped=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=vertices[i];var normal=normals[i];
                    mapped[i]=Mathf.Abs(normal.y)>.9f?new Vector2(p.x,p.z)/tile:Mathf.Abs(normal.x)>.9f?new Vector2(p.z,p.y)/tile:new Vector2(p.x,p.y)/tile;
                }
                mesh.uv=mapped;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
                meshes[key] = mesh;
            }
            var obj = Group(name, parent, center);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) obj.AddComponent<BoxCollider>().size = size;
            obj.isStatic = true; return obj;
        }
        static Mesh CreateBox(Vector3 size, float tile)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            var h = size * .5f;
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int n = v.Count; v.AddRange(new[] { a, b, c, d });
                var normal=Vector3.Cross(b-a,c-a).normalized;
                foreach(var p in new[]{a,b,c,d})
                    uv.Add(Mathf.Abs(normal.y)>.9f ? new Vector2(p.x,p.z)/tile : Mathf.Abs(normal.x)>.9f ? new Vector2(p.z,p.y)/tile : new Vector2(p.x,p.y)/tile);
                t.AddRange(new[] { n, n+1, n+2, n, n+2, n+3 });
            }
            Face(new Vector3(-h.x,-h.y,-h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,-h.y,-h.z));
            Face(new Vector3(h.x,-h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z),new Vector3(-h.x,-h.y,h.z));
            Face(new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,-h.y,-h.z));
            Face(new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(h.x,-h.y,h.z));
            Face(new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(h.x,h.y,-h.z));
            Face(new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z));
            var mesh = new Mesh { vertices = v.ToArray(), uv = uv.ToArray(), triangles = t.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        public static GameObject Cylinder(string name, Transform parent, Vector3 p, float radius, float height, Material mat, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = p;
            obj.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.isStatic = true; return obj;
        }
        public static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float width, Material mat, bool collision = false)
        {
            var obj = Box(name, parent, (a+b)*.5f, new Vector3(width, Vector3.Distance(a,b), width), mat, collision);
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b-a).normalized);
        }
        public static void Sign(string text, Transform parent, Vector3 p, float width, Material backing, Color color)
        {
            Box("Sign backing", parent, p, new Vector3(width, .55f, .1f), backing, false);
            var obj = Group(text, parent, p + Vector3.back * .061f);
            var label = obj.AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
            label.fontSize = 64; label.characterSize = Mathf.Min(.13f, width / Mathf.Max(1, text.Length) / .65f); label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string key="SignFont";
            if(!materials.TryGetValue(key,out var fontMaterial))
            {
                string path=Generated+"/Materials/SignFont.mat";
                fontMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!fontMaterial) { fontMaterial=new Material(Shader.Find("ZeroRise/WorldLettering"));AssetDatabase.CreateAsset(fontMaterial,path); }
                fontMaterial.shader=Shader.Find("ZeroRise/WorldLettering");
                fontMaterial.SetTexture("_MainTex",label.font.material.mainTexture);
                materials[key]=fontMaterial;EditorUtility.SetDirty(fontMaterial);
            }
            obj.GetComponent<MeshRenderer>().sharedMaterial=fontMaterial;
            obj.AddComponent<WorldLabel>();
        }
    }
}
