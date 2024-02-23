using System.IO;
using System.Collections.Generic;
using System.Text;
using NetDefines;
using System.Security.Cryptography;
using System;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Net.Http;

namespace Server
{
    public static class Config
    {
        public static Dictionary<string, string> settings = new Dictionary<string, string>();
        public static List<PlayerProfile> profiles = new List<PlayerProfile>();
        public static RSAParameters rsaParams;
        public static string pubKey, privKey;
        public static string itemSettingsJson = "";
        public static uint playerProfileUpdateCounter = 0;

        public static void Init()
        {
            if(!File.Exists("config.txt"))
            {
                Log.Print("CONFIG Error : config.txt not found!");
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
            Log.Print("CONFIG settings loaded:");
            foreach (KeyValuePair<string, string> pair in settings)
                Log.Print(" - " + pair.Key + " = " + pair.Value);
            HttpServerWV.secure = settings["use_https"] == "1";
            LoadItemSettings();
            LoadServerKeys();
            StatusServer.Init();
            ReloadPlayerProfiles();
            Log.Print("CONFIG Player profiles loaded:");
            foreach (PlayerProfile p in profiles)
                Log.Print(" - " + p.publicKey.Substring(23, 10) + "... " + p.name);
        }

        public static void LoadItemSettings()
        {
            try
            {
                itemSettingsJson = File.ReadAllText("item_settings.json");
                Log.Print("CONFIG Item settings loaded");
            }
            catch (Exception ex)
            {
                Log.Print("CONFIG Error loading item settings: " + ex.Message + "\n" + ex.InnerException.Message);
            }
        }

        private static void LoadServerKeys()
        {
            if (!File.Exists("server.keys"))
            {
                Log.Print("CONFIG Error : cant find server.keys!");
                string[] keys = NetHelper.MakeSigningKeys();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("public=" + keys[0]);
                sb.AppendLine("private=" + keys[1]);
                File.WriteAllText("server.keys", sb.ToString());
            }
            string[] lines = File.ReadAllLines("server.keys");
            pubKey = lines[0].Split('=')[1].Trim();
            privKey = lines[1].Split('=')[1].Trim();
            rsaParams = NetHelper.LoadSigningKeys(pubKey, privKey);
        }

        public static string GetGdsBaseAddress()
        {
            return settings["gds_ip"] + ":" + settings["gds_port"];
        }

        public static void ReloadPlayerProfiles()
        {
            profiles = new List<PlayerProfile>();
            if (!settings.ContainsKey("gds_ip") || !settings.ContainsKey("gds_port"))
                return;
            try
            {
                string reply = HttpServerWV.GetResponseData(HttpServerWV.SendSignedRestRequest(rsaParams, pubKey, HttpMethod.Get, GetGdsBaseAddress(), "/profile_list", ""));
                XElement json = NetHelper.StringToJSON(reply);
                XElement list = json.XPathSelectElement("profiles");
                if (list != null)
                    foreach (XElement entry in list.Elements())
                    {
                        XElement id = entry.XPathSelectElement("id");
                        XElement name = entry.XPathSelectElement("name");
                        XElement public_key = entry.XPathSelectElement("public_key");
                        if (name != null && public_key != null)
                        {
                            PlayerProfile p = new PlayerProfile
                            {
                                id = int.Parse(id.Value),
                                name = name.Value,
                                publicKey = public_key.Value
                            };
                            profiles.Add(p);
                        }
                    }
            } 
            catch
            {
                Log.Print("CONFIG Error : failed to receive player profiles!");
            }
        }
    }
}
