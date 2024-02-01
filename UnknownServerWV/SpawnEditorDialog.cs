using Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetDefines;

namespace UnknownServerWV
{
    public partial class SpawnEditorDialog : Form
    {
        public SpawnManager.SpawnRange range;
        public SpawnEditorDialog()
        {
            InitializeComponent();
        }

        private void SpawnEditorDialog_Load(object sender, EventArgs e)
        {
            toolStripComboBox1.Items.Clear();
            foreach (Item item in Enum.GetValues(typeof(Item)))
                if (item != Item.UNDEFINED)
                    toolStripComboBox1.Items.Add(item);
            toolStripComboBox1.SelectedIndex = 0;
            textBox1.Text = (range.end - range.start + 1).ToString();
            RefreshList();
        }

        private void RefreshList()
        {
            listBox1.Items.Clear();
            foreach (Item item in range.items)
                listBox1.Items.Add(item);
        }

        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            range.items.Add((Item)toolStripComboBox1.SelectedIndex);
            RefreshList();
        }

        private void ToolStripButton2_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            range.items.RemoveAt(n);
            RefreshList();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            range.start = 0;
            range.end = int.Parse(textBox1.Text) - 1;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
