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
        private string _path;
        private static string _logFile;
        Process _process = null;
        bool _stopping = false;
        private static string serviceName = "Wise40Watcher";

        private string applicationPath()
        {
            string ret = string.Empty;
            string top = Simulated ?
                "c:/Users/Blumzi/Documents/Visual Studio 2015/Projects/Wise40" :
                "c:/Users/mizpe/source/repos/ASCOM.Wise40";

            switch (Name)
            {
                case "ascom":
                    ret = Const.wiseASCOMServerPath;
                    break;

                case "weatherlink":
                    ret = Const.wiseWeatherLinkPath;
                    break;

                case "dash":
                    ret = top + "/Dash/bin/x86/Debug/Dash.exe";
                    break;

                case "obsmon":
                    ret = top + "/ObservatoryMonitor/bin/x86/Debug/ObservatoryMonitor.exe";
                    break;

                default:
                    break;
            }
            return ret;
        }

        public Watcher(string name)
        {
            string logDir = string.Format(Const.topWise40Directory + "Logs/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(logDir);
            _logFile = logDir + "/" + serviceName + ".log";

            Name = name;
            _path = applicationPath();
        }

        public void watcher()
        {
            int pid;

            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_path, out pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    log("Start: waiting for pid {0} ({1}) ...", pid, _path);
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

        public void Start(string[] args)
        {
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
            _process.Kill(); ;
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
