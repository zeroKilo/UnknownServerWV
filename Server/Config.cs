using System.IO;
using System.Collections.Generic;
using System.Text;
using NetDefines;
using System.Security.Cryptography;
using System.Net.Sockets;
using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Server
{
    public static class Config
    {
        public static Dictionary<string, string> settings = new Dictionary<string, string>();
        public static List<PlayerProfile> profiles = new List<PlayerProfile>();
        public static RSAParameters rsaParams;
        public static string pubKey, privKey;

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
            LoadServerKeys();
            ReloadPlayerProfiles();
            Log.Print("CONFIG Player profiles loaded:");
            foreach (PlayerProfile p in profiles)
                Log.Print(" - " + p.publicKey.Substring(23, 10) + "... " + p.name);
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

        public static void ReloadPlayerProfiles()
        {
            profiles = new List<PlayerProfile>();
            if (!settings.ContainsKey("gds_ip") || !settings.ContainsKey("gds_port"))
                return;
            try
            {
                byte[] signature = NetHelper.MakeSignature(new byte[0], rsaParams);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("GET /profile_list HTTP/1.1");
                sb.AppendLine("Signature: " + NetHelper.MakeHexString(signature));
                sb.AppendLine("Public-Key: " + pubKey);
                sb.AppendLine();
                TcpClient client = new TcpClient(settings["gds_ip"], Convert.ToInt32(settings["gds_port"]));
                NetworkStream ns = client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
                ns.Write(data, 0, data.Length);
                byte[] response = NetHelper.ReadAll(ns);
                client.Close();
                string content = Encoding.ASCII.GetString(response);
                StringReader sr = new StringReader(content);
                while (sr.ReadLine() != "") ;
                XElement json = NetHelper.StringToJSON(sr.ReadToEnd());
                XElement list = json.XPathSelectElement("profiles");
                if (list != null)
                    foreach (XElement entry in list.Elements())
                    {
                        XElement name = entry.XPathSelectElement("name");
                        XElement public_key = entry.XPathSelectElement("public_key");
                        if (name != null && public_key != null)
                        {
                            PlayerProfile p = new PlayerProfile();
                            p.name = name.Value;
                            p.publicKey = public_key.Value;
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
