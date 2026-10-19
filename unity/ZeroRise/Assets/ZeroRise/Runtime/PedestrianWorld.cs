using System;
using UnityEngine;
using UnityEngine.AI;

namespace ZeroRise
{
    [DefaultExecutionOrder(-220)] public sealed class PedestrianWorld:MonoBehaviour
    {
        public enum Activity { Sit, Shop, Wait, Phone, Talk, Stretch, Visit }
        [Serializable] public sealed class Place
        {
            public string label;public Activity activity;public Vector3 approach,seat;public Quaternion facing;public int partner=-1;
            [NonSerialized] public PedestrianAgent owner;
        }
        [Serializable] public sealed class Crossing
        {
            public Vector3 a,b;[NonSerialized] public PedestrianAgent owner;
        }
        public NavMeshData navigation;public Place[] places;public Crossing[] crossings;public PedestrianAgent[] people;
        public static PedestrianWorld Active { get; private set; }
        NavMeshDataInstance instance;NavMeshLinkInstance[] links;System.Random random=new System.Random(82031);
        readonly Collider[] seatContacts=new Collider[16];
        public int completedActivities,crossedRoads,blockedCrossings,replans;
        public void LoadNavigation()
        {
            if(instance.valid)return;instance=NavMesh.AddNavMeshData(navigation);links=new NavMeshLinkInstance[crossings.Length];
            for(int i=0;i<links.Length;i++)links[i]=NavMesh.AddLink(new NavMeshLinkData{startPosition=crossings[i].a,endPosition=crossings[i].b,width=1.6f,bidirectional=true,costModifier=4,agentTypeID=0,area=3});
        }
        void OnEnable()
        {
            Active=this;if(navigation)LoadNavigation();
            // Serialized agents start disabled: native agent activation must
            // follow registration of the runtime NavMesh, including in an EXE.
            if(Application.isPlaying&&people!=null)foreach(var person in people)if(person&&person.agent)person.agent.enabled=true;
        }
        void OnDisable()
        {if(Application.isPlaying&&people!=null)foreach(var person in people)if(person&&person.agent)person.agent.enabled=false;UnloadNavigation();}
        public void UnloadNavigation()
        {
            if(instance.valid)instance.Remove();if(links!=null)foreach(var link in links)if(link.valid)link.Remove();
            if(places!=null)foreach(var place in places)place.owner=null;
            if(crossings!=null)foreach(var crossing in crossings)crossing.owner=null;
            if(Active==this)Active=null;
        }
        public int Choose(PedestrianAgent person)
        {
            int selected=-1;float best=float.NegativeInfinity;
            for(int i=0;i<places.Length;i++)
            {
                var place=places[i];if(place.owner||i==person.previousPlace||i==person.destination||!SeatAvailable(place,person))continue;
                float distance=Vector3.Distance(person.transform.position,place.approach);if(distance<5||distance>220)continue;
                if(!person.CanReach(place.approach))continue;
                float preference=place.activity==Activity.Sit?Mathf.Min(5,person.walkedSinceRest/40):place.activity==Activity.Visit&&i%3==person.personality%3?2:0;
                if(place.activity==Activity.Talk&&place.partner>=0&&places[place.partner].owner)preference+=4;
                float score=(float)random.NextDouble()*3+preference-distance*.013f;
                if(score>best){best=score;selected=i;}
            }
            if(selected>=0)places[selected].owner=person;return selected;
        }
        public void Release(PedestrianAgent person)
        {foreach(var place in places)if(place.owner==person)place.owner=null;foreach(var crossing in crossings)if(crossing.owner==person)crossing.owner=null;}
        public bool SeatAvailable(Place place,PedestrianAgent person)
        {
            if(place.activity!=Activity.Sit)return true;
            int count=Physics.OverlapCapsuleNonAlloc(place.seat+Vector3.up*.22f,place.seat+Vector3.up*.88f,.32f,seatContacts,(1<<2)|(1<<9),QueryTriggerInteraction.Ignore);
            if(count==seatContacts.Length)return false;for(int i=0;i<count;i++)if(!seatContacts[i].transform.IsChildOf(person.transform))return false;return true;
        }
        public int FindCrossing(Vector3 at)
        {int selected=-1;float distance=3;for(int i=0;i<crossings.Length;i++){float d=Mathf.Min(Vector3.Distance(at,crossings[i].a),Vector3.Distance(at,crossings[i].b));if(d<distance){distance=d;selected=i;}}return selected;}
        public bool ReserveCrossing(int index,PedestrianAgent person)
        {
            if(index<0)return false;var crossing=crossings[index];if(crossing.owner&&crossing.owner!=person)return false;
            var traffic=FindAnyObjectByType<WestbruchTraffic>();
            if(traffic)foreach(var car in traffic.vehicles)
            {
                float gap=SegmentDistance(car.transform.position,crossing.a,crossing.b)-car.length*.5f;
                if(gap<Mathf.Max(3,car.speed*car.speed/9.6f+car.speed*.35f+1.5f)){blockedCrossings++;return false;}
            }
            var driven=FindAnyObjectByType<SimpleCar>();
            if(driven)
            {
                float speed=driven.GetComponent<Rigidbody>().linearVelocity.magnitude;
                if(SegmentDistance(driven.transform.position,crossing.a,crossing.b)<3+speed*speed/8){blockedCrossings++;return false;}
            }
            crossing.owner=person;return true;
        }
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {p.y=a.y=b.y=0;var ab=b-a;return Vector3.Distance(p,a+ab*Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude));}
        public bool BlocksTraffic(Vector3 position,float radius)
        {foreach(var crossing in crossings)if(crossing.owner&&SegmentDistance(position,crossing.a,crossing.b)<radius+.65f)return true;return false;}
        public float Duration(Activity activity,int personality)
        {return activity==Activity.Sit?22+personality%5*4:activity==Activity.Wait?16+personality%4*3:activity==Activity.Talk?15:8+personality%5*2;}
    }
}
