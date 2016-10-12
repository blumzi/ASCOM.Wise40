﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace Dash
{
    public partial class FormDash : Form
    {
        WiseTele wisetele = WiseTele.Instance;
        WiseDome wisedome = WiseDome.Instance;
        WiseFocuser wisefocuser = WiseFocuser.Instance;
        Hardware hardware = Hardware.Instance;
        WiseSite wisesite = WiseSite.Instance;
        DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;
        DebuggingForm debuggingForm = new DebuggingForm();
        Debugger debugger = Debugger.Instance;

        Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, weatherStatus;

        private double handpadRate = Const.rateSlew;

        private List<ToolStripMenuItem> debugMenuItems;

        #region Initialization
        public FormDash()
        {
            hardware.init();
            wisetele.init();
            wisetele.Connected = true;
            wisedome.init();
            wisedome.Connected = true;
            wisefocuser.init();
            wisefocuser.Connected = true;

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
            weatherStatus = new Statuser(labelWeatherStatus);

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new Wise40ToolstripRenderer();

            telescopeStatus.Show("");
            focuserStatus.Show("");
            weatherStatus.Show("");

            checkBoxTrack.Checked = wisetele.Tracking;
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

            foreach (var item in checkedItems)
                item.Text += " ✓";

            tracingToolStripMenuItem.Text = debugger.Tracing ? "Tracing ✓" : "Tracing";
        }
        #endregion

        #region Refresh
        public void RefreshDisplay(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime utcTime = now.ToUniversalTime();
            DateTime localTime = now.ToLocalTime();
            ASCOM.Utilities.Util u = new ASCOM.Utilities.Util();

            labelDate.Text = localTime.ToLongDateString() + "\r\n" + localTime.ToLongTimeString();

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
            string activeSlewers = wisetele.slewers.ToString();
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
            #endregion

            #region RefreshSafety
            string why;
            if (wisesite.computerControl == null)
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                why = "Cannot read the computer control switch!";
            }
            else if (wisesite.computerControl.IsSafe)
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Good];
                why = "Computer control is enabled.";
            }
            else
            {
                labelDashComputerControl.ForeColor = Statuser.colors[Statuser.Severity.Error];
                why = why = "Computer control switch is OFF!";
            }
            toolTip.SetToolTip(labelDashComputerControl, why);

            if (wisesite.safeToOpen == null)
            {
                labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                why = "Cannot connect to the safeToOpen driver!";
            }
            else if (wisesite.safeToOpen.IsSafe)
            {
                labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Good];
                why = "Conditions are safe to open the dome.";
            }
            else
            {
                labelDashSafeToOpen.ForeColor = Statuser.colors[Statuser.Severity.Error];
                why = wisesite.safeToOpen.CommandString("unsafeReasons", false);
            }
            toolTip.SetToolTip(labelDashSafeToOpen, why);

            if (wisesite.safeToImage == null)
            {
                labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                why = "Cannot connect to the safeToImage driver!";
            }
            else if (wisesite.safeToImage.IsSafe)
            {
                labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Good];
                why = "Conditions are safe to image.";
            }
            else
            {
                labelDashSafeToImage.ForeColor = Statuser.colors[Statuser.Severity.Error];
                why = wisesite.safeToImage.CommandString("unsafeReasons", false);
            }
            toolTip.SetToolTip(labelDashSafeToImage, why);
            #endregion

            #region RefreshDome
            labelDomeAzimuthValue.Text = domeSlaveDriver.Azimuth;
            if (labelDomeStatus.Text == string.Empty)
                domeStatus.Show(domeSlaveDriver.Status);
            if (labelDomeShutterStatus.Text == string.Empty)
                shutterStatus.Show(domeSlaveDriver.ShutterStatus);
            buttonDomePark.Text = wisedome.AtPark ? "Unpark" : "Park";
            #endregion

            #region RefreshWeather
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
            ASCOM.Wise40.DaqsForm daqsForm = new ASCOM.Wise40.DaqsForm();
            daqsForm.Visible = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm form = new AboutForm();
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
                telescopeStatus.Show(string.Format("Slewing to {0}/{1}", new Angle(textBoxRA.Text), new Angle(textBoxDec.Text)),
                    0, Statuser.Severity.Good);
                wisetele.SlewToCoordinatesAsync(new Angle(textBoxRA.Text).Hours, new Angle(textBoxDec.Text).Degrees);
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
            textBoxRA.Text = Angle.FromHours(wisetele.RightAscension).ToString();
        }

        private void textBoxDec_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            textBoxDec.Text = Angle.FromDegrees(wisetele.Declination).ToString();
        }

        private void checkBoxEnslaveDome_CheckedChanged(object sender, EventArgs e)
        {
            wisetele._enslaveDome = checkBoxEnslaveDome.Checked;
            telescopeStatus.Show((wisetele._enslaveDome ? "Started" : "Stopped") + " dome enslaving", 1000, Statuser.Severity.Good);
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
                    telescopeStatus.Show("Parking", 0, Statuser.Severity.Good);
                    wisetele.Park();
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
                    domeSlaveDriver.OpenShutter();
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

        private void buttonDomeAzGo_Click(object sender, EventArgs e)
        {
            if (textBoxDomeAzGo.Text == string.Empty)
                return;

            double az = Convert.ToDouble(textBoxDomeAzGo.Text);
            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show(string.Format("Invalid azimuth: {0}", textBoxDomeAzGo.Text), 2000, Statuser.Severity.Error);
                textBoxDomeAzGo.Text = "";
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
        }

        private void buttonCalibrateDome_Click(object sender, EventArgs e)
        {
            domeStatus.Show("Started dome calibration", 1000, Statuser.Severity.Good);
            wisedome.FindHome();
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
                    wisedome.Park();
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
                if (! item.Text.EndsWith(" ✓"))
                    item.Text += " ✓";
            }
        }

        private void debugNoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.StopDebugging(Debugger.DebugLevel.DebugAll);

            foreach (var item in debugMenuItems)
            {
                if (item.Text.EndsWith(" ✓"))
                    item.Text = item.Text.Remove(item.Text.IndexOf(' '));
            }
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
                item.Text += " ✓";
                debugger.StartDebugging(selectedLevel);
            }
            item.Invalidate();
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "New debug level: {0}", debugger.Level);
            #endregion
        }

        private void saveToProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.WriteProfile();
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
                item.Text = "Tracing ✓";
                debugger.Tracing = true;
            }
            item.Invalidate();
        }
        #endregion
    }
}
