using NetDefines;
using NetDefines.StateDefines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using static NetDefines.NetObjVehicleState;

namespace Server
{
    public static class EnvServer
    {
        public enum State
        {
            Error,
            Connecting,
            MainLoop
        }

        public static string currentMapName = "";
        public static State state = State.Error;
        public static bool isMapLoaded;

        private static readonly object _syncExitTCP = new object();
        private static readonly object _syncRunningTCP = new object();
        private static readonly object _syncExitUDP = new object();
        private static readonly object _syncRunningUDP = new object();
        private static readonly object _syncPacket = new object();
        private static bool _runningTCP = false;
        private static bool _exitTCP = false;
        private static bool _runningUDP = false;
        private static bool _exitUDP = false;
        private static bool env_enabled;
        private static string env_ip;
        private static ushort env_port_tcp;
        private static ushort env_port_udp_tx;
        private static ushort env_port_udp_rx;
        private static TcpClient tcpClient;
        private static UdpClient udpSender;
        private static UdpClient udpReceiver;
        private static Stopwatch pingSW = new Stopwatch();
        private static Stream tcpStream;
        public static bool IsRunningTCP
        {
            get
            {
                bool result = false;
                lock (_syncRunningTCP)
                {
                    result = _runningTCP;
                }
                return result;
            }
            set
            {
                lock (_syncRunningTCP)
                {
                    _runningTCP = value;
                }
            }
        }
        public static bool ShouldExitTCP
        {
            get
            {
                bool result = false;
                lock (_syncExitTCP)
                {
                    result = _exitTCP;
                }
                return result;
            }
            set
            {
                lock (_syncExitTCP)
                {
                    _exitTCP = value;
                }
            }
        }
        public static bool IsRunningUDP
        {
            get
            {
                bool result = false;
                lock (_syncRunningUDP)
                {
                    result = _runningUDP;
                }
                return result;
            }
            set
            {
                lock (_syncRunningUDP)
                {
                    _runningUDP = value;
                }
            }
        }
        public static bool ShouldExitUDP
        {
            get
            {
                bool result = false;
                lock (_syncExitUDP)
                {
                    result = _exitUDP;
                }
                return result;
            }
            set
            {
                lock (_syncExitUDP)
                {
                    _exitUDP = value;
                }
            }
        }

        public static void Init()
        {
            string[] keys = { "env_enabled", "env_ip", "env_port_tcp", "env_port_udp_tx", "env_port_udp_rx" };
            foreach (string key in keys)
                if (!Config.settings.ContainsKey(key))
                {
                    Log.Print("ENVSERVER Error : cant find setting " + key + "!");
                    Log.Print("ENVSERVER init failed!");
                    env_enabled = false;
                    return;
                }
            env_enabled = int.Parse(Config.settings[keys[0]]) == 1;
            env_ip = Config.settings[keys[1]];
            env_port_tcp = ushort.Parse(Config.settings[keys[2]]);
            env_port_udp_tx = ushort.Parse(Config.settings[keys[3]]);
            env_port_udp_rx = ushort.Parse(Config.settings[keys[4]]);
        }

        public static void Start()
        {
            if(!env_enabled)
            {
                Log.Print("ENVSERVER is disabled!");
                return;
            }
            if (IsRunningTCP)
                return;
            IsRunningTCP = IsRunningUDP = true;
            ShouldExitTCP = ShouldExitUDP = false;
            state = State.Connecting;
            currentMapName = "";
            isMapLoaded = false;
            new Thread(ThreadMainTCP).Start();
            new Thread(ThreadMainUDP).Start();
        }

        public static void Stop()
        {
            ShouldExitTCP = true;
            ShouldExitUDP = true;
            while (true)
            {
                if (!IsRunningTCP && !IsRunningUDP)
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static void ThreadMainTCP(object obj)
        {
            Log.Print("ENVSERVER main tcp loop running...");
            while (true)
            {
                try
                {
                    if (ShouldExitTCP)
                    {
                        Log.Print("ENVSERVER main tcp loop is exiting...");
                        break;
                    }
                    switch (state)
                    {
                        case State.Error:
                            Log.Print("ENVSERVER entered error state!");
                            ShouldExitTCP = true;
                            break;
                        case State.Connecting:
                            if (tcpClient != null)
                                tcpClient.Close();
                            tcpClient = new TcpClient(env_ip, env_port_tcp);
                            tcpStream = tcpClient.GetStream();
                            pingSW.Restart();
                            Log.Print("ENVSERVER connected");
                            state = State.MainLoop;
                            break;
                        case State.MainLoop:
                            if (tcpClient.Available == 0)
                            {
                                if (pingSW.ElapsedMilliseconds > 1000)
                                {
                                    NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.PingReq, new byte[0]);
                                    pingSW.Restart();
                                }
                                Thread.Sleep(100);
                            }
                            else
                            {
                                byte[] packet = ReadPacket();
                                if (packet == null)
                                {
                                    Log.Print("ENVSERVER Fatal error, unable to read packet, shutting down");
                                    state = State.Error;
                                    break;
                                }
                                HandlePacket(packet);
                            }
                            break;

                    }
                }
                catch (Exception ex)
                {
                    Log.Print("ENVSERVER Fatal error, shutting down:\n" + NetHelper.GetExceptionDetails(ex));
                    state = State.Error;
                    break;
                }
            }
            Log.Print("ENVSERVER main tcp loop stopped");
            IsRunningTCP = false;
            ShouldExitUDP = true;
        }

        public static void ThreadMainUDP(object obj)
        {
            Log.Print("ENVSERVER main udp loop running...");
            udpSender = new UdpClient();
            udpSender.Connect(new IPEndPoint(IPAddress.Parse(env_ip), env_port_udp_tx));
            if (udpReceiver != null)
                udpReceiver.Close();
            udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Parse(env_ip), env_port_udp_rx));
            while (true)
            {
                if (ShouldExitUDP)
                {
                    Log.Print("ENVSERVER main udp loop is exiting...");
                    break;
                }
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    if (udpReceiver.Available > 0)
                    {
                        byte[] data = udpReceiver.Receive(ref sender);
                        lock (MainServer._syncMain)
                        {
                            MainServer.dataCounter += data.Length;
                        }
                        try
                        {
                            MainServer.ProcessData(sender, data);
                        }
                        catch { }
                    }
                    else
                        Thread.Sleep(1);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset || (uint)ex.HResult == 0x80004005)
                    {
                        lock (MainServer._syncMain)
                        {
                            MainServer.errorCounter++;
                        }
                        if (udpReceiver != null)
                            udpReceiver.Close();
                        udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Parse(env_ip), env_port_udp_rx));
                    }
                    else
                    {
                        Log.Print("ENVSERVER SocketException error:\n" + NetHelper.GetExceptionDetails(ex));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Print("ENVSERVER Exception error:\n" + NetHelper.GetExceptionDetails(ex));
                    break;
                }
            }
            Log.Print("ENVSERVER main udp loop stopped");
            IsRunningUDP = false;
        }

        private static void HandlePacket(byte[] data)
        {
            uint who, byWho;
            List<uint> hitList;
            byte[] buff;
            NetObjVehicleState netVehicle;
            MemoryStream m = new MemoryStream(data);
            MemoryStream mRes = new MemoryStream();
            EnvServerCommand cmd = (EnvServerCommand)NetHelper.ReadU32(m);
            switch (cmd)
            {
                //Requests
                case EnvServerCommand.MapLoadedReq:
                    Log.Print("ENVSERVER Loading map done");
                    isMapLoaded = true;
                    NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.MapLoadedRes, new byte[0]);
                    SendSetBotCountRequest();
                    break;
                case EnvServerCommand.SpawnVehiclesReq:
                    Log.Print("ENVSERVER Got spawn request for vehicles");
                    uint count = NetHelper.ReadU32(m);
                    NetHelper.WriteU32(mRes, count);
                    for(int i = 0; i < count; i++)
                    {
                        uint id = NetHelper.ReadU32(m);
                        NetHelper.WriteU32(mRes, id);
                        VehiclePrefab vehicle = (VehiclePrefab)NetHelper.ReadU32(m);
                        Log.Print(" -> ID=" + id + "\tVehicle=" + vehicle);
                        netVehicle = CreateVehicleNetObj(vehicle, prefabTypeMap[vehicle]);
                        ObjectManager.Add(netVehicle);
                        buff = netVehicle.Create(true);
                        mRes.Write(buff,0, buff.Length);
                    }
                    NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.SpawnVehiclesRes, mRes.ToArray());
                    break;
                case EnvServerCommand.ShotTriggeredReq:
                    Backend.BroadcastCommand((uint)BackendCommand.ShotTriggeredReq, NetHelper.CopyCommandData(m));
                    break;
                case EnvServerCommand.PlayerHitReq:
                    who = NetHelper.ReadU32(m);
                    byWho = NetHelper.ReadU32(m);
                    HitLocation loc = (HitLocation)NetHelper.ReadU32(m);
                    foreach (ClientInfo other in Backend.ClientList)
                        if (other.objIDs.Contains(who))
                        {
                            m = new MemoryStream();
                            NetHelper.WriteU32(m, (uint)loc);
                            NetHelper.WriteU32(m, byWho);
                            NetHelper.ServerSendCMDPacket(other.ns, (uint)BackendCommand.PlayerHitReq, m.ToArray(), other._sync);
                            break;
                        }
                    break;
                case EnvServerCommand.ImpactTriggeredReq:
                    Backend.BroadcastCommand((uint)BackendCommand.ImpactTriggeredReq, NetHelper.CopyCommandData(m));
                    break;
                case EnvServerCommand.InventoryUpdateReq:
                    who = NetHelper.ReadU32(m);
                    NetState_Inventory inv = new NetState_Inventory();
                    inv.Read(m);
                    Backend.UpdatePlayerInventory(who, inv);
                    buff = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommand((uint)BackendCommand.InventoryUpdateReq, buff);
                    break;
                case EnvServerCommand.PlayerDiedReq:
                    who = NetHelper.ReadU32(m);
                    count = NetHelper.ReadU32(m);
                    hitList = new List<uint>();
                    for (int i = 0; i < count; i++)
                    {
                        uint u = NetHelper.ReadU32(m);
                        if (!hitList.Contains(u))
                            hitList.Add(u);
                    }
                    if (hitList.Count == 0)
                        break;
                    byWho = hitList.Last();
                    switch (Backend.mode)
                    {
                        case ServerMode.TeamDeathMatchMode:
                            for (int i = 0; i < 2; i++)
                            {
                                PlayerScoreEntry[] scores = TeamDeathMatchServerLogic.playerScoresPerLocation[i];
                                for (int j = 0; j < scores.Length; j++)
                                {
                                    PlayerScoreEntry entry = scores[j];
                                    if (entry.netObjID == who)
                                        entry.deaths++;
                                    else if (entry.netObjID == byWho)
                                        entry.kills++;
                                    else if (hitList.Contains(entry.netObjID))
                                        entry.assists++;
                                    scores[j] = entry;
                                }
                                TeamDeathMatchServerLogic.playerScoresPerLocation[i] = scores;
                            }
                            TeamDeathMatchMode.SendScoreBoardUpdate();
                            break;
                    }
                    NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.PlayerDiedRes, new byte[0]);
                    break;
                case EnvServerCommand.ReloadTriggeredReq:
                    Backend.BroadcastCommand((uint)BackendCommand.ReloadTriggeredReq, NetHelper.CopyCommandData(m));
                    NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.ReloadTriggeredRes, new byte[0]);
                    break;
                //Responses
                case EnvServerCommand.PingRes:
                    pingSW.Restart();
                    break;
                case EnvServerCommand.LoadMapRes:
                case EnvServerCommand.SpawnPlayerRes:
                case EnvServerCommand.ChangeControlVehicleRes:
                case EnvServerCommand.ChangeVehicleSeatIDRes:
                case EnvServerCommand.DeleteObjectsRes:
                case EnvServerCommand.SetBotCountRes:
                case EnvServerCommand.BackendStateChangedRes:
                case EnvServerCommand.ScoresUpdateRes:
                    break;
                default:
                    Log.Print("ENVSERVER Got unknown comman, ignoring (0x" + cmd.ToString("X8") + ")");
                    break;
            }
        }

        private static NetObjVehicleState CreateVehicleNetObj(VehiclePrefab prefab, VehicleType type)
        {
            NetObjVehicleState vehicleObject = new NetObjVehicleState
            {
                ID = ObjectManager.GetNextID(),
                accessKey = ObjectManager.MakeNewAccessKey()
            };
            vehicleObject.SetVehiclePrefab(prefab);
            vehicleObject.SetVehicleType(type);
            return vehicleObject;
        }

        public static void SendUDPPacket(byte[] data)
        {
            if (state != State.MainLoop)
                return;
            udpSender.Send(data, data.Length);
        }

        public static void SendLoadMapRequest(string name)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send map load request when not connected");
                return;
            }
            Log.Print("ENVSERVER Loading map '" + name + "'...");
            MemoryStream m = new MemoryStream();
            NetHelper.WriteCString(m, name);
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.LoadMapReq, m.ToArray());
            SendBackendStateChangedRequest();
            currentMapName = name;
            isMapLoaded = false;
        }

        public static void SendBackendStateChangedRequest()
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send backend state change request when not connected");
                return;
            }
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, (uint)Backend.mode);
            NetHelper.WriteU32(m, (uint)Backend.modeState);
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.BackendStateChangedReq, m.ToArray());
        }

        public static void SendPlayerSpawnRequest(byte[] data)
        {
            if(state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send player spawn request when not connected");
                return;
            }
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.SpawnPlayerReq, data);
        }

        public static void SendPlayerHitRequest(byte[] data)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send player hit request when not connected");
                return;
            }
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.PlayerHitReq, data);
        }

        public static void SendChangeControlVehicleRequest(uint vehicleID, uint accessKey, bool takeControl, bool neutral)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send change control vehicle request when not connected");
                return;
            }
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, vehicleID);
            NetHelper.WriteU32(m, accessKey);
            m.WriteByte((byte)(takeControl ? 1 : 0));
            m.WriteByte((byte)(neutral ? 1 : 0));
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.ChangeControlVehicleReq, m.ToArray());
        }

        public static void SendChangeVehicleSeatID(uint vehicleID, uint playerID, int seatIdx)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send vehicle seat id change request when not connected");
                return;
            }
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, vehicleID);
            NetHelper.WriteU32(m, playerID);
            NetHelper.WriteU32(m, (uint)seatIdx);
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.ChangeVehicleSeatIDReq, m.ToArray());
        }

        public static void SendObjectDeleteRequest(byte[] data)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send objects delete request when not connected");
                return;
            }
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.DeleteObjectsReq, data);
        }

        public static void SendScoresUpdateRequest(byte[] data)
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send scores update request when not connected");
                return;
            }
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.ScoresUpdateReq, data);
        }

        public static void SendSetBotCountRequest()
        {
            if (state != State.MainLoop)
            {
                Log.Print("ENVSERVER Error: Tried to send set bot count request when not connected");
                return;
            }
            int botCount;
            switch (Backend.mode)
            {
                case ServerMode.BattleRoyaleMode:
                    botCount = BattleRoyaleServerLogic.botCount;
                    break;
                case ServerMode.DeathMatchMode:
                    botCount = DeathMatchServerLogic.botCount;
                    break;
                case ServerMode.TeamDeathMatchMode:
                    botCount = TeamDeathMatchServerLogic.botCount;
                    break;
                default:
                    botCount = 0;
                    break;
            }
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, (uint)botCount);
            List<uint> ids = new List<uint>();
            for (int i = 0; i < botCount; i++)
            {
                NetObjPlayerState netPlayer = new NetObjPlayerState
                {
                    ID = ObjectManager.GetNextID(),
                    accessKey = ObjectManager.MakeNewAccessKey()
                };
                ObjectManager.Add(netPlayer);
                NetHelper.WriteU32(m, netPlayer.ID);
                NetHelper.WriteU32(m, netPlayer.accessKey);
                ids.Add(netPlayer.ID);
            }
            RefreshScoreboardsWithBots(botCount, ids);
            NetHelper.ClientSendCMDPacket(tcpStream, (uint)EnvServerCommand.SetBotCountReq, m.ToArray());
            switch (Backend.mode)
            {
                case ServerMode.TeamDeathMatchMode:
                    TeamDeathMatchMode.SendScoreBoardUpdate();
                    break;
                default:
                    break;
            }
        }

        private static void RefreshScoreboardsWithBots(int botCount, List<uint> ids)
        {
            switch(Backend.mode)
            {
                case ServerMode.TeamDeathMatchMode:
                    int countA = botCount / 2;
                    int countB = botCount - countA;
                    List<PlayerScoreEntry> sideScores = new List<PlayerScoreEntry>();
                    for (int i = 0; i < countA; i++)
                        sideScores.Add(new PlayerScoreEntry(ids[i], true));
                    TeamDeathMatchServerLogic.playerScoresPerLocation[0] = sideScores.ToArray();
                    sideScores = new List<PlayerScoreEntry>();
                    for (int i = 0; i < countB; i++)
                        sideScores.Add(new PlayerScoreEntry(ids[i + countA], true));
                    TeamDeathMatchServerLogic.playerScoresPerLocation[1] = sideScores.ToArray();
                    break;
                default:
                    break;
            }
        }

        private static byte[] ReadPacket()
        {
            uint magic = NetHelper.ReadU32(tcpStream);
            if (magic != NetConstants.PACKET_MAGIC)
            {
                Log.Print("ENVSERVER Error client send invalid message, abort");
                return null;
            }
            uint size = NetHelper.ReadU32(tcpStream);
            if (size > 0x100000)//1MB
            {
                Log.Print("ENVSERVER Error client send too big message, abort");
                return null;
            }
            byte[] buff = new byte[size];
            for (uint i = 0; i < size; i++)
            {
                int v = tcpStream.ReadByte();
                if (v == -1)
                {
                    Log.Print("ENVSERVER Error client send not enough data, abort");
                    break;
                }
                buff[i] = (byte)v;
            }
            return buff;
        }
    }
}
