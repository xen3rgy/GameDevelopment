using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZeroRise.Editor
{
    internal static class WestbruchEarthCoverageQA
    {
        [Serializable] sealed class Report
        {
            public int eligibleSoilSamples,coveredSoilSamples;
            public int[] eligibleQuadrants=new int[4],coveredQuadrants=new int[4];
            public float coverage,maxUncoveredDistance;
            public List<string> failures=new List<string>();
        }
        // Measure distribution on the saved scene, not only a total plant count.
        // The old island-heavy layout could hit its geometry budget and still
        // leave entire plots empty. Hard surfaces and used paths are excluded.
        public static void Validate(WestbruchLayout plan)
        {
            WestbruchLandscape.InspectScene(plan);Physics.SyncTransforms();
            var grid=new Dictionary<Vector2Int,List<Vector2>>();
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if(!filter.name.StartsWith("Ground flora "))continue;
                var position=filter.transform.position;
                if(filter.GetComponent<Renderer>().bounds.size.y<.10f)continue;
                var key=new Vector2Int(Mathf.FloorToInt(position.x/3),Mathf.FloorToInt(position.z/3));
                if(!grid.TryGetValue(key,out var list)){list=new List<Vector2>();grid[key]=list;}list.Add(new Vector2(position.x,position.z));
            }
            var report=new Report();
            for(float z=-107;z<108;z+=3.2f)for(float x=-108;x<126;x+=3.2f)
            {
                if(!WestbruchLandscape.OnEarth(x,z,.65f))continue;
                var key=new Vector2Int(Mathf.FloorToInt(x/3),Mathf.FloorToInt(z/3));float nearest=12;
                for(int dz=-3;dz<=3;dz++)for(int dx=-3;dx<=3;dx++)
                    if(grid.TryGetValue(key+new Vector2Int(dx,dz),out var list))
                        foreach(var plant in list)nearest=Mathf.Min(nearest,Vector2.Distance(new Vector2(x,z),plant));
                int quadrant=(x>=0?1:0)+(z>=0?2:0);report.eligibleSoilSamples++;report.eligibleQuadrants[quadrant]++;
                report.maxUncoveredDistance=Mathf.Max(report.maxUncoveredDistance,nearest);
                if(nearest<=2.8f){report.coveredSoilSamples++;report.coveredQuadrants[quadrant]++;}
            }
            report.coverage=report.coveredSoilSamples/(float)Mathf.Max(1,report.eligibleSoilSamples);
            if(report.eligibleSoilSamples<500||report.coverage<.93f||report.maxUncoveredDistance>6)report.failures.Add("Visible ground cover leaves large soil gaps");
            for(int q=0;q<4;q++)if(report.coveredQuadrants[q]<report.eligibleQuadrants[q]*.88f)report.failures.Add("Sparse earth quadrant "+q);
            Directory.CreateDirectory("QA");File.WriteAllText("QA/earth-coverage-report.json",JsonUtility.ToJson(report,true));
            if(report.failures.Count>0)throw new Exception("Earth coverage QA: "+string.Join("; ",report.failures));
            Debug.Log("Earth coverage QA passed: "+report.coveredSoilSamples+" / "+report.eligibleSoilSamples+" soil samples near visible growth ("+(report.coverage*100).ToString("F1")+"%); largest nearest-growth distance "+report.maxUncoveredDistance.ToString("F2")+"m.");
        }
    }
}
