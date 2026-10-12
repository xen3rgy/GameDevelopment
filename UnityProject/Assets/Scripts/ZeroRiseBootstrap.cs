using UnityEngine;

namespace ZeroRise
{
    public static class ZeroRiseBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<ZeroRiseGame>() != null) return;
            var root = new GameObject("ZERO // RISE | Unity prototype");
            root.AddComponent<ZeroRiseGame>();
        }
    }
}
