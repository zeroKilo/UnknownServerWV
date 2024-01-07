using System;
using System.Text;
using System.Windows.Forms;
using Server;

namespace UnknownServerWV
{
    public partial class ObjectViewer : Form
    {
        public ObjectViewer()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (NetDefines.NetObject no in ObjectManager.objects)
            {
                sb.Append((count++).ToString("D4") + " ID=" + no.ID.ToString("X8"));
                sb.Append(" AK=" + no.accessKey.ToString("X8") + " Type=" + no.type);
                sb.Append(no.GetDetails());
            }
            rtb1.Text = sb.ToString();
        }
    }
}
