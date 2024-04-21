using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;
using System.Net.Sockets;
using System.Threading;

namespace NetDefines
{
    public static class NetHelper
    {
        private static readonly object _client_sync = new object();
        public static Random rnd = new Random();
        public static SHA256 sha = SHA256.Create();
        public static readonly string version = "9";
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

        public static string ReadCString(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            while(true)
            {
                int b = s.ReadByte();
                if (b == 0) 
                    break;
                sb.Append((char)b);
            }
            return sb.ToString();
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
            s.WriteByte(0);
        }
        public static void WriteArray(Stream s, byte[] data)
        {
            WriteU32(s, (uint)data.Length);
            s.Write(data, 0, data.Length);
        }

        public static byte[] ReadArray(Stream s)
        {
            int len = (int)ReadU32(s);
            byte[] result = new byte[len];
            s.Read(result, 0, len);
            return result;
        }

        public static byte[] ReadAll(NetworkStream s, int retries = 5, int retryDelay = 100)
        {
            MemoryStream m = new MemoryStream();
            int size = 1024;
            byte[] buff = new byte[size];
            while (true)
            {
                int count = s.Read(buff, 0, size);
                if (count == 0)
                    break;
                m.Write(buff, 0, count);
                if (!s.DataAvailable)
                    for (int i = 0; i < retries; i++)
                    {
                        Thread.Sleep(retryDelay);
                        if (s.DataAvailable)
                            break;
                    }
                if (!s.DataAvailable)
                    break;
            }
            return m.ToArray();
        }

        public static string MakeHexString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }

        public static byte[] HexStringToArray(string data)
        {
            return Enumerable.Range(0, data.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(data.Substring(x, 2), 16))
                        .ToArray();
        }

        public static string CreateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));
                return sb.ToString();
            }
        }
        public static string[] MakeSigningKeys()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            RSAParameters rsaKeyInfo = rsa.ExportParameters(true);
            MemoryStream m = new MemoryStream();
            WriteArray(m, rsaKeyInfo.Exponent);
            WriteArray(m, rsaKeyInfo.Modulus);
            string pubKey = MakeHexString(m.ToArray());
            m = new MemoryStream();
            WriteArray(m, rsaKeyInfo.D);
            WriteArray(m, rsaKeyInfo.DP);
            WriteArray(m, rsaKeyInfo.DQ);
            WriteArray(m, rsaKeyInfo.InverseQ);
            WriteArray(m, rsaKeyInfo.P);
            WriteArray(m, rsaKeyInfo.Q);
            string privKey = MakeHexString(m.ToArray());
            return new string[] { pubKey, privKey };
        }

        public static RSAParameters LoadSigningKeys(string pubKeyHex, string privKeyHex = null)
        {
            if (privKeyHex == null)
                return LoadSigningKeys(HexStringToArray(pubKeyHex));
            else
                return LoadSigningKeys(HexStringToArray(pubKeyHex), HexStringToArray(privKeyHex));
        }

        public static RSAParameters LoadSigningKeys(byte[] pubKey, byte[] privKey = null)
        {
            RSAParameters result = new RSAParameters();
            MemoryStream m = new MemoryStream(pubKey);
            result.Exponent = ReadArray(m);
            result.Modulus = ReadArray(m);
            if (privKey != null)
            {
                m = new MemoryStream(privKey);
                result.D = ReadArray(m);
                result.DP = ReadArray(m);
                result.DQ = ReadArray(m);
                result.InverseQ = ReadArray(m);
                result.P = ReadArray(m);
                result.Q = ReadArray(m);
            }
            return result;
        }

        public static byte[] MakeSignature(byte[] data, RSAParameters p)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.ImportParameters(p);
            return provider.SignData(data, sha);
        }

        public static bool VerifySignature(byte[] data, byte[] signature, RSAParameters Key)
        {
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
            provider.ImportParameters(Key);
            return provider.VerifyData(data, sha, signature);
        }

        public static void ClientSendCMDPacket(Stream s, uint cmd, byte[] data)
        {
            lock (_client_sync)
            {
                MemoryStream m = new MemoryStream();
                WriteU32(m, NetConstants.PACKET_MAGIC);
                WriteU32(m, (uint)data.Length + 4);
                WriteU32(m, cmd);
                m.Write(data, 0, data.Length);
                byte[] packet = m.ToArray();
                s.Write(packet, 0, packet.Length);
            }
        }

        public static void ServerSendCMDPacket(Stream s, uint cmd, byte[] data, object _sync)
        {
            lock (_sync)
            {
                MemoryStream m = new MemoryStream();
                WriteU32(m, NetConstants.PACKET_MAGIC);
                WriteU32(m, (uint)data.Length + 4);
                WriteU32(m, cmd);
                m.Write(data, 0, data.Length);
                byte[] packet = m.ToArray();
                s.Write(packet, 0, packet.Length);
            }
        }

        public static byte[] CopyCommandData(Stream s)
        {
            byte[] result = new byte[s.Length - 4];
            s.Seek(4, 0);
            s.Read(result, 0, result.Length);
            return result;
        }

        public static XElement StringToJSON(string s)
        {
            XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(s), new XmlDictionaryReaderQuotas());
            return XElement.Load(jsonReader);
        }

        public static string XMLToJSONString(XElement root)
        {
            MemoryStream m = new MemoryStream();
            XmlDictionaryWriter jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(m, Encoding.UTF8);
            root.Save(jsonWriter);
            jsonWriter.Flush();
            return Encoding.UTF8.GetString(m.ToArray());
        }
        public static string Base64Encode(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        public static string Base64Decode(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

        public static bool IsClose(float[] a, float[] b, float maxDistance = 0.1f)
        {
            float[] diff =
            {
                b[0]- a[0],
                b[1]- a[1],
                b[2]- a[2],
            };
            double sum = 0;
            sum += diff[0] * diff[0];
            sum += diff[1] * diff[1];
            sum += diff[2] * diff[2];
            double magnitude = Math.Sqrt(sum);
            return magnitude < maxDistance;
        }
    }
}
