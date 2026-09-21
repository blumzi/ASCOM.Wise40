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

        // Kept so the unsubscribe below removes the SAME delegate instance that was added -
        //  Guarded.Event returns a new closure each call, so "-= Guarded.Event(...)" would
        //  silently remove nothing and leave the handler attached across Close().
        private EventHandler _exitHandler;
        private bool _stopping = false;

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

        //
        // THIS CALLBACK KILLED THE SERVICE SIX TIMES.
        //
        // Exited is raised on a thread-pool thread, and an exception escaping a thread-pool
        //  callback terminates the process.  There was no try/catch here, and the body reads
        //  p.Id, p.ExitCode and p.ExitTime - every one of which throws InvalidOperationException
        //  once the Process object has been Closed by the worker loop below:
        //
        //      System.InvalidOperationException
        //         at System.Diagnostics.Process.get_Id()
        //         at Wise40Watcher.Watcher.OnExit(Object, EventArgs)
        //         at System.Diagnostics.Process.OnExited()
        //
        // Logged by Windows as "The Wise40Watcher service terminated unexpectedly. It has done
        //  this 6 time(s)."  The cost is not a lost log line: the watcher is what restarts a
        //  dead Dash or RemoteServer and what stops them all when the service stops.  On
        //  2026-09-21 it died at 03:59:15 and the four children ran orphaned until 10:35 -
        //  6.6 hours, through the end of the night, with nothing supervising the observatory.
        //  A deploy in that state found nothing to stop and had to kill the children itself.
        //
        // So: everything guarded, and the catch logs rather than rethrows.  A watcher that
        //  cannot describe an exit is a nuisance; a watcher that is not running is an outage.
        //
        private static void OnExit(object sender, System.EventArgs e)
        {
            try
            {
                if (sender is Process p)
                    Wise40Watcher.Log($"OnExit: Process {p.Id} has exited with {p.ExitCode} at {p.ExitTime}");
            }
            catch (Exception ex)
            {
                try { Wise40Watcher.Log($"OnExit: caught {ex.GetType().Name}: {ex.Message}"); } catch { }
            }
        }

        public void Worker()
        {
            while (!_stopping)
            {
                //
                // The whole body is guarded.  This runs on its own thread, so anything that
                //  escapes takes the service down with it - and there are several candidates
                //  besides the Exited race: GetProcessById throws ArgumentException if the
                //  child dies between being launched and being looked up, and LaunchChildProcess
                //  reaches into another session and can fail for reasons we do not control.
                //
                try
                {
                    CreateProcessAsUserWrapper.LaunchChildProcess(_app.Path, out int pid);
                    if (pid == 0)
                    {
                        //
                        // Launch failed.  Without this pause the loop spins as fast as the CPU
                        //  allows, re-attempting a launch that is probably going to keep failing.
                        //
                        Wise40Watcher.Log($"Worker ({WiseName}): could not launch ({_app.Path}), retrying in 5s ...");
                        Thread.Sleep(5000);
                        continue;
                    }

                    _process = Process.GetProcessById(pid);
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): watching over process ({_app.Path}) ...");
                    _process.EnableRaisingEvents = true;

                    //
                    // Guarded as well as detached-before-Close below.  OnExit has its own
                    //  try/catch, but routing through Guarded means the day someone simplifies
                    //  that method the protection does not leave with it.
                    //
                    _exitHandler = Guarded.Event(nameof(OnExit), OnExit);
                    _process.Exited += _exitHandler;
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): waiting for process to exit ({_app.Path}) ...");
                    _process.WaitForExit();
                    Wise40Watcher.Log($"Worker ({WiseName}:[{pid}]): process has exited ({_app.Path}) ...");

                    //
                    // UNSUBSCRIBE BEFORE Close, which is the actual fix rather than the safety net.
                    //
                    // WaitForExit returns as soon as the process ends, but Exited is dispatched
                    //  separately on a thread-pool thread.  Close() then releases the object's
                    //  state, so a callback arriving a moment later found p.Id throwing.  Detaching
                    //  the handler first means the late callback has nothing to run.
                    //
                    _process.Exited -= _exitHandler;
                    _process.Close();
                }
                catch (Exception ex)
                {
                    Wise40Watcher.Log($"Worker ({WiseName}): caught {ex.GetType().Name}: {ex.Message} at\n{ex.StackTrace}");
                    Thread.Sleep(2000);
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
                Thread thread = new Thread(Guarded.Thread(nameof(Worker), Worker));
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
