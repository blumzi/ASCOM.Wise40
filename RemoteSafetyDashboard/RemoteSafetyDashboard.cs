using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40SafeToOperate;
using Newtonsoft.Json;

namespace RemoteSafetyDashboard
{
    public partial class RemoteSafetyDashboard : Form
    {
        static public AscomClient ascomClientSafeToOperate, ascomClientTelescope, ascomClientDome;
        static public TimeSpan _intervalBetweenChecks = TimeSpan.FromSeconds(30);
        static private DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(5);
        static private DateTime _lastSuccessfullCheck = DateTime.MinValue;
        static private bool _checking = false;
        static private SafeToOperateDigest safetooperateDigest;
        static private TelescopeDigest telescopeDigest;
        static private Statuser statuser;
        public static bool OnWise40 = (WiseSite.ObservatoryName == "wise40");

        private static readonly Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        private static readonly Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        private static readonly Color safeColor = Statuser.colors[Statuser.Severity.Good];
        private static readonly Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        static public string remoteHost;
        private static readonly Debugger debugger = Debugger.Instance;

        public RemoteSafetyDashboard()
        {
            InitializeComponent();

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            labelObservatory.Text = WiseSite.ObservatoryName;
            labelHost.Text = Environment.MachineName.ToLower();

            statuser = new Statuser(labelStatus, toolTip);

            if (OnWise40)
            {
                panelWise40.Visible = true;
                Text = "Wise Safety Dashboard";
                labelTitle.Text = "Safety Dashboard";
                remoteHost = "127.0.0.1";
            }
            else
            {
                panelWise40.Visible = false;
                Text = "Wise Remote Safety Dashboard";
                labelTitle.Text = "Remote Safety Dashboard";
                remoteHost = "132.66.65.9";
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            labelDate.Text = $"{now:dd MMM yyyy HH:mm:ss (zzz)}";

            if (_checking)
                return;

            TimeSpan ts = _nextCheck - now;
            labelNextCheckLabel.Text = "Next check in:";
            labelNextCheck.Text = ts.ToMinimalString(showMillis: false);

            labelInformationAge.Text = (_lastSuccessfullCheck == DateTime.MinValue) ?
                    "***" :
                    (now - _lastSuccessfullCheck).ToMinimalString(showMillis: false);

            if (_nextCheck > now)
                return;

            _checking = true;
            if (ClientsAreConnected())
                UpdateDisplay();
            else
                statuser.Show($"No connection to ASCOM.Server on {remoteHost}", millis: 5000, Statuser.Severity.Error, true);
            _nextCheck = DateTime.Now + _intervalBetweenChecks;
            _checking = false;
        }

        public static Color SensorDigestColor(Sensor.SensorDigest sensorDigest)
        {
            Color ret;

            if (!sensorDigest.AffectsSafety)
            {
                ret = normalColor;
            }
            else
            {
                if (sensorDigest.Safe)
                    ret = safeColor;
                else if (sensorDigest.Stale)
                    ret = warningColor;
                else
                    ret = unsafeColor;
            }

            return ret;
        }

        public void RefreshSensor(Label label, Sensor.SensorDigest digest)
        {
            label.Text = digest.Symbolic;
            label.ForeColor = SensorDigestColor(digest);
            toolTip.SetToolTip(label, digest.ToolTip);
        }

        private void ClearLabels(string s, Color color)
        {
            foreach (Label l in new List<Label> {
                    labelHumidityValue,
                    labelCloudCoverValue,
                    labelDewPointValue,
                    labelWindSpeedValue,
                    labelWindDirValue,
                    labelPressureValue,
                    labelSkyTempValue,
                    labelRainRateValue,
                    labelSunElevationValue,
                    labelTempValue,
                })
            {
                l.Text = s;
                l.ForeColor = color;
            }
        }

        public void UpdateDisplay()
        {
            string safetyResponse, telescopeResponse;
            string op;

            statuser.Show("Checking ...", 0);
            statuser.Label.Refresh();

            #region Global Safety
            op = "ascomClientSafeToOperate.Action(\"status\")";
            try
            {
                safetyResponse = ascomClientSafeToOperate.Action("status", "");
                if (safetyResponse == null)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "{op}: null response");
                    #endregion
                    statuser.Show("Safetooperate: empty response", 3000, Statuser.Severity.Error, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                statuser.Show($"Safetooperate: exception {ex.Message}", 3000, Statuser.Severity.Error, true);
                return;
            }

            try
            {
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(safetyResponse);
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: deserialize caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                statuser.Show($"Safetooperate: exception: {ex.Message}", 3000, Statuser.Severity.Error, true);
                return;
            }

            if (safetooperateDigest == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: safetooperateDigest == null");
                #endregion
                statuser.Show("Safetooperate: NULL digest", 3000, Statuser.Severity.Error, true);
                return;
            }

            RefreshSensor(labelHumidityValue, safetooperateDigest.Humidity);
            RefreshSensor(labelCloudCoverValue, safetooperateDigest.CloudCover);
            RefreshSensor(labelDewPointValue, safetooperateDigest.DewPoint);
            RefreshSensor(labelWindSpeedValue, safetooperateDigest.WindSpeed);
            RefreshSensor(labelWindDirValue, safetooperateDigest.WindDirection);
            RefreshSensor(labelPressureValue, safetooperateDigest.Pressure);
            RefreshSensor(labelSkyTempValue, safetooperateDigest.SkyTemperature);
            RefreshSensor(labelRainRateValue, safetooperateDigest.RainRate);
            RefreshSensor(labelSunElevationValue, safetooperateDigest.SunElevation);
            RefreshSensor(labelTempValue, safetooperateDigest.Temperature);

            if (!safetooperateDigest.HumanIntervention.Safe)
            {
                statuser.SetToolTip(String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n  "));
                statuser.Show("Human Intervention", 0, Statuser.Severity.Error, true);
            }
            else if (safetooperateDigest.Bypassed)
            {
                statuser.SetToolTip("Safety checks are bypassed!");
                statuser.Show("Safety bypassed", 0, Statuser.Severity.Warning, true);
            }
            else
            {
                bool isSafe;
                string action = OnWise40 ? "issafe" : "wise-issafe";
                op = $"ascomClientSafeToOperate.Action({action})";

                try
                {
                    string json = ascomClientSafeToOperate.Action(action, "");
                    isSafe = JsonConvert.DeserializeObject<bool>(json);
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    statuser.Show($"{action}: caught {ex.Message}", 2000, Statuser.Severity.Error, true);
                    return;
                }

                if (isSafe)
                {
                    statuser.SetToolTip("Conditions are safe to operate.");
                    statuser.Show("Safe to operate", 0, Statuser.Severity.Good, true);
                }
                else
                {
                    action = OnWise40 ? "unsafereasons-json" : "wise-unsafereasons";
                    List<string> unsafereasons = new List<string>();
                    op = $"ascomClientSafeToOperate.Action({action})";

                    try
                    {
                        unsafereasons = JsonConvert.DeserializeObject<List<string>>(ascomClientSafeToOperate.Action(action, ""));
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: caught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        statuser.Show($"{action}: caught {ex.Message}", 2000, Statuser.Severity.Error, true);
                    }

                    statuser.SetToolTip(string.Join("\n", unsafereasons).Replace(Const.recordSeparator, "\n"));
                    statuser.Show("Not safe to operate", 0, Statuser.Severity.Error, true);
                }
            }
            _lastSuccessfullCheck = DateTime.Now;
            #endregion

            if (! OnWise40)
                return;

            #region Wise40 Specific
            op = $"ascomClientTelescope.Action(\"status\")";
            try
            {
                telescopeResponse = ascomClientTelescope.Action("status", "");
                if (telescopeResponse == null)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "{op}: null response");
                    #endregion
                    statuser.Show("Telescope: Empty response", 3000, Statuser.Severity.Error, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                statuser.Show($"Telescope: : caught {ex.Message}", 3000, Statuser.Severity.Error, true);
                return;
            }

            try
            {
                telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(telescopeResponse);
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: failed to deserialize JSON");
                #endregion
                statuser.Show($"deserialize caught: {ex.Message}", 3000, Statuser.Severity.Error, true);
                return;
            }

            if (telescopeDigest == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: telescopeDigest == null");
                #endregion
                statuser.Show("Empty telescope digest", 3000, Statuser.Severity.Error, true);
                return;
            }

            #region Telescope Status
            if (telescopeDigest.Active)
            {
                labelTelescopeStatus.Text = "Active";
                toolTip.SetToolTip(labelTelescopeStatus, String.Join(",", telescopeDigest.Activities));
            }
            else
            {
                labelTelescopeStatus.Text = "Idle";
                toolTip.SetToolTip(labelTelescopeStatus, "Wis40 is idle");
            }
            #endregion

            #region HumanIntervention
            if (HumanIntervention.IsSet())
            {
                labelHumanInterventionStatus.Text = "Active";
                buttonManualIntervention.Text = "Deactivate";
                toolTip.SetToolTip(labelHumanInterventionStatus, HumanIntervention.Details.ToString().Replace(Const.recordSeparator, "\n"));
            }
            else
            {
                labelHumanInterventionStatus.Text = "Inactive";
                buttonManualIntervention.Text = "Activate";
                toolTip.SetToolTip(labelHumanInterventionStatus, string.Empty);
            }
            #endregion

            #region Projector
            bool status = JsonConvert.DeserializeObject<bool>(ascomClientDome.Action("projector", ""));
            buttonProjector.Text = "Projector " + (status ? "Off" : "On");
            #endregion

            labelNextCheckLabel.Text = "Next check in:";
            _lastSuccessfullCheck = DateTime.Now;

            #endregion
        }

        public bool ClientsAreConnected()
        {
            int connections = 0;

            #region SafeToOperate
            if (ascomClientSafeToOperate == null)
                ascomClientSafeToOperate = new AscomClient($"http://{remoteHost}:11111/api/v1/safetymonitor/0/");

            if (ascomClientSafeToOperate.Connected)
                connections++;
            else
            {
                try
                {
                    ascomClientSafeToOperate.Connected = true;

                    while (!ascomClientSafeToOperate.Connected)
                    {
                        Application.DoEvents();
                    }
                    connections++;
                }
                catch
                {
                    statuser.Show($"No connection to safetooperate on {remoteHost}", 0, Statuser.Severity.Error, true);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"CheckConnections: No connection to {remoteHost}");
                    #endregion
                }
            }
            #endregion

            if (! OnWise40)
                return connections == 1;

            #region Wise40
            if (ascomClientTelescope == null)
                ascomClientTelescope = new AscomClient($"http://{remoteHost}:11111/api/v1/telescope/0/");

            if (ascomClientTelescope.Connected)
                connections++;
            else
            {
                try
                {
                    ascomClientTelescope.Connected = true;

                    while (!ascomClientTelescope.Connected)
                    {
                        Application.DoEvents();
                    }
                    connections++;
                }
                catch
                {
                    statuser.Show($"No connection to ASCOM.Server on {remoteHost}", 0, Statuser.Severity.Error, true);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"CheckConnections: No connection to {remoteHost}");
                    #endregion
                }
            }

            if (ascomClientDome == null)
                ascomClientDome = new AscomClient($"http://{remoteHost}:11111/api/v1/dome/0/");

            if (ascomClientDome.Connected)
                connections++;
            else
            {
                try
                {
                    ascomClientDome.Connected = true;

                    while (!ascomClientDome.Connected)
                    {
                        Application.DoEvents();
                    }
                    connections++;
                }
                catch
                {
                    statuser.Show($"No connection to ASCOM.Server on {remoteHost}", 0, Statuser.Severity.Error, true);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"CheckConnections: No connection to {remoteHost}");
                    #endregion
                }
            }
            #endregion
            return connections == 3;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void safetyOnTheWIse40WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/blumzi/ASCOM.Wise40/wiki/safetooperate");
        }

        public void RemoveHumanInterventionFile(object sender, DoWorkEventArgs e)
        {
            HumanIntervention.Remove();
        }

        public void AfterRemoveHumanInterventionFile(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        private void UpdateManualInterventionControls()
        {
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
            }

            UpdateManualInterventionControls();
        }

        private void buttonProjector_Click(object sender, EventArgs e)
        {
            try
            {
                string status = ascomClientDome.Action("projector", "");

                if (!String.IsNullOrEmpty(status))
                {
                    bool on = JsonConvert.DeserializeObject<bool>(status);

                    on = Convert.ToBoolean(ascomClientDome.Action("projector", (!on).ToString()));
                    buttonProjector.Text = "Projector " + (on ? "Off" : "On");
                }
            }
            catch
            {
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About form = new About();
            form.Visible = true;
        }
    }
}
