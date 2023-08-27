using NetDefines;
using System;
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
        private static readonly object _sync = new object();
        private static bool _exit = false;
        private static bool _running = false;
        private static Stopwatch sw = new Stopwatch();
        private static Stopwatch swLobby = new Stopwatch();
        private static int minWaitTimeLobbyMs = 3000;
        private static int maxWaitTimeLobbyMs = 60000;
        public static void Start()
        {
            _exit = false;
            _running = false;
            TeamDeathMatchMode.ResetPlayerSpawnLocations();
            new Thread(tMain).Start();
        }

        public static void Stop()
        {
            lock (_sync)
            {
                _exit = true;
            }
            while (true)
            {
                lock (_sync)
                {
                    if (!_running)
                        break;
                }
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static void tMain()
        {
            _running = true;
            long lastTick = 0;
            Log.Print("SERVERLOGIC main loop running...");
            sw.Start();
            swLobby.Start();
            minWaitTimeLobbyMs = int.Parse(Config.settings["min_lobby_wait"]);
            maxWaitTimeLobbyMs = int.Parse(Config.settings["max_lobby_wait"]);
            while (true)
            {
                lock (_sync)
                {
                    if (_exit)
                        break;
                }
                switch (Backend.modeState)
                {
                    case ServerModeState.TDM_LobbyState:
                        if (swLobby.ElapsedMilliseconds > maxWaitTimeLobbyMs)
                        {
                            ShutDown();
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
                            ShutDown();
                        break;
                    case ServerModeState.TDM_RoundEndState:
                        if(sw.ElapsedMilliseconds > 60000)
                            ShutDown();
                        break;
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            _running = false;
        }

        private static void ShutDown()
        {
            Backend.BroadcastServerStateChange(ServerMode.TeamDeathMatchMode, ServerModeState.Offline);
            DoorManager.Reset();
            SpawnManager.Reset();
            ObjectManager.Reset();
            TeamDeathMatchMode.ResetPlayerSpawnLocations();
            sw.Stop();
            swLobby.Stop();
            lock (_sync)
            {
                _exit = true;
                _running = false;
            }
        }
    }
}
