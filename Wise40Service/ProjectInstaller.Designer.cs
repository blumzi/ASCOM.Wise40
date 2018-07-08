namespace Wise40Watcher
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceInstallerWise40Watcher = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceInstallerWise40Watcher
            // 
            this.serviceInstallerWise40Watcher.Description = "Watches and restarts the Wise40 processes";
            this.serviceInstallerWise40Watcher.DisplayName = "Wise40Watcher";
            this.serviceInstallerWise40Watcher.ServiceName = "Wise40Watcher";
            this.serviceInstallerWise40Watcher.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstallerWise40Watcher});

        }

        #endregion
        private System.ServiceProcess.ServiceInstaller serviceInstallerWise40Watcher;
    }
}