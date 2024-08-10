using NetDefines;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace GameDataServer
{
    public partial class Form1 : Form
    {
        public GameServer[] servers = { };
        public PlayerProfile[] profiles = { };
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Init(rtb1);
            Config.Init();
            string key = "path_db";
            if (!Config.settings.ContainsKey(key))
            {
                MessageBox.Show("config setting for '" + key + "' not found, exiting!");
                this.Close();
            }
            DBManager.Init(Config.settings[key]);
            RefreshAll();
            if (Config.settings.ContainsKey("autostart"))
            {
                string v = Config.settings["autostart"];
                if (v == "1")
                    StartToolStripMenuItem_Click(null, null);
            }
        }

        private void StartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameDataServer.Start();
            Thread.Sleep(100);
            if (GameDataServer.IsRunning())
            {
                startToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
            }
        }

        private void StopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameDataServer.Stop();
            Thread.Sleep(100);
            if (!GameDataServer.IsRunning())
            {
                startToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
            }
        }

        private void RefreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            DBManager.Update();
            DBManager.Reload();
            servers = DBManager.GetServerProfiles();
            profiles = DBManager.GetPlayerProfiles();
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            foreach (GameServer gs in servers)
                listBox1.Items.Add(gs.Name);
            foreach (PlayerProfile p in profiles)
                listBox2.Items.Add(p.Name);
            Log.Print("Refreshed display");
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            bool needsUpdate = DBManager.NeedsUpdate();
            if (needsUpdate)
            {
                foreach (GameServer gs in servers)
                    if (gs.NeedsUpdate)
                        DBManager.UpdateGameServer(gs);
                foreach (PlayerProfile p in profiles)
                    if (p.NeedsUpdate)
                        DBManager.UpdatePlayerProfile(p);
                RefreshAll();
                Log.Print("Saved changes to db");
            }
        }

        private void AddServer_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog
            {
                Filter = "server.info|server.info"
            };
            if (d.ShowDialog() == DialogResult.OK)
                AddServer(d.FileName);
        }

        private void AddServer(string path)
        {
            string[] lines = File.ReadAllLines(path);
            string pubKey = null, name = null, ip = null, portUDP = null, portTCP = null;
            foreach (string line in lines)
            {
                if (!line.Contains("="))
                    continue;
                string[] parts = line.Split('=');
                if (parts.Length != 2)
                    continue;
                string value = parts[1].Trim();
                switch (parts[0].Trim().ToLower())
                {
                    case "pubkey":
                        pubKey = value;
                        break;
                    case "name":
                        name = value;
                        break;
                    case "port_udp":
                        portUDP = value;
                        break;
                    case "port_tcp":
                        portTCP = value;
                        break;
                    case "ip":
                        ip = value;
                        break;
                }
            }
            if (pubKey != null &&
                name != null &&
                portUDP != null &&
                portTCP != null &&
                ip != null)
            {
                GameServer gs = new GameServer(-1, pubKey, name, ip, portUDP, portTCP, "{}");
                DBManager.AddGameServer(gs);
                RefreshAll();
            }
        }

        private void EditServer_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
        }

        private void RemoveServer_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            DBManager.RemoveGameServer(servers[n]);
            RefreshAll();
        }

        private void AddProfile_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog
            {
                Filter = "player.info|player.info"
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(d.FileName);
                string metaData = "{\"creationDate\":" + DateTimeOffset.Now.ToUnixTimeSeconds() + "}";
                PlayerProfile p = new PlayerProfile(-1, lines[1].Split('=')[1].Trim(), lines[0].Split('=')[1].Trim(), metaData);
                DBManager.AddPlayerProfile(p);
                RefreshAll();
            }
        }

        private void EditProfile_Click(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
        }

        private void RemoveProfile_Click(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            DBManager.RemovePlayerProfile(profiles[n]);
            RefreshAll();
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Public key : " + servers[n].PublicKey);
            sb.AppendLine("Name       : " + servers[n].Name);
            sb.AppendLine("IP         : " + servers[n].IP);
            sb.AppendLine("Port UDP   : " + servers[n].PortUDP);
            sb.AppendLine("Port TCP   : " + servers[n].PortTCP);
            sb.AppendLine("Status     : " + servers[n].Status);
            rtb2.Text = sb.ToString();
        }

        private void ListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            //rtb3.Text = "Public Key: " + profiles[n].PublicKey + "\n" + profiles[n].MetaData;
            rtb3.Text = profiles[n].TryParseMetaData(servers);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            GameDataServer.Stop();
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigDialog d = new ConfigDialog();
            if (File.Exists("config.txt"))
                d.rtb1.Text = File.ReadAllText("config.txt");
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("#start server on load, 1 or 0");
                sb.AppendLine("autostart = 1");
                sb.AppendLine();
                sb.AppendLine("#client ping timeout in ms");
                sb.AppendLine("timeout = 30000");
                sb.AppendLine();
                sb.AppendLine("#server bind ip");
                sb.AppendLine("ip = 127.0.0.1");
                sb.AppendLine();
                sb.AppendLine("#server tcp port");
                sb.AppendLine("port_tcp = 4321");
                sb.AppendLine();
                sb.AppendLine("#database path");
                sb.AppendLine("path_db = data.db");
                sb.AppendLine();
                sb.AppendLine("# use https (needs ssl/tls certificate!)");
                sb.AppendLine("use_https = 0");
                d.rtb1.Text = sb.ToString();
            }
            if (d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText("config.txt", d.rtb1.Text);
                Config.Init();
            }
        }

        private void SetupAndAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog
            {
                Filter = "UnknownServerWV.exe|UnknownServerWV.exe"
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                string path = Path.GetDirectoryName(d.FileName) + "\\";
                ServerSetupDialog d2 = new ServerSetupDialog();
                if (d2.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("#server name");
                    sb.AppendLine("name = " + d2.textBox1.Text);
                    sb.AppendLine();
                    sb.AppendLine("#start server on load, 1 or 0");
                    sb.AppendLine("autostart = " + d2.textBox2.Text);
                    sb.AppendLine();
                    sb.AppendLine("#client ping timeout in ms");
                    sb.AppendLine("timeout = " + d2.textBox3.Text);
                    sb.AppendLine();
                    sb.AppendLine("#server bind ip");
                    sb.AppendLine("bind_ip = " + d2.textBox4.Text);
                    sb.AppendLine();
                    sb.AppendLine("#server backend tcp port (from [min] to [min+range])");
                    sb.AppendLine("port_tcp_min = " + d2.textBox5.Text);
                    sb.AppendLine("port_tcp_range = " + d2.textBox6.Text);
                    sb.AppendLine();
                    sb.AppendLine("#server dedicated udp port (from [min] to [min+range])");
                    sb.AppendLine("port_udp_min = " + d2.textBox7.Text);
                    sb.AppendLine("port_udp_range = " + d2.textBox8.Text);
                    sb.AppendLine();
                    sb.AppendLine("#min/max waiting time in lobby in ms");
                    sb.AppendLine("min_lobby_wait = " + d2.textBox9.Text);
                    sb.AppendLine("max_lobby_wait = " + d2.textBox10.Text);
                    sb.AppendLine();
                    sb.AppendLine("#game data server");
                    sb.AppendLine("gds_ip = " + Config.settings["ip"]);
                    sb.AppendLine("gds_port = " + Config.settings["port_tcp"]);
                    sb.AppendLine("gds_wait = 10");
                    sb.AppendLine();
                    sb.AppendLine("#use https (needs ssl/tls certificate!)");
                    sb.AppendLine("use_https = 0");
                    sb.AppendLine();
                    sb.AppendLine("#environment server settings");
                    sb.AppendLine("env_enabled = 1");
                    sb.AppendLine("env_ip = 127.0.0.1");
                    sb.AppendLine("env_port_tcp = 9997");
                    sb.AppendLine("env_port_udp_tx = 9998");
                    sb.AppendLine("env_port_udp_rx = 9999");
                    File.WriteAllText(path + "config.txt", sb.ToString());
                    string[] keys = NetHelper.MakeSigningKeys();
                    sb = new StringBuilder();
                    sb.AppendLine("public=" + keys[0]);
                    sb.AppendLine("private=" + keys[1]);
                    File.WriteAllText(path + "server.keys", sb.ToString());
                    sb = new StringBuilder();
                    sb.AppendLine("pubKey=" + keys[0]);
                    sb.AppendLine("name=" + d2.textBox1.Text);
                    sb.AppendLine("ip=" + d2.textBox4.Text);
                    sb.AppendLine("port_udp=0");
                    sb.AppendLine("port_tcp=0");
                    File.WriteAllText(path + "server.info", sb.ToString());
                    AddServer(path + "server.info");
                    MessageBox.Show("Done.");
                }
            }
        }
    }
}