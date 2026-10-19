using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise
{
    // Opt-in standalone verification. No effect during normal gameplay.
    public sealed class WindowsSmokeCheck:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnableIfRequested()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--zero-rise-smoke")>=0)
                new GameObject("Windows build verification").AddComponent<WindowsSmokeCheck>();
        }
        IEnumerator Start()
        {
            for(int i=0;i<90;i++)yield return null;
            var car=FindAnyObjectByType<SimpleCar>();var player=FindAnyObjectByType<ThirdPersonMotor>();
            var vegetation=FindAnyObjectByType<VegetationMotion>();var urbanShader=Shader.Find("ZeroRise/UrbanLit");
            bool pass=car&&player&&Camera.main&&car.wheels.Length==4&&vegetation&&vegetation.player==player&&vegetation.car==car&&vegetation.LiveContacts>0&&urbanShader&&urbanShader.isSupported;
            var canal=FindAnyObjectByType<WaterSurface>();float waterStart=canal?canal.simulationTime:0;
            var pipeline=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            pass=pass&&canal&&canal.surfaceType==WaterSurfaceType.River&&!canal.GetComponent<Collider>()&&pipeline&&pipeline.currentPlatformRenderPipelineSettings.supportWater;
            var terrain=Terrain.activeTerrain;
            pass=pass&&terrain&&terrain.GetComponent<TerrainCollider>()&&terrain.terrainData.heightmapResolution>=1025&&terrain.terrainData.terrainLayers.Length==4
                &&terrain.materialTemplate&&terrain.materialTemplate.shader.name=="ZeroRise/UrbanGround"&&terrain.materialTemplate.shader.isSupported;
            pass=pass&&player.avatar&&player.avatar.skin&&player.avatar.animator&&player.avatar.animator.avatar&&player.avatar.animator.avatar.isValid&&new HashSet<AnimationClip>(player.avatar.animator.runtimeAnimatorController.animationClips).Count==15;
            var traffic=FindAnyObjectByType<WestbruchTraffic>();var types=new HashSet<int>();
            pass=pass&&traffic&&traffic.vehicles.Length==12&&traffic.nodes.Length==9;
            float trafficStart=0;
            if(traffic)foreach(var vehicle in traffic.vehicles){types.Add(vehicle.kind);trafficStart+=vehicle.travelled;}
            pass=pass&&types.Count==4;
            var contactCars=new List<Transform>();if(traffic)foreach(var vehicle in traffic.vehicles)contactCars.Add(vehicle.transform);if(car)contactCars.Add(car.transform);
            var district=GameObject.Find("Westbruch – authored district");if(district)foreach(Transform item in district.transform)if(item.name.StartsWith("Parked vehicle "))contactCars.Add(item);
            int verifiedContacts=0;foreach(var vehicle in contactCars)if(VerifyVehicleContact(vehicle))verifiedContacts++;
            pass=pass&&verifiedContacts==17;Debug.Log("WINDOWS VEHICLE CONTACT SMOKE: "+verifiedContacts+" / 17 vehicles with working body/cabin collision and clear air over bonnet/boot");
            var residents=FindAnyObjectByType<PedestrianWorld>();float residentStart=0;
            pass=pass&&residents&&residents.people.Length==18&&residents.navigation;
            if(residents)foreach(var person in residents.people){residentStart+=person.totalDistance;pass=pass&&person.agent.isOnNavMesh&&person.avatar.skin.enabled;}
            if(pass)
            {
                player.enabled=false;var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;
                var approach=car.transform.TransformPoint(new Vector3(-1.68f,0,-.44f));
                if(Physics.Raycast(approach+Vector3.up*3,Vector3.down,out var ground,5,1))approach=ground.point+Vector3.up*.035f;
                player.transform.position=approach;capsule.enabled=true;Physics.SyncTransforms();
                float start=Time.time;while(Time.time-start<.3f){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                car.Enter(player);start=Time.time;float angle=0;
                while(car.State==SimpleCar.Occupancy.Entering&&Time.time-start<5){angle=Mathf.Max(angle,car.DoorAngle);yield return null;}
                pass=player.driving&&player.visual.gameObject.activeSelf&&angle>60&&Vector3.Distance(player.transform.position,car.seatAnchor.position)<.06f;
                car.TryExit(player);start=Time.time;
                while(car.State==SimpleCar.Occupancy.Exiting&&Time.time-start<4)yield return null;
                pass=pass&&!player.driving&&!player.Interacting&&capsule.enabled&&car.DoorAngle<.01f;
            }
            if(pass)
            {
                var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;
                var farSide=car.transform.TransformPoint(new Vector3(1.68f,0,-.44f));
                if(Physics.Raycast(farSide+Vector3.up*3,Vector3.down,out var floor,5,1))farSide=floor.point+Vector3.up*.035f;
                player.transform.SetPositionAndRotation(farSide,Quaternion.identity);capsule.enabled=true;Physics.SyncTransforms();
                float start=Time.time;while(Time.time-start<.3f){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                car.Enter(player);start=Time.time;while(player.Interacting&&Time.time-start<12)yield return null;
                pass=player.driving&&!player.Interacting;Debug.Log("WINDOWS REENTRY SMOKE: "+(pass?"PASSED":"FAILED")+" · opposite-side approach after exit");
                if(pass){car.TryExit(player);start=Time.time;while(player.Interacting&&Time.time-start<8)yield return null;pass=!player.driving&&!player.Interacting&&capsule.enabled;}
            }
            if(pass)
            {
                // Verify the actual feature in the shipped executable, with
                // the driver's doorway physically blocked by a static solid.
                var blockedDoor=new GameObject("Temporary blocked driver doorway smoke test");
                blockedDoor.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(-1.5f,.9f,-.44f)),car.transform.rotation);
                blockedDoor.AddComponent<BoxCollider>().size=new Vector3(.2f,1.8f,2);
                var capsule=player.GetComponent<CharacterController>();capsule.enabled=false;
                var approach=car.transform.TransformPoint(new Vector3(1.68f,0,-.44f));
                if(Physics.Raycast(approach+Vector3.up*3,Vector3.down,out var floor,5,1))approach=floor.point+Vector3.up*.035f;
                player.transform.SetPositionAndRotation(approach,Quaternion.identity);capsule.enabled=true;Physics.SyncTransforms();
                float start=Time.time;while(Time.time-start<.3f){player.MoveStep(Vector2.zero,false,false,Time.deltaTime);yield return null;}
                car.Enter(player);start=Time.time;bool rightDoor=false,transfer=false;
                while(player.Interacting&&Time.time-start<12){rightDoor|=car.InteractionSide>0&&car.DoorAngle>60;transfer|=car.SeatTransferProgress>.2f;yield return null;}
                pass=player.driving&&rightDoor&&transfer;bool rightExit=false;
                if(pass)
                {
                    car.TryExit(player);start=Time.time;while(player.Interacting&&Time.time-start<8)yield return null;
                    rightExit=!player.driving&&!player.Interacting&&capsule.enabled&&car.InteractionSide>0;pass=rightExit;
                }
                Debug.Log("WINDOWS PASSENGER ENTRY SMOKE: "+(pass?"PASSED":"FAILED")+" - blocked driver door, passenger boarding, seat transfer, passenger exit");
                Destroy(blockedDoor);
            }
            float trafficEnd=0;if(traffic)foreach(var vehicle in traffic.vehicles)trafficEnd+=vehicle.travelled;
            pass=pass&&trafficEnd>trafficStart+5;
            Debug.Log("WINDOWS TRAFFIC SMOKE: "+types.Count+" families, "+(trafficEnd-trafficStart).ToString("F2")+" m travelled during boarding verification");
            float residentEnd=0;int activities=0;if(residents)foreach(var person in residents.people){residentEnd+=person.totalDistance;if(person.state==PedestrianAgent.Behaviour.Activity)activities++;}
            pass=pass&&residentEnd>residentStart+1&&activities>0;
            Debug.Log("WINDOWS RESIDENT SMOKE: "+(residents?residents.people.Length:0)+" people, "+activities+" active activities, "+(residentEnd-residentStart).ToString("F2")+" m walked during boarding verification");
            float waterAdvance=canal?canal.simulationTime-waterStart:0;pass=pass&&waterAdvance>.1f;
            Debug.Log("WINDOWS CANAL SMOKE: "+(canal&&waterAdvance>.1f?"PASSED":"FAILED")+" - native river GPU simulation advanced "+waterAdvance.ToString("F2")+" s");
            Debug.Log("ZERO RISE WINDOWS SMOKE: "+(pass?"PASSED":"FAILED"));
            var args=Environment.GetCommandLineArgs();
            int output=Array.IndexOf(args,"--qa-output");
            if(output>=0&&output+1<args.Length)
            {
                Directory.CreateDirectory(args[output+1]);
                File.WriteAllText(Path.Combine(args[output+1],"windows-smoke.txt"),(pass?"PASSED":"FAILED")+" · "+Application.unityVersion);
                ScreenCapture.CaptureScreenshot(Path.Combine(args[output+1],"windows-player.png"));
                for(int i=0;i<5;i++)yield return null;
            }
            Application.Quit(pass?0:1);
        }
        static bool VerifyVehicleContact(Transform vehicle)
        {
            var lowerObject=vehicle.Find("Lower rounded body collision");var upperObject=vehicle.Find("Sloped cabin and roof collision");if(!lowerObject||!upperObject)return false;
            var lower=lowerObject.GetComponent<MeshCollider>();var cabin=upperObject.GetComponent<MeshCollider>();var envelope=vehicle.GetComponent<BoxCollider>();
            if(!lower||!cabin||!lower.enabled||!cabin.enabled||!lower.convex||!cabin.convex||!envelope||envelope.enabled)return false;
            if(!lower.Raycast(new Ray(vehicle.TransformPoint(new Vector3(3,.82f,0)),-vehicle.right),out _,6)||!cabin.Raycast(new Ray(cabin.bounds.center+vehicle.right*3,-vehicle.right),out _,6))return false;
            var agent=vehicle.GetComponent<TrafficVehicle>();int kind=agent?agent.kind:vehicle.name.EndsWith("3")?3:vehicle.name.EndsWith("2")?2:vehicle.name.EndsWith("1")?1:0;
            float length=kind==3?4.65f:kind==2?4.35f:kind==1?4.2f:3.8f;
            foreach(int end in new[]{-1,1})
            {
                var ray=new Ray(vehicle.TransformPoint(new Vector3(3,1.28f,end*(length*.5f-.11f))),-vehicle.right);
                if(lower.Raycast(ray,out _,6)||cabin.Raycast(ray,out _,6))return false;
            }
            return true;
        }
    }
}
