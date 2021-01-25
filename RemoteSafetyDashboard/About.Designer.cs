namespace RemoteSafetyDashboard
{
    partial class About
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
            this.labelAbout = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelAbout
            // 
            this.labelAbout.ForeColor = System.Drawing.Color.DarkOrange;
            this.labelAbout.Location = new System.Drawing.Point(12, 9);
            this.labelAbout.Name = "labelAbout";
            this.labelAbout.Size = new System.Drawing.Size(318, 136);
            this.labelAbout.TabIndex = 0;
            this.labelAbout.Text = "This dashboard shows the current weather and safety\r\nconditions at the Wise compo" +
    "und.\r\n\r\nIt does this by periodically contacting the\r\ncentral computer (dome-pc)\r" +
    "\n";
            this.labelAbout.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonOK
            // 
            this.buttonOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buttonOK.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.buttonOK.Location = new System.Drawing.Point(134, 148);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = false;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // About
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(26)))), ((int)(((byte)(26)))));
            this.ClientSize = new System.Drawing.Size(336, 186);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelAbout);
            this.ForeColor = System.Drawing.Color.Chocolate;
            this.Name = "About";
            this.Text = "About RemoteSafetyDashboard";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelAbout;
        private System.Windows.Forms.Button buttonOK;
    }
}