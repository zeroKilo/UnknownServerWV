using NetDefines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ReplayExplorer
{
    public partial class Form1 : Form
    {
        public List<PacketHelper.Packet> packets = new List<PacketHelper.Packet>();
        public List<int> selIndicies = new List<int>();
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.replay|*.replay";
            if(d.ShowDialog() == DialogResult.OK)
            {
                packets.Clear();
                FileStream fs  = new FileStream(d.FileName, FileMode.Open, FileAccess.Read);
                fs.Seek(0, SeekOrigin.End);
                long size = fs.Position;
                fs.Seek(0, SeekOrigin.Begin);
                int index = 0;
                while (fs.Position < size)
                    packets.Add(new PacketHelper.Packet(fs, index++));
                fs.Close();
                statusLabel.Text = "Loaded packets: " + packets.Count;
                vScrollBar1.Value = 0;
                vScrollBar1.Maximum = packets.Count - 1;
                RefreshList();
            }
        }

        private void listBox1_Resize(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            bool showUdp = toolStripButton1.Checked;
            bool showTcpPlayer = toolStripButton2.Checked;
            bool showTcpEnv = toolStripButton3.Checked;
            listBox1.Items.Clear();
            selIndicies.Clear();
            int start = vScrollBar1.Value;
            int count = listBox1.Height / listBox1.ItemHeight;
            for (int i = 0; listBox1.Items.Count < count && start + i < packets.Count; i++)
            {
                int n = start + i;
                PacketHelper.Packet p = packets[n];
                bool addIndex = true;
                if (showUdp && p.type == ReplayPacketTypes.UDP)
                    listBox1.Items.Add(p);
                else if (showTcpPlayer && p.type == ReplayPacketTypes.TCP_Player)
                    listBox1.Items.Add(p);
                else if (showTcpEnv && p.type == ReplayPacketTypes.TCP_Env)
                    listBox1.Items.Add(p);
                else
                    addIndex = false;
                if (addIndex)
                    selIndicies.Add(n);
            }
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            RefreshList();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1 || n >= selIndicies.Count)
                return;
            int index = selIndicies[n];
            PacketHelper.Packet p = packets[index];
            rtb1.Text = PacketHelper.HexDump(p.data);
        }
    }
}
