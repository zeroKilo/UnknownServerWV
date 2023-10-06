using NetDefines;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

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
                    startToolStripMenuItem_Click(null, null);
            }
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameDataServer.Start();
            if (GameDataServer.isRunning())
            {
                startToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameDataServer.Stop();
            if (!GameDataServer.isRunning())
            {
                startToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            foreach (GameServer gs in servers)
                if (gs.NeedsUpdate)
                    DBManager.UpdateGameServer(gs);
            foreach (PlayerProfile p in profiles)
                if (p.NeedsUpdate)
                    DBManager.UpdatePlayerProfile(p);
            bool needsUpdate = DBManager.NeedsUpdate();
            if(needsUpdate)
            {
                RefreshAll();
                Log.Print("Saved changes to db");
            }
        }

        private void addServer_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "server.info|server.info";
            if (d.ShowDialog() == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(d.FileName);
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
                        case "public":
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
        }

        private void editServer_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
        }

        private void removeServer_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            DBManager.RemoveGameServer(servers[n]);
            RefreshAll();
        }

        private void addProfile_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "player.info|player.info";
            if (d.ShowDialog() == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(d.FileName);
                PlayerProfile p = new PlayerProfile(-1, lines[1].Split('=')[1].Trim(), lines[0].Split('=')[1].Trim());
                DBManager.AddPlayerProfile(p);
                RefreshAll();
            }
        }

        private void editProfile_Click(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
        }

        private void removeProfile_Click(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            DBManager.RemovePlayerProfile(profiles[n]);
            RefreshAll();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
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

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox2.SelectedIndex;
            if (n == -1)
                return;
            rtb3.Text = "Public Key: " + profiles[n].PublicKey;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            GameDataServer.Stop();
        }
    }
}
