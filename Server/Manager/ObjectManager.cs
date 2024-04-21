﻿using System;
using System.IO;
using System.Collections.Generic;
using NetDefines;

namespace Server
{
    public static class ObjectManager
    {
        public static List<NetObject> objects = new List<NetObject>();

        public static uint objectIDcounter = 0x1000;

        public static void Reset()
        {
            objects = new List<NetObject>();
            Log.Print("RESET ObjectManager");
        }

        public static NetObject FindByAccessKey(uint key)
        {
            foreach (NetObject o in objects)
                if (o.accessKey == key)
                    return o;
            return null;
        }

        public static NetObject FindByID(uint ID)
        {
            foreach (NetObject o in objects)
                if (o.ID == ID)
                    return o;
            return null;
        }

        public static void RemoveClientObjects(ClientInfo c)
        {
            MemoryStream m = new MemoryStream();
            for (int i = 0; i < objects.Count; i++)
                if(c.objIDs.Contains(objects[i].ID))
                {
                    if (objects[i] is NetObjPlayerState)
                        RemovePlayerFromVehicles(objects[i].ID);
                    NetHelper.WriteU32(m, objects[i].ID);
                    objects.RemoveAt(i);
                    i--;
                }
            Backend.BroadcastCommand((uint)BackendCommand.DeleteObjectsReq, m.ToArray());
            EnvServer.SendObjectDeleteRequest(m.ToArray());
        }

        private static void RemovePlayerFromVehicles(uint id)
        {
            for (int i = 0; i < objects.Count; i++)
                if (objects[i] is NetObjVehicleState netVehicle)
                    for (int j = 0; j < netVehicle.GetSeatCount(); j++)
                        if (netVehicle.GetSeatPlayerID(j) == id)
                        {
                            netVehicle.SetSeatPlayerID(j, 0);
                            EnvServer.SendChangeVehicleSeatID(netVehicle.ID, 0, j);
                            if (j == 0)
                            {
                                netVehicle.accessKey = MakeNewAccessKey();
                                EnvServer.SendChangeControlVehicleRequest(netVehicle.ID, netVehicle.accessKey, true, true);
                            }
                        }
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
