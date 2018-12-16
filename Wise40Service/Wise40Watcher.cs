using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.VantagePro;
using ASCOM.Utilities;
using System.Threading;

namespace Wise40Watcher
{
    public partial class Wise40Watcher : ServiceBase
    {
        Watcher ascomWatcher;
        Watcher dashWatcher;
        Watcher obsmonWatcher;
        Watcher weatherLinkWatcher;
        bool watchWeatherLink = false;

        public Wise40Watcher()
        {
            InitializeComponent();
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                WiseVantagePro.OpMode mode;

                Enum.TryParse<WiseVantagePro.OpMode>(driverProfile.GetValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_OpMode, string.Empty, WiseVantagePro.OpMode.File.ToString()), out mode);
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

        protected override void OnStart(string[] args)
        {
            if (watchWeatherLink)
            {
                weatherLinkWatcher.Start(args, waitForResponse: true);
            }
            ascomWatcher.Start(args, waitForResponse: true);
            dashWatcher.Start(args);
            obsmonWatcher.Start(args);
        }

        protected override void OnStop()
        {
            dashWatcher.Stop();
            obsmonWatcher.Stop();
            ascomWatcher.Stop();
            if (watchWeatherLink)
                weatherLinkWatcher.Stop();
        }
    }
}