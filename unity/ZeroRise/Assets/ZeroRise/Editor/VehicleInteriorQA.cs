using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class VehicleInteriorQA
    {
        [Serializable] sealed class Report { public int vehicles,sideSamples,floorSamples,uncovered;public bool passed;public List<string> failures=new List<string>(); }
        struct Triangle { public Vector3 a,b,c; }
        public static void Run(){EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Verify();}
        public static void RebuildAndVerify(){WestbruchVehicleArt.RebuildVehicles();Verify();}
        static void Verify()
        {
            var report=new Report();var cars=new List<Transform>();foreach(var car in UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None))cars.Add(car.transform);foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>(FindObjectsSortMode.None))cars.Add(car.transform);
            foreach(Transform child in GameObject.Find("Westbruch – authored district").transform)if(child.name.StartsWith("Parked vehicle "))cars.Add(child);
            foreach(var car in cars)
            {
                var triangles=new List<Triangle>();foreach(var filter in car.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;string mat=renderer.sharedMaterial.name;
                    if(!mat.StartsWith("Vehicle")||mat.Contains("Glass")||mat.Contains("SeatFabric")||mat.Contains("Lamp"))continue;
                    var matrix=car.worldToLocalMatrix*filter.transform.localToWorldMatrix;var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
                    for(int i=0;i<indices.Length;i+=3)triangles.Add(new Triangle{a=matrix.MultiplyPoint3x4(vertices[indices[i]]),b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]),c=matrix.MultiplyPoint3x4(vertices[indices[i+2]])});
                }
                var agent=car.GetComponent<TrafficVehicle>();int kind=agent?agent.kind:car.name.EndsWith("3")?3:car.name.EndsWith("2")?2:car.name.EndsWith("1")?1:0;
                float length=kind==3?4.65f:kind==2?4.35f:kind==1?4.2f:3.8f,width=kind==3?1.90f:1.72f;
                for(int row=0;row<45;row++)
                {
                    float z=Mathf.Lerp(-length*.5f+.08f,length*.5f-.08f,(row+.37f)/45);
                    foreach(float y in new[]{.68f,.82f,.96f})foreach(int side in new[]{-1,1})
                    {
                        report.sideSamples++;if(!Covered(triangles,new Vector3(0,y,z),Vector3.right*side,p=>p.x*side>width*.32f))Fail(report,car.name+" inward side "+side+" at "+y+" / "+z);
                    }
                    foreach(float x in new[]{-.48f,0,.48f})
                    {report.floorSamples++;if(!Covered(triangles,new Vector3(x,.65f,z),Vector3.down,p=>p.y>.40f&&p.y<.64f))Fail(report,car.name+" floor at "+x+" / "+z);}
                }
                report.vehicles++;
            }
            report.passed=report.vehicles==17&&report.uncovered==0;Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-interior-report.json",JsonUtility.ToJson(report,true));Debug.Log("Vehicle interior coverage: "+JsonUtility.ToJson(report));EditorApplication.Exit(report.passed?0:1);
        }
        static void Fail(Report report,string text){report.uncovered++;if(report.failures.Count<20)report.failures.Add(text);}
        static bool Covered(List<Triangle> triangles,Vector3 origin,Vector3 direction,Func<Vector3,bool> shell)
        {
            foreach(var t in triangles)
            {
                var e1=t.b-t.a;var e2=t.c-t.a;var cross=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,cross);if(determinant<.00000001f)continue;
                var offset=origin-t.a;float u=Vector3.Dot(offset,cross)/determinant;if(u<0||u>1)continue;var q=Vector3.Cross(offset,e1);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)continue;
                float distance=Vector3.Dot(e2,q)/determinant;if(distance>0&&distance<2&&shell(origin+direction*distance))return true;
            }return false;
        }
    }
}
