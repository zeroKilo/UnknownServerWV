using NetDefines;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Server
{
    public static class StatusServer
    {
        private static readonly object _syncLogin = new object();
        private static readonly object _syncExit = new object();
        private static readonly object _syncRunning = new object();
        private static string gdsIP;
        private static string gdsPort;
        private static int gdsWait;
        private static int lastDay = DateTime.Now.Day;
        private static int _loginCount = 0;
        private static bool _running = false;
        private static bool _exit = false;
        public static int LoginCount
        {
            get 
            {
                int result = 0;
                lock(_syncLogin)
                {
                    result = _loginCount;
                }
                return result;
            }
            set
            {
                lock (_syncLogin)
                {
                    _loginCount = value;
                }
            }
        }
        public static bool IsRunning
        {
            get
            {
                bool result = false;
                lock(_syncRunning)
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

        public static void Init()
        {
            string[] keys = { "gds_ip", "gds_port", "gds_wait" };
            foreach (string key in keys)
                if (!Config.settings.ContainsKey(key))
                {
                    Log.Print("STATUSSRV Error : cant find setting " + key + "!");
                    Log.Print("STATUSSRV main loop stopped");
                    return;
                }
            gdsIP = Config.settings[keys[0]];
            gdsPort = Config.settings[keys[1]];
            gdsWait = int.Parse(Config.settings[keys[2]]);
            if (gdsWait < 10)
                gdsWait = 10;
        }

        public static void Start()
        {
            if (IsRunning)
                return;
            IsRunning = true;
            ShouldExit = false;
            new Thread(tMain).Start();
        }

        public static void tMain(object obj)
        {
            Log.Print("STATUSSRV main loop running...");
            while (true)
            {
                try
                {
                    if (ShouldExit)
                    {
                        Log.Print("STATUSSRV main loop is exiting...");
                        break;
                    }
                    try
                    {
                        SendStatus();
                    }
                    catch
                    {
                        Log.Print("STATUSSRV failed to send status!");
                    }
                    for (int i = 0; i < 100; i++)
                    {
                        Thread.Sleep(gdsWait * 10);
                        if (ShouldExit)
                            break;
                    }
                }
                catch
                {
                    ShouldExit = true;
                    break;
                }
            }
            Log.Print("STATUSSRV main loop stopped");
            IsRunning = false;
        }

        private static void SendStatus()
        {
            if(DateTime.Now.Day != lastDay)
            {
                LoginCount = 0;
                lastDay = DateTime.Now.Day;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"timestamp\":\"" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "\",");
            sb.Append("\"server\":{");
            sb.Append("\"name\":\"" + Config.settings["name"] + "\",");
            sb.Append("\"timeout\":\"" + Config.settings["timeout"] + "\",");
            sb.Append("\"port_tcp\":\"" + Backend.port + "\",");
            sb.Append("\"port_udp\":\"" + MainServer.port + "\",");
            sb.Append("\"map_name\":\"" + Backend.currentMap + "\",");
            sb.Append("\"login_count\":\"" + LoginCount + "\",");
            sb.Append("\"backend_mode\":\"" + Backend.mode.ToString() + "\",");
            sb.Append("\"backend_mode_state\":\"" + Backend.modeState.ToString() + "\"");
            sb.Append("},");
            sb.Append("\"clients\":{");
            for (int i = 0; i < Backend.clientList.Count; i++)
            {
                ClientInfo info = Backend.clientList[i];
                if (info.profile == null)
                    break;
                sb.Append("\"" + info.ID + "\":{");
                sb.Append("\"name\":\"" + info.profile.name + "\"");
                sb.Append("}");
                if (i < Backend.clientList.Count - 1)
                    sb.Append(",");
            }
            sb.Append("}");
            sb.Append("}");
            string reply = HttpServerWV.GetResponseData(HttpServerWV.SendSignedRestRequest(Config.rsaParams, Config.pubKey, HttpMethod.Post, Config.GetGdsBaseAddress(), "/server_status", sb.ToString()));
            XElement json = NetHelper.StringToJSON(reply);
            XElement countElement = json.XPathSelectElement("playerUpdateCount");
            uint count = uint.Parse(countElement.Value);
            if (Config.playerProfileUpdateCounter != count)
            {
                Config.playerProfileUpdateCounter = count;
                Log.Print("STATUSSRV detected player changes, reloading profiles");
                Config.ReloadPlayerProfiles();
                Log.Print("STATUSSRV loaded " + Config.profiles.Count + " profiles");
            }
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
    }
}
