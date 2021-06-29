#define MYSQL_WORKS
//#define SERVER_ONLY

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
        private Watcher alpacaWatcher;
#if !SERVER_ONLY
        private Watcher dashWatcher;
        private Watcher obsmonWatcher;
#endif
        private Watcher weatherLinkWatcher;
        private readonly bool weatherLinkNeedsWatching = false;
        private const string serviceName = "Wise40Watcher";
        private static readonly WiseSite.OpMode opMode = WiseSite.OperationalMode;
        private static readonly object _lock = new object();
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
                } catch { }
            }
        }

        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            Log($"=========== Start (opMode: {opMode}) ===========");
            
            ConnectWlan();
            
            if (weatherLinkNeedsWatching)
                weatherLinkWatcher = new Watcher("weatherlink");
            ascomWatcher = new Watcher("ascom");
            alpacaWatcher = new Watcher("alpaca");
#if !SERVER_ONLY
            switch (opMode)
            {
                case WiseSite.OpMode.ACP:
                    dashWatcher = new Watcher("dash");  //dashWatcher = new Watcher("safetydash");
                    break;
                case WiseSite.OpMode.WISE:
                    dashWatcher = new Watcher("dash");  //dashWatcher = new Watcher("safetydash");
                    break;
                case WiseSite.OpMode.LCO:
                    dashWatcher = new Watcher("dash");
                    obsmonWatcher = new Watcher("obsmon");
                    break;
            }
#endif
            if (weatherLinkNeedsWatching)
                weatherLinkWatcher.Start(args, waitForResponse: true);

#if MYSQL_WORKS
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
#endif

            ascomWatcher.Start(args, waitForResponse: true);
            alpacaWatcher.Start(args);
#if !SERVER_ONLY
            switch (opMode)
            {
                case WiseSite.OpMode.WISE:
                case WiseSite.OpMode.ACP:
                    dashWatcher.Start(args);
                    break;
                case WiseSite.OpMode.LCO:
                    dashWatcher.Start(args);
                    obsmonWatcher.Start(args);
                    break;
            }
#endif
            Thread.Sleep(2000);
            Log("=========== Start done ===========");
        }

        protected override void OnStop()
        {
            try
            {
                Log("=========== Stop ===========");
#if !SERVER_ONLY
                switch (opMode)
                {
                    case WiseSite.OpMode.WISE:
                    case WiseSite.OpMode.ACP:
                        dashWatcher.Stop();
                        break;
                    case WiseSite.OpMode.LCO:
                        dashWatcher.Stop();
                        obsmonWatcher.Stop();
                        break;
                }
#endif
                ascomWatcher.Stop();
                alpacaWatcher.Stop();
                if (weatherLinkNeedsWatching)
                    weatherLinkWatcher.Stop();

                Log("=========== Stop done ===========");
            }
            catch (Exception ex)
            {
                Log($"OnStop(): caught {ex.Message} at {ex.StackTrace}");
            }
        }

        private void ConnectWlan()
        {
            string output;
            string interfaceName = "Wireless Network Connection 4";

            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = $"interface show interface name=\"{interfaceName}\"";
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            if (output.Contains("Connected"))
            {
                Log($"Interface '{interfaceName}' already connected.");
            }
            else
            {
                using (var p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.FileName = "netsh.exe";
                    p.StartInfo.Arguments = $"wlan connect interface=\"{interfaceName}\" name=wo";
                    p.Start();

                    Log($"Initiated WiFi connection with: {p.StartInfo.FileName} {p.StartInfo.Arguments} ...");
                }
            }
        }
    }
}