﻿using System;
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
using ASCOM.Wise40SafeToOperate;
using ASCOM.Wise40.Common;
using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;


namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObsMainForm : Form
    {
        static bool _simulated = WiseObject.Simulated;
        public const int _maxLogItems = 100000;
        static DriverAccess.Telescope wisetelescope = null;
        static WiseSite wisesite = WiseSite.Instance;
        static DriverAccess.Dome wisedome = null;
        static DriverAccess.SafetyMonitor wisesafetooperate = null;
        Version version = new Version(0, 2);
        private static long _shuttingDown = 0;
        static DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(10);
        static public TimeSpan _intervalBetweenChecks;
        static public int _minutesToIdle;
        static TimeSpan _intervalBetweenLogs = _simulated ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(20);
        private static bool telescope_EnslavesDome = false;
        static DateTime _lastLog = DateTime.MinValue;
        static readonly string deltaFromUT = "(UT+" + DateTime.Now.Subtract(DateTime.UtcNow).Hours.ToString() + ")";

        static Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        static Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        static Color safeColor = Statuser.colors[Statuser.Severity.Good];
        static Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        static CancellationTokenSource CTS = new CancellationTokenSource();
        static CancellationToken CT;
        static TimeZoneInfo tz = TimeZoneInfo.Local;

        TelescopeDigest telescopeDigest;
        SafeToOperateDigest safetooperateDigest;

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
                    wisetelescope = new DriverAccess.Telescope("ASCOM.Remote1.Telescope");
                if (!wisetelescope.Connected)
                {
                    wisetelescope.Connected = true;
                    while (wisetelescope.Connected == false)
                    {
                        Log("Waiting for the \"Telescope\" client to connect ...", 5);
                        Application.DoEvents();
                    }
                }

                if (!buttonShutdown.Enabled)
                    buttonShutdown.Enabled = true;

                if (wisesafetooperate == null)
                    wisesafetooperate = new DriverAccess.SafetyMonitor("ASCOM.Remote1.SafetyMonitor");      // Must match ASCOM Remote Server Setup
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

                if (!buttonProjector.Enabled)
                    buttonProjector.Enabled = true;
            }
            catch (Exception ex)
            {
                Log(string.Format("Exception[0]: {0}", ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }
        }

        public ObsMainForm()
        {
            InitializeComponent();
            listBoxLog.SelectionMode = SelectionMode.None;
            ReadProfile();

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
            if (ShuttingDown)
                return;

            _nextCheck = DateTime.Now + _intervalBetweenChecks;

            Process[] ascomServer = Process.GetProcessesByName(Const.wiseASCOMServerAppName);
            if (ascomServer.Count() == 0)
            {
                Log(string.Format("No active {0}", Const.wiseASCOMServerAppName), 20);
                return;
            }

            CheckConnections();

            telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));
            safetooperateDigest = JsonConvert.DeserializeObject<Wise40SafeToOperate.SafeToOperateDigest>(wisesafetooperate.Action("status", ""));

            bool safe = false;

            UpdateConditionsBypassToolStripMenuItem(safetooperateDigest.Bypassed);

            if (safetooperateDigest.Ready)
                safe = safetooperateDigest.Safe;
            if (safetooperateDigest.Bypassed)
                safe = true;

            string reasons = string.Empty;
            if (safetooperateDigest.ComputerControlIsSafe)
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

            if (telescopeDigest.Active)
            {
                labelActivity.Text = "Active";
                labelActivity.ForeColor = safeColor;
                toolTip.SetToolTip(labelActivity, string.Join(",", telescopeDigest.Activities));
            }
            else
            {
                labelActivity.Text = "Idle";
                labelActivity.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelActivity, "");
            }

            UpdateProjectorControls();

            if (ShuttingDown)
                return;

            List<string> reasonsList = new List<string>();

            if (safetooperateDigest.Ready && !safe)
                reasonsList.Add(wisesafetooperate.Action("unsafereasons", ""));
            if (!telescopeDigest.Active)
                reasonsList.Add("Telescope is Idle");

            if (reasonsList.Count != 0)
            {
                string reason = String.Join(" and ", reasonsList);

                if (ObservatoryIsLogicallyParked && ObservatoryIsPhysicallyParked)
                {
                    Log("Wise40 already parked.");
                }
                else
                {
                    if (safetooperateDigest.ComputerControlIsSafe)
                        DoShutdownObservatory(reason);
                    else
                        Log("No ComputerControl, shutdown skipped.");
                }
            }
            else
            {
                string safetyMessage = "", activityMessage = "";

                if (!safetooperateDigest.HumanInterventionIsSafe)
                    safetyMessage = "Not safe (intervention)";
                else if (safetooperateDigest.Bypassed)
                    safetyMessage = "Not safe (but bypassed)";
                else if (!safetooperateDigest.Ready)
                    safetyMessage = "Not safe (inconclusive safety info)";
                else if (safe)
                    safetyMessage = "Safe";
                else
                    safetyMessage = "Not safe (conditions)";

                if (telescopeDigest.Active)
                    activityMessage = "active (" + string.Join(", ", telescopeDigest.Activities) + ")";
                else
                    activityMessage = "idle";
                
                Log(safetyMessage + " and " + activityMessage);
            }
        }

        void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy");
            labelTime.Text = localTime.ToString("hh:mm:ss tt " + deltaFromUT);
            DateTime now = DateTime.Now;

            if (ShuttingDown)
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
                TimeSpan remaining = _nextCheck.Subtract(now);
                if (remaining.Minutes > 0)
                    s += string.Format("{0:D2}m", remaining.Minutes);
                s += string.Format("{0:D2}s", remaining.Seconds);
                labelNextCheck.Text = s;

                buttonShutdown.Text = "Shutdown Now";
                toolTip.SetToolTip(buttonShutdown, "Close shutter\nStop activities\nPark equipment");
            }

            buttonManualIntervention.Enabled = !ShuttingDown;
            updateManualInterventionControls();

            if (now >= _nextCheck)
                CheckSituation();

            if (!ShuttingDown)
                UpdateConditionsControls();
        }

        private void UpdateConditionsControls()
        {
            if (wisesafetooperate == null)
                return;

            string text = string.Empty, tip = string.Empty;
            string reasons = wisesafetooperate.Action("unsafereasons", string.Empty);
            Color color = normalColor;
            
            if (!safetooperateDigest.HumanInterventionIsSafe)
            {
                text = "Intervention";
                color = unsafeColor;
                tip = HumanIntervention.Info.Replace(Const.recordSeparator, "\n  ");
            }
            else if (safetooperateDigest.Bypassed)
            {
                text = "Bypassed";
                color = warningColor;
                tip = "Manually bypassed (from Settings)";
            }
            else if (!safetooperateDigest.Ready)
            {
                if (reasons.Contains("stabilizing"))
                {
                    text = "Stabilizing";
                    color = warningColor;
                    tip = "Waiting for data to stabilize";
                }
                else
                {
                    text = "Not ready";
                    color = warningColor;
                    tip = "Not enough safety information yet";
                }
            }
            else if (!safetooperateDigest.Safe)
            {
                text = "Not safe";
                color = unsafeColor;
                tip = reasons.Replace(',', '\n');
                tip = tip.Replace('|', '\n');
            }
            else
            {
                text = "Safe";
                color = safeColor;
            }

            labelConditions.Text = text;
            labelConditions.ForeColor = color;
            toolTip.SetToolTip(labelConditions, tip);

            UpdateConditionsBypassToolStripMenuItem(safetooperateDigest.Bypassed);
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

        private void Log(string msg, int afterSecs = 0)
        {
            if (afterSecs != 0 && DateTime.Now.CompareTo(_lastLog) < 0)
                return;

            string line = string.Format("{0} - {1}", DateTime.UtcNow.ToString("H:mm:ss UT"), msg);

            string dailyDir = Common.Debugger.LogDirectory();
            Directory.CreateDirectory(dailyDir);
            string logFile = dailyDir + "/ObservatoryMonitor.txt";

            try
            {
                using (StreamWriter sw = File.Exists(logFile) ?
                            File.AppendText(logFile) :
                            File.CreateText(logFile))
                {
                    sw.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                ;
            }

            logToGUI(line);
            _lastLog = DateTime.UtcNow;
        }

        public void logToGUI(string line)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (listBoxLog.InvokeRequired)
            {
                this.Invoke(new LogDelegate(logToGUI), new object[] { line });
            }
            else
            {
                if (listBoxLog.Items.Count > _maxLogItems)
                    listBoxLog.Items.RemoveAt(0);
                listBoxLog.Items.Add(line);

                int visibleItems = listBoxLog.ClientSize.Height / listBoxLog.ItemHeight;
                listBoxLog.TopIndex = Math.Max(listBoxLog.Items.Count - visibleItems + 1, 0);
            }
        }

        private void AbortShutdown()
        {
            CTS.Cancel();
            CTS = new CancellationTokenSource();
        }

        private void DoShutdownObservatory(string reason)
        {
            if (ObservatoryIsLogicallyParked && ObservatoryIsPhysicallyParked)
            {
                Log(string.Format("Observatory is logically and physically parked. Ignoring \"{0}\".", reason));
                return;
            }

            CT = CTS.Token;

            ShuttingDown = true;
            Task.Run(() =>
            {
                try
                {
                    //Log(string.Format("Started Wise40 shutdown (reason: {0})...", reason));
                    ShutdownObservatory(reason);
                }
                catch (Exception ex)
                {
                    Log(string.Format("Exception[1]: {0}", ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                }
            }, CT).ContinueWith((x) => {
                ShuttingDown = false;
                //Log(string.Format("Completed Wise40 shutdown (reason: {0})...", reason));
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        bool ShuttingDown
        {
            get
            {
                return Interlocked.Read(ref _shuttingDown) == 1;
            }

            set
            {
                Interlocked.Exchange(ref _shuttingDown, value ? 1 : 0);
            }
        }

        private static void SleepWhileProcessingEvents()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            while (sw.ElapsedMilliseconds < _intervalBetweenLogs.TotalMilliseconds)
                Application.DoEvents();
            sw.Stop();
        }

        private bool ObservatoryIsPhysicallyParked
        {
            get
            {
                return Convert.ToBoolean(wisetelescope.Action("nearly-parked", ""));
            }
        }

        private bool ObservatoryIsLogicallyParked
        {
            get
            {
                return wisetelescope.AtPark && wisedome.AtPark && (wisedome.ShutterStatus == ShutterState.shutterClosed);
            }
        }

        private void ShutdownObservatory(string reason)
        {
            DomeDigest domeDigest;

            try
            {
                Angle domeAz = DomeAzimuth; // Possibly force dome calibration

                if (!wisetelescope.AtPark || !ObservatoryIsPhysicallyParked)
                {
                    if (CT.IsCancellationRequested) throw new Exception("Shutdown aborted");

                    Log("   Starting Wise40 park ...");

                    Log(string.Format("    Parking telescope at {0} {1} and dome at {2} ...",
                        wisesite.LocalSiderealTime,
                        (new Angle(66, Angle.Type.Dec)).ToString(),
                        (new Angle(90, Angle.Type.Az).ToNiceString())));

                    Task telescopeShutdownTask = Task.Run(() =>
                    {
                        try
                        {
                            if (wisetelescope.Action("shutdown", "") != "ok")
                                throw new OperationCanceledException("Action(\"telescope:shutdown\") did not reply with \"ok\"");
                        }
                        catch (Exception ex)
                        {
                            Log(string.Format("Exception[2]: {0}", ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                        }
                    }, CT);

                    ShutterState shutterState;
                    List<string> activities;

                    do
                    {
                        if (CT.IsCancellationRequested)
                        {
                            telescopeShutdownTask.Dispose();
                            throw new Exception("Shutdown aborted");
                        }
                        SleepWhileProcessingEvents();
                        if (CT.IsCancellationRequested)
                        {
                            telescopeShutdownTask.Dispose();
                            throw new Exception("Shutdown aborted");
                        }

                        telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));
                        domeDigest = JsonConvert.DeserializeObject<DomeDigest>(wisedome.Action("status", ""));

                        Angle ra, dec, az;
                        ra = Angle.FromHours(telescopeDigest.Current.RightAscension, Angle.Type.RA);
                        dec = Angle.FromDegrees(telescopeDigest.Current.Declination, Angle.Type.Dec);
                        az = Angle.FromDegrees(domeDigest.Azimuth);
                        shutterState = domeDigest.ShutterState;
                        activities = telescopeDigest.Activities;

                        Log(string.Format("    Telescope at {0} {1}, dome at {2}, shutter {3} ...",
                            ra.ToNiceString(),
                            dec.ToNiceString(),
                            az.ToNiceString(),
                            shutterState.ToString().ToLower().Remove(0, "shutter".Length)),
                            _simulated ? 1 : 10);
                    } while (activities.Contains("ShuttingDown"));

                    Log("   Wise40 is parked.");
                }

                if (!telescopeDigest.EnslavesDome)
                {
                    if (!wisedome.AtPark)
                    {
                        Log("    Starting dome park ...");
                        wisedome.Park();
                        do
                        {
                            if (CT.IsCancellationRequested)
                            {
                                wisedome.AbortSlew();
                                throw new Exception("Shutdown aborted");
                            }
                            SleepWhileProcessingEvents();
                            Angle az = DomeAzimuth;
                            Log(string.Format("  Dome at {0} ...", az.ToNiceString()), 10);
                        } while (!wisedome.AtPark);
                        Log("    Dome is parked");
                    }

                    if (wisedome.ShutterStatus != ShutterState.shutterClosed && wisedome.ShutterStatus != ShutterState.shutterClosing)
                    {
                        Log("    Starting shutter close ...");
                        wisedome.CloseShutter();
                        do
                        {
                            if (CT.IsCancellationRequested) throw new Exception("Shutdown aborted");
                            SleepWhileProcessingEvents();
                            Log("    Shutter is closing ...", 10);
                        } while (wisedome.ShutterStatus != ShutterState.shutterClosed);
                        Log("    Shutter is closed.");
                    }
                }

                Log("Wise40 is parked and closed");
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                if (ex.Message == "Shutdown aborted")
                    Log("Shutdown aborted by operator");
                else
                    Log(string.Format("Exception[3] occurred:\n{0}, aborting shutdown!", ex.Message));

                wisetelescope.Action("abort-shutdown", "");
            }
            ShuttingDown = false;
        }

        /// <summary>
        /// If the dome is not calibrated the http transaction may timeout.
        /// Wait till the dome calibrates and returns its Azimuth.
        /// </summary>
        private Angle DomeAzimuth
        {
            get
            {
                double degrees = Double.NaN;

                while (Double.IsNaN(degrees))
                {
                    try
                    {
                        degrees = wisedome.Azimuth;
                    }
                    catch (Exception ex)
                    {
                        Exception inner = ex.InnerException;
                        Log(string.Format("Exception[4]: Waiting for dome Azimuth ({0}) ...",
                            inner != null ? "inner: " + inner.Message : ex.Message));
                    }
                }
                return Angle.FromDegrees(degrees, Angle.Type.Az);
            }
        }

        private void buttonPark_Click(object sender, EventArgs e)
        {
            if (ShuttingDown)
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
                toolTip.SetToolTip(labelHumanInterventionStatus, HumanIntervention.Info.Replace(";", "\n  "));
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

            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                MinutesToIdle = Convert.ToInt32(driverProfile.GetValue(Const.wiseTelescopeDriverID,
                    Const.ProfileName.Telescope_MinutesToIdle, string.Empty, "15"));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                driverProfile.WriteValue(Const.wiseObservatoryMonitorDriverID, "MinutesBetweenChecks",
                    MinutesBetweenChecks.ToString());
            }

            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                driverProfile.WriteValue(Const.wiseTelescopeDriverID,
                    Const.ProfileName.Telescope_MinutesToIdle, MinutesToIdle.ToString());
            }
        }

        public int MinutesBetweenChecks
        {
            get
            {
                return _intervalBetweenChecks.Minutes;
            }

            set
            {
                _intervalBetweenChecks = new TimeSpan(0, value, 0);
            }
        }

        public int MinutesToIdle
        {
            get
            {
                return _minutesToIdle;
            }

            set
            {
                _minutesToIdle = value;
            }
        }

        private void conditionsBypassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wisesafetooperate == null)
                return;

            wisesafetooperate.Action(safetooperateDigest.Bypassed ? "end-bypass" : "start-bypass", string.Empty);
        }

        private void buttonProjector_Click(object sender, EventArgs e) {
            bool status = JsonConvert.DeserializeObject<bool>(wisedome.Action("projector", ""));

            wisedome.Action("projector", (!status).ToString());
        }

        private void UpdateProjectorControls()
        {
            bool status = JsonConvert.DeserializeObject<bool>(wisedome.Action("projector", ""));

            buttonProjector.Text = "Projector " + ((status == true) ? "Off" : "On");
        }
    }
}