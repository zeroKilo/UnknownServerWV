﻿using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Xml.Linq;
using System;
using System.Text;
using NetDefines;
using System.Net.Http;

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
        public List<XElement> metaData = new List<XElement>();
        public List<XElement> specificMetaData = new List<XElement>();
        public string machineInfo = "{}";

        public string lastSeen = "";
        public long loginCount = 0;

        public void RequestMetaData()
        {
            string id = profile.id.ToString();
            string data = HttpServerWV.GetResponseData(HttpServerWV.SendSignedRestRequest(Config.rsaParams, Config.pubKey, HttpMethod.Post, Config.GetGdsBaseAddress(), "/get_player_meta", id, null, "text/plain"));
            XElement root = NetHelper.StringToJSON(data);
            metaData = new List<XElement>();
            foreach(XNode node in root.Nodes())
            {
                XElement x = (XElement)node;
                if (x.Attribute("item").Value == id)
                {
                    foreach (XNode node2 in x.Nodes())
                        metaData.Add((XElement)node2);
                    break;
                }
            }
            ProcessSpecificMetaData();
        }

        public void ProcessSpecificMetaData()
        {
            string myKey = Config.pubKey;
            specificMetaData = new List<XElement>();
            foreach (XElement el in metaData)
                if (el.Name.LocalName == "specificData")
                {
                    bool found = false;
                    foreach (XNode node in el.Nodes())
                    {
                        XElement x = (XElement)node;
                        if (x.Name.LocalName == "serverKey" && x.Value == myKey)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        foreach (XNode node in el.Nodes())
                            specificMetaData.Add((XElement)node);
                        break;
                    }
                }
            lastSeen = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            foreach (XElement el in specificMetaData)
                switch (el.Name.LocalName)
                {
                    case "lastSeen":
                        lastSeen = el.Value;
                        break;
                    case "loginCount":
                        loginCount = long.Parse(el.Value);
                        break;
                }
        }

        public void UpdateSpecificMetaData()
        {
            string myKey = Config.pubKey;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(profile.id.ToString());
            sb.Append("{");
            sb.Append("\"serverKey\":\"" + myKey + "\",");
            sb.Append("\"lastSeen\":" + lastSeen + ",");
            sb.Append("\"loginCount\":" + loginCount + ",");
            sb.Append("\"machineInfo\":\"" + NetHelper.Base64Encode(machineInfo) + "\"}");
            HttpServerWV.SendSignedRestRequest(Config.rsaParams, Config.pubKey, HttpMethod.Post, Config.GetGdsBaseAddress(), "/set_player_meta", sb.ToString());
            HttpServerWV.SendSignedRestRequest(Config.rsaParams, Config.pubKey, HttpMethod.Post, Config.GetGdsBaseAddress(), "/add_single_login", profile.id.ToString());
        }
    }
}
