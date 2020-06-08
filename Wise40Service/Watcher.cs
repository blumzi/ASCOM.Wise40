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
using System.Runtime.CompilerServices;

namespace Wise40Watcher
{
    public class Watcher : WiseObject
    {
        private Const.App _application;
        private static string _logFile;
        private Process _process = null;
        private bool _stopping = false;
        private const string serviceName = "Wise40Watcher";

        private void Init(string name)
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

            Init(name);
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

        public void Worker()
        {
            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_application.Path, out int pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    Log($"Start: watching for pid {pid} ({_application.Path}) ...");
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

                if (processes.Length == 0)
                    return;

                foreach (var p in processes)
                {
                    Log($"KillAllProcesses: Killing pid: {p.Id} ({p.ProcessName}) ...");
                    p.Kill();
                    Thread.Sleep(1000);
                }
            }
        }

        private void KillAll()
        {
            KillAllProcesses(_application.appName);

            if (WiseName == "ascom")
            {
                KillAllProcesses(Const.Apps[Const.Application.OCH].appName);
                KillAllProcesses(Const.Apps[Const.Application.RemoteClientLocalServer].appName);
            }
        }

        public void Start(string[] args, bool waitForResponse = false)
        {
            KillAll();
            const int waitMillis = 1000;

            try
            {
                Thread thread = new Thread(Worker);
                thread.Start();
                if (waitForResponse)
                {
                    do
                    {
                        Log($"Start: Waiting {waitMillis} for the process to be created ...");
                        Thread.Sleep(waitMillis);
                    } while (_process == null);

                    do
                    {
                        Log($"Start: Waiting {waitMillis} millis for process {_process.Id} to Respond ...");
                        Thread.Sleep(waitMillis);
                    } while (!_process.Responding);
                }
            }
            catch (Exception ex)
            {
                Log($"Start: Exception: {ex.Message} at {ex.StackTrace}");
                return;
            }
        }

        public void Stop()
        {
            _stopping = true;
            Log($"Stop: The {serviceName} service was Stopped, killing process {_process.Id} ({_process.ProcessName})...");
            _process.Kill();
            Thread.Sleep(1000);

            KillAll();
        }

        public void Log(string fmt, params object[] o)
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
