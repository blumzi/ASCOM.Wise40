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
        Watcher ascomWatcher;
        Watcher dashWatcher;
        Watcher obsmonWatcher;
        Watcher weatherLinkWatcher;
        bool watchWeatherLink = false;
        private static string serviceName = "Wise40Watcher";
        string _logFile;

        public Wise40Watcher()
        {
            InitializeComponent();

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                WiseVantagePro.OpMode mode;

                Enum.TryParse<WiseVantagePro.OpMode>(driverProfile.GetValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_OpMode, string.Empty, WiseVantagePro.OpMode.File.ToString()), out mode);
                watchWeatherLink = (mode == WiseVantagePro.OpMode.File);
            }
            ascomWatcher = new Watcher("ascom");
            dashWatcher = new Watcher("dash");
            obsmonWatcher = new Watcher("obsmon");
            if (watchWeatherLink)
            {
                weatherLinkWatcher = new Watcher("weatherlink");
            }
        }

        public void log(string fmt, params object[] o)
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
            log("=========== Start ===========");
            if (watchWeatherLink)
            {
                weatherLinkWatcher.Start(args, waitForResponse: true);
            }
            ascomWatcher.Start(args, waitForResponse: true);
            dashWatcher.Start(args);
            obsmonWatcher.Start(args);
            Thread.Sleep(2000);
            log("=========== Start done ===========");
        }

        protected override void OnStop()
        {
            log("=========== Stop ===========");
            dashWatcher.Stop();
            obsmonWatcher.Stop();
            ascomWatcher.Stop();
            if (watchWeatherLink)
                weatherLinkWatcher.Stop();
            log("=========== Stop done ===========");
        }
    }
}