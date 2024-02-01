using NetDefines;
using Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnknownServerWV
{
    public partial class SpawnEditor : Form
    {
        public static Dictionary<string, List<SpawnManager.SpawnRange>> singleItemSpawnRanges;
        public static Dictionary<string, List<SpawnManager.SpawnRange>> multiItemSpawnRanges;
        public static Dictionary<string, int> maxSingleSpawnRange;
        public static Dictionary<string, int> maxMultiSpawnRange;
        public Graphics g;
        public SpawnEditor()
        {
            InitializeComponent();
            g = pic1.CreateGraphics();
        }

        private void SpawnEditor_Load(object sender, EventArgs e)
        {
            SpawnManager.Reset();
            singleItemSpawnRanges = new Dictionary<string, List<SpawnManager.SpawnRange>>();
            multiItemSpawnRanges = new Dictionary<string, List<SpawnManager.SpawnRange>>();
            maxSingleSpawnRange = new Dictionary<string, int>();
            maxMultiSpawnRange = new Dictionary<string, int>();
            foreach (string mapName in SpawnManager.singleItemSpawnRanges.Keys)
            {
                List<SpawnManager.SpawnRange> list = new List<SpawnManager.SpawnRange>();
                foreach (SpawnManager.SpawnRange range in SpawnManager.singleItemSpawnRanges[mapName])
                    list.Add(range.Copy());
                singleItemSpawnRanges.Add(mapName, list);
            }
            foreach (string mapName in SpawnManager.multiItemSpawnRanges.Keys)
            {
                List<SpawnManager.SpawnRange> list = new List<SpawnManager.SpawnRange>();
                foreach (SpawnManager.SpawnRange range in SpawnManager.multiItemSpawnRanges[mapName])
                    list.Add(range.Copy());
                multiItemSpawnRanges.Add(mapName, list);
            }
            RecalcRanges();
            RefreshTree();
        }

        private void RefreshTree()
        {
            g.Clear(Color.White);
            tv1.Nodes.Clear();
            tv1.Nodes.Add(MakeCategory("Single Spawns", singleItemSpawnRanges));
            tv1.Nodes.Add(MakeCategory("Grouped Spawns", multiItemSpawnRanges));
        }

        private TreeNode MakeCategory(string name, Dictionary<string, List<SpawnManager.SpawnRange>> ranges)
        {
            TreeNode result = new TreeNode(name);
            foreach(string mapName in ranges.Keys)
                result.Nodes.Add(MakeMapEntry(mapName, ranges[mapName]));
            result.Expand();
            return result;
        }

        private TreeNode MakeMapEntry(string name, List<SpawnManager.SpawnRange> entries)
        {
            TreeNode result = new TreeNode(name);
            foreach (SpawnManager.SpawnRange range in entries)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Weight:" + (range.end - range.start + 1).ToString().PadRight(5) + "Items: ");
                bool isFirst = true;
                foreach (Item item in range.items)
                    if (isFirst)
                    {
                        sb.Append(item);
                        isFirst = false;
                    }
                    else
                        sb.Append("," + item);
                result.Nodes.Add(sb.ToString());
            }
            return result;
        }

        private void Tv1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode sel = e.Node;
            if (sel.Parent == null)
                return;
            TreeNode parent = sel.Parent;
            if (parent.Parent == null)
                return;
            TreeNode category = parent.Parent;
            if(category.Index == 0)
            {
                List<SpawnManager.SpawnRange> ranges = singleItemSpawnRanges[parent.Text];
                int max = maxSingleSpawnRange[parent.Text];
                DrawRanges(ranges, max, sel.Index);
            }
            else if (category.Index == 1)
            {
                List<SpawnManager.SpawnRange> ranges = multiItemSpawnRanges[parent.Text];
                int max = maxMultiSpawnRange[parent.Text];
                DrawRanges(ranges, max, sel.Index);
            }
        }

        private void DrawRanges(List<SpawnManager.SpawnRange> ranges, int max, int sel)
        {
            g.Clear(Color.White);            
            for (int i = 0; i < ranges.Count; i++)
            {
                float left = ranges[i].start / (float)max;
                float right = (ranges[i].end + 1) / (float)max;
                left *= pic1.Width;
                right *= pic1.Width;
                Brush b = i % 2 == 0 ? Brushes.Black : Brushes.White;
                if (i == sel)
                    b = Brushes.Orange;
                g.FillRectangle(b, left, 0, right - left, pic1.Height);
            }
        }

        private void RecalcRanges()
        {
            maxSingleSpawnRange = new Dictionary<string, int>();
            maxMultiSpawnRange = new Dictionary<string, int>();
            string[] mapNames = singleItemSpawnRanges.Keys.ToArray();
            foreach(string mapName in mapNames)
            {
                int pos = 0;
                List<SpawnManager.SpawnRange> list = new List<SpawnManager.SpawnRange>();
                foreach(SpawnManager.SpawnRange range in singleItemSpawnRanges[mapName])
                {
                    int size = range.end - range.start + 1;
                    list.Add(new SpawnManager.SpawnRange(range.items, pos, pos + size - 1));
                    pos += size;
                }
                singleItemSpawnRanges[mapName] = list;
                maxSingleSpawnRange.Add(mapName, pos);
            }
            mapNames = multiItemSpawnRanges.Keys.ToArray();
            foreach (string mapName in mapNames)
            {
                int pos = 0;
                List<SpawnManager.SpawnRange> list = new List<SpawnManager.SpawnRange>();
                foreach (SpawnManager.SpawnRange range in multiItemSpawnRanges[mapName])
                {
                    int size = range.end - range.start + 1;
                    list.Add(new SpawnManager.SpawnRange(range.items, pos, pos + size - 1));
                    pos += size;
                }
                multiItemSpawnRanges[mapName] = list;
                maxMultiSpawnRange.Add(mapName, pos);
            }
        }

        private void ContextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            TreeNode sel = tv1.SelectedNode;
            if (sel == null)
                return;
            bool showAddEntry = false;
            bool showEditEntry = false;
            bool showRemoveEntry = false;
            switch (sel.Level)
            {
                case 1:
                    showAddEntry = true;                    
                    break;
                case 2:
                    showEditEntry = showRemoveEntry = true;
                    break;
            }
            addEntryToolStripMenuItem.Visible = showAddEntry;
            editEntryToolStripMenuItem.Visible = showEditEntry;
            removeEntryToolStripMenuItem.Visible = showRemoveEntry;
            if (showAddEntry == false &&
                showEditEntry == false &&
                showRemoveEntry == false)
                e.Cancel = true;
        }

        private void RemoveEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode sel = tv1.SelectedNode;
            if (sel == null || sel.Level != 2)
                return;
            TreeNode mapNode = sel.Parent;
            TreeNode category = mapNode.Parent;
            if (category.Index == 0)
            {
                List<SpawnManager.SpawnRange> list = singleItemSpawnRanges[mapNode.Text];
                list.RemoveAt(sel.Index);
            }
            else
            {
                List<SpawnManager.SpawnRange> list = multiItemSpawnRanges[mapNode.Text];
                list.RemoveAt(sel.Index);
            }
            RecalcRanges();
            RefreshTree();
            category = tv1.Nodes[category.Index];
            mapNode = category.Nodes[mapNode.Index];
            tv1.SelectedNode = mapNode;
            mapNode.Expand();
        }

        private void AddEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode sel = tv1.SelectedNode;
            if (sel == null || sel.Level != 1)
                return;
            TreeNode category = sel.Parent;
            SpawnEditorDialog d = new SpawnEditorDialog
            {
                range = new SpawnManager.SpawnRange(new List<Item>(), 0, 0)
            };
            if (d.ShowDialog() == DialogResult.OK)
            {
                if (category.Index == 0)
                {
                    if (d.range.items.Count == 0)
                        return;
                    if (d.range.items.Count > 1)
                        d.range.items = new List<Item>() { d.range.items[0] };
                    singleItemSpawnRanges[sel.Text].Add(d.range);
                }
                else
                    multiItemSpawnRanges[sel.Text].Add(d.range);
                RecalcRanges();
                RefreshTree();
                category = tv1.Nodes[category.Index];
                sel = category.Nodes[sel.Index];
                tv1.SelectedNode = sel;
                sel.Expand();
            }
        }

        private void EditEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditEntry();
        }

        private void EditEntry()
        {
            TreeNode sel = tv1.SelectedNode;
            if (sel == null || sel.Level != 2)
                return;
            TreeNode mapNode = sel.Parent;
            TreeNode category = mapNode.Parent;
            SpawnEditorDialog d = new SpawnEditorDialog();
            if (category.Index == 0)
                d.range = singleItemSpawnRanges[mapNode.Text][sel.Index];
            else
                d.range = multiItemSpawnRanges[mapNode.Text][sel.Index];
            if (d.ShowDialog() == DialogResult.OK)
            {
                if (category.Index == 0)
                {
                    if (d.range.items.Count == 0)
                        return;
                    if (d.range.items.Count > 1)
                        d.range.items = new List<Item>() { d.range.items[0] };
                    singleItemSpawnRanges[mapNode.Text][sel.Index] = d.range;
                }
                else
                    multiItemSpawnRanges[mapNode.Text][sel.Index] = d.range;
                RecalcRanges();
                RefreshTree();
                category = tv1.Nodes[category.Index];
                mapNode = category.Nodes[mapNode.Index];
                sel = mapNode.Nodes[sel.Index];
                tv1.SelectedNode = sel;
                sel.Expand();
            }
        }

        private void Tv1_DoubleClick(object sender, EventArgs e)
        {
            EditEntry();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"singleSpawns\":{");
            foreach(string mapName in singleItemSpawnRanges.Keys)
            {
                sb.Append("\"" + mapName + "\":[");
                foreach(SpawnManager.SpawnRange range in singleItemSpawnRanges[mapName])
                {
                    int weight = range.end - range.start + 1;
                    int item = (int)range.items[0];
                    sb.Append("[" + weight + "," + item + "],");
                }
                sb.Length--;
                sb.Append("],");
            }
            sb.Length--;
            sb.Append("},\"multiSpawns\":{");
            foreach (string mapName in multiItemSpawnRanges.Keys)
            {
                sb.Append("\"" + mapName + "\":[");
                foreach (SpawnManager.SpawnRange range in multiItemSpawnRanges[mapName])
                {
                    int weight = range.end - range.start + 1;
                    sb.Append("[" + weight + ",[");
                    foreach(Item item in range.items)
                    {
                        int i = (int)item;
                        sb.Append(i + ",");
                    }
                    sb.Length--;
                    sb.Append("]],");
                }
                sb.Length--;
                sb.Append("],");
            }
            sb.Length--;
            sb.Append("}}");
            File.WriteAllText("spawn_table.json", sb.ToString());
            SpawnManager.Reset();
            Close();
        }
    }
}
