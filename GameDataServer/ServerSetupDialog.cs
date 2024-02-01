using System;
using System.Windows.Forms;

namespace GameDataServer
{
    public partial class ServerSetupDialog : Form
    {
        public ServerSetupDialog()
        {
            InitializeComponent();
            DialogResult = DialogResult.Abort;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
