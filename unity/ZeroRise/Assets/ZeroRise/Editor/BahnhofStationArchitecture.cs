using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    // One coherent railway-era building and its passenger furniture. The rail
    // formation and existing platform/stair navigation footprints are retained.
    internal static class BahnhofStationArchitecture
    {
        const string HallName="Bahnhof Westbruch – accessible ticket hall";
        static Material masonry,plaster,stone,steel,timber,roof,glass,dark,cream,green;
        static Transform furnishing;
        static GameObject B(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false,float tile=2)
            =>Box("BV2_"+name,parent,p,size,mat,solid,tile);
        static Material M(string id,string surface,Color color,float rough=.12f,float metal=0)
            =>Material("BV2_Station_"+id,surface,color,rough,metal);

        public static void Rebuild(Transform root)
        {
            var old=root.Find(HallName);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var extra=root.Find("BV2_Station passenger details");if(extra)UnityEngine.Object.DestroyImmediate(extra.gameObject);
            // All these are retained source objects, not anything in Westbruch.
            foreach(var item in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Platform canopy"||t.name=="Platform canopy column"||t.name=="Platform name board"||t.name=="Station street canopy luminaire"||t.name=="Platform tactile warning strip").ToArray())
                if(item)UnityEngine.Object.DestroyImmediate(item.gameObject);
            masonry=M("WarmBrick","brick",new Color(.92f,.88f,.81f));
            plaster=M("OldLimeRender","plaster",new Color(.84f,.81f,.73f),.08f);
            stone=M("CutStone","concrete",new Color(.76f,.77f,.72f),.10f);
            steel=M("PaintedIron","steel",new Color(.24f,.29f,.28f),.25f,.48f);
            timber=M("DarkTimber","wood",new Color(.48f,.43f,.34f),.13f);
            roof=BahnhofDetailKit.Load("OldRoof")??M("Slate","asphalt",new Color(.35f,.36f,.33f),.1f);
            glass=M("WindowGlass",null,new Color(.12f,.17f,.17f),.40f,.08f);
            dark=M("InteriorShadow",null,new Color(.16f,.18f,.17f),.08f);
            cream=M("LetteringCream",null,new Color(.75f,.74f,.65f),.16f);
            green=M("EnamelGreen",null,new Color(.15f,.23f,.20f),.22f,.12f);
            furnishing=Group("BV2_Station passenger details",root,Vector3.zero).transform;
            Hall(root);Platforms(root);AccessDetails(root);Train(root);
        }
        static void Hall(Transform root)
        {
            var hall=Group(HallName,root,new Vector3(140,.14f,265),-90).transform;
            // Two station wings flank the central through passage. Windows have
            // actual wall openings and deep reveals rather than glass on a box.
            B("Terrazzo hall floor",hall,new Vector3(0,-.08f,0),new Vector3(34,.16f,16),BahnhofDetailKit.Load("Paving"),true,1.8f);
            foreach(int side in new[]{-1,1})
            {
                var face=Group("BV2_Station long facade "+side,hall,new Vector3(0,0,side*7.8f),side<0?0:180).transform;
                FacadeWing(face,-10,14);FacadeWing(face,10,14);
                B("Door head masonry",face,new Vector3(0,4.72f,0),new Vector3(6,1.84f,.44f),masonry,true);
                B("Entrance lintel stone",face,new Vector3(0,3.75f,-.04f),new Vector3(6.2f,.25f,.58f),stone,true);
                foreach(float x in new[]{-3f,3f})
                {
                    B("Entrance stone pier",face,new Vector3(x,1.82f,-.04f),new Vector3(.30f,3.64f,.56f),stone,true);
                    B("Entrance base plinth",face,new Vector3(x,.25f,-.10f),new Vector3(.45f,.5f,.69f),stone);
                }
                B("Continuous station cornice",face,new Vector3(0,5.48f,-.07f),new Vector3(34.7f,.23f,.65f),stone);
                B("Timber roof fascia",face,new Vector3(0,5.72f,-.24f),new Vector3(35,.18f,.13f),timber);
                Cylinder("BV2_Half-round gutter",face,new Vector3(0,5.62f,-.44f),.07f,34.8f,steel).transform.localRotation=Quaternion.Euler(0,0,90);
            }
            foreach(int side in new[]{-1,1})
            {
                var end=Group("BV2_Station end facade "+side,hall,new Vector3(side*16.8f,0,0),side<0?90:-90).transform;
                FacadeWing(end,0,15.6f);
            }
            // One lower wing roof and a cross-gabled middle bay, never the old
            // flat roof underneath a second roof. Ends and ridge are closed.
            BahnhofDetailKit.GableRoof(hall,"BV2_StationWingRoof",35,17,5.78f,2.15f,roof,plaster);
            var centralRoof=Group("BV2_Clock middle gable",hall,Vector3.zero,90).transform;
            BahnhofDetailKit.GableRoof(centralRoof,"BV2_StationCrossGable",18.4f,7.4f,5.80f,3.15f,roof,masonry);
            foreach(float x in new[]{-3.35f,3.35f})
                B("Central bay projecting pilaster",hall,new Vector3(x,2.8f,-8.12f),new Vector3(.55f,5.6f,.95f),masonry,true);
            // Clock and station name are physically mounted on the middle bay.
            BahnhofDetailKit.Plate(hall,"WESTBRUCH",new Vector3(0,4.65f,-8.048f),0,5.25f,.70f,green,new Color(.91f,.89f,.78f));
            Clock(hall,new Vector3(0,7.20f,-9.235f));
            Canopy(hall);
            foreach(float x in new[]{-15.75f,15.75f})
            {
                Cylinder("BV2_Station drainpipe",hall,new Vector3(x,2.77f,-8.09f),.055f,5.54f,steel);
                foreach(float y in new[]{.75f,2.6f,4.55f})B("Drainpipe collar",hall,new Vector3(x,y,-8.10f),new Vector3(.19f,.055f,.15f),steel);
                B("Rainwater shoe",hall,new Vector3(x,.18f,-8.22f),new Vector3(.16f,.22f,.35f),steel);
            }
            // Interior partitions leave the full 5.4m central passage free.
            foreach(float x in new[]{-3.0f,3.0f})B("Hall side partition",hall,new Vector3(x,1.925f,0),new Vector3(.18f,3.85f,14.5f),plaster,true);
            B("Hall ceiling",hall,new Vector3(0,3.85f,0),new Vector3(6.2f,.16f,15.4f),plaster);
            foreach(float z in new[]{-4.8f,0,4.8f})
            {
                B("Interior ceiling joist",hall,new Vector3(0,3.70f,z),new Vector3(5.9f,.18f,.22f),timber);
                Fixture(hall,new Vector3(0,3.55f,z),440,8);
            }
            // Ticket counter and noticeboard are against the side wall. These
            // details cannot block the east-west path checked by the player.
            B("Ticket counter base",hall,new Vector3(-2.47f,.6f,2.5f),new Vector3(.72f,1.2f,3.8f),timber,true);
            B("Ticket counter worktop",hall,new Vector3(-2.46f,1.25f,2.5f),new Vector3(.93f,.10f,4.0f),stone);
            var counter=Group("BV2_Mounted ticket counter sign",hall,new Vector3(-2.88f,2.65f,2.5f),-90).transform;
            BahnhofDetailKit.Plate(counter,"FAHRKARTEN",Vector3.zero,0,2.8f,.42f,green,new Color(.89f,.87f,.75f));
            var board=Group("BV2_Hall mounted departures",hall,new Vector3(2.85f,2.10f,1.4f),90).transform;
            B("Departure board frame",board,Vector3.zero,new Vector3(2.25f,1.45f,.10f),steel);
            BahnhofDetailKit.Plate(board,"ABFAHRTEN",new Vector3(0,.43f,-.065f),0,2.05f,.36f,green,new Color(.89f,.87f,.75f));
            BahnhofDetailKit.Plate(board,"REGIONALBAHN\nGleis 1    /    Gleis 2",new Vector3(0,-.14f,-.065f),0,2.02f,.65f,cream,new Color(.16f,.20f,.18f));
            foreach(float z in new[]{-4.8f,5.1f})
            {
                var bench=Group("BV2_Hall bench placement",hall,new Vector3(2.15f,0,z),-90).transform;
                WestbruchProps.Bench(bench,Vector3.zero,0);
            }
            // Back sign spans a real lintel; two modest wall lamps mark the exit.
            B("Rear doorway sign mounting beam",hall,new Vector3(0,3.5f,7.7f),new Vector3(5.9f,.40f,.30f),timber,true);
            BahnhofDetailKit.Plate(hall,"ZU DEN GLEISEN",new Vector3(0,3.48f,7.515f),0,3.8f,.22f,green,new Color(.91f,.89f,.78f));
            foreach(float z in new[]{-7.25f,7.25f})Fixture(hall,new Vector3(0,3.50f,z),480,10);
        }
        static void FacadeWing(Transform face,float center,float width)
        {
            const float height=5.4f;
            B("Station stone base",face,new Vector3(center,.32f,.01f),new Vector3(width,.64f,.54f),stone,true,1.5f);
            B("Lower station brickwork",face,new Vector3(center,.91f,0),new Vector3(width,.54f,.44f),masonry,true);
            B("Upper station brickwork",face,new Vector3(center,4.64f,0),new Vector3(width,1.52f,.44f),masonry,true);
            int bays=Mathf.Max(2,Mathf.RoundToInt(width/3.5f));float pitch=width/bays;
            float cursor=center-width/2;
            for(int i=0;i<bays;i++)
            {
                float x=center-width/2+(i+.5f)*pitch,left=x-.835f;
                if(left>cursor)B("Brick window pier",face,new Vector3((cursor+left)/2,2.53f,0),new Vector3(left-cursor,2.7f,.44f),masonry,true);
                Window(face,x,2.53f,1.67f,2.55f);cursor=x+.835f;
            }
            float right=center+width/2;
            if(right>cursor)B("Brick window pier",face,new Vector3((cursor+right)/2,2.53f,0),new Vector3(right-cursor,2.7f,.44f),masonry,true);
            B("Station stringcourse",face,new Vector3(center,4.0f,-.09f),new Vector3(width,.15f,.66f),stone);
        }
        static void Window(Transform face,float x,float y,float width,float height)
        {
            // The wall opening is at z=-.22. Glass is recessed 12cm, while all
            // frames, reveal surfaces and sill have separate depth planes.
            B("Station recessed glazing",face,new Vector3(x,y,-.09f),new Vector3(width-.10f,height-.10f,.035f),glass);
            foreach(int side in new[]{-1,1})
            {
                B("Stone window jamb",face,new Vector3(x+side*(width*.5f+.07f),y,-.16f),new Vector3(.14f,height+.22f,.35f),stone);
                B("Timber window stile",face,new Vector3(x+side*(width*.5f-.035f),y,-.26f),new Vector3(.07f,height,.065f),timber);
                B("Stone window head and sill",face,new Vector3(x,y+side*(height*.5f+.07f),-.18f),new Vector3(width+.30f,.14f,.43f),stone);
                B("Timber glazing head and sill",face,new Vector3(x,y+side*(height*.5f-.035f),-.26f),new Vector3(width,.07f,.065f),timber);
            }
            B("Window central mullion",face,new Vector3(x,y,-.275f),new Vector3(.055f,height,.065f),timber);
            B("Station window transom",face,new Vector3(x,y+.40f,-.277f),new Vector3(width,.055f,.065f),timber);
            B("Projecting weathered sill",face,new Vector3(x,y-height*.5f-.12f,-.29f),new Vector3(width+.4f,.09f,.60f),stone);
        }
        static void Clock(Transform hall,Vector3 at)
        {
            var mount=Group("BV2_Clock mounting",hall,at).transform;
            var ring=Cylinder("BV2_Clock iron rim",mount,Vector3.zero,.74f,.09f,steel);ring.transform.localRotation=Quaternion.Euler(90,0,0);
            var dial=Cylinder("BV2_Clock enamel face",mount,new Vector3(0,0,-.055f),.67f,.025f,cream);dial.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;var mark=B("Clock hour index",mount,new Vector3(Mathf.Sin(a)*.56f,Mathf.Cos(a)*.56f,-.075f),new Vector3(.035f,.10f,.012f),dark);mark.transform.localRotation=Quaternion.Euler(0,0,-i*30);
            }
            var hands=Group("Station clock hands",mount,new Vector3(0,0,-.092f));hands.AddComponent<StationClockHands>();
            Box("Hour hand",hands.transform,new Vector3(0,.19f,0),new Vector3(.055f,.40f,.018f),dark,false).isStatic=false;
            Box("Minute hand",hands.transform,new Vector3(0,.28f,-.02f),new Vector3(.032f,.59f,.018f),dark,false).isStatic=false;
            var pin=Cylinder("BV2_Clock spindle",mount,new Vector3(0,0,-.12f),.045f,.03f,dark);pin.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        static void Canopy(Transform hall)
        {
            const float front=-11.3f;
            foreach(float x in new[]{-3.2f,3.2f})
            {
                Cylinder("BV2_Entrance canopy cast column",hall,new Vector3(x,1.65f,front),.065f,3.3f,steel,true);
                B("Canopy column stone foot",hall,new Vector3(x,.16f,front),new Vector3(.32f,.32f,.32f),stone,true);
                Beam("BV2_Canopy bracket",hall,new Vector3(x,2.55f,front),new Vector3(x,3.30f,front+1.0f),.045f,steel);
            }
            B("Entrance canopy front girder",hall,new Vector3(0,3.32f,front),new Vector3(7.1f,.20f,.15f),steel);
            B("Entrance canopy weather roof",hall,new Vector3(0,3.48f,-9.80f),new Vector3(7.3f,.13f,3.6f),roof,true);
            for(float x=-3.3f;x<=3.3f;x+=1.1f)B("Canopy timber rafter",hall,new Vector3(x,3.32f,-9.8f),new Vector3(.09f,.18f,3.4f),timber);
            B("Canopy gutter front",hall,new Vector3(0,3.41f,-11.59f),new Vector3(7.4f,.12f,.13f),steel);
            foreach(float x in new[]{-1.8f,1.8f})Fixture(hall,new Vector3(x,3.23f,-10.2f),520,13);
        }
        static void Fixture(Transform parent,Vector3 at,float intensity,float range)
        {
            B("Luminaire metal housing",parent,at,new Vector3(.44f,.10f,.24f),steel);
            var lens=M("WarmLampLens",null,new Color(.81f,.76f,.59f),.23f);
            lens.SetColor("_EmissiveColor",new Color(1,.78f,.49f)*1.0f);HDMaterial.ValidateMaterial(lens);EditorUtility.SetDirty(lens);
            B("Frosted luminaire lens",parent,at+Vector3.down*.06f,new Vector3(.35f,.025f,.17f),lens);
            var go=Group("Station street canopy luminaire",parent,at+Vector3.down*.085f);go.transform.localRotation=Quaternion.Euler(90,0,0);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=range;light.spotAngle=112;light.innerSpotAngle=85;light.lightUnit=LightUnit.Candela;light.intensity=intensity;light.useColorTemperature=true;light.colorTemperature=3100;light.shadows=LightShadows.None;
            var hd=go.AddComponent<HDAdditionalLightData>();hd.fadeDistance=270;hd.volumetricDimmer=.02f;
        }
        static void Platforms(Transform root)
        {
            var concrete=M("PlatformConcrete","concrete",new Color(.80f,.81f,.76f),.09f);
            var warning=M("TactileIvory",null,new Color(.66f,.64f,.48f),.08f);
            var platformBodies=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Station platform").ToArray();
            foreach(var body in platformBodies)
            {
                var renderer=body.GetComponent<MeshRenderer>();if(renderer)renderer.sharedMaterial=concrete;
                // Replace the top material with a properly seated wearing slab.
                var p=body.localPosition;p.y=3.46f;body.localPosition=p;body.localScale=new Vector3(1,7.02f/7.1f,1);
                float x=body.position.x;bool east=x>118;float outer=x+(east?1:-1)*2.30f,edge=x+(east?-1:1)*2.30f;
                B("Platform wearing slab",furnishing,new Vector3(x,7.01f,274),new Vector3(4.6f,.08f,108),concrete,true,1.5f);
                B("Platform stone coping",furnishing,new Vector3(edge+(east?.20f:-.20f),7.0425f,274),new Vector3(.40f,.055f,108),stone,false,1.2f);
                B("Continuous platform safety line",furnishing,new Vector3(edge+(east?.65f:-.65f),7.056f,274),new Vector3(.10f,.012f,108),warning);
                for(float z=222;z<327;z+=1.2f)
                    for(int stripe=0;stripe<5;stripe++)B("Tactile edge rib",furnishing,new Vector3(edge+(east?.38f:-.38f),7.064f,z+stripe*.12f),new Vector3(.22f,.015f,.035f),warning);
                float posts=x+(east?1.15f:-1.15f);
                for(float z=242;z<=306;z+=16)
                {
                    Cylinder("BV2_Platform canopy round column",furnishing,new Vector3(posts,8.60f,z),.075f,3.10f,steel,true);
                    B("Canopy column bolted base",furnishing,new Vector3(posts,7.095f,z),new Vector3(.30f,.09f,.30f),steel);
                    B("Canopy cross girder",furnishing,new Vector3(x,10.12f,z),new Vector3(4.8f,.20f,.13f),steel);
                    Beam("BV2_Canopy knee brace",furnishing,new Vector3(posts,9.35f,z),new Vector3(x-(east?.8f:-.8f),10.02f,z),.065f,steel);
                    Fixture(furnishing,new Vector3(x,10.02f,z),450,15);
                }
                foreach(float dx in new[]{-2.30f,0,2.30f})B("Longitudinal canopy purlin",furnishing,new Vector3(x+dx,10.26f+(east?-dx:dx)*.035f,274),new Vector3(.11f,.16f,72.6f),steel);
                var canopy=B("Platform standing seam roof",furnishing,new Vector3(x,10.40f,274),new Vector3(5.0f,.12f,73.2f),roof,true);canopy.transform.rotation=Quaternion.Euler(0,0,east?-2:2);
                for(float dx=-2.35f;dx<=2.35f;dx+=.47f)B("Canopy folded standing seam",furnishing,new Vector3(x+dx,10.48f+(east?-dx:dx)*.035f,274),new Vector3(.025f,.045f,73),steel);
                B("Canopy outer gutter",furnishing,new Vector3(outer,10.38f,274),new Vector3(.13f,.13f,73.2f),steel);
                foreach(float z in new[]{243f,304})Cylinder("BV2_Platform downpipe",furnishing,new Vector3(outer,8.69f,z),.04f,3.28f,steel);
                // Outer railing is fixed beyond the walking strip, with real posts.
                foreach(var span in east?new[]{new Vector2(220,221.7f),new Vector2(226.3f,239.2f),new Vector2(242.7f,328)}:new[]{new Vector2(220,328)})
                {
                    for(float z=span.x;z<=span.y;z+=2.4f)Cylinder("BV2_Platform back railing post",furnishing,new Vector3(outer,7.60f,z),.025f,1.10f,steel);
                    foreach(float y in new[]{7.45f,7.85f,8.15f})Beam("BV2_Platform back safety rail",furnishing,new Vector3(outer,y,span.x),new Vector3(outer,y,span.y),.032f,steel);
                }
                foreach(float z in new[]{250f,286})
                {
                    // Two hangers attach each name board to the canopy structure.
                    foreach(float dx in new[]{-.9f,.9f})Cylinder("BV2_Platform board hanger",furnishing,new Vector3(x+dx,9.93f,z),.012f,.54f,steel);
                    BahnhofDetailKit.Plate(furnishing,"WESTBRUCH",new Vector3(x,9.48f,z),0,3.2f,.45f,green,new Color(.91f,.89f,.78f));
                    B("Platform number board bracket",furnishing,new Vector3(posts,9.45f,z+.095f),new Vector3(.075f,.06f,.18f),steel);
                    BahnhofDetailKit.Plate(furnishing,east?"1":"2",new Vector3(posts,9.45f,z+.15f),180,.45f,.50f,green,new Color(.91f,.89f,.78f));
                }
                foreach(var bench in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Worn public bench"&&t.position.y>6&&Mathf.Abs(t.position.x-x)<1).ToArray())
                {var b=bench.position;b.x=x+(east?1.15f:-1.15f);bench.position=b;}
                // Information cases are freestanding, behind the walking line.
                float infoX=x+(east?1.20f:-1.20f);
                foreach(float z in new[]{233f,311})
                {
                    var info=Group("BV2_Platform information case",furnishing,new Vector3(infoX,0,z),east?90:-90).transform;
                    foreach(float dx in new[]{-.40f,.40f})Cylinder("BV2_Information case post",info,new Vector3(dx,8.05f,0),.028f,2,steel);
                    B("Timetable cabinet",info,new Vector3(0,8.8f,0),new Vector3(1.1f,1.3f,.13f),steel);
                    BahnhofDetailKit.Plate(info,"FAHRPLAN",new Vector3(0,9.14f,-.075f),0,.98f,.24f,cream,new Color(.18f,.22f,.20f));
                    B("Timetable paper",info,new Vector3(0,8.63f,-.085f),new Vector3(.88f,.68f,.015f),cream);
                    for(int row=0;row<7;row++)B("Printed timetable row",info,new Vector3(0,8.88f-row*.08f,-.099f),new Vector3(.75f,.012f,.005f),dark);
                }
            }
        }
        static void AccessDetails(Transform root)
        {
            // Railing infill and floor edge trim finish the existing overbridge.
            foreach(float z in new[]{321.55f,324.45f})
            {
                Beam("BV2_Passenger bridge mid rail",furnishing,new Vector3(103.5f,12.35f,z),new Vector3(126.5f,12.35f,z),.035f,steel);
                for(float x=103.65f;x<126.4f;x+=.19f)Cylinder("BV2_Passenger bridge infill",furnishing,new Vector3(x,12.40f,z),.012f,.82f,steel);
                B("Bridge steel edge stringer",furnishing,new Vector3(115,11.57f,z),new Vector3(23,.36f,.18f),steel);
            }
            // Guard the far edge of each landing without closing its access.
            foreach(var line in new[]{new Vector4(121,222,134.0f,222),new Vector4(125,226,138,226),new Vector4(122.4f,239.5f,129.2f,239.5f)})
            {
                float y=line.y<230?7.05f:7.05f;var a=new Vector3(line.x,y,line.y);var b=new Vector3(line.z,y,line.w);
                int count=Mathf.CeilToInt(Vector3.Distance(a,b)/1.4f);for(int i=0;i<=count;i++)Cylinder("BV2_Landing guard post",furnishing,Vector3.Lerp(a,b,i/(float)count)+Vector3.up*.55f,.025f,1.1f,steel);
                foreach(float h in new[]{.45f,1.1f})Beam("BV2_Landing guardrail",furnishing,a+Vector3.up*h,b+Vector3.up*h,.035f,steel);
            }
            // Every diagonal stair handrail now has vertical supporting posts.
            for(int i=0;i<=38;i+=4)
            {
                float z=254-i*.34f,y=.14f+6.91f*i/38;
                foreach(float x in new[]{126.83f,129.17f})Cylinder("BV2_Main stair railing baluster",furnishing,new Vector3(x,y+.52f,z),.023f,1.04f,steel);
            }
            foreach(float x in new[]{106.9f,123f})for(int i=0;i<=26;i+=3)
            {
                float z=313+i*.34f,y=7.05f+4.82f*i/26;
                foreach(float side in new[]{-.97f,.97f})Cylinder("BV2_Bridge stair railing baluster",furnishing,new Vector3(x+side,y+.52f,z),.023f,1.04f,steel);
            }
            // The access ramp reads as a supported concrete structure, not a
            // floating paving strip. Columns terminate at its actual underside.
            for(float z=142;z<224;z+=12)
            {
                float top=Mathf.Lerp(.14f,7.05f,(z-130)/94)-.20f;
                B("Ramp supporting pier",furnishing,new Vector3(136,(top-.035f)/2,z),new Vector3(.40f,top+.035f,.65f),stone,true);
                B("Ramp crosshead",furnishing,new Vector3(136,top-.10f,z),new Vector3(3.0f,.20f,.70f),stone,true);
            }
            // Rear exit is a clear, roofed passage into the existing stair lobby.
            B("Rear station access canopy",furnishing,new Vector3(130,3.4f,265),new Vector3(4,.14f,5.7f),roof,true);
            foreach(float z in new[]{262.6f,267.4f})Cylinder("BV2_Rear station canopy post",furnishing,new Vector3(128.2f,1.75f,z),.055f,3.22f,steel,true);
            Fixture(furnishing,new Vector3(130,3.25f,265),550,10);
        }
        static void Train(Transform root)
        {
            var old=root.Find("Regional train – parked at platform one");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var train=Group("Regional train – parked at platform one",root,new Vector3(118,6.66f,263)).transform;
            var body=M("RegionalBody",null,new Color(.65f,.66f,.58f),.36f,.27f);
            var stripe=M("RegionalGreen",null,new Color(.13f,.25f,.23f),.30f,.25f);
            var window=M("TrainGlass",null,new Color(.075f,.13f,.14f),.57f,.12f);
            var rubber=M("Bogie",null,new Color(.075f,.08f,.075f),.15f,.42f);
            var lens=M("TrainLampLens",null,new Color(.77f,.78f,.66f),.40f);
            foreach(float z in new[]{-11.2f,11.2f})
            {
                var car=Group("BV2_Shaped regional railcar",train,new Vector3(0,0,z)).transform;
                TrainBody(car,body);
                B("Train sill underframe",car,new Vector3(0,.70f,0),new Vector3(2.5f,.35f,20),rubber);
                foreach(float x in new[]{-1.445f,1.445f})
                {
                    B("Regional waist livery",car,new Vector3(x,1.35f,0),new Vector3(.025f,.38f,19.4f),stripe);
                    for(float z1=-8.7f;z1<9;z1+=2.3f)
                    {
                        if(Mathf.Abs(z1-6.45f)<1.6f||Mathf.Abs(z1+6.45f)<1.6f)continue;
                        B("Train window rubber surround",car,new Vector3(x,2.45f,z1),new Vector3(.045f,1.0f,1.78f),rubber);
                        B("Train passenger glazing",car,new Vector3(x+Mathf.Sign(x)*.027f,2.45f,z1),new Vector3(.025f,.87f,1.62f),window);
                        B("Window fine mullion",car,new Vector3(x+Mathf.Sign(x)*.042f,2.45f,z1),new Vector3(.018f,.88f,.035f),steel);
                    }
                    foreach(float z1 in new[]{-6.45f,6.45f})
                    {
                        B("Passenger door surround",car,new Vector3(x,1.90f,z1),new Vector3(.05f,2.12f,1.38f),rubber);
                        B("Closed train double doors",car,new Vector3(x+Mathf.Sign(x)*.032f,1.90f,z1),new Vector3(.028f,2.0f,1.24f),stripe);
                        foreach(float side in new[]{-.31f,.31f})B("Door upper glazing",car,new Vector3(x+Mathf.Sign(x)*.05f,2.37f,z1+side),new Vector3(.025f,.78f,.44f),window);
                        B("Door centre seal",car,new Vector3(x+Mathf.Sign(x)*.052f,1.90f,z1),new Vector3(.018f,2.0f,.03f),rubber);
                        B("Door threshold step",car,new Vector3(x+Mathf.Sign(x)*.12f,.69f,z1),new Vector3(.26f,.12f,1.3f),steel);
                    }
                }
                foreach(float z1 in new[]{-10.82f,10.82f})
                {
                    float s=Mathf.Sign(z1);
                    B("Cab windscreen surround",car,new Vector3(0,2.41f,z1),new Vector3(2.17f,.97f,.045f),rubber);
                    B("Cab front glazing",car,new Vector3(0,2.43f,z1+s*.03f),new Vector3(2.02f,.83f,.025f),window);
                    B("Cab windscreen mullion",car,new Vector3(0,2.43f,z1+s*.048f),new Vector3(.045f,.85f,.015f),steel);
                    foreach(float x in new[]{-.94f,.94f})
                    {
                        B("Railcar headlamp casing",car,new Vector3(x,1.36f,z1),new Vector3(.33f,.27f,.09f),rubber);
                        B("Railcar headlamp lens",car,new Vector3(x,1.36f,z1+s*.06f),new Vector3(.25f,.19f,.035f),lens);
                        Beam("BV2_Windscreen wiper",car,new Vector3(x*.5f,2.15f,z1+s*.052f),new Vector3(x*.5f+.22f,2.68f,z1+s*.052f),.022f,steel);
                    }
                    B("Cab buffer beam",car,new Vector3(0,.95f,z1+s*.12f),new Vector3(2.3f,.25f,.20f),stripe);
                    B("Automatic rail coupling",car,new Vector3(0,.64f,z1+s*.48f),new Vector3(.35f,.30f,.70f),rubber);
                }
                foreach(float zz in new[]{-6.5f,6.5f})
                {
                    B("Bogie frame",car,new Vector3(0,.51f,zz),new Vector3(2.30f,.35f,2.70f),rubber);
                    foreach(float z1 in new[]{zz-.82f,zz+.82f})foreach(float x in new[]{-1.2f,1.2f})
                    {
                        var wheel=Cylinder("BV2_Rail wheel",car,new Vector3(x,.43f,z1),.43f,.19f,rubber);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                        var hub=Cylinder("BV2_Bearing cap",car,new Vector3(x+Mathf.Sign(x)*.11f,.43f,z1),.15f,.06f,steel);hub.transform.localRotation=Quaternion.Euler(0,0,90);
                    }
                }
                foreach(float z1 in new[]{-3f,3f})
                {
                    B("Low train roof ventilation",car,new Vector3(0,3.43f,z1),new Vector3(1.75f,.15f,2.4f),steel);
                    for(float zz=-.95f;zz<=.95f;zz+=.19f)B("Roof vent grille",car,new Vector3(0,3.51f,z1+zz),new Vector3(1.5f,.025f,.055f),rubber);
                }
                var collider=car.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.95f,0);collider.size=new Vector3(2.80f,2.5f,21.6f);
            }
            B("Flexible inter-car bellows",train,new Vector3(0,1.95f,0),new Vector3(2.5f,2.30f,.90f),rubber);
            for(float z=-.38f;z<=.39f;z+=.13f)B("Bellows folded ribs",train,new Vector3(0,1.95f,z),new Vector3(2.60f,2.40f,.045f),steel);
        }
        static void TrainBody(Transform parent,Material material)
        {
            var profile=new[]{new Vector2(-1.25f,.78f),new Vector2(-1.43f,1.02f),new Vector2(-1.43f,2.94f),new Vector2(-1.18f,3.36f),new Vector2(1.18f,3.36f),new Vector2(1.43f,2.94f),new Vector2(1.43f,1.02f),new Vector2(1.25f,.78f)};
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float z in new[]{-10.8f,-9.8f,9.8f,10.8f})foreach(var p in profile)
            {float inset=Mathf.Abs(z)>10?.10f:0;v.Add(new Vector3(p.x-Mathf.Sign(p.x)*inset,p.y-(p.y>2.9f?inset:0),z));}
            for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++)
            {
                int a=ring*8+i,b=ring*8+(i+1)%8,c=(ring+1)*8+i,d=(ring+1)*8+(i+1)%8;
                t.AddRange(new[]{a,c,b,b,c,d});
            }
            for(int i=1;i<7;i++){t.AddRange(new[]{0,i,i+1});t.AddRange(new[]{24,24+i+1,24+i});}
            // Orient each triangle from the body centre; the railcar is convex.
            for(int i=0;i<t.Count;i+=3)
            {
                var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3-new Vector3(0,2,0))>=0)continue;
                int q=t[i+1];t[i+1]=t[i+2];t[i+2]=q;
            }
            BahnhofDetailKit.Mesh("BV2_RailcarBody",parent,v,t,material,false);
        }
    }
}
