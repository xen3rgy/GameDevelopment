using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class WestbruchPedestrianQA
    {
        const string Flag="ZeroRise.PedestrianQA";
        static WestbruchPedestrianQA(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary resident verification").AddComponent<PedestrianPlayTest>();}};}
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);ValidateAndPlay();}
        public static void ValidateAndPlay()
        {
            try
            {
                var world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();world.LoadNavigation();
                if(world.people.Length!=18||world.places.Length<25||world.crossings.Length<10)throw new Exception("Insufficient residents, activities or connected road crossings");
                int connected=0;var path=new NavMeshPath();
                foreach(var place in world.places)
                {
                    if(!NavMesh.SamplePosition(place.approach,out var floor,.2f,NavMesh.AllAreas))throw new Exception("Unsupported activity: "+place.label);
                    if(NavMesh.CalculatePath(world.places[0].approach,floor.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)connected++;
                }
                if(connected<world.places.Length*.85f)throw new Exception("Disconnected activity destinations: "+connected+" / "+world.places.Length);
                Debug.Log("Resident navigation passed: "+connected+" / "+world.places.Length+" destinations connected, "+world.crossings.Length+" designated crossings.");
                SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
    public sealed class PedestrianPlayTest:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public int residents,activities,crossings,seatFrames,phoneFrames,talkFrames,waitFrames,overlaps,offNavigation,roadViolations,trafficOverlaps,replans,playerPenetrations,carOverlaps;
            public float minimumTravel=10000,minimumCarTravel=10000,maximumFrameStep,maximumExcessStep,playerDetourTravel;public bool playerObstacleTest,passed;
        }
        PedestrianWorld world;WestbruchTraffic traffic;Report report=new Report();Vector3[] previous;float started,lastDelta;int frames;WestbruchLayout plan;
        ThirdPersonMotor player;Vector3 playerOrigin;PedestrianAgent obstacleSubject;float obstacleTravel,obstacleAt;bool obstacleRemoved;
        void Start()
        {
            world=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();traffic=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();previous=new Vector3[world.people.Length];
            for(int i=0;i<previous.Length;i++)previous[i]=world.people[i].transform.position;
            player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();player.enabled=false;player.orbit.enabled=false;playerOrigin=player.transform.position;started=Time.time;Time.timeScale=3;
            plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
            Camera.main.enabled=false;
        }
        void Update()
        {
            try
            {
                frames++;float elapsed=Time.time-started;
                for(int i=0;i<world.people.Length;i++)
                {
                    var person=world.people[i];var at=person.transform.position;
                    float step=Vector3.Distance(at,previous[i]);if(frames>3){float excess=Mathf.Max(0,step-2*Mathf.Max(Time.deltaTime,lastDelta));if(excess>.2f&&excess>report.maximumExcessStep)Debug.LogWarning("Resident movement discontinuity: "+person.name+" "+person.state+" from "+previous[i]+" to "+at+" excess "+excess);report.maximumFrameStep=Mathf.Max(report.maximumFrameStep,step);report.maximumExcessStep=Mathf.Max(report.maximumExcessStep,excess);}previous[i]=at;
                    if(!person.agent.isOnNavMesh)report.offNavigation++;
                    if(person.state!=PedestrianAgent.Behaviour.Crossing)foreach(var road in plan.roads)if(Mathf.Abs(at.x-road.x)<road.w*.5f-.2f&&Mathf.Abs(at.z-road.z)<road.d*.5f-.2f){report.roadViolations++;break;}
                    if(obstacleSubject&&!obstacleRemoved){var difference=at-player.transform.position;difference.y=0;if(difference.magnitude<.5f)report.playerPenetrations++;}
                    if(person.state==PedestrianAgent.Behaviour.Activity)
                    {
                        var activity=world.places[person.destination].activity;
                        if(activity==PedestrianWorld.Activity.Sit)report.seatFrames++;
                        if(activity==PedestrianWorld.Activity.Phone)report.phoneFrames++;
                        if(activity==PedestrianWorld.Activity.Talk)report.talkFrames++;
                        if(activity==PedestrianWorld.Activity.Wait)report.waitFrames++;
                    }
                    if(frames%6!=0)continue;
                    for(int j=i+1;j<world.people.Length;j++)
                    {
                        var other=world.people[j];if((at-other.transform.position).sqrMagnitude>4)continue;
                        if(Physics.ComputePenetration(person.capsule,at,person.transform.rotation,other.capsule,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.06f){if(report.overlaps<5)Debug.LogWarning("Resident overlap: "+person.name+" "+person.state+" / "+other.name+" "+other.state+" depth "+depth+" at "+at);report.overlaps++;}
                    }
                    foreach(var car in traffic.vehicles)
                        if((at-car.transform.position).sqrMagnitude<25&&Physics.ComputePenetration(person.capsule,at,person.transform.rotation,car.bodyCollider,car.transform.position,car.transform.rotation,out _,out float depth)&&depth>.03f)report.trafficOverlaps++;
                }
                lastDelta=Time.deltaTime;
                if(frames%6==0)for(int i=0;i<traffic.vehicles.Length;i++)for(int j=i+1;j<traffic.vehicles.Length;j++){var a=traffic.vehicles[i];var b=traffic.vehicles[j];if((a.transform.position-b.transform.position).sqrMagnitude<40&&Physics.ComputePenetration(a.bodyCollider,a.transform.position,a.transform.rotation,b.bodyCollider,b.transform.position,b.transform.rotation,out _,out float depth)&&depth>.02f)report.carOverlaps++;}
                if(!obstacleSubject&&elapsed>90&&elapsed<170)TryPlayerObstacle();
                if(obstacleSubject&&!obstacleRemoved&&Time.time-obstacleAt>15){report.playerDetourTravel=obstacleSubject.totalDistance-obstacleTravel;report.playerObstacleTest=report.playerDetourTravel>2&&report.playerPenetrations==0;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=playerOrigin;cc.enabled=true;Physics.SyncTransforms();obstacleRemoved=true;}
                if(elapsed<300)return;
                report.residents=world.people.Length;report.activities=world.completedActivities;report.crossings=world.crossedRoads;report.replans=world.replans;
                foreach(var person in world.people)report.minimumTravel=Mathf.Min(report.minimumTravel,person.totalDistance);
                foreach(var person in world.people)if(person.totalDistance<20)
                {
                    Debug.LogWarning("RESIDENT ROUTE DIAGNOSTIC: "+person.name+" distance="+person.totalDistance+" state="+person.state+" held="+person.StateTime+" at="+person.transform.position+" destination="+person.destination+" place="+(person.destination>=0?world.places[person.destination].label:"none")+" recoveries="+person.recoveries+" desired="+person.agent.desiredVelocity+" remaining="+person.agent.remainingDistance+" stopped="+person.agent.isStopped);
                    var direction=person.agent.desiredVelocity.normalized;foreach(var hit in Physics.CapsuleCastAll(person.transform.position+Vector3.up*.45f,person.transform.position+Vector3.up*1.4f,.24f,direction,.55f,1|(1<<2)|(1<<8)|(1<<9)))if(!hit.transform.IsChildOf(person.transform))Debug.LogWarning("RESIDENT OBSTACLE: "+hit.collider.name+" root="+hit.transform.root.name+" at="+hit.point);
                    int crossing=world.FindCrossing(person.transform.position);if(crossing>=0)Debug.LogWarning("RESIDENT CROSSING: "+crossing+" a="+world.crossings[crossing].a+" b="+world.crossings[crossing].b+" owner="+world.crossings[crossing].owner);
                }
                foreach(var car in traffic.vehicles)report.minimumCarTravel=Mathf.Min(report.minimumCarTravel,car.travelled);
                report.passed=report.activities>=15&&report.crossings>=8&&report.seatFrames>0&&report.phoneFrames>0&&report.waitFrames>0&&report.minimumTravel>20&&report.minimumCarTravel>100&&report.overlaps==0&&report.trafficOverlaps==0&&report.carOverlaps==0&&report.offNavigation==0&&report.roadViolations==0&&report.maximumExcessStep<.2f&&report.playerObstacleTest;
                Directory.CreateDirectory("QA");File.WriteAllText("QA/pedestrian-report.json",JsonUtility.ToJson(report,true));
                Debug.Log("Resident play verification: "+JsonUtility.ToJson(report));Time.timeScale=1;EditorApplication.Exit(report.passed?0:1);enabled=false;
            }
            catch(Exception error){Debug.LogException(error);Time.timeScale=1;EditorApplication.Exit(1);enabled=false;}
        }
        void TryPlayerObstacle()
        {
            foreach(var person in world.people)
            {
                if(person.state!=PedestrianAgent.Behaviour.Walking||person.agent.desiredVelocity.magnitude<.7f||person.agent.remainingDistance<12)continue;
                var at=person.transform.position+person.agent.desiredVelocity.normalized*4;
                if(NavMesh.Raycast(person.transform.position,at,out _,NavMesh.AllAreas)||!NavMesh.SamplePosition(at,out var floor,.3f,NavMesh.AllAreas))continue;
                bool clear=true;foreach(var other in world.people)if(Vector3.Distance(other.transform.position,floor.position)<2)clear=false;if(!clear)continue;
                var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=floor.position;cc.enabled=true;Physics.SyncTransforms();obstacleSubject=person;obstacleAt=Time.time;obstacleTravel=person.totalDistance;return;
            }
        }
    }
}
