using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchCourtyardDetails
    {
        static Bounds shrubFootprint;
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            var root=Group("Courtyard use – shared seating and planted edges",parent,Vector3.zero).transform;
            var timber=Material("YardWeatheredWood","wood",new Color(.67f,.65f,.56f),.08f);
            var steel=Material("YardOldSteel","steel",new Color(.24f,.29f,.28f),.18f,.55f);
            // A worn desire path joins the existing west courtyard walk. The
            // small seating footprint is flattened by the terrain authoring pass.
            var spot=Group("Residents shared sitting corner",root,new Vector3(-11,WestbruchLandscape.Ground(-11,-58),-58)).transform;
            var table=Group("Worn communal picnic table",spot,new Vector3(1,0,0)).transform;
            for(int plank=0;plank<6;plank++)Box("Table top plank",table,new Vector3(0,.77f,(plank-2.5f)*.155f),new Vector3(1.8f,.045f,.145f),timber,false,.65f);
            var top=table.gameObject.AddComponent<BoxCollider>();top.center=new Vector3(0,.76f,0);top.size=new Vector3(1.8f,.08f,.94f);
            foreach(int side in new[]{-1,1})
            {
                Box("Steel table leg",table,new Vector3(side*.63f,.37f,0),new Vector3(.055f,.74f,.67f),steel,false);
                Box("Worn picnic seat",table,new Vector3(0,.43f,side*.76f),new Vector3(1.9f,.065f,.27f),timber);
                Box("Seat support",table,new Vector3(side*.63f,.22f,side*.62f),new Vector3(.055f,.44f,.40f),steel,false);
            }
            WestbruchProps.Bench(root,new Vector3(-13,WestbruchLandscape.Ground(-13,-56.5f),-56.5f),180);
            WestbruchProps.Bin(root,new Vector3(-14.2f,WestbruchLandscape.Ground(-14.2f,-59.2f),-59.2f),0);
            // Isolated vegetation clumps give the large forecourts readable edges
            // without a uniform hedge, and never occupy driving/walking surfaces.
            Physics.SyncTransforms();
            var occupied=parent.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!(c is TerrainCollider)).Select(c=>c.bounds).ToArray();
            var bush=BushMesh(0);var footprint=shrubFootprint;var farBush=BushMesh(1);int placed=0;
            var centres=new[]{new Vector2(-25,-69),new Vector2(-17,-52),new Vector2(8,-48),new Vector2(-30,-60),
                new Vector2(-52,35),new Vector2(-18,47),new Vector2(8,46),new Vector2(34,31),new Vector2(75,32),new Vector2(105,48),
                new Vector2(-106,-13),new Vector2(-106,48),new Vector2(-106,-94),new Vector2(-106,91),
                new Vector2(-83,94),new Vector2(-45,96),new Vector2(5,95),new Vector2(-45,-101),new Vector2(5,-103),
                new Vector2(34,12),new Vector2(34,-25),new Vector2(80,-10),new Vector2(79,-94),new Vector2(78,94),
                new Vector2(118,34),new Vector2(120,-19),new Vector2(118,-93),new Vector2(121,91),new Vector2(116,-46),
                // Soft colonies break up the newly covered soil, including the
                // canal corner, housing gaps and south-ring forecourts.
                new Vector2(-106,-63),new Vector2(-106,-37),new Vector2(-105,-105),
                new Vector2(-82,-91),new Vector2(-8,-91),new Vector2(36,-91),new Vector2(76,-71),
                new Vector2(-54,-31),new Vector2(-22,34),new Vector2(7,34),new Vector2(36,68),
                new Vector2(-82,48),new Vector2(77,34),new Vector2(117,1),new Vector2(119,-63)};
            foreach(var centre in centres)foreach(var offset in new[]{Vector2.zero,new Vector2(1.15f,.65f)})
            {
                var p=centre+offset;
                float h=WestbruchLandscape.Ground(p.x,p.y);var position=new Vector3(p.x,h-.018f,p.y);
                float scale=.74f+placed%4*.08f;var rotation=Quaternion.Euler(0,placed*137.5f,0);var matrix=Matrix4x4.TRS(position,rotation,Vector3.one*scale);
                var source=footprint;var bounds=new Bounds(matrix.MultiplyPoint3x4(source.center),Vector3.zero);
                foreach(int sx in new[]{-1,1})foreach(int sy in new[]{-1,1})foreach(int sz in new[]{-1,1})bounds.Encapsulate(matrix.MultiplyPoint3x4(source.center+Vector3.Scale(source.extents,new Vector3(sx,sy,sz))));
                float radius=Mathf.Max(Mathf.Abs(bounds.min.x-p.x),Mathf.Abs(bounds.max.x-p.x),Mathf.Abs(bounds.min.z-p.y),Mathf.Abs(bounds.max.z-p.y));
                if(!WestbruchLandscape.OnEarth(p.x,p.y,radius+.05f)||occupied.Any(b=>b.Intersects(bounds)))continue;
                if(!new[]{Vector3.right,Vector3.forward,new Vector3(.7071f,0,.7071f),new Vector3(.7071f,0,-.7071f)}.Any(axis=>!Physics.CheckBox(position+Vector3.up*.98f,new Vector3(.32f,.60f,1.85f),Quaternion.LookRotation(axis),1,QueryTriggerInteraction.Ignore)))continue;
                var obj=Group("Dense low courtyard shrub "+placed,root,position,placed*137.5f);obj.transform.localScale=Vector3.one*scale;obj.isStatic=false;
                obj.AddComponent<MeshFilter>().sharedMesh=bush;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{WestbruchNatureMaterials.ShrubBark,WestbruchNatureMaterials.ShrubLeaves};
                var distant=Group("Shrub far detail",obj.transform,Vector3.zero);distant.AddComponent<MeshFilter>().sharedMesh=farBush;
                var reduced=distant.AddComponent<MeshRenderer>();reduced.sharedMaterials=renderer.sharedMaterials;
                var lod=obj.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;
                lod.SetLODs(new[]{new LOD(.075f,new Renderer[]{renderer}),new LOD(.009f,new Renderer[]{reduced})});lod.RecalculateBounds();
                // Soft undergrowth never blocks the player, wheels or camera.
                // Keep the object transform: the shader bends about this root.
                placed++;
            }
            Debug.Log("Purposeful courtyard details: shared seating, connected worn access, "+placed+" dense low shrubs.");
        }
        static Mesh BushMesh(int lod)
        {
            string path=Generated+"/Meshes/"+(lod==0?"LowCourtyardShrub":"LowCourtyardShrubFar")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            var source=WestbruchTreeMeshes.Get(0,1);var sourceVertices=source.vertices;var sourceUV=source.uv;var sourceColors=source.colors;
            var sourceBark=source.GetTriangles(0);var sourceLeaves=source.GetTriangles(1);
            var foliage=new Bounds(sourceVertices[sourceLeaves[0]],Vector3.zero);
            foreach(int index in sourceLeaves)foliage.Encapsulate(sourceVertices[index]);
            float sourceRadius=Mathf.Max(foliage.extents.x,foliage.extents.z);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();
            var bark=new List<int>();var leaves=new List<int>();
            // Map the foliage envelope, rather than the whole tree height, to
            // shrub scale. Leaders sink beneath the soil and exposed forks form
            // a coppiced multi-stem base, with leaves starting near the ground.
            for(int stem=0;stem<2;stem++)
            {
                int start=vertices.Count;float low=stem==0?.10f:.14f,high=stem==0?.83f:.90f;
                float sy=(high-low)/Mathf.Max(.01f,foliage.size.y),sx=(stem==0?.46f:.50f)/Mathf.Max(.01f,sourceRadius);
                var offset=stem==0?new Vector3(-.15f,0,.06f):new Vector3(.14f,0,-.10f);
                var rotation=Quaternion.Euler(0,stem*143.5f,0);
                for(int index=0;index<sourceVertices.Length;index++)
                {
                    var original=sourceVertices[index];
                    var p=new Vector3((original.x-foliage.center.x)*sx,(original.y-foliage.min.y)*sy+low,(original.z-foliage.center.z)*sx);
                    p=rotation*p+offset;p.y=Mathf.Max(-.022f,p.y);
                    vertices.Add(p);uv.Add(sourceUV[index]);colors.Add(sourceColors.Length==sourceVertices.Length?sourceColors[index]:Color.white);
                }
                for(int triangle=0;triangle<sourceBark.Length;triangle+=3)
                {
                    int a=start+sourceBark[triangle],b=start+sourceBark[triangle+1],c=start+sourceBark[triangle+2];
                    if(Mathf.Max(vertices[a].y,vertices[b].y,vertices[c].y)<.015f)continue;
                    bark.Add(a);bark.Add(b);bark.Add(c);
                }
                // Remove complete, distributed leaf cards, never partial faces.
                // Two overlapping foliage lobes remain dense below60k triangles.
                for(int card=0;card<sourceLeaves.Length/12;card++)
                {
                    if((card+stem*3)%7==4||(lod==1&&card%3!=0))continue;
                    for(int corner=0;corner<12;corner++)leaves.Add(start+sourceLeaves[card*12+corner]);
                }
            }
            if((bark.Count+leaves.Count)/3>60000)throw new System.InvalidOperationException("Courtyard shrub exceeds60k triangle budget.");
            mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.subMeshCount=2;
            mesh.SetTriangles(bark,0);mesh.SetTriangles(leaves,1);mesh.RecalculateNormals();mesh.RecalculateTangents();
            mesh.name="Low courtyard shrub asset";mesh.RecalculateBounds();shrubFootprint=mesh.bounds;
            var windBounds=shrubFootprint;windBounds.Expand(new Vector3(1.6f,.3f,1.6f));mesh.bounds=windBounds;
            EditorUtility.SetDirty(mesh);return mesh;
        }
    }
}
