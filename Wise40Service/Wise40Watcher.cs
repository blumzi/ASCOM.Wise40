using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace Wise40Watcher
{
    public partial class Wise40Watcher : ServiceBase
    {
        WiseObject wiseobject = new WiseObject();
        private Process serverProcess, dashProcess;
        private bool _stopping = false;

        private void serverExited(object sender, System.EventArgs e)
        {
            if (!_stopping)
                StartServer();
        }

        private void dashExited(object sender, System.EventArgs e)
        {
            if (! _stopping)
                StartDash();
        }

        public void StartServer()
        {
            serverProcess = new Process();
            serverProcess.StartInfo.CreateNoWindow = true;
            serverProcess.StartInfo.FileName = Const.wiseASCOMServerPath;
            serverProcess.EnableRaisingEvents = true;
            serverProcess.Exited += new EventHandler(serverExited);
            serverProcess.Start();
        }

        public void StartDash()
        {
            dashProcess = new Process();
            dashProcess.StartInfo.CreateNoWindow = true;
            dashProcess.StartInfo.FileName = wiseobject.Simulated ? Const.wiseSimulatedDashPath : Const.wiseASCOMServerPath;
            dashProcess.EnableRaisingEvents = true;
            dashProcess.Exited += new EventHandler(dashExited);
            dashProcess.Start();
        }

        public Wise40Watcher()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartServer();
            StartDash();
        }

        protected override void OnStop()
        {
            _stopping = true;
            serverProcess.Kill();
            dashProcess.Kill();
        }
    }
}
