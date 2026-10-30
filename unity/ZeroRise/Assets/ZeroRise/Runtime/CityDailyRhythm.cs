using UnityEngine;

namespace ZeroRise
{
    // Uses the world clock for demand; movement and native avoidance continue
    // at their original real-time rates, including when testing 20x game time.
    [DefaultExecutionOrder(-100)]
    public sealed class CityDailyRhythm : MonoBehaviour
    {
        public static CityDailyRhythm Instance { get; private set; }
        public int TargetPedestrians { get; private set; }
        public int ActivePedestrians { get; private set; }
        public int TargetTraffic { get; private set; }
        public int ActiveTraffic { get; private set; }
        public int ActiveDeliveryVans { get; private set; }
        public int RetiredResidents { get; private set; }
        public int ReturnedResidents { get; private set; }
        public int ParkedVehicles { get; private set; }
        public int ReturnedVehicles { get; private set; }
        public int MorningVanRouteChoices { get; private set; }
        public float CurrentHour=>WorldWeatherClock.Instance?WorldWeatherClock.Instance.Hour:12;
        public bool MorningDeliveries=>Running&&CurrentHour>=6&&CurrentHour<10.5f;
        public float SideStreetSpeed=>Running?Mathf.Lerp(.78f,1,WorldWeatherClock.Instance.Daylight):1;
        bool Running=>WorldWeatherClock.Instance;
        WestbruchTraffic traffic;
        PedestrianWorld pedestrians;
        ThirdPersonMotor player;
        Camera view;
        float refreshAt;

        void OnEnable()=>Instance=this;
        void OnDisable(){if(Instance==this)Instance=null;}
        void Start()
        {
            traffic=FindAnyObjectByType<WestbruchTraffic>();pedestrians=FindAnyObjectByType<PedestrianWorld>();player=FindAnyObjectByType<ThirdPersonMotor>();
            view=Camera.main;if(!view)view=FindAnyObjectByType<Camera>();refreshAt=Time.unscaledTime+2;
            if(pedestrians&&pedestrians.people!=null)TargetPedestrians=Mathf.Max(1,Mathf.RoundToInt(pedestrians.people.Length*PedestrianDemand(CurrentHour)));
            if(traffic&&traffic.vehicles!=null)TargetTraffic=Mathf.Max(1,Mathf.RoundToInt(traffic.vehicles.Length*TrafficDemand(CurrentHour)));
            CountPopulation();
        }
        void Update()
        {
            if(!Running||Time.unscaledTime<refreshAt)return;
            refreshAt=Time.unscaledTime+.75f;RefreshPopulation();
        }
        public void RefreshPopulation()
        {
            if(!Running)return;
            if(!traffic)traffic=FindAnyObjectByType<WestbruchTraffic>();if(!pedestrians)pedestrians=FindAnyObjectByType<PedestrianWorld>();
            if(!player)player=FindAnyObjectByType<ThirdPersonMotor>();
            if(pedestrians&&pedestrians.people!=null)
            {
                TargetPedestrians=Mathf.Clamp(Mathf.RoundToInt(pedestrians.people.Length*PedestrianDemand(CurrentHour)),1,pedestrians.people.Length);
                int changes=0;
                foreach(var person in pedestrians.people)
                {
                    if(!person)continue;bool wanted=WantsResident(person);
                    if(wanted&&person.DailyOffDuty&&CanChangePopulation(person.transform.position,40)&&person.TryDailyWake()){ReturnedResidents++;changes++;}
                    else if(!wanted&&!person.DailyOffDuty&&person.AtHomeDoor&&CanChangePopulation(person.transform.position,40)&&person.TryDailyRest()){RetiredResidents++;changes++;}
                    if(changes>=2)break;
                }
            }
            if(traffic&&traffic.vehicles!=null)
            {
                TargetTraffic=Mathf.Clamp(Mathf.RoundToInt(traffic.vehicles.Length*TrafficDemand(CurrentHour)),1,traffic.vehicles.Length);
                foreach(var car in traffic.vehicles)
                {
                    if(!car)continue;bool wanted=WantsVehicle(car);
                    if(wanted&&car.DailyParked&&CanChangePopulation(car.transform.position,95)&&traffic.TryDailyReturn(car)){ReturnedVehicles++;break;}
                    if(!wanted&&car.DailyActive&&CanChangePopulation(car.transform.position,95)&&traffic.TryDailyPark(car)){ParkedVehicles++;break;}
                }
            }
            CountPopulation();
        }
        void CountPopulation()
        {
            ActivePedestrians=0;if(pedestrians&&pedestrians.people!=null)foreach(var person in pedestrians.people)if(person&&person.gameObject.activeInHierarchy)ActivePedestrians++;
            ActiveTraffic=traffic?traffic.ActiveVehicles:0;ActiveDeliveryVans=0;if(traffic&&traffic.vehicles!=null)foreach(var car in traffic.vehicles)if(car&&car.DailyActive&&car.kind==3)ActiveDeliveryVans++;
        }
        bool CanChangePopulation(Vector3 position,float minimumDistance)
        {
            // On-camera actors always finish normally. Density can lag the
            // clock while the player watches a street or a home entrance.
            if(player&&(player.transform.position-position).sqrMagnitude<minimumDistance*minimumDistance)return false;
            if(!view)return false;
            var point=view.WorldToViewportPoint(position+Vector3.up);
            return point.z<0||point.x<-.2f||point.x>1.2f||point.y<-.2f||point.y>1.2f;
        }
        public bool WantsResident(PedestrianAgent person)
        {
            if(!Running||!pedestrians||pedestrians.people==null||TargetPedestrians<=0)return true;
            for(int i=0;i<pedestrians.people.Length;i++)if(pedestrians.people[i]==person)return i<TargetPedestrians;
            return true;
        }
        public bool WantsVehicle(TrafficVehicle car)
        {
            if(!Running||!traffic||traffic.vehicles==null||TargetTraffic<=0)return true;
            // Existing compact cars continue through the day; the three
            // existing cargo vans make the morning supply traffic visible.
            int rank=0;
            if(car.kind==3)foreach(var other in traffic.vehicles)if(other&&other.kind!=3)rank++;
            foreach(var other in traffic.vehicles)
            {
                if(other==car)return rank<TargetTraffic;
                if(other&&((other.kind==3)==(car.kind==3)))rank++;
            }
            return true;
        }
        public float RouteWeight(TrafficVehicle car,bool sideStreet)
        {
            if(!Running)return 1;
            if(MorningDeliveries&&car&&car.kind==3)return sideStreet?2.1f:.9f;
            float quiet=Mathf.Max(Mathf.InverseLerp(17.5f,22,CurrentHour),1-Mathf.InverseLerp(5,7,CurrentHour));
            return sideStreet?Mathf.Lerp(1,.18f,quiet):Mathf.Lerp(1,1.6f,quiet);
        }
        public void RecordRouteChoice(TrafficVehicle car,bool sideStreet)
        {if(MorningDeliveries&&car&&car.kind==3&&sideStreet)MorningVanRouteChoices++;}
        public float PlacePreference(PedestrianAgent person,PedestrianWorld.Activity activity)
        {
            if(!Running)return 0;
            if(!WantsResident(person))return activity==PedestrianWorld.Activity.Visit?22:-8;
            bool lunch=CurrentHour>=11&&CurrentHour<15;
            if(lunch&&(activity==PedestrianWorld.Activity.Shop||activity==PedestrianWorld.Activity.Talk))return 2.5f;
            if(CurrentHour>=18&&CurrentHour<22&&activity==PedestrianWorld.Activity.Talk)return 2;
            return 0;
        }
        public float ActivityDurationScale(PedestrianWorld.Activity activity)
        {
            if(!Running)return 1;
            if(CurrentHour>=10&&CurrentHour<17)return activity==PedestrianWorld.Activity.Visit?.72f:.82f;
            return CurrentHour<6||CurrentHour>=22?1.5f:1.08f;
        }
        public static float PedestrianDemand(float hour)
        {
            hour=Mathf.Repeat(hour,24);
            if(hour<5)return .28f;
            if(hour<10)return Mathf.Lerp(.28f,.89f,Mathf.SmoothStep(0,1,(hour-5)/5));
            if(hour<12)return Mathf.Lerp(.89f,1,(hour-10)/2);
            if(hour<17)return 1;
            if(hour<20)return Mathf.Lerp(1,.56f,(hour-17)/3);
            if(hour<23)return Mathf.Lerp(.56f,.28f,(hour-20)/3);
            return .28f;
        }
        public static float TrafficDemand(float hour)
        {
            hour=Mathf.Repeat(hour,24);
            if(hour<5)return .5f;
            if(hour<7)return Mathf.Lerp(.5f,1,(hour-5)/2);
            if(hour<10)return 1;
            if(hour<17)return .83f;
            if(hour<20)return Mathf.Lerp(.83f,.67f,(hour-17)/3);
            if(hour<23)return Mathf.Lerp(.67f,.5f,(hour-20)/3);
            return .5f;
        }
    }
}
