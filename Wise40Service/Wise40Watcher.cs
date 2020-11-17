using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.VantagePro;
using ASCOM.Utilities;
using System.Threading;
using System.IO;
using ASCOM.Wise40;

namespace Wise40Watcher
{
    public partial class Wise40Watcher : ServiceBase
    {
        private Watcher ascomWatcher;
        private Watcher dashWatcher;
        private Watcher obsmonWatcher;
        private Watcher weatherLinkWatcher;
        private readonly bool weatherLinkNeedsWatching = false;
        private const string serviceName = "Wise40Watcher";
        private static readonly WiseSite.OpMode opMode = WiseSite.OperationalMode;
        private static object _lock = new object();
        private static readonly int pid = Process.GetCurrentProcess().Id;

        public Wise40Watcher()
        {
            InitializeComponent();

            Log($"opMode: {opMode}");

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                Enum.TryParse<WiseVantagePro.OpMode>(driverProfile.GetValue(Const.WiseDriverID.VantagePro,
                    Const.ProfileName.VantagePro_OpMode, string.Empty,
                    nameof(WiseVantagePro.OpMode.File)), out WiseVantagePro.OpMode mode);
                weatherLinkNeedsWatching = (mode == WiseVantagePro.OpMode.File);
            }
        }

        public static void Log(string fmt, params object[] o)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            string _logFile = $"{logDir}/{serviceName}.{pid}.txt";
            Directory.CreateDirectory(logDir);

            string pre = string.Format("{0,-12} ", serviceName);
            string msg = string.Format(pre + fmt, o);
            lock (_lock)
            {
                try
                {
                    using (var sw = new System.IO.StreamWriter(_logFile, true))
                    {
                        sw.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff UT ") + msg);
                    }
                }
                catch (Exception ex) {
                    ;
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            Log($"=========== Start (opMode: {opMode}) ===========");

            if (weatherLinkNeedsWatching)
                weatherLinkWatcher = new Watcher("weatherlink");
            ascomWatcher = new Watcher("ascom");
            switch (opMode)
            {
                case WiseSite.OpMode.ACP:
                case WiseSite.OpMode.LCO:
                case WiseSite.OpMode.WISE:
                    dashWatcher = new Watcher("dash");
                    obsmonWatcher = new Watcher("obsmon");
                    break;
            }

            if (weatherLinkNeedsWatching)
                weatherLinkWatcher.Start(args, waitForResponse: true);

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
            switch (opMode)
            {
                case WiseSite.OpMode.WISE:
                case WiseSite.OpMode.LCO:
                case WiseSite.OpMode.ACP:
                    dashWatcher.Start(args);
                    obsmonWatcher.Start(args);
                    break;
            }
            Thread.Sleep(2000);
            Log("=========== Start done ===========");
        }

        protected override void OnStop()
        {
            try
            {
                Log("=========== Stop ===========");

                switch (opMode)
                {
                    case WiseSite.OpMode.WISE:
                    case WiseSite.OpMode.LCO:
                    case WiseSite.OpMode.ACP:
                        dashWatcher.Stop();
                        obsmonWatcher.Stop();
                        break;
                }
                ascomWatcher.Stop();
                if (weatherLinkNeedsWatching)
                    weatherLinkWatcher.Stop();

                Log("=========== Stop done ===========");
            } catch (Exception ex)
            {
                Log($"OnStop(): caught {ex.Message} at {ex.StackTrace}");
            }
        }
    }
}