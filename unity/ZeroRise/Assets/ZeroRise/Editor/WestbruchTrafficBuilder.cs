using System.Collections.Generic;
using UnityEngine;
using static ZeroRise.Editor.WestbruchGeometry;

namespace ZeroRise.Editor
{
    internal static class WestbruchTrafficBuilder
    {
        public static void Build(Transform parent,WestbruchLayout plan)
        {
            var root=Group("District traffic – right-hand lanes and junction reservations",parent,Vector3.zero);
            var world=root.AddComponent<WestbruchTraffic>();var nodes=new List<Vector3>();
            foreach(var h in plan.roads)if(h.w>h.d)
                foreach(var v in plan.roads)if(v.d>v.w&&Mathf.Abs(v.x-h.x)<=h.w*.5f&&Mathf.Abs(h.z-v.z)<=v.d*.5f)nodes.Add(new Vector3(v.x,.04f,h.z));
            var links=new List<WestbruchTraffic.Link>();
            foreach(var road in plan.roads)
            {
                bool horizontal=road.w>road.d;var along=new List<int>();
                for(int i=0;i<nodes.Count;i++)if(horizontal?Mathf.Abs(nodes[i].z-road.z)<.01f:Mathf.Abs(nodes[i].x-road.x)<.01f)along.Add(i);
                along.Sort((a,b)=>(horizontal?nodes[a].x:nodes[a].z).CompareTo(horizontal?nodes[b].x:nodes[b].z));
                for(int i=1;i<along.Count;i++)links.Add(new WestbruchTraffic.Link{a=along[i-1],b=along[i],offset=(horizontal?road.d:road.w)*.25f-.3f,speed=road.id=="Stadtanschluss"?8.0f:road.id=="Hofstrasse"?5.6f:6.8f});
            }
            world.nodes=nodes.ToArray();world.links=links.ToArray();world.vehicles=new TrafficVehicle[12];
            for(int i=0;i<world.vehicles.Length;i++)world.vehicles[i]=WestbruchVehicleArt.Create(root.transform,i%4,(i*5+1)%6,false,true,Vector3.zero,0).GetComponent<TrafficVehicle>();
            world.Initialize();
        }
    }
}
