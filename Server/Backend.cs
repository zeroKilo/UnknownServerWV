using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using NetDefines;

namespace Server
{
    public static class Backend
    {
        public static ServerMode mode;
        public static ServerModeState modeState;
        public static List<ClientInfo> clientList = new List<ClientInfo>();
        public static uint clientTeamIDCounter = 333;
        public static string currentMap = "";
        private static readonly object _sync = new object();
        private static readonly object _syncBroadcast = new object();
        private static TcpListener tcp;
        private static bool _running = false;
        private static bool _exit = false;
        private static uint clientIDCounter = 0;
        private static uint clientTimeout = 0;

        public static void Start()
        {
            if (isRunning())
                return;
            lock (_sync)
            {
                _exit = false;
                _running = true;
            }
            clientTimeout = Convert.ToUInt32(Config.settings["timeout"]);
            new Thread(tMain).Start();
        }

        public static bool isRunning()
        {
            lock(_sync)
            {
                return _running;
            }
        }

        public static void tMain(object obj)
        {
            Log.Print("BACKEND main loop running...");
            if(!Config.settings.ContainsKey("port_tcp"))
            {
                _running = false;
                Log.Print("BACKEND Error : cant find setting port_tcp!");
                Log.Print("BACKEND main loop stopped");
                return;
            }
            ushort port = Convert.ToUInt16(Config.settings["port_tcp"]);
            string ip = Config.settings["bind_ip"];
            Log.Print("BACKEND Binding to " + ip + ":" + port + "...");
            tcp = new TcpListener(IPAddress.Parse(ip), port);
            tcp.Start();
            Log.Print("BACKEND Started listening");
            while (true)
            {
                lock (_sync)
                {
                    if (_exit)
                    {
                        Log.Print("BACKEND main loop is exiting...");
                        break;
                    }
                }
                try
                {
                    ClientInfo cInfo = new ClientInfo();
                    cInfo.tcp = tcp.AcceptTcpClient();
                    cInfo.ns = cInfo.tcp.GetStream();
                    cInfo.sw.Start();
                    cInfo.ID = clientIDCounter++;
                    clientList.Add(cInfo);
                    new Thread(tClient).Start(cInfo);
                }
                catch
                {
                    Log.Print("BACKEND main loop is exiting...");
                    break;
                }
            }
            try
            {
                tcp.Stop();
            }
            catch { }
            Log.Print("BACKEND main loop stopped");
            _running = false;
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
                for (int i = 0; i < clientList.Count; i++)
                    if (clientList[i].ID == c.ID)
                    {
                        clientList.RemoveAt(i);
                        break;
                    }
                ObjectManager.RemoveClientObjects(c);
                BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
            }
        }

        public static void tClient(object obj)
        {
            ClientInfo cInfo = (ClientInfo)obj;
            Log.Print("BACKEND Client connected with ID=" + cInfo.ID);
            NetHelper.ServerSendCMDPacket(cInfo.ns, (uint)mode, BitConverter.GetBytes((uint)modeState), cInfo._sync); 
            while (true)
            {
                bool exit = false;
                lock (_sync)
                {
                    exit = _exit;
                }
                if(exit)
                {
                    Log.Print("BACKEND : stopped thread for client ID=" + cInfo.ID);
                    break;
                }
                if (!cInfo.cleanUp && cInfo.ns.DataAvailable)
                {
                    uint magic = NetHelper.ReadU32(cInfo.ns);
                    if(magic != NetConstants.PACKET_MAGIC)
                    {
                        Log.Print("BACKEND Error : client ID=" + cInfo.ID + " send invalid message, abort");
                        break;
                    }
                    uint size = NetHelper.ReadU32(cInfo.ns);
                    if(size > 0x100000)//1MB
                    {
                        Log.Print("BACKEND Error : client ID=" + cInfo.ID + " send too big message, abort");
                        break;
                    }
                    byte[] buff = new byte[size];
                    for(uint i = 0; i < size; i++)
                    {
                        int v = cInfo.ns.ReadByte();
                        if (v == -1)
                        {
                            Log.Print("BACKEND Error : client ID=" + cInfo.ID + " send not enough data, abort");
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
                        Log.Print("BACKEND Error : client ID=" + cInfo.ID + " send bad data, abort");
                        break;
                    }
                }
                if(cInfo.sw.ElapsedMilliseconds > clientTimeout)
                {
                    Log.Print("BACKEND Error : client ID=" + cInfo.ID + " timed out, abort");
                    break;
                }
                if(cInfo.cleanUp)
                {
                    Log.Print("BACKEND Cleanup : client ID=" + cInfo.ID + " removed");
                    for (int i = 0; i < clientList.Count; i++)
                        if (clientList[i].ID == cInfo.ID)
                        {
                            clientList.RemoveAt(i);
                            break;
                        }
                    break;
                }
                Thread.Sleep(1);
            }
            cInfo.tcp.Close();
            RemoveClient(cInfo);
            Log.Print("BACKEND Client disconnected with ID=" + cInfo.ID);
        }

        public static void Stop()
        {
            lock (_sync)
            {
                _exit = true;
            }
            if (tcp != null)
                tcp.Stop();
            while(true)
            {
                lock(_sync)
                {
                    if (!_running)
                        break;
                }
                Thread.Sleep(10);
                Application.DoEvents();
            }
            clientList.Clear();
        }

        public static void BroadcastCommand(uint cmd, byte[] data)
        {
            BroadcastCommandExcept(cmd, data, null);
        }

        public static void BroadcastCommandExcept(uint cmd, byte[] data, ClientInfo except)
        {
            foreach (ClientInfo client in clientList)
            {
                if (client != except)
                    try
                    {
                        if(!client.cleanUp)
                            NetHelper.ServerSendCMDPacket(client.ns, cmd, data, client._sync);
                    }
                    catch (Exception ex)
                    {
                        Log.Print("BACKEND Broadcast failed with : " + ex.Message);
                    }
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



        public static List<BackendCommand> backendCmdFilter = new List<BackendCommand>
        {
            BackendCommand.PingReq,
            BackendCommand.PlayFootStepSoundReq,
            BackendCommand.SpawnGroupItemReq,
            BackendCommand.SpawnGroupRemovalsReq,
        };

        public static bool ShouldFilterInLog(BackendCommand cmd)
        {
            return backendCmdFilter.Contains(cmd);
        }
    }
}
