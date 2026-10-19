using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchYardLitter
    {
        const string RootName="Yard surface details – leaf drifts and fallen twigs";
        const int Grid=4,Families=3,MaximumLeaves=25000,MaximumTriangles=250000;
        public static int LeafCount { get; private set; }
        public static int TwigCount { get; private set; }
        public static int ChipCount { get; private set; }
        public static int TriangleCount { get; private set; }
        public static int RendererCount { get; private set; }
        sealed class Band { public Vector2 center;public float length,width,angle;public int species;public bool edge; }
        sealed class Batch
        {
            public Vector3 origin;
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<Color> colors=new List<Color>();
            public readonly List<int> triangles=new List<int>();
            public int Vertex(Vector3 p,Vector2 texture,Color tint)
            {int index=vertices.Count;tint.a=1;vertices.Add(p-origin);uv.Add(texture);colors.Add(tint);return index;}
            public void Tri(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
        }
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            if(!parent||plan==null)throw new ArgumentNullException(!parent?nameof(parent):nameof(plan));
            var terrain=parent.GetComponentInChildren<Terrain>();if(!terrain)throw new InvalidOperationException("Yard litter requires the district terrain.");
            var leafMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_FallenLeaves.mat");
            var barkMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/Nature_Bark.mat");
            var chipMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/Materials/GroundDetail_EdgeRubble.mat");
            if(!leafMaterial||!barkMaterial)throw new InvalidOperationException("Prepare Nature_FallenLeaves and Nature_Bark materials before baking yard litter.");
            var previous=parent.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            LeafCount=TwigCount=ChipCount=TriangleCount=RendererCount=0;
            var root=Group(RootName,parent,Vector3.zero).transform;Physics.SyncTransforms();
            var blocked=new List<Bounds>();
            foreach(var collider in parent.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger&&!(collider is TerrainCollider))blocked.Add(collider.bounds);
            foreach(var renderer in parent.GetComponentsInChildren<Renderer>())
                if(renderer.name.IndexOf("planter",StringComparison.OrdinalIgnoreCase)>=0||renderer.name=="Raised bed soil")blocked.Add(renderer.bounds);
            var random=new System.Random(2026101607);var bands=new List<Band>();
            float F(float a,float b)=>a+(b-a)*(float)random.NextDouble();
            Vector2 Out(float a)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            const float windAngle=.60f;
            foreach(var tree in parent.GetComponentsInChildren<LODGroup>())
            {
                if(!tree.name.StartsWith("Urban tree "))continue;
                var lods=tree.GetLODs();if(lods.Length==0||lods[0].renderers.Length==0)continue;
                var bounds=new Bounds(tree.transform.position,Vector3.zero);int species=0;
                foreach(var renderer in lods[0].renderers)
                {
                    if(!renderer)continue;bounds.Encapsulate(renderer.bounds);
                    var filter=renderer.GetComponent<MeshFilter>();
                    if(filter&&filter.sharedMesh)
                    {
                        var parts=filter.sharedMesh.name.Split('_');
                        if(parts.Length>=3&&parts[0]=="UrbanTree"&&int.TryParse(parts[1],out int parsed))species=Mathf.Clamp(parsed,0,3);
                    }
                }
                float radius=Mathf.Clamp(Mathf.Max(bounds.extents.x,bounds.extents.z)*.82f,1.15f,3.9f);
                var planted=new Vector2(tree.transform.position.x,tree.transform.position.z);
                int count=random.Next(7,11);
                for(int band=0;band<count;band++)
                {
                    // Broken, displaced arcs and elongated windrows. They never
                    // describe a complete ring or fill the canopy footprint solid.
                    float angle=F(0,Mathf.PI*2);
                    var center=planted+Out(angle)*(radius*F(.48f,1.05f))+Out(windAngle)*(radius*.17f);
                    bands.Add(new Band{center=center,length=F(.42f,1.23f),width=F(.12f,.40f),angle=windAngle+F(-.65f,.65f),species=species});
                }
            }
            foreach(var building in plan.buildings)
            {
                var rear=Out((90-building.yaw)*Mathf.Deg2Rad);var along=new Vector2(rear.y,-rear.x);
                for(int strip=0;strip<2;strip++)
                {
                    var center=new Vector2(building.x,building.z)+rear*(building.d*.5f+F(1.35f,1.92f))+along*(building.w*(strip==0?-.24f:.25f));
                    bands.Add(new Band{center=center,length=building.w*F(.15f,.23f),width=F(.13f,.31f),angle=Mathf.Atan2(along.y,along.x),species=random.Next(4),edge=true});
                }
            }
            int fenceBands=0;
            foreach(var bounds in blocked)
            {
                float longSide=Mathf.Max(bounds.size.x,bounds.size.z),shortSide=Mathf.Min(bounds.size.x,bounds.size.z);
                if(fenceBands>=60||bounds.size.y<.65f||longSide<2.8f||shortSide>.45f||bounds.min.y>.40f)continue;
                bool horizontal=bounds.size.x>bounds.size.z;
                var away=horizontal?Vector2.up:Vector2.right;
                bands.Add(new Band{center=new Vector2(bounds.center.x,bounds.center.z)+away*F(.20f,.40f),length=Mathf.Min(3.7f,longSide*.45f),width=.16f,angle=horizontal?0:Mathf.PI*.5f,species=random.Next(4),edge=true});
                fenceBands++;
            }
            var batches=new Batch[Grid*Grid*Families];
            float Ground(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
            bool Safe(float x,float z,float radius)
            {
                if(Mathf.Abs(x)+radius>plan.width*.5f-3||Mathf.Abs(z)+radius>plan.depth*.5f-3||x-radius< -110)return false;
                if(x> -82-radius&&x< -48+radius&&z> -58-radius&&z< -40+radius)return false;
                if(x>72-radius&&x<118+radius&&z>57-radius&&z<73+radius)return false;
                return WestbruchLandscape.CanGrow(x,z,radius+.025f);
            }
            bool Clear(Bounds footprint)
            {foreach(var obstacle in blocked)if(obstacle.Intersects(footprint))return false;return true;}
            Batch GetBatch(float x,float z,int family)
            {
                int ix=Mathf.Clamp(Mathf.FloorToInt((x+plan.width*.5f)/(plan.width/Grid)),0,Grid-1);
                int iz=Mathf.Clamp(Mathf.FloorToInt((z+plan.depth*.5f)/(plan.depth/Grid)),0,Grid-1),key=(iz*Grid+ix)*Families+family;
                return batches[key]??(batches[key]=new Batch{origin=new Vector3((ix+.5f)*plan.width/Grid-plan.width*.5f,0,(iz+.5f)*plan.depth/Grid-plan.depth*.5f)});
            }
            Vector2 Pick(Band band)
            {
                float theta=F(0,Mathf.PI*2),radius=Mathf.Sqrt(F(0,1));
                float u=Mathf.Cos(theta)*band.length*radius,v=Mathf.Sin(theta)*band.width*radius;
                return band.center+Out(band.angle)*u+Out(band.angle+Mathf.PI*.5f)*v;
            }
            bool FallenLeaf(Vector2 center,Band band)
            {
                float length=F(.06f,.16f),width=length*F(.25f,.38f),angle=band.angle+F(-1.5f,1.5f);
                float radius=Mathf.Sqrt(length*length*.25f+width*width);
                if(!Safe(center.x,center.y,radius))return false;
                var direction=Out(angle);var across=Out(angle-Mathf.PI*.5f);
                var start=center-direction*(length*.5f);var end=center+direction*(length*.5f);
                var points=new[]{start-across*width,start,start+across*width,end-across*width,end,end+across*width};
                var positions=new Vector3[6];float low=float.PositiveInfinity,high=float.NegativeInfinity;
                float curl=F(.005f,.025f),roll=F(-.7f,.7f);
                for(int i=0;i<points.Length;i++)
                {
                    var p=points[i];float floor=Ground(p.x,p.y);low=Mathf.Min(low,floor);high=Mathf.Max(high,floor);
                    float edge=i%3==1?0:curl*(i<3?.28f:1)*(i%3==0?.65f+roll*.5f:.65f-roll*.5f);
                    positions[i]=new Vector3(p.x,floor+.008f+edge,p.y);
                }
                if(high-low>.065f)return false;
                var footprint=new Bounds(positions[0],Vector3.zero);foreach(var p in positions)footprint.Encapsulate(p);footprint.Expand(new Vector3(.012f,.012f,.012f));
                if(!Clear(footprint))return false;
                var batch=GetBatch(center.x,center.y,0);int first=batch.vertices.Count;
                int col=band.species%2,row=band.species<2?1:0;const float pad=.0325f;float span=.5f-pad*2;
                float u0=col*.5f+pad,v0=row*.5f+pad;
                var shade=new Color(F(.84f,1),F(.83f,.98f),F(.80f,.94f),1).linear;
                for(int i=0;i<positions.Length;i++)batch.Vertex(positions[i],new Vector2(u0+(i%3)*.5f*span,v0+(i/3)*span),shade);
                batch.Tri(first,first+3,first+1);batch.Tri(first+1,first+3,first+4);
                batch.Tri(first+1,first+4,first+2);batch.Tri(first+2,first+4,first+5);
                LeafCount++;TriangleCount+=4;return true;
            }
            bool Twig(Vector2 center,float angle,float length,float radius)
            {
                if(!Safe(center.x,center.y,length*.53f))return false;
                var direction=Out(angle);var side=Out(angle-Mathf.PI*.5f);
                var points=new Vector3[3];
                for(int i=0;i<3;i++)
                {
                    var p=center+direction*((i*.5f-.5f)*length)+side*(i==1?length*.055f:0);
                    points[i]=new Vector3(p.x,Ground(p.x,p.y)+radius+.006f,p.y);
                }
                var footprint=new Bounds(points[0],Vector3.zero);foreach(var point in points)footprint.Encapsulate(point);footprint.Expand(radius*2+.012f);
                if(!Clear(footprint))return false;
                var batch=GetBatch(center.x,center.y,1);int first=batch.vertices.Count;const int sides=4;
                var shade=new Color(.93f,.88f,.80f,1).linear;
                for(int ring=0;ring<3;ring++)
                {
                    float r=radius*Mathf.Lerp(1,.45f,ring*.5f);
                    for(int j=0;j<=sides;j++)
                    {
                        float a=j*Mathf.PI*2/sides;
                        var radial=new Vector3(side.x,0,side.y)*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);
                        batch.Vertex(points[ring]+radial*r,new Vector2(j/(float)sides*radius*Mathf.PI*2/.5f,ring*.5f*length/.65f),shade);
                        if(ring==0||j==sides)continue;
                        int previous=first+(ring-1)*(sides+1)+j,current=previous+sides+1;
                        batch.Tri(previous,previous+1,current);batch.Tri(previous+1,current+1,current);
                    }
                }
                int bottom=batch.Vertex(points[0],Vector2.zero,shade),top=batch.Vertex(points[2],Vector2.one,shade);
                for(int j=0;j<sides;j++){batch.Tri(bottom,first+j+1,first+j);batch.Tri(top,first+10+j,first+10+j+1);}
                TwigCount++;TriangleCount+=24;return true;
            }
            void Chip(Vector2 center)
            {
                float w=F(.025f,.070f),d=F(.018f,.055f),h=F(.012f,.034f);
                if(!Safe(center.x,center.y,Mathf.Sqrt(w*w+d*d)))return;
                float angle=F(0,Mathf.PI*2);var along=Out(angle);var across=Out(angle+Mathf.PI*.5f);
                var positions=new Vector3[8];var footprint=new Bounds(new Vector3(center.x,Ground(center.x,center.y),center.y),Vector3.zero);
                for(int layer=0;layer<2;layer++)for(int corner=0;corner<4;corner++)
                {
                    float sx=corner==0||corner==3?-1:1,sz=corner<2?-1:1;
                    var p=center+along*(sx*w)+across*(sz*d);float y=Ground(p.x,p.y)+.003f+(layer==0?0:h*(corner==2?.52f:1));
                    positions[layer*4+corner]=new Vector3(p.x,y,p.y);footprint.Encapsulate(positions[layer*4+corner]);
                }
                footprint.Expand(.012f);if(!Clear(footprint))return;
                var batch=GetBatch(center.x,center.y,2);int first=batch.vertices.Count;
                foreach(var p in positions)batch.Vertex(p,new Vector2(p.x,p.z)*2,new Color(.89f,.79f,.67f,1).linear);
                batch.Tri(first+4,first+7,first+5);batch.Tri(first+5,first+7,first+6);
                for(int sideIndex=0;sideIndex<4;sideIndex++)
                {int a=first+sideIndex,b=first+(sideIndex+1)%4;batch.Tri(a,a+4,b);batch.Tri(b,a+4,b+4);}
                ChipCount++;TriangleCount+=10;
            }
            if(bands.Count>0)
            {
                for(int attempt=0;attempt<180000&&LeafCount<MaximumLeaves&&TriangleCount+4<=MaximumTriangles;attempt++)
                {var band=bands[random.Next(bands.Count)];FallenLeaf(Pick(band),band);}
                for(int group=0;group<220&&TriangleCount+72<=MaximumTriangles;group++)
                {
                    var band=bands[random.Next(bands.Count)];var p=Pick(band);float angle=band.angle+F(-.9f,.9f),length=F(.14f,.45f);
                    if(!Twig(p,angle,length,F(.003f,.008f)))continue;
                    if(F(0,1)<.55f)Twig(p+Out(angle)*length*.13f,angle+F(.55f,1.05f),length*F(.35f,.58f),.0028f);
                }
                if(chipMaterial)for(int attempt=0;attempt<800&&ChipCount<160&&TriangleCount+10<=MaximumTriangles;attempt++)
                {var band=bands[random.Next(bands.Count)];if(band.edge)Chip(Pick(band));}
            }
            Directory.CreateDirectory(Generated+"/Meshes");
            var materials=new[]{leafMaterial,barkMaterial,chipMaterial};
            for(int key=0;key<batches.Length;key++)
            {
                var batch=batches[key];if(batch==null||batch.triangles.Count==0)continue;
                string id="YardLitter_"+(key/Families)+"_"+(key%Families),path=Generated+"/Meshes/"+id+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.name=id;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(batch.vertices);mesh.SetUVs(0,batch.uv);mesh.SetColors(batch.colors);mesh.SetTriangles(batch.triangles,0);
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var obj=Group(id,root,batch.origin);obj.isStatic=true;obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[key%Families];renderer.shadowCastingMode=ShadowCastingMode.On;RendererCount++;
            }
            Debug.Log("Yard litter baked: "+LeafCount+" fallen leaves, "+TwigCount+" twigs, "+ChipCount+" chips / "+TriangleCount+" triangles / "+RendererCount+" spatial renderers; zero new colliders.");
        }
    }
}
