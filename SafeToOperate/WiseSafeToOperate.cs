﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ASCOM;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using ASCOM.DriverAccess;

namespace ASCOM.Wise40SafeToOperate
{
    public class WiseSafeToOperate
    {
        private static Version version = new Version(0, 2);

        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        public string driverID;
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public string driverDescription;
        private string name;

        public Profile _profile;
        
        public WindSensor windSensor;
        public CloudsSensor cloudsSensor;
        public RainSensor rainSensor;
        public HumiditySensor humiditySensor;
        public SunSensor sunSensor;
        public HumanInterventionSensor humanInterventionSensor;
        public WiseComputerControl wisecomputercontrol;
        public List<Sensor> _sensors;
        private bool _bypassed = false;
        private bool _shuttingDown = false;
        public int ageMaxSeconds;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected = false;

        private Wise40.Common.Debugger debugger = Debugger.Instance;
        private static TraceLogger tl;
        
        public ObservingConditions och = WiseSite.Instance.och;
        
        private static object syncObject = new object();

        private static volatile WiseSafeToOperate _instance;
        private static bool initialized = false;
        
        public TimeSpan _stabilizationPeriod;
        private int _defaultStabilizationPeriodMinutes = 15;

        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public double siteLatitude, siteLongitude, siteElevation;
        public Astrometry.OnSurface onSurface;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption = Astrometry.RefractionOption.NoRefraction;
        Object3 Sun = new Object3();

        public static WiseSafeToOperate Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock(syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseSafeToOperate();
                    }
                }
                _instance.init();
                return _instance;
            }
        }

        public WiseSafeToOperate()
        {
        }

        public void init()
        {
            if (initialized)
                return;

            och = new ObservingConditions("ASCOM.OCH.ObservingConditions");
            if (tl == null)
                tl = new TraceLogger("", "Wise40.SafeToOperate");
            name = "Wise40 SafeToOperate";
            driverID = Const.wiseSafeToOperateDriverID;
            driverDescription = string.Format("ASCOM Wise40.SafeToOperate v{0}", version.ToString());

            if (_profile == null)
            {
                _profile = new Profile() { DeviceType = "SafetyMonitor" };
            }
            
            humiditySensor = new HumiditySensor(this);
            windSensor = new WindSensor(this);
            sunSensor = new SunSensor(this);
            cloudsSensor = new CloudsSensor(this);
            rainSensor = new RainSensor(this);
            humanInterventionSensor = new HumanInterventionSensor(this);
            wisecomputercontrol = WiseComputerControl.Instance;
            _sensors = new List<Sensor>() {
                windSensor,
                cloudsSensor,
                rainSensor,
                humiditySensor,
                sunSensor,
                humanInterventionSensor };
            
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false;

            novas31 = new NOVAS31();
            astroutils = new AstroUtils();
            ascomutils = new Util();

            siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
            siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
            siteElevation = 882.9;
            novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);
            novas31.MakeObject(0, Convert.ToInt16(Body.Sun), "Sun", new CatEntry3(), ref Sun);

            ReadProfile(); // Read device configuration from the ASCOM Profile store
            initialized = true;

            tl.LogMessage("SafetyMonitor", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE ISafetyMonitor IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (_connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SafeToOperateSetupDialogForm F = new SafeToOperateSetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        private ArrayList supportedActions = new ArrayList() { "start-bypass", "end-bypass", "status", "sensor-is-safe" };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            string ret = string.Empty;

            switch (actionName.ToLower())
            {
                case "sensor-is-safe":
                    switch(actionParameters)
                    {
                        case "HumanIntervention":
                            ret = humanInterventionSensor.isSafe.ToString();
                            break;

                        case "Sun":
                            ret = sunSensor.isSafe.ToString();
                            break;

                        case "Wind":
                            ret = windSensor.isSafe.ToString();
                            break;

                        case "Rain":
                            ret = rainSensor.isSafe.ToString();
                            break;

                        case "Humidity":
                            ret = humiditySensor.isSafe.ToString();
                            break;

                        case "Clouds":
                            ret = cloudsSensor.isSafe.ToString();
                            break;
                    }
                    break;

                case "start-bypass":
                    _bypassed = true;
                    _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                    ret = "ok";
                    break;

                case "end-bypass":
                    _bypassed = false;
                    _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                    ret = "ok";
                    break;

                case "status":
                    _bypassed = Convert.ToBoolean(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, string.Empty, false.ToString()));

                    List<string> stat = new List<string>() {
                        "computer-control:" + (!wisecomputercontrol.Maintenance).ToString().ToLower(),
                        "platform-lowered:" + wisecomputercontrol.PlatformIsDown.ToString().ToLower(),
                        "no-human-intervention:" + humanInterventionSensor.isSafe.ToString().ToLower(),
                        "bypassed:" + _bypassed.ToString().ToLower(),
                        "ready:" + isReady.ToString().ToLower(),
                        "safe:" + IsSafe.ToString().ToLower(),
                    };
                    ret = string.Join(",", stat);
                    break;

                case "unsafereasons":
                    bool dummy = IsSafe;
                    ret = string.Join(", ", UnsafeReasons);
                    break;

                case "start-shutdown":      // hidden
                    _shuttingDown = true;
                    ret = "ok";
                    break;

                case "end-shutdown":        // hidden
                    _shuttingDown = false;
                    ret = "ok";
                    break;

                default:
                    throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
            }
            return ret;
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");

            if (command.ToLower() == "ready")
                return isReady;
            else
                throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            if (command.ToLower() == "unsafereasons")
            {
                return Action("unsafereasons", string.Empty);
            }
            else
                return stringSafetyCommand(command);
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                och.Connected = value;
                _connected = och.Connected;

                if (_connected)
                    startSensors();
                else
                    stopSensors();
            }
        }

        public void stopSensors()
        {
            foreach (Sensor s in _sensors)
                s.Stop();
        }

        public void startSensors()
        {
            foreach (Sensor s in _sensors)
                s.Start();
        }

        public string DriverId
        {
            get
            {
                tl.LogMessage("DriverId Get", driverID);
                return driverID;
            }
        }

        public string Description
        {
            get
            {
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "Implements Wise40 weather max. values, wraps Boltwood CloudSensorII and Davis VantagePro. Version: " + DriverVersion;
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public static string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                if (tl == null)
                    tl = new TraceLogger("", "Wise40.SafeToOperate");
                tl.LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                if (tl == null)
                    tl = new TraceLogger("", "Wise40.SafeToOperate");
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        public List<string> UnsafeReasons
        {
            get
            {
                List<string> reasons = new List<string>();

                if (!wisecomputercontrol.IsSafe)
                {
                    foreach (string reason in wisecomputercontrol.UnsafeReasons())
                        reasons.Add(reason);
                }
                else if (!humanInterventionSensor.isSafe)
                {
                    reasons.Add(humanInterventionSensor.reason());
                }
                else
                {
                    string reason;
                    foreach (Sensor s in _sensors)
                    {
                        if (s.Name == "Sun")
                            continue;
                        if (s.nReadings < s._repeats)
                        {
                            reasons.Add(string.Format("{0} - not enough readings ({1} < {2})", s.Name, s.nReadings, s._repeats));
                        }
                        else if (!s.isSafe && (reason = s.reason()) != string.Empty)
                            reasons.Add(reason);
                    }

                    double elev = SunElevation, maxElevation = Convert.ToDouble(sunSensor.MaxAsString);
                    if (elev > maxElevation)
                        reasons.Add(string.Format("Sun elevation ({0:f1}deg) is higher than {1:f1}deg.", elev, maxElevation));
                }
                return reasons;
            }
        }

        #region Individual Property Implementations
        #region Boolean Properties (for ASCOM)

        private string stringSafetyCommand(string command)
        {
            Const.TriStateStatus status = Const.TriStateStatus.Good;
            string ret = "unknown";
            string msg = string.Empty;

            //lock (reasonsLock)
            {
                //unsafeReasons.Clear();
                switch (command.ToLower())
                {
                    case "humidity": status = isSafeHumidity; break;
                    case "wind": status = isSafeWindSpeed; break;
                    case "sun": status = isSafeSunElevation; break;
                    case "clouds": status = isSafeCloudCover; break;
                    case "rain": status = isSafeRain; break;
                    default:
                        status = Const.TriStateStatus.Error;
                        //unsafeReasons.Add(string.Format("invalid command \"{0}\"", command));
                        msg = string.Format("invalid command \"{0}\"", command);
                        break;
                }
            }

            switch (status)
            {
                case Const.TriStateStatus.Normal:
                case Const.TriStateStatus.Good:
                    return "ok";
                case Const.TriStateStatus.Error:
                    return "error: " + msg;
                case Const.TriStateStatus.Warning:
                    return "warning: " + msg;
            }

            return ret;
        }

        #endregion

        #region TriState Properties (for object)
        public Const.TriStateStatus isSafeCloudCover
        {
            get
            {
                return cloudsSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                return windSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                return humiditySensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                return rainSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeSunElevation
        {
            get
            {
                double max = Convert.ToDouble(sunSensor.MaxAsString);

                return SunElevation <=  max ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }
        #endregion
        #endregion

        public double SunElevation
        {
            get
            {
                double ra = 0, dec = 0, dis = 0;
                double jdt = astroutils.JulianDateUT1(0);
                short res;

                res = novas31.LocalPlanet(
                    astroutils.JulianDateUT1(0),
                    Sun,
                    astroutils.DeltaT(),
                    onSurface,
                    astrometricAccuracy,
                    ref ra, ref dec, ref dis);

                if (res != 0)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Failed to get LocalPlanet for the Sun (res: {0})", res);
                    return 0.0;
                }

                double rar = 0, decr = 0, zd = 0, az = 0;
                novas31.Equ2Hor(jdt, 0,
                    astrometricAccuracy,
                    0, 0,
                    onSurface,
                    ra, dec,
                    refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                if (res != 0)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Failed to convert equ2hor (res: {0})", res);
                    return 0.0;
                }

                return 90.0 - zd;
            }
        }

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                bool ret = true;

                if (!_connected)
                    ret = false;
                else if (!wisecomputercontrol.IsSafe)
                    ret = false;
                else if (!_shuttingDown && !humanInterventionSensor.isSafe)
                    ret = false;
                else if (_bypassed)
                    return true;
                else
                {
                    foreach (Sensor s in _sensors)
                    {
                        if (!s.isSafe)
                        {
                            ret = false;
                            break;
                        }
                    }

                    if (SunElevation > Convert.ToDouble(sunSensor.MaxAsString))
                        ret = false;
                }

                //tl.LogMessage("IsSafe Get", ret.ToString());
                return ret;
            }
        }

        #endregion

        public bool isReady
        {
            get
            {
                foreach (Sensor s in _sensors)
                {
                    if (s.Name == "Sun")
                        continue;

                    if (s.nReadings < s._repeats)
                        return false;
                }

                return true;
            }
        }

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        public void ReadProfile()
        {
            ageMaxSeconds = Convert.ToInt32(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_AgeMaxSeconds, string.Empty, 180.ToString()));
            _bypassed = Convert.ToBoolean(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, string.Empty, false.ToString()));

            int minutes = Convert.ToInt32(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_StableAfterMin, string.Empty, _defaultStabilizationPeriodMinutes.ToString()));
            _stabilizationPeriod = new TimeSpan(0, minutes, 0);

            foreach (Sensor s in _sensors)
                s.readProfile();

            using (Profile driverProfile = new Profile())
            {
                string telescopeDriverId = Const.wiseTelescopeDriverID;

                driverProfile.DeviceType = "Telescope";
                astrometricAccuracy =
                    driverProfile.GetValue(telescopeDriverId, Const.ProfileName.Telescope_AstrometricAccuracy, string.Empty, "Full") == "Full" ?
                        Accuracy.Full :
                        Accuracy.Reduced;
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_AgeMaxSeconds, ageMaxSeconds.ToString());
            _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_StableAfterMin, _stabilizationPeriod.Minutes.ToString());
            foreach (Sensor s in _sensors)
                s.writeProfile();
        }
        #endregion
    }
}
