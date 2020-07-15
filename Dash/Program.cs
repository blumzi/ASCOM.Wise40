using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40;

namespace Dash
{
    internal static class Program
    {
        public static FormDash formDash;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 1)
            {
                if (args[0] == "--dio-monitor")
                {
                    Application.Run(new ASCOM.Wise40.HardwareForm(new ASCOM.DriverAccess.Telescope("ASCOM.Remote1.Telescope")));
                }
                else if (args[0].StartsWith("--mode="))
                {
                    if (Enum.TryParse<WiseSite.OpMode>(args[0].Substring("--mode=".Length).ToUpper(), out WiseSite.OpMode mode))
                        WiseSite.OperationalMode = mode;
                }

                Environment.Exit(0);
            }
            else
            {
                // Set the unhandled exception mode to force all Windows Forms errors to go through
                // our handler.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                formDash = new FormDash();

                Application.ApplicationExit += formDash.OnApplicationExit;

                // Add the event handler for handling UI thread exceptions to the event.
                Application.ThreadException += formDash.HandleThreadException;

                // Add the event handler for handling non-UI thread exceptions to the event. 
                AppDomain.CurrentDomain.UnhandledException += formDash.HandleDomainUnhandledException;

                Application.Run(formDash);
            }
        }
    }
}
