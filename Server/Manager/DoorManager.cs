using System.Collections.Generic;
using NetDefines;

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
            foreach (DoorInfo di in doorChanges)
                if (NetHelper.IsClose(di.location, pos))
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
