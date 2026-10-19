using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ZeroRise.Editor
{
    // Runs exclusively on a disposable play-mode copy of the saved scene.
    public static class WestbruchOverpassQA
    {
        const string Pending="WestbruchOverpassQA.Pending";
        internal const string AuthoredReport="WestbruchOverpassQA.AuthoredReport",AuthoredFailed="WestbruchOverpassQA.AuthoredFailed";
        public static void CaptureOnly()=>Launch(false);
        public static void CaptureSaved()=>Launch(true);
        public static void RebuildAndValidate(){WestbruchBridgeStart.Apply();CaptureSaved();}
        static void Launch(bool validate)
        {
            Directory.CreateDirectory("QA");
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)
                EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            File.WriteAllText("QA/overpass-"+(validate?"validation":"baseline")+".txt","RUNNING: saved scene runtime QA\n");
            if(validate)ValidateAuthoredAssets();
            SessionState.SetInt(Pending,validate?2:1);
            EditorApplication.EnterPlaymode();
        }
        public static void ValidateAuthoredAssets()
        {
            // Inspect authored assets before Unity substitutes non-readable
            // production static-batch meshes on entering play mode.
            var lines=new List<string>();bool failed=false;
            void Record(string label,bool pass,string detail){failed|=!pass;lines.Add((pass?"PASS: ":"FAIL: ")+label+". "+detail);}
            var bridge=GameObject.Find(WestbruchBridgeStart.BridgeName);bool valid=bridge;int meshes=0,triangles=0;
            if(bridge)foreach(var f in bridge.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=f.GetComponent<MeshRenderer>();if(!f.sharedMesh||!renderer||!renderer.enabled)continue;
                if(!f.sharedMesh.isReadable){valid=false;continue;}
                meshes++;var vertices=f.sharedMesh.vertices;var indices=f.sharedMesh.triangles;triangles+=indices.Length/3;
                foreach(var p in vertices)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))valid=false;
                foreach(int index in indices)if(index<0||index>=vertices.Length)valid=false;
            }
            Record("Authored visible bridge mesh integrity",valid&&meshes>0,"Visible meshes "+meshes+", triangles "+triangles);
            bool rootsValid=true;float largestRootDistance=0;int floraVertices=0;
            foreach(string name in new[]{WestbruchBridgeStart.BridgeName,WestbruchBridgeStart.CampName})
            {
                var group=GameObject.Find(name);if(!group)continue;
                foreach(var filter in group.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();
                    if(!renderer||!renderer.enabled||!renderer.sharedMaterial||!renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE"))continue;
                    var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable){rootsValid=false;continue;}
                    var vertices=mesh.vertices;var heights=mesh.uv3;var roots=mesh.uv4;
                    if(heights.Length!=vertices.Length||roots.Length!=vertices.Length){rootsValid=false;continue;}
                    floraVertices+=vertices.Length;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var world=filter.transform.TransformPoint(vertices[i]);float delta=Vector2.Distance(new Vector2(world.x,world.z),roots[i]);
                        largestRootDistance=Mathf.Max(largestRootDistance,delta);if(delta>3||Mathf.Abs(world.y-heights[i].x)>3)rootsValid=false;
                    }
                }
            }
            Record("Authored embankment and camp foliage retains per-plant wind/contact roots",rootsValid&&floraVertices>0,"Vertices "+floraVertices+"; maximum horizontal vertex/root distance "+largestRootDistance.ToString("F3")+" m");
            var roadRoots=new HashSet<Vector2>();int unreadable=0;
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer||!renderer.enabled||!renderer.sharedMaterial||!renderer.sharedMaterial.IsKeywordEnabled("_ZR_FOLIAGE"))continue;
                var mesh=filter.sharedMesh;if(!mesh||!mesh.isReadable){unreadable++;continue;}
                var roots=mesh.uv4;if(roots.Length!=mesh.vertexCount){unreadable++;continue;}
                // Local cleanup deliberately retains unused source vertices;
                // only indices still rendered can grow through the new road.
                foreach(int index in new HashSet<int>(mesh.triangles))
                {
                    var p=roots[index];
                    if((p.x>100.4f&&p.x<155.6f&&p.y>75.4f&&p.y<84.6f)||(p.x>142.4f&&p.x<159.6f&&p.y>61.4f&&p.y<98.6f))
                        roadRoots.Add(new Vector2(Mathf.Round(p.x*100)/100,Mathf.Round(p.y*100)/100));
                }
            }
            string examples="";int shown=0;foreach(var root in roadRoots){if(shown++>=5)break;examples+=" "+root.ToString("F2");}
            Record("New asphalt lanes and turning court exclude interior vegetation",roadRoots.Count==0&&unreadable==0,"Interior plant roots "+roadRoots.Count+"; unreadable authored flora meshes "+unreadable+examples);
            SessionState.SetString(AuthoredReport,string.Join("\n",lines));SessionState.SetBool(AuthoredFailed,failed);
            File.WriteAllLines("QA/overpass-authored-assets.txt",lines);
        }
        [InitializeOnLoadMethod] static void Register()
        {
            EditorApplication.playModeStateChanged-=OnPlay;
            EditorApplication.playModeStateChanged+=OnPlay;
        }
        static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            int pending=SessionState.GetInt(Pending,0);if(pending==0)return;
            SessionState.SetInt(Pending,0);
            var runner=new GameObject("Disposable overpass runtime QA").AddComponent<WestbruchOverpassQARunner>();
            runner.validate=pending==2;
        }
    }
    [DefaultExecutionOrder(1000)]
    public sealed class WestbruchOverpassQARunner:MonoBehaviour
    {
        public bool validate;
        Camera captureCamera;RenderTexture target;ThirdPersonMotor player;OrbitCamera orbit;
        readonly List<string> results=new List<string>();readonly HashSet<string> errorMessages=new HashSet<string>();int errors;bool failed,finished;
        SimpleCar testCar;Rigidbody carBody;CharacterController controller;
        bool driveControl,walkReached;float throttle,steering;bool braking;
        float maximumTilt,maximumLaneError,airborneTime,maximumAirborne,orbitYaw;
        void Start()=>StartCoroutine(Guard(Run()));
        IEnumerator Guard(IEnumerator inner)
        {
            while(!finished)
            {
                object current=null;bool next=false;
                try{next=inner.MoveNext();if(next)current=inner.Current;}
                catch(Exception e){Finish(e.ToString());}
                if(!next||finished)yield break;
                if(current is IEnumerator child)yield return Guard(child);else yield return current;
            }
        }
        IEnumerator Run()
        {
            Application.logMessageReceived+=OnLog;
            if(validate){results.AddRange(SessionState.GetString(WestbruchOverpassQA.AuthoredReport,"").Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries));failed=SessionState.GetBool(WestbruchOverpassQA.AuthoredFailed,false);}
            player=FindAnyObjectByType<ThirdPersonMotor>();captureCamera=Camera.main;
            if(!player||!captureCamera){Finish("Missing player or main camera.");yield break;}
            player.enabled=false;orbit=player.orbit;controller=player.GetComponent<CharacterController>();
            target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);
            captureCamera.targetTexture=target;
            var hd=captureCamera.GetComponent<HDAdditionalCameraData>();
            if(hd){hd.customRenderingSettings=true;
                foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.DepthOfField})
                {hd.renderingPathCustomFrameSettings.SetEnabled(field,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;}}
            for(int i=0;i<100;i++)yield return null;
            orbitYaw=orbit.transform.eulerAngles.y;
            yield return Capture("spawn",null,null);
            if(orbit)orbit.enabled=false;
            yield return Capture("overall",new Vector3(151,26,111),new Vector3(118,3,80));
            yield return Capture("street",new Vector3(103,1.8f,77.5f),new Vector3(125,3,80));
            yield return Capture("north-track",new Vector3(118,9,45),new Vector3(118,7,105));
            yield return Capture("south-track",new Vector3(118,9,113),new Vector3(118,7,60));
            yield return Capture("shelter",new Vector3(111,2.2f,73),new Vector3(116,1,70));
            yield return Capture("junction",new Vector3(81,9,93),new Vector3(114,1,80));
            if(validate)yield return ValidateRuntime();
            Finish(null);
        }
        IEnumerator Capture(string suffix,Vector3? position,Vector3? look)
        {
            if(position.HasValue){captureCamera.transform.position=position.Value;captureCamera.transform.LookAt(look.Value);}
            for(int i=0;i<35;i++)yield return null;
            RenderPipeline.SubmitRenderRequest(captureCamera,new RenderPipeline.StandardRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
            File.WriteAllBytes("QA/overpass-"+(validate?"":"baseline-")+suffix+".png",pixels.EncodeToPNG());
            Destroy(pixels);RenderTexture.active=previous;
        }
        IEnumerator ValidateRuntime()
        {
            Geometry();
            Check("Authored new-game spawn",Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(116,70))<.2f,player.transform.position.ToString("F3"));
            var initial=player.transform.position;
            for(int i=0;i<30;i++){MoveForFrame(Vector2.zero);yield return null;}
            Check("Spawn grounded using actual motor",player.IsGrounded&&Mathf.Abs(initial.y-player.transform.position.y)<.25f,player.transform.position.ToString("F3"));
            orbit.enabled=false;orbit.transform.rotation=Quaternion.identity;
            var walking=new[]{new Vector2(111,70),new Vector2(110,73.5f),new Vector2(143,73.5f),new Vector2(145,86.5f),new Vector2(104,86.5f)};
            float walked=0;bool allWalked=true;
            foreach(var point in walking)
            {
                var before=player.transform.position;yield return WalkTo(point);
                walked+=Vector3.Distance(before,player.transform.position);allWalked&=walkReached;
                if(!walkReached)break;
            }
            Check("Actual ThirdPersonMotor walking from camp gate around pier and through underpass",allWalked,"Travel "+walked.ToString("F2")+" m; ended "+player.transform.position.ToString("F3"));
            yield return Capture("walked-underpass",new Vector3(103,2,86.5f),new Vector3(120,2.4f,84));
            testCar=player.nearbyCar?player.nearbyCar:FindAnyObjectByType<SimpleCar>();
            if(!testCar){Check("Driveable car exists",false,"");yield break;}
            carBody=testCar.GetComponent<Rigidbody>();
            yield return DriveRoute("eastbound",new Vector2(103,77.5f),new Vector2(149,77.5f),90);
            yield return DriveRoute("westbound",new Vector2(149,82.5f),new Vector2(102,82.5f),270);
            Check("No scene gameplay objects removed",FindObjectsByType<ThirdPersonMotor>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<SimpleCar>(FindObjectsSortMode.None).Length>0,"Tests use the saved scene's actual player/car; scene never saved from play mode.");
        }
        void Geometry()
        {
            Physics.SyncTransforms();bool continuous=true,clear=true;float largestStep=0,previous=0,minHeadroom=100;
            // Both the original north/south street and new east/west extension
            // must have continuous ground and unobstructed vehicle envelopes.
            foreach(bool extension in new[]{false,true})foreach(float lane in extension?new[]{77.5f,82.5f}:new[]{91.5f,96.5f})
            {
                bool first=true;
                for(float along=extension?102:40;along<=(extension?148:92);along+=.5f)
                {
                    float x=extension?along:lane,z=extension?lane:along;
                    if(!Physics.Raycast(new Vector3(x,2,z),Vector3.down,out var floor,4,1,QueryTriggerInteraction.Ignore)||floor.normal.y<.85f){continuous=false;continue;}
                    if(!first){largestStep=Mathf.Max(largestStep,Mathf.Abs(floor.point.y-previous));if(Mathf.Abs(floor.point.y-previous)>.15f)continuous=false;}first=false;previous=floor.point.y;
                    if(Physics.Raycast(floor.point+Vector3.up*.5f,Vector3.up,out var ceiling,12,1,QueryTriggerInteraction.Ignore)){minHeadroom=Mathf.Min(minHeadroom,ceiling.point.y-floor.point.y);if(ceiling.point.y-floor.point.y<4.2f)clear=false;}
                    if(Physics.CheckBox(floor.point+Vector3.up*2.25f,extension?new Vector3(.24f,1.85f,1.05f):new Vector3(1.05f,1.85f,.24f),Quaternion.identity,1,QueryTriggerInteraction.Ignore))clear=false;
                }
            }
            Check("Both lanes continuous on Stadtanschluss z40–92 and Nordring extension x102–148",continuous,"Largest 0.5 m sample elevation change "+largestStep.ToString("F3")+" m");
            Check("Vehicle envelope and 4.2 m minimum overhead clearance",clear,"Lowest overhead "+minHeadroom.ToString("F3")+" m");
            bool railbed=true;float previousRail=0,largestRailStep=0;
            for(float z=60;z<=100;z+=.5f)
            {
                if(!Physics.Raycast(new Vector3(118,8,z),Vector3.down,out var bed,4,1,QueryTriggerInteraction.Ignore)||bed.normal.y<.85f||bed.point.y<5.7f){railbed=false;continue;}
                if(z>60){float step=Mathf.Abs(bed.point.y-previousRail);largestRailStep=Mathf.Max(largestRailStep,step);if(step>.25f)railbed=false;}
                previousRail=bed.point.y;
            }
            Check("Continuous railway support across bridge and both embankment joins",railbed,"Largest 0.5 m sample elevation change "+largestRailStep.ToString("F3")+" m");
        }
        IEnumerator WalkTo(Vector2 targetPoint)
        {
            walkReached=false;float began=Time.time;
            float timeout=Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),targetPoint)/1.3f+8;
            while(Time.time-began<timeout)
            {
                var delta=targetPoint-new Vector2(player.transform.position.x,player.transform.position.z);
                if(delta.magnitude<.25f){walkReached=true;break;}
                // Identity camera yaw gives world X/Z input; actual turning,
                // acceleration, gravity, step solver and collisions remain active.
                MoveForFrame(delta.normalized);
                yield return null;
            }
            for(int i=0;i<8;i++){MoveForFrame(Vector2.zero);yield return null;}
            if(walkReached&&!player.IsGrounded)walkReached=false;
        }
        IEnumerator DriveRoute(string name,Vector2 start,Vector2 end,float yaw)
        {
            driveControl=false;testCar.enabled=false;yield return null;testCar.enabled=true;
            carBody.linearVelocity=carBody.angularVelocity=Vector3.zero;
            var road=Ground(new Vector3(start.x,0,start.y));
            carBody.position=road+Vector3.up*.28f;carBody.rotation=Quaternion.Euler(0,yaw,0);
            testCar.transform.SetPositionAndRotation(carBody.position,carBody.rotation);Physics.SyncTransforms();
            for(int i=0;i<70;i++)yield return new WaitForFixedUpdate();
            PlacePlayer(Ground(testCar.transform.TransformPoint(new Vector3(-1.68f,0,-.44f)))+Vector3.up*.04f);
            for(int i=0;i<25;i++){MoveForFrame(Vector2.zero);yield return null;}
            testCar.Enter(player);float began=Time.time;
            while(player.Interacting&&Time.time-began<15)yield return null;
            bool entered=player.driving&&testCar.State==SimpleCar.Occupancy.Driving;
            Check(name+" real animated entry",entered,player.InteractionHint??"");if(!entered)yield break;
            orbit.Rotate(yaw-orbitYaw,0);orbitYaw=yaw;orbit.enabled=true;
            var from=carBody.position;maximumTilt=maximumLaneError=airborneTime=maximumAirborne=0;
            driveControl=true;began=Time.time;bool crossed=false,captured=false;
            while(Time.time-began<35)
            {
                float remaining=Vector2.Distance(new Vector2(carBody.position.x,carBody.position.z),end);
                var direction=new Vector3(end.x-carBody.position.x,0,end.y-carBody.position.z);
                steering=Mathf.Clamp(Vector3.SignedAngle(testCar.transform.forward,direction,Vector3.up)/24,-1,1);
                float speed=carBody.linearVelocity.magnitude,desired=Mathf.Min(5.5f,Mathf.Sqrt(Mathf.Max(0,remaining-1.5f)*3.5f));
                braking=speed>desired+.4f;throttle=speed<desired?.75f:0;
                maximumLaneError=Mathf.Max(maximumLaneError,Mathf.Abs(carBody.position.z-start.y));
                if(!captured&&Mathf.Abs(carBody.position.x-118)<3)
                {
                    captured=true;
                    // Use the production collision-aware orbit camera while
                    // the real player is seated and the wheels are driving.
                    yield return Capture("driving-"+name,null,null);
                }
                if(remaining<2.5f){crossed=true;break;}
                if(carBody.position.y< -2||Vector3.Angle(testCar.transform.up,Vector3.up)>35)break;
                yield return null;
            }
            throttle=0;braking=true;
            began=Time.time;while(carBody.linearVelocity.magnitude>.12f&&Time.time-began<6)yield return null;
            driveControl=false;
            orbit.enabled=false;
            bool upright=maximumTilt<12,tracked=maximumLaneError<1.4f,grounded=maximumAirborne<.4f;
            Check(name+" WheelCollider drive through authored underpass",crossed&&upright&&tracked&&grounded,
                "Travel "+Vector3.Distance(from,carBody.position).ToString("F2")+" m; tilt "+maximumTilt.ToString("F2")+"°; lane deviation "+maximumLaneError.ToString("F3")+" m; maximum airborne "+maximumAirborne.ToString("F3")+" s; end "+carBody.position.ToString("F3"));
            testCar.TryExit(player);began=Time.time;while(player.Interacting&&Time.time-began<15)yield return null;
            Check(name+" real animated exit after stopping",!player.driving&&!player.Interacting&&controller.enabled,player.InteractionHint??"");
        }
        void FixedUpdate()
        {
            if(!driveControl||!testCar)return;
            testCar.DriveStep(throttle,steering,braking);
            maximumTilt=Mathf.Max(maximumTilt,Vector3.Angle(testCar.transform.up,Vector3.up));
            bool contact=false;foreach(var wheel in testCar.wheels)if(wheel.GetGroundHit(out _))contact=true;
            airborneTime=contact?0:airborneTime+Time.fixedDeltaTime;maximumAirborne=Mathf.Max(maximumAirborne,airborneTime);
        }
        Vector3 Ground(Vector3 at)
        {
            var hits=Physics.RaycastAll(new Vector3(at.x,3,at.z),Vector3.down,7,1,QueryTriggerInteraction.Ignore);bool found=false;float highest=float.NegativeInfinity;Vector3 point=at;
            foreach(var hit in hits)
            {
                if(testCar&&hit.transform.IsChildOf(testCar.transform))continue;
                if(hit.normal.y>.8f&&hit.point.y>highest){point=hit.point;highest=point.y;found=true;}
            }
            if(!found)throw new Exception("No authored ground at "+at);return point;
        }
        void PlacePlayer(Vector3 p){controller.enabled=false;player.transform.SetPositionAndRotation(p,Quaternion.identity);player.visual.rotation=Quaternion.identity;controller.enabled=true;Physics.SyncTransforms();}
        void MoveForFrame(Vector2 input)
        {
            // Headless HDRP may run below 20 fps. Substeps preserve elapsed
            // simulation time without giving the motor a large gravity step.
            float remaining=Time.deltaTime;
            while(remaining>.00001f){float dt=Mathf.Min(remaining,.025f);player.MoveStep(input,false,false,dt);remaining-=dt;}
        }
        void Check(string label,bool pass,string detail){failed|=!pass;results.Add((pass?"PASS: ":"FAIL: ")+label+". "+detail);File.WriteAllLines("QA/overpass-validation.txt",results);}
        void OnLog(string text,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;errors++;
            if(errorMessages.Count<16)errorMessages.Add(text+"\n"+stack);
        }
        void Finish(string problem)
        {
            if(finished)return;finished=true;driveControl=false;
            if(problem!=null){failed=true;results.Add(problem);}
            results.Add("Runtime errors: "+errors);
            foreach(var message in errorMessages)results.Add("Unique runtime error: "+message);
            results.Add("Passed: "+(!failed&&errors==0));
            File.WriteAllLines("QA/overpass-"+(validate?"validation":"baseline")+".txt",results);
            EditorApplication.Exit(!failed&&errors==0?0:1);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;if(target){if(captureCamera)captureCamera.targetTexture=null;target.Release();Destroy(target);}}
    }
}
