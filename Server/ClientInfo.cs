using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Server
{
    public class ClientInfo
    {
        public uint ID;
        public uint teamID;
        public bool isTeamReady = false;
        public TcpClient tcp;
        public IPEndPoint udp;
        public NetworkStream ns;
        public Stopwatch sw = new Stopwatch();
        public PlayerProfile profile;
        public List<uint> objIDs = new List<uint>();
        public readonly object _sync = new object();
        public bool isReady = false;
        public bool cleanUp = false;
    }
}
