using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Server;
using System.Net.NetworkInformation;
using NetDefines;

namespace UnknownServerWV
{
    public partial class UserManager : Form
    {
        public UserManager()
        {
            InitializeComponent();
        }

        private void UserManager_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            foreach (PlayerProfile p in Config.profiles)
                listBox1.Items.Add(p.name);
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "Add":
                    OnAdd();
                    break;
                case "Delete":
                    OnDelete();
                    break;
                case "Export Login File":
                    OnExportLogin();
                    break;
            }
        }

        private void OnAdd()
        {
            string input = Interaction.InputBox("Please enter new name", "Add Player");
            if(input.Trim() != "")
            {
                string name = input.Trim();
                bool found = false;
                foreach(PlayerProfile p in Config.profiles)
                    if(p.name == name)
                    {
                        found = true;
                        break;
                    }
                if (found)
                    MessageBox.Show("Player already exists!");
                else
                {
                    PlayerProfile p = new PlayerProfile();
                    p.name = name;
                    p.key = NetHelper.CreateMD5(name + Server.Backend.SERVER_SALT);
                    for (int i = 1000; i < 100000; i++)
                    {
                        if (File.Exists("profiles\\" + i + ".txt"))
                            continue;
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("name=" + p.name);
                        sb.AppendLine("key=" + p.key);
                        File.WriteAllText("profiles\\" + i + ".txt", sb.ToString());
                        break;
                    }
                    Config.profiles.Add(p);
                    listBox1.Items.Clear();
                    foreach (PlayerProfile pp in Config.profiles)
                        listBox1.Items.Add(pp.name);
                }
            }
        }

        private void OnDelete()
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            PlayerProfile p = Config.profiles[n];
            string[] files = Directory.GetFiles("profiles");
            foreach(string file in files)
            {
                string content = File.ReadAllText(file);
                if (content.Contains("key=" + p.key))
                {
                    File.Delete(file);
                    break;
                }
            }
            Config.profiles.RemoveAt(n);
            listBox1.Items.Clear();
            foreach (PlayerProfile pp in Config.profiles)
                listBox1.Items.Add(pp.name);
        }

        private void OnExportLogin()
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            PlayerProfile p = Config.profiles[n];
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("key=" + p.key);
            List<string> ips = new List<string>();
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                    ips.Add(addr.Address.ToString());
            }
            sb.Append("ip_list=");
            if (ips.Count > 0)
                sb.Append(ips[0]);
            for (int i = 1; i < ips.Count; i++)
                sb.Append(";" + ips[i]);
            sb.AppendLine();
            sb.AppendLine("port_tcp=" + Config.settings["port_tcp"].Trim());
            sb.AppendLine("port_udp=" + Config.settings["port_udp"].Trim());
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "login.info|login.info";
            d.FileName = "login.info";
            if (d.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(d.FileName, sb.ToString());
                MessageBox.Show("Done.");
            }
        }
    }
}
