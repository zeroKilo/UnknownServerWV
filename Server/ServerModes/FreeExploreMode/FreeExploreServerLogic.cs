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
        private static readonly object _sync = new object();
        private static bool _exit = false;
        private static bool _running = false;
        private static Stopwatch sw = new Stopwatch();

        public static void Start()
        {
            _exit = false;
            _running = false;
            sw.Start();
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
            playerIDs = new List<uint>();
            Log.Print("SERVERLOGIC main loop running...");
            while (true)
            {
                lock (_sync)
                {
                    if (_exit)
                        break;
                }
                switch (Backend.modeState)
                {
                    case ServerModeState.FEM_LobbyState:
                        if (sw.Elapsed.TotalSeconds > roundTime)
                        {
                            Backend.BroadcastServerStateChange(ServerMode.FreeExploreMode, ServerModeState.Offline);
                            sw.Stop();
                            lock (_sync)
                            {
                                _exit = true;
                                _running = false;
                            }
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
