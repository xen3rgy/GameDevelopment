using System;
using System.IO;
using UnityEngine;

namespace ZeroRise
{
    [Serializable]
    public class NeedState
    {
        public float health = 100f, hunger = 78f, thirst = 75f, energy = 85f, hygiene = 65f, stress = 12f;
        public void Normalize()
        {
            health = Mathf.Clamp(health, 0, 100); hunger = Mathf.Clamp(hunger, 0, 100);
            thirst = Mathf.Clamp(thirst, 0, 100); energy = Mathf.Clamp(energy, 0, 100);
            hygiene = Mathf.Clamp(hygiene, 0, 100); stress = Mathf.Clamp(stress, 0, 100);
        }
    }

    [Serializable]
    public class GameState
    {
        public int schema = 1;
        public string mode = "story";
        public int money = 0; // Integer CENTS, as in the browser game.
        public int bank = 0, debt = 0, day = 1;
        public float minute = 480f;
        public float x = -175f, z = -8f, yaw = 0f;
        public NeedState needs = new NeedState();
        public int bottles = 0, returned = 0, jobs = 0, earned = 0, food = 0, water = 0;
        public bool hasHome = false;
        public int sleepCount = 0;
        public bool hasVehicle = false, riding = false;
        public string vehicleType = "car";
        public float vehicleX = 73f, vehicleZ = 9f, vehicleYaw = 180f, vehicleFuel = 45f, vehicleCondition = 100f;
        public int[] collectedBottleIds = new int[0];
        public int questIndex = 0;
        public bool courierActive = false;
        public string courierDestination = "";
        public float playedSeconds = 0;

        public static GameState New(bool sandbox)
        {
            var s = new GameState { mode = sandbox ? "sandbox" : "story", money = sandbox ? 10000000 : 0 };
            if (sandbox) { s.x = -27; s.z = -4; }
            return s;
        }
        public void Normalize()
        {
            if (needs == null) needs = new NeedState();
            needs.Normalize(); money = Mathf.Max(0, money); bank = Mathf.Max(0, bank);
            debt = Mathf.Max(0, debt); day = Mathf.Max(1, day);
            minute = Mathf.Repeat(minute, 1440f);
            bottles = Mathf.Clamp(bottles, 0, 10000);
            collectedBottleIds ??= new int[0];
            if (string.IsNullOrEmpty(mode)) mode = "story";
            if (string.IsNullOrEmpty(vehicleType)) vehicleType = "car";
        }
    }

    public static class ZeroRiseSave
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "zero-rise-unity-v1.json");

        public static void Write(GameState state)
        {
            if (state == null) return;
            state.Normalize();
            string target = SavePath, temp = target + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(state, true));
            if (File.Exists(target)) File.Copy(target, target + ".bak", true);
            if (File.Exists(target)) File.Delete(target);
            File.Move(temp, target);
        }
        public static GameState Read()
        {
            foreach (string path in new[] { SavePath, SavePath + ".bak" })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var state = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                    if (state == null || state.schema != 1 || (state.mode != "story" && state.mode != "sandbox")) continue;
                    state.Normalize();
                    return state;
                }
                catch (Exception e) { Debug.LogWarning("Save read failed: " + e.Message); }
            }
            return null;
        }
    }

    // Deliberately LIMITED transfer: browser JS state is not a native Unity save.
    // Import selected portable fields; never overwrite/delete the browser original.
    [Serializable]
    internal class BrowserPoint { public float x, z; }
    [Serializable]
    internal class BrowserSnapshot
    {
        public string mode;
        public int money, bank, debt, day, quest;
        public float minute;
        public BrowserPoint position;
        public NeedState needs;
    }
    public static class BrowserSaveBridge
    {
        public static GameState ImportBasics(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty browser save");
            var src = JsonUtility.FromJson<BrowserSnapshot>(json);
            if (src == null || (src.mode != "story" && src.mode != "sandbox"))
                throw new ArgumentException("Not a ZERO // RISE browser save");
            var state = GameState.New(src.mode == "sandbox");
            state.money = Mathf.Max(0, src.money); state.bank = Mathf.Max(0, src.bank);
            state.debt = Mathf.Max(0, src.debt); state.day = Mathf.Max(1, src.day);
            state.minute = Mathf.Repeat(src.minute, 1440);
            state.questIndex = Mathf.Max(0, src.quest);
            if (src.position != null) { state.x = src.position.x; state.z = src.position.z; }
            if (src.needs != null) state.needs = src.needs;
            state.Normalize(); return state;
        }
    }
}
