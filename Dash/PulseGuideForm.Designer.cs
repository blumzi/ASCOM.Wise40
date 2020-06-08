namespace Dash
{
    partial class PulseGuideForm
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
            this.textBoxRaMillis = new System.Windows.Forms.TextBox();
            this.textBoxDecMillis = new System.Windows.Forms.TextBox();
            this.buttonGo = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonDone = new System.Windows.Forms.Button();
            this.labelRaDeltaDeg = new System.Windows.Forms.Label();
            this.labelRaDeltaEnc = new System.Windows.Forms.Label();
            this.labelDecDeltaEnc = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label10 = new System.Windows.Forms.Label();
            this.labelDecDeltaDeg = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.radioButtonSouth = new System.Windows.Forms.RadioButton();
            this.radioButtonDecIdle = new System.Windows.Forms.RadioButton();
            this.radioButtonNorth = new System.Windows.Forms.RadioButton();
            this.radioButtonEast = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButtonWest = new System.Windows.Forms.RadioButton();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.buttonStop = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxRaMillis
            // 
            this.textBoxRaMillis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRaMillis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxRaMillis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxRaMillis.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRaMillis.Location = new System.Drawing.Point(252, 34);
            this.textBoxRaMillis.Name = "textBoxRaMillis";
            this.textBoxRaMillis.Size = new System.Drawing.Size(68, 21);
            this.textBoxRaMillis.TabIndex = 43;
            this.textBoxRaMillis.Text = "1000";
            this.textBoxRaMillis.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxDecMillis
            // 
            this.textBoxDecMillis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDecMillis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxDecMillis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxDecMillis.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDecMillis.Location = new System.Drawing.Point(252, 82);
            this.textBoxDecMillis.Name = "textBoxDecMillis";
            this.textBoxDecMillis.Size = new System.Drawing.Size(68, 21);
            this.textBoxDecMillis.TabIndex = 42;
            this.textBoxDecMillis.Text = "1000";
            this.textBoxDecMillis.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonGo
            // 
            this.buttonGo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGo.FlatAppearance.BorderSize = 0;
            this.buttonGo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonGo.Location = new System.Drawing.Point(72, 201);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(200, 48);
            this.buttonGo.TabIndex = 50;
            this.buttonGo.Text = "Go";
            this.toolTip1.SetToolTip(this.buttonGo, "Start guiding");
            this.buttonGo.UseCompatibleTextRendering = true;
            this.buttonGo.UseVisualStyleBackColor = false;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(191, 265);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(28, 13);
            this.label5.TabIndex = 51;
            this.label5.Text = "millis";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(252, 1);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 20);
            this.label6.TabIndex = 52;
            this.label6.Text = "Millis";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonDone
            // 
            this.buttonDone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDone.FlatAppearance.BorderSize = 0;
            this.buttonDone.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDone.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonDone.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDone.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonDone.Location = new System.Drawing.Point(424, 201);
            this.buttonDone.Name = "buttonDone";
            this.buttonDone.Size = new System.Drawing.Size(84, 48);
            this.buttonDone.TabIndex = 53;
            this.buttonDone.Text = "Close";
            this.toolTip1.SetToolTip(this.buttonDone, "Close this form");
            this.buttonDone.UseVisualStyleBackColor = false;
            this.buttonDone.Click += new System.EventHandler(this.buttonDone_Click);
            // 
            // labelRaDeltaDeg
            // 
            this.labelRaDeltaDeg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRaDeltaDeg.AutoSize = true;
            this.labelRaDeltaDeg.Location = new System.Drawing.Point(327, 22);
            this.labelRaDeltaDeg.Name = "labelRaDeltaDeg";
            this.labelRaDeltaDeg.Size = new System.Drawing.Size(81, 46);
            this.labelRaDeltaDeg.TabIndex = 56;
            this.labelRaDeltaDeg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRaDeltaEnc
            // 
            this.labelRaDeltaEnc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRaDeltaEnc.AutoSize = true;
            this.labelRaDeltaEnc.Location = new System.Drawing.Point(415, 22);
            this.labelRaDeltaEnc.Name = "labelRaDeltaEnc";
            this.labelRaDeltaEnc.Size = new System.Drawing.Size(81, 46);
            this.labelRaDeltaEnc.TabIndex = 58;
            this.labelRaDeltaEnc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDecDeltaEnc
            // 
            this.labelDecDeltaEnc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDecDeltaEnc.AutoSize = true;
            this.labelDecDeltaEnc.Location = new System.Drawing.Point(415, 69);
            this.labelDecDeltaEnc.Name = "labelDecDeltaEnc";
            this.labelDecDeltaEnc.Size = new System.Drawing.Size(81, 47);
            this.labelDecDeltaEnc.TabIndex = 59;
            this.labelDecDeltaEnc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel1.ColumnCount = 5;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label4, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelDecDeltaEnc, 4, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelRaDeltaEnc, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.label10, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelDecDeltaDeg, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.label9, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelRaDeltaDeg, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxDecMillis, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBoxRaMillis, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 2);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(27, 67);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(500, 117);
            this.tableLayoutPanel1.TabIndex = 60;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(415, 1);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(81, 20);
            this.label10.TabIndex = 63;
            this.label10.Text = "Delta encoder";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDecDeltaDeg
            // 
            this.labelDecDeltaDeg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDecDeltaDeg.AutoSize = true;
            this.labelDecDeltaDeg.Location = new System.Drawing.Point(327, 69);
            this.labelDecDeltaDeg.Name = "labelDecDeltaDeg";
            this.labelDecDeltaDeg.Size = new System.Drawing.Size(81, 47);
            this.labelDecDeltaDeg.TabIndex = 57;
            this.labelDecDeltaDeg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(327, 1);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(81, 20);
            this.label9.TabIndex = 62;
            this.label9.Text = "Delta coord";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // radioButtonSouth
            // 
            this.radioButtonSouth.AutoSize = true;
            this.radioButtonSouth.Location = new System.Drawing.Point(136, 12);
            this.radioButtonSouth.Name = "radioButtonSouth";
            this.radioButtonSouth.Size = new System.Drawing.Size(53, 17);
            this.radioButtonSouth.TabIndex = 2;
            this.radioButtonSouth.Text = "South";
            this.radioButtonSouth.UseVisualStyleBackColor = true;
            // 
            // radioButtonDecIdle
            // 
            this.radioButtonDecIdle.AutoSize = true;
            this.radioButtonDecIdle.Checked = true;
            this.radioButtonDecIdle.Location = new System.Drawing.Point(78, 12);
            this.radioButtonDecIdle.Name = "radioButtonDecIdle";
            this.radioButtonDecIdle.Size = new System.Drawing.Size(51, 17);
            this.radioButtonDecIdle.TabIndex = 1;
            this.radioButtonDecIdle.TabStop = true;
            this.radioButtonDecIdle.Text = "None";
            this.radioButtonDecIdle.UseVisualStyleBackColor = true;
            // 
            // radioButtonNorth
            // 
            this.radioButtonNorth.AutoSize = true;
            this.radioButtonNorth.Location = new System.Drawing.Point(16, 12);
            this.radioButtonNorth.Name = "radioButtonNorth";
            this.radioButtonNorth.Size = new System.Drawing.Size(51, 17);
            this.radioButtonNorth.TabIndex = 0;
            this.radioButtonNorth.Text = "North";
            this.radioButtonNorth.UseVisualStyleBackColor = true;
            // 
            // radioButtonEast
            // 
            this.radioButtonEast.AutoSize = true;
            this.radioButtonEast.Location = new System.Drawing.Point(136, 16);
            this.radioButtonEast.Name = "radioButtonEast";
            this.radioButtonEast.Size = new System.Drawing.Size(46, 17);
            this.radioButtonEast.TabIndex = 2;
            this.radioButtonEast.Text = "East";
            this.radioButtonEast.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(78, 16);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(51, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "None";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButtonWest
            // 
            this.radioButtonWest.AutoSize = true;
            this.radioButtonWest.Location = new System.Drawing.Point(16, 16);
            this.radioButtonWest.Name = "radioButtonWest";
            this.radioButtonWest.Size = new System.Drawing.Size(50, 17);
            this.radioButtonWest.TabIndex = 0;
            this.radioButtonWest.Text = "West";
            this.radioButtonWest.UseVisualStyleBackColor = true;
            // 
            // labelStatus
            // 
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.Location = new System.Drawing.Point(27, 264);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(500, 24);
            this.labelStatus.TabIndex = 61;
            this.labelStatus.Text = "Hello world";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(24, 16);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(264, 40);
            this.label11.TabIndex = 64;
            this.label11.Text = "Select guiding directions and durations,\r\nthen press Go";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonStop
            // 
            this.buttonStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStop.FlatAppearance.BorderSize = 0;
            this.buttonStop.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonStop.Location = new System.Drawing.Point(280, 201);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(72, 48);
            this.buttonStop.TabIndex = 71;
            this.buttonStop.Text = "Stop";
            this.toolTip1.SetToolTip(this.buttonStop, "Abort guiding");
            this.buttonStop.UseVisualStyleBackColor = false;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 46);
            this.label1.TabIndex = 70;
            this.label1.Text = "Ra";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 47);
            this.label2.TabIndex = 71;
            this.label2.Text = "Dec";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radioButtonEast);
            this.panel1.Controls.Add(this.radioButtonWest);
            this.panel1.Controls.Add(this.radioButton2);
            this.panel1.Location = new System.Drawing.Point(45, 25);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(200, 40);
            this.panel1.TabIndex = 72;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.radioButtonSouth);
            this.panel2.Controls.Add(this.radioButtonNorth);
            this.panel2.Controls.Add(this.radioButtonDecIdle);
            this.panel2.Location = new System.Drawing.Point(45, 72);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(200, 41);
            this.panel2.TabIndex = 73;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(4, 1);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 20);
            this.label3.TabIndex = 74;
            this.label3.Text = "Axis";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PulseGuideForm
            // 
            this.AcceptButton = this.buttonDone;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(564, 294);
            this.ControlBox = false;
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonDone);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonGo);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.Name = "PulseGuideForm";
            this.Text = "PulseGuide";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBoxRaMillis;
        private System.Windows.Forms.TextBox textBoxDecMillis;
        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonDone;
        private System.Windows.Forms.Label labelRaDeltaDeg;
        private System.Windows.Forms.Label labelRaDeltaEnc;
        private System.Windows.Forms.Label labelDecDeltaEnc;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label labelDecDeltaDeg;
        private System.Windows.Forms.RadioButton radioButtonSouth;
        private System.Windows.Forms.RadioButton radioButtonDecIdle;
        private System.Windows.Forms.RadioButton radioButtonNorth;
        private System.Windows.Forms.RadioButton radioButtonEast;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButtonWest;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}