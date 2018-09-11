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
        Telescope wisetelescope = null;
        WiseSite wisesite = WiseSite.Instance;
        DriverAccess.Dome wisedome = null;
        SafetyMonitor wisesafetooperate = null;
        Version version = new Version(0, 2);
        private bool _shuttingDown = false;
        DateTime _nextCheck = DateTime.MaxValue;
        public TimeSpan _intervalBetweenChecks;
        TimeSpan _intervalBetweenLogs = _simulated ? new TimeSpan(0, 0, 10) : new TimeSpan(0, 0, 20);
        private bool _telescopeEnslavesDome = false;
        DateTime _lastLog;

        Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        Color safeColor = Statuser.colors[Statuser.Severity.Good];
        Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        CancellationTokenSource CTS = new CancellationTokenSource();
        CancellationToken CT;
        Task workerTask;
        string safeToOperateStatus = string.Empty;

        public void CloseConnections()
        {
            if (wisetelescope != null)
            {
                if (wisetelescope.Connected)
                    wisetelescope.Connected = false;
                wisetelescope.Dispose();
                wisetelescope = null;
            }

            if (wisedome != null)
            {
                if (wisedome.Connected)
                    wisedome.Connected = false;
                wisedome.Dispose();
                wisedome = null;
            }

            if (wisesafetooperate != null)
            {
                if (wisesafetooperate.Connected)
                    wisesafetooperate.Connected = false;
                wisesafetooperate.Dispose();
                wisesafetooperate = null;
            }
        }

        public void CheckConnections() {

            try
            {
                if (wisetelescope == null)
                    wisetelescope = new Telescope("ASCOM.Remote1.Telescope");
                if (!wisetelescope.Connected)
                {
                    wisetelescope.Connected = true;
                    while (wisetelescope.Connected == false)
                    {
                        Log("Waiting for the \"Telescope\" client to connect ...", 5);
                        Application.DoEvents();
                    }
                }

                if (wisesafetooperate == null)
                    wisesafetooperate = new SafetyMonitor("ASCOM.Remote1.SafetyMonitor");      // Must match ASCOM Remote Server Setup
                if (!wisesafetooperate.Connected)
                {
                    wisesafetooperate.Connected = true;
                    while (!wisesafetooperate.Connected)
                    {
                        Log("Waiting for the \"SafeToOperate\" client to connect ...", 5);
                        Application.DoEvents();
                    }
                }

                if (wisedome == null)
                    wisedome = new DriverAccess.Dome("ASCOM.Remote1.Dome");
                if (!wisedome.Connected)
                {
                    wisedome.Connected = true;
                    while (!wisedome.Connected)
                    {
                        Log("Waiting for the \"Dome\" client to connect", 5);
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(string.Format("Exception: {0}", ex.Message));
            }
        }

        public ObsMainForm()
        {
            InitializeComponent();
            listBoxLog.SelectionMode = SelectionMode.None;
            ReadProfile();
            _nextCheck = DateTime.Now.Add(new TimeSpan(0, 0, 10));

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            updateManualInterventionControls();
            UpdateOpModeControls();
            
            conditionsBypassToolStripMenuItem.Text = "";
            conditionsBypassToolStripMenuItem.Enabled = false;
        }

        private void UpdateConditionsBypassToolStripMenuItem(bool bypassed)
        {
            if (!conditionsBypassToolStripMenuItem.Enabled)
                conditionsBypassToolStripMenuItem.Enabled = true;

            conditionsBypassToolStripMenuItem.Text = "Bypass safety";
            if (bypassed)
                conditionsBypassToolStripMenuItem.Text += Const.checkmark;
        }

        private void CheckSituation()
        {
            bool active = true, inControl = false, ready = false, safe = true, bypassed = false;

            Process[] ascomServer = Process.GetProcessesByName(Const.wiseASCOMServerAppName);
            if (ascomServer.Count() == 0)
            {
                Log(string.Format("No active {0}", Const.wiseASCOMServerAppName), 20);
                _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
                return;
            }
            
            CheckConnections();

            try
            {
                active = Convert.ToBoolean(wisetelescope.Action("telescope:get-active", ""));
                _telescopeEnslavesDome = Convert.ToBoolean(wisetelescope.Action("dome:enslaved", string.Empty));

                safeToOperateStatus = wisesafetooperate.Action("status", string.Empty);
                ready = safeToOperateStatus.Contains("ready:false") ? false : true;
                bypassed = safeToOperateStatus.Contains("bypassed:false") ? false : true;
                UpdateConditionsBypassToolStripMenuItem(bypassed);

                if (ready)
                    safe = safeToOperateStatus.Contains("safe:false") ? false : true;
                if (bypassed)
                    safe = true;

                inControl = safeToOperateStatus.Contains("computer-control:false") ? false : true;
            } catch (Exception ex)
            {
                Log(string.Format("Oops: {0}", ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
                return;
            }

            string reasons = string.Empty;
            if (inControl)
            {
                labelComputerControl.Text = "Operational";
                labelComputerControl.ForeColor = safeColor;
            } else
            {
                labelComputerControl.Text = "Maintenance";
                labelComputerControl.ForeColor = unsafeColor;
                reasons = wisesafetooperate.Action("unsafereasons", "");
            }
            toolTip.SetToolTip(labelComputerControl, reasons.Replace(',', '\n'));

            if (active)
            {
                labelActivity.Text = "Active";
                labelActivity.ForeColor = safeColor;
                toolTip.SetToolTip(labelActivity, wisetelescope.Action("telescope:get-activities", ""));
            }
            else
            {
                labelActivity.Text = "Idle";
                labelActivity.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelActivity, "");
            }
            
            if (_shuttingDown)
                return;

            List<string> reasonsList = new List<string>();

            if (ready && !safe)
                reasonsList.Add(wisesafetooperate.Action("unsafereasons", ""));
            if (!active)
                reasonsList.Add("Telescope is Idle");

            if (reasonsList.Count != 0)
            {
                string reason = String.Join(" and ", reasonsList);
                if (inControl)
                    DoShutdownObservatory(reason);
                else
                    Log(string.Format("No ComputerControl, shutdown (reason: {0}) skipped.", reason));
            }
            else
                Log("OK");
            
            _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
        }

        void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy, HH:mm:ss");

            if (DateTime.Now.CompareTo(_nextCheck) >= 0)
            {
                CheckSituation();
            }

            if (_shuttingDown)
            {
                labelNextCheck.Visible = false;
                labelNextCheckLabel.Visible = false;
                buttonShutdown.Text = "Abort Shutdown";
                toolTip.SetToolTip(buttonShutdown, "Abort the shutdown procedure");
            }
            else
            {
                labelNextCheck.Visible = true;
                labelNextCheckLabel.Visible = true;
                string s = string.Empty;
                TimeSpan remaining = _nextCheck.Subtract(DateTime.Now);
                if (remaining.Minutes > 0)
                    s += string.Format("{0:D2}m", remaining.Minutes);
                s += string.Format("{0:D2}s", remaining.Seconds);
                labelNextCheck.Text = s;

                buttonShutdown.Text = "Shutdown Now";
                toolTip.SetToolTip(buttonShutdown, "Stop activities\nPark equipment\nClose shutter");
            }

            buttonManualIntervention.Enabled = !_shuttingDown;
            updateManualInterventionControls();
            UpdateConditionsControls();
        }

        private void UpdateConditionsControls()
        {
            if (wisesafetooperate == null)
                return;

            string safety = null;

            try
            {
                safety = wisesafetooperate.Action("status", string.Empty);
            } catch (Exception ex)
            {
                return;
            }

            bool bypassed = safety.Contains("bypassed:false") ? false : true;
            bool ready = safety.Contains("ready:false") ? false : true;
            bool safe = safety.Contains("safe:false") ? false : true;
            bool intervention = HumanIntervention.IsSet();
            string text = string.Empty, tip = string.Empty;
            Color color = normalColor;
            
            if (intervention)
            {
                text = "Intervention";
                color = warningColor;
                tip = HumanIntervention.Info.Replace(";", "\n  ");
            }
            else if (bypassed)
            {
                text = "Bypassed";
                color = warningColor;
                tip = "Manually bypassed";
            }
            else if (!ready)
            {
                text = "Not ready";
                color = normalColor;
                tip = "Not enough safety information yet";
            }
            else if (!safe)
            {
                text = "Not safe";
                color = unsafeColor;
                tip = wisesafetooperate.Action("unsafereasons", string.Empty).Replace(',', '\n');
            }
            else
            {
                text = "Safe";
                color = safeColor;
            }

            labelConditions.Text = text;
            labelConditions.ForeColor = color;
            toolTip.SetToolTip(labelConditions, tip);

            UpdateConditionsBypassToolStripMenuItem(bypassed);
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
        }

        private void ShutdownObservatory(string reason)
        {
            _shuttingDown = true;
            _nextCheck = DateTime.MinValue;
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

                    Task parkerTask = Task.Run(() => {
                        try
                        {
                            wisetelescope.Action("telescope:shutdown", "");
                        }
                        catch (Exception ex)
                        {
                            Log(string.Format("Exception: {0}", ex.Message));
                        }
                    }, CT);

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
                        Log(string.Format("    Telescope at {0}, {1} (=> {2}, {3}), dome at {4}...",
                            ra.ToNiceString(),
                            dec.ToNiceString(),
                            Angle.FromHours(wisetelescope.TargetRightAscension, Angle.Type.RA),
                            Angle.FromDegrees(wisetelescope.TargetDeclination, Angle.Type.Dec),
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
                Log(string.Format("Exception occurred:\n{0}, aborting shutdown!", ex.Message));
                _shuttingDown = false;
            }
            _shuttingDown = false;
            _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
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

        private void updateManualInterventionControls()
        {
            if (HumanIntervention.IsSet()) {
                labelHumanInterventionStatus.Text = "Active";
                labelHumanInterventionStatus.ForeColor = unsafeColor;
                buttonManualIntervention.Text = "Deactivate";
                toolTip.SetToolTip(labelHumanInterventionStatus, HumanIntervention.Info);
            } else
            {
                labelHumanInterventionStatus.Text = "Inactive";
                buttonManualIntervention.Text = "Activate";
                labelHumanInterventionStatus.ForeColor = safeColor;
                toolTip.SetToolTip(labelHumanInterventionStatus, "");
            }
        }

        private void removeHumanInterventionFile(object sender, DoWorkEventArgs e)
        {
            HumanIntervention.Remove();
        }

        private void afterRemoveHumanInterventionFile(object sender, RunWorkerCompletedEventArgs e)
        {
            Log("Removed operator intervention.");
        }

        private void buttonManualIntervention_Click(object sender, EventArgs e)
        {
            if (HumanIntervention.IsSet())
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(removeHumanInterventionFile);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(afterRemoveHumanInterventionFile);
                bw.RunWorkerAsync();
            }
            else
            {
                DialogResult result = new InterventionForm().ShowDialog();
                if (result == DialogResult.OK)
                    Log("Created operator intervention");
            }

            updateManualInterventionControls();
            CheckSituation();
        }
        
        private void SelectOpMode(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedItem = sender as ToolStripMenuItem;
            WiseSite.OpMode mode = (selectedItem == wISEToolStripMenuItem) ? WiseSite.OpMode.WISE :
                (selectedItem == lCOToolStripMenuItem) ? WiseSite.OpMode.LCO : WiseSite.OpMode.ACP;
            
            wisesite.OperationalMode = mode;

            UpdateOpModeControls();
            CloseConnections();
            KillWise40Apps();
        }

        private void KillWise40Apps()
        {
            foreach (var proc in Process.GetProcessesByName("ASCOM.RESTServer"))
                proc.Kill();

            foreach (var proc in Process.GetProcessesByName("Dash"))
                proc.Kill();
        }

        private void UpdateOpModeControls()
        {
            WiseSite.OpMode currentMode = wisesite.OperationalMode;

            List<ToolStripMenuItem> items = new List<ToolStripMenuItem>() {
                wISEToolStripMenuItem,
                lCOToolStripMenuItem,
                aCPToolStripMenuItem,
            };

            foreach (var item in items)
                if (item.Text.EndsWith(Const.checkmark))
                    item.Text = item.Text.Substring(0, item.Text.Length - Const.checkmark.Length);

            ToolStripMenuItem selected = null;
            switch (currentMode)
            {
                case WiseSite.OpMode.LCO: selected = lCOToolStripMenuItem; break;
                case WiseSite.OpMode.ACP: selected = aCPToolStripMenuItem; break;
                case WiseSite.OpMode.WISE: selected = wISEToolStripMenuItem; break;
            }
            if (selected != null)
                selected.Text += Const.checkmark;
            labelOperatingMode.Text = currentMode.ToString();
        }

        private void setupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ObservatoryMonitorSetupDialogForm(this).Show();
        }

        public void ReadProfile()
        {            
            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                driverProfile.Register(Const.wiseObservatoryMonitorDriverID, "Wise40 ObservatoryMonitor");

                int minutes = Convert.ToInt32(driverProfile.GetValue(Const.wiseObservatoryMonitorDriverID,
                    "MinutesBetweenChecks", string.Empty, "5"));

                _intervalBetweenChecks = new TimeSpan(0, minutes, 0);
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                driverProfile.WriteValue(Const.wiseObservatoryMonitorDriverID, "MinutesBetweenChecks", Minutes.ToString());
            }
        }

        public int Minutes
        {
            get
            {
                return _intervalBetweenChecks.Minutes;
            }

            set
            {
                _intervalBetweenChecks = new TimeSpan(0, value, 0);
                _nextCheck = DateTime.Now.Add(_intervalBetweenChecks);
            }
        }

        private void conditionsBypassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wisesafetooperate == null)
                return;

            safeToOperateStatus = wisesafetooperate.Action("status", string.Empty);
            bool currentlyBypassed = safeToOperateStatus.Contains("bypassed:false") ? false : true;

            wisesafetooperate.Action(currentlyBypassed ? "end-bypass" : "start-bypass", string.Empty);
        }

        private void ObsMainForm_Load(object sender, EventArgs e)
        {

        }
    }
}