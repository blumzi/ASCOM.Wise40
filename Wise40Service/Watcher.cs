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
using System.Net.Http;
using Newtonsoft.Json;

namespace Wise40Watcher
{
    public class Watcher : WiseObject
    {
        private Const.App _app;
        private Process _process = null;
        private bool _stopping = false;
        private readonly AscomServerFetcher ascomServerFetcher = new AscomServerFetcher();

        private readonly Dictionary<string, Const.Application> _appNameToToken = new Dictionary<string, Const.Application>
        {
            { "ascom", Const.Application.RESTServer },
            { "alpaca", Const.Application.AlpacaClientLocalServer },
            { "weatherlink", Const.Application.WeatherLink },
            { "dash", Const.Application.Dash },
            { "obsmon", Const.Application.ObservatoryMonitor },
            { "safetydash", Const.Application.SafetyDash },
        };

        private void Init(string shortName)
        {
            WiseName = shortName;
            _app = Const.Apps[_appNameToToken[shortName]];
        }

        public Watcher(string shortName)
        {
            string logDir = ASCOM.Wise40.Common.Debugger.LogDirectory();
            Directory.CreateDirectory(logDir);

            Init(shortName);
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

            Wise40Watcher.Log($"OnExit: Process {p.Id} on session {p.SessionId} has exited with {p.ExitCode} at {p.ExitTime}");
        }

        public void Worker()
        {
            while (!_stopping)
            {
                CreateProcessAsUserWrapper.LaunchChildProcess(_app.Path, out int pid);
                if (pid != 0)
                {
                    _process = Process.GetProcessById(pid);
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): watching over process ({_app.Path}) ...");
                    _process.EnableRaisingEvents = true;
                    _process.Exited += new EventHandler(OnExit);
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): waiting for process to exit ({_app.Path}) ...");
                    _process.WaitForExit();
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): process has exited ({_app.Path}) ...");
                    _process.Close();
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
                KillAllProcesses(Const.Apps[Const.Application.AlpacaClientLocalServer].appName);
            }
        }

        public void Start(string[] args, bool waitForResponse = false)
        {
            string op = args.Length != 0 ?
                $"Start({args.ToList()})" :
                 "Start" + $" {WiseName}";

            KillAll();
            const int waitMillis = 1000;

            try
            {
                Thread thread = new Thread(Worker);
                thread.Start();
                Wise40Watcher.Log($"{op}: worker thread started ...");
                if (waitForResponse)
                {
                    do
                    {
                        Wise40Watcher.Log($"{op}: waiting {waitMillis} millis for process to be created ...");
                        Thread.Sleep(waitMillis);
                    } while (_process == null);
                    Wise40Watcher.Log($"{op}:[{_process.Id}]: process was created");

                    do
                    {
                        Wise40Watcher.Log($"{op}:[{_process.Id}]: waiting {waitMillis} millis for process to Respond ...");
                        Thread.Sleep(waitMillis);
                    } while (!_process.Responding);

                    if (_app == Const.Apps[Const.Application.RESTServer])
                    {
                        try
                        {
                            int concurrency = -1; // concurrencyFetcher.Value;
                            Wise40Watcher.Log($"{op}:[{_process.Id}]: concurrency: {concurrency}");
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    Wise40Watcher.Log($"{op}:[{_process.Id}]: process is responding");
                }
            }
            catch (Exception ex)
            {
                Wise40Watcher.Log($"{op} Exception: {ex.Message} at {ex.StackTrace}");
            }
        }

        public void Stop()
        {
            _stopping = true;
            Wise40Watcher.Log($"Stop ({WiseName}:[{_process.Id}]): The service was Stopped, killing process ({_process.ProcessName})...");
            _process.Kill();
            Thread.Sleep(1000);

            KillAll();
        }

        //private bool GetConcurrency()
        //{
        //    int tries;
        //    string op = "GetConcurrency";

        //    for (tries = 0; tries < 10; tries++)
        //    {
        //        try
        //        {
        //            using (HttpRequestMessage httpRequest = new HttpRequestMessage
        //            {
        //                RequestUri = new Uri(Const.RESTServer.top + "concurrency"),
        //                Method = HttpMethod.Get,
        //            })
        //            {
        //                using (HttpResponseMessage response = _client.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead).Result)
        //                {
        //                    using (HttpContent content = response.Content)
        //                    {
        //                        string json = content.ReadAsStringAsync().Result;
        //                        ASCOMResponse ascomResponse = JsonConvert.DeserializeObject<ASCOMResponse>(json);
        //                        #region debug
        //                        Wise40Watcher.Log($"{op}: Succeeded at try #{tries + 1}, concurrency value: {ascomResponse.Value}.");
        //                        #endregion
        //                        return true;      // tries loop
        //                    }
        //                }
        //            }
        //        }
        //        catch (TaskCanceledException ex)
        //        {
        //            if (ex.InnerException?.InnerException is TaskCanceledException)
        //            {
        //                #region debug
        //                Wise40Watcher.Log($"{op}: Timedout: {ex.Message} at\n{ex.StackTrace}");
        //                #endregion
        //            }
        //        }
        //        catch (AggregateException ae)
        //        {
        //            ae.Handle((x) =>
        //            {
        //                if (x is HttpRequestException)
        //                {
        //                    #region debug
        //                    Wise40Watcher.Log($"{op}: HttpRequestException: {x.Message}");
        //                    #endregion
        //                    return true;
        //                }
        //                return false;
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            #region debug
        //            string msg = $"{op}: Caught: {ex.Message}";
        //            if (ex.InnerException != null)
        //                msg += $" from {ex.InnerException}";
        //            msg += $" at {ex.StackTrace}";
        //            Wise40Watcher.Log(msg);
        //            #endregion
        //        }
        //        finally
        //        {
        //            Thread.Sleep(5000);
        //        }
        //    }

        //    return false;
        //}
    }

    public class ASCOMResponse
    {
        public string Value;
        public int ClientTransactionID;
        public int ServerTransactionID;
        public int ErrorNumber;
        public string ErrorMessage;
        public string DriverException;
    }
}
