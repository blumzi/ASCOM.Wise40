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
using System.IO;

namespace ASCOM.Wise40
{
    public partial class HandpadForm : Form
    {
        public DaqsForm daqsForm;
        WiseTele T;
        private double handpadRate = WiseTele.rateSlew;
        private BackgroundWorker scopeBackgroundMover;

        private class TimedMovementArg {
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
                dEncoder = enc_value[curr] - enc_value[prev];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s = string.Format(" start-to-stop: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDeg(dDeg).ToFormattedString(Angle.Format.Deg));

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStop;
                dTime = time[curr].Subtract(time[prev]);
                dEncoder = enc_value[curr] - enc_value[prev];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s += string.Format("       inertia: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDeg(dDeg).ToFormattedString(Angle.Format.Deg));

                curr = (int)TimedMovementResult.ResultSelector.AtIdle;
                prev = (int)TimedMovementResult.ResultSelector.AtStart;
                dTime = time[curr].Subtract(time[prev]);
                dEncoder = enc_value[curr] - enc_value[prev];
                dDeg = enc_angle[curr].Degrees - enc_angle[prev].Degrees;
                s += string.Format("         total: dTime: {0} dDeg: {2} dEnc: {1}\r\n", dTime, dEncoder, Angle.FromDeg(dDeg).ToFormattedString(Angle.Format.Deg)); s += "\r\n";

                return s;
            }
        }

        private List<TimedMovementResult> results;
        private bool resultsAvailable = false;

        public HandpadForm()
        {
            InitializeComponent();
            T = WiseTele.Instance;
            checkBoxTrack.Checked = T.Tracking;
            //daqsForm = new DaqsForm(this);
            results = new List<TimedMovementResult>();
        }

        private void radioButtonSlew_Click(object sender, EventArgs e)
        {
            handpadRate = WiseTele.rateSlew;
        }

        private void radioButtonSet_Click(object sender, EventArgs e)
        {
            handpadRate = WiseTele.rateSet;
        }

        private void radioButtonGuide_Click(object sender, EventArgs e)
        {
            handpadRate = WiseTele.rateGuide;
        }

        private void checkBoxTrack_CheckedChanged(object sender, EventArgs e)
        {
            T.Tracking = ((CheckBox)sender).Checked;
        }

        private void buttonHardware_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            daqsForm = new DaqsForm(this);
            daqsForm.Visible = true;
        }

        private void buttonDome_Click(object sender, EventArgs e)
        {
            if (panelDome.Visible)
            {
                buttonDome.Text = "Show Dome";
                panelDome.Visible = false;
            }
            else {
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
            T.Stop();
            if (scopeBackgroundMover.IsBusy)
                scopeBackgroundMover.CancelAsync();
        }

        private void RefreshDisplay()
        {
            if (!panelControls.Visible)
                return;

            DateTime now = DateTime.Now;
            DateTime utc = now.ToUniversalTime();
            ASCOM.Utilities.Util u = new Utilities.Util();

            labelDate.Text = utc.ToLongDateString();
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\:mm\:ss\.f\ ");
            labelUTValue.Text = utc.TimeOfDay.ToString(@"hh\:mm\:ss\.f\ ");
            labelSiderealValue.Text = u.DegreesToHMS(WiseSite.Instance.LocalSiderealTime, ":", ":", "", 1);

            labelRightAscensionValue.Text = Angle.FromDeg(T.RightAscension).ToFormattedString(Angle.Format.RAhms);
            labelDeclinationValue.Text = Angle.FromDeg(T.Declination).ToFormattedString(Angle.Format.Dec);
            labelHourAngleValue.Text = Angle.FromDeg(T.HourAngle).ToFormattedString(Angle.Format.HAhms);

            labelAltitudeValue.Text = Angle.FromDeg(T.Altitude).ToFormattedString(Angle.Format.Alt);
            labelAzimuthValue.Text = Angle.FromDeg(T.Azimuth).ToFormattedString(Angle.Format.Az);

            labelHAEncValue.Text = T.HAEncoder.Value.ToString();
            labelDecEncValue.Text = T.DecEncoder.Value.ToString();

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
        }

        private void HandpadForm_VisibleChanged(object sender, EventArgs e)
        {
            displayTimer.Enabled = ((Form)sender).Visible ? true : false;
        }

        private void buttonHandpad_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            panelControls.Visible = false;
        }

        private void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            if (button == buttonNorth)
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Increasing);
            else if (button == buttonSouth)
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Decreasing);
            else if (button == buttonWest)
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Increasing);
            else if (button == buttonEast)
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Decreasing);
            else if (button == buttonNE)
            {
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Increasing);
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Decreasing);
            }
            else if (button == buttonNW)
            {
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Increasing);
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Increasing);
            }
            else if (button == buttonSE)
            {
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Decreasing);
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Decreasing);
            }
            else if (button == buttonSW)
            {
                T.MoveAxis(TelescopeAxes.axisSecondary, handpadRate, WiseTele.AxisDirection.Decreasing);
                T.MoveAxis(TelescopeAxes.axisPrimary, handpadRate, WiseTele.AxisDirection.Increasing);
            }
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            T.Stop();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void buttonStopStudy_Click(object sender, EventArgs e)
        {
            if (scopeBackgroundMover.IsBusy)
                scopeBackgroundMover.CancelAsync();
        }

        /// <summary>
        /// For movement-study ONLY.  Implements a timed-movement (as opposed to a measured-movement in the driver)
        /// </summary>
        /// <returns></returns>
        List<TimedMovementResult> MakeStepsInTheBackground(BackgroundWorker bgw, TimedMovementArg arg)
        {
            List<TimedMovementResult> bgResults = new List<TimedMovementResult>();
            WiseTele.AxisDirection direction = arg.rate < 0 ? WiseTele.AxisDirection.Decreasing : WiseTele.AxisDirection.Increasing;

            for (int stepNo = 0; stepNo < arg.nsteps; stepNo++)
            {
                int selector;
                TimedMovementResult res = new TimedMovementResult();
                res.cancelled = false;

                res.threadId = Thread.CurrentThread.ManagedThreadId;

                selector = (int)TimedMovementResult.ResultSelector.AtStart;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Value : T.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromRad((arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Angle.Radians : T.DecEncoder.Angle.Radians);

                if (bgw.CancellationPending)
                {
                    res.cancelled = true;
                    bgResults.Add(res);
                    break;
                }

                WiseTele.Instance.log("#{3} Before: MoveAxis({0}, {1}, {4}) for {2} millis", arg.axis, arg.rate, arg.millis, stepNo, direction);
                T.MoveAxis(arg.axis, Math.Abs(arg.rate), direction);
                for (long endTicks = DateTime.Now.Ticks + 10000 * arg.millis; DateTime.Now.Ticks < endTicks; Thread.Sleep(1))
                {
                    if (bgw.CancellationPending)
                    {
                        T.Stop();
                        res.cancelled = true;
                        bgResults.Add(res);
                        goto Out;
                    }
                }
                WiseTele.Instance.log("#{3} After loop: MoveAxis({0}, {1}, {4}) for {2} millis", arg.axis, arg.rate, arg.millis, stepNo, direction);

                selector = (int)TimedMovementResult.ResultSelector.AtStop;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Value : T.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromDeg((arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Angle.Degrees : T.DecEncoder.Angle.Degrees);

                if (T.simulated)    // move some more, to simulate telescope inertia
                {
                    T.MoveAxis(arg.axis, Math.Abs(arg.rate), direction);
                    long deltaTicks = 10000 * (long) (WiseTele.Instance.movementParameters[arg.axis][arg.rate].stopMovement.Degrees * WiseTele.Instance.movementParameters[arg.axis][arg.rate].millisecondsPerDegree);
                    for (long endTicks = DateTime.Now.Ticks + deltaTicks; DateTime.Now.Ticks < endTicks; Thread.Sleep(10))
                    {
                        if (bgw.CancellationPending)
                        {
                            T.Stop();
                            res.cancelled = true;
                            bgResults.Add(res);
                            goto Out;
                        }
                    }
                }

                T.Stop();

                WiseTele.Instance.log("#{3} After:  MoveAxis({0}, {1}) for {2} millis", arg.axis, arg.rate, arg.millis, stepNo);

                if (bgw.CancellationPending)
                {
                    res.cancelled = true;
                    bgResults.Add(res);
                    goto Out;
                }

                if (!T.simulated)
                {
                    Thread.Sleep(2000);     // wait for real scope to stop

                    if (bgw.CancellationPending)
                    {
                        res.cancelled = true;
                        bgResults.Add(res);
                        goto Out;
                    }
                }

                selector = (int)TimedMovementResult.ResultSelector.AtIdle;
                res.time[selector] = DateTime.Now;
                res.enc_value[selector] = (arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Value : T.DecEncoder.Value;
                res.enc_angle[selector] = Angle.FromDeg((arg.axis == TelescopeAxes.axisPrimary) ? T.HAEncoder.Angle.Degrees : T.DecEncoder.Angle.Degrees);

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

            scopeBackgroundMover.RunWorkerAsync(new TimedMovementArg() { axis = axis, rate = rate, millis = millis, nsteps =  nsteps});
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
                rate = WiseTele.rateGuide;
            else if (radioButtonSpeedSlew.Checked)
                rate = WiseTele.rateSlew;
            else if (radioButtonSpeedSet.Checked)
                rate = WiseTele.rateSet;

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
    }
}
