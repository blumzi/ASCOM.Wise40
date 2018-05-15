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
using ASCOM.Astrometry;
using ASCOM.DriverAccess;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40SafeToOpen;
using ASCOM.Wise40.FilterWheel;
using ASCOM.Wise40.VantagePro;

namespace Dash
{
    public partial class FormDash : Form
    {
        public WiseTele wisetele = WiseTele.Instance;
        public WiseDome wisedome = WiseDome.Instance;
        public WiseFocuser wisefocuser = WiseFocuser.Instance;
        Hardware hardware = Hardware.Instance;
        public WiseSite wisesite = WiseSite.Instance;
        public WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.InstanceOpen;
        public WiseBoltwood wiseboltwood = WiseBoltwood.Instance;
        public WiseVantagePro wisevantagepro = WiseVantagePro.Instance;
        public WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;
        public WiseDomePlatform wisedomeplatform = WiseDomePlatform.Instance;
        WiseObject wiseobject = new WiseObject();

        DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;
        DebuggingForm debuggingForm = new DebuggingForm();
        Debugger debugger = Debugger.Instance;
        //FilterWheelForm filterWheelForm = new FilterWheelForm();

        Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, weatherStatus, filterWheelStatus;

        private double handpadRate = Const.rateSlew;
        private bool _bypassSafety = false;
        private bool _saveFocusUpperLimit = false, _saveFocusLowerLimit = false;

        DateTime _lastShutterStatusUpdate = DateTime.MinValue;

        private List<ToolStripMenuItem> debugMenuItems;
        private Dictionary<object, string> alteredItems = new Dictionary<object, string>();


        private long stoppingAxes;

        void onWheelOrPositionChanged(object sender, EventArgs e)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Dash.onWheelOrFilterChanged");
            #endregion
            LoadFilterWheelInformation();
        }

        #region Initialization
        public FormDash()
        {


            debugger.init();
            hardware.init();
            wisetele.init();
            wisetele.Connected = true;
            wisedome.init();
            wisedome.Connected = true;
            wisedome.wisedomeshutter.init();
            wisefocuser.init();
            wisefocuser.Connected = true;
            wisesafetooperate.init();
            wisesafetooperate.Connected = true;
            wiseboltwood.Connected = true;
            //wisefilterwheel.init();
            //wisefilterwheel.Connected = true;
            wisedomeplatform.init();

            InitializeComponent();

            if (wisesite.OperationalMode != WiseSite.OpMode.WISE)
            {
                List<Control> readonlyControls = new List<Control>()
                {
                    textBoxRA, textBoxDec,
                    buttonGoCoord,
                    buttonNorth, buttonSouth, buttonEast, buttonWest,
                    buttonNW, buttonNE, buttonSE, buttonSW,
                    buttonStop, buttonMainStop,
                    buttonTrack,
                    buttonZenith, buttonFlat, buttonHandleCover, buttonTelescopePark,

                    buttonDomeLeft, buttonDomeRight, buttonDomeStop, buttonDomePark,
                    buttonDomeAzGo, buttonDomeAzSet, textBoxDomeAzValue,
                    buttonCalibrateDome, buttonVent,
                    buttonFullOpenShutter, buttonFullCloseShutter, buttonOpenShutter, buttonCloseShutter, buttonStopShutter,

                    buttonFocusAllDown, buttonFocusAllUp, buttonFocusDecrease, buttonFocusIncrease,
                    buttonFocuserStop, buttonFocusGoto, textBoxFocusGotoPosition,
                    buttonFocusUp, buttonFocusDown, comboBoxFocusStep,

                    pictureBoxStop,

                    radioButtonGuide, radioButtonSet, radioButtonSlew, radioButtonSelectFilterWheel4, radioButtonSelectFilterWheel8,
                    textBoxFilterWheelPosition, buttonSetFilterWheelPosition,
                    buttonFilterWheelGo, comboBoxFilterWheelPositions,
                };

                foreach (var c in readonlyControls)
                {
                    c.Enabled = false;
                }

                annunciatorReadonly.Text = string.Format("Readonly mode ({0})", wisesite.OperationalMode.ToString());
                annunciatorReadonly.ForeColor = Statuser.colors[Statuser.Severity.Warning];
                annunciatorReadonly.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            }
            else
                annunciatorReadonly.Text = "";

            debugMenuItems = new List<ToolStripMenuItem> {
                debugASCOMToolStripMenuItem ,
                debugAxesToolStripMenuItem,
                debugDeviceToolStripMenuItem,
                debugMotorsToolStripMenuItem,
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
            filterWheelStatus = new Statuser(labelFilterWheelStatus);

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new ASCOM.Wise40.Common.Wise40ToolstripRenderer();

            telescopeStatus.Show("");
            focuserStatus.Show("");
            weatherStatus.Show("");

            buttonVent.Text = wisedome.Vent ? "Close Vent" : "Open Vent";

            List<ToolStripMenuItem> checkedItems = new List<ToolStripMenuItem>();
            if (debugger.Debugging(Debugger.DebugLevel.DebugASCOM))
                checkedItems.Add(debugASCOMToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugDevice))
                checkedItems.Add(debugDeviceToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugAxes))
                checkedItems.Add(debugAxesToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugLogic))
                checkedItems.Add(debugLogicToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugEncoders))
                checkedItems.Add(debugEncodersToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugMotors))
                checkedItems.Add(debugMotorsToolStripMenuItem);
            if (debugger.Debugging(Debugger.DebugLevel.DebugExceptions))
                checkedItems.Add(debugExceptionsToolStripMenuItem);

            if (debugger.Tracing)
                checkedItems.Add(tracingToolStripMenuItem);

            foreach (var item in checkedItems)
            {
                item.Text += Const.checkmark;
                item.Tag = true;
                item.Invalidate();
            }

            buttonVent.Text = wisedome.Vent ? "Close Vent" : "Open Vent";
            buttonProjector.Text = wisedome.Projector ? "Turn projector Off" : "Turn projector On";

            toolStripTextBoxCloudSensorDataFile.Text = wiseboltwood.DataFile;
            toolStripTextBoxCloudSensorDataFile.Tag = wiseboltwood.DataFile;

            toolStripTextBoxVantagePro2ReportFile.Tag = wisevantagepro.DataFile;
            toolStripTextBoxVantagePro2ReportFile.Text = wisevantagepro.DataFile;

            //wisefilterwheel.wheelOrPositionChanged += onWheelOrPositionChanged;
        }
        #endregion

        #region Refresh
        public void RefreshDisplay(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime utcTime = now.ToUniversalTime();
            DateTime localTime = now.ToLocalTime();
            ASCOM.Utilities.Util u = new ASCOM.Utilities.Util();

            Angle ra = Angle.FromHours(wisetele.RightAscension);
            Angle dec = Angle.FromDegrees(wisetele.Declination);
            Angle ha = Angle.FromHours(wisetele.HourAngle, Angle.Type.HA);
            string safetyError = wisetele.SafeAtCoordinates(ra, dec);

            Color safeColor = Statuser.colors[Statuser.Severity.Normal];
            Color unsafeColor = Statuser.colors[Statuser.Severity.Error];

            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy\n hh:mm:ss tt");

            #region RefreshTelescope
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utcTime.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelSiderealValue.Text = wisesite.LocalSiderealTime.ToString();

            labelRightAscensionValue.Text = ra.ToNiceString();

            labelDeclinationValue.Text = dec.ToNiceString();
            labelDeclinationValue.ForeColor = safetyError.Contains("Declination") ? unsafeColor : safeColor;

            labelHourAngleValue.Text = ha.ToNiceString();
            labelHourAngleValue.ForeColor = safetyError.Contains("HourAngle") ? unsafeColor : safeColor;

            labelAltitudeValue.Text = Angle.FromDegrees(wisetele.Altitude).ToNiceString();
            labelAltitudeValue.ForeColor = safetyError.Contains("Altitude") ? unsafeColor : safeColor;

            labelAzimuthValue.Text = Angle.FromDegrees(wisetele.Azimuth).ToNiceString();

            buttonTelescopePark.Text = wisetele.AtPark ? "Unpark" : "Park";

            TimeSpan ts = wisetele.inactivityMonitor.RemainingTime;
            if (ts == TimeSpan.MaxValue)
            {
                labelCountdown.Text = "";
                toolTip.SetToolTip(labelCountdown, "");
            }
            else
            {
                labelCountdown.Text = string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
                toolTip.SetToolTip(labelCountdown, "Inactivity Countdown\n(time to Observatory Shutdown)");
            }

            annunciatorTrack.Cadence = wisetele.Tracking ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
            annunciatorSlew.Cadence = wisetele.Slewing ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
            annunciatorPulse.Cadence = wisetele.IsPulseGuiding ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;

            if (wisedome.Slewing)
            {
                if (wisedome.MotorsAreActive)
                    annunciatorDome.Cadence = ASCOM.Controls.CadencePattern.BlinkFast;
                else
                {
                    // STUCK: Slewing but not moving
                    annunciatorDome.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                }
            }
            else
                annunciatorDome.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;

            WiseVirtualMotor primaryMotor = null, secondaryMotor = null;
            double currentRate = Const.rateStopped;

            annunciatorPrimary.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            primaryMotor = null;
            if (wisetele.WestMotor.isOn)
                primaryMotor = wisetele.WestMotor;
            else if (wisetele.EastMotor.isOn)
                primaryMotor = wisetele.EastMotor;
            if (primaryMotor != null)
            {
                annunciatorPrimary.Cadence = ASCOM.Controls.CadencePattern.BlinkFast;
                currentRate = primaryMotor.currentRate;
            }

            annunciatorSecondary.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            secondaryMotor = null;
            if (wisetele.NorthMotor.isOn)
                secondaryMotor = wisetele.NorthMotor;
            else if (wisetele.SouthMotor.isOn)
                secondaryMotor = wisetele.SouthMotor;
            if (secondaryMotor != null)
            {
                annunciatorSecondary.Cadence = ASCOM.Controls.CadencePattern.BlinkFast;
                currentRate = secondaryMotor.currentRate;
            }

            annunciatorRateSlew.Cadence = annunciatorRateSet.Cadence = annunciatorRateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            if (currentRate == Const.rateSlew)
                annunciatorRateSlew.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (currentRate == Const.rateSet)
                annunciatorRateSet.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (currentRate == Const.rateGuide)
                annunciatorRateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;

            telescopeStatus.Show(wisetele.Status);

            #endregion

            #region RefreshSafety
            #region ComputerControl
            string tip;
            if (wisesite.computerControl == null)
            {
                annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
                tip = "Cannot read the computer control switch!";
            }
            else if (wisesite.computerControl.IsSafe)
            {
                annunciatorComputerControl.Text = "Computer has control";
                annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "The computer control switch is ON.";
            }
            else
            {
                annunciatorComputerControl.Text = "No computer control";
                annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
                tip = "The computer control switch is OFF!";
            }
            toolTip.SetToolTip(annunciatorComputerControl, tip);
            #endregion
            #region DomePlatform
            tip = null;
            if (_bypassSafety)
            {
                annunciatorDomePlatform.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "Safety is bypassed (from Settings)";
            }
            else
            {
                if (wisedomeplatform.IsSafe)
                {
                    annunciatorDomePlatform.Text = "Platform is lowered";
                    annunciatorDomePlatform.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                    tip = "Dome platform is at its lowest position.";
                }
                else
                {
                    annunciatorDomePlatform.Text = "Platform is RAISED";
                    annunciatorDomePlatform.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
                    tip = "Dome platform is NOT at its lowest position!";
                }
            }
            toolTip.SetToolTip(annunciatorDomePlatform, tip);
            #endregion
            #region SafeToOpen
            tip = null;
            if (_bypassSafety)
            {
                annunciatorSafeToOperate.Text = "Safe to operate";
                annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "Safety is bypassed (from Settings)";
            }
            else
            {
                if (wisesite.safeToOperate == null)
                {
                    annunciatorSafeToOperate.Text = "Safe to operate ???";
                    annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
                    tip = "Cannot connect to the SafeToOpen driver!";
                }
                else if (wisesite.safeToOperate.IsSafe)
                {
                    annunciatorSafeToOperate.Text = "Safe to operate";
                    annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                    tip = "Conditions are safe to operate.";
                }
                else
                {
                    annunciatorSafeToOperate.Text = "Not safe to operate";
                    annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
                    tip = string.Join("\n", wisesafetooperate.UnsafeReasons);
                }
            }
            toolTip.SetToolTip(annunciatorSafeToOperate, tip);
            #endregion
            #region Simulation
            tip = null;

            if (wiseobject.Simulated)
            {
                annunciatorSimulation.Text = "SIMULATED HARDWARE";
                annunciatorSimulation.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                tip = "Hardware access is simulated by software";
            }
            else
            {
                annunciatorSimulation.Text = "";
                annunciatorSimulation.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "Real Hardware Access (not simulated)";
            }
            toolTip.SetToolTip(annunciatorSimulation, tip);
            #endregion
            #endregion

            #region RefreshDome
            labelDomeAzimuthValue.Text = domeSlaveDriver.Azimuth;
            domeStatus.Show(domeSlaveDriver.Status);
            buttonDomePark.Text = wisedome.AtPark ? "Unpark" : "Park";
            buttonVent.Text = wisedome.Vent ? "Close Vent" : "Open Vent";

            if (_lastShutterStatusUpdate == DateTime.MinValue || now.Subtract(_lastShutterStatusUpdate).TotalSeconds >= 2)
            {
                string stat = domeSlaveDriver.ShutterStatus;
                Statuser.Severity severity = Statuser.Severity.Normal;

                if (stat.Contains("error"))
                    severity = Statuser.Severity.Error;
                shutterStatus.Show(domeSlaveDriver.ShutterStatus, 0, severity);
                _lastShutterStatusUpdate = now;
            }
            #endregion

            #region RefreshWeather
            if (wisesite.och == null || !wisesite.och.Connected)
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
                    ASCOM.DriverAccess.ObservingConditions oc = wisesite.och;

                    #region ObservingConditions from OCH
                    labelAgeValue.Text = ((int)Math.Round(oc.TimeSinceLastUpdate(""), 2)).ToString() + "sec";
                    labelDewPointValue.Text = oc.DewPoint.ToString() + "°C";
                    labelSkyTempValue.Text = oc.SkyTemperature.ToString() + "°C";
                    labelTempValue.Text = oc.Temperature.ToString() + "°C";
                    labelPressureValue.Text = oc.Pressure.ToString() + "mB";
                    labelWindDirValue.Text = oc.WindDirection.ToString() + "°";
                    labelHumidityValue.Text = oc.Humidity.ToString() + "%";
                    labelHumidityValue.ForeColor = Statuser.TriStateColor(wisesafetooperate.isSafeHumidity);

                    double d = oc.CloudCover;
                    if (d == 0.0)
                        labelCloudCoverValue.Text = "Clear";
                    else if (d == 50.0)
                        labelCloudCoverValue.Text = "Cloudy";
                    else if (d == 90.0)
                        labelCloudCoverValue.Text = "VeryCloudy";
                    else
                        labelCloudCoverValue.Text = "Unknown";
                    labelCloudCoverValue.ForeColor = Statuser.TriStateColor(wisesafetooperate.isSafeCloudCover);

                    double windSpeedMps = oc.WindSpeed;
                    labelWindSpeedValue.Text = string.Format("{0:G3} km/h", wisevantagepro.KMH(windSpeedMps));
                    labelWindSpeedValue.ForeColor = Statuser.TriStateColor(wisesafetooperate.isSafeWindSpeed);

                    labelRainRateValue.Text = (oc.RainRate > 0.0) ? "Wet" : "Dry";
                    labelRainRateValue.ForeColor = Statuser.TriStateColor(wisesafetooperate.isSafeRain);
                    #endregion

                    #region Light from Boltwood
                    string light = wiseboltwood.CommandString("daylight", true);
                    labelLightValue.Text = light.Substring(3);
                    labelLightValue.ForeColor = Statuser.TriStateColor(wisesafetooperate.isSafeLight);
                    #endregion

                    #region Forecast from VantagePro
                    if (wisesafetooperate._vantageProIsValid)
                        dashStatus.Show("Forecast: " + wisevantagepro.Forecast, 0, Statuser.Severity.Normal);
                    else
                        dashStatus.Show("No forecast: bad connection to VantagePro", 0, Statuser.Severity.Warning);
                    #endregion

                    #region SafeToOpen
                    if (wisesafetooperate.IsSafe)
                    {
                        weatherStatus.Show("Safe to operate", 0, Statuser.Severity.Good);
                        weatherStatus.SetToolTip("");
                    }
                    else
                    {
                        if (_bypassSafety)
                            weatherStatus.Show("Safe to operate (safety bypassed)", 0, Statuser.Severity.Good);
                        else
                            weatherStatus.Show("Not safe to operate", 0, Statuser.Severity.Error, true);
                        weatherStatus.SetToolTip(string.Join("\n", wisesafetooperate.UnsafeReasons));
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
            focuserStatus.Show(wisefocuser.Status);
            annunciatorFocus.Cadence = wisefocuser.Status.StartsWith("Moving") ? ASCOM.Controls.CadencePattern.BlinkFast : ASCOM.Controls.CadencePattern.SteadyOff;
            #endregion

            #region RefreshFilterWheel
            //string fwstat = wisefilterwheel.Status;
            //if (fwstat == "Idle")
            //{
            //    annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            //} else
            //{
            //    annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.BlinkSlow;
            //    filterWheelStatus.Show(fwstat);
            //}
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

        class Movement
        {
            public TelescopeAxes _axis;
            public double _rate;
            public Const.CardinalDirection _direction;

            public Movement(Const.CardinalDirection direction, TelescopeAxes axis, double rate)
            {
                _axis = axis;
                _rate = rate;
                _direction = direction;
            }
        };

        private bool SafeAtCurrentCoords()
        {
            return wisetele.SafeAtCoordinates(Angle.FromHours(wisetele.RightAscension), Angle.FromDegrees(wisetele.Declination)) == string.Empty;
        }

        #region TelescopeControl
        public void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;

            List<Movement> movements = new List<Movement>();

            if (button == buttonNorth)
                movements.Add(new Movement(Const.CardinalDirection.North, TelescopeAxes.axisSecondary, handpadRate));
            else if (button == buttonSouth)
                movements.Add(new Movement(Const.CardinalDirection.South, TelescopeAxes.axisSecondary, -handpadRate));
            else if (button == buttonEast)
                movements.Add(new Movement(Const.CardinalDirection.East, TelescopeAxes.axisPrimary, handpadRate));
            else if (button == buttonWest)
                movements.Add(new Movement(Const.CardinalDirection.West, TelescopeAxes.axisPrimary, -handpadRate));
            else if (button == buttonNW)
            {
                movements.Add(new Movement(Const.CardinalDirection.North, TelescopeAxes.axisSecondary, handpadRate));
                movements.Add(new Movement(Const.CardinalDirection.East, TelescopeAxes.axisPrimary, handpadRate));
            }
            else if (button == buttonNE)
            {
                movements.Add(new Movement(Const.CardinalDirection.North, TelescopeAxes.axisSecondary, handpadRate));
                movements.Add(new Movement(Const.CardinalDirection.West, TelescopeAxes.axisPrimary, -handpadRate));
            }
            else if (button == buttonSE)
            {
                movements.Add(new Movement(Const.CardinalDirection.South, TelescopeAxes.axisSecondary, -handpadRate));
                movements.Add(new Movement(Const.CardinalDirection.West, TelescopeAxes.axisPrimary, -handpadRate));
            }
            else if (button == buttonSW)
            {
                movements.Add(new Movement(Const.CardinalDirection.South, TelescopeAxes.axisSecondary, -handpadRate));
                movements.Add(new Movement(Const.CardinalDirection.East, TelescopeAxes.axisPrimary, handpadRate));
            }

            List<Const.CardinalDirection> whichWay = new List<Const.CardinalDirection>();
            List<string> Directions = new List<string>();
            foreach (var m in movements)
            {
                whichWay.Add(m._direction);
                Directions.Add(m._direction.ToString());
            }

            if (SafeAtCurrentCoords() || wisetele.SafeToMove(whichWay))
            {
                string message = string.Format("Moving {0} at {1}", String.Join("-", Directions.ToArray()), WiseTele.RateName(handpadRate).Remove(0, 4));
                telescopeStatus.Show(message, 0, Statuser.Severity.Good);
                foreach (var m in movements)
                {
                    wisetele.HandpadMoveAxis(m._axis, m._rate);
                }
            }
            else
            {
                string message = string.Format("Unsafe to move {0}", String.Join("-", Directions.ToArray()));
                telescopeStatus.Show(message, 2000, Statuser.Severity.Error);
            }
        }

        private void axisStopper_DoWork(object sender, DoWorkEventArgs e)
        {
            TelescopeAxes axis = (int)e.Argument == 0 ? TelescopeAxes.axisPrimary : TelescopeAxes.axisSecondary;

            wisetele.MoveAxis(axis, Const.rateStopped);
        }

        private void axisStopper_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            Interlocked.Decrement(ref stoppingAxes);
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            List<TelescopeAxes> activeAxes = new List<TelescopeAxes>();

            if (wisetele.NorthMotor.isOn || wisetele.SouthMotor.isOn)
                activeAxes.Add(TelescopeAxes.axisSecondary);
            if (wisetele.WestMotor.isOn || wisetele.EastMotor.isOn)
                activeAxes.Add(TelescopeAxes.axisPrimary);

            Interlocked.Exchange(ref stoppingAxes, activeAxes.Count());
            telescopeStatus.Show("Stopping ...");
            foreach (TelescopeAxes axis in activeAxes)
            {
                BackgroundWorker axisStopper = new BackgroundWorker();

                axisStopper.DoWork += new DoWorkEventHandler(axisStopper_DoWork);
                axisStopper.RunWorkerCompleted += new RunWorkerCompletedEventHandler(axisStopper_Completed);
                axisStopper.RunWorkerAsync(axis == TelescopeAxes.axisPrimary ? 0 : 1);
            }

            while (Interlocked.Read(ref stoppingAxes) != 0)
            {
                Application.DoEvents();
            }

            telescopeStatus.Show("Stopped", 1000, Statuser.Severity.Good);
            wisetele.inactivityMonitor.EndActivity(InactivityMonitor.Activity.Handpad);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Handpad: stopped");
            #endregion
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
                telescopeStatus.Show(ex.Message, 5000, Statuser.Severity.Error);
            }
        }

        private void buttonTelescopeStop_Click(object sender, EventArgs e)
        {
            wisetele.AbortSlew();
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
                    bool wasSlavingTheDome = wisetele._enslaveDome;
                    wisetele._enslaveDome = false;
                    wisetele.ParkFromGui(wasSlavingTheDome);
                    while (wisetele.Slewing)
                    {
                        Application.DoEvents();
                    }
                    wisetele.AtPark = true;
                    wisetele.Tracking = saveTracking;
                    wisetele._enslaveDome = wasSlavingTheDome;
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
                wisefocuser.Move(Convert.ToUInt32(textBoxFocusGotoPosition.Text));
            }
            catch (Exception ex)
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
                if (!item.Text.EndsWith(Const.checkmark))
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
            }
            else
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
                item.Text = item.Text.Remove(item.Text.Length - Const.checkmark.Length);
            }
            else
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
            double dec = wisesite.Latitude.Degrees;

            wisetele._enslaveDome = false;
            wisetele.Tracking = true;
            try
            {
                wisetele.SlewToCoordinatesAsync(ra, dec, false);
            }
            catch (Exception ex)
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
            else if (item == debugDeviceToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugDevice;
            else if (item == debugAxesToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugAxes;
            else if (item == debugEncodersToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugEncoders;
            else if (item == debugMotorsToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugMotors;
            else if (item == debugExceptionsToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugExceptions;
            else if (item == debugLogicToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugLogic;

            if (selectedLevel == Debugger.DebugLevel.DebugNone)
                return;

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

        private void LoadFilterWheelInformation()
        {
            WiseFilterWheel.Wheel wheel = WiseFilterWheel.Instance.currentWheel;
            short position = wheel._position;
#if RFID_IS_WORKING
            if (position == -1)
            {
                labelFilterWheelName.Text = "Unknown";
                labelFilterWheelPosition.Text = "";
                return;
            }

            labelFilterWheelName.Text = string.Format("{0} ({1} filters)", wheel.name, wheel.positions.Length);
            labelFilterWheelPosition.Text = (position + 1).ToString();
#endif
            comboBoxFilterWheelPositions.Items.Clear();
            for (int pos = 0; pos < wheel._positions.Length; pos++)
            {
                string filterName = wheel._positions[pos].filterName;
                string item;
                if (filterName == string.Empty)
                    item = string.Format("{0} - Clear", pos + 1);
                else
                {
                    string desc = WiseFilterWheel.filterInventory[wheel._filterSize].Find((x) => x.FilterName == filterName).FilterDescription;

                    item = string.Format("{0} - {1}: {2}", pos + 1, filterName, desc);
                }
                comboBoxFilterWheelPositions.Items.Add(item);
                if (pos == position)
                    comboBoxFilterWheelPositions.Text = item;
            }

            comboBoxFilterWheelPositions.Invalidate();
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

            string alterations = string.Empty;
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
                alterations += string.Format("  {0,-20} {1} {2}", alteredItems[key] + ":", mark, text) + Const.crnl;
            }

            if (alterations != string.Empty)
            {
                saveToProfileToolStripMenuItem.ToolTipText = "To be saved to profile:" + Const.crnl + Const.crnl + alterations;
                saveToProfileToolStripMenuItem.Text = "** Save To Profile **";
            }
            else
            {
                saveToProfileToolStripMenuItem.ToolTipText = "No changes";
                saveToProfileToolStripMenuItem.Text = "Save To Profile";
            }
        }

        private void saveToProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.WriteProfile();
            wisedome.WriteProfile();
            wisetele.WriteProfile();
            if (_saveFocusUpperLimit || _saveFocusLowerLimit)
                wisefocuser.WriteProfile();

            if ((string)toolStripTextBoxCloudSensorDataFile.Tag != toolStripTextBoxCloudSensorDataFile.Text)
            {
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                {
                    driverProfile.DeviceType = "ObservingConditions";
                    driverProfile.WriteValue("ASCOM.Wise40.Boltwood.ObservingConditions", "Data File", toolStripTextBoxCloudSensorDataFile.Text);
                }
            }

            if ((string)toolStripTextBoxVantagePro2ReportFile.Tag != toolStripTextBoxVantagePro2ReportFile.Text)
            {
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
                {
                    driverProfile.DeviceType = "ObservingConditions";
                    driverProfile.WriteValue("ASCOM.Wise40.VantagePro.ObservingConditions", "DataFile", toolStripTextBoxVantagePro2ReportFile.Text);
                }
            }
            saveToProfileToolStripMenuItem.Text = "Save To Profile";
        }

        private void tracingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (debugger.Tracing)
            {
                item.Text = "Tracing";
                debugger.Tracing = false;
            }
            else
            {
                item.Text = "Tracing" + Const.checkmark;
                debugger.Tracing = true;
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Tracing");
        }

        private void toolStripMenuItemSafeToOpen_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40SafeToOpen.SafeToOperateSetupDialogForm().Show();
        }

        private void toolStripMenuItemFilterWheel_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.FilterWheel.FilterWheelSetupDialogForm().Show();
        }

        private void filterWheelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //wisefilterwheel.init();
            //new FilterWheelForm().Show();
        }

        private void syncVentWithShutterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string text = item.Text;

            if (item.Text.EndsWith(Const.checkmark))
            {
                wisedome._syncVentWithShutter = false;
                item.Text = text.Remove(text.Length - Const.checkmark.Length);
            }
            else
            {
                wisedome._syncVentWithShutter = true;
                item.Text = text + Const.checkmark;
            }
            item.Invalidate();
            UpdateAlteredItems(item, "Dome");
        }

        private void buttonFocusIncrease_Click(object sender, EventArgs e)
        {
            uint newPos = wisefocuser.Position + Convert.ToUInt32(comboBoxFocusStep.Text);
            if (newPos > wisefocuser.UpperLimit)
                newPos = wisefocuser.UpperLimit;

            if (newPos != wisefocuser.Position)
                wisefocuser.Move(newPos);
        }

        private void buttonFocusDecrease_Click(object sender, EventArgs e)
        {
            uint newPos = wisefocuser.Position - Convert.ToUInt32(comboBoxFocusStep.Text);
            if (newPos < wisefocuser.LowerLimit)
                newPos = wisefocuser.LowerLimit;

            if (newPos != wisefocuser.Position)
                wisefocuser.Move(newPos);
        }

        private void buttonFilterWheelGo_Click(object sender, EventArgs e)
        {
            short targetPosition = (short)comboBoxFilterWheelPositions.SelectedIndex;
            short humanTargetPosition = (short)(targetPosition + 1);

            if (wisefilterwheel.Simulated)
            {
                filterWheelStatus.Show(string.Format("Moved to position {0}", humanTargetPosition), 1000, Statuser.Severity.Good);
                textBoxFilterWheelPosition.Text = humanTargetPosition.ToString();
            }
            else
            {
                filterWheelStatus.Show(string.Format("Moving to position {0}", humanTargetPosition), 5000, Statuser.Severity.Good);
            }
            wisefilterwheel.Position = targetPosition;
        }

        private void buttonSetFilterWheelPosition_Click(object sender, EventArgs e)
        {
            short selectedPosition = -1;
            try
            {
                selectedPosition = Convert.ToInt16(textBoxFilterWheelPosition.Text);
            }
            catch (FormatException)
            {
                filterWheelStatus.Show("Invalid position \"Current position\"", 1000, Statuser.Severity.Error);
                textBoxFilterWheelPosition.Text = "";
                return;
            }
            WiseFilterWheel.Wheel selectedWheel = radioButtonSelectFilterWheel8.Checked ? WiseFilterWheel.wheel8 : WiseFilterWheel.wheel4;
            int maxPositions = selectedWheel._nPositions;

            if (!(selectedPosition > 0 && selectedPosition < maxPositions))
            {
                textBoxFilterWheelPosition.Text = "";
                filterWheelStatus.Show(
                    string.Format("Position must be between 1 and {0}", maxPositions), 2000, Statuser.Severity.Error);
                return;
            }

            wisefilterwheel.SetCurrent(
                radioButtonSelectFilterWheel8.Checked ? WiseFilterWheel.wheel8 : WiseFilterWheel.wheel4,
                (short)(selectedPosition - 1));

            LoadFilterWheelInformation();
            filterWheelStatus.Show(string.Format("Manually set to {0}, position {1}",
                wisefilterwheel.currentWheel._name, selectedPosition),
                1000, Statuser.Severity.Good);
        }

        private void manage2InchFilterInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wisefilterwheel.init();
            new FiltersForm(2).Show();
        }

        private void toolStripMenuItemPulseGuide_Click(object sender, EventArgs e)
        {
            new PulseGuideForm().Show();
        }

        private void buttonFullStop_Click(object sender, EventArgs e)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "FullStop !!!");
            #endregion
            wisetele.FullStop();
            wisedome.Stop();
            wisedome.wisedomeshutter.Stop();
            wisefocuser.Stop();
        }

        private void buttonProjector_Click(object sender, EventArgs e)
        {
            wisedome.Projector = !wisedome.Projector;
            buttonProjector.Text = wisedome.Projector ? "Turn projector Off" : "Turn projector On";
        }

        private void telescopeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wisetele.init();
            new TelescopeSetupDialogForm(wisetele.debugger.Tracing, wisetele.debugger.Level, wisesite.astrometricAccuracy, wisetele._enslaveDome).Show();
        }

        private void domeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wisedome.init();
            new DomeSetupDialogForm().Show();
        }

        private void manage3InchFilterInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wisefilterwheel.init();
            new FiltersForm(3).Show();
        }

        private void buttonTrack_Click(object sender, EventArgs e)
        {
            wisetele.Tracking = !wisetele.Tracking;
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
            wisetele.BypassSafety = _bypassSafety;
        }

        public void StopEverything(Exception e = null)
        {
            try
            {
                wisetele.Stop();
                wisetele.Tracking = false;
                wisedome.Stop();
                wisedome.wisedomeshutter.Stop();
                wisefocuser.Stop();
            }
            catch { }
            #region debug
            string msg = "\nStopEverything:\n";
            if (e != null)
            {
                msg += string.Format(" Exception -- : {0}\n", e.Message);
                msg += string.Format("    Source -- : {0}\n", e.Source);
                msg += string.Format("StackTrace -- : {0}\n", e.StackTrace);
                msg += string.Format("TargetSite -- : {0}\n", e.TargetSite);
            }
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
