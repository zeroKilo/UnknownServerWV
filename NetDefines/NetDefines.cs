﻿using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace NetDefines
{
    public enum Item
    {
        //Ammo
        AmmoBoxMagnum300,
        AmmoBoxACP45,
        AmmoBoxGauge12,
        AmmoBoxNato556mm,
        AmmoBoxNato762mm,
        AmmoBoxPistol9mm,
        AmmoBoxFlare,
        AmmoBolt,

        //Magazines
        BigMagQ,
        BigMagEX,
        BigMagEXQ,
        SmallMagQ,
        SmallMagEX,
        SmallMagEXQ,
        SniperMagQ,
        SniperMagEX,
        SniperMagEXQ,
        MagDP28,
        MagM249,
        MagPP19,

        //Helmets
        HelmetL1,
        HelmetL2,
        HelmetL3,

        //Rifles
        AK47,
        AUG_A3,
        DP28,
        G36C,
        Groza,
        M16A4,
        M249,
        M416,
        M762,
        MK47,
        PP19,
        S12K,
        S686,
        SCAR_L,
        Thompson,
        QBZ,
        UMP,
        UZI,
        UZI_PRO,
        Vector,
        Win1897,

        //Snipers
        AWM,
        Kar98,
        M24,
        Mini14,
        MosinNagant,
        MK14,
        QBU,
        SKS,
        SLR,
        VSS,
        Win1894,

        //Pistols
        P18,
        P1911,
        P92,
        R45,
        SawedOff,
        Skorpion,

        //Grenades
        SmokeGrenade,

        //Melee
        Pan,

        //Scopes
        ScopeHD,
        ScopeRedDot,
        Scope2X,
        Scope3X,
        Scope4X,
        Scope6X,
        Scope8X,

        //Muzzle
        Choke,
        DuckBill,
        Large_Compensator,
        Large_FlashHider,
        Large_Suppressor,
        Mid_Compensator,
        Mid_FlashHider,
        Mid_Suppressor,
        Small_Suppressor,
        Sniper_Compensator,
        Sniper_FlashHider,
        Sniper_Suppressor,

        //Stocks
        TacticalStock,
        SniperStock,
        UziStock,
        ShotgunBulletLoop,
        SniperBulletLoop,

        //Grips
        AngledGrip,
        HalfGrip,
        LightGrip,
        ThumbGrip,
        VerticalGrip,

        //Health
        Bandage,
        EnergyDrink,
        Pills,
        Injection,
        FirstAid,
        FirstAidBox,

        //Undefined
        UNDEFINED = 0x7FFFFFFF
    }

    public enum FireMode
    {
        Single,
        Burst,
        Auto
    }

    public enum BackendCommand
    {
        WelcomeReq,
        WelcomeRes,
        PingReq,
        PingRes,
        LoginReq,
        LoginSuccessRes,
        LoginFailRes,
        GetItemConfigReq,
        GetItemConfigRes,
        GetMapReq,
        GetMapRes,
        GetSpawnLocReq,
        GetSpawnLocRes,
        DeleteObjectsReq,
        DeleteObjectsRes,
        GetAllObjectsReq,
        GetAllObjectsRes,
        CreatePlayerObjectReq,
        CreatePlayerObjectRes,
        CreateEnemyObjectReq,
        CreateEnemyObjectRes,
        ReloadTriggeredReq,
        ReloadTriggeredRes,
        InventoryUpdateReq,
        InventoryUpdateRes,
        ShotTriggeredReq,
        ImpactTriggeredReq,
        DoorStateChangedReq,
        GetDoorStatesReq,
        GetDoorStatesRes,
        ServerStateChangedReq,
        PlayerReadyReq,
        PlayerNotReadyReq,
        SetCountDownNumberReq,
        SetFlightPathReq,
        PlayerHitReq,
        PlayerDiedReq,
        SpawnGroupItemReq,
        SpawnGroupItemRemoveReq,
        ItemDroppedReq,
        RemoveDroppedItemReq,
        RemoveContainerItemReq,
        SpawnGroupRemovalsReq,
        GetAllPickupsReq,
        GetAllItemContainersReq,
        UpdateBlueZoneReq,
        PlayFootStepSoundReq,
        GetPlayersOnServerReq,
        GetPlayersOnServerRes,
        RefreshPlayerListReq,
        TeamInviteReq,
        TeamInviteAcceptReq,
        TeamLeaveReq,
        SetTeamReadyStateReq,
        PickupInfiniteItemReq,
        KillsToWinReq,
        KillsToWinRes,
        UpdateRoundTimeReq,
        GetPlayerScoresReq,
        GetPlayerScoresRes,
        AddScoresReq,
        TryEnterVehicleReq,
        TryEnterVehicleRes,
        TryExitVehicleReq,
        TryExitVehicleRes,
        ChangeVehicleSeatIDReq,
        ChangeVehicleSeatIDRes,
        GetAllMovingTargetsReq,
        GetAllMovingTargetsRes,
        MovingTargetHitReq,
        MovingTargetHitRes
    }

    public enum EnvServerCommand
    {
        PingReq,
        PingRes,
        LoadMapReq,
        LoadMapRes,
        MapLoadedReq,
        MapLoadedRes,
        SpawnVehiclesReq,
        SpawnVehiclesRes,
        SpawnPlayerReq,
        SpawnPlayerRes,
        ChangeControlVehicleReq,
        ChangeControlVehicleRes,
        ChangeVehicleSeatIDReq,
        ChangeVehicleSeatIDRes,
        DeleteObjectsReq,
        DeleteObjectsRes,
        SetBotCountReq,
        SetBotCountRes,
        BackendStateChangedReq,
        BackendStateChangedRes,
        ShotTriggeredReq,
        ShotTriggeredRes,
        PlayerHitReq,
        PlayerHitRes,
        ImpactTriggeredReq,
        ImpactTriggeredRes,
        InventoryUpdateReq,
        InventoryUpdateRes,
        PlayerDiedReq,
        PlayerDiedRes,
        ScoresUpdateReq,
        ScoresUpdateRes,
        ReloadTriggeredReq,
        ReloadTriggeredRes,
        CreateMovingTargetNetIdsReq,
        CreateMovingTargetNetIdsRes,
        MovingTargetHitReq, 
        MovingTargetHitRes,
    }

    public enum HitLocation
    {
        Limbs,
        Body,
        Head
    }

    public enum SpawnTierLevel
    {
        Low,
        Mid,
        High
    }

    public enum ItemContainerType
    {
        PlayerCrate,
        CarePackage
    }

    public enum BlueZoneState
    {
        Waiting,
        Shrinking,
        Stop
    }

    public enum ServerMode
    {
        Offline,
        BattleRoyaleMode,
        DeathMatchMode,
        TeamDeathMatchMode,
        FreeExploreMode
    }

    public enum ServerModeState
    {
        Offline,
        BR_LobbyState,
        BR_CountDownState,
        BR_MainGameState,
        TDM_LobbyState,
        TDM_CountDownState,
        TDM_MainGameState,
        TDM_RoundEndState,
        DM_LobbyState,
        DM_CountDownState,
        DM_MainGameState,
        DM_RoundEndState,
        FEM_LobbyState
    }

    public enum ReplayPacketTypes
    {
        TCP_Player,
        TCP_Env,
        UDP
    }

    public static class NetConstants
    {
        public static uint PACKET_MAGIC = 0x57564E54;

        public static string[] ServerModeNames = new string[]
        {
            "Offline",
            "Battle Royale Mode",
            "Death Match Mode",
            "Team Death Match Mode",
            "Free Explore Mode"
        };
    }

    public class ServerPlayerInfo
    {
        public uint ID;
        public uint teamID;
        public bool isReady;
        public string name;
        public List<uint> netObjList = new List<uint>();

        public ServerPlayerInfo() { }

        public ServerPlayerInfo(Stream s)
        {
            Read(s);
        }

        public ServerPlayerInfo(uint id, uint tID, bool ready, string n)
        {
            ID = id;
            teamID = tID;
            isReady = ready;
            name = n;
        }

        public void Write(Stream s)
        {
            NetHelper.WriteU32(s, ID);
            NetHelper.WriteU32(s, teamID);
            s.WriteByte((byte)(isReady ? 1 : 0));
            NetHelper.WriteU32(s, (uint)name.Length);
            foreach (char c in name)
                s.WriteByte((byte)c);
            NetHelper.WriteU32(s, (uint)netObjList.Count);
            foreach (uint u in netObjList)
                NetHelper.WriteU32(s, u);
        }

        public void Read(Stream s)
        {
            ID = NetHelper.ReadU32(s);
            teamID = NetHelper.ReadU32(s);
            isReady = s.ReadByte() != 0;
            int count = (int)NetHelper.ReadU32(s);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append((char)s.ReadByte());
            name = sb.ToString();
            netObjList.Clear();
            count = (int)NetHelper.ReadU32(s);
            for (int i = 0; i < count; i++)
                netObjList.Add(NetHelper.ReadU32(s));
        }

        public uint Hash()
        {
            uint result = ID + teamID + (uint)name.Length + (uint)(isReady ? 123 : 321);
            foreach (char c in name)
                result += (uint)c;
            foreach (uint u in netObjList)
                result += u;
            return result;
        }
    }

    public class PlayerScoreEntry
    {
        public uint netObjID;
        public uint kills;
        public uint deaths;
        public uint assists;
        public bool isBot;
        public PlayerScoreEntry(uint ID, bool bot = false)
        {
            netObjID = ID;
            kills = deaths = assists = 0;
            isBot = bot;
        }

        public PlayerScoreEntry(Stream s)
        {
            Read(s);
        }

        public void Read(Stream s)
        {
            netObjID = NetHelper.ReadU32(s);
            kills = NetHelper.ReadU32(s);
            deaths = NetHelper.ReadU32(s);
            assists = NetHelper.ReadU32(s);
            isBot = s.ReadByte() != 0;
        }

        public void Write(Stream s)
        {
            NetHelper.WriteU32(s, netObjID);
            NetHelper.WriteU32(s, kills);
            NetHelper.WriteU32(s, deaths);
            NetHelper.WriteU32(s, assists);
            s.WriteByte((byte)(isBot ? 1 : 0));
        }
    }

    public class SpawnGroupRemoveInfo
    {
        public float[] location = new float[3];
        public List<int> indicies = new List<int>();

        public SpawnGroupRemoveInfo(float[] pos)
        {
            location = pos;
        }

        public void AddRemoval(int index)
        {
            indicies.Add(index);
        }
    }

    public class DroppedItemInfo
    {
        public float[] location = new float[3];
        public ItemSpawnInfo spawnInfo;
        public DroppedItemInfo(float[] pos, ItemSpawnInfo info)
        {
            location = pos;
            spawnInfo = info;
        }
    }

    public class ItemContainerInfo
    {
        public float[] location = new float[3];
        public ItemContainerType type;
        public string name;
        public List<ItemSpawnInfo> items = new List<ItemSpawnInfo>();
        public List<int> removedIndicies = new List<int>();

        public ItemContainerInfo()
        { }

        public ItemContainerInfo(Stream s)
        {
            Read(s);
        }

        public void Write(Stream s)
        {
            foreach (float f in location)
                NetHelper.WriteFloat(s, f);
            NetHelper.WriteU32(s, (uint)type);
            NetHelper.WriteU32(s, (uint)name.Length);
            foreach (char c in name)
                s.WriteByte((byte)c);
            NetHelper.WriteU32(s, (uint)items.Count);
            foreach (ItemSpawnInfo item in items)
                item.Write(s);
            NetHelper.WriteU32(s, (uint)removedIndicies.Count);
            foreach (uint i in removedIndicies)
                NetHelper.WriteU32(s, i);
        }

        public void Read(Stream s)
        {
            location = new float[] { NetHelper.ReadFloat(s), NetHelper.ReadFloat(s), NetHelper.ReadFloat(s) };
            type = (ItemContainerType)NetHelper.ReadU32(s);
            uint len = NetHelper.ReadU32(s);
            name = "";
            for (int j = 0; j < len; j++)
                name += (char)s.ReadByte();
            len = NetHelper.ReadU32(s);
            items = new List<ItemSpawnInfo>();
            for (int j = 0; j < len; j++)
                items.Add(new ItemSpawnInfo(s));
            len = NetHelper.ReadU32(s);
            for (int j = 0; j < len; j++)
                removedIndicies.Add((int)NetHelper.ReadU32(s));
        }
    }

    public class BlueZoneStateStep
    {
        public BlueZoneState state;
        public float radius;
        public float targetRadius;
        public float time;
        public float damage;
        public float[] center;
        public float[] nextCenter;

        public BlueZoneStateStep()
        { }

        public BlueZoneStateStep(BlueZoneState s, float r, float tr, float t, float d, float[] c, float[] nc)
        {
            state = s;
            radius = r;
            targetRadius = tr;
            time = t;
            damage = d;
            center = c;
            nextCenter = nc;
        }

        public void Read(Stream s)
        {
            state = (BlueZoneState)NetHelper.ReadU32(s);
            radius = NetHelper.ReadFloat(s);
            targetRadius = NetHelper.ReadFloat(s);
            time = NetHelper.ReadFloat(s);
            damage = NetHelper.ReadFloat(s);
            center = new float[] { NetHelper.ReadFloat(s), NetHelper.ReadFloat(s) };
            nextCenter = new float[] { NetHelper.ReadFloat(s), NetHelper.ReadFloat(s) };
        }

        public void Write(Stream s)
        {
            NetHelper.WriteU32(s, (uint)state);
            NetHelper.WriteFloat(s, radius);
            NetHelper.WriteFloat(s, targetRadius);
            NetHelper.WriteFloat(s, time);
            NetHelper.WriteFloat(s, damage);
            foreach (float f in center)
                NetHelper.WriteFloat(s, f);
            foreach (float f in nextCenter)
                NetHelper.WriteFloat(s, f);
        }
    }
}
