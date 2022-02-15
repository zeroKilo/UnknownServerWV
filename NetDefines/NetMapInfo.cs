using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines
{
    public class NetMapInfo
    {
        public string name;
        public List<string> spawnLocations;

        public NetMapInfo()
        { }

        public NetMapInfo(string n, List<string> sLocs)
        {
            name = n;
            spawnLocations = sLocs;
        }
    }
}
