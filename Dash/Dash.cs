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

namespace Dash
{
    public partial class FormDash : Form
    {
        public WiseSite wisesite = WiseSite.Instance;
        private readonly ASCOM.Utilities.Util ascomutil = new Util();
        public enum GoToMode { RaDec, HaDec, AltAz /*, DeltaRa, DeltaHa */};
        private GoToMode goToMode = GoToMode.RaDec;

        private DebuggingForm debuggingForm = new DebuggingForm();
        private readonly Debugger debugger = Debugger.Instance;
        private readonly FilterWheelForm filterWheelForm;

        private readonly Statuser dashStatus, telescopeStatus, domeStatus, shutterStatus, focuserStatus, safetooperateStatus, filterWheelStatus, filterWheelArduinoStatus;

        private double handpadRate = Const.rateSlew;
        private readonly bool _saveFocusUpperLimit = false, _saveFocusLowerLimit = false;

        private readonly RefreshPacer domePacer = new RefreshPacer(TimeSpan.FromSeconds(1));
        private readonly RefreshPacer safettoperatePacer = new RefreshPacer(TimeSpan.FromSeconds(20));
        private readonly RefreshPacer focusPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        private readonly RefreshPacer filterWheelPacer = new RefreshPacer(TimeSpan.FromSeconds(5));
        private readonly RefreshPacer telescopePacer = new RefreshPacer(TimeSpan.FromMilliseconds(200));
        private readonly RefreshPacer forecastPacer = new RefreshPacer(TimeSpan.FromMinutes(2));

        private SafeToOperateDigest safetooperateDigest = null;
        private DomeDigest domeDigest = null;
        private TelescopeDigest telescopeDigest = null;
        private FocuserDigest focuserDigest = null;
        private WiseFilterWheelDigest filterWheelDigest = null;
        private string forecast;

        private readonly List<ToolStripMenuItem> debugMenuItems;
        private readonly Dictionary<object, string> alteredItems = new Dictionary<object, string>();

        public Color safeColor = Statuser.colors[Statuser.Severity.Normal];
        public Color unsafeColor = Statuser.colors[Statuser.Severity.Error];
        public Color warningColor = Statuser.colors[Statuser.Severity.Warning];
        public Color goodColor = Statuser.colors[Statuser.Severity.Good];

        private readonly Moon moon = Moon.Instance;
        public ASCOM.DriverAccess.Telescope wiseTelescope;
        public ASCOM.DriverAccess.Dome wiseDome;
        public ASCOM.DriverAccess.Focuser wiseFocuser;
        public ASCOM.DriverAccess.FilterWheel wiseFilterWheel;
        public ASCOM.DriverAccess.SafetyMonitor wiseSafeToOperate;
        public ASCOM.DriverAccess.ObservingConditions wiseVantagePro, wiseBoltwood;

        private readonly int focuserMaxStep = 0;
        private readonly int focuserLowerLimit = 0;
        private readonly int focuserUpperLimit = 0;

        private static List<Control> readonlyControls;
        private static bool Readonly
        {
            get
            {
                return WiseSite.OperationalMode != WiseSite.OpMode.WISE;
            }
        }

        #region Initialization
        public FormDash()
        {
            InitializeComponent();

            wiseTelescope = new ASCOM.DriverAccess.Telescope("ASCOM.Remote1.Telescope");
            wiseDome = new ASCOM.DriverAccess.Dome("ASCOM.Remote1.Dome");
            wiseFocuser = new ASCOM.DriverAccess.Focuser("ASCOM.Remote1.Focuser");
            wiseFilterWheel = new ASCOM.DriverAccess.FilterWheel("ASCOM.Remote1.FilterWheel");
            wiseSafeToOperate = new ASCOM.DriverAccess.SafetyMonitor("ASCOM.Remote1.SafetyMonitor");
            wiseVantagePro = new ASCOM.DriverAccess.ObservingConditions(Const.WiseDriverID.VantagePro);
            wiseBoltwood = new ASCOM.DriverAccess.ObservingConditions(Const.WiseDriverID.Boltwood);

            filterWheelForm = new FilterWheelForm(wiseFilterWheel);

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

            readonlyControls = new List<Control>() {
                    textBoxRaDecRa, textBoxRaDecDec,
                    textBoxHaDecHa, textBoxHaDecDec,
                    textBoxAltAzAlt, textBoxAltAzAz,
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

                    radioButtonGuide, radioButtonSet, radioButtonSlew,
                    buttonFilterWheelGo, comboBoxFilterWheelPositions,
                };

            if (Readonly)
            {
                groupBoxTarget.Text += $"(from {WiseSite.OperationalMode}) ";

                foreach (var c in readonlyControls)
                {
                    c.Enabled = false;
                }

                annunciatorReadonly.Text = $"Readonly mode ({WiseSite.OperationalMode})";
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

            UpdateFilterWheelControls();
            //tabControlGoTo.DrawMode = TabDrawMode.OwnerDrawFixed;
            //tabControlGoTo.DrawItem += tabControlDrawItem;
            tabControlGoTo.SelectedTab = tabPageRaDec;
        }

        //private void tabControlDrawItem(object sender, DrawItemEventArgs e)
        //{
        //    TabControl tc = (TabControl)sender;

        //    Graphics g = e.Graphics;
        //    TabPage tp = tc.TabPages[e.Index];

        //    StringFormat sf = new StringFormat
        //    {
        //        Alignment = StringAlignment.Center  //optional
        //    };

        //    // This is the rectangle to draw "over" the tabpage title
        //    RectangleF headerRect = new RectangleF(e.Bounds.X, e.Bounds.Y + 2, e.Bounds.Width, e.Bounds.Height - 2);

        //    // This is the default colour to use for the non-selected tabs
        //    SolidBrush sb = new SolidBrush(Color.FromArgb(64, 64, 64));

        //    // This changes the colour if we're trying to draw the selected tabpage
        //    //if (tc.SelectedIndex == e.Index)
        //    //    sb.Color =  Color.Aqua;

        //    // Colour the header of the current tabpage based on what we did above
        //    g.FillRectangle(sb, e.Bounds);

        //    //Remember to redraw the text - I'm always using black for title text
        //    g.DrawString(tp.Text, tc.Font, new SolidBrush(Color.DarkOrange), headerRect, sf);
        //}
        #endregion

        public void UpdateFilterWheelControls()
        {
            if (WiseSite.OperationalMode == WiseSite.OpMode.LCO)
            {
                toolStripMenuItemFilterWheel.Enabled = false;
                labelFWWheel.Text = "";
                labelFWPosition.Text = "";
                filterWheelStatus.Show("Not available in LCO mode");
                return;
            }

            toolStripMenuItemFilterWheel.Enabled = true;

            if (filterWheelDigest == null)
                return;

            if (!filterWheelDigest.Enabled)
            {
                filterWheelStatus.Show("Disabled (see Settings->FilterWheel->Settings)");
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

            bool refreshDome = domePacer.ShouldRefresh(now);
            bool refreshSafeToOperate = safettoperatePacer.ShouldRefresh(now);
            bool refreshTelescope = telescopePacer.ShouldRefresh(now);
            bool refreshFocus = focusPacer.ShouldRefresh(now);
            bool refreshFilterWheel = WiseSite.OperationalMode != WiseSite.OpMode.LCO && filterWheelPacer.ShouldRefresh(now);
            bool refreshForecast = forecastPacer.ShouldRefresh(now);
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
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    domeStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }

            if (refreshSafeToOperate)
            {
                try
                {
                    safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wiseSafeToOperate.Action("status", ""));
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    safetooperateStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }

            if (refreshFocus)
            {
                try
                {
                    focuserDigest = JsonConvert.DeserializeObject<FocuserDigest>(wiseFocuser.Action("status", ""));
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    focuserStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }

            if (refreshForecast)
            {
                forecast = wiseVantagePro.Action("forecast", "");
            }

            if (refreshFilterWheel)
            {
                try
                {
                    filterWheelDigest = JsonConvert.DeserializeObject<WiseFilterWheelDigest>(wiseFilterWheel.Action("status", ""));
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshDisplay: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    filterWheelStatus.Show("ASCOM communication error", 2000, Statuser.Severity.Error);
                }
            }
            #endregion

            #region RefreshTelescope

            Angle telescopeRa = null, telescopeDec = null, telescopeHa = null;

            #region Coordinates Info
            labelDate.Text = localTime.ToString("ddd, dd MMM yyyy\n hh:mm:ss tt");
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");
            labelUTValue.Text = utcTime.TimeOfDay.ToString(@"hh\hmm\mss\.f\s");

            if (telescopeDigest != null)
            {
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

                #region Telescope Target
                if (telescopeDigest.Slewing)
                {
                    if (telescopeDigest.Target.RightAscension == Const.noTarget)
                    {
                        textBoxRaDecRa.Text = "";
                        toolTip.SetToolTip(textBoxRaDecRa, "Target RightAscension either not set or already reached");
                    }
                    else
                    {
                        textBoxRaDecRa.Text = Angle.RaFromHours(telescopeDigest.Target.RightAscension).ToNiceString();
                        toolTip.SetToolTip(textBoxRaDecRa, "Current target RightAscension");
                    }

                    if (telescopeDigest.Target.Declination == Const.noTarget)
                    {
                        textBoxRaDecDec.Text = "";
                        toolTip.SetToolTip(textBoxRaDecDec, "Target Declination either not set or already reached");
                        textBoxHaDecHa.Text = "";
                        toolTip.SetToolTip(textBoxHaDecDec, "Target Declination either not set or already reached");
                    }
                    else
                    {
                        textBoxRaDecDec.Text = Angle.DecFromDegrees(telescopeDigest.Target.Declination).ToNiceString();
                        toolTip.SetToolTip(textBoxRaDecDec, "Current target Declination");
                        textBoxHaDecDec.Text = Angle.DecFromDegrees(telescopeDigest.Target.Declination).ToNiceString();
                        toolTip.SetToolTip(textBoxHaDecDec, "Current target Declination");
                    }

                    if (telescopeDigest.Target.HourAngle == Const.noTarget)
                    {
                        textBoxHaDecHa.Text = "";
                        toolTip.SetToolTip(textBoxHaDecHa, "Target HourAngle either not set or already reached");
                    }
                    else
                    {
                        textBoxHaDecHa.Text = Angle.FromHours(telescopeDigest.Target.HourAngle).ToNiceString();
                        toolTip.SetToolTip(textBoxHaDecHa, "Current target HourAngle");
                    }

                    if (telescopeDigest.Target.Altitude == Const.noTarget)
                    {
                        textBoxAltAzAlt.Text = "";
                        toolTip.SetToolTip(textBoxAltAzAlt, "Target Altiude either not set or already reached");
                    }
                    else
                    {
                        textBoxAltAzAlt.Text = Angle.AltFromDegrees(telescopeDigest.Target.Altitude).ToNiceString();
                        toolTip.SetToolTip(textBoxAltAzAlt, "Current target Altitude");
                    }

                    if (telescopeDigest.Target.Azimuth == Const.noTarget)
                    {
                        textBoxAltAzAz.Text = "";
                        toolTip.SetToolTip(textBoxAltAzAz, "Target Azimuth either not set or already reached");
                    }
                    else
                    {
                        textBoxAltAzAz.Text = Angle.FromDegrees(telescopeDigest.Target.Azimuth).ToNiceString();
                        toolTip.SetToolTip(textBoxAltAzAz, "Current target Azimuth");
                    }
                }
                else
                {
                    textBoxRaDecRa.Text = "";
                    textBoxRaDecDec.Text = "";
                    textBoxHaDecHa.Text = "";
                    textBoxHaDecDec.Text = "";
                    textBoxAltAzAlt.Text = "";
                    textBoxAltAzAz.Text = "";
                }
                #endregion
            }

            #endregion

            #region Annunciators

            if (telescopeDigest != null)
            {
                buttonTelescopePark.Text = telescopeDigest.AtPark ? "Unpark" : "Park";

                annunciatorTrack.Cadence = telescopeDigest.Tracking ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
                annunciatorSlew.Cadence = telescopeDigest.Slewing ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;
                annunciatorPulse.Cadence = telescopeDigest.PulseGuiding ? ASCOM.Controls.CadencePattern.SteadyOn : ASCOM.Controls.CadencePattern.SteadyOff;

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
            }
            #endregion

            #region Inactivity Countdown

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
                        toolTip.SetToolTip(labelCountdown, "Time to observatory idle.");
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
            }

            #endregion

            #region Moon
            if (telescopeDigest != null)
            {
                labelMoonIllum.Text = (moon.Illumination * 100).ToString("F0") + "%";
                labelMoonDist.Text = moon.Distance(telescopeRa.Radians, telescopeDec.Radians).ToShortNiceString();
                labelMoonIllum.ForeColor = labelMoonDist.ForeColor = safeColor;
            }
            else
            {
                labelMoonIllum.Text = labelMoonDist.Text = Const.noValue;
                labelMoonIllum.ForeColor = labelMoonDist.ForeColor = warningColor;
            }
            #endregion

            #region Air Mass
            if (telescopeDigest != null)
            {
                Angle alt = Angle.AltFromDegrees(telescopeDigest.Current.Altitude);
                labelAirMass.Text = WiseSite.AirMass(alt.Radians).ToString("g4");
                labelAirMass.ForeColor = safeColor;
                #endregion

                telescopeStatus.Show(telescopeDigest.Status);
            }
            else
            {
                labelAirMass.Text = Const.noValue;
                labelAirMass.ForeColor = warningColor;
            }

            #endregion

            #region RefreshAnnunciators

            #region ComputerControl Annunciator
            if (safetooperateDigest != null)
            {
                if (safetooperateDigest.ComputerControl.Safe)
                {
                    annunciatorComputerControl.Text = "Computer has control";
                    annunciatorComputerControl.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
                    tip = "The computer control switch is ON";

                    if (WiseSite.OperationalMode == WiseSite.OpMode.WISE)
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

                if (!safetooperateDigest.HumanIntervention.Safe)
                {
                    text = "Human Intervention";
                    annunciatorSafeToOperate.ForeColor = unsafeColor;
                    annunciatorSafeToOperate.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
                    severity = Statuser.Severity.Error;
                    tip = String.Join("\n", safetooperateDigest.UnsafeReasons).Replace(Const.recordSeparator, "\n  ");
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

                if (safetooperateDigest.Platform.Safe)
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
            }
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
            if (domeDigest != null)
            {
                labelDomeAzimuthValue.Text = Angle.AzFromDegrees(domeDigest.Azimuth).ToShortNiceString();
                domeStatus.Show(domeDigest.Status);
                buttonDomePark.Text = domeDigest.AtPark ? "Unpark" : "Park";
                buttonVent.Text = domeDigest.Vent ? "Close Vent" : "Open Vent";
                buttonProjector.Text = domeDigest.Projector ? "Turn projector Off" : "Turn projector On";

                annunciatorDome.Cadence = domeDigest.DirectionMotorsAreActive ?
                    ASCOM.Controls.CadencePattern.SteadyOn :
                    ASCOM.Controls.CadencePattern.SteadyOff;

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
            }
            #endregion

            #endregion

            #region RefreshWeather
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
                }
                catch (ASCOM.PropertyNotImplementedException ex)
                {
                    this.safetooperateStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
                }
            }
            #endregion

            #region RefreshFocuser
            if (focuserDigest != null)
            {
                labelFocusCurrentValue.Text = focuserDigest.Position.ToString();
                focuserStatus.Show(focuserDigest.StatusString);
                annunciatorFocus.Cadence = focuserDigest.IsMoving ?
                    ASCOM.Controls.CadencePattern.SteadyOn :
                    ASCOM.Controls.CadencePattern.SteadyOff;
            }
            #endregion

            #region RefreshFilterWheel
            if (filterWheelDigest?.Enabled == true &&
                    (filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Communicating ||
                    filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Moving))
            {
                annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.SteadyOn;
            }
            else
            {
                annunciatorFilterWheel.Cadence = ASCOM.Controls.CadencePattern.SteadyOff;
            }
            LoadFilterWheelInformation();
            #endregion

            #region RefreshForecast
            if (forecast != null)
                dashStatus.Show("Forecast: " + forecast);
            #endregion

            timerRefreshDisplay.Enabled = true;
        }

        private void RefreshInConditionsformation(Label label, Sensor.SensorDigest digest)
        {
            label.Text = digest.Symbolic;
            label.ForeColor = digest.Color;

            string tip;
            if (digest.ToolTip != null && digest.ToolTip != string.Empty)
                tip = digest.ToolTip;
            else
                tip = $"latest reading {DateTime.Now.Subtract(digest.LatestReading.timeOfLastUpdate).TotalSeconds:f1} seconds ago";
            
            if (digest.Stale && !tip.Contains("stale"))
                tip += tip != string.Empty ? " (stale)" : "Stale";

            if (!tip.StartsWith(digest.Name + " - "))
                tip = digest.Name + " - " + tip;

            toolTip.SetToolTip(label, tip);
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
                string message = $"Unsafe to move {String.Join("-", Directions.ToArray())}";
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

        private void _startMovingShutter(bool open)
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
            if (tb.Text == string.Empty)
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
            try
            {
                wiseDome.Action("calibrate", "");
                domeStatus.Show("Started dome calibration", 1000, Statuser.Severity.Good);
            }
            catch (Exception ex)
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
            Debugger.StartDebugging(Debugger.DebugLevel.DebugAll);

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
            telescopeStatus.Show("Moving to Zenith", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "zenith");
        }

        private void buttonHandleCover_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to cover station", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "cover");
        }

        private void buttonFlat_Click(object sender, EventArgs e)
        {
            telescopeStatus.Show("Moving to Flat", 2000, Statuser.Severity.Good);
            wiseTelescope.Action("move-to-preset", "flat");
        }

        private void checkBoxTrack_Click(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;

            wiseTelescope.Tracking = box.Checked;
        }

        private void debugNoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debugger.StopDebugging(Debugger.DebugLevel.DebugAll);

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
                Debugger.StopDebugging(selectedLevel);
            else
                Debugger.StartDebugging(selectedLevel);
            UpdateCheckmark(item, debugger.Debugging(selectedLevel));
            UpdateAlteredItems(item, "Debugging");

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "New debug level: {0}", debugger.Level);
            #endregion
        }

        private void DashOutFilterWheelControls()
        {
            foreach (Label l in new List<Label> { labelFWWheel, labelFWFilterSize, labelFWPosition, labelFWFilter}){
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
                filterWheelArduinoStatus.Show(arduinoStatus, 5000, severity);
                return;
            }

            short position = (short)filterWheelDigest.Wheel.CurrentPosition.Position;

            labelFWWheel.Text = $"{filterWheelDigest.Wheel.Name}";
            labelFWFilterSize.Text = filterWheelDigest.Wheel.Type == WiseFilterWheel.WheelType.Wheel4 ? "3 inch" : "2 inch";
            labelFWPosition.Text = (position + 1).ToString();

            WiseFilterWheel.Wheel.PositionDigest currentFilter = filterWheelDigest.Wheel.Filters[position];
            if (currentFilter.Name == string.Empty)
            {
                labelFWFilter.Text = "Clear";
                toolTip.SetToolTip(labelFWFilter, "");
            }
            else
            {
                labelFWFilter.Text = $"{currentFilter.Name}: {currentFilter.Description}";
                toolTip.SetToolTip(labelFWFilter,
                    " Filter name:  " + currentFilter.Name + Const.crnl +
                    " Description:  " + currentFilter.Description + Const.crnl +
                    " Focus offset: " + currentFilter.Offset.ToString());
            }

            if (filterWheelDigest.Wheel.Filters.Count() != comboBoxFilterWheelPositions.Items.Count)
            {
                comboBoxFilterWheelPositions.Items.Clear();
                for (int pos = 0; pos < filterWheelDigest.Wheel.Filters.Count(); pos++)
                    comboBoxFilterWheelPositions.Items.Add($"{pos + 1} - Clear");
            }

            for (int pos = 0; pos < filterWheelDigest.Wheel.Filters.Count(); pos++)
            {
                if (filterWheelDigest.Wheel.Filters[pos].Name != string.Empty)
                    comboBoxFilterWheelPositions.Items[pos] =
                        $"{pos + 1} - " +
                        $"{filterWheelDigest.Wheel.Filters[pos].Name}: " +
                        $"{filterWheelDigest.Wheel.Filters[pos].Description}";

            }

            filterWheelStatus.Show((filterWheelDigest.Status != null && filterWheelDigest.Status != "Idle") ? filterWheelDigest.Status : "");
            if (filterWheelDigest.Arduino.Status == ArduinoInterface.ArduinoStatus.Idle)
                filterWheelArduinoStatus.Show("");

            if (filterWheelDigest.Arduino.Error != null && filterWheelDigest.Arduino.Error != string.Empty)
                filterWheelArduinoStatus.Show(filterWheelDigest.Arduino.Error, 5000, Statuser.Severity.Error);
            else if (filterWheelDigest.Arduino.StatusString != string.Empty)
                filterWheelArduinoStatus.Show(filterWheelDigest.Arduino.StatusString);

            string tip = "Arduino:\r\n";
            TimeSpan ts = DateTime.Now.Subtract(Convert.ToDateTime(filterWheelDigest.LastDataReceived));
            tip += $"  Age:  {ts.ToString(@"s\.ff")} seconds\r\n";
            string err = filterWheelDigest.Arduino.Error != null && filterWheelDigest.Arduino.Error != string.Empty ?
                filterWheelDigest.Arduino.Error : "none";
            tip += $"  Status:  {filterWheelDigest.Arduino.StatusString}\r\n";
            if (filterWheelDigest.Arduino.LastCommand != null)
                tip += $"  Last cmd: {filterWheelDigest.Arduino.LastCommand}\r\n";
            tip += $"  Error:   {err}\r\n";
            toolTip.SetToolTip(filterWheelArduinoStatus.Label, tip);
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
                alterations += $"  {alteredItems[key] + ":",-20} {mark} {text}" + Const.crnl;
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
                WiseFocuser.WriteProfile();
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
            new FilterWheelForm(wiseFilterWheel).Show();
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
            int targetPosition = comboBoxFilterWheelPositions.SelectedIndex;

            filterWheelStatus.Show($"Moving to position {targetPosition + 1}", 5000, Statuser.Severity.Good);
            wiseFilterWheel.Position = (short)targetPosition;
        }

        private void manage2InchFilterInventoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FiltersForm(wiseFilterWheel, WiseFilterWheel.FilterSize.TwoInch).Show();
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

        private void groupBoxFilterWheel_Enter(object sender, EventArgs e)
        {

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
