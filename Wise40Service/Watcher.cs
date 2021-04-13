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
        private Const.App _app;
        private Process _process = null;
        private bool _stopping = false;

        private void Init(string name)
        {
            WiseName = name;
            switch (WiseName)
            {
                case "ascom":
                    _app = Const.Apps[Const.Application.RESTServer];
                    break;

                case "weatherlink":
                    _app = Const.Apps[Const.Application.WeatherLink];
                    break;

                case "dash":
                    _app = Const.Apps[Const.Application.Dash];
                    break;

                case "obsmon":
                    _app = Const.Apps[Const.Application.ObservatoryMonitor];
                    break;

                case "safetydash":
                    _app = Const.Apps[Const.Application.SafetyDash];
                    break;
            }
        }

        public Watcher(string name)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            Directory.CreateDirectory(logDir);

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

        private static void OnExit(object sender, System.EventArgs e)
        {
            Process p = sender as Process;

            Wise40Watcher.Log($"Exit: Process {p.Id} on session {p.SessionId} has exited with {p.ExitCode} at {p.ExitTime}");
        }

        public void Worker()
        {
            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_app.Path, out int pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    Wise40Watcher.Log($"Worker ({WiseName}): watching over pid {pid} ({_app.Path}) ...");
                    _process.Exited += OnExit;
                    _process.WaitForExit();
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
                    try
                    {
                        p.Kill();
                        Wise40Watcher.Log($"KillAllProcesses: Killed pid: {p.Id} ({p.ProcessName}) ...");
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex) when (
                        ex is InvalidOperationException ||
                        ex is NotSupportedException ||
                        ex is System.ComponentModel.Win32Exception
                    )
                    {
                        Wise40Watcher.Log($"KillAllProcesses: Pid: {p.Id} ({p.ProcessName}): Caught {ex.Message} at\n{ex.StackTrace}");
                    }
                }
            }
        }

        private void KillAll()
        {
            KillAllProcesses(_app.appName);

            if (WiseName == "ascom")
            {
                KillAllProcesses(Const.Apps[Const.Application.OCH].appName);
                KillAllProcesses(Const.Apps[Const.Application.RemoteClientLocalServer].appName);
            }
        }

        public void Start(string[] args, bool waitForResponse = false)
        {
            string op = args.Length != 0 ? $"Start({args.ToList()})" : "Start" + $" ({WiseName})";

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
                        Wise40Watcher.Log($"{op}: waiting {waitMillis} millis for process to be created ...");
                        Thread.Sleep(waitMillis);
                    } while (_process == null);

                    do
                    {
                        Wise40Watcher.Log($"{op}: waiting {waitMillis} millis for the process to Respond ...");
                        Thread.Sleep(waitMillis);
                    } while (!_process.Responding);
                }
            }
            catch (Exception ex)
            {
                Wise40Watcher.Log($"{op} Exception: {ex.Message} at {ex.StackTrace}");
                return;
            }
        }

        public void Stop()
        {
            _stopping = true;
            Wise40Watcher.Log($"Stop ({WiseName}): The service was Stopped, killing process {_process.Id} ({_process.ProcessName})...");
            _process.Kill();
            Thread.Sleep(1000);

            KillAll();
        }
    }
}
