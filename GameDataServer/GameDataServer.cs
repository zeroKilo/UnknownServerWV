using NetDefines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GameDataServer
{
    public static class GameDataServer
    {
        private static readonly object _sync = new object();
        private static TcpListener tcp;
        private static bool _running = false;
        private static bool _exit = false;

        public static void Start()
        {
            if (isRunning())
                return;
            lock (_sync)
            {
                _exit = false;
                _running = true;
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
            if (tcp != null)
                tcp.Stop();
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

        public static void tMain(object obj)
        {
            Log.Print("GDS main loop running...");
            if (!Config.settings.ContainsKey("port_tcp"))
            {
                _running = false;
                Log.Print("GDS Error : cant find setting port_tcp!");
                Log.Print("GDS main loop stopped");
                return;
            }
            ushort port = Convert.ToUInt16(Config.settings["port_tcp"]);
            string ip = Config.settings["ip"].Trim();
            Log.Print("GDS Binding to " + ip + ":" + port + "...");
            tcp = new TcpListener(IPAddress.Parse(ip), port);
            tcp.Start();
            Log.Print("GDS Started listening");
            while (true)
            {
                lock (_sync)
                {
                    if (_exit)
                    {
                        Log.Print("GDS main loop is exiting...");
                        break;
                    }
                }
                try
                {
                    TcpClient client = tcp.AcceptTcpClient();
                    new Thread(tClient).Start(client);
                }
                catch
                {
                    Log.Print("GDS main loop is exiting...");
                    break;
                }
            }
            try
            {
                tcp.Stop();
            }
            catch { }
            Log.Print("GDS main loop stopped");
            _running = false;
        }
        public static void tClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream ns = client.GetStream();
            try
            {
                byte[] data = NetHelper.ReadAll(ns);
                string s = Encoding.ASCII.GetString(data);
                StringReader sr = new StringReader(s);
                string line = sr.ReadLine();
                string[] parts = line.Split(' ');
                string response;
                if (parts.Length < 1)
                    throw new Exception();
                switch (parts[0])
                {
                    case "POST":
                        response = HandlePOST(parts[1].ToLower(), sr);
                        break;
                    case "GET":
                        response = HandleGET(parts[1].ToLower(), sr);
                        break;
                    default:
                        throw new Exception();
                }
                if (response != null)
                {
                    data = Encoding.ASCII.GetBytes(response);
                    ns.Write(data, 0, data.Length);
                }
            } catch { }
            client.Close();
        }


        public static string HandlePOST(string url, StringReader sr)
        {
            string response;
            List<string> headers = GetHeaders(sr);
            string content = sr.ReadToEnd();
            switch(url)
            {
                case "/register_player":
                    response = HandlePostRegisterPlayer(headers, content);
                    break;
                case "/server_status":
                    response = HandlePostServerStatus(headers, content);
                    break;
                default:
                    throw new Exception();
            }
            return response;
        }

        public static string HandlePostRegisterPlayer(List<string> headers, string content)
        {
            string result = "";
            try
            {
                StringReader sr = new StringReader(content);
                string nameLine = sr.ReadLine();
                string keyLine = sr.ReadLine();
                if (!nameLine.Contains("=") || !keyLine.Contains("="))
                    throw new Exception();
                string[] nameParts = nameLine.Split('=');
                string[] keyParts = keyLine.Split('=');
                if (nameParts.Length != 2 || keyParts.Length != 2)
                    throw new Exception();
                if (nameParts[0].Trim() != "name" || keyParts[0].Trim() != "pubKey")
                    throw new Exception();
                string name = nameParts[1].Trim();
                string pubkey = keyParts[1].Trim();
                if (!Helper.ValidName(name) || pubkey.Length != 534)
                    throw new Exception();
                PlayerProfile[] list = DBManager.GetPlayerProfiles();
                foreach (PlayerProfile p in list)
                    if (p.Name == name)
                    {
                        result = "Name already taken!";
                        Log.Print("HandlePostRegisterPlayer: " + result + " (" + name +")");
                        break;
                    }
                if(result == "")
                {
                    DBManager.AddPlayerProfile(new PlayerProfile(0, pubkey, name));
                    result = "Successfully added!";
                    Log.Print("HandlePostRegisterPlayer: " + result + " (" + name + ")");
                }
            }
            catch
            {
                result = "Error processing!";
                Log.Print("HandlePostRegisterPlayer: " + result);
            }
            return MakeHeaderJSON("{\"result\":\"" + result + "\"}");
        }

        public static string HandlePostServerStatus(List<string> headers, string content)
        {
            GameServer gs = CheckServerSignature(headers, content);
            gs.Status = content;
            DBManager.UpdateGameServer(gs);
            Log.Print("HandlePostServerStatus: updated status of server");
            return MakeHeaderJSON("{\"playerCount\" : " + DBManager.GetPlayerProfiles().Length + "}");
        }

        public static string HandleGET(string url, StringReader sr)
        {
            string response;
            List<string> headers = GetHeaders(sr);
            string content = sr.ReadToEnd();
            switch (url)
            {
                case "/profile_list":
                    response = HandleGetProfileList(headers, content);
                    break;
                case "/server_list":
                    response = HandleGetServerList();
                    break;
                case "/status_list":
                    response = HandleGetStatusList();
                    break;
                default:
                    throw new Exception();
            }
            return response;
        }
        public static string HandleGetProfileList(List<string> headers, string content)
        {
            CheckServerSignature(headers, content);
            Log.Print("HandleGetProfileList: sending profiles to server");
            PlayerProfile[] profiles = DBManager.GetPlayerProfiles();
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"profiles\":[");
            int count = 0;
            foreach(PlayerProfile p in profiles)
            {
                sb.Append("{\"name\":\"" + p.Name + "\",");
                sb.Append("\"public_key\":\"" + p.PublicKey + "\"}");
                if (++count < profiles.Length)
                    sb.Append(",");
            }
            sb.Append("]}");
            return MakeHeaderJSON(sb.ToString());
        }

        public static string HandleGetServerList()
        {
            Log.Print("HandleGetServerList: sending server list to client");
            GameServer[] gameServers = DBManager.GetServerProfiles();
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"list\":[");
            int count = 0;
            foreach (GameServer gs in gameServers)
            {
                sb.Append("{\"pub_key\":\"" + gs.PublicKey + "\",");
                sb.Append("\"name\":\"" + gs.Name + "\",");
                sb.Append("\"ip\":\"" + gs.IP + "\",");
                sb.Append("\"portUDP\":\"" + gs.PortUDP + "\",");
                sb.Append("\"portTCP\":\"" + gs.PortTCP + "\",");
                sb.Append("\"status\":\"" + Helper.Base64Encode(gs.Status) + "\"}");
                if (++count < gameServers.Length)
                    sb.Append(",");
            }
            sb.Append("]}");
            return MakeHeaderJSON(sb.ToString());
        }

        public static string HandleGetStatusList()
        {
            Log.Print("HandleGetStatusList: sending status");
            GameServer[] gameServers = DBManager.GetServerProfiles();
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"list\":[");
            for (int i = 0; i < gameServers.Length; i++)
            {
                GameServer gs = gameServers[i];
                sb.Append("{\"status\":\"" + Helper.Base64Encode(gs.Status) + "\"}");
                if (i < gameServers.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]}");
            return MakeHeaderJSON(sb.ToString());
        }



        private static GameServer CheckServerSignature(List<string> headers, string content)
        {
            string signature = headers[0].Split(':')[1].Trim();
            string pubKey = headers[1].Split(':')[1].Trim();
            RSAParameters p = NetHelper.LoadSigningKeys(pubKey);
            byte[] data = Encoding.ASCII.GetBytes(content);
            if (!NetHelper.VerifySignature(data, NetHelper.HexStringToArray(signature), p))
                throw new Exception();
            GameServer[] serverList = DBManager.GetServerProfiles();
            foreach (GameServer gs in serverList)
                if (gs.PublicKey == pubKey)
                    return gs;            
            throw new Exception();
        }
        private static string MakeHeaderJSON(string content)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("Access-Control-Allow-Origin: *");
            sb.AppendLine("Content-Type: application/json; charset=utf-8");
            sb.AppendLine("Content-Length: " + content.Length);
            sb.AppendLine();
            sb.Append(content);
            return sb.ToString();
        }

        private static List<string> GetHeaders(StringReader sr)
        {
            List<string> result = new List<string>();
            string line;
            while (true)
            {
                line = sr.ReadLine();
                if (line == null || line == "")
                    break;
                result.Add(line);
            }
            return result;
        }

        public static bool isRunning()
        {
            lock (_sync)
            {
                return _running;
            }
        }
    }
}
