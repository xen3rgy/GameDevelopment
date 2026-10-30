using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    // Invoked only by the explicit release smoke test. No bootstrap, production
    // colliders, gameplay inputs or scene assets are changed by this helper.
    public static class GameplaySoundFeedbackChecks
    {
        sealed class Route
        {
            public Vector3 a,b;
            public string support;
        }
        sealed class Motion
        {
            public float maxSpeed,distance;
            public int repeatedClips;
            public readonly HashSet<string> clips=new HashSet<string>();
        }

        public static IEnumerator Run(ThirdPersonMotor player,Camera camera,GameplayAudioDirector sound,Action<string,bool,string> check,string output)
        {
            if(!player||!camera||!sound||!sound.library)
            {check("Feedback regression actors exist",false,"Missing real player, camera or sound library");yield break;}
            Directory.CreateDirectory(output);
            var controller=player.GetComponent<CharacterController>();
            if(!controller){check("Feedback regression character controller exists",false,"");yield break;}
            float deadline=Time.realtimeSinceStartup+78;
            GameplayAudioRecording recorder=null;
            try
            {
                recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,52);
                foreach(var surface in new[]{GroundSound.Concrete,GroundSound.Soil,GroundSound.Grass,GroundSound.Gravel})
                {
                    if(Time.realtimeSinceStartup>deadline-24)
                    {check("Surface regression time budget",false,"Stopped before remaining foliage checks");break;}
                    if(!FindRoute(surface,out var route))
                    {check("Safe authored "+surface+" route exists",false,"Bounded central district search found no clear, continuous 2.4–3.2 m route");continue;}
                    Place(player,route.a,route.b-route.a);Follow(camera,player);
                    yield return Still(player,camera,.35f);
                    int before=sound.FootstepSurfaceCounts[(int)surface],all=sound.PlayerSteps,cloth=sound.ClothingCues;
                    var walk=new Motion();
                    yield return Travel(player,camera,sound,route.b,false,4.0f,walk);
                    yield return Still(player,camera,.25f);
                    int walkSteps=sound.FootstepSurfaceCounts[(int)surface]-before;
                    check("Actual walking uses "+surface+" contact samples",walkSteps>=3&&walk.distance>2&&walk.clips.Count>=2&&walk.repeatedClips==0,
                        route.support+"; steps "+walkSteps+" / all "+(sound.PlayerSteps-all)+"; movement "+walk.distance.ToString("F2")+" m; clips "+string.Join(", ",walk.clips)+"; repeats "+walk.repeatedClips);
                    check("Steady "+surface+" walking has no repeated clothing cue",sound.ClothingCues==cloth,"Clothing cues "+(sound.ClothingCues-cloth));

                    Place(player,route.a,route.b-route.a);
                    yield return Still(player,camera,.30f);
                    before=sound.FootstepSurfaceCounts[(int)surface];cloth=sound.ClothingCues;
                    var run=new Motion();
                    yield return Travel(player,camera,sound,route.b,true,2.8f,run);
                    yield return Still(player,camera,.25f);
                    int runSteps=sound.FootstepSurfaceCounts[(int)surface]-before;
                    check("Actual running retains "+surface+" material and quiet clothing",runSteps>=2&&run.maxSpeed>2.4f&&run.distance>2&&sound.ClothingCues==cloth&&run.repeatedClips==0,
                        "Steps "+runSteps+"; max speed "+run.maxSpeed.ToString("F2")+" m/s; distance "+run.distance.ToString("F2")+" m; clothing "+(sound.ClothingCues-cloth)+"; clips "+string.Join(", ",run.clips));
                    if(!Application.isEditor)
                    {ScreenCapture.CaptureScreenshot(Path.Combine(output,"sfx-feedback-"+surface.ToString().ToLowerInvariant()+".png"));yield return null;}
                }
                EndTake(recorder,output,"sfx-feedback-surfaces.wav","Actual surface and clothing listener capture",check);recorder=null;

                check("Individual visible shrub crowns are authored",sound.FoliageVolumeCount>0,"Individual crowns "+sound.FoliageVolumeCount);
                if(FindShrub(sound,out var through,out var outside,out var center,out int shrub))
                {
                    Place(player,through.a,center-through.a);Follow(camera,player);
                    yield return Still(player,camera,.35f);
                    recorder=Application.isEditor?null:GameplayAudioRecording.Begin(camera,24);
                    int rustles=sound.BushRustles;
                    yield return Travel(player,camera,sound,center,false,3.3f,new Motion());
                    // Decelerate through the real motor before the stationary
                    // assertion; controller settling must not become a false cue.
                    yield return Still(player,camera,.35f);
                    check("Walking through a visible bush starts actual leaf playback",sound.BushRustles>rustles,
                        "Crown "+shrub+"; position "+player.transform.position.ToString("F2")+"; successful voices "+(sound.BushRustles-rustles));
                    bool inside=sound.TouchesFoliage(player.transform.position,player.transform.position,out int actualCrown,out _);
                    int still=sound.BushRustles;
                    yield return Still(player,camera,2.0f);
                    check("Two seconds standing inside leaves adds no rustle",inside&&sound.BushRustles==still,
                        "Touching crown "+actualCrown+"; new voices "+(sound.BushRustles-still));
                    if(!Application.isEditor)
                    {ScreenCapture.CaptureScreenshot(Path.Combine(output,"sfx-feedback-bush-contact.png"));yield return null;}
                    yield return Travel(player,camera,sound,through.b,false,3.3f,new Motion());
                    yield return Still(player,camera,.25f);

                    Place(player,outside.a,outside.b-outside.a);
                    yield return Still(player,camera,.35f);
                    int away=sound.BushRustles;
                    bool clear=!sound.TouchesFoliage(outside.a,outside.b,out _,out _);
                    var outsideMove=new Motion();
                    yield return Travel(player,camera,sound,outside.b,false,4.2f,outsideMove);
                    yield return Still(player,camera,.25f);
                    check("Passing outside shrub crowns is silent",clear&&outsideMove.distance>1.8f&&sound.BushRustles==away,
                        "No capsule/crown contact "+clear+"; travelled "+outsideMove.distance.ToString("F2")+" m; new voices "+(sound.BushRustles-away));
                    EndTake(recorder,output,"sfx-feedback-bush-traversal.wav","Actual bush contact and silence listener capture",check);recorder=null;
                }
                else check("Safe visible shrub traversal exists",false,"No clear source crown with an independent outside control route among the bounded authored shrub candidates");
            }
            finally
            {
                if(recorder)EndTake(recorder,output,"sfx-feedback-interrupted.wav","Interrupted feedback listener capture",check);
                // Continue the main smoke test from the real sheltered camp.
                if(TryGround(new Vector3(111,0,70),out var camp))Place(player,camp.point,Vector3.forward);
                else Place(player,new Vector3(111,.10f,70),Vector3.forward);
                Follow(camera,player);
            }
            yield return Still(player,camera,.25f);
        }

        static bool FindRoute(GroundSound wanted,out Route route)
        {
            // Existing pavement/court and rough-grass islands are tried first.
            // The fallback visits at most 441 central terrain sites, never the
            // distant railway slopes, entire scene hierarchy or generated assets.
            var seeds=new[]{new Vector3(-60,0,42),new Vector3(-35,0,-40),new Vector3(111,0,70),new Vector3(110,0,69),
                new Vector3(73,0,30),new Vector3(-45,0,34),new Vector3(-25,0,-70),new Vector3(-1,0,-69),new Vector3(-17,0,-48),
                new Vector3(10,0,-51),new Vector3(61,0,31),new Vector3(-10,0,45),new Vector3(35,0,31),new Vector3(8,0,-67)};
            foreach(var seed in seeds)if(TrySurfaceRoute(seed,wanted,out route))return true;
            for(int z=-100;z<=100;z+=10)for(int x=-100;x<=100;x+=10)
                if(TrySurfaceRoute(new Vector3(x,0,z),wanted,out route))return true;
            route=null;return false;
        }
        static bool TrySurfaceRoute(Vector3 center,GroundSound wanted,out Route route)
        {
            route=null;if(!TryGround(center,out var hit)||GameAudioLibrary.SurfaceAt(hit)!=wanted)return false;
            foreach(float half in new[]{1.6f,1.2f})
            foreach(var direction in new[]{Vector3.right,Vector3.forward,new Vector3(.7071f,0,.7071f),new Vector3(.7071f,0,-.7071f)})
                if(TryLine(hit.point-direction*half,hit.point+direction*half,out route,wanted))return true;
            return false;
        }
        static bool TryLine(Vector3 a,Vector3 b,out Route route,GroundSound? surface=null)
        {
            route=null;float lowest=float.PositiveInfinity,highest=float.NegativeInfinity;
            Vector3 start=default,end=default;string support="";
            for(int i=0;i<=8;i++)
            {
                var p=Vector3.Lerp(a,b,i/8f);
                if(!TryGround(p,out var hit)||surface.HasValue&&GameAudioLibrary.SurfaceAt(hit)!=surface.Value)return false;
                if(Physics.CheckCapsule(hit.point+Vector3.up*.48f,hit.point+Vector3.up*1.45f,.30f,1,QueryTriggerInteraction.Ignore))return false;
                lowest=Mathf.Min(lowest,hit.point.y);highest=Mathf.Max(highest,hit.point.y);
                if(highest-lowest>.14f)return false;
                if(i==0){start=hit.point;support=Describe(hit);}
                if(i==8)end=hit.point;
            }
            route=new Route{a=start,b=end,support=support};return true;
        }
        static bool TryGround(Vector3 point,out RaycastHit hit)
        {
            return Physics.Raycast(new Vector3(point.x,9,point.z),Vector3.down,out hit,11,1,QueryTriggerInteraction.Ignore)
                &&hit.point.y>-.3f&&hit.point.y<1.1f&&hit.normal.y>.94f;
        }
        static string Describe(RaycastHit hit)
        {
            if(hit.collider is TerrainCollider terrain&&terrain.terrainData)
            {
                var data=terrain.terrainData;var local=terrain.transform.InverseTransformPoint(hit.point);
                int x=Mathf.Clamp((int)(local.x/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1);
                int z=Mathf.Clamp((int)(local.z/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
                var weights=data.GetAlphamaps(x,z,1,1);var layers=data.terrainLayers;int best=0;
                for(int i=1;i<layers.Length;i++)if(weights[0,0,i]>weights[0,0,best])best=i;
                return "Actual terrain layer "+(layers.Length>0&&layers[best]?layers[best].name:"unknown")+" @ "+hit.point.ToString("F2");
            }
            var renderer=hit.collider.GetComponent<Renderer>();
            return hit.collider.name+" / "+(renderer&&renderer.sharedMaterial?renderer.sharedMaterial.name:"no renderer")+" @ "+hit.point.ToString("F2");
        }
        static bool FindShrub(GameplayAudioDirector sound,out Route through,out Route outside,out Vector3 center,out int index)
        {
            through=outside=null;center=default;index=-1;
            var crowns=sound.library.foliage;if(crowns==null)return false;
            int checkedCrowns=0;
            for(int i=0;i<crowns.Length&&checkedCrowns<256;i++)
            {
                var crown=crowns[i];
                if(Mathf.Abs(crown.center.x)>102||Mathf.Abs(crown.center.z)>92||crown.center.y>1.3f||crown.radii.x<.1f||crown.radii.z<.1f)continue;
                checkedCrowns++;
                if(!TryGround(crown.center,out var hit))continue;
                foreach(var direction in new[]{Vector3.right,Vector3.forward})
                {
                    float along=direction.x!=0?crown.radii.x:crown.radii.z;
                    float across=direction.x!=0?crown.radii.z:crown.radii.x;
                    var side=Vector3.Cross(Vector3.up,direction);
                    float reach=Mathf.Max(1.25f,along+.65f);
                    var a=hit.point-direction*reach;var b=hit.point+direction*reach;
                    if(!TryLine(a,b,out var candidate))continue;
                    foreach(int sign in new[]{-1,1})
                    {
                        var offset=side*(across+.95f)*sign;
                        if(!TryLine(a+offset,b+offset,out var control)||sound.TouchesFoliage(control.a,control.b,out _,out _))continue;
                        through=candidate;outside=control;center=hit.point;index=i;return true;
                    }
                }
            }
            return false;
        }
        static void Place(ThirdPersonMotor player,Vector3 feet,Vector3 facing)
        {
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;
            player.transform.position=feet+Vector3.up*.035f;
            player.SetDriving(false);
            facing.y=0;if(player.visual&&facing.sqrMagnitude>.001f)player.visual.rotation=Quaternion.LookRotation(facing.normalized);
            Physics.SyncTransforms();
        }
        static IEnumerator Travel(ThirdPersonMotor player,Camera camera,GameplayAudioDirector sound,Vector3 target,bool run,float seconds,Motion motion)
        {
            float until=Time.realtimeSinceStartup+seconds;var start=player.transform.position;
            int previousSteps=sound.PlayerSteps;string previousClip=null;
            while(Time.realtimeSinceStartup<until)
            {
                var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.16f)break;
                // Ease the actual input near the target so the motor's normal
                // deceleration finishes within the already validated surface.
                var direction=delta.normalized*Mathf.Clamp01(delta.magnitude/(run?1.3f:.5f));
                Follow(camera,player);Move(player,camera,direction,run);
                yield return null;
                motion.maxSpeed=Mathf.Max(motion.maxSpeed,player.avatar?player.avatar.ActualSpeed:0);
                if(sound.PlayerSteps>previousSteps)
                {
                    string clip=sound.LastFootstepClip;
                    if(clip==previousClip)motion.repeatedClips++;
                    if(!string.IsNullOrEmpty(clip))motion.clips.Add(clip);
                    previousClip=clip;previousSteps=sound.PlayerSteps;
                }
            }
            motion.distance=Vector3.ProjectOnPlane(player.transform.position-start,Vector3.up).magnitude;
        }
        static IEnumerator Still(ThirdPersonMotor player,Camera camera,float seconds)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<until){Follow(camera,player);Move(player,camera,Vector3.zero,false);yield return null;}
        }
        static void Follow(Camera camera,ThirdPersonMotor player)
        {
            camera.transform.position=player.transform.position+new Vector3(-3,2.6f,4);
            camera.transform.LookAt(player.transform.position+Vector3.up*1.1f);
        }
        static void Move(ThirdPersonMotor player,Camera camera,Vector3 direction,bool run)
        {
            var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            var right=Vector3.Cross(Vector3.up,forward);
            var input=new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward));
            float remaining=Mathf.Min(Time.deltaTime,.5f);
            while(remaining>.00001f){float dt=Mathf.Min(.02f,remaining);player.MoveStep(input,run,false,dt);remaining-=dt;}
        }
        static void EndTake(GameplayAudioRecording recorder,string output,string filename,string label,Action<string,bool,string> check)
        {
            if(!recorder)return;
            var stats=recorder.End(Path.Combine(output,filename));
            check(label,stats.frames>48000&&stats.channels==2&&stats.peak>.001f&&stats.peak<.93f&&stats.rms>.00003,
                stats.seconds.ToString("F2")+" s; peak "+stats.peak.ToString("F5")+"; RMS "+stats.rms.ToString("F6")+"; channels "+stats.channels+"; "+filename);
        }
    }
}
