using System;

namespace ASCOM.Wise40 //.Telescope
{
    partial class TelescopeSetupDialogForm: IDisposable
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
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.accuracyBox = new System.Windows.Forms.ComboBox();
            this.acuracyLabel = new System.Windows.Forms.Label();
            this.checkBoxEnslaveDome = new System.Windows.Forms.CheckBox();
            this.checkBoxCalculateRefraction = new System.Windows.Forms.CheckBox();
            this.checkBoxBypassSafety = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(248, 108);
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
            this.cmdCancel.Location = new System.Drawing.Point(248, 134);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Location = new System.Drawing.Point(24, 24);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(165, 20);
            this.descriptionLabel.TabIndex = 2;
            this.descriptionLabel.Text = "Wise40 Telescope driver setup.";
            this.descriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = global::ASCOM.Wise40.Properties.Resources.ASCOM;
            this.picASCOM.Location = new System.Drawing.Point(253, 16);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // accuracyBox
            // 
            this.accuracyBox.DisplayMember = "Full";
            this.accuracyBox.Items.AddRange(new object[] {
            "Full",
            "Reduced"});
            this.accuracyBox.Location = new System.Drawing.Point(136, 70);
            this.accuracyBox.Name = "accuracyBox";
            this.accuracyBox.Size = new System.Drawing.Size(69, 21);
            this.accuracyBox.TabIndex = 8;
            this.accuracyBox.Text = "Full";
            // 
            // acuracyLabel
            // 
            this.acuracyLabel.AutoSize = true;
            this.acuracyLabel.Location = new System.Drawing.Point(24, 72);
            this.acuracyLabel.Name = "acuracyLabel";
            this.acuracyLabel.Size = new System.Drawing.Size(107, 13);
            this.acuracyLabel.TabIndex = 10;
            this.acuracyLabel.Text = "Astrometric Accuracy";
            // 
            // checkBoxEnslaveDome
            // 
            this.checkBoxEnslaveDome.AutoSize = true;
            this.checkBoxEnslaveDome.Location = new System.Drawing.Point(24, 100);
            this.checkBoxEnslaveDome.Name = "checkBoxEnslaveDome";
            this.checkBoxEnslaveDome.Size = new System.Drawing.Size(93, 17);
            this.checkBoxEnslaveDome.TabIndex = 14;
            this.checkBoxEnslaveDome.Text = "Enslave dome";
            this.checkBoxEnslaveDome.UseVisualStyleBackColor = true;
            // 
            // checkBoxCalculateRefraction
            // 
            this.checkBoxCalculateRefraction.AutoSize = true;
            this.checkBoxCalculateRefraction.Checked = true;
            this.checkBoxCalculateRefraction.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCalculateRefraction.Location = new System.Drawing.Point(24, 120);
            this.checkBoxCalculateRefraction.Name = "checkBoxCalculateRefraction";
            this.checkBoxCalculateRefraction.Size = new System.Drawing.Size(117, 17);
            this.checkBoxCalculateRefraction.TabIndex = 15;
            this.checkBoxCalculateRefraction.Text = "Calculate refraction";
            this.checkBoxCalculateRefraction.UseVisualStyleBackColor = true;
            // 
            // checkBoxBypassSafety
            // 
            this.checkBoxBypassSafety.AutoSize = true;
            this.checkBoxBypassSafety.Location = new System.Drawing.Point(24, 144);
            this.checkBoxBypassSafety.Name = "checkBoxBypassSafety";
            this.checkBoxBypassSafety.Size = new System.Drawing.Size(152, 17);
            this.checkBoxBypassSafety.TabIndex = 16;
            this.checkBoxBypassSafety.Text = "Bypass Coordinates Safety";
            this.toolTip1.SetToolTip(this.checkBoxBypassSafety, "Dangerous!!!  Telescope will not perform safety checks!");
            this.checkBoxBypassSafety.UseVisualStyleBackColor = true;
            // 
            // TelescopeSetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(321, 171);
            this.Controls.Add(this.checkBoxBypassSafety);
            this.Controls.Add(this.checkBoxCalculateRefraction);
            this.Controls.Add(this.checkBoxEnslaveDome);
            this.Controls.Add(this.acuracyLabel);
            this.Controls.Add(this.accuracyBox);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.ForeColor = System.Drawing.Color.DarkOrange;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TelescopeSetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40 Telescope Setup";
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.ComboBox accuracyBox;
        private System.Windows.Forms.Label acuracyLabel;
        private System.Windows.Forms.CheckBox checkBoxEnslaveDome;
        private System.Windows.Forms.CheckBox checkBoxCalculateRefraction;
        private System.Windows.Forms.CheckBox checkBoxBypassSafety;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}