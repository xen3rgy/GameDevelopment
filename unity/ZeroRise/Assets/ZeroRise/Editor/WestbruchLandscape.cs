using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchLandscape
    {
        const string Art="Assets/ZeroRise/Art/SurfacesV3/";
        static WestbruchLayout layout;
        static Terrain terrain;
        static Plot[] walks;
        public static void InspectScene(WestbruchLayout plan)
        {layout=plan;terrain=Terrain.activeTerrain;walks=WestbruchBuilder.WalkPlan(plan).ToArray();}
        public static void SetWalks(IEnumerable<Plot> paths){walks=paths.ToArray();}
        static float RectDistance(Plot p,float x,float z,float margin=0)
            =>new Vector2(Mathf.Max(0,Mathf.Abs(x-p.x)-p.w*.5f-margin),Mathf.Max(0,Mathf.Abs(z-p.z)-p.d*.5f-margin)).magnitude;
        public static bool CanGrow(float x,float z,float radius=.35f)
        {
            if(WestbruchGroundCover.CourtyardPathDistance(x,z)<.7f+radius)return false;
            if(layout.roads.Any(p=>RectDistance(p,x,z,3.15f)<radius))return false;
            if((walks??layout.paths).Any(p=>RectDistance(p,x,z,.12f)<radius))return false;
            if(layout.buildings.Any(p=>RectDistance(p,x,z,1.15f)<radius))return false;
            if(layout.cars.Any(c=>Mathf.Abs(x-c.x)<(c.yaw==90?2.6f:1.35f)+radius&&Mathf.Abs(z-c.z)<(c.yaw==90?1.35f:2.6f)+radius))return false;
            if(x>-81&&x<-49&&z>-57&&z<-41)return false;
            if(WestbruchOverpassArchitecture.IsPaved(x,z,radius))return false;
            if(x>103.5f-radius&&x<133+radius&&(z<63+radius||z>97-radius))return false;
            if(x>112.7f&&x<121.3f&&z>66&&z<72.4f)return false;
            return true;
        }
        // The circulation plan is the cheap first filter; the real collider
        // below each footprint also excludes later concrete pads and props.
        public static bool OnEarth(float x,float z,float radius=.25f,Transform ignore=null)
        {
            if(x-radius< -110||x+radius>layout.width*.5f-3||Mathf.Abs(z)+radius>layout.depth*.5f-3||!CanGrow(x,z,radius))return false;
            for(int sample=0;sample<9;sample++)
            {
                float a=(sample-1)*Mathf.PI*.25f;
                float px=x+(sample==0?0:Mathf.Cos(a)*radius),pz=z+(sample==0?0:Mathf.Sin(a)*radius);
                float h=terrain.SampleHeight(new Vector3(px,0,pz))+terrain.transform.position.y;
                var origin=new Vector3(px,h+8,pz);
                if(!ignore)
                {if(!Physics.Raycast(origin,Vector3.down,out var hit,8.3f,1,QueryTriggerInteraction.Ignore)||!(hit.collider is TerrainCollider))return false;}
                else
                {
                    var contacts=Physics.RaycastAll(origin,Vector3.down,8.3f,1,QueryTriggerInteraction.Ignore).Where(hit=>!hit.collider.transform.IsChildOf(ignore)).OrderBy(hit=>hit.distance).ToArray();
                    if(contacts.Length==0||!(contacts[0].collider is TerrainCollider))return false;
                }
            }
            return true;
        }
        public static float Ground(float x,float z)
        {
            if(layout.roads.Any(p=>RectDistance(p,x,z)<.01f))return .04f;
            if((walks??layout.paths).Any(p=>RectDistance(p,x,z)<.01f)||layout.roads.Any(p=>RectDistance(p,x,z,3)<.01f))return .14f;
            return terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        }
        public static void TerrainGround(Transform parent,WestbruchLayout plan)
        {
            WestbruchNatureMaterials.Prepare();
            layout=plan;walks=WestbruchBuilder.WalkPlan(plan).ToArray();
            WestbruchGroundCover.Configure(plan,walks);
            string path=Generated+"/WestbruchTerrain.asset";
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if(!data){data=new TerrainData();AssetDatabase.CreateAsset(data,path);}
            float width=plan.width+200,depth=plan.depth+200;
            const int resolution=1025,alphaResolution=512;
            data.heightmapResolution=resolution;data.alphamapResolution=alphaResolution;data.size=new Vector3(width,4,depth);
            var heights=new float[resolution,resolution];
            var workPads=new[]{new Vector2(110,51),new Vector2(34,-44),new Vector2(-53,37),new Vector2(-60,44),new Vector2(-15,46),new Vector2(34,-62),new Vector2(-11,-58)};
            for(int iz=0;iz<resolution;iz++)for(int ix=0;ix<resolution;ix++)
            {
                float x=ix/(resolution-1f)*width-width*.5f,z=iz/(resolution-1f)*depth-depth*.5f;
                float clearance=100;
                foreach(var p in plan.roads)clearance=Mathf.Min(clearance,RectDistance(p,x,z,3.15f));
                foreach(var p in walks)clearance=Mathf.Min(clearance,RectDistance(p,x,z,.20f));
                foreach(var p in plan.buildings)clearance=Mathf.Min(clearance,RectDistance(p,x,z,.55f));
                float mask=Mathf.SmoothStep(0,1,Mathf.Clamp01(clearance/2.25f));
                float broad=(Mathf.PerlinNoise((x+271)*.035f,(z+157)*.035f)-.35f)*.38f;
                float folds=(Mathf.PerlinNoise((x+821)*.19f,(z+438)*.19f)-.5f)*.14f;
                float grit=(Mathf.PerlinNoise((x+412)*.73f,(z+614)*.73f)-.5f)*.024f;
                float relief=.085f+broad+folds+grit;
                relief+=Patch(x,z,61,31,6,4)*.34f+Patch(x,z,-56,35,7,4)*.30f+Patch(x,z,-16,-48,5,7)*.26f+Patch(x,z,-106,18,4,16)*.32f;
                relief-=Patch(x,z,73,30,5,7)*.19f+Patch(x,z,-45,34,5,3)*.15f;
                float h=-.025f+mask*relief;
                // Flat bearing pads support parked vehicles and existing yard
                // equipment; natural relief blends around their usable footprint.
                float bearing=0;
                foreach(var car in plan.cars)
                {
                    float dx=Mathf.Max(0,Mathf.Abs(x-car.x)-(car.yaw==90?2.8f:1.4f));
                    float dz=Mathf.Max(0,Mathf.Abs(z-car.z)-(car.yaw==90?1.4f:2.8f));
                    bearing=Mathf.Max(bearing,1-Mathf.SmoothStep(0,1,new Vector2(dx,dz).magnitude/1.1f));
                }
                h=Mathf.Lerp(h,.12f,bearing*mask);
                float workPad=0;
                foreach(var pad in workPads)
                {
                    float dx=Mathf.Max(0,Mathf.Abs(x-pad.x)-3.6f),dz=Mathf.Max(0,Mathf.Abs(z-pad.y)-2.0f);
                    workPad=Mathf.Max(workPad,1-Mathf.SmoothStep(0,1,new Vector2(dx,dz).magnitude/1.2f));
                }
                h=Mathf.Lerp(h,.105f,workPad*mask);
                // A native soil shoulder meets the paved courtyard access.
                // Blend the 18 cm kerb drop over a few metres so sprinting off
                // the walk follows continuous ground instead of a short fall.
                if(x>-36.7f&&x<-29.8f)
                {
                    float join=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-33.2f,-29.8f,x));
                    float widthMask=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.65f,WestbruchGroundCover.CourtyardPathDistance(x,z)));
                    h=Mathf.Lerp(h,.12f,join*widthMask);
                }
                if(x>-81&&x<-49&&z>-57&&z<-41)h=Mathf.Lerp(h,-.025f,Patch(x,z,-65,-49,17,9)*1.5f);
                if(x>70&&x<81&&z>-65&&z<-54)h=Mathf.Lerp(h,.115f,Patch(x,z,75,-60,7,7));
                if(Mathf.Abs(x)>plan.width*.5f+8||Mathf.Abs(z)>plan.depth*.5f+10)h=.08f+Mathf.PerlinNoise((x+800)*.014f,(z+310)*.014f)*.45f;
                // Actual canal depression lets the water sit below the banks.
                if(x<-113&&x>-131)h=Mathf.Lerp(h,-.76f,Mathf.SmoothStep(0,1,Mathf.Clamp01((-113-x)/3)));
                heights[iz,ix]=(h+1)/4;
            }
            data.SetHeights(0,0,heights);
            var names=new[]{"yard","grass","mud","sand"};var layers=new TerrainLayer[4];
            for(int i=0;i<names.Length;i++)
            {
                string layerPath=Generated+"/Urban_"+names[i]+".terrainlayer";
                var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if(!layer){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,layerPath);}
                layer.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(i==0?WestbruchNatureMaterials.Folder+"Earth.png":Art+names[i]+".jpg");
                layer.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(i==0?WestbruchNatureMaterials.Folder+"Earth-height.png":Art+names[i]+"-height.png");
                layer.maskMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(i==0?WestbruchNatureMaterials.Folder+"Earth-mask.png":Art+names[i]+"-mask.png");
                float tile=new[]{1.8f,.85f,1.2f,2.1f}[i];layer.tileSize=Vector2.one*tile;layer.tileOffset=new Vector2(i*.73f,i*.41f);
                layer.normalScale=new[]{.66f,.28f,.51f,.40f}[i];layer.smoothness=.08f;
                var tint=new[]{new Vector4(.72f,.74f,.70f,1),new Vector4(.76f,.78f,.64f,1),new Vector4(.69f,.71f,.67f,1),new Vector4(.85f,.81f,.72f,1)}[i];
                layer.diffuseRemapMin=Vector4.zero;layer.diffuseRemapMax=tint;
                layer.maskMapRemapMin=new Vector4(0,.72f,0,i==2?.13f:.035f);
                layer.maskMapRemapMax=new Vector4(0,1,.22f,i==2?.30f:.16f);
                layers[i]=layer;EditorUtility.SetDirty(layer);
            }
            data.terrainLayers=layers;
            PaintGround(data);
            var obj=Terrain.CreateTerrainGameObject(data);obj.name="Layered urban terrain â€“ sculpted soil banks";obj.transform.SetParent(parent,false);
            obj.transform.localPosition=new Vector3(-width*.5f,-1,-depth*.5f);terrain=obj.GetComponent<Terrain>();terrain.heightmapPixelError=2;
            string matPath=Generated+"/Materials/Terrain.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat){mat=new Material(Shader.Find("HDRP/TerrainLit"));AssetDatabase.CreateAsset(mat,matPath);}
            mat.shader=Shader.Find("HDRP/TerrainLit");mat.SetFloat("_EnableHeightBlend",1);mat.SetFloat("_HeightTransition",.09f);HDMaterial.ValidateMaterial(mat);
            mat.shader=Shader.Find("ZeroRise/UrbanGround");
            mat.enableInstancing=true;mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_MASKMAP");mat.DisableKeyword("_TERRAIN_BLEND_DENSITY");
            mat.EnableKeyword("_TERRAIN_BLEND_HEIGHT");mat.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
            terrain.materialTemplate=mat;terrain.drawInstanced=true;terrain.basemapDistance=1000;
            data.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(mat);EditorUtility.SetDirty(data);
        }
        static float Patch(float x,float z,float cx,float cz,float rx,float rz)
        {
            float q=(x-cx)*(x-cx)/(rx*rx)+(z-cz)*(z-cz)/(rz*rz);
            return Mathf.Pow(Mathf.Max(0,1-q),2);
        }
        public static void Nature(Transform parent,WestbruchLayout plan)
        {
            var root=Group("Urban trees – branched species and photographic foliage",parent,Vector3.zero).transform;
            var meshes=new Mesh[4,2];var heights=new float[4];
            for(int kind=0;kind<4;kind++)for(int level=0;level<2;level++)
            {
                var mesh=WestbruchTreeMeshes.Get(kind,level);mesh.RecalculateBounds();if(level==0)heights[kind]=mesh.bounds.size.y;
                var bounds=mesh.bounds;bounds.Expand(new Vector3(.24f,.12f,.24f));mesh.bounds=bounds;EditorUtility.SetDirty(mesh);meshes[kind,level]=mesh;
            }
            var positions=new List<Point>(plan.trees);
            foreach(float x in new[]{-106f,-53,-22,34,79,108})foreach(float z in new[]{-94f,-53,31,54,94})
                if(CanGrow(x,z,2.5f)&&plan.buildings.All(b=>RectDistance(b,x,z)>4)&&positions.All(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(x,z))>8))positions.Add(new Point{x=x,z=z});
            int matureCount=positions.Count;
            foreach(var yard in new[]{new Vector2(-1,-74),new Vector2(15,-57),new Vector2(-27,-49),new Vector2(-4,-48)})
                if(CanGrow(yard.x,yard.y,2.6f)&&positions.All(p=>Vector2.Distance(new Vector2(p.x,p.z),yard)>8))positions.Add(new Point{x=yard.x,z=yard.y});
            var saved=new bool[4];
            for(int i=0;i<positions.Count;i++)
            {
                var p=positions[i];int kind=i%4;float height=i>=matureCount?5.4f+(i%3)*.35f:new[]{7.1f,8.1f,6.0f,7.8f}[kind]+(i%3)*.3f;
                var tree=Group("Urban tree "+kind+" · "+i,root,p.Position(Ground(p.x,p.z)-.02f),i*137.5f);
                var renderers=new Renderer[2];float scale=height/heights[kind];
                for(int level=0;level<2;level++)
                {
                    var crown=Group("Tree LOD "+level,tree.transform,Vector3.zero);crown.transform.localScale=Vector3.one*scale;crown.isStatic=true;
                    crown.AddComponent<MeshFilter>().sharedMesh=meshes[kind,level];
                    var renderer=crown.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{WestbruchNatureMaterials.Bark,WestbruchNatureMaterials.Leaves};renderers[level]=renderer;
                }
                bool ClearCrown()=>plan.buildings.All(b=>renderers.All(r=>!r.bounds.Intersects(GameObject.Find(b.id).transform.Find("Structural shell").GetComponent<Renderer>().bounds)));
                bool ClearSite()
                {
                    var at=tree.transform.position;
                    return OnEarth(at.x,at.z,.44f)&&ClearCrown()&&!Physics.CheckCapsule(at+Vector3.up*.55f,at+Vector3.up*3,.36f,1,QueryTriggerInteraction.Ignore);
                }
                if(!ClearSite())
                {
                    bool found=false;
                    foreach(float step in new[]{2f,4f,7f,10f})
                    {
                        foreach(var offset in new[]{new Vector2(-step,0),new Vector2(step,0),new Vector2(0,-step),new Vector2(0,step),new Vector2(-step,step),new Vector2(step,step),new Vector2(-step,-step),new Vector2(step,-step)})
                        {
                            float x=p.x+offset.x,z=p.z+offset.y;if(!CanGrow(x,z,.45f))continue;
                            tree.transform.position=new Vector3(x,Ground(x,z)-.02f,z);if(ClearSite()){found=true;break;}
                        }
                        if(found)break;
                    }
                    if(!found)throw new InvalidOperationException("No supported earth/tree clearance near "+p.x+" / "+p.z);
                }
                var lod=tree.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=true;
                lod.SetLODs(new[]{new LOD(.105f,new[]{renderers[0]}),new LOD(.012f,new[]{renderers[1]})});lod.RecalculateBounds();
                var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=kind==1?.17f:.25f;trunk.height=3.2f;trunk.center=Vector3.up*1.6f;
                if(!saved[kind])
                {
                    var asset=UnityEngine.Object.Instantiate(tree);asset.name="Urban tree asset "+kind;asset.transform.SetParent(null);asset.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    PrefabUtility.SaveAsPrefabAsset(asset,Generated+"/Prefabs/UrbanTree_"+kind+".prefab");UnityEngine.Object.DestroyImmediate(asset);saved[kind]=true;
                }
            }
            WestbruchGroundCover.SetTrees(root.GetComponentsInChildren<LODGroup>().Select(lod=>lod.transform.position));
            PaintGround(terrain.terrainData);terrain.Flush();
        }
        static void PaintGround(TerrainData data)
        {
            int resolution=data.alphamapResolution;float width=data.size.x,depth=data.size.z;
            var alpha=new float[resolution,resolution,4];
            for(int iz=0;iz<resolution;iz++)for(int ix=0;ix<resolution;ix++)
            {
                float x=ix/(resolution-1f)*width-width*.5f,z=iz/(resolution-1f)*depth-depth*.5f;
                var cover=WestbruchGroundCover.Sample(x,z);
                alpha[iz,ix,0]=cover.gravel;alpha[iz,ix,1]=cover.grass;alpha[iz,ix,2]=cover.soil;alpha[iz,ix,3]=cover.sand;
            }
            data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();EditorUtility.SetDirty(data);
        }
    }
}
