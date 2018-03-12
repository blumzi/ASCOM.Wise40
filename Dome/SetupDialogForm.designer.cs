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
            this.checkBoxBypassSafety = new System.Windows.Forms.CheckBox();
            this.checkBoxSyncVent = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxMinimalStep = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(284, 120);
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
            this.cmdCancel.Location = new System.Drawing.Point(284, 150);
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
            // checkBoxBypassSafety
            // 
            this.checkBoxBypassSafety.AutoSize = true;
            this.checkBoxBypassSafety.Location = new System.Drawing.Point(25, 103);
            this.checkBoxBypassSafety.Name = "checkBoxBypassSafety";
            this.checkBoxBypassSafety.Size = new System.Drawing.Size(136, 17);
            this.checkBoxBypassSafety.TabIndex = 10;
            this.checkBoxBypassSafety.TabStop = false;
            this.checkBoxBypassSafety.Text = "Bypass SafeToOperate";
            this.toolTip1.SetToolTip(this.checkBoxBypassSafety, "Allow the shutter to be opened even if SafeToOperate is False");
            this.checkBoxBypassSafety.UseVisualStyleBackColor = true;
            this.checkBoxBypassSafety.CheckedChanged += new System.EventHandler(this.checkBoxBypassSafety_CheckedChanged);
            // 
            // checkBoxSyncVent
            // 
            this.checkBoxSyncVent.AutoSize = true;
            this.checkBoxSyncVent.Location = new System.Drawing.Point(24, 128);
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
            this.textBoxMinimalStep.Location = new System.Drawing.Point(176, 156);
            this.textBoxMinimalStep.Name = "textBoxMinimalStep";
            this.textBoxMinimalStep.Size = new System.Drawing.Size(24, 20);
            this.textBoxMinimalStep.TabIndex = 12;
            this.textBoxMinimalStep.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip1.SetToolTip(this.textBoxMinimalStep, "When tracking the telescope do not adjust Azimuth for less than this step");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(40, 160);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(135, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Minimal step when tracking";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(200, 160);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "deg";
            // 
            // DomeSetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(354, 197);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxMinimalStep);
            this.Controls.Add(this.checkBoxSyncVent);
            this.Controls.Add(this.checkBoxBypassSafety);
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
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.CheckBox checkBoxAutoCalibrate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxBypassSafety;
        private System.Windows.Forms.CheckBox checkBoxSyncVent;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox textBoxMinimalStep;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}