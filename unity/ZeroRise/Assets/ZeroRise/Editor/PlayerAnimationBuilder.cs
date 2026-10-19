using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ZeroRise.Editor
{
    internal static class PlayerAnimationBuilder
    {
        sealed class Pose
        {
            public Vector3[] rotation;
            public float hipsY,hipsX,chestY,leftPlant=1,rightPlant=1;
            public Pose(int count,float y){rotation=new Vector3[count];hipsY=y;}
        }
        public static RuntimeAnimatorController Build(Transform root,Transform[] bones)
        {
            string folder=PlayerAvatarBuilder.Folder;
            var clips=new Dictionary<string,AnimationClip>();
            float hipsRest=bones[1].localPosition.y,chestRest=bones[2].localPosition.y;
            Pose Idle(float t)
            {
                // Soft knees leave genuine reach headroom; the former fully
                // straight legs continuously retriggered the foot safety step.
                var p=new Pose(bones.Length,hipsRest-.010f){chestY=chestRest+.008f+Mathf.Sin(t*Mathf.PI*2)*.0012f};
                p.rotation[2]=new Vector3(Mathf.Sin(t*Mathf.PI*2)*.28f,0,0);
                p.rotation[4]=new Vector3(.22f*Mathf.Sin(t*Mathf.PI*2),Mathf.Sin(t*Mathf.PI*2)*.7f,0);
                for(int side=0;side<2;side++){Leg(p,side,0,.065f);int arm=side==0?6:12;p.rotation[arm]=new Vector3(4,0,side==0?-3:3);p.rotation[arm+1]=new Vector3(-12,0,0);}
                return p;
            }
            void Leg(Pose p,int side,float z,float ankleY,float pitch=0)
            {
                int thigh=side==0?9:15;
                float upper=.38f,lower=.405f,dy=p.hipsY-ankleY;
                float distance=Mathf.Min(Mathf.Sqrt(dy*dy+z*z),upper+lower-.0001f);
                float knee=Mathf.Acos(Mathf.Clamp((distance*distance-upper*upper-lower*lower)/(2*upper*lower),-1,1));
                float hip=Mathf.Atan2(-z,dy)-Mathf.Atan2(lower*Mathf.Sin(knee),upper+lower*Mathf.Cos(knee));
                p.rotation[thigh]=new Vector3(hip*Mathf.Rad2Deg,0,0);
                p.rotation[thigh+1]=new Vector3(knee*Mathf.Rad2Deg,0,0);
                p.rotation[thigh+2]=new Vector3(-(hip+knee)*Mathf.Rad2Deg+pitch,0,0);
            }
            Pose Gait(float phase,float speed,float duration,float stance,float lift,float lean,float swing)
            {
                float travel=speed*duration*stance;
                bool walking=speed<2;float wave=Mathf.Sin(phase*Mathf.PI*2);
                var p=Idle(phase);p.hipsY=walking?.818f-Mathf.Cos(phase*Mathf.PI*4)*.004f:speed<4?.796f-Mathf.Cos(phase*Mathf.PI*4)*.009f:.791f-Mathf.Cos(phase*Mathf.PI*4)*.013f;
                p.hipsX=-wave*(walking?.012f:.008f);
                p.rotation[1]=new Vector3(0,wave*(walking?3:5),-wave*(walking?1.2f:1.7f));
                // Low, smooth weight transfer, independent of swing-foot height.
                // Valid stance reach comes from the shorter contact durations.
                p.chestY=chestRest-(p.hipsY-hipsRest)*(walking?.35f:.30f);
                p.rotation[2]=new Vector3(lean,-wave*(walking?4:6),wave*.75f);
                p.rotation[3]=new Vector3(-lean*.65f,wave*1.5f,0);p.rotation[4]=new Vector3(0,wave,0);
                p.rotation[5]=new Vector3(Mathf.Sin((phase-.035f)*Mathf.PI*4)*.75f,0,Mathf.Sin((phase-.04f)*Mathf.PI*2)*.65f);
                for(int side=0;side<2;side++)
                {
                    float f=Mathf.Repeat(phase+side*.5f,1),z,ankle=.065f,pitch=0;
                    float plant=0;
                    if(f<stance)
                    {
                        float q=f/stance;z=travel*(.5f-q);pitch=walking?FootRoll(q):0;ankle=SoleHeight(pitch);
                        plant=Mathf.SmoothStep(0,1,q/.07f)*(1-Mathf.SmoothStep(0,1,(q-.88f)/.12f));
                    }
                    else
                    {
                        float u=(f-stance)/(1-stance);
                        float ease=u*u*u*(u*(u*6-15)+10);
                        z=travel*(ease-.5f);
                        pitch=walking?Mathf.Lerp(28,-20,ease)-Mathf.Sin(u*Mathf.PI)*7:-Mathf.Sin(u*Mathf.PI)*14;
                        ankle=SoleHeight(pitch)+Mathf.Sin(u*Mathf.PI)*lift;
                    }
                    if(side==0)p.leftPlant=plant;else p.rightPlant=plant;
                    Leg(p,side,z,ankle,pitch);
                    int arm=side==0?6:12;
                    p.rotation[arm]=new Vector3(Mathf.Cos(f*Mathf.PI*2)*swing+lean*.4f,Mathf.Sin(f*Mathf.PI*2)*2,side==0?-3:3);
                    p.rotation[arm+1]=new Vector3(-(walking?11:62)+Mathf.Sin((f-.07f)*Mathf.PI*2)*(walking?4:7),0,0);
                    p.rotation[arm+2]=new Vector3(-2,0,Mathf.Sin((f-.10f)*Mathf.PI*2)*2);
                }
                return p;
            }
            float FootRoll(float q)=>q<.18f?Mathf.Lerp(-20,0,Mathf.SmoothStep(0,1,q/.18f)):Mathf.Lerp(0,28,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,q)));
            float SoleHeight(float pitch)
            {
                float a=pitch*Mathf.Deg2Rad;
                return .065f*Mathf.Cos(a)+Mathf.Max(-.065f*Mathf.Sin(a),.145f*Mathf.Sin(a));
            }
            Pose Air(float t,bool falling)
            {
                var p=Idle(t);p.leftPlant=p.rightPlant=0;float weight=falling?1:Mathf.SmoothStep(0,1,t);
                p.chestY=chestRest;p.rotation[2]=new Vector3(5*weight,0,0);p.rotation[3]=new Vector3(-3*weight,0,0);p.rotation[4]=Vector3.zero;
                for(int side=0;side<2;side++)
                {
                    Leg(p,side,(side==0?.05f:-.07f)*weight,.065f+(falling?.10f:.18f)*weight,-5*weight);
                    int arm=side==0?6:12;p.rotation[arm]=new Vector3(-17*weight,0,(side==0?-1:1)*11*weight);p.rotation[arm+1]=new Vector3(-34*weight,0,0);
                }
                return p;
            }
            Pose Land(float t)
            {
                var p=Idle(0);float impact=Mathf.Sin(t*Mathf.PI);p.hipsY=hipsRest-impact*.07f;p.chestY=chestRest;
                p.rotation[2]=new Vector3(impact*7,0,0);p.rotation[3]=new Vector3(-impact*4,0,0);
                for(int side=0;side<2;side++){Leg(p,side,0,.065f);int arm=side==0?6:12;p.rotation[arm].x=-impact*12;p.rotation[arm+1].x=-12-impact*16;}
                return p;
            }
            Pose Drive(float t)
            {
                var p=Idle(t);p.hipsY=.5f;p.chestY=chestRest;p.leftPlant=p.rightPlant=0;
                p.rotation[2]=new Vector3(5+Mathf.Sin(t*Mathf.PI*2)*.5f,0,0);p.rotation[3]=new Vector3(-5,0,0);
                for(int side=0;side<2;side++)
                {
                    Leg(p,side,.28f,.15f);int arm=side==0?6:12;
                    p.rotation[arm]=new Vector3(-63,0,side==0?8:-8);p.rotation[arm+1]=new Vector3(-30,0,0);p.rotation[arm+2]=new Vector3(8,0,0);
                }
                return p;
            }
            Pose Blend(Pose a,Pose b,float t)
            {
                var p=new Pose(bones.Length,Mathf.Lerp(a.hipsY,b.hipsY,t)){chestY=Mathf.Lerp(a.chestY,b.chestY,t),hipsX=Mathf.Lerp(a.hipsX,b.hipsX,t),leftPlant=Mathf.Lerp(a.leftPlant,b.leftPlant,t),rightPlant=Mathf.Lerp(a.rightPlant,b.rightPlant,t)};
                for(int i=0;i<bones.Length;i++)p.rotation[i]=Vector3.Lerp(a.rotation[i],b.rotation[i],t);
                return p;
            }
            Pose Board(float t,bool exiting)
            {
                float sit=Mathf.SmoothStep(0,1,Mathf.InverseLerp(exiting?.28f:.34f,exiting?.72f:.76f,t));
                if(exiting)sit=1-sit;
                var p=Blend(Idle(0),Drive(0),sit);
                float duck=Mathf.Sin(sit*Mathf.PI);p.rotation[2].x+=duck*25;p.rotation[3].x-=duck*12;
                float reach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.03f,.15f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.48f,t)));
                int arm=exiting?6:12;
                p.rotation[arm]=Vector3.Lerp(p.rotation[arm],new Vector3(-15,exiting?15:-15,exiting?-62:62),reach);
                p.rotation[arm+1]=Vector3.Lerp(p.rotation[arm+1],new Vector3(-28,0,0),reach);
                float close=Mathf.Sin(Mathf.InverseLerp(.76f,1,t)*Mathf.PI);
                if(!exiting)p.rotation[6]=Vector3.Lerp(p.rotation[6],new Vector3(-12,10,-55),close*.8f);
                else p.rotation[12]=Vector3.Lerp(p.rotation[12],new Vector3(-15,-15,62),close);
                p.rotation[4].y=(exiting?-1:1)*reach*18;
                return p;
            }
            Pose WeightShift(float t,bool stop)
            {
                var p=Idle(0);float weight=Mathf.Sin(t*Mathf.PI);p.hipsY=hipsRest-.035f*weight;
                p.rotation[2]=new Vector3(weight*(stop?-5:7),0,0);p.rotation[3].x=-p.rotation[2].x*.65f;
                for(int side=0;side<2;side++)
                {
                    Leg(p,side,weight*(side==0?.025f:-.025f),.065f);
                    int arm=side==0?6:12;p.rotation[arm].x+=weight*(side==0?-12:12);p.rotation[arm+1].x-=weight*15;
                }
                return p;
            }
            Pose Turn(float t,float sign)
            {
                var p=Idle(t);float wave=Mathf.Sin(t*Mathf.PI*2);p.hipsY=hipsRest-.025f;
                p.rotation[2]=new Vector3(0,-sign*8,sign*1.5f);p.rotation[4].y=sign*12;
                for(int side=0;side<2;side++)
                {
                    float step=Mathf.Max(0,(side==0?1:-1)*wave);Leg(p,side,0,.065f+.045f*step);
                    p.rotation[side==0?9:15].y=sign*wave*8;
                    float contact=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.25f,step));
                    if(side==0)p.leftPlant=contact;else p.rightPlant=contact;
                }
                return p;
            }
            Pose Relax(float t)
            {
                var p=Idle(t);float look=Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI*2);
                p.rotation[4]=new Vector3(Mathf.Sin(t*Mathf.PI)*-1.2f,look*9,look*.6f);
                p.rotation[2].y=look*1.5f;p.rotation[5].x=Mathf.Sin(t*Mathf.PI)*.6f;
                p.rotation[6].x+=Mathf.Sin(t*Mathf.PI)*-2.5f;
                return p;
            }
            void Clip(string name,float duration,bool loop,Func<float,Pose> pose)
            {
                string path=folder+"/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
                clip.name=name;clip.frameRate=60;clip.legacy=false;
                int frames=Mathf.CeilToInt(duration*60);var samples=new Pose[frames+1];
                for(int i=0;i<=frames;i++)samples[i]=pose(i/(float)frames);
                void Curve(string bone,string property,Func<Pose,float> value)
                {
                    var keys=new Keyframe[frames+1];float step=duration/frames;
                    for(int i=0;i<=frames;i++)
                    {
                        int before=i==0?(loop?frames-1:0):i-1,after=i==frames?(loop?1:frames):i+1;
                        float slope=(value(samples[after])-value(samples[before]))/(i==0||i==frames?(loop?2*step:step):2*step);
                        keys[i]=new Keyframe(i*step,value(samples[i]),slope,slope);
                    }
                    clip.SetCurve(bone,typeof(Transform),property,new AnimationCurve(keys));
                }
                for(int index=1;index<18;index++)
                {
                    int id=index;string binding=AnimationUtility.CalculateTransformPath(bones[id],root);
                    Curve(binding,"localEulerAnglesRaw.x",p=>p.rotation[id].x);Curve(binding,"localEulerAnglesRaw.y",p=>p.rotation[id].y);Curve(binding,"localEulerAnglesRaw.z",p=>p.rotation[id].z);
                }
                Curve(AnimationUtility.CalculateTransformPath(bones[1],root),"m_LocalPosition.y",p=>p.hipsY);
                Curve(AnimationUtility.CalculateTransformPath(bones[1],root),"m_LocalPosition.x",p=>p.hipsX);
                Curve(AnimationUtility.CalculateTransformPath(bones[2],root),"m_LocalPosition.y",p=>p.chestY);
                void Contact(string property,Func<Pose,float> value)
                {
                    var keys=new Keyframe[frames+1];for(int i=0;i<=frames;i++)keys[i]=new Keyframe(i*duration/frames,value(samples[i]),0,0);
                    clip.SetCurve("",typeof(Animator),property,new AnimationCurve(keys));
                }
                Contact("LeftPlant",p=>p.leftPlant);Contact("RightPlant",p=>p.rightPlant);
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
                EditorUtility.SetDirty(clip);clips.Add(name,clip);
            }
            Clip("Idle",3.4f,true,Idle);
            Clip("Walk",PlayerGaitProfile.WalkCycle,true,t=>Gait(t,PlayerGaitProfile.WalkSpeed,PlayerGaitProfile.WalkCycle,PlayerGaitProfile.WalkStance,.045f,1.5f,19));
            Clip("Jog",PlayerGaitProfile.JogCycle,true,t=>Gait(t,PlayerGaitProfile.JogSpeed,PlayerGaitProfile.JogCycle,PlayerGaitProfile.JogStance,.11f,5,28));
            Clip("Run",PlayerGaitProfile.RunCycle,true,t=>Gait(t,PlayerGaitProfile.RunSpeed,PlayerGaitProfile.RunCycle,PlayerGaitProfile.RunStance,.16f,8,38));
            Clip("Jump",.24f,false,t=>Air(t,false));Clip("Fall",.8f,true,t=>Air(t,true));Clip("Land",.24f,false,Land);
            Clip("StartMove",.22f,false,t=>WeightShift(t,false));Clip("StopMove",.26f,false,t=>WeightShift(t,true));
            Clip("TurnLeft",.7f,true,t=>Turn(t,-1));Clip("TurnRight",.7f,true,t=>Turn(t,1));Clip("IdleLook",4,false,Relax);
            Clip("EnterCar",SimpleCar.EnterDuration,false,t=>Board(t,false));Clip("ExitCar",SimpleCar.ExitDuration,false,t=>Board(t,true));Clip("Drive",3,true,Drive);
            string controllerPath=folder+"/StreetwearHero.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.layers=Array.Empty<AnimatorControllerLayer>();controller.parameters=Array.Empty<AnimatorControllerParameter>();
            foreach(var old in AssetDatabase.LoadAllAssetsAtPath(controllerPath))if(old&&old!=controller)UnityEngine.Object.DestroyImmediate(old,true);
            controller.AddLayer("Base");controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter{name="Grounded",type=AnimatorControllerParameterType.Bool,defaultBool=true});
            controller.AddParameter("Turn",AnimatorControllerParameterType.Float);
            controller.AddParameter("GaitPhase",AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter{name="LeftPlant",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            controller.AddParameter(new AnimatorControllerParameter{name="RightPlant",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            foreach(string trigger in new[]{"MoveStart","MoveStop","Relax"})controller.AddParameter(trigger,AnimatorControllerParameterType.Trigger);
            var machine=controller.layers[0].stateMachine;var locomotion=machine.AddState("Locomotion");
            var idle=machine.AddState("Idle");idle.motion=clips["Idle"];machine.defaultState=idle;
            locomotion.timeParameterActive=true;locomotion.timeParameter="GaitPhase";
            var blend=new BlendTree{name="Distance driven Walk Jog Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(blend,controller);
            blend.AddChild(clips["Walk"],1.45f);blend.AddChild(clips["Jog"],3.2f);blend.AddChild(clips["Run"],5.8f);locomotion.motion=blend;
            var jump=machine.AddState("Jump");jump.motion=clips["Jump"];
            var fall=machine.AddState("Fall");fall.motion=clips["Fall"];
            var land=machine.AddState("Land");land.motion=clips["Land"];
            var start=machine.AddState("StartMove");start.motion=clips["StartMove"];
            var stop=machine.AddState("StopMove");stop.motion=clips["StopMove"];
            var relax=machine.AddState("IdleLook");relax.motion=clips["IdleLook"];
            var turning=machine.AddState("Turning");
            var turnBlend=new BlendTree{name="Planted turn steps",blendType=BlendTreeType.Simple1D,blendParameter="Turn",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(turnBlend,controller);
            turnBlend.AddChild(clips["TurnLeft"],-1);turnBlend.AddChild(clips["Idle"],0);turnBlend.AddChild(clips["TurnRight"],1);turning.motion=turnBlend;
            foreach(string state in new[]{"EnterCar","ExitCar","Drive"}){var vehicle=machine.AddState(state);vehicle.motion=clips[state];}
            AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,float duration=.1f)
            {var t=from.AddTransition(to);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=duration;return t;}
            foreach(var state in new[]{idle,locomotion,land,relax,turning})
            {
                var t=Transition(state,jump,.06f);t.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");t.AddCondition(AnimatorConditionMode.Greater,.3f,"VerticalSpeed");
                if(state!=land){t=Transition(state,fall);t.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");t.AddCondition(AnimatorConditionMode.Less,.3f,"VerticalSpeed");}
            }
            foreach(var state in new[]{idle,relax,turning})
            {
                var t=Transition(state,locomotion,.14f);t.AddCondition(AnimatorConditionMode.If,0,"Grounded");t.AddCondition(AnimatorConditionMode.Greater,.08f,"Speed");
            }
            var stopping=Transition(locomotion,idle,.18f);stopping.AddCondition(AnimatorConditionMode.If,0,"Grounded");stopping.AddCondition(AnimatorConditionMode.Less,.035f,"Speed");
            var idleAction=Transition(idle,relax,.2f);idleAction.AddCondition(AnimatorConditionMode.If,0,"Relax");idleAction.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");
            var relaxed=Transition(relax,idle,.2f);relaxed.hasExitTime=true;relaxed.exitTime=.94f;
            foreach(float sign in new[]{-1f,1f})
            {
                var t=Transition(idle,turning,.12f);t.AddCondition(sign<0?AnimatorConditionMode.Less:AnimatorConditionMode.Greater,sign*.25f,"Turn");t.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            }
            var turnEnd=Transition(turning,idle,.12f);turnEnd.AddCondition(AnimatorConditionMode.Greater,-.12f,"Turn");turnEnd.AddCondition(AnimatorConditionMode.Less,.12f,"Turn");
            var turnMove=Transition(turning,locomotion,.08f);turnMove.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            var down=Transition(jump,fall,.1f);down.AddCondition(AnimatorConditionMode.Less,.15f,"VerticalSpeed");
            foreach(var state in new[]{jump,fall}){var t=Transition(state,land,.07f);t.AddCondition(AnimatorConditionMode.If,0,"Grounded");}
            var recover=Transition(land,idle,.12f);recover.hasExitTime=true;recover.exitTime=.94f;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return controller;
        }
    }
}
