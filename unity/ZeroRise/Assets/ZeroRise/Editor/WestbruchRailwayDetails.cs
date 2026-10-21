using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // A local, additive art pass for the existing non-electrified freight branch.
    // Signal aspects are deliberately static: no train dispatch system is added.
    internal static class WestbruchRailwayDetails
    {
        public const string RootName="Railway service infrastructure – Westbruch";
        public const int SignalCount=2,ChainageCount=3;
        static Material concrete,steel,dark,enamel,red,green;

        public static void Build(Transform parent)
        {
            var root=Group(RootName,parent,Vector3.zero).transform;
            concrete=Material("RailDetail_PrecastConcrete","concrete",new Color(.65f,.66f,.62f),.08f);
            steel=Material("RailDetail_Galvanised","steel",new Color(.39f,.43f,.42f),.25f,.65f);
            dark=Material("RailDetail_SignalBlack",null,new Color(.045f,.052f,.050f),.15f);
            enamel=Material("RailDetail_WhiteEnamel",null,new Color(.78f,.79f,.72f),.22f);
            red=Material("RailDetail_StopLens",null,new Color(.48f,.035f,.018f),.42f);
            red.SetColor("_EmissiveColor",new Color(2.2f,.035f,.008f));
            HDMaterial.ValidateMaterial(red);EditorUtility.SetDirty(red);
            green=Material("RailDetail_UnlitGreenLens",null,new Color(.025f,.09f,.058f),.45f);
            Physics.SyncTransforms();

            // The existing boundary is only 55 cm from the ballast toe. Keep the
            // narrow inspection bench entirely inside it and clear of sleepers.
            Bench(root,-110,59.3f);
            Bench(root,100.7f,128);
            CableRoute(root,-125,59.3f,false);
            CableRoute(root,59.3f,63.3f,true);
            CableRoute(root,63.3f,96.7f,true);
            CableRoute(root,96.7f,100.7f,true);
            CableRoute(root,100.7f,145,false);

            // Each signal faces the approach, on the right of its direction of
            // travel. Their posts and heads stand outside the running gauge.
            Signal(root,new Vector2(120.30f,44),0,"WB 11");
            Signal(root,new Vector2(115.70f,116),180,"WB 12");
            Chainage(root,new Vector2(120.62f,-78),0,"12\n3");
            Chainage(root,new Vector2(120.62f,22),180,"12\n4");
            Chainage(root,new Vector2(120.62f,122),0,"12\n5");
            Warning(root,103.15f,55,90);
            Warning(root,136,106.175f,270);
            RelayCabinet(root);
        }

        // Query the saved scene rather than cached terrain-generation state.
        // This also works when this pass is run without rebuilding the banks.
        internal static float Support(float x,float z)
        {
            float y=float.NegativeInfinity;
            foreach(var h in Physics.RaycastAll(new Vector3(x,16,z),Vector3.down,35,1,QueryTriggerInteraction.Ignore))
            {
                if(h.normal.y<.25f)continue;
                string name=h.collider.name;
                bool supporting=h.collider is TerrainCollider||name=="Continuous South railway earth bank"||name=="Continuous North railway earth bank"||name=="Continuous supported ballast formation"||name=="Maintenance walkway"||name.StartsWith("Seated maintenance approach ramp")||name=="Retained maintenance approach apron"||name.Contains("hardstanding");
                if(supporting)y=Mathf.Max(y,h.point.y);
            }
            if(float.IsNegativeInfinity(y))throw new InvalidOperationException("Railway detail has no physical support at "+x+", "+z);
            return y;
        }

        static void Bench(Transform root,float start,float end)
        {
            var bench=Group("Bank inspection bench "+start,root,Vector3.zero).transform;
            int n=Mathf.CeilToInt((end-start)/2.5f);
            for(int i=0;i<n;i++)
            {
                float z0=Mathf.Lerp(start,end,i/(float)n),z1=Mathf.Lerp(start,end,(i+1)/(float)n);
                var a=new Vector3(115.28f,Support(115.28f,z0)+.035f,z0);
                var b=new Vector3(115.28f,Support(115.28f,z1)+.035f,z1);
                var slab=Box("Seated inspection bench slab",bench,(a+b)*.5f-Vector3.up*.05f,new Vector3(.44f,.10f,Vector3.Distance(a,b)-.012f),concrete,true,1.4f);
                slab.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
            }
        }

        static float CableX(float z)
        {
            if(z<59.3f||z>100.7f)return 120.78f;
            if(z<63.3f)return Mathf.Lerp(120.78f,121.87f,Mathf.InverseLerp(59.3f,63.3f,z));
            if(z>96.7f)return Mathf.Lerp(121.87f,120.78f,Mathf.InverseLerp(96.7f,100.7f,z));
            return 121.87f;
        }

        static void CableRoute(Transform root,float start,float end,bool bridge)
        {
            var route=Group("Covered railway cable route "+start,root,Vector3.zero).transform;
            int n=Mathf.CeilToInt((end-start)/1.25f);
            for(int i=0;i<n;i++)
            {
                float z0=Mathf.Lerp(start,end,i/(float)n),z1=Mathf.Lerp(start,end,(i+1)/(float)n);
                var cuts=new List<float>{z0,z1};
                if(bridge)for(float post=63;post<=97;post+=2)foreach(float edge in new[]{post-.12f,post+.12f})if(edge>z0&&edge<z1)cuts.Add(edge);
                cuts.Sort();
                for(int j=0;j<cuts.Count-1;j++)
                {
                    float aZ=cuts[j],bZ=cuts[j+1],midZ=(aZ+bZ)*.5f;
                    // At each parapet foot the conduit passes below the paving.
                    // Leave the actual base plate clear instead of drawing a
                    // concrete lid through its bolts and narrowing the path.
                    if(bridge&&midZ>=62.88f&&midZ<=97.12f&&Mathf.Abs(midZ-(63+Mathf.Round((midZ-63)/2)*2))<.12f)continue;
                    if(bZ-aZ<.045f)continue;
                    var a=new Vector3(CableX(aZ),0,aZ);var b=new Vector3(CableX(bZ),0,bZ);
                    a.y=Support(a.x,a.z);b.y=Support(b.x,b.z);
                    Vector3 middle=(a+b)*.5f;
                    float width=bridge?.10f:.18f;
                    // Cover and recessed body are a single closed solid. Lid
                    // joints are gaps between units, not coplanar overlays.
                    var trough=Box("Seated covered cable trough",route,middle+Vector3.up*.0275f,new Vector3(width,.095f,Vector3.Distance(a,b)-.025f),concrete,false,1.1f);
                    trough.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                    if(i%7==3&&bZ-aZ>.35f)
                    {
                        var clip=Box("Cable lid lifting inset",route,middle+Vector3.up*.076f,new Vector3(.05f,.004f,.085f),dark,false);
                        clip.transform.rotation=trough.transform.rotation;
                    }
                }
            }
        }

        static void Signal(Transform root,Vector2 at,float yaw,string id)
        {
            // A level plinth carries the mast above the sloped ballast shoulder;
            // its bottom penetrates the lowest soil instead of floating on it.
            float low=float.PositiveInfinity,high=float.NegativeInfinity;
            foreach(float dx in new[]{-.18f,0,.18f})foreach(float dz in new[]{-.25f,.25f})
            {float y=Support(at.x+dx,at.y+dz);low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
            float baseTop=high+.065f,baseBottom=low-.055f;
            var signal=Group("Freight stop signal "+id,root,new Vector3(at.x,baseTop,at.y),yaw).transform;
            Box("Embedded signal foundation",signal,new Vector3(0,(baseBottom-baseTop)*.5f,0),new Vector3(.36f,baseTop-baseBottom,.50f),concrete,false,1.4f);
            Box("Signal mast base plate",signal,new Vector3(0,.018f,0),new Vector3(.25f,.036f,.32f),steel,false);
            Cylinder("Signal tubular mast",signal,new Vector3(0,1.68f,0),.064f,3.36f,steel);
            Box("Signal head backplate",signal,new Vector3(0,3.62f,-.08f),new Vector3(.48f,1.03f,.14f),dark,false);
            Lens(signal,new Vector3(0,3.86f,-.16f),green);
            Lens(signal,new Vector3(0,3.40f,-.16f),red);
            foreach(float y in new[]{3.40f,3.86f})
            {
                Box("Rain visor over signal lens",signal,new Vector3(0,y+.12f,-.245f),new Vector3(.22f,.035f,.21f),dark,false);
                foreach(int side in new[]{-1,1})Box("Signal lens side hood",signal,new Vector3(side*.102f,y+.045f,-.215f),new Vector3(.020f,.13f,.15f),dark,false);
            }
            foreach(int side in new[]{-1,1})Box("Signal maintenance ladder stile",signal,new Vector3(side*.13f,1.72f,.13f),new Vector3(.025f,2.95f,.025f),steel,false);
            for(float y=.40f;y<3.1f;y+=.32f)Beam("Signal maintenance ladder rung",signal,new Vector3(-.14f,y,.13f),new Vector3(.14f,y,.13f),.022f,steel);
            FittedSign(id,signal,new Vector3(0,2.05f,-.13f),.47f,enamel,new Color(.08f,.09f,.08f));
            Box("Signal cable entry",signal,new Vector3(.085f,.20f,.035f),new Vector3(.04f,.40f,.055f),dark,false);
        }

        static void Lens(Transform root,Vector3 p,Material material)
        {
            var bezel=Cylinder("Signal lens bezel",root,p,.095f,.03f,steel);
            bezel.transform.localRotation=Quaternion.Euler(90,0,0);
            var lens=Cylinder("Signal optical lens",root,p+Vector3.back*.019f,.074f,.012f,material);
            lens.transform.localRotation=Quaternion.Euler(90,0,0);
        }

        static void Chainage(Transform root,Vector2 at,float yaw,string digits)
        {
            float floor=Support(at.x,at.y);
            var marker=Group("Railway hectometre marker "+digits.Replace('\n','.'),root,new Vector3(at.x,floor,at.y),yaw).transform;
            Box("Embedded kilometre post",marker,new Vector3(0,.39f,0),new Vector3(.10f,.84f,.10f),concrete,false,1.2f);
            FittedSign(digits,marker,new Vector3(0,.70f,-.015f),.43f,enamel,new Color(.07f,.08f,.07f));
        }

        static void Warning(Transform root,float x,float z,float yaw)
        {
            // Attach to an actual existing fence post; do not add another
            // fence or invent a gate on an uninterrupted security boundary.
            var nearest=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t=>t.name=="Fence post"&&Mathf.Abs(t.position.x-x)<.10f)
                .OrderBy(t=>Mathf.Abs(t.position.z-z)).FirstOrDefault();
            if(!nearest||Mathf.Abs(nearest.position.z-z)>2)throw new InvalidOperationException("Missing railway warning fence support.");
            z=nearest.position.z;float floor=Support(x,z);
            var warning=Group("Railway security notice",root,new Vector3(x+(x<118?-.055f:.055f),floor+.94f,z),yaw).transform;
            var yellow=Material("RailDetail_WarningEnamel",null,new Color(.77f,.66f,.34f),.17f);
            FittedSign("BAHNANLAGE\nBETRETEN VERBOTEN",warning,Vector3.zero,1.15f,yellow,new Color(.10f,.09f,.07f));
            foreach(int side in new[]{-1,1})Box("Fence notice mounting clamp",warning,new Vector3(0,side*.15f,.075f),new Vector3(.11f,.035f,.10f),steel,false);
        }

        static void RelayCabinet(Transform root)
        {
            const float x=134.8f,z=104.5f;
            float floor=Support(x,z);
            var cabinet=Group("Lineside relay and cable termination",root,new Vector3(x,floor,z)).transform;
            Box("Relay cabinet seated concrete plinth",cabinet,new Vector3(0,.025f,0),new Vector3(1.30f,.15f,.82f),concrete,false,1.3f);
            Box("Weatherproof relay cabinet",cabinet,new Vector3(0,.88f,0),new Vector3(1.06f,1.56f,.60f),steel,false,1.1f);
            Box("Relay cabinet folded rain cap",cabinet,new Vector3(0,1.69f,0),new Vector3(1.14f,.06f,.70f),steel,false);
            Box("Relay door joint",cabinet,new Vector3(0,.85f,-.306f),new Vector3(.012f,1.42f,.012f),dark,false);
            Cylinder("Relay cabinet key lock",cabinet,new Vector3(.09f,.90f,-.325f),.025f,.025f,dark).transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<5;i++)Box("Relay cabinet ventilation louvre",cabinet,new Vector3(-.26f,.35f+i*.045f,-.313f),new Vector3(.33f,.016f,.025f),dark,false);
            FittedSign("WB · SIGNALTECHNIK",cabinet,new Vector3(0,1.35f,-.335f),.85f,enamel,new Color(.09f,.10f,.09f));
            foreach(float side in new[]{-.30f,.30f})Cylinder("Buried relay cable entry",cabinet,new Vector3(side,.13f,.15f),.035f,.35f,dark);
        }

        static void FittedSign(string text,Transform parent,Vector3 at,float width,Material backing,Color ink)
        {
            Sign(text,parent,at,width,backing,ink);
            var label=parent.GetComponentsInChildren<TextMesh>(true).Last(t=>t.text==text);
            label.font.RequestCharactersInTexture(text,label.fontSize,label.fontStyle);
            float widest=0;
            var lines=text.Split('\n');
            foreach(string line in lines)
            {
                float advance=0;
                foreach(char glyph in line)
                    if(label.font.GetCharacterInfo(glyph,out var metrics,label.fontSize,label.fontStyle))advance+=metrics.advance;
                widest=Mathf.Max(widest,advance);
            }
            // Legacy TextMesh uses ten font pixels per world unit at
            // characterSize=1. Character count alone cannot fit a 64 px font.
            float fittedWidth=width*.82f*10/Mathf.Max(1,widest);
            float fittedHeight=.55f*.80f*10/(label.fontSize*1.25f*Mathf.Max(1,lines.Length)*label.lineSpacing);
            label.characterSize=Mathf.Min(fittedWidth,fittedHeight);
        }
    }
}
