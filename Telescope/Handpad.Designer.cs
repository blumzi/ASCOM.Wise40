namespace ASCOM.Wise40
{
    partial class HandpadForm
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
            this.displayRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.panelDebug = new System.Windows.Forms.Panel();
            this.groupBoxMovementStudy = new System.Windows.Forms.GroupBox();
            this.groupBoxCurrentRates = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelAxesState = new System.Windows.Forms.TableLayoutPanel();
            this.labelCurrPrimRateValue = new System.Windows.Forms.Label();
            this.labelCurrPrimDirValue = new System.Windows.Forms.Label();
            this.labelCurrSecRateValue = new System.Windows.Forms.Label();
            this.labelCurrSecDirValue = new System.Windows.Forms.Label();
            this.buttonGoCoord = new System.Windows.Forms.Button();
            this.textBoxDec = new System.Windows.Forms.TextBox();
            this.textBoxRA = new System.Windows.Forms.TextBox();
            this.labelDec = new System.Windows.Forms.Label();
            this.labelRA = new System.Windows.Forms.Label();
            this.groupBoxEncoders = new System.Windows.Forms.GroupBox();
            this.wormValue = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.axisValue = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelDecEnc = new System.Windows.Forms.Label();
            this.labelDecEncValue = new System.Windows.Forms.Label();
            this.labelHAEnc = new System.Windows.Forms.Label();
            this.labelHAEncValue = new System.Windows.Forms.Label();
            this.buttonSaveResults = new System.Windows.Forms.Button();
            this.buttonStopStudy = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxMillis = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDownStepCount = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.TextBoxLog = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.radioButtonAxisDec = new System.Windows.Forms.RadioButton();
            this.radioButtonAxisHA = new System.Windows.Forms.RadioButton();
            this.buttonGoStudy = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.radioButtonDirDown = new System.Windows.Forms.RadioButton();
            this.radioButtonDirUp = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButtonSpeedGuide = new System.Windows.Forms.RadioButton();
            this.radioButtonSpeedSet = new System.Windows.Forms.RadioButton();
            this.radioButtonSpeedSlew = new System.Windows.Forms.RadioButton();
            this.panelControls = new System.Windows.Forms.Panel();
            this.groupBox36 = new System.Windows.Forms.GroupBox();
            this.radioButtonSlew = new System.Windows.Forms.RadioButton();
            this.radioButtonGuide = new System.Windows.Forms.RadioButton();
            this.radioButtonSet = new System.Windows.Forms.RadioButton();
            this.panelDirectionButtons = new System.Windows.Forms.Panel();
            this.buttonNW = new System.Windows.Forms.Button();
            this.buttonSW = new System.Windows.Forms.Button();
            this.buttonSE = new System.Windows.Forms.Button();
            this.buttonNE = new System.Windows.Forms.Button();
            this.buttonNorth = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonSouth = new System.Windows.Forms.Button();
            this.buttonEast = new System.Windows.Forms.Button();
            this.buttonWest = new System.Windows.Forms.Button();
            this.groupBoxTelescope = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelCoordinates = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.labelLTValue = new System.Windows.Forms.Label();
            this.labelAltitude = new System.Windows.Forms.Label();
            this.labelAltitudeValue = new System.Windows.Forms.Label();
            this.labelRightAscension = new System.Windows.Forms.Label();
            this.labelUTValue = new System.Windows.Forms.Label();
            this.labelHourAngle = new System.Windows.Forms.Label();
            this.labelSiderealValue = new System.Windows.Forms.Label();
            this.labelRightAscensionValue = new System.Windows.Forms.Label();
            this.labelUT = new System.Windows.Forms.Label();
            this.labelLT = new System.Windows.Forms.Label();
            this.labelHourAngleValue = new System.Windows.Forms.Label();
            this.labelAzimuth = new System.Windows.Forms.Label();
            this.labelDeclinationValue = new System.Windows.Forms.Label();
            this.labelAzimuthValue = new System.Windows.Forms.Label();
            this.labelDeclination = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkBoxSlewingIsActive = new System.Windows.Forms.CheckBox();
            this.checkBoxSecondaryIsActive = new System.Windows.Forms.CheckBox();
            this.checkBoxTrackingIsActive = new System.Windows.Forms.CheckBox();
            this.checkBoxPrimaryIsActive = new System.Windows.Forms.CheckBox();
            this.labelDate = new System.Windows.Forms.Label();
            this.groupBoxTracking = new System.Windows.Forms.GroupBox();
            this.checkBoxEnslaveDome = new System.Windows.Forms.CheckBox();
            this.checkBoxTrack = new System.Windows.Forms.CheckBox();
            this.panelShowHideButtons = new System.Windows.Forms.Panel();
            this.buttonWeather = new System.Windows.Forms.Button();
            this.buttonStudy = new System.Windows.Forms.Button();
            this.buttonDome = new System.Windows.Forms.Button();
            this.buttonFocuser = new System.Windows.Forms.Button();
            this.buttonHardware = new System.Windows.Forms.Button();
            this.panelDome = new System.Windows.Forms.Panel();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.buttonCloseShutter = new System.Windows.Forms.Button();
            this.buttonOpenShutter = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.labelDomeShutterStatusValue = new System.Windows.Forms.Label();
            this.groupBoxDome = new System.Windows.Forms.GroupBox();
            this.label15 = new System.Windows.Forms.Label();
            this.labelDomeStatusValue = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.labelDomeAzimuthValue = new System.Windows.Forms.Label();
            this.groupBoxDomeGroup = new System.Windows.Forms.GroupBox();
            this.labelDomeSlavedConfValue = new System.Windows.Forms.Label();
            this.labelConfDomeSlaved = new System.Windows.Forms.Label();
            this.panelFocuser = new System.Windows.Forms.Panel();
            this.groupBoxFocuser = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelFocuser = new System.Windows.Forms.TableLayoutPanel();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1FocusCurrentValue = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.groupBoxWeather = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelWeather = new System.Windows.Forms.TableLayoutPanel();
            this.labelWindSpeedValue = new System.Windows.Forms.Label();
            this.labelWindDirValue = new System.Windows.Forms.Label();
            this.labelTempValue = new System.Windows.Forms.Label();
            this.labelSkyTempValue = new System.Windows.Forms.Label();
            this.labelRainRateValue = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.labelAgeValue = new System.Windows.Forms.Label();
            this.labelCloudCoverValue = new System.Windows.Forms.Label();
            this.labelDewPointValue = new System.Windows.Forms.Label();
            this.labelHumidityValue = new System.Windows.Forms.Label();
            this.labelPressureValue = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.timerStatus = new System.Windows.Forms.Timer(this.components);
            this.panelDebug.SuspendLayout();
            this.groupBoxMovementStudy.SuspendLayout();
            this.groupBoxCurrentRates.SuspendLayout();
            this.tableLayoutPanelAxesState.SuspendLayout();
            this.groupBoxEncoders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStepCount)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panelControls.SuspendLayout();
            this.groupBox36.SuspendLayout();
            this.panelDirectionButtons.SuspendLayout();
            this.groupBoxTelescope.SuspendLayout();
            this.tableLayoutPanelCoordinates.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBoxTracking.SuspendLayout();
            this.panelShowHideButtons.SuspendLayout();
            this.panelDome.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.groupBoxDome.SuspendLayout();
            this.groupBoxDomeGroup.SuspendLayout();
            this.panelFocuser.SuspendLayout();
            this.groupBoxFocuser.SuspendLayout();
            this.tableLayoutPanelFocuser.SuspendLayout();
            this.tableLayoutPanelMain.SuspendLayout();
            this.groupBoxWeather.SuspendLayout();
            this.tableLayoutPanelWeather.SuspendLayout();
            this.SuspendLayout();
            // 
            // displayRefreshTimer
            // 
            this.displayRefreshTimer.Interval = 200;
            this.displayRefreshTimer.Tick += new System.EventHandler(this.displayTimer_Tick);
            // 
            // panelDebug
            // 
            this.panelDebug.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panelDebug.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelDebug.Controls.Add(this.groupBoxMovementStudy);
            this.panelDebug.Location = new System.Drawing.Point(672, 3);
            this.panelDebug.Name = "panelDebug";
            this.tableLayoutPanelMain.SetRowSpan(this.panelDebug, 3);
            this.panelDebug.Size = new System.Drawing.Size(492, 573);
            this.panelDebug.TabIndex = 14;
            this.panelDebug.Visible = false;
            // 
            // groupBoxMovementStudy
            // 
            this.groupBoxMovementStudy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.groupBoxMovementStudy.Controls.Add(this.groupBoxCurrentRates);
            this.groupBoxMovementStudy.Controls.Add(this.buttonGoCoord);
            this.groupBoxMovementStudy.Controls.Add(this.textBoxDec);
            this.groupBoxMovementStudy.Controls.Add(this.textBoxRA);
            this.groupBoxMovementStudy.Controls.Add(this.labelDec);
            this.groupBoxMovementStudy.Controls.Add(this.labelRA);
            this.groupBoxMovementStudy.Controls.Add(this.groupBoxEncoders);
            this.groupBoxMovementStudy.Controls.Add(this.buttonSaveResults);
            this.groupBoxMovementStudy.Controls.Add(this.buttonStopStudy);
            this.groupBoxMovementStudy.Controls.Add(this.label5);
            this.groupBoxMovementStudy.Controls.Add(this.textBoxMillis);
            this.groupBoxMovementStudy.Controls.Add(this.label4);
            this.groupBoxMovementStudy.Controls.Add(this.numericUpDownStepCount);
            this.groupBoxMovementStudy.Controls.Add(this.label3);
            this.groupBoxMovementStudy.Controls.Add(this.TextBoxLog);
            this.groupBoxMovementStudy.Controls.Add(this.groupBox4);
            this.groupBoxMovementStudy.Controls.Add(this.buttonGoStudy);
            this.groupBoxMovementStudy.Controls.Add(this.groupBox5);
            this.groupBoxMovementStudy.Controls.Add(this.groupBox2);
            this.groupBoxMovementStudy.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxMovementStudy.Location = new System.Drawing.Point(8, 11);
            this.groupBoxMovementStudy.Name = "groupBoxMovementStudy";
            this.groupBoxMovementStudy.Size = new System.Drawing.Size(484, 557);
            this.groupBoxMovementStudy.TabIndex = 8;
            this.groupBoxMovementStudy.TabStop = false;
            this.groupBoxMovementStudy.Text = " Movement Study ";
            // 
            // groupBoxCurrentRates
            // 
            this.groupBoxCurrentRates.Controls.Add(this.tableLayoutPanelAxesState);
            this.groupBoxCurrentRates.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxCurrentRates.Location = new System.Drawing.Point(8, 96);
            this.groupBoxCurrentRates.Name = "groupBoxCurrentRates";
            this.groupBoxCurrentRates.Size = new System.Drawing.Size(216, 40);
            this.groupBoxCurrentRates.TabIndex = 26;
            this.groupBoxCurrentRates.TabStop = false;
            this.groupBoxCurrentRates.Text = " Current Axes State  ";
            // 
            // tableLayoutPanelAxesState
            // 
            this.tableLayoutPanelAxesState.ColumnCount = 4;
            this.tableLayoutPanelAxesState.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelAxesState.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelAxesState.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelAxesState.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelAxesState.Controls.Add(this.labelCurrPrimRateValue, 0, 0);
            this.tableLayoutPanelAxesState.Controls.Add(this.labelCurrPrimDirValue, 0, 0);
            this.tableLayoutPanelAxesState.Controls.Add(this.labelCurrSecRateValue, 3, 0);
            this.tableLayoutPanelAxesState.Controls.Add(this.labelCurrSecDirValue, 2, 0);
            this.tableLayoutPanelAxesState.Location = new System.Drawing.Point(3, 12);
            this.tableLayoutPanelAxesState.Name = "tableLayoutPanelAxesState";
            this.tableLayoutPanelAxesState.RowCount = 1;
            this.tableLayoutPanelAxesState.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelAxesState.Size = new System.Drawing.Size(205, 24);
            this.tableLayoutPanelAxesState.TabIndex = 0;
            // 
            // labelCurrPrimRateValue
            // 
            this.labelCurrPrimRateValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrPrimRateValue.AutoSize = true;
            this.labelCurrPrimRateValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelCurrPrimRateValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelCurrPrimRateValue.Location = new System.Drawing.Point(37, 0);
            this.labelCurrPrimRateValue.Name = "labelCurrPrimRateValue";
            this.labelCurrPrimRateValue.Size = new System.Drawing.Size(47, 24);
            this.labelCurrPrimRateValue.TabIndex = 11;
            this.labelCurrPrimRateValue.Text = "Stopped";
            this.labelCurrPrimRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCurrPrimDirValue
            // 
            this.labelCurrPrimDirValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrPrimDirValue.AutoSize = true;
            this.labelCurrPrimDirValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCurrPrimDirValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelCurrPrimDirValue.Location = new System.Drawing.Point(3, 0);
            this.labelCurrPrimDirValue.Name = "labelCurrPrimDirValue";
            this.labelCurrPrimDirValue.Size = new System.Drawing.Size(28, 24);
            this.labelCurrPrimDirValue.TabIndex = 5;
            this.labelCurrPrimDirValue.Text = "RA:";
            this.labelCurrPrimDirValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCurrSecRateValue
            // 
            this.labelCurrSecRateValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrSecRateValue.AutoSize = true;
            this.labelCurrSecRateValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelCurrSecRateValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelCurrSecRateValue.Location = new System.Drawing.Point(130, 0);
            this.labelCurrSecRateValue.Name = "labelCurrSecRateValue";
            this.labelCurrSecRateValue.Size = new System.Drawing.Size(72, 24);
            this.labelCurrSecRateValue.TabIndex = 12;
            this.labelCurrSecRateValue.Text = "Stopped";
            this.labelCurrSecRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCurrSecDirValue
            // 
            this.labelCurrSecDirValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrSecDirValue.AutoSize = true;
            this.labelCurrSecDirValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCurrSecDirValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelCurrSecDirValue.Location = new System.Drawing.Point(90, 0);
            this.labelCurrSecDirValue.Name = "labelCurrSecDirValue";
            this.labelCurrSecDirValue.Size = new System.Drawing.Size(34, 24);
            this.labelCurrSecDirValue.TabIndex = 6;
            this.labelCurrSecDirValue.Text = "Dec:";
            this.labelCurrSecDirValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonGoCoord
            // 
            this.buttonGoCoord.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGoCoord.FlatAppearance.BorderSize = 0;
            this.buttonGoCoord.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGoCoord.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGoCoord.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonGoCoord.Location = new System.Drawing.Point(279, 192);
            this.buttonGoCoord.Name = "buttonGoCoord";
            this.buttonGoCoord.Size = new System.Drawing.Size(48, 32);
            this.buttonGoCoord.TabIndex = 25;
            this.buttonGoCoord.Text = "Go";
            this.buttonGoCoord.UseVisualStyleBackColor = false;
            this.buttonGoCoord.Click += new System.EventHandler(this.buttonGoCoord_Click);
            // 
            // textBoxDec
            // 
            this.textBoxDec.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDec.Location = new System.Drawing.Point(169, 198);
            this.textBoxDec.Name = "textBoxDec";
            this.textBoxDec.Size = new System.Drawing.Size(100, 23);
            this.textBoxDec.TabIndex = 24;
            this.textBoxDec.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.textBoxDec_MouseDoubleClick);
            // 
            // textBoxRA
            // 
            this.textBoxRA.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRA.Location = new System.Drawing.Point(34, 198);
            this.textBoxRA.Name = "textBoxRA";
            this.textBoxRA.Size = new System.Drawing.Size(100, 23);
            this.textBoxRA.TabIndex = 23;
            this.textBoxRA.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.textBoxRA_MouseDoubleClick);
            // 
            // labelDec
            // 
            this.labelDec.AutoSize = true;
            this.labelDec.Location = new System.Drawing.Point(140, 202);
            this.labelDec.Name = "labelDec";
            this.labelDec.Size = new System.Drawing.Size(30, 13);
            this.labelDec.TabIndex = 22;
            this.labelDec.Text = "Dec:";
            // 
            // labelRA
            // 
            this.labelRA.AutoSize = true;
            this.labelRA.Location = new System.Drawing.Point(8, 202);
            this.labelRA.Name = "labelRA";
            this.labelRA.Size = new System.Drawing.Size(25, 13);
            this.labelRA.TabIndex = 21;
            this.labelRA.Text = "RA:";
            // 
            // groupBoxEncoders
            // 
            this.groupBoxEncoders.Controls.Add(this.wormValue);
            this.groupBoxEncoders.Controls.Add(this.label8);
            this.groupBoxEncoders.Controls.Add(this.axisValue);
            this.groupBoxEncoders.Controls.Add(this.label6);
            this.groupBoxEncoders.Controls.Add(this.labelDecEnc);
            this.groupBoxEncoders.Controls.Add(this.labelDecEncValue);
            this.groupBoxEncoders.Controls.Add(this.labelHAEnc);
            this.groupBoxEncoders.Controls.Add(this.labelHAEncValue);
            this.groupBoxEncoders.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxEncoders.Location = new System.Drawing.Point(235, 24);
            this.groupBoxEncoders.Name = "groupBoxEncoders";
            this.groupBoxEncoders.Size = new System.Drawing.Size(240, 112);
            this.groupBoxEncoders.TabIndex = 20;
            this.groupBoxEncoders.TabStop = false;
            this.groupBoxEncoders.Text = " Encoders ";
            // 
            // wormValue
            // 
            this.wormValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.wormValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.wormValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.wormValue.Location = new System.Drawing.Point(160, 50);
            this.wormValue.Name = "wormValue";
            this.wormValue.Size = new System.Drawing.Size(60, 20);
            this.wormValue.TabIndex = 23;
            this.wormValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.wormValue.Click += new System.EventHandler(this.label9_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label8.Location = new System.Drawing.Point(118, 47);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(39, 18);
            this.label8.TabIndex = 22;
            this.label8.Text = "Wo:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // axisValue
            // 
            this.axisValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.axisValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.axisValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.axisValue.Location = new System.Drawing.Point(49, 50);
            this.axisValue.Name = "axisValue";
            this.axisValue.Size = new System.Drawing.Size(63, 20);
            this.axisValue.TabIndex = 21;
            this.axisValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.axisValue.Click += new System.EventHandler(this.label7_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label6.Location = new System.Drawing.Point(7, 47);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(36, 18);
            this.label6.TabIndex = 20;
            this.label6.Text = "Ax:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // labelDecEnc
            // 
            this.labelDecEnc.AutoSize = true;
            this.labelDecEnc.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDecEnc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelDecEnc.Location = new System.Drawing.Point(8, 75);
            this.labelDecEnc.Name = "labelDecEnc";
            this.labelDecEnc.Size = new System.Drawing.Size(45, 18);
            this.labelDecEnc.TabIndex = 18;
            this.labelDecEnc.Text = "Dec:";
            this.labelDecEnc.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDecEncValue
            // 
            this.labelDecEncValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDecEncValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDecEncValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDecEncValue.Location = new System.Drawing.Point(56, 76);
            this.labelDecEncValue.Name = "labelDecEncValue";
            this.labelDecEncValue.Size = new System.Drawing.Size(136, 20);
            this.labelDecEncValue.TabIndex = 19;
            this.labelDecEncValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHAEnc
            // 
            this.labelHAEnc.AutoSize = true;
            this.labelHAEnc.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHAEnc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelHAEnc.Location = new System.Drawing.Point(16, 19);
            this.labelHAEnc.Name = "labelHAEnc";
            this.labelHAEnc.Size = new System.Drawing.Size(37, 18);
            this.labelHAEnc.TabIndex = 16;
            this.labelHAEnc.Text = "HA:";
            this.labelHAEnc.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHAEncValue
            // 
            this.labelHAEncValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelHAEncValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHAEncValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHAEncValue.Location = new System.Drawing.Point(56, 18);
            this.labelHAEncValue.Name = "labelHAEncValue";
            this.labelHAEncValue.Size = new System.Drawing.Size(136, 20);
            this.labelHAEncValue.TabIndex = 17;
            this.labelHAEncValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // buttonSaveResults
            // 
            this.buttonSaveResults.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSaveResults.FlatAppearance.BorderSize = 0;
            this.buttonSaveResults.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSaveResults.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSaveResults.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonSaveResults.Location = new System.Drawing.Point(144, 232);
            this.buttonSaveResults.Name = "buttonSaveResults";
            this.buttonSaveResults.Size = new System.Drawing.Size(200, 32);
            this.buttonSaveResults.TabIndex = 15;
            this.buttonSaveResults.Text = "Save results";
            this.buttonSaveResults.UseVisualStyleBackColor = false;
            this.buttonSaveResults.Click += new System.EventHandler(this.buttonSaveResults_Click);
            // 
            // buttonStopStudy
            // 
            this.buttonStopStudy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStopStudy.FlatAppearance.BorderSize = 0;
            this.buttonStopStudy.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStopStudy.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStopStudy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonStopStudy.Location = new System.Drawing.Point(333, 171);
            this.buttonStopStudy.Name = "buttonStopStudy";
            this.buttonStopStudy.Size = new System.Drawing.Size(144, 32);
            this.buttonStopStudy.TabIndex = 14;
            this.buttonStopStudy.Text = "Full Stop";
            this.buttonStopStudy.UseVisualStyleBackColor = false;
            this.buttonStopStudy.Click += new System.EventHandler(this.buttonStopStudy_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(199, 162);
            this.label5.MaximumSize = new System.Drawing.Size(100, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "millis each";
            // 
            // textBoxMillis
            // 
            this.textBoxMillis.Location = new System.Drawing.Point(151, 158);
            this.textBoxMillis.Name = "textBoxMillis";
            this.textBoxMillis.Size = new System.Drawing.Size(40, 20);
            this.textBoxMillis.TabIndex = 12;
            this.textBoxMillis.Text = "1000";
            this.textBoxMillis.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(95, 162);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "step(s) of";
            // 
            // numericUpDownStepCount
            // 
            this.numericUpDownStepCount.Location = new System.Drawing.Point(47, 158);
            this.numericUpDownStepCount.Name = "numericUpDownStepCount";
            this.numericUpDownStepCount.Size = new System.Drawing.Size(40, 20);
            this.numericUpDownStepCount.TabIndex = 10;
            this.numericUpDownStepCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 162);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Make";
            // 
            // TextBoxLog
            // 
            this.TextBoxLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.TextBoxLog.Font = new System.Drawing.Font("Lucida Console", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextBoxLog.ForeColor = System.Drawing.Color.DarkOrange;
            this.TextBoxLog.Location = new System.Drawing.Point(8, 272);
            this.TextBoxLog.Multiline = true;
            this.TextBoxLog.Name = "TextBoxLog";
            this.TextBoxLog.ReadOnly = true;
            this.TextBoxLog.ShortcutsEnabled = false;
            this.TextBoxLog.Size = new System.Drawing.Size(472, 282);
            this.TextBoxLog.TabIndex = 8;
            this.TextBoxLog.Text = "hiho";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.radioButtonAxisDec);
            this.groupBox4.Controls.Add(this.radioButtonAxisHA);
            this.groupBox4.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox4.Location = new System.Drawing.Point(8, 24);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(56, 56);
            this.groupBox4.TabIndex = 2;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = " Axis ";
            // 
            // radioButtonAxisDec
            // 
            this.radioButtonAxisDec.AutoSize = true;
            this.radioButtonAxisDec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonAxisDec.Location = new System.Drawing.Point(8, 32);
            this.radioButtonAxisDec.Name = "radioButtonAxisDec";
            this.radioButtonAxisDec.Size = new System.Drawing.Size(45, 17);
            this.radioButtonAxisDec.TabIndex = 1;
            this.radioButtonAxisDec.Text = "Dec";
            this.radioButtonAxisDec.UseVisualStyleBackColor = true;
            // 
            // radioButtonAxisHA
            // 
            this.radioButtonAxisHA.AutoSize = true;
            this.radioButtonAxisHA.Checked = true;
            this.radioButtonAxisHA.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonAxisHA.Location = new System.Drawing.Point(8, 16);
            this.radioButtonAxisHA.Name = "radioButtonAxisHA";
            this.radioButtonAxisHA.Size = new System.Drawing.Size(40, 17);
            this.radioButtonAxisHA.TabIndex = 0;
            this.radioButtonAxisHA.TabStop = true;
            this.radioButtonAxisHA.Text = "HA";
            this.radioButtonAxisHA.UseVisualStyleBackColor = true;
            // 
            // buttonGoStudy
            // 
            this.buttonGoStudy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGoStudy.FlatAppearance.BorderSize = 0;
            this.buttonGoStudy.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGoStudy.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGoStudy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonGoStudy.Location = new System.Drawing.Point(279, 154);
            this.buttonGoStudy.Name = "buttonGoStudy";
            this.buttonGoStudy.Size = new System.Drawing.Size(48, 32);
            this.buttonGoStudy.TabIndex = 7;
            this.buttonGoStudy.Text = "Go";
            this.buttonGoStudy.UseVisualStyleBackColor = false;
            this.buttonGoStudy.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.radioButtonDirDown);
            this.groupBox5.Controls.Add(this.radioButtonDirUp);
            this.groupBox5.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox5.Location = new System.Drawing.Point(80, 24);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(64, 56);
            this.groupBox5.TabIndex = 3;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = " Dir";
            // 
            // radioButtonDirDown
            // 
            this.radioButtonDirDown.AutoSize = true;
            this.radioButtonDirDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonDirDown.Location = new System.Drawing.Point(8, 32);
            this.radioButtonDirDown.Name = "radioButtonDirDown";
            this.radioButtonDirDown.Size = new System.Drawing.Size(53, 17);
            this.radioButtonDirDown.TabIndex = 1;
            this.radioButtonDirDown.Text = "Down";
            this.radioButtonDirDown.UseVisualStyleBackColor = true;
            // 
            // radioButtonDirUp
            // 
            this.radioButtonDirUp.AutoSize = true;
            this.radioButtonDirUp.Checked = true;
            this.radioButtonDirUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonDirUp.Location = new System.Drawing.Point(8, 16);
            this.radioButtonDirUp.Name = "radioButtonDirUp";
            this.radioButtonDirUp.Size = new System.Drawing.Size(39, 17);
            this.radioButtonDirUp.TabIndex = 0;
            this.radioButtonDirUp.TabStop = true;
            this.radioButtonDirUp.Text = "Up";
            this.radioButtonDirUp.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonSpeedGuide);
            this.groupBox2.Controls.Add(this.radioButtonSpeedSet);
            this.groupBox2.Controls.Add(this.radioButtonSpeedSlew);
            this.groupBox2.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox2.Location = new System.Drawing.Point(160, 24);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(64, 72);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Speed ";
            // 
            // radioButtonSpeedGuide
            // 
            this.radioButtonSpeedGuide.AutoSize = true;
            this.radioButtonSpeedGuide.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonSpeedGuide.Location = new System.Drawing.Point(8, 48);
            this.radioButtonSpeedGuide.Name = "radioButtonSpeedGuide";
            this.radioButtonSpeedGuide.Size = new System.Drawing.Size(53, 17);
            this.radioButtonSpeedGuide.TabIndex = 2;
            this.radioButtonSpeedGuide.Text = "Guide";
            this.radioButtonSpeedGuide.UseVisualStyleBackColor = true;
            this.radioButtonSpeedGuide.CheckedChanged += new System.EventHandler(this.radioButton3_CheckedChanged);
            // 
            // radioButtonSpeedSet
            // 
            this.radioButtonSpeedSet.AutoSize = true;
            this.radioButtonSpeedSet.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonSpeedSet.Location = new System.Drawing.Point(8, 32);
            this.radioButtonSpeedSet.Name = "radioButtonSpeedSet";
            this.radioButtonSpeedSet.Size = new System.Drawing.Size(41, 17);
            this.radioButtonSpeedSet.TabIndex = 1;
            this.radioButtonSpeedSet.Text = "Set";
            this.radioButtonSpeedSet.UseVisualStyleBackColor = true;
            // 
            // radioButtonSpeedSlew
            // 
            this.radioButtonSpeedSlew.AutoSize = true;
            this.radioButtonSpeedSlew.Checked = true;
            this.radioButtonSpeedSlew.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.radioButtonSpeedSlew.Location = new System.Drawing.Point(8, 16);
            this.radioButtonSpeedSlew.Name = "radioButtonSpeedSlew";
            this.radioButtonSpeedSlew.Size = new System.Drawing.Size(48, 17);
            this.radioButtonSpeedSlew.TabIndex = 0;
            this.radioButtonSpeedSlew.TabStop = true;
            this.radioButtonSpeedSlew.Text = "Slew";
            this.radioButtonSpeedSlew.UseVisualStyleBackColor = true;
            // 
            // panelControls
            // 
            this.panelControls.AutoSize = true;
            this.panelControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panelControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelControls.Controls.Add(this.groupBox36);
            this.panelControls.Controls.Add(this.panelDirectionButtons);
            this.panelControls.Controls.Add(this.groupBoxTelescope);
            this.panelControls.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelControls.Location = new System.Drawing.Point(3, 3);
            this.panelControls.Name = "panelControls";
            this.tableLayoutPanelMain.SetRowSpan(this.panelControls, 3);
            this.panelControls.Size = new System.Drawing.Size(363, 571);
            this.panelControls.TabIndex = 0;
            // 
            // groupBox36
            // 
            this.groupBox36.Controls.Add(this.radioButtonSlew);
            this.groupBox36.Controls.Add(this.radioButtonGuide);
            this.groupBox36.Controls.Add(this.radioButtonSet);
            this.groupBox36.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox36.Location = new System.Drawing.Point(84, 392);
            this.groupBox36.Name = "groupBox36";
            this.groupBox36.Size = new System.Drawing.Size(108, 77);
            this.groupBox36.TabIndex = 12;
            this.groupBox36.TabStop = false;
            this.groupBox36.Text = " Speed ";
            // 
            // radioButtonSlew
            // 
            this.radioButtonSlew.AutoSize = true;
            this.radioButtonSlew.Checked = true;
            this.radioButtonSlew.Location = new System.Drawing.Point(24, 53);
            this.radioButtonSlew.Name = "radioButtonSlew";
            this.radioButtonSlew.Size = new System.Drawing.Size(48, 17);
            this.radioButtonSlew.TabIndex = 0;
            this.radioButtonSlew.TabStop = true;
            this.radioButtonSlew.Text = "Slew";
            this.radioButtonSlew.UseVisualStyleBackColor = true;
            this.radioButtonSlew.Click += new System.EventHandler(this.radioButtonSlew_Click);
            // 
            // radioButtonGuide
            // 
            this.radioButtonGuide.AutoSize = true;
            this.radioButtonGuide.Location = new System.Drawing.Point(24, 19);
            this.radioButtonGuide.Name = "radioButtonGuide";
            this.radioButtonGuide.Size = new System.Drawing.Size(53, 17);
            this.radioButtonGuide.TabIndex = 2;
            this.radioButtonGuide.Text = "Guide";
            this.radioButtonGuide.UseVisualStyleBackColor = true;
            this.radioButtonGuide.Click += new System.EventHandler(this.radioButtonGuide_Click);
            // 
            // radioButtonSet
            // 
            this.radioButtonSet.AutoSize = true;
            this.radioButtonSet.Location = new System.Drawing.Point(24, 36);
            this.radioButtonSet.Name = "radioButtonSet";
            this.radioButtonSet.Size = new System.Drawing.Size(41, 17);
            this.radioButtonSet.TabIndex = 1;
            this.radioButtonSet.Text = "Set";
            this.radioButtonSet.UseVisualStyleBackColor = true;
            this.radioButtonSet.Click += new System.EventHandler(this.radioButtonSet_Click);
            // 
            // panelDirectionButtons
            // 
            this.panelDirectionButtons.Controls.Add(this.buttonNW);
            this.panelDirectionButtons.Controls.Add(this.buttonSW);
            this.panelDirectionButtons.Controls.Add(this.buttonSE);
            this.panelDirectionButtons.Controls.Add(this.buttonNE);
            this.panelDirectionButtons.Controls.Add(this.buttonNorth);
            this.panelDirectionButtons.Controls.Add(this.buttonStop);
            this.panelDirectionButtons.Controls.Add(this.buttonSouth);
            this.panelDirectionButtons.Controls.Add(this.buttonEast);
            this.panelDirectionButtons.Controls.Add(this.buttonWest);
            this.panelDirectionButtons.Location = new System.Drawing.Point(56, 245);
            this.panelDirectionButtons.Name = "panelDirectionButtons";
            this.panelDirectionButtons.Size = new System.Drawing.Size(160, 152);
            this.panelDirectionButtons.TabIndex = 11;
            // 
            // buttonNW
            // 
            this.buttonNW.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNW.FlatAppearance.BorderSize = 0;
            this.buttonNW.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNW.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonNW.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonNW.Location = new System.Drawing.Point(13, 10);
            this.buttonNW.Name = "buttonNW";
            this.buttonNW.Size = new System.Drawing.Size(40, 40);
            this.buttonNW.TabIndex = 8;
            this.buttonNW.Text = "NW";
            this.buttonNW.UseVisualStyleBackColor = false;
            this.buttonNW.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonNW.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonSW
            // 
            this.buttonSW.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSW.FlatAppearance.BorderSize = 0;
            this.buttonSW.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSW.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSW.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonSW.Location = new System.Drawing.Point(13, 102);
            this.buttonSW.Name = "buttonSW";
            this.buttonSW.Size = new System.Drawing.Size(40, 40);
            this.buttonSW.TabIndex = 7;
            this.buttonSW.Text = "SW";
            this.buttonSW.UseVisualStyleBackColor = false;
            this.buttonSW.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonSW.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonSE
            // 
            this.buttonSE.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSE.FlatAppearance.BorderSize = 0;
            this.buttonSE.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSE.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSE.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonSE.Location = new System.Drawing.Point(104, 102);
            this.buttonSE.Name = "buttonSE";
            this.buttonSE.Size = new System.Drawing.Size(40, 40);
            this.buttonSE.TabIndex = 6;
            this.buttonSE.Text = "SE";
            this.buttonSE.UseVisualStyleBackColor = false;
            this.buttonSE.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonSE.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonNE
            // 
            this.buttonNE.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNE.FlatAppearance.BorderSize = 0;
            this.buttonNE.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNE.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonNE.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonNE.Location = new System.Drawing.Point(104, 10);
            this.buttonNE.Name = "buttonNE";
            this.buttonNE.Size = new System.Drawing.Size(40, 40);
            this.buttonNE.TabIndex = 5;
            this.buttonNE.Text = "NE";
            this.buttonNE.UseVisualStyleBackColor = false;
            this.buttonNE.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonNE.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonNorth
            // 
            this.buttonNorth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNorth.FlatAppearance.BorderSize = 0;
            this.buttonNorth.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonNorth.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonNorth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonNorth.Location = new System.Drawing.Point(59, 10);
            this.buttonNorth.Name = "buttonNorth";
            this.buttonNorth.Size = new System.Drawing.Size(40, 40);
            this.buttonNorth.TabIndex = 0;
            this.buttonNorth.Text = "N";
            this.buttonNorth.UseVisualStyleBackColor = false;
            this.buttonNorth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonNorth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonStop
            // 
            this.buttonStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStop.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.buttonStop.FlatAppearance.BorderSize = 0;
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonStop.Location = new System.Drawing.Point(59, 56);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(40, 40);
            this.buttonStop.TabIndex = 4;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = false;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonSouth
            // 
            this.buttonSouth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSouth.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSouth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonSouth.Location = new System.Drawing.Point(59, 102);
            this.buttonSouth.Name = "buttonSouth";
            this.buttonSouth.Size = new System.Drawing.Size(40, 40);
            this.buttonSouth.TabIndex = 2;
            this.buttonSouth.Text = "S";
            this.buttonSouth.UseVisualStyleBackColor = false;
            this.buttonSouth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonSouth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonEast
            // 
            this.buttonEast.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonEast.FlatAppearance.BorderSize = 0;
            this.buttonEast.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonEast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonEast.Location = new System.Drawing.Point(13, 56);
            this.buttonEast.Name = "buttonEast";
            this.buttonEast.Size = new System.Drawing.Size(40, 40);
            this.buttonEast.TabIndex = 1;
            this.buttonEast.Text = "E";
            this.buttonEast.UseVisualStyleBackColor = false;
            this.buttonEast.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonEast.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // buttonWest
            // 
            this.buttonWest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonWest.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonWest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonWest.Location = new System.Drawing.Point(105, 56);
            this.buttonWest.Name = "buttonWest";
            this.buttonWest.Size = new System.Drawing.Size(40, 40);
            this.buttonWest.TabIndex = 3;
            this.buttonWest.Text = "W";
            this.buttonWest.UseVisualStyleBackColor = false;
            this.buttonWest.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonWest.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
            // 
            // groupBoxTelescope
            // 
            this.groupBoxTelescope.Controls.Add(this.labelStatus);
            this.groupBoxTelescope.Controls.Add(this.tableLayoutPanelCoordinates);
            this.groupBoxTelescope.Controls.Add(this.groupBox3);
            this.groupBoxTelescope.Controls.Add(this.labelDate);
            this.groupBoxTelescope.Controls.Add(this.groupBoxTracking);
            this.groupBoxTelescope.Controls.Add(this.panelShowHideButtons);
            this.groupBoxTelescope.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxTelescope.Location = new System.Drawing.Point(0, 11);
            this.groupBoxTelescope.Name = "groupBoxTelescope";
            this.groupBoxTelescope.Size = new System.Drawing.Size(360, 557);
            this.groupBoxTelescope.TabIndex = 18;
            this.groupBoxTelescope.TabStop = false;
            this.groupBoxTelescope.Text = " Telescope ";
            // 
            // tableLayoutPanelCoordinates
            // 
            this.tableLayoutPanelCoordinates.ColumnCount = 4;
            this.tableLayoutPanelCoordinates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.68365F));
            this.tableLayoutPanelCoordinates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36.99398F));
            this.tableLayoutPanelCoordinates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13.3284F));
            this.tableLayoutPanelCoordinates.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36.99398F));
            this.tableLayoutPanelCoordinates.Controls.Add(this.label2, 2, 1);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelLTValue, 3, 0);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelAltitude, 0, 3);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelAltitudeValue, 1, 3);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelRightAscension, 0, 2);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelUTValue, 1, 0);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelHourAngle, 0, 1);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelSiderealValue, 3, 1);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelRightAscensionValue, 1, 2);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelUT, 0, 0);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelLT, 2, 0);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelHourAngleValue, 1, 1);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelAzimuth, 2, 3);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelDeclinationValue, 3, 2);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelAzimuthValue, 3, 3);
            this.tableLayoutPanelCoordinates.Controls.Add(this.labelDeclination, 2, 2);
            this.tableLayoutPanelCoordinates.Location = new System.Drawing.Point(4, 56);
            this.tableLayoutPanelCoordinates.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanelCoordinates.Name = "tableLayoutPanelCoordinates";
            this.tableLayoutPanelCoordinates.RowCount = 4;
            this.tableLayoutPanelCoordinates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanelCoordinates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanelCoordinates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanelCoordinates.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanelCoordinates.Size = new System.Drawing.Size(350, 128);
            this.tableLayoutPanelCoordinates.TabIndex = 18;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label2.Location = new System.Drawing.Point(176, 32);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 32);
            this.label2.TabIndex = 24;
            this.label2.Text = "LST:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelLTValue
            // 
            this.labelLTValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLTValue.AutoSize = true;
            this.labelLTValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelLTValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLTValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelLTValue.Location = new System.Drawing.Point(232, 0);
            this.labelLTValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelLTValue.Name = "labelLTValue";
            this.labelLTValue.Size = new System.Drawing.Size(118, 32);
            this.labelLTValue.TabIndex = 22;
            this.labelLTValue.Text = "00h00m00.0s";
            this.labelLTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelAltitude
            // 
            this.labelAltitude.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAltitude.AutoSize = true;
            this.labelAltitude.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAltitude.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelAltitude.Location = new System.Drawing.Point(5, 96);
            this.labelAltitude.Margin = new System.Windows.Forms.Padding(0);
            this.labelAltitude.Name = "labelAltitude";
            this.labelAltitude.Size = new System.Drawing.Size(39, 32);
            this.labelAltitude.TabIndex = 14;
            this.labelAltitude.Text = "ALT:";
            this.labelAltitude.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelAltitudeValue
            // 
            this.labelAltitudeValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAltitudeValue.AutoSize = true;
            this.labelAltitudeValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelAltitudeValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAltitudeValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelAltitudeValue.Location = new System.Drawing.Point(55, 96);
            this.labelAltitudeValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelAltitudeValue.Name = "labelAltitudeValue";
            this.labelAltitudeValue.Size = new System.Drawing.Size(118, 32);
            this.labelAltitudeValue.TabIndex = 17;
            this.labelAltitudeValue.Text = "00°00\'00.0\"";
            this.labelAltitudeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelRightAscension
            // 
            this.labelRightAscension.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRightAscension.AutoSize = true;
            this.labelRightAscension.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRightAscension.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelRightAscension.Location = new System.Drawing.Point(9, 64);
            this.labelRightAscension.Margin = new System.Windows.Forms.Padding(0);
            this.labelRightAscension.Name = "labelRightAscension";
            this.labelRightAscension.Size = new System.Drawing.Size(35, 32);
            this.labelRightAscension.TabIndex = 11;
            this.labelRightAscension.Text = "RA:";
            this.labelRightAscension.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelUTValue
            // 
            this.labelUTValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelUTValue.AutoSize = true;
            this.labelUTValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelUTValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUTValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelUTValue.Location = new System.Drawing.Point(55, 0);
            this.labelUTValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelUTValue.Name = "labelUTValue";
            this.labelUTValue.Size = new System.Drawing.Size(118, 32);
            this.labelUTValue.TabIndex = 20;
            this.labelUTValue.Text = "00h00m00.0s";
            this.labelUTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHourAngle
            // 
            this.labelHourAngle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHourAngle.AutoSize = true;
            this.labelHourAngle.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHourAngle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelHourAngle.Location = new System.Drawing.Point(7, 32);
            this.labelHourAngle.Margin = new System.Windows.Forms.Padding(0);
            this.labelHourAngle.Name = "labelHourAngle";
            this.labelHourAngle.Size = new System.Drawing.Size(37, 32);
            this.labelHourAngle.TabIndex = 9;
            this.labelHourAngle.Text = "HA:";
            this.labelHourAngle.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelSiderealValue
            // 
            this.labelSiderealValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSiderealValue.AutoSize = true;
            this.labelSiderealValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelSiderealValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSiderealValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelSiderealValue.Location = new System.Drawing.Point(232, 32);
            this.labelSiderealValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelSiderealValue.Name = "labelSiderealValue";
            this.labelSiderealValue.Size = new System.Drawing.Size(118, 32);
            this.labelSiderealValue.TabIndex = 8;
            this.labelSiderealValue.Text = "00h00m00.0s";
            this.labelSiderealValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelRightAscensionValue
            // 
            this.labelRightAscensionValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRightAscensionValue.AutoSize = true;
            this.labelRightAscensionValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelRightAscensionValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRightAscensionValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelRightAscensionValue.Location = new System.Drawing.Point(45, 64);
            this.labelRightAscensionValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelRightAscensionValue.Name = "labelRightAscensionValue";
            this.labelRightAscensionValue.Size = new System.Drawing.Size(128, 32);
            this.labelRightAscensionValue.TabIndex = 12;
            this.labelRightAscensionValue.Text = "-00h00m00.0s";
            this.labelRightAscensionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelUT
            // 
            this.labelUT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelUT.AutoSize = true;
            this.labelUT.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUT.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelUT.Location = new System.Drawing.Point(8, 0);
            this.labelUT.Margin = new System.Windows.Forms.Padding(0);
            this.labelUT.Name = "labelUT";
            this.labelUT.Size = new System.Drawing.Size(36, 32);
            this.labelUT.TabIndex = 19;
            this.labelUT.Text = "UT:";
            this.labelUT.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelLT
            // 
            this.labelLT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLT.AutoSize = true;
            this.labelLT.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLT.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelLT.Location = new System.Drawing.Point(185, 0);
            this.labelLT.Margin = new System.Windows.Forms.Padding(0);
            this.labelLT.Name = "labelLT";
            this.labelLT.Size = new System.Drawing.Size(34, 32);
            this.labelLT.TabIndex = 21;
            this.labelLT.Text = "LT:";
            this.labelLT.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHourAngleValue
            // 
            this.labelHourAngleValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHourAngleValue.AutoSize = true;
            this.labelHourAngleValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelHourAngleValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHourAngleValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHourAngleValue.Location = new System.Drawing.Point(45, 32);
            this.labelHourAngleValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelHourAngleValue.Name = "labelHourAngleValue";
            this.labelHourAngleValue.Size = new System.Drawing.Size(128, 32);
            this.labelHourAngleValue.TabIndex = 10;
            this.labelHourAngleValue.Text = "-00h00m00.0s";
            this.labelHourAngleValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelAzimuth
            // 
            this.labelAzimuth.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAzimuth.AutoSize = true;
            this.labelAzimuth.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAzimuth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelAzimuth.Location = new System.Drawing.Point(178, 96);
            this.labelAzimuth.Margin = new System.Windows.Forms.Padding(0);
            this.labelAzimuth.Name = "labelAzimuth";
            this.labelAzimuth.Size = new System.Drawing.Size(41, 32);
            this.labelAzimuth.TabIndex = 15;
            this.labelAzimuth.Text = " AZ:";
            this.labelAzimuth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDeclinationValue
            // 
            this.labelDeclinationValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDeclinationValue.AutoSize = true;
            this.labelDeclinationValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDeclinationValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeclinationValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDeclinationValue.Location = new System.Drawing.Point(232, 64);
            this.labelDeclinationValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelDeclinationValue.Name = "labelDeclinationValue";
            this.labelDeclinationValue.Size = new System.Drawing.Size(118, 32);
            this.labelDeclinationValue.TabIndex = 16;
            this.labelDeclinationValue.Text = "00°00\'00.0\"";
            this.labelDeclinationValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelAzimuthValue
            // 
            this.labelAzimuthValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAzimuthValue.AutoSize = true;
            this.labelAzimuthValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelAzimuthValue.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAzimuthValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelAzimuthValue.Location = new System.Drawing.Point(222, 96);
            this.labelAzimuthValue.Margin = new System.Windows.Forms.Padding(0);
            this.labelAzimuthValue.Name = "labelAzimuthValue";
            this.labelAzimuthValue.Size = new System.Drawing.Size(128, 32);
            this.labelAzimuthValue.TabIndex = 18;
            this.labelAzimuthValue.Text = "000°00\'00.0\"";
            this.labelAzimuthValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDeclination
            // 
            this.labelDeclination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDeclination.AutoSize = true;
            this.labelDeclination.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDeclination.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeclination.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelDeclination.Location = new System.Drawing.Point(178, 64);
            this.labelDeclination.Margin = new System.Windows.Forms.Padding(0);
            this.labelDeclination.Name = "labelDeclination";
            this.labelDeclination.Size = new System.Drawing.Size(41, 32);
            this.labelDeclination.TabIndex = 13;
            this.labelDeclination.Text = "DEC:";
            this.labelDeclination.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.checkBoxSlewingIsActive);
            this.groupBox3.Controls.Add(this.checkBoxSecondaryIsActive);
            this.groupBox3.Controls.Add(this.checkBoxTrackingIsActive);
            this.groupBox3.Controls.Add(this.checkBoxPrimaryIsActive);
            this.groupBox3.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox3.Location = new System.Drawing.Point(224, 253);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(88, 102);
            this.groupBox3.TabIndex = 17;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = " Active  ";
            // 
            // checkBoxSlewingIsActive
            // 
            this.checkBoxSlewingIsActive.AutoCheck = false;
            this.checkBoxSlewingIsActive.AutoSize = true;
            this.checkBoxSlewingIsActive.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxSlewingIsActive.Location = new System.Drawing.Point(8, 80);
            this.checkBoxSlewingIsActive.Name = "checkBoxSlewingIsActive";
            this.checkBoxSlewingIsActive.Size = new System.Drawing.Size(63, 17);
            this.checkBoxSlewingIsActive.TabIndex = 3;
            this.checkBoxSlewingIsActive.Text = "Slewing";
            this.checkBoxSlewingIsActive.UseVisualStyleBackColor = true;
            // 
            // checkBoxSecondaryIsActive
            // 
            this.checkBoxSecondaryIsActive.AutoCheck = false;
            this.checkBoxSecondaryIsActive.AutoSize = true;
            this.checkBoxSecondaryIsActive.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxSecondaryIsActive.Location = new System.Drawing.Point(8, 40);
            this.checkBoxSecondaryIsActive.Name = "checkBoxSecondaryIsActive";
            this.checkBoxSecondaryIsActive.Size = new System.Drawing.Size(77, 17);
            this.checkBoxSecondaryIsActive.TabIndex = 1;
            this.checkBoxSecondaryIsActive.Text = "Secondary";
            this.checkBoxSecondaryIsActive.UseVisualStyleBackColor = true;
            // 
            // checkBoxTrackingIsActive
            // 
            this.checkBoxTrackingIsActive.AutoCheck = false;
            this.checkBoxTrackingIsActive.AutoSize = true;
            this.checkBoxTrackingIsActive.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxTrackingIsActive.Location = new System.Drawing.Point(8, 60);
            this.checkBoxTrackingIsActive.Name = "checkBoxTrackingIsActive";
            this.checkBoxTrackingIsActive.Size = new System.Drawing.Size(68, 17);
            this.checkBoxTrackingIsActive.TabIndex = 2;
            this.checkBoxTrackingIsActive.Text = "Tracking";
            this.checkBoxTrackingIsActive.UseVisualStyleBackColor = true;
            this.checkBoxTrackingIsActive.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // checkBoxPrimaryIsActive
            // 
            this.checkBoxPrimaryIsActive.AutoCheck = false;
            this.checkBoxPrimaryIsActive.AutoSize = true;
            this.checkBoxPrimaryIsActive.ForeColor = System.Drawing.Color.DarkOrange;
            this.checkBoxPrimaryIsActive.Location = new System.Drawing.Point(8, 20);
            this.checkBoxPrimaryIsActive.Name = "checkBoxPrimaryIsActive";
            this.checkBoxPrimaryIsActive.Size = new System.Drawing.Size(60, 17);
            this.checkBoxPrimaryIsActive.TabIndex = 0;
            this.checkBoxPrimaryIsActive.Text = "Primary";
            this.checkBoxPrimaryIsActive.UseVisualStyleBackColor = true;
            // 
            // labelDate
            // 
            this.labelDate.AutoSize = true;
            this.labelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelDate.Location = new System.Drawing.Point(118, 22);
            this.labelDate.Name = "labelDate";
            this.labelDate.Size = new System.Drawing.Size(152, 17);
            this.labelDate.TabIndex = 23;
            this.labelDate.Text = "Feb 16, 2016 12:51:00";
            // 
            // groupBoxTracking
            // 
            this.groupBoxTracking.Controls.Add(this.checkBoxEnslaveDome);
            this.groupBoxTracking.Controls.Add(this.checkBoxTrack);
            this.groupBoxTracking.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxTracking.Location = new System.Drawing.Point(84, 482);
            this.groupBoxTracking.Name = "groupBoxTracking";
            this.groupBoxTracking.Size = new System.Drawing.Size(108, 56);
            this.groupBoxTracking.TabIndex = 0;
            this.groupBoxTracking.TabStop = false;
            this.groupBoxTracking.Text = " Tracking ";
            // 
            // checkBoxEnslaveDome
            // 
            this.checkBoxEnslaveDome.AutoSize = true;
            this.checkBoxEnslaveDome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.checkBoxEnslaveDome.Location = new System.Drawing.Point(8, 32);
            this.checkBoxEnslaveDome.Name = "checkBoxEnslaveDome";
            this.checkBoxEnslaveDome.Size = new System.Drawing.Size(95, 17);
            this.checkBoxEnslaveDome.TabIndex = 7;
            this.checkBoxEnslaveDome.Text = "Enslave Dome";
            this.checkBoxEnslaveDome.UseVisualStyleBackColor = true;
            this.checkBoxEnslaveDome.CheckedChanged += new System.EventHandler(this.checkBoxEnslaveDome_CheckedChanged);
            // 
            // checkBoxTrack
            // 
            this.checkBoxTrack.AutoSize = true;
            this.checkBoxTrack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.checkBoxTrack.Location = new System.Drawing.Point(8, 16);
            this.checkBoxTrack.Name = "checkBoxTrack";
            this.checkBoxTrack.Size = new System.Drawing.Size(54, 17);
            this.checkBoxTrack.TabIndex = 6;
            this.checkBoxTrack.Text = "Track";
            this.checkBoxTrack.UseVisualStyleBackColor = true;
            this.checkBoxTrack.CheckedChanged += new System.EventHandler(this.checkBoxTrack_CheckedChanged);
            // 
            // panelShowHideButtons
            // 
            this.panelShowHideButtons.Controls.Add(this.buttonWeather);
            this.panelShowHideButtons.Controls.Add(this.buttonStudy);
            this.panelShowHideButtons.Controls.Add(this.buttonDome);
            this.panelShowHideButtons.Controls.Add(this.buttonFocuser);
            this.panelShowHideButtons.Controls.Add(this.buttonHardware);
            this.panelShowHideButtons.Location = new System.Drawing.Point(218, 404);
            this.panelShowHideButtons.Name = "panelShowHideButtons";
            this.panelShowHideButtons.Size = new System.Drawing.Size(100, 136);
            this.panelShowHideButtons.TabIndex = 15;
            // 
            // buttonWeather
            // 
            this.buttonWeather.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonWeather.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonWeather.Location = new System.Drawing.Point(4, 85);
            this.buttonWeather.Name = "buttonWeather";
            this.buttonWeather.Size = new System.Drawing.Size(93, 23);
            this.buttonWeather.TabIndex = 16;
            this.buttonWeather.Text = "Show Weather";
            this.buttonWeather.UseVisualStyleBackColor = false;
            this.buttonWeather.Click += new System.EventHandler(this.buttonWeather_Click);
            // 
            // buttonStudy
            // 
            this.buttonStudy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStudy.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonStudy.Location = new System.Drawing.Point(4, 4);
            this.buttonStudy.Name = "buttonStudy";
            this.buttonStudy.Size = new System.Drawing.Size(93, 23);
            this.buttonStudy.TabIndex = 15;
            this.buttonStudy.Text = "Show Study";
            this.buttonStudy.UseVisualStyleBackColor = false;
            this.buttonStudy.Click += new System.EventHandler(this.buttonHandpad_Click);
            // 
            // buttonDome
            // 
            this.buttonDome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonDome.Location = new System.Drawing.Point(4, 31);
            this.buttonDome.Name = "buttonDome";
            this.buttonDome.Size = new System.Drawing.Size(93, 23);
            this.buttonDome.TabIndex = 13;
            this.buttonDome.Text = "Show Dome";
            this.buttonDome.UseVisualStyleBackColor = false;
            this.buttonDome.Click += new System.EventHandler(this.buttonDome_Click);
            // 
            // buttonFocuser
            // 
            this.buttonFocuser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocuser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocuser.Location = new System.Drawing.Point(4, 58);
            this.buttonFocuser.Name = "buttonFocuser";
            this.buttonFocuser.Size = new System.Drawing.Size(93, 23);
            this.buttonFocuser.TabIndex = 14;
            this.buttonFocuser.Text = "Show Focuser";
            this.buttonFocuser.UseVisualStyleBackColor = false;
            this.buttonFocuser.Click += new System.EventHandler(this.buttonFocuser_Click);
            // 
            // buttonHardware
            // 
            this.buttonHardware.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonHardware.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonHardware.Location = new System.Drawing.Point(4, 112);
            this.buttonHardware.Name = "buttonHardware";
            this.buttonHardware.Size = new System.Drawing.Size(93, 23);
            this.buttonHardware.TabIndex = 10;
            this.buttonHardware.Text = "Show Hardware";
            this.buttonHardware.UseVisualStyleBackColor = false;
            this.buttonHardware.Click += new System.EventHandler(this.buttonHardware_Click);
            // 
            // panelDome
            // 
            this.panelDome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelDome.Controls.Add(this.groupBox7);
            this.panelDome.Controls.Add(this.groupBoxDome);
            this.panelDome.Controls.Add(this.groupBoxDomeGroup);
            this.panelDome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.panelDome.Location = new System.Drawing.Point(372, 3);
            this.panelDome.Name = "panelDome";
            this.panelDome.Size = new System.Drawing.Size(294, 253);
            this.panelDome.TabIndex = 13;
            this.panelDome.Visible = false;
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.buttonCloseShutter);
            this.groupBox7.Controls.Add(this.buttonOpenShutter);
            this.groupBox7.Controls.Add(this.label9);
            this.groupBox7.Controls.Add(this.labelDomeShutterStatusValue);
            this.groupBox7.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox7.Location = new System.Drawing.Point(24, 136);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(248, 88);
            this.groupBox7.TabIndex = 24;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = " Shutter ";
            // 
            // buttonCloseShutter
            // 
            this.buttonCloseShutter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonCloseShutter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonCloseShutter.Location = new System.Drawing.Point(128, 56);
            this.buttonCloseShutter.Name = "buttonCloseShutter";
            this.buttonCloseShutter.Size = new System.Drawing.Size(58, 23);
            this.buttonCloseShutter.TabIndex = 21;
            this.buttonCloseShutter.Text = "Close";
            this.buttonCloseShutter.UseVisualStyleBackColor = false;
            this.buttonCloseShutter.Click += new System.EventHandler(this.buttonCloseShutterClick);
            // 
            // buttonOpenShutter
            // 
            this.buttonOpenShutter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonOpenShutter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonOpenShutter.Location = new System.Drawing.Point(56, 56);
            this.buttonOpenShutter.Name = "buttonOpenShutter";
            this.buttonOpenShutter.Size = new System.Drawing.Size(58, 23);
            this.buttonOpenShutter.TabIndex = 20;
            this.buttonOpenShutter.Text = "Open";
            this.buttonOpenShutter.UseVisualStyleBackColor = false;
            this.buttonOpenShutter.Click += new System.EventHandler(this.buttonOpenShutterClick);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label9.Location = new System.Drawing.Point(34, 16);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(64, 18);
            this.label9.TabIndex = 18;
            this.label9.Text = "Status:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDomeShutterStatusValue
            // 
            this.labelDomeShutterStatusValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDomeShutterStatusValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDomeShutterStatusValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDomeShutterStatusValue.Location = new System.Drawing.Point(104, 21);
            this.labelDomeShutterStatusValue.Name = "labelDomeShutterStatusValue";
            this.labelDomeShutterStatusValue.Size = new System.Drawing.Size(128, 20);
            this.labelDomeShutterStatusValue.TabIndex = 19;
            this.labelDomeShutterStatusValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBoxDome
            // 
            this.groupBoxDome.Controls.Add(this.label15);
            this.groupBoxDome.Controls.Add(this.labelDomeStatusValue);
            this.groupBoxDome.Controls.Add(this.label17);
            this.groupBoxDome.Controls.Add(this.labelDomeAzimuthValue);
            this.groupBoxDome.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxDome.Location = new System.Drawing.Point(24, 48);
            this.groupBoxDome.Name = "groupBoxDome";
            this.groupBoxDome.Size = new System.Drawing.Size(248, 80);
            this.groupBoxDome.TabIndex = 23;
            this.groupBoxDome.TabStop = false;
            this.groupBoxDome.Text = " Dome ";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label15.Location = new System.Drawing.Point(34, 48);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(64, 18);
            this.label15.TabIndex = 18;
            this.label15.Text = "Status:";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDomeStatusValue
            // 
            this.labelDomeStatusValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDomeStatusValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDomeStatusValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDomeStatusValue.Location = new System.Drawing.Point(104, 54);
            this.labelDomeStatusValue.Name = "labelDomeStatusValue";
            this.labelDomeStatusValue.Size = new System.Drawing.Size(128, 20);
            this.labelDomeStatusValue.TabIndex = 19;
            this.labelDomeStatusValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label17.Location = new System.Drawing.Point(16, 19);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(82, 18);
            this.label17.TabIndex = 16;
            this.label17.Text = "Azimuth:";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDomeAzimuthValue
            // 
            this.labelDomeAzimuthValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDomeAzimuthValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDomeAzimuthValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDomeAzimuthValue.Location = new System.Drawing.Point(104, 24);
            this.labelDomeAzimuthValue.Name = "labelDomeAzimuthValue";
            this.labelDomeAzimuthValue.Size = new System.Drawing.Size(128, 20);
            this.labelDomeAzimuthValue.TabIndex = 17;
            this.labelDomeAzimuthValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBoxDomeGroup
            // 
            this.groupBoxDomeGroup.Controls.Add(this.labelDomeSlavedConfValue);
            this.groupBoxDomeGroup.Controls.Add(this.labelConfDomeSlaved);
            this.groupBoxDomeGroup.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxDomeGroup.Location = new System.Drawing.Point(8, 11);
            this.groupBoxDomeGroup.Name = "groupBoxDomeGroup";
            this.groupBoxDomeGroup.Size = new System.Drawing.Size(280, 232);
            this.groupBoxDomeGroup.TabIndex = 25;
            this.groupBoxDomeGroup.TabStop = false;
            this.groupBoxDomeGroup.Text = " Dome ";
            // 
            // labelDomeSlavedConfValue
            // 
            this.labelDomeSlavedConfValue.AutoSize = true;
            this.labelDomeSlavedConfValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDomeSlavedConfValue.Location = new System.Drawing.Point(92, 24);
            this.labelDomeSlavedConfValue.Name = "labelDomeSlavedConfValue";
            this.labelDomeSlavedConfValue.Size = new System.Drawing.Size(138, 13);
            this.labelDomeSlavedConfValue.TabIndex = 1;
            this.labelDomeSlavedConfValue.Text = "Not enslaved while tracking";
            // 
            // labelConfDomeSlaved
            // 
            this.labelConfDomeSlaved.AutoSize = true;
            this.labelConfDomeSlaved.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfDomeSlaved.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelConfDomeSlaved.Location = new System.Drawing.Point(48, 22);
            this.labelConfDomeSlaved.Name = "labelConfDomeSlaved";
            this.labelConfDomeSlaved.Size = new System.Drawing.Size(51, 13);
            this.labelConfDomeSlaved.TabIndex = 0;
            this.labelConfDomeSlaved.Text = "Config: ";
            this.labelConfDomeSlaved.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelFocuser
            // 
            this.panelFocuser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelFocuser.Controls.Add(this.groupBoxFocuser);
            this.panelFocuser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.panelFocuser.Location = new System.Drawing.Point(372, 262);
            this.panelFocuser.Name = "panelFocuser";
            this.panelFocuser.Size = new System.Drawing.Size(294, 146);
            this.panelFocuser.TabIndex = 14;
            this.panelFocuser.Visible = false;
            // 
            // groupBoxFocuser
            // 
            this.groupBoxFocuser.Controls.Add(this.tableLayoutPanelFocuser);
            this.groupBoxFocuser.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxFocuser.Location = new System.Drawing.Point(8, -1);
            this.groupBoxFocuser.Name = "groupBoxFocuser";
            this.groupBoxFocuser.Size = new System.Drawing.Size(280, 137);
            this.groupBoxFocuser.TabIndex = 26;
            this.groupBoxFocuser.TabStop = false;
            this.groupBoxFocuser.Text = " Focuser ";
            // 
            // tableLayoutPanelFocuser
            // 
            this.tableLayoutPanelFocuser.ColumnCount = 4;
            this.tableLayoutPanelFocuser.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelFocuser.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelFocuser.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelFocuser.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelFocuser.Controls.Add(this.button3, 3, 1);
            this.tableLayoutPanelFocuser.Controls.Add(this.button4, 2, 1);
            this.tableLayoutPanelFocuser.Controls.Add(this.button2, 3, 0);
            this.tableLayoutPanelFocuser.Controls.Add(this.button1, 0, 1);
            this.tableLayoutPanelFocuser.Controls.Add(this.textBox2, 1, 1);
            this.tableLayoutPanelFocuser.Controls.Add(this.label1FocusCurrentValue, 1, 0);
            this.tableLayoutPanelFocuser.Controls.Add(this.label11, 0, 0);
            this.tableLayoutPanelFocuser.Controls.Add(this.button5, 2, 0);
            this.tableLayoutPanelFocuser.Location = new System.Drawing.Point(8, 32);
            this.tableLayoutPanelFocuser.Name = "tableLayoutPanelFocuser";
            this.tableLayoutPanelFocuser.RowCount = 2;
            this.tableLayoutPanelFocuser.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelFocuser.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelFocuser.Size = new System.Drawing.Size(256, 88);
            this.tableLayoutPanelFocuser.TabIndex = 27;
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.button3.Location = new System.Drawing.Point(206, 47);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(47, 38);
            this.button3.TabIndex = 24;
            this.button3.Text = "All Out";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.button4.Location = new System.Drawing.Point(155, 47);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(45, 38);
            this.button4.TabIndex = 25;
            this.button4.Text = "Out";
            this.button4.UseVisualStyleBackColor = false;
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.button2.Location = new System.Drawing.Point(206, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(47, 38);
            this.button2.TabIndex = 23;
            this.button2.Text = "All In";
            this.button2.UseVisualStyleBackColor = false;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.button1.Location = new System.Drawing.Point(3, 54);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(70, 23);
            this.button1.TabIndex = 21;
            this.button1.Text = "Goto";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.Location = new System.Drawing.Point(79, 55);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(70, 21);
            this.textBox2.TabIndex = 22;
            // 
            // label1FocusCurrentValue
            // 
            this.label1FocusCurrentValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1FocusCurrentValue.AutoSize = true;
            this.label1FocusCurrentValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1FocusCurrentValue.Location = new System.Drawing.Point(79, 13);
            this.label1FocusCurrentValue.Name = "label1FocusCurrentValue";
            this.label1FocusCurrentValue.Size = new System.Drawing.Size(70, 18);
            this.label1FocusCurrentValue.TabIndex = 20;
            this.label1FocusCurrentValue.Text = "960";
            this.label1FocusCurrentValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label11.Location = new System.Drawing.Point(3, 15);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(70, 13);
            this.label11.TabIndex = 19;
            this.label11.Text = "Current:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button5
            // 
            this.button5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.button5.Location = new System.Drawing.Point(155, 3);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(45, 38);
            this.button5.TabIndex = 26;
            this.button5.Text = "In";
            this.button5.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.AutoSize = true;
            this.tableLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.tableLayoutPanelMain.ColumnCount = 3;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.Controls.Add(this.panelFocuser, 1, 1);
            this.tableLayoutPanelMain.Controls.Add(this.panelDome, 1, 0);
            this.tableLayoutPanelMain.Controls.Add(this.panelControls, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.panelDebug, 2, 0);
            this.tableLayoutPanelMain.Controls.Add(this.groupBoxWeather, 1, 2);
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 3;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(1167, 579);
            this.tableLayoutPanelMain.TabIndex = 12;
            // 
            // groupBoxWeather
            // 
            this.groupBoxWeather.Controls.Add(this.tableLayoutPanelWeather);
            this.groupBoxWeather.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxWeather.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxWeather.Location = new System.Drawing.Point(372, 414);
            this.groupBoxWeather.Name = "groupBoxWeather";
            this.groupBoxWeather.Size = new System.Drawing.Size(294, 162);
            this.groupBoxWeather.TabIndex = 15;
            this.groupBoxWeather.TabStop = false;
            this.groupBoxWeather.Text = " Weather ";
            this.groupBoxWeather.Visible = false;
            // 
            // tableLayoutPanelWeather
            // 
            this.tableLayoutPanelWeather.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelWeather.ColumnCount = 4;
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.55959F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.5932F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29.30832F));
            this.tableLayoutPanelWeather.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19.53888F));
            this.tableLayoutPanelWeather.Controls.Add(this.labelWindSpeedValue, 3, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.labelWindDirValue, 3, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.labelTempValue, 3, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.labelSkyTempValue, 3, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.labelRainRateValue, 3, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.label23, 0, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.label22, 2, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.label21, 2, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.label20, 2, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.label19, 2, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.label18, 2, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.label16, 0, 4);
            this.tableLayoutPanelWeather.Controls.Add(this.label14, 0, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.label13, 0, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.label12, 0, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.labelAgeValue, 1, 0);
            this.tableLayoutPanelWeather.Controls.Add(this.labelCloudCoverValue, 1, 1);
            this.tableLayoutPanelWeather.Controls.Add(this.labelDewPointValue, 1, 2);
            this.tableLayoutPanelWeather.Controls.Add(this.labelHumidityValue, 1, 3);
            this.tableLayoutPanelWeather.Controls.Add(this.labelPressureValue, 1, 4);
            this.tableLayoutPanelWeather.Location = new System.Drawing.Point(3, 18);
            this.tableLayoutPanelWeather.Name = "tableLayoutPanelWeather";
            this.tableLayoutPanelWeather.RowCount = 5;
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelWeather.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanelWeather.Size = new System.Drawing.Size(285, 135);
            this.tableLayoutPanelWeather.TabIndex = 0;
            // 
            // labelWindSpeedValue
            // 
            this.labelWindSpeedValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWindSpeedValue.AutoSize = true;
            this.labelWindSpeedValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWindSpeedValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelWindSpeedValue.Location = new System.Drawing.Point(231, 108);
            this.labelWindSpeedValue.Name = "labelWindSpeedValue";
            this.labelWindSpeedValue.Size = new System.Drawing.Size(51, 27);
            this.labelWindSpeedValue.TabIndex = 19;
            this.labelWindSpeedValue.Text = "windspeed";
            this.labelWindSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelWindDirValue
            // 
            this.labelWindDirValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWindDirValue.AutoSize = true;
            this.labelWindDirValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelWindDirValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelWindDirValue.Location = new System.Drawing.Point(231, 81);
            this.labelWindDirValue.Name = "labelWindDirValue";
            this.labelWindDirValue.Size = new System.Drawing.Size(51, 27);
            this.labelWindDirValue.TabIndex = 18;
            this.labelWindDirValue.Text = "winddir";
            this.labelWindDirValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelTempValue
            // 
            this.labelTempValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTempValue.AutoSize = true;
            this.labelTempValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelTempValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelTempValue.Location = new System.Drawing.Point(231, 54);
            this.labelTempValue.Name = "labelTempValue";
            this.labelTempValue.Size = new System.Drawing.Size(51, 27);
            this.labelTempValue.TabIndex = 17;
            this.labelTempValue.Text = "temp";
            this.labelTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelSkyTempValue
            // 
            this.labelSkyTempValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSkyTempValue.AutoSize = true;
            this.labelSkyTempValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelSkyTempValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelSkyTempValue.Location = new System.Drawing.Point(231, 27);
            this.labelSkyTempValue.Name = "labelSkyTempValue";
            this.labelSkyTempValue.Size = new System.Drawing.Size(51, 27);
            this.labelSkyTempValue.TabIndex = 16;
            this.labelSkyTempValue.Text = "skytemp";
            this.labelSkyTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRainRateValue
            // 
            this.labelRainRateValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRainRateValue.AutoSize = true;
            this.labelRainRateValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelRainRateValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelRainRateValue.Location = new System.Drawing.Point(231, 0);
            this.labelRainRateValue.Name = "labelRainRateValue";
            this.labelRainRateValue.Size = new System.Drawing.Size(51, 27);
            this.labelRainRateValue.TabIndex = 15;
            this.labelRainRateValue.Text = "rain";
            this.labelRainRateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label23
            // 
            this.label23.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label23.AutoSize = true;
            this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label23.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label23.Location = new System.Drawing.Point(42, 0);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(33, 27);
            this.label23.TabIndex = 9;
            this.label23.Text = "Age:";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label22
            // 
            this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label22.Location = new System.Drawing.Point(149, 108);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(76, 27);
            this.label22.TabIndex = 8;
            this.label22.Text = "WindSpeed:";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label21
            // 
            this.label21.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label21.AutoSize = true;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label21.Location = new System.Drawing.Point(169, 81);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(56, 27);
            this.label21.TabIndex = 7;
            this.label21.Text = "WindDir:";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label20
            // 
            this.label20.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label20.Location = new System.Drawing.Point(183, 54);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(42, 27);
            this.label20.TabIndex = 6;
            this.label20.Text = "Temp:";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label19.Location = new System.Drawing.Point(162, 27);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(63, 27);
            this.label19.TabIndex = 5;
            this.label19.Text = "SkyTemp:";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label18.Location = new System.Drawing.Point(161, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(64, 27);
            this.label18.TabIndex = 4;
            this.label18.Text = "RainRate:";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label16
            // 
            this.label16.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label16.Location = new System.Drawing.Point(15, 108);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(60, 27);
            this.label16.TabIndex = 3;
            this.label16.Text = "Pressure:";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label14.Location = new System.Drawing.Point(16, 81);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(59, 27);
            this.label14.TabIndex = 2;
            this.label14.Text = "Humidity:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label13.Location = new System.Drawing.Point(10, 54);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(65, 27);
            this.label13.TabIndex = 1;
            this.label13.Text = "DewPoint:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label12.Location = new System.Drawing.Point(3, 27);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(72, 27);
            this.label12.TabIndex = 0;
            this.label12.Text = "CloudCover:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelAgeValue
            // 
            this.labelAgeValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAgeValue.AutoSize = true;
            this.labelAgeValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelAgeValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelAgeValue.Location = new System.Drawing.Point(81, 0);
            this.labelAgeValue.Name = "labelAgeValue";
            this.labelAgeValue.Size = new System.Drawing.Size(61, 27);
            this.labelAgeValue.TabIndex = 10;
            this.labelAgeValue.Text = "age";
            this.labelAgeValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCloudCoverValue
            // 
            this.labelCloudCoverValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCloudCoverValue.AutoSize = true;
            this.labelCloudCoverValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelCloudCoverValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelCloudCoverValue.Location = new System.Drawing.Point(81, 27);
            this.labelCloudCoverValue.Name = "labelCloudCoverValue";
            this.labelCloudCoverValue.Size = new System.Drawing.Size(61, 27);
            this.labelCloudCoverValue.TabIndex = 11;
            this.labelCloudCoverValue.Text = "cloud";
            this.labelCloudCoverValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelDewPointValue
            // 
            this.labelDewPointValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDewPointValue.AutoSize = true;
            this.labelDewPointValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDewPointValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelDewPointValue.Location = new System.Drawing.Point(81, 54);
            this.labelDewPointValue.Name = "labelDewPointValue";
            this.labelDewPointValue.Size = new System.Drawing.Size(61, 27);
            this.labelDewPointValue.TabIndex = 12;
            this.labelDewPointValue.Text = "dew";
            this.labelDewPointValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelHumidityValue
            // 
            this.labelHumidityValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHumidityValue.AutoSize = true;
            this.labelHumidityValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHumidityValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelHumidityValue.Location = new System.Drawing.Point(81, 81);
            this.labelHumidityValue.Name = "labelHumidityValue";
            this.labelHumidityValue.Size = new System.Drawing.Size(61, 27);
            this.labelHumidityValue.TabIndex = 13;
            this.labelHumidityValue.Text = "humidity";
            this.labelHumidityValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelPressureValue
            // 
            this.labelPressureValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPressureValue.AutoSize = true;
            this.labelPressureValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelPressureValue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelPressureValue.Location = new System.Drawing.Point(81, 108);
            this.labelPressureValue.Name = "labelPressureValue";
            this.labelPressureValue.Size = new System.Drawing.Size(61, 27);
            this.labelPressureValue.TabIndex = 14;
            this.labelPressureValue.Text = "pressure";
            this.labelPressureValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelStatus
            // 
            this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelStatus.Location = new System.Drawing.Point(16, 200);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(328, 17);
            this.labelStatus.TabIndex = 24;
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timerStatus
            // 
            this.timerStatus.Tick += new System.EventHandler(this.timerStatus_Tick);
            // 
            // HandpadForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1167, 580);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Name = "HandpadForm";
            this.Text = "Wise40 Telescope Handpad";
            this.VisibleChanged += new System.EventHandler(this.HandpadForm_VisibleChanged);
            this.panelDebug.ResumeLayout(false);
            this.groupBoxMovementStudy.ResumeLayout(false);
            this.groupBoxMovementStudy.PerformLayout();
            this.groupBoxCurrentRates.ResumeLayout(false);
            this.tableLayoutPanelAxesState.ResumeLayout(false);
            this.tableLayoutPanelAxesState.PerformLayout();
            this.groupBoxEncoders.ResumeLayout(false);
            this.groupBoxEncoders.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStepCount)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panelControls.ResumeLayout(false);
            this.groupBox36.ResumeLayout(false);
            this.groupBox36.PerformLayout();
            this.panelDirectionButtons.ResumeLayout(false);
            this.groupBoxTelescope.ResumeLayout(false);
            this.groupBoxTelescope.PerformLayout();
            this.tableLayoutPanelCoordinates.ResumeLayout(false);
            this.tableLayoutPanelCoordinates.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBoxTracking.ResumeLayout(false);
            this.groupBoxTracking.PerformLayout();
            this.panelShowHideButtons.ResumeLayout(false);
            this.panelDome.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBoxDome.ResumeLayout(false);
            this.groupBoxDome.PerformLayout();
            this.groupBoxDomeGroup.ResumeLayout(false);
            this.groupBoxDomeGroup.PerformLayout();
            this.panelFocuser.ResumeLayout(false);
            this.groupBoxFocuser.ResumeLayout(false);
            this.tableLayoutPanelFocuser.ResumeLayout(false);
            this.tableLayoutPanelFocuser.PerformLayout();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.groupBoxWeather.ResumeLayout(false);
            this.tableLayoutPanelWeather.ResumeLayout(false);
            this.tableLayoutPanelWeather.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer displayRefreshTimer;
        private System.Windows.Forms.Panel panelDebug;
        private System.Windows.Forms.Panel panelControls;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox checkBoxSlewingIsActive;
        private System.Windows.Forms.CheckBox checkBoxSecondaryIsActive;
        private System.Windows.Forms.CheckBox checkBoxTrackingIsActive;
        private System.Windows.Forms.CheckBox checkBoxPrimaryIsActive;
        private System.Windows.Forms.Panel panelShowHideButtons;
        private System.Windows.Forms.Button buttonStudy;
        private System.Windows.Forms.Button buttonDome;
        private System.Windows.Forms.Button buttonFocuser;
        public System.Windows.Forms.Button buttonHardware;
        private System.Windows.Forms.GroupBox groupBox36;
        private System.Windows.Forms.RadioButton radioButtonSlew;
        private System.Windows.Forms.RadioButton radioButtonGuide;
        private System.Windows.Forms.RadioButton radioButtonSet;
        private System.Windows.Forms.Panel panelDirectionButtons;
        private System.Windows.Forms.Button buttonNW;
        private System.Windows.Forms.Button buttonSW;
        private System.Windows.Forms.Button buttonSE;
        private System.Windows.Forms.Button buttonNE;
        private System.Windows.Forms.Button buttonNorth;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonSouth;
        private System.Windows.Forms.Button buttonEast;
        private System.Windows.Forms.Button buttonWest;
        private System.Windows.Forms.Label labelDate;
        private System.Windows.Forms.CheckBox checkBoxTrack;
        private System.Windows.Forms.GroupBox groupBoxTelescope;
        private System.Windows.Forms.Panel panelDome;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label labelDomeShutterStatusValue;
        private System.Windows.Forms.GroupBox groupBoxDome;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label labelDomeStatusValue;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label labelDomeAzimuthValue;
        private System.Windows.Forms.GroupBox groupBoxDomeGroup;
        private System.Windows.Forms.Panel panelFocuser;
        private System.Windows.Forms.GroupBox groupBoxFocuser;
        private System.Windows.Forms.GroupBox groupBoxMovementStudy;
        private System.Windows.Forms.Button buttonGoCoord;
        private System.Windows.Forms.TextBox textBoxDec;
        private System.Windows.Forms.TextBox textBoxRA;
        private System.Windows.Forms.Label labelDec;
        private System.Windows.Forms.Label labelRA;
        private System.Windows.Forms.GroupBox groupBoxEncoders;
        private System.Windows.Forms.Label wormValue;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label axisValue;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelDecEnc;
        private System.Windows.Forms.Label labelDecEncValue;
        private System.Windows.Forms.Label labelHAEnc;
        private System.Windows.Forms.Label labelHAEncValue;
        private System.Windows.Forms.Button buttonSaveResults;
        private System.Windows.Forms.Button buttonStopStudy;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxMillis;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDownStepCount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TextBoxLog;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton radioButtonAxisDec;
        private System.Windows.Forms.RadioButton radioButtonAxisHA;
        private System.Windows.Forms.Button buttonGoStudy;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.RadioButton radioButtonDirDown;
        private System.Windows.Forms.RadioButton radioButtonDirUp;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonSpeedGuide;
        private System.Windows.Forms.RadioButton radioButtonSpeedSet;
        private System.Windows.Forms.RadioButton radioButtonSpeedSlew;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.Button buttonCloseShutter;
        private System.Windows.Forms.Button buttonOpenShutter;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelFocuser;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label1FocusCurrentValue;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label labelConfDomeSlaved;
        private System.Windows.Forms.Label labelDomeSlavedConfValue;
        private System.Windows.Forms.GroupBox groupBoxTracking;
        private System.Windows.Forms.CheckBox checkBoxEnslaveDome;
        private System.Windows.Forms.Button buttonWeather;
        private System.Windows.Forms.GroupBox groupBoxWeather;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelWeather;
        private System.Windows.Forms.Label labelWindSpeedValue;
        private System.Windows.Forms.Label labelWindDirValue;
        private System.Windows.Forms.Label labelTempValue;
        private System.Windows.Forms.Label labelSkyTempValue;
        private System.Windows.Forms.Label labelRainRateValue;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label labelAgeValue;
        private System.Windows.Forms.Label labelCloudCoverValue;
        private System.Windows.Forms.Label labelDewPointValue;
        private System.Windows.Forms.Label labelHumidityValue;
        private System.Windows.Forms.Label labelPressureValue;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelCoordinates;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelLTValue;
        private System.Windows.Forms.Label labelAltitude;
        private System.Windows.Forms.Label labelAltitudeValue;
        private System.Windows.Forms.Label labelRightAscension;
        private System.Windows.Forms.Label labelUTValue;
        private System.Windows.Forms.Label labelHourAngle;
        private System.Windows.Forms.Label labelSiderealValue;
        private System.Windows.Forms.Label labelRightAscensionValue;
        private System.Windows.Forms.Label labelUT;
        private System.Windows.Forms.Label labelLT;
        private System.Windows.Forms.Label labelHourAngleValue;
        private System.Windows.Forms.Label labelAzimuth;
        private System.Windows.Forms.Label labelDeclinationValue;
        private System.Windows.Forms.Label labelAzimuthValue;
        private System.Windows.Forms.Label labelDeclination;
        private System.Windows.Forms.GroupBox groupBoxCurrentRates;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelAxesState;
        private System.Windows.Forms.Label labelCurrPrimRateValue;
        private System.Windows.Forms.Label labelCurrPrimDirValue;
        private System.Windows.Forms.Label labelCurrSecRateValue;
        private System.Windows.Forms.Label labelCurrSecDirValue;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Timer timerStatus;
    }
}