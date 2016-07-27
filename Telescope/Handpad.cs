using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System.IO;

namespace ASCOM.Wise40
{
    public partial class HandpadForm : Form
    {
        public DaqsForm daqsForm;
        private static WiseTele wisetele = WiseTele.Instance;
        private double handpadRate = Const.rateSlew;
        private BackgroundWorker scopeBackgroundMover;
        private static WiseSite wisesite = WiseSite.Instance;

        private class TimedMovementArg
        {
            public TelescopeAxes axis;
            public double rate;
            public int millis;
            public int nsteps;
        }

        private class TimedMovementResult
        {
            public uint[] enc_value;
            public Angle[] enc_angle;
            public DateTime[] time;
            public int threadId;
            public bool cancelled;

            public enum ResultSelector { AtStart = 0, AtStop = 1, AtIdle = 2 };

            public TimedMovementResult()
            {
                enc_value = new uint[3];
                enc_angle = new Angle[3];
                time = new DateTime[3];
                cancelled = false;
            }

            public override string ToString()
            {
                string s;
                int curr, prev;
                uint dEncoder;
                TimeSpan dTime;
                double dDeg;

                curr = (int)TimedMovementResult.ResultSelector.AtStop;
                prev = (int)TimedMovementResult.ResultSelector.AtStart;
                dTime = time[curr].Subtract(time[prev]);
                dEncoder = (enc_value[curr] > enc_value[prev]) ? enc_value[curr] - enc_value[prev] : enc_value[prev] - enc_value[curr];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s = string.Format(" start-to-stop: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDegrees(dDeg));

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStop;
                dTime = time[curr].Subtract(time[prev]);
                dEncoder = (enc_value[curr] > enc_value[prev]) ? enc_value[curr] - enc_value[prev] : enc_value[prev] - enc_value[curr];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s += string.Format("       inertia: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDegrees(dDeg));

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStart;
                dTime = time[curr].Subtract(time[prev]);
                dEncoder = (enc_value[curr] > enc_value[prev]) ? enc_value[curr] - enc_value[prev] : enc_value[prev] - enc_value[curr];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s += string.Format("         total: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDegrees(dDeg)); s += "\r\n";

                return s;
            }
        }

        private List<TimedMovementResult> results;
        private bool resultsAvailable = false;

        public HandpadForm()
        {
            InitializeComponent();
            checkBoxTrack.Checked = wisetele.Tracking;
            //daqsForm = new DaqsForm(this);
            results = new List<TimedMovementResult>();
            wisesite.init();
            WiseDome.Instance.init();

            groupBoxTelescope.Text = string.Format(" {0} - v{1} ", wisetele.Name, wisetele.DriverVersion);
            groupBoxWeather.Text = string.Format(" {0} - v{1} ", wisesite.observingConditions.Name, wisesite.observingConditions.DriverVersion);
            groupBoxDomeGroup.Text = string.Format(" {0} - v{1} ", WiseDome.Instance.Name, WiseDome.Instance.DriverVersion);
        }

        private void radioButtonSlew_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateSlew;
        }

        private void radioButtonSet_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateSet;
        }

        private void radioButtonGuide_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateGuide;
        }

        private void checkBoxTrack_CheckedChanged(object sender, EventArgs e)
        {
            wisetele.Tracking = ((CheckBox)sender).Checked;
        }

        private void buttonHardware_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            if (daqsForm == null)
            {
                daqsForm = new DaqsForm(this);
                daqsForm.Visible = true;
                buttonHardware.Text = "Hide hardware";
            }
            else
            {
                if (daqsForm.Visible)
                {
                    buttonHardware.Text = "Show hardware";
                    daqsForm.Visible = false;
                }
                else
                {
                    buttonHardware.Text = "Hide hardware";
                    daqsForm.Visible = true;
                }
            }
        }

        private void buttonDome_Click(object sender, EventArgs e)
        {
            if (panelDome.Visible)
            {
                buttonDome.Text = "Show Dome";
                panelDome.Visible = false;
            }
            else
            {
                buttonDome.Text = "Hide Dome";
                panelDome.Visible = true;
            }
        }

        private void buttonFocuser_Click(object sender, EventArgs e)
        {
            if (panelFocuser.Visible)
            {
                buttonFocuser.Text = "Show Focuser";
                panelFocuser.Visible = false;
            }
            else
            {
                buttonFocuser.Text = "Hide Focuser";
                panelFocuser.Visible = true;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            wisetele.Stop();
        }

        private void RefreshDisplay()
        {
            if (!panelControls.Visible)
                return;

            DateTime now = DateTime.Now;
            DateTime utc = now.ToUniversalTime();
            ASCOM.Utilities.Util u = new Utilities.Util();

            labelDate.Text = utc.ToLongDateString();
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utc.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelSiderealValue.Text = wisesite.LocalSiderealTime.ToString();

            labelRightAscensionValue.Text = Angle.FromHours(wisetele.RightAscension).ToNiceString();
            labelDeclinationValue.Text = Angle.FromDegrees(wisetele.Declination).ToNiceString();
            labelHourAngleValue.Text = Angle.FromHours(wisetele.HourAngle, Angle.Type.HA).ToNiceString();

            labelAltitudeValue.Text = Angle.FromDegrees(wisetele.Altitude).ToNiceString();
            labelAzimuthValue.Text = Angle.FromDegrees(wisetele.Azimuth).ToNiceString();

            labelHAEncValue.Text = wisetele.HAEncoder.Value.ToString();
            labelDecEncValue.Text = wisetele.DecEncoder.Value.ToString();

            axisValue.Text = wisetele.HAEncoder.AxisValue.ToString();
            wormValue.Text = wisetele.HAEncoder.WormValue.ToString();

            checkBoxPrimaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisPrimary);
            checkBoxSecondaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisSecondary);
            checkBoxSlewingIsActive.Checked = wisetele.Slewing;
            checkBoxTrackingIsActive.Checked = wisetele.Tracking;

            checkBoxTrack.Checked = wisetele.Tracking;

            if (scopeBackgroundMover != null && scopeBackgroundMover.IsBusy)
                TextBoxLog.Text = "Working ...";

            if (resultsAvailable)
            {
                TextBoxLog.Clear();
                if (results.Count == 0)
                    TextBoxLog.Text = "Cancelled by user!";
                else
                    for (int i = 0; i < results.Count; i++)
                        TextBoxLog.Text += string.Format("[{0}]: ({2})\r\n{1}", i, results[i].ToString(), results[i].cancelled ? "cancelled" : "completed");
                resultsAvailable = false;
            }

            if (panelDome.Visible)
            {
                if (labelDomeSlavedConfValue.Text == string.Empty)
                {
                    using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                    {
                        driverProfile.DeviceType = "Telescope";
                        bool confDomeSlaved = Convert.ToBoolean(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Enslave Dome", string.Empty, "false"));

                        labelDomeSlavedConfValue.Text = confDomeSlaved ? "Enslaved" : "Not enslaved" + " while tracking";
                    }
                }

                labelDomeAzimuthValue.Text = DomeSlaveDriver.Instance.Azimuth;
                labelDomeStatusValue.Text = DomeSlaveDriver.Instance.Status;
                labelDomeShutterStatusValue.Text = DomeSlaveDriver.Instance.ShutterStatus;
            }

            if (groupBoxWeather.Visible)
            {
                ObservingConditions oc = wisesite.observingConditions;

                labelAgeValue.Text = ((int) Math.Round(oc.TimeSinceLastUpdate(""), 2)).ToString() + "sec";

                double d = oc.CloudCover;
                if (d == 0.0)
                    labelCloudCoverValue.Text = "Clear";
                else if (d == 50.0)
                    labelCloudCoverValue.Text = "Cloudy";
                else if (d == 90.0)
                    labelCloudCoverValue.Text = "VeryCloudy";
                else
                    labelCloudCoverValue.Text = "Unknown";

                labelDewPointValue.Text = oc.DewPoint.ToString() + "°C";
                labelSkyTempValue.Text = oc.SkyTemperature.ToString() + "°C";
                labelTempValue.Text = oc.Temperature.ToString() + "°C";
                labelHumidityValue.Text = oc.Humidity.ToString() + "%";
                labelPressureValue.Text = oc.Pressure.ToString() + "mB";
                labelRainRateValue.Text = (oc.RainRate > 0.0) ? "Wet" : "Dry";
                labelWindSpeedValue.Text = oc.WindSpeed.ToString() + "m/s";
                labelWindDirValue.Text = oc.WindDirection.ToString() + "°";
            }

            if (groupBoxCurrentRates.Visible)
            {
                WiseVirtualMotor m;

                m = wisetele.WestMotor.isOn ? wisetele.WestMotor : wisetele.EastMotor.isOn ? wisetele.EastMotor : null;
                if (m == null)
                    labelCurrPrimRateValue.Text = "Stopped";
                else
                    labelCurrPrimRateValue.Text = m.name.Replace("Motor", "") + "@" + WiseTele.RateName(m.currentRate).Replace("rate", "");

                m = wisetele.SouthMotor.isOn ? wisetele.SouthMotor : wisetele.NorthMotor.isOn ? wisetele.NorthMotor : null;
                if (m == null)
                    labelCurrSecRateValue.Text = "Stopped";
                else
                    labelCurrSecRateValue.Text = m.name.Replace("Motor", "") + "@" + WiseTele.RateName(m.currentRate).Replace("rate", "");
            }
        }

        private void HandpadForm_VisibleChanged(object sender, EventArgs e)
        {
            displayRefreshTimer.Enabled = ((Form)sender).Visible ? true : false;
        }

        private void buttonHandpad_Click(object sender, EventArgs e)
        {
            if (panelDebug.Visible)
            {
                buttonStudy.Text = "Show Study";
                panelDebug.Visible = false;
            } else
            {
                buttonStudy.Text = "Hide Study";
                panelDebug.Visible = true;
            }
        }

        private void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            if (button == buttonNorth)
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
            else if (button == buttonSouth)
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
            else if (button == buttonWest)
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, handpadRate);
            else if (button == buttonEast)
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
            else if (button == buttonNE)
            {
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
            }
            else if (button == buttonNW)
            {
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
            }
            else if (button == buttonSE)
            {
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
            }
            else if (button == buttonSW)
            {
                wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                wisetele.MoveAxis(TelescopeAxes.axisPrimary, handpadRate);
            }
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            wisetele.Stop();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void buttonStopStudy_Click(object sender, EventArgs e)
        {
            wisetele.Stop();
        }

        /// <summary>
        /// For movement-study ONLY.  Implements a timed-movement (as opposed to a measured-movement in the driver)
        /// </summary>
        /// <returns></returns>
        List<TimedMovementResult> MakeStepsInTheBackground(BackgroundWorker bgw, TimedMovementArg arg)
        {
            List<TimedMovementResult> bgResults = new List<TimedMovementResult>();
            Const.AxisDirection direction = arg.rate < 0 ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            for (int stepNo = 0; stepNo < arg.nsteps; stepNo++)
            {
                int selector;
                TimedMovementResult res = new TimedMovementResult();
                res.cancelled = false;

                res.threadId = Thread.CurrentThread.ManagedThreadId;

                selector = (int)TimedMovementResult.ResultSelector.AtStart;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Value : wisetele.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromRadians((arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Angle.Radians : wisetele.DecEncoder.Angle.Radians);

                if (bgw.CancellationPending)
                {
                    res.cancelled = true;
                    bgResults.Add(res);
                    break;
                }

                wisetele.MoveAxis(arg.axis, arg.rate);
                for (long endTicks = DateTime.Now.Ticks + 10000 * arg.millis; DateTime.Now.Ticks < endTicks; Thread.Sleep(1))
                {
                    if (bgw.CancellationPending)
                    {
                        wisetele.Stop();
                        res.cancelled = true;
                        bgResults.Add(res);
                        goto Out;
                    }
                }

                selector = (int)TimedMovementResult.ResultSelector.AtStop;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Value : wisetele.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromDegrees((arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Angle.Degrees : wisetele.DecEncoder.Angle.Degrees);

                if (wisetele.simulated)    // move some more, to simulate telescope inertia
                {
                    wisetele.MoveAxis(arg.axis, arg.rate);
                    long deltaTicks = 10000 * (long)(WiseTele.Instance.movementParameters[arg.axis][arg.rate].stopMovement.Degrees * WiseTele.Instance.movementParameters[arg.axis][arg.rate].millisecondsPerDegree);
                    for (long endTicks = DateTime.Now.Ticks + deltaTicks; DateTime.Now.Ticks < endTicks; Thread.Sleep(10))
                    {
                        if (bgw.CancellationPending)
                        {
                            wisetele.Stop();
                            res.cancelled = true;
                            bgResults.Add(res);
                            goto Out;
                        }
                    }
                }

                wisetele.Stop();

                if (bgw.CancellationPending)
                {
                    res.cancelled = true;
                    bgResults.Add(res);
                    goto Out;
                }

                if (!wisetele.simulated)
                {
                    Thread.Sleep(10000);     // wait for real scope to stop

                    if (bgw.CancellationPending)
                    {
                        res.cancelled = true;
                        bgResults.Add(res);
                        goto Out;
                    }
                }

                selector = (int)TimedMovementResult.ResultSelector.AtIdle;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Value : wisetele.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromDegrees((arg.axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Angle.Degrees : wisetele.DecEncoder.Angle.Degrees);

                bgResults.Add(res);
            }

            Out:
            return bgResults;
        }

        private void scopeBackgroundMover_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                results = new List<TimedMovementResult>();
            else
                results = (List<TimedMovementResult>)e.Result;
            resultsAvailable = true;
        }

        private void scopeBackgroundMover_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgw = sender as BackgroundWorker;
            List<TimedMovementResult> results = MakeStepsInTheBackground(bgw, (TimedMovementArg)e.Argument);
            TimedMovementResult lastRes = results[results.Count - 1];

            if (lastRes.cancelled)
                e.Cancel = true;
            else
                e.Result = results;
        }

        private void MakeSteps(TelescopeAxes axis, double rate, int millis, int nsteps)
        {
            scopeBackgroundMover = new BackgroundWorker();

            scopeBackgroundMover.WorkerSupportsCancellation = true;
            scopeBackgroundMover.RunWorkerCompleted += new RunWorkerCompletedEventHandler(scopeBackgroundMover_RunWorkerCompleted);
            scopeBackgroundMover.DoWork += new DoWorkEventHandler(scopeBackgroundMover_DoWork);

            scopeBackgroundMover.RunWorkerAsync(new TimedMovementArg() { axis = axis, rate = rate, millis = millis, nsteps = nsteps });
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            int nSteps, millis;
            TelescopeAxes axis;
            double rate = 0.0;

            int.TryParse(numericUpDownStepCount.Text, out nSteps);
            int.TryParse(textBoxMillis.Text, out millis);

            if (nSteps <= 0)
            {
                MessageBox.Show("Number of steps must be at least 1.", "Error");
                return;
            }

            if (millis < 1)
            {
                MessageBox.Show("Millis must be at least 1.", "Error");
                return;
            }

            if (radioButtonAxisHA.Checked)
                axis = TelescopeAxes.axisPrimary;
            else
                axis = TelescopeAxes.axisSecondary;

            if (radioButtonSpeedGuide.Checked)
                rate = Const.rateGuide;
            else if (radioButtonSpeedSlew.Checked)
                rate = Const.rateSlew;
            else if (radioButtonSpeedSet.Checked)
                rate = Const.rateSet;

            if (radioButtonDirDown.Checked)
                rate = -rate;

            MakeSteps(axis, rate, millis, nSteps);
        }

        private void buttonSaveResults_Click(object sender, EventArgs e)
        {

            string path = @"c:/Logs/MovementStudy.txt";

            if (!File.Exists(path))
                using (StreamWriter sw = File.CreateText(path))
                    sw.WriteLine("# Movement Study Results for Wise40.");

            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine("");
                sw.WriteLine(string.Format("# {0}", DateTime.Now));

                foreach (TimedMovementResult res in results)
                {
                    sw.WriteLine(res.ToString());
                }
            }
        }

        private void displayTimer_Tick(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void buttonGoCoord_Click(object sender, EventArgs e)
        {
            if (!wisetele.Tracking)
            {
                MessageBox.Show("Telescope is NOT tracking!", "Error");
                return;
            }

            wisetele.SlewToCoordinatesAsync(new Angle(textBoxRA.Text).Hours, new Angle(textBoxDec.Text).Degrees);
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBoxRA_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBoxRA.Text = Angle.FromHours(wisetele.RightAscension).ToString();
        }

        private void textBoxDec_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBoxDec.Text = Angle.FromDegrees(wisetele.Declination).ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WiseDome.Instance.OpenShutter();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WiseDome.Instance.CloseShutter();
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void buttonWeather_Click(object sender, EventArgs e)
        {
            if (groupBoxWeather.Visible)
            {
                buttonWeather.Text = "Show Weather";
                groupBoxWeather.Visible = false;
            }
            else
            {
                buttonWeather.Text = "Hide Weather";
                groupBoxWeather.Visible = true;
            }
        }

        private void checkBoxEnslaveDome_CheckedChanged(object sender, EventArgs e)
        {
            wisetele._enslaveDome = checkBoxEnslaveDome.Checked;
        }
    }
}
