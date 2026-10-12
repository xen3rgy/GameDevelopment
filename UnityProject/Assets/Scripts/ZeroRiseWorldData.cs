using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    public readonly struct Road
    {
        public readonly string name;
        public readonly float x, z, width, depth;
        public Road(string name, float x, float z, float w, float d)
        { this.name = name; this.x = x; this.z = z; width = w; depth = d; }
    }

    public readonly struct Building
    {
        public readonly string name, district;
        public readonly float x, z, width, depth, height;
        public Building(string name, float x, float z, float w, float d, float h, string district)
        { this.name = name; this.x = x; this.z = z; width = w; depth = d; height = h; this.district = district; }
    }

    public readonly struct Landmark
    {
        public readonly string id, name, kind;
        public readonly Vector2 position;
        public Landmark(string id, string name, string kind, float x, float z)
        { this.id = id; this.name = name; this.kind = kind; position = new Vector2(x, z); }
    }

    public static class ZeroRiseWorldData
    {
        // Coordinates transcribed from dist/city-layout.js, dist/data.js and dist/expansion-layout.js.
        public const float MinX = -260, MaxX = 440, MinZ = -255, MaxZ = 255;
        public static readonly Road[] Roads =
        {
            new Road("Lindenallee", -50,-65,350,13), new Road("Lindenallee", -50,0,350,13),
            new Road("Am Kanal", -50,65,350,13),
            new Road("Westbogen",-202,0,13,160), new Road("Bahnhofstraße",-110,0,13,250),
            new Road("Lindenstraße",0,0,13,250), new Road("Parkring",110,0,13,250),
            new Road("Nordring",0,-175,464,13), new Road("Werksstraße",0,175,464,13),
            new Road("Kontorallee",274,0,302,13), new Road("Hafenbogen",274,-65,302,13),
            new Road("Uferallee",274,65,302,13), new Road("Campusallee",316,-175,208,13),
            new Road("Höhenallee",316,175,208,13), new Road("Ostring",220,0,13,474),
            new Road("Höhenweg",400,0,13,414), new Road("Nordbogen",-202,-153,13,176),
            new Road("Werksbogen",-202,153,13,176), new Road("Westkai Nord",-110,-177,13,116),
            new Road("Westkai Süd",-110,177,13,116), new Road("Lindenstraße Nord",0,-177,13,116),
            new Road("Lindenstraße Süd",0,177,13,116), new Road("Parkring Nord",110,-177,13,116),
            new Road("Parkring Süd",110,177,13,116), new Road("Ahornstraße",0,-230,438,9),
            new Road("Speditionsweg",0,230,438,9),new Road("Forschungsgasse",305,-207.5f,9,65),
            new Road("Gartenstieg",310,207.5f,9,65),new Road("Kontorpassage",310,32.5f,9,65),
            new Road("Werkhof",165,202.5f,9,55)
        };

        public static readonly Building[] Core =
        {
            new Building("MARKT 24",-36,-28,24,28,15,"Innenstadt"),
            new Building("ANLAUFSTELLE NORD",-70,-28,26,28,20,"Innenstadt"),
            new Building("KIEZ & KURIER",27,-28,26,28,17,"Innenstadt"),
            new Building("CAFÉ MORGEN",65,-28,29,28,22,"Innenstadt"),
            new Building("LINDENHÖFE",-35,28,29,28,24,"Lindenhöfe"),
            new Building("CAMPUS WEST",-74,28,28,28,17,"Lindenhöfe"),
            new Building("STADTBANK",33,28,31,28,27,"Innenstadt"),
            new Building("MOBILWERK",76,29,27,29,19,"Innenstadt"),
            new Building("WESTHAFEN LOGISTIK",-71,-91,32,26,11,"Westhafen"),
            new Building("NORDKONTOR",30,-93,28,30,30,"Westhafen"),
            new Building("STUDIO NORD",68,-91,27,26,24,"Westhafen"),
            new Building("ATELIER AM KANAL",-68,94,30,32,17,"Kanalviertel"),
            new Building("BUCHHANDLUNG KAPITEL",29,94,28,32,19,"Kanalviertel"),
            new Building("BAHNHOF LINDENSTADT WEST",-175,-42,36,18,10,"Bahnhofsviertel"),
            new Building("KIOSK AM GLEIS",-179,-19,9,8,4.2f,"Bahnhofsviertel"),
            new Building("GLEISHÖFE",-179,29,28,20,17,"Bahnhofsviertel"),
            new Building("WERKSTATT WEST",-174,52,22,12,7,"Bahnhofsviertel")
        };

        public static readonly Landmark[] Places =
        {
            new Landmark("recycle","Pfandrückgabe","recycle",-23,-12),
            new Landmark("market","MARKT 24","shop",-36,-12),
            new Landmark("shelter","Anlaufstelle Nord","shelter",-68,-12),
            new Landmark("jobs","Kiez & Kurier","jobs",26,-12),
            new Landmark("cafe","Café Morgen","cafe",63,-12),
            new Landmark("home","Lindenhöfe","home",-34,12),
            new Landmark("school","Campus West","school",-72,12),
            new Landmark("garage","Mobilwerk","garage",76,12),
            new Landmark("bank","Stadtbank","bank",31,12),
            new Landmark("depot","Westhafen Logistik","depot",-70,-76),
            new Landmark("agency","Studio Nord","agency",65,-76),
            new Landmark("park","Lindenpark","park",65,75.7f),
            new Landmark("station","Bahnhof Lindenstadt West","station",-175,-31),
            new Landmark("stationHome","Gleishöfe","home",-179,17),
            new Landmark("deliveryA","Buchhandlung Kapitel","delivery",28,76),
            new Landmark("deliveryB","Atelier am Kanal","delivery",-68,76),
            new Landmark("deliveryC","Nordkontor","delivery",29,-76),
            new Landmark("deliveryNord","Nordring Buch & Papier","delivery",-83,-163),
            new Landmark("deliveryWerk","Südwerk Ersatzteillager","delivery",-83,187),
            new Landmark("deliveryOst","Kontorhaus Empfang","delivery",264,-53),
            new Landmark("deliveryTech","Campus Poststelle","delivery",263,-163),
            new Landmark("deliveryHoehen","Lindenblick Gärtnerei","delivery",356,163),
            new Landmark("nordCourt","Nordring Sporthof","landmark",-55,-203),
            new Landmark("nordGarden","Ahornhöfe","landmark",55,-202),
            new Landmark("loadingYard","Südwerk Verladehof","landmark",-48,202),
            new Landmark("railYard","Altes Stellwerk","landmark",-155,204),
            new Landmark("kontorSquare","Kontorplatz","landmark",288,-25),
            new Landmark("campusGarden","Forum der Ideen","landmark",355,-210),
            new Landmark("overlook","Lindenblick","landmark",357,216)
        };

        public static string DistrictAt(float x, float z)
        {
            if (z < -120 && x < 120) return "NORDSTADT";
            if (z > 120 && x < 220) return "SÜDWERK";
            if (x > 220 && z < -115) return "TECHPARK";
            if (x > 220 && z > 115) return "HÖHENVIERTEL";
            if (x > 120) return "OSTKONTOR";
            if (x < -120) return "BAHNHOFSVIERTEL";
            if (z < -48) return "WESTHAFEN";
            if (z > 48) return "KANALVIERTEL";
            return x > 15 ? "INNENSTADT" : "LINDENHÖFE";
        }

        public static bool OnRoad(float x, float z)
        {
            foreach (var r in Roads)
                if (Mathf.Abs(x-r.x) <= r.width*.5f+1 && Mathf.Abs(z-r.z) <= r.depth*.5f+1) return true;
            return false;
        }

        public static bool InExistingBuilding(float x,float z, float margin = 0)
        {
            foreach (var b in Core)
                if (Mathf.Abs(x-b.x) < b.width*.5f+margin && Mathf.Abs(z-b.z) < b.depth*.5f+margin) return true;
            return false;
        }

        public static IEnumerable<Building> OuterBuildings()
        {
            // Purposefully simplified from the source's procedural layout.
            // Same neighborhoods/coordinate ranges; not a one-to-one world geometry port.
            var regions = new (string name, float minX,float maxX,float minZ,float maxZ,float w,float d,float h)[]
            {
                ("Nordstadt",-250,110,-248,-125,20,20,17),
                ("Südwerk",-250,200,125,245,34,25,10),
                ("Ostkontor",125,430,-110,110,24,22,28),
                ("Techpark",225,430,-248,-125,25,21,18),
                ("Höhenviertel",225,430,125,245,18,17,12)
            };
            foreach (var reg in regions)
            {
                int index = 0;
                for (float x = reg.minX+22; x < reg.maxX-12; x += reg.w + 14)
                for (float z = reg.minZ+19; z < reg.maxZ-12; z += reg.d + 13)
                {
                    index++;
                    float fx = x + (index%3-1)*2;
                    float fz = z + (index%2)*2;
                    if (OnRoad(fx,fz)) continue;
                    bool clash = false;
                    foreach (var road in Roads)
                        if (Mathf.Abs(fx-road.x) < (reg.w+road.width)*.5f+3 &&
                            Mathf.Abs(fz-road.z) < (reg.d+road.depth)*.5f+3){clash=true;break;}
                    if (clash) continue;
                    foreach (var l in Places)
                        if (l.kind=="landmark" && Mathf.Abs(fx-l.position.x)<26 && Mathf.Abs(fz-l.position.y)<23){clash=true;break;}
                    if (clash) continue;
                    yield return new Building("",fx,fz,reg.w,reg.d,reg.h + index%3*2,reg.name);
                }
            }
        }
    }
}
