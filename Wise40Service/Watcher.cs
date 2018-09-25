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
    public class Watcher: WiseObject
    {
        private string _path;
        private static string _logFile;
        Process _process = null;
        bool _stopping = false;

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
            log("Watcher[{0}]: path {1}", Name, ret);
            return ret;
        }

        public Watcher(string name)
        {
            string logDir = string.Format(Const.topWise40Directory + "Logs/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(logDir);
            _logFile = logDir + "/Wise40Watcher.log";

            Name = name;
            _path = applicationPath();
        }

        public void func()
        {
            int pid;

            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_path, out pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);                    
                    _process.WaitForExit();
                }
            }
        }

        public void Start(string[] args)
        {
            try
            {
                Thread thread = new Thread(func);
                thread.Start();
            }
            catch (Exception ex)
            {
                log("Watcher[{0}]: Thread start: Exception: {1}", Name, ex.Message);
                return;
            }
        }

        public void Stop()
        {
            _stopping = true;
            log("Watcher[{0}]: The service was Stopped, killing process {1} ...", Name, _process.Id);
            _process.Kill(); ;
        }

        public static void log(string fmt, params object[] o)
        {
            string msg = string.Format(fmt, o);
            using (var sw = new StreamWriter(_logFile, true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd, HH:mm:ss.ffff ") + msg);
            }
        }
    }
}
