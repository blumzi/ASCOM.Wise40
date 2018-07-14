using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ASCOM.Wise40.Common;

namespace Wise40Watcher
{
    public partial class Wise40Watcher : ServiceBase
    {
        Watcher ascomWatcher;
        Watcher dashWatcher;
        Watcher obsmonWatcher;

        public Wise40Watcher()
        {
            InitializeComponent();

            ascomWatcher = new Watcher("ascom");
            dashWatcher = new Watcher("dash");
            obsmonWatcher = new Watcher("obsmon");
        }

        protected override void OnStart(string[] args)
        {
            ascomWatcher.Start(args);
            dashWatcher.Start(args);
            obsmonWatcher.Start(args);
        }

        protected override void OnStop()
        {
            ascomWatcher.Stop();
            dashWatcher.Stop();
            obsmonWatcher.Stop();
        }
    }
}