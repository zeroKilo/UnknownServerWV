using System.IO;
using System.Collections.Generic;

namespace Server
{
    public static class Config
    {
        public static Dictionary<string, string> settings = new Dictionary<string, string>();
        public static List<PlayerProfile> profiles = new List<PlayerProfile>();

        public static void Init()
        {
            if(!File.Exists("config.txt"))
            {
                Log.Print("Error : config.txt not found!");
                return;
            }
            string[] lines = File.ReadAllLines("config.txt");
            foreach(string line in lines)
                if(line.Trim() != "" && !line.StartsWith("#") && line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    if (!settings.ContainsKey(key))
                        settings.Add(key, value);
                }
            Log.Print("Config loaded:");
            foreach (KeyValuePair<string, string> pair in settings)
                Log.Print(" - " + pair.Key + " = " + pair.Value);
            if (!settings.ContainsKey("path_profiles"))
                return;
            string pathProfiles = settings["path_profiles"];
            if (!pathProfiles.EndsWith("\\"))
                pathProfiles += "\\";
            if(!Directory.Exists(pathProfiles))
            {
                Log.Print("Error : cant find player profile folder");
                return;
            }
            string[] files = Directory.GetFiles("profiles");
            foreach(string file in files)
            {
                PlayerProfile p = new PlayerProfile(file);
                if (p.name != null && p.key != null)
                    profiles.Add(p);
            }
            Log.Print("Player profiles loaded:");
            foreach (PlayerProfile p in profiles)
                Log.Print(" - " + p.key + " " + p.name);
        }
    }
}
