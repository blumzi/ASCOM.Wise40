using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

using ASCOM.Utilities;
using ASCOM;
using ASCOM.DriverAccess;
using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;

using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;


namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObsMainForm : Form
    {
        static bool _simulated = new WiseObject().Simulated;
        public const int _maxLogItems = 1000;
        Telescope wisetelescope;
        WiseSite wisesite = WiseSite.Instance;
        DriverAccess.Dome wisedome;
        SafetyMonitor wisesafetooperate;
        Version version = new Version(0, 2);
        private bool _shuttingDown = false;
        DateTime _nextCheck = DateTime.MaxValue;
        TimeSpan _intervalBetweenChecks = _simulated ? new TimeSpan(0, 2, 0) : new TimeSpan(0, 5, 0);
        TimeSpan _intervalBetweenLogs = _simulated ? new TimeSpan(0, 0, 10) : new TimeSpan(0, 0, 20);
        private bool _telescopeEnslavesDome = false;
        DateTime _lastLog;

        Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        Color safeColor = Statuser.colors[Statuser.Severity.Good];

        CancellationTokenSource CTS = new CancellationTokenSource();
        CancellationToken CT;
        Task workerTask;

        public void CloseConnections()
        {
            if (wisetelescope != null)
            {
                if (wisetelescope.Connected)
                    wisetelescope.Connected = false;
                wisetelescope.Dispose();
            }

            if (wisedome != null)
            {
                if (wisedome.Connected)
                    wisedome.Connected = false;
                wisedome.Dispose();
            }

            if (wisesafetooperate != null)
            {
                if (wisesafetooperate.Connected)
                    wisesafetooperate.Connected = false;
                wisesafetooperate.Dispose();
            }
        }

        public void OpenConnections() {
            wisetelescope = new Telescope("ASCOM.Web1.Telescope");
            wisetelescope.Connected = true;

            wisesafetooperate = new SafetyMonitor("ASCOM.Web1.SafetyMonitor");
            wisesafetooperate.Connected = true;

            wisedome = new DriverAccess.Dome("ASCOM.Web1.Dome");
            wisedome.Connected = true;
        }

        public ObsMainForm()
        {
            InitializeComponent();
            listBoxLog.SelectionMode = SelectionMode.None;
            _nextCheck = DateTime.Now.Add(new TimeSpan(0, 0, 10));

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            try
            {
                OpenConnections();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception while connecting to ASCOM drivers:\n\t{0}", ex.Message));
                Application.Exit();
            }

            wisesite.init();
            WiseSite.OpMode opMode = wisesite.OperationalMode;
            switch (opMode)
            {
                case WiseSite.OpMode.LCO:
                case WiseSite.OpMode.WISE:
                    _telescopeEnslavesDome = true;
                    break;
                case WiseSite.OpMode.ACP:
                    _telescopeEnslavesDome = false;
                    break;
            }
            labelOperatingMode.Text = opMode.ToString();
            updateManualInterventionButton();
            UpdateOpModeControls(opMode);
        }

        private void CheckSituation()
        {
            bool active = true, safe = true;

            try
            {
                active = wisetelescope.CommandBool("active", false);
                safe = wisesafetooperate.IsSafe;
            } catch (Exception ex)
            {
                Log(string.Format("Oops: {0}", ex.InnerException.Message));
                _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
                return;
            }

            if (active)
            {
                labelActivity.Text = "Active";
                labelActivity.ForeColor = safeColor;
            }
            else
            {
                labelActivity.Text = "Idle";
                labelActivity.ForeColor = unsafeColor;
            }

            if (safe)
            {
                labelConditions.Text = "Safe";
                labelConditions.ForeColor = safeColor;
                toolTip.SetToolTip(labelConditions, "");
            } else
            {
                labelConditions.Text = "Not safe";
                labelConditions.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelConditions, wisesafetooperate.CommandString("unsafereasons", false).Replace(',', '\n'));
            }

            if (_shuttingDown)
                return;

            if (!(active && safe))
            {
                List<string> reasons = new List<string>();

                if (!active)
                    reasons.Add("Inactive");
                if (!safe)
                    reasons.Add("Not SafeToOperate");

                DoShutdownObservatory(String.Join(" and ", reasons));
            }
            else
                Log("OK");
            _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
        }

        void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy\n hh:mm:ss tt");

            if (DateTime.Now.CompareTo(_nextCheck) >= 0)
            {
                CheckSituation();
            }
            string s = string.Empty;
            TimeSpan remaining = _nextCheck.Subtract(DateTime.Now);
            if (remaining.Minutes > 0)
                s += string.Format("{0:D2}m", remaining.Minutes);
            s += string.Format("{0:D2}s", remaining.Seconds);
            labelNextCheck.Text = s;
            
            if (_shuttingDown)
            {
                buttonPark.Text = "Abort Shutdown";
                toolTip.SetToolTip(buttonPark, "Abort the shutdown procedure");
            } else
            {
                buttonPark.Text = "Shutdown Now";
                toolTip.SetToolTip(buttonPark, "Stop activities\nPark equipment\nClose shutter");
            }

            if (HumanIntervention.IsSet())
            {
                labelConditions.Text = "Not safe";
                labelConditions.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelConditions, HumanIntervention.Info);
            }
            buttonManualIntervention.Enabled = !_shuttingDown;
            updateManualInterventionButton();
        }

        private void timerDisplayRefresh_Tick(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        delegate void LogDelegate(string text);

        public void Log(string msg, int afterSecs = 0)
        {
            if (afterSecs != 0 && DateTime.Now.CompareTo(_lastLog) < 0)
                return;

            DateTime now = DateTime.Now;
            if (now.DayOfYear != _lastLog.DayOfYear)
                log(string.Format("\n=== {0} ===\n", now.ToString("dd MMMM, yyyy")));

            log(string.Format("{0} - {1}", DateTime.Now.ToString("H:mm:ss"), msg));
            _lastLog = DateTime.Now;
        }

        public void log(string line)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (listBoxLog.InvokeRequired)
            {
                LogDelegate _log = new LogDelegate(log);
                this.Invoke(_log, new object[] { line });
            }
            else
            {
                if (listBoxLog.Items.Count > _maxLogItems)
                    listBoxLog.Items.RemoveAt(0);
                listBoxLog.Items.Add(line);

                int visibleItems = listBoxLog.ClientSize.Height / listBoxLog.ItemHeight;
                listBoxLog.TopIndex = Math.Max(listBoxLog.Items.Count - visibleItems + 1, 0);
            }

            string dir = string.Format(Const.topWise40Directory + "Logs/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dir);
            using (var sw = new StreamWriter(dir + "/ObservatoryMonitor.log", true))
            {
                sw.WriteLine(line);
            }
        }

        private void AbortShutdown()
        {
            CTS.Cancel();
            CTS = new CancellationTokenSource();
        }

        private void DoShutdownObservatory(string reason)
        {
            CT = CTS.Token;

            workerTask = Task.Run(() =>
            {
                try
                {
                    ShutdownObservatory(reason);
                }
                catch (Exception ex)
                {
                    Log(string.Format("Exception: {0}", ex.Message));
                }
            }, CT);
            //.ContinueWith((t) =>
            //{
            //    #region debug
            //    debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic,
            //        "pulser \"{0}\" on {1} completed with status: {2}",
            //        t.ToString(), pulserTask._axis.ToString(), t.Status.ToString());
            //    #endregion
            //    Deactivate(pulserTask);
            //}, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void ShutdownObservatory(string reason)
        {
            _shuttingDown = true;
            string header = string.Format("Starting Wise40 shutdown (reason: {0})...", reason);
            string trailer = string.Format("Completed Wise40 shutdown (reason: {0})...", reason);
            bool _headerWasLogged = false;

            try
            {
                string what = string.Format("Telescope{0}", _telescopeEnslavesDome ? " and dome" : "");


                if (wisetelescope.IsPulseGuiding)
                {
                    if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                    do
                    {
                        if (CT.IsCancellationRequested)
                            throw new Exception("Shutdown aborted");

                        Log("    Waiting for PulseGuiding to stop ...");
                        Thread.Sleep(_intervalBetweenLogs);
                    } while (wisetelescope.IsPulseGuiding);
                    Log("    PulseGuiding stopped.");
                }

                if (wisetelescope.Slewing)
                {
                    if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                    wisetelescope.AbortSlew();
                    while (wisetelescope.Slewing)
                    {
                        if (CT.IsCancellationRequested)
                            throw new Exception("Shutdown aborted");
                        Log("    Waiting for Slewing to stop ...", 10);
                        Thread.Sleep(_intervalBetweenLogs);
                    }
                    Log("    Slew aborted.");
                }

                if (wisetelescope.Tracking)
                {
                    if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                    wisetelescope.Tracking = false;
                    Log("    Tracking stopped.");
                }

                if (!wisetelescope.AtPark)
                {
                    if (CT.IsCancellationRequested) throw new Exception("Shutdown aborted");

                    if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                    Log(string.Format("    Starting {0} park ...", what.ToLower()));
                    
                    Task parkerTask = Task.Run(() => wisetelescope.Park(), CT);

                    do
                    {
                        if (CT.IsCancellationRequested)
                        {
                            parkerTask.Dispose();
                            throw new Exception("Shutdown aborted");
                        }
                        Thread.Sleep(_intervalBetweenLogs);
                        if (CT.IsCancellationRequested)
                        {
                            parkerTask.Dispose();
                            throw new Exception("Shutdown aborted");
                        }
                        Angle ra, dec, az;
                        ra = Angle.FromDegrees(wisetelescope.RightAscension, Angle.Type.RA);
                        dec = Angle.FromDegrees(wisetelescope.Declination, Angle.Type.Dec);
                        az = Angle.FromDegrees(wisedome.Azimuth, Angle.Type.Az);
                        Log(string.Format("    Telescope at {0}, {1} dome at {2}...",
                            ra.ToNiceString(),
                            dec.ToNiceString(),
                            az.ToNiceString()),
                            _simulated ? 1 : 10);
                    } while (!wisetelescope.AtPark);
                    Log(string.Format("    {0} parked.", what));
                }

                if (!_telescopeEnslavesDome)
                {
                    if (!wisedome.AtPark)
                    {
                        if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                        Log("    Starting dome park ...");
                        wisedome.Park();
                        do
                        {
                            if (CT.IsCancellationRequested)
                            {
                                wisedome.AbortSlew();
                                throw new Exception("Shutdown aborted");
                            }
                            Thread.Sleep(_intervalBetweenLogs);
                            Angle az = Angle.FromDegrees(wisedome.Azimuth, Angle.Type.Az);
                            Log(string.Format("  Dome is parking, now at {0} ...", az.ToNiceString()), 10);
                        } while (!wisedome.AtPark);
                        Log("    Dome is parked");
                    }
                }

                if (wisedome.ShutterStatus != ShutterState.shutterClosed && wisedome.ShutterStatus != ShutterState.shutterClosing)
                {
                    if (!_headerWasLogged) { Log(header); _headerWasLogged = true; }
                    Log("    Starting shutter close ...");
                    wisedome.CloseShutter();
                    do
                    {
                        if (CT.IsCancellationRequested) throw new Exception("Shutdown aborted");
                        Thread.Sleep(_intervalBetweenLogs);                        
                        Log("    Shutter is closing ...", 10);
                    } while (wisedome.ShutterStatus != ShutterState.shutterClosed);
                    Log("    Shutter is closed.");
                }

                if (_headerWasLogged)
                    Log(trailer);
                else
                    Log("Wise40 is parked and closed");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Exception occurred while Shutting Down:\n{0}", ex.Message));
                _shuttingDown = false;
            }
            _shuttingDown = false;
        }

        private void buttonPark_Click(object sender, EventArgs e)
        {
            if (_shuttingDown)
                AbortShutdown();
            else
                DoShutdownObservatory("Manual Shutdown");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ObservatoryMonitorAboutForm(version).Show();
        }

        private void updateManualInterventionButton()
        {
            buttonManualIntervention.Text = HumanIntervention.IsSet() ?
                "Remove Operator Intervention\n\n(make observatory safe to\noperate)" :
                "Create Operator Intervention\n\n(make observatory unsafe to\noperate)";
        }

        private void buttonManualIntervention_Click(object sender, EventArgs e)
        {
            if (HumanIntervention.IsSet())
            {
                HumanIntervention.Remove();
                Log("Removed operator intervention.");
            }
            else
            {
                DialogResult result = new InterventionForm().ShowDialog();
                if (result == DialogResult.OK)
                    Log("Created operator intervention");
            }

            updateManualInterventionButton();
            CheckSituation();
        }

        public class App
        {
            public string _name;
            Process _process = null;
            string _path;

            public App(string name, string path)
            {
                _path = path;
                _name = name;
                Process[] processes = Process.GetProcessesByName(name);

                if (processes.Length > 0)
                {
                    _process = processes[0];
                }
                else
                    throw new Exception(string.Format("Cannot find process for \"{0}\"", name));
            }

            public bool IsRunning
            {
                get
                {
                    return _process != null;
                }
            }

            public void Restart()
            {
                if (IsRunning)
                    _process.Kill();
                if (_path != null)
                {
                    Thread.Sleep(5000);
                    Process.Start(_path);
                }
            }
        };

        private bool RestartApps(WiseSite.OpMode newMode)
        {
            string message = string.Format("\nYou are about to change the Wise40 operational\n   mode from \"{0}\" to \"{1}\" !\n\n",
                wisesite.OperationalMode, newMode);

            List<App> apps = new List<App>();
            try
            {
                apps.Add(new App("ASCOM.RemoteDeviceServer", Const.wiseASCOMServerPath));
                if (newMode == WiseSite.OpMode.WISE)
                    apps.Add(new App(Const.wiseDashboardAppName,
                        _simulated ? Const.wiseSimulatedDashPath : Const.wiseRealDashPath));
            } catch (Exception ex) { }
            
            if (apps.Count > 0)
            {
                message += "The following applications will be restarted:\n";
                foreach (var app in apps)
                    if (app.IsRunning)
                        message += "    " + app._name + '\n';
            }
            message += "\nPlease confirm or cancel!";

            if (MessageBox.Show(message, "Change the Wise40 Operation Mode", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                CloseConnections();
                wisesite.OperationalMode = newMode;
                foreach (var app in apps)
                    app.Restart();
                OpenConnections();
                return true;
            }
            return false;
        }
        
        private void SelectOpMode(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedItem = sender as ToolStripMenuItem;
            WiseSite.OpMode mode = (selectedItem == wISEToolStripMenuItem) ? WiseSite.OpMode.WISE :
                (selectedItem == lCOToolStripMenuItem) ? WiseSite.OpMode.LCO : WiseSite.OpMode.ACP;

            if (RestartApps(mode))
                UpdateOpModeControls(mode);
        }

        private void UpdateOpModeControls(WiseSite.OpMode mode)
        {
            List<ToolStripMenuItem> items = new List<ToolStripMenuItem>() {
                wISEToolStripMenuItem,
                lCOToolStripMenuItem,
                aCPToolStripMenuItem,
            };

            foreach (var item in items)
                if (item.Text.EndsWith(Const.checkmark))
                    item.Text = item.Text.Substring(0, item.Text.Length - Const.checkmark.Length);

            ToolStripMenuItem selected = null;
            switch (mode)
            {
                case WiseSite.OpMode.LCO: selected = lCOToolStripMenuItem; break;
                case WiseSite.OpMode.ACP: selected = aCPToolStripMenuItem; break;
                case WiseSite.OpMode.WISE: selected = wISEToolStripMenuItem; break;
            }
            if (selected != null)
                selected.Text += Const.checkmark;
            labelOperatingMode.Text = mode.ToString();
        }
    }
}