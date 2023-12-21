using System.IO;
using System.Collections.Generic;

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
        AddScoresReq
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

    public class PlayerScoreEntry
    {
        public uint playerID;
        public uint kills;
        public uint deaths;
        public uint assists;
        public PlayerScoreEntry(uint ID)
        {
            playerID = ID;
            kills = deaths = assists = 0;
        }

        public PlayerScoreEntry(Stream s)
        {
            Read(s);
        }

        public void Read(Stream s)
        {
            playerID = NetHelper.ReadU32(s);
            kills = NetHelper.ReadU32(s);
            deaths = NetHelper.ReadU32(s);
            assists = NetHelper.ReadU32(s);
        }

        public void Write(Stream s)
        {
            NetHelper.WriteU32(s, playerID);
            NetHelper.WriteU32(s, kills);
            NetHelper.WriteU32(s, deaths);
            NetHelper.WriteU32(s, assists);
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
                name = name + (char)s.ReadByte();
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
