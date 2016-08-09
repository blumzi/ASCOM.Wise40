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

using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;

namespace Dashboard
{
    public partial class DashboardForm : Form
    {
        private Form daqsForm;
        private WiseTele wisetele = WiseTele.Instance;
        private WiseDome wisedome = WiseDome.Instance;
        private WiseSite wisesite = WiseSite.Instance;
        //private WiseTele wisetele = WiseTele.Instance;
        private Debugger debugger = Debugger.Instance;

        private double handpadRate = Const.rateSlew;

        public DashboardForm()
        {
            InitializeComponent();
            wisetele.init();
            wisesite.init();
            wisedome.init();

            wisetele.Connected = true;
            wisedome.Connected = true;
        }

        private void toolStripButtonTelescope_Click(object sender, EventArgs e)
        {
            if (panelTelescope.Visible)
            {
                toolStripButtonTelescope.Text = "Show Telescope";
                panelTelescope.Visible = false;
            } else
            {
                toolStripButtonTelescope.Text = "Hide Telescope";
                panelTelescope.Visible = true;
            }
        }

        private void toolStripButtonDome_Click(object sender, EventArgs e)
        {
            if (panelDome.Visible)
            {
                toolStripButtonDome.Text = "Show Dome";
                panelDome.Visible = false;
            }
            else
            {
                toolStripButtonDome.Text = "Hide Dome";
                panelDome.Visible = true;
            }
        }

        private void toolStripButtonFocuser_Click(object sender, EventArgs e)
        {
            if (panelFocuser.Visible)
            {
                toolStripButtonFocuser.Text = "Show Focuser";
                panelFocuser.Visible = false;
            }
            else
            {
                toolStripButtonFocuser.Text = "Hide Focuser";
                panelFocuser.Visible = true;
            }
        }

        private void toolStripButtonWeather_Click(object sender, EventArgs e)
        {
            if (groupBoxWeather.Visible)
            {
                toolStripButtonWeather.Text = "Show Weather";
                groupBoxWeather.Visible = false;
            }
            else
            {
                toolStripButtonWeather.Text = "Hide Weather";
                groupBoxWeather.Visible = true;
            }
        }

        private void toolStripButtonMotionStudy_Click(object sender, EventArgs e)
        {
            if (groupBoxMovementStudy.Visible)
            {
                toolStripButtonMotionStudy.Text = "Show MotionStudy";
                groupBoxMovementStudy.Visible = false;
            }
            else
            {
                toolStripButtonMotionStudy.Text = "Hide MotionStudy";
                groupBoxMovementStudy.Visible = true;
            }
        }

        private void toolStripButtonDIO_Click(object sender, EventArgs e)
        {
            if (daqsForm == null)
            {
                daqsForm = new ASCOM.Wise40.DaqsForm();
                daqsForm.Visible = true;
                toolStripButtonDIO.Text = "Hide Digital IO";
            }
            else
            {
                if (daqsForm.Visible)
                {
                    toolStripButtonDIO.Text = "Show Digital IO";
                    daqsForm.Visible = false;
                }
                else
                {
                    toolStripButtonDIO.Text = "Hide Digital IO";
                    daqsForm.Visible = true;
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButtonConnectTelescope_Click(object sender, EventArgs e)
        {
            if (wisetele.Connected)
            {
                wisetele.Connect(false);
                (sender as ToolStripButton).Text = "Connect Telescope";
            } else
            {
                wisetele.Connect(true);
                (sender as ToolStripButton).Text = "Disconnect Telescope";
            }
        }

        private void toolStripButtonConnectDome_Click(object sender, EventArgs e)
        {
            if (wisedome.Connected)
            {
                wisedome.Connect(false);
                (sender as ToolStripButton).Text = "Connect Dome";
            }
            else
            {
                wisedome.Connect(true);
                (sender as ToolStripButton).Text = "Disconnect Dome";
            }
        }

        private void RefreshTelescope()
        {
            if (!panelTelescope.Visible)
                return;

            DateTime now = DateTime.Now;
            DateTime utc = now.ToUniversalTime();
            ASCOM.Utilities.Util u = new ASCOM.Utilities.Util();
            
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utc.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");

            if (wisetele.Connected)
            {
                groupBoxTelescope.Text = string.Format(" {0} v{1} ", wisetele.Name, wisetele.DriverVersion);
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

            }
            else
            {
                groupBoxTelescope.Text = " Telescope (not connected) ";
                labelSiderealValue.Text = string.Empty;
                labelRightAscensionValue.Text = string.Empty;
                labelDeclinationValue.Text = string.Empty;
                labelHourAngleValue.Text = string.Empty;

                labelAltitudeValue.Text = string.Empty;
                labelAzimuthValue.Text = string.Empty;

                labelHAEncValue.Text = string.Empty;
                labelDecEncValue.Text = string.Empty;

                axisValue.Text = string.Empty;
                wormValue.Text = string.Empty;

                checkBoxPrimaryIsActive.Checked = false;
                checkBoxSecondaryIsActive.Checked = false;
                checkBoxSlewingIsActive.Checked = false;
                checkBoxTrackingIsActive.Checked = false;
                checkBoxTrack.Checked = false;
            }
        }

        private void RefreshWeather()
        {
            if (groupBoxWeather.Visible)
            {
                try
                {
                    ObservingConditions oc = wisesite.observingConditions;

                    groupBoxWeather.Text = string.Format(" {0} v{1} ", oc.Name, oc.DriverVersion);
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
                catch (ASCOM.PropertyNotImplementedException e)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OC: exception: {0}", e.Message);
                }
            }

        }

        private void RefreshDome()
        {
            if (!panelDome.Visible)
                return;

            if (labelDomeSlavedConfValue.Text == string.Empty)
            {
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                {
                    driverProfile.DeviceType = "Telescope";
                    bool confDomeSlaved = Convert.ToBoolean(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Enslave Dome", string.Empty, "false"));

                    labelDomeSlavedConfValue.Text = confDomeSlaved ? "Enslaved" : "Not enslaved" + " while tracking";
                }
            }

            if (wisedome.Connected)
            {
                string shutterStatus;
                groupBoxDome.Text = string.Format(" {0} v{1} ", wisedome.Name, wisedome.DriverVersion);

                labelDomeAzimuthValue.Text = wisedome.Calibrated ? wisedome.Azimuth.ToNiceString() : "not calibrated";
                labelDomeStatusValue.Text = wisedome.Status;

                switch (wisedome.ShutterStatus)
                {
                    case ShutterState.shutterClosed:
                        shutterStatus = "Closed";
                        break;
                    case ShutterState.shutterClosing:
                        shutterStatus = "Closing";
                        break;
                    case ShutterState.shutterOpen:
                        shutterStatus = "Open";
                        break;
                    case ShutterState.shutterOpening:
                        shutterStatus = "Opening";
                        break;
                    default:
                        shutterStatus = "Unknown";
                        break;
                }
                labelDomeShutterStatusValue.Text = shutterStatus;
            }
            else
            {
                groupBoxDome.Text = " Dome (not connected) ";

                labelDomeAzimuthValue.Text = string.Empty;
                labelDomeStatusValue.Text = string.Empty;
                labelDomeShutterStatusValue.Text = string.Empty;
            }
        }

        private void RefreshDisplay()
        {
            DateTime now = DateTime.Now.ToLocalTime();

            labelDate.Text = string.Format("{0}:{1}:{2} {3}", now.Hour, now.Minute, now.Second , now.ToLongDateString());

            RefreshTelescope();
            RefreshDome();
            RefreshWeather();




            //if (scopeBackgroundMover != null && scopeBackgroundMover.IsBusy)
            //    TextBoxLog.Text = "Working ...";

            //if (resultsAvailable)
            //{
            //    TextBoxLog.Clear();
            //    if (results.Count == 0)
            //        TextBoxLog.Text = "Cancelled by user!";
            //    else
            //    {
            //        TelescopeAxes axis = results[0].axis;

            //        for (int i = 0; i < results.Count; i++)
            //        {
            //            TextBoxLog.Text += string.Format("[{0}]: ({2})\r\n{1}",
            //                i, results[i].ToString(), results[i].cancelled ? "cancelled" : "completed");
            //        }
            //    }
            //    resultsAvailable = false;
            //}



            //if (groupBoxWeather.Visible)
            //{
            //    try
            //    {
            //        ObservingConditions oc = wisesite.observingConditions;

            //        labelAgeValue.Text = ((int)Math.Round(oc.TimeSinceLastUpdate(""), 2)).ToString() + "sec";

            //        double d = oc.CloudCover;
            //        if (d == 0.0)
            //            labelCloudCoverValue.Text = "Clear";
            //        else if (d == 50.0)
            //            labelCloudCoverValue.Text = "Cloudy";
            //        else if (d == 90.0)
            //            labelCloudCoverValue.Text = "VeryCloudy";
            //        else
            //            labelCloudCoverValue.Text = "Unknown";

            //        labelDewPointValue.Text = oc.DewPoint.ToString() + "°C";
            //        labelSkyTempValue.Text = oc.SkyTemperature.ToString() + "°C";
            //        labelTempValue.Text = oc.Temperature.ToString() + "°C";
            //        labelHumidityValue.Text = oc.Humidity.ToString() + "%";
            //        labelPressureValue.Text = oc.Pressure.ToString() + "mB";
            //        labelRainRateValue.Text = (oc.RainRate > 0.0) ? "Wet" : "Dry";
            //        labelWindSpeedValue.Text = oc.WindSpeed.ToString() + "m/s";
            //        labelWindDirValue.Text = oc.WindDirection.ToString() + "°";
            //    }
            //    catch (PropertyNotImplementedException e)
            //    {
            //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OC: exception: {0}", e.Message);
            //    }
            //}

            //if (groupBoxCurrentRates.Visible)
            //{
            //    WiseVirtualMotor m;

            //    m = null;
            //    if (wisetele.WestMotor.isOn)
            //        m = wisetele.WestMotor;
            //    else if (wisetele.EastMotor.isOn)
            //        m = wisetele.EastMotor;

            //    if (m == null)
            //        labelCurrPrimRateValue.Text = "Stopped";
            //    else
            //        labelCurrPrimRateValue.Text = m.name.Remove(m.name.IndexOf('M')) + "@" +
            //            WiseTele.RateName(m.currentRate).Replace("rate", "");

            //    m = null;
            //    if (wisetele.NorthMotor.isOn)
            //        m = wisetele.NorthMotor;
            //    else if (wisetele.SouthMotor.isOn)
            //        m = wisetele.SouthMotor;

            //    if (m == null)
            //        labelCurrSecRateValue.Text = "Stopped";
            //    else
            //        labelCurrSecRateValue.Text = m.name.Remove(m.name.IndexOf('M')) + "@" +
            //            WiseTele.RateName(m.currentRate).Replace("rate", "");
            //}
        }

        private void timerRefreshDisplay_Tick(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

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
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            wisetele.Stop();
        }

        private void directionButton_MouseUp(object sender, EventArgs e)
        {
            wisetele.Stop();
        }
    }
}
