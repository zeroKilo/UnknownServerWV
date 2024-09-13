using System;
using System.IO;
using System.IO.Compression;
using NetDefines;

namespace Server
{
    public static class ReplayManager
    {
        public static bool enabled = false;
        public static string outputFolder = null;
        private static readonly object _sync = new object();
        private static FileStream fileStream;
        private static ZipArchive zipArchive;
        private static ZipArchiveEntry zipEntry;
        private static Stream replayStream = null;
        private static bool isOpen = false;
        private static int max_hrs = 1;

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
            if (Config.settings.ContainsKey("replay_max_hrs"))
                max_hrs = int.Parse(Config.settings["replay_max_hrs"]);
            CheckCleanUp();
        }

        public static void CheckCleanUp()
        {
            TimeSpan maxDiff = TimeSpan.FromHours(max_hrs);            
            string[] files = Directory.GetFiles(outputFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string[] parts = name.Split('_');
                if(parts.Length > 6)
                {
                    try
                    {
                        DateTime date = new DateTime(
                            int.Parse(parts[0]),
                            int.Parse(parts[1]),
                            int.Parse(parts[2]),
                            int.Parse(parts[3]),
                            int.Parse(parts[4]),
                            int.Parse(parts[5])
                            );
                        TimeSpan diff = DateTime.Now - date;
                        if (diff > maxDiff)
                        {
                            File.Delete(file);
                            Log.Print("Deleted old replay " + name);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Print("Error on replay cleanup: " + e);
                    }
                }
            }
        }

        public static void StartNewReplaySession(string name)
        {
            if (!enabled)
                return;
            CheckCleanUp();
            lock (_sync)
            {
                if (isOpen)
                    CloseReplaySession();
                string filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + name;
                fileStream = new FileStream(outputFolder + filename + ".zip", FileMode.CreateNew, FileAccess.Write);
                zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true);
                zipEntry = zipArchive.CreateEntry(filename + ".replay", CompressionLevel.Optimal);
                replayStream = zipEntry.Open();
                isOpen = true;
                Log.Print("REPLAY Opened replay session file: " + filename);
            }
        }

        public static void CloseReplaySession()
        {
            lock (_sync)
            {
                replayStream.Flush();
                replayStream.Close();
                zipArchive.Dispose();
                fileStream.Flush();
                fileStream.Close();
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
