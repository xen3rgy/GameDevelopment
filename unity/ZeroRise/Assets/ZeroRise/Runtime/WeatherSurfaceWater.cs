using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise
{
    // Bounded surface detail: cached ground samples, one mesh, one particle system.
    // It never changes navigation, colliders or authored terrain.
    public sealed class WeatherSurfaceWater : MonoBehaviour
    {
        sealed class Basin { public Vector3 center;public int start;public float retention; }
        sealed class SprayVehicle { public Transform body;public Rigidbody rigidbody;public TrafficVehicle traffic; }
        readonly List<Basin> basins=new List<Basin>();
        readonly List<Vector3> original=new List<Vector3>();
        readonly List<SprayVehicle> vehicles=new List<SprayVehicle>();
        Mesh mesh;Material water;GameObject surface;ParticleSystem splashes;
        HDAdditionalCameraData cameraData;bool previousCustomSettings;FrameSettings previousFrames;FrameSettingsOverrideMask previousMask;
        Vector3[] vertices;Transform view;float nextUpdate,nextSpray;
        public int PuddleCount=>basins.Count;
        public int SplashCount=>splashes?splashes.particleCount:0;
        public bool UsesLitReflections=>water&&water.shader.name=="HDRP/Lit";
        public Vector3 InspectionPoint { get; private set; }
        public void Initialize(Material source,Material drops)
        {
            view=Camera.main?Camera.main.transform:null;
            if(view&&view.TryGetComponent(out cameraData))
            {
                previousCustomSettings=cameraData.customRenderingSettings;
                previousFrames=cameraData.renderingPathCustomFrameSettings;previousMask=cameraData.renderingPathCustomFrameSettingsOverrideMask;
                cameraData.customRenderingSettings=true;
                foreach(var field in new[]{FrameSettingsField.SSR,FrameSettingsField.TransparentSSR,FrameSettingsField.TransparentPrepass})
                {cameraData.renderingPathCustomFrameSettings.SetEnabled(field,true);cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}
            }
            water=new Material(source){name="Runtime reflective shallow water"};
            var uv=new List<Vector2>();var triangles=new List<int>();var normals=new List<Vector3>();var tangents=new List<Vector4>();
            float best=float.PositiveInfinity;
            for(int z=-90;z<=100;z+=6)for(int x=-110;x<=155;x+=6)
            {
                if(basins.Count>=120)break;
                var p=new Vector3(x+Mathf.Sin(z*7+x)*1.7f,30,z+Mathf.Sin(x*11-z)*1.7f);
                if(!Physics.Raycast(p,Vector3.down,out var hit,45,1,QueryTriggerInteraction.Ignore)||hit.normal.y<.995f||hit.point.y>1.5f)continue;
                if(Physics.Raycast(hit.point+Vector3.up*.1f,Vector3.up,80,1,QueryTriggerInteraction.Ignore))continue;
                var renderer=hit.collider.GetComponent<Renderer>();if(!renderer)continue;
                bool paved=false;foreach(var m in renderer.sharedMaterials)if(m&&(m.name.StartsWith("Asphalt")||m.name.StartsWith("Paving")||m.name.StartsWith("RoadRepair")))paved=true;
                if(!paved)continue;
                // Prefer local lows and raised kerb edges; low capacity remains on flat paving.
                float mean=0;bool kerb=false;int samples=0;
                for(int n=0;n<8;n++)
                {
                    float a=n*Mathf.PI*.25f;
                    if(Physics.Raycast(hit.point+new Vector3(Mathf.Cos(a)*2,.5f,Mathf.Sin(a)*2),Vector3.down,out var near,1.5f,1,QueryTriggerInteraction.Ignore))
                    {mean+=near.point.y;samples++;kerb|=near.point.y>hit.point.y+.045f&&near.point.y<hit.point.y+.3f;}
                }
                float low=samples>0?Mathf.Clamp01((mean/samples-hit.point.y)*12):0;
                float selection=Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f)*43758.5453f,1);
                if(selection>(kerb?.72f:Mathf.Lerp(.14f,.65f,low)))continue;
                const int sides=24;var ring=new Vector3[sides];bool flat=true;
                float rx=kerb?1.25f:.75f,rz=kerb?.48f:.62f;
                for(int n=0;n<sides;n++)
                {
                    float a=n*Mathf.PI*2/sides,warp=.86f+.10f*Mathf.Sin(a*3+x)+.04f*Mathf.Sin(a*7+z);
                    var point=hit.point+new Vector3(Mathf.Cos(a)*rx*warp,0,Mathf.Sin(a)*rz*warp);
                    if(!Physics.Raycast(point+Vector3.up*.2f,Vector3.down,out var edge,.4f,1,QueryTriggerInteraction.Ignore)||Mathf.Abs(edge.point.y-hit.point.y)>.018f){flat=false;break;}
                    ring[n]=edge.point+Vector3.up*.018f;
                }
                if(!flat)continue;
                int start=original.Count;var center=hit.point+Vector3.up*.018f;
                basins.Add(new Basin{center=center,start=start,retention=kerb?1.35f:Mathf.Lerp(.78f,1.3f,low)});
                original.Add(center);original.AddRange(ring);
                for(int n=0;n<=sides;n++){uv.Add(new Vector2(original[start+n].x,original[start+n].z)*.6f);normals.Add(Vector3.up);tangents.Add(new Vector4(1,0,0,-1));}
                for(int n=0;n<sides;n++){triangles.Add(start);triangles.Add(start+1+(n+1)%sides);triangles.Add(start+1+n);}
                float distance=(center-new Vector3(111,0,70)).sqrMagnitude;if(distance<best){best=distance;InspectionPoint=center;}
            }
            vertices=original.ToArray();mesh=new Mesh{name="Kerb and low-ground surface water"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.SetUVs(0,uv);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            surface=new GameObject("Reflective rainfall reservoirs");surface.transform.SetParent(transform,false);
            surface.AddComponent<MeshFilter>().sharedMesh=mesh;var surfaceRenderer=surface.AddComponent<MeshRenderer>();surfaceRenderer.sharedMaterial=water;surfaceRenderer.shadowCastingMode=ShadowCastingMode.Off;
            var splashObject=new GameObject("Rain impacts and wet wheel spray");splashObject.transform.SetParent(transform,false);splashes=splashObject.AddComponent<ParticleSystem>();splashes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=splashes.main;main.maxParticles=256;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=1;main.startSize=.025f;main.startLifetime=.25f;main.startSpeed=0;
            var emission=splashes.emission;emission.enabled=false;var shape=splashes.shape;shape.enabled=false;
            var color=splashes.colorOverLifetime;color.enabled=true;color.color=new ParticleSystem.MinMaxGradient(new Gradient{colorKeys=new[]{new GradientColorKey(new Color(.55f,.66f,.74f),0),new GradientColorKey(new Color(.55f,.66f,.74f),1)},alphaKeys=new[]{new GradientAlphaKey(.45f,0),new GradientAlphaKey(0,1)}});
            splashes.GetComponent<ParticleSystemRenderer>().sharedMaterial=drops;splashes.Play();
            foreach(var car in FindObjectsByType<SimpleCar>())vehicles.Add(new SprayVehicle{body=car.transform,rigidbody=car.GetComponent<Rigidbody>()});
            foreach(var car in FindObjectsByType<TrafficVehicle>())vehicles.Add(new SprayVehicle{body=car.transform,traffic=car});
        }
        public void Impact(Vector3 point)
        {
            if(!splashes||!view||(point-view.position).sqrMagnitude>22*22)return;
            var emit=new ParticleSystem.EmitParams{position=point+Vector3.up*.02f,velocity=new Vector3(Random.Range(-.35f,.35f),Random.Range(.4f,.9f),Random.Range(-.35f,.35f)),startSize=Random.Range(.015f,.035f),startLifetime=.20f};splashes.Emit(emit,2);
        }
        void Update()
        {
            var world=WorldWeatherClock.Instance;if(!world||!mesh)return;
            if(Time.unscaledTime>=nextUpdate)
            {
                nextUpdate=Time.unscaledTime+.10f;
                foreach(var basin in basins)
                {
                    float fill=Mathf.Clamp01((world.PuddleAmount-.08f)*basin.retention/ .92f),scale=Mathf.Sqrt(fill);
                    for(int n=0;n<=24;n++)vertices[basin.start+n]=Vector3.Lerp(basin.center,original[basin.start+n],scale);
                }
                mesh.vertices=vertices;
                water.SetFloat("_NormalScale",Mathf.Lerp(.025f,.16f,world.RainIntensity));
                water.SetTextureOffset("_BaseColorMap",new Vector2(Time.unscaledTime*.013f,Time.unscaledTime*.009f));
                water.SetColor("_BaseColor",new Color(.018f,.032f,.045f,Mathf.Lerp(.2f,.82f,world.PuddleAmount)));
                surface.SetActive(world.PuddleAmount>.085f);
            }
            if(world.Wetness<.45f||Time.unscaledTime<nextSpray||!view)return;
            nextSpray=Time.unscaledTime+.10f;
            foreach(var car in vehicles)
            {
                if(!car.body||!car.body.gameObject.activeInHierarchy||(car.body.position-view.position).sqrMagnitude>25*25)continue;
                float speed=car.traffic?car.traffic.speed:car.rigidbody?car.rigidbody.linearVelocity.magnitude:0;
                if(speed<2)continue;
                foreach(float side in new[]{-.8f,.8f})
                {
                    var p=car.body.TransformPoint(new Vector3(side,.3f,-1.2f));
                    if(Physics.Raycast(p,Vector3.down,out var hit,.8f,1,QueryTriggerInteraction.Ignore)&&!Physics.Raycast(hit.point+Vector3.up*.15f,Vector3.up,8,1,QueryTriggerInteraction.Ignore))
                    {
                        var emit=new ParticleSystem.EmitParams{position=hit.point+Vector3.up*.04f,velocity=-car.body.forward*Mathf.Min(2,speed*.2f)+car.body.right*Mathf.Sign(side)*.65f+Vector3.up*.65f,startSize=.035f,startLifetime=.3f};splashes.Emit(emit,2);
                    }
                }
            }
        }
        void OnDestroy(){if(cameraData){cameraData.customRenderingSettings=previousCustomSettings;cameraData.renderingPathCustomFrameSettings=previousFrames;cameraData.renderingPathCustomFrameSettingsOverrideMask=previousMask;}if(water)Destroy(water);if(mesh)Destroy(mesh);if(surface)Destroy(surface);if(splashes)Destroy(splashes.gameObject);}
    }
}
