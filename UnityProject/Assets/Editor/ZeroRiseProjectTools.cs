#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZeroRise.EditorTools
{
    public static class ZeroRiseProjectTools
    {
        private const string Scene = "Assets/Scenes/ZeroRise.unity";

        [MenuItem("ZERO // RISE/Validate Unity Foundation")]
        public static void ValidateFoundation()
        {
            int errors=0;
            void Check(bool ok,string message)
            {
                if (!ok) { Debug.LogError("[ZERO // RISE] FAILED: "+message); errors++; }
                else Debug.Log("[ZERO // RISE] OK: "+message);
            }

            Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene)!=null, "Unity scene exists");
            Check(ZeroRiseWorldData.Roads.Length>=25, "Core + expansion road definitions");
            Check(ZeroRiseWorldData.Core.Length>=15, "Named city building footprints");
            Check(ZeroRiseWorldData.Places.Length>=25, "Known location coordinates");
            Check(GameState.New(false).money==0, "Story starts with zero money");
            Check(GameState.New(true).money==10000000, "Sandbox starts with 100,000 EUR");
            Check(Math.Abs(GameState.New(false).minute-480)<.001f, "Game starts at 08:00");
            Check(ZeroRiseWorldData.MinX < -200 && ZeroRiseWorldData.MaxX>400,
                "Source city bounds are represented");
            var test=GameState.New(false);
            var original=JsonUtility.ToJson(test);
            var copy=JsonUtility.FromJson<GameState>(original);
            Check(copy!=null && copy.money==0 && copy.needs!=null, "Native save JSON roundtrip");
            Debug.Log(errors==0
                ? "[ZERO // RISE] Foundation validation PASSED. Full gameplay/editor compile still requires running Unity."
                : "[ZERO // RISE] Foundation validation FAILED: "+errors+" issue(s).");
        }

        [MenuItem("ZERO // RISE/Rebuild Empty Bootstrap Scene")]
        public static void RebuildScene()
        {
            if (!EditorUtility.DisplayDialog("ZERO // RISE",
                "Rebuild the empty scene? The native game is instantiated by ZeroRiseBootstrap at Play.",
                "Rebuild", "Cancel")) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,Scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            AssetDatabase.Refresh();
            Debug.Log("[ZERO // RISE] Bootstrap scene rebuilt. Press Play to run.");
        }

        [MenuItem("ZERO // RISE/Import Browser Save (BASIC FIELDS ONLY)")]
        public static void ImportBrowserSave()
        {
            if (!EditorUtility.DisplayDialog("Limited import",
                "Only money, bank/debt, day/time, basic needs, quest index and outdoor position can transfer. "+
                "Browser inventories, owned vehicles, housing, companies, NPC relationships and interiors are NOT migrated. "+
                "A separate Unity save will be written. The original browser save remains untouched.",
                "Continue","Cancel")) return;
            var path=EditorUtility.OpenFilePanel("Select exported ZERO // RISE browser JSON","","json");
            if (string.IsNullOrEmpty(path))return;
            try
            {
                var state=BrowserSaveBridge.ImportBasics(File.ReadAllText(path));
                ZeroRiseSave.Write(state);
                Debug.Log("[ZERO // RISE] Partial import written to "+ZeroRiseSave.SavePath);
                EditorUtility.DisplayDialog("Import completed",
                    "Basic save fields imported. Press Play and choose Weiterspielen. Other systems were not migrated.", "OK");
            }
            catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("Import failed",e.Message,"OK");}
        }
    }
}
#endif
