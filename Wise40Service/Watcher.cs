using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using System.Diagnostics;
using System.IO;

namespace Wise40Watcher
{
    public class Watcher : WiseObject
    {
        private string _applicationPath;
        private string _applicationName;
        private static string _logFile;
        Process _process = null;
        bool _stopping = false;
        private static string serviceName = "Wise40Watcher";

        private void init(string name)
        {
            string top = Simulated ?
                "c:/Users/Blumzi/Documents/Visual Studio 2015/Projects/Wise40" :
                "c:/Users/mizpe/source/repos/ASCOM.Wise40";

            Name = name;
            switch (Name)
            {
                case "ascom":
                    _applicationPath = Const.wiseASCOMServerPath;
                    _applicationName = Const.wiseASCOMServerAppName;
                    break;

                case "weatherlink":
                    _applicationPath = Const.wiseWeatherLinkAppPath;
                    _applicationName = Const.wiseWeatherLinkAppName;
                    break;

                case "dash":
                    _applicationPath = top + "/Dash/bin/x86/Debug/Dash.exe";
                    _applicationName = Const.wiseDashboardAppName;
                    break;

                case "obsmon":
                    _applicationPath = top + "/ObservatoryMonitor/bin/x86/Debug/ObservatoryMonitor.exe";
                    _applicationName = Const.wiseObservatoryMonitorAppName;
                    break;
            }
        }

        public Watcher(string name)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            Directory.CreateDirectory(logDir);
            _logFile = logDir + "/" + serviceName + ".log";

            init(name);
        }

        public void watcher()
        {
            int pid;

            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_applicationPath, out pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    log("Start: waiting for pid {0} ({1}) ...", pid, _applicationPath);
                    _process.WaitForExit();

                    // TBD: Get the exit code

                    //while (!_process.HasExited)
                    //{
                    //    Thread.Sleep(10);
                    //    _process.Refresh();
                    //}
                    //log("Start: process {0} exited with code: {1}", pid, _process.ExitCode);
                    //_process.Close();
                }
            }
        }

        private void KillAllProcesses()
        {
            Process[] processes = Process.GetProcessesByName(_applicationName);

            foreach (var p in processes)
            {
                log("KillAllProcesses: Killing pid: {0} ({1}) ...", p.Id, p.ProcessName);
                p.Kill();
                Thread.Sleep(1000);
            }
        }

        public void Start(string[] args)
        {
            KillAllProcesses();

            try
            {
                Thread thread = new Thread(watcher);
                thread.Start();
            }
            catch (Exception ex)
            {
                log("Start: Exception: {0}", ex.Message);
                return;
            }
        }

        public void Stop()
        {
            _stopping = true;
            log("The {0} service was Stopped, killing process {1} ...", serviceName, _process.Id);
            _process.Kill();
            Thread.Sleep(1000);

            KillAllProcesses();
        }

        public void log(string fmt, params object[] o)
        {
            string pre = string.Format("{0,-12} ", Name);
            string msg = string.Format(pre + fmt, o);
            using (var sw = new StreamWriter(_logFile, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd, HH:mm:ss.ffff ") + msg);
            }
        }
    }
}
