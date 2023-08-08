using System.IO;

namespace Server
{
    public class PlayerProfile
    {
        public string name = null;
        public string key = null;

        public PlayerProfile()
        { }
        public PlayerProfile(string path)
        {
            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                    if (line.Trim() != "" && !line.StartsWith("#") && line.Contains("="))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2)
                            continue;
                        string k = parts[0].Trim();
                        string v = parts[1].Trim();
                        if (k == "name")
                            name = v;
                        else if (k == "key")
                            key = v;
                    }
            }
            catch { }
        }
    }
}
