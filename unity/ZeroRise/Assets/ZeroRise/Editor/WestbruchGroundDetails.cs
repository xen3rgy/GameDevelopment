using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // Small embedded relief, baked into spatial meshes. No individual runtime
    // stone objects, colliders, particles or per-frame geometry generation.
    internal static class WestbruchGroundDetails
    {
        public const int MaximumPieces=12000,MaximumTriangles=220000;
        const int Grid=4,Materials=3;
        const string RootName="Embedded ground relief – gravel, soil clods and edge rubble";
        static readonly Dictionary<int,Variant> variants=new Dictionary<int,Variant>();
        public static int PieceCount { get; private set; }
        public static int TriangleCount { get; private set; }
        public static int RendererCount { get; private set; }

        sealed class Variant
        {
            public Mesh mesh;
            public Vector3[] vertices,normals;
            public int[] triangles;
        }
        sealed class Batch
        {
            public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<int> triangles=new List<int>();
            public Vector3 origin;
            public void Add(Variant source,Vector3 position,Quaternion rotation,Vector3 scale)
            {
                int start=vertices.Count;
                var inverse=new Vector3(1/scale.x,1/scale.y,1/scale.z);
                for(int i=0;i<source.vertices.Length;i++)
                {
                    var world=position+rotation*Vector3.Scale(source.vertices[i],scale);
                    var normal=(rotation*Vector3.Scale(source.normals[i],inverse)).normalized;
                    vertices.Add(world-origin);normals.Add(normal);
                    uv.Add(Mathf.Abs(normal.y)>.35f?new Vector2(world.x,world.z)*2:
                        Mathf.Abs(normal.x)>Mathf.Abs(normal.z)?new Vector2(world.z,world.y)*2:new Vector2(world.x,world.y)*2);
                }
                foreach(int index in source.triangles)triangles.Add(start+index);
            }
        }

        // Call after TerrainGround, connected walks and the district props exist.
        // CanGrow supplies the authoritative road/path/entrance exclusion mask.
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            if(!parent||plan==null)throw new ArgumentNullException(!parent?nameof(parent):nameof(plan));
            var previous=parent.Find(RootName);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            PieceCount=TriangleCount=RendererCount=0;
            foreach(string directory in new[]{"Meshes","Materials","Prefabs"})Directory.CreateDirectory(Generated+"/"+directory);
            AssetDatabase.Refresh();Physics.SyncTransforms();
            var root=Group(RootName,parent,Vector3.zero).transform;
            var clearance=new List<Bounds>();
            foreach(var collider in parent.GetComponentsInChildren<Collider>())
                if(collider.enabled&&!collider.isTrigger&&!(collider is TerrainCollider))clearance.Add(collider.bounds);
            foreach(var renderer in parent.GetComponentsInChildren<Renderer>())
                if(renderer.name.IndexOf("planter",StringComparison.OrdinalIgnoreCase)>=0)clearance.Add(renderer.bounds);
            var batches=new Batch[Grid*Grid*Materials];
            var random=new System.Random(20261015);
            float left=-plan.width*.5f+4,right=plan.width*.5f-4;
            float bottom=-plan.depth*.5f+4,top=plan.depth*.5f-4;
            float cellWidth=plan.width/Grid,cellDepth=plan.depth/Grid;
            float Range(float minimum,float maximum)=>minimum+(maximum-minimum)*(float)random.NextDouble();

            bool Clear(float x,float z,float radius,out float height)
            {
                height=0;
                if(x-radius<left||x+radius>right||z-radius<bottom||z+radius>top||x-radius< -110)return false;
                // Keep the playground, bridge abutments and canal banks untouched.
                if(x> -82-radius&&x< -48+radius&&z> -58-radius&&z< -40+radius)return false;
                if(x>72-radius&&x<118+radius&&z>57-radius&&z<73+radius)return false;
                if(plan.spawn!=null&&new Vector2(x-plan.spawn.x,z-plan.spawn.z).sqrMagnitude<2.25f)return false;
                if(!WestbruchLandscape.CanGrow(x,z,radius+.08f))return false;
                height=WestbruchLandscape.Ground(x,z);
                if(float.IsNaN(height)||float.IsInfinity(height)||height< -.12f||height>.8f)return false;
                float low=height,high=height;
                foreach(var offset in new[]{new Vector2(-radius,0),new Vector2(radius,0),new Vector2(0,-radius),new Vector2(0,radius)})
                {
                    float sample=WestbruchLandscape.Ground(x+offset.x,z+offset.y);
                    low=Mathf.Min(low,sample);high=Mathf.Max(high,sample);
                }
                if(high-low>.12f)return false;
                var footprint=new Bounds(new Vector3(x,height+.07f,z),new Vector3(radius*2+.06f,.18f,radius*2+.06f));
                foreach(var bounds in clearance)if(bounds.Intersects(footprint))return false;
                height=low;return true;
            }

            // Unequal elliptical clusters, biased toward worn verge and yard
            // edges. Clear patches remain between them rather than a uniform grid.
            for(int trial=0;trial<10000&&PieceCount<MaximumPieces;trial++)
            {
                float cx=Range(left,right),cz=Range(bottom,top);
                if(!Clear(cx,cz,.12f,out _))continue;
                float path=Distance(plan.paths,cx,cz),road=Distance(plan.roads,cx,cz),building=Distance(plan.buildings,cx,cz);
                bool edge=path<2.8f||road<5.8f||building<4.2f;
                float patch=Mathf.PerlinNoise((cx+316)*.047f,(cz+183)*.047f);
                if(Range(0,1)>(edge?.94f:Mathf.Lerp(.16f,.70f,patch)))continue;
                float major=Range(.45f,edge?2.0f:2.8f),minor=major*Range(.22f,.65f),angle=Range(0,Mathf.PI*2);
                float ca=Mathf.Cos(angle),sa=Mathf.Sin(angle);
                int count=random.Next(edge?11:7,edge?23:15);
                for(int piece=0;piece<count&&PieceCount<MaximumPieces;piece++)
                {
                    float a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(Range(0,1));
                    float px=Mathf.Cos(a)*major*r,pz=Mathf.Sin(a)*minor*r;
                    float x=cx+ca*px-sa*pz,z=cz+sa*px+ca*pz;
                    int kind=Range(0,1)<.24f?2:edge&&building<5&&Range(0,1)<.34f?1:0;
                    float diameter=Range(.045f,.155f);
                    if(Range(0,1)<(edge?.18f:.10f))diameter=Range(.16f,.34f);
                    if(kind==2)diameter*=Range(.9f,1.3f);
                    float radius=diameter*.5f;
                    if(!Clear(x,z,radius,out float ground))continue;
                    var source=Source(kind,random.Next(4));int triangles=source.triangles.Length/3;
                    if(TriangleCount+triangles>MaximumTriangles)break;
                    int ix=Mathf.Clamp(Mathf.FloorToInt((x+plan.width*.5f)/cellWidth),0,Grid-1);
                    int iz=Mathf.Clamp(Mathf.FloorToInt((z+plan.depth*.5f)/cellDepth),0,Grid-1);
                    int key=(iz*Grid+ix)*Materials+kind;
                    var batch=batches[key]??(batches[key]=new Batch{origin=new Vector3((ix+.5f)*cellWidth-plan.width*.5f,0,(iz+.5f)*cellDepth-plan.depth*.5f)});
                    float rise=diameter*Range(kind==2?.24f:.32f,kind==2?.48f:.62f);
                    var rotation=Quaternion.Euler(Range(-7,7),Range(0,360),Range(-7,7));
                    batch.Add(source,new Vector3(x,ground-rise*.16f,z),rotation,new Vector3(radius,rise,radius*Range(.62f,1.08f)));
                    PieceCount++;TriangleCount+=triangles;
                }
                if(TriangleCount>=MaximumTriangles-18)break;
            }
            var materials=new[]{
                Material("GroundDetail_EmbeddedGravel","yard",new Color(.76f,.73f,.66f),.09f),
                Material("GroundDetail_EdgeRubble","concrete",new Color(.70f,.65f,.56f),.07f),
                Material("GroundDetail_ErodedClods","mud",new Color(.57f,.50f,.40f),.04f)};
            for(int key=0;key<batches.Length;key++)
            {
                var batch=batches[key];if(batch==null||batch.triangles.Count==0)continue;
                string id="GroundRelief_"+(key/Materials)+"_"+(key%Materials);
                string path=Generated+"/Meshes/"+id+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.name=id;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(batch.vertices);mesh.SetNormals(batch.normals);mesh.SetUVs(0,batch.uv);mesh.SetTriangles(batch.triangles,0);
                mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var obj=Group(id,root,batch.origin);obj.isStatic=true;
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[key%Materials];
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                RendererCount++;
            }
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Generated+"/Prefabs/WestbruchGroundDetails.prefab");
            Debug.Log("Ground relief baked: "+PieceCount+" embedded pieces, "+TriangleCount+" triangles, "+RendererCount+" spatial renderers; zero added colliders.");
        }

        static float Distance<T>(T[] plots,float x,float z) where T:Plot
        {
            float closest=float.PositiveInfinity;
            if(plots==null)return closest;
            foreach(var plot in plots)
            {
                float dx=Mathf.Max(0,Mathf.Abs(x-plot.x)-plot.w*.5f),dz=Mathf.Max(0,Mathf.Abs(z-plot.z)-plot.d*.5f);
                closest=Mathf.Min(closest,Mathf.Sqrt(dx*dx+dz*dz));
            }
            return closest;
        }

        static Variant Source(int kind,int variant)
        {
            int key=kind*4+variant;if(variants.TryGetValue(key,out var cached))return cached;
            string path=Generated+"/Meshes/GroundReliefSource_"+kind+"_"+variant+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                var random=new System.Random(1889+key*197);int sides=kind==0?6:5;
                var lower=new Vector3[sides];var shoulder=new Vector3[sides];
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides+(float)random.NextDouble()*.16f;
                    float radius=.76f+(float)random.NextDouble()*.24f;
                    lower[i]=new Vector3(Mathf.Cos(a)*radius,-.18f,Mathf.Sin(a)*radius);
                    shoulder[i]=new Vector3(Mathf.Cos(a)*radius*(kind==1?.92f:.74f),.27f+(float)random.NextDouble()*.16f,Mathf.Sin(a)*radius*(kind==1?.72f:.74f));
                }
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();
                void Face(Vector3 a,Vector3 b,Vector3 c)
                {
                    int start=vertices.Count;var normal=Vector3.Cross(b-a,c-a).normalized;
                    vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(normal);normals.Add(normal);normals.Add(normal);
                    indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                }
                var crown=new Vector3(((float)random.NextDouble()-.5f)*.25f,kind==2?.58f:.72f,((float)random.NextDouble()-.5f)*.22f);
                for(int i=0;i<sides;i++)
                {
                    int next=(i+1)%sides;
                    Face(lower[i],shoulder[i],lower[next]);Face(lower[next],shoulder[i],shoulder[next]);Face(shoulder[i],crown,shoulder[next]);
                }
                mesh=new Mesh{name="Cached irregular ground source "+key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,path);
            }
            cached=new Variant{mesh=mesh,vertices=mesh.vertices,normals=mesh.normals,triangles=mesh.triangles};variants.Add(key,cached);return cached;
        }
    }
}
