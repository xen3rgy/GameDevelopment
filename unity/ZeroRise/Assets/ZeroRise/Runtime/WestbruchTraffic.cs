using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroRise
{
    // Directed right-hand lanes, sampled turning paths and one conflict-zone
    // owner per junction. All decisions use the same pre-movement snapshot.
    [DefaultExecutionOrder(-180)]
    public sealed class WestbruchTraffic:MonoBehaviour
    {
        [Serializable] public struct Link { public int a,b;public float offset,speed; }
        public Vector3[] nodes;
        public Link[] links;
        public TrafficVehicle[] vehicles;
        public const float StopOffset=9,MinimumGap=2.4f;
        int[] owners;
        List<int>[] neighbours;
        System.Random random;
        float clock;
        ThirdPersonMotor player;
        readonly Collider[] contacts=new Collider[40];
        readonly float[] decisions=new float[32];
        public int BlockedReservations { get; private set; }
        public int YieldEvents { get; private set; }
        public float SimulatedTime=>clock;
        public void Initialize(int seed=17301)
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();random=new System.Random(seed);clock=0;BlockedReservations=YieldEvents=0;
            owners=new int[nodes.Length];neighbours=new List<int>[nodes.Length];
            for(int i=0;i<nodes.Length;i++){owners[i]=-1;neighbours[i]=new List<int>();}
            foreach(var edge in links){neighbours[edge.a].Add(edge.b);neighbours[edge.b].Add(edge.a);}
            for(int i=0;i<vehicles.Length;i++)
            {
                var car=vehicles[i];int link=i%links.Length;var edge=links[link];
                car.from=i%2==0?edge.a:edge.b;car.to=i%2==0?edge.b:edge.a;car.next=Choose(car.from,car.to);
                car.turning=false;car.heldJunction=-1;car.speed=car.desiredSpeed=0;car.progress=10+(i/links.Length)*24;
                car.travelled=car.waitingTime=car.maxWait=0;car.waitingSince=-1;car.completedTurns=car.leftTurns=car.rightTurns=car.straightMoves=0;
                PlanCurve(car);Evaluate(car,car.progress,out var at,out var heading);car.Pose(at,heading,0,0,.02f,true);
            }
            Physics.SyncTransforms();
        }
        void Start(){Initialize();}
        void FixedUpdate(){Step(Time.fixedDeltaTime);}
        int Choose(int previous,int junction)
        {
            var choices=neighbours[junction];int straight=-1;var incoming=(nodes[junction]-nodes[previous]).normalized;
            foreach(int node in choices)if(node!=previous&&Vector3.Dot(incoming,(nodes[node]-nodes[junction]).normalized)>.9f)straight=node;
            if(straight>=0&&random.NextDouble()<.56)return straight;
            int count=0;foreach(int node in choices)if(node!=previous)count++;
            int pick=random.Next(Mathf.Max(1,count));foreach(int node in choices)if(node!=previous&&pick--==0)return node;
            return previous;
        }
        public Link Edge(int a,int b)
        {foreach(var link in links)if((link.a==a&&link.b==b)||(link.a==b&&link.b==a))return link;throw new InvalidOperationException("Missing traffic connection");}
        public Vector3 LaneStart(int a,int b)
        {var d=(nodes[b]-nodes[a]).normalized;return nodes[a]+d*StopOffset+Vector3.Cross(Vector3.up,d)*Edge(a,b).offset;}
        public Vector3 LaneEnd(int a,int b)
        {var d=(nodes[b]-nodes[a]).normalized;return nodes[b]-d*StopOffset+Vector3.Cross(Vector3.up,d)*Edge(a,b).offset;}
        public float RoadLength(int a,int b)=>Vector3.Distance(LaneStart(a,b),LaneEnd(a,b));
        public Vector3[] TurnPath(int from,int junction,int next)
        {
            var a=LaneEnd(from,junction);var d=LaneStart(junction,next);
            var incoming=(nodes[junction]-nodes[from]).normalized;var outgoing=(nodes[next]-nodes[junction]).normalized;
            float turn=Vector3.Cross(incoming,outgoing).y;
            float rightTangent=Mathf.Clamp(StopOffset-Mathf.Min(Edge(from,junction).offset,Edge(junction,next).offset)*2.1f,StopOffset*.43f,StopOffset*.62f);
            float tangent=Vector3.Dot(incoming,outgoing)>.9f?StopOffset*.66f:turn>.1f?rightTangent:StopOffset*.68f;
            var b=a+incoming*tangent;var c=d-outgoing*tangent;var path=new Vector3[49];
            for(int i=0;i<path.Length;i++){float t=i/(float)(path.Length-1),s=1-t;path[i]=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;}
            return path;
        }
        void PlanCurve(TrafficVehicle car)
        {
            car.curve=TurnPath(car.from,car.to,car.next);car.arc=new float[car.curve.Length];
            for(int i=1;i<car.curve.Length;i++)car.arc[i]=car.arc[i-1]+Vector3.Distance(car.curve[i-1],car.curve[i]);
            car.curveLength=car.arc[car.arc.Length-1];
        }
        void CurvePoint(TrafficVehicle car,float distance,out Vector3 p,out Vector3 forward)
        {
            int index=1;while(index<car.arc.Length-1&&car.arc[index]<distance)index++;
            float t=Mathf.InverseLerp(car.arc[index-1],car.arc[index],distance);
            p=Vector3.Lerp(car.curve[index-1],car.curve[index],t);
            var before=(car.curve[index]-car.curve[Mathf.Max(0,index-2)]).normalized;
            var after=(car.curve[Mathf.Min(index+1,car.curve.Length-1)]-car.curve[index-1]).normalized;
            forward=Vector3.Slerp(before,after,t).normalized;
        }
        public void Evaluate(TrafficVehicle car,float distance,out Vector3 p,out Vector3 forward)
        {
            float road=RoadLength(car.from,car.to);
            if(car.turning)
            {
                if(distance<=car.curveLength){CurvePoint(car,distance,out p,out forward);return;}
                forward=(nodes[car.next]-nodes[car.to]).normalized;p=LaneStart(car.to,car.next)+forward*(distance-car.curveLength);return;
            }
            if(distance<=road){forward=(nodes[car.to]-nodes[car.from]).normalized;p=LaneStart(car.from,car.to)+forward*distance;return;}
            CurvePoint(car,Mathf.Min(car.curveLength,distance-road),out p,out forward);
        }
        bool ExitClear(TrafficVehicle car,int self)
        {
            foreach(var other in vehicles)
            {
                if(other==car)continue;
                if(!other.turning&&other.from==car.to&&other.to==car.next&&other.progress<car.length+other.length*.5f+MinimumGap+3)return false;
            }
            var direction=(nodes[car.next]-nodes[car.to]).normalized;
            var at=LaneStart(car.to,car.next)+direction*(car.length*.5f+MinimumGap);
            return !Occupied(car,at,direction,car.length*.5f+MinimumGap);
        }
        bool Occupied(TrafficVehicle self,Vector3 at,Vector3 direction,float halfLength)
        {
            if(PedestrianWorld.Active&&PedestrianWorld.Active.BlocksTraffic(at,halfLength))return true;
            // Boarding temporarily disables the native player controller.
            // Keep its space reserved for traffic throughout the animation.
            if(player&&player.BlocksTrafficBox(at+Vector3.up*.91f,new Vector3(self.width*.5f+.06f,.48f,halfLength),Quaternion.LookRotation(direction)))return true;
            int count=Physics.OverlapBoxNonAlloc(at+Vector3.up*.91f,new Vector3(self.width*.5f+.06f,.48f,halfLength),contacts,Quaternion.LookRotation(direction),1|(1<<2)|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
            if(count==contacts.Length)return true;
            for(int i=0;i<count;i++)if(!contacts[i].transform.IsChildOf(self.transform))return true;
            return false;
        }
        bool MainPriority(TrafficVehicle car)
        {var incoming=nodes[car.to]-nodes[car.from];return (Mathf.Abs(nodes[car.to].x-94)<.1f&&Mathf.Abs(incoming.z)>1)||(Mathf.Abs(nodes[car.to].z)<.1f&&Mathf.Abs(incoming.x)>1);}
        void Reservations()
        {
            for(int junction=0;junction<nodes.Length;junction++)
            {
                if(owners[junction]>=0)continue;
                int winner=-1;float oldest=float.MaxValue;bool main=false;
                for(int i=0;i<vehicles.Length;i++)
                {
                    var car=vehicles[i];if(car.turning||car.to!=junction||car.waitingSince<0||car.heldJunction>=0)continue;
                    if(!ExitClear(car,i)){BlockedReservations++;continue;}
                    bool priority=MainPriority(car);
                    // Aged arrivals also get a turn; the low-priority approach
                    // cannot be starved by an endless main-road stream.
                    if(clock-car.waitingSince>14)priority=true;
                    bool yieldRight=false;var direction=(nodes[car.to]-nodes[car.from]).normalized;
                    if(!priority)
                        foreach(var other in vehicles)
                        {
                            if(other==car||other.turning||other.to!=junction||other.waitingSince<0)continue;
                            var otherApproach=(nodes[other.from]-nodes[junction]).normalized;
                            if(Vector3.Dot(Vector3.Cross(Vector3.up,direction),otherApproach)>.8f&&clock-car.waitingSince<8)yieldRight=true;
                        }
                    float score=car.waitingSince+(yieldRight?8:0);
                    if(winner<0||(priority&&!main)||(priority==main&&score<oldest)){winner=i;oldest=score;main=priority;}
                }
                if(winner>=0)
                {
                    var car=vehicles[winner];
                    // A player or car already crossing owns the physical space,
                    // irrespective of the NPC's logical right of way.
                    bool clear=true;
                    for(int s=0;s<car.curve.Length;s+=4)
                        if(Occupied(car,car.curve[s],(car.curve[Mathf.Min(s+1,car.curve.Length-1)]-car.curve[Mathf.Max(0,s-1)]).normalized,car.length*.5f+.35f)){clear=false;break;}
                    if(clear){owners[junction]=winner;car.heldJunction=junction;}else YieldEvents++;
                }
            }
        }
        float FreeDistance(TrafficVehicle car,float maximum)
        {
            for(float d=.45f;d<=maximum;d+=.65f)
            {
                Evaluate(car,car.progress+d,out var at,out var forward);
                if(Occupied(car,at,forward,car.length*.5f+.30f))return Mathf.Max(0,d-.85f);
            }
            return maximum;
        }
        public void Step(float dt,bool immediate=false)
        {
            if(owners==null)Initialize();clock+=dt;
            for(int i=0;i<vehicles.Length;i++)
            {
                var car=vehicles[i];
                if(!car.turning&&car.heldJunction>=0&&car.heldJunction!=car.to&&car.progress>car.length*.5f+1)
                {owners[car.heldJunction]=-1;car.heldJunction=-1;}
                float remaining=RoadLength(car.from,car.to)-car.progress;
                if(!car.turning&&remaining<20&&car.waitingSince<0)car.waitingSince=clock;
            }
            Reservations();
            // Compute every speed before committing positions. There is no
            // execution-order advantage for the first vehicle in the array.
            for(int i=0;i<vehicles.Length;i++)
            {
                var car=vehicles[i];float target=Edge(car.from,car.to).speed*(car.kind==3?.82f:1);
                float available=35;
                float remaining=RoadLength(car.from,car.to)-car.progress;
                bool owns=owners[car.to]==i;
                if(car.turning||remaining<14)target=Mathf.Min(target,car.kind==3?3.1f:4.0f);
                if(!car.turning&&!owns)available=Mathf.Min(available,Mathf.Max(0,remaining));
                float scan=Mathf.Min(22,Mathf.Max(3,car.speed*.9f+car.speed*car.speed/7+2));
                available=Mathf.Min(available,FreeDistance(car,scan));
                if(available<scan-.1f||(!car.turning&&!owns&&remaining<scan))target=Mathf.Min(target,Mathf.Sqrt(2*3.4f*Mathf.Max(0,available-.55f)));
                if(available<.65f)target=0;
                car.desiredSpeed=target;
                float speed=Mathf.MoveTowards(car.speed,target,(target<car.speed?4.8f:1.65f)*dt);
                // Emergency spatial clamp supplements the comfortable braking
                // model if a player suddenly enters the immediate stopping gap.
                decisions[i]=Mathf.Min(speed,Mathf.Max(0,available-.12f)/dt);
            }
            for(int i=0;i<vehicles.Length;i++)
            {
                var car=vehicles[i];float before=car.progress;car.speed=decisions[i];float travel=car.speed*dt;car.progress+=travel;car.travelled+=travel;
                if(car.speed<.25f){car.waitingTime+=dt;car.maxWait=Mathf.Max(car.maxWait,car.waitingTime);}else car.waitingTime=0;
                if(!car.turning&&car.progress>=RoadLength(car.from,car.to)-.001f)
                {
                    float road=RoadLength(car.from,car.to);
                    if(owners[car.to]==i){car.progress=Mathf.Max(0,car.progress-road);car.turning=true;}
                    else {car.progress=road;car.speed=0;}
                }
                if(car.turning&&car.progress>=car.curveLength)
                {
                    var incoming=(nodes[car.to]-nodes[car.from]).normalized;var outgoing=(nodes[car.next]-nodes[car.to]).normalized;
                    float turn=Vector3.Cross(incoming,outgoing).y;
                    if(turn>.1f)car.rightTurns++;else if(turn<-.1f)car.leftTurns++;else car.straightMoves++;
                    car.progress-=car.curveLength;car.from=car.to;car.to=car.next;car.next=Choose(car.from,car.to);car.turning=false;car.waitingSince=-1;car.completedTurns++;PlanCurve(car);
                }
                Evaluate(car,car.progress,out var position,out var forward);
                Evaluate(car,car.progress+.9f,out _,out var ahead);
                float curvature=Vector3.SignedAngle(forward,ahead,Vector3.up)*Mathf.Deg2Rad/.9f;
                float steer=Mathf.Atan(car.length*.62f*curvature)*Mathf.Rad2Deg,intent=0;
                if(car.turning||RoadLength(car.from,car.to)-car.progress<16)
                    intent=Vector3.SignedAngle((nodes[car.to]-nodes[car.from]).normalized,(nodes[car.next]-nodes[car.to]).normalized,Vector3.up);
                car.Pose(position,forward,travel,steer,dt,immediate,intent);
            }
            if(immediate)Physics.SyncTransforms();
        }
    }
}
