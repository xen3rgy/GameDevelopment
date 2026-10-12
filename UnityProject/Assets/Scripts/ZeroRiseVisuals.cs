using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    public static class Art
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        public static Material MaterialFor(Color color, float smoothness = .12f, float metallic = 0)
        {
            var key = ColorUtility.ToHtmlStringRGBA(color) + ":" + smoothness.ToString("F2") + ":" + metallic.ToString("F2");
            if (Cache.TryGetValue(key, out var material)) return material;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            Cache[key] = material;
            return material;
        }
        public static Material Surface(string resourcePath, float tileX, float tileY, Color tint)
        {
            string key = "surface:" + resourcePath + ":" + tileX.ToString("F1") + ":" + tileY.ToString("F1")
                + ":" + ColorUtility.ToHtmlStringRGBA(tint);
            if (Cache.TryGetValue(key,out var cached)) return cached;
            var texture=Resources.Load<Texture2D>(resourcePath);
            if(texture==null) return MaterialFor(tint,.08f);
            Shader shader=Shader.Find("Standard");
            if(shader==null) shader=Shader.Find("Universal Render Pipeline/Lit");
            var material=new Material(shader) { mainTexture=texture, color=tint, mainTextureScale=new Vector2(tileX,tileY) };
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.08f);
            Cache[key]=material;
            return material;
        }
        public static Color Hex(uint hex) => new Color32((byte)(hex>>16), (byte)(hex>>8), (byte)hex, 255);
        public static GameObject Part(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Color color, bool collision=false, float smoothness=.13f, float metallic=0)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color, smoothness, metallic);
            if (!collision && go.TryGetComponent<Collider>(out var col)) UnityEngine.Object.Destroy(col);
            return go;
        }
        public static GameObject Block(Transform parent, string name, float x, float y, float z,
            float width, float height, float depth, Color color, bool collision=false)
            => Part(parent, PrimitiveType.Cube, name, new Vector3(x,y,z),
                new Vector3(width,height,depth), color, collision);
    }

    public sealed class CitizenVisual : MonoBehaviour
    {
        private Transform leftArm, rightArm, leftLeg, rightLeg;
        private float phase, intensity;
        public void Build(bool isPlayer, int seed)
        {
            var r = new System.Random(seed*13+91);
            Color skin = new Color(.58f+(float)r.NextDouble()*.28f,.36f+(float)r.NextDouble()*.23f,.25f+(float)r.NextDouble()*.19f);
            Color jacket = isPlayer ? Art.Hex(0x29485b) : Art.Hex(new uint[]{0x536574,0x746a5e,0x566750,0x77645a,0x283e51}[r.Next(5)]);
            Color pants = isPlayer ? Art.Hex(0x263340) : Art.Hex(new uint[]{0x293b45,0x585b57,0x4c5550}[r.Next(3)]);
            Art.Part(transform,PrimitiveType.Capsule,"Torso",new Vector3(0,1.28f,0),new Vector3(.40f,.36f,.27f),jacket);
            Art.Part(transform,PrimitiveType.Cube,"Jacket hem",new Vector3(0,1.02f,-.01f),new Vector3(.60f,.12f,.32f),jacket);
            Art.Part(transform,PrimitiveType.Capsule,"Neck",new Vector3(0,1.61f,0),new Vector3(.105f,.10f,.105f),skin);
            Art.Part(transform,PrimitiveType.Sphere,"Head",new Vector3(0,1.79f,.01f),new Vector3(.34f,.40f,.32f),skin);
            Color hair=Art.Hex(new uint[]{0x1e2628,0x302923,0x6c5035,0x4b3b31}[r.Next(4)]);
            Art.Part(transform,PrimitiveType.Sphere,"Hair",new Vector3(0,1.97f,-.015f),new Vector3(.35f,.13f,.33f),hair);
            Art.Part(transform,PrimitiveType.Cube,"Nose",new Vector3(0,1.77f,.175f),new Vector3(.055f,.075f,.075f),skin);
            foreach(float side in new[]{-1f,1f})
            {
                Art.Part(transform,PrimitiveType.Sphere,"Eye",new Vector3(side*.082f,1.82f,.165f),new Vector3(.038f,.028f,.016f),Art.Hex(0x282625));
                var arm = new GameObject(side<0?"Left arm":"Right arm").transform;
                arm.SetParent(transform,false);arm.localPosition=new Vector3(side*.30f,1.53f,0);
                Art.Part(arm,PrimitiveType.Capsule,"Sleeve",new Vector3(side*.02f,-.24f,0),new Vector3(.17f,.32f,.18f),jacket);
                Art.Part(arm,PrimitiveType.Sphere,"Hand",new Vector3(side*.02f,-.53f,0),new Vector3(.13f,.13f,.12f),skin);
                var leg=new GameObject(side<0?"Left leg":"Right leg").transform;
                leg.SetParent(transform,false);leg.localPosition=new Vector3(side*.14f,.94f,0);
                Art.Part(leg,PrimitiveType.Capsule,"Trouser",new Vector3(0,-.36f,0),new Vector3(.19f,.41f,.20f),pants);
                Art.Part(leg,PrimitiveType.Cube,"Shoe",new Vector3(0,-.83f,.075f),new Vector3(.22f,.12f,.37f),Art.Hex(0x252d30));
                if (side<0){leftArm=arm;leftLeg=leg;} else {rightArm=arm;rightLeg=leg;}
            }
            if (isPlayer)
            {
                Art.Part(transform,PrimitiveType.Cube,"Olive backpack",new Vector3(0,1.32f,-.26f),
                    new Vector3(.40f,.49f,.20f),Art.Hex(0x7d8960));
                Art.Part(transform,PrimitiveType.Cube,"Backpack pocket",new Vector3(0,1.19f,-.38f),
                    new Vector3(.32f,.17f,.055f),Art.Hex(0x677650));
            }
            float width = isPlayer?1f:.94f+(float)r.NextDouble()*.16f;
            transform.localScale = new Vector3(width,.96f+(float)r.NextDouble()*.07f,width);
        }
        public void Animate(float dt, float forward, bool running)
        {
            phase += dt * (running?11f:8f) * Mathf.Clamp01(forward);
            intensity = Mathf.MoveTowards(intensity,Mathf.Clamp01(forward),dt*7f);
            float sway=Mathf.Sin(phase)*intensity;
            if (leftArm){ leftArm.localRotation=Quaternion.Euler(-sway*23f,0,0);rightArm.localRotation=Quaternion.Euler(sway*23f,0,0); }
            if (leftLeg){ leftLeg.localRotation=Quaternion.Euler(sway*27f,0,0);rightLeg.localRotation=Quaternion.Euler(-sway*27f,0,0); }
        }
    }

    public sealed class CarVisual : MonoBehaviour
    {
        public Transform[] Wheels {get;private set;}
        private GameObject smoke;
        private ParticleSystem smokeFx;
        public void Build(string type)
        {
            bool van=type=="van",sport=type=="sport",bike=type=="bike";
            if(bike)
            {
                Wheels=new Transform[2];
                for(int i=0;i<2;i++)
                {
                    var w=Art.Part(transform,PrimitiveType.Cylinder,"Bike wheel",
                        new Vector3(0,.47f,i==0?-0.68f:.68f),
                        new Vector3(.85f,.055f,.85f),Art.Hex(0x222b30));
                    w.transform.localRotation=Quaternion.Euler(0,0,90);
                    Wheels[i]=w.transform;
                }
                Art.Block(transform,"Bicycle frame",0,.92f,0,.09f,.12f,1.25f,Art.Hex(0x436659));
                Art.Block(transform,"Handlebar",0,1.3f,.65f,.72f,.06f,.06f,Art.Hex(0xb1b8b1));
                Art.Block(transform,"Seat",0,1.02f,-.25f,.3f,.06f,.33f,Art.Hex(0x23292d));
                return;
            }
            float width = sport?1.90f:van?1.96f:1.84f;
            float length = van?4.70f:sport?4.38f:3.95f;
            float top=van?2.15f:sport?1.23f:1.49f;
            Color body=Art.Hex(type=="sport"?0x4b686a:type=="van"?0xd4d2c8:0x677a85);
            Art.Block(transform,"Painted chassis",0,.69f,0,width, .64f,length,body);
            Art.Block(transform,"Cabin",0,van?1.51f:1.24f,van?.28f:-.14f,
                width*.87f,van?1.10f:top-.61f,van?length*.74f:length*.53f,body);
            Color glass=Art.Hex(0x253b48);
            Art.Block(transform,"Windshield",0,van?1.57f:1.30f,length*.27f,
                width*.79f,van?.80f:.43f,.06f,glass);
            Art.Block(transform,"Rear glass",0,van?1.53f:1.29f,-length*.27f,
                width*.72f,van?.70f:.36f,.06f,glass);
            for(int s=-1;s<=1;s+=2)
            {
                Art.Block(transform,"Door side glass",s*(width/2+.01f),van?1.55f:1.32f,0,
                    .065f,van?.64f:.31f,length*.36f,glass);
                Art.Block(transform,"Door handle",s*(width/2+.035f),1.01f,.07f,
                    .045f,.045f,.19f,Art.Hex(0xb8bec2));
                Art.Block(transform,"Headlamp",s*width*.37f,.76f,length*.5f+.014f,.32f,.16f,.035f,Art.Hex(0xffe7b7));
                Art.Block(transform,"Taillamp",s*width*.37f,.74f,-length*.5f-.015f,.28f,.16f,.035f,Art.Hex(0xbb4338));
            }
            Art.Block(transform,"Front grille",0,.63f,length*.5f+.02f,.75f,.28f,.045f,Art.Hex(0x232b30));
            Wheels=new Transform[4];
            int idx=0;
            foreach(int s in new[]{-1,1})foreach(int a in new[]{-1,1})
            {
                float wx=s*(width*.51f), wz=a*(length*.32f);
                var wheel=Art.Part(transform,PrimitiveType.Cylinder,"Wheel",new Vector3(wx,.38f,wz),
                    new Vector3(.67f,.16f,.67f),Art.Hex(0x202124),false,.08f);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                var rim=Art.Part(wheel.transform,PrimitiveType.Cylinder,"Alloy",Vector3.zero,
                    new Vector3(.60f,1.04f,.60f),Art.Hex(0x9da9a9),false,.58f,.65f);
                rim.transform.localRotation=Quaternion.identity;
                Wheels[idx++]=wheel.transform;
            }
            smoke = new GameObject("Engine smoke");
            smoke.transform.SetParent(transform,false);smoke.transform.localPosition=new Vector3(0,1.0f,length*.28f);
            smokeFx=smoke.AddComponent<ParticleSystem>();
            var main=smokeFx.main;main.loop=true;main.startLifetime=1.6f;main.startSpeed=.65f;main.startSize=.4f;main.maxParticles=28;
            main.startColor=new Color(.2f,.2f,.2f,.38f);
            var em=smokeFx.emission; em.rateOverTime=0;
            var shape=smokeFx.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12f;shape.radius=.16f;
            smokeFx.Play();
        }
        public void UpdateCondition(float condition)
        {
            if(smokeFx==null)return;
            var em=smokeFx.emission;em.rateOverTime=condition<8?22:condition<25?9:0;
        }
        public void RotateWheels(float distance)
        {
            if (Wheels==null)return;
            foreach(var w in Wheels) if(w) w.Rotate(Vector3.up,distance*95,Space.Self);
        }
    }
}
