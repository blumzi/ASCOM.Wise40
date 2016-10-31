namespace Dash
{
    partial class DebuggingForm
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
            this.menuStripDebugging = new System.Windows.Forms.MenuStrip();
            this.followToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxDebugMessages = new System.Windows.Forms.ListBox();
            this.menuStripDebugging.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripDebugging
            // 
            this.menuStripDebugging.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.followToolStripMenuItem,
            this.markToolStripMenuItem,
            this.clearToolStripMenuItem,
            this.saveToFileToolStripMenuItem});
            this.menuStripDebugging.Location = new System.Drawing.Point(0, 0);
            this.menuStripDebugging.Name = "menuStripDebugging";
            this.menuStripDebugging.Size = new System.Drawing.Size(1238, 24);
            this.menuStripDebugging.TabIndex = 0;
            this.menuStripDebugging.Text = "menuStrip1";
            // 
            // followToolStripMenuItem
            // 
            this.followToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.followToolStripMenuItem.Name = "followToolStripMenuItem";
            this.followToolStripMenuItem.Size = new System.Drawing.Size(94, 20);
            this.followToolStripMenuItem.Text = "KeepUpdated";
            this.followToolStripMenuItem.Click += new System.EventHandler(this.followToolStripMenuItem_Click);
            // 
            // markToolStripMenuItem
            // 
            this.markToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.markToolStripMenuItem.Name = "markToolStripMenuItem";
            this.markToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.markToolStripMenuItem.Text = "Mark";
            this.markToolStripMenuItem.Click += new System.EventHandler(this.markToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // saveToFileToolStripMenuItem
            // 
            this.saveToFileToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.saveToFileToolStripMenuItem.Name = "saveToFileToolStripMenuItem";
            this.saveToFileToolStripMenuItem.Size = new System.Drawing.Size(78, 20);
            this.saveToFileToolStripMenuItem.Text = "SaveToFile";
            this.saveToFileToolStripMenuItem.Click += new System.EventHandler(this.saveToFileToolStripMenuItem_Click);
            // 
            // listBoxDebugMessages
            // 
            this.listBoxDebugMessages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listBoxDebugMessages.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listBoxDebugMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxDebugMessages.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(161)))), ((int)(((byte)(142)))));
            this.listBoxDebugMessages.FormattingEnabled = true;
            this.listBoxDebugMessages.HorizontalScrollbar = true;
            this.listBoxDebugMessages.Location = new System.Drawing.Point(0, 24);
            this.listBoxDebugMessages.Name = "listBoxDebugMessages";
            this.listBoxDebugMessages.Size = new System.Drawing.Size(1238, 420);
            this.listBoxDebugMessages.TabIndex = 1;
            // 
            // DebuggingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1238, 444);
            this.Controls.Add(this.listBoxDebugMessages);
            this.Controls.Add(this.menuStripDebugging);
            this.MainMenuStrip = this.menuStripDebugging;
            this.Name = "DebuggingForm";
            this.Text = "DebuggingForm";
            this.menuStripDebugging.ResumeLayout(false);
            this.menuStripDebugging.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripDebugging;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem markToolStripMenuItem;
        private System.Windows.Forms.ListBox listBoxDebugMessages;
        private System.Windows.Forms.ToolStripMenuItem followToolStripMenuItem;
    }
}