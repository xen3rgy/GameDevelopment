using UnityEngine;

namespace ZeroRise
{
    // One small contact history for the whole district; all plant motion runs on
    // the GPU. No per-plant Update, colliders, allocations or changing meshes.
    [DefaultExecutionOrder(100)]
    public sealed class VegetationMotion : MonoBehaviour
    {
        public ThirdPersonMotor player;
        public SimpleCar car;
        const int Samples = 12;
        readonly Vector4[] from = new Vector4[Samples * 2];
        readonly Vector4[] to = new Vector4[Samples * 2];
        readonly Vector3[] previous = new Vector3[2];
        readonly bool[] initialized = new bool[2];
        readonly Vector4[] bounds = new Vector4[2];
        int slot;
        float nextSample;
        static readonly int FromId=Shader.PropertyToID("_ZRPlantFrom"), ToId=Shader.PropertyToID("_ZRPlantTo"),
            BoundsId=Shader.PropertyToID("_ZRPlantBounds"), TimeId=Shader.PropertyToID("_ZRPlantTime");
        public int LiveContacts { get; private set; }

        void OnEnable() { for(int i=0;i<from.Length;i++)from[i].w=-100; Publish(Time.time); }
        void LateUpdate() { Step(Time.time); }
        public void Step(float now)
        {
            if(now>=nextSample)
            {
                Record(0,player?player.transform:null,player&&!player.driving,.95f,now);
                Record(1,car?car.transform:null,car,1.65f,now);
                slot=(slot+1)%Samples; nextSample=now+.1f;
            }
            Publish(now);
        }
        void Record(int actor,Transform subject,bool active,float radius,float now)
        {
            int index=actor*Samples+slot;
            if(!active||!subject){to[index]=Vector4.zero;initialized[actor]=false;return;}
            var point=subject.position;
            // Ignore teleport sweeps; normal walking/car travel is swept between samples.
            var start=initialized[actor]&&Vector3.Distance(previous[actor],point)<5?previous[actor]:point;
            from[index]=new Vector4(start.x,start.y,start.z,now);
            to[index]=new Vector4(point.x,point.y,point.z,radius);
            previous[actor]=point;initialized[actor]=true;
        }
        void Publish(float now)
        {
            LiveContacts=0;
            for(int actor=0;actor<2;actor++)
            {
                var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
                var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);int active=0;
                for(int i=actor*Samples;i<(actor+1)*Samples;i++)
                {
                    if(to[i].w<=0||now-from[i].w>1.45f)continue;
                    var a=new Vector2(from[i].x,from[i].z);var b=new Vector2(to[i].x,to[i].z);
                    min=Vector2.Min(min,Vector2.Min(a,b)-Vector2.one*to[i].w);
                    max=Vector2.Max(max,Vector2.Max(a,b)+Vector2.one*to[i].w);active++;
                }
                var center=(min+max)*.5f;
                bounds[actor]=active>0?new Vector4(center.x,center.y,(max-min).magnitude*.5f,1):Vector4.zero;
                LiveContacts+=active;
            }
            Shader.SetGlobalVectorArray(BoundsId,bounds);
            Shader.SetGlobalVectorArray(FromId,from);Shader.SetGlobalVectorArray(ToId,to);Shader.SetGlobalFloat(TimeId,now);
        }
        void OnDisable() { bounds[0]=bounds[1]=Vector4.zero;Shader.SetGlobalVectorArray(BoundsId,bounds); }
    }
}
