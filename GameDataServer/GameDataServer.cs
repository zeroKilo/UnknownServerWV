using NetDefines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GameDataServer
{
    public static class GameDataServer
    {
        private static HttpServerWV server;

        public static void Start()
        {
            if (server != null && server.IsRunning)
                return;
            if (!Config.settings.ContainsKey("port_tcp"))
            {
                Log.Print("GDS Error : cant find setting port_tcp!");
                Log.Print("GDS main loop stopped");
                return;
            }
            ushort port = Convert.ToUInt16(Config.settings["port_tcp"]);
            string ip = Config.settings["ip"].Trim();
            server = new HttpServerWV(ip, port);
            server.AddHandlerGET("/stats", HandleGetStats);
            server.AddHandlerGET("/profile_list", HandleGetProfileList);
            server.AddHandlerGET("/server_list", HandleGetServerList);
            server.AddHandlerGET("/status_list", HandleGetStatusList);
            server.AddHandlerPOST("/register_player", HandlePostRegisterPlayer);
            server.AddHandlerPOST("/get_player_meta", HandlePostGetPlayerMeta);
            server.AddHandlerPOST("/server_status", HandlePostServerStatus);
            server.AddHandlerPOST("/set_player_metas", HandlePostSetPlayerMeta);
            server.Start();
            Log.Print("GDS started");
        }

        public static void Stop()
        {
            if (server == null || !server.IsRunning)
                return;
            server.Stop();
            while (true)
            {
                if (!server.IsRunning)
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
            Log.Print("GDS stopped");
        }

        public static bool isRunning()
        {
            if (server == null)
                return false;
            else
                return server.IsRunning;
        }

        public static int HandlePostRegisterPlayer(HttpListenerContext ctx)
        {
            string result = "";
            try
            {
                StringReader sr = new StringReader(HttpServerWV.GetRequestData(ctx));
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
                        Log.Print("HandlePostRegisterPlayer: " + result + " (" + name + ")");
                        break;
                    }
                if (result == "")
                {
                    string metaData = "{\"creationDate\":" + DateTimeOffset.Now.ToUnixTimeSeconds() + "}";
                    DBManager.AddPlayerProfile(new PlayerProfile(0, pubkey, name, metaData));
                    result = "Successfully added!";
                    Log.Print("HandlePostRegisterPlayer: " + result + " (" + name + ")");
                }
            }
            catch
            {
                result = "Error processing!";
                Log.Print("HandlePostRegisterPlayer: " + result);
            }
            HttpServerWV.SendJsonResponse(ctx, "{\"result\":\"" + result + "\"}");
            return 0;
        }

        public static int HandlePostGetPlayerMeta(HttpListenerContext ctx)
        {
            string content = HttpServerWV.GetRequestData(ctx);
            CheckServerSignature(ctx, content);
            Log.Print("HandleGetPlayerMeta: sending player metadata to server");
            string[] ids = content.Replace(" ", "").Split(',');
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            PlayerProfile[] profiles = DBManager.GetPlayerProfiles();
            List<string> results = new List<string>();
            foreach (string id in ids)
            {
                int n = int.Parse(id);
                PlayerProfile p = null;
                foreach (PlayerProfile profile in profiles)
                    if (profile.Id == n)
                    {
                        p = profile;
                        break;
                    }
                if (p == null)
                    continue;
                results.Add("\"" + id + "\":" + p.MetaData);
            }
            for (int i = 0; i < results.Count; i++)
            {
                sb.Append(results[i]);
                if (i < results.Count - 1)
                    sb.Append(",");
            }
            sb.Append("}");
            HttpServerWV.SendJsonResponse(ctx, sb.ToString());
            return 0;
        }

        public static int HandlePostServerStatus(HttpListenerContext ctx)
        {
            string content = HttpServerWV.GetRequestData(ctx);
            GameServer gs = CheckServerSignature(ctx, content);
            gs.Status = content;
            gs.ProcessStatusUpdate();
            DBManager.UpdateGameServer(gs);
            Log.Print("HandlePostServerStatus: updated status of server");
            HttpServerWV.SendJsonResponse(ctx, "{\"playerUpdateCount\" : " + DBManager.GetPlayerProfileUpdateCounter() + "}");
            return 0;
        }

        public static int HandlePostSetPlayerMeta(HttpListenerContext ctx)
        {
            try
            {
                string content = HttpServerWV.GetRequestData(ctx);
                GameServer gs = CheckServerSignature(ctx, content);
                StringReader sr = new StringReader(content);
                int profileId = int.Parse(sr.ReadLine());
                string data = sr.ReadToEnd();
                Log.Print("HandlePostSetPlayerMeta: updating specific metadata for profile id " + profileId);
                PlayerProfile[] profiles = DBManager.GetPlayerProfiles();
                PlayerProfile p = null;
                foreach (PlayerProfile profile in profiles)
                    if (profile.Id == profileId)
                    {
                        p = profile;
                        break;
                    }
                if (p == null)
                {
                    Log.Print("HandlePostSetPlayerMeta: Error, cant find the player profile id " + profileId);
                    throw new Exception();
                }
                bool found = false;
                XElement rootData = NetHelper.StringToJSON(data);
                foreach (XNode node in rootData.Nodes())
                {
                    XElement x = (XElement)node;
                    if (x.Name.LocalName == "serverKey" && gs.PublicKey == x.Value)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Log.Print("HandlePostSetPlayerMeta: Error, cant find server key");
                    throw new Exception();
                }
                XElement root = NetHelper.StringToJSON(p.MetaData);
                found = false;
                foreach (XNode node in root.Nodes())
                {
                    XElement x = (XElement)node;
                    if (x.Name.LocalName == "specificData")
                    {
                        foreach (XNode node2 in ((XElement)node).Nodes())
                        {
                            XElement x2 = (XElement)node2;
                            if (x2.Name.LocalName == "serverKey" && gs.PublicKey == x2.Value)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            continue;
                        x.RemoveNodes();
                        foreach (XNode n in rootData.Nodes())
                            x.Add(n);
                        break;
                    }
                }
                if (!found)
                {
                    XElement x = new XElement("specificData");
                    x.SetAttributeValue("type", "object");
                    foreach (XNode n in rootData.Nodes())
                        x.Add(n);
                    root.Add(x);
                }
                p.MetaData = NetHelper.XMLToJSONString(root);
                DBManager.UpdatePlayerProfile(p);
                DBManager.Update();
                Log.Print("HandlePostSetPlayerMeta: updated profile id " + profileId);
            }
            catch { }
            HttpServerWV.SendJsonResponse(ctx, "");
            return 0;
        }

        public static int HandleGetProfileList(HttpListenerContext ctx)
        {
            string content = HttpServerWV.GetRequestData(ctx);
            CheckServerSignature(ctx, content);
            Log.Print("HandleGetProfileList: sending profiles to server");
            PlayerProfile[] profiles = DBManager.GetPlayerProfiles();
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"profiles\":[");
            int count = 0;
            foreach (PlayerProfile p in profiles)
            {
                sb.Append("{\"id\":" + p.Id + ",");
                sb.Append("\"name\":\"" + p.Name + "\",");
                sb.Append("\"public_key\":\"" + p.PublicKey + "\"}");
                if (++count < profiles.Length)
                    sb.Append(",");
            }
            sb.Append("]}");
            HttpServerWV.SendJsonResponse(ctx, sb.ToString());
            return 0;
        }

        public static int HandleGetServerList(HttpListenerContext ctx)
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
            HttpServerWV.SendJsonResponse(ctx, sb.ToString());
            return 0;
        }

        public static int HandleGetStatusList(HttpListenerContext ctx)
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
            HttpServerWV.SendJsonResponse(ctx, sb.ToString());
            return 0;
        }

        public static int HandleGetStats(HttpListenerContext ctx)
        {
            Log.Print("HandleGetStats: sending server stats");
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"registeredPlayer\":" + DBManager.GetPlayerProfiles().Length + ",");
            sb.Append("\"registeredServer\":" + DBManager.GetServerProfiles().Length + ",");
            sb.Append("\"pageViews\":" + DBManager.GetPageViews() + "}");
            HttpServerWV.SendJsonResponse(ctx, sb.ToString());
            return 0;
        }

        private static GameServer CheckServerSignature(HttpListenerContext ctx, string content)
        {
            HttpListenerRequest req = ctx.Request;
            string signature = req.Headers.Get("Signature");
            string pubKey = req.Headers.Get("Public-Key");
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
    }
}
