using NetDefines;
using System;
using System.Text;
using System.Xml.Linq;

namespace GameDataServer
{
    public class PlayerProfile
    {
        private int id;
        private string name;
        private string pubKey;
        private string metaData;
        private bool needsUpdate = false;

        public int Id {
            get { return id; }
            set { id = value; needsUpdate = true; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; needsUpdate = true; }
        }

        public string MetaData
        {
            get { return metaData; }
            set { metaData = value; needsUpdate = true; }
        }

        public string PublicKey
        {
            get { return pubKey; }
            set { pubKey = value; needsUpdate = true; }
        }

        public bool NeedsUpdate
        {
            get { return needsUpdate; }
        }

        public PlayerProfile(int id, string pubKey, string name, string metaData)
        {
            this.id = id;
            this.name = name;
            this.pubKey = pubKey;
            this.metaData = metaData;
        }

        public PlayerProfile Clone()
        {
            return new PlayerProfile(id, pubKey, name, metaData);
        }

        public void Reset()
        {
            needsUpdate = false;
        }

        private DateTime UnixToDate(long l)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(l).ToLocalTime();
            return dateTime;
        }

        public string TryParseMetaData(GameServer[] servers)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                XElement root = NetHelper.StringToJSON(metaData);
                sb.AppendLine("Public Key: " + pubKey);
                sb.AppendLine("Name: " + name);
                foreach(XElement child in root.Elements())
                    switch(child.Name.ToString())
                    {
                        case "creationDate":
                            DateTime dateTime = UnixToDate(long.Parse(child.Value));
                            sb.AppendLine("Creation Date: " + dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString());
                            break;
                        case "specificData":
                            foreach (XElement child2 in child.Elements())
                                switch (child2.Name.ToString())
                                {
                                    case "serverKey":
                                        string key = child2.Value;
                                        foreach(GameServer g in servers)
                                            if(g.PublicKey == key)
                                            {
                                                sb.AppendLine("Specific Data for Server \"" + g.Name + "\":");
                                                break;
                                            }
                                        break;
                                    case "lastSeen":
                                        DateTime dateTime2 = UnixToDate(long.Parse(child2.Value));
                                        sb.AppendLine("\tLast seen: " + dateTime2.ToShortDateString() + " " + dateTime2.ToShortTimeString());
                                        break;
                                    case "loginCount":
                                        sb.AppendLine("\tLogin count:" + child2.Value);
                                        break;
                                }
                            break;
                    }
            }
            catch(Exception ex)
            {
                sb.AppendLine("Error: " + ex);
            }
            return sb.ToString();
        }
    }
}
