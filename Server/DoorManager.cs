using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class DoorManager
    {
        public class DoorInfo
        {
            public float[] location = new float[3];
            public int state = 0;

            public DoorInfo(float[] pos, int s)
            {
                location = pos;
                state = s;
            }
        }

        public static List<DoorInfo> doorChanges = new List<DoorInfo>();

        public static void UpdateDoor(float[] pos, int newState)
        {
            foreach(DoorInfo di in doorChanges)
                if( di.location[0] == pos[0] &&
                    di.location[1] == pos[1] &&
                    di.location[2] == pos[2])
                {
                    di.state = newState;
                    return;
                }
            doorChanges.Add(new DoorInfo(pos, newState));
        }

        public static void Reset()
        {
            doorChanges = new List<DoorInfo>();
            Log.Print("RESET DoorManager");
        }
    }

}
