using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZeroRise.Editor
{
    public static class WestbruchMacBuild
    {
        [Serializable]
        private sealed class Release
        {
            public string feature = "Westbruch macOS build";
            public string branch = "zero-rise-v0.7.2";
            public string utc;
            public string sceneHash;
            public string buildGuid;
            public string executableSha256;
            public long executableBytes;
            public int totalErrors;
            public int totalWarnings;
        }

        public static void Build()
        {
            try
            {
                var scenePath = "Assets/ZeroRise/Scenes/Westbruch.unity";
                if (!File.Exists(scenePath))
                    throw new FileNotFoundException("Westbruch scene is missing.", scenePath);

                WeatherSurfaceAssets.Prepare();
                var output = "Builds/macOS/ZeroRise.app";
                Directory.CreateDirectory("Builds/macOS");

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new Exception("macOS build failed: " + report.summary.result + " / errors " + report.summary.totalErrors);

                var executable = Path.Combine(output, "Contents/MacOS/ZERO RISE – Westbruch");
                var sceneText = File.ReadAllText(scenePath);
                var release = new Release
                {
                    feature = sceneText.Contains("m_Name: Westbruch weather and daily rhythm")
                        ? "Westbruch weather and daily rhythm"
                        : "Westbruch macOS build",
                    utc = DateTime.UtcNow.ToString("O"),
                    sceneHash = AssetDatabase.GetAssetDependencyHash(scenePath).ToString(),
                    buildGuid = report.summary.guid.ToString(),
                    executableBytes = new FileInfo(executable).Length,
                    totalErrors = report.summary.totalErrors,
                    totalWarnings = report.summary.totalWarnings
                };
                using (var stream = File.OpenRead(executable))
                using (var sha = SHA256.Create())
                    release.executableSha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();

                File.WriteAllText("Builds/macOS/Westbruch-build.json", JsonUtility.ToJson(release, true));
                Debug.Log("Fresh Westbruch macOS release created: " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
