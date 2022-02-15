using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDefines;

namespace Server
{
    public static class ObjectManager
    {
        public static List<NetObject> objects = new List<NetObject>();

        public static uint objectIDcounter = 0x1000;
        public static NetObject FindByAccessKey(uint key)
        {
            foreach (NetObject o in objects)
                if (o.accessKey == key)
                    return o;
            return null;
        }

        public static void RemoveClientObjects(ClientInfo c)
        {
            MemoryStream m = new MemoryStream();
            for (int i = 0; i < objects.Count; i++)
                if(c.objIDs.Contains(objects[i].ID))
                {
                    NetHelper.WriteU32(m, objects[i].ID);
                    objects.RemoveAt(i);
                    i--;
                }
            Backend.BroadcastCommand((uint)BackendCommand.DeleteObjectsReq, m.ToArray());
        }

        public static uint MakeNewAccessKey()
        {
            while(true)
            {
                byte[] buff = new byte[4];
                NetHelper.rnd.NextBytes(buff);
                uint key = BitConverter.ToUInt32(buff, 0);
                bool found = false;
                foreach (NetObject o in objects)
                    if (o.accessKey == key)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    return key;
            }
        }
    }
}
