using NetDefines;
using Server;
using System;
using System.Windows.Forms;

namespace UnknownServerWV
{
    public partial class PlaylistEntryEditor : Form
    {
        public PlaylistManager.PlaylistEntry entry;
        public PlaylistEntryEditor()
        {
            InitializeComponent();
        }

        private void PlaylistEntryEditor_Load(object sender, EventArgs e)
        {
            SetVisible(true, false, false, false, false, false);
            comboBoxMode.Items.Clear();
            foreach(ServerMode mode in Enum.GetValues(typeof(ServerMode)))
            {
                if (mode == ServerMode.Offline)
                    continue;
                comboBoxMode.Items.Add(mode.ToString());
            }
            comboBoxMode.SelectedIndex = (int)entry.mode - 1;
            textBoxCountDown.Text = entry.countDown.ToString();
            textBoxKills.Text = entry.killsToWin.ToString();
            textBoxMinPlayer.Text = entry.minPlayer.ToString();
            textBoxRoundTime.Text = entry.roundTime.ToString();
        }

        private void ComboBoxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            entry.mode = (ServerMode)(comboBoxMode.SelectedIndex + 1);
            comboBoxMap.Items.Clear();            
            switch (entry.mode)
            {
                case ServerMode.BattleRoyaleMode:
                    foreach (NetMapInfo map in BattleRoyaleMode.mapInfos)
                        comboBoxMap.Items.Add(map.name);
                    SetVisible(true, true, true, true, false, false);
                    break;
                case ServerMode.DeathMatchMode:
                    foreach (NetMapInfo map in DeathMatchMode.mapInfos)
                        comboBoxMap.Items.Add(map.name);
                    SetVisible(true, false, true, true, true, true);
                    break;
                case ServerMode.TeamDeathMatchMode:
                    foreach (NetMapInfo map in TeamDeathMatchMode.mapInfos)
                        comboBoxMap.Items.Add(map.name);
                    SetVisible(true, false, true, true, true, true);
                    break;
                case ServerMode.FreeExploreMode:
                    foreach (NetMapInfo map in FreeExploreMode.mapInfos)
                        comboBoxMap.Items.Add(map.name);
                    SetVisible(true, true, false, false, true, false);
                    break;
            }
            if (comboBoxMap.Items.Count > 0)
                comboBoxMap.SelectedIndex = 0;
        }

        private void ComboBoxMap_SelectedIndexChanged(object sender, EventArgs e)
        {
            entry.map = comboBoxMap.SelectedIndex;
            NetMapInfo map;
            comboBoxLocation.Items.Clear();
            switch (entry.mode)
            {
                case ServerMode.BattleRoyaleMode:
                    map = BattleRoyaleMode.mapInfos[comboBoxMap.SelectedIndex];
                    foreach(string loc in map.spawnLocations)
                        comboBoxLocation.Items.Add(loc);
                    break;
                case ServerMode.FreeExploreMode:
                    map = FreeExploreMode.mapInfos[comboBoxMap.SelectedIndex];
                    foreach (string loc in map.spawnLocations)
                        comboBoxLocation.Items.Add(loc);
                    break;
                case ServerMode.DeathMatchMode:
                case ServerMode.TeamDeathMatchMode:
                    entry.spawnLoc = -1;
                    break;
            }
            if (entry.spawnLoc != -1)
                comboBoxLocation.SelectedIndex = entry.spawnLoc;
        }

        private void ComboBoxLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            entry.spawnLoc = comboBoxLocation.SelectedIndex;
        }

        private void SetVisible(bool map, bool spawnLoc, bool minPlayer, bool countDown, bool roundTime, bool kills)
        {
            comboBoxMap.Visible =
            labelMap.Visible = map;
            comboBoxLocation.Visible =
            labelLocation.Visible = spawnLoc;
            textBoxMinPlayer.Visible =
            labelMinPlayer.Visible = minPlayer;
            textBoxCountDown.Visible =
            labelCountDown.Visible = countDown;
            textBoxRoundTime.Visible =
            labelRoundTime.Visible = roundTime;
            textBoxKills.Visible =
            labelKills.Visible = kills;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            entry.countDown = int.Parse(textBoxCountDown.Text);
            entry.killsToWin = int.Parse(textBoxKills.Text);
            entry.minPlayer = int.Parse(textBoxMinPlayer.Text);
            entry.roundTime = int.Parse(textBoxRoundTime.Text);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {

            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
