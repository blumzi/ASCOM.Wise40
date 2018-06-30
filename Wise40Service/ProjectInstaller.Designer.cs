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
            this.serviceInstallerWise40 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceInstallerWise40
            // 
            this.serviceInstallerWise40.Description = "Watches and restarts the Wise40 processes";
            this.serviceInstallerWise40.DisplayName = "Wise40Watcher";
            this.serviceInstallerWise40.ServiceName = "Wise40Watcher";
            this.serviceInstallerWise40.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceInstallerWise40});

        }

        #endregion
        private System.ServiceProcess.ServiceInstaller serviceInstallerWise40;
    }
}