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
        private static Debugger debugger = Debugger.Instance;
        private static DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;
        private DateTime statusDisplayExpiration = new DateTime();
        public enum Severity { None, Good, Warning, Error };

        private class TimedMovementArg
        {
            public TelescopeAxes axis;
            public double rate;
            public int millis;
            public int nsteps;
        }

        private class TimedMovementResult
        {
            public Angle[] encoder_angle;
            public DateTime[] time;
            public int threadId;
            public bool cancelled;
            public TelescopeAxes axis;

            public enum ResultSelector { AtStart = 0, AtStop = 1, AtIdle = 2 };

            public TimedMovementResult(TelescopeAxes axis)
            {
                encoder_angle = new Angle[3];
                time = new DateTime[3];
                cancelled = false;
                this.axis = axis;
            }

            public override string ToString()
            {
                string s;
                int curr, prev;
                TimeSpan dTime;
                Angle dAngle;
                ShortestDistanceResult dist;

                curr = (int)TimedMovementResult.ResultSelector.AtStop;
                prev = (int)TimedMovementResult.ResultSelector.AtStart;
                dTime = time[curr].Subtract(time[prev]);dist = encoder_angle[curr].ShortestDistance(encoder_angle[prev]);
                dAngle = dist.angle;s = string.Format(" start-to-stop: dTime: {0} dAng: {1}\r\n", dTime, dAngle);

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStop;
                dTime = time[curr].Subtract(time[prev]);dist = encoder_angle[curr].ShortestDistance(encoder_angle[prev]);
                dAngle = dist.angle;s += string.Format("       inertia: dTime: {0} dAng: {1}\r\n", dTime, dAngle);

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStart;
                dTime = time[curr].Subtract(time[prev]);dist = encoder_angle[curr].ShortestDistance(encoder_angle[prev]);
                dAngle = dist.angle;s += string.Format("        total: dTime: {0} dAng: {1}\r\n", dTime, dAngle);

                return s;
            }
        }

        private List<TimedMovementResult> results;
        private bool resultsAvailable = false;

        public HandpadForm()
        {
            InitializeComponent();
            checkBoxTrack.Checked = wisetele.Tracking;
            results = new List<TimedMovementResult>();
            wisesite.init();
            WiseDome.Instance.init();

            groupBoxTelescope.Text = string.Format(" {0} - v{1} ", wisetele.Name, wisetele.DriverVersion);
            groupBoxWeather.Text = wisesite.observingConditions.Connected ?
                string.Format(" {0} - v{1} ", wisesite.observingConditions.Name, wisesite.observingConditions.DriverVersion) :
                " Weather - Not connected ";
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
                daqsForm = new DaqsForm();
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

            labelNewRA.Text = "";
            labelNewRARadians.Text = Angle.FromHours(wisetele.RightAscension).Radians.ToString();
            labelNewDec.Text = "";
            labelNewDecRadians.Text = Angle.FromDegrees(wisetele.Declination).Radians.ToString();

            labelAltitudeValue.Text = Angle.FromDegrees(wisetele.Altitude).ToNiceString();
            labelAzimuthValue.Text = Angle.FromDegrees(wisetele.Azimuth).ToNiceString();

            labelHAEncValue.Text = wisetele.HAEncoder.Value.ToString();
            labelDecEncValue.Text = wisetele.DecEncoder.Value.ToString();

            axisValue.Text = wisetele.HAEncoder.AxisValue.ToString();
            wormValue.Text = wisetele.HAEncoder.WormValue.ToString();

            labelComputerControl.ForeColor = wisesite.computerControl == null ? Color.Yellow : (wisesite.computerControl.IsSafe ? Color.Green : Color.Red);
            labelSafeToOpen.ForeColor = wisesite.safeToOpen == null ? Color.Yellow : (wisesite.safeToOpen.IsSafe ? Color.Green : Color.Red);
            labelSafeToImage.ForeColor = wisesite.safeToImage == null ? Color.Yellow : (wisesite.safeToImage.IsSafe ? Color.Green : Color.Red);

            checkBoxPrimaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisPrimary);
            checkBoxSecondaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisSecondary);
            string activeSlewers = Wise40.WiseTele.activeSlewers.ToString();
            checkBoxSlewingIsActive.Text = (activeSlewers == string.Empty) ? "Slewing" : "Slewing (" + activeSlewers + ")";
            checkBoxSlewingIsActive.Checked = wisetele.Slewing;
            checkBoxTrackingIsActive.Checked = wisetele.Tracking;

            WiseVirtualMotor m;

            m = null;
            if (wisetele.WestMotor.isOn)
                m = wisetele.WestMotor;
            else if (wisetele.EastMotor.isOn)
                m = wisetele.EastMotor;

            checkBoxPrimaryIsActive.Text = "Primary";
            if (m != null)
                checkBoxPrimaryIsActive.Text += ": " + m.Name.Remove(m.Name.IndexOf('M')) + "@" +
                    WiseTele.RateName(m.currentRate).Replace("rate", "");

            m = null;
            if (wisetele.NorthMotor.isOn)
                m = wisetele.NorthMotor;
            else if (wisetele.SouthMotor.isOn)
                m = wisetele.SouthMotor;


            checkBoxSecondaryIsActive.Text = "Secondary";
            if (m != null)
                checkBoxSecondaryIsActive.Text += ": " + m.Name.Remove(m.Name.IndexOf('M')) + "@" +
                    WiseTele.RateName(m.currentRate).Replace("rate", "");

            checkBoxTrack.Checked = wisetele.Tracking;

            if (scopeBackgroundMover != null && scopeBackgroundMover.IsBusy)
                TextBoxLog.Text = "Working ...";

            if (resultsAvailable)
            {
                TextBoxLog.Clear();
                if (results.Count == 0)
                    TextBoxLog.Text = "Cancelled by user!";
                else
                {
                    TelescopeAxes axis = results[0].axis;

                    for (int i = 0; i < results.Count; i++)
                    {
                        TextBoxLog.Text += string.Format("[{0}]: ({2})\r\n{1}",
                            i, results[i].ToString(), results[i].cancelled ? "cancelled" : "completed");
                    }
                }
                resultsAvailable = false;
            }

            if (panelDome.Visible)
            {
                labelDomeAzimuthValue.Text = domeSlaveDriver.Azimuth;
                labelDomeStatusValue.Text = domeSlaveDriver.Status;
                labelDomeShutterStatusValue.Text = domeSlaveDriver.ShutterStatus;
            }

            if (groupBoxWeather.Visible)
            {
                if (!wisesite.observingConditions.Connected)
                {
                    string nc = "???";

                    labelAgeValue.Text = nc;
                    labelCloudCoverValue.Text = nc;
                    labelCloudCoverValue.Text = nc;
                    labelDewPointValue.Text = nc;
                    labelSkyTempValue.Text = nc;
                    labelTempValue.Text = nc;
                    labelHumidityValue.Text = nc;
                    labelPressureValue.Text = nc;
                    labelRainRateValue.Text = nc;
                    labelWindSpeedValue.Text = nc;
                    labelWindDirValue.Text = nc;
                }
                else
                {
                    try
                    {
                        ObservingConditions oc = wisesite.observingConditions;

                        labelAgeValue.Text = ((int)Math.Round(oc.TimeSinceLastUpdate(""), 2)).ToString() + "sec";

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
                    catch (PropertyNotImplementedException e)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OC: exception: {0}", e.Message);
                    }
                }
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

        public void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            try
            {
                if (button == buttonNorth)
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                else if (button == buttonSouth)
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                else if (button == buttonEast)
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, handpadRate);
                else if (button == buttonWest)
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
            } catch (Exception ex)
            {
                Status(ex.Message, 2000, Severity.Error);
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
                TimedMovementResult res = new TimedMovementResult(arg.axis);
                res.cancelled = false;

                res.threadId = Thread.CurrentThread.ManagedThreadId;

                selector = (int)TimedMovementResult.ResultSelector.AtStart;
                res.time[selector] = DateTime.Now;res.encoder_angle[selector] = arg.axis == TelescopeAxes.axisPrimary ?
                    Angle.FromHours(wisetele.RightAscension) :
                    Angle.FromDegrees(wisetele.Declination);

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
                res.time[selector] = DateTime.Now;res.encoder_angle[selector] = arg.axis == TelescopeAxes.axisPrimary ?
                     Angle.FromHours(wisetele.RightAscension) :
                     Angle.FromDegrees(wisetele.Declination);

                if (wisetele.Simulated)    // move some more, to simulate telescope inertia
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

                while (wisetele.AxisIsMoving(arg.axis))
                {

                    if (bgw.CancellationPending)
                    {
                        res.cancelled = true;
                        bgResults.Add(res);
                        goto Out;
                    }
                    Thread.Sleep(500);
                }

                selector = (int)TimedMovementResult.ResultSelector.AtIdle;
                res.time[selector] = DateTime.Now;res.encoder_angle[selector] = arg.axis == TelescopeAxes.axisPrimary ?
                     Angle.FromHours(wisetele.RightAscension) :
                     Angle.FromDegrees(wisetele.Declination);

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

            try
            {
                MakeSteps(axis, rate, millis, nSteps);
            } catch (Exception ex)
            {
                Status(ex.Message, 2000, Severity.Warning);
            }
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
                Status("Telescope is NOT tracking!", 1000, Severity.Error);
                return;
            }

            try
            {
                wisetele.SlewToCoordinatesAsync(new Angle(textBoxRA.Text).Hours, new Angle(textBoxDec.Text).Degrees);
            } catch (Exception ex)
            {
                Status(ex.Message, 2000, Severity.Error);
            }
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

        private void buttonOpenShutterClick(object sender, EventArgs e)
        {
            domeSlaveDriver.OpenShutter();
        }

        private void buttonCloseShutterClick(object sender, EventArgs e)
        {
            domeSlaveDriver.CloseShutter();
        }

        private void Status(string s, int millis = 0, Severity severity = Severity.None)
        {
            Dictionary<Severity, Color> colors = new Dictionary<Severity, Color>()
            {
                { Severity.None, Color.FromArgb(176, 161, 142) },
                { Severity.Warning, Color.LightGoldenrodYellow },
                { Severity.Error, Color.IndianRed },
                { Severity.Good, Color.Green },
            };

            labelStatus.ForeColor = colors[severity];
            labelStatus.Text = s;
            if (millis > 0)
            {
                statusDisplayExpiration = DateTime.Now.AddMilliseconds(millis);
                timerStatus.Start();
            }
        }

        private void timerStatus_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            if (now.CompareTo(statusDisplayExpiration) > 0)
            {
                labelStatus.Text = "";
                statusDisplayExpiration = now;
                timerStatus.Stop();
            }
        }

        private void labelConfDomeSlaved_Click(object sender, EventArgs e)
        {

        }

        private void buttonSafety_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            if (groupBoxSafety.Visible)
            {
                buttonSafety.Text = "Show safety";
                groupBoxSafety.Visible = false;
            }
            else
            {
                buttonSafety.Text = "Hide safety";
                groupBoxSafety.Visible = true;
            }
        }

        private void textBoxDomeAzGo_Validated(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == string.Empty)
                return;
            double az = Convert.ToDouble(tb.Text);

            if (az < 0.0 || az >= 360.0)
                tb.Text = "";
        }

        private void buttonDomeAzGo_Click(object sender, EventArgs e)
        {
            if (textBoxDomeAzGo.Text == string.Empty)
                return;

            double az = Convert.ToDouble(textBoxDomeAzGo.Text);
            if (az < 0.0 || az >= 360.0)
                textBoxDomeAzGo.Text = "";

            wisetele.DomeSlewer(az);
        }

        private void buttonDomeLeft_MouseDown(object sender, MouseEventArgs e)
        {
            WiseDome.Instance.StartMovingCCW();
        }

        private void buttonDomeRight_MouseDown(object sender, MouseEventArgs e)
        {
            WiseDome.Instance.StartMovingCW();
        }

        private void buttonDomeRight_MouseUp(object sender, MouseEventArgs e)
        {
            WiseDome.Instance.Stop();
        }

        private void buttonDomeLeft_MouseUp(object sender, MouseEventArgs e)
        {
            WiseDome.Instance.Stop();
        }

        private void buttonDomeStop_Click(object sender, EventArgs e)
        {
            WiseDome.Instance.Stop();
        }
    }
}
