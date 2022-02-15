using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines
{
    public class NetPlaySound3D
    {
        public float[] location = new float[3];
        public uint itemID;
        public bool suppressed;

        public void Read(Stream s)
        {
            location[0] = NetHelper.ReadFloat(s);
            location[1] = NetHelper.ReadFloat(s);
            location[2] = NetHelper.ReadFloat(s);
            itemID = NetHelper.ReadU32(s);
            suppressed = s.ReadByte() == 1;
        }

        public void Write(Stream s)
        {
            NetHelper.WriteFloat(s, location[0]);
            NetHelper.WriteFloat(s, location[1]);
            NetHelper.WriteFloat(s, location[2]);
            NetHelper.WriteU32(s, itemID);
            s.WriteByte((byte)(suppressed ? 1 : 0));
        }
    }
}
