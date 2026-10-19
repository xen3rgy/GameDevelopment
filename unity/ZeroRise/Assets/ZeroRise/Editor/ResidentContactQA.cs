using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class ResidentContactQA
    {
        const string Flag="ZeroRise.ResidentContactQA";
        static ResidentContactQA()
        {EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary continuous resident contact verification").AddComponent<ResidentContactPlayTest>();}};}
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    [DefaultExecutionOrder(100)] public sealed class ResidentContactPlayTest:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public float standingDisplacement,seatedDisplacement,maximumTransitionExcess,maximumWalkingExcess,walkingProgress,carDisplacement,standingContact=100,seatedContact=100;
            public int penetrationFrames,offNavigationFrames;
            public bool standingTest,seatedTest,blockedExitWaited,exitResumed,walkingTest,trafficTest,passed;
            public bool observedLeaving;public float blockerDisplacement,blockedExitGap,leavingDuration;
            public Vector3 exitOrigin,exitActorPosition,exitBlockerPosition;
        }
        ThirdPersonMotor player;CharacterController controller;PedestrianWorld world;WestbruchTraffic traffic;Report report=new Report();
        readonly Collider[] contacts=new Collider[64];
        IEnumerator Start()
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();controller=player.GetComponent<CharacterController>();player.enabled=false;player.orbit.enabled=false;
            world=FindAnyObjectByType<PedestrianWorld>();traffic=FindAnyObjectByType<WestbruchTraffic>();traffic.enabled=false;
            Camera.main.enabled=false;Time.timeScale=2;
            yield return new WaitForSeconds(2.2f);
            yield return Standing();yield return SittingAndBlockedExit();yield return Walking();yield return Traffic();
            report.passed=report.standingTest&&report.seatedTest&&report.blockedExitWaited&&report.exitResumed&&report.walkingTest&&report.trafficTest&&report.penetrationFrames==0&&report.offNavigationFrames==0&&report.maximumTransitionExcess<.12f&&report.maximumWalkingExcess<.12f;
            Directory.CreateDirectory("QA");File.WriteAllText("QA/resident-contact-report.json",JsonUtility.ToJson(report,true));Debug.Log("Continuous resident contact: "+JsonUtility.ToJson(report));Time.timeScale=1;EditorApplication.Exit(report.passed?0:1);
        }
        IEnumerator Standing()
        {
            PedestrianAgent target=null;foreach(var person in world.people)if(person.state==PedestrianAgent.Behaviour.Activity&&world.places[person.destination].activity==PedestrianWorld.Activity.Phone){target=person;break;}
            if(!target||!Approach(target))yield break;
            Vector3 anchor=target.transform.position;float until=Time.time+4;
            while(Time.time<until)
            {Pursue(target.transform.position);report.standingDisplacement=Mathf.Max(report.standingDisplacement,Horizontal(target.transform.position-anchor));report.standingContact=Mathf.Min(report.standingContact,Horizontal(target.transform.position-player.transform.position));Check(target);yield return null;}
            report.standingTest=report.standingDisplacement<.025f&&report.standingContact<.85f&&target.state==PedestrianAgent.Behaviour.Activity;
            ParkPlayer();
        }
        IEnumerator SittingAndBlockedExit()
        {
            PedestrianAgent target=null;foreach(var person in world.people)if(person.state==PedestrianAgent.Behaviour.Activity&&world.places[person.destination].activity==PedestrianWorld.Activity.Sit){target=person;break;}
            if(!target||!Approach(target))yield break;
            Vector3 seat=target.transform.position;float until=Time.time+4;
            while(Time.time<until)
            {Pursue(target.transform.position);report.seatedDisplacement=Mathf.Max(report.seatedDisplacement,Horizontal(target.transform.position-seat));report.seatedContact=Mathf.Min(report.seatedContact,Horizontal(target.transform.position-player.transform.position));Check(target);yield return null;}
            report.seatedTest=report.seatedDisplacement<.025f&&report.seatedContact<.85f;
            var approach=world.places[target.destination].approach;Teleport(approach);Vector3 blockerOrigin=player.transform.position,previous=target.transform.position;float lastDt=Time.deltaTime;
            until=Time.time+42;float blockedAt=-1;
            while(Time.time<until)
            {
                player.MoveStep(Vector2.zero,false,false,Time.deltaTime);Check(target);
                report.blockerDisplacement=Mathf.Max(report.blockerDisplacement,Horizontal(player.transform.position-blockerOrigin));
                float excess=Mathf.Max(0,Vector3.Distance(previous,target.transform.position)-target.agent.speed*Mathf.Max(Time.deltaTime,lastDt)-.025f);report.maximumTransitionExcess=Mathf.Max(report.maximumTransitionExcess,excess);previous=target.transform.position;lastDt=Time.deltaTime;
                if(target.state==PedestrianAgent.Behaviour.Leaving)
                {report.observedLeaving=true;if(blockedAt<0)blockedAt=Time.time;report.leavingDuration=Time.time-blockedAt;if(Time.time-blockedAt>3){report.blockedExitWaited=Horizontal(target.transform.position-approach)>.4f&&report.blockerDisplacement<.1f;break;}}
                else if(blockedAt>=0)break;
                yield return null;
            }
            report.exitOrigin=approach;report.exitActorPosition=target.transform.position;report.exitBlockerPosition=player.transform.position;report.blockedExitGap=Horizontal(target.transform.position-approach);
            ParkPlayer();until=Time.time+7;
            while(Time.time<until)
            {if(target.state==PedestrianAgent.Behaviour.Walking){report.exitResumed=true;break;}yield return null;}
        }
        IEnumerator Walking()
        {
            PedestrianAgent target=null;float until=Time.time+12;
            while(Time.time<until&&!target)
            {foreach(var person in world.people)if(person.state==PedestrianAgent.Behaviour.Walking&&person.agent.desiredVelocity.magnitude>.6f&&person.agent.remainingDistance>15&&Approach(person)){target=person;break;}if(!target)yield return null;}
            if(!target)yield break;
            var previous=target.transform.position;float lastDt=Time.deltaTime,travel=target.totalDistance;until=Time.time+7;
            while(Time.time<until)
            {
                Pursue(target.transform.position);Check(target);float excess=Mathf.Max(0,Vector3.Distance(previous,target.transform.position)-target.agent.speed*Mathf.Max(Time.deltaTime,lastDt)-.025f);report.maximumWalkingExcess=Mathf.Max(report.maximumWalkingExcess,excess);previous=target.transform.position;lastDt=Time.deltaTime;yield return null;
            }
            ParkPlayer();yield return new WaitForSeconds(7);report.walkingProgress=target.totalDistance-travel;report.walkingTest=target.agent.isOnNavMesh&&report.walkingProgress>3;
        }
        IEnumerator Traffic()
        {
            var car=traffic.vehicles[0];Vector3 anchor=car.transform.position;
            if(!Approach(car.transform,car.bodyCollider))yield break;
            float until=Time.time+4;
            while(Time.time<until)
            {Pursue(car.transform.position);report.carDisplacement=Mathf.Max(report.carDisplacement,Vector3.Distance(anchor,car.transform.position));if(Physics.ComputePenetration(controller,player.transform.position,player.transform.rotation,car.bodyCollider,car.transform.position,car.transform.rotation,out _,out float depth)&&depth>.025f)report.penetrationFrames++;yield return null;}
            report.trafficTest=car.GetComponent<Rigidbody>().isKinematic&&report.carDisplacement<.01f;
        }
        bool Approach(PedestrianAgent person)=>Approach(person.transform,person.capsule);
        bool Approach(Transform target,Collider body)
        {
            float radius=body is CapsuleCollider?1.2f:3.5f;
            for(int i=0;i<16;i++)
            {
                var offset=target.rotation*Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*radius;var point=target.position+offset;
                if(body is CapsuleCollider){if(!NavMesh.SamplePosition(point,out var floor,.25f,NavMesh.AllAreas))continue;point=floor.position;}
                else if(Physics.Raycast(point+Vector3.up*2,Vector3.down,out var hit,4,1,QueryTriggerInteraction.Ignore))point.y=hit.point.y;
                int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*.35f,point+Vector3.up*1.45f,.33f,contacts,1|(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);bool clear=true;
                for(int c=0;c<count;c++)if(!contacts[c].transform.IsChildOf(target)){clear=false;break;}
                if(!clear)continue;Teleport(point);return true;
            }
            return false;
        }
        void Teleport(Vector3 position){controller.enabled=false;player.transform.position=position;player.SetDriving(false);Physics.SyncTransforms();}
        void ParkPlayer()=>Teleport(new Vector3(-102,.2f,-102));
        void Pursue(Vector3 target)
        {
            var heading=target-player.transform.position;heading.y=0;if(heading.sqrMagnitude>.001f){player.orbit.transform.rotation=Quaternion.LookRotation(heading);player.visual.rotation=Quaternion.LookRotation(heading);}player.MoveStep(Vector2.up,true,false,Time.deltaTime);
        }
        void Check(PedestrianAgent target)
        {if(!target.agent.isOnNavMesh)report.offNavigationFrames++;if(Physics.ComputePenetration(controller,player.transform.position,player.transform.rotation,target.capsule,target.transform.position,target.transform.rotation,out _,out float depth)&&depth>.025f)report.penetrationFrames++;}
        static float Horizontal(Vector3 difference){difference.y=0;return difference.magnitude;}
    }
}
