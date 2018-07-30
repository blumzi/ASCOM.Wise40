namespace ASCOM.Wise40.VantagePro
{
    partial class SetupDialogForm
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
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.buttonChoose = new System.Windows.Forms.Button();
            this.labelReportFileValue = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButtonSerialPort = new System.Windows.Forms.RadioButton();
            this.radioButtonDataFile = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxSerialPort = new System.Windows.Forms.ComboBox();
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
            this.cmdOK.Location = new System.Drawing.Point(493, 97);
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
            this.cmdCancel.Location = new System.Drawing.Point(493, 127);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(231, 31);
            this.label1.TabIndex = 2;
            this.label1.Text = "Davis VantagePro at Wise40 driver";
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.ErrorImage = global::ASCOM.Wise40.VantagePro.Properties.Resources.ASCOM;
            this.picASCOM.Image = global::ASCOM.Wise40.VantagePro.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(498, 9);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // buttonChoose
            // 
            this.buttonChoose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonChoose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonChoose.Location = new System.Drawing.Point(92, 16);
            this.buttonChoose.Name = "buttonChoose";
            this.buttonChoose.Size = new System.Drawing.Size(75, 23);
            this.buttonChoose.TabIndex = 7;
            this.buttonChoose.Text = "Choose";
            this.buttonChoose.UseVisualStyleBackColor = false;
            this.buttonChoose.Click += new System.EventHandler(this.buttonChoose_Click);
            // 
            // labelReportFileValue
            // 
            this.labelReportFileValue.Location = new System.Drawing.Point(184, 16);
            this.labelReportFileValue.Name = "labelReportFileValue";
            this.labelReportFileValue.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.labelReportFileValue.Size = new System.Drawing.Size(268, 23);
            this.labelReportFileValue.TabIndex = 8;
            this.labelReportFileValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Title = "WeatherLink Report File";
            this.openFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog_FileOk);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBoxSerialPort);
            this.groupBox1.Controls.Add(this.labelReportFileValue);
            this.groupBox1.Controls.Add(this.buttonChoose);
            this.groupBox1.Controls.Add(this.radioButtonSerialPort);
            this.groupBox1.Controls.Add(this.radioButtonDataFile);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(8, 72);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(464, 80);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Operation mode ";
            // 
            // radioButtonSerialPort
            // 
            this.radioButtonSerialPort.AutoSize = true;
            this.radioButtonSerialPort.Location = new System.Drawing.Point(80, 16);
            this.radioButtonSerialPort.Name = "radioButtonSerialPort";
            this.radioButtonSerialPort.Size = new System.Drawing.Size(73, 17);
            this.radioButtonSerialPort.TabIndex = 1;
            this.radioButtonSerialPort.Text = "Serial Port";
            this.radioButtonSerialPort.UseVisualStyleBackColor = true;
            this.radioButtonSerialPort.Click += new System.EventHandler(this.radioButtonSerialPort_Click);
            // 
            // radioButtonDataFile
            // 
            this.radioButtonDataFile.AutoSize = true;
            this.radioButtonDataFile.Checked = true;
            this.radioButtonDataFile.Location = new System.Drawing.Point(16, 16);
            this.radioButtonDataFile.Name = "radioButtonDataFile";
            this.radioButtonDataFile.Size = new System.Drawing.Size(61, 17);
            this.radioButtonDataFile.TabIndex = 0;
            this.radioButtonDataFile.TabStop = true;
            this.radioButtonDataFile.Text = "Datafile";
            this.radioButtonDataFile.UseVisualStyleBackColor = true;
            this.radioButtonDataFile.Click += new System.EventHandler(this.radioButtonDataFile_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.comboBoxSerialPort);
            this.groupBox2.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox2.Location = new System.Drawing.Point(32, 192);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(120, 64);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Serial Port ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Port:";
            // 
            // comboBoxSerialPort
            // 
            this.comboBoxSerialPort.FormattingEnabled = true;
            this.comboBoxSerialPort.Location = new System.Drawing.Point(40, 28);
            this.comboBoxSerialPort.Name = "comboBoxSerialPort";
            this.comboBoxSerialPort.Size = new System.Drawing.Size(64, 21);
            this.comboBoxSerialPort.TabIndex = 0;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(556, 194);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40 Vantage Setup";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
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
        private System.Windows.Forms.Button buttonChoose;
        private System.Windows.Forms.Label labelReportFileValue;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonSerialPort;
        private System.Windows.Forms.RadioButton radioButtonDataFile;
        private System.Windows.Forms.ComboBox comboBoxSerialPort;
        //private System.Windows.Forms.ComboBox comboBoxComPort;
    }
}