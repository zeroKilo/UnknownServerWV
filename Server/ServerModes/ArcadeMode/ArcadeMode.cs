using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDefines;

namespace Server
{
    public static class ArcadeMode
    {

        public static string mapName;
        public static List<NetMapInfo> mapInfos = new List<NetMapInfo>()
        {
            new NetMapInfo("bodie", new List<string>(){})
        };

        public static void Start()
        {
            Log.Print("Starting arcade mode...");
            Backend.mode = ServerMode.ArcadeMode;
            Backend.modeState = ServerModeState.ARC_LobbyState;
            Backend.Start();
            MainServer.Start();
            ArcadeModeServerLogic.Start();
        }
        public static void Stop()
        {
            Log.Print("Stopping arcade mode...");
            MainServer.Stop();
            Backend.Stop();
            ArcadeModeServerLogic.Stop();
        }
        public static void HandleMessage(byte[] msg, ClientInfo client)
        {
            uint ID;
            byte[] data;
            uint objectID, index, count;
            ItemSpawnInfo spawnInfo;
            NetDefines.StateDefines.NetState_Inventory inventory;
            lock (client._sync)
            {
                MemoryStream m = new MemoryStream(msg);
                MemoryStream tmp = new MemoryStream();
                BackendCommand cmd = (BackendCommand)NetHelper.ReadU32(m);
                if (cmd != BackendCommand.PingReq)
                    Log.Print("BattleRoyaleMode: Client " + client.ID + " send CMD " + cmd);
                switch (cmd)
                {
                    //Requests
                    case BackendCommand.WelcomeReq:
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.WelcomeRes, Encoding.UTF8.GetBytes(Config.settings["name"]));
                        break;
                    case BackendCommand.PingReq:
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.PingRes, new byte[0]);
                        client.sw.Restart();
                        break;
                    case BackendCommand.LoginReq:
                        string key = "";
                        while (m.Position < m.Length)
                            key += (char)m.ReadByte();
                        PlayerProfile target = null;
                        foreach (PlayerProfile p in Config.profiles)
                            if (p.key == key)
                            {
                                target = p;
                                break;
                            }
                        if (target == null)
                        {
                            Log.Print("cant find player profile for client ID=" + client.ID + " with key " + key);
                            NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginFailRes, new byte[0]);
                        }
                        else
                        {
                            Log.Print("Client ID=" + client.ID + " tries to login as " + target.name);
                            bool found = false;
                            foreach (ClientInfo c in Backend.clientList)
                                if (c.profile != null && c.profile.key == key)
                                {
                                    found = true;
                                    break;
                                }
                            if (found)
                            {
                                Log.Print("Error : client ID=" + client.ID + " cant login as " + target.name + " because another client is already logged in");
                                NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginFailRes, new byte[0]);
                            }
                            else
                            {
                                Log.Print("Client ID=" + client.ID + " logged in as " + target.name);
                                client.profile = target;
                                NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginSuccessRes, Encoding.UTF8.GetBytes(target.name));
                            }
                        }
                        break;
                    case BackendCommand.GetMapReq:
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetMapRes, Encoding.UTF8.GetBytes(mapName));
                        break;
                    case BackendCommand.CreatePlayerObjectReq:
                        NetObjPlayerState playerTransform = new NetObjPlayerState();
                        playerTransform.ID = ObjectManager.objectIDcounter++;
                        playerTransform.accessKey = ObjectManager.MakeNewAccessKey();
                        playerTransform.ReadUpdate(m);
                        ObjectManager.objects.Add(playerTransform);
                        client.objIDs.Add(playerTransform.ID);
                        data = playerTransform.Create(true);
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.CreatePlayerObjectRes, data);
                        data = playerTransform.Create(false);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.CreateEnemyObjectReq, data, client);
                        break;
                    case BackendCommand.GetAllObjectsReq:
                        m = new MemoryStream();
                        foreach (NetObject no in ObjectManager.objects)
                        {
                            NetHelper.WriteU32(m, no.ID);
                            NetHelper.WriteU32(m, (uint)no.type);
                            tmp = new MemoryStream();
                            no.WriteUpdate(tmp);
                            data = tmp.ToArray();
                            NetHelper.WriteU32(m, (uint)data.Length);
                            m.Write(data, 0, data.Length);
                        }
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllObjectsRes, m.ToArray());
                        break;
                    case BackendCommand.ReloadTriggeredReq:
                        objectID = NetHelper.ReadU32(m);
                        uint len = NetHelper.ReadU32(m);
                        string name = "";
                        for (int i = 0; i < len; i++)
                            name += (char)m.ReadByte();
                        Log.Print("Reload triggered for object 0x" + objectID.ToString("X8"));
                        Log.Print("Animation Name = " + name);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.ReloadTriggeredReq, data, client);
                        break;
                    case BackendCommand.InventoryUpdateReq:
                        objectID = NetHelper.ReadU32(m);
                        Log.Print("Inventory update for object 0x" + objectID.ToString("X8"));
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.InventoryUpdateReq, data, client);
                        break;
                    case BackendCommand.ShotTriggeredReq:
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.ShotTriggeredReq, data, client);
                        break;
                    case BackendCommand.ImpactTriggeredReq:
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.ImpactTriggeredReq, data, client);
                        break;
                    case BackendCommand.DoorStateChangedReq:
                        float[] pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        int newState = (int)NetHelper.ReadU32(m);
                        DoorManager.UpdateDoor(pos, newState);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.DoorStateChangedReq, data, client);
                        break;
                    case BackendCommand.GetDoorStatesReq:
                        m = new MemoryStream();
                        NetHelper.WriteU32(m, (uint)DoorManager.doorChanges.Count);
                        foreach (DoorManager.DoorInfo di in DoorManager.doorChanges)
                        {
                            foreach (float f in di.location)
                                NetHelper.WriteFloat(m, f);
                            NetHelper.WriteU32(m, (uint)di.state);
                        }
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetDoorStatesRes, m.ToArray());
                        break;
                    case BackendCommand.PlayerReadyReq:
                        lock (client._sync)
                        {
                            client.isReady = true;
                        }
                        break;
                    case BackendCommand.PlayerHitReq:
                        ID = NetHelper.ReadU32(m);
                        HitLocation loc = (HitLocation)NetHelper.ReadU32(m);
                        foreach (ClientInfo other in Backend.clientList)
                        {
                            if (other.objIDs.Contains(ID))
                            {
                                m = new MemoryStream();
                                NetHelper.WriteU32(m, (uint)loc);
                                lock (other._sync)
                                {
                                    NetHelper.ServerSendCMDPacket(other.ns, (uint)BackendCommand.PlayerHitReq, m.ToArray());
                                }
                                Log.Print("Player with id 0x" + ID.ToString("X8") + " was informed about hit in : " + loc);
                                break;
                            }
                        }
                        break;
                    case BackendCommand.PlayerDiedReq:
                        ID = NetHelper.ReadU32(m);
                        Log.Print("Player with id 0x" + ID.ToString("X8") + " died!");
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        inventory = new NetDefines.StateDefines.NetState_Inventory();
                        inventory.Read(m);
                        count = NetHelper.ReadU32(m);
                        name = "";
                        for (int i = 0; i < count; i++)
                            name = name + (char)m.ReadByte();
                        SpawnManager.RegisterItemContainer(pos, ItemContainerType.PlayerCrate, name, inventory);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommand((uint)BackendCommand.PlayerDiedReq, data);
                        ObjectManager.RemoveClientObjects(client);
                        break;
                    case BackendCommand.SpawnGroupItemRemoveReq:
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        index = NetHelper.ReadU32(m);
                        SpawnManager.AddSpawnGroupRemoval(pos, (int)index);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommand((uint)BackendCommand.SpawnGroupItemRemoveReq, data);
                        break;
                    case BackendCommand.ItemDroppedReq:
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        spawnInfo = new ItemSpawnInfo(m);
                        SpawnManager.droppedItems.Add(new DroppedItemInfo(pos, spawnInfo));
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommand((uint)BackendCommand.ItemDroppedReq, data);
                        break;
                    case BackendCommand.RemoveDroppedItemReq:
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        SpawnManager.RemoveDroppedItem(pos);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommand((uint)BackendCommand.RemoveDroppedItemReq, data);
                        break;
                    case BackendCommand.RemoveContainerItemReq:
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        index = NetHelper.ReadU32(m);
                        SpawnManager.AddItemContainerRemoval(pos, (int)index);
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommand((uint)BackendCommand.RemoveContainerItemReq, data);
                        break;
                    case BackendCommand.SpawnGroupRemovalsReq:
                        pos = new float[] { NetHelper.ReadFloat(m), NetHelper.ReadFloat(m), NetHelper.ReadFloat(m) };
                        m = new MemoryStream();
                        SpawnManager.WriteSpawnGroupRemovals(m, pos);
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.SpawnGroupRemovalsReq, m.ToArray());
                        break;
                    case BackendCommand.GetAllPickupsReq:
                        m = new MemoryStream();
                        SpawnManager.WriteDroppedItems(m);
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllPickupsReq, m.ToArray());
                        break;
                    case BackendCommand.GetAllItemContainersReq:
                        m = new MemoryStream();
                        SpawnManager.WriteItemContainers(m);
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllItemContainersReq, m.ToArray());
                        break;
                    case BackendCommand.PlayFootStepSoundReq:
                        data = NetHelper.CopyCommandData(m);
                        Backend.BroadcastCommandExcept((uint)BackendCommand.PlayFootStepSoundReq, data, client);
                        break;
                    //Responses
                    case BackendCommand.DeleteObjectsRes:
                    case BackendCommand.CreateEnemyObjectRes:
                    case BackendCommand.GetAllObjectsRes:
                        break;
                    default:
                        throw new Exception("Unknown command 0x" + cmd.ToString("X"));
                }
            }
        }
    }
}
