using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using NetDefines;
using System.Windows.Forms;

namespace Server
{
    public static class BattleRoyaleServerLogic
    {
        public static int neededPlayers;
        public static int countDownTime = 10000;
        private static readonly object _sync = new object();
        private static bool _exit = false;
        private static bool _running = false;
        private static Stopwatch sw = new Stopwatch();
        private static Stopwatch swLobby = new Stopwatch();
        private static int minWaitTimeLobbyMs = 3000;
        private static int maxWaitTimeLobbyMs = 60000;
        private static Random rnd = new Random();
        public static void tMain()
        {
            _running = true;
            Log.Print("SERVERLOGIC main loop running...");
            sw.Start();
            swLobby.Start();
            minWaitTimeLobbyMs = int.Parse(Config.settings["min_lobby_wait"]);
            maxWaitTimeLobbyMs = int.Parse(Config.settings["max_lobby_wait"]);
            while (true)
            {
                lock(_sync)
                {
                    if (_exit)
                        break;
                }
                switch (Backend.modeState)
                {
                    case ServerModeState.BR_LobbyState:
                        if(swLobby.ElapsedMilliseconds > maxWaitTimeLobbyMs)
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
                                    Backend.BroadcastServerStateChange(ServerMode.BattleRoyaleMode, ServerModeState.BR_CountDownState);
                                    MemoryStream m = new MemoryStream();
                                    NetHelper.WriteU32(m, (uint)countDownTime);
                                    Backend.BroadcastCommand((uint)BackendCommand.SetCountDownNumberReq, m.ToArray());
                                    sw.Restart();
                                }
                            }
                        }
                        break;
                    case ServerModeState.BR_CountDownState:
                        if(sw.ElapsedMilliseconds > countDownTime)
                        {
                            BlueZoneManager.Reset();
                            MemoryStream m = new MemoryStream();
                            float angle1 = (float)(rnd.NextDouble() * 360f);
                            float angle2 = angle1 + (10f + (float)(rnd.NextDouble() * 340f));
                            while (angle2 > 360f)
                                angle2 -= 360f;
                            NetHelper.WriteFloat(m, angle1);
                            NetHelper.WriteFloat(m, angle2);
                            Backend.BroadcastCommand((uint)BackendCommand.SetFlightPathReq, m.ToArray());
                            Backend.BroadcastServerStateChange(ServerMode.BattleRoyaleMode, ServerModeState.BR_MainGameState);
                            sw.Restart();
                        }
                        break;
                    case ServerModeState.BR_MainGameState:
                        BlueZoneManager.Update();
                        if (Backend.clientList.Count == 0)
                            ShutDown();
                        break;
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            _running = false;
        }

        public static void Start()
        {
            _exit = false;
            _running = false;
            swLobby.Restart();
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

        private static void ShutDown()
        {
            Backend.BroadcastServerStateChange(ServerMode.BattleRoyaleMode, ServerModeState.Offline);
            DoorManager.Reset();
            SpawnManager.Reset();
            ObjectManager.Reset();
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
