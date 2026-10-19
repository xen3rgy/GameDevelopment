using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class WestbruchOverpassBuild
    {
        [Serializable] public sealed class Release
        {
            public string feature="Westbruch engineered railway overpass";
            public string branch="zero-rise-v0.7.2",utc,sceneHash,buildGuid,executableSha256;
            public long executableBytes;public int totalErrors,totalWarnings;
        }
        public static void RebuildWindows()
        {
            try
            {
                WestbruchBridgeStart.Apply();
                WestbruchOverpassQA.ValidateAuthoredAssets();
                if(SessionState.GetBool(WestbruchOverpassQA.AuthoredFailed,true))throw new Exception("Authored release geometry validation failed.");
                Physics.SyncTransforms();
                foreach(float lane in new[]{77.5f,82.5f})for(float x=102;x<=149;x+=.5f)
                    if(!Physics.Raycast(new Vector3(x,2,lane),Vector3.down,out var floor,4,1,QueryTriggerInteraction.Ignore)||Mathf.Abs(floor.point.y-.04f)>.01f||floor.normal.y<.99f)throw new Exception("Release road collider discontinuity.");
                var mesh=GameObject.Find("Continuous supported ballast formation").GetComponent<MeshFilter>().sharedMesh;
                var vertices=mesh.vertices;var triangles=mesh.triangles;float edge=0;
                for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++)edge=Mathf.Max(edge,Vector3.Distance(vertices[triangles[i+j]],vertices[triangles[i+(j+1)%3]]));
                if(edge>20)throw new Exception("Rail formation collider contains oversized triangles.");
                File.WriteAllText("QA/overpass-release-geometry.txt",SessionState.GetString(WestbruchOverpassQA.AuthoredReport,"")+"\nRelease road collider continuity: PASS\nMaximum ballast collider edge: "+edge.ToString("F3")+" m\n");
                Windows();
            }
            catch(Exception e){File.WriteAllText("QA/overpass-build-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Windows()=>Build(true);
        public static void FromMenu()=>Build(false);
        static void Build(bool exit)
        {
            try
            {
                Directory.CreateDirectory("QA");
                if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
                if(!GameObject.Find("Continuous rail head")||!GameObject.Find("Connected Nordring east carriageway"))throw new Exception("Refusing to package an outdated bridge scene.");
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                string output="Builds/Windows/ZeroRise.exe";Directory.CreateDirectory("Builds/Windows");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{WestbruchBuilder.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result+" / errors "+report.summary.totalErrors);
                var release=new Release{utc=DateTime.UtcNow.ToString("O"),sceneHash=AssetDatabase.GetAssetDependencyHash(WestbruchBuilder.ScenePath).ToString(),buildGuid=report.summary.guid.ToString(),executableBytes=new FileInfo(output).Length,totalErrors=report.summary.totalErrors,totalWarnings=report.summary.totalWarnings};
                using(var stream=File.OpenRead(output))using(var sha=SHA256.Create())release.executableSha256=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                string json=JsonUtility.ToJson(release,true);File.WriteAllText("Builds/Windows/Westbruch-build.json",json);File.WriteAllText("QA/overpass-windows-build.json",json);
                Debug.Log("Fresh Westbruch Windows release: "+json);if(exit)EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("QA/overpass-build-error.txt",e.ToString());Debug.LogException(e);if(exit)EditorApplication.Exit(1);else throw;}
        }
    }
}
