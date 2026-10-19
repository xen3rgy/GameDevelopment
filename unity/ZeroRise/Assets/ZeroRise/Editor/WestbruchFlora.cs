using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchFlora
    {
        public const int MaximumPlants=68000,MaximumTriangles=5800000;
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            var root=Group("Ground flora – coherent dry verges and sheltered soil",parent,Vector3.zero).transform;
            var material=Material("DistrictFlora",null,new Color(.90f,.88f,.82f),.10f);
            material.shader=Shader.Find("HDRP/Lit");material.SetFloat("_DoubleSidedEnable",1);HDMaterial.ValidateMaterial(material);
            material.shader=Shader.Find("ZeroRise/UrbanLit");material.EnableKeyword("_ZR_FOLIAGE");EditorUtility.SetDirty(material);
            Physics.SyncTransforms();
            var blocked=parent.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&!(c is TerrainCollider)).Select(c=>c.bounds).ToList();
            foreach(var renderer in parent.GetComponentsInChildren<Renderer>())
                if(renderer.name.IndexOf("planter",StringComparison.OrdinalIgnoreCase)>=0||renderer.name=="Raised bed soil")blocked.Add(renderer.bounds);
            var terrain=Terrain.activeTerrain;var random=new System.Random(20261016);int count=0,triangles=0;
            var shrubs=new List<Vector2>();var used=new HashSet<Vector2Int>();
            var meshes=new Mesh[6,3];for(int kind=0;kind<6;kind++)for(int variant=0;variant<3;variant++)meshes[kind,variant]=WestbruchFloraMeshes.Get(kind,variant);
            float Range(float a,float b)=>a+(b-a)*(float)random.NextDouble();
            // Visit every soil tile before spending geometry on extra detail.
            // A shuffled stratified pass prevents early dense islands from
            // consuming the budget and leaving whole forecourts empty.
            const float spacing=2.8f;
            var sites=new List<Vector2>();
            for(float z=-109;z<109;z+=spacing)for(float x=-109;x<127;x+=spacing)
                sites.Add(new Vector2(x+Range(.15f,spacing-.15f),z+Range(.15f,spacing-.15f)));
            for(int i=sites.Count-1;i>0;i--){int j=random.Next(i+1);var swap=sites[i];sites[i]=sites[j];sites[j]=swap;}
            for(int pass=0;pass<3;pass++)
            for(int patch=0;patch<sites.Count&&count<MaximumPlants;patch++)
            {
                float cx=sites[patch].x,cz=sites[patch].y;
                if(!WestbruchLandscape.CanGrow(cx,cz,.16f))continue;
                var context=WestbruchGroundCover.Sample(cx,cz);
                if(context.sand>.5f)continue;
                float abundance=Mathf.Clamp01(context.grass*1.4f+context.shelter*.25f+.18f);
                float radius=Range(1.25f,2.05f),minor=radius*Range(.65f,1),angle=Range(0,Mathf.PI*2);
                int pieces=pass==0?Mathf.RoundToInt(Mathf.Lerp(15,24,abundance)):pass==1?Mathf.RoundToInt(Mathf.Lerp(2,4,abundance)):Mathf.RoundToInt(Mathf.Lerp(8,15,abundance));
                for(int piece=0;piece<pieces&&count<MaximumPlants;piece++)
                {
                    float a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(Range(0,1)),u=Mathf.Cos(a)*radius*r,v=Mathf.Sin(a)*minor*r;
                    float x=cx+Mathf.Cos(angle)*u-Mathf.Sin(angle)*v,z=cz+Mathf.Sin(angle)*u+Mathf.Cos(angle)*v;
                    if(pass==0&&piece==0){x=cx;z=cz;}
                    var cover=WestbruchGroundCover.Sample(x,z);
                    // Even a dense patch has compacted bare breaks. Rosettes
                    // colonise occasional gravel edges; tall weeds need shelter.
                    if(cover.sand>.5f||cover.wear>.68f)continue;
                    float bare= Mathf.PerlinNoise((x+351)*.47f,(z+785)*.47f);
                    if(bare<.28f&&Range(0,1)>.20f)continue;
                    float pick=Range(0,1);int kind=0;
                    if(pass==1)
                    {
                        if(pick<.10f&&cover.wear<.20f)kind=4;
                        else if(pick<.12f&&cover.grass>.38f&&cover.wear<.25f)kind=5;
                        else if(pick<.38f)kind=3;
                        else if(pick<.72f)kind=1;
                        else kind=2;
                    }
                    if(kind==4&&shrubs.Any(p=>(p-new Vector2(x,z)).sqrMagnitude<6.25f))continue;
                    var cell=new Vector2Int(Mathf.FloorToInt(x*7),Mathf.FloorToInt(z*7));if(used.Contains(cell))continue;
                    float scale=Range(.80f,1.18f);if(cover.wear>.4f)scale*=.75f;
                    if(kind==0)scale*=Mathf.Lerp(.87f,1.35f,abundance*(1-cover.wear));
                    var mesh=meshes[kind,random.Next(3)];
                    if(!WestbruchLandscape.CanGrow(x,z,.05f))continue;
                    float ground=WestbruchLandscape.Ground(x,z);
                    var position=new Vector3(x,ground-.016f,z);
                    float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x,nz=(z-terrain.transform.position.z)/terrain.terrainData.size.z;
                    var normal=terrain.terrainData.GetInterpolatedNormal(nx,nz);
                    var rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Lerp(Vector3.up,normal,.7f))*Quaternion.Euler(0,Range(0,360),0);
                    var size=new Vector3(scale*Range(.83f,1.16f),scale,scale*Range(.83f,1.16f));
                    var matrix=Matrix4x4.TRS(position,rotation,size);var source=mesh.bounds;
                    var ax=matrix.MultiplyVector(Vector3.right*source.extents.x);var ay=matrix.MultiplyVector(Vector3.up*source.extents.y);var az=matrix.MultiplyVector(Vector3.forward*source.extents.z);
                    Vector3 Abs(Vector3 p)=>new Vector3(Mathf.Abs(p.x),Mathf.Abs(p.y),Mathf.Abs(p.z));
                    var bounds=new Bounds(matrix.MultiplyPoint3x4(source.center),(Abs(ax)+Abs(ay)+Abs(az))*2);
                    float clearance=Mathf.Max(Mathf.Abs(bounds.min.x-x),Mathf.Abs(bounds.max.x-x),Mathf.Abs(bounds.min.z-z),Mathf.Abs(bounds.max.z-z));
                    if(!WestbruchLandscape.OnEarth(x,z,clearance+.045f))continue;
                    bounds.Expand(.035f);if(blocked.Any(b=>b.Intersects(bounds)))continue;
                    int cost=(int)mesh.GetIndexCount(0)/3;
                    if(triangles+cost>MaximumTriangles||(pass==1&&triangles+cost>MaximumTriangles-1000000))continue;
                    var plant=Group("Ground flora "+kind+" · patch "+patch,root,position);
                    plant.transform.rotation=rotation;plant.transform.localScale=size;plant.isStatic=true;
                    plant.AddComponent<MeshFilter>().sharedMesh=mesh;plant.AddComponent<MeshRenderer>().sharedMaterial=material;
                    used.Add(cell);count++;triangles+=cost;if(kind==4)shrubs.Add(new Vector2(x,z));
                }
            }
            Debug.Log("Coherent ground flora baked: "+count+" plants / "+triangles+" triangles; one shared animated material, zero plant colliders.");
        }
    }
}
