using System;
using System.IO;
using System.Windows.Forms;
using NetDefines;

namespace Server
{
    public static class ReplayManager
    {
        public static bool enabled = false;
        public static string outputFolder = null;
        private static readonly object _sync = new object();
        private static Stream replayStream = null;
        private static bool isOpen = false;

        public static void Init()
        {
            if (Config.settings.ContainsKey("replay_folder"))
            {
                outputFolder = Config.settings["replay_folder"];
                if (!outputFolder.EndsWith("\\"))
                    outputFolder += "\\";
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);
            }
            if (Config.settings.ContainsKey("replay_save"))
                enabled = Config.settings["replay_save"] == "1";
        }

        public static void StartNewReplaySession(string name)
        {
            if (!enabled)
                return;
            lock (_sync)
            {
                if (isOpen)
                    CloseReplaySession();
                string filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + name + ".replay";
                replayStream = new FileStream(outputFolder + filename, FileMode.CreateNew, FileAccess.Write);
                isOpen = true;
                Log.Print("REPLAY Opened replay session file: " + filename);
            }
        }

        public static void CloseReplaySession()
        {
            lock (_sync)
            {
                replayStream.Close();
                isOpen = false;
                Log.Print("REPLAY Closed replay session file");
            }
        }

        public static void WriteTcpPacketPlayer(byte[] msg, ClientInfo client, bool isRecv)
        {
            lock (_sync)
            {
                if (!enabled || !isOpen)
                    return;
                Stream s = replayStream;
                long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                byte typeAndDir = (byte)(isRecv ? 0x80 : 0);
                typeAndDir |= (byte)ReplayPacketTypes.TCP_Player;
                s.WriteByte(typeAndDir);
                NetHelper.WriteU64(s, (ulong)timestamp);
                NetHelper.WriteU32(s, client.ID);
                if (!isRecv && msg.Length > 8)
                {
                    MemoryStream m = new MemoryStream();
                    m.Write(msg, 8, msg.Length - 8);
                    msg = m.ToArray();
                }
                NetHelper.WriteArray(s, msg);
            }
        }

        public static void WriteTcpPacketEnv(byte[] msg, bool isRecv)
        {
            lock (_sync)
            {
                if (!enabled || !isOpen)
                    return;
                Stream s = replayStream;
                long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                byte typeAndDir = (byte)(isRecv ? 0x80 : 0);
                typeAndDir |= (byte)ReplayPacketTypes.TCP_Env;
                s.WriteByte(typeAndDir);
                NetHelper.WriteU64(s, (ulong)timestamp);
                if (!isRecv && msg.Length > 8)
                {
                    MemoryStream m = new MemoryStream();
                    m.Write(msg, 8, msg.Length - 8);
                    msg = m.ToArray();
                }
                NetHelper.WriteArray(s, msg);
            }
        }

        public static void WriteUdpPacket(byte[] msg)
        {
            lock (_sync)
            {
                if (!enabled || !isOpen)
                    return;
                Stream s = replayStream;
                long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                s.WriteByte((byte)ReplayPacketTypes.UDP);
                NetHelper.WriteU64(s, (ulong)timestamp);
                NetHelper.WriteArray(s, msg);
            }
        }

        public static void ServerSendCMDPacketToPlayer(ClientInfo client, uint cmd, byte[] data, object _sync)
        {
            lock (_sync)
            {
                Stream s = client.tcp.GetStream();
                MemoryStream m = new MemoryStream();
                NetHelper.WriteU32(m, NetConstants.PACKET_MAGIC);
                NetHelper.WriteU32(m, (uint)data.Length + 4);
                NetHelper.WriteU32(m, cmd);
                m.Write(data, 0, data.Length);
                byte[] packet = m.ToArray();
                if (enabled && isOpen)
                    WriteTcpPacketPlayer(packet, client, false);
                s.Write(packet, 0, packet.Length);
            }
        }

        public static void ServerSendCMDPacketToEnv(Stream s, uint cmd, byte[] data)
        {
            lock (_sync)
            {
                MemoryStream m = new MemoryStream();
                NetHelper.WriteU32(m, NetConstants.PACKET_MAGIC);
                NetHelper.WriteU32(m, (uint)data.Length + 4);
                NetHelper.WriteU32(m, cmd);
                m.Write(data, 0, data.Length);
                byte[] packet = m.ToArray();
                if (enabled && isOpen)
                    WriteTcpPacketEnv(packet, false);
                s.Write(packet, 0, packet.Length);
            }
        }
    }
}
