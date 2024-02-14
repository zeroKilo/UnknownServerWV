using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using NetDefines;
using System.ComponentModel;

namespace Server
{
    public static class Backend
    {
        public static List<BackendCommand> backendCmdFilter = new List<BackendCommand>
        {
            BackendCommand.PingReq,
            BackendCommand.PlayFootStepSoundReq,
            BackendCommand.SpawnGroupItemReq,
            BackendCommand.SpawnGroupRemovalsReq,
            BackendCommand.ImpactTriggeredReq,
            BackendCommand.ShotTriggeredReq,
            BackendCommand.ReloadTriggeredReq
        };

        public static ServerMode mode;
        public static ServerModeState modeState;
        public static uint clientTeamIDCounter = 333;
        public static string currentMap = "";
        public static ushort port;
        private static readonly object _syncBroadcast = new object();
        private static readonly object _syncExit = new object();
        private static readonly object _syncRunning = new object();
        private static readonly object _syncPlayerCount = new object();
        private static readonly object _syncClientList = new object();
        private static TcpListener tcp;
        private static uint clientIDCounter = 0;
        private static uint clientTimeout = 0;
        private static List<ClientInfo> _clientList = new List<ClientInfo>();
        private static bool _running = false;
        private static bool _exit = false;
        private static uint _playersReady = 0;
        private static uint _playersWaiting = 0;
        private static uint _playersNeeded = 0;
        public static bool IsRunning
        {
            get
            {
                bool result = false;
                lock (_syncRunning)
                {
                    result = _running;
                }
                return result;
            }
            set
            {
                lock (_syncRunning)
                {
                    _running = value;
                }
            }
        }
        public static bool ShouldExit
        {
            get
            {
                bool result = false;
                lock (_syncExit)
                {
                    result = _exit;
                }
                return result;
            }
            set
            {
                lock (_syncExit)
                {
                    _exit = value;
                }
            }
        }
        public static uint PlayersReady
        {
            get
            {
                uint result = 0;
                lock (_syncPlayerCount)
                {
                    result = _playersReady;
                }
                return result;
            }
            set
            {
                lock (_syncPlayerCount)
                {
                    _playersReady = value;
                }
            }
        }
        public static uint PlayersWaiting
        {
            get
            {
                uint result = 0;
                lock (_syncPlayerCount)
                {
                    result = _playersWaiting;
                }
                return result;
            }
            set
            {
                lock (_syncPlayerCount)
                {
                    _playersWaiting = value;
                }
            }
        }
        public static uint PlayersNeeded
        {
            get
            {
                uint result = 0;
                lock (_syncPlayerCount)
                {
                    result = _playersNeeded;
                }
                return result;
            }
            set
            {
                lock (_syncPlayerCount)
                {
                    _playersNeeded = value;
                }
            }
        }
        public static List<ClientInfo> ClientList
        {
            get
            {
                List<ClientInfo> result = new List<ClientInfo>();
                lock (_syncClientList)
                {
                    result.AddRange(_clientList);
                }
                return result;
            }
            set
            {
                lock (_syncClientList)
                {
                    _clientList = value;
                }
            }
        }

        public static void Start()
        {
            if (IsRunning)
                return;
            IsRunning = true;
            ShouldExit = false;
            clientTimeout = Convert.ToUInt32(Config.settings["timeout"]);
            new Thread(ThreadMain).Start();
        }

        public static void Stop()
        {
            ShouldExit = true;
            while (true)
            {
                if (!IsRunning)
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }
        public static ushort GetNextPort()
        {
            int min = Convert.ToInt32(Config.settings["port_tcp_min"]);
            int range = Convert.ToInt32(Config.settings["port_tcp_range"]);
            return (ushort)NetHelper.rnd.Next(min, min + range);
        }

        public static void ThreadMain(object obj)
        {
            Log.Print("BACKEND main loop running...");
            if(!Config.settings.ContainsKey("port_tcp_min") || !Config.settings.ContainsKey("port_tcp_range"))
            {
                IsRunning = false;
                Log.Print("BACKEND Error : cant find settings for port_tcp!");
                Log.Print("BACKEND main loop stopped");
                return;
            }
            port = GetNextPort();
            string ip = Config.settings["bind_ip"];
            Log.Print("BACKEND Binding to " + ip + ":" + port + "...");
            tcp = new TcpListener(IPAddress.Parse(ip), port);
            tcp.Start();
            Log.Print("BACKEND Started listening");
            while (true)
            {
                if(ShouldExit)
                {
                    Log.Print("BACKEND main loop is exiting normally...");
                    break;
                }
                try
                {
                    if (!tcp.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    ClientInfo cInfo = new ClientInfo
                    {
                        tcp = tcp.AcceptTcpClient(),
                        ID = clientIDCounter++
                    };
                    cInfo.ns = cInfo.tcp.GetStream();
                    cInfo.sw.Start();
                    AddClient(cInfo);
                    new Thread(ThreadClient).Start(cInfo);
                }
                catch(Exception ex)
                {
                    if(ex is Win32Exception exception)
                    {
                        uint code = (uint)exception.HResult;
                        if (code == 0x80004005) //ignore error on tcp accept
                            break;
                    }
                    Log.Print("BACKEND mainloop error: " + ex);
                    break;
                }
            }
            try
            {
                Log.Print("BACKEND kicking clients...");
                foreach (ClientInfo client in ClientList)
                    try
                    {
                        client.tcp.Close();
                    }
                    catch
                    {
                        Log.Print("BACKEND: Error kicking client #" + client.ID);
                    }
                ClearClientList();
                Log.Print("BACKEND stopping listener...");
                if (tcp != null)
                    tcp.Stop();
            }
            catch (Exception ex)
            {
                Log.Print("BACKEND error stopping listener:" + ex.ToString());
            }
            Log.Print("BACKEND main loop stopped");
            IsRunning = false;
        }

        public static void ThreadClient(object obj)
        {
            ClientInfo cInfo = (ClientInfo)obj;
            Log.Print("BACKEND Client connected with ID=" + cInfo.ID);
            NetHelper.ServerSendCMDPacket(cInfo.ns, (uint)mode, BitConverter.GetBytes((uint)modeState), cInfo._sync);
            while (true)
            {
                if (ShouldExit)
                {
                    Log.Print("BACKEND stopped thread for client ID=" + cInfo.ID);
                    break;
                }
                if (!cInfo.cleanUp && cInfo.ns.DataAvailable)
                {
                    uint magic = NetHelper.ReadU32(cInfo.ns);
                    if (magic != NetConstants.PACKET_MAGIC)
                    {
                        Log.Print("BACKEND Error client ID=" + cInfo.ID + " send invalid message, abort");
                        break;
                    }
                    uint size = NetHelper.ReadU32(cInfo.ns);
                    if (size > 0x100000)//1MB
                    {
                        Log.Print("BACKEND Error client ID=" + cInfo.ID + " send too big message, abort");
                        break;
                    }
                    byte[] buff = new byte[size];
                    for (uint i = 0; i < size; i++)
                    {
                        int v = cInfo.ns.ReadByte();
                        if (v == -1)
                        {
                            Log.Print("BACKEND Error client ID=" + cInfo.ID + " send not enough data, abort");
                            break;
                        }
                        buff[i] = (byte)v;
                    }
                    try
                    {
                        switch (mode)
                        {
                            case ServerMode.DeathMatchMode:
                                DeathMatchMode.HandleMessage(buff, cInfo);
                                break;
                            case ServerMode.TeamDeathMatchMode:
                                TeamDeathMatchMode.HandleMessage(buff, cInfo);
                                break;
                            case ServerMode.BattleRoyaleMode:
                                BattleRoyaleMode.HandleMessage(buff, cInfo);
                                break;
                            case ServerMode.FreeExploreMode:
                                FreeExploreMode.HandleMessage(buff, cInfo);
                                break;
                        }
                    }
                    catch
                    {
                        Log.Print("BACKEND Error client ID=" + cInfo.ID + " send bad data, abort");
                        break;
                    }
                }
                if (cInfo.sw.ElapsedMilliseconds > clientTimeout)
                {
                    Log.Print("BACKEND Error client ID=" + cInfo.ID + " timed out, abort");
                    break;
                }
                if (cInfo.cleanUp)
                {
                    Log.Print("BACKEND Cleanup client ID=" + cInfo.ID + " removed");
                    break;
                }
                Thread.Sleep(1);
            }
            RemoveClient(cInfo);
            Log.Print("BACKEND Client disconnected with ID=" + cInfo.ID);
        }

        public static void AddClient(ClientInfo info)
        {
            lock (_syncClientList)
            {
                _clientList.Add(info);
            }
        }

        private static void RemoveClient(ClientInfo c)
        {
            lock (_syncBroadcast)
            {
                switch (mode)
                {
                    case ServerMode.DeathMatchMode:
                        DeathMatchMode.RemovePlayer(c.ID);
                        break;
                    case ServerMode.TeamDeathMatchMode:
                        TeamDeathMatchMode.RemovePlayer(c.ID);
                        break;
                    case ServerMode.FreeExploreMode:
                        FreeExploreMode.RemovePlayer(c.ID);
                        break;
                }
                lock (_syncClientList)
                {
                    for (int i = 0; i < _clientList.Count; i++)
                        if (_clientList[i].ID == c.ID)
                        {
                            _clientList.RemoveAt(i);
                            i--;
                        }
                }
                try
                {
                    c.tcp.Close();
                }
                catch
                {
                    Log.Print("BACKEND: Error exiting client #" + c.ID);
                }
                ObjectManager.RemoveClientObjects(c);
                BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
            }
        }

        private static void ClearClientList()
        {
            lock (_syncClientList)
            {
                _clientList.Clear();
            }
        }

        public static void BroadcastCommand(uint cmd, byte[] data)
        {
            BroadcastCommandExcept(cmd, data, null);
        }

        public static void BroadcastCommandExcept(uint cmd, byte[] data, ClientInfo except)
        {
            try
            {
                foreach (ClientInfo client in ClientList)
                {
                    if (client != except)
                        try
                        {
                            if (!client.cleanUp)
                                NetHelper.ServerSendCMDPacket(client.ns, cmd, data, client._sync);
                        }
                        catch (Exception ex)
                        {
                            Log.Print("BACKEND Broadcast failed for client with : " + ex.Message);
                            client.cleanUp = true;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Print("BACKEND Broadcast failed with : " + ex.Message);
            }
        }

        public static void BroadcastServerStateChange(ServerMode newMode, ServerModeState newState)
        {
            mode = newMode;
            modeState = newState;
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, (uint)mode);
            NetHelper.WriteU32(m, (uint)modeState);
            BroadcastCommand((uint)BackendCommand.ServerStateChangedReq, m.ToArray());
            Log.Print("BACKEND changed mode to " + mode + " : " + modeState);
        }

        public static bool ShouldFilterInLog(BackendCommand cmd)
        {
            return backendCmdFilter.Contains(cmd);
        }

        public static void HandlePing(ClientInfo client)
        {
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, PlayersReady);
            NetHelper.WriteU32(m, PlayersWaiting);
            NetHelper.WriteU32(m, PlayersNeeded);
            NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.PingRes, m.ToArray(), client._sync);
            client.sw.Restart();
        }

        public static void UpdatePlayerCounts(uint neededPlayers)
        {
            uint ready = 0;
            uint waiting = 0;
            foreach (ClientInfo c in ClientList)
                lock (c._sync)
                {
                    if (!c.isReady)
                        waiting++;
                    else
                        ready++;
                }
            PlayersNeeded = neededPlayers;
            PlayersReady = ready;
            PlayersWaiting = waiting;
        }
    }
}
