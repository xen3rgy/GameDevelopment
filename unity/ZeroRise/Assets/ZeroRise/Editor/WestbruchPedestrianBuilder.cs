using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.HighDefinition;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    public static class WestbruchPedestrianBuilder
    {
        const string NavPath=Generated+"/WestbruchPedestrianNavigation.asset";
        public static void RebuildAll(){WestbruchVehicleArt.RebuildVehicles();Rebuild();}
        [MenuItem("ZERO RISE/Fussgaenger und Aktivitaeten aktualisieren")]
        public static void Rebuild()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);Clear();
            var plan=JsonUtility.FromJson<WestbruchLayout>(File.ReadAllText("Assets/ZeroRise/Data/Westbruch.json"));var root=GameObject.Find("Westbruch – authored district").transform;
            var old=UnityEngine.Object.FindAnyObjectByType<PedestrianWorld>();if(old){old.UnloadNavigation();UnityEngine.Object.DestroyImmediate(old.gameObject);}
            Build(root,plan);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            if(Application.isBatchMode)WestbruchPedestrianQA.ValidateAndPlay();
        }
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            var root=Group("Westbruch residents – purposeful walks and activities",parent,Vector3.zero);var world=root.AddComponent<PedestrianWorld>();
            Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();var markups=new List<NavMeshBuildMarkup>();
            foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>())markups.Add(new NavMeshBuildMarkup{root=car.transform,ignoreFromBuild=true});
            var bounds=new Bounds(new Vector3(0,1,0),new Vector3(224,7,224));
            NavMeshBuilder.CollectSources(bounds,1,NavMeshCollectGeometry.PhysicsColliders,0,markups,sources);
            for(int i=0;i<sources.Count;i++){var source=sources[i];if(source.sourceObject is TerrainData){source.area=4;sources[i]=source;}}
            foreach(var road in plan.roads)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.ModifierBox,transform=Matrix4x4.TRS(new Vector3(road.x,.5f,road.z),Quaternion.identity,Vector3.one),size=new Vector3(road.w,4,road.d),area=1});
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.34f;settings.agentHeight=1.8f;settings.agentClimb=.27f;settings.agentSlope=40;settings.overrideVoxelSize=true;settings.voxelSize=.09f;settings.minRegionArea=1;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)throw new Exception("Pedestrian navigation build failed");
            var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);if(existing){EditorUtility.CopySerialized(data,existing);UnityEngine.Object.DestroyImmediate(data);data=existing;}else AssetDatabase.CreateAsset(data,NavPath);
            world.navigation=data;world.crossings=Array.Empty<PedestrianWorld.Crossing>();world.LoadNavigation();
            var crossingList=new List<PedestrianWorld.Crossing>();
            foreach(var road in plan.roads)
            {
                bool horizontal=road.w>road.d;float[] along=horizontal?new[]{-77f,-14f,72f}:new[]{-67f,-13f,66f};
                foreach(float t in along)
                {
                    var a=horizontal?new Vector3(t,.14f,road.z-road.d*.5f-.95f):new Vector3(road.x-road.w*.5f-.95f,.14f,t);
                    var b=horizontal?new Vector3(t,.14f,road.z+road.d*.5f+.95f):new Vector3(road.x+road.w*.5f+.95f,.14f,t);
                    if(!NavMesh.SamplePosition(a,out var start,.65f,NavMesh.AllAreas)||!NavMesh.SamplePosition(b,out var end,.65f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(start.position.y-a.y)>.4f||Mathf.Abs(end.position.y-b.y)>.4f)continue;
                    crossingList.Add(new PedestrianWorld.Crossing{a=start.position,b=end.position});
                    // Faded short transverse bars make the designated crossing legible.
                    var paint=Material("PedestrianCrossingWorn", "asphalt",new Color(.48f,.47f,.40f),.08f);
                    int count=horizontal?Mathf.FloorToInt(road.d/.9f):Mathf.FloorToInt(road.w/.9f);
                    for(int s=0;s<count;s++)
                    {float across=(s-(count-1)*.5f)*.9f;Box("Faded pedestrian crossing",root.transform,horizontal?new Vector3(t,.044f,road.z+across):new Vector3(road.x+across,.044f,t),horizontal?new Vector3(2.5f,.004f,.42f):new Vector3(.42f,.004f,2.5f),paint,false);}
                }
            }
            // Reload once with the complete persistent link list.
            world.UnloadNavigation();world.crossings=crossingList.ToArray();world.LoadNavigation();
            var places=new List<PedestrianWorld.Place>();
            bool Add(string label,PedestrianWorld.Activity activity,Vector3 at,Quaternion facing,Vector3 seat=default)
            {
                if(!NavMesh.SamplePosition(at,out var hit,.7f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-at.y)>.5f)return false;
                if(Physics.CheckCapsule(hit.position+Vector3.up*.4f,hit.position+Vector3.up*1.4f,.29f,1,QueryTriggerInteraction.Ignore))return false;
                places.Add(new PedestrianWorld.Place{label=label,activity=activity,approach=hit.position,facing=facing,seat=seat});return true;
            }
            foreach(var item in parent.GetComponentsInChildren<Transform>())if(item.name=="Worn public bench")
            {
                var seat=item.TransformPoint(new Vector3(0,.4875f,-.10f));var at=item.TransformPoint(new Vector3(0,0,-.95f));
                if(Physics.Raycast(at+Vector3.up*2,Vector3.down,out var ground,4,1))at.y=ground.point.y;
                Add("Sitzbank "+places.Count,PedestrianWorld.Activity.Sit,at,item.rotation*Quaternion.Euler(0,180,0),seat);
            }
            foreach(var building in plan.buildings)
            {
                float sign=building.yaw==180?1:-1;var at=new Vector3(building.x,.14f,building.z+sign*(building.d*.5f+1.2f));
                Add(building.id,building.kind=="shop"?PedestrianWorld.Activity.Shop:building.kind=="workshop"?PedestrianWorld.Activity.Stretch:PedestrianWorld.Activity.Visit,at,Quaternion.LookRotation(Vector3.back*sign));
            }
            foreach(var p in new[]{new Vector3(-51,.14f,-49),new Vector3(-35,.14f,-43),new Vector3(-51,.14f,42),new Vector3(6,.14f,42),new Vector3(77,.14f,25),new Vector3(80,.14f,-39)})
            {
                Add("Nachbarschaft – Blick / Telefon",places.Count%2==0?PedestrianWorld.Activity.Phone:PedestrianWorld.Activity.Stretch,p,Quaternion.Euler(0,places.Count*47,0));
                int first=places.Count;if(Add("Nachbarschaft – Gespraech A",PedestrianWorld.Activity.Talk,p+Vector3.right*2,Quaternion.LookRotation(Vector3.forward)))
                    if(Add("Nachbarschaft – Gespraech B",PedestrianWorld.Activity.Talk,p+Vector3.right*2+Vector3.forward*1.8f,Quaternion.LookRotation(Vector3.back)))
                    {places[first].partner=first+1;places[first+1].partner=first;}
            }
            // The bus shelter is authored at this site, reached from its open front.
            Add("Bushaltestelle",PedestrianWorld.Activity.Wait,new Vector3(82.8f,.14f,-7),Quaternion.Euler(0,90,0));
            world.places=places.ToArray();if(places.Count<25)throw new Exception("Too few supported pedestrian activities: "+places.Count);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAvatarBuilder.Folder+"/StreetwearHero.prefab");if(!prefab)throw new Exception("Existing rigged character prefab missing");
            world.people=new PedestrianAgent[18];
            var spawns=new List<int>();foreach(var activity in new[]{PedestrianWorld.Activity.Sit,PedestrianWorld.Activity.Sit,PedestrianWorld.Activity.Sit,PedestrianWorld.Activity.Phone,PedestrianWorld.Activity.Talk,PedestrianWorld.Activity.Talk,PedestrianWorld.Activity.Wait})
                for(int j=0;j<places.Count;j++)if(places[j].activity==activity&&!spawns.Contains(j)){spawns.Add(j);break;}
            for(int j=0;spawns.Count<18&&j<places.Count;j++){int index=j*17%places.Count;if(!spawns.Contains(index)&&spawns.TrueForAll(s=>Vector3.Distance(places[s].approach,places[index].approach)>2))spawns.Add(index);}
            for(int i=0;i<world.people.Length;i++)
            {
                var place=places[spawns[i]];var person=Group("Resident "+(i+1),root.transform,place.approach, i*37);person.layer=9;
                var agent=person.AddComponent<NavMeshAgent>();agent.radius=.38f;agent.height=1.8f;agent.speed=1.12f+i%5*.085f;agent.acceleration=3;agent.angularSpeed=155;agent.stoppingDistance=.08f;agent.autoBraking=true;agent.autoTraverseOffMeshLink=false;agent.autoRepath=true;agent.obstacleAvoidanceType=ObstacleAvoidanceType.HighQualityObstacleAvoidance;agent.avoidancePriority=25+i*3;
                var collider=person.AddComponent<CapsuleCollider>();collider.radius=.27f;collider.height=1.8f;collider.center=Vector3.up*.9f;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,person.transform);visual.name="Resident rig – varied streetwear";var avatar=visual.GetComponent<PlayerAvatar>();
                if(i<3||i%3!=0)avatar.skin.sharedMesh=WestbruchVehicleArt.WithoutBackpack(avatar.skin.sharedMesh,avatar.skin.sharedMaterials);
                var mats=avatar.skin.sharedMaterials;for(int m=0;m<mats.Length;m++)
                {
                    if(mats[m].name.Contains("Eye")||mats[m].name.Contains("Pupil")||mats[m].name.Contains("Iris"))continue;
                    string path=Generated+"/Materials/Resident_"+(i%6)+"_"+m+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!material){material=new Material(mats[m]);AssetDatabase.CreateAsset(material,path);}
                    Color[] coats={new Color(.22f,.27f,.29f),new Color(.33f,.24f,.17f),new Color(.25f,.28f,.17f),new Color(.31f,.16f,.13f),new Color(.22f,.20f,.29f),new Color(.37f,.34f,.28f)};
                    var original=mats[m].GetColor("_BaseColor");var tint=mats[m].name.ToLowerInvariant();Color color=tint.Contains("skin")?new[]{new Color(.64f,.46f,.34f),new Color(.40f,.28f,.21f),new Color(.73f,.56f,.44f)}[i%3]:tint.Contains("cloth")||tint.Contains("pack")?coats[i%6]:original*new Color(.83f+i%3*.07f,.87f,.87f);
                    material.SetColor("_BaseColor",color);HDMaterial.ValidateMaterial(material);EditorUtility.SetDirty(material);mats[m]=material;
                }
                avatar.skin.sharedMaterials=mats;avatar.animator.applyRootMotion=false;avatar.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var npc=person.AddComponent<PedestrianAgent>();npc.agent=agent;npc.avatar=avatar;npc.capsule=collider;npc.world=world;npc.personality=i;world.people[i]=npc;
                if(i<7)npc.initialPlace=spawns[i];
                npc.phone=Box("Handheld phone",avatar.rightHand,new Vector3(0,-.04f,.028f),new Vector3(.055f,.11f,.012f),Material("ResidentPhone",null,new Color(.035f,.043f,.045f),.4f),false);npc.phone.SetActive(false);
                var avoidance=Group("Actual activity and crossing avoidance",person.transform,Vector3.zero);npc.manualObstacle=avoidance.AddComponent<NavMeshObstacle>();npc.manualObstacle.shape=NavMeshObstacleShape.Box;npc.manualObstacle.center=Vector3.up*.9f;npc.manualObstacle.size=new Vector3(.78f,1.8f,.78f);npc.manualObstacle.carving=false;npc.manualObstacle.enabled=false;
                agent.enabled=false;foreach(var child in person.GetComponentsInChildren<Transform>()){child.gameObject.layer=9;child.gameObject.isStatic=false;}
            }
            foreach(var player in UnityEngine.Object.FindObjectsByType<ThirdPersonMotor>())Obstacle(player.gameObject,new Vector3(.72f,1.8f,.72f),Vector3.up*.9f);
            foreach(var car in UnityEngine.Object.FindObjectsByType<SimpleCar>())Obstacle(car.gameObject,new Vector3(1.85f,1.7f,4.1f),Vector3.up*.9f);
            AssetDatabase.SaveAssets();Debug.Log("Pedestrians authored: "+places.Count+" activity slots, "+crossingList.Count+" crossings, "+world.people.Length+" residents, "+sources.Count+" nav sources.");
        }
        static void Obstacle(GameObject owner,Vector3 size,Vector3 center)
        {var obstacle=owner.GetComponent<NavMeshObstacle>();if(!obstacle)obstacle=owner.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=center;obstacle.size=size;obstacle.carving=false;}
    }
}
