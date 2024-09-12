﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NetDefines;
using NetDefines.StateDefines;

namespace Server
{
    public static class TeamDeathMatchMode
    {

        public static string mapName;
        public static List<NetMapInfo> mapInfos = new List<NetMapInfo>()
        {
            new NetMapInfo("bodie", new List<string>(){"SpawnLocA", "SpawnLocB"})
        };

        public static void ResetPlayerSpawnLocations()
        {
            foreach(NetMapInfo netmap in mapInfos)
                if(netmap.name == mapName)
                {
                    TeamDeathMatchServerLogic.playerScoresPerLocation = new List<PlayerScoreEntry[]>();
                    for (int i = 0; i < netmap.spawnLocations.Count; i++)
                        TeamDeathMatchServerLogic.playerScoresPerLocation.Add(new PlayerScoreEntry[0]);
                    break;
                }
        }

        public static void RemovePlayer(uint id)
        {
            for (int i = 0; i < TeamDeathMatchServerLogic.playerScoresPerLocation.Count; i++)
            {
                List<PlayerScoreEntry> scores = new List<PlayerScoreEntry>(TeamDeathMatchServerLogic.playerScoresPerLocation[i]);
                for (int j = 0; j < scores.Count; j++)
                    if (scores[j].netObjID == id)
                    {
                        scores.RemoveAt(j);
                        break;
                    }
                TeamDeathMatchServerLogic.playerScoresPerLocation[i] = scores.ToArray();
            }
        }

        public static void Start()
        {
            Log.Print("Starting team death match mode...");
            Backend.mode = ServerMode.TeamDeathMatchMode;
            Backend.modeState = ServerModeState.TDM_LobbyState;
            Backend.Start();
            MainServer.Start();
            StatusServer.Start();
            EnvServer.Start();
            TeamDeathMatchServerLogic.Start();
        }

        public static void Stop()
        {
            Log.Print("Stopping team death match mode...");
            Log.Print("Stopping environment server...");
            EnvServer.Stop();
            Log.Print("Stopping status server...");
            StatusServer.Stop();
            Log.Print("Stopping main server...");
            MainServer.Stop();
            Log.Print("Stopping backend...");
            Backend.Stop();
            Log.Print("Stopping logic...");
            TeamDeathMatchServerLogic.Stop();
            Log.Print("Stopping team death match mode done");
        }

        public static void HandleMessage(byte[] msg, ClientInfo client)
        {
            uint ID;
            byte[] data;
            uint index, count, fromID, toID;
            bool found;
            ItemSpawnInfo spawnInfo;
            NetState_Inventory inventory;
            MemoryStream m = new MemoryStream(msg);
            MemoryStream tmp;
            BackendCommand cmd = (BackendCommand)NetHelper.ReadU32(m);
            if (!Backend.ShouldFilterInLog(cmd))
                Log.Print("TeamDeathMatchMode Client " + client.ID + " send CMD " + cmd);
            switch (cmd)
            {
                //Requests
                case BackendCommand.WelcomeReq:
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.WelcomeRes, Encoding.UTF8.GetBytes(Config.settings["name"]), client._sync);
                    break;
                case BackendCommand.PingReq:
                    Backend.HandlePing(client);
                    break;
                case BackendCommand.LoginReq:
                    string key = Encoding.UTF8.GetString(NetHelper.ReadArray(m));
                    string machineInfo = Encoding.UTF8.GetString(NetHelper.ReadArray(m));
                    PlayerProfile target = null;
                    foreach (PlayerProfile p in Config.profiles)
                        if (p.publicKey == key)
                        {
                            target = p;
                            break;
                        }
                    if (target == null)
                    {
                        Log.Print("cant find player profile for client ID=" + client.ID + " with key " + key);
                        ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.LoginFailRes, new byte[0], client._sync);
                    }
                    else
                    {
                        Log.Print("Client ID=" + client.ID + " tries to login as " + target.name);
                        ClientInfo oldClient = null;
                        foreach (ClientInfo c in Backend.ClientList)
                            if (c.profile != null && c.profile.publicKey == key)
                            {
                                oldClient = c;
                                break;
                            }
                        if (oldClient != null)
                        {
                            Log.Print("Warning : client ID=" + client.ID + " was already logged in as " + target.name + "!");
                            Backend.RemoveClient(oldClient);
                        }
                        Log.Print("Client ID=" + client.ID + " logged in as " + target.name);
                        client.profile = target;
                        client.teamID = Backend.clientTeamIDCounter++;
                        client.isTeamReady = true;
                        client.RequestMetaData();
                        client.loginCount++;
                        client.machineInfo = machineInfo;
                        client.UpdateSpecificMetaData();
                        m = new MemoryStream();
                        NetHelper.WriteU32(m, client.ID);
                        NetHelper.WriteU32(m, client.teamID);
                        NetHelper.WriteU32(m, (uint)target.name.Length);
                        foreach (char c in target.name)
                            m.WriteByte((byte)c);
                        ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.LoginSuccessRes, m.ToArray(), client._sync);
                        Backend.BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
                        StatusServer.LoginCount++;
                    }
                    break;
                case BackendCommand.GetItemConfigReq:
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetItemConfigRes, Encoding.UTF8.GetBytes(Config.itemSettingsJson), client._sync);
                    break;
                case BackendCommand.GetMapReq:
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetMapRes, Encoding.UTF8.GetBytes(mapName), client._sync);
                    break;
                case BackendCommand.CreatePlayerObjectReq:
                    //check if team already exists on a spawn location
                    bool foundTeam = false;
                    uint foundLocIdx = 0;
                    PlayerScoreEntry playerScore = null;
                    foreach (PlayerScoreEntry[] entries in TeamDeathMatchServerLogic.playerScoresPerLocation)
                    {
                        foreach (PlayerScoreEntry entry in entries)
                            if (!foundTeam)
                                foreach (ClientInfo info in Backend.ClientList)
                                    if (!entry.isBot && info.objIDs.Contains(entry.netObjID) && info.teamID == client.teamID)
                                    {
                                        playerScore = entry;
                                        foundTeam = true;
                                        break;
                                    }
                        if (foundTeam)
                            break;
                        foundLocIdx++;
                    }
                    uint spawnLoc;
                    //if team was found, the player side is already decided
                    if (foundTeam)
                        spawnLoc = foundLocIdx;
                    else //otherwise choose the location with least amount of players
                    {
                        foundLocIdx = 0;
                        int foundCount = TeamDeathMatchServerLogic.playerScoresPerLocation[0].Length;
                        for (int i = 1; i < TeamDeathMatchServerLogic.playerScoresPerLocation.Count; i++)
                            if (TeamDeathMatchServerLogic.playerScoresPerLocation[i].Length < foundCount)
                            {
                                foundLocIdx = (uint)i;
                                foundCount = TeamDeathMatchServerLogic.playerScoresPerLocation[i].Length;
                            }
                        //now add all players of the team to the found location
                        List<PlayerScoreEntry> list = new List<PlayerScoreEntry>();
                        list.AddRange(TeamDeathMatchServerLogic.playerScoresPerLocation[(int)foundLocIdx]);
                        foreach (ClientInfo info in Backend.ClientList)
                            if (info.teamID == client.teamID)
                            {
                                playerScore = new PlayerScoreEntry(info.ID);
                                list.Add(playerScore);
                            }
                        TeamDeathMatchServerLogic.playerScoresPerLocation[(int)foundLocIdx] = list.ToArray();
                        spawnLoc = foundLocIdx;
                    }
                    NetObjPlayerState playerState = new NetObjPlayerState
                    {
                        ID = ObjectManager.GetNextID(),
                        accessKey = ObjectManager.MakeNewAccessKey()
                    };
                    playerState.ReadUpdate(m);
                    playerState.SetTeamID((ushort)spawnLoc);
                    ObjectManager.Add(playerState);
                    client.objIDs.Add(playerState.ID);
                    playerScore.netObjID = playerState.ID;
                    data = playerState.Create(true);
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, spawnLoc);
                    m.Write(data, 0, data.Length);
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.CreatePlayerObjectRes, m.ToArray(), client._sync);
                    data = playerState.Create(false);
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, client.ID);
                    m.Write(data, 0, data.Length);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.CreateEnemyObjectReq, m.ToArray(), client);
                    EnvServer.SendPlayerSpawnRequest(m.ToArray());
                    SendScoreBoardUpdate();
                    Backend.BroadcastServerPlayerList();
                    break;
                case BackendCommand.GetAllObjectsReq:
                    m = new MemoryStream();
                    foreach (NetObject no in ObjectManager.GetCopy())
                    {
                        NetHelper.WriteU32(m, no.ID);
                        NetHelper.WriteU32(m, (uint)no.type);
                        tmp = new MemoryStream();
                        no.WriteUpdate(tmp);
                        if (no is NetObjPlayerState netObjPlayer)
                        {
                            inventory = netObjPlayer.GetStateInventory();
                            inventory.Write(tmp);
                        }
                        data = tmp.ToArray();
                        NetHelper.WriteU32(m, (uint)data.Length);
                        m.Write(data, 0, data.Length);
                    }
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetAllObjectsRes, m.ToArray(), client._sync);
                    break;
                case BackendCommand.ReloadTriggeredReq:
                    NetHelper.ReadU32(m);
                    uint len = NetHelper.ReadU32(m);
                    string name = "";
                    for (int i = 0; i < len; i++)
                        name += (char)m.ReadByte();
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.ReloadTriggeredReq, data, client);
                    break;
                case BackendCommand.InventoryUpdateReq:
                    uint who = NetHelper.ReadU32(m);
                    NetState_Inventory inv = new NetState_Inventory();
                    inv.Read(m);
                    Backend.UpdatePlayerInventory(who, inv);
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
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetDoorStatesRes, m.ToArray(), client._sync);
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
                    found = false;
                    foreach (ClientInfo other in Backend.ClientList)
                        if (other.objIDs.Contains(ID))
                        {
                            m = new MemoryStream();
                            NetHelper.WriteU32(m, (uint)loc);
                            NetHelper.WriteU32(m, client.objIDs[0]);
                            ReplayManager.ServerSendCMDPacketToPlayer(other, (uint)BackendCommand.PlayerHitReq, m.ToArray(), other._sync);
                            found = true;
                            break;
                        }
                    if (!found)
                    {
                        m = new MemoryStream();
                        NetHelper.WriteU32(m, client.objIDs[0]);
                        NetHelper.WriteU32(m, ID);
                        NetHelper.WriteU32(m, (uint)loc);
                        EnvServer.SendPlayerHitRequest(m.ToArray());
                    }
                    break;
                case BackendCommand.PlayerDiedReq:
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
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.SpawnGroupRemovalsReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.GetAllPickupsReq:
                    m = new MemoryStream();
                    SpawnManager.WriteDroppedItems(m);
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetAllPickupsReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.GetAllItemContainersReq:
                    m = new MemoryStream();
                    SpawnManager.WriteItemContainers(m);
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.GetAllItemContainersReq, m.ToArray(), client._sync);
                    break;
                case BackendCommand.PlayFootStepSoundReq:
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommandExcept((uint)BackendCommand.PlayFootStepSoundReq, data, client);
                    break;
                case BackendCommand.GetPlayersOnServerReq:
                    Backend.BroadcastServerPlayerList();
                    break;
                case BackendCommand.TeamInviteReq:
                    fromID = NetHelper.ReadU32(m);
                    toID = NetHelper.ReadU32(m);
                    foreach (ClientInfo other in Backend.ClientList)
                        if (other.ID == toID)
                        {
                            Log.Print("Sending team invite from " + fromID + " to " + toID);
                            data = NetHelper.CopyCommandData(m);
                            ReplayManager.ServerSendCMDPacketToPlayer(other, (uint)BackendCommand.TeamInviteReq, data, other._sync);
                            break;
                        }
                    break;
                case BackendCommand.TeamInviteAcceptReq:
                    toID = NetHelper.ReadU32(m);
                    foreach (ClientInfo other in Backend.ClientList)
                        if (other.ID == toID)
                        {
                            Log.Print("Accepting team invite by " + client.ID + " into team of " + toID);
                            client.teamID = other.teamID;
                            foreach (ClientInfo other2 in Backend.ClientList)
                                if (other2.teamID == client.teamID)
                                    other2.isTeamReady = false;
                            Backend.BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
                            break;
                        }
                    break;
                case BackendCommand.TeamLeaveReq:
                    client.teamID = Backend.clientTeamIDCounter++;
                    client.isTeamReady = true;
                    Backend.BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
                    break;
                case BackendCommand.SetTeamReadyStateReq:
                    client.isTeamReady = m.ReadByte() == 1;
                    Backend.BroadcastCommand((uint)BackendCommand.RefreshPlayerListReq, new byte[0]);
                    break;
                case BackendCommand.PickupInfiniteItemReq:
                    data = NetHelper.CopyCommandData(m);
                    Backend.BroadcastCommand((uint)BackendCommand.PickupInfiniteItemReq, data);
                    break;
                case BackendCommand.KillsToWinReq:
                    m = new MemoryStream();
                    NetHelper.WriteU32(m, (uint)TeamDeathMatchServerLogic.killsToWin);
                    ReplayManager.ServerSendCMDPacketToPlayer(client, (uint)BackendCommand.KillsToWinRes, m.ToArray(), client._sync);
                    break;
                case BackendCommand.GetPlayerScoresReq:
                    SendScoreBoardUpdate();
                    break;
                case BackendCommand.AddScoresReq:
                    uint killer = NetHelper.ReadU32(m);
                    count = NetHelper.ReadU32(m);
                    List<uint> assists = new List<uint>();
                    for (int i = 0; i < count; i++)
                        assists.Add(NetHelper.ReadU32(m));
                    foreach (PlayerScoreEntry[] list in TeamDeathMatchServerLogic.playerScoresPerLocation)
                        foreach (PlayerScoreEntry e in list)
                            if (e.netObjID == killer)
                                e.kills++;
                            else if (client.objIDs.Contains(e.netObjID))
                                e.deaths++;
                            else if (assists.Contains(e.netObjID))
                                e.assists++;
                    SendScoreBoardUpdate();
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

        public static void SendScoreBoardUpdate()
        {
            MemoryStream m = new MemoryStream();
            NetHelper.WriteU32(m, (uint)TeamDeathMatchServerLogic.playerScoresPerLocation.Count);
            foreach (PlayerScoreEntry[] list in TeamDeathMatchServerLogic.playerScoresPerLocation)
            {
                NetHelper.WriteU32(m, (uint)list.Length);
                foreach (PlayerScoreEntry e in list)
                    e.Write(m);
            }
            Backend.BroadcastCommand((uint)BackendCommand.GetPlayerScoresRes, m.ToArray());
            EnvServer.SendScoresUpdateRequest(m.ToArray());
        }
    }
}
