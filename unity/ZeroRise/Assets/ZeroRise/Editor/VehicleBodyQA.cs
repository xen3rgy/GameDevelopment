using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    // Check the visible, baked paint meshes with backface culling, rather than
    // the simplified collision box which can hide a hole in the rendered body.
    public static class VehicleBodyQA
    {
        [Serializable] sealed class Report
        {
            public int vehicles, families, sideSamples, endSamples, cabinSamples, uncovered;
            public bool passed;
            public List<string> failures = new List<string>();
        }
        struct Triangle { public Vector3 a,b,c; }
        public static void Run() { EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath); Verify(true); }
        public static void RebuildAndVerify() { WestbruchVehicleArt.RebuildVehicles(); Verify(true); }
        public static void RebuildAndCapture()
        {
            WestbruchVehicleArt.RebuildVehicles();
            if(Verify(false)) VehicleBodyShowcase.Capture(); else EditorApplication.Exit(1);
        }
        static bool Verify(bool exit)
        {
            var report=new Report();var families=new HashSet<int>();
            try
            {
                var vehicles=new List<Transform>();
                foreach(var car in UnityEngine.Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None)) { vehicles.Add(car.transform);families.Add(car.kind); }
                foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>(FindObjectsSortMode.None))vehicles.Add(car.transform);
                var district=GameObject.Find("Westbruch – authored district").transform;
                foreach(Transform child in district)if(child.name.StartsWith("Parked vehicle "))vehicles.Add(child);
                foreach(var car in vehicles)
                {
                    var triangles=new List<Triangle>();
                    foreach(var filter in car.GetComponentsInChildren<MeshFilter>())
                    {
                        var renderer=filter.GetComponent<MeshRenderer>();
                        if(!renderer||!renderer.enabled)continue;
                        string material=renderer.sharedMaterial.name;
                        if(!material.StartsWith("VehiclePaint")&&material!="VehicleClearGlass"&&material!="VehicleRubberTrim")continue;
                        var matrix=car.worldToLocalMatrix*filter.transform.localToWorldMatrix;var vertices=filter.sharedMesh.vertices;var indices=filter.sharedMesh.triangles;
                        for(int i=0;i<indices.Length;i+=3)triangles.Add(new Triangle{a=matrix.MultiplyPoint3x4(vertices[indices[i]]),b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]),c=matrix.MultiplyPoint3x4(vertices[indices[i+2]])});
                    }
                    var traffic=car.GetComponent<TrafficVehicle>();int kind=traffic?traffic.kind:car.name.EndsWith("3")?3:car.name.EndsWith("2")?2:car.name.EndsWith("1")?1:0;
                    float length=kind==3?4.65f:kind==2?4.35f:kind==1?4.2f:3.8f,width=kind==3?1.90f:1.72f;
                    float height=car.GetComponent<SimpleCar>()?1.80f:kind==3?1.90f:kind==0?1.65f:1.59f;
                    float screenFront=kind==3?1.15f:.72f,roofFront=kind==3?.81f:.25f;
                    float screenBack=kind==1?-1.43f:kind==3?-2.08f:kind==2?-1.92f:-1.50f;
                    float roofBack=kind==2?-1.40f:kind==3?-1.98f:kind==1?-.90f:-1.12f;
                    for(int sample=0;sample<100;sample++)
                    {
                        float z=Mathf.Lerp(-length*.5f+.025f,length*.5f-.025f,(sample+.37f)/100);
                        // Narrow, intentional door shut lines are not body holes.
                        if(Mathf.Abs(z+.64f)<.025f||Mathf.Abs(z-.60f)<.025f)continue;
                        foreach(float y in new[]{.79f,.88f,.935f,.98f,1.005f})foreach(int side in new[]{-1,1})
                        {
                            report.sideSamples++;
                            var origin=new Vector3(side*(width*.5f+1),y,z);var direction=Vector3.left*side;
                            if(!Covered(triangles,origin,direction,p=>side*p.x>width*.36f))Failure(report,car.name+" side "+side+" at y="+y.ToString("F3")+", z="+z.ToString("F3"));
                        }
                    }
                    foreach(int end in new[]{-1,1})for(int sample=0;sample<17;sample++)foreach(float y in new[]{.80f,.94f,.985f})
                    {
                        float x=Mathf.Lerp(-width*.38f,width*.38f,(sample+.3f)/17);report.endSamples++;
                        if(!Covered(triangles,new Vector3(x,y,end*(length*.5f+1)),Vector3.back*end,p=>end*p.z>length*.5f-.03f))Failure(report,car.name+" end "+end+" at x="+x.ToString("F3")+", y="+y);
                    }
                    for(int row=0;row<5;row++)
                    {
                        float y=Mathf.Lerp(1.10f,height-.12f,(row+.3f)/5),blend=Mathf.InverseLerp(1.04f,height-.045f,y);
                        float front=Mathf.Lerp(screenFront,roofFront,blend),rear=Mathf.Lerp(screenBack,roofBack,blend);
                        for(int sample=0;sample<40;sample++)foreach(int side in new[]{-1,1})
                        {
                            float z=Mathf.Lerp(rear+.04f,front-.04f,(sample+.37f)/40);report.cabinSamples++;
                            if(!Covered(triangles,new Vector3(side*(width*.5f+1),y,z),Vector3.left*side,p=>side*p.x>width*.36f))Failure(report,car.name+" cabin "+side+" at y="+y.ToString("F3")+", z="+z.ToString("F3"));
                        }
                    }
                    report.vehicles++;
                }
                report.families=families.Count;report.passed=report.vehicles>=17&&report.families==4&&report.uncovered==0;
            }
            catch(Exception error){report.failures.Add(error.ToString());Debug.LogException(error);}
            Directory.CreateDirectory("QA");File.WriteAllText("QA/vehicle-body-report.json",JsonUtility.ToJson(report,true));
            Debug.Log("Visible vehicle body QA: "+JsonUtility.ToJson(report));if(exit)EditorApplication.Exit(report.passed?0:1);return report.passed;
        }
        static void Failure(Report report,string message){report.uncovered++;if(report.failures.Count<16)report.failures.Add(message);}
        static bool Covered(List<Triangle> triangles,Vector3 origin,Vector3 direction,Func<Vector3,bool> nearSide)
        {
            foreach(var triangle in triangles)
            {
                var edge1=triangle.b-triangle.a;var edge2=triangle.c-triangle.a;var cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);
                if(determinant<.00000001f)continue; // Exactly the renderer's outward-facing triangles.
                var offset=origin-triangle.a;float u=Vector3.Dot(offset,cross)/determinant;if(u<0||u>1)continue;
                var q=Vector3.Cross(offset,edge1);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)continue;
                float distance=Vector3.Dot(edge2,q)/determinant;if(distance>0&&nearSide(origin+direction*distance))return true;
            }
            return false;
        }
    }
}
