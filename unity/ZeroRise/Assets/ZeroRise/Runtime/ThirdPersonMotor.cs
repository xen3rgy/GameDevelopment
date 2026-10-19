using UnityEngine;

namespace ZeroRise
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        public OrbitCamera orbit;
        public Transform visual;
        public PlayerAvatar avatar;
        public SimpleCar nearbyCar;
        public bool driving;
        public bool Interacting { get; private set; }
        public string InteractionHint { get; private set; }
        CharacterController controller;
        float fall;
        Vector3 velocity;
        readonly RaycastHit[] actorHits=new RaycastHit[32];
        CapsuleCollider actorQuery;
        float groundGrace,jumpBuffer;
        float hintUntil;
        void Awake()
        {
            controller=GetComponent<CharacterController>();controller.minMoveDistance=0;
            var query=new GameObject("Disabled capsule for actor contact queries");query.transform.SetParent(transform,false);query.layer=gameObject.layer;
            actorQuery=query.AddComponent<CapsuleCollider>();actorQuery.enabled=false;actorQuery.center=controller.center;actorQuery.radius=controller.radius+.015f;actorQuery.height=controller.height;
        }
        void Start() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (Input.GetKeyDown(KeyCode.E) && nearbyCar != null && !Interacting)
            {
                if (driving) nearbyCar.TryExit(this);
                else if (Vector3.Distance(transform.position, nearbyCar.transform.position) < 4) nearbyCar.Enter(this);
            }
            if (driving || Interacting) return;
            var input = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
            MoveStep(input,Input.GetKey(KeyCode.LeftShift),Input.GetKeyDown(KeyCode.Space),Time.deltaTime,Input.GetKey(KeyCode.LeftAlt));
        }
        public bool IsGrounded => controller && controller.isGrounded;
        public bool CanInteract => IsGrounded||groundGrace>0;
        public float VerticalSpeed => fall;
        public void MoveStep(Vector2 input,bool running,bool jump,float dt,bool jogging=false)
        {
            if(driving||Interacting||dt<=0)return;
            input=Vector2.ClampMagnitude(input,1);
            var forward = Vector3.ProjectOnPlane(orbit.transform.forward, Vector3.up).normalized;
            var direction = forward * input.y + orbit.transform.right * input.x;
            direction.y = 0;
            groundGrace=controller.isGrounded&&fall<=0?.12f:Mathf.Max(0,groundGrace-dt);
            jumpBuffer=jump?.12f:Mathf.Max(0,jumpBuffer-dt);
            if (controller.isGrounded && fall < 0) fall = -2;
            if (groundGrace>0 && jumpBuffer>0){fall=5;groundGrace=0;jumpBuffer=0;}
            fall -= 18 * dt;
            var oldVelocity=velocity;
            float turn=0,remaining=0;
            if(direction.sqrMagnitude>.01f||Input.GetMouseButton(1))
            {
                var facing=direction.sqrMagnitude>.01f?direction:forward;
                float angle=Vector3.SignedAngle(visual.forward,facing,Vector3.up);
                float step=Mathf.Clamp(angle,-540*dt,540*dt);visual.rotation=Quaternion.AngleAxis(step,Vector3.up)*visual.rotation;
                turn=step/Mathf.Max(dt,.0001f)/540;remaining=Mathf.Abs(angle-step);
            }
            float requested=(running?PlayerGaitProfile.RunSpeed:jogging?PlayerGaitProfile.JogSpeed:PlayerGaitProfile.WalkSpeed)*input.magnitude;
            requested*=Mathf.Clamp01((100-remaining)/70);
            float pace= Mathf.MoveTowards(velocity.magnitude,requested,(remaining>65?32:direction.sqrMagnitude>.01f?12:18)*dt);
            // Translation and facing share one heading. A raw diagonal/reverse
            // input must not rotate the body away from its continuing velocity.
            velocity=visual.forward*pace;
            var before=transform.position;
            // Keep the controller's skin/step solver from penetrating people
            // or riding into traffic. Tangential movement still slides along
            // contact, while jumping and gravity keep their vertical motion.
            var horizontal=ConstrainActors(velocity*dt);
            controller.Move(horizontal+Vector3.up*fall*dt);
            float actual=Vector3.ProjectOnPlane(transform.position-before,Vector3.up).magnitude/Mathf.Max(dt,.0001f);
            if(velocity.sqrMagnitude>.04f&&actual<velocity.magnitude*.1f&&direction.sqrMagnitude>.01f)velocity=Vector3.zero;
            if(avatar)avatar.Motion(actual,fall,controller.isGrounded,dt,turn,Vector3.Dot(velocity-oldVelocity,visual.forward)/dt);
        }
        Vector3 ConstrainActors(Vector3 movement)
        {
            if(movement.sqrMagnitude<.000001f)return movement;
            Physics.SyncTransforms();
            var first=SweepActors(transform.position,movement,out var normal);
            if(normal.sqrMagnitude<.001f)return first;
            var remainder=movement-first;normal.y=0;normal.Normalize();
            var slide=Vector3.ProjectOnPlane(remainder,normal);slide.y=0;
            return first+SweepActors(transform.position+first,slide,out _);
        }
        Vector3 SweepActors(Vector3 position,Vector3 movement,out Vector3 normal)
        {
            normal=Vector3.zero;float distance=movement.magnitude;if(distance<.0001f)return Vector3.zero;
            var center=position+transform.rotation*controller.center;float half=Mathf.Max(0,controller.height*.5f-controller.radius);
            var direction=movement/distance;float allowed=distance;
            int count=Physics.CapsuleCastNonAlloc(center+Vector3.up*half,center-Vector3.up*half,controller.radius+.015f,direction,actorHits,distance+.012f,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=actorHits[i];if(hit.transform.IsChildOf(transform)||hit.normal.y>.7f)continue;
                var contactNormal=hit.normal;
                if(hit.distance<.0001f)
                {
                    // Initial-overlap casts can return distance zero and a
                    // fabricated normal opposite the requested direction. That
                    // also blocked escape from a moving vehicle's corner.
                    // Use the actual separation direction, and always permit
                    // movement away from or along an existing contact.
                    if(!Physics.ComputePenetration(actorQuery,position,transform.rotation,hit.collider,hit.collider.transform.position,hit.collider.transform.rotation,out contactNormal,out _))continue;
                }
                if(contactNormal.y>.7f||Vector3.Dot(direction,contactNormal)>=-.0001f)continue;
                float advance=Mathf.Max(0,hit.distance-.012f);
                if(advance>=allowed)continue;allowed=advance;normal=contactNormal;
            }
            return direction*allowed;
        }
        public bool BlocksTrafficBox(Vector3 center,Vector3 halfSize,Quaternion rotation)
        {
            if(!Interacting||!gameObject.activeInHierarchy)return false;
            var local=Quaternion.Inverse(rotation)*(transform.position+Vector3.up*controller.center.y-center);
            float radius=controller.radius+controller.skinWidth+.025f,half=Mathf.Max(0,controller.height*.5f-controller.radius);
            var separation=new Vector3(Mathf.Max(0,Mathf.Abs(local.x)-halfSize.x),Mathf.Max(0,Mathf.Abs(local.y)-half-halfSize.y),Mathf.Max(0,Mathf.Abs(local.z)-halfSize.z));
            return separation.sqrMagnitude<radius*radius;
        }
        public void BeginInteraction()
        {
            Interacting=true;controller.enabled=false;fall=0;velocity=Vector3.zero;groundGrace=jumpBuffer=0;InteractionHint=null;
        }
        public void Hint(string value){InteractionHint=value;hintUntil=Time.time+3;}
        public void SetDriving(bool value)
        {
            driving=value;Interacting=false;controller.enabled=!value;visual.gameObject.SetActive(true);fall=0;velocity=Vector3.zero;groundGrace=jumpBuffer=0;
            if(!value&&avatar)avatar.ResetMotion();
        }
        void OnGUI()
        {
            GUI.Label(new Rect(22, 20, 550, 32), "ZERO // RISE  ·  WESTBRUCH  ·  UNITY WINDOWS PROTOTYP");
            GUI.Label(new Rect(22, Screen.height - 44, 900, 32), Interacting?"Ein-/Aussteigen …":driving
                ? "W/S Fahren · A/D Lenken · Leertaste Bremse · E Aussteigen · Maus Kamera · Esc Maus freigeben"
                : "WASD Gehen · Alt Joggen · Shift Sprinten · Leertaste Springen · E Auto · Rechte Maustaste Ausrichten · Esc Maus freigeben");
            if(!Interacting&&Time.time<hintUntil&&!string.IsNullOrEmpty(InteractionHint))GUI.Label(new Rect(22,Screen.height-76,800,28),InteractionHint);
            else if(!driving&&!Interacting&&nearbyCar&&Vector3.Distance(transform.position,nearbyCar.transform.position)<4)GUI.Label(new Rect(22,Screen.height-76,800,28),"E · Einsteigen");
        }
    }
}
