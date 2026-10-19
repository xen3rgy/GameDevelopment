using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZeroRise.Editor
{
    public static class WestbruchTrafficQA
    {
        [Serializable] sealed class Report
        {
            public int vehicles,types,lanes,turnPaths,simulationSteps,leftTurns,rightTurns,straightMoves;
            public int intersections,overlaps,roadDepartures;
            public float minimumTravel=100000,maximumWait,obstacleSpeed,resumeDistance,minimumTurnRadius=1000;
            public bool obstacleStop;
            public List<string> failures=new List<string>();
        }
        public static void RebuildAndCapture(){WestbruchBuilder.Build();Run();}
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
                var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));
                var world=UnityEngine.Object.FindAnyObjectByType<WestbruchTraffic>();if(!world)throw new Exception("Traffic world missing");
                var report=new Report{vehicles=world.vehicles.Length,lanes=world.links.Length*2,intersections=world.nodes.Length};var types=new HashSet<int>();
                foreach(var car in world.vehicles)
                {
                    types.Add(car.kind);
                    if(!car.transform.Find("Visible seated NPC driver"))report.failures.Add("Invisible driver");
                    if(car.GetComponentsInChildren<Light>().Length>0||car.GetComponent<SimpleCar>())report.failures.Add("NPC uses dynamic lights or player input");
                }
                report.types=types.Count;
                foreach(var edge in world.links)foreach(bool reverse in new[]{false,true})
                {
                    int from=reverse?edge.b:edge.a,to=reverse?edge.a:edge.b;
                    foreach(var next in world.links)
                    {
                        int dest=next.a==to?next.b:next.b==to?next.a:-1;if(dest<0||dest==from)continue;
                        var path=world.TurnPath(from,to,dest);report.turnPaths++;
                        for(int s=1;s<path.Length-1;s++)
                        {
                            var heading=(path[s+1]-path[s-1]).normalized;var right=Vector3.Cross(Vector3.up,heading);
                            float angle=Vector3.Angle(path[s]-path[s-1],path[s+1]-path[s])*Mathf.Deg2Rad;
                            if(angle>.0005f)report.minimumTurnRadius=Mathf.Min(report.minimumTurnRadius,Vector3.Distance(path[s-1],path[s+1])*.5f/angle);
                            foreach(int x in new[]{-1,0,1})foreach(int z in new[]{-1,0,1})
                            {
                                var p=path[s]+right*(x*.98f)+heading*(z*2.40f);
                                if(!OnRoad(plan,p,.015f))report.roadDepartures++;
                            }
                        }
                    }
                }
                for(int run=0;run<2;run++)
                {
                    world.Initialize(17301+run*71);float dt=run==0?.05f:.0333333f;
                    int steps=Mathf.RoundToInt(360/dt);
                    for(int step=0;step<steps;step++)
                    {
                        world.Step(dt,true);report.simulationSteps++;
                        if(step%4==0)CheckVehicles(world,plan,report);
                    }
                    foreach(var car in world.vehicles)
                    {
                        report.minimumTravel=Mathf.Min(report.minimumTravel,car.travelled);report.maximumWait=Mathf.Max(report.maximumWait,car.maxWait);
                        report.leftTurns+=car.leftTurns;report.rightTurns+=car.rightTurns;report.straightMoves+=car.straightMoves;
                    }
                }
                world.Initialize();var subject=world.vehicles[0];world.Evaluate(subject,subject.progress+14,out var obstacle,out _);
                var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="Temporary traffic obstacle";barrier.transform.position=obstacle+Vector3.up*.8f;barrier.transform.localScale=new Vector3(.75f,1.6f,2.0f);Physics.SyncTransforms();
                for(int step=0;step<300;step++)world.Step(.05f,true);
                report.obstacleSpeed=subject.speed;report.obstacleStop=subject.speed<.05f&&subject.travelled<12.5f;
                float previous=subject.travelled;UnityEngine.Object.DestroyImmediate(barrier);Physics.SyncTransforms();
                for(int step=0;step<600;step++)world.Step(.05f,true);
                report.resumeDistance=subject.travelled-previous;
                if(report.vehicles<10||report.types<4||report.turnPaths<30)report.failures.Add("Vehicle/network variety missing");
                if(report.overlaps>0||report.roadDepartures>0)report.failures.Add("Vehicle overlap or path outside asphalt: "+report.overlaps+" / "+report.roadDepartures);
                if(report.minimumTravel<120||report.maximumWait>65||report.leftTurns<8||report.rightTurns<8||report.straightMoves<8)report.failures.Add("Traffic starvation or route variety regression");
                if(report.minimumTurnRadius<3.0f)report.failures.Add("Unrealistically tight turn radius");
                if(!report.obstacleStop||report.resumeDistance<12)report.failures.Add("Obstacle stop/resume failed");
                Directory.CreateDirectory("QA");File.WriteAllText("QA/traffic-report.json",JsonUtility.ToJson(report,true));
                if(report.failures.Count>0)throw new Exception("Traffic QA: "+string.Join("; ",report.failures));
                Debug.Log("Traffic QA passed: "+report.turnPaths+" swept turn paths, "+report.simulationSteps+" steps, no overlaps, obstacle stop/resume, 4 vehicle types.");
                world.Initialize();for(int step=0;step<650;step++)world.Step(.05f,true);Capture(world);
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static bool OnRoad(WestbruchLayout plan,Vector3 p,float inset)
        {foreach(var road in plan.roads)if(Mathf.Abs(p.x-road.x)<road.w*.5f-inset&&Mathf.Abs(p.z-road.z)<road.d*.5f-inset)return true;return false;}
        static void CheckVehicles(WestbruchTraffic world,WestbruchLayout plan,Report report)
        {
            for(int i=0;i<world.vehicles.Length;i++)
            {
                var car=world.vehicles[i];
                if(!OnRoad(plan,car.transform.position,.10f))report.roadDepartures++;
                if(!car.turning)
                {
                    var forward=(world.nodes[car.to]-world.nodes[car.from]).normalized;var right=Vector3.Cross(Vector3.up,forward);
                    float lateral=Vector3.Dot(car.transform.position-world.nodes[car.from],right);
                    if(Mathf.Abs(lateral-world.Edge(car.from,car.to).offset)>.015f)report.roadDepartures++;
                }
                for(int j=i+1;j<world.vehicles.Length;j++)
                {
                    var other=world.vehicles[j];if((car.transform.position-other.transform.position).sqrMagnitude>40)continue;
                    if(Physics.ComputePenetration(car.bodyCollider,car.transform.position,car.transform.rotation,other.bodyCollider,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.015f)report.overlaps++;
                }
            }
        }
        static void Capture(WestbruchTraffic world)
        {
            var camera=Camera.main;var target=new RenderTexture(2048,1152,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var player=UnityEngine.Object.FindAnyObjectByType<ThirdPersonMotor>();foreach(var r in player.GetComponentsInChildren<Renderer>())r.enabled=false;
            int view=0,frames=0;EditorApplication.CallbackFunction render=null;
            render=()=>
            {
                try
                {
                    Vector3 at,eye;
                    if(view<4){var car=world.vehicles[view];at=car.transform.position+Vector3.up*.8f;eye=car.transform.TransformPoint(new Vector3(-3.4f,2.2f,4.9f));}
                    else if(view==4){var car=UnityEngine.Object.FindAnyObjectByType<SimpleCar>();at=car.transform.position+Vector3.up*.9f;eye=car.transform.TransformPoint(new Vector3(-3.6f,2.2f,4.9f));}
                    else {at=new Vector3(20,0,0);eye=new Vector3(-2,13,-22);}
                    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(at-eye));EditorApplication.QueuePlayerLoopUpdate();if(++frames<35)return;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(2048,1152,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,2048,1152),0,0);pixels.Apply();File.WriteAllBytes("QA/vehicles-"+view+".png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
                    frames=0;if(++view<6)return;camera.targetTexture=null;target.Release();EditorApplication.update-=render;Debug.Log("Vehicle/traffic native HDRP captures complete");EditorApplication.Exit(0);
                }
                catch(Exception ex){Debug.LogException(ex);EditorApplication.update-=render;EditorApplication.Exit(1);}
            };EditorApplication.update+=render;
        }
    }
}
