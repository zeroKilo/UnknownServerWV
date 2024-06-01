
namespace UnknownServerWV
{
    partial class PlaylistEntryEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxKills = new System.Windows.Forms.TextBox();
            this.labelKills = new System.Windows.Forms.Label();
            this.textBoxRoundTime = new System.Windows.Forms.TextBox();
            this.labelRoundTime = new System.Windows.Forms.Label();
            this.textBoxCountDown = new System.Windows.Forms.TextBox();
            this.labelCountDown = new System.Windows.Forms.Label();
            this.textBoxMinPlayer = new System.Windows.Forms.TextBox();
            this.labelMinPlayer = new System.Windows.Forms.Label();
            this.comboBoxLocation = new System.Windows.Forms.ComboBox();
            this.labelLocation = new System.Windows.Forms.Label();
            this.comboBoxMap = new System.Windows.Forms.ComboBox();
            this.labelMap = new System.Windows.Forms.Label();
            this.comboBoxMode = new System.Windows.Forms.ComboBox();
            this.labelMode = new System.Windows.Forms.Label();
            this.textBoxBots = new System.Windows.Forms.TextBox();
            this.labelBots = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonOK.Location = new System.Drawing.Point(326, 341);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 30);
            this.buttonOK.TabIndex = 28;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonCancel.Location = new System.Drawing.Point(0, 341);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 30);
            this.buttonCancel.TabIndex = 29;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.textBoxBots);
            this.panel1.Controls.Add(this.labelBots);
            this.panel1.Controls.Add(this.textBoxKills);
            this.panel1.Controls.Add(this.labelKills);
            this.panel1.Controls.Add(this.textBoxRoundTime);
            this.panel1.Controls.Add(this.labelRoundTime);
            this.panel1.Controls.Add(this.textBoxCountDown);
            this.panel1.Controls.Add(this.labelCountDown);
            this.panel1.Controls.Add(this.textBoxMinPlayer);
            this.panel1.Controls.Add(this.labelMinPlayer);
            this.panel1.Controls.Add(this.comboBoxLocation);
            this.panel1.Controls.Add(this.labelLocation);
            this.panel1.Controls.Add(this.comboBoxMap);
            this.panel1.Controls.Add(this.labelMap);
            this.panel1.Controls.Add(this.comboBoxMode);
            this.panel1.Controls.Add(this.labelMode);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(401, 341);
            this.panel1.TabIndex = 30;
            // 
            // textBoxKills
            // 
            this.textBoxKills.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxKills.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.textBoxKills.Location = new System.Drawing.Point(0, 280);
            this.textBoxKills.Name = "textBoxKills";
            this.textBoxKills.Size = new System.Drawing.Size(401, 20);
            this.textBoxKills.TabIndex = 42;
            this.textBoxKills.Text = "40";
            this.textBoxKills.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelKills
            // 
            this.labelKills.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelKills.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelKills.Location = new System.Drawing.Point(0, 258);
            this.labelKills.Name = "labelKills";
            this.labelKills.Size = new System.Drawing.Size(401, 22);
            this.labelKills.TabIndex = 41;
            this.labelKills.Text = "Kills to Win";
            this.labelKills.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxRoundTime
            // 
            this.textBoxRoundTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxRoundTime.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.textBoxRoundTime.Location = new System.Drawing.Point(0, 238);
            this.textBoxRoundTime.Name = "textBoxRoundTime";
            this.textBoxRoundTime.Size = new System.Drawing.Size(401, 20);
            this.textBoxRoundTime.TabIndex = 40;
            this.textBoxRoundTime.Text = "20";
            this.textBoxRoundTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelRoundTime
            // 
            this.labelRoundTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelRoundTime.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelRoundTime.Location = new System.Drawing.Point(0, 216);
            this.labelRoundTime.Name = "labelRoundTime";
            this.labelRoundTime.Size = new System.Drawing.Size(401, 22);
            this.labelRoundTime.TabIndex = 39;
            this.labelRoundTime.Text = "Round Time in Minutes";
            this.labelRoundTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxCountDown
            // 
            this.textBoxCountDown.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxCountDown.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.textBoxCountDown.Location = new System.Drawing.Point(0, 196);
            this.textBoxCountDown.Name = "textBoxCountDown";
            this.textBoxCountDown.Size = new System.Drawing.Size(401, 20);
            this.textBoxCountDown.TabIndex = 38;
            this.textBoxCountDown.Text = "3";
            this.textBoxCountDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelCountDown
            // 
            this.labelCountDown.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelCountDown.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelCountDown.Location = new System.Drawing.Point(0, 174);
            this.labelCountDown.Name = "labelCountDown";
            this.labelCountDown.Size = new System.Drawing.Size(401, 22);
            this.labelCountDown.TabIndex = 37;
            this.labelCountDown.Text = "Count Down Time in Seconds";
            this.labelCountDown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxMinPlayer
            // 
            this.textBoxMinPlayer.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxMinPlayer.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.textBoxMinPlayer.Location = new System.Drawing.Point(0, 154);
            this.textBoxMinPlayer.Name = "textBoxMinPlayer";
            this.textBoxMinPlayer.Size = new System.Drawing.Size(401, 20);
            this.textBoxMinPlayer.TabIndex = 36;
            this.textBoxMinPlayer.Text = "1";
            this.textBoxMinPlayer.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelMinPlayer
            // 
            this.labelMinPlayer.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelMinPlayer.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelMinPlayer.Location = new System.Drawing.Point(0, 132);
            this.labelMinPlayer.Name = "labelMinPlayer";
            this.labelMinPlayer.Size = new System.Drawing.Size(401, 22);
            this.labelMinPlayer.TabIndex = 35;
            this.labelMinPlayer.Text = "Minimal Player Count";
            this.labelMinPlayer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxLocation
            // 
            this.comboBoxLocation.Dock = System.Windows.Forms.DockStyle.Top;
            this.comboBoxLocation.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.comboBoxLocation.FormattingEnabled = true;
            this.comboBoxLocation.Location = new System.Drawing.Point(0, 110);
            this.comboBoxLocation.Name = "comboBoxLocation";
            this.comboBoxLocation.Size = new System.Drawing.Size(401, 22);
            this.comboBoxLocation.TabIndex = 46;
            this.comboBoxLocation.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLocation_SelectedIndexChanged);
            // 
            // labelLocation
            // 
            this.labelLocation.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelLocation.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelLocation.Location = new System.Drawing.Point(0, 88);
            this.labelLocation.Name = "labelLocation";
            this.labelLocation.Size = new System.Drawing.Size(401, 22);
            this.labelLocation.TabIndex = 45;
            this.labelLocation.Text = "Spawn Location";
            this.labelLocation.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxMap
            // 
            this.comboBoxMap.Dock = System.Windows.Forms.DockStyle.Top;
            this.comboBoxMap.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.comboBoxMap.FormattingEnabled = true;
            this.comboBoxMap.Location = new System.Drawing.Point(0, 66);
            this.comboBoxMap.Name = "comboBoxMap";
            this.comboBoxMap.Size = new System.Drawing.Size(401, 22);
            this.comboBoxMap.TabIndex = 34;
            this.comboBoxMap.SelectedIndexChanged += new System.EventHandler(this.ComboBoxMap_SelectedIndexChanged);
            // 
            // labelMap
            // 
            this.labelMap.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelMap.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelMap.Location = new System.Drawing.Point(0, 44);
            this.labelMap.Name = "labelMap";
            this.labelMap.Size = new System.Drawing.Size(401, 22);
            this.labelMap.TabIndex = 33;
            this.labelMap.Text = "Map";
            this.labelMap.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxMode
            // 
            this.comboBoxMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.comboBoxMode.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.comboBoxMode.FormattingEnabled = true;
            this.comboBoxMode.Location = new System.Drawing.Point(0, 22);
            this.comboBoxMode.Name = "comboBoxMode";
            this.comboBoxMode.Size = new System.Drawing.Size(401, 22);
            this.comboBoxMode.TabIndex = 44;
            this.comboBoxMode.SelectedIndexChanged += new System.EventHandler(this.ComboBoxMode_SelectedIndexChanged);
            // 
            // labelMode
            // 
            this.labelMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelMode.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelMode.Location = new System.Drawing.Point(0, 0);
            this.labelMode.Name = "labelMode";
            this.labelMode.Size = new System.Drawing.Size(401, 22);
            this.labelMode.TabIndex = 43;
            this.labelMode.Text = "Mode";
            this.labelMode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxBots
            // 
            this.textBoxBots.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxBots.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.textBoxBots.Location = new System.Drawing.Point(0, 322);
            this.textBoxBots.Name = "textBoxBots";
            this.textBoxBots.Size = new System.Drawing.Size(401, 20);
            this.textBoxBots.TabIndex = 48;
            this.textBoxBots.Text = "0";
            this.textBoxBots.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelBots
            // 
            this.labelBots.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelBots.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.labelBots.Location = new System.Drawing.Point(0, 300);
            this.labelBots.Name = "labelBots";
            this.labelBots.Size = new System.Drawing.Size(401, 22);
            this.labelBots.TabIndex = 47;
            this.labelBots.Text = "Bot Count";
            this.labelBots.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PlaylistEntryEditor
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(401, 371);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlaylistEntryEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Playlist Entry";
            this.Load += new System.EventHandler(this.PlaylistEntryEditor_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox textBoxKills;
        private System.Windows.Forms.Label labelKills;
        private System.Windows.Forms.TextBox textBoxRoundTime;
        private System.Windows.Forms.Label labelRoundTime;
        private System.Windows.Forms.TextBox textBoxCountDown;
        private System.Windows.Forms.Label labelCountDown;
        private System.Windows.Forms.TextBox textBoxMinPlayer;
        private System.Windows.Forms.Label labelMinPlayer;
        private System.Windows.Forms.ComboBox comboBoxLocation;
        private System.Windows.Forms.Label labelLocation;
        private System.Windows.Forms.ComboBox comboBoxMap;
        private System.Windows.Forms.Label labelMap;
        private System.Windows.Forms.ComboBox comboBoxMode;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.TextBox textBoxBots;
        private System.Windows.Forms.Label labelBots;
    }
}