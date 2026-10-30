using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ZeroRise.Editor
{
    [InitializeOnLoad]
    public static class GameplayAudioActionQA
    {
        const string Pending="ZeroRise.GameAudioActionQA",Folder="QA/GameplayAudioEditor",Report=Folder+"/game-sfx-windows-validation.txt";
        static GameplayAudioActionQA(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
        public static void Run()
        {
            Directory.CreateDirectory(Folder);File.WriteAllText(Report,"RUNNING\n");
            if(EditorSceneManager.GetActiveScene().path!=WestbruchBuilder.ScenePath)EditorSceneManager.OpenScene(WestbruchBuilder.ScenePath);
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))GameplaySoundSmoke.Launch(Folder);}
        static void Poll()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||!File.Exists(Report))return;
            string text=File.ReadAllText(Report);if(!text.StartsWith("Passed:"))return;
            SessionState.SetBool(Pending,false);EditorApplication.Exit(text.StartsWith("Passed: True")?0:1);
        }
    }
}
