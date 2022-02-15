using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines
{
    [Serializable]
    public class ItemSpawnInfo
    {
        public Item item;
        public uint count;

        public ItemSpawnInfo()
        { }

        public ItemSpawnInfo(Item i, uint c)
        {
            item = i;
            count = c;
        }

        public ItemSpawnInfo(Stream s)
        {
            Read(s);
        }

        public void Read(Stream s)
        {
            item = (Item)NetHelper.ReadU32(s);
            count = NetHelper.ReadU32(s);
        }

        public void Write(Stream s)
        {
            NetHelper.WriteU32(s, (uint)item);
            NetHelper.WriteU32(s, count);
        }
    }
}
