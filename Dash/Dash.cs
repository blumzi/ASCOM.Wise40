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
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40SafeToOperate;
using ASCOM.Wise40.FilterWheel;

using ASCOM.Utilities;
using Newtonsoft.Json;

namespace Dash
{
    public partial class FormDash : Form
    {
        public WiseSite wisesite = WiseSite.Instance;
        public WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;
        private ASCOM.Utilities.Util ascomutil = new Util();
        enum GoToMode { Ra, Ha, DeltaRa, DeltaHa };
        private GoToMode goToMode = GoToMode.Ra;
        private static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;
        DebuggingForm debuggingForm = new DebuggingForm();
        Debugger debugger = Debugger.Instance;
        //FilterWheelForm filterWheelForm = new FilterWheelForm();

        Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, safetooperateStatus, filterWheelStatus;

        private double handpadRate = Const.rateSlew;
        private bool _bypassSafety = false;
        private bool _saveFocusUpperLimit = false, _saveFocusLowerLimit = false;

        RefreshPacer domePacer = new RefreshPacer(TimeSpan.FromSeconds(1));
        RefreshPacer safettoperatePacer = new RefreshPacer(TimeSpan.FromSeconds(20));
        RefreshPacer weatherPacer = new RefreshPacer(TimeSpan.FromSeconds(20));
        RefreshPacer focusPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        RefreshPacer filterWheelPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        RefreshPacer telescopePacer = new RefreshPacer(TimeSpan.FromMilliseconds(200));
        RefreshPacer forecastPacer = new RefreshPacer(TimeSpan.FromMinutes(2));

        SafeToOperateDigest safetooperateDigest = null;
        DomeDigest domeDigest = null;
        TelescopeDigest telescopeDigest = null;
        FocuserDigest focuserDigest = null;
        FilterWheelDigest filterWheelDigest = null;
        string forecast;

        private List<ToolStripMenuItem> debugMenuItems;
        private Dictionary<object, string> alteredItems = new Dictionary<object, string>();

        public Color safeColor = Statuser.colors[Statuser.Severity.Normal];
        public Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        public Color warningColor = Statuser.colors[Statuser.Severity.Warning];
        public Color goodColor = Statuser.colors[Statuser.Severity.Good];

        private Moon moon = Moon.Instance;
        public ASCOM.DriverAccess.Telescope wiseTelescope;
        public ASCOM.DriverAccess.Dome wiseDome;
        public ASCOM.DriverAccess.Focuser wiseFocuser;
        //ASCOM.DriverAccess.FilterWheel remoteFilterWheel;
        public ASCOM.DriverAccess.SafetyMonitor wiseSafeToOperate;
        public ASCOM.DriverAccess.ObservingConditions wiseVantagePro, wiseBoltwood;

        private int focuserMaxStep = 0;
        private int focuserLowerLimit = 0;
        private int focuserUpperLimit = 0;

        void onWheelOrPositionChanged(object sender, EventArgs e)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Dash.onWheelOrFilterChanged");
            #endregion
            LoadFilterWheelInformation();
        }

        static List<Control> readonlyControls;

        private static bool Readonly
        {
            get
            {
                return WiseSite.Instance.OperationalMode != WiseSite.OpMode.WISE;
            }
        }

        #region Initialization
        public FormDash()
        {
            //wisefilterwheel.init();
            //wisefilterwheel.Connected = true;

            InitializeComponent();

            wiseTelescope = new ASCOM.DriverAccess.Telescope("ASCOM.Remote1.Telescope");
            wiseDome = new ASCOM.DriverAccess.Dome("ASCOM.Remote1.Dome");
            wiseFocuser = new ASCOM.DriverAccess.Focuser("ASCOM.Remote1.Focuser");
            //remoteFilterWheel = new ASCOM.DriverAccess.FilterWheel("ASCOM.Remote1.FilterWheel");
            wiseSafeToOperate = new ASCOM.DriverAccess.SafetyMonitor("ASCOM.Remote1.SafetyMonitor");
            wiseVantagePro = new ASCOM.DriverAccess.ObservingConditions("ASCOM.Wise40.VantagePro.ObservingConditions");
            wiseBoltwood = new ASCOM.DriverAccess.ObservingConditions("ASCOM.Wise40.Boltwood.ObservingConditions");

            focuserMaxStep = wiseFocuser.MaxStep;
            focuserLowerLimit = Convert.ToInt32(wiseFocuser.Action("limit", "lower"));
            focuserUpperLimit = Convert.ToInt32(wiseFocuser.Action("limit", "upper"));

            readonlyControls = new List<Control>() {
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

            if (Readonly)
            {
                groupBoxTarget.Text += string.Format("(from {0}) ", wisesite.OperationalMode.ToString());

                foreach (var c in readonlyControls)
                {
                    c.Enabled = false;
                }

                annunciatorReadonly.Text = string.Format("Readonly mode ({0})", wisesite.OperationalMode.ToString());
                annunciatorReadonly.ForeColor = warningColor;
                annunciatorReadonly.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;

                pictureBoxStop.Visible = false;
            }
            else
            {
                annunciatorReadonly.ForeColor = safeColor;
                annunciatorReadonly.Text = "Controls are active";
            }

            debugMenuItems = new List<ToolStripMenuItem> {
                debugASCOMToolStripMenuItem ,
                debugAxesToolStripMenuItem,
                debugDeviceToolStripMenuItem,
                debugMotorsToolStripMenuItem,
                debugEncodersToolStripMenuItem,
                debugExceptionsToolStripMenuItem,
                debugLogicToolStripMenuItem,
                debugSafetyToolStripMenuItem,
                debugDomeToolStripMenuItem,
                debugShutterToolStripMenuItem,
                debugDAQsToolStripMenuItem,
            };

            dashStatus = new Statuser(labelDashStatus);
            telescopeStatus = new Statuser(labelTelescopeStatus);
            domeStatus = new Statuser(labelDomeStatus);
            shutterStatus = new Statuser(labelDomeShutterStatus);
            focuserStatus = new Statuser(labelFocuserStatus);
            safetooperateStatus = new Statuser(labelWeatherStatus, toolTip);
            filterWheelStatus = new Statuser(labelFilterWheelStatus);

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new ASCOM.Wise40.Common.Wise40ToolstripRenderer();

            telescopeStatus.Show("");
            focuserStatus.Show("");
            safetooperateStatus.Show("");

            UpdateCheckmark(debugASCOMToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugASCOM));
            UpdateCheckmark(debugDeviceToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugDevice));
            UpdateCheckmark(debugAxesToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugAxes));
            UpdateCheckmark(debugLogicToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugAxes));
            UpdateCheckmark(debugEncodersToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugAxes));
            UpdateCheckmark(debugMotorsToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugAxes));
            UpdateCheckmark(debugExceptionsToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugExceptions));
            UpdateCheckmark(debugSafetyToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugSafety));
            UpdateCheckmark(debugDomeToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugDome));
            UpdateCheckmark(debugShutterToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugShutter));
            UpdateCheckmark(debugDAQsToolStripMenuItem, debugger.Debugging(Debugger.DebugLevel.DebugDAQs));
            //UpdateCheckmark(bypassSafetyToolStripMenuItem, _bypassSafety);
        }
        #endregion


        public double MPS(double kmh)
        {
            return kmh * (1000.0 / 3600.0);
        }

        public double KMH(double mps)
        {
            return mps * 3.6;
        }

        #region Refresh
        public void RefreshDisplay(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            DateTime utcTime = now.ToUniversalTime();
            DateTime localTime = now.ToLocalTime();
            ASCOM.Utilities.Util u = new ASCOM.Utilities.Util();
            Statuser.Severity severity = Statuser.Severity.Normal;

            bool refreshDome = domePacer.ShouldRefresh(now);
            bool refreshSafeToOperate = safettoperatePacer.ShouldRefresh(now);
            bool refreshTelescope = telescopePacer.ShouldRefresh(now);
            bool refreshFocus = focusPacer.ShouldRefresh(now);
            bool refreshFilterWheel = filterWheelPacer.ShouldRefresh(now);
            bool refreshForecast = forecastPacer.ShouldRefresh(now);
            string tip = null;

            if (refreshTelescope)
            {
                try
                {
                    telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wiseTelescope.Action("status", ""));
                } catch (Exception ex)
                {
                    telescopeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                }
            }

            if (refreshDome)
            {
                domeDigest = JsonConvert.DeserializeObject<DomeDigest>(wiseDome.Action("status", ""));
            }

            if (refreshSafeToOperate)
            {
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wiseSafeToOperate.Action("status", ""));
            }

            if (refreshFocus)
                focuserDigest = JsonConvert.DeserializeObject<FocuserDigest>(wiseFocuser.Action("status", ""));

            if (refreshForecast)
                forecast = wiseVantagePro.Action("forecast", "");

            //if (refreshFilterWheel)
            //{
            //    filterWheelDigest = JsonConvert.DeserializeObject<FilterWheelDigest>(
            //        wisesite.OperationalModeRequiresRESTServer ?
            //        remoteFilterWheel.Action("status", "") :
            //        wisefilterwheel.Action("status", ""));
            //}

            #region RefreshTelescope

            Angle telescopeRa = null, telescopeDec = null, telescopeHa = null;

            #region Coordinates Info
            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy\n hh:mm:ss tt");
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utcTime.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelSiderealValue.Text = Angle.FromHours(telescopeDigest.LocalSiderealTime).ToNiceString();

            telescopeRa = Angle.FromHours(telescopeDigest.Current.RightAscension, Angle.Type.RA);
            telescopeDec = Angle.FromDegrees(telescopeDigest.Current.Declination, Angle.Type.Dec);
            telescopeHa = Angle.FromHours(telescopeDigest.HourAngle, Angle.Type.HA);

            string safetyError = telescopeDigest.SafeAtCurrentCoordinates;

            labelRightAscensionValue.Text = telescopeRa.ToNiceString();
            labelDeclinationValue.Text = telescopeDec.ToNiceString();

            if (safetyError.Contains("Declination"))
                labelDeclinationValue.ForeColor = telescopeDigest.BypassCoordinatesSafety ?
                    warningColor :
                    unsafeColor;
            else
                labelDeclinationValue.ForeColor = safeColor;

            labelHourAngleValue.Text = telescopeHa.ToNiceString();
            labelHourAngleValue.ForeColor = safetyError.Contains("HourAngle") ? unsafeColor : safeColor;

            labelAltitudeValue.Text = Angle.FromDegrees(telescopeDigest.Altitude).ToNiceString();
            labelAltitudeValue.ForeColor = safetyError.Contains("Altitude") ? unsafeColor : safeColor;

            labelAzimuthValue.Text = Angle.FromDegrees(telescopeDigest.Azimuth).ToNiceString();

            #region Telescope Target
            if (telescopeDigest.Slewing)
            {
                if (telescopeDigest.Target.RightAscension == Const.noTarget)
                {
                    textBoxRA.Text = "";
                    toolTip.SetToolTip(textBoxRA, "Target RightAscension either not set or already reached");
                }
                else
                {
                    textBoxRA.Text = Angle.FromHours(telescopeDigest.Target.RightAscension, Angle.Type.RA).ToNiceString();
                    toolTip.SetToolTip(textBoxRA, "Current target RightAscension");
                }

                if (telescopeDigest.Target.Declination == Const.noTarget)
                {
                    textBoxDec.Text = "";
                    toolTip.SetToolTip(textBoxDec, "Target Declination either not set or already reached");
                }
                else
                {
                    textBoxDec.Text = Angle.FromDegrees(telescopeDigest.Target.Declination, Angle.Type.Dec).ToNiceString();
                    toolTip.SetToolTip(textBoxDec, "Current target Declination");
                }
            }
            else
            {
                textBoxRA.Text = "";
                textBoxDec.Text = "";
            }
            #endregion

            #endregion

            #region Annunciators

            buttonTelescopePark.Text = telescopeDigest.AtPark ? "Unpark" : "Park";

            annunciatorTrack.Cadence = telescopeDigest.Tracking ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
            annunciatorSlew.Cadence = telescopeDigest.Slewing ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
            annunciatorPulse.Cadence = telescopeDigest.PulseGuiding ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;

            double primaryRate = Const.rateStopped;
            double secondaryRate = Const.rateStopped;

            if (telescopeDigest.PrimaryPins.GuidePin)
                primaryRate = Const.rateGuide;
            else if (telescopeDigest.PrimaryPins.SetPin)
            {
                primaryRate = telescopeDigest.SlewPin ? Const.rateSlew : Const.rateSet;
            }

            if (telescopeDigest.SecondaryPins.GuidePin)
                secondaryRate = Const.rateGuide;
            else if (telescopeDigest.SecondaryPins.SetPin)
            {
                secondaryRate = telescopeDigest.SlewPin ? Const.rateSlew : Const.rateSet;
            }

            annunciatorRARateSlew.Cadence = annunciatorRARateSet.Cadence = annunciatorRARateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            if (primaryRate == Const.rateSlew)
                annunciatorRARateSlew.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (primaryRate == Const.rateSet)
                annunciatorRARateSet.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (primaryRate == Const.rateGuide)
                annunciatorRARateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;

            annunciatorDECRateSlew.Cadence = annunciatorDECRateSet.Cadence = annunciatorDECRateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            if (secondaryRate == Const.rateSlew)
                annunciatorDECRateSlew.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (secondaryRate == Const.rateSet)
                annunciatorDECRateSet.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            else if (secondaryRate == Const.rateGuide)
                annunciatorDECRateGuide.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            #endregion

            #region Inactivity Countdown

            if (telescopeDigest.Active)
            {
                if (telescopeDigest.Activities.Count() == 1 && telescopeDigest.Activities[0].StartsWith("GoingIdle"))
                {
                    TimeSpan ts = TimeSpan.FromSeconds(telescopeDigest.SecondsTillIdle);

                    labelCountdown.Text = string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
                    toolTip.SetToolTip(labelCountdown, "Inactivity countdown");
                }
                else
                {
                    labelCountdown.Text = "Active";
                    toolTip.SetToolTip(labelCountdown, string.Join(",", telescopeDigest.Activities));
                }
            }
            else if (telescopeDigest.SecondsTillIdle == -1)
            {
                labelCountdown.Text = "Idle";
                toolTip.SetToolTip(labelCountdown, "Observatory is idle");
            }

            #endregion

            #region Moon
            labelMoonIllum.Text = (moon.Illumination * 100).ToString("g2") + "%";
            labelMoonDist.Text = moon.Distance(telescopeRa.Radians, telescopeDec.Radians).ToNiceString();
            #endregion

            #region Air Mass
            Angle alt = Angle.FromDegrees(telescopeDigest.Altitude);
            labelAirMass.Text = wisesite.AirMass(alt.Radians).ToString("g4");
            #endregion

            telescopeStatus.Show(telescopeDigest.Status);

            #endregion

            #region RefreshSafety

            #region ComputerControl Annunciator
            if (safetooperateDigest.ComputerControlIsSafe)
            {
                annunciatorComputerControl.Text = "Computer has control";
                annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "The computer control switch is ON";

                if (wisesite.OperationalMode == WiseSite.OpMode.WISE)
                    foreach (Control c in readonlyControls)
                        c.Enabled = true;
            }
            else
            {
                annunciatorComputerControl.Text = "No computer control";
                annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                tip = "The computer control switch is OFF!";

                foreach (Control c in readonlyControls)
                    c.Enabled = false;
            }
            toolTip.SetToolTip(annunciatorComputerControl, tip);
            #endregion

            #region SafeToOperate Annunciator
            tip = null;
            string text = "";
            severity = Statuser.Severity.Normal;

            if (!safetooperateDigest.HumanInterventionIsSafe)
            {
                text = "Human Intervention";
                annunciatorSafeToOperate.ForeColor = unsafeColor;
                annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                severity = Statuser.Severity.Error;
                tip = String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n");
            }
            else if (safetooperateDigest.Bypassed)
            {
                text = "Safety bypassed";
                annunciatorSafeToOperate.ForeColor = warningColor;
                annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                severity = Statuser.Severity.Warning;
                tip = "Safety checks are bypassed!";
            }
            else if (safetooperateDigest.Safe)
            {
                text = "Safe to operate";
                annunciatorSafeToOperate.ForeColor = goodColor;
                annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                severity = Statuser.Severity.Good;
                tip = "Conditions are safe to operate.";
            }
            else
            {
                text = "Not safe to operate";
                annunciatorSafeToOperate.ForeColor = unsafeColor;
                annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                severity = Statuser.Severity.Error;
                tip = string.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n");
            }
            annunciatorSafeToOperate.Text = text;
            toolTip.SetToolTip(annunciatorSafeToOperate, tip);
            toolTip.SetToolTip(safetooperateStatus.Label, tip);
            safetooperateStatus.Show(text, 0, severity, true);
            #endregion

            #region Platform Annunciator
            tip = null;

            if (safetooperateDigest.PlatformIsSafe)
            {
                annunciatorDomePlatform.Text = "Platform is safe";
                annunciatorDomePlatform.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "Dome platform is at its lowest position.";
            }
            else
            {
                annunciatorDomePlatform.Text = "Platform is NOT SAFE";
                annunciatorDomePlatform.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                tip = "Dome platform is NOT at its lowest position!";
            }
            toolTip.SetToolTip(annunciatorDomePlatform, tip);
            #endregion

            #region Simulation Annunciator
            tip = null;

            if (WiseObject.Simulated)
            {
                annunciatorSimulation.Text = "SIMULATED HARDWARE";
                annunciatorSimulation.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                tip = "Hardware access is simulated by software";
            }
            else
            {
                annunciatorSimulation.Text = "";
                annunciatorSimulation.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                tip = "";
            }
            toolTip.SetToolTip(annunciatorSimulation, tip);
            #endregion

            #endregion

            #region RefreshDome
            labelDomeAzimuthValue.Text = Angle.FromDegrees(domeDigest.Azimuth, Angle.Type.Az).ToNiceString();
            domeStatus.Show(domeDigest.Status);
            buttonDomePark.Text = domeDigest.AtPark ? "Unpark" : "Park";
            buttonVent.Text = domeDigest.Vent ? "Close Vent" : "Open Vent";
            buttonProjector.Text = domeDigest.Projector ? "Turn projector Off" : "Turn projector On";

            annunciatorDome.Cadence = domeDigest.DirectionMotorsAreActive ?
                ASCOM.Controls.CadencePattern.SteadyOn :
                ASCOM.Controls.CadencePattern.SteadyOff;

            #region Shutter
            string msg = "";

            if (domeDigest.ShutterStatus.Contains("error"))
            {
                msg = "Shutter error";
                severity = Statuser.Severity.Error;
            }
            else
            {
                msg = "Shutter is " + domeDigest.ShutterStatus;
                severity = Statuser.Severity.Normal;
            }
            shutterStatus.Show(msg, 0, severity);

            switch (domeDigest.ShutterState)
            {
                case ShutterState.shutterOpening:
                    annunciatorShutter.Text = "SHUTTER(<->)";
                    annunciatorShutter.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                    break;

                case ShutterState.shutterClosing:
                    annunciatorShutter.Text = "SHUTTER(>-<)";
                    annunciatorShutter.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                    break;

                default:
                    annunciatorShutter.Text = "SHUTTER";
                    annunciatorShutter.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                    break;
            }
            #endregion

            #endregion

            #region RefreshSafeToOperate
                if (WiseSite.och == null || !WiseSite.och.Connected)
                {
                    string nc = "???";

                    List<Label> labels = new List<Label>() {
                    labelCloudCoverValue,
                    labelDewPointValue,
                    labelSkyTempValue,
                    labelTempValue,
                    labelHumidityValue,
                    labelPressureValue,
                    labelRainRateValue,
                    labelWindSpeedValue,
                    labelWindDirValue,
                    labelSunElevationValue,
                };

                    foreach (var label in labels)
                    {
                        label.Text = nc;
                        label.ForeColor = warningColor;
                    }
                }
                else
                {
                    try
                    {
                        #region ObservingConditions from OCH

                        ASCOM.DriverAccess.ObservingConditions oc = WiseSite.och;

                        labelDewPointValue.Text = oc.DewPoint.ToString() + "°C";
                        labelSkyTempValue.Text = oc.SkyTemperature.ToString() + "°C";
                        labelTempValue.Text = oc.Temperature.ToString() + "°C";
                        labelPressureValue.Text = oc.Pressure.ToString() + "mB";
                        labelWindDirValue.Text = oc.WindDirection.ToString() + "°";

                        labelHumidityValue.Text = oc.Humidity.ToString() + "%";
                        labelHumidityValue.ForeColor = Color.FromArgb(safetooperateDigest.Colors.HumidityColorArgb);

                        double d = oc.CloudCover;
                        labelCloudCoverValue.Text = Math.Floor(oc.CloudCover).ToString();
                        labelCloudCoverValue.ForeColor = Color.FromArgb(safetooperateDigest.Colors.CloudCoverColorArgb);

                        double windSpeedMps = oc.WindSpeed;
                        labelWindSpeedValue.Text = string.Format("{0:G3} km/h", KMH(windSpeedMps));
                        labelWindSpeedValue.ForeColor = Color.FromArgb(safetooperateDigest.Colors.WindSpeedColorArgb);

                        labelRainRateValue.Text = (oc.RainRate > 0.0) ? "Wet" : "Dry";
                        labelRainRateValue.ForeColor = Color.FromArgb(safetooperateDigest.Colors.RainColorArgb);

                        labelSunElevationValue.Text = safetooperateDigest.SunElevation.ToString("f1") + "°";
                        labelSunElevationValue.ForeColor = Color.FromArgb(safetooperateDigest.Colors.SunElevationColorArgb);
                        #endregion
                    }
                    catch (ASCOM.PropertyNotImplementedException ex)
                    {
                        this.safetooperateStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
                    }
                }
            #endregion

            #region RefreshFocuser
            labelFocusCurrentValue.Text = focuserDigest.Position.ToString();
            focuserStatus.Show(focuserDigest.StatusString);
            annunciatorFocus.Cadence = focuserDigest.IsMoving ?
                ASCOM.Controls.CadencePattern.SteadyOn :
                ASCOM.Controls.CadencePattern.SteadyOff;
            #endregion

            #region RefreshFilterWheel
            //    string fwstat = wisefilterwheel.Status;
            //    if (fwstat == "Idle")
            //    {
            //        annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            //    }
            //    else
            //    {
            //        annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            //        filterWheelStatus.Show(fwstat);
            //    }
            #endregion

            #region RefreshForecast
            dashStatus.Show("Forecast: " + forecast);
            #endregion
        }
        #endregion

        #region MainMenu
        private void digitalIOCardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HardwareForm hardwareForm = new HardwareForm(wiseTelescope);
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
            return telescopeDigest.SafeAtCurrentCoordinates == string.Empty;
        }

        #region TelescopeControl
        public void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!safetooperateDigest.ComputerControlIsSafe)
                telescopeStatus.Show(string.Join(", ", safetooperateDigest.UnsafeReasons), 1000, Statuser.Severity.Error);

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

            toolTip.SetToolTip(telescopeStatus.Label, "");
            List<Const.CardinalDirection> whichWay = new List<Const.CardinalDirection>();
            List<string> Directions = new List<string>();
            foreach (var m in movements)
            {
                if ((m._direction == Const.CardinalDirection.East || m._direction == Const.CardinalDirection.West) && telescopeDigest.PrimaryIsMoving)
                {
                    telescopeStatus.Show("Primary axis is in motion", 1000, Statuser.Severity.Error);
                    return;
                }
                else if ((m._direction == Const.CardinalDirection.North || m._direction == Const.CardinalDirection.South) && telescopeDigest.SecondaryIsMoving)
                {
                    telescopeStatus.Show("Secondary axis is in motion", 1000, Statuser.Severity.Error);
                    return;
                }
                whichWay.Add(m._direction);
                Directions.Add(m._direction.ToString());
            }

            if (telescopeDigest.BypassCoordinatesSafety)
            {
                string message = string.Format("Moving {0} at {1} (safety bypassed)", String.Join("-", Directions.ToArray()), WiseTele.RateName(handpadRate).Remove(0, 4));
                telescopeStatus.Show(message, 0, Statuser.Severity.Good);

                foreach (var m in movements)
                {
                    wiseTelescope.Action("handpad-move-axis",
                                                    JsonConvert.SerializeObject(new HandpadMoveAxisParameter {
                                                        axis = m._axis,
                                                        rate = m._rate }
                                                    ));
                }
            }
            else if (SafeAtCurrentCoords() || 
                            Convert.ToBoolean(wiseTelescope.Action("safe-to-move", JsonConvert.SerializeObject(whichWay))))
            {
                string message = string.Format("Moving {0} at {1}", String.Join("-", Directions.ToArray()), WiseTele.RateName(handpadRate).Remove(0, 4));
                telescopeStatus.Show(message, 0, Statuser.Severity.Good);

                try
                {
                    foreach (var m in movements)
                    {
                        wiseTelescope.Action("handpad-move-axis",
                                                        JsonConvert.SerializeObject(new HandpadMoveAxisParameter
                                                        {
                                                            axis = m._axis,
                                                            rate = m._rate
                                                        }
                                                        ));
                    }
                }
                catch (Exception ex)
                {
                    telescopeStatus.Show("Not safe to move", 2000, Statuser.Severity.Error);
                    toolTip.SetToolTip(telescopeStatus.Label, ex.Message.Replace(',', '\n'));
                }
            }
            else
            {
                string message = string.Format("Unsafe to move {0}", String.Join("-", Directions.ToArray()));
                telescopeStatus.Show(message, 2000, Statuser.Severity.Error);
            }
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            wiseTelescope.Action("handpad-stop", "");
        }

        private void debuggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (debuggingForm.IsDisposed || debuggingForm == null)
                debuggingForm = new DebuggingForm();
            debuggingForm.Visible = true;
        }

        private void buttonGoCoord_Click(object sender, EventArgs e)
        {
            if ((goToMode == GoToMode.Ra || goToMode == GoToMode.DeltaRa) && !telescopeDigest.Tracking)
            {
                telescopeStatus.Show("Telescope is NOT tracking!", 1000, Statuser.Severity.Error);
                return;
            }

            if ((goToMode == GoToMode.Ha || goToMode == GoToMode.DeltaHa) && telescopeDigest.Tracking)
            {
                telescopeStatus.Show("Telescope is TRACKING!", 1000, Statuser.Severity.Error);
                return;
            }

            try
            {
                double ra = ascomutil.HMSToHours(textBoxRA.Text);
                double dec = ascomutil.DMSToDegrees(textBoxDec.Text);
                Angle ang;

                switch (goToMode)
                {
                    case GoToMode.Ra:
                        ra = ascomutil.HMSToHours(textBoxRA.Text);
                        dec = ascomutil.DMSToDegrees(textBoxDec.Text);
                        break;

                    case GoToMode.Ha:
                        ang = wisesite.LocalSiderealTime - Angle.FromHours(ascomutil.HMSToHours(textBoxRA.Text));
                        ra = ang.Hours;
                        dec = ascomutil.DMSToDegrees(textBoxDec.Text);
                        break;

                    case GoToMode.DeltaRa:
                        ra = telescopeDigest.Current.RightAscension + ascomutil.HMSToHours(textBoxRA.Text);
                        dec = telescopeDigest.Current.Declination + ascomutil.DMSToDegrees(textBoxDec.Text);
                        break;

                    case GoToMode.DeltaHa:
                        double ha = telescopeDigest.HourAngle + ascomutil.HMSToHours(textBoxRA.Text);
                        ang = Angle.FromHours(ha);
                        ra = (wisesite.LocalSiderealTime - ang).Hours;
                        dec = telescopeDigest.Current.Declination + ascomutil.DMSToDegrees(textBoxDec.Text);
                        break;

                }

                telescopeStatus.Show(string.Format("Slewing to ra: {0} dec: {1}",
                    Angle.FromHours(ra).ToNiceString(), Angle.FromDegrees(dec).ToNiceString()), 0, Statuser.Severity.Good);
                wiseTelescope.SlewToCoordinatesAsync(ra, dec);
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 5000, Statuser.Severity.Error);
            }
        }

        private void buttonTelescopeStop_Click(object sender, EventArgs e)
        {
            wiseTelescope.AbortSlew();
            telescopeStatus.Show("Stopped", 1000, Statuser.Severity.Good);
        }

        private void textBoxRA_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string text = "";

            switch (goToMode)
            {
                case GoToMode.Ra:
                    text = Angle.FromHours(telescopeDigest.Current.RightAscension).ToString();
                    break;
                case GoToMode.Ha:
                    text = Angle.FromHours(telescopeDigest.HourAngle, Angle.Type.HA).ToString();
                    break;
                case GoToMode.DeltaRa:
                case GoToMode.DeltaHa:
                    text = new Angle("00h00m00.0s").ToString();
                    break;

            }
            textBoxRA.Text = text.Replace('h', ':').Replace('m', ':').Replace('s', ' ');
        }

        private void textBoxDec_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string text = "";

            switch (goToMode)
            {
                case GoToMode.Ra:
                case GoToMode.Ha:
                    text = Angle.FromDegrees(telescopeDigest.Current.Declination).ToString();
                    break;
                case GoToMode.DeltaRa:
                case GoToMode.DeltaHa:
                    text = new Angle("00:00:00.0").ToString();
                    break;
            }
            textBoxDec.Text = text;
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
            wiseTelescope.Action("park", "");            
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
                domeSlaveDriver.StopShutter("User action");
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
                domeSlaveDriver.StopShutter("User action");
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

            wiseDome.Action("set-azimuth", az.ToString());
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
                return;
            }

            try
            {
                wiseDome.SlewToAzimuth(az);
                domeStatus.Show(string.Format("Slewing to {0}", Angle.FromDegrees(az).ToNiceString()), 0, Statuser.Severity.Normal);
            }
            catch (Exception ex)
            {
                domeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void buttonDomeLeft_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                wiseDome.Action("start-moving", "ccw");
                domeStatus.Show("Moving CCW", 0, Statuser.Severity.Good);
            }
            catch (Exception ex)
            {
                domeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void buttonDomeRight_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                wiseDome.Action("start-moving", "cw");
                domeStatus.Show("Moving CW", 0, Statuser.Severity.Good);
            }
            catch (Exception ex) {
                domeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void buttonDomeRight_MouseUp(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Stopped moving CW", 1000, Statuser.Severity.Good);
            wiseDome.Action("halt", "");
        }

        private void buttonDomeLeft_MouseUp(object sender, MouseEventArgs e)
        {
            domeStatus.Show("Stopped moving CCW", 1000, Statuser.Severity.Good);
            wiseDome.Action("halt", "");
        }

        private void buttonDomeStop_Click(object sender, EventArgs e)
        {
            wiseDome.Action("halt", "");
            domeStatus.Show("Stopped moving", 1000, Statuser.Severity.Good);
        }

        private void buttonCalibrateDome_Click(object sender, EventArgs e)
        {
            try
            {
                wiseDome.Action("calibrate", "");
                domeStatus.Show("Started dome calibration", 1000, Statuser.Severity.Good);
            } catch (Exception ex)
            {
                domeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void buttonVent_Click(object sender, EventArgs e)
        {
            bool status;

            if (wisesite.OperationalModeRequiresRESTServer)
            {
                status = JsonConvert.DeserializeObject<bool>(wiseDome.Action("vent", ""));
                wiseDome.Action("vent", (!status).ToString());
            }
            else
            {
                status = JsonConvert.DeserializeObject<bool>(wiseDome.Action("vent", ""));
                wiseDome.Action("vent", (!status).ToString());
            }
        }

        private void buttonDomePark_Click(object sender, EventArgs e)
        {
            try
            {
                if (wiseDome.AtPark)
                    wiseDome.Action("unpark", "");
                else
                    Task.Run(() => wiseDome.Park());
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
            try
            {
                wiseFocuser.Action("halt", "Button focust stop");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocuserStop_Click(object sender, EventArgs e)
        {
            try
            {
                wiseFocuser.Action("halt", "Button focuser stop");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocusUp_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {

                wiseFocuser.Action("move", "up");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocusDown_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                wiseFocuser.Action("move", "down");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocusStop(object sender, MouseEventArgs e)
        {
            try
            {
                wiseFocuser.Action("halt", "Button focuser stop");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocusGoto_Click(object sender, EventArgs e)
        {
            if (textBoxFocusGotoPosition.Text == string.Empty)
                return;

            try
            {
                wiseFocuser.Move(Convert.ToInt32(textBoxFocusGotoPosition.Text));
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

            if (pos < 0 || pos >= focuserMaxStep)
            {
                focuserStatus.Show("Bad focuser target position", 1000, Statuser.Severity.Error);
                box.Text = string.Empty;
            }
        }

        private void buttonFocusAllUp_Click(object sender, EventArgs e)
        {
            try
            {
                wiseFocuser.Action("move", "all-up");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }

        private void buttonFocusAllDown_Click(object sender, EventArgs e)
        {
            try
            {

                if (wiseFocuser.Position > focuserLowerLimit)
                    wiseFocuser.Action("move", "all-down");
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
                return;
            }
        }
        #endregion

        #region Settings
        private void debugAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.StartDebugging(Debugger.DebugLevel.DebugAll);

            foreach (var item in debugMenuItems)
                UpdateCheckmark(item, true);
        }

        private void domeAutoCalibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            bool autoCalibrate = !IsCheckmarked(item);

            wiseDome.Action("auto-calibrate", autoCalibrate.ToString());
            UpdateCheckmark(item, autoCalibrate);
            UpdateAlteredItems(item, "Dome");
        }

        private void enslaveDomeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            bool onOff = !IsCheckmarked(item);

            wiseTelescope.Action("enslave-dome", onOff.ToString());
            UpdateCheckmark(item, onOff);
            UpdateAlteredItems(item, "Telescope");
        }

        private void buttonZenith_Click(object sender, EventArgs e)
        {
            wiseTelescope.Action("move-to-preset", "zenith");
        }

        private void buttonHandleCover_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to cover station", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "cover");
        }

        private void buttonFlat_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to Zenith", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "flat");
        }

        private void checkBoxTrack_Click(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;

            wiseTelescope.Tracking = box.Checked;
        }

        private void debugNoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.StopDebugging(Debugger.DebugLevel.DebugAll);

            foreach (var item in debugMenuItems)
                UpdateCheckmark(item, false);
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
            else if (item == debugSafetyToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugSafety;
            else if (item == debugDomeToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugDome;
            else if (item == debugShutterToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugShutter;
            else if (item == debugDAQsToolStripMenuItem)
                selectedLevel = Debugger.DebugLevel.DebugDAQs;

            if (selectedLevel == Debugger.DebugLevel.DebugNone)
                return;

            if (debugger.Debugging(selectedLevel))
                debugger.StopDebugging(selectedLevel);
            else
                debugger.StartDebugging(selectedLevel);
            UpdateCheckmark(item, debugger.Debugging(selectedLevel));
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
            bool currentSetting = IsCheckmarked(item);
            if (alteredItems.ContainsKey(item))
                alteredItems.Remove(item);
            else
                alteredItems[item] = title;

            string alterations = string.Empty;
            foreach (var key in alteredItems.Keys)
            {
                string text = ((ToolStripMenuItem)key).Text;
                string mark;

                if (IsCheckmarked(key as ToolStripMenuItem))
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
            WiseDome.Instance.WriteProfile();
            WiseTele.WriteProfile();
            if (_saveFocusUpperLimit || _saveFocusLowerLimit)
                WiseFocuser.Instance.WriteProfile();
            saveToProfileToolStripMenuItem.Text = "Save To Profile";
        }

        private void toolStripMenuItemSafeToOperate_Click(object sender, EventArgs e)
        {
            new SafeToOperateSetupDialogForm().Show();
        }

        private void toolStripMenuItemFilterWheel_Click(object sender, EventArgs e)
        {
            new FilterWheelSetupDialogForm().Show();
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

            bool sync = Convert.ToBoolean(wiseDome.Action("sync-vent-with-shutter", ""));

            sync = !sync;
            wiseDome.Action("sync-vent-with-shutter", sync.ToString());
            UpdateCheckmark(item, sync);
            UpdateAlteredItems(item, "Dome");
        }

        private void buttonFocusIncrease_Click(object sender, EventArgs e)
        {
            int newPos = wiseFocuser.Position + Convert.ToInt32(comboBoxFocusStep.Text);
            if (newPos > focuserUpperLimit)
                newPos = focuserUpperLimit;

            if (newPos != wiseFocuser.Position)
                wiseFocuser.Move(newPos);
        }

        private void buttonFocusDecrease_Click(object sender, EventArgs e)
        {
            int newPos = wiseFocuser.Position - Convert.ToInt32(comboBoxFocusStep.Text);
            if (newPos < focuserLowerLimit)
                newPos = focuserLowerLimit;

            if (newPos != wiseFocuser.Position)
                wiseFocuser.Move(newPos);
        }

        private void buttonFilterWheelGo_Click(object sender, EventArgs e)
        {
            short targetPosition = (short)comboBoxFilterWheelPositions.SelectedIndex;
            short humanTargetPosition = (short)(targetPosition + 1);

            if (WiseObject.Simulated)
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
            wiseTelescope.Action("full-stop", "");
            wiseDome.Action("halt", "");
            wiseDome.Action("shutter", "halt");
            wiseFocuser.Action("stop", "Button full stop");
        }

        private void buttonProjector_Click(object sender, EventArgs e)
        {
            bool status;

            if (wisesite.OperationalModeRequiresRESTServer)
            {
                status = JsonConvert.DeserializeObject<bool>(wiseDome.Action("projector", ""));
                wiseDome.Action("projector", (!status).ToString());
            }
            else
            {
                status = JsonConvert.DeserializeObject<bool>(wiseDome.Action("projector", ""));
                wiseDome.Action("projector", (!status).ToString());
            }
        }

        private void telescopeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new TelescopeSetupDialogForm().Show();
        }

        private void domeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DomeSetupDialogForm().Show();
        }

        private void manage3InchFilterInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wisefilterwheel.init();
            new FiltersForm(3).Show();
        }

        private void buttonTrack_Click(object sender, EventArgs e)
        {
            try
            {
                wiseTelescope.Tracking = !wiseTelescope.Tracking;
            }
            catch (Exception ex)
            {
                telescopeStatus.Show(ex.Message, 2000, Statuser.Severity.Error);
            }
        }

        private void UpdateGoToControls()
        {
            string modeTip = "", raTip = "", decTip = "";
            string raText = "", decText = "";

            switch (goToMode)
            {
                case GoToMode.Ra:
                    raText = "RA";
                    decText = "Dec";
                    modeTip = "Enter RA and Dec coordinates and pres Go";
                    raTip = "Double click for current Right Ascension";
                    decTip = "Double click for current Declination";
                    break;
                case GoToMode.Ha:
                    raText = "HA";
                    decText = "Dec";
                    modeTip = "Enter HA and Dec coordinates and pres Go";
                    raTip = "Double click for current Hour Angle";
                    decTip = "Double click for current Declination";
                    break;
                case GoToMode.DeltaRa:
                    raText = "dRA";
                    decText = "dDec";
                    modeTip = "Enter RA and Dec distances and pres Go";
                    raTip = "Double click for default RA distance";
                    decTip = "Double click for default Dec distance";
                    break;
                case GoToMode.DeltaHa:
                    raText = "dHA";
                    decText = "dDec";
                    modeTip = "Enter HA and Dec distances and pres Go";
                    raTip = "Double click for default HA distance";
                    decTip = "Double click for default Dec distance";
                    break;
            }

            labelRA.Text = raText + ":";
            labelDec.Text = decText + ":";
            textBoxRA.Text = "";
            textBoxDec.Text = "";
            toolTip.SetToolTip(comboBoxGoToMode, modeTip);
            toolTip.SetToolTip(textBoxRA, raTip);
            toolTip.SetToolTip(textBoxDec, decTip);
        }

        private void comboBoxGoToMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            goToMode = (GoToMode)cb.SelectedIndex;
            UpdateGoToControls();
            textBoxRA.Focus();
        }

        private void davisVantagePro2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.VantagePro.SetupDialogForm().Show();
        }

        private void boltwoodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.Boltwood.SetupDialogForm().Show();
        }

        public void UpdateCheckmark(ToolStripMenuItem item, bool state)
        {
            if (state && !item.Text.EndsWith(Const.checkmark))
                item.Text += Const.checkmark;
            if (!state && item.Text.EndsWith(Const.checkmark))
                item.Text = item.Text.Remove(item.Text.Length - Const.checkmark.Length);
            item.Tag = state;
            item.Invalidate();
        }

        public bool IsCheckmarked(ToolStripMenuItem item)
        {
            return item.Text.EndsWith(Const.checkmark);
        }

        public void StopEverything(Exception e = null)
        {
            try
            {
                wiseTelescope.Action("full-stop", "");
                wiseDome.Action("halt", "");
                wiseDome.Action("shutter", "halt");
                wiseFocuser.Action("halt", "Stop everything");
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

    public class RefreshPacer
    {
        private TimeSpan _interval;
        private DateTime _lastTime;

        public RefreshPacer(TimeSpan interval)
        {
            _interval = interval;
            _lastTime = DateTime.MinValue;
        }

        public bool ShouldRefresh(DateTime cachedTime)
        {
            if ((cachedTime - _lastTime) >= _interval)
            {
                _lastTime = cachedTime;
                return true;
            }
            return false;
        }
    }

}
