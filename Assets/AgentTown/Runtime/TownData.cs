using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AgentTown
{
    [Serializable] public class Memory
    {
        public string time, player, reply, source;
        public int importance = 1;
    }
    [Serializable] public class ResidentState
    {
        public string id;
        public float affinity = 20, energy = 90, mood = 80, x, y;
        public bool met;
        public List<Memory> memories = new List<Memory>();
    }
    [Serializable] public class TownSave
    {
        public int version = 1, day = 1;
        public float minute = 540, playerX = -1.25f, playerY = -3.4f;
        public int conversations, socialEvents;
        public List<string> visited = new List<string>();
        public bool introductionReward, friendshipReward;
        public List<ResidentState> residents = new List<ResidentState>();
    }
    [Serializable] public class TownPreferences
    {
        public bool useBackend, sound = true;
        public bool english;
        public string endpoint = "http://127.0.0.1:8000";
        public string playerId = "";
    }
    public sealed class Resident
    {
        public string Id, Name, Role, Personality, Expertise, Sprite, Thought, Activity, Location;
        public Color Color;
        public Vector2 Home, Destination;
        public ResidentState State;
        public TownActor Actor;
        public float DecisionTimer, BubbleTimer;
        public string Bubble = "";
        public int Schedule = -1;
        public static string Relationship(float v) => v < 20 ? "陌生" : v < 40 ? "相识" : v < 60 ? "友好" : v < 80 ? "亲密" : "挚友";
    }
    public static class TownStorage
    {
        public static string DirectoryPath => Application.persistentDataPath;
        public static string SavePath => Path.Combine(DirectoryPath, "town-save.json");
        public static string PreferencesPath => Path.Combine(DirectoryPath, "preferences.json");
        public static void AtomicWrite(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        public static T Read<T>(string path) where T : class
        {
            foreach (string file in new[] { path, path + ".bak" })
            {
                if (!File.Exists(file)) continue;
                try { var value = JsonUtility.FromJson<T>(File.ReadAllText(file)); if (value != null) return value; }
                catch (Exception ex) { Debug.LogWarning("存档读取失败，尝试备份：" + ex.GetType().Name); }
            }
            return null;
        }
    }
    [Serializable] public class ChatRequest
    {
        public string npc_name, message, player_id, location, activity, game_time;
        public string language;
        public float affinity;
    }
    [Serializable] public class ChatReply
    {
        public string npc_name, message, source, error;
        public bool success;
        public float affinity, affinity_delta;
        public string[] recalled_memories;
    }
    [Serializable] public class HealthReply { public string status, mode, model; }
    [Serializable] public class StatusReply { public AmbientReply[] residents; }
    [Serializable] public class AmbientReply { public string name, thought; }
    [Serializable] public class TownEvent { public string time, title, text; public Color color; }
}
