using NetDefines;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public static class TeamDeathMatchServerLogic
    {
        public static int neededPlayers;
        public static int countDownTime = 10000;
        public static int roundTime;
        public static int killsToWin;
        public static List<uint[]> playersPerLocation;
        public static List<PlayerScoreEntry[]> playerScoresPerLocation;
        private static Stopwatch sw = new Stopwatch();
        private static Stopwatch swLobby = new Stopwatch();
        private static int minWaitTimeLobbyMs = 3000;
        private static int maxWaitTimeLobbyMs = 60000;
        private static readonly object _syncExit = new object();
        private static readonly object _syncRunning = new object();
        private static bool _running = false;
        private static bool _exit = false;
        public static bool IsRunning
        {
            get
            {
                bool result = false;
                lock (_syncRunning)
                {
                    result = _running;
                }
                return result;
            }
            set
            {
                lock (_syncRunning)
                {
                    _running = value;
                }
            }
        }
        public static bool ShouldExit
        {
            get
            {
                bool result = false;
                lock (_syncExit)
                {
                    result = _exit;
                }
                return result;
            }
            set
            {
                lock (_syncExit)
                {
                    _exit = value;
                }
            }
        }
        public static void Start()
        {
            ShouldExit = false;
            IsRunning = false;
            TeamDeathMatchMode.ResetPlayerSpawnLocations();
            new Thread(tMain).Start();
        }

        public static void Stop()
        {
            ShouldExit = true;
            while (true)
            {
                if (!IsRunning)
                    break;
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static void tMain()
        {
            IsRunning = true;
            long lastTick = 0;
            Log.Print("SERVERLOGIC main loop running...");
            sw.Restart();
            swLobby.Restart();
            minWaitTimeLobbyMs = int.Parse(Config.settings["min_lobby_wait"]);
            maxWaitTimeLobbyMs = int.Parse(Config.settings["max_lobby_wait"]);
            while (true)
            {
                if (ShouldExit)
                    break;
                switch (Backend.modeState)
                {
                    case ServerModeState.TDM_LobbyState:
                        if (swLobby.ElapsedMilliseconds > maxWaitTimeLobbyMs)
                        {
                            ShutDown("Lobby timeout");
                            break;
                        }
                        if (Backend.clientList.Count != neededPlayers)
                            sw.Stop();
                        else
                        {
                            bool found = false;
                            foreach (ClientInfo c in Backend.clientList)
                                lock (c._sync)
                                {
                                    if (!c.isReady)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            if (!found)
                            {
                                if (!sw.IsRunning)
                                {
                                    Log.Print("Have enough players ready, starting in " + minWaitTimeLobbyMs + "ms");
                                    sw.Start();
                                }
                                if (sw.ElapsedMilliseconds > minWaitTimeLobbyMs)
                                {
                                    Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.TDM_CountDownState);
                                    MemoryStream m = new MemoryStream();
                                    NetHelper.WriteU32(m, (uint)countDownTime);
                                    Backend.BroadcastCommand((uint)BackendCommand.SetCountDownNumberReq, m.ToArray());
                                    sw.Restart();
                                }
                            }
                        }
                        break;
                    case ServerModeState.TDM_CountDownState:
                        if (sw.ElapsedMilliseconds > countDownTime)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.TDM_MainGameState);
                            sw.Restart();
                            lastTick = sw.ElapsedMilliseconds / 1000;
                            playerScoresPerLocation = new List<PlayerScoreEntry[]>();
                            foreach(uint[] playerList in playersPerLocation)
                            {
                                List<PlayerScoreEntry> list = new List<PlayerScoreEntry>();
                                foreach (uint playerID in playerList)
                                    list.Add(new PlayerScoreEntry(playerID));
                                playerScoresPerLocation.Add(list.ToArray());
                            }
                            TeamDeathMatchMode.SendScoreBoardUpdate();
                        }
                        break;
                    case ServerModeState.TDM_MainGameState:
                        long temp = sw.ElapsedMilliseconds / 1000;
                        if (temp != lastTick)
                        {
                            lastTick = temp;
                            MemoryStream m = new MemoryStream();
                            int timeLeft = roundTime - (int)temp;
                            if (timeLeft >= 0)
                            {
                                NetHelper.WriteU32(m, (uint)timeLeft);
                                Backend.BroadcastCommand((uint)BackendCommand.UpdateRoundTimeReq, m.ToArray());
                            }
                            else
                            {
                                TeamDeathMatchMode.SendScoreBoardUpdate();
                                Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.TDM_RoundEndState);
                                sw.Restart();
                            }
                            foreach(PlayerScoreEntry[] scores in playerScoresPerLocation)
                            {
                                uint total = 0;
                                foreach (PlayerScoreEntry e in scores)
                                    total += e.kills;
                                if(total >= killsToWin)
                                {
                                    TeamDeathMatchMode.SendScoreBoardUpdate();
                                    Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.TDM_RoundEndState);
                                    sw.Restart();
                                    break;
                                }
                            }
                        }
                        if (Backend.clientList.Count == 0)
                            ShutDown("All player left");
                        break;
                    case ServerModeState.TDM_RoundEndState:
                        if(sw.ElapsedMilliseconds > 60000)
                            ShutDown("Round ended");
                        break;
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            IsRunning = false;
        }

        private static void ShutDown(string reason)
        {
            Log.Print("Shutting down, reason: " + reason);
            Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.Offline);
            DoorManager.Reset();
            SpawnManager.Reset();
            ObjectManager.Reset();
            TeamDeathMatchMode.ResetPlayerSpawnLocations();
            sw.Stop();
            swLobby.Stop();
            ShouldExit = true;
            IsRunning = false;
        }
    }
}
