using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ZeroRise
{
    public sealed class ZeroRiseGame : MonoBehaviour
    {
        private GameState state;
        private ZeroRiseWorldView view;
        private float lookYaw, lookPitch=16, velocityY, vehicleSpeed;
        private float saveAge, noticeAge;
        private string notice="", focusName="", focusId="";
        private bool playing, mapOpen, shopOpen, garageOpen, bankOpen;
        private Landmark? waypoint;
        private readonly CultureInfo culture = new CultureInfo("de-AT");
        private readonly HashSet<int> collected = new HashSet<int>();
        private GUIStyle label, headline, card, small, button, title;
        private Texture2D fill;
        private Vector3 smoothCamera;

        private void Awake()
        {
            state=ZeroRiseSave.Read()??GameState.New(false);
            Build(state);
            playing=false;
        }

        private void Build(GameState incoming)
        {
            if(view!=null)Destroy(view.gameObject);
            state=incoming;state.Normalize();
            collected.Clear();
            foreach(int i in state.collectedBottleIds)collected.Add(i);
            var root=new GameObject("ZERO // RISE world");
            view=root.AddComponent<ZeroRiseWorldView>();
            view.Build(state);
            if(state.riding && view.Vehicle!=null)
            {
                view.Controller.enabled=false;
                view.PlayerVisual.gameObject.SetActive(false);
                view.Player.transform.position=view.Vehicle.transform.position;
            }
            lookYaw=state.yaw;lookPitch=16;
            smoothCamera=view.Player.transform.position+new Vector3(0,3,-7);
            view.Camera.transform.position=smoothCamera;
            UpdateCamera(1);
        }

        private void StartStory() {Build(GameState.New(false));playing=true;Status("Dein Anfang: 0 €. Sammle Pfand und finde Arbeit.");Save();}
        private void StartSandbox() {Build(GameState.New(true));playing=true;Status("Freies Spiel: 100.000 € Startkapital.");Save();}
        private void Continue() {playing=true;Status("Willkommen zurück in Lindenstadt.");}
        private void Save()
        {
            state.collectedBottleIds=new int[collected.Count];collected.CopyTo(state.collectedBottleIds);
            try{ZeroRiseSave.Write(state);}catch(Exception ex){Debug.LogError(ex);Status("Speichern fehlgeschlagen: "+ex.Message);}
        }
        private void Load()
        {
            GameState loaded=ZeroRiseSave.Read();
            if(loaded==null){Status("Kein gültiger Spielstand gefunden.");return;}
            Build(loaded);playing=true;Status("Spielstand geladen.");
        }
        private void Status(string text)
        {
            notice=text;noticeAge=5.5f;
            Debug.Log("[ZERO // RISE] "+text);
        }

        private void Update()
        {
            float dt=Mathf.Min(Time.deltaTime,.08f);
            if(noticeAge>0)noticeAge-=dt;
            if(!playing||view==null)return;
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(shopOpen||garageOpen||bankOpen||mapOpen){shopOpen=garageOpen=bankOpen=mapOpen=false;}
                else {playing=false;Save();}
            }
            if(Input.GetKeyDown(KeyCode.M))mapOpen=!mapOpen;
            if(Input.GetKeyDown(KeyCode.F5)){Save();Status("Spielstand gespeichert.");}
            if(Input.GetKeyDown(KeyCode.F9)){Load();return;}
            if(Input.GetKeyDown(KeyCode.Alpha1))Eat();
            if(Input.GetKeyDown(KeyCode.Alpha2))Drink();

            bool paused=mapOpen||shopOpen||garageOpen||bankOpen;
            if(!paused)
            {
                if(Input.GetKeyDown(KeyCode.F))ToggleVehicle();
                if(state.riding)Drive(dt);else Walk(dt);
                if(Input.GetKeyDown(KeyCode.E))Interact();
                TickSurvival(dt);
                FindFocus();
            }
            else focusName="";
            ReadCameraMouse();
            UpdateCamera(dt);
            saveAge+=dt;
            if(saveAge>45){saveAge=0;Save();}
        }

        private static float Axis(KeyCode negative, KeyCode positive)
            => (Input.GetKey(positive)?1:0)-(Input.GetKey(negative)?1:0);

        private void Walk(float dt)
        {
            if(view.Controller==null)return;
            float x=Axis(KeyCode.A,KeyCode.D),z=Axis(KeyCode.S,KeyCode.W);
            Vector3 input=Vector3.ClampMagnitude(new Vector3(x,0,z),1);
            bool run=Input.GetKey(KeyCode.LeftShift)&&state.needs.energy>8;
            float speed=run?5.2f:3.4f;
            Vector3 world=Quaternion.Euler(0,lookYaw,0)*input;
            if(world.sqrMagnitude>.01f)
                view.Player.transform.rotation=Quaternion.Slerp(view.Player.transform.rotation,
                    Quaternion.LookRotation(world),Mathf.Min(1,dt*11));
            if(view.Controller.isGrounded&&velocityY<0)velocityY=-1.5f;
            if(Input.GetKeyDown(KeyCode.Space)&&view.Controller.isGrounded)velocityY=5.5f;
            velocityY-=17f*dt;
            Vector3 move=world*speed+Vector3.up*velocityY;
            view.Controller.Move(move*dt);
            Vector3 p=view.Player.transform.position;
            p.x=Mathf.Clamp(p.x,ZeroRiseWorldData.MinX+2,ZeroRiseWorldData.MaxX-2);
            p.z=Mathf.Clamp(p.z,ZeroRiseWorldData.MinZ+2,ZeroRiseWorldData.MaxZ-2);
            view.Player.transform.position=p;
            state.x=p.x;state.z=p.z;state.yaw=view.Player.transform.eulerAngles.y;
            view.PlayerVisual.Animate(dt,input.magnitude,run);
            if(run&&input.sqrMagnitude>.01f)state.needs.energy=Mathf.Max(0,state.needs.energy-.012f*dt);
        }

        private void Drive(float dt)
        {
            if(!state.hasVehicle||view.Vehicle==null){state.riding=false;return;}
            float throttle=Axis(KeyCode.S,KeyCode.W);
            float steering=Axis(KeyCode.A,KeyCode.D);
            bool braking=Input.GetKey(KeyCode.Space);
            float max=state.vehicleType=="sport"?24:state.vehicleType=="van"?15:state.vehicleType=="bike"?8.5f:17;
            float acceleration=state.vehicleType=="sport"?8f:state.vehicleType=="bike"?3f:5.3f;
            float target=braking?0:throttle*max*(throttle<0?.42f:1);
            vehicleSpeed=Mathf.MoveTowards(vehicleSpeed,target,dt*(braking?19f:throttle==0?6f:acceleration));
            if(Mathf.Abs(vehicleSpeed)>.1f)
            {
                view.Vehicle.transform.Rotate(0,steering*dt*75f*Mathf.Sign(vehicleSpeed)*
                    Mathf.Clamp01(Mathf.Abs(vehicleSpeed)/6f),0);
            }
            Vector3 direction=view.Vehicle.transform.forward;
            float travel=vehicleSpeed*dt;
            bool obstructed=false;
            if(Mathf.Abs(travel)>.001f)
            {
                Vector3 origin=view.Vehicle.transform.position+Vector3.up*.8f;
                var hits=Physics.RaycastAll(origin,direction*Mathf.Sign(travel),Mathf.Abs(travel)+1.65f);
                foreach(var hit in hits)
                    if(!hit.collider.transform.IsChildOf(view.Vehicle.transform)&&
                       !hit.collider.transform.IsChildOf(view.Player.transform)&&
                       hit.collider.bounds.max.y>.65f){obstructed=true;break;}
            }
            if(obstructed)vehicleSpeed=0;
            else
            {
                Vector3 future=view.Vehicle.transform.position+direction*travel;
                if(future.x>ZeroRiseWorldData.MinX+2&&future.x<ZeroRiseWorldData.MaxX-2&&
                   future.z>ZeroRiseWorldData.MinZ+2&&future.z<ZeroRiseWorldData.MaxZ-2)
                {
                    view.Vehicle.transform.position=future;
                    view.VehicleVisual.RotateWheels(Mathf.Abs(travel));
                    if(state.vehicleType!="bike")
                    {
                        state.vehicleFuel=Mathf.Max(0,state.vehicleFuel-Mathf.Abs(travel)*.0008f);
                        state.vehicleCondition=Mathf.Max(0,state.vehicleCondition-Mathf.Abs(travel)*.00006f);
                    }
                }
                else vehicleSpeed=0;
            }
            if(state.vehicleFuel<=0&&state.vehicleType!="bike")vehicleSpeed=0;
            if(state.vehicleCondition<=0)vehicleSpeed=0;
            var pos=view.Vehicle.transform.position;
            state.vehicleX=pos.x;state.vehicleZ=pos.z;state.vehicleYaw=view.Vehicle.transform.eulerAngles.y;
            view.VehicleVisual.UpdateCondition(state.vehicleCondition);
            view.Player.transform.position=pos+Vector3.up*.2f;
            state.x=pos.x;state.z=pos.z;
        }

        private void ToggleVehicle()
        {
            if(state.riding)
            {
                state.riding=false;vehicleSpeed=0;
                Vector3 candidate=view.Vehicle.transform.position+view.Vehicle.transform.right*2f+Vector3.up*.65f;
                view.Controller.enabled=false;
                view.Player.transform.position=candidate;
                view.Controller.enabled=true;view.PlayerVisual.gameObject.SetActive(true);
                state.x=candidate.x;state.z=candidate.z;
                Status("Fahrzeug verlassen.");
                return;
            }
            if(!state.hasVehicle||view.Vehicle==null||Vector3.Distance(view.Player.transform.position,view.Vehicle.transform.position)>4f)return;
            state.riding=true;view.PlayerVisual.gameObject.SetActive(false);
            view.Controller.enabled=false;
            view.Player.transform.position=view.Vehicle.transform.position;
            Status("Fahren: WASD, Leertaste bremsen, F aussteigen.");
        }

        private void ReadCameraMouse()
        {
            if(!Input.GetMouseButton(1))return;
            lookYaw+=Input.GetAxis("Mouse X")*3f;
            lookPitch=Mathf.Clamp(lookPitch-Input.GetAxis("Mouse Y")*2.5f,-10,60);
        }
        private void UpdateCamera(float dt)
        {
            if(view?.Camera==null)return;
            Transform focus=state.riding&&view.Vehicle!=null?view.Vehicle.transform:view.Player.transform;
            Vector3 anchor=focus.position+Vector3.up*(state.riding?1.4f:1.55f);
            float desiredDistance=state.riding?9.2f:6.7f;
            Vector3 orbit=Quaternion.Euler(lookPitch,lookYaw,0)*new Vector3(0,0,-desiredDistance);
            Vector3 direction=orbit.normalized;
            float distance=desiredDistance;
            foreach(var hit in Physics.SphereCastAll(anchor,.22f,direction,desiredDistance))
            {
                if(view.Player!=null&&hit.collider.transform.IsChildOf(view.Player.transform))continue;
                if(view.Vehicle!=null&&hit.collider.transform.IsChildOf(view.Vehicle.transform))continue;
                if(hit.distance>.1f)distance=Mathf.Min(distance,hit.distance-.18f);
            }
            distance=Mathf.Max(state.riding?3f:1.9f,distance);
            Vector3 candidate=anchor+direction*distance;
            smoothCamera=Vector3.Lerp(smoothCamera,candidate,1-Mathf.Exp(-dt*10));
            view.Camera.transform.position=smoothCamera;
            view.Camera.transform.rotation=Quaternion.LookRotation(anchor-smoothCamera,Vector3.up);
        }

        private Landmark? NearbyLocation(float maxDistance=3.5f)
        {
            Landmark? best=null;float distance=maxDistance;
            foreach(var p in ZeroRiseWorldData.Places)
            {
                if(p.kind=="landmark")continue;
                float d=Vector2.Distance(new Vector2(state.x,state.z),p.position);
                if(d<distance){best=p;distance=d;}
            }
            return best;
        }

        private void FindFocus()
        {
            focusId="";focusName="";
            if(state.riding){focusName="F · Aussteigen";return;}
            float best=2.15f;
            foreach(var b in view.Bottles)
                if(b && !collected.Contains(b.id))
                {
                    float d=Vector2.Distance(new Vector2(state.x,state.z),
                        new Vector2(b.transform.position.x,b.transform.position.z));
                    if(d<best){best=d;focusId="bottle:"+b.id;focusName="E · Pfandflasche sammeln";}
                }
            var place=NearbyLocation(best<2.15f?best:3.5f);
            if(place.HasValue&&best>=2.15f){focusId=place.Value.id;focusName="E · "+place.Value.name;}
            if(state.hasVehicle&&view.Vehicle!=null&&Vector3.Distance(view.Player.transform.position,
                view.Vehicle.transform.position)<3.8f)focusName+="  |  F · Einsteigen";
        }

        private void Interact()
        {
            if(focusId.StartsWith("bottle:",StringComparison.Ordinal))
            {
                int id=int.Parse(focusId.Substring(7));
                if(!collected.Add(id))return;
                state.bottles++;state.collectedBottleIds=new int[collected.Count];
                collected.CopyTo(state.collectedBottleIds);
                foreach(var b in view.Bottles)if(b&&b.id==id){Destroy(b.gameObject);break;}
                Status("Pfandflasche gefunden · 0,25 € Pfand.");
                CheckQuests();return;
            }
            var site=NearbyLocation();
            if(!site.HasValue)return;
            string id=site.Value.id;
            if(state.courierActive&&id==state.courierDestination)
            {
                state.courierActive=false;state.courierDestination="";state.jobs++;
                state.money+=3200;state.earned+=3200;Status("Kurierauftrag abgeschlossen. +32,00 €");
                CheckQuests();return;
            }
            switch(id)
            {
                case "recycle":
                    if(state.bottles<1){Status("Du hast keine Pfandflaschen.");return;}
                    state.money+=state.bottles*25;state.returned+=state.bottles;
                    Status(state.bottles+" Flaschen zurückgegeben.");state.bottles=0;CheckQuests();break;
                case "market":shopOpen=true;break;
                case "shelter":
                    if(state.money<500){Status("Übernachtung kostet 5 €. Kostenloses Wasser: Taste 2 nach Kauf oder bei Anlaufstelle E.");state.needs.thirst=100;return;}
                    state.money-=500;Rest();break;
                case "jobs":
                    if(state.courierActive){Status("Lieferung läuft nach "+state.courierDestination);return;}
                    state.courierActive=true;
                    string[] destinations={"deliveryA","deliveryB","deliveryC","deliveryNord","deliveryWerk"};
                    state.courierDestination=destinations[state.day%destinations.Length];
                    Status("Kurierauftrag angenommen. Ziel auf der Karte markieren (M).");break;
                case "garage":garageOpen=true;break;
                case "bank":bankOpen=true;break;
                case "home":case "stationHome":
                    if(state.hasHome){Rest();return;}
                    if(state.money<12000){Status("Ein Zimmer kostet mindestens 120 € Kaution.");return;}
                    state.money-=12000;state.hasHome=true;Status("Du hast dein erstes Zimmer gemietet!");CheckQuests();break;
                case "cafe":case "agency":case "school":case "depot":
                    Status(site.Value.name+": In der Unity-Portierung noch nicht spielbar.");break;
                default:Status(site.Value.name+" entdeckt.");break;
            }
        }
        private void TickSurvival(float dt)
        {
            // 24 minutes of real-time = one in-game day, matching browser time scaling.
            state.playedSeconds+=dt;
            state.minute+=dt;
            view.UpdateDaylight(state.minute);
            if(state.minute>=1440)
            {state.minute-=1440;state.day++;Status("Neuer Spieltag: "+state.day);Save();}
            state.needs.hunger=Mathf.Max(0,state.needs.hunger-dt*.011f);
            state.needs.thirst=Mathf.Max(0,state.needs.thirst-dt*.015f);
            state.needs.energy=Mathf.Max(0,state.needs.energy-dt*.015f);
            if(state.needs.hunger==0||state.needs.thirst==0)
                state.needs.health=Mathf.Max(0,state.needs.health-dt*.07f);
            if(state.needs.health<=0){state.needs.health=65;state.needs.hunger=50;state.needs.thirst=50;
                state.needs.energy=50;state.money=Mathf.Max(0,state.money-1500);
                view.Controller.enabled=false;view.Player.transform.position=new Vector3(-68,.5f,-12);
                view.Controller.enabled=true;state.x=-68;state.z=-12;Status("Zusammengebrochen. Die Anlaufstelle hat dir geholfen.");}
        }
        private void Rest()
        {
            state.minute+=480;while(state.minute>=1440){state.minute-=1440;state.day++;}
            state.needs.energy=95;state.needs.stress=Mathf.Max(0,state.needs.stress-15);
            state.needs.hunger=Mathf.Max(0,state.needs.hunger-12);
            state.needs.thirst=Mathf.Max(0,state.needs.thirst-16);
            state.sleepCount++;Status("Acht Stunden geschlafen.");CheckQuests();
        }
        private void Eat()
        {
            if(state.food>0)
            {
                state.food--;state.needs.hunger=Mathf.Min(100,state.needs.hunger+38);
                Status("Sandwich gegessen.");
            }
            else if(state.rolls>0)
            {
                state.rolls--;state.needs.hunger=Mathf.Min(100,state.needs.hunger+14);
                Status("Backshop-Semmel gegessen.");
            }
            else {Status("Kein verzehrfertiges Essen im Rucksack.");return;}
            state.consumed++;CheckQuests();
        }
        private void Drink()
        {
            if(state.water<=0)
            {
                if(Vector2.Distance(new Vector2(state.x,state.z),new Vector2(-68,-12))<4)
                {state.needs.thirst=100;Status("Kostenloses Trinkwasser.");}
                else Status("Kein Wasser im Rucksack.");
                return;
            }
            state.water--;state.bottles++;state.consumed++;state.needs.thirst=Mathf.Min(100,state.needs.thirst+45);
            Status("Wasser getrunken. Leere Pfandflasche erhalten.");CheckQuests();
        }
        private void Purchase(string id,int price)
        {
            if(state.money<price){Status("Nicht genug Bargeld.");return;}
            state.money-=price;
            if(id=="water")state.water++;
            else if(id=="roll")state.rolls++;
            else state.food++;
            Status(id+" gekauft für "+Currency(price));
        }
        private void BuyVehicle(string type, int price)
        {
            if(state.hasVehicle){Status("Du besitzt bereits ein Fahrzeug (Prototyp-Limit).");return;}
            if(state.money<price){Status("Dafür reicht dein Geld nicht.");return;}
            state.money-=price;state.hasVehicle=true;state.vehicleType=type;
            state.vehicleX=79;state.vehicleZ=16;state.vehicleYaw=180;
            state.vehicleFuel=type=="bike"?100:45;state.vehicleCondition=100;
            view.MakeVehicle(state);garageOpen=false;
            Status("Fahrzeug gekauft. F neben dem Fahrzeug zum Einsteigen.");
        }
        private void CheckQuests()
        {
            int[] rewards={500,800,1200,5000,3500,10000};
            bool progressing=true;
            while(progressing&&state.questIndex<rewards.Length)
            {
                bool completed=false;
                switch(state.questIndex)
                {
                    case 0:completed=collected.Count>=5;break;
                    case 1:completed=state.returned>=5;break;
                    case 2:completed=state.consumed>=1;break;
                    case 3:completed=state.jobs>=1;break;
                    case 4:completed=state.sleepCount>=1;break;
                    case 5:completed=state.hasHome;break;
                }
                if(completed)
                {
                    state.money+=rewards[state.questIndex];state.earned+=rewards[state.questIndex];
                    state.questIndex++;Status("Etappenziel erreicht! Belohnung "+Currency(rewards[state.questIndex-1]));
                }
                else progressing=false;
            }
        }

        private string Currency(int cents) => (cents/100f).ToString("N2",culture)+" €";
        private void InitStyles()
        {
            if(fill!=null)return;
            fill=new Texture2D(1,1);fill.SetPixel(0,0,Color.white);fill.Apply();
            card=new GUIStyle(GUI.skin.box){fontSize=14,alignment=TextAnchor.UpperLeft};
            card.normal.textColor=Color.white;
            label=new GUIStyle(GUI.skin.label){fontSize=16,normal={textColor=Art.Hex(0xe3e9e6)}};
            small=new GUIStyle(label){fontSize=12};
            headline=new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold};
            title=new GUIStyle(label){fontSize=42,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){fontSize=16,fixedHeight=43};
        }
        private void Panel(Rect r,Color c)
        {
            Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,fill);GUI.color=old;
        }
        private bool ActionButton(Rect r,string text) => GUI.Button(r,text,button);
        private void OnGUI()
        {
            InitStyles();
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,
                (Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(!playing)
            {
                Panel(new Rect(0,0,1600,900),new Color(.035f,.09f,.12f,.94f));
                GUI.Label(new Rect(75,90,750,55),"ZERO // RISE",title);
                GUI.Label(new Rect(75,170,780,60),"Jeder Aufstieg hat einen Anfang.",headline);
                GUI.Label(new Rect(78,235,680,42),"LINDENSTADT · UNITY 6 PORTIERUNG / SPIELBARER PROTOTYP",small);
                if(ZeroRiseSave.Read()!=null&&ActionButton(new Rect(80,345,350,52),"Weiterspielen"))Continue();
                if(ActionButton(new Rect(80,407,350,52),"Bei null beginnen · 0 €"))StartStory();
                if(ActionButton(new Rect(80,470,350,52),"Freies Spiel · 100.000 €"))StartSandbox();
                GUI.Label(new Rect(80,570,760,110),
                    "WASD bewegen · rechte Maustaste Kamera · Shift sprinten · E interagieren\n"+
                    "F Fahrzeug · M Karte · 1 Essen · 2 Trinken · F5 Speichern · F9 Laden",label);
                GUI.Label(new Rect(80,760,900,100),
                    "WICHTIG: Dies ist eine erste native Unity-Grundlage. Café, Werkstatt, Kleidung,\n"+
                    "vollständige Innenräume und die Original-Spielstände sind noch nicht 1:1 portiert.",small);
                return;
            }

            Panel(new Rect(16,16,448,123),new Color(.045f,.115f,.14f,.90f));
            GUI.Label(new Rect(30,24,405,32),"ZERO // RISE  ·  "+ZeroRiseWorldData.DistrictAt(state.x,state.z),headline);
            GUI.Label(new Rect(30,62,260,27),"TAG "+state.day+" · "+
                (int)(state.minute/60)+":"+Mathf.FloorToInt(state.minute%60).ToString("00"),label);
            GUI.Label(new Rect(285,60,168,29),Currency(state.money),headline);
            string[] quests={"Sammle 5 Pfandflaschen.","Gib 5 Pfandflaschen ab.",
                "Kaufe und verwende Lebensmittel.","Erledige deinen ersten Job.",
                "Schlafe in einer Unterkunft.","Miete ein Zimmer.","Weiter aufsteigen ..."};
            GUI.Label(new Rect(30,102,420,26),quests[Mathf.Clamp(state.questIndex,0,quests.Length-1)],small);

            Panel(new Rect(17,708,410,173),new Color(.045f,.115f,.14f,.88f));
            GUI.Label(new Rect(30,718,372,24),"ÜBERLEBEN",headline);
            NeedBar(30,753,"Gesundheit",state.needs.health,Art.Hex(0xbe6862));
            NeedBar(30,778,"Hunger",state.needs.hunger,Art.Hex(0xcdad6e));
            NeedBar(30,803,"Durst",state.needs.thirst,Art.Hex(0x7299be));
            NeedBar(30,828,"Energie",state.needs.energy,Art.Hex(0x7ba582));
            GUI.Label(new Rect(30,859,370,18),
                "Pfand: "+state.bottles+"  ·  Essen: "+(state.food+state.rolls)+"  ·  Wasser: "+state.water,small);

            Panel(new Rect(1170,18,412,96),new Color(.045f,.115f,.14f,.88f));
            GUI.Label(new Rect(1184,29,380,26),focusName.Length>0?focusName:"WASD · Bewegen / Rechtsklick · Kamera",label);
            GUI.Label(new Rect(1184,64,380,24),"M Karte  ·  F5 Speichern  ·  Esc Pause",small);
            if(state.courierActive)GUI.Label(new Rect(1184,89,380,22),
                "Lieferziel: "+state.courierDestination,small);
            if(noticeAge>0)
            {
                Panel(new Rect(490,19,670,65),new Color(.13f,.19f,.2f,.93f));
                GUI.Label(new Rect(506,36,635,36),notice,label);
            }
            if(state.riding)
            {
                Panel(new Rect(1190,763,387,110),new Color(.045f,.115f,.14f,.88f));
                GUI.Label(new Rect(1210,777,335,29),"FAHRZEUG",headline);
                GUI.Label(new Rect(1210,814,335,44),"Tank: "+state.vehicleFuel.ToString("F1")+
                    " l  ·  Zustand: "+state.vehicleCondition.ToString("F0")+" %",label);
            }
            if(mapOpen)DrawMap();
            if(shopOpen)DrawShop();
            if(garageOpen)DrawGarage();
            if(bankOpen)DrawBank();
        }

        private void NeedBar(float x,float y,string text,float value,Color color)
        {
            GUI.Label(new Rect(x,y,130,20),text,small);
            Panel(new Rect(x+136,y+4,165,11),new Color(.22f,.31f,.31f,.95f));
            Panel(new Rect(x+136,y+4,165*value/100f,11),color);
            GUI.Label(new Rect(x+311,y,45,20),Mathf.RoundToInt(value).ToString(),small);
        }
        private void DrawMap()
        {
            Panel(new Rect(285,120,1025,672),new Color(.04f,.11f,.14f,.97f));
            GUI.Label(new Rect(310,145,480,42),"LINDENSTADT · STADTKARTE",headline);
            if(ActionButton(new Rect(1110,140,170,38),"Schließen"))mapOpen=false;
            Rect map=new Rect(310,200,635,555);
            Panel(map,new Color(.19f,.29f,.29f,1));
            Vector2 Project(Vector2 v)=>new Vector2(map.x+(v.x-ZeroRiseWorldData.MinX)/700*map.width,
                map.y+(v.y-ZeroRiseWorldData.MinZ)/510*map.height);
            foreach(var road in ZeroRiseWorldData.Roads)
            {
                Vector2 a=Project(new Vector2(road.x-road.width/2,road.z-road.depth/2));
                Vector2 b=Project(new Vector2(road.x+road.width/2,road.z+road.depth/2));
                Panel(new Rect(a.x,a.y,b.x-a.x,b.y-a.y),Art.Hex(0x4e6266));
            }
            foreach(var building in ZeroRiseWorldData.Core)
            {
                Vector2 a=Project(new Vector2(building.x-building.width/2,building.z-building.depth/2));
                Vector2 b=Project(new Vector2(building.x+building.width/2,building.z+building.depth/2));
                Panel(new Rect(a.x,a.y,b.x-a.x,b.y-a.y),Art.Hex(0xa9b5ac));
            }
            var self=Project(new Vector2(state.x,state.z));
            Panel(new Rect(self.x-5,self.y-5,10,10),Art.Hex(0xf4d188));
            if(waypoint.HasValue)
            {
                var target=Project(waypoint.Value.position);
                Panel(new Rect(target.x-5,target.y-5,10,10),Art.Hex(0xe08b66));
            }
            int index=0;
            foreach(var site in ZeroRiseWorldData.Places)
            {
                if(site.kind=="landmark")continue;
                if(index>=20)break;
                int column=index/10,row=index%10;index++;
                if(GUI.Button(new Rect(970+column*158,201+row*49,148,43),site.name,
                    new GUIStyle(GUI.skin.button){fontSize=11}))
                {waypoint=site;Status("Ziel markiert: "+site.name);}
            }
        }

        private void DrawShop()
        {
            Panel(new Rect(510,180,580,470),new Color(.045f,.13f,.16f,.98f));
            GUI.Label(new Rect(542,207,506,38),"MARKT 24 · LEBENSMITTEL",headline);
            GUI.Label(new Rect(542,254,505,24),"Bargeld: "+Currency(state.money),label);
            if(ActionButton(new Rect(545,303,500,51),"Backshop-Semmel  ·  1,00 €"))Purchase("roll",100);
            if(ActionButton(new Rect(545,364,500,51),"Mineralwasser  ·  1,00 €"))Purchase("water",100);
            if(ActionButton(new Rect(545,425,500,51),"Käsesandwich  ·  2,50 €"))Purchase("sandwich",250);
            if(ActionButton(new Rect(545,533,500,54),"Fertig / Laden verlassen"))shopOpen=false;
        }
        private void DrawGarage()
        {
            Panel(new Rect(510,165,580,548),new Color(.045f,.13f,.16f,.98f));
            GUI.Label(new Rect(542,195,515,36),"MOBILWERK · FAHRZEUGE",headline);
            GUI.Label(new Rect(542,247,490,27),state.hasVehicle?
                "Du besitzt bereits ein Fahrzeug.":"Bargeld: "+Currency(state.money),label);
            if(ActionButton(new Rect(540,292,510,51),"Stadtrad  ·  125,00 €"))BuyVehicle("bike",12500);
            if(ActionButton(new Rect(540,354,510,51),"Alter Kleinwagen  ·  900,00 €"))BuyVehicle("car",90000);
            if(ActionButton(new Rect(540,416,510,51),"Lieferwagen  ·  1.800,00 €"))BuyVehicle("van",180000);
            if(ActionButton(new Rect(540,478,510,51),"Sportcoupé  ·  6.500,00 €"))BuyVehicle("sport",650000);
            if(ActionButton(new Rect(540,600,510,48),"Schließen"))garageOpen=false;
        }
        private void DrawBank()
        {
            Panel(new Rect(510,250,580,365),new Color(.045f,.13f,.16f,.98f));
            GUI.Label(new Rect(542,280,490,38),"STADTBANK",headline);
            GUI.Label(new Rect(542,335,490,35),"Bargeld: "+Currency(state.money),label);
            GUI.Label(new Rect(542,377,490,35),"Bankkonto: "+Currency(state.bank),label);
            if(ActionButton(new Rect(540,429,230,49),"100 € einzahlen"))
            {if(state.money>=10000){state.money-=10000;state.bank+=10000;}else Status("Zu wenig Bargeld.");}
            if(ActionButton(new Rect(786,429,256,49),"100 € abheben"))
            {if(state.bank>=10000){state.bank-=10000;state.money+=10000;}else Status("Nicht genug Guthaben.");}
            if(ActionButton(new Rect(540,529,504,50),"Schließen"))bankOpen=false;
        }
    }
}
