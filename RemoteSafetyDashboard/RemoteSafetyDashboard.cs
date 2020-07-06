using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Common;
using ASCOM.Wise40SafeToOperate;
using ASCOM.DriverAccess;
using Newtonsoft.Json;
using System.Threading;

namespace RemoteSafetyDashboard
{
    public partial class RemoteSafetyDashboard : Form
    {
        static public Communicator communicator;
        static public TimeSpan _intervalBetweenChecks = TimeSpan.FromSeconds(20);
        static private DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(5);
        static private bool _checking = false;
        static private bool _connected = false;
        static private SafeToOperateDigest safetooperateDigest;
        static private Statuser safetooperateStatus;

        private static readonly Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        private static readonly Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        private static readonly Color safeColor = Statuser.colors[Statuser.Severity.Good];
        private static readonly Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        static public string remoteHost = "132.66.65.9";
        private static readonly Debugger debugger = Debugger.Instance;

        public RemoteSafetyDashboard()
        {
            InitializeComponent();

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            labelTitle.Text = $"Remote Safety Dashboard running on {Environment.MachineName.ToLower()}";

            safetooperateStatus = new Statuser(labelWeatherStatus, toolTip);
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            labelDate.Text = $"{now:dd MMM yyyy HH:mm:ss (zzz)}";

            if (_checking)
            {
                if (communicator != null)
                    safetooperateStatus.Show(communicator.Status, 2000, communicator.Severity, silent: true);
                return;
            }

            TimeSpan ts = _nextCheck - now;
            labelNextCheck.Text = ts.ToMinimalString(showMillis: false);

            if (ts <= TimeSpan.FromMilliseconds(0))
            {
                _checking = true;
                CheckConnections();
                if (_connected)
                    UpdateDisplay();
                else
                    safetooperateStatus.Show($"No connection to ASCOM.Server on {remoteHost}", 0, Statuser.Severity.Error, true);
                _nextCheck = DateTime.Now + _intervalBetweenChecks;
                _checking = false;
            }
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

        private void ClearLabels(string s)
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
            }
        }

        public void UpdateDisplay()
        {
            string response = "";

            try
            {
                response = communicator.Action("status", "");
                if (response == null)
                {
                    ClearLabels("NULL");
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Get \"status\": null response");
                    #endregion
                    return;
                }
                //#region debug
                //debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Get \"status\": response: \"{response}\"");
                //#endregion
            }
            catch (Exception ex)
            {
                safetooperateStatus.Show($"Get \"status\": {ex.Message}", 3000, Statuser.Severity.Error, true);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Get \"status\": caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                return;
            }

            try
            {
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(response);
            } catch (Exception ex)
            {
                ClearLabels("JSON");
                safetooperateStatus.Show($"deserialize caught: {ex.Message}", 3000, Statuser.Severity.Error, true);
                return;
            }

            if (safetooperateDigest == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Get \"status\": response: {response} => safetooperateDigest == null");
                #endregion
                safetooperateStatus.Show("NULL digest", 3000, Statuser.Severity.Error, true);
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
                toolTip.SetToolTip(safetooperateStatus.Label,
                    String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n  "));
                safetooperateStatus.Show("Human Intervention", 0, Statuser.Severity.Error, true);
            }
            else if (safetooperateDigest.Bypassed)
            {
                toolTip.SetToolTip(safetooperateStatus.Label, "Safety checks are bypassed!");
                safetooperateStatus.Show("Safety bypassed", 0, Statuser.Severity.Warning, true);
            }
            else
            {
                bool wise_issafe;

                try
                {
                    wise_issafe = JsonConvert.DeserializeObject<bool>(communicator.Action("wise-issafe", ""));
                }
                catch (Exception ex)
                {
                    safetooperateStatus.Show($"Get wise-issafe caught {ex.Message}", 2000, Statuser.Severity.Error, true);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Get wise-safe caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    return;
                }

                if (wise_issafe)
                {
                    toolTip.SetToolTip(safetooperateStatus.Label, "Conditions are safe to operate.");
                    safetooperateStatus.Show("Safe to operate", 0, Statuser.Severity.Good, true);
                }
                else
                {
                    List<string> wise_unsafereasons = new List<string>();

                    try
                    {
                        wise_unsafereasons = JsonConvert.DeserializeObject<List<string>>(communicator.Action("wise-unsafereasons", ""));
                    }
                    catch (Exception ex)
                    {
                        safetooperateStatus.Show($"Get wise-unsafereasons caught {ex.Message}", 2000, Statuser.Severity.Error, true);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"Get wise-unsafereasons caught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                    }

                    toolTip.SetToolTip(safetooperateStatus.Label, string.Join("\n", wise_unsafereasons).Replace(Const.recordSeparator, "\n"));
                    safetooperateStatus.Show("Not safe to operate", 0, Statuser.Severity.Error, true);
                }
            }
        }

        public static void CheckConnections()
        {
            if (communicator == null)
                communicator = new Communicator(Communicator.Type.Fake, remoteHost);
                //communicator = new Communicator(Communicator.Type.ASCOM, remoteHost); // Till we can use ASCOM.RemoteClient

            if (communicator.Connected)
            {
                _connected = true;
                return;
            }
            else
            {
                try
                {
                    communicator.Connected = true;

                    while (!communicator.Connected)
                    {
                        Application.DoEvents();
                    }
                    _connected = true;
                }
                catch
                {
                    _connected = false;
                    safetooperateStatus.Show($"No connection to ASCOM.Server on {remoteHost}", 0, Statuser.Severity.Error, true);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"CheckConnections: No connection to {remoteHost}");
                    #endregion
                    return;
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void safetyOnTheWIse40WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/blumzi/ASCOM.Wise40/wiki/safetooperate");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About form = new About();
            form.Visible = true;
        }
    }
}
