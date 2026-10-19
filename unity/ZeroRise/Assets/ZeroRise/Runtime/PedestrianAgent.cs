using UnityEngine;
using UnityEngine.AI;

namespace ZeroRise
{
    [DefaultExecutionOrder(30)] public sealed class PedestrianAgent:MonoBehaviour
    {
        public enum Behaviour { Walking, Arriving, Activity, Leaving, WaitingToCross, Crossing, WaitingForRoute, Yielding }
        public NavMeshAgent agent;public PlayerAvatar avatar;public CapsuleCollider capsule;public PedestrianWorld world;public int personality;
        public int destination=-1,previousPlace=-1,initialPlace=-1,activities,roadCrossings,recoveries;public float walkedSinceRest,totalDistance;
        public Behaviour state;public GameObject phone;public NavMeshObstacle manualObstacle;public float StateTime=>Time.time-stateStarted;
        float stateStarted,chooseAt,stillTime,probeAt,poseBlend,transitionTime,walkingRadius;Vector3 lastPosition,activityOrigin,linkStart,linkEnd;int crossing=-1;
        Vector3 yieldTarget;float yieldUntil;
        NavMeshPath route;readonly RaycastHit[] hits=new RaycastHit[12];
        ThirdPersonMotor player;CharacterController playerCapsule;
        public bool CanReach(Vector3 position)=>agent&&agent.isOnNavMesh&&agent.CalculatePath(position,route)&&route.status==NavMeshPathStatus.PathComplete;
        void Start()
        {
            route=new NavMeshPath();walkingRadius=agent.radius;agent.SetAreaCost(4,2.8f);lastPosition=transform.position;chooseAt=Time.time+personality*.16f;
            player=FindAnyObjectByType<ThirdPersonMotor>();if(player)playerCapsule=player.GetComponent<CharacterController>();
            if(initialPlace>=0&&!world.places[initialPlace].owner){destination=initialPlace;world.places[destination].owner=this;AnchorHere();SetState(Behaviour.Arriving);}
            else {AnchorHere();SetState(Behaviour.WaitingForRoute);}
        }
        void SetState(Behaviour next){state=next;stateStarted=Time.time;stillTime=transitionTime=0;}
        void AnchorHere()
        {
            activityOrigin=transform.position;agent.isStopped=true;agent.velocity=Vector3.zero;agent.updatePosition=agent.updateRotation=false;
            if(manualObstacle)manualObstacle.enabled=true;
        }
        void ResumeNavigation()
        {
            if(manualObstacle)manualObstacle.enabled=false;agent.radius=walkingRadius;
            // Synchronize at the position actually reached. Enabling native
            // movement must never pull an actor back through a blocked exit.
            agent.nextPosition=transform.position;agent.updatePosition=agent.updateRotation=true;agent.isStopped=false;
        }
        void OnDisable(){if(world)world.Release(this);}
        void Plan()
        {
            if(destination>=0){previousPlace=destination;world.Release(this);destination=-1;}
            destination=world.Choose(this);
            if(destination<0){agent.ResetPath();AnchorHere();chooseAt=Time.time+2;SetState(Behaviour.WaitingForRoute);return;}
            ResumeNavigation();agent.SetDestination(world.places[destination].approach);avatar.enabled=true;SetState(Behaviour.Walking);
        }
        public bool RequestVehicleSpace(SimpleCar vehicle)
        {
            if(!agent||!agent.isOnNavMesh||state==Behaviour.Crossing||state==Behaviour.WaitingToCross)return false;
            if(state==Behaviour.Yielding)return true;
            if(destination>=0&&world.places[destination].activity==PedestrianWorld.Activity.Sit)return false;
            var away=transform.position-vehicle.transform.position;away.y=0;
            if(away.sqrMagnitude>12.25f||away.sqrMagnitude<.01f)return false;away.Normalize();
            // Choose reachable pavement on this same side of the car. Yield
            // through normal navigation and avoidance, never by moving the
            // resident's transform out of the player's way.
            for(int i=0;i<12;i++)
            {
                float angle=(i%2==0?1:-1)*(i/2)*18;
                var candidate=transform.position+Quaternion.AngleAxis(angle,Vector3.up)*away*(1.8f+i*.08f);
                if(!NavMesh.SamplePosition(candidate,out var ground,.65f,agent.areaMask)||Vector3.Dot(ground.position-vehicle.transform.position,away)<2.8f)continue;
                if(!agent.CalculatePath(ground.position,route)||route.status!=NavMeshPathStatus.PathComplete)continue;
                if(Physics.CheckCapsule(ground.position+Vector3.up*.35f,ground.position+Vector3.up*1.5f,.29f,1|(1<<2)|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))continue;
                if(destination>=0){previousPlace=destination;world.Release(this);destination=-1;}
                yieldTarget=ground.position;yieldUntil=Time.time+6;ResumeNavigation();agent.SetDestination(yieldTarget);avatar.enabled=true;avatar.ResetMotion();SetState(Behaviour.Yielding);return true;
            }
            return false;
        }
        bool RouteObstructed()
        {
            var heading=agent.desiredVelocity.normalized;if(heading.sqrMagnitude<.01f)return false;
            if(player&&player.Interacting&&player.BlocksTrafficBox(transform.position+heading*.225f+Vector3.up*.9f,new Vector3(.32f,.85f,.45f),Quaternion.LookRotation(heading)))return true;
            int count=Physics.CapsuleCastNonAlloc(transform.position+Vector3.up*.45f,transform.position+Vector3.up*1.4f,.24f,heading,hits,.45f,1|(1<<2)|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform)&&!hits[i].collider.GetComponentInParent<PedestrianAgent>())return true;
            return false;
        }
        void Update()
        {
            if(!agent.isOnNavMesh)return;
            float dt=Time.deltaTime;var delta=transform.position-lastPosition;delta.y=0;float moved=delta.magnitude;totalDistance+=moved;walkedSinceRest+=moved;lastPosition=transform.position;
            // isStopped only stops route following: native avoidance still
            // moves stopped agents. Activities own their world transform, and
            // advertise a stationary obstacle instead of being shoved away.
            if(state==Behaviour.Arriving||state==Behaviour.Activity||state==Behaviour.Leaving||state==Behaviour.WaitingForRoute)agent.nextPosition=activityOrigin;
            if(state==Behaviour.Yielding)
            {
                avatar.Motion(dt>0?moved/dt:0,0,true,dt);
                if(Time.time>=probeAt){probeAt=Time.time+.12f;agent.isStopped=RouteObstructed();}
                if(Time.time>=yieldUntil||(!agent.pathPending&&Vector3.Distance(transform.position,yieldTarget)<.25f)){Plan();}
                return;
            }
            if(state==Behaviour.WaitingForRoute){avatar.Motion(0,0,true,dt);if(Time.time>=chooseAt)Plan();return;}
            if(state==Behaviour.Walking)
            {
                avatar.Motion(dt>0?moved/dt:0,0,true,dt);
                if(agent.isOnOffMeshLink)
                {
                    var link=agent.currentOffMeshLinkData;crossing=world.FindCrossing(transform.position);if(crossing<0)throw new System.InvalidOperationException("Unknown resident crossing");
                    var crossingData=world.crossings[crossing];var a=crossingData.a;var b=crossingData.b;if(Vector3.Distance(transform.position,b)<Vector3.Distance(transform.position,a)){var swap=a;a=b;b=swap;}
                    var side=Vector3.Cross(Vector3.up,(b-a).normalized)*.65f;linkStart=a+side;linkEnd=b+side;
                    if(NavMesh.SamplePosition(linkStart,out var start,.6f,NavMesh.AllAreas))linkStart=start.position;
                    if(NavMesh.SamplePosition(linkEnd,out var end,.6f,NavMesh.AllAreas))linkEnd=end.position;
                    agent.isStopped=true;agent.updatePosition=agent.updateRotation=false;if(manualObstacle)manualObstacle.enabled=true;SetState(Behaviour.WaitingToCross);return;
                }
                if(!agent.pathPending&&agent.pathStatus!=NavMeshPathStatus.PathComplete){world.replans++;Plan();return;}
                if(!agent.pathPending&&agent.remainingDistance<.20f&&agent.velocity.magnitude<.16f){if(!world.SeatAvailable(world.places[destination],this)){Plan();return;}AnchorHere();avatar.ResetMotion();SetState(Behaviour.Arriving);return;}
                if(Time.time>=probeAt)
                {
                    probeAt=Time.time+.12f;agent.isStopped=RouteObstructed();
                }
                if(moved<.08f*dt)stillTime+=dt;else stillTime=0;
                if(stillTime>6){recoveries++;world.replans++;agent.ResetPath();Plan();}
                return;
            }
            if(state==Behaviour.WaitingToCross)
            {
                MoveSafely(linkStart,dt);avatar.Motion(dt>0?moved/dt:0,0,true,dt);Face(linkEnd-transform.position,dt);
                if(Vector3.Distance(transform.position,linkStart)<.04f&&world.ReserveCrossing(crossing,this))SetState(Behaviour.Crossing);return;
            }
            if(state==Behaviour.Crossing)
            {
                MoveSafely(linkEnd,dt);Face(linkEnd-linkStart,dt);avatar.Motion(dt>0?moved/dt:0,0,true,dt);
                if(Vector3.Distance(transform.position,linkEnd)<.025f){agent.CompleteOffMeshLink();agent.Warp(linkEnd);if(manualObstacle)manualObstacle.enabled=false;agent.updatePosition=true;agent.updateRotation=true;agent.isStopped=false;agent.SetDestination(world.places[destination].approach);world.crossings[crossing].owner=null;roadCrossings++;world.crossedRoads++;crossing=-1;SetState(Behaviour.Walking);}return;
            }
            var place=world.places[destination];avatar.Motion(0,0,true,dt);
            if(state==Behaviour.Arriving)
            {
                Face(place.facing*Vector3.forward,dt);
                if(place.activity==PedestrianWorld.Activity.Sit)
                {
                    avatar.enabled=false;
                    agent.radius=1.15f;
                    AdvanceSeatTransition(place,true,dt);
                }
                else {transitionTime+=dt;poseBlend=Mathf.SmoothStep(0,1,transitionTime/1.4f);}
                if(transitionTime>=1.4f){SetState(Behaviour.Activity);}return;
            }
            if(state==Behaviour.Activity)
            {
                avatar.enabled=false;poseBlend=1;
                bool partnerPresent=place.partner>=0&&world.places[place.partner].owner&&world.places[place.partner].owner.state==Behaviour.Activity;
                if(partnerPresent)Face(world.places[place.partner].owner.transform.position-transform.position,dt);
                if(StateTime>world.Duration(place.activity,personality))
                {activities++;world.completedActivities++;if(place.activity==PedestrianWorld.Activity.Sit)walkedSinceRest=0;SetState(Behaviour.Leaving);}return;
            }
            if(state==Behaviour.Leaving)
            {
                if(place.activity==PedestrianWorld.Activity.Sit)AdvanceSeatTransition(place,false,dt);
                else {transitionTime+=dt;poseBlend=1-Mathf.SmoothStep(0,1,transitionTime/1.4f);}
                if(transitionTime>=1.4f)
                {
                    capsule.height=1.8f;capsule.center=Vector3.up*.9f;avatar.enabled=true;avatar.ResetMotion();Plan();
                }
            }
        }
        void AdvanceSeatTransition(PedestrianWorld.Place place,bool arriving,float dt)
        {
            var seated=new Vector3(place.seat.x,activityOrigin.y,place.seat.z);
            float nextTime=Mathf.Min(1.4f,transitionTime+dt),blend=Mathf.SmoothStep(0,1,nextTime/1.4f);if(!arriving)blend=1-blend;
            var requested=Vector3.Lerp(activityOrigin,seated,blend);float desiredHeight=Mathf.Lerp(1.8f,.95f,blend),desiredCenter=Mathf.Lerp(.9f,1.02f,blend);
            MoveSafely(requested,dt,desiredHeight,desiredCenter);
            // A blocked transition waits at physical contact; both the pose
            // and the activity timer follow achieved movement, not wall time.
            if((transform.position-requested).sqrMagnitude<.0001f&&PlayerClearance(transform.position,desiredHeight,Vector3.up*desiredCenter)>=0)transitionTime=nextTime;
            var span=seated-activityOrigin;poseBlend=span.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(transform.position-activityOrigin,span)/span.sqrMagnitude):blend;
            capsule.height=Mathf.Lerp(1.8f,.95f,poseBlend);capsule.center=Vector3.up*Mathf.Lerp(.9f,1.02f,poseBlend);
        }
        void MoveSafely(Vector3 target,float dt,float desiredHeight=-1,float desiredCenterY=0)
        {
            var delta=target-transform.position;float distance=Mathf.Min(delta.magnitude,agent.speed*dt);if(distance<.0001f)return;
            float height=desiredHeight<0?capsule.height:desiredHeight;var localCenter=desiredHeight<0?capsule.center:Vector3.up*desiredCenterY;
            var center=transform.position+transform.rotation*localCenter;float half=Mathf.Max(0,height*.5f-capsule.radius);
            Physics.SyncTransforms();
            int count=Physics.CapsuleCastNonAlloc(center+Vector3.up*half,center-Vector3.up*half,capsule.radius,delta.normalized,hits,distance+.06f,(1<<2)|(1<<9),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform))distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.045f));
            // A controller can be depenetrated out of the way before a later
            // physics overlap check observes any penetration. Test predicted
            // capsule geometry directly, including its native skin and the
            // growing stand-up volume, before moving the resident at all.
            float safeClearance=Mathf.Min(0,PlayerClearance(transform.position,height,localCenter));
            if(PlayerClearance(transform.position+delta.normalized*distance,height,localCenter)<safeClearance)
            {
                float low=0,high=distance;for(int i=0;i<10;i++){float middle=(low+high)*.5f;if(PlayerClearance(transform.position+delta.normalized*middle,height,localCenter)>=safeClearance)low=middle;else high=middle;}distance=low;
            }
            transform.position+=delta.normalized*distance;
        }
        float PlayerClearance(Vector3 point,float height,Vector3 localCenter)
        {
            if(!playerCapsule||!player.gameObject.activeInHierarchy||(player.driving&&!player.Interacting)||(!playerCapsule.enabled&&!player.Interacting))return float.PositiveInfinity;
            // Both character capsules stay upright. Distance between their
            // vertical axis segments also covers jumping/uneven ground; it
            // must not turn into an infinite vertical wall above a resident.
            var a=point+transform.rotation*localCenter;var b=playerCapsule.transform.TransformPoint(playerCapsule.center);
            float selfRadius=capsule.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.z));
            float otherRadius=playerCapsule.radius*Mathf.Max(Mathf.Abs(playerCapsule.transform.lossyScale.x),Mathf.Abs(playerCapsule.transform.lossyScale.z));
            float selfHalf=Mathf.Max(0,height*Mathf.Abs(transform.lossyScale.y)*.5f-selfRadius),otherHalf=Mathf.Max(0,playerCapsule.height*Mathf.Abs(playerCapsule.transform.lossyScale.y)*.5f-otherRadius);
            float vertical=Mathf.Max(0,Mathf.Abs(a.y-b.y)-selfHalf-otherHalf),dx=a.x-b.x,dz=a.z-b.z;
            return Mathf.Sqrt(dx*dx+dz*dz+vertical*vertical)-selfRadius-otherRadius-playerCapsule.skinWidth-.025f;
        }
        void Face(Vector3 direction,float dt)
        {direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),dt*150);}
        void LateUpdate()
        {
            if(phone)phone.SetActive(destination>=0&&world.places[destination].activity==PedestrianWorld.Activity.Phone&&(state==Behaviour.Activity||state==Behaviour.Arriving||state==Behaviour.Leaving));
            if(destination<0||state==Behaviour.Yielding||state==Behaviour.Walking||state==Behaviour.Crossing||state==Behaviour.WaitingToCross||state==Behaviour.WaitingForRoute)return;
            var place=world.places[destination];float w=poseBlend;var a=avatar;
            // Animator supplies a fresh idle pose each frame; these offsets do not accumulate.
            if(place.activity==PedestrianWorld.Activity.Sit)
            {
                a.transform.localPosition=Vector3.zero;float target=place.seat.y-transform.position.y+.075f;
                a.hips.localPosition+=Vector3.up*(target-a.hips.position.y+transform.position.y)*w;
                a.leftThigh.localRotation=Quaternion.Slerp(a.leftThigh.localRotation,Quaternion.Euler(-86,0,-3),w);a.rightThigh.localRotation=Quaternion.Slerp(a.rightThigh.localRotation,Quaternion.Euler(-86,0,3),w);
                a.leftCalf.localRotation=Quaternion.Slerp(a.leftCalf.localRotation,Quaternion.Euler(88,0,0),w);a.rightCalf.localRotation=Quaternion.Slerp(a.rightCalf.localRotation,Quaternion.Euler(88,0,0),w);
                a.leftArm.localRotation*=Quaternion.Euler(-22*w,0,-4*w);a.rightArm.localRotation*=Quaternion.Euler(-22*w,0,4*w);
                a.leftForearm.localRotation*=Quaternion.Euler(-65*w,0,0);a.rightForearm.localRotation*=Quaternion.Euler(-65*w,0,0);
            }
            else if(place.activity==PedestrianWorld.Activity.Phone)
            {a.rightArm.localRotation*=Quaternion.Euler(-24*w,0,12*w);a.rightForearm.localRotation*=Quaternion.Euler(-112*w,0,0);a.head.localRotation*=Quaternion.Euler(14*w,0,0);}
            else if(place.activity==PedestrianWorld.Activity.Stretch)
            {float stretch=Mathf.Sin(StateTime*.65f)*.5f+.5f;a.leftArm.localRotation*=Quaternion.Euler(0,0,-100*stretch*w);a.rightArm.localRotation*=Quaternion.Euler(0,0,100*stretch*w);}
            else if(place.activity==PedestrianWorld.Activity.Talk&&place.partner>=0&&world.places[place.partner].owner&&world.places[place.partner].owner.state==Behaviour.Activity&&Vector3.Distance(transform.position,world.places[place.partner].owner.transform.position)<3)
            {a.rightArm.localRotation*=Quaternion.Euler(-18*w,0,12*w);a.rightForearm.localRotation*=Quaternion.Euler((-45+Mathf.Sin(Time.time*2+personality)*12)*w,0,0);a.head.localRotation*=Quaternion.Euler(Mathf.Sin(Time.time*1.5f)*3*w,0,0);}
            else a.head.localRotation*=Quaternion.Euler(0,Mathf.Sin(Time.time*.45f+personality)*18*w,0);
        }
    }
}
