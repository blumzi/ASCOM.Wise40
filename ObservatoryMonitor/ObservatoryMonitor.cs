using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.Wise40SafeToOperate;
using ASCOM.Wise40.Common;
using Newtonsoft.Json;

using System.Net;
using System.Diagnostics;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObsMainForm : Form
    {
        private static bool _simulated = WiseObject.Simulated;
        public const int _maxLogItems = 100000;
        private static DriverAccess.Telescope wisetelescope = null;
        private static readonly WiseSite wisesite = WiseSite.Instance;
        private static DriverAccess.Dome wisedome = null;
        private static DriverAccess.SafetyMonitor wisesafetooperate = null;
        private readonly Version version = new Version(0, 2);
        private static DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(20);
        private static bool _checking = false;
        static public TimeSpan _intervalBetweenChecks;
        static public int MinutesToIdle { get; set; }
        private static TimeSpan _intervalBetweenLogs = _simulated ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(20);
        private static DateTime _lastLog = DateTime.MinValue;
        private static readonly string deltaFromUT = "(UT+" + DateTime.Now.Subtract(DateTime.UtcNow).Hours.ToString() + ")";

        private static readonly Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        private static readonly Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        private static readonly Color safeColor = Statuser.colors[Statuser.Severity.Good];
        private static readonly Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        private static readonly CancellationTokenSource CTS = new CancellationTokenSource();
        private static CancellationToken CT;

        private TelescopeDigest telescopeDigest;
        private DomeDigest domeDigest;
        private SafeToOperateDigest safetooperateDigest;

        private static readonly Common.Debugger debugger = Common.Debugger.Instance;

        public static bool connected = false;

        public static bool shuttingDown = false;

        public static void CloseConnections()
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
            connected = false;
        }

        public void CheckConnections()
        {
            #region Check connectivity to RemoteServer
            using (var client = new WebClient())
            {
                try
                {
                    UpdateCheckingStatus("connecting ASCOM server");
                    DateTime start = DateTime.Now;

                    client.DownloadDataAsync(new Uri(Const.RESTServer.top + "concurrency")); // GET to http://www.xxx.yyy.zzz/server/v1/concurrency
                    while (client.IsBusy)
                    {
                        if (DateTime.Now.Subtract(start).TotalMilliseconds > 500)
                        {
                            client.CancelAsync();
                            Log("Connecting ASCOM server timed out");
                            connected = false;
                            return;
                        }
                        Application.DoEvents();
                    }
                    connected = true;
                }
                catch (Exception ex)
                {
                    Log($"CheckConnections:Server:Exception: {(ex.InnerException ?? ex).Message}");
                    connected = false;
                    return;
                }
            }
            #endregion

            #region Connect to remote ASCOM Drivers
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

                connected = true;
            }
            catch (Exception ex)
            {
                Log($"CheckConnections:Exception: {(ex.InnerException ?? ex).Message}");
                connected = false;
            }
            #endregion
        }

        public ObsMainForm()
        {
            debugger.Autoflush = true;
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
            #region GetStatus
            try
            {
                UpdateCheckingStatus("telescope status");
                telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));

                UpdateCheckingStatus("safetooperate status");
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wisesafetooperate.Action("status", ""));
            } catch (Exception ex)
            {
                UpdateCheckingStatus("");
                Log($"CheckSituation:Exception: {(ex.InnerException ?? ex).Message}");
                return;
            }
            #endregion

            #region UpdateDisplay

            UpdateConditionsBypassToolStripMenuItem(safetooperateDigest.Bypassed);

            #region ComputerControlLabel
            if (safetooperateDigest.ComputerControl.Safe)
            {
                labelComputerControl.Text = "Operational";
                labelComputerControl.ForeColor = safeColor;
                toolTip.SetToolTip(labelComputerControl, "Computer Control is ON");
            } else
            {
                labelComputerControl.Text = "Maintenance";
                labelComputerControl.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelComputerControl, "Computer Control is OFF");
            }
            #endregion

            #region ActivityLabel
            if (telescopeDigest == null)
            {
                labelActivity.Text = "";
            }
            else if (telescopeDigest.ShuttingDown)
            {
                labelActivity.Text = "ShuttingDown";
                labelActivity.ForeColor = warningColor;
                toolTip.SetToolTip(labelActivity, "Wise40 is shutting down");
            }
            else if (telescopeDigest.Active)
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
            #endregion

            UpdateProjectorControls();
            #endregion

            #region Decide if shutdown is needed
            List<string> reasonsList = new List<string>();

            if (!safetooperateDigest.Safe)
                reasonsList.Add(string.Join(Const.recordSeparator, safetooperateDigest.UnsafeReasons));

            // Comment this out to ignore telescope being idle
            if (!telescopeDigest.Active)
                reasonsList.Add("Telescope is Idle");

            if (reasonsList.Count != 0)
            {
                #region ShouldShutdown
                string shutdownReason = String.Join(" and ", reasonsList);

                if (telescopeDigest.ShuttingDown)
                {
                    //Log("Wise40 is shutting down.");
                }
                else if (ObservatoryIsLogicallyParked && ObservatoryIsPhysicallyParked)
                {
                    if (! shuttingDown)
                        Log("Wise40 already parked.");
                }
                else
                {
                    if (!safetooperateDigest.ComputerControl.Safe)
                        Log("No ComputerControl, shutdown skipped.");
                    else
                        DoShutdownObservatory(shutdownReason);
                }
                #endregion
            }
            else
            {
                #region NoNeedToShutdown
                string safetyMessage = "", activityMessage = "";

                if (safetooperateDigest.Safe)
                {
                    safetyMessage = "Safe";
                    if (safetooperateDigest.Bypassed)
                        safetyMessage += " (due to safety bypass)";
                }
                else
                {
                    safetyMessage = "Not safe";
                    if (safetooperateDigest.UnsafeBecauseNotReady)
                        safetyMessage += " (inconclusive safety info)";
                    else if (!safetooperateDigest.HumanIntervention.Safe)
                        safetyMessage += " (intervention)";
                }

                if (telescopeDigest.Active)
                    activityMessage = "active (" + string.Join(", ", telescopeDigest.Activities) + ")";
                else
                    activityMessage = "idle";
                
                Log(safetyMessage + " and " + activityMessage);
                #endregion
            }
            #endregion
        }

        delegate void UpdateCheckingStatus_delegate(string text);

        private void UpdateCheckingStatus(string s)
        {
            if (labelNextCheck.InvokeRequired)
            {
                Invoke(new UpdateCheckingStatus_delegate(UpdateCheckingStatus), new object[] { s });
            }
            else
            {
                labelNextCheck.Text = s;
                labelNextCheck.Refresh();
            }
        }

        private void RefreshDisplay()
        {
            DateTime localTime = DateTime.Now.ToLocalTime();
            labelDate.Text = $"{localTime:ddd, dd MMM yyyy}";
            labelTime.Text = $"{localTime:hh:mm:ss tt} " + deltaFromUT;
            DateTime now = DateTime.Now;

            string s = string.Empty;
            TimeSpan remaining = _nextCheck.Subtract(now);
            if (remaining.Minutes > 0)
                s += $"{remaining.Minutes:D2}m";
            s += $"{remaining.Seconds:D2}s";
            labelNextCheck.Text = "in " + s;

            if (telescopeDigest?.ShuttingDown == false)
                buttonManualIntervention.Enabled = true;
            updateManualInterventionControls();

            if (now >= _nextCheck && !_checking)
            {
                _checking = true;
                CheckConnections();
                if (connected)
                {
                    CheckSituation();
                    UpdateConditionsControls();
                }

                _nextCheck = DateTime.Now + _intervalBetweenChecks;
                _checking = false;
            }
        }

        private void UpdateConditionsControls()
        {
            if (wisesafetooperate == null || safetooperateDigest == null)
                return;

            string text, tip = "";
            string reasons = string.Join(",", safetooperateDigest.UnsafeReasons);
            Color color;
            
            if (!safetooperateDigest.HumanIntervention.Safe)
            {
                text = "Intervention";
                color = unsafeColor;
                tip = String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n  ");
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

        private delegate void LogDelegate(string text);

        private void Log(string msg, int afterSecs = 0, bool debugOnly = false)
        {
            if (afterSecs != 0 && DateTime.Now.CompareTo(_lastLog) < 0)
                return;

            debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, msg);
            if (!debugOnly)
            {
                logToGUI($"{DateTime.UtcNow:H:mm:ss UT} - {msg}");
                _lastLog = DateTime.UtcNow;
            }
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

        private void DoShutdownObservatory(string reason)
        {
            if (ObservatoryIsLogicallyParked && ObservatoryIsPhysicallyParked)
            {
                Log($"Observatory is logically and physically parked. Ignoring \"{reason}\".");
                return;
            }

            CT = CTS.Token;

            Task.Run(() =>
            {
                try
                {
                    ShutdownObservatory(reason);
                }
                catch (Exception ex)
                {
                    Log($"DoShutdownObservatory:Exception: {(ex.InnerException ?? ex).Message}");
                }
            }, CT);
        }

        private static void SleepWhileProcessingEvents()
        {
            DateTime start = DateTime.Now;
            while (DateTime.Now.Subtract(start).TotalMilliseconds < _intervalBetweenLogs.TotalMilliseconds)
                Application.DoEvents();
        }

        private bool ObservatoryIsPhysicallyParked
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(wisetelescope.Action("nearly-parked", ""));
                }
                catch (Exception ex)
                {
                    Exception e = ex ?? ex;

                    Log($"ObservatoryIsPhysicallyParked: Caught: {e.Message} at\n{ex.StackTrace}", debugOnly: true);
                    return false;
                }
            }
        }

        private bool ObservatoryIsLogicallyParked
        {
            get
            {
                try
                {
                    return wisetelescope.AtPark && wisedome.AtPark && (wisedome.ShutterStatus == ShutterState.shutterClosed);
                } catch (Exception ex)
                {
                    Exception e = ex.InnerException ?? ex;

                    Log($"ObservatoryIsLogicallyParked: Caught: {e.Message} at\n{e.StackTrace}", debugOnly: true);
                    return false;
                }
            }
        }

        private void ShutdownObservatory(string reason)
        {
            if (shuttingDown)
                return;

            try
            {
                Angle domeAz = DomeAzimuth; // Possibly force dome calibration

                if (!wisetelescope.AtPark || !ObservatoryIsPhysicallyParked)
                {
                    if (CT.IsCancellationRequested) throw new Exception("Shutdown aborted");

                    shuttingDown = true;
                    Log($"   Starting Wise40 park (reason: {reason}) ...");

                    Log($"    Parking telescope at {wisesite.LocalSiderealTime} {new Angle(66, Angle.AngleType.Dec)} and dome at {new Angle(90, Angle.AngleType.Az).ToNiceString()} ...");

                    #region Initiate shutdown
                    Task telescopeShutdownTask = Task.Run(() =>
                    {
                        try
                        {
                            if (wisetelescope.Action("shutdown", reason) != "ok")
                                throw new OperationCanceledException("Action(\"telescope:shutdown\") did not reply with \"ok\"");
                            labelActivity.Text = "ShuttingDown";
                            labelActivity.ForeColor = warningColor;
                            toolTip.SetToolTip(labelActivity, "Wise40 is shutting down");
                        }
                        catch (Exception ex)
                        {
                            Log($"ShutdownObservatory:Exception: {(ex.InnerException ?? ex).Message}");
                        }
                    }, CT);
                    #endregion

                    ShutterState shutterState;
                    List<string> activities;
                    bool done = false;

                    #region Wait for shutdown completion
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

                        try
                        {
                            #region Fetch various statuses
                            UpdateCheckingStatus("telescope status");
                            telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));

                            UpdateCheckingStatus("dome status");
                            domeDigest = JsonConvert.DeserializeObject<DomeDigest>(wisedome.Action("status", ""));

                            UpdateCheckingStatus("safetooperate status");
                            safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wisesafetooperate.Action("status", ""));
                            UpdateCheckingStatus("");
                            #endregion

                            if (!safetooperateDigest.ComputerControl.Safe)
                            {
                                Log("  Computer control switched to MAINTENANCE, shutdown aborted!", 10);
                                done = true;
                            }
                            else
                            {
                                Angle ra, dec, az;
                                ra = Angle.FromHours(telescopeDigest.Current.RightAscension, Angle.AngleType.RA);
                                dec = Angle.FromDegrees(telescopeDigest.Current.Declination, Angle.AngleType.Dec);
                                az = Angle.FromDegrees(domeDigest.Azimuth, Angle.AngleType.Az);
                                shutterState = domeDigest.Shutter.State;
                                activities = telescopeDigest.Activities;

                                done = telescopeDigest.AtPark && domeDigest.AtPark && shutterState == ShutterState.shutterClosed;

                                Log("    " +
                                    $"Telescope at {ra.ToNiceString()} {dec.ToNiceString()}, " +
                                    $"dome at {az.ToNiceString()}, " +
                                    $"shutter {shutterState.ToString().ToLower().Remove(0, "shutter".Length)} ...",
                                    _simulated ? 1 : 10);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"    Exception: {ex.Message}");
                            Log($"Caught: {ex.Message} at\n{ex.StackTrace}", debugOnly: true);
                            done = true;
                        }
                    } while (!done);
                    #endregion

                    Log("   Done parking Wise40.");
                    labelActivity.Text = telescopeDigest.Active ? "Active" : "Idle";
                    shuttingDown = false;
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
                            Log($"  Dome at {az.ToNiceString()} ...", 10);
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

                if (telescopeDigest.AtPark && domeDigest.AtPark && domeDigest.Shutter.State == ShutterState.shutterClosed)
                    Log("Wise40 is parked and closed");
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                if (ex.Message == "Shutdown aborted")
                    Log("Shutdown aborted by operator");
                else
                    Log($"ShutdownObservatory:Exception occurred:\n{ex.Message}, aborting shutdown!");

                wisetelescope.Action("abort-shutdown", "");
            }
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
                        Log("DomeAzimuth: ASCOM communication timed out ...");

                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        Log($"DomeAzimuth:Exception: Waiting for dome Azimuth: Caught: {msg}");
                        SleepWhileProcessingEvents();
                    }
                }
                return Angle.FromDegrees(degrees, Angle.AngleType.Az);
            }
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
                toolTip.SetToolTip(labelHumanInterventionStatus,
                    String.Join("\n", HumanIntervention.Details).Replace(Const.recordSeparator, "\n  "));
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
            
            WiseSite.OperationalMode = mode;

            UpdateOpModeControls();
            CloseConnections();
            KillWise40Apps();
        }

        private void KillWise40Apps()
        {
            using (var client = new WebClient()) {
                try
                {
                    client.UploadData(Const.RESTServer.top + "restart", "PUT", null); // PUT to http://www.xxx.yyy.zzz/server/v1/restart
                    Thread.Sleep(5000);
                } catch { }
            }

            foreach (var proc in Process.GetProcessesByName("Dash"))
                proc.Kill();
        }

        private void UpdateOpModeControls()
        {
            WiseSite.OpMode currentMode = WiseSite.OperationalMode;

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
                driverProfile.Register(Const.WiseDriverID.ObservatoryMonitor, "Wise40 ObservatoryMonitor");

                int minutes = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.ObservatoryMonitor,
                    "MinutesBetweenChecks", string.Empty, "5"));

                _intervalBetweenChecks = new TimeSpan(0, minutes, 0);
            }

            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                MinutesToIdle = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.Telescope,
                    Const.ProfileName.Telescope_MinutesToIdle, string.Empty, "15"));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.ObservatoryMonitor, "MinutesBetweenChecks",
                    MinutesBetweenChecks.ToString());
            }

            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.Telescope,
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

        private void conditionsBypassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wisesafetooperate == null)
                return;

            wisesafetooperate.Action(safetooperateDigest.Bypassed ? "end-bypass" : "start-bypass", string.Empty);
        }

        private void buttonProjector_Click(object sender, EventArgs e) {
            try
            {
                bool status = JsonConvert.DeserializeObject<bool>(wisedome.Action("projector", ""));

                status = Convert.ToBoolean(wisedome.Action("projector", (!status).ToString()));
                buttonProjector.Text = "Projector " + ((status == true) ? "Off" : "On");
            }
            catch (Exception ex)
            {
                Log($"buttonProjector_Click:Exception: {(ex.InnerException ?? ex).Message}");
            }
        }

        private void UpdateProjectorControls()
        {
            if (!connected)
                return;

            try
            {
                bool status = JsonConvert.DeserializeObject<bool>(wisedome.Action("projector", ""));

                buttonProjector.Text = "Projector " + ((status) ? "Off" : "On");
            }
            catch (Exception ex)
            {
                Log($"UpdateProjectorControls:Exception: {(ex.InnerException ?? ex).Message}");
            }
        }
    }
}