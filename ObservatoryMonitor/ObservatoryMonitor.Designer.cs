namespace ASCOM.Wise40.ObservatoryMonitor
{
    partial class ObsMainForm
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.operationModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lCOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wISEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aCPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.labelDate = new System.Windows.Forms.Label();
            this.buttonPark = new System.Windows.Forms.Button();
            this.timerDisplayRefresh = new System.Windows.Forms.Timer(this.components);
            this.buttonManualIntervention = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelConditions = new System.Windows.Forms.Label();
            this.labelActivity = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelNextCheck = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.labelOperatingMode = new System.Windows.Forms.Label();
            this.menuStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(567, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.operationModeToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // operationModeToolStripMenuItem
            // 
            this.operationModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aCPToolStripMenuItem,
            this.lCOToolStripMenuItem,
            this.wISEToolStripMenuItem});
            this.operationModeToolStripMenuItem.Name = "operationModeToolStripMenuItem";
            this.operationModeToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.operationModeToolStripMenuItem.Text = "Operation Mode";
            // 
            // lCOToolStripMenuItem
            // 
            this.lCOToolStripMenuItem.Name = "lCOToolStripMenuItem";
            this.lCOToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.lCOToolStripMenuItem.Text = "LCO";
            this.lCOToolStripMenuItem.Click += new System.EventHandler(this.SelectOpMode);
            // 
            // wISEToolStripMenuItem
            // 
            this.wISEToolStripMenuItem.Name = "wISEToolStripMenuItem";
            this.wISEToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.wISEToolStripMenuItem.Text = "WISE";
            this.wISEToolStripMenuItem.Click += new System.EventHandler(this.SelectOpMode);
            // 
            // aCPToolStripMenuItem
            // 
            this.aCPToolStripMenuItem.Name = "aCPToolStripMenuItem";
            this.aCPToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.aCPToolStripMenuItem.Text = "ACP";
            this.aCPToolStripMenuItem.Click += new System.EventHandler(this.SelectOpMode);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // listBoxLog
            // 
            this.listBoxLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.listBoxLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listBoxLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.HorizontalScrollbar = true;
            this.listBoxLog.ItemHeight = 15;
            this.listBoxLog.Location = new System.Drawing.Point(0, 272);
            this.listBoxLog.Margin = new System.Windows.Forms.Padding(8, 3, 3, 3);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.ScrollAlwaysVisible = true;
            this.listBoxLog.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listBoxLog.Size = new System.Drawing.Size(567, 454);
            this.listBoxLog.TabIndex = 40;
            // 
            // labelDate
            // 
            this.labelDate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDate.Location = new System.Drawing.Point(32, 40);
            this.labelDate.Name = "labelDate";
            this.labelDate.Size = new System.Drawing.Size(256, 48);
            this.labelDate.TabIndex = 53;
            this.labelDate.Text = "Jun 19, 2017\r\nhh:mm:ss";
            this.labelDate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonPark
            // 
            this.buttonPark.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPark.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonPark.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonPark.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonPark.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonPark.Location = new System.Drawing.Point(320, 184);
            this.buttonPark.Name = "buttonPark";
            this.buttonPark.Size = new System.Drawing.Size(192, 48);
            this.buttonPark.TabIndex = 54;
            this.buttonPark.Text = "Shutdown Now";
            this.toolTip.SetToolTip(this.buttonPark, "Stop activities\r\nPark the telescope\r\nPark the dome and close the shutter");
            this.buttonPark.UseVisualStyleBackColor = false;
            this.buttonPark.Click += new System.EventHandler(this.buttonPark_Click);
            // 
            // timerDisplayRefresh
            // 
            this.timerDisplayRefresh.Enabled = true;
            this.timerDisplayRefresh.Interval = 1000;
            this.timerDisplayRefresh.Tick += new System.EventHandler(this.timerDisplayRefresh_Tick);
            // 
            // buttonManualIntervention
            // 
            this.buttonManualIntervention.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonManualIntervention.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonManualIntervention.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonManualIntervention.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonManualIntervention.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonManualIntervention.Location = new System.Drawing.Point(320, 88);
            this.buttonManualIntervention.Name = "buttonManualIntervention";
            this.buttonManualIntervention.Size = new System.Drawing.Size(192, 72);
            this.buttonManualIntervention.TabIndex = 55;
            this.buttonManualIntervention.Text = "Remove Operator Intervention\r\n\r\nMake Observatory Safe to\r\nOperate";
            this.buttonManualIntervention.UseVisualStyleBackColor = false;
            this.buttonManualIntervention.Click += new System.EventHandler(this.buttonManualIntervention_Click);
            // 
            // labelConditions
            // 
            this.labelConditions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelConditions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConditions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelConditions.Location = new System.Drawing.Point(168, 24);
            this.labelConditions.Name = "labelConditions";
            this.labelConditions.Size = new System.Drawing.Size(80, 24);
            this.labelConditions.TabIndex = 57;
            this.labelConditions.Text = "Unknown";
            this.labelConditions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelConditions, "This information is from the SafeToOperate driver.\r\nIt combines environmental and" +
        " human\r\nintervention sensors");
            // 
            // labelActivity
            // 
            this.labelActivity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelActivity.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelActivity.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelActivity.Location = new System.Drawing.Point(168, 48);
            this.labelActivity.Name = "labelActivity";
            this.labelActivity.Size = new System.Drawing.Size(80, 24);
            this.labelActivity.TabIndex = 60;
            this.labelActivity.Text = "Unknown";
            this.labelActivity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelActivity, "Information from the telescope driver");
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.DarkOrange;
            this.label1.Location = new System.Drawing.Point(16, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 24);
            this.label1.TabIndex = 56;
            this.label1.Text = "Operating Conditions:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.DarkOrange;
            this.label3.Location = new System.Drawing.Point(32, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 24);
            this.label3.TabIndex = 58;
            this.label3.Text = "Telescope is:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.DarkOrange;
            this.label4.Location = new System.Drawing.Point(88, 224);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(96, 24);
            this.label4.TabIndex = 59;
            this.label4.Text = "Next Check in:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelNextCheck
            // 
            this.labelNextCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelNextCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelNextCheck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelNextCheck.Location = new System.Drawing.Point(200, 224);
            this.labelNextCheck.Name = "labelNextCheck";
            this.labelNextCheck.Size = new System.Drawing.Size(80, 24);
            this.labelNextCheck.TabIndex = 61;
            this.labelNextCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.labelConditions);
            this.groupBox1.Controls.Add(this.labelActivity);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(32, 128);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(256, 88);
            this.groupBox1.TabIndex = 62;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Status ";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.DarkOrange;
            this.label2.Location = new System.Drawing.Point(56, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(136, 24);
            this.label2.TabIndex = 63;
            this.label2.Text = "Operating Mode:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelOperatingMode
            // 
            this.labelOperatingMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelOperatingMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelOperatingMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelOperatingMode.Location = new System.Drawing.Point(192, 96);
            this.labelOperatingMode.Name = "labelOperatingMode";
            this.labelOperatingMode.Size = new System.Drawing.Size(48, 24);
            this.labelOperatingMode.TabIndex = 64;
            this.labelOperatingMode.Text = "???";
            this.labelOperatingMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelOperatingMode, "This information is from the SafeToOperate driver.\r\nIt combines environmental and" +
        " human\r\nintervention sensors");
            // 
            // ObsMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(567, 726);
            this.Controls.Add(this.labelOperatingMode);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.labelNextCheck);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonManualIntervention);
            this.Controls.Add(this.buttonPark);
            this.Controls.Add(this.labelDate);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.menuStrip);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.MainMenuStrip = this.menuStrip;
            this.Name = "ObsMainForm";
            this.Text = "Observatory Monitor";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Label labelDate;
        private System.Windows.Forms.Button buttonPark;
        private System.Windows.Forms.Timer timerDisplayRefresh;
        private System.Windows.Forms.Button buttonManualIntervention;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelConditions;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelActivity;
        private System.Windows.Forms.Label labelNextCheck;
        public System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem operationModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lCOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wISEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aCPToolStripMenuItem;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelOperatingMode;
    }
}

