using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines
{

    public static class NetHelper
    {
        public static Random rnd = new Random();
        public static ushort ReadU16(Stream s)
        {
            byte[] buff = new byte[2];
            s.Read(buff, 0, 2);
            return BitConverter.ToUInt16(buff, 0);
        }

        public static uint ReadU32(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToUInt32(buff, 0);
        }

        public static ulong ReadU64(Stream s)
        {
            byte[] buff = new byte[8];
            s.Read(buff, 0, 8);
            return BitConverter.ToUInt64(buff, 0);
        }

        public static float ReadFloat(Stream s)
        {
            byte[] buff = new byte[4];
            s.Read(buff, 0, 4);
            return BitConverter.ToSingle(buff, 0);
        }

        public static void WriteU16(Stream s, ushort u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 2);
        }

        public static void WriteU32(Stream s, uint u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 4);
        }

        public static void WriteU64(Stream s, ulong u)
        {
            s.Write(BitConverter.GetBytes(u), 0, 8);
        }

        public static void WriteFloat(Stream s, float f)
        {
            s.Write(BitConverter.GetBytes(f), 0, 4);
        }

        public static void WriteCString(Stream s, string str)
        {
            foreach (char c in str)
                s.WriteByte((byte)c);
        }

        public static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));
                return sb.ToString();
            }
        }
        public static void ClientSendCMDPacket(Stream s, uint cmd, byte[] data)
        {
            MemoryStream m = new MemoryStream();
            WriteU32(m, NetConstants.PACKET_MAGIC);
            WriteU32(m, (uint)data.Length + 4);
            WriteU32(m, cmd);
            m.Write(data, 0, data.Length);
            byte[] packet = m.ToArray();
            s.Write(packet, 0, packet.Length);
        }

        public static byte[] ServerMakeCMDPacket(uint cmd, byte[] data)
        {
            MemoryStream m = new MemoryStream();
            ServerSendCMDPacket(m, cmd, data);
            return m.ToArray();
        }

        public static void ServerSendCMDPacket(Stream s, uint cmd, byte[] data)
        {
            MemoryStream m = new MemoryStream();
            WriteU32(m, NetConstants.PACKET_MAGIC);
            WriteU32(m, (uint)data.Length + 4);
            WriteU32(m, cmd);
            m.Write(data, 0, data.Length);
            byte[] packet = m.ToArray();
            s.Write(packet, 0, packet.Length);
        }

        public static byte[] CopyCommandData(Stream s)
        {
            byte[] result = new byte[s.Length - 4];
            s.Seek(4, 0);
            s.Read(result, 0, result.Length);
            return result;
        }
    }
}
