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
        private static readonly object _sync = new object();
        private static bool _running = false;
        private static bool _exit = false;
        private static UdpClient udp;
        private static long dataCounter;
        private static long errorCounter;
        public static void Start()
        {
            if (isRunning())
                return;
            lock (_sync)
            {
                _exit = false;
                _running = true;
                dataCounter = 0;
                errorCounter = 0;
            }
            new Thread(tMain).Start();
        }

        public static void Stop()
        {
            if (!isRunning())
                return;
            lock (_sync)
            {
                _exit = true;
            }            
            while (true)
            {
                lock (_sync)
                {
                    if (!_running)
                        break;
                }
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static bool isRunning()
        {
            lock (_sync)
            {
                return _running;
            }
        }

        public static long getDataCount()
        {
            lock(_sync)
            {
                return dataCounter;
            }
        }

        public static long getErrorCount()
        {
            lock (_sync)
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

        public static void tMain(object obj)
        {
            Log.Print("MAINSERVER main loop running...");
            if (!Config.settings.ContainsKey("port_udp_min") || !Config.settings.ContainsKey("port_udp_range"))
            {
                _running = false;
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
                bool exit;
                lock (_sync)
                {
                    exit = _exit;
                }
                if (exit)
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
                        lock (_sync)
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
                        lock(_sync)
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
            _running = false;
        }

        public static void ProcessData(IPEndPoint sender, byte[] data)
        {
            MemoryStream m = new MemoryStream(data);
            uint accessKey = NetHelper.ReadU32(m);
            NetObject obj = ObjectManager.FindByAccessKey(accessKey);
            if(obj != null && obj is NetObjPlayerState)
            {
                NetObjPlayerState playerTransform = (NetObjPlayerState)obj;
                playerTransform.ReadUpdate(m);
                m = new MemoryStream();
                NetHelper.WriteU32(m, playerTransform.ID);
                playerTransform.WriteUpdate(m);
                foreach (ClientInfo client in Backend.clientList)
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
