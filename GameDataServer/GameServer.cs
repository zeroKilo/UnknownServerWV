using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDataServer
{
    public class GameServer
    {
        private int id;
        private string pub_key;
        private string name;
        private string ip;
        private string portUDP;
        private string portTCP;
        private string status;
        private bool needsUpdate = false;

        public int Id
        {
            get { return id; }
            set { id = value; needsUpdate = true; }
        }

        public string PublicKey
        {
            get { return pub_key; }
            set { pub_key = value; needsUpdate = true; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; needsUpdate = true; }
        }

        public string IP
        {
            get { return ip; }
            set { ip = value; needsUpdate = true; }
        }

        public string PortUDP
        {
            get { return portUDP; }
            set { portUDP = value; needsUpdate = true; }
        }

        public string PortTCP
        {
            get { return portTCP; }
            set { portTCP = value; needsUpdate = true; }
        }

        public bool NeedsUpdate
        {
            get { return needsUpdate; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; needsUpdate = true; }
        }
        public GameServer(int id, string pub_key, string name, string ip, string portUDP, string portTCP, string status)
        {
            this.id = id;
            this.pub_key = pub_key;
            this.name = name;
            this.ip = ip;
            this.portUDP = portUDP;
            this.portTCP = portTCP;
            this.status = status;
        }

        public GameServer Clone()
        {
            return new GameServer(id, pub_key, name, ip, portUDP, portTCP, status);
        }

        public void Reset()
        {
            needsUpdate = false;
        }
    }
}
