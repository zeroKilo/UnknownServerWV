using System.Collections.Generic;
using System.IO;

namespace GameDataServer
{
    public static class Config
    {
        public static Dictionary<string, string> settings;

        public static void Init()
        {
            if (!File.Exists("config.txt"))
            {
                Log.Print("Error : config.txt not found!");
                return;
            }
            settings = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines("config.txt");
            foreach (string line in lines)
                if (line.Trim() != "" && !line.StartsWith("#") && line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;
                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();
                    if (!settings.ContainsKey(key))
                        settings.Add(key, value);
                }
            Log.Print("Config loaded:");
            foreach (KeyValuePair<string, string> pair in settings)
                Log.Print(" - " + pair.Key + " = " + pair.Value);
        }
    }
}
