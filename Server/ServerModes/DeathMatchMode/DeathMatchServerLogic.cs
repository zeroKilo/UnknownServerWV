using NetDefines;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public class DeathMatchServerLogic
    {
        public static int neededPlayers;
        public static int countDownTime = 10000;
        public static int roundTime;
        public static int killsToWin;
        public static List<uint> playerIDs = new List<uint>();
        public static List<PlayerScoreEntry> playerScores = new List<PlayerScoreEntry>();
        private static readonly object _sync = new object();
        private static bool _exit = false;
        private static bool _running = false;
        private static Stopwatch sw = new Stopwatch();
        private static int minWaitTimeLobbyMs = 3000;
        public static void Start()
        {
            _exit = false;
            _running = false;
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
            playerIDs = new List<uint>();
            playerScores = new List<PlayerScoreEntry>();
            Log.Print("SERVERLOGIC main loop running...");
            sw.Start();
            while (true)
            {
                lock (_sync)
                {
                    if (_exit)
                        break;
                }
                switch (Backend.modeState)
                {
                    case ServerModeState.DM_LobbyState:
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
                                    Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_CountDownState);
                                    MemoryStream m = new MemoryStream();
                                    NetHelper.WriteU32(m, (uint)countDownTime);
                                    Backend.BroadcastCommand((uint)BackendCommand.SetCountDownNumberReq, m.ToArray());
                                    sw.Restart();
                                }
                            }
                        }
                        break;
                    case ServerModeState.DM_CountDownState:
                        if (sw.ElapsedMilliseconds > countDownTime)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_MainGameState);
                            sw.Restart();
                            lastTick = sw.ElapsedMilliseconds / 1000;
                            playerScores = new List<PlayerScoreEntry>();
                            for (int i = 0; i < playerIDs.Count; i++)
                                playerScores.Add(new PlayerScoreEntry(playerIDs[i]));
                            DeathMatchMode.SendScoreBoardUpdate();
                        }
                        break;
                    case ServerModeState.DM_MainGameState:
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
                                DeathMatchMode.SendScoreBoardUpdate();
                                Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_RoundEndState);
                                sw.Restart();
                            }
                            foreach (PlayerScoreEntry score in playerScores)
                            {
                                if (score.kills >= killsToWin)
                                {
                                    DeathMatchMode.SendScoreBoardUpdate();
                                    Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_RoundEndState);
                                    sw.Restart();
                                    break;
                                }
                            }
                        }
                        if (Backend.clientList.Count == 0)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_LobbyState);
                            DoorManager.Reset();
                            SpawnManager.Reset();
                            ObjectManager.Reset();
                            sw.Restart();
                        }
                        break;
                    case ServerModeState.DM_RoundEndState:
                        if (sw.ElapsedMilliseconds > 15000)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.DeathMatchMode, ServerModeState.DM_LobbyState);
                            DoorManager.Reset();
                            SpawnManager.Reset();
                            ObjectManager.Reset();
                            sw.Restart();
                            playerIDs = new List<uint>();
                            playerScores = new List<PlayerScoreEntry>();
                        }
                        break;
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            _running = false;
        }
    }
}
