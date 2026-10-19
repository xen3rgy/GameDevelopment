using System.Collections;
using UnityEngine;

namespace ZeroRise
{
    [RequireComponent(typeof(Rigidbody)),DefaultExecutionOrder(-120)]
    public sealed class SimpleCar : MonoBehaviour
    {
        public enum Occupancy { Parked,Entering,Driving,Exiting }
        public WheelCollider[] wheels;
        public Transform[] wheelVisuals;
        public Transform[] doors;
        public Transform seatAnchor,steeringWheel;
        public Occupancy State { get; private set; }
        public float DoorAngle { get; private set; }
        public float ActionProgress { get; private set; }
        public float InteractionSide { get; private set; }=-1;
        public float SeatTransferProgress { get; private set; }=-1;
        bool seatTransferCompleted;
        public const float EnterDuration=2.05f,ExitDuration=1.75f,SeatTransferDuration=1.55f;
        Rigidbody body;
        ThirdPersonMotor driver;
        Vector3 returnPosition;
        Quaternion returnRotation;
        Transform returnParent;
        Coroutine interaction;
        float steerValue;
        float steeringAngle;
        bool cancelRequested,closeDoorsWhenClear;
        float cancelAfter;
        readonly RaycastHit[] interactionHits=new RaycastHit[32];
        readonly Collider[] interactionContacts=new Collider[32];
        static readonly Quaternion SteeringTilt=Quaternion.Euler(65,0,0);
        const int VehicleObstacles=1|(1<<8)|(1<<9);
        void Awake() { body = GetComponent<Rigidbody>(); body.centerOfMass = new Vector3(0, .35f, 0); }
        bool Outside(float side,out Vector3 point,bool actors=true)
        {
            return FootPosition(transform.TransformPoint(new Vector3(side*1.68f,0,-.44f)),out point,actors);
        }
        bool DoorClear(float side,bool actors=true)
        {
            // Test the actual hinged panel through its entire opening arc.
            // A broad box around the mirror used to reject people beside the
            // bonnet even when neither the doorway nor its sweep was occupied.
            return DoorArcClear(side,0,70,actors);
        }
        bool WalkSegment(Vector3 from,Vector3 to,bool actors=true)
        {
            var previous=from;int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.18f));
            for(int i=1;i<=samples;i++)
            {
                var desired=Vector3.Lerp(from,to,i/(float)samples);
                if(!FootPosition(desired,out var next,actors)||Mathf.Abs(next.y-previous.y)>.25f||!CapsuleRouteClear(previous,next,false,actors))return false;
                previous=next;
            }
            return true;
        }
        bool EntryRoute(Vector3 from,Vector3 outside,out Vector3[] path,bool actors=true)
        {
            path=null;Physics.SyncTransforms();
            if(WalkSegment(from,outside,actors)){path=new[]{outside};return true;}
            // A tiny visibility graph follows clear ground around this car.
            // Every edge checks its complete capsule corridor, including the
            // car itself. No straight-line teleport through the passenger seat.
            var bounds=GetComponent<BoxCollider>();
            float x=Mathf.Max(1.68f,bounds?bounds.size.x*.5f+.70f:1.68f),z=Mathf.Max(2.55f,bounds?bounds.size.z*.5f+.65f:2.55f);
            var points=new Vector3[8];points[0]=from;points[1]=outside;
            var locals=new[]{new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,-z),new Vector3(x,0,z),new Vector3(x,0,-.44f),new Vector3(-x,0,-.44f)};
            var valid=new bool[8];valid[0]=valid[1]=true;
            for(int i=2;i<8;i++)valid[i]=FootPosition(transform.TransformPoint(locals[i-2]),out points[i],actors);
            var costs=new float[8];var previous=new int[8];var visited=new bool[8];
            for(int i=0;i<8;i++){costs[i]=float.PositiveInfinity;previous[i]=-1;}costs[0]=0;
            for(int step=0;step<8;step++)
            {
                int at=-1;for(int i=0;i<8;i++)if(valid[i]&&!visited[i]&&(at<0||costs[i]<costs[at]))at=i;
                if(at<0||float.IsInfinity(costs[at]))break;if(at==1)break;visited[at]=true;
                for(int next=1;next<8;next++)
                {
                    float cost=costs[at]+Vector3.Distance(points[at],points[next]);
                    if(!valid[next]||visited[next]||cost>=costs[next]||cost>13||!WalkSegment(points[at],points[next],actors))continue;
                    costs[next]=cost;previous[next]=at;
                }
            }
            if(previous[1]<0)return false;var route=new System.Collections.Generic.List<Vector3>();
            for(int at=1;at!=0;at=previous[at])route.Add(points[at]);route.Reverse();path=route.ToArray();return true;
        }
        void AskResidentsForSpace()
        {
            int count=Physics.OverlapSphereNonAlloc(transform.position,3.5f,interactionContacts,1<<9,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var person=interactionContacts[i].GetComponentInParent<PedestrianAgent>();
                if(!person)continue;var local=transform.InverseTransformPoint(person.transform.position);
                bool doorway=local.x*InteractionSide>.8f&&local.z> -1.7f&&local.z<1.2f;
                bool approach=driver&&Vector3.Distance(person.transform.position,driver.transform.position)<1.2f;
                if(doorway||approach)person.RequestVehicleSpace(this);}
        }
        bool TryEntrySide(Vector3 from,float side,bool actors,out Vector3 outside,out Vector3[] route)
        {
            route=null;return Outside(side,out outside,actors)&&DoorClear(side,actors)&&EntryRoute(from,outside,out route,actors);
        }
        Vector3 SeatPosition(float side)
        {
            var point=seatAnchor.localPosition;if(side>0)point.x=-point.x;return point;
        }
        public void Enter(ThirdPersonMotor player)
        {
            if(!body)body=GetComponent<Rigidbody>();
            if(!player||State!=Occupancy.Parked||player.driving||player.Interacting)return;
            if(body.linearVelocity.magnitude>.8f){player.Hint("Das Auto muss zuerst stehen.");return;}
            if(Vector3.Distance(player.transform.position,transform.position)>4||!player.CanInteract){player.Hint("Gehe zur Fahrertür.");return;}
            if(!seatAnchor||doors==null||doors.Length!=2)return;
            float side=-1;Vector3 outside;Vector3[] route;
            // Prefer the driver's door, but use the passenger door when its
            // complete approach and opening arc are free. Only wait for people
            // to yield when neither side currently offers a clear entry.
            if(!TryEntrySide(player.transform.position,side,true,out outside,out route))
            {
                side=1;
                if(!TryEntrySide(player.transform.position,side,true,out outside,out route))
                {
                    side=-1;
                    if(!TryEntrySide(player.transform.position,side,false,out outside,out route))
                    {
                        side=1;
                        if(!TryEntrySide(player.transform.position,side,false,out outside,out route))
                        {player.Hint("Beide Einstiege sind blockiert. Suche eine freie Autot\u00fcr.");return;}
                    }
                }
            }
            InteractionSide=side;SeatTransferProgress=-1;
            driver=player;returnPosition=player.transform.position;returnRotation=player.visual.rotation;returnParent=player.transform.parent;AskResidentsForSpace();
            cancelRequested=false;cancelAfter=Time.time+.25f;State=Occupancy.Entering;player.BeginInteraction();player.orbit.IgnoreVehicle(transform);player.orbit.Follow(transform,2.2f,7);
            interaction=StartCoroutine(EnterSequence(side,outside,route));
        }
        IEnumerator EnterSequence(float side,Vector3 outside,Vector3[] route)
        {
            float elapsed=0,blockedFor=0;
            foreach(var waypoint in route)
            {
                var from=driver.transform.position;float approach=Mathf.Max(.12f,Vector3.Distance(from,waypoint)/2.1f);elapsed=0;
                while(elapsed<approach)
                {
                    if(cancelRequested||blockedFor>=8){yield return RecoverInteraction(false,0,side);yield break;}
                    float dt=Time.deltaTime,next=Mathf.Min(approach,elapsed+dt);
                    var before=driver.transform.position;var desired=Vector3.Lerp(from,waypoint,Mathf.SmoothStep(0,1,next/approach));
                    if(!FootPosition(desired,out var target)||!MoveDriver(target))
                    {blockedFor+=dt;driver.avatar.Motion(0,0,true,dt);AskResidentsForSpace();driver.Hint("Warte auf einen freien Weg. E · Abbrechen");yield return null;continue;}
                    blockedFor=0;elapsed=next;var direction=target-before;direction.y=0;
                    if(direction.sqrMagnitude>.00001f)driver.visual.rotation=Quaternion.RotateTowards(driver.visual.rotation,Quaternion.LookRotation(direction),420*dt);
                    driver.avatar.Motion(Vector3.ProjectOnPlane(target-before,Vector3.up).magnitude/Mathf.Max(dt,.0001f),0,true,dt);yield return null;
                }
            }
            blockedFor=0;
            while(!DoorClear(side)||!Outside(side,out outside))
            {
                if(cancelRequested||blockedFor>=8){yield return RecoverInteraction(false,0,side);yield break;}
                blockedFor+=Time.deltaTime;AskResidentsForSpace();driver.avatar.Motion(0,0,true,Time.deltaTime);driver.Hint("Warte, bis die Tür frei ist. E · Abbrechen");yield return null;
            }
            driver.transform.rotation=transform.rotation;driver.visual.localRotation=Quaternion.identity;
            driver.avatar.BindSteeringWheel(side<0?steeringWheel:null);
            var localOutside=transform.InverseTransformPoint(outside);elapsed=blockedFor=0;
            while(elapsed<EnterDuration)
            {
                if(cancelRequested||blockedFor>=8){yield return RecoverInteraction(false,elapsed/EnterDuration,side);yield break;}
                float next=Mathf.Min(EnterDuration,elapsed+Time.deltaTime),t=next/EnterDuration;
                float seat=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.34f,.76f,t));
                if(!DoorSweepClear(side,DoorEnvelope(t))||!MoveDriver(transform.TransformPoint(Vector3.Lerp(localOutside,SeatPosition(side),seat))))
                {blockedFor+=Time.deltaTime;driver.avatar.VehiclePose("EnterCar",elapsed/EnterDuration,side);driver.Hint("Der Weg zum Sitz ist blockiert. E · Abbrechen");yield return null;continue;}
                blockedFor=0;elapsed=next;ActionProgress=t;SetDoor(side,DoorEnvelope(t));driver.transform.rotation=transform.rotation;
                driver.avatar.VehiclePose("EnterCar",t,side);yield return null;
            }
            SetDoor(side,0);
            if(side>0){yield return ChangeSeat(true);if(!seatTransferCompleted)yield break;}
            State=Occupancy.Driving;driver.SetDriving(true);driver.avatar.BindSteeringWheel(steeringWheel);driver.avatar.VehiclePose("Drive");interaction=null;
        }
        public void TryExit(ThirdPersonMotor player)
        {
            if(player!=driver||State!=Occupancy.Driving)return;
            if(body.linearVelocity.magnitude>.8f){player.Hint("Zum Aussteigen anhalten.");return;}
            float side=-1;
            if(!Outside(side,out var outside)||!DoorClear(side))
            {
                side=1;if(!Outside(side,out outside)||!DoorClear(side)){player.Hint("Beide Türen sind blockiert.");return;}
            }
            InteractionSide=side;SeatTransferProgress=-1;cancelRequested=false;cancelAfter=Time.time+.25f;State=Occupancy.Exiting;player.BeginInteraction();interaction=StartCoroutine(ExitSequence(side,outside));
        }
        IEnumerator ExitSequence(float side,Vector3 outside)
        {
            if(side>0){yield return ChangeSeat(false);if(!seatTransferCompleted)yield break;driver.avatar.BindSteeringWheel(null);}
            var localOutside=transform.InverseTransformPoint(outside);float elapsed=0,blockedFor=0;
            while(elapsed<ExitDuration)
            {
                if(cancelRequested||blockedFor>=8){yield return RecoverInteraction(true,elapsed/ExitDuration,side);yield break;}
                float next=Mathf.Min(ExitDuration,elapsed+Time.deltaTime),t=next/ExitDuration;
                float stand=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.72f,t));
                if(!DoorSweepClear(side,DoorEnvelope(t))||!MoveDriver(transform.TransformPoint(Vector3.Lerp(SeatPosition(side),localOutside,stand))))
                {blockedFor+=Time.deltaTime;driver.avatar.VehiclePose("ExitCar",elapsed/ExitDuration);driver.avatar.VehicleSide=side;driver.Hint("Der Ausgang ist blockiert. E · Im Auto bleiben");yield return null;continue;}
                blockedFor=0;elapsed=next;ActionProgress=t;SetDoor(side,DoorEnvelope(t));driver.transform.rotation=transform.rotation;
                driver.avatar.VehiclePose("ExitCar",t);driver.avatar.VehicleSide=side;yield return null;
            }
            // A person may arrive after the last animated step. Keep the
            // controller disabled until a grounded, unoccupied exit is valid.
            while(!Outside(side,out outside)||!RestoreOnFoot(outside))
            {
                blockedFor+=Time.deltaTime;if(cancelRequested||blockedFor>=8){yield return RecoverInteraction(true,1,side);yield break;}
                driver.Hint("Der Ausgang ist blockiert. Warte auf einen freien Platz.");yield return null;
            }
            SetDoor(side,0);
        }
        IEnumerator ChangeSeat(bool toDriver)
        {
            seatTransferCompleted=false;float elapsed=0,blockedFor=0;bool cancelled=false;
            while(elapsed<SeatTransferDuration)
            {
                if(cancelRequested)
                {
                    // Once inside, cancellation returns to the driver along
                    // the same tucked-leg path. Never back through a closed
                    // passenger door or drop through the centre console.
                    cancelRequested=false;cancelled=true;
                    if(!toDriver){elapsed=SeatTransferDuration-elapsed;toDriver=true;}
                }
                if(blockedFor>=8)
                {SeatTransferProgress=-1;yield return RecoverInteraction(true,0,1);yield break;}
                float next=Mathf.Min(SeatTransferDuration,elapsed+Time.deltaTime),t=next/SeatTransferDuration;
                float blend=Mathf.SmoothStep(0,1,t),lift=Mathf.Sin(t*Mathf.PI);lift*=lift;
                var local=Vector3.Lerp(SeatPosition(toDriver?1:-1),SeatPosition(toDriver?-1:1),blend)+new Vector3(0,lift*.16f,-lift*.10f);
                if(!MoveDriver(transform.TransformPoint(local)))
                {blockedFor+=Time.deltaTime;driver.Hint("Der Innenraum ist blockiert. E \u00b7 Abbrechen");yield return null;continue;}
                blockedFor=0;elapsed=next;SeatTransferProgress=t;driver.transform.rotation=transform.rotation;
                driver.avatar.CabinTransfer(t,toDriver);yield return null;
            }
            SeatTransferProgress=-1;
            if(cancelled)
            {
                State=Occupancy.Driving;driver.SetDriving(true);driver.avatar.BindSteeringWheel(steeringWheel);driver.avatar.VehiclePose("Drive");interaction=null;
                driver.Hint("Du sitzt wieder am Steuer.");yield break;
            }
            seatTransferCompleted=true;
        }
        static float DoorEnvelope(float t)=>70*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.29f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,.98f,t)));
        void SetDoor(float side,float angle)
        {
            DoorAngle=angle;int index=side<0?0:1;
            if(doors!=null&&doors.Length>index&&doors[index])doors[index].localRotation=Quaternion.Euler(0,-side*angle,0);
        }
        bool DoorSweepClear(float side,float angle)
        {
            var hinge=doors[side<0?0:1];float current=Mathf.Abs(Mathf.DeltaAngle(0,hinge.localEulerAngles.y));
            return DoorArcClear(side,current,angle,true);
        }
        bool DoorArcClear(float side,float from,float to,bool actors)
        {
            var hinge=doors[side<0?0:1];int samples=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(to-from)/5));
            for(int i=0;i<=samples;i++)
            {
                var rotation=transform.rotation*Quaternion.Euler(0,-side*Mathf.Lerp(from,to,i/(float)samples),0);
                var center=hinge.position+rotation*new Vector3(0,1.14f,-.61f);
                int count=Physics.OverlapBoxNonAlloc(center,new Vector3(.07f,.68f,.65f),interactionContacts,rotation,actors?VehicleObstacles:1|(1<<8),QueryTriggerInteraction.Ignore);
                if(count==interactionContacts.Length)return false;
                for(int h=0;h<count;h++)if(!interactionContacts[h].transform.IsChildOf(transform))return false;
            }
            return true;
        }
        bool MoveDriver(Vector3 point)
        {if(!CanMoveDriver(point))return false;driver.transform.position=point;return true;}
        bool CanMoveDriver(Vector3 point)=>CapsuleRouteClear(driver.transform.position,point,true,true);
        bool CapsuleRouteClear(Vector3 from,Vector3 point,bool ignoreOwn,bool actors)
        {
            Physics.SyncTransforms();DriverCapsule(from,out var a,out var b,out var radius);
            int mask=actors?VehicleObstacles:1|(1<<8);var delta=point-from;float distance=delta.magnitude;
            if(distance>.00001f)
            {
                int count=Physics.CapsuleCastNonAlloc(a,b,radius,delta/distance,interactionHits,distance,mask,QueryTriggerInteraction.Ignore);
                if(count==interactionHits.Length)return false;
                for(int i=0;i<count;i++)
                {
                    var hit=interactionHits[i];if(ignoreOwn&&hit.transform.IsChildOf(transform))continue;
                    if(hit.normal.y>.75f&&hit.point.y<=Mathf.Max(from.y,point.y)+.08f)continue;
                    return false;
                }
            }
            DriverCapsule(point,out a,out b,out radius);
            int contacts=Physics.OverlapCapsuleNonAlloc(a,b,radius,interactionContacts,mask,QueryTriggerInteraction.Ignore);
            if(contacts==interactionContacts.Length)return false;
            for(int i=0;i<contacts;i++)if(!ignoreOwn||!interactionContacts[i].transform.IsChildOf(transform))return false;
            return true;
        }
        IEnumerator RecoverInteraction(bool exiting,float progress,float side)
        {
            cancelRequested=false;var actor=driver;Vector3 target=driver.transform.position;bool seated=false;
            // The interrupted approach is normally still a valid standing
            // location. Recover there instead of restoring an obsolete origin.
            if(FootPosition(target,out var standing)&&Vector3.Distance(target,standing)<.18f&&CanMoveDriver(standing))target=standing;
            else
            {
                bool clear=exiting&&CanMoveDriver(seatAnchor.position);seated=clear;if(clear)target=seatAnchor.position;
                if(!clear&&Outside(-1,out var left)&&CanMoveDriver(left)){target=left;clear=true;side=-1;}
                if(!clear&&Outside(1,out var right)&&CanMoveDriver(right)){target=right;clear=true;side=1;}
                if(!clear&&CanMoveDriver(seatAnchor.position)){target=seatAnchor.position;clear=seated=true;}
                // A physical blocker can itself move during recovery. Keep the
                // actor at the last safe pose while checking both doors again.
                while(!clear)
                {
                    actor.Hint("Weg blockiert. Ein freier Ausstieg wird gesucht.");yield return null;
                    if(Outside(-1,out left)&&CanMoveDriver(left)){target=left;clear=true;side=-1;}
                    else if(Outside(1,out right)&&CanMoveDriver(right)){target=right;clear=true;side=1;}
                    else if(CanMoveDriver(seatAnchor.position)){target=seatAnchor.position;clear=seated=true;}
                    else if(FootPosition(actor.transform.position,out standing)&&CanMoveDriver(standing)){target=standing;clear=true;}
                }
            }
            var from=actor.transform.position;float duration=Mathf.Clamp(Vector3.Distance(from,target)/1.4f,.25f,1.2f),elapsed=0;
            while(elapsed<duration)
            {
                float next=Mathf.Min(duration,elapsed+Time.deltaTime),t=Mathf.SmoothStep(0,1,next/duration);
                if(!MoveDriver(Vector3.Lerp(from,target,t))){actor.Hint("Warte auf einen freien Weg.");yield return null;continue;}
                elapsed=next;actor.transform.rotation=transform.rotation;
                float pose=seated?(exiting?Mathf.Lerp(progress,0,t):Mathf.Lerp(progress,1,t)):Mathf.Lerp(exiting?progress:1-progress,1,t);
                actor.avatar.VehiclePose(exiting||!seated?"ExitCar":"EnterCar",pose);actor.avatar.VehicleSide=side;
                yield return null;
            }
            if(seated)
            {
                State=Occupancy.Driving;actor.SetDriving(true);actor.avatar.BindSteeringWheel(steeringWheel);actor.avatar.VehiclePose("Drive");interaction=null;
                actor.Hint("Aussteigen abgebrochen. Wähle einen freien Platz.");
            }
            else
            {
                while(!RestoreOnFoot(target)){actor.Hint("Warte auf einen freien Standplatz.");yield return null;}
                actor.Hint("Einsteigen abgebrochen.");
            }
            closeDoorsWhenClear=true;
        }
        void DriverCapsule(Vector3 point,out Vector3 a,out Vector3 b,out float radius)
        {
            var controller=driver?driver.GetComponent<CharacterController>():null;
            radius=controller?controller.radius:.3f;float height=controller?controller.height:1.8f,center=controller?controller.center.y:.9f;
            // The native controller has a contact skin. Interaction sweeps
            // need a small sole clearance too, otherwise a kerb/terrain contact
            // is treated as an impassable wall after disabling that controller.
            a=point+Vector3.up*(center+height*.5f-radius-.015f);
            b=point+Vector3.up*(center-height*.5f+radius+.045f);
        }
        bool FootPosition(Vector3 desired,out Vector3 point,bool actors=true)
        {
            point=desired;Physics.SyncTransforms();
            int count=Physics.RaycastNonAlloc(desired+Vector3.up*2,Vector3.down,interactionHits,5,1,QueryTriggerInteraction.Ignore);
            float closest=float.PositiveInfinity;bool grounded=false;
            for(int i=0;i<count;i++)
            {
                var hit=interactionHits[i];if(hit.transform.IsChildOf(transform)||hit.normal.y<.75f||hit.distance>=closest)continue;
                point=hit.point+Vector3.up*.035f;closest=hit.distance;grounded=true;
            }
            if(!grounded||count==interactionHits.Length)return false;
            // Support the footprint, not just one infinitely thin centre ray.
            // At a kerb the leading edge of a foot reaches the raised pavement
            // before its centre does. Lift onto that step before a full-radius
            // capsule sweep would hit the vertical kerb face.
            float baseHeight=point.y,bestHeight=baseHeight;
            for(int probe=0;probe<8;probe++)
            {
                float angle=probe*Mathf.PI/4;var offset=new Vector3(Mathf.Cos(angle)*.31f,0,Mathf.Sin(angle)*.31f);
                int n=Physics.RaycastNonAlloc(desired+offset+Vector3.up*2,Vector3.down,interactionHits,5,1,QueryTriggerInteraction.Ignore);
                for(int h=0;h<n;h++)
                {
                    var hit=interactionHits[h];float height=hit.point.y+.035f;
                    if(!hit.transform.IsChildOf(transform)&&hit.normal.y>.75f&&height>bestHeight&&height-baseHeight<=.25f)bestHeight=height;
                }
            }
            point.y=bestHeight;
            DriverCapsule(point,out var a,out var b,out var radius);
            return Physics.OverlapCapsuleNonAlloc(a,b,radius,interactionContacts,actors?VehicleObstacles:1|(1<<8),QueryTriggerInteraction.Ignore)==0;
        }
        bool SafeFootPosition(Vector3 preferred,out Vector3 point)
        {
            if(FootPosition(preferred,out point)||FootPosition(driver.transform.position,out point))return true;
            if(FootPosition(transform.TransformPoint(new Vector3(-1.68f,0,-.44f)),out point)||FootPosition(transform.TransformPoint(new Vector3(1.68f,0,-.44f)),out point)||FootPosition(returnPosition,out point))return true;
            // Cleanup also runs if the vehicle is disabled midway through an
            // interaction. Never enable a standing controller inside its body
            // or at an old position that another actor has since occupied.
            for(float radius=2;radius<=12;radius+=1)
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;var candidate=transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                    if(FootPosition(candidate,out point))return true;
                }
            return false;
        }
        bool RestoreOnFoot(Vector3 preferred,bool emergencySearch=false)
        {
            if(!driver)return false;
            Vector3 point;
            if(emergencySearch){if(!SafeFootPosition(preferred,out point))return false;}
            else if(!FootPosition(preferred,out point)||!CanMoveDriver(point))return false;
            driver.transform.SetParent(returnParent,true);driver.transform.position=point;driver.transform.rotation=Quaternion.identity;driver.visual.rotation=transform.rotation;
            driver.SetDriving(false);driver.orbit.Follow(driver.transform,1.5f,5.5f);driver.orbit.ReleaseVehicle(transform);
            driver=null;State=Occupancy.Parked;ActionProgress=0;SeatTransferProgress=-1;interaction=null;return true;
        }
        void OnDisable()
        {
            if(!driver)return;
            if(interaction!=null)StopCoroutine(interaction);
            if(DoorSweepClear(-1,0))SetDoor(-1,0);if(DoorSweepClear(1,0))SetDoor(1,0);var rotation=returnRotation;
            Vector3 point=Outside(-1,out var exit)?exit:returnPosition;
            var player=driver;if(RestoreOnFoot(point,true))player.visual.rotation=rotation;
        }
        void Update()
        {
            if(driver&&(State==Occupancy.Entering||State==Occupancy.Exiting)&&Time.time>=cancelAfter&&Input.GetKeyDown(KeyCode.E))cancelRequested=true;
        }
        void OnGUI()
        {
            if(driver&&driver.Interacting)
                GUI.Label(new Rect(22,Screen.height-76,1000,28),string.IsNullOrEmpty(driver.InteractionHint)?"E · Vorgang abbrechen":driver.InteractionHint);
        }
        void FixedUpdate()
        {
            if(closeDoorsWhenClear)
            {
                bool pending=false;
                for(int i=0;i<2;i++)
                {
                    float side=i==0?-1:1;
                    float angle=Mathf.Abs(Mathf.DeltaAngle(0,doors[side<0?0:1].localEulerAngles.y));if(angle<.01f)continue;
                    float next=Mathf.MoveTowards(angle,0,120*Time.fixedDeltaTime);
                    if(DoorSweepClear(side,next))SetDoor(side,next);else next=angle;
                    pending|=next>.01f;
                }
                closeDoorsWhenClear=pending;
            }
            bool ready=State==Occupancy.Driving;
            DriveStep(ready?Input.GetAxis("Vertical"):0,ready?Input.GetAxis("Horizontal"):0,!ready||Input.GetKey(KeyCode.Space));
        }
        void LateUpdate()
        {
            if(driver&&State==Occupancy.Driving)
            {
                driver.transform.SetPositionAndRotation(seatAnchor.position,transform.rotation);driver.visual.localRotation=Quaternion.identity;driver.avatar.Steer(steerValue);
            }
        }
        public void DriveStep(float throttle,float steer,bool brake)
        {
            if(!body)body=GetComponent<Rigidbody>();
            if(State==Occupancy.Entering||State==Occupancy.Exiting){throttle=steer=0;brake=true;}
            steerValue=steer;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].steerAngle = i < 2 ? steer * Mathf.Lerp(29, 12, body.linearVelocity.magnitude / 25) : 0;
                wheels[i].motorTorque = i >= 2 && body.linearVelocity.magnitude < 25 ? throttle * 330 : 0;
                wheels[i].brakeTorque = brake ? 1600 : 0;
                wheels[i].GetWorldPose(out var position, out var rotation);
                wheelVisuals[i].SetPositionAndRotation(position, rotation);
            }
            // Ring geometry lies in local XZ: rotate about its local Y shaft,
            // then apply the fixed column tilt. The old Euler Z turn tilted it.
            steeringAngle=Mathf.Lerp(steeringAngle,-Mathf.Clamp(steer,-1,1)*75,1-Mathf.Exp(-9*Time.fixedDeltaTime));
            if(steeringWheel)steeringWheel.localRotation=SteeringTilt*Quaternion.AngleAxis(steeringAngle,Vector3.up);
            if(driver&&State==Occupancy.Driving)driver.transform.SetPositionAndRotation(seatAnchor.position,transform.rotation);
        }
        public void ResetSteeringVisual(){steeringAngle=0;if(steeringWheel)steeringWheel.localRotation=SteeringTilt;}
    }
}
