namespace FocuserApplication
{
    partial class FormFocus
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
            this.buttonFocusIncrease = new System.Windows.Forms.Button();
            this.buttonFocusDecrease = new System.Windows.Forms.Button();
            this.buttonFocusGoto = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxFocusGotoPosition = new System.Windows.Forms.TextBox();
            this.buttonFocusDown = new System.Windows.Forms.Button();
            this.labelFocusCurrentValue = new System.Windows.Forms.Label();
            this.buttonFocuserStop = new System.Windows.Forms.Button();
            this.buttonFocusAllDown = new System.Windows.Forms.Button();
            this.buttonFocusUp = new System.Windows.Forms.Button();
            this.buttonFocusAllUp = new System.Windows.Forms.Button();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.comboBoxFocusStep = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxPID_P = new System.Windows.Forms.TextBox();
            this.textBoxPID_I = new System.Windows.Forms.TextBox();
            this.textBoxPID_D = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonSetPID = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonFocusIncrease
            // 
            this.buttonFocusIncrease.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusIncrease.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFocusIncrease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusIncrease.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusIncrease.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusIncrease.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusIncrease.Location = new System.Drawing.Point(135, 68);
            this.buttonFocusIncrease.Name = "buttonFocusIncrease";
            this.buttonFocusIncrease.Size = new System.Drawing.Size(33, 21);
            this.buttonFocusIncrease.TabIndex = 32;
            this.buttonFocusIncrease.Text = "+";
            this.buttonFocusIncrease.UseVisualStyleBackColor = false;
            this.buttonFocusIncrease.Click += new System.EventHandler(this.buttonFocusIncrease_Click);
            // 
            // buttonFocusDecrease
            // 
            this.buttonFocusDecrease.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusDecrease.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFocusDecrease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusDecrease.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusDecrease.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusDecrease.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusDecrease.Location = new System.Drawing.Point(8, 68);
            this.buttonFocusDecrease.Name = "buttonFocusDecrease";
            this.buttonFocusDecrease.Size = new System.Drawing.Size(33, 21);
            this.buttonFocusDecrease.TabIndex = 31;
            this.buttonFocusDecrease.Text = "-";
            this.buttonFocusDecrease.UseVisualStyleBackColor = false;
            this.buttonFocusDecrease.Click += new System.EventHandler(this.buttonFocusDecrease_Click);
            // 
            // buttonFocusGoto
            // 
            this.buttonFocusGoto.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusGoto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusGoto.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusGoto.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusGoto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusGoto.Location = new System.Drawing.Point(8, 34);
            this.buttonFocusGoto.Name = "buttonFocusGoto";
            this.buttonFocusGoto.Size = new System.Drawing.Size(80, 21);
            this.buttonFocusGoto.TabIndex = 21;
            this.buttonFocusGoto.Text = "Go";
            this.buttonFocusGoto.UseVisualStyleBackColor = false;
            this.buttonFocusGoto.Click += new System.EventHandler(this.buttonFocusGoto_Click);
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.label11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label11.Location = new System.Drawing.Point(8, 5);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(81, 18);
            this.label11.TabIndex = 19;
            this.label11.Text = "Position:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxFocusGotoPosition
            // 
            this.textBoxFocusGotoPosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFocusGotoPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxFocusGotoPosition.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFocusGotoPosition.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFocusGotoPosition.Location = new System.Drawing.Point(105, 34);
            this.textBoxFocusGotoPosition.Name = "textBoxFocusGotoPosition";
            this.textBoxFocusGotoPosition.Size = new System.Drawing.Size(63, 21);
            this.textBoxFocusGotoPosition.TabIndex = 22;
            this.textBoxFocusGotoPosition.Validated += new System.EventHandler(this.textBoxFocusGotoPosition_Validated);
            // 
            // buttonFocusDown
            // 
            this.buttonFocusDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusDown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusDown.Location = new System.Drawing.Point(184, 49);
            this.buttonFocusDown.Name = "buttonFocusDown";
            this.buttonFocusDown.Size = new System.Drawing.Size(49, 40);
            this.buttonFocusDown.TabIndex = 25;
            this.buttonFocusDown.Text = "Down";
            this.buttonFocusDown.UseVisualStyleBackColor = false;
            this.buttonFocusDown.MouseDown += new System.Windows.Forms.MouseEventHandler(this.buttonFocusDown_MouseDown);
            this.buttonFocusDown.MouseUp += new System.Windows.Forms.MouseEventHandler(this.focuserHalt);
            // 
            // labelFocusCurrentValue
            // 
            this.labelFocusCurrentValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFocusCurrentValue.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F);
            this.labelFocusCurrentValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.labelFocusCurrentValue.Location = new System.Drawing.Point(104, 5);
            this.labelFocusCurrentValue.Name = "labelFocusCurrentValue";
            this.labelFocusCurrentValue.Size = new System.Drawing.Size(64, 18);
            this.labelFocusCurrentValue.TabIndex = 20;
            this.labelFocusCurrentValue.Text = "960";
            this.labelFocusCurrentValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonFocuserStop
            // 
            this.buttonFocuserStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocuserStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocuserStop.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocuserStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocuserStop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocuserStop.Location = new System.Drawing.Point(296, 5);
            this.buttonFocuserStop.Name = "buttonFocuserStop";
            this.buttonFocuserStop.Size = new System.Drawing.Size(49, 84);
            this.buttonFocuserStop.TabIndex = 28;
            this.buttonFocuserStop.Text = "Stop";
            this.buttonFocuserStop.UseVisualStyleBackColor = false;
            this.buttonFocuserStop.Click += new System.EventHandler(this.buttonFocuserStop_Click);
            // 
            // buttonFocusAllDown
            // 
            this.buttonFocusAllDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusAllDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusAllDown.Enabled = false;
            this.buttonFocusAllDown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusAllDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusAllDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusAllDown.Location = new System.Drawing.Point(240, 49);
            this.buttonFocusAllDown.Name = "buttonFocusAllDown";
            this.buttonFocusAllDown.Size = new System.Drawing.Size(49, 40);
            this.buttonFocusAllDown.TabIndex = 24;
            this.buttonFocusAllDown.Text = "All\r\nDown";
            this.buttonFocusAllDown.UseVisualStyleBackColor = false;
            this.buttonFocusAllDown.Click += new System.EventHandler(this.buttonFocusAllDown_Click);
            // 
            // buttonFocusUp
            // 
            this.buttonFocusUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusUp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusUp.Location = new System.Drawing.Point(184, 5);
            this.buttonFocusUp.Name = "buttonFocusUp";
            this.buttonFocusUp.Size = new System.Drawing.Size(49, 40);
            this.buttonFocusUp.TabIndex = 26;
            this.buttonFocusUp.Text = "Up";
            this.buttonFocusUp.UseVisualStyleBackColor = false;
            this.buttonFocusUp.MouseDown += new System.Windows.Forms.MouseEventHandler(this.buttonFocusUp_MouseDown);
            this.buttonFocusUp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.focuserHalt);
            // 
            // buttonFocusAllUp
            // 
            this.buttonFocusAllUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFocusAllUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonFocusAllUp.Enabled = false;
            this.buttonFocusAllUp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonFocusAllUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFocusAllUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonFocusAllUp.Location = new System.Drawing.Point(240, 5);
            this.buttonFocusAllUp.Name = "buttonFocusAllUp";
            this.buttonFocusAllUp.Size = new System.Drawing.Size(49, 40);
            this.buttonFocusAllUp.TabIndex = 23;
            this.buttonFocusAllUp.Text = "All\r\nUp";
            this.buttonFocusAllUp.UseVisualStyleBackColor = false;
            this.buttonFocusAllUp.Click += new System.EventHandler(this.buttonFocusAllUp_Click);
            // 
            // timerRefresh
            // 
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // comboBoxFocusStep
            // 
            this.comboBoxFocusStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.comboBoxFocusStep.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxFocusStep.FormattingEnabled = true;
            this.comboBoxFocusStep.Items.AddRange(new object[] {
            "50",
            "100",
            "200",
            ""});
            this.comboBoxFocusStep.Location = new System.Drawing.Point(48, 68);
            this.comboBoxFocusStep.Name = "comboBoxFocusStep";
            this.comboBoxFocusStep.Size = new System.Drawing.Size(82, 21);
            this.comboBoxFocusStep.TabIndex = 35;
            this.comboBoxFocusStep.Text = "50";
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.comboBoxFocusStep);
            this.panel1.Controls.Add(this.buttonFocusDecrease);
            this.panel1.Controls.Add(this.buttonFocusUp);
            this.panel1.Controls.Add(this.textBoxFocusGotoPosition);
            this.panel1.Controls.Add(this.buttonFocusDown);
            this.panel1.Controls.Add(this.buttonFocusAllDown);
            this.panel1.Controls.Add(this.labelFocusCurrentValue);
            this.panel1.Controls.Add(this.buttonFocusAllUp);
            this.panel1.Controls.Add(this.buttonFocusGoto);
            this.panel1.Controls.Add(this.buttonFocuserStop);
            this.panel1.Controls.Add(this.buttonFocusIncrease);
            this.panel1.Location = new System.Drawing.Point(8, 8);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(392, 92);
            this.panel1.TabIndex = 36;
            // 
            // labelStatus
            // 
            this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStatus.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, System.Drawing.FontStyle.Italic);
            this.labelStatus.Location = new System.Drawing.Point(8, 141);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(352, 29);
            this.labelStatus.TabIndex = 37;
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip1.SetToolTip(this.labelStatus, "Focuse status");
            // 
            // textBoxPID_P
            // 
            this.textBoxPID_P.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPID_P.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxPID_P.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPID_P.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPID_P.Location = new System.Drawing.Point(40, 109);
            this.textBoxPID_P.Name = "textBoxPID_P";
            this.textBoxPID_P.Size = new System.Drawing.Size(40, 21);
            this.textBoxPID_P.TabIndex = 38;
            // 
            // textBoxPID_I
            // 
            this.textBoxPID_I.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPID_I.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxPID_I.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPID_I.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPID_I.Location = new System.Drawing.Point(120, 109);
            this.textBoxPID_I.Name = "textBoxPID_I";
            this.textBoxPID_I.Size = new System.Drawing.Size(40, 21);
            this.textBoxPID_I.TabIndex = 39;
            // 
            // textBoxPID_D
            // 
            this.textBoxPID_D.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPID_D.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxPID_D.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPID_D.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPID_D.Location = new System.Drawing.Point(200, 109);
            this.textBoxPID_D.Name = "textBoxPID_D";
            this.textBoxPID_D.Size = new System.Drawing.Size(40, 21);
            this.textBoxPID_D.TabIndex = 40;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label1.Location = new System.Drawing.Point(16, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 18);
            this.label1.TabIndex = 41;
            this.label1.Text = "P:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label2.Location = new System.Drawing.Point(96, 110);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(19, 18);
            this.label2.TabIndex = 42;
            this.label2.Text = "I:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Lucida Sans Unicode", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(151)))), ((int)(((byte)(116)))), ((int)(((byte)(58)))));
            this.label3.Location = new System.Drawing.Point(176, 110);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 18);
            this.label3.TabIndex = 43;
            this.label3.Text = "D:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonSetPID
            // 
            this.buttonSetPID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSetPID.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonSetPID.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonSetPID.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSetPID.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonSetPID.Location = new System.Drawing.Point(280, 110);
            this.buttonSetPID.Name = "buttonSetPID";
            this.buttonSetPID.Size = new System.Drawing.Size(49, 24);
            this.buttonSetPID.TabIndex = 44;
            this.buttonSetPID.Text = "Set";
            this.buttonSetPID.UseVisualStyleBackColor = false;
            this.buttonSetPID.Click += new System.EventHandler(this.buttonSetPID_Click);
            // 
            // FormFocus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(366, 211);
            this.Controls.Add(this.buttonSetPID);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxPID_D);
            this.Controls.Add(this.textBoxPID_I);
            this.Controls.Add(this.textBoxPID_P);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.panel1);
            this.Name = "FormFocus";
            this.Text = "Wise40 Focus";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonFocusIncrease;
        private System.Windows.Forms.Button buttonFocusDecrease;
        private System.Windows.Forms.Button buttonFocusGoto;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBoxFocusGotoPosition;
        private System.Windows.Forms.Button buttonFocusDown;
        private System.Windows.Forms.Label labelFocusCurrentValue;
        private System.Windows.Forms.Button buttonFocuserStop;
        private System.Windows.Forms.Button buttonFocusAllDown;
        private System.Windows.Forms.Button buttonFocusUp;
        private System.Windows.Forms.Button buttonFocusAllUp;
        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.ComboBox comboBoxFocusStep;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.TextBox textBoxPID_P;
        private System.Windows.Forms.TextBox textBoxPID_I;
        private System.Windows.Forms.TextBox textBoxPID_D;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonSetPID;
    }
}

