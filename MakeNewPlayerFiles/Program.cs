using System.IO;
using System.Text;

namespace MakeNewPlayerFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] keys = NetDefines.NetHelper.MakeSigningKeys();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public=" + keys[0]);
            sb.AppendLine("private=" + keys[1]);
            File.WriteAllText("client.keys", sb.ToString());
            sb = new StringBuilder();
            sb.AppendLine("name=NewPlayer");
            sb.AppendLine("pubKey=" + keys[0]);
            File.WriteAllText("player.info", sb.ToString());
        }
    }
}
