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
        private static readonly Stopwatch sw = new Stopwatch();
        private static readonly Stopwatch swLobby = new Stopwatch();
        private static int minWaitTimeLobbyMs = 3000;
        private static int maxWaitTimeLobbyMs = 60000;
        private static readonly Random rnd = new Random();
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
        public static void ThreadMain()
        {
            IsRunning = true;
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
            IsRunning = false;
        }

        public static void Start()
        {
            ShouldExit = false;
            IsRunning = false;
            swLobby.Restart();
            new Thread(ThreadMain).Start();
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

        private static void ShutDown()
        {
            Backend.BroadcastServerStateChange(ServerMode.BattleRoyaleMode, ServerModeState.Offline);
            DoorManager.Reset();
            SpawnManager.Reset();
            ObjectManager.Reset();
            sw.Stop();
            swLobby.Stop();
            ShouldExit = true;
            IsRunning = false;
        }
    }
}
