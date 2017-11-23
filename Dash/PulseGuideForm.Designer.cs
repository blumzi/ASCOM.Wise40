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
            this.textBoxRaMillis = new System.Windows.Forms.TextBox();
            this.textBoxDecMillis = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBarDec = new System.Windows.Forms.TrackBar();
            this.trackBarRa = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonGo = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonDone = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelRaDeltaDeg = new System.Windows.Forms.Label();
            this.labelDecDeltaDeg = new System.Windows.Forms.Label();
            this.labelRaDeltaEnc = new System.Windows.Forms.Label();
            this.labelDecDeltaEnc = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarRa)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxRaMillis
            // 
            this.textBoxRaMillis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRaMillis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxRaMillis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxRaMillis.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRaMillis.Location = new System.Drawing.Point(192, 184);
            this.textBoxRaMillis.Name = "textBoxRaMillis";
            this.textBoxRaMillis.Size = new System.Drawing.Size(40, 21);
            this.textBoxRaMillis.TabIndex = 43;
            this.textBoxRaMillis.Text = "6000";
            this.textBoxRaMillis.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBoxDecMillis
            // 
            this.textBoxDecMillis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDecMillis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.textBoxDecMillis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxDecMillis.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxDecMillis.Location = new System.Drawing.Point(56, 88);
            this.textBoxDecMillis.Name = "textBoxDecMillis";
            this.textBoxDecMillis.Size = new System.Drawing.Size(40, 21);
            this.textBoxDecMillis.TabIndex = 42;
            this.textBoxDecMillis.Text = "6000";
            this.textBoxDecMillis.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 44;
            this.label1.Text = "North";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 160);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 45;
            this.label2.Text = "South";
            // 
            // trackBarDec
            // 
            this.trackBarDec.Location = new System.Drawing.Point(56, 114);
            this.trackBarDec.Maximum = 2;
            this.trackBarDec.Name = "trackBarDec";
            this.trackBarDec.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBarDec.Size = new System.Drawing.Size(45, 64);
            this.trackBarDec.TabIndex = 46;
            this.trackBarDec.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.trackBarDec.Value = 1;
            // 
            // trackBarRa
            // 
            this.trackBarRa.Location = new System.Drawing.Point(109, 184);
            this.trackBarRa.Maximum = 2;
            this.trackBarRa.Name = "trackBarRa";
            this.trackBarRa.Size = new System.Drawing.Size(64, 45);
            this.trackBarRa.TabIndex = 47;
            this.trackBarRa.Value = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(109, 216);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 48;
            this.label3.Text = "West";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(152, 216);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 13);
            this.label4.TabIndex = 49;
            this.label4.Text = "East";
            // 
            // buttonGo
            // 
            this.buttonGo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGo.FlatAppearance.BorderSize = 0;
            this.buttonGo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonGo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonGo.Location = new System.Drawing.Point(112, 88);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(120, 88);
            this.buttonGo.TabIndex = 50;
            this.buttonGo.Text = "Go";
            this.buttonGo.UseVisualStyleBackColor = false;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(198, 216);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(28, 13);
            this.label5.TabIndex = 51;
            this.label5.Text = "millis";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(24, 92);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 13);
            this.label6.TabIndex = 52;
            this.label6.Text = "millis";
            // 
            // buttonDone
            // 
            this.buttonDone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDone.FlatAppearance.BorderSize = 0;
            this.buttonDone.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonDone.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonDone.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDone.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonDone.Location = new System.Drawing.Point(312, 216);
            this.buttonDone.Name = "buttonDone";
            this.buttonDone.Size = new System.Drawing.Size(80, 32);
            this.buttonDone.TabIndex = 53;
            this.buttonDone.Text = "Done";
            this.buttonDone.UseVisualStyleBackColor = false;
            this.buttonDone.Click += new System.EventHandler(this.buttonDone_Click);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(37, 44);
            this.label7.TabIndex = 54;
            this.label7.Text = "RA";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 44);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(37, 44);
            this.label8.TabIndex = 55;
            this.label8.Text = "Dec";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRaDeltaDeg
            // 
            this.labelRaDeltaDeg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRaDeltaDeg.AutoSize = true;
            this.labelRaDeltaDeg.Location = new System.Drawing.Point(46, 0);
            this.labelRaDeltaDeg.Name = "labelRaDeltaDeg";
            this.labelRaDeltaDeg.Size = new System.Drawing.Size(80, 44);
            this.labelRaDeltaDeg.TabIndex = 56;
            this.labelRaDeltaDeg.Text = "00h00m00.0s";
            this.labelRaDeltaDeg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDecDeltaDeg
            // 
            this.labelDecDeltaDeg.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDecDeltaDeg.AutoSize = true;
            this.labelDecDeltaDeg.Location = new System.Drawing.Point(46, 44);
            this.labelDecDeltaDeg.Name = "labelDecDeltaDeg";
            this.labelDecDeltaDeg.Size = new System.Drawing.Size(80, 44);
            this.labelDecDeltaDeg.TabIndex = 57;
            this.labelDecDeltaDeg.Text = "00:00:00.0";
            this.labelDecDeltaDeg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRaDeltaEnc
            // 
            this.labelRaDeltaEnc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRaDeltaEnc.AutoSize = true;
            this.labelRaDeltaEnc.Location = new System.Drawing.Point(132, 0);
            this.labelRaDeltaEnc.Name = "labelRaDeltaEnc";
            this.labelRaDeltaEnc.Size = new System.Drawing.Size(81, 44);
            this.labelRaDeltaEnc.TabIndex = 58;
            this.labelRaDeltaEnc.Text = "00000000";
            this.labelRaDeltaEnc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDecDeltaEnc
            // 
            this.labelDecDeltaEnc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDecDeltaEnc.AutoSize = true;
            this.labelDecDeltaEnc.Location = new System.Drawing.Point(132, 44);
            this.labelDecDeltaEnc.Name = "labelDecDeltaEnc";
            this.labelDecDeltaEnc.Size = new System.Drawing.Size(81, 44);
            this.labelDecDeltaEnc.TabIndex = 59;
            this.labelDecDeltaEnc.Text = "00000000";
            this.labelDecDeltaEnc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelDecDeltaEnc, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelRaDeltaEnc, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelDecDeltaDeg, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelRaDeltaDeg, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(248, 88);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(216, 88);
            this.tableLayoutPanel1.TabIndex = 60;
            // 
            // labelStatus
            // 
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.Location = new System.Drawing.Point(16, 264);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(464, 24);
            this.labelStatus.TabIndex = 61;
            this.labelStatus.Text = "Hello world";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(304, 64);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 13);
            this.label9.TabIndex = 62;
            this.label9.Text = "Delta deg";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(384, 64);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 63;
            this.label10.Text = "Delta encoder";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(24, 16);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(264, 48);
            this.label11.TabIndex = 64;
            this.label11.Text = "Select guiding directions and durations,\r\nthen press Go";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PulseGuideForm
            // 
            this.AcceptButton = this.buttonDone;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(490, 294);
            this.ControlBox = false;
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.buttonDone);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonGo);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.trackBarRa);
            this.Controls.Add(this.trackBarDec);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxRaMillis);
            this.Controls.Add(this.textBoxDecMillis);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.Name = "PulseGuideForm";
            this.Text = "PulseGuide";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarDec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarRa)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBoxRaMillis;
        private System.Windows.Forms.TextBox textBoxDecMillis;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar trackBarDec;
        private System.Windows.Forms.TrackBar trackBarRa;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonDone;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label labelRaDeltaDeg;
        private System.Windows.Forms.Label labelDecDeltaDeg;
        private System.Windows.Forms.Label labelRaDeltaEnc;
        private System.Windows.Forms.Label labelDecDeltaEnc;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
    }
}