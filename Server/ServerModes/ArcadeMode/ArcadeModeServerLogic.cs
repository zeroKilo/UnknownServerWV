using NetDefines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public static class ArcadeModeServerLogic
    {
        private static readonly object _sync = new object();
        private static bool _exit = false;
        private static bool _running = false;
        private static Stopwatch sw = new Stopwatch();
        public static void Start()
        {
            _exit = false;
            _running = false;
            DoorManager.Reset();
            SpawnManager.Reset();
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
                    case ServerModeState.ARC_LobbyState:
                        break;                    
                }
                Thread.Sleep(10);
            }
            Log.Print("SERVERLOGIC main loop stopped...");
            _running = false;
        }
    }
}
