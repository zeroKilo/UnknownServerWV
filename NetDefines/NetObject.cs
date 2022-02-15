using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines
{
    public abstract class NetObject
    {
        public enum NetObjectType
        {
            PlayerState
        }

        public uint ID;
        public uint accessKey;
        public NetObjectType type;

        public abstract byte[] Create(bool withAccessKey);

        public abstract void ReadUpdate(Stream s);

        public abstract void WriteUpdate(Stream s);

        public abstract string GetDetails();

    }
}
