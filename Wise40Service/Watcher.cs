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
        //private string _applicationPath;
        //private string _applicationName;
        Const.App _application;
        private static string _logFile;
        Process _process = null;
        bool _stopping = false;
        private static string serviceName = "Wise40Watcher";

        private void init(string name)
        {
            WiseName = name;
            switch (WiseName)
            {
                case "ascom":
                    _application = Const.Apps[Const.Application.RESTServer];
                    break;

                case "weatherlink":
                    _application = Const.Apps[Const.Application.WeatherLink];
                    break;

                case "dash":
                    _application = Const.Apps[Const.Application.Dash];
                    break;

                case "obsmon":
                    _application = Const.Apps[Const.Application.ObservatoryMonitor];
                    break;
            }
        }

        public Watcher(string name)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            Directory.CreateDirectory(logDir);
            _logFile = logDir + "/" + serviceName + ".txt";

            init(name);
        }

        public bool Responding
        {
            get
            {
                if (_process == null)
                    return false;
                return _process.Responding;
            }
        }

        public void watcher()
        {
            int pid;

            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_application.Path, out pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    log("Start: watching for pid {0} ({1}) ...", pid, _application.Path);
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

        private void KillAllProcesses(string appName)
        {
            for (int tries = 3; tries != 0; tries--)
            {
                var processes = Process.GetProcessesByName(appName);

                if (processes.Count() == 0)
                    return;

                foreach (var p in processes)
                {
                    log("KillAllProcesses: Killing pid: {0} ({1}) ...", p.Id, p.ProcessName);
                    p.Kill();
                    Thread.Sleep(1000);
                }
            }
        }

        private void KillAll()
        {
            if (WiseName == "ascom")
                KillAllProcesses(Const.Apps[Const.Application.OCH].appName);

            KillAllProcesses(_application.appName);

            if (WiseName == "ascom")
                KillAllProcesses(Const.Apps[Const.Application.RemoteClientLocalServer].appName);
        }

        public void Start(string[] args, bool waitForResponse = false)
        {
            KillAll();
            int waitMillis = 1000;

            try
            {
                Thread thread = new Thread(watcher);
                thread.Start();
                if (waitForResponse)
                {
                    do
                    {
                        log("Start: Waiting {0} for the process to be created ...", waitMillis);
                        Thread.Sleep(waitMillis);
                    } while (_process == null);

                    do
                    {
                        log("Start: Waiting {0} millis for process {1} to Respond ...", waitMillis, _process.Id);
                        Thread.Sleep(waitMillis);
                    } while (!_process.Responding);
                }
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
            log("Stop: The {0} service was Stopped, killing process {1} ({2})...", serviceName, _process.Id, _process.ProcessName);
            _process.Kill();
            Thread.Sleep(1000);

            KillAll();
        }

        public void log(string fmt, params object[] o)
        {
            string pre = string.Format("{0,-12} ", WiseName);
            string msg = string.Format(pre + fmt, o);
            using (var sw = new StreamWriter(_logFile, true))
            {
                sw.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff UT ") + msg);
            }
        }
    }
}
