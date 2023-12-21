namespace ASCOM.Wise40SafeToOperate
{
    partial class SafeToOperateSetupDialogForm
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
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxAge = new System.Windows.Forms.TextBox();
            this.textBoxHumidity = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxWind = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxSunElevationAtDawn = new System.Windows.Forms.TextBox();
            this.textBoxRain = new System.Windows.Forms.TextBox();
            this.textBoxRestoreSafety = new System.Windows.Forms.TextBox();
            this.textBoxCloudCoverPercent = new System.Windows.Forms.TextBox();
            this.textBoxDoorLockDelay = new System.Windows.Forms.TextBox();
            this.textBoxSunElevationAtDusk = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label26 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.checkBoxARDOEnabled = new System.Windows.Forms.CheckBox();
            this.checkBoxOWLEnabled = new System.Windows.Forms.CheckBox();
            this.checkBoxTessWEnabled = new System.Windows.Forms.CheckBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.textBoxCloudIntervalSeconds = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.textBoxCloudRepeats = new System.Windows.Forms.TextBox();
            this.textBoxWindIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxRainIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxHumidityIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxHumidityRepeats = new System.Windows.Forms.TextBox();
            this.textBoxRainRepeats = new System.Windows.Forms.TextBox();
            this.textBoxWindRepeats = new System.Windows.Forms.TextBox();
            this.textBoxSunIntervalSeconds = new System.Windows.Forms.TextBox();
            this.panelIntegration = new System.Windows.Forms.Panel();
            this.checkBoxCloud = new System.Windows.Forms.CheckBox();
            this.checkBoxRain = new System.Windows.Forms.CheckBox();
            this.checkBoxWind = new System.Windows.Forms.CheckBox();
            this.checkBoxHumidity = new System.Windows.Forms.CheckBox();
            this.checkBoxSun = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(378, 447);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(59, 24);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.toolTip1.SetToolTip(this.cmdOK, "Changes will tale effect ONLY\r\nafter ASCOM Server restart !");
            this.cmdOK.UseVisualStyleBackColor = false;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdCancel.Location = new System.Drawing.Point(378, 481);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // label1
            // 
            this.label1.ForeColor = System.Drawing.Color.DarkOrange;
            this.label1.Location = new System.Drawing.Point(65, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(244, 55);
            this.label1.TabIndex = 2;
            this.label1.Text = "Wise40 SafeToOperate SafetyMonitor.\r\nSets safety parameters and values for operat" +
    "ing the Wise40 observatory.";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = global::ASCOM.Wise40SafeToOperate.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(383, 15);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.DarkOrange;
            this.label2.Location = new System.Drawing.Point(112, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(170, 26);
            this.label2.TabIndex = 7;
            this.label2.Text = "Maximum safe values for operating\r\nthe Wise40 observatory";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.DarkOrange;
            this.label4.Location = new System.Drawing.Point(56, 330);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(133, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Unsafe if data is older than";
            // 
            // textBoxAge
            // 
            this.textBoxAge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxAge.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxAge.Location = new System.Drawing.Point(192, 326);
            this.textBoxAge.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxAge.Name = "textBoxAge";
            this.textBoxAge.Size = new System.Drawing.Size(24, 20);
            this.textBoxAge.TabIndex = 14;
            this.textBoxAge.Text = "0";
            this.textBoxAge.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxAge, "Set to zero to use data of any age.\r\n");
            this.textBoxAge.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxAge_Validating);
            // 
            // textBoxHumidity
            // 
            this.textBoxHumidity.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxHumidity.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxHumidity.Location = new System.Drawing.Point(112, 213);
            this.textBoxHumidity.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxHumidity.Name = "textBoxHumidity";
            this.textBoxHumidity.Size = new System.Drawing.Size(32, 20);
            this.textBoxHumidity.TabIndex = 19;
            this.textBoxHumidity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxHumidity, "Maximal humidity (percent)");
            this.textBoxHumidity.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxHumidity_Validating);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.DarkOrange;
            this.label9.Location = new System.Drawing.Point(152, 217);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(15, 13);
            this.label9.TabIndex = 20;
            this.label9.Text = "%";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.ForeColor = System.Drawing.Color.DarkOrange;
            this.label10.Location = new System.Drawing.Point(152, 188);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(60, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "meters/sec";
            // 
            // textBoxWind
            // 
            this.textBoxWind.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxWind.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxWind.Location = new System.Drawing.Point(112, 184);
            this.textBoxWind.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxWind.Name = "textBoxWind";
            this.textBoxWind.Size = new System.Drawing.Size(32, 20);
            this.textBoxWind.TabIndex = 21;
            this.textBoxWind.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxWind, "Maximal wind speed (meters/sec)");
            this.textBoxWind.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxWind_Validating);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.ForeColor = System.Drawing.Color.DarkOrange;
            this.label11.Location = new System.Drawing.Point(248, 132);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(47, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "(TessW)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.ForeColor = System.Drawing.Color.DarkOrange;
            this.label13.Location = new System.Drawing.Point(248, 160);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(75, 13);
            this.label13.TabIndex = 25;
            this.label13.Text = "(VantagePro2)";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.ForeColor = System.Drawing.Color.DarkOrange;
            this.label14.Location = new System.Drawing.Point(248, 188);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(75, 13);
            this.label14.TabIndex = 26;
            this.label14.Text = "(VantagePro2)";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.ForeColor = System.Drawing.Color.DarkOrange;
            this.label15.Location = new System.Drawing.Point(248, 217);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(75, 13);
            this.label15.TabIndex = 27;
            this.label15.Text = "(VantagePro2)";
            // 
            // textBoxSunElevationAtDawn
            // 
            this.textBoxSunElevationAtDawn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunElevationAtDawn.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunElevationAtDawn.Location = new System.Drawing.Point(112, 262);
            this.textBoxSunElevationAtDawn.Name = "textBoxSunElevationAtDawn";
            this.textBoxSunElevationAtDawn.Size = new System.Drawing.Size(32, 20);
            this.textBoxSunElevationAtDawn.TabIndex = 31;
            this.textBoxSunElevationAtDawn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxSunElevationAtDawn, "Maximal Sun elevation (degrees)");
            // 
            // textBoxRain
            // 
            this.textBoxRain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRain.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRain.Location = new System.Drawing.Point(112, 156);
            this.textBoxRain.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRain.Name = "textBoxRain";
            this.textBoxRain.Size = new System.Drawing.Size(32, 20);
            this.textBoxRain.TabIndex = 55;
            this.textBoxRain.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxRain, "A value of 0.0 is considered \"Dry\", anything higher\r\nrepresents \"some-rain\"");
            // 
            // textBoxRestoreSafety
            // 
            this.textBoxRestoreSafety.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRestoreSafety.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRestoreSafety.Location = new System.Drawing.Point(192, 354);
            this.textBoxRestoreSafety.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRestoreSafety.Name = "textBoxRestoreSafety";
            this.textBoxRestoreSafety.Size = new System.Drawing.Size(24, 20);
            this.textBoxRestoreSafety.TabIndex = 57;
            this.textBoxRestoreSafety.Text = "0";
            this.textBoxRestoreSafety.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxRestoreSafety, "Stabilizing period (in minutes) after conditions return to safe values.\r\n");
            // 
            // textBoxCloudCoverPercent
            // 
            this.textBoxCloudCoverPercent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxCloudCoverPercent.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxCloudCoverPercent.Location = new System.Drawing.Point(112, 128);
            this.textBoxCloudCoverPercent.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxCloudCoverPercent.Name = "textBoxCloudCoverPercent";
            this.textBoxCloudCoverPercent.Size = new System.Drawing.Size(32, 20);
            this.textBoxCloudCoverPercent.TabIndex = 59;
            this.textBoxCloudCoverPercent.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxCloudCoverPercent, "Maximal humidity (percent)");
            // 
            // textBoxDoorLockDelay
            // 
            this.textBoxDoorLockDelay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxDoorLockDelay.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxDoorLockDelay.Location = new System.Drawing.Point(192, 383);
            this.textBoxDoorLockDelay.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxDoorLockDelay.Name = "textBoxDoorLockDelay";
            this.textBoxDoorLockDelay.Size = new System.Drawing.Size(24, 20);
            this.textBoxDoorLockDelay.TabIndex = 63;
            this.textBoxDoorLockDelay.Text = "0";
            this.textBoxDoorLockDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxDoorLockDelay, "Time allowed between opening the door lock\r\n  and activating the bypass\r\n");
            // 
            // textBoxSunElevationAtDusk
            // 
            this.textBoxSunElevationAtDusk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunElevationAtDusk.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunElevationAtDusk.Location = new System.Drawing.Point(180, 262);
            this.textBoxSunElevationAtDusk.Name = "textBoxSunElevationAtDusk";
            this.textBoxSunElevationAtDusk.Size = new System.Drawing.Size(32, 20);
            this.textBoxSunElevationAtDusk.TabIndex = 65;
            this.textBoxSunElevationAtDusk.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxSunElevationAtDusk, "Maximal Sun elevation (degrees)");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label26);
            this.groupBox1.Controls.Add(this.label25);
            this.groupBox1.Controls.Add(this.label24);
            this.groupBox1.Controls.Add(this.checkBoxARDOEnabled);
            this.groupBox1.Controls.Add(this.checkBoxOWLEnabled);
            this.groupBox1.Controls.Add(this.checkBoxTessWEnabled);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(27, 412);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(296, 100);
            this.groupBox1.TabIndex = 70;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " External sensors ";
            this.toolTip1.SetToolTip(this.groupBox1, "These sensors are monitored and their data\r\n is stored in the DataBase.\r\nThey don" +
        "\'t affect the SafeToOperate decision.\r\nThey can be enabled/disabled to isolate t" +
        "he system\r\nfrom external failures");
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.ForeColor = System.Drawing.Color.DarkOrange;
            this.label26.Location = new System.Drawing.Point(82, 68);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(41, 13);
            this.label26.TabIndex = 55;
            this.label26.Text = "TessW";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.ForeColor = System.Drawing.Color.DarkOrange;
            this.label25.Location = new System.Drawing.Point(82, 45);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(179, 13);
            this.label25.TabIndex = 54;
            this.label25.Text = "Korean observatory weather stations";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.ForeColor = System.Drawing.Color.DarkOrange;
            this.label24.Location = new System.Drawing.Point(82, 21);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(177, 13);
            this.label24.TabIndex = 53;
            this.label24.Text = "Adrien\'s observatory weather station";
            // 
            // checkBoxARDOEnabled
            // 
            this.checkBoxARDOEnabled.AutoSize = true;
            this.checkBoxARDOEnabled.Checked = true;
            this.checkBoxARDOEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxARDOEnabled.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxARDOEnabled.Location = new System.Drawing.Point(13, 19);
            this.checkBoxARDOEnabled.Name = "checkBoxARDOEnabled";
            this.checkBoxARDOEnabled.Size = new System.Drawing.Size(57, 17);
            this.checkBoxARDOEnabled.TabIndex = 52;
            this.checkBoxARDOEnabled.Text = "ARDO";
            this.checkBoxARDOEnabled.UseVisualStyleBackColor = true;
            // 
            // checkBoxOWLEnabled
            // 
            this.checkBoxOWLEnabled.AutoSize = true;
            this.checkBoxOWLEnabled.Checked = true;
            this.checkBoxOWLEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOWLEnabled.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxOWLEnabled.Location = new System.Drawing.Point(13, 43);
            this.checkBoxOWLEnabled.Name = "checkBoxOWLEnabled";
            this.checkBoxOWLEnabled.Size = new System.Drawing.Size(51, 17);
            this.checkBoxOWLEnabled.TabIndex = 51;
            this.checkBoxOWLEnabled.Text = "OWL";
            this.checkBoxOWLEnabled.UseVisualStyleBackColor = true;
            // 
            // checkBoxTessWEnabled
            // 
            this.checkBoxTessWEnabled.AutoSize = true;
            this.checkBoxTessWEnabled.Checked = true;
            this.checkBoxTessWEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxTessWEnabled.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxTessWEnabled.Location = new System.Drawing.Point(13, 66);
            this.checkBoxTessWEnabled.Name = "checkBoxTessWEnabled";
            this.checkBoxTessWEnabled.Size = new System.Drawing.Size(60, 17);
            this.checkBoxTessWEnabled.TabIndex = 50;
            this.checkBoxTessWEnabled.Text = "TessW";
            this.toolTip1.SetToolTip(this.checkBoxTessWEnabled, "TessW is currently the ONLY cloud sensor Wise has.\r\nEnabling/disabling it will au" +
        "tomatically enable/disable\r\nthe Cloud sensor (above)");
            this.checkBoxTessWEnabled.UseVisualStyleBackColor = true;
            this.checkBoxTessWEnabled.CheckedChanged += new System.EventHandler(this.checkBoxTessWEnabled_CheckedChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.ForeColor = System.Drawing.Color.DarkOrange;
            this.label16.Location = new System.Drawing.Point(216, 330);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(24, 13);
            this.label16.TabIndex = 28;
            this.label16.Text = "sec";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.ForeColor = System.Drawing.Color.DarkOrange;
            this.label18.Location = new System.Drawing.Point(152, 266);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(25, 13);
            this.label18.TabIndex = 32;
            this.label18.Text = "deg";
            // 
            // textBoxCloudIntervalSeconds
            // 
            this.textBoxCloudIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxCloudIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxCloudIntervalSeconds.Location = new System.Drawing.Point(357, 128);
            this.textBoxCloudIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxCloudIntervalSeconds.Name = "textBoxCloudIntervalSeconds";
            this.textBoxCloudIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxCloudIntervalSeconds.TabIndex = 33;
            this.textBoxCloudIntervalSeconds.Text = "30";
            this.textBoxCloudIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.ForeColor = System.Drawing.Color.DarkOrange;
            this.label19.Location = new System.Drawing.Point(336, 88);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(67, 26);
            this.label19.TabIndex = 34;
            this.label19.Text = "Check every\r\n(seconds)";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.ForeColor = System.Drawing.Color.DarkOrange;
            this.label20.Location = new System.Drawing.Point(416, 88);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(59, 26);
            this.label20.TabIndex = 35;
            this.label20.Text = "Repeats till\r\nnot-safe";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxCloudRepeats
            // 
            this.textBoxCloudRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxCloudRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxCloudRepeats.Location = new System.Drawing.Point(435, 128);
            this.textBoxCloudRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxCloudRepeats.Name = "textBoxCloudRepeats";
            this.textBoxCloudRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxCloudRepeats.TabIndex = 36;
            this.textBoxCloudRepeats.Text = "3";
            this.textBoxCloudRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxWindIntervalSeconds
            // 
            this.textBoxWindIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxWindIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxWindIntervalSeconds.Location = new System.Drawing.Point(357, 184);
            this.textBoxWindIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxWindIntervalSeconds.Name = "textBoxWindIntervalSeconds";
            this.textBoxWindIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxWindIntervalSeconds.TabIndex = 37;
            this.textBoxWindIntervalSeconds.Text = "30";
            this.textBoxWindIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxRainIntervalSeconds
            // 
            this.textBoxRainIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRainIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRainIntervalSeconds.Location = new System.Drawing.Point(357, 156);
            this.textBoxRainIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRainIntervalSeconds.Name = "textBoxRainIntervalSeconds";
            this.textBoxRainIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxRainIntervalSeconds.TabIndex = 38;
            this.textBoxRainIntervalSeconds.Text = "30";
            this.textBoxRainIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxHumidityIntervalSeconds
            // 
            this.textBoxHumidityIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxHumidityIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxHumidityIntervalSeconds.Location = new System.Drawing.Point(357, 213);
            this.textBoxHumidityIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxHumidityIntervalSeconds.Name = "textBoxHumidityIntervalSeconds";
            this.textBoxHumidityIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxHumidityIntervalSeconds.TabIndex = 39;
            this.textBoxHumidityIntervalSeconds.Text = "30";
            this.textBoxHumidityIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxHumidityRepeats
            // 
            this.textBoxHumidityRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxHumidityRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxHumidityRepeats.Location = new System.Drawing.Point(435, 213);
            this.textBoxHumidityRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxHumidityRepeats.Name = "textBoxHumidityRepeats";
            this.textBoxHumidityRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxHumidityRepeats.TabIndex = 43;
            this.textBoxHumidityRepeats.Text = "4";
            this.textBoxHumidityRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxRainRepeats
            // 
            this.textBoxRainRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRainRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRainRepeats.Location = new System.Drawing.Point(435, 156);
            this.textBoxRainRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRainRepeats.Name = "textBoxRainRepeats";
            this.textBoxRainRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxRainRepeats.TabIndex = 42;
            this.textBoxRainRepeats.Text = "3";
            this.textBoxRainRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxWindRepeats
            // 
            this.textBoxWindRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxWindRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxWindRepeats.Location = new System.Drawing.Point(435, 184);
            this.textBoxWindRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxWindRepeats.Name = "textBoxWindRepeats";
            this.textBoxWindRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxWindRepeats.TabIndex = 41;
            this.textBoxWindRepeats.Text = "3";
            this.textBoxWindRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxSunIntervalSeconds
            // 
            this.textBoxSunIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunIntervalSeconds.Location = new System.Drawing.Point(357, 262);
            this.textBoxSunIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxSunIntervalSeconds.Name = "textBoxSunIntervalSeconds";
            this.textBoxSunIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxSunIntervalSeconds.TabIndex = 45;
            this.textBoxSunIntervalSeconds.Text = "30";
            this.textBoxSunIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panelIntegration
            // 
            this.panelIntegration.Location = new System.Drawing.Point(336, 88);
            this.panelIntegration.Name = "panelIntegration";
            this.panelIntegration.Size = new System.Drawing.Size(136, 218);
            this.panelIntegration.TabIndex = 47;
            // 
            // checkBoxCloud
            // 
            this.checkBoxCloud.AutoSize = true;
            this.checkBoxCloud.Checked = true;
            this.checkBoxCloud.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCloud.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxCloud.Location = new System.Drawing.Point(40, 130);
            this.checkBoxCloud.Name = "checkBoxCloud";
            this.checkBoxCloud.Size = new System.Drawing.Size(53, 17);
            this.checkBoxCloud.TabIndex = 49;
            this.checkBoxCloud.Text = "Cloud";
            this.checkBoxCloud.UseVisualStyleBackColor = true;
            this.checkBoxCloud.CheckedChanged += new System.EventHandler(this.checkBoxCloud_CheckedChanged);
            // 
            // checkBoxRain
            // 
            this.checkBoxRain.AutoSize = true;
            this.checkBoxRain.Checked = true;
            this.checkBoxRain.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRain.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxRain.Location = new System.Drawing.Point(40, 158);
            this.checkBoxRain.Name = "checkBoxRain";
            this.checkBoxRain.Size = new System.Drawing.Size(48, 17);
            this.checkBoxRain.TabIndex = 51;
            this.checkBoxRain.Text = "Rain";
            this.checkBoxRain.UseVisualStyleBackColor = true;
            // 
            // checkBoxWind
            // 
            this.checkBoxWind.AutoSize = true;
            this.checkBoxWind.Checked = true;
            this.checkBoxWind.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWind.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxWind.Location = new System.Drawing.Point(40, 186);
            this.checkBoxWind.Name = "checkBoxWind";
            this.checkBoxWind.Size = new System.Drawing.Size(51, 17);
            this.checkBoxWind.TabIndex = 52;
            this.checkBoxWind.Text = "Wind";
            this.checkBoxWind.UseVisualStyleBackColor = true;
            // 
            // checkBoxHumidity
            // 
            this.checkBoxHumidity.AutoSize = true;
            this.checkBoxHumidity.Checked = true;
            this.checkBoxHumidity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxHumidity.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxHumidity.Location = new System.Drawing.Point(40, 215);
            this.checkBoxHumidity.Name = "checkBoxHumidity";
            this.checkBoxHumidity.Size = new System.Drawing.Size(66, 17);
            this.checkBoxHumidity.TabIndex = 53;
            this.checkBoxHumidity.Text = "Humidity";
            this.checkBoxHumidity.UseVisualStyleBackColor = true;
            // 
            // checkBoxSun
            // 
            this.checkBoxSun.AutoSize = true;
            this.checkBoxSun.Checked = true;
            this.checkBoxSun.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSun.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxSun.Location = new System.Drawing.Point(40, 264);
            this.checkBoxSun.Name = "checkBoxSun";
            this.checkBoxSun.Size = new System.Drawing.Size(45, 17);
            this.checkBoxSun.TabIndex = 54;
            this.checkBoxSun.Text = "Sun";
            this.checkBoxSun.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.DarkOrange;
            this.label3.Location = new System.Drawing.Point(216, 358);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(23, 13);
            this.label3.TabIndex = 58;
            this.label3.Text = "min";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.DarkOrange;
            this.label5.Location = new System.Drawing.Point(88, 358);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 13);
            this.label5.TabIndex = 56;
            this.label5.Text = "Restore safety after";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.Color.DarkOrange;
            this.label6.Location = new System.Drawing.Point(152, 132);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(15, 13);
            this.label6.TabIndex = 60;
            this.label6.Text = "%";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.DarkOrange;
            this.label7.Location = new System.Drawing.Point(248, 266);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 61;
            this.label7.Text = "(Calculated)";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.DarkOrange;
            this.label8.Location = new System.Drawing.Point(216, 387);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(24, 13);
            this.label8.TabIndex = 64;
            this.label8.Text = "sec";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.ForeColor = System.Drawing.Color.DarkOrange;
            this.label12.Location = new System.Drawing.Point(106, 387);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(81, 13);
            this.label12.TabIndex = 62;
            this.label12.Text = "Door lock delay";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.ForeColor = System.Drawing.Color.DarkOrange;
            this.label17.Location = new System.Drawing.Point(220, 266);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(25, 13);
            this.label17.TabIndex = 66;
            this.label17.Text = "deg";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(361, 309);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(41, 13);
            this.label21.TabIndex = 67;
            this.label21.Text = "label21";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.ForeColor = System.Drawing.Color.DarkOrange;
            this.label22.Location = new System.Drawing.Point(112, 245);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(35, 13);
            this.label22.TabIndex = 68;
            this.label22.Text = "Dawn";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.ForeColor = System.Drawing.Color.DarkOrange;
            this.label23.Location = new System.Drawing.Point(180, 245);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(32, 13);
            this.label23.TabIndex = 69;
            this.label23.Text = "Dusk";
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.ForeColor = System.Drawing.Color.DarkOrange;
            this.label27.Location = new System.Drawing.Point(37, 101);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(46, 13);
            this.label27.TabIndex = 71;
            this.label27.Text = "Enabled";
            // 
            // SafeToOperateSetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(481, 525);
            this.Controls.Add(this.label27);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.textBoxSunElevationAtDusk);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.textBoxDoorLockDelay);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBoxCloudCoverPercent);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxRestoreSafety);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxRain);
            this.Controls.Add(this.checkBoxSun);
            this.Controls.Add(this.checkBoxHumidity);
            this.Controls.Add(this.checkBoxWind);
            this.Controls.Add(this.checkBoxRain);
            this.Controls.Add(this.checkBoxCloud);
            this.Controls.Add(this.textBoxSunIntervalSeconds);
            this.Controls.Add(this.textBoxHumidityRepeats);
            this.Controls.Add(this.textBoxRainRepeats);
            this.Controls.Add(this.textBoxWindRepeats);
            this.Controls.Add(this.textBoxHumidityIntervalSeconds);
            this.Controls.Add(this.textBoxRainIntervalSeconds);
            this.Controls.Add(this.textBoxWindIntervalSeconds);
            this.Controls.Add(this.textBoxCloudRepeats);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.textBoxCloudIntervalSeconds);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.textBoxSunElevationAtDawn);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textBoxWind);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.textBoxHumidity);
            this.Controls.Add(this.textBoxAge);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.panelIntegration);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SafeToOperateSetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40.SafeToOperate Setup";
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxAge;
        private System.Windows.Forms.TextBox textBoxHumidity;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxWind;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBoxSunElevationAtDawn;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBoxCloudIntervalSeconds;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBoxCloudRepeats;
        private System.Windows.Forms.TextBox textBoxWindIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxRainIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxHumidityIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxHumidityRepeats;
        private System.Windows.Forms.TextBox textBoxRainRepeats;
        private System.Windows.Forms.TextBox textBoxWindRepeats;
        private System.Windows.Forms.TextBox textBoxSunIntervalSeconds;
        private System.Windows.Forms.Panel panelIntegration;
        private System.Windows.Forms.CheckBox checkBoxCloud;
        private System.Windows.Forms.CheckBox checkBoxRain;
        private System.Windows.Forms.CheckBox checkBoxWind;
        private System.Windows.Forms.CheckBox checkBoxHumidity;
        private System.Windows.Forms.CheckBox checkBoxSun;
        private System.Windows.Forms.TextBox textBoxRain;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxRestoreSafety;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxCloudCoverPercent;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBoxDoorLockDelay;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textBoxSunElevationAtDusk;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox checkBoxARDOEnabled;
        private System.Windows.Forms.CheckBox checkBoxOWLEnabled;
        private System.Windows.Forms.CheckBox checkBoxTessWEnabled;
        private System.Windows.Forms.Label label27;
    }
}