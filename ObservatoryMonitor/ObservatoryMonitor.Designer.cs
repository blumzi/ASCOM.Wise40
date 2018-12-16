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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObsMainForm));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setupToolStripMenuItemSetup = new System.Windows.Forms.ToolStripMenuItem();
            this.operationModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aCPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lCOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wISEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.conditionsBypassToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.labelDate = new System.Windows.Forms.Label();
            this.buttonShutdown = new System.Windows.Forms.Button();
            this.timerDisplayRefresh = new System.Windows.Forms.Timer(this.components);
            this.buttonManualIntervention = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelConditions = new System.Windows.Forms.Label();
            this.labelActivity = new System.Windows.Forms.Label();
            this.labelOperatingMode = new System.Windows.Forms.Label();
            this.labelComputerControl = new System.Windows.Forms.Label();
            this.labelHumanInterventionStatus = new System.Windows.Forms.Label();
            this.buttonProjector = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labelNextCheckLabel = new System.Windows.Forms.Label();
            this.labelNextCheck = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.labelTime = new System.Windows.Forms.Label();
            this.menuStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
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
            this.menuStrip.Size = new System.Drawing.Size(600, 24);
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
            this.setupToolStripMenuItemSetup,
            this.operationModeToolStripMenuItem,
            this.conditionsBypassToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // setupToolStripMenuItemSetup
            // 
            this.setupToolStripMenuItemSetup.Name = "setupToolStripMenuItemSetup";
            this.setupToolStripMenuItemSetup.Size = new System.Drawing.Size(227, 22);
            this.setupToolStripMenuItemSetup.Text = "Observatory Monitor Setup";
            this.setupToolStripMenuItemSetup.Click += new System.EventHandler(this.setupToolStripMenuItem_Click);
            // 
            // operationModeToolStripMenuItem
            // 
            this.operationModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aCPToolStripMenuItem,
            this.lCOToolStripMenuItem,
            this.wISEToolStripMenuItem});
            this.operationModeToolStripMenuItem.Name = "operationModeToolStripMenuItem";
            this.operationModeToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.operationModeToolStripMenuItem.Text = "Operation Mode";
            // 
            // aCPToolStripMenuItem
            // 
            this.aCPToolStripMenuItem.Name = "aCPToolStripMenuItem";
            this.aCPToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.aCPToolStripMenuItem.Text = "ACP";
            this.aCPToolStripMenuItem.Click += new System.EventHandler(this.SelectOpMode);
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
            // conditionsBypassToolStripMenuItem
            // 
            this.conditionsBypassToolStripMenuItem.Name = "conditionsBypassToolStripMenuItem";
            this.conditionsBypassToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.conditionsBypassToolStripMenuItem.Text = "Bypass Operating Conditions";
            this.conditionsBypassToolStripMenuItem.Click += new System.EventHandler(this.conditionsBypassToolStripMenuItem_Click);
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
            this.listBoxLog.Location = new System.Drawing.Point(0, 242);
            this.listBoxLog.Margin = new System.Windows.Forms.Padding(8, 3, 3, 3);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.ScrollAlwaysVisible = true;
            this.listBoxLog.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listBoxLog.Size = new System.Drawing.Size(600, 484);
            this.listBoxLog.TabIndex = 40;
            // 
            // labelDate
            // 
            this.labelDate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDate.Location = new System.Drawing.Point(217, 37);
            this.labelDate.Name = "labelDate";
            this.labelDate.Size = new System.Drawing.Size(152, 27);
            this.labelDate.TabIndex = 53;
            this.labelDate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonShutdown
            // 
            this.buttonShutdown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonShutdown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonShutdown.Enabled = false;
            this.buttonShutdown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonShutdown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonShutdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonShutdown.Location = new System.Drawing.Point(439, 135);
            this.buttonShutdown.Name = "buttonShutdown";
            this.buttonShutdown.Size = new System.Drawing.Size(121, 42);
            this.buttonShutdown.TabIndex = 54;
            this.buttonShutdown.Text = "Shutdown Now";
            this.toolTip.SetToolTip(this.buttonShutdown, "Close the dome\r\nStop activities\r\nPark the telescope and dome");
            this.buttonShutdown.UseVisualStyleBackColor = false;
            this.buttonShutdown.Click += new System.EventHandler(this.buttonPark_Click);
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
            this.buttonManualIntervention.Location = new System.Drawing.Point(16, 48);
            this.buttonManualIntervention.Name = "buttonManualIntervention";
            this.buttonManualIntervention.Size = new System.Drawing.Size(96, 40);
            this.buttonManualIntervention.TabIndex = 55;
            this.buttonManualIntervention.Text = "Activate";
            this.buttonManualIntervention.UseVisualStyleBackColor = false;
            this.buttonManualIntervention.Click += new System.EventHandler(this.buttonManualIntervention_Click);
            // 
            // toolTip
            // 
            this.toolTip.IsBalloon = true;
            // 
            // labelConditions
            // 
            this.labelConditions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelConditions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConditions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelConditions.Location = new System.Drawing.Point(152, 40);
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
            this.labelActivity.Location = new System.Drawing.Point(152, 64);
            this.labelActivity.Name = "labelActivity";
            this.labelActivity.Size = new System.Drawing.Size(80, 24);
            this.labelActivity.TabIndex = 60;
            this.labelActivity.Text = "Unknown";
            this.labelActivity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelActivity, "The telescope can be either \"Active\" or \"Idle\"");
            // 
            // labelOperatingMode
            // 
            this.labelOperatingMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelOperatingMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelOperatingMode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelOperatingMode.Location = new System.Drawing.Point(312, 83);
            this.labelOperatingMode.Name = "labelOperatingMode";
            this.labelOperatingMode.Size = new System.Drawing.Size(48, 24);
            this.labelOperatingMode.TabIndex = 64;
            this.labelOperatingMode.Text = "???";
            this.labelOperatingMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelOperatingMode, "This information is from the SafeToOperate driver.\r\nIt combines environmental and" +
        " human\r\nintervention sensors");
            // 
            // labelComputerControl
            // 
            this.labelComputerControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelComputerControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelComputerControl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelComputerControl.Location = new System.Drawing.Point(152, 16);
            this.labelComputerControl.Name = "labelComputerControl";
            this.labelComputerControl.Size = new System.Drawing.Size(80, 24);
            this.labelComputerControl.TabIndex = 62;
            this.labelComputerControl.Text = "Unknown";
            this.labelComputerControl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelComputerControl, "The Computer Control switch states are either\r\n\"Operational\" or \"Maintenance\"");
            // 
            // labelHumanInterventionStatus
            // 
            this.labelHumanInterventionStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelHumanInterventionStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHumanInterventionStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHumanInterventionStatus.Location = new System.Drawing.Point(64, 16);
            this.labelHumanInterventionStatus.Name = "labelHumanInterventionStatus";
            this.labelHumanInterventionStatus.Size = new System.Drawing.Size(56, 24);
            this.labelHumanInterventionStatus.TabIndex = 64;
            this.labelHumanInterventionStatus.Text = "Inactive";
            this.labelHumanInterventionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.labelHumanInterventionStatus, "The Computer Control switch states are either\r\n\"Operational\" or \"Maintenance\"");
            // 
            // buttonProjector
            // 
            this.buttonProjector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonProjector.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonProjector.Enabled = false;
            this.buttonProjector.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonProjector.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonProjector.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonProjector.Location = new System.Drawing.Point(454, 183);
            this.buttonProjector.Name = "buttonProjector";
            this.buttonProjector.Size = new System.Drawing.Size(92, 33);
            this.buttonProjector.TabIndex = 67;
            this.buttonProjector.Text = "Projector";
            this.buttonProjector.UseVisualStyleBackColor = false;
            this.buttonProjector.Click += new System.EventHandler(this.buttonProjector_Click);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.DarkOrange;
            this.label1.Location = new System.Drawing.Point(8, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 24);
            this.label1.TabIndex = 56;
            this.label1.Text = "Safety:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.DarkOrange;
            this.label3.Location = new System.Drawing.Point(24, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 24);
            this.label3.TabIndex = 58;
            this.label3.Text = "Telescope:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelNextCheckLabel
            // 
            this.labelNextCheckLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelNextCheckLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelNextCheckLabel.ForeColor = System.Drawing.Color.DarkOrange;
            this.labelNextCheckLabel.Location = new System.Drawing.Point(208, 104);
            this.labelNextCheckLabel.Name = "labelNextCheckLabel";
            this.labelNextCheckLabel.Size = new System.Drawing.Size(96, 24);
            this.labelNextCheckLabel.TabIndex = 59;
            this.labelNextCheckLabel.Text = "Next Check in:";
            this.labelNextCheckLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelNextCheck
            // 
            this.labelNextCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelNextCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelNextCheck.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelNextCheck.Location = new System.Drawing.Point(312, 104);
            this.labelNextCheck.Name = "labelNextCheck";
            this.labelNextCheck.Size = new System.Drawing.Size(80, 24);
            this.labelNextCheck.TabIndex = 61;
            this.labelNextCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.labelComputerControl);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.labelConditions);
            this.groupBox1.Controls.Add(this.labelActivity);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(24, 128);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(240, 96);
            this.groupBox1.TabIndex = 62;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Status ";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.DarkOrange;
            this.label5.Location = new System.Drawing.Point(8, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(136, 24);
            this.label5.TabIndex = 61;
            this.label5.Text = "Computer Control:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.DarkOrange;
            this.label2.Location = new System.Drawing.Point(200, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 24);
            this.label2.TabIndex = 63;
            this.label2.Text = "Operating Mode:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.labelHumanInterventionStatus);
            this.groupBox2.Controls.Add(this.buttonManualIntervention);
            this.groupBox2.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox2.Location = new System.Drawing.Point(288, 128);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(128, 96);
            this.groupBox2.TabIndex = 65;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Human Intervention ";
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.DarkOrange;
            this.label4.Location = new System.Drawing.Point(16, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 24);
            this.label4.TabIndex = 63;
            this.label4.Text = "Status:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelTime
            // 
            this.labelTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.labelTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelTime.Location = new System.Drawing.Point(218, 56);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(152, 27);
            this.labelTime.TabIndex = 66;
            this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ObsMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(600, 726);
            this.Controls.Add(this.buttonProjector);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.labelOperatingMode);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.labelNextCheck);
            this.Controls.Add(this.labelNextCheckLabel);
            this.Controls.Add(this.buttonShutdown);
            this.Controls.Add(this.labelDate);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.menuStrip);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.Name = "ObsMainForm";
            this.Text = "Observatory Monitor";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
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
        private System.Windows.Forms.Button buttonShutdown;
        private System.Windows.Forms.Timer timerDisplayRefresh;
        private System.Windows.Forms.Button buttonManualIntervention;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelConditions;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelNextCheckLabel;
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
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelComputerControl;
        private System.Windows.Forms.ToolStripMenuItem setupToolStripMenuItemSetup;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelHumanInterventionStatus;
        private System.Windows.Forms.ToolStripMenuItem conditionsBypassToolStripMenuItem;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.Button buttonProjector;
    }
}

