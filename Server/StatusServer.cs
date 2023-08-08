using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public static class StatusServer
    {
        private static bool _running = false;
        private static bool _exit = false;
        private static readonly object _sync = new object();
        private static string logPath;

        public static void Start()
        {
            if (isRunning())
                return;
            if (!Config.settings.ContainsKey("path_status"))
            {
                Log.Print("STATUSSRV Error : cant find setting path_status!");
                Log.Print("STATUSSRV main loop stopped");
                return;
            }
            logPath = Config.settings["path_status"];
            lock (_sync)
            {
                _exit = false;
                _running = true;
            }
            new Thread(tMain).Start();
        }

        public static bool isRunning()
        {
            lock (_sync)
            {
                return _running;
            }
        }

        public static void tMain(object obj)
        {
            Log.Print("STATUSSRV main loop running...");
            while (true)
            {
                try
                {
                    lock (_sync)
                    {
                        if (_exit)
                        {
                            Log.Print("STATUSSRV main loop is exiting...");
                            break;
                        }
                        MakeStatus();
                        Thread.Sleep(3000);
                    }
                }
                catch
                {
                    _exit = true;
                    break;
                }
            }
            Log.Print("STATUSSRV main loop stopped");
            _running = false;
        }

        private static void MakeStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"timestamp\":\"" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "\",");
            sb.Append("\"server\":{");
            sb.Append("\"name\":\"" + Config.settings["name"] + "\",");
            sb.Append("\"timeout\":\"" + Config.settings["timeout"] + "\",");
            sb.Append("\"port_tcp\":\"" + Config.settings["port_tcp"] + "\",");
            sb.Append("\"port_udp\":\"" + Config.settings["port_udp"] + "\",");
            sb.Append("\"map_name\":\"" + Backend.currentMap + "\",");
            sb.Append("\"backend_mode\":\"" + Backend.mode.ToString() + "\",");
            sb.Append("\"backend_mode_state\":\"" + Backend.modeState.ToString() + "\"");
            sb.Append("},");
            sb.Append("\"clients\":{");
            for (int i = 0; i < Backend.clientList.Count; i++)
            {
                ClientInfo info = Backend.clientList[i];
                sb.Append("\"" + info.ID + "\":{");
                sb.Append("\"name\":\"" + info.profile.name + "\"");
                sb.Append("}");
                if (i < Backend.clientList.Count - 1)
                    sb.Append(",");
            }
            sb.Append("}");
            sb.Append("}");
            File.WriteAllText(logPath, sb.ToString());
        }

        public static void Stop()
        {
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
    }
}
