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

using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.SafeToOperate;

namespace Dash
{
    public partial class FormDash : Form
    {
        public WiseTele wisetele = WiseTele.Instance;
        public WiseDome wisedome = WiseDome.Instance;
        public WiseFocuser wisefocuser = WiseFocuser.Instance;
        Hardware hardware = Hardware.Instance;
        public WiseSite wisesite = WiseSite.Instance;
        public WiseSafeToOperate wisesafetoopen = WiseSafeToOperate.Instance(WiseSafeToOperate.Operation.Open);
        public ObservingConditions boltwood = new ASCOM.DriverAccess.ObservingConditions("ASCOM.CloudSensor.ObservingConditions");

        DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;
        DebuggingForm debuggingForm = new DebuggingForm();
        Debugger debugger = Debugger.Instance;

        Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, weatherStatus;

        private double handpadRate = Const.rateSlew;
        private bool _bypassSafety = false;

        private List<ToolStripMenuItem> debugMenuItems;
        private Dictionary<object, string> alteredItems = new Dictionary<object, string>();

        #region Initialization
        public FormDash()
        {
            debugger.init();
            hardware.init();
            wisetele.init();
            wisetele.Connected = true;
            wisedome.init();
            wisedome.Connected = true;
            wisefocuser.init();
            wisefocuser.Connected = true;
            wisesafetoopen.init();
            wisesafetoopen.Connected = true;
            boltwood.Connected = true;

            InitializeComponent();

            debugMenuItems = new List<ToolStripMenuItem> {
                debugASCOMToolStripMenuItem ,
                debugAxesToolStripMenuItem,
                debugDeviceToolStripMenuItem,
                debugEncodersToolStripMenuItem,
                debugExceptionsToolStripMenuItem,
                debugLogicToolStripMenuItem,
            };

            dashStatus = new Statuser(labelDashStatus);
            telescopeStatus = new Statuser(labelTelescopeStatus);
            domeStatus = new Statuser(labelDomeStatus);
            shutterStatus = new Statuser(labelDomeShutterStatus);
            focuserStatus = new Statuser(labelFocuserStatus);
            weatherStatus = new Statuser(labelWeatherStatus, toolTip);

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            telescopeStatus.Show("");
            focuserStatus.Show("");
            weatherStatus.Show("");
            
            buttonVent.Text = wisedome.Vent ? "Close Vent" : "Open Vent";

            List<ToolStripMenuItem> checkedItems = new List<ToolStripMenuItem>();
            if (debugger.Debugging(Debugger.DebugLevel.DebugASCOM))
                checkedItems.Add(debugASCOMToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugDevice))
                checkedItems.Add(debugAxesToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugAxes))
                checkedItems.Add(debugAxesToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugLogic))
                checkedItems.Add(debugLogicToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugEncoders))
                checkedItems.Add(debugEncodersToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugExceptions))
                checkedItems.Add(debugExceptionsToolStripMenuItem);

            if (debugger.Tracing)
                checkedItems.Add(tracingToolStripMenuItem);
            if (wisedome._autoCalibrate)
                checkedItems.Add(domeAutoCalibrateToolStripMenuItem);
            if (wisetele._enslaveDome)
                checkedItems.Add(enslaveDomeToolStripMenuItem);

            foreach (var item in checkedItems)
            {
                item.Text += Const.checkmark;
                item.Tag = true;
                item.Invalidate();
            }

            ToolStripTextBox tb;
            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                string dataFile = driverProfile.GetValue("ASCOM.CloudSensor.ObservingConditions", "Data File", string.Empty, string.Empty);
                tb = toolStripTextBoxCloudSensorDataFile;

                tb.MaxLength = 256;
                tb.Size = new System.Drawing.Size(300, 25);
                tb.Text = dataFile;
                tb.Tag = dataFile;
                tb.BackColor = Color.FromArgb(64, 64, 64);
                tb.ForeColor = Color.FromArgb(176, 161, 142);
            }


            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                string reportFile = driverProfile.GetValue("ASCOM.Vantage.ObservingConditions", "Report File", string.Empty, string.Empty);
                tb = toolStripTextBoxVantagePro2ReportFile;

                tb.MaxLength = 256;
                tb.Size = new System.Drawing.Size(300, 25);
                tb.Text = reportFile;
                tb.Tag = reportFile;
                tb.BackColor = Color.FromArgb(64, 64, 64);
                tb.ForeColor = Color.FromArgb(176, 161, 142);
            }
        }
        #endregion

        #region Refresh
        public void RefreshDisplay(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime utcTime = now.ToUniversalTime();
            DateTime localTime = now.ToLocalTime();
            ASCOM.Utilities.Util u = new ASCOM.Utilities.Util();

            labelDate.Text = localTime.ToLongDateString() + Const.crnl + localTime.ToLongTimeString();

            #region RefreshTelescope
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utcTime.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelSiderealValue.Text = wisesite.LocalSiderealTime.ToString();

            labelRightAscensionValue.Text = Angle.FromHours(wisetele.RightAscension).ToNiceString();
            labelDeclinationValue.Text = Angle.FromDegrees(wisetele.Declination).ToNiceString();
            labelHourAngleValue.Text = Angle.FromHours(wisetele.HourAngle, Angle.Type.HA).ToNiceString();

            labelAltitudeValue.Text = Angle.FromDegrees(wisetele.Altitude).ToNiceString();
            labelAzimuthValue.Text = Angle.FromDegrees(wisetele.Azimuth).ToNiceString();

            buttonTelescopePark.Text = wisetele.AtPark ? "Unpark" : "Park";

            checkBoxPrimaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisPrimary);
            checkBoxSecondaryIsActive.Checked = wisetele.AxisIsMoving(TelescopeAxes.axisSecondary);
            checkBoxSlewingIsActive.Text = (wisetele.slewers.Count == 0) ? "Slewing" :
                "Slewing (" + wisetele.slewers.ToString() + ")";
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

            telescopeStatus.Show(wisetele.Status);

            #endregion

            #region RefreshSafety
            string tip;
            if (wisesite.computerControl == null)
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                tip = "Cannot read the computer control switch!";
            }
            else if (wisesite.computerControl.IsSafe)
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Good];
                tip = "Computer control is enabled.";
            }
            else
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Error];
                tip = "Computer control switch is OFF!";
            }
            toolTip.SetToolTip(labelDashComputerControl, tip);

            tip = null;
            if (_bypassSafety)
            {
                labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Good];
                tip = "Safety is bypassed (from Settings)";
            }
            else
            {
                if (wisesite.safeToOpen == null)
                {
                    labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                    tip = "Cannot connect to the safeToOpen driver!";
                }
                else if (wisesite.safeToOpen.IsSafe)
                {
                    labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Good];
                    tip = "Conditions are safe to open the dome.";
                }
                else
                {
                    labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Error];
                    //tip = wisesite.safeToOpen.CommandString("unsafeReasons", false);
                }
            }
            toolTip.SetToolTip(labelDashSafeToOpen, tip);

            tip = null;
            if (_bypassSafety)
            {
                labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Good];
                tip = "Safety is bypassed (from Settings)";
            }
            else
            {
                if (wisesite.safeToImage == null)
                {
                    labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                    tip = "Cannot connect to the safeToImage driver!";
                }
                else if (wisesite.safeToImage.IsSafe)
                {
                    labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Good];
                    tip = "Conditions are safe to image.";
                }
                else
                {
                    labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Error];
                    //tip = wisesite.safeToImage.CommandString("unsafeReasons", false);
                }
            }
            toolTip.SetToolTip(labelDashSafeToImage, tip);
            #endregion

            #region RefreshDome
            labelDomeAzimuthValue.Text = domeSlaveDriver.Azimuth;
            domeStatus.Show(domeSlaveDriver.Status);
            //if (labelDomeShutterStatus.Text == string.Empty)
                shutterStatus.Show(domeSlaveDriver.ShutterStatus);
            buttonDomePark.Text = wisedome.AtPark ? "Unpark" : "Park";
            buttonVent.Text = wisedome.Vent ? "Close Vent" : "Open Vent";
            #endregion

            #region RefreshWeather
            if (!wisesite.observingConditions.Connected)
            {
                string nc = "???";

                List<Label> labels = new List<Label>() {
                    labelAgeValue,
                    labelCloudCoverValue,
                    labelDewPointValue,
                    labelSkyTempValue,
                    labelTempValue,
                    labelHumidityValue,
                    labelPressureValue,
                    labelRainRateValue,
                    labelWindSpeedValue,
                    labelWindDirValue,
                };

                foreach (var label in labels)
                {
                    label.Text = nc;
                    label.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                }
            }
            else
            {
                try
                {
                    ObservingConditions oc = wisesite.observingConditions;

                    #region ObservingConditions Informational
                    labelAgeValue.Text = ((int)Math.Round(oc.TimeSinceLastUpdate(""), 2)).ToString() + "sec";
                    labelDewPointValue.Text = oc.DewPoint.ToString() + "°C";
                    labelSkyTempValue.Text = oc.SkyTemperature.ToString() + "°C";
                    labelTempValue.Text = oc.Temperature.ToString() + "°C";
                    labelPressureValue.Text = oc.Pressure.ToString() + "mB";
                    labelWindDirValue.Text = oc.WindDirection.ToString() + "°";
                    #endregion

                    #region ObservingConditions governed by SafeToOpen
                    labelHumidityValue.Text = oc.Humidity.ToString() + "%";
                    labelHumidityValue.ForeColor = Statuser.TriStateColor(wisesafetoopen.isSafeHumidity);

                    double d = oc.CloudCover;
                    if (d == 0.0)
                        labelCloudCoverValue.Text = "Clear";
                    else if (d == 50.0)
                        labelCloudCoverValue.Text = "Cloudy";
                    else if (d == 90.0)
                        labelCloudCoverValue.Text = "VeryCloudy";
                    else
                        labelCloudCoverValue.Text = "Unknown";
                    labelCloudCoverValue.ForeColor = Statuser.TriStateColor(wisesafetoopen.isSafeCloudCover);

                    string light = boltwood.CommandString("daylight", true);
                    labelLightValue.Text = light;
                    labelLightValue.ForeColor = Statuser.TriStateColor(wisesafetoopen.isSafeLight);

                    labelWindSpeedValue.Text = oc.WindSpeed.ToString() + "m/s";
                    labelWindSpeedValue.ForeColor = Statuser.TriStateColor(wisesafetoopen.isSafeWindSpeed);

                    labelRainRateValue.Text = (oc.RainRate > 0.0) ? "Wet" : "Dry";
                    labelRainRateValue.ForeColor = Statuser.TriStateColor(wisesafetoopen.isSafeRain);

                    if (wisesafetoopen.IsSafe)
                    {
                        weatherStatus.SetToolTip("");
                    } else
                    {
                        if (_bypassSafety)
                            weatherStatus.Show("Safe to open (safety bypassed)", 0, Statuser.Severity.Good);
                        else
                            weatherStatus.Show("Not safe to open", 0, Statuser.Severity.Error, true);
                        weatherStatus.SetToolTip(string.Join(Const.crnl, wisesafetoopen.UnsafeReasons));
                    }
                    #endregion
                }
                catch (ASCOM.PropertyNotImplementedException ex)
                {
                    weatherStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
                }
            }
            #endregion

            #region RefreshFocuser
            labelFocusCurrentValue.Text = wisefocuser.position.ToString();
            if (labelFocuserStatus.Text == string.Empty)
                focuserStatus.Show(wisefocuser.Status);
            #endregion
        }
        #endregion

        #region MainMenu
        private void digitalIOCardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ASCOM.Wise40.HardwareForm hardwareForm = new ASCOM.Wise40.HardwareForm();
            hardwareForm.Visible = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm form = new AboutForm(this);
            form.Visible = true;
        }
        #endregion

        #region TelescopeControl
        public void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            try
            {
                string atRate = string.Format(" at rate {0}", WiseTele.RateName(handpadRate).Remove(0, 4));

                if (button == buttonNorth)
                {
                    telescopeStatus.Show("Moving North" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                }
                else if (button == buttonSouth)
                {
                    telescopeStatus.Show("Moving South" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                }
                else if (button == buttonEast)
                {
                    telescopeStatus.Show("Moving East" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, handpadRate);
                }
                else if (button == buttonWest)
                {
                    telescopeStatus.Show("Moving West" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
                }
                else if (button == buttonNE)
                {
                    telescopeStatus.Show("Moving North-East" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
                }
                else if (button == buttonNW)
                {
                    telescopeStatus.Show("Moving North-West" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, handpadRate);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
                }
                else if (button == buttonSE)
                {
                    telescopeStatus.Show("Moving South-East" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, -handpadRate);
                }
                else if (button == buttonSW)
                {
                    telescopeStatus.Show("Moving North-West" + atRate, 0, Statuser.Severity.Good);
                    wisetele.MoveAxis(TelescopeAxes.axisSecondary, -handpadRate);
                    wisetele.MoveAxis(TelescopeAxes.axisPrimary, handpadRate);
                }
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            wisetele.Stop();
            telescopeStatus.Show("Stopped", 1000, Statuser.Severity.Good);
        }

        private void debuggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debuggingForm.IsDisposed || debuggingForm == null)
                debuggingForm = new DebuggingForm();
            debuggingForm.Visible = true;
        }

        private void buttonGoCoord_Click(object sender, EventArgs e)
        {
            if (!wisetele.Tracking)
            {
                telescopeStatus.Show("Telescope is NOT tracking!", 1000, Statuser.Severity.Error);
                return;
            }

            try
            {
                string raText = textBoxRA.Text.Replace(':', 'h').Replace(':', 'm') + 's';

                telescopeStatus.Show(string.Format("Slewing to ra: {0} dec: {1}", new Angle(raText), new Angle(textBoxDec.Text)),
                    0, Statuser.Severity.Good);
                wisetele.SlewToCoordinatesAsync(new Angle(raText).Hours, new Angle(textBoxDec.Text).Degrees);
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void buttonTelescopeStop_Click(object sender, EventArgs e)
        {
            wisetele.Stop();
            telescopeStatus.Show("Stopped", 1000, Statuser.Severity.Good);
        }

        private void textBoxRA_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBoxRA.Text = Angle.FromHours(wisetele.RightAscension).ToString().Replace('h', ':').Replace('m', ':').Replace('s', ' ');
        }

        private void textBoxDec_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBoxDec.Text = Angle.FromDegrees(wisetele.Declination).ToString();
        }

        private void checkBoxTrack_CheckedChanged(object sender, EventArgs e)
        {
            wisetele.Tracking = ((CheckBox)sender).Checked;
            telescopeStatus.Show((wisetele.Tracking ? "Started" : "Stopped") + " tracking", 1000, Statuser.Severity.Good);
        }

        private void radioButtonSlew_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateSlew;
            telescopeStatus.Show("Selected 'Slew' handpad rate", 1000, Statuser.Severity.Good);
        }

        private void radioButtonSet_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateSet;
            telescopeStatus.Show("Selected 'Set' handpad rate", 1000, Statuser.Severity.Good);
        }

        private void radioButtonGuide_Click(object sender, EventArgs e)
        {
            handpadRate = Const.rateGuide;
            telescopeStatus.Show("Selected 'Guide' handpad rate", 1000, Statuser.Severity.Good);
        }

        private void buttonTelescopePark_Click(object sender, EventArgs e)
        {
            try
            {
                if (wisetele.AtPark)
                    wisetele.AtPark = false;
                else
                {
                    bool saveTracking = wisetele.Tracking;
                    wisetele.Tracking = true;
                    telescopeStatus.Show("Parking", 0, Statuser.Severity.Good);
                    wisetele.ParkFromGui();
                    while (wisetele.Slewing)
                    {
                        Application.DoEvents();
                    }
                    wisetele.AtPark = true;
                    wisetele.Tracking = saveTracking;
                    telescopeStatus.Show("");
                }
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }
        #endregion

        #region ShutterControl

        private void _startMovingShutter(bool open)
        {
            try
            {
                if (open)
                    domeSlaveDriver.OpenShutter(_bypassSafety);
                else
                    domeSlaveDriver.CloseShutter();
            }
            catch (Exception ex)
            {
                shutterStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void MoveShutterClick(object sender, EventArgs e)
        {
            Button button = sender as Button;

            try
            {
                if (button == buttonFullOpenShutter)
                {
                    shutterStatus.Show("Started opening shutter", 1000, Statuser.Severity.Good);
                    _startMovingShutter(true);
                }
                else if (button == buttonFullCloseShutter)
                {
                    shutterStatus.Show("Started closing shutter", 1000, Statuser.Severity.Good);
                    _startMovingShutter(false);
                }
            }
            catch (Exception ex)
            {
                shutterStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void MoveShutterClick(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;

            try
            {
                if (button == buttonOpenShutter)
                {
                    shutterStatus.Show("Opening shutter", 0, Statuser.Severity.Good);
                    _startMovingShutter(true);
                }
                else if (button == buttonCloseShutter)
                {
                    shutterStatus.Show("Closing shutter", 0, Statuser.Severity.Good);
                    _startMovingShutter(false);
                }
            }
            catch (Exception ex)
            {
                shutterStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void buttonStopShutter_Click(object sender, EventArgs e)
        {
            try
            {
                domeSlaveDriver.StopShutter();
                shutterStatus.Show("Stopped shutter", 1000, Statuser.Severity.Good);
            }
            catch (Exception ex)
            {
                shutterStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void buttonStopShutter_Click(object sender, MouseEventArgs e)
        {
            try
            {
                domeSlaveDriver.StopShutter();
                shutterStatus.Show("Stopped shutter", 1000, Statuser.Severity.Good);
            }
            catch (Exception ex)
            {
                shutterStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }
        #endregion

        #region DomeControl
        private void textBoxDomeAzGo_Validated(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == string.Empty)
                return;
            double az = Convert.ToDouble(tb.Text);

            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show(string.Format("Invalid azimuth: {0}", tb.Text), 2000, Statuser.Severity.Error);
                tb.Text = "";
            }
        }
        private void buttonDomeAzSet_Click(object sender, EventArgs e)
        {
            if (textBoxDomeAzValue.Text == string.Empty)
                return;

            double az = Convert.ToDouble(textBoxDomeAzValue.Text);
            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show(string.Format("Invalid azimuth: {0}", textBoxDomeAzValue.Text), 2000, Statuser.Severity.Error);
                textBoxDomeAzValue.Text = "";
            }

            wisedome.Azimuth = Angle.FromDegrees(az, Angle.Type.Az);
        }

        private void buttonDomeAzGo_Click(object sender, EventArgs e)
        {
            if (textBoxDomeAzValue.Text == string.Empty)
                return;

            double az = Convert.ToDouble(textBoxDomeAzValue.Text);
            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show(string.Format("Invalid azimuth: {0}", textBoxDomeAzValue.Text), 2000, Statuser.Severity.Error);
                textBoxDomeAzValue.Text = "";
            }

            wisetele.DomeSlewer(az);
        }

        private void buttonDomeLeft_MouseDown(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Moving CCW", 0, Statuser.Severity.Good);
            wisedome.StartMovingCCW();
        }

        private void buttonDomeRight_MouseDown(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Moving CW", 0, Statuser.Severity.Good);
            wisedome.StartMovingCW();
        }

        private void buttonDomeRight_MouseUp(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Stopped moving CW", 1000, Statuser.Severity.Good);
            wisedome.Stop();
        }

        private void buttonDomeLeft_MouseUp(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Stopped moving CCW", 1000, Statuser.Severity.Good);
            wisedome.Stop();
        }

        private void buttonDomeStop_Click(object sender, EventArgs e)
        {
            domeStatus.Show("Stopped moving", 1000, Statuser.Severity.Good);
            wisedome.Stop();
            wisetele.DomeStopper();
        }

        private void buttonCalibrateDome_Click(object sender, EventArgs e)
        {
            domeStatus.Show("Started dome calibration", 1000, Statuser.Severity.Good);
            wisetele.DomeCalibrator();
        }

        private void buttonVent_Click(object sender, EventArgs e)
        {
            if (wisedome.Vent)
            {
                wisedome.Vent = false;
                domeStatus.Show("Closed dome vent", 1000, Statuser.Severity.Good);
                buttonVent.Text = "Open Vent";
            }
            else
            {
                wisedome.Vent = true;
                domeStatus.Show("Opened dome vent", 1000, Statuser.Severity.Good);
                buttonVent.Text = "Close Vent";
            }
        }

        private void buttonDomePark_Click(object sender, EventArgs e)
        {
            try
            {
                if (wisedome.AtPark)
                    wisedome.AtPark = false;
                else
                    wisetele.DomeParker();
            }
            catch (Exception ex)
            {
                domeStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }
        #endregion

        #region FocuserControl
        private void focuserHalt(object sender, MouseEventArgs e)
        {
            wisefocuser.Halt();
        }

        private void buttonFocuserStop_Click(object sender, EventArgs e)
        {
            wisefocuser.Stop();
        }

        private void buttonFocusUp_MouseDown(object sender, MouseEventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.Up);
        }

        private void buttonFocusDown_MouseDown(object sender, MouseEventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.Down);
        }

        private void buttonFocusStop(object sender, MouseEventArgs e)
        {
            wisefocuser.Stop();
        }

        private void buttonFocusGoto_Click(object sender, EventArgs e)
        {
            if (textBoxFocusGotoPosition.Text == string.Empty)
                return;

            try
            {
                wisefocuser.Move(Convert.ToInt32(textBoxFocusGotoPosition.Text));
            } catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void textBoxFocusGotoPosition_Validated(object sender, EventArgs e)
        {
            TextBox box = (sender as TextBox);

            if (box.Text == string.Empty)
                return;

            int pos = Convert.ToInt32(box.Text);

            if (pos < 0 || pos >= wisefocuser.MaxStep)
            {
                focuserStatus.Show("Bad focuser target position", 1000, Statuser.Severity.Error);
                box.Text = string.Empty;
            }
        }

        private void buttonFocusAllUp_Click(object sender, EventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.AllUp);
        }

        private void buttonFocusAllDown_Click(object sender, EventArgs e)
        {
            if (wisefocuser.Position > 0)
                wisefocuser.Move(WiseFocuser.Direction.AllDown);
        }
        #endregion

        #region Settings
        private void debugAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.StartDebugging(Debugger.DebugLevel.DebugAll);

            foreach (var item in debugMenuItems)
            {
                if (! item.Text.EndsWith(Const.checkmark))
                    item.Text += Const.checkmark;
            }
        }

        private void domeAutoCalibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string text = item.Text;

            if (item.Text.EndsWith(Const.checkmark))
            {
                wisedome._autoCalibrate = false;
                item.Text = text.Remove(text.Length - Const.checkmark.Length);
            } else
            {
                wisedome._autoCalibrate = true;
                item.Text = text + Const.checkmark;
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Dome");
        }

        private void enslaveDomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item.Text.EndsWith(Const.checkmark))
            {
                wisetele._enslaveDome = false;
                item.Text.Remove(item.Text.Length - Const.checkmark.Length);
            } else
            {
                wisetele._enslaveDome = true;
                item.Text += Const.checkmark;
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Telescope");
        }

        private void buttonZenith_Click(object sender, EventArgs e)
        {
            bool savedEnslaveDome = wisetele._enslaveDome;
            double ra = wisesite.LocalSiderealTime.Hours;
            double dec = 90.0 - wisesite.Latitude.Degrees;

            wisetele._enslaveDome = false;
            wisetele.Tracking = true;
            try
            {
                wisetele.SlewToCoordinatesAsync(ra, dec, false);
            } catch(Exception ex)
            {
                telescopeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
            wisetele._enslaveDome = savedEnslaveDome;
            wisetele.Tracking = false;
        }

        private void MoveToPresetCoords(Angle ha, Angle dec)
        {
            Angle ra = wisesite.LocalSiderealTime - ha;
            bool savedEnslaveDome = wisetele._enslaveDome;

            wisetele._enslaveDome = false;
            wisetele.Tracking = true;
            try
            {
                wisetele.SlewToCoordinatesAsync(ra.Hours, dec.Degrees, false);
                wisetele.DomeSlewer(90.0);
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
            wisetele._enslaveDome = savedEnslaveDome;
            wisetele.Tracking = false;
        }

        private void buttonHandleCover_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to cover station", 2000, Statuser.Severity.Good);
            MoveToPresetCoords(new Angle("11h55m00.0s"), new Angle("88:00:00.0"));
        }

        private void buttonFlat_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to Zenith", 2000, Statuser.Severity.Good);
            MoveToPresetCoords(new Angle("-1h35m59.0s"), new Angle("41:59:20.0"));
        }

        private void checkBoxTrack_Click(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;

            wisetele.Tracking = box.Checked;
        }

        private void debugNoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.StopDebugging(Debugger.DebugLevel.DebugAll);

            foreach (var item in debugMenuItems)
            {
                if (item.Text.EndsWith(Const.checkmark))
                    item.Text = item.Text.Remove(item.Text.IndexOf(' '));
            }
        }

        private void wise40WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/blumzi/ASCOM.Wise40/wiki");
        }

        private void debugSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            Debugger.DebugLevel selectedLevel = Debugger.DebugLevel.DebugNone;

            if (item == debugASCOMToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugASCOM;
            else if (item == debugAxesToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugDevice;
            else if (item == debugAxesToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugAxes;
            else if (item == debugEncodersToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugEncoders;
            else if (item == debugExceptionsToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugExceptions;
            else if (item == debugLogicToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugLogic;

            if (debugger.Debugging(selectedLevel))
            {
                item.Text = item.Text.Remove(item.Text.IndexOf(' '));
                debugger.StopDebugging(selectedLevel);
            }
            else
            {
                item.Text += Const.checkmark;
                debugger.StartDebugging(selectedLevel);
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Debugging");

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "New debug level: {0}", debugger.Level);
            #endregion
        }

        private void UpdateAlteredItems(ToolStripMenuItem item, string title)
        {
            bool originalSetting = (item.Tag == null) ? false : (bool)item.Tag;
            bool currentSetting = item.Text.EndsWith(Const.checkmark);
            if (originalSetting == currentSetting)
            {
                if (alteredItems.ContainsKey(item))
                    alteredItems.Remove(item);
            }
            else
            {
                if (!alteredItems.ContainsKey(item))
                    alteredItems[item] = title;
            }

            string tip = "To be saved to profile:" + Const.crnl + Const.crnl;
            foreach (var key in alteredItems.Keys)
            {
                string text = ((ToolStripMenuItem)key).Text;
                string mark;

                if (text.EndsWith(Const.checkmark))
                {
                    text = text.Remove(text.Length - Const.checkmark.Length);
                    mark = "+";
                }
                else
                    mark = "-";
                tip += string.Format("  {0,-20} {1} {2}", alteredItems[key] + ":", mark, text) + Const.crnl;
            }
            saveToProfileToolStripMenuItem.ToolTipText = tip;
        }

        private void saveToProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.WriteProfile();
            wisedome.WriteProfile();
            wisetele.WriteProfile();

            if ((string) toolStripTextBoxCloudSensorDataFile.Tag != toolStripTextBoxCloudSensorDataFile.Text)
            {
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                {
                    driverProfile.DeviceType = "ObservingConditions";
                    driverProfile.WriteValue("ASCOM.CloudSensor.ObservingConditions", "Data File", toolStripTextBoxCloudSensorDataFile.Text);
                }
            }

            if ((string) toolStripTextBoxVantagePro2ReportFile.Tag != toolStripTextBoxVantagePro2ReportFile.Text)
            {
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                {
                    driverProfile.DeviceType = "ObservingConditions";
                    driverProfile.WriteValue("ASCOM.Vantage.ObservingConditions", "Report File", toolStripTextBoxVantagePro2ReportFile.Text);
                }
            }
        }

        private void tracingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (debugger.Tracing)
            {
                item.Text = "Tracing";
                debugger.Tracing = false;
            } else
            {
                item.Text = "Tracing" + Const.checkmark;
                debugger.Tracing = true;
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Tracing");
        }

        private void toolStripMenuItemSafeToOpen_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.SafeToOperate.SetupDialogForm().Show();
        }

        private void safetyOverrideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string menuText = "Bypass Safety";
            if (_bypassSafety)
            {
                _bypassSafety = false;
                safetyOverrideToolStripMenuItem.Text = menuText;
            }
            else
            {
                safetyOverrideToolStripMenuItem.Text = menuText + Const.checkmark;
                _bypassSafety = true;
            }
        }

        public void StopEverything(Exception e = null)
        {
            wisetele.Stop();
            wisedome.Stop();
            wisedome.ShutterStop();
            #region debug
            string msg = "StopEverything: ";
            if (e != null)
                msg += string.Format("Exception: {0}, ", e.Message);
            msg += "Application will exit!";
            debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, msg);
            #endregion
            Application.Exit();
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            StopEverything();
        }

        public void HandleThreadException(object sender, ThreadExceptionEventArgs e)
        {
            StopEverything(e.Exception);
        }

        public void HandleDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StopEverything(e.ExceptionObject as Exception);
        }
        #endregion
    }
}
