using NetDefines;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public class FreeExploreServerLogic
    {
        public static List<uint> playerIDs = new List<uint>();
        public static int roundTime;
        private static readonly Stopwatch sw = new Stopwatch();
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
            sw.Restart();
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

        public static void ThreadMain()
        {
            IsRunning = true;
            playerIDs = new List<uint>();
            Log.Print("SERVERLOGIC main loop running...");
            Backend.modeState = ServerModeState.FEM_LobbyState;
            Backend.PlayersNeeded =
            Backend.PlayersReady =
            Backend.PlayersWaiting = 0;
            Backend.BroadcastServerStateChange(ServerMode.FreeExploreMode, ServerModeState.FEM_LobbyState);
            while (true)
            {
                if (ShouldExit)
                    break;
                switch (Backend.modeState)
                {
                    case ServerModeState.FEM_LobbyState:
                        if (sw.Elapsed.TotalSeconds > roundTime)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.FreeExploreMode, ServerModeState.Offline);
                            sw.Stop();
                            ShouldExit = true;
                            IsRunning = false;
                        }
                        break;
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            IsRunning = false;
        }
    }
}
