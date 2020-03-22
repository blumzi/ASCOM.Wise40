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
        Version version = new Version(0, 2);
        static public TimeSpan _intervalBetweenChecks = TimeSpan.FromSeconds(20);
        static DateTime _nextCheck = DateTime.Now + TimeSpan.FromSeconds(5);
        static bool _checking = false;
        static bool _connected = false;
        SafeToOperateDigest safetooperateDigest;
        static Statuser safetooperateStatus;

        static Color normalColor = Statuser.colors[Statuser.Severity.Normal];
        static Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        static Color safeColor = Statuser.colors[Statuser.Severity.Good];
        static Color warningColor = Statuser.colors[Statuser.Severity.Warning];

        static public string remoteHost = "132.66.65.9";

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
                    safetooperateStatus.Show(communicator.Status, 0, communicator.Severity);
                return;
            }

            TimeSpan ts = _nextCheck - now;
            labelNextCheck.Text = ts.ToMinimalString(showMillis: false);

            if (ts <= TimeSpan.FromSeconds(0))
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

        public Color SensorDigestColor(Sensor.SensorDigest sensorDigest)
        {
            Color ret;

            if (! sensorDigest.AffectsSafety)
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
            toolTip.SetToolTip(label, digest.AffectsSafety ? digest.ToolTip : "Does not affect safety");
        }

        public void UpdateDisplay()
        {
            try
            {
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(communicator.Action("status", ""));
            } catch (Exception ex)
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
                    l.Text = "";
                }
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

            string tip = null;
            string text = "";
            Statuser.Severity severity = Statuser.Severity.Normal;

            if (!safetooperateDigest.HumanIntervention.Safe)
            {
                text = "Human Intervention";
                severity = Statuser.Severity.Error;
                tip = String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n  ");
            }
            else if (safetooperateDigest.Bypassed)
            {
                text = "Safety bypassed";
                severity = Statuser.Severity.Warning;
                tip = "Safety checks are bypassed!";
            }
            else if (safetooperateDigest.Safe)
            {
                text = "Safe to operate";
                severity = Statuser.Severity.Good;
                tip = "Conditions are safe to operate.";
            }
            else
            {
                text = "Not safe to operate";
                severity = Statuser.Severity.Error;
                tip = string.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n");
            }
            safetooperateStatus.Label.Text = text;
            toolTip.SetToolTip(safetooperateStatus.Label, tip);
            safetooperateStatus.Show(text, 0, severity, true);
        }

        public void CheckConnections()
        {
            if (communicator == null)
                communicator = new Communicator(Communicator.Type.Fake, remoteHost);
                //communicator = new Communicator(Communicator.Type.ASCOM, remoteHost);

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
