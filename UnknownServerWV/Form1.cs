using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Server;
using NetDefines;

namespace UnknownServerWV
{
    public partial class Form1 : Form
    {        
        private List<ServerMode> availableModes = new List<ServerMode>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            foreach (ServerMode mode in Enum.GetValues(typeof(ServerMode)))
                if (mode != ServerMode.Offline)
                {
                    listBox1.Items.Add(NetConstants.ServerModeNames[(int)mode]);
                    availableModes.Add(mode);
                }
            listBox1.SelectedIndex = 0;
            Log.Init(rtb1);
            Config.Init();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(availableModes[listBox1.SelectedIndex])
            {
                case ServerMode.BattleRoyaleMode:
                    comboBox1.Items.Clear();
                    foreach (NetMapInfo map in BattleRoyaleMode.mapInfos)
                        comboBox1.Items.Add(map.name);
                    comboBox1.SelectedIndex = 0;
                    panel_br_mode.BringToFront();
                    break;
                case ServerMode.DeathMatchMode:
                    comboBox4.Items.Clear();
                    foreach (NetMapInfo map in DeathMatchMode.mapInfos)
                        comboBox4.Items.Add(map.name);
                    comboBox4.SelectedIndex = 0;
                    panel_dm_mode.BringToFront();
                    break;
                case ServerMode.TeamDeathMatchMode:
                    comboBox3.Items.Clear();
                    foreach (NetMapInfo map in TeamDeathMatchMode.mapInfos)
                        comboBox3.Items.Add(map.name);
                    comboBox3.SelectedIndex = 0;
                    panel_tdm_mode.BringToFront();
                    break;
                case ServerMode.FreeExploreMode:
                    comboBox5.Items.Clear();
                    foreach (NetMapInfo map in FreeExploreMode.mapInfos)
                        comboBox5.Items.Add(map.name);
                    comboBox5.SelectedIndex = 0;
                    panel_fem_mode.BringToFront();
                    break;
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch(e.ClickedItem.Text)
            {
                case "Start":
                    OnStart();
                    break;
                case "Stop":
                    OnStop();
                    break;
                case "User Manager":
                    OnUserManager();
                    break;
                case "Object Viewer":
                    OnObjectViewer();
                    break;
            }
        }

        private void OnStart()
        {
            ObjectManager.Reset();
            DoorManager.Reset();
            SpawnManager.Reset();
            ServerMode mode = availableModes[listBox1.SelectedIndex];
            switch (mode)
            {
                case ServerMode.DeathMatchMode:
                    DeathMatchMode.mapName = comboBox4.SelectedItem.ToString();
                    DeathMatchMode.Start();
                    DeathMatchServerLogic.neededPlayers = Convert.ToInt32(textBox10.Text);
                    DeathMatchServerLogic.countDownTime = Convert.ToInt32(textBox9.Text) * 1000;
                    DeathMatchServerLogic.roundTime = Convert.ToInt32(textBox8.Text) * 60;
                    DeathMatchServerLogic.killsToWin = Convert.ToInt32(textBox7.Text);
                    break;
                case ServerMode.TeamDeathMatchMode:
                    TeamDeathMatchMode.mapName = comboBox3.SelectedItem.ToString();
                    TeamDeathMatchMode.Start();
                    TeamDeathMatchServerLogic.neededPlayers = Convert.ToInt32(textBox4.Text);
                    TeamDeathMatchServerLogic.countDownTime = Convert.ToInt32(textBox3.Text) * 1000;
                    TeamDeathMatchServerLogic.roundTime = Convert.ToInt32(textBox5.Text) * 60;
                    TeamDeathMatchServerLogic.killsToWin = Convert.ToInt32(textBox6.Text);
                    break;
                case ServerMode.BattleRoyaleMode:
                    BattleRoyaleMode.mapName = comboBox1.SelectedItem.ToString();
                    BattleRoyaleMode.spawnLocIdx = comboBox2.SelectedIndex;
                    BattleRoyaleMode.spawnLocNames = BattleRoyaleMode.mapInfos[comboBox1.SelectedIndex].spawnLocations.ToArray();
                    BattleRoyaleMode.Start();
                    BattleRoyaleServerLogic.neededPlayers = Convert.ToInt32(textBox1.Text);
                    BattleRoyaleServerLogic.countDownTime = Convert.ToInt32(textBox2.Text) * 1000;
                    break;
                case ServerMode.FreeExploreMode:
                    FreeExploreMode.mapName = comboBox5.SelectedItem.ToString();
                    FreeExploreMode.spawnLocIdx = comboBox6.SelectedIndex;
                    FreeExploreMode.spawnLocNames = FreeExploreMode.mapInfos[comboBox5.SelectedIndex].spawnLocations.ToArray();
                    FreeExploreMode.Start();
                    break;
                default:
                    return;
            }
        }

        private void OnStop()
        {
            ServerMode mode = availableModes[listBox1.SelectedIndex];
            switch (mode)
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
                default:
                    return;
            }
        }

        private void OnUserManager()
        {
            UserManager f = new UserManager();
            f.ShowDialog();
        }

        private void OnObjectViewer()
        {
            ObjectViewer ov = new ObjectViewer();
            ov.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool running = Backend.isRunning();
            splitContainer1.Panel1.Enabled = !running;
            toolStripMenuItem1.Enabled = !running;
            toolStripMenuItem2.Enabled = running;
            if(running)
            {
                status.Text = "Status : UDP Data=0x" + MainServer.getDataCount().ToString("X") 
                            + " UDP Errors=" + MainServer.getErrorCount()
                            + " ServerMode=" + Backend.mode
                            + " ServerState=" + Backend.modeState;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            OnStop();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = comboBox1.SelectedIndex;
            if (n == -1)
                return;
            string[] names = BattleRoyaleMode.mapInfos[n].spawnLocations.ToArray();
            comboBox2.Items.Clear();
            comboBox2.Items.Add("<Random>");
            foreach (string name in names)
                comboBox2.Items.Add(name);
            comboBox2.SelectedIndex = 0;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            rtb1.Text = "";
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = comboBox5.SelectedIndex;
            if (n == -1)
                return;
            string[] names = FreeExploreMode.mapInfos[n].spawnLocations.ToArray();
            comboBox6.Items.Clear();
            comboBox6.Items.Add("<Random>");
            foreach (string name in names)
                comboBox6.Items.Add(name);
            comboBox6.SelectedIndex = 0;
        }
    }
}
