namespace ASCOM.Wise40
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
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.accuracyBox = new System.Windows.Forms.ComboBox();
            this.acuracyLabel = new System.Windows.Forms.Label();
            this.traceBox = new System.Windows.Forms.CheckBox();
            this.groupBoxDebugging = new System.Windows.Forms.GroupBox();
            this.checkBoxDebugging = new System.Windows.Forms.CheckBox();
            this.checkBoxDebugEncoders = new System.Windows.Forms.CheckBox();
            this.checkBoxDebugAxes = new System.Windows.Forms.CheckBox();
            this.checkBoxDebugMotors = new System.Windows.Forms.CheckBox();
            this.checkBoxDebugExceptions = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.groupBoxDebugging.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.cmdOK.Location = new System.Drawing.Point(248, 182);
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
            this.cmdCancel.Location = new System.Drawing.Point(248, 214);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = false;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Location = new System.Drawing.Point(24, 34);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(165, 20);
            this.descriptionLabel.TabIndex = 2;
            this.descriptionLabel.Text = "Wise40 Telescope driver setup.";
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
            this.accuracyBox.Location = new System.Drawing.Point(136, 78);
            this.accuracyBox.Name = "accuracyBox";
            this.accuracyBox.Size = new System.Drawing.Size(69, 21);
            this.accuracyBox.TabIndex = 8;
            this.accuracyBox.Text = "Full";
            // 
            // acuracyLabel
            // 
            this.acuracyLabel.AutoSize = true;
            this.acuracyLabel.Location = new System.Drawing.Point(24, 80);
            this.acuracyLabel.Name = "acuracyLabel";
            this.acuracyLabel.Size = new System.Drawing.Size(107, 13);
            this.acuracyLabel.TabIndex = 10;
            this.acuracyLabel.Text = "Astrometric Accuracy";
            // 
            // traceBox
            // 
            this.traceBox.AutoSize = true;
            this.traceBox.Location = new System.Drawing.Point(24, 110);
            this.traceBox.Name = "traceBox";
            this.traceBox.Size = new System.Drawing.Size(54, 17);
            this.traceBox.TabIndex = 11;
            this.traceBox.Text = "Trace";
            this.traceBox.UseVisualStyleBackColor = true;
            // 
            // groupBoxDebugging
            // 
            this.groupBoxDebugging.Controls.Add(this.checkBoxDebugExceptions);
            this.groupBoxDebugging.Controls.Add(this.checkBoxDebugMotors);
            this.groupBoxDebugging.Controls.Add(this.checkBoxDebugAxes);
            this.groupBoxDebugging.Controls.Add(this.checkBoxDebugEncoders);
            this.groupBoxDebugging.ForeColor = System.Drawing.Color.DarkOrange;
            this.groupBoxDebugging.Location = new System.Drawing.Point(88, 142);
            this.groupBoxDebugging.Name = "groupBoxDebugging";
            this.groupBoxDebugging.Size = new System.Drawing.Size(144, 96);
            this.groupBoxDebugging.TabIndex = 12;
            this.groupBoxDebugging.TabStop = false;
            this.groupBoxDebugging.Text = " Debugged components";
            // 
            // checkBoxDebugging
            // 
            this.checkBoxDebugging.AutoSize = true;
            this.checkBoxDebugging.Location = new System.Drawing.Point(24, 142);
            this.checkBoxDebugging.Name = "checkBoxDebugging";
            this.checkBoxDebugging.Size = new System.Drawing.Size(58, 17);
            this.checkBoxDebugging.TabIndex = 13;
            this.checkBoxDebugging.Text = "Debug";
            this.checkBoxDebugging.UseVisualStyleBackColor = true;
            this.checkBoxDebugging.CheckedChanged += new System.EventHandler(this.checkBoxDebugging_CheckedChanged);
            // 
            // checkBoxDebugEncoders
            // 
            this.checkBoxDebugEncoders.AutoSize = true;
            this.checkBoxDebugEncoders.Location = new System.Drawing.Point(8, 24);
            this.checkBoxDebugEncoders.Name = "checkBoxDebugEncoders";
            this.checkBoxDebugEncoders.Size = new System.Drawing.Size(71, 17);
            this.checkBoxDebugEncoders.TabIndex = 0;
            this.checkBoxDebugEncoders.Text = "Encoders";
            this.checkBoxDebugEncoders.UseVisualStyleBackColor = true;
            // 
            // checkBoxDebugAxes
            // 
            this.checkBoxDebugAxes.AutoSize = true;
            this.checkBoxDebugAxes.Location = new System.Drawing.Point(8, 40);
            this.checkBoxDebugAxes.Name = "checkBoxDebugAxes";
            this.checkBoxDebugAxes.Size = new System.Drawing.Size(49, 17);
            this.checkBoxDebugAxes.TabIndex = 1;
            this.checkBoxDebugAxes.Text = "Axes";
            this.checkBoxDebugAxes.UseVisualStyleBackColor = true;
            // 
            // checkBoxDebugMotors
            // 
            this.checkBoxDebugMotors.AutoSize = true;
            this.checkBoxDebugMotors.Location = new System.Drawing.Point(8, 56);
            this.checkBoxDebugMotors.Name = "checkBoxDebugMotors";
            this.checkBoxDebugMotors.Size = new System.Drawing.Size(58, 17);
            this.checkBoxDebugMotors.TabIndex = 2;
            this.checkBoxDebugMotors.Text = "Motors";
            this.checkBoxDebugMotors.UseVisualStyleBackColor = true;
            // 
            // checkBoxDebugExceptions
            // 
            this.checkBoxDebugExceptions.AutoSize = true;
            this.checkBoxDebugExceptions.Location = new System.Drawing.Point(8, 72);
            this.checkBoxDebugExceptions.Name = "checkBoxDebugExceptions";
            this.checkBoxDebugExceptions.Size = new System.Drawing.Size(78, 17);
            this.checkBoxDebugExceptions.TabIndex = 3;
            this.checkBoxDebugExceptions.Text = "Exceptions";
            this.checkBoxDebugExceptions.UseVisualStyleBackColor = true;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(22)))), ((int)(((byte)(16)))));
            this.ClientSize = new System.Drawing.Size(321, 251);
            this.Controls.Add(this.groupBoxDebugging);
            this.Controls.Add(this.checkBoxDebugging);
            this.Controls.Add(this.traceBox);
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
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wise40 Setup";
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.groupBoxDebugging.ResumeLayout(false);
            this.groupBoxDebugging.PerformLayout();
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
        private System.Windows.Forms.CheckBox traceBox;
        private System.Windows.Forms.GroupBox groupBoxDebugging;
        private System.Windows.Forms.CheckBox checkBoxDebugAxes;
        private System.Windows.Forms.CheckBox checkBoxDebugEncoders;
        private System.Windows.Forms.CheckBox checkBoxDebugging;
        private System.Windows.Forms.CheckBox checkBoxDebugMotors;
        private System.Windows.Forms.CheckBox checkBoxDebugExceptions;
    }
}