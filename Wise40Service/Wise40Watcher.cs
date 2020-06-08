using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.VantagePro;
using ASCOM.Utilities;
using System.Threading;
using System.IO;

namespace Wise40Watcher
{
    public partial class Wise40Watcher : ServiceBase
    {
        private readonly Watcher ascomWatcher;
        private readonly Watcher dashWatcher;
        private readonly Watcher obsmonWatcher;
        private readonly Watcher weatherLinkWatcher;
        private readonly bool weatherLinkNeedsWatching = false;
        private const string serviceName = "Wise40Watcher";
        string _logFile;

        public Wise40Watcher()
        {
            InitializeComponent();

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                Enum.TryParse<WiseVantagePro.OpMode>(driverProfile.GetValue(Const.WiseDriverID.VantagePro,
                    Const.ProfileName.VantagePro_OpMode, string.Empty,
                    nameof(WiseVantagePro.OpMode.File)), out WiseVantagePro.OpMode mode);
                weatherLinkNeedsWatching = (mode == WiseVantagePro.OpMode.File);
            }
            ascomWatcher = new Watcher("ascom");
            dashWatcher = new Watcher("dash");
            obsmonWatcher = new Watcher("obsmon");
            if (weatherLinkNeedsWatching)
            {
                weatherLinkWatcher = new Watcher("weatherlink");
            }
        }

        public void Log(string fmt, params object[] o)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            _logFile = logDir + "/" + serviceName + ".txt";
            Directory.CreateDirectory(logDir);

            string pre = string.Format("{0,-12} ", serviceName);
            string msg = string.Format(pre + fmt, o);
            using (var sw = new System.IO.StreamWriter(_logFile, true))
            {
                sw.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff UT ") + msg);
            }
        }

        protected override void OnStart(string[] args)
        {
            Log("=========== Start ===========");
            if (weatherLinkNeedsWatching)
            {
                weatherLinkWatcher.Start(args, waitForResponse: true);
            }

            const string mySQL = "MySQL80";
            using (ServiceController sc = new ServiceController(mySQL))
            {
                ServiceControllerStatus status = sc.Status;
                switch (status)
                {
                    case ServiceControllerStatus.StartPending:
                    case ServiceControllerStatus.Running:
                        Log($" {mySQL} status is {status}");
                        break;

                    default:
                        Log($" {mySQL} status is {status}. Starting {mySQL} ...");
                        sc.Start();
                        while ((status = sc.Status) != ServiceControllerStatus.Running)
                        {
                            Log($" {mySQL} status is {status}. Waiting for {mySQL} ...");
                            Thread.Sleep(1000);
                        }
                        Log($" {mySQL} status is {status}");
                        break;
                }
            }

            ascomWatcher.Start(args, waitForResponse: true);
            dashWatcher.Start(args);
            obsmonWatcher.Start(args);
            Thread.Sleep(2000);
            Log("=========== Start done ===========");
        }

        protected override void OnStop()
        {
            Log("=========== Stop ===========");
            dashWatcher.Stop();
            obsmonWatcher.Stop();
            ascomWatcher.Stop();
            if (weatherLinkNeedsWatching)
                weatherLinkWatcher.Stop();
            Log("=========== Stop done ===========");
        }
    }
}