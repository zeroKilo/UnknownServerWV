using System;
using System.Windows.Forms;
using Server;
using System.Text;
using System.IO;
using System.Threading;
using NetDefines;

namespace UnknownServerWV
{
    public partial class Form1 : Form
    {
        private int playlistIndex;
        private int playlistCount;
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
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Init(rtb1);
            Config.Init();
            PlaylistManager.Init();
            RefreshPlaylist();
            if (Config.settings.ContainsKey("autostart"))
            {
                string v = Config.settings["autostart"];
                if (v == "1")
                    OnStart();
            }
        }

        public void RefreshPlaylist()
        {
            listBox1.Items.Clear();
            int idx = 0;
            foreach (PlaylistManager.PlaylistEntry entry in PlaylistManager.playlist)
                listBox1.Items.Add(idx++.ToString("D2") + " : " + entry);
            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;
        }

        private void LoadPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog
            {
                Filter = "*.txt|*.txt"
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                string data = File.ReadAllText(d.FileName);
                File.WriteAllText(PlaylistManager.defaultName, data);
                PlaylistManager.Init();
                RefreshPlaylist();
            }
        }

        private void ExportPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog
            {
                Filter = "*.txt|*.txt"
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("#do no edit by hand!");
                foreach (PlaylistManager.PlaylistEntry entry in PlaylistManager.playlist)
                    sb.AppendLine(entry.Save());
                File.WriteAllText(d.FileName, sb.ToString());
            }
        }

        private void AddEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlaylistEntryEditor ed = new PlaylistEntryEditor
            {
                entry = new PlaylistManager.PlaylistEntry()
            };
            if (ed.ShowDialog() == DialogResult.OK)
            {
                PlaylistManager.playlist.Add(ed.entry);
                RefreshPlaylist();
            }
        }

        private void EditEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            PlaylistEntryEditor ed = new PlaylistEntryEditor
            {
                entry = PlaylistManager.playlist[n]
            };
            if (ed.ShowDialog() == DialogResult.OK)
            {
                PlaylistManager.playlist[n] = ed.entry;
                RefreshPlaylist();
            }
        }

        private void DeleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            PlaylistManager.playlist.RemoveAt(n);
            RefreshPlaylist();
        }

        private void MoveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n < 1)
                return;
            PlaylistManager.PlaylistEntry entry = PlaylistManager.playlist[n - 1];
            PlaylistManager.playlist[n - 1] = PlaylistManager.playlist[n];
            PlaylistManager.playlist[n] = entry;
            RefreshPlaylist();
            listBox1.SelectedIndex = n - 1;
        }

        private void MoveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1 || n >= PlaylistManager.playlist.Count - 1)
                return;
            PlaylistManager.PlaylistEntry entry = PlaylistManager.playlist[n + 1];
            PlaylistManager.playlist[n + 1] = PlaylistManager.playlist[n];
            PlaylistManager.playlist[n] = entry;
            RefreshPlaylist();
            listBox1.SelectedIndex = n + 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            OnStop();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#do no edit by hand!");
            foreach (PlaylistManager.PlaylistEntry entry in PlaylistManager.playlist)
                sb.AppendLine(entry.Save());
            File.WriteAllText("playlist.txt", sb.ToString());
        }

        private void MenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "Start":
                    OnStart();
                    break;
                case "Stop":
                    OnStop();
                    break;
                case "Object Viewer":
                    OnObjectViewer();
                    break;
                case "Export Server Info":
                    OnExportServerInfo();
                    break;
            }
        }

        private void OnStart()
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
            {
                if (listBox1.Items.Count > 0)
                    n = 0;
                else
                    return;
            }
            if (IsRunning)
                return;
            playlistIndex = listBox1.SelectedIndex;
            playlistCount = listBox1.Items.Count;
            new Thread(ThreadRun).Start();
        }

        private void ThreadRun(object obj)
        {
            IsRunning = true;
            ShouldExit = false;
            while (true)
            {
                ObjectManager.Reset();
                DoorManager.Reset();
                SpawnManager.Reset();
                PlaylistManager.PlaylistEntry entry = PlaylistManager.playlist[playlistIndex];
                ServerMode mode = entry.mode;
                switch (mode)
                {
                    case ServerMode.DeathMatchMode:
                        Backend.currentMap =
                        DeathMatchMode.mapName = DeathMatchMode.mapInfos[entry.map].name;
                        DeathMatchServerLogic.neededPlayers = entry.minPlayer;
                        DeathMatchServerLogic.countDownTime = entry.countDown * 1000;
                        DeathMatchServerLogic.roundTime = entry.roundTime * 60;
                        DeathMatchServerLogic.killsToWin = entry.killsToWin;
                        DeathMatchMode.Start();
                        break;
                    case ServerMode.TeamDeathMatchMode:
                        Backend.currentMap =
                        TeamDeathMatchMode.mapName = TeamDeathMatchMode.mapInfos[entry.map].name;
                        TeamDeathMatchServerLogic.neededPlayers = entry.minPlayer;
                        TeamDeathMatchServerLogic.countDownTime = entry.countDown * 1000;
                        TeamDeathMatchServerLogic.roundTime = entry.roundTime * 60;
                        TeamDeathMatchServerLogic.killsToWin = entry.killsToWin;
                        TeamDeathMatchMode.Start();
                        break;
                    case ServerMode.BattleRoyaleMode:
                        Backend.currentMap =
                        BattleRoyaleMode.mapName = BattleRoyaleMode.mapInfos[entry.map].name;
                        BattleRoyaleMode.spawnLocIdx = entry.spawnLoc;
                        BattleRoyaleMode.spawnLocNames = BattleRoyaleMode.mapInfos[entry.spawnLoc].spawnLocations.ToArray();
                        BattleRoyaleMode.Start();
                        BattleRoyaleServerLogic.neededPlayers = entry.minPlayer;
                        BattleRoyaleServerLogic.countDownTime = entry.countDown * 1000;
                        break;
                    case ServerMode.FreeExploreMode:
                        Backend.currentMap =
                        FreeExploreMode.mapName = FreeExploreMode.mapInfos[entry.map].name;
                        FreeExploreMode.spawnLocIdx = entry.spawnLoc;
                        FreeExploreMode.spawnLocNames = FreeExploreMode.mapInfos[entry.spawnLoc].spawnLocations.ToArray();
                        FreeExploreServerLogic.roundTime = entry.roundTime * 60;
                        FreeExploreMode.Start();
                        break;
                    default:
                        return;
                }

                while (true)
                {
                    if (ShouldExit)
                        break;
                    if (Backend.modeState == ServerModeState.Offline)
                        break;
                    Thread.Sleep(1000);
                }

                switch (Backend.mode)
                {
                    case ServerMode.DeathMatchMode:
                        DeathMatchMode.Stop();
                        break;
                    case ServerMode.TeamDeathMatchMode:
                        TeamDeathMatchMode.Stop();
                        break;
                    case ServerMode.BattleRoyaleMode:
                        BattleRoyaleMode.Stop();
                        break;
                    case ServerMode.FreeExploreMode:
                        FreeExploreMode.Stop();
                        break;
                }
                if (ShouldExit)
                    break;
                playlistIndex = (playlistIndex + 1) % playlistCount;
            }
            IsRunning = false;
            Log.Print("Server main loop exited");
        }

        private void OnStop()
        {
            ShouldExit = true;
        }

        private void OnObjectViewer()
        {
            ObjectViewer ov = new ObjectViewer();
            ov.Show();
        }

        private void OnExportServerInfo()
        {
            SaveFileDialog d = new SaveFileDialog
            {
                Filter = "server.info|server.info",
                FileName = "server.info"
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("public=" + Config.pubKey);
                sb.AppendLine("name=" + Config.settings["name"]);
                sb.AppendLine("ip=127.0.0.1");
                sb.AppendLine("port_udp=" + Config.settings["port_udp"]);
                sb.AppendLine("port_tcp=" + Config.settings["port_tcp"]);
                File.WriteAllText(d.FileName, sb.ToString());
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            bool running = Backend.IsRunning;
            splitContainer1.Panel1.Enabled = !running;
            toolStripMenuItem1.Enabled = !running;
            toolStripMenuItem2.Enabled = running;
            if (running)
            {
                listBox1.SelectedIndex = playlistIndex;
                status.Text = "Status : UDP=0x" + MainServer.GetDataCount().ToString("X")
                            + " UDPErr=" + MainServer.GetErrorCount()
                            + " Mode=" + Backend.mode
                            + " State=" + Backend.modeState
                            + " Clients=" + Backend.clientList.Count 
                            + " Logins=" + StatusServer.LoginCount;
            }
        }

        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            rtb1.Text = "";
        }

        private void SpawnEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpawnEditor se = new SpawnEditor();
            se.ShowDialog();
        }
    }
}
