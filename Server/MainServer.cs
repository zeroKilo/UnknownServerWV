using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using NetDefines;

namespace Server
{
    public static class MainServer
    {
        public static ushort port;
        private static readonly object _syncMain = new object();
        private static readonly object _syncExit = new object();
        private static readonly object _syncRunning = new object();
        private static UdpClient udp;
        private static long dataCounter;
        private static long errorCounter;
        private static bool _running = false;
        private static bool _exit = false;
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
        public static void Start()
        {
            if (IsRunning)
                return;
            ShouldExit = false;
            IsRunning = true;
            lock (_syncMain)
            {
                dataCounter = 0;
                errorCounter = 0;
            }
            new Thread(ThreadMain).Start();
        }

        public static void Stop()
        {
            if (!IsRunning)
                return;
            ShouldExit = true;
            while (true)
            {
                if (!IsRunning)
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static long GetDataCount()
        {
            lock(_syncMain)
            {
                return dataCounter;
            }
        }

        public static long GetErrorCount()
        {
            lock (_syncMain)
            {
                return errorCounter;
            }
        }

        public static ushort GetNextPort()
        {
            int min = Convert.ToInt32(Config.settings["port_udp_min"]);
            int range = Convert.ToInt32(Config.settings["port_udp_range"]);
            return (ushort)NetHelper.rnd.Next(min, min + range);
        }

        public static void ThreadMain(object obj)
        {
            Log.Print("MAINSERVER main loop running...");
            if (!Config.settings.ContainsKey("port_udp_min") || !Config.settings.ContainsKey("port_udp_range"))
            {
                IsRunning = false;
                Log.Print("MAINSERVER Error : cant find settings for port_udp!");
                Log.Print("MAINSERVER main loop stopped");
                return;
            }
            port = GetNextPort();
            string ip = Config.settings["bind_ip"];
            Log.Print("MAINSERVER Binding to " + ip + ":" + port + "...");
            udp = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), port));
            Log.Print("MAINSERVER Started listening");
            while (true)
            {
                if (ShouldExit)
                {
                    Log.Print("MAINSERVER main loop is exiting normally...");
                    break;
                }
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    if (udp.Available > 0)
                    {
                        byte[] data = udp.Receive(ref sender);
                        lock (_syncMain)
                        {
                            dataCounter += data.Length;
                        }
                        ProcessData(sender, data);
                    }
                    else
                        Thread.Sleep(1);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset || (uint)ex.HResult == 0x80004005)
                    {
                        lock(_syncMain)
                        {
                            errorCounter++;
                        }
                        if(udp != null)
                            udp.Close();
                        udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                    }
                    else
                    {
                        Log.Print("MAINSERVER SocketException error: " + ex);
                        break;
                    }
                }
                catch(Exception ex)
                {

                    Log.Print("MAINSERVER Exception error: " + ex);
                    break;
                }
            }
            Log.Print("MAINSERVER closing listener...");
            if (udp != null)
                udp.Close();
            Log.Print("MAINSERVER main loop stopped");
            IsRunning = false;
        }

        public static void ProcessData(IPEndPoint sender, byte[] data)
        {
            MemoryStream m = new MemoryStream(data);
            uint accessKey = NetHelper.ReadU32(m);
            NetObject obj = ObjectManager.FindByAccessKey(accessKey);
            if(obj != null && obj is NetObjPlayerState state)
            {
                NetObjPlayerState playerTransform = state;
                playerTransform.ReadUpdate(m);
                m = new MemoryStream();
                NetHelper.WriteU32(m, playerTransform.ID);
                playerTransform.WriteUpdate(m);
                foreach (ClientInfo client in Backend.ClientList)
                {
                    if(client.objIDs.Contains(playerTransform.ID))
                    {
                        if (client.udp == null)
                            client.udp = sender;
                        continue;
                    }
                    if (client.udp == null)
                        continue;
                    udp.Send(m.ToArray(), (int)m.Length, client.udp);
                }
            }
        }
    }
}
