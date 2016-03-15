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
            this.buttonNorth = new System.Windows.Forms.Button();
            this.buttonWest = new System.Windows.Forms.Button();
            this.buttonSouth = new System.Windows.Forms.Button();
            this.buttonEast = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.radioButtonGuide = new System.Windows.Forms.RadioButton();
            this.radioButtonSet = new System.Windows.Forms.RadioButton();
            this.radioButtonSlew = new System.Windows.Forms.RadioButton();
            this.checkBoxTrack = new System.Windows.Forms.CheckBox();
            this.labelSiderealValue = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.labelDate = new System.Windows.Forms.Label();
            this.labelLTValue = new System.Windows.Forms.Label();
            this.labelLT = new System.Windows.Forms.Label();
            this.labelUTValue = new System.Windows.Forms.Label();
            this.labelUT = new System.Windows.Forms.Label();
            this.labelAzimuthValue = new System.Windows.Forms.Label();
            this.labelAltitudeValue = new System.Windows.Forms.Label();
            this.labelDeclinationValue = new System.Windows.Forms.Label();
            this.labelAzimuth = new System.Windows.Forms.Label();
            this.labelAltitude = new System.Windows.Forms.Label();
            this.labelDeclination = new System.Windows.Forms.Label();
            this.labelRightAscensionValue = new System.Windows.Forms.Label();
            this.labelRightAscension = new System.Windows.Forms.Label();
            this.labelHourAngleValue = new System.Windows.Forms.Label();
            this.labelHourAngle = new System.Windows.Forms.Label();
            this.buttonHardware = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panelFocuser = new System.Windows.Forms.Panel();
            this.panelDome = new System.Windows.Forms.Panel();
            this.panelControls = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonHandpad = new System.Windows.Forms.Button();
            this.buttonDome = new System.Windows.Forms.Button();
            this.buttonFocuser = new System.Windows.Forms.Button();
            this.groupBox36 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel10 = new System.Windows.Forms.Panel();
            this.buttonNW = new System.Windows.Forms.Button();
            this.buttonSW = new System.Windows.Forms.Button();
            this.buttonSE = new System.Windows.Forms.Button();
            this.buttonNE = new System.Windows.Forms.Button();
            this.timerDisplayRefresh = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panelControls.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBox36.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel10.SuspendLayout();
            this.SuspendLayout();
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
            // buttonWest
            // 
            this.buttonWest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonWest.FlatAppearance.BorderSize = 0;
            this.buttonWest.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonWest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonWest.Location = new System.Drawing.Point(13, 56);
            this.buttonWest.Name = "buttonWest";
            this.buttonWest.Size = new System.Drawing.Size(40, 40);
            this.buttonWest.TabIndex = 1;
            this.buttonWest.Text = "W";
            this.buttonWest.UseVisualStyleBackColor = false;
            this.buttonWest.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonWest.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
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
            this.buttonEast.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonEast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonEast.Location = new System.Drawing.Point(105, 56);
            this.buttonEast.Name = "buttonEast";
            this.buttonEast.Size = new System.Drawing.Size(40, 40);
            this.buttonEast.TabIndex = 3;
            this.buttonEast.Text = "E";
            this.buttonEast.UseVisualStyleBackColor = false;
            this.buttonEast.MouseDown += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseDown);
            this.buttonEast.MouseUp += new System.Windows.Forms.MouseEventHandler(this.directionButton_MouseUp);
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
            // radioButtonGuide
            // 
            this.radioButtonGuide.AutoSize = true;
            this.radioButtonGuide.Checked = true;
            this.radioButtonGuide.Location = new System.Drawing.Point(15, 19);
            this.radioButtonGuide.Name = "radioButtonGuide";
            this.radioButtonGuide.Size = new System.Drawing.Size(53, 17);
            this.radioButtonGuide.TabIndex = 2;
            this.radioButtonGuide.TabStop = true;
            this.radioButtonGuide.Text = "Guide";
            this.radioButtonGuide.UseVisualStyleBackColor = true;
            this.radioButtonGuide.Click += new System.EventHandler(this.radioButtonGuide_Click);
            // 
            // radioButtonSet
            // 
            this.radioButtonSet.AutoSize = true;
            this.radioButtonSet.Location = new System.Drawing.Point(15, 36);
            this.radioButtonSet.Name = "radioButtonSet";
            this.radioButtonSet.Size = new System.Drawing.Size(41, 17);
            this.radioButtonSet.TabIndex = 1;
            this.radioButtonSet.Text = "Set";
            this.radioButtonSet.UseVisualStyleBackColor = true;
            this.radioButtonSet.Click += new System.EventHandler(this.radioButtonSet_Click);
            // 
            // radioButtonSlew
            // 
            this.radioButtonSlew.AutoSize = true;
            this.radioButtonSlew.Location = new System.Drawing.Point(15, 53);
            this.radioButtonSlew.Name = "radioButtonSlew";
            this.radioButtonSlew.Size = new System.Drawing.Size(48, 17);
            this.radioButtonSlew.TabIndex = 0;
            this.radioButtonSlew.Text = "Slew";
            this.radioButtonSlew.UseVisualStyleBackColor = true;
            this.radioButtonSlew.Click += new System.EventHandler(this.radioButtonSlew_Click);
            // 
            // checkBoxTrack
            // 
            this.checkBoxTrack.AutoSize = true;
            this.checkBoxTrack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.checkBoxTrack.Location = new System.Drawing.Point(94, 492);
            this.checkBoxTrack.Name = "checkBoxTrack";
            this.checkBoxTrack.Size = new System.Drawing.Size(54, 17);
            this.checkBoxTrack.TabIndex = 6;
            this.checkBoxTrack.Text = "Track";
            this.checkBoxTrack.UseVisualStyleBackColor = true;
            this.checkBoxTrack.CheckedChanged += new System.EventHandler(this.checkBoxTrack_CheckedChanged);
            // 
            // labelSiderealValue
            // 
            this.labelSiderealValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelSiderealValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSiderealValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelSiderealValue.Location = new System.Drawing.Point(240, 64);
            this.labelSiderealValue.Name = "labelSiderealValue";
            this.labelSiderealValue.Size = new System.Drawing.Size(125, 20);
            this.labelSiderealValue.TabIndex = 8;
            this.labelSiderealValue.Text = " 00:00.00";
            this.labelSiderealValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.labelDate);
            this.panel1.Controls.Add(this.labelLTValue);
            this.panel1.Controls.Add(this.labelLT);
            this.panel1.Controls.Add(this.labelUTValue);
            this.panel1.Controls.Add(this.labelUT);
            this.panel1.Controls.Add(this.labelAzimuthValue);
            this.panel1.Controls.Add(this.labelAltitudeValue);
            this.panel1.Controls.Add(this.labelDeclinationValue);
            this.panel1.Controls.Add(this.labelAzimuth);
            this.panel1.Controls.Add(this.labelAltitude);
            this.panel1.Controls.Add(this.labelDeclination);
            this.panel1.Controls.Add(this.labelRightAscensionValue);
            this.panel1.Controls.Add(this.labelRightAscension);
            this.panel1.Controls.Add(this.labelHourAngleValue);
            this.panel1.Controls.Add(this.labelHourAngle);
            this.panel1.Controls.Add(this.labelSiderealValue);
            this.panel1.Location = new System.Drawing.Point(6, 80);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(368, 133);
            this.panel1.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label2.Location = new System.Drawing.Point(185, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 20);
            this.label2.TabIndex = 24;
            this.label2.Text = "LST:";
            // 
            // labelDate
            // 
            this.labelDate.AutoSize = true;
            this.labelDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelDate.Location = new System.Drawing.Point(74, 10);
            this.labelDate.Name = "labelDate";
            this.labelDate.Size = new System.Drawing.Size(152, 17);
            this.labelDate.TabIndex = 23;
            this.labelDate.Text = "Feb 16, 2016 12:51:00";
            // 
            // labelLTValue
            // 
            this.labelLTValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelLTValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLTValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelLTValue.Location = new System.Drawing.Point(245, 43);
            this.labelLTValue.Name = "labelLTValue";
            this.labelLTValue.Size = new System.Drawing.Size(108, 20);
            this.labelLTValue.TabIndex = 22;
            this.labelLTValue.Text = "00:00.00";
            this.labelLTValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelLT
            // 
            this.labelLT.AutoSize = true;
            this.labelLT.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLT.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelLT.Location = new System.Drawing.Point(197, 43);
            this.labelLT.Name = "labelLT";
            this.labelLT.Size = new System.Drawing.Size(34, 20);
            this.labelLT.TabIndex = 21;
            this.labelLT.Text = "LT:";
            // 
            // labelUTValue
            // 
            this.labelUTValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelUTValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUTValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelUTValue.Location = new System.Drawing.Point(45, 43);
            this.labelUTValue.Name = "labelUTValue";
            this.labelUTValue.Size = new System.Drawing.Size(134, 20);
            this.labelUTValue.TabIndex = 20;
            this.labelUTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelUT
            // 
            this.labelUT.AutoSize = true;
            this.labelUT.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUT.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelUT.Location = new System.Drawing.Point(9, 43);
            this.labelUT.Name = "labelUT";
            this.labelUT.Size = new System.Drawing.Size(37, 20);
            this.labelUT.TabIndex = 19;
            this.labelUT.Text = "UT:";
            // 
            // labelAzimuthValue
            // 
            this.labelAzimuthValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelAzimuthValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAzimuthValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelAzimuthValue.Location = new System.Drawing.Point(233, 103);
            this.labelAzimuthValue.Name = "labelAzimuthValue";
            this.labelAzimuthValue.Size = new System.Drawing.Size(128, 20);
            this.labelAzimuthValue.TabIndex = 18;
            this.labelAzimuthValue.Text = "123\'12\'12.3\"";
            this.labelAzimuthValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelAltitudeValue
            // 
            this.labelAltitudeValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelAltitudeValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAltitudeValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelAltitudeValue.Location = new System.Drawing.Point(45, 104);
            this.labelAltitudeValue.Name = "labelAltitudeValue";
            this.labelAltitudeValue.Size = new System.Drawing.Size(134, 20);
            this.labelAltitudeValue.TabIndex = 17;
            this.labelAltitudeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDeclinationValue
            // 
            this.labelDeclinationValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDeclinationValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeclinationValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelDeclinationValue.Location = new System.Drawing.Point(239, 83);
            this.labelDeclinationValue.Name = "labelDeclinationValue";
            this.labelDeclinationValue.Size = new System.Drawing.Size(114, 20);
            this.labelDeclinationValue.TabIndex = 16;
            this.labelDeclinationValue.Text = " 00\'00\'00.0\"";
            this.labelDeclinationValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelAzimuth
            // 
            this.labelAzimuth.AutoSize = true;
            this.labelAzimuth.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAzimuth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelAzimuth.Location = new System.Drawing.Point(189, 103);
            this.labelAzimuth.Name = "labelAzimuth";
            this.labelAzimuth.Size = new System.Drawing.Size(42, 20);
            this.labelAzimuth.TabIndex = 15;
            this.labelAzimuth.Text = " AZ:";
            // 
            // labelAltitude
            // 
            this.labelAltitude.AutoSize = true;
            this.labelAltitude.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAltitude.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelAltitude.Location = new System.Drawing.Point(0, 103);
            this.labelAltitude.Name = "labelAltitude";
            this.labelAltitude.Size = new System.Drawing.Size(46, 20);
            this.labelAltitude.TabIndex = 14;
            this.labelAltitude.Text = "ALT:";
            // 
            // labelDeclination
            // 
            this.labelDeclination.AutoSize = true;
            this.labelDeclination.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDeclination.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeclination.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelDeclination.Location = new System.Drawing.Point(180, 83);
            this.labelDeclination.Name = "labelDeclination";
            this.labelDeclination.Size = new System.Drawing.Size(51, 20);
            this.labelDeclination.TabIndex = 13;
            this.labelDeclination.Text = "DEC:";
            // 
            // labelRightAscensionValue
            // 
            this.labelRightAscensionValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelRightAscensionValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRightAscensionValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelRightAscensionValue.Location = new System.Drawing.Point(45, 85);
            this.labelRightAscensionValue.Name = "labelRightAscensionValue";
            this.labelRightAscensionValue.Size = new System.Drawing.Size(134, 20);
            this.labelRightAscensionValue.TabIndex = 12;
            this.labelRightAscensionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelRightAscension
            // 
            this.labelRightAscension.AutoSize = true;
            this.labelRightAscension.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRightAscension.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelRightAscension.Location = new System.Drawing.Point(7, 83);
            this.labelRightAscension.Name = "labelRightAscension";
            this.labelRightAscension.Size = new System.Drawing.Size(39, 20);
            this.labelRightAscension.TabIndex = 11;
            this.labelRightAscension.Text = "RA:";
            // 
            // labelHourAngleValue
            // 
            this.labelHourAngleValue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelHourAngleValue.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHourAngleValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelHourAngleValue.Location = new System.Drawing.Point(45, 65);
            this.labelHourAngleValue.Name = "labelHourAngleValue";
            this.labelHourAngleValue.Size = new System.Drawing.Size(134, 20);
            this.labelHourAngleValue.TabIndex = 10;
            this.labelHourAngleValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelHourAngle
            // 
            this.labelHourAngle.AutoSize = true;
            this.labelHourAngle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHourAngle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.labelHourAngle.Location = new System.Drawing.Point(7, 63);
            this.labelHourAngle.Name = "labelHourAngle";
            this.labelHourAngle.Size = new System.Drawing.Size(39, 20);
            this.labelHourAngle.TabIndex = 9;
            this.labelHourAngle.Text = "HA:";
            // 
            // buttonHardware
            // 
            this.buttonHardware.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonHardware.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonHardware.Location = new System.Drawing.Point(4, 88);
            this.buttonHardware.Name = "buttonHardware";
            this.buttonHardware.Size = new System.Drawing.Size(93, 23);
            this.buttonHardware.TabIndex = 10;
            this.buttonHardware.Text = "Show Hardware";
            this.buttonHardware.UseVisualStyleBackColor = false;
            this.buttonHardware.Click += new System.EventHandler(this.buttonHardware_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.groupBox1.Controls.Add(this.panelFocuser);
            this.groupBox1.Controls.Add(this.panelDome);
            this.groupBox1.Controls.Add(this.panelControls);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(700, 585);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            // 
            // panelFocuser
            // 
            this.panelFocuser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelFocuser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.panelFocuser.Location = new System.Drawing.Point(400, 297);
            this.panelFocuser.Name = "panelFocuser";
            this.panelFocuser.Size = new System.Drawing.Size(294, 216);
            this.panelFocuser.TabIndex = 14;
            this.panelFocuser.Visible = false;
            // 
            // panelDome
            // 
            this.panelDome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelDome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.panelDome.Location = new System.Drawing.Point(400, 12);
            this.panelDome.Name = "panelDome";
            this.panelDome.Size = new System.Drawing.Size(294, 279);
            this.panelDome.TabIndex = 13;
            this.panelDome.Visible = false;
            // 
            // panelControls
            // 
            this.panelControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelControls.Controls.Add(this.label1);
            this.panelControls.Controls.Add(this.panel2);
            this.panelControls.Controls.Add(this.groupBox36);
            this.panelControls.Controls.Add(this.pictureBox1);
            this.panelControls.Controls.Add(this.panel10);
            this.panelControls.Controls.Add(this.panel1);
            this.panelControls.Controls.Add(this.checkBoxTrack);
            this.panelControls.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.panelControls.Location = new System.Drawing.Point(12, 12);
            this.panelControls.Name = "panelControls";
            this.panelControls.Size = new System.Drawing.Size(382, 554);
            this.panelControls.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label1.Location = new System.Drawing.Point(15, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(227, 20);
            this.label1.TabIndex = 16;
            this.label1.Text = "Wise40 Telescope Driver (v1.2)";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonHandpad);
            this.panel2.Controls.Add(this.buttonDome);
            this.panel2.Controls.Add(this.buttonFocuser);
            this.panel2.Controls.Add(this.buttonHardware);
            this.panel2.Location = new System.Drawing.Point(173, 404);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(100, 119);
            this.panel2.TabIndex = 15;
            // 
            // buttonHandpad
            // 
            this.buttonHandpad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonHandpad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonHandpad.Location = new System.Drawing.Point(4, 4);
            this.buttonHandpad.Name = "buttonHandpad";
            this.buttonHandpad.Size = new System.Drawing.Size(93, 23);
            this.buttonHandpad.TabIndex = 15;
            this.buttonHandpad.Text = "Hide Hanpad";
            this.buttonHandpad.UseVisualStyleBackColor = false;
            this.buttonHandpad.Click += new System.EventHandler(this.buttonHandpad_Click);
            // 
            // buttonDome
            // 
            this.buttonDome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonDome.Location = new System.Drawing.Point(3, 32);
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
            this.buttonFocuser.Location = new System.Drawing.Point(4, 60);
            this.buttonFocuser.Name = "buttonFocuser";
            this.buttonFocuser.Size = new System.Drawing.Size(93, 23);
            this.buttonFocuser.TabIndex = 14;
            this.buttonFocuser.Text = "Show Focuser";
            this.buttonFocuser.UseVisualStyleBackColor = false;
            this.buttonFocuser.Click += new System.EventHandler(this.buttonFocuser_Click);
            // 
            // groupBox36
            // 
            this.groupBox36.Controls.Add(this.radioButtonSlew);
            this.groupBox36.Controls.Add(this.radioButtonGuide);
            this.groupBox36.Controls.Add(this.radioButtonSet);
            this.groupBox36.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.groupBox36.Location = new System.Drawing.Point(80, 404);
            this.groupBox36.Name = "groupBox36";
            this.groupBox36.Size = new System.Drawing.Size(74, 77);
            this.groupBox36.TabIndex = 12;
            this.groupBox36.TabStop = false;
            this.groupBox36.Text = " Speed ";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::ASCOM.Wise40.Properties.Resources.ASCOM;
            this.pictureBox1.InitialImage = global::ASCOM.Wise40.Properties.Resources.ASCOM;
            this.pictureBox1.Location = new System.Drawing.Point(263, 21);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(35, 44);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // panel10
            // 
            this.panel10.Controls.Add(this.buttonNW);
            this.panel10.Controls.Add(this.buttonSW);
            this.panel10.Controls.Add(this.buttonSE);
            this.panel10.Controls.Add(this.buttonNE);
            this.panel10.Controls.Add(this.buttonNorth);
            this.panel10.Controls.Add(this.buttonStop);
            this.panel10.Controls.Add(this.buttonSouth);
            this.panel10.Controls.Add(this.buttonWest);
            this.panel10.Controls.Add(this.buttonEast);
            this.panel10.Location = new System.Drawing.Point(99, 229);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(160, 152);
            this.panel10.TabIndex = 11;
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
            // timerDisplayRefresh
            // 
            this.timerDisplayRefresh.Tick += new System.EventHandler(this.timerDisplayRefresh_Tick);
            // 
            // HandpadForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1025, 933);
            this.Controls.Add(this.groupBox1);
            this.Name = "HandpadForm";
            this.Text = "Wise40 Telescope Handpad";
            this.Load += new System.EventHandler(this.HandpadForm_Load);
            this.VisibleChanged += new System.EventHandler(this.HandpadForm_VisibleChanged);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.panelControls.ResumeLayout(false);
            this.panelControls.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.groupBox36.ResumeLayout(false);
            this.groupBox36.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel10.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonNorth;
        private System.Windows.Forms.Button buttonWest;
        private System.Windows.Forms.Button buttonSouth;
        private System.Windows.Forms.Button buttonEast;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.RadioButton radioButtonGuide;
        private System.Windows.Forms.RadioButton radioButtonSet;
        private System.Windows.Forms.RadioButton radioButtonSlew;
        private System.Windows.Forms.CheckBox checkBoxTrack;
        private System.Windows.Forms.Label labelSiderealValue;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelRightAscensionValue;
        private System.Windows.Forms.Label labelRightAscension;
        private System.Windows.Forms.Label labelHourAngleValue;
        private System.Windows.Forms.Label labelHourAngle;
        public System.Windows.Forms.Button buttonHardware;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel panelControls;
        private System.Windows.Forms.Label labelAzimuthValue;
        private System.Windows.Forms.Label labelAltitudeValue;
        private System.Windows.Forms.Label labelDeclinationValue;
        private System.Windows.Forms.Label labelAzimuth;
        private System.Windows.Forms.Label labelAltitude;
        private System.Windows.Forms.Label labelDeclination;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.GroupBox groupBox36;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button buttonFocuser;
        private System.Windows.Forms.Button buttonDome;
        private System.Windows.Forms.Panel panelFocuser;
        private System.Windows.Forms.Panel panelDome;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Timer timerDisplayRefresh;
        private System.Windows.Forms.Label labelDate;
        private System.Windows.Forms.Label labelLTValue;
        private System.Windows.Forms.Label labelLT;
        private System.Windows.Forms.Label labelUTValue;
        private System.Windows.Forms.Label labelUT;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonNW;
        private System.Windows.Forms.Button buttonSW;
        private System.Windows.Forms.Button buttonSE;
        private System.Windows.Forms.Button buttonNE;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonHandpad;
    }
}