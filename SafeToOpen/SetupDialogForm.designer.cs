namespace ASCOM.Wise40SafeToOpen
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
            this.comboBoxCloud = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxAge = new System.Windows.Forms.TextBox();
            this.textBoxHumidity = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.textBoxWind = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxSunElevation = new System.Windows.Forms.TextBox();
            this.textBoxRain = new System.Windows.Forms.TextBox();
            this.textBoxRestoreSafety = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.textBoxCloudIntervalSeconds = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.textBoxCloudRepeats = new System.Windows.Forms.TextBox();
            this.textBoxWindIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxRainIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxHumidityIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxLightIntervalSeconds = new System.Windows.Forms.TextBox();
            this.textBoxLightRepeats = new System.Windows.Forms.TextBox();
            this.textBoxHumidityRepeats = new System.Windows.Forms.TextBox();
            this.textBoxRainRepeats = new System.Windows.Forms.TextBox();
            this.textBoxWindRepeats = new System.Windows.Forms.TextBox();
            this.textBoxSunRepeats = new System.Windows.Forms.TextBox();
            this.textBoxSunIntervalSeconds = new System.Windows.Forms.TextBox();
            this.panelIntegration = new System.Windows.Forms.Panel();
            this.checkBoxCloud = new System.Windows.Forms.CheckBox();
            this.checkBoxLight = new System.Windows.Forms.CheckBox();
            this.checkBoxRain = new System.Windows.Forms.CheckBox();
            this.checkBoxWind = new System.Windows.Forms.CheckBox();
            this.checkBoxHumidity = new System.Windows.Forms.CheckBox();
            this.checkBoxSun = new System.Windows.Forms.CheckBox();
            this.comboBoxLight = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(493, 309);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(59, 24);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = false;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdCancel.Location = new System.Drawing.Point(493, 339);
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
            this.label1.Location = new System.Drawing.Point(40, 16);
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
            this.picASCOM.Location = new System.Drawing.Point(498, 15);
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
            // comboBoxCloud
            // 
            this.comboBoxCloud.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBoxCloud.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxCloud.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.comboBoxCloud.DisplayMember = "1";
            this.comboBoxCloud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCloud.ForeColor = System.Drawing.Color.DarkOrange;
            this.comboBoxCloud.FormattingEnabled = true;
            this.comboBoxCloud.Items.AddRange(new object[] {
            "Clear",
            "Cloudy",
            "Very Cloudy",
            "Wet"});
            this.comboBoxCloud.Location = new System.Drawing.Point(112, 128);
            this.comboBoxCloud.Name = "comboBoxCloud";
            this.comboBoxCloud.Size = new System.Drawing.Size(121, 21);
            this.comboBoxCloud.TabIndex = 8;
            this.comboBoxCloud.ValueMember = "1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.DarkOrange;
            this.label4.Location = new System.Drawing.Point(64, 320);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(133, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Unsafe if data is older than";
            // 
            // textBoxAge
            // 
            this.textBoxAge.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxAge.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxAge.Location = new System.Drawing.Point(200, 316);
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
            this.textBoxHumidity.Location = new System.Drawing.Point(112, 248);
            this.textBoxHumidity.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxHumidity.Name = "textBoxHumidity";
            this.textBoxHumidity.Size = new System.Drawing.Size(32, 20);
            this.textBoxHumidity.TabIndex = 19;
            this.textBoxHumidity.Text = "60";
            this.textBoxHumidity.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxHumidity, "Maximal humidity (percent)");
            this.textBoxHumidity.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxHumidity_Validating);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.DarkOrange;
            this.label9.Location = new System.Drawing.Point(152, 252);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(15, 13);
            this.label9.TabIndex = 20;
            this.label9.Text = "%";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.ForeColor = System.Drawing.Color.DarkOrange;
            this.label10.Location = new System.Drawing.Point(152, 223);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(60, 13);
            this.label10.TabIndex = 22;
            this.label10.Text = "meters/sec";
            // 
            // textBoxWind
            // 
            this.textBoxWind.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxWind.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxWind.Location = new System.Drawing.Point(112, 219);
            this.textBoxWind.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxWind.Name = "textBoxWind";
            this.textBoxWind.Size = new System.Drawing.Size(32, 20);
            this.textBoxWind.TabIndex = 21;
            this.textBoxWind.Text = "40";
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
            this.label11.Size = new System.Drawing.Size(57, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "(Boltwood)";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.ForeColor = System.Drawing.Color.DarkOrange;
            this.label12.Location = new System.Drawing.Point(248, 162);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(57, 13);
            this.label12.TabIndex = 24;
            this.label12.Text = "(Boltwood)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.ForeColor = System.Drawing.Color.DarkOrange;
            this.label13.Location = new System.Drawing.Point(248, 192);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(75, 13);
            this.label13.TabIndex = 25;
            this.label13.Text = "(VantagePro2)";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.ForeColor = System.Drawing.Color.DarkOrange;
            this.label14.Location = new System.Drawing.Point(248, 223);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(75, 13);
            this.label14.TabIndex = 26;
            this.label14.Text = "(VantagePro2)";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.ForeColor = System.Drawing.Color.DarkOrange;
            this.label15.Location = new System.Drawing.Point(248, 252);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(75, 13);
            this.label15.TabIndex = 27;
            this.label15.Text = "(VantagePro2)";
            // 
            // textBoxSunElevation
            // 
            this.textBoxSunElevation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunElevation.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunElevation.Location = new System.Drawing.Point(112, 280);
            this.textBoxSunElevation.Name = "textBoxSunElevation";
            this.textBoxSunElevation.Size = new System.Drawing.Size(32, 20);
            this.textBoxSunElevation.TabIndex = 31;
            this.textBoxSunElevation.Text = "-7";
            this.textBoxSunElevation.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxSunElevation, "Maximal Sun elevation (degrees)");
            // 
            // textBoxRain
            // 
            this.textBoxRain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRain.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRain.Location = new System.Drawing.Point(112, 188);
            this.textBoxRain.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRain.Name = "textBoxRain";
            this.textBoxRain.Size = new System.Drawing.Size(32, 20);
            this.textBoxRain.TabIndex = 55;
            this.textBoxRain.Text = "0.0";
            this.textBoxRain.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxRain, "A value of 0.0 is considered \"Dry\", anything higher\r\nrepresents \"some-rain\"");
            // 
            // textBoxRestoreSafety
            // 
            this.textBoxRestoreSafety.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxRestoreSafety.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxRestoreSafety.Location = new System.Drawing.Point(200, 348);
            this.textBoxRestoreSafety.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxRestoreSafety.Name = "textBoxRestoreSafety";
            this.textBoxRestoreSafety.Size = new System.Drawing.Size(24, 20);
            this.textBoxRestoreSafety.TabIndex = 57;
            this.textBoxRestoreSafety.Text = "0";
            this.textBoxRestoreSafety.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxRestoreSafety, "Stabilizing period (in minutes) after conditions return to safe values.\r\n");
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.ForeColor = System.Drawing.Color.DarkOrange;
            this.label16.Location = new System.Drawing.Point(224, 320);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(24, 13);
            this.label16.TabIndex = 28;
            this.label16.Text = "sec";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.ForeColor = System.Drawing.Color.DarkOrange;
            this.label18.Location = new System.Drawing.Point(152, 284);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(45, 13);
            this.label18.TabIndex = 32;
            this.label18.Text = "degrees";
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
            this.label20.Size = new System.Drawing.Size(62, 26);
            this.label20.TabIndex = 35;
            this.label20.Text = "Repeats for\r\nnot-safe";
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
            this.textBoxWindIntervalSeconds.Location = new System.Drawing.Point(357, 219);
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
            this.textBoxRainIntervalSeconds.Location = new System.Drawing.Point(357, 188);
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
            this.textBoxHumidityIntervalSeconds.Location = new System.Drawing.Point(357, 248);
            this.textBoxHumidityIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxHumidityIntervalSeconds.Name = "textBoxHumidityIntervalSeconds";
            this.textBoxHumidityIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxHumidityIntervalSeconds.TabIndex = 39;
            this.textBoxHumidityIntervalSeconds.Text = "30";
            this.textBoxHumidityIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxLightIntervalSeconds
            // 
            this.textBoxLightIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxLightIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxLightIntervalSeconds.Location = new System.Drawing.Point(357, 158);
            this.textBoxLightIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxLightIntervalSeconds.Name = "textBoxLightIntervalSeconds";
            this.textBoxLightIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxLightIntervalSeconds.TabIndex = 40;
            this.textBoxLightIntervalSeconds.Text = "30";
            this.textBoxLightIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxLightRepeats
            // 
            this.textBoxLightRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxLightRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxLightRepeats.Location = new System.Drawing.Point(435, 158);
            this.textBoxLightRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxLightRepeats.Name = "textBoxLightRepeats";
            this.textBoxLightRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxLightRepeats.TabIndex = 44;
            this.textBoxLightRepeats.Text = "3";
            this.textBoxLightRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxHumidityRepeats
            // 
            this.textBoxHumidityRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxHumidityRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxHumidityRepeats.Location = new System.Drawing.Point(435, 248);
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
            this.textBoxRainRepeats.Location = new System.Drawing.Point(435, 188);
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
            this.textBoxWindRepeats.Location = new System.Drawing.Point(435, 219);
            this.textBoxWindRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxWindRepeats.Name = "textBoxWindRepeats";
            this.textBoxWindRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxWindRepeats.TabIndex = 41;
            this.textBoxWindRepeats.Text = "3";
            this.textBoxWindRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxSunRepeats
            // 
            this.textBoxSunRepeats.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunRepeats.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunRepeats.Location = new System.Drawing.Point(435, 280);
            this.textBoxSunRepeats.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxSunRepeats.Name = "textBoxSunRepeats";
            this.textBoxSunRepeats.Size = new System.Drawing.Size(24, 20);
            this.textBoxSunRepeats.TabIndex = 46;
            this.textBoxSunRepeats.Text = "1";
            this.textBoxSunRepeats.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxSunIntervalSeconds
            // 
            this.textBoxSunIntervalSeconds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.textBoxSunIntervalSeconds.ForeColor = System.Drawing.Color.DarkOrange;
            this.textBoxSunIntervalSeconds.Location = new System.Drawing.Point(357, 280);
            this.textBoxSunIntervalSeconds.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxSunIntervalSeconds.Name = "textBoxSunIntervalSeconds";
            this.textBoxSunIntervalSeconds.Size = new System.Drawing.Size(24, 20);
            this.textBoxSunIntervalSeconds.TabIndex = 45;
            this.textBoxSunIntervalSeconds.Text = "30";
            this.textBoxSunIntervalSeconds.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panelIntegration
            // 
            this.panelIntegration.Location = new System.Drawing.Point(336, 120);
            this.panelIntegration.Name = "panelIntegration";
            this.panelIntegration.Size = new System.Drawing.Size(136, 192);
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
            // 
            // checkBoxLight
            // 
            this.checkBoxLight.AutoSize = true;
            this.checkBoxLight.Checked = true;
            this.checkBoxLight.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxLight.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxLight.Location = new System.Drawing.Point(40, 160);
            this.checkBoxLight.Name = "checkBoxLight";
            this.checkBoxLight.Size = new System.Drawing.Size(49, 17);
            this.checkBoxLight.TabIndex = 50;
            this.checkBoxLight.Text = "Light";
            this.checkBoxLight.UseVisualStyleBackColor = true;
            // 
            // checkBoxRain
            // 
            this.checkBoxRain.AutoSize = true;
            this.checkBoxRain.Checked = true;
            this.checkBoxRain.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRain.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxRain.Location = new System.Drawing.Point(40, 190);
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
            this.checkBoxWind.Location = new System.Drawing.Point(40, 221);
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
            this.checkBoxHumidity.Location = new System.Drawing.Point(40, 250);
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
            this.checkBoxSun.Location = new System.Drawing.Point(40, 282);
            this.checkBoxSun.Name = "checkBoxSun";
            this.checkBoxSun.Size = new System.Drawing.Size(45, 17);
            this.checkBoxSun.TabIndex = 54;
            this.checkBoxSun.Text = "Sun";
            this.checkBoxSun.UseVisualStyleBackColor = true;
            // 
            // comboBoxLight
            // 
            this.comboBoxLight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.comboBoxLight.DisplayMember = "1";
            this.comboBoxLight.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLight.ForeColor = System.Drawing.Color.DarkOrange;
            this.comboBoxLight.FormattingEnabled = true;
            this.comboBoxLight.Items.AddRange(new object[] {
            "Dark",
            "Light",
            "VeryLight"});
            this.comboBoxLight.Location = new System.Drawing.Point(112, 158);
            this.comboBoxLight.Name = "comboBoxLight";
            this.comboBoxLight.Size = new System.Drawing.Size(121, 21);
            this.comboBoxLight.TabIndex = 17;
            this.comboBoxLight.ValueMember = "1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.DarkOrange;
            this.label3.Location = new System.Drawing.Point(224, 352);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(23, 13);
            this.label3.TabIndex = 58;
            this.label3.Text = "min";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.DarkOrange;
            this.label5.Location = new System.Drawing.Point(96, 352);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 13);
            this.label5.TabIndex = 56;
            this.label5.Text = "Restore safety after";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SafeToOperateSetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(562, 385);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxRestoreSafety);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxRain);
            this.Controls.Add(this.checkBoxSun);
            this.Controls.Add(this.checkBoxHumidity);
            this.Controls.Add(this.checkBoxWind);
            this.Controls.Add(this.checkBoxRain);
            this.Controls.Add(this.checkBoxLight);
            this.Controls.Add(this.checkBoxCloud);
            this.Controls.Add(this.textBoxSunRepeats);
            this.Controls.Add(this.textBoxSunIntervalSeconds);
            this.Controls.Add(this.textBoxLightRepeats);
            this.Controls.Add(this.textBoxHumidityRepeats);
            this.Controls.Add(this.textBoxRainRepeats);
            this.Controls.Add(this.textBoxWindRepeats);
            this.Controls.Add(this.textBoxLightIntervalSeconds);
            this.Controls.Add(this.textBoxHumidityIntervalSeconds);
            this.Controls.Add(this.textBoxRainIntervalSeconds);
            this.Controls.Add(this.textBoxWindIntervalSeconds);
            this.Controls.Add(this.textBoxCloudRepeats);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.textBoxCloudIntervalSeconds);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.textBoxSunElevation);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.textBoxWind);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.textBoxHumidity);
            this.Controls.Add(this.comboBoxLight);
            this.Controls.Add(this.textBoxAge);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comboBoxCloud);
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
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxCloud;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxAge;
        private System.Windows.Forms.TextBox textBoxHumidity;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxWind;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBoxSunElevation;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBoxCloudIntervalSeconds;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBoxCloudRepeats;
        private System.Windows.Forms.TextBox textBoxWindIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxRainIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxHumidityIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxLightIntervalSeconds;
        private System.Windows.Forms.TextBox textBoxLightRepeats;
        private System.Windows.Forms.TextBox textBoxHumidityRepeats;
        private System.Windows.Forms.TextBox textBoxRainRepeats;
        private System.Windows.Forms.TextBox textBoxWindRepeats;
        private System.Windows.Forms.TextBox textBoxSunRepeats;
        private System.Windows.Forms.TextBox textBoxSunIntervalSeconds;
        private System.Windows.Forms.Panel panelIntegration;
        private System.Windows.Forms.CheckBox checkBoxCloud;
        private System.Windows.Forms.CheckBox checkBoxLight;
        private System.Windows.Forms.CheckBox checkBoxRain;
        private System.Windows.Forms.CheckBox checkBoxWind;
        private System.Windows.Forms.CheckBox checkBoxHumidity;
        private System.Windows.Forms.CheckBox checkBoxSun;
        private System.Windows.Forms.ComboBox comboBoxLight;
        private System.Windows.Forms.TextBox textBoxRain;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxRestoreSafety;
        private System.Windows.Forms.Label label5;
    }
}