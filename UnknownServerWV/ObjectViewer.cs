using System;
using System.Text;
using System.Windows.Forms;
using NetDefines;
using NetDefines.StateDefines;
using Server;

namespace UnknownServerWV
{
    public partial class ObjectViewer : Form
    {
        public ObjectViewer()
        {
            InitializeComponent();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            TreeNode sel = tv1.SelectedNode;
            uint selId = 0;
            if (sel != null && sel.Name != "")
                selId = uint.Parse(sel.Name);
            tv1.Nodes.Clear();
            TreeNode vehicles = new TreeNode("Vehicles");
            tv1.Nodes.Add(vehicles);
            foreach (NetObject no in ObjectManager.GetCopy())
                if (no is NetObjVehicleState veh)
                {
                    TreeNode t = new TreeNode("Vehicle ID=" + veh.ID.ToString("X8") + " AK=" + veh.accessKey.ToString("X8"));
                    t.Name = veh.ID.ToString();
                    vehicles.Nodes.Add(t);
                    if (no.ID == selId)
                        tv1.SelectedNode = t;
                }
            TreeNode players = new TreeNode("Players");
            tv1.Nodes.Add(players);
            foreach (NetObject no in ObjectManager.GetCopy())
                if (no is NetObjPlayerState player)
                {
                    TreeNode t = new TreeNode("Player ID=" + player.ID.ToString("X8") + " AK=" + player.accessKey.ToString("X8"));
                    t.Name = player.ID.ToString();
                    players.Nodes.Add(t);
                    if (no.ID == selId)
                        tv1.SelectedNode = t;
                }
            if (selId == 0)
                tv1.ExpandAll();
        }

        private void tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode sel = tv1.SelectedNode;
            if (sel == null || sel.Name == "")
                return;
            uint id = uint.Parse(sel.Name);
            foreach (NetObject no in ObjectManager.GetCopy())
                if (no.ID == id)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("ID=" + no.ID.ToString("X8"));
                    sb.Append(" AK=" + no.accessKey.ToString("X8") + " Type=" + no.type);
                    sb.Append(no.GetDetails());
                    if (no is NetObjPlayerState player)
                    {
                        sb.AppendLine("Inventory:");
                        sb.Append(player.GetStateInventory().ToDetails());
                    }
                    rtb1.Text = sb.ToString();
                    break;
                }
        }
    }
}
