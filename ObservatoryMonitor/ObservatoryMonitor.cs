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
using System.Net.Http;
using System.Linq;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObsMainForm : Form
    {
        private static readonly bool _simulated = WiseObject.Simulated;
        public const int _maxLogItems = 100000;
        private static DriverAccess.Telescope wisetelescope = null;
        private static readonly WiseSite wisesite = WiseSite.Instance;
        private static DriverAccess.Dome wisedome = null;
        private static DriverAccess.SafetyMonitor wisesafetooperate = null;
        private readonly Version version = new Version(0, 2);
        private static DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(25);
        private static bool _checking = false;
        public static TimeSpan _intervalBetweenRegularChecks;
        public static TimeSpan _intervalBetweenChecksWhileShuttingDown = TimeSpan.FromSeconds(20);
        private DateTime LatestSuccessfulServerConnection = DateTime.MinValue;
        private TimeSpan TimeToRestartServer = TimeSpan.FromSeconds(30);
        static public int MinutesToIdle { get; set; }
        private static TimeSpan _intervalBetweenLogs = _simulated ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(20);
        private static DateTime _lastLog = DateTime.MinValue;
        private static readonly string deltaFromUT = "(UT+" + DateTime.Now.Subtract(DateTime.UtcNow).Hours.ToString() + ")";

        private static readonly Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        private static readonly Color safeColor = Statuser.colors[Statuser.Severity.Good];
        private static readonly Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        private static readonly CancellationTokenSource CTS = new CancellationTokenSource();
        private static CancellationToken CT;

        private TelescopeDigest telescopeDigest;
        private DomeDigest domeDigest;
        private SafeToOperateDigest safetooperateDigest;

        private static readonly Common.Debugger debugger = Common.Debugger.Instance;

        public static bool weInitiatedShutdown = false;

        private static readonly HttpClient serverCheckerHttpClient = new HttpClient();

        private const string parkedMessage = "Wise40 is parked and closed";

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
        }

        public bool CanConnect()
        {
            #region Check connectivity to RemoteServer
            try
            {
                UpdateCheckingStatus("connecting ASCOM server");

                Task serverCheckerTask = serverCheckerHttpClient.GetAsync(Const.RESTServer.top + "concurrency",
                    HttpCompletionOption.ResponseHeadersRead);

                while (!serverCheckerTask.IsCompleted)
                    Application.DoEvents();

                LatestSuccessfulServerConnection = DateTime.Now;
            }
            catch (HttpRequestException ex)
            {
                Log("Cannot connect to ASCOM Server");
                Log($"ASCOM server connect exception: {(ex.InnerException ?? ex).Message}", debugOnly: true);

                if (DateTime.Now.Subtract(LatestSuccessfulServerConnection) > TimeToRestartServer)
                {
                    Log($"ASCOM server did not answer for {TimeToRestartServer.TotalSeconds} seconds");
                    foreach (var proc in Process.GetProcessesByName("ASCOM.RemoteServer"))
                    {
                        Log($"Killed ASCOM Server process {proc.Id}.");
                        proc.Kill();
                    }
                }
                return false;
            }
            #endregion

            string remoteDriver = null;
            #region Connect to remote ASCOM Drivers

            remoteDriver = "Telescope";
            if (wisetelescope == null)
            {
                try
                {
                    wisetelescope = new DriverAccess.Telescope("ASCOM.AlpacaDynamic1.Telescope");
                }
                catch (Exception ex)
                {
                    Log($"Cannot connect the remote {remoteDriver} client");

                    string msg = $"CanConnect:Exception: {ex.Message}";
                    if (ex.InnerException != null)
                        msg += $" caused by {ex.InnerException}";
                    msg += $" at {ex.StackTrace}";

                    Log(msg, debugOnly: true);
                    return false;
                }
            }

            if (!wisetelescope.Connected)
            {
                wisetelescope.Connected = true;
                while (!wisetelescope.Connected)
                {
                    Log($"Waiting for the \"{remoteDriver}\" client to connect ...", 5);
                    Application.DoEvents();
                }
            }

            remoteDriver = "SafeToOperate";
            if (wisesafetooperate == null)
                try
                {
                    wisesafetooperate = new DriverAccess.SafetyMonitor("ASCOM.AlpacaDynamic1.SafetyMonitor");      // Must match ASCOM Remote Server Setup
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;
                    Log($"Cannot connect the remote {remoteDriver} client");
                    Log($"CheckConnections:Exception: {ex?.Message} at {ex?.StackTrace}", debugOnly: true);
                    return false;
                }

            if (!wisesafetooperate.Connected)
            {
                wisesafetooperate.Connected = true;
                while (!wisesafetooperate.Connected)
                {
                    Log($"Waiting for the \"{remoteDriver}\" client to connect ...", 5);
                    Application.DoEvents();
                }
            }

            remoteDriver = "Dome";
            if (wisedome == null)
                try
                {
                    wisedome = new DriverAccess.Dome("ASCOM.AlpacaDynamic1.Dome");
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;
                    Log($"Cannot connect the remote {remoteDriver} client");
                    Log($"CheckConnections:Exception: {ex?.Message} at {ex?.StackTrace}", debugOnly: true);
                    return false;
                }

            if (!wisedome.Connected)
            {
                wisedome.Connected = true;
                while (!wisedome.Connected)
                {
                    Log($"Waiting for the \"{remoteDriver}\" client to connect", 5);
                    Application.DoEvents();
                }
            }

            if (!buttonProjector.Enabled)
                    buttonProjector.Enabled = true;

                return true;
            //}
            //catch (Exception ex)
            //{
            //    if (ex.InnerException != null)
            //        ex = ex.InnerException;
            //    Log($"Cannot connect the remote {remoteDriver} client");
            //    Log($"CheckConnections:Exception: {ex?.Message} at {ex?.StackTrace}", debugOnly: true);
            //    return false;
            //}
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

            UpdateManualInterventionControls();

            serverCheckerHttpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        private void CheckSituation()
        {
            string op = "";

            #region GetStatus
            try
            {
                op = "telescope status";
                UpdateCheckingStatus(op);
                telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));

                op = "safetooperate status";
                UpdateCheckingStatus(op);
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wisesafetooperate.Action("status", ""));

                op = "dome status";
                UpdateCheckingStatus(op);
                domeDigest = JsonConvert.DeserializeObject<DomeDigest>(wisedome.Action("status", ""));
            }
            catch (Exception ex)
            {
                UpdateCheckingStatus("");
                Log($"Failed to get {op}");
                Log($"CheckSituation:Exception: {(ex.InnerException ?? ex).Message} at\n{ex.StackTrace}", debugOnly: true);
                return;
            }
            #endregion

            #region UpdateDisplay

            #region ComputerControlLabel
            if (safetooperateDigest.ComputerControl.Safe)
            {
                labelComputerControl.Text = "Operational";
                labelComputerControl.ForeColor = safeColor;
                toolTip.SetToolTip(labelComputerControl, "Computer Control is ON");
            }
            else
            {
                labelComputerControl.Text = "Maintenance";
                labelComputerControl.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelComputerControl, "Computer Control is OFF");
            }
            #endregion

            #region Human Intervention
            if (telescopeDigest.ShuttingDown)
            {
                buttonManualIntervention.Enabled = false;
                toolTip.SetToolTip(buttonManualIntervention, Const.UnsafeReasons.ShuttingDown);
            }
            else
            {
                buttonManualIntervention.Enabled = true;
                toolTip.SetToolTip(buttonManualIntervention, "");
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
                toolTip.SetToolTip(labelActivity, Const.UnsafeReasons.ShuttingDown);
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

            if (weInitiatedShutdown)
            {
                if (!telescopeDigest.ShuttingDown)
                {
                    weInitiatedShutdown = false;
                    LogCurrentPosition();
                    Log(" Done parking Wise40.");
                    Log("Done Wise40 shutdown");
                    return;
                }

                if (!safetooperateDigest.ComputerControl.Safe)
                {
                    weInitiatedShutdown = false;
                    Log("Wise40 is in MAINTENANCE mode, shutdown aborted");
                    return;
                }
            }

            if (telescopeDigest.ShuttingDown)
            {
                // Wise40 is shutting down
                LogCurrentPosition();
                return;
            }

            if (ObservatoryIsLogicallyParked)
            {
                Log(parkedMessage);
                return;
            }

            if (safetooperateDigest.Safe && telescopeDigest.Active)
            {
                Log($"Safe and active ({String.Join(", ", telescopeDigest.Activities.ToArray())})");
                return;
            }

            if (!safetooperateDigest.Safe && !safetooperateDigest.UnsafeBecauseNotReady)
            {
                DoShutdownObservatory(string.Join(Const.recordSeparator, safetooperateDigest.UnsafeReasons));
                return;
            }

            // Get a fresh status from the telescope before deciding it is Idle
            try
            {
                telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wisetelescope.Action("status", ""));
                if (!telescopeDigest.Active)
                {
                    DoShutdownObservatory("Wise40 is idle");
                    return;
                }
            }
            catch { }
        }

        private void LogCurrentPosition()
        {
            Angle ra = Angle.RaFromHours(telescopeDigest.Current.RightAscension);
            Angle dec = Angle.DecFromDegrees(telescopeDigest.Current.Declination);
            Angle az = Angle.AzFromDegrees(domeDigest.Azimuth);
            ShutterState shutterState = domeDigest.Shutter.State;

            Log("    " +
                $"Telescope at {ra.ToNiceString()} {dec.ToNiceString()}, " +
                $"dome at {az.ToNiceString()}, " +
                $"shutter {shutterState.ToString().ToLower().Remove(0, "shutter".Length)} ...",
                _simulated ? 1 : 10);
        }

        private delegate void UpdateCheckingStatus_delegate(string text);

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

            if (DateTime.Now >= _nextCheck && !_checking)
            {
                _checking = true;
                if (CanConnect())
                {
                    CheckSituation();
                    UpdateManualInterventionControls();
                    UpdateConditionsControls();
                }
                _nextCheck = DateTime.Now + ((weInitiatedShutdown || (telescopeDigest?.ShuttingDown == true)) ?
                    _intervalBetweenChecksWhileShuttingDown :
                    _intervalBetweenRegularChecks);
                _checking = false;
            }

            labelNextCheck.Text = $"in {_nextCheck.Subtract(DateTime.Now).ToMinimalString(showMillis: false)}";
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
        }

#pragma warning disable IDE1006 // Naming Styles
        private void timerDisplayRefresh_Tick(object sender, EventArgs e)
#pragma warning restore IDE1006 // Naming Styles
        {
            RefreshDisplay();
        }

#pragma warning disable IDE1006 // Naming Styles
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
#pragma warning restore IDE1006 // Naming Styles
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
                LogToGUI($"{DateTime.UtcNow:HH:mm:ss UT} - {msg}");
                _lastLog = DateTime.UtcNow;
            }
        }

        public void LogToGUI(string line)
        {
            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (listBoxLog.InvokeRequired)
            {
                this.Invoke(new LogDelegate(LogToGUI), new object[] { line });
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
            if (!safetooperateDigest.ComputerControl.Safe)
            {
                Log("Wise40 is in MAINTENANCE mode, shutdown skipped");
                return;
            }

            if (ObservatoryIsLogicallyParked && ObservatoryIsPhysicallyParked)
            {
                Log($"Wise40 is logically and physically parked. Ignoring \"{reason}\".");
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

            labelActivity.Text = "ShuttingDown";
            labelActivity.ForeColor = warningColor;
            toolTip.SetToolTip(labelActivity, Const.UnsafeReasons.ShuttingDown);
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
                }
                catch (Exception ex)
                {
                    Exception e = ex.InnerException ?? ex;

                    Log($"ObservatoryIsLogicallyParked: Caught: {e.Message} at\n{e.StackTrace}", debugOnly: true);
                    return false;
                }
            }
        }

        private void ShutdownObservatory(string reason)
        {
            if (weInitiatedShutdown)    // already initiated a shutdown
                return;

            #region Initiate shutdown
            if (wisetelescope.Action("shutdown", reason) == "ok")
            {
                string indent = "";

                Log("Initiating Wise40 shutdown. Reason(s):");
                foreach(string r in reason.Split(Const.recordSeparator[0]).ToList<string>())
                {
                    Log($"    {indent}{r}");
                    if (r.StartsWith("HumanIntervention"))
                        indent = " ";
                }
                Angle ra = wisesite.LocalSiderealTime;
                Angle dec = new Angle(66, Angle.AngleType.Dec);
                Angle az = new Angle(90, Angle.AngleType.Az);

                Log($" Parking telescope at {ra} {dec} and dome at {az.ToNiceString()} ...");
                LogCurrentPosition();
                weInitiatedShutdown = true;
                _nextCheck = DateTime.Now + _intervalBetweenChecksWhileShuttingDown;
                return;
            }
            else
            {
                Log("   Failed to initiate Wise40 shutdown.");
                Exceptor.Throw<OperationCanceledException>($"ShutdownObservatory({reason})",
                        "Action(\"telescope:shutdown\") did not reply with \"ok\"");
                weInitiatedShutdown = false;
                return;
            }
            #endregion
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ObservatoryMonitorAboutForm(version).Show();
        }

        private void UpdateManualInterventionControls()
        {
            if (telescopeDigest != null)
                buttonManualIntervention.Enabled = !telescopeDigest.ShuttingDown;

            if (HumanIntervention.IsSet())
            {
                labelHumanInterventionStatus.Text = "Active";
                labelHumanInterventionStatus.ForeColor = unsafeColor;
                buttonManualIntervention.Text = "Deactivate";
                toolTip.SetToolTip(labelHumanInterventionStatus,
                    String.Join("\n", HumanIntervention.Details).Replace(Const.recordSeparator, "\n  "));
            }
            else
            {
                labelHumanInterventionStatus.Text = "Inactive";
                buttonManualIntervention.Text = "Activate";
                labelHumanInterventionStatus.ForeColor = safeColor;
                toolTip.SetToolTip(labelHumanInterventionStatus, "");
            }
        }

        private void RemoveHumanInterventionFile(object sender, DoWorkEventArgs e)
        {
            HumanIntervention.Remove();
        }

        private void AfterRemoveHumanInterventionFile(object sender, RunWorkerCompletedEventArgs e)
        {
            Log("Removed operator intervention.");
        }

        private void buttonManualIntervention_Click(object sender, EventArgs e)
        {
            if (HumanIntervention.IsSet())
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += RemoveHumanInterventionFile;
                bw.RunWorkerCompleted += AfterRemoveHumanInterventionFile;
                bw.RunWorkerAsync();
            }
            else
            {
                DialogResult result = new InterventionForm().ShowDialog();
                if (result == DialogResult.OK)
                    Log("Created operator intervention");
            }

            UpdateManualInterventionControls();
            CheckSituation();
        }

        private void KillWise40Apps()
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.UploadData(Const.RESTServer.top + "restart", "PUT", null); // PUT to http://www.xxx.yyy.zzz/server/v1/restart
                    Thread.Sleep(5000);
                }
                catch { }
            }

            foreach (var proc in Process.GetProcessesByName("Dash"))
                proc.Kill();
        }

        private void setupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ObservatoryMonitorSetupDialogForm(this).Show();
        }

        public static void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                driverProfile.Register(Const.WiseDriverID.ObservatoryMonitor, "Wise40 ObservatoryMonitor");

                int minutes = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.ObservatoryMonitor,
                    "MinutesBetweenChecks", string.Empty, "5"));

                _intervalBetweenRegularChecks = TimeSpan.FromMinutes(minutes);
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
                return (int) _intervalBetweenRegularChecks.TotalMinutes;
            }

            set
            {
                _intervalBetweenRegularChecks = TimeSpan.FromMinutes(value);
            }
        }

        private void buttonProjector_Click(object sender, EventArgs e)
        {
            try
            {
                bool status = JsonConvert.DeserializeObject<bool>(wisedome.Action("projector", ""));

                status = Convert.ToBoolean(wisedome.Action("projector", (!status).ToString()));
                buttonProjector.Text = "Projector " + (status ? "Off" : "On");
            }
            catch (Exception ex)
            {
                Log($"buttonProjector_Click:Exception: {(ex.InnerException ?? ex).Message}");
            }
        }

        private void UpdateProjectorControls()
        {
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