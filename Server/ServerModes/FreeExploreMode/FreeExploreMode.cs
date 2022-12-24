using NetDefines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class FreeExploreMode
    {
        public static string mapName;
        public static string[] spawnLocNames;
        public static int spawnLocIdx;
        public static List<NetMapInfo> mapInfos = new List<NetMapInfo>()
        {
            new NetMapInfo("miramar", new List<string>(){"SpawnLoc"})
        };

        public static void RemovePlayer(uint id)
        {
            int index = -1;
            for (int i = 0; i < FreeExploreServerLogic.playerIDs.Count; i++)
                if (FreeExploreServerLogic.playerIDs[i] == id)
                {
                    index = i;
                    break;
                }
            if (index != -1)
                FreeExploreServerLogic.playerIDs.RemoveAt(index);
        }

        public static void Start()
        {
            Log.Print("Starting free explore mode...");
            if (spawnLocIdx == 0)
                spawnLocIdx = NetHelper.rnd.Next(spawnLocNames.Length);
            else
                spawnLocIdx--;
            Log.Print("Choosen spawn location: " + spawnLocNames[spawnLocIdx]);
            Backend.mode = ServerMode.FreeExploreMode;
            Backend.modeState = ServerModeState.FEM_LobbyState;
            Backend.Start();
            MainServer.Start();
            FreeExploreServerLogic.Start();
        }

        public static void Stop()
        {
            Log.Print("Stopping free explore mode...");
            MainServer.Stop();
            Backend.Stop();
            FreeExploreServerLogic.Stop();
        }

        public static void HandleMessage(byte[] msg, ClientInfo client)
        {
            uint ID;
            byte[] data;
            bool found;
            uint objectID, index;
            ItemSpawnInfo spawnInfo;
            MemoryStream m = new MemoryStream(msg);
            MemoryStream tmp;
            BackendCommand cmd = (BackendCommand)NetHelper.ReadU32(m);
            if (cmd != BackendCommand.PingReq)
                Log.Print("FreeExploreMode: Client " + client.ID + " send CMD " + cmd);
            switch (cmd)
            {
                //Requests
                case BackendCommand.WelcomeReq:
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.WelcomeRes, Encoding.UTF8.GetBytes(Config.settings["name"]), client._sync);
                    break;
                case BackendCommand.PingReq:
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.PingRes, new byte[0], client._sync);
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
                        NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginFailRes, new byte[0], client._sync);
                    }
                    else
                    {
                        Log.Print("Client ID=" + client.ID + " tries to login as " + target.name);
                        found = false;
                        foreach (ClientInfo c in Backend.clientList)
                            if (c.profile != null && c.profile.key == key)
                            {
                                found = true;
                                break;
                            }
                        if (found)
                        {
                            Log.Print("Error : client ID=" + client.ID + " cant login as " + target.name + " because another client is already logged in");
                            NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginFailRes, new byte[0], client._sync);
                        }
                        else
                        {
                            Log.Print("Client ID=" + client.ID + " logged in as " + target.name);
                            client.profile = target;
                            client.teamID = Backend.clientTeamIDCounter++;
                            client.isTeamReady = true;
                            m = new MemoryStream();
                            NetHelper.WriteU32(m, client.ID);
                            NetHelper.WriteU32(m, client.teamID);
                            NetHelper.WriteU32(m, (uint)target.name.Length);
                            foreach (char c in target.name)
                                m.WriteByte((byte)c);
                            NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.LoginSuccessRes, m.ToArray(), client._sync);
                            Backend.BroadcastCommandExcept((uint)BackendCommand.RefreshPlayerListReq, new byte[0], client);
                        }
                    }
                    break;
                case BackendCommand.GetMapReq:
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetMapRes, Encoding.UTF8.GetBytes(mapName), client._sync);
                    break;
                case BackendCommand.GetSpawnLocReq:
                    tmp = new MemoryStream();
                    NetHelper.WriteU32(tmp, (uint)spawnLocIdx);
                    byte[] spawnLocName = Encoding.UTF8.GetBytes(spawnLocNames[spawnLocIdx]);
                    tmp.Write(spawnLocName, 0, spawnLocName.Length);
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetSpawnLocRes, tmp.ToArray(), client._sync);
                    break;
                case BackendCommand.CreatePlayerObjectReq:
                    found = false;
                    foreach (uint id in FreeExploreServerLogic.playerIDs)
                        if (id == client.ID)
                        {
                            found = true;
                            break;
                        }
                    if (!found)
                        FreeExploreServerLogic.playerIDs.Add(client.ID);
                    NetObjPlayerState playerTransform = new NetObjPlayerState();
                    playerTransform.ID = ObjectManager.objectIDcounter++;
                    playerTransform.accessKey = ObjectManager.MakeNewAccessKey();
                    playerTransform.ReadUpdate(m);
                    ObjectManager.objects.Add(playerTransform);
                    client.objIDs.Add(playerTransform.ID);
                    data = playerTransform.Create(true);
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, 0);
                    m.Write(data, 0, data.Length);
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.CreatePlayerObjectRes, m.ToArray(), client._sync);
                    data = playerTransform.Create(false);
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, client.ID);
                    m.Write(data, 0, data.Length);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.CreateEnemyObjectReq, m.ToArray(), client);
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
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllObjectsRes, m.ToArray(), client._sync);
                    break;
                case BackendCommand.ReloadTriggeredReq:
                    objectID = NetHelper.ReadU32(m);
                    uint len = NetHelper.ReadU32(m);
                    string name = "";
                    for (int i = 0; i < len; i++)
                        name += (char)m.ReadByte();
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.ReloadTriggeredReq, data, client);
                    break;
                case BackendCommand.InventoryUpdateReq:
                    objectID = NetHelper.ReadU32(m);
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
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetDoorStatesRes, m.ToArray(), client._sync);
                    break;
                case BackendCommand.PlayerReadyReq:
                    lock (client._sync)
                    {
                        client.isReady = true;
                    }
                    break;
                case BackendCommand.PlayerNotReadyReq:
                    lock (client._sync)
                    {
                        client.isReady = false;
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
                            NetHelper.WriteU32(m, (uint)client.ID);
                            NetHelper.ServerSendCMDPacket(other.ns, (uint)BackendCommand.PlayerHitReq, m.ToArray(), other._sync);
                            break;
                        }
                    }
                    break;
                case BackendCommand.SpawnGroupItemReq:
                    HandleSpawnGroupItemRequest(client, m);
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
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.SpawnGroupRemovalsReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.GetAllPickupsReq:
                    m = new MemoryStream();
                    SpawnManager.WriteDroppedItems(m);
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllPickupsReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.GetAllItemContainersReq:
                    m = new MemoryStream();
                    SpawnManager.WriteItemContainers(m);
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetAllItemContainersReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.PlayFootStepSoundReq:
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.PlayFootStepSoundReq, data, client);
                    break;
                case BackendCommand.GetPlayersOnServerReq:
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, (uint)Backend.clientList.Count);
                    foreach (ClientInfo other in Backend.clientList)
                    {
                        NetHelper.WriteU32(m, other.ID);
                        NetHelper.WriteU32(m, other.teamID);
                        m.WriteByte((byte)(other.isTeamReady ? 1 : 0));
                        NetHelper.WriteU32(m, (uint)other.profile.name.Length);
                        foreach (char c in other.profile.name)
                            m.WriteByte((byte)c);
                    }
                    NetHelper.ServerSendCMDPacket(client.ns, (uint)BackendCommand.GetPlayersOnServerRes, m.ToArray(), client._sync);
                    break;
                case BackendCommand.PickupInfiniteItemReq:
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommand((uint)BackendCommand.PickupInfiniteItemReq, data);
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
        private static void HandleSpawnGroupItemRequest(ClientInfo client, Stream s)
        {
            float[] pos = new float[] { NetHelper.ReadFloat(s), NetHelper.ReadFloat(s), NetHelper.ReadFloat(s) };
            uint count = NetHelper.ReadU32(s);
            SpawnTierLevel tierLevel = (SpawnTierLevel)NetHelper.ReadU32(s);
            List<ItemSpawnInfo> resultList = SpawnManager.GetRandomSpawn(tierLevel, count);
            MemoryStream m = new MemoryStream();
            foreach (float f in pos)
                NetHelper.WriteFloat(m, f);
            NetHelper.WriteU32(m, (uint)resultList.Count);
            foreach (ItemSpawnInfo i in resultList)
                i.Write(m);
            Backend.BroadcastCommand((uint)BackendCommand.SpawnGroupItemReq, m.ToArray());
        }
    }
}
