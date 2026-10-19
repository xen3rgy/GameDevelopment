using UnityEngine;

namespace ZeroRise
{
    // Visual state only. Movement, collision and camera remain on the motor.
    [DefaultExecutionOrder(80)]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        public Animator animator;
        public SkinnedMeshRenderer skin;
        public Transform leftEye,rightEye,hips;
        public Transform chest,backpack,leftArm,rightArm;
        public Transform head,leftForearm,rightForearm,leftHand,rightHand;
        public Transform leftThigh,leftCalf,leftFoot,rightThigh,rightCalf,rightFoot;
        public float ActualSpeed { get; private set; }
        public float VehicleSide { get; set; }=-1;
        public bool Grounded { get; private set; }
        static readonly int Speed=Animator.StringToHash("Speed"),OnGround=Animator.StringToHash("Grounded"),Vertical=Animator.StringToHash("VerticalSpeed");
        float blinkClock;
        float timeSinceGround,gaitPhase;
        bool hasSupport;float supportHeight;
        float turn,lean,idleClock,steering;
        bool moving,inVehicle;
        Transform steeringWheel;
        float handWeight;
        float groundOffset,idleSupportOffset;
        string vehicleClip;
        float vehicleProgress;
        bool transferToDriver;
        sealed class Plant
        {
            public bool locked,turnReleased,recovering,hasPose;public Vector3 anchor,normal,recoveryStart,lastPosition;public Quaternion facing,recoveryRotation,lastRotation;
            public float weight,groundHeight,recovery;
        }
        readonly Plant leftPlant=new Plant(),rightPlant=new Plant();
        public float GaitPhase=>gaitPhase;
        public float LeftFootPlantWeight=>leftPlant.weight;
        public float RightFootPlantWeight=>rightPlant.weight;
        public void Motion(float horizontalSpeed,float verticalSpeed,bool grounded,float dt,float turning=0,float acceleration=0)
        {
            ActualSpeed=horizontalSpeed;
            timeSinceGround=grounded?0:timeSinceGround+dt;
            // Brief CharacterController contact gaps on kerbs/tiny frame steps
            // should not restart falling/landing while standing. Upward jumps
            // bypass this visual grace immediately; physics is unchanged.
            RaycastHit support=default;
            hasSupport=transform.parent&&Physics.Raycast(transform.parent.position+Vector3.up*.35f,Vector3.down,out support,.49f,1,QueryTriggerInteraction.Ignore);
            supportHeight=hasSupport?support.point.y:0;
            Grounded=grounded||(verticalSpeed<=0&&(hasSupport||(timeSinceGround<.06f&&verticalSpeed>-3.5f)));
            if(!animator||!animator.isActiveAndEnabled)return;
            turn=Mathf.Lerp(turn,turning,1-Mathf.Exp(-9*dt));lean=Mathf.Lerp(lean,Mathf.Clamp(acceleration*.25f,-5,5),1-Mathf.Exp(-8*dt));
            animator.SetFloat(Speed,horizontalSpeed);
            if(Grounded&&horizontalSpeed>.025f)gaitPhase=Mathf.Repeat(gaitPhase+horizontalSpeed*dt/PlayerGaitProfile.Stride(horizontalSpeed),1);
            animator.SetFloat("GaitPhase",gaitPhase);
            animator.SetFloat(Vertical,verticalSpeed);animator.SetBool(OnGround,Grounded);
            animator.SetFloat("Turn",turn);
            bool nowMoving=horizontalSpeed>.18f;
            moving=nowMoving;idleClock=nowMoving||!Grounded?0:idleClock+dt;
            if(idleClock>18){animator.SetTrigger("Relax");idleClock=0;}
        }
        public void VehiclePose(string state,float progress=-1,float side=-1)
        {
            inVehicle=true;ActualSpeed=0;turn=lean=groundOffset=idleSupportOffset=0;VehicleSide=side;transform.localPosition=Vector3.zero;
            vehicleClip=state;vehicleProgress=progress;
            if(progress>=0)handWeight=0;
            if(progress>=0)animator.Play(state,0,progress);
            else animator.CrossFadeInFixedTime(state,.12f);
        }
        public void CabinTransfer(float progress,bool toDriver)
        {
            // Re-sample a fresh seated pose every frame; offsets never add up.
            VehiclePose("Drive",0);vehicleClip="CabinTransfer";vehicleProgress=progress;transferToDriver=toDriver;
        }
        void TransferPose()
        {
            float wave=Mathf.Sin(Mathf.Clamp01(vehicleProgress)*Mathf.PI);wave*=wave;
            float direction=transferToDriver?-1:1;
            if(chest)chest.localRotation*=Quaternion.Euler(Mathf.Sqrt(wave)*55,direction*wave*18,-direction*wave*5);
            if(head)head.localRotation*=Quaternion.Euler(-wave*14,-direction*wave*12,0);
            if(hips)hips.localRotation*=Quaternion.Euler(0,direction*wave*14,0);
            if(leftArm)leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(-35,0,-18),wave);
            if(rightArm)rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(-35,0,18),wave);
            if(leftForearm)leftForearm.localRotation=Quaternion.Slerp(leftForearm.localRotation,Quaternion.Euler(-85,0,0),wave);
            if(rightForearm)rightForearm.localRotation=Quaternion.Slerp(rightForearm.localRotation,Quaternion.Euler(-85,0,0),wave);
            TransferFoot(leftThigh,leftCalf,leftFoot,-1,wave);TransferFoot(rightThigh,rightCalf,rightFoot,1,wave);
        }
        void TransferFoot(Transform thigh,Transform calf,Transform foot,float side,float wave)
        {
            if(!thigh||!calf||!foot)return;
            float tuck=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.01f,.12f,vehicleProgress))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,.99f,vehicleProgress)));
            var target=transform.TransformPoint(new Vector3(side*.12f,.22f+tuck*.32f,.28f-wave*.12f));
            var pole=transform.TransformPoint(new Vector3(side*.15f,.8f,.8f));
            AvatarLimbIK.Solve(thigh,calf,foot,target,pole,transform.rotation,Mathf.Max(wave,tuck));
        }
        public void Steer(float value){steering=Mathf.Lerp(steering,value,1-Mathf.Exp(-10*Time.deltaTime));}
        public void BindSteeringWheel(Transform wheel){steeringWheel=wheel;handWeight=0;}
        public void ResetMotion()
        {
            ActualSpeed=0;Grounded=true;blinkClock=0;timeSinceGround=0;idleClock=0;turn=lean=steering=0;moving=inVehicle=false;
            steeringWheel=null;handWeight=0;leftPlant.locked=rightPlant.locked=false;leftPlant.weight=rightPlant.weight=0;
            leftPlant.turnReleased=rightPlant.turnReleased=leftPlant.recovering=rightPlant.recovering=false;hasSupport=false;
            leftPlant.hasPose=rightPlant.hasPose=false;
            idleSupportOffset=0;
            if(!animator||!animator.isActiveAndEnabled)return;
            animator.SetFloat(Speed,0);animator.SetFloat(Vertical,0);animator.SetFloat("Turn",0);animator.SetBool(OnGround,true);
            foreach(string trigger in new[]{"MoveStart","MoveStop","Relax"})animator.ResetTrigger(trigger);
            animator.CrossFadeInFixedTime("Idle",.12f);
        }
        void LateUpdate()
        {
            // Tiny eye bones close the sclera/iris together; no material swapping.
            blinkClock+=Time.deltaTime;float cycle=blinkClock%4.7f;
            float blink=cycle>.08f&&cycle<.24f?Mathf.Sin((cycle-.08f)/.16f*Mathf.PI):0;
            var scale=new Vector3(1,1-.94f*blink*blink,1);
            if(leftEye)leftEye.localScale=scale;if(rightEye)rightEye.localScale=scale;
            if(!inVehicle)
            {
                // The controller's contact skin may settle its origin slightly
                // below the ground. Correct the visual root before solving feet;
                // bending both knees to compensate made the old gait crouch.
                float offset=0;
                if(Grounded&&hasSupport&&transform.parent)offset=Mathf.Clamp(supportHeight-transform.parent.position.y+.004f,-.10f,.12f);
                groundOffset=Mathf.Lerp(groundOffset,offset,1-Mathf.Exp(-16*Time.deltaTime));transform.localPosition=Vector3.up*groundOffset;
                var state=animator.GetCurrentAnimatorStateInfo(0);float support=0;
                var next=animator.GetNextAnimatorStateInfo(0);
                bool standingBlend=!animator.IsInTransition(0)||next.IsName("Idle")||next.IsName("IdleLook");
                if(Grounded&&ActualSpeed<.035f&&standingBlend&&(state.IsName("Idle")||state.IsName("IdleLook")))
                    support=Mathf.Max(IdleLegSupport(leftThigh,leftCalf,leftFoot,leftPlant),IdleLegSupport(rightThigh,rightCalf,rightFoot,rightPlant));
                idleSupportOffset=Mathf.Lerp(idleSupportOffset,support,1-Mathf.Exp(-10*Time.deltaTime));
                if(hips)hips.localPosition-=Vector3.up*idleSupportOffset;
                if(chest)chest.localPosition+=Vector3.up*(idleSupportOffset*.7f);
                if(chest)chest.localRotation*=Quaternion.Euler(lean,turn*5,-turn*Mathf.Clamp(ActualSpeed*1.1f,0,6));
                if(backpack)backpack.localRotation*=Quaternion.Euler(-lean*.35f,0,turn*2);
                GroundFoot(leftThigh,leftCalf,leftFoot,leftPlant,animator.GetFloat("LeftPlant"),-1);
                GroundFoot(rightThigh,rightCalf,rightFoot,rightPlant,animator.GetFloat("RightPlant"),1);
            }
            else if(vehicleClip=="CabinTransfer")TransferPose();
            else if(VehicleSide>0&&(animator.GetCurrentAnimatorStateInfo(0).IsName("ExitCar")||animator.GetCurrentAnimatorStateInfo(0).IsName("EnterCar")))
            {
                Mirror(leftArm,rightArm);Mirror(leftForearm,rightForearm);Mirror(leftHand,rightHand);
                Mirror(leftThigh,rightThigh);Mirror(leftCalf,rightCalf);Mirror(leftFoot,rightFoot);
                foreach(var bone in new[]{hips,chest})if(bone){var r=bone.localEulerAngles;bone.localRotation=Quaternion.Euler(r.x,-r.y,-r.z);}
                if(head){var r=head.localEulerAngles;head.localRotation=Quaternion.Euler(r.x,-r.y,-r.z);}
                BoardingGrip();
            }
            else if(animator.GetCurrentAnimatorStateInfo(0).IsName("Drive"))
            {
                handWeight=Mathf.MoveTowards(handWeight,1,Time.deltaTime*8);
                if(head)head.localRotation*=Quaternion.Euler(0,steering*4,0);
                Grip(leftArm,leftForearm,leftHand,-1);Grip(rightArm,rightForearm,rightHand,1);
            }
            else BoardingGrip();
        }
        void BoardingGrip()
        {
            float left=0,right=0,t=vehicleProgress;
            if(vehicleClip=="EnterCar")
            {
                left=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,t));right=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,.90f,t));
            }
            else if(vehicleClip=="ExitCar")
            {
                left=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.16f,t));right=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.34f,t));
                if(VehicleSide>0){float swap=left;left=right;right=swap;}
            }
            Grip(leftArm,leftForearm,leftHand,-1,left);Grip(rightArm,rightForearm,rightHand,1,right);
        }
        void Grip(Transform arm,Transform forearm,Transform hand,float side,float weight=-1)
        {
            if(!steeringWheel)return;
            var target=steeringWheel.TransformPoint(new Vector3(side*.19f,-.022f,0));
            var pole=arm.position+transform.right*(side*.30f)-transform.forward*.18f-Vector3.up*.20f;
            var rotation=steeringWheel.rotation*Quaternion.Euler(0,0,-side*90);
            AvatarLimbIK.Solve(arm,forearm,hand,target,pole,rotation,weight<0?handWeight:weight);
        }
        void GroundFoot(Transform thigh,Transform calf,Transform foot,Plant plant,float contact,float side)
        {
            if(!foot||!thigh||!calf)return;
            if(!Grounded||!Physics.Raycast(foot.position+Vector3.up*.32f,Vector3.down,out var ground,.8f,1,QueryTriggerInteraction.Ignore)||ground.normal.y<.65f)
            {plant.weight=0;plant.locked=plant.recovering=plant.hasPose=false;return;}
            float wanted=Mathf.Clamp01(contact);
            var heel=foot.TransformPoint(new Vector3(0,-.065f,-.065f));var toe=foot.TransformPoint(new Vector3(0,-.065f,.145f));
            float correction=ground.point.y+.004f-Mathf.Min(heel.y,toe.y);
            if(Mathf.Abs(correction)>.18f)wanted=0;
            if(contact<.12f){plant.locked=false;plant.turnReleased=false;}
            var target=foot.position+Vector3.up*Mathf.Clamp(correction,-.12f,.12f);
            var rotation=Quaternion.FromToRotation(transform.up,ground.normal)*foot.rotation;
            var authoredTarget=foot.position+Vector3.up*Mathf.Max(0,correction);var authoredRotation=rotation;
            if(wanted>.20f&&!plant.turnReleased&&!plant.recovering)
            {
                // A state blend must not turn an airborne swing foot into a
                // planted foot just because Idle contributes contact = 1.
                if(!plant.locked&&Mathf.Abs(correction)<.025f){plant.anchor=target;plant.facing=transform.rotation;plant.groundHeight=ground.point.y;plant.normal=ground.normal;plant.locked=true;}
                if(!plant.locked)wanted=0;
                if(plant.locked)
                {
                    float yaw=Quaternion.Angle(plant.facing,transform.rotation);
                    wanted*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(35,65,yaw));
                    if(yaw>65){plant.turnReleased=true;plant.locked=false;}
                    else
                    {
                        rotation=Quaternion.FromToRotation(Vector3.up,plant.normal)*plant.facing*(Quaternion.Inverse(transform.rotation)*foot.rotation);
                        var heelOffset=rotation*new Vector3(0,-.065f,-.065f);var toeOffset=rotation*new Vector3(0,-.065f,.145f);
                        target=plant.anchor;target.y=plant.groundHeight+.004f-Mathf.Min(heelOffset.y,toeOffset.y);
                    }
                }
            }
            // A fast turn or stopping blend can leave the last contact beyond
            // leg reach. Take a small visible settling step instead of lifting
            // a supposedly planted sole or dragging its anchor toward the body.
            float reach=Vector3.Distance(thigh.position,calf.position)+Vector3.Distance(calf.position,foot.position)-.002f;
            // Millimetre solver tolerances must never become visible steps.
            // Stationary support adjusts the stance rather than cycling feet.
            var state=animator.GetCurrentAnimatorStateInfo(0);var next=animator.GetNextAnimatorStateInfo(0);
            bool gaitBlend=animator.IsInTransition(0)&&(state.IsName("Locomotion")||state.IsName("Turning")||state.IsName("Land")||next.IsName("Locomotion")||next.IsName("Turning"));
            bool mayStep=ActualSpeed>.035f||Mathf.Abs(turn)>.12f||gaitBlend;
            if(mayStep&&plant.locked&&wanted>.65f&&Vector3.Distance(thigh.position,target)>reach+.006f)
            {plant.recovering=true;plant.recovery=0;plant.recoveryStart=plant.hasPose?plant.lastPosition:target;plant.recoveryRotation=plant.hasPose?plant.lastRotation:rotation;plant.locked=false;}
            if(plant.recovering)
            {
                plant.recovery=Mathf.Min(1,plant.recovery+Time.deltaTime/.20f);
                var home=authoredTarget;
                rotation=Quaternion.Slerp(plant.recoveryRotation,authoredRotation,Mathf.SmoothStep(0,1,plant.recovery));
                float lift=Mathf.Sin(plant.recovery*Mathf.PI);
                target=Vector3.Lerp(plant.recoveryStart,home,Mathf.SmoothStep(0,1,plant.recovery))+Vector3.up*(lift*lift*.055f);
                plant.weight=0;
                if(plant.recovery>=1){plant.recovering=false;plant.locked=contact>.20f&&Mathf.Abs(correction)<.025f;plant.turnReleased=false;plant.anchor=home;plant.facing=transform.rotation;plant.normal=ground.normal;plant.groundHeight=ground.point.y;}
                AvatarLimbIK.Solve(thigh,calf,foot,target,thigh.position+transform.forward*.55f+transform.right*(side*.025f),rotation,1);
                FinishFoot(thigh,calf,foot,plant,ground.point.y,side);return;
            }
            if(plant.turnReleased)wanted=0;
            plant.weight=wanted;
            var pole=thigh.position+transform.forward*.55f+transform.right*(side*.025f);
            AvatarLimbIK.Solve(thigh,calf,foot,target,pole,rotation,plant.weight);
            FinishFoot(thigh,calf,foot,plant,ground.point.y,side);
        }
        float IdleLegSupport(Transform thigh,Transform calf,Transform foot,Plant plant)
        {
            if(!thigh||!calf||!foot||plant.recovering)return 0;
            var target=plant.locked?plant.anchor:foot.position;
            if(!Physics.Raycast(target+Vector3.up*.32f,Vector3.down,out var ground,.8f,1,QueryTriggerInteraction.Ignore)||ground.normal.y<.65f)return 0;
            var rotation=Quaternion.FromToRotation(transform.up,ground.normal)*foot.rotation;
            target.y=ground.point.y+.004f-Mathf.Min((rotation*new Vector3(0,-.065f,-.065f)).y,(rotation*new Vector3(0,-.065f,.145f)).y);
            float reach=Vector3.Distance(thigh.position,calf.position)+Vector3.Distance(calf.position,foot.position)-.006f;
            float horizontal=Vector3.ProjectOnPlane(target-thigh.position,Vector3.up).sqrMagnitude;
            float allowed=target.y+Mathf.Sqrt(Mathf.Max(0,reach*reach-horizontal));
            return Mathf.Clamp(thigh.position.y-allowed,0,.055f);
        }
        void FinishFoot(Transform thigh,Transform calf,Transform foot,Plant plant,float floor,float side)
        {
            float sole=Mathf.Min(foot.TransformPoint(new Vector3(0,-.065f,-.065f)).y,foot.TransformPoint(new Vector3(0,-.065f,.145f)).y);
            if(sole<floor+.003f)
                AvatarLimbIK.Solve(thigh,calf,foot,foot.position+Vector3.up*(floor+.004f-sole),thigh.position+transform.forward*.55f+transform.right*(side*.025f),foot.rotation,1);
            plant.hasPose=true;plant.lastPosition=foot.position;plant.lastRotation=foot.rotation;
        }
        static void Mirror(Transform left,Transform right)
        {
            if(!left||!right)return;var a=left.localEulerAngles;var b=right.localEulerAngles;
            left.localRotation=Quaternion.Euler(b.x,-b.y,-b.z);right.localRotation=Quaternion.Euler(a.x,-a.y,-a.z);
        }
    }
}
