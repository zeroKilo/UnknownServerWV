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
            listBox1.SelectedIndex = 1;
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
                case ServerMode.ArcadeMode:
                    comboBox3.Items.Clear();
                    foreach (NetMapInfo map in ArcadeMode.mapInfos)
                        comboBox3.Items.Add(map.name);
                    comboBox3.SelectedIndex = 0;
                    panel_arcade_mode.BringToFront();
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
            ServerMode mode = availableModes[listBox1.SelectedIndex];
            switch (mode)
            {
                case ServerMode.ArcadeMode:
                    ArcadeMode.mapName = comboBox3.SelectedItem.ToString();
                    ArcadeMode.Start();
                    break;
                case ServerMode.BattleRoyaleMode:
                    BattleRoyaleMode.mapName = comboBox1.SelectedItem.ToString();
                    BattleRoyaleMode.spawnLocIdx = comboBox2.SelectedIndex;
                    BattleRoyaleMode.spawnLocNames = BattleRoyaleMode.mapInfos[comboBox1.SelectedIndex].spawnLocations.ToArray();
                    BattleRoyaleMode.Start();
                    BattleRoyaleServerLogic.neededPlayers = Convert.ToInt32(textBox1.Text);
                    BattleRoyaleServerLogic.countDownTime = Convert.ToInt32(textBox2.Text) * 1000;
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
                case ServerMode.ArcadeMode:
                    ArcadeMode.Stop();
                    break;
                case ServerMode.BattleRoyaleMode:
                    BattleRoyaleMode.Stop();
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
    }
}
