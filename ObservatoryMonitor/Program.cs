using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            WiseSite.Instance.init();

            Process[] processes = Process.GetProcessesByName("ASCOM.RemoteDeviceServer");
            if (processes.Length == 0)
            {
                try
                {
                    Process.Start(Const.wiseASCOMServerPath);
                }
                catch (Exception ex)
                {
                    string message;

                    message = string.Format("Could not start the \"{0}\"!\n\n", Const.wiseASCOMServerAppName);
                    message += string.Format("(Exception: {0})\n", ex.Message);
                    message += string.Format("\nPlease start it manually then start \"{0}\" again.", Const.wiseObservatoryMonitorAppName);

                    MessageBox.Show(message,
                        string.Format("Failed to start \"{0}\"!", Const.wiseASCOMServerAppName),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    Application.Exit();
                }
            }
            Application.Run(new ObsMainForm());
        }
    }
}
