using System;

namespace ASCOM.Wise40 //.Dome
{
    partial class DomeSetupDialogForm : IDisposable
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
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.checkBoxAutoCalibrate = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxSyncVent = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxMinimalStep = new System.Windows.Forms.TextBox();
            this.textBoxShutterIpAddress = new System.Windows.Forms.TextBox();
            this.textBoxShutterHighestValue = new System.Windows.Forms.TextBox();
            this.textBoxShutterLowestValue = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
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
            this.cmdOK.Location = new System.Drawing.Point(284, 205);
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
            this.cmdCancel.Location = new System.Drawing.Point(284, 235);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Image = global::ASCOM.Wise40.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(292, 16);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // checkBoxAutoCalibrate
            // 
            this.checkBoxAutoCalibrate.AutoSize = true;
            this.checkBoxAutoCalibrate.Location = new System.Drawing.Point(24, 80);
            this.checkBoxAutoCalibrate.Name = "checkBoxAutoCalibrate";
            this.checkBoxAutoCalibrate.Size = new System.Drawing.Size(92, 17);
            this.checkBoxAutoCalibrate.TabIndex = 7;
            this.checkBoxAutoCalibrate.TabStop = false;
            this.checkBoxAutoCalibrate.Text = "Auto Calibrate";
            this.toolTip1.SetToolTip(this.checkBoxAutoCalibrate, "If not calibrated, send dome to find nearest calibration point.");
            this.checkBoxAutoCalibrate.UseVisualStyleBackColor = true;
            this.checkBoxAutoCalibrate.CheckedChanged += new System.EventHandler(this.checkBoxAutoCalibrate_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Wise40 Dome driver setup";
            // 
            // checkBoxSyncVent
            // 
            this.checkBoxSyncVent.AutoSize = true;
            this.checkBoxSyncVent.Location = new System.Drawing.Point(24, 103);
            this.checkBoxSyncVent.Name = "checkBoxSyncVent";
            this.checkBoxSyncVent.Size = new System.Drawing.Size(131, 17);
            this.checkBoxSyncVent.TabIndex = 11;
            this.checkBoxSyncVent.TabStop = false;
            this.checkBoxSyncVent.Text = "Sync vent with shutter";
            this.toolTip1.SetToolTip(this.checkBoxSyncVent, "Open the vent when the shutter is opened and close it when the shutter is closed");
            this.checkBoxSyncVent.UseVisualStyleBackColor = true;
            // 
            // textBoxMinimalStep
            // 
            this.textBoxMinimalStep.Location = new System.Drawing.Point(176, 132);
            this.textBoxMinimalStep.Name = "textBoxMinimalStep";
            this.textBoxMinimalStep.Size = new System.Drawing.Size(24, 20);
            this.textBoxMinimalStep.TabIndex = 12;
            this.textBoxMinimalStep.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxMinimalStep, "When tracking the telescope do not adjust Azimuth for less than this step");
            // 
            // textBoxShutterIpAddress
            // 
            this.textBoxShutterIpAddress.Location = new System.Drawing.Point(88, 24);
            this.textBoxShutterIpAddress.Name = "textBoxShutterIpAddress";
            this.textBoxShutterIpAddress.Size = new System.Drawing.Size(104, 20);
            this.textBoxShutterIpAddress.TabIndex = 16;
            this.toolTip1.SetToolTip(this.textBoxShutterIpAddress, "When tracking the telescope do not adjust Azimuth for less than this step");
            // 
            // textBoxShutterHighestValue
            // 
            this.textBoxShutterHighestValue.Location = new System.Drawing.Point(88, 46);
            this.textBoxShutterHighestValue.Name = "textBoxShutterHighestValue";
            this.textBoxShutterHighestValue.Size = new System.Drawing.Size(48, 20);
            this.textBoxShutterHighestValue.TabIndex = 19;
            this.toolTip1.SetToolTip(this.textBoxShutterHighestValue, "When tracking the telescope do not adjust Azimuth for less than this step");
            // 
            // textBoxShutterLowestValue
            // 
            this.textBoxShutterLowestValue.Location = new System.Drawing.Point(88, 68);
            this.textBoxShutterLowestValue.Name = "textBoxShutterLowestValue";
            this.textBoxShutterLowestValue.Size = new System.Drawing.Size(48, 20);
            this.textBoxShutterLowestValue.TabIndex = 20;
            this.toolTip1.SetToolTip(this.textBoxShutterLowestValue, "When tracking the telescope do not adjust Azimuth for less than this step");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(40, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(135, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Minimal step when tracking";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(200, 136);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "deg";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "IP Address";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBoxShutterLowestValue);
            this.groupBox1.Controls.Add(this.textBoxShutterHighestValue);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.textBoxShutterIpAddress);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBox1.Location = new System.Drawing.Point(32, 168);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(232, 96);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Shutter ";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 72);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Lowest value";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "Highest value";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DomeSetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(354, 283);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxMinimalStep);
            this.Controls.Add(this.checkBoxSyncVent);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxAutoCalibrate);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DomeSetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40 Dome Setup";
            this.Load += new System.EventHandler(this.DomeSetupDialogForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.CheckBox checkBoxAutoCalibrate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxSyncVent;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox textBoxMinimalStep;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxShutterIpAddress;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBoxShutterLowestValue;
        private System.Windows.Forms.TextBox textBoxShutterHighestValue;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
    }
}