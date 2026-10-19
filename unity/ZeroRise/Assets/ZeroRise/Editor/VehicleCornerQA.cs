using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    [InitializeOnLoad] public static class VehicleCornerQA
    {
        const string Flag="ZeroRise.VehicleCornerQA";
        static VehicleCornerQA(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Temporary vehicle corner escape regression").AddComponent<VehicleCornerTest>();}};}
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class VehicleCornerTest:MonoBehaviour
    {
        [Serializable] sealed class Report { public int cases,failedEscapes,airObstructions;public float minimumEscape=100,maximumResidualPenetration;public bool passed;public List<string> failures=new List<string>(); }
        readonly Report report=new Report();ThirdPersonMotor player;CharacterController controller;readonly Collider[] contacts=new Collider[48];
        IEnumerator Start()
        {
            player=FindAnyObjectByType<ThirdPersonMotor>();controller=player.GetComponent<CharacterController>();player.enabled=false;player.orbit.enabled=false;Camera.main.enabled=false;
            Time.captureDeltaTime=1f/60;
            var world=FindAnyObjectByType<PedestrianWorld>();foreach(var person in world.people)person.gameObject.SetActive(false);
            var traffic=FindAnyObjectByType<WestbruchTraffic>();traffic.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,24.75f,0);floor.transform.localScale=new Vector3(100,.5f,100);
            var variants=new List<Transform>();var kinds=new HashSet<int>();foreach(var car in traffic.vehicles)if(kinds.Add(car.kind))variants.Add(car.transform);
            var own=FindAnyObjectByType<SimpleCar>();own.enabled=false;own.GetComponent<Rigidbody>().isKinematic=true;variants.Add(own.transform);
            for(int i=0;i<traffic.vehicles.Length;i++)traffic.vehicles[i].transform.position=new Vector3(i*10,50,0);
            for(int family=0;family<variants.Count;family++)
            {
                var car=variants[family];car.SetPositionAndRotation(new Vector3(family*12-24,25.15f,0),Quaternion.Euler(0,family*17,0));Physics.SyncTransforms();
                var agent=car.GetComponent<TrafficVehicle>();float length=agent?agent.length-.12f:3.8f;
                foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})foreach(float intrusion in new[]{-.008f,.025f})
                {
                    float width=agent?agent.width:1.72f;
                    var outward=car.TransformDirection(new Vector3(side,0,end).normalized);var corner=car.TransformPoint(new Vector3(side*width*.5f,.82f,end*length*.5f));var ray=new Ray(corner+outward*2,-outward);
                    if(!Physics.Raycast(ray,out var hit,5,1<<8,QueryTriggerInteraction.Ignore)&&agent){report.failures.Add("Missing actual car collision");continue;}
                    if(!agent)
                    {
                        if(!Physics.Raycast(ray,out hit,5,1,QueryTriggerInteraction.Ignore)){report.failures.Add("Missing player car collision");continue;}
                    }
                    outward=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
                    var at=hit.point+outward*(controller.radius-intrusion);at.y=25.08f;Place(at,outward);Vector3 before=player.transform.position;
                    for(int frame=0;frame<90;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    float travel=Vector3.Dot(player.transform.position-before,outward);report.minimumEscape=Mathf.Min(report.minimumEscape,travel);report.cases++;
                    float penetration=Penetration(car);report.maximumResidualPenetration=Mathf.Max(report.maximumResidualPenetration,penetration);
                    if(travel<1||penetration>.02f){report.failedEscapes++;report.failures.Add(car.name+" corner "+side+"/"+end+" intrusion "+intrusion+" travel "+travel+" penetration "+penetration);}
                }
                foreach(int end in new[]{-1,1})
                {
                    var at=car.TransformPoint(new Vector3(0,1.28f,end*(length*.5f-.11f)));
                    int count=Physics.OverlapSphereNonAlloc(at,.035f,contacts,1|(1<<8),QueryTriggerInteraction.Ignore);
                    for(int i=0;i<count;i++)if(contacts[i].transform.IsChildOf(car)){report.airObstructions++;break;}
                }
                // Establish real contact, then reverse and move along either
                // side. This catches zero-distance casts after normal walking.
                foreach(int side in new[]{-1,1})
                {
                    var outward=car.right*side;Place(car.TransformPoint(new Vector3(side*1.7f,.0f,0))+Vector3.up*.08f,-outward);
                    for(int frame=0;frame<60;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    var before=player.transform.position;player.orbit.transform.rotation=Quaternion.LookRotation(outward);player.visual.rotation=Quaternion.LookRotation(outward);
                    for(int frame=0;frame<75;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    float travel=Vector3.Dot(player.transform.position-before,outward);report.cases++;report.minimumEscape=Mathf.Min(report.minimumEscape,travel);if(travel<1){report.failedEscapes++;report.failures.Add(car.name+" reversal "+side+" travel "+travel);}
                }
                foreach(int side in new[]{-1,1})
                {
                    var outward=car.right*side;Place(car.TransformPoint(new Vector3(side*1.7f,0,0))+Vector3.up*.08f,-outward);
                    for(int frame=0;frame<60;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    var before=player.transform.position;player.orbit.transform.rotation=Quaternion.LookRotation(car.forward);player.visual.rotation=Quaternion.LookRotation(car.forward);
                    for(int frame=0;frame<75;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    float travel=Vector3.Dot(player.transform.position-before,car.forward);report.cases++;report.minimumEscape=Mathf.Min(report.minimumEscape,travel);if(travel<1){report.failedEscapes++;report.failures.Add(car.name+" side slide "+side+" travel "+travel);}
                }
                foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
                {
                    var outward=car.TransformDirection(new Vector3(side,0,end).normalized);float width=agent?agent.width:1.72f;
                    var ray=new Ray(car.TransformPoint(new Vector3(side*width*.5f,.82f,end*length*.5f))+outward*2,-outward);
                    if(!Physics.Raycast(ray,out var hit,5,agent?1<<8:1,QueryTriggerInteraction.Ignore)){report.failures.Add("Missing moving contact surface");continue;}
                    outward=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;var point=hit.point+outward*(controller.radius+.04f);point.y=25.08f;Place(point,outward);
                    var body=car.GetComponent<Rigidbody>();for(int frame=0;frame<12;frame++){body.MovePosition(body.position+outward*.012f);yield return new WaitForFixedUpdate();}
                    var before=player.transform.position;for(int frame=0;frame<90;frame++){player.MoveStep(Vector2.up,true,false,1f/60);yield return null;}
                    float travel=Vector3.Dot(player.transform.position-before,outward);report.cases++;report.minimumEscape=Mathf.Min(report.minimumEscape,travel);float penetration=Penetration(car);report.maximumResidualPenetration=Mathf.Max(report.maximumResidualPenetration,penetration);
                    if(travel<1||penetration>.02f){report.failedEscapes++;report.failures.Add(car.name+" moving corner "+side+"/"+end+" travel "+travel+" penetration "+penetration);}
                }
            }
            report.passed=report.cases==80&&report.failedEscapes==0&&report.airObstructions==0&&report.failures.Count==0;
            Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-corner-report.json",JsonUtility.ToJson(report,true));Debug.Log("Vehicle corner regression: "+JsonUtility.ToJson(report));Time.captureDeltaTime=0;EditorApplication.Exit(report.passed?0:1);
        }
        void Place(Vector3 at,Vector3 heading){controller.enabled=false;player.transform.SetPositionAndRotation(at,Quaternion.identity);player.SetDriving(false);player.visual.rotation=Quaternion.LookRotation(heading);player.orbit.transform.rotation=Quaternion.LookRotation(heading);Physics.SyncTransforms();}
        float Penetration(Transform car)
        {
            float depth=0;foreach(var collider in car.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger&&!(collider is WheelCollider)&&Physics.ComputePenetration(controller,player.transform.position,player.transform.rotation,collider,collider.transform.position,collider.transform.rotation,out _,out float overlap))depth=Mathf.Max(depth,overlap);return depth;
        }
    }
}
