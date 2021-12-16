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
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40SafeToOperate;

using ASCOM.Utilities;
using Newtonsoft.Json;
using TA.WinFormsControls;

namespace Dash
{
    public partial class FormDash : Form
    {
        public WiseSite wisesite = WiseSite.Instance;
        private readonly Util ascomutil = new Util();
        public enum GoToMode { RaDec, HaDec, AltAz /*, DeltaRa, DeltaHa */};
        private GoToMode goToMode = GoToMode.RaDec;

        private readonly Debugger debugger = Debugger.Instance;

        private readonly Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, safetooperateStatus, filterWheelStatus, filterWheelArduinoStatus;

        private double handpadRate = Const.rateSlew;

        private readonly RefreshPacer domePacer = new RefreshPacer(TimeSpan.FromSeconds(1));
        private readonly RefreshPacer safettoperatePacer = new RefreshPacer(TimeSpan.FromSeconds(20));
        private readonly RefreshPacer focusPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        private readonly RefreshPacer filterWheelPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        private readonly RefreshPacer telescopePacer = new RefreshPacer(TimeSpan.FromMilliseconds(200));
        private readonly RefreshPacer forecastPacer = new RefreshPacer(TimeSpan.FromMinutes(2));
        private readonly RefreshPacer humanInterventionPacer = new RefreshPacer(TimeSpan.FromSeconds(5));

        private SafeToOperateDigest safetooperateDigest = null;
        private DomeDigest domeDigest = null;
        private TelescopeDigest telescopeDigest = null;
        private FocuserDigest focuserDigest = null;
        private WiseFilterWheelDigest filterWheelDigest = null;
        private string forecast;
        private bool bypassSafetyPending, locallyBypassed;

        public readonly Color safeColor = Statuser.colors[Statuser.Severity.Normal];
        public readonly Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        public readonly Color warningColor = Statuser.colors[Statuser.Severity.Warning];
        public readonly Color goodColor = Statuser.colors[Statuser.Severity.Good];

        public ASCOM.DriverAccess.Telescope wiseTelescope;
        public ASCOM.DriverAccess.Dome wiseDome;
        public ASCOM.DriverAccess.Focuser wiseFocuser;
        public ASCOM.DriverAccess.FilterWheel wiseFilterWheel;
        public ASCOM.DriverAccess.SafetyMonitor wiseSafeToOperate;
        public ASCOM.DriverAccess.ObservingConditions wiseVantagePro, wiseBoltwood;

        private List<ASCOM.DriverAccess.AscomDriver> drivers;

        private readonly int focuserMaxStep = 0;
        private readonly int focuserLowerLimit = 0;
        private readonly int focuserUpperLimit = 0;

        private static Dictionary<WiseSite.OpMode, List<Control>> ActiveControls;
        private static List<Control> WiseActiveControls, ACPActiveControls, LCOActiveControls;

        private static Dictionary<WiseSite.OpMode, List<Control>> InvisibleControls;
        private static List<Control> WiseInvisibleControls, ACPInvisibleControls, LCOInvisibleControls;
        private static Dictionary<TextBox, Tuple<double, string>> targetTextBox = new Dictionary<TextBox, Tuple<double, string>>(6);
        private static Dictionary<TextBox, bool> targetIsActive = new Dictionary<TextBox, bool>(6);

        #region Initialization
        public FormDash()
        {
            InitializeComponent();

            foreach (var tb in new List<TextBox> { textBoxRaDecRa, textBoxRaDecDec, textBoxHaDecHa, textBoxHaDecDec, textBoxAltAzAlt, textBoxAltAzAz })
                targetIsActive[tb] = false;

            wiseTelescope = new ASCOM.DriverAccess.Telescope("ASCOM.AlpacaDynamic1.Telescope");
            wiseDome = new ASCOM.DriverAccess.Dome("ASCOM.AlpacaDynamic1.Dome");
            wiseFocuser = new ASCOM.DriverAccess.Focuser("ASCOM.AlpacaDynamic1.Focuser");
            wiseFilterWheel = new ASCOM.DriverAccess.FilterWheel("ASCOM.AlpacaDynamic1.FilterWheel");
            wiseSafeToOperate = new ASCOM.DriverAccess.SafetyMonitor("ASCOM.AlpacaDynamic1.SafetyMonitor");
            wiseVantagePro = new ASCOM.DriverAccess.ObservingConditions(Const.WiseDriverID.VantagePro);
            wiseBoltwood = new ASCOM.DriverAccess.ObservingConditions(Const.WiseDriverID.Boltwood);

            drivers = new List<ASCOM.DriverAccess.AscomDriver> {
                wiseTelescope,
                wiseDome,
                wiseFocuser,
                wiseFilterWheel,
                wiseSafeToOperate,
                wiseVantagePro,
                wiseBoltwood,
            };

            //filterWheelForm = new FilterWheelForm(wiseFilterWheel);

            dashStatus = new Statuser(labelDashStatus);
            telescopeStatus = new Statuser(labelTelescopeStatus);
            domeStatus = new Statuser(labelDomeStatus);
            shutterStatus = new Statuser(labelDomeShutterStatus, toolTip);
            focuserStatus = new Statuser(labelFocuserStatus);
            safetooperateStatus = new Statuser(labelWeatherStatus, toolTip);
            filterWheelStatus = new Statuser(labelFilterWheelStatus);
            filterWheelArduinoStatus = new Statuser(labelFWArduinoStatus);

            try
            {
                focuserMaxStep = wiseFocuser.MaxStep;
                focuserLowerLimit = Convert.ToInt32(wiseFocuser.Action("limit", "lower"));
                focuserUpperLimit = Convert.ToInt32(wiseFocuser.Action("limit", "upper"));
            }
            catch
            {
                focuserStatus.Show("Cannot connect to ASCOM server", severity: Statuser.Severity.Error, silent: true);
            }

            WiseActiveControls = new List<Control>() {
                        textBoxRaDecRa, textBoxRaDecDec,
                        textBoxHaDecHa, textBoxHaDecDec,
                        textBoxAltAzAlt, textBoxAltAzAz,
                        buttonGoCoord,
                        buttonNorth, buttonSouth, buttonEast, buttonWest,
                        buttonNW, buttonNE, buttonSE, buttonSW,
                        buttonStop, buttonMainStop,
                        buttonTrack,
                        buttonZenith, buttonHandleCover, buttonTelescopePark,

                        buttonDomeLeft, buttonDomeRight, buttonDomeStop, buttonDomePark,
                        buttonDomeAzGo, buttonDomeAzSet, textBoxDomeAzValue,
                        buttonCalibrateDome, buttonVent,
                        buttonFullOpenShutter, buttonFullCloseShutter, buttonOpenShutter, buttonCloseShutter, buttonStopShutter,

                        buttonFocusAllDown, buttonFocusAllUp, buttonFocusDecrease, buttonFocusIncrease,
                        buttonFocuserStop, buttonFocusGoto, textBoxFocusGotoPosition,
                        buttonFocusUp, buttonFocusDown, comboBoxFocusStep,

                        pictureBoxStop,

                        radioButtonGuide, radioButtonSet, radioButtonSlew,
                        buttonFilterWheelGo, comboBoxFilterWheelPositions
                    };

            ACPActiveControls = new List<Control>() {
                        buttonDomeLeft, buttonDomeRight, buttonDomeStop, buttonDomePark,
                        buttonDomeAzGo, buttonDomeAzSet, textBoxDomeAzValue,
                        buttonCalibrateDome, buttonVent,
                        buttonFullOpenShutter, buttonFullCloseShutter, buttonOpenShutter, buttonCloseShutter, buttonStopShutter,
                    };

            LCOActiveControls = new List<Control>();

            ActiveControls = new Dictionary<WiseSite.OpMode, List<Control>>() {
                { WiseSite.OpMode.WISE, WiseActiveControls },
                { WiseSite.OpMode.ACP, ACPActiveControls },
                { WiseSite.OpMode.LCO, LCOActiveControls },
            };

            LCOInvisibleControls = new List<Control>()
            {
                pictureBoxStop,
                labelFWArduinoStatus, labelFWFilter, labelFWWheel, labelFWPosition, labelFilterWheelStatus,
            };

            ACPInvisibleControls = new List<Control>()
            {
                pictureBoxStop,
            };

            WiseInvisibleControls = new List<Control>();

            InvisibleControls = new Dictionary<WiseSite.OpMode, List<Control>>() {
                { WiseSite.OpMode.WISE, WiseInvisibleControls },
                { WiseSite.OpMode.ACP, ACPInvisibleControls },
                { WiseSite.OpMode.LCO, LCOInvisibleControls },
            };

            WiseSite.OpMode opMode = WiseSite.OperationalMode;

            if (opMode == WiseSite.OpMode.ACP || opMode == WiseSite.OpMode.LCO)
            {
                groupBoxTarget.Text += $"(from {opMode}) ";
            }

            foreach (var c in InvisibleControls[opMode])
                c.Visible = false;

            annunciatorOpMode.Text = $"Mode: {opMode}";
            annunciatorOpMode.ForeColor = warningColor;
            annunciatorOpMode.Cadence = CadencePattern.SteadyOn;
            switch (opMode)
            {
                case WiseSite.OpMode.LCO:
                    toolTip.SetToolTip(annunciatorOpMode, "Wise40 is controlled by LCO");
                    break;
                case WiseSite.OpMode.ACP:
                    toolTip.SetToolTip(annunciatorOpMode, "Wise40 is controlled by ACP");
                    break;
                case WiseSite.OpMode.WISE:
                    toolTip.SetToolTip(annunciatorOpMode, "Wise40 is controlled by this Dashboard");
                    break;
            }

            UpdateHumanInterventionControls();

            UpdateCheckmark(LCOToolStripMenuItem, opMode == WiseSite.OpMode.LCO);
            UpdateCheckmark(WISEToolStripMenuItem, opMode == WiseSite.OpMode.WISE);
            UpdateCheckmark(ACPToolStripMenuItem, opMode == WiseSite.OpMode.ACP);

            debugMenuItemDict = new Dictionary<Debugger.DebugLevel, ToolStripMenuItem>() {
                { Debugger.DebugLevel.DebugActivity, debugActivityToolStripMenuItem},
                { Debugger.DebugLevel.DebugASCOM, debugASCOMToolStripMenuItem },
                { Debugger.DebugLevel.DebugAxes, debugAxesToolStripMenuItem },
                { Debugger.DebugLevel.DebugDAQs, debugDAQsToolStripMenuItem},
                { Debugger.DebugLevel.DebugDevice, debugDeviceToolStripMenuItem },
                { Debugger.DebugLevel.DebugDome, debugDomeToolStripMenuItem },
                { Debugger.DebugLevel.DebugEncoders, debugEncodersToolStripMenuItem },
                { Debugger.DebugLevel.DebugExceptions, debugExceptionsToolStripMenuItem },
                { Debugger.DebugLevel.DebugFocuser, debugFocuserToolStripMenuItem},
                { Debugger.DebugLevel.DebugFilterWheel, debugFilterWheelToolStripMenuItem},
                { Debugger.DebugLevel.DebugHTTP, debugHTTPToolStripMenuItem},
                { Debugger.DebugLevel.DebugLogic, debugLogicToolStripMenuItem },
                { Debugger.DebugLevel.DebugMoon, debugMoonToolStripMenuItem},
                { Debugger.DebugLevel.DebugMotors, debugMotorsToolStripMenuItem},
                { Debugger.DebugLevel.DebugSafety, debugSafetyToolStripMenuItem },
                { Debugger.DebugLevel.DebugShutter, debugShutterToolStripMenuItem},
                { Debugger.DebugLevel.DebugWise, debugWiseToolStripMenuItem},
                { Debugger.DebugLevel.DebugTele, debugTelescopeToolStripMenuItem},
                { Debugger.DebugLevel.DebugWeather, debugWeatherToolStripMenuItem},
            };

            menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            ToolStripManager.Renderer = new ASCOM.Wise40.Common.Wise40ToolstripRenderer();

            telescopeStatus.Show("");
            focuserStatus.Show("");
            safetooperateStatus.Show("");

            UpdateDebuggingCheckmarks();

            UpdateFilterWheelControls();
            tabControlGoTo.SelectedTab = tabPageRaDec;
        }
        #endregion

        private Dictionary<Debugger.DebugLevel, ToolStripMenuItem> debugMenuItemDict = new Dictionary<Debugger.DebugLevel, ToolStripMenuItem>();

        private void UpdateDebuggingCheckmarks()
        {
            try
            {
                Enum.TryParse<Debugger.DebugLevel>(wiseTelescope.Action("debug", ""), out Debugger.DebugLevel current);

                foreach (var level in debugMenuItemDict.Keys)
                    UpdateCheckmark(debugMenuItemDict[level], (current & level) != 0);
            }
            catch { }
        }

        public void UpdateFilterWheelControls()
        {
            if (! WiseSite.FilterWheelInUse)
            {
                toolStripMenuItemFilterWheel.Enabled = false;
                groupBoxFilterWheel.Text = $" FilterWheel (not used in {WiseSite.OperationalMode} mode) ";
                return;
            }

            toolStripMenuItemFilterWheel.Enabled = true;

            if (filterWheelDigest == null)
                return;

            if (!filterWheelDigest.Enabled)
            {
                filterWheelStatus.Show("Disabled (see Settings -> FilterWheel -> Loaded Filters)");
            }
        }

        #region Refresh
        public void RefreshDisplay(object sender, EventArgs e)
        {
            timerRefreshDisplay.Enabled = false;

            DateTime now = DateTime.Now;
            DateTime utcTime = now.ToUniversalTime();
            DateTime localTime = now.ToLocalTime();
            Statuser.Severity severity;
            WiseSite.OpMode opMode = WiseSite.OperationalMode;

            bool refreshDome = domePacer.ShouldRefresh(now);
            bool refreshSafeToOperate = safettoperatePacer.ShouldRefresh(now);
            bool refreshTelescope = telescopePacer.ShouldRefresh(now);
            bool refreshFocus = focusPacer.ShouldRefresh(now);
            bool refreshFilterWheel = opMode != WiseSite.OpMode.LCO && filterWheelPacer.ShouldRefresh(now);
            bool refreshForecast = forecastPacer.ShouldRefresh(now);
            bool refreshHumanIntervention = humanInterventionPacer.ShouldRefresh(now);
            string tip;

            #region GetStatuses
            if (refreshTelescope)
            {
                try
                {
                    telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(wiseTelescope.Action("status", ""));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    telescopeStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }

            if (refreshDome)
            {
                try
                {
                    domeDigest = JsonConvert.DeserializeObject<DomeDigest>(wiseDome.Action("status", ""));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    domeStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }

            if (refreshSafeToOperate)
            {
                groupBoxSafeToOperate.Text = " Safe To Operate (latest readings) - refreshing ";
                try
                {
                    safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wiseSafeToOperate.Action("status", ""));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    safetooperateStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }
            else
            {
                string ttr = safettoperatePacer.TimeToRefresh.ToMinimalString();
                groupBoxSafeToOperate.Text = $" Safe To Operate (latest readings) " + (string.IsNullOrEmpty(ttr) ?
                    "" : $"- refreshing in {ttr} ");
            }

            if (refreshFocus)
            {
                groupBoxFocuser.Text = " Focuser - refreshing ";
                try
                {
                    focuserDigest = JsonConvert.DeserializeObject<FocuserDigest>(wiseFocuser.Action("status", ""));
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        ex = ex.InnerException;

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    focuserStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }
            else
            {
                string ttr = focusPacer.TimeToRefresh.ToMinimalString();
                groupBoxFocuser.Text = $" Focuser " + (string.IsNullOrEmpty(ttr) ?
                    "" : $"- refreshing in {ttr} ");
            }

            if (refreshForecast)
            {
                forecast = wiseVantagePro.Action("forecast", "");
            }

            if (refreshHumanIntervention)
                UpdateHumanInterventionControls();

            if (WiseSite.FilterWheelInUse)
            {
                if (refreshFilterWheel)
                {
                    groupBoxFilterWheel.Text = " FilterWheel - refreshing ";
                    try
                    {
                        filterWheelDigest = JsonConvert.DeserializeObject<WiseFilterWheelDigest>(wiseFilterWheel.Action("status", ""));
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                            ex = ex.InnerException;

                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        filterWheelStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                    }
                }
                else
                {
                    string ttr = filterWheelPacer.TimeToRefresh.ToMinimalString();
                    groupBoxFilterWheel.Text = $" FilterWheel " + (string.IsNullOrEmpty(ttr) ?
                        "" : $"- refreshing in {ttr} ");
                }
            }
            #endregion

            #region RefreshTelescope
            Angle telescopeRa = null, telescopeDec = null, telescopeHa;

            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy\n hh:mm:ss tt");
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utcTime.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");

            if (telescopeDigest != null)
            {
                #region Coordinates
                labelSiderealValue.Text = Angle.RaFromHours(telescopeDigest.LocalSiderealTime).ToNiceString();

                telescopeRa = Angle.RaFromHours(telescopeDigest.Current.RightAscension);
                telescopeDec = Angle.DecFromDegrees(telescopeDigest.Current.Declination);
                telescopeHa = Angle.HaFromHours(telescopeDigest.Current.HourAngle);

                string safetyError = telescopeDigest.SafeAtCurrentCoordinates;

                labelRightAscensionValue.Text = telescopeRa.ToNiceString();
                labelDeclinationValue.Text = telescopeDec.ToNiceString();

                if (safetyError.Contains("Declination"))
                {
                    labelDeclinationValue.ForeColor = telescopeDigest.BypassCoordinatesSafety ?
                        warningColor :
                        unsafeColor;
                }
                else
                {
                    labelDeclinationValue.ForeColor = safeColor;
                }

                labelHourAngleValue.Text = telescopeHa.ToNiceString();
                labelHourAngleValue.ForeColor = safetyError.Contains("HourAngle") ? unsafeColor : safeColor;

                labelAltitudeValue.Text = Angle.FromDegrees(telescopeDigest.Current.Altitude, Angle.AngleType.Deg).ToNiceString();
                labelAltitudeValue.ForeColor = safetyError.Contains("Altitude") ? unsafeColor : safeColor;

                labelAzimuthValue.Text = Angle.FromDegrees(telescopeDigest.Current.Azimuth, Angle.AngleType.Deg).ToNiceString();
                #endregion

                double coord;
                string coordName;

                targetTextBox[textBoxRaDecRa] = new Tuple<double, string>(telescopeDigest.Target.RaDec_RA, "RighAscension");
                targetTextBox[textBoxRaDecDec] = new Tuple<double, string>(telescopeDigest.Target.RaDec_Dec, "Declination");
                targetTextBox[textBoxHaDecHa] = new Tuple<double, string>(telescopeDigest.Target.HaDec_HA, "HourAngle");
                targetTextBox[textBoxHaDecDec] = new Tuple<double, string>(telescopeDigest.Target.HaDec_Dec, "Declination");
                targetTextBox[textBoxAltAzAlt] = new Tuple<double, string>(telescopeDigest.Target.Alt, "Altitude");
                targetTextBox[textBoxAltAzAz] = new Tuple<double, string>(telescopeDigest.Target.Az, "Azimuth");

                foreach (var tb in targetTextBox.Keys)
                {
                    coord = targetTextBox[tb].Item1;
                    coordName = targetTextBox[tb].Item2;

                    if (telescopeDigest.Slewing)
                    {
                        if (coord == Const.noTarget)
                        {
                            tb.Text = "";
                            toolTip.SetToolTip(tb, $"Target {coordName} either not set or already reached");
                        }
                        else
                        {
                            if (tb == textBoxRaDecRa)
                                tb.Text = Angle.RaFromHours(coord).ToNiceString();
                            else if (tb == textBoxRaDecDec)
                                tb.Text = Angle.DecFromDegrees(telescopeDigest.Target.RaDec_Dec).ToNiceString();
                            else if (tb == textBoxHaDecHa)
                                tb.Text = Angle.FromHours(telescopeDigest.Target.HaDec_HA).ToNiceString();
                            else if (tb == textBoxHaDecDec)
                                tb.Text = Angle.DecFromDegrees(telescopeDigest.Target.HaDec_Dec).ToNiceString();
                            else if (tb == textBoxAltAzAlt)
                                tb.Text = Angle.AltFromDegrees(telescopeDigest.Target.Alt).ToNiceString();
                            else if (tb == textBoxAltAzAz)
                                tb.Text = Angle.FromDegrees(telescopeDigest.Target.Az).ToNiceString();

                            toolTip.SetToolTip(tb, $"Current target {coordName}");
                            targetIsActive[tb] = true;
                        }
                    }
                    else
                    {
                        if (targetIsActive[tb])
                        {
                            tb.Text = "";
                            targetIsActive[tb] = false;
                        }
                    }
                }
            }
            #endregion

            #region Telescope
            if (telescopeDigest != null)
            {
                buttonTelescopePark.Text = telescopeDigest.AtPark ? "Unpark" : "Park";

                annunciatorTrack.Cadence = telescopeDigest.Tracking ? CadencePattern.SteadyOn : CadencePattern.SteadyOff;
                toolTip.SetToolTip(annunciatorTrack, telescopeDigest.Tips.Tracking);

                annunciatorSlew.Cadence = telescopeDigest.Slewing ? CadencePattern.SteadyOn : CadencePattern.SteadyOff;
                toolTip.SetToolTip(annunciatorSlew, telescopeDigest.Tips.Slewing);

                annunciatorPulse.Cadence = telescopeDigest.PulseGuiding ? CadencePattern.SteadyOn : CadencePattern.SteadyOff;
                toolTip.SetToolTip(annunciatorPulse, telescopeDigest.Tips.PulseGuiding);

                double primaryRate = Const.rateStopped;
                double secondaryRate = Const.rateStopped;

                if (telescopeDigest.PrimaryPins.GuidePin)
                    primaryRate = Const.rateGuide;
                else if (telescopeDigest.PrimaryPins.SetPin)
                    primaryRate = telescopeDigest.SlewPin ? Const.rateSlew : Const.rateSet;

                if (telescopeDigest.SecondaryPins.GuidePin)
                    secondaryRate = Const.rateGuide;
                else if (telescopeDigest.SecondaryPins.SetPin)
                    secondaryRate = telescopeDigest.SlewPin ? Const.rateSlew : Const.rateSet;

                annunciatorRARateSlew.Cadence = annunciatorRARateSet.Cadence = annunciatorRARateGuide.Cadence = CadencePattern.SteadyOff;
                if (primaryRate == Const.rateSlew)
                    annunciatorRARateSlew.Cadence = CadencePattern.SteadyOn;
                else if (primaryRate == Const.rateSet)
                    annunciatorRARateSet.Cadence = CadencePattern.SteadyOn;
                else if (primaryRate == Const.rateGuide)
                    annunciatorRARateGuide.Cadence = CadencePattern.SteadyOn;

                annunciatorDECRateSlew.Cadence = annunciatorDECRateSet.Cadence = annunciatorDECRateGuide.Cadence = CadencePattern.SteadyOff;
                if (secondaryRate == Const.rateSlew)
                    annunciatorDECRateSlew.Cadence = CadencePattern.SteadyOn;
                else if (secondaryRate == Const.rateSet)
                    annunciatorDECRateSet.Cadence = CadencePattern.SteadyOn;
                else if (secondaryRate == Const.rateGuide)
                    annunciatorDECRateGuide.Cadence = CadencePattern.SteadyOn;
            }
            #endregion

            #region Inactivity

            if (telescopeDigest != null)
            {
                if (telescopeDigest.Active)
                {
                    if (telescopeDigest.Activities.Count == 1 && telescopeDigest.Activities[0].StartsWith("GoingIdle"))
                    {
                        TimeSpan ts = TimeSpan.FromSeconds(telescopeDigest.SecondsTillIdle);

                        string s = "Idle in ";
                        if (ts.Minutes > 0)
                            s += $"{ts.Minutes:D2}m";
                        s += $"{ts.Seconds:D2}s";

                        labelCountdown.Text = s;
                        toolTip.SetToolTip(labelCountdown, "Time to Wise40 idle.");
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
                    toolTip.SetToolTip(labelCountdown, "Wise40 is idle");
                }
            }

            #endregion

            #region Moon
            labelMoonIllum.Text = (Moon.Instance.Illumination * 100).ToString("F0") + "%";
            labelMoonIllum.ForeColor = safeColor;
            toolTip.SetToolTip(labelMoonIllum, "Calculated Moon illumination");

            if (telescopeRa != null && telescopeDec != null)
            {
                try
                {
                    Angle distance = Moon.Instance.Distance(telescopeRa.Radians, telescopeDec.Radians);
                    Angle dist = Angle.FromDegrees(distance.Degrees, Angle.AngleType.Az);

                    labelMoonDist.Text = dist.ToShortNiceString();
                    labelMoonDist.ForeColor = safeColor;
                    toolTip.SetToolTip(labelMoonDist, "Calculated angular distance between the telescope and the Moon");
                }
                catch (InvalidOperationException)
                {
                    labelMoonDist.Text = "unknown";
                    labelMoonDist.ForeColor = warningColor;
                    toolTip.SetToolTip(labelMoonDist, "Moon distance could not be calculated!");
                }
            }
            #endregion

            #region Air Mass
            if (telescopeDigest != null)
            {
                Angle alt = Angle.AltFromDegrees(telescopeDigest.Current.Altitude);
                labelAirMass.Text = WiseSite.AirMass(alt.Radians).ToString("g4");
                labelAirMass.ForeColor = safeColor;

                telescopeStatus.Show(telescopeDigest.Status);
            }
            else
            {
                labelAirMass.Text = Const.noValue;
                labelAirMass.ForeColor = warningColor;
            }
            #endregion

            #region Safety Bypass
            if (telescopeDigest != null)
            {
                if (telescopeDigest.ShuttingDown)
                {
                    bypassSafetyToolStripMenuItem.Enabled = false;
                    bypassSafetyToolStripMenuItem.ToolTipText = Const.UnsafeReasons.ShuttingDown;
                }
                else
                {
                    bypassSafetyToolStripMenuItem.Enabled = true;
                    bypassSafetyToolStripMenuItem.ToolTipText = "";
                }
            }
            else
                bypassSafetyToolStripMenuItem.Enabled = false;
            #endregion

            #region RefreshAnnunciators
            if (safetooperateDigest != null)
            {
                #region ComputerControl Annunciator
                if (safetooperateDigest.ComputerControl.Safe)
                {
                    annunciatorComputerControl.Text = "Computer has control";
                    annunciatorComputerControl.Cadence = CadencePattern.SteadyOff;
                    tip = "The computer control switch is ON";

                    foreach (Control c in ActiveControls[opMode])
                    {
                        c.Enabled = true;
                    }
                }
                else
                {
                    annunciatorComputerControl.Text = "No computer control";
                    annunciatorComputerControl.Cadence = CadencePattern.SteadyOn;
                    tip = "The computer control switch is OFF!";

                    foreach (Control c in ActiveControls[opMode])
                        c.Enabled = false;
                }
                toolTip.SetToolTip(annunciatorComputerControl, tip);
                #endregion

                #region SafeToOperate Annunciator
                UpdateSafeToOperateControls();
                #endregion

                #region Platform Annunciator
                if (safetooperateDigest.Platform.Safe)
                {
                    annunciatorDomePlatform.Text = "Platform is safe";
                    annunciatorDomePlatform.Cadence = CadencePattern.SteadyOff;
                    tip = "Dome platform is at its lowest position.";
                }
                else
                {
                    annunciatorDomePlatform.Text = "Platform is NOT SAFE";
                    annunciatorDomePlatform.Cadence = CadencePattern.SteadyOn;
                    tip = "Dome platform is NOT at its lowest position!";
                }
                toolTip.SetToolTip(annunciatorDomePlatform, tip);
                #endregion
            }
            #region Simulation Annunciator
            if (WiseObject.Simulated)
            {
                annunciatorSimulation.Text = "SIMULATED HARDWARE";
                annunciatorSimulation.Cadence = CadencePattern.SteadyOn;
                tip = "Hardware access is simulated by software";
            }
            else
            {
                annunciatorSimulation.Text = "";
                annunciatorSimulation.Cadence = CadencePattern.SteadyOff;
                tip = "";
            }
            toolTip.SetToolTip(annunciatorSimulation, tip);
            #endregion
            #endregion

            #region Dome
            if (domeDigest != null)
            {
                double azimuth = domeDigest.Azimuth;

                if (azimuth == Double.NaN)
                {
                    toolTip.SetToolTip(labelDomeAzimuthValue, ASCOM.Wise40.Hardware.Hardware.MaintenanceMode ?
                        "Dome cannot be calibrated due to MAINTENENCE MODE!" :
                        "Dome azimuth cannot be calculated!");
                }
                else
                    toolTip.SetToolTip(labelDomeAzimuthValue, "");

                labelDomeAzimuthValue.Text = Angle.AzFromDegrees(azimuth).ToShortNiceString();
                domeStatus.Show(domeDigest.Status);
                buttonDomePark.Text = domeDigest.AtPark ? "Unpark" : "Park";
                buttonVent.Text = domeDigest.Vent ? "Close Vent" : "Open Vent";
                buttonProjector.Text = domeDigest.Projector ? "Turn projector Off" : "Turn projector On";

                annunciatorDome.Cadence = domeDigest.DirectionMotorsAreActive ?
                    CadencePattern.SteadyOn :
                    CadencePattern.SteadyOff;
                toolTip.SetToolTip(annunciatorDome, domeDigest.Tip);

                #region Shutter
                string status = domeDigest.Shutter.Status, msg;
                severity = Statuser.Severity.Normal;
                if (status.Contains("error:") || domeDigest.Shutter.State == ShutterState.shutterError)
                    severity = Statuser.Severity.Error;
                msg = "Shutter is " + status;
                shutterStatus.Show(msg, 0, severity);
                shutterStatus.SetToolTip(domeDigest.Shutter.Reason);

                switch (domeDigest.Shutter.State)
                {
                    case ShutterState.shutterOpening:
                        annunciatorShutter.Text = "SHUTTER(<->)";
                        annunciatorShutter.Cadence = CadencePattern.SteadyOn;
                        break;

                    case ShutterState.shutterClosing:
                        annunciatorShutter.Text = "SHUTTER(>-<)";
                        annunciatorShutter.Cadence = CadencePattern.SteadyOn;
                        break;

                    default:
                        annunciatorShutter.Text = "SHUTTER";
                        annunciatorShutter.Cadence = CadencePattern.SteadyOff;
                        break;
                }
                toolTip.SetToolTip(annunciatorShutter, domeDigest.Shutter.Tip);
            }
            #endregion

            #endregion

            #region Weather
            if (safetooperateDigest == null)
            {
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
                    label.Text = Const.noValue;
                    label.ForeColor = warningColor;
                }
            }
            else
            {
                try
                {
                    #region Observing Conditions from SafeToOperate

                    RefreshInConditionsformation(labelDewPointValue, safetooperateDigest.DewPoint);
                    RefreshInConditionsformation(labelSkyTempValue, safetooperateDigest.SkyTemperature);
                    RefreshInConditionsformation(labelTempValue, safetooperateDigest.Temperature);
                    RefreshInConditionsformation(labelPressureValue, safetooperateDigest.Pressure);
                    RefreshInConditionsformation(labelWindDirValue, safetooperateDigest.WindDirection);
                    RefreshInConditionsformation(labelWindSpeedValue, safetooperateDigest.WindSpeed);
                    RefreshInConditionsformation(labelHumidityValue, safetooperateDigest.Humidity);
                    RefreshInConditionsformation(labelCloudCoverValue, safetooperateDigest.CloudCover);
                    RefreshInConditionsformation(labelRainRateValue, safetooperateDigest.RainRate);
                    RefreshInConditionsformation(labelSunElevationValue, safetooperateDigest.SunElevation);

                    #endregion

                    #region BypassSafetyMenuItem
                    bypassSafetyToolStripMenuItem.Visible = true;
                    bool bypassedAtServer = safetooperateDigest.Bypassed;
                    if (bypassSafetyPending)
                    {
                        if (locallyBypassed == bypassedAtServer)
                        {
                            bypassSafetyPending = false;
                            UpdateCheckmark(bypassSafetyToolStripMenuItem, locallyBypassed);
                        }
                    }
                    else
                    {
                        UpdateCheckmark(bypassSafetyToolStripMenuItem, safetooperateDigest.Bypassed);
                    }
                    #endregion
                }
                catch (ASCOM.PropertyNotImplementedException ex)
                {
                    this.safetooperateStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
                }
            }
            #endregion

            #region Focuser
            if (focuserDigest != null)
            {
                labelFocusCurrentValue.Text = focuserDigest.Position.ToString();
                focuserStatus.Show(focuserDigest.StatusString);
                annunciatorFocus.Cadence = focuserDigest.IsMoving ?
                    CadencePattern.SteadyOn :
                    CadencePattern.SteadyOff;
            }
            #endregion

            #region FilterWheel
            if (filterWheelDigest?.Enabled == true &&
                    (filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Communicating ||
                    filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Moving))
            {
                annunciatorFilterWheel.Cadence = CadencePattern.SteadyOn;
            }
            else
            {
                annunciatorFilterWheel.Cadence = CadencePattern.SteadyOff;
            }
            LoadFilterWheelInformation();
            #endregion

            #region Forecast
            if (forecast != null)
                dashStatus.Show("Forecast: " + forecast);
            #endregion

            timerRefreshDisplay.Enabled = true;
        }
        #endregion

        private void UpdateSafeToOperateControls()
        {
            string tip, text;
            Statuser.Severity severity;

            if (HumanIntervention.IsSet())
            {
                text = "Human Intervention";
                annunciatorSafeToOperate.ActiveColor = unsafeColor;
                annunciatorSafeToOperate.Cadence = CadencePattern.SteadyOn;
                severity = Statuser.Severity.Error;
                tip = String.Join("\n", HumanIntervention.Details).Replace(Const.recordSeparator, "\n  ");
            }
            else if (safetooperateDigest.Bypassed)
            {
                text = "Safety bypassed";
                annunciatorSafeToOperate.ActiveColor = warningColor;
                annunciatorSafeToOperate.Cadence = CadencePattern.SteadyOn;
                severity = Statuser.Severity.Warning;
                tip = "Safety checks are bypassed!";
            }
            else if (safetooperateDigest.Safe)
            {
                text = "Safe to operate";
                annunciatorSafeToOperate.ActiveColor = goodColor;
                annunciatorSafeToOperate.Cadence = CadencePattern.SteadyOff;
                severity = Statuser.Severity.Good;
                tip = "Conditions are safe to operate.";
            }
            else
            {
                text = "Not safe to operate";
                annunciatorSafeToOperate.ActiveColor = unsafeColor;
                annunciatorSafeToOperate.Cadence = CadencePattern.SteadyOn;
                severity = Statuser.Severity.Error;
                tip = string.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n");
            }

            annunciatorSafeToOperate.Text = text;
            toolTip.SetToolTip(annunciatorSafeToOperate, tip);
            toolTip.SetToolTip(safetooperateStatus.Label, tip);
            safetooperateStatus.Show(text, 0, severity, true);
        }

        private void RefreshInConditionsformation(Label label, Sensor.SensorDigest digest)
        {
            label.Text = digest.Symbolic;
            label.ForeColor = digest.Color;

            string tip;
            if (! string.IsNullOrEmpty(digest.ToolTip))
                tip = digest.ToolTip;
            else
                tip = $"latest reading {DateTime.Now.Subtract(digest.LatestReading.timeOfLastUpdate).TotalSeconds:f1} seconds ago";
            
            if (digest.Stale && !tip.Contains("stale"))
                tip += !string.IsNullOrEmpty(tip) ? " (stale)" : "Stale";

            if (!tip.StartsWith(digest.Name + " - "))
                tip = digest.Name + " - " + tip;

            toolTip.SetToolTip(label, tip);
        }

        #region MainMenu
        private void digitalIOCardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new HardwareForm(wiseTelescope)
            {
                Visible = true,
            }.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutForm(this)
            {
                Visible = true,
            }.Show();
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
            return string.IsNullOrEmpty(telescopeDigest.SafeAtCurrentCoordinates);
        }

        #region TelescopeControl
        public void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!safetooperateDigest.ComputerControl.Safe)
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
                string message = $"Moving {String.Join("-", Directions.ToArray())} " +
                    $"at {WiseTele.RateName(handpadRate).Remove(0, 4)} (safety bypassed)";

                telescopeStatus.Show(message, 0, Statuser.Severity.Good);

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
            else if (SafeAtCurrentCoords() ||
                            Convert.ToBoolean(wiseTelescope.Action("safe-to-move", JsonConvert.SerializeObject(whichWay))))
            {
                string message = $"Moving {String.Join("-", Directions.ToArray())} " +
                    $"at {WiseTele.RateName(handpadRate).Remove(0, 4)}";
                telescopeStatus.Show(message, 0, Statuser.Severity.Good);

                foreach (var m in movements)
                {
                    try
                    {
                        wiseTelescope.Action("handpad-move-axis",
                                                        JsonConvert.SerializeObject(new HandpadMoveAxisParameter
                                                        {
                                                            axis = m._axis,
                                                            rate = m._rate
                                                        }
                                                        ));
                    }
                    catch (Exception ex)
                    {
                        telescopeStatus.Show("Not safe to move", 2000, Statuser.Severity.Error);
                        toolTip.SetToolTip(telescopeStatus.Label, ex.Message.Replace(',', '\n'));
                        break;
                    }
                }
            }
            else
            {
                string message = $"Unsafe to move {String.Join("-", Directions.ToArray())}";
                telescopeStatus.Show(message, 2000, Statuser.Severity.Error);
            }
        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            wiseTelescope.Action("handpad-stop", "");
        }

        private void buttonGoCoord_Click(object sender, EventArgs e)
        {
            switch (tabControlGoTo.SelectedTab.Name)
            {
                case "tabPageRaDec":
                    goToMode = GoToMode.RaDec;
                    break;
                case "tabPageHaDec":
                    goToMode = GoToMode.HaDec;
                    break;
                case "tabPageAltAz":
                    goToMode = GoToMode.AltAz;
                    break;
            }

            if (goToMode == GoToMode.RaDec && !telescopeDigest.Tracking)
            {
                telescopeStatus.Show("Telescope is NOT tracking!", 1000, Statuser.Severity.Error);
                return;
            }

            if ((goToMode == GoToMode.HaDec || goToMode == GoToMode.AltAz) && telescopeDigest.Tracking)
            {
                telescopeStatus.Show("Telescope is TRACKING!", 1000, Statuser.Severity.Error);
                return;
            }

            try
            {
                double ra, ha, dec, alt, az;

                switch (goToMode)
                {
                    case GoToMode.RaDec:
                        ra = ascomutil.HMSToHours(textBoxRaDecRa.Text);
                        dec = ascomutil.DMSToDegrees(textBoxRaDecDec.Text);
                        telescopeStatus.Show("Slewing to " +
                                $"ra: {Angle.RaFromHours(ra).ToNiceString()} " +
                                $"dec: {Angle.DecFromDegrees(dec).ToNiceString()}",
                            0, Statuser.Severity.Good);
                        wiseTelescope.SlewToCoordinatesAsync(ra, dec);
                        break;

                    case GoToMode.HaDec:
                        ha = ascomutil.HMSToHours(textBoxHaDecHa.Text);
                        dec = ascomutil.DMSToDegrees(textBoxHaDecDec.Text);
                        telescopeStatus.Show("Slewing to " +
                                $"ha: {Angle.HaFromHours(ha).ToNiceString()} " +
                                $"dec: {Angle.DecFromDegrees(dec).ToNiceString()}",
                            0, Statuser.Severity.Good);
                        wiseTelescope.Action("slew-to-ha-dec", $"HourAngle={ha},Declination={dec}");
                        break;

                    case GoToMode.AltAz:
                        alt = ascomutil.DMSToDegrees(textBoxAltAzAlt.Text);
                        az = ascomutil.DMSToDegrees(textBoxAltAzAz.Text);
                        telescopeStatus.Show("Slewing to " +
                                $"alt: {Angle.AltFromDegrees(alt).ToNiceString()} " +
                                $"az: {Angle.AzFromDegrees(az).ToNiceString()}",
                            0, Statuser.Severity.Good);
                        wiseTelescope.SlewToAltAzAsync(az, alt);
                        break;
                }
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

        public void coordBox_MouseDoubleClick(object sender, EventArgs e)
        {
            TextBox tb;

            switch (((TextBox) sender).Name)
            {
                case "textBoxRaDecRa":
                case "textBoxRaDecDec":
                    tb = Controls.Find("textBoxRaDecRa", true)[0] as TextBox;
                    tb.Text = Angle.RaFromHours(telescopeDigest.Current.RightAscension).ToString().
                        Replace('h', ':').Replace('m', ':').Replace('s', ' ');

                    tb = Controls.Find("textBoxRaDecDec", true)[0] as TextBox;
                    tb.Text = Angle.DecFromDegrees(telescopeDigest.Current.Declination).ToString();
                    break;

                case "textBoxHaDecHa":
                case "textBoxHaDecDec":
                    Angle ha = wisesite.LocalSiderealTime - Angle.HaFromHours(telescopeDigest.Current.RightAscension);
                    tb = Controls.Find("textBoxHaDecHa", true)[0] as TextBox;
                    tb.Text = ha.ToString().Replace('h', ':').Replace('m', ':').Replace('s', ' ');

                    tb = Controls.Find("textBoxHaDecDec", true)[0] as TextBox;
                    tb.Text = Angle.DecFromDegrees(telescopeDigest.Current.Declination).ToString();
                    break;

                case "textBoxAltAzAlt":
                case "textBoxAltAzAz":
                    tb = Controls.Find("textBoxAltAzAlt", true)[0] as TextBox;
                    tb.Text = Angle.AltFromDegrees(telescopeDigest.Current.Altitude).ToString();

                    tb = Controls.Find("textBoxAltAzAz", true)[0] as TextBox;
                    tb.Text = Angle.AzFromDegrees(telescopeDigest.Current.Azimuth).ToString();
                    break;
            }
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

        private void StartMovingShutter(bool open)
        {
            try
            {
                if (open)
                    wiseDome.OpenShutter();
                else
                    wiseDome.CloseShutter();
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
                    StartMovingShutter(true);
                }
                else if (button == buttonFullCloseShutter)
                {
                    shutterStatus.Show("Started closing shutter", 1000, Statuser.Severity.Good);
                    StartMovingShutter(false);
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
                    StartMovingShutter(true);
                }
                else if (button == buttonCloseShutter)
                {
                    shutterStatus.Show("Closing shutter", 0, Statuser.Severity.Good);
                    StartMovingShutter(false);
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
                wiseDome.Action("shutter", "halt");
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
                wiseDome.Action("shutter", "halt");
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
            if (string.IsNullOrEmpty(tb.Text))
                return;
            double az = Convert.ToDouble(tb.Text);

            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show($"Invalid azimuth: {tb.Text}", 2000, Statuser.Severity.Error);
                tb.Text = "";
            }
        }
        private void buttonDomeAzSet_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxDomeAzValue.Text))
                return;

            double az = Convert.ToDouble(textBoxDomeAzValue.Text);
            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show($"Invalid azimuth: {textBoxDomeAzValue.Text}", 2000, Statuser.Severity.Error);
                textBoxDomeAzValue.Text = "";
            }

            wiseDome.Action("set-azimuth", az.ToString());
        }

        private void buttonDomeAzGo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxDomeAzValue.Text))
                return;

            double az = Convert.ToDouble(textBoxDomeAzValue.Text);
            if (az < 0.0 || az >= 360.0)
            {
                domeStatus.Show($"Invalid azimuth: {textBoxDomeAzValue.Text}", 2000, Statuser.Severity.Error);
                textBoxDomeAzValue.Text = "";
                return;
            }

            try
            {
                wiseDome.SlewToAzimuth(az);
                domeStatus.Show($"Slewing to {Angle.AzFromDegrees(az).ToNiceString()}", 0, Statuser.Severity.Normal);
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
            catch (Exception ex)
            {
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
            if (!wiseSafeToOperate.IsSafe)
            {
                domeStatus.Show("Not safe to move!", 3000, Statuser.Severity.Error);
                return;
            }
            wiseDome.Action("calibrate", "");
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
            if (string.IsNullOrEmpty(textBoxFocusGotoPosition.Text))
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

            if (string.IsNullOrEmpty(box.Text))
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
            Debugger.DebugLevel current = Debugger.DebugLevel.DebugAll;
            Debugger.SetCurrentLevel(current);

            foreach (ASCOM.DriverAccess.AscomDriver driver in drivers)
                driver.Action("debug", $"{current}");

            foreach (var level in debugMenuItemDict.Keys)
                UpdateCheckmark(debugMenuItemDict[level], true);
        }

        private void debugNoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debugger.DebugLevel current = Debugger.DebugLevel.DebugNone;
            Debugger.SetCurrentLevel(current);

            foreach (ASCOM.DriverAccess.AscomDriver driver in drivers)
                driver.Action("debug", $"{current}");

            foreach (var level in debugMenuItemDict.Keys)
                UpdateCheckmark(debugMenuItemDict[level], false);
        }

        private void buttonZenith_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to Zenith", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "zenith");
        }

        private void buttonHandleCover_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to cover station", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "cover");
        }

        private void checkBoxTrack_Click(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;

            wiseTelescope.Tracking = box.Checked;
        }

        private void wise40WikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/blumzi/ASCOM.Wise40/wiki");
        }

        private void debugSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            Debugger.DebugLevel selectedLevel = Debugger.DebugLevel.DebugNone;
            Debugger.DebugLevel current;

            try
            {
                Enum.TryParse<Debugger.DebugLevel>(wiseTelescope.Action("debug", ""), out current);
            }
            catch
            {
                return;
            }

            foreach (var level in debugMenuItemDict.Keys)
            {
                if (debugMenuItemDict[level] == item)
                {
                    selectedLevel = level;
                    break;
                }
            }

            if (selectedLevel == Debugger.DebugLevel.DebugNone)
                return;

            current ^= selectedLevel;
            foreach (ASCOM.DriverAccess.AscomDriver driver in drivers)
                driver.Action("debug", $"{current}");

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"New debug level: {current}");
            #endregion

            UpdateDebuggingCheckmarks();
        }

        private void DashOutFilterWheelControls()
        {
            foreach (Label l in new List<Label> { labelFWWheel, labelFWPosition, labelFWFilter}){
                l.Text = Const.noValue;
                l.ForeColor = warningColor;
            }
        }

        private void LoadFilterWheelInformation()
        {
            if (!WiseSite.FilterWheelInUse)
            {
                DashOutFilterWheelControls();
                filterWheelStatus.Show($"Not available in {WiseSite.OperationalMode} mode!");
                return;
            }
            else if (filterWheelDigest == null)
            {
                DashOutFilterWheelControls();
                return;
            }
            else if (!filterWheelDigest.Enabled)
            {
                DashOutFilterWheelControls();
                filterWheelStatus.Show("Disabled (see Settings->FilterWheel->Settings)");
                return;
            }
            else if (filterWheelDigest.Wheel == null)
            {
                DashOutFilterWheelControls();
                filterWheelStatus.Show("Cannot detect a filter wheel", 5000, Statuser.Severity.Error);

                string arduinoStatus = filterWheelDigest.Arduino.StatusString;
                Statuser.Severity severity = Statuser.Severity.Normal;

                if (filterWheelDigest.Arduino.Error != null)
                {
                    severity = Statuser.Severity.Error;
                }
                filterWheelArduinoStatus.Show("Arduino: " + arduinoStatus, 5000, severity);
                return;
            }

            short position = (short)filterWheelDigest.Wheel.CurrentPosition.Position;

            labelFWWheel.ForeColor = labelFWPosition.ForeColor = labelFWFilter.ForeColor = safeColor;
            labelFWWheel.Text = (filterWheelDigest.Wheel.Type == WiseFilterWheel.WheelType.Wheel4) ?
                "4 positions, 3 inch filters" :
                "8 positions, 2 inch filters";
            labelFWPosition.Text = (position + 1).ToString();

            WiseFilterWheel.Wheel.PositionDigest currentFilter = filterWheelDigest.Wheel.Filters[position];
            if (string.IsNullOrEmpty(currentFilter.Name))
            {
                labelFWFilter.Text = "Clear";
                toolTip.SetToolTip(labelFWFilter, "");
            }
            else
            {
                labelFWFilter.Text = $"{currentFilter.Name}";
                if (!string.IsNullOrEmpty(currentFilter.Description))
                    labelFWFilter.Text += $"({currentFilter.Description})";
                toolTip.SetToolTip(labelFWFilter,
                    " Name:    " + currentFilter.Name + Const.crnl +
                    " Desc:    " + (string.IsNullOrEmpty(currentFilter.Description) ? "(no description)" : currentFilter.Description) + Const.crnl +
                    " Offset:  " + currentFilter.Offset.ToString() + Const.crnl +
                    " Comment: " + (string.IsNullOrEmpty(currentFilter.Comment) ? "(no comment)" : currentFilter.Comment));
            }

            if (filterWheelDigest.Wheel.Filters.Count() != comboBoxFilterWheelPositions.Items.Count)
            {
                comboBoxFilterWheelPositions.Items.Clear();
                for (int pos = 0; pos < filterWheelDigest.Wheel.Filters.Count(); pos++)
                    comboBoxFilterWheelPositions.Items.Add($"{pos + 1} - Clear");
            }

            for (int pos = 0; pos < filterWheelDigest.Wheel.Filters.Count(); pos++)
            {
                if (!string.IsNullOrEmpty(filterWheelDigest.Wheel.Filters[pos].Name))
                {
                    comboBoxFilterWheelPositions.Items[pos] =
                        $"{pos + 1} - {filterWheelDigest.Wheel.Filters[pos].Name}: {filterWheelDigest.Wheel.Filters[pos].Description}";
                }
            }

            filterWheelStatus.Show((filterWheelDigest.Status != null && filterWheelDigest.Status != "Idle") ? filterWheelDigest.Status : "");
            if (filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Idle)
                filterWheelArduinoStatus.Show("");

            if (!string.IsNullOrEmpty(filterWheelDigest.Arduino.Error))
                filterWheelArduinoStatus.Show("Arduino: " + filterWheelDigest.Arduino.Error, 5000, Statuser.Severity.Error);
            else if (!string.IsNullOrEmpty(filterWheelDigest.Arduino.StatusString))
                filterWheelArduinoStatus.Show("Arduino: " + filterWheelDigest.Arduino.StatusString);

            string tip = "Arduino:\r\n";
            TimeSpan ts = DateTime.Now.Subtract(Convert.ToDateTime(filterWheelDigest.LastDataReceived));
            tip += $"  Age:  {ts:s\\.ff} seconds\r\n";
            string err = !string.IsNullOrEmpty(filterWheelDigest.Arduino.Error) ?
                filterWheelDigest.Arduino.Error : "none";
            tip += $"  Status:  {filterWheelDigest.Arduino.StatusString}\r\n";
            if (filterWheelDigest.Arduino.LastCommand != null)
                tip += $"  Last cmd: {filterWheelDigest.Arduino.LastCommand}\r\n";
            tip += $"  Error:   {err}\r\n";
            toolTip.SetToolTip(filterWheelArduinoStatus.Label, tip);
        }

        private void toolStripMenuItemSafeToOperate_Click(object sender, EventArgs e)
        {
            new SafeToOperateSetupDialogForm().Show();
        }

        private void toolStripMenuItemFilterWheel_Click(object sender, EventArgs e)
        {
            new FilterWheelSetupDialogForm().Show();
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
            int targetPosition = comboBoxFilterWheelPositions.SelectedIndex;

            filterWheelStatus.Show($"Moving to position {targetPosition + 1}", 5000, Statuser.Severity.Good);
            wiseFilterWheel.Position = (short)targetPosition;
        }

        private void manage2InchFilterInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FiltersForm(wiseFilterWheel, WiseFilterWheel.FilterSize.TwoInch).Show();
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
            new FiltersForm(wiseFilterWheel, WiseFilterWheel.FilterSize.ThreeInch).Show();
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

        private void groupBoxDomeGroup_Enter(object sender, EventArgs e)
        {}

        private void UpdateHumanInterventionControls()
        {
            if (HumanIntervention.IsSet())
            {
                buttonHumanIntervention.Text = "Deactivate";
                labelHumanInterventionStatus.Text = "Active";
                labelHumanInterventionStatus.ForeColor = unsafeColor;
                toolTip.SetToolTip(labelHumanInterventionStatus,
                    String.Join("\n", HumanIntervention.Details).Replace(Const.recordSeparator, "\n  "));
            }
            else
            {
                buttonHumanIntervention.Text = "Activate";
                labelHumanInterventionStatus.Text = "Inactive";
                labelHumanInterventionStatus.ForeColor = goodColor;
                toolTip.SetToolTip(labelHumanInterventionStatus, "");
            }
        }

        private void buttonHumanIntervention_Click(object sender, EventArgs e)
        {
            if (HumanIntervention.IsSet())
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += RemoveHumanInterventionFile;
                bw.RunWorkerCompleted += AfterRemoveHumanInterventionFile;
                bw.RunWorkerAsync();
            }
            else
            {
                DialogResult result = new InterventionForm().ShowDialog();
                if (result == DialogResult.OK)
                    dashStatus.Show("Created human intervention");
            }
            UpdateHumanInterventionControls();

            try
            {
                safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wiseSafeToOperate.Action("status", ""));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at {ex.StackTrace}");
                #endregion
                safetooperateStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
            }
            UpdateSafeToOperateControls();
        }

        private void RemoveHumanInterventionFile(object sender, DoWorkEventArgs e)
        {
            HumanIntervention.Remove();
        }

        private void AfterRemoveHumanInterventionFile(object sender, RunWorkerCompletedEventArgs e)
        {
            dashStatus.Show("Removed human intervention.");
        }

        private void ChangeOperationalMode(object sender, EventArgs e)
        {
            WiseSite.OpMode newMode = WiseSite.OpMode.NONE;
            WiseSite.OpMode currentMode = WiseSite.OperationalMode;

            if (sender == LCOToolStripMenuItem)
                newMode = WiseSite.OpMode.LCO;
            else if (sender == ACPToolStripMenuItem)
                newMode = WiseSite.OpMode.ACP;
            else if (sender == WISEToolStripMenuItem)
                newMode = WiseSite.OpMode.WISE;

            if (newMode == WiseSite.OpMode.NONE || newMode == currentMode)
                return;

            DialogResult result = MessageBox.Show(
                "The Wise40 service must be restarted in order to change\n" +
                $"     the operational mode from {currentMode} to {newMode}.\n\n" +
                "  Are you sure?",
                "Wise40 Operational Mode Change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
            {
                WiseSite.OperationalMode = newMode;
                ChangeWise40Service("Restart");
            }
        }

        private void debugDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debugger.DebugLevel current = Debugger.DebugLevel.DebugDefault;
            Debugger.SetCurrentLevel(current);
            foreach (ASCOM.DriverAccess.AscomDriver driver in drivers)
                driver.Action("debug", $"{current}");

            foreach (var level in debugMenuItemDict.Keys)
                UpdateCheckmark(debugMenuItemDict[level], (current & level) != 0);
        }

        private void saveDebugToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Debugger.WriteProfile();
        }

        private void RestartWise40(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "The Wise40 service will be restarted.\n\n" +
                "  Are you sure?",
                "Restart Wise40 Service",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
                ChangeWise40Service("Restart");
        }

        private void renishawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RenishawForm(wiseTelescope).Show();
        }

        private string EnableDisableLCONetwork()
        {
            bool shouldBeEnabled = WiseSite.OperationalMode == WiseSite.OpMode.LCO;

            string output;
            string interfaceName = "LCO Ethernet";

            using (var p = new System.Diagnostics.Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = "netsh.exe";
                p.StartInfo.Arguments = $"interface show interface name=\"{interfaceName}\"";
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            bool isEnabled = output.Contains("Enabled");
            if (isEnabled == shouldBeEnabled)
                return null;

            return  $"netsh interface set interface name='{interfaceName}' admin=" +
                    (shouldBeEnabled ? "enable" : "disable") +
                    "; ";
        }

        void ChangeWise40Service(string verb)
        {
            string command =
                $"-WindowStyle Hidden -Command \"{EnableDisableLCONetwork()}{verb}-Service -Name Wise40Watcher\"";

            try
            {
                System.Diagnostics.ProcessStartInfo si = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = command,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas",
                };

                System.Diagnostics.Process proc = new System.Diagnostics.Process()
                {
                    EnableRaisingEvents = true,
                    StartInfo = si,
                };
                proc.Start();

                proc.WaitForExit();
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"powershell({command}), exitCode: {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"powershell({command}), caught: {ex.Message}");
            }
        }

        private void StopWise40(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "The Wise40 service will be stopped.\n\n" +
                "  Are you sure?",
                "Stop Wise40 Service",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
                ChangeWise40Service("Stop");
        }

        private void groupBoxFilterWheel_Enter(object sender, EventArgs e)
        {}

        private void davisVantagePro2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.VantagePro.SetupDialogForm().Show();
        }

        private void boltwoodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ASCOM.Wise40.Boltwood.SetupDialogForm().Show();
        }

        public static void UpdateCheckmark(ToolStripMenuItem item, bool state)
        {
            if (state && !item.Text.EndsWith(Const.checkmark))
                item.Text += Const.checkmark;
            if (!state && item.Text.EndsWith(Const.checkmark))
                item.Text = item.Text.Remove(item.Text.Length - Const.checkmark.Length);
            item.Tag = state;
            item.Invalidate();
        }

        public static bool IsCheckmarked(ToolStripMenuItem item)
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
                msg += $" Exception -- : {e.Message}\n";
                msg += $"    Source -- : {e.Source}\n";
                msg += $"StackTrace -- : {e.StackTrace}\n";
                msg += $"TargetSite -- : {e.TargetSite}\n";
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

        private void BypassOperatingConditionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wiseSafeToOperate == null || safetooperateDigest == null)
                return;

            bool bypassed = safetooperateDigest.Bypassed;
            if (bypassed)
            {
                wiseSafeToOperate.Action("bypass", "end");
                locallyBypassed = false;
            }
            else
            {
                wiseSafeToOperate.Action("bypass", "start");
                locallyBypassed = true;
            }
            bypassSafetyPending = true;
            UpdateCheckmark(bypassSafetyToolStripMenuItem, locallyBypassed);
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

        public TimeSpan TimeToRefresh
        {
            get
            {
                return (_lastTime + _interval) - DateTime.Now;
            }
        }
    }
}
