using System;
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

using Newtonsoft.Json;

namespace ASCOM.Wise40SafeToOperate
{
    public class WiseSafeToOperate
    {
        private static Version version = new Version(0, 2);

        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        public string driverID = Const.wiseSafeToOperateDriverID;
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public string driverDescription;
        private string name;

        public Profile _profile;

        public static WindSensor windSensor;
        public static CloudsSensor cloudsSensor;
        public static RainSensor rainSensor;
        public static HumiditySensor humiditySensor;
        public static SunSensor sunSensor;
        public static HumanInterventionSensor humanInterventionSensor;

        public static DoorLockSensor doorLockSensor;
        public static ComputerControlSensor computerControlSensor;
        public static PlatformSensor platformSensor;

        public static List<Sensor> _cumulativeSensors, _prioritizedSensors;
        private static bool _bypassed = false;
        private static bool _shuttingDown = false;
        public static int ageMaxSeconds;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private static bool _connected = false;

        private Wise40.Common.Debugger debugger = Debugger.Instance;

        private static WiseSite wisesite = WiseSite.Instance;

        private static object syncObject = new object();

        private static volatile WiseSafeToOperate _instance = new WiseSafeToOperate();
        private static bool initialized = false;

        public static TimeSpan _stabilizationPeriod;
        private static int _defaultStabilizationPeriodMinutes = 15;

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
                    if (syncObject == null)
                        syncObject = new object();

                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseSafeToOperate();
                    }
                }
                _instance.init();
                return _instance;
            }
        }

        public WiseSafeToOperate() { }

        static WiseSafeToOperate() { }

        public void init()
        {
            if (initialized)
                return;

            name = "Wise40 SafeToOperate";
            driverDescription = string.Format("ASCOM Wise40.SafeToOperate v{0}", version.ToString());

            if (_profile == null)
            {
                _profile = new Profile() { DeviceType = "SafetyMonitor" };
            }

            WiseSite.och.Connected = true;

            humiditySensor = new HumiditySensor(this);
            windSensor = new WindSensor(this);
            sunSensor = new SunSensor(this);
            cloudsSensor = new CloudsSensor(this);
            rainSensor = new RainSensor(this);
            humanInterventionSensor = new HumanInterventionSensor(this);
            computerControlSensor = new ComputerControlSensor(this);
            platformSensor = new PlatformSensor(this);
            doorLockSensor = new DoorLockSensor(this);

            //
            // The sensors in priotity order.  The first one that:
            //   - is enabled
            //   - not bypassed
            //   - forces decision
            //   - is not safe
            // causes SafeToOperate to be NOT SAFE
            //
            _prioritizedSensors = new List<Sensor>()
            {
                humanInterventionSensor,
                computerControlSensor,
                platformSensor,
                doorLockSensor,
                sunSensor,
                windSensor,
                cloudsSensor,
                rainSensor,
                humiditySensor,
            };

            _cumulativeSensors = new List<Sensor>();
            foreach (Sensor s in _prioritizedSensors)
                if (!s.HasAttribute(Sensor.SensorAttribute.Immediate))
                    _cumulativeSensors.Add(s);

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

        private ArrayList supportedActions = new ArrayList() {
            "start-bypass",
            "end-bypass",
            "status",
            "sensor-is-safe"
        };

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
                    switch (actionParameters)
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
                    if (actionParameters.ToLower() != "temporary")
                        _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Started bypass (parameter: {0})", actionParameters);
                    #endregion
                    ret = "ok";
                    break;

                case "end-bypass":
                    _bypassed = false;
                    if (actionParameters.ToLower() != "temporary")
                        _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Ended bypass (parameter: {0})", actionParameters);
                    #endregion
                    ret = "ok";
                    break;

                case "status":
                    if (actionParameters == string.Empty)
                        return Digest;
                    else
                        return DigestSensors(actionParameters);

                case "unsafereasons":
                    bool dummy = IsSafe;
                    ret = string.Join(Const.recordSeparator, UnsafeReasonsList);
                    break;

                case "start-shutdown":      // hidden
                    _shuttingDown = true;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Started shutdown");
                    #endregion
                    ret = "ok";
                    break;

                case "end-shutdown":        // hidden
                    _shuttingDown = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Ended shutdown");
                    #endregion
                    ret = "ok";
                    break;

                case "digest":
                    List<object> objects = new List<object>();

                    foreach (Sensor s in _prioritizedSensors)
                        if (s.WiseName == "DoorLock")
                            objects.Add(s.Digest());
                    ret = JsonConvert.SerializeObject(objects);
                    break;


                default:
                    throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
            }
            return ret;
        }

        public string Digest
        {
            get
            {
                _bypassed = Convert.ToBoolean(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, string.Empty, false.ToString()));

                return JsonConvert.SerializeObject(new SafeToOperateDigest()
                {
                    ComputerControlIsSafe = computerControlSensor.isSafe,
                    PlatformIsSafe = platformSensor.isSafe,
                    HumanInterventionIsSafe = humanInterventionSensor.isSafe,
                    Bypassed = _bypassed,
                    Ready = CumulativeSensorsAreReady,
                    Safe = IsSafe,
                    UnsafeReasons = UnsafeReasonsList,
                    Colors = new Colors() {
                        SunElevationColorArgb = Statuser.TriStateColor(isSafeSunElevation).ToArgb(),
                        RainColorArgb = Statuser.TriStateColor(isSafeRain).ToArgb(),
                        WindSpeedColorArgb = Statuser.TriStateColor(isSafeWindSpeed).ToArgb(),
                        CloudCoverColorArgb = Statuser.TriStateColor(isSafeCloudCover).ToArgb(),
                        HumidityColorArgb = Statuser.TriStateColor(isSafeHumidity).ToArgb(),
                    },
                    SunElevation = SunElevation,

                });
            }
        }

        public string DigestSensors(string sensorName)
        {
            if (sensorName == "all")
                return JsonConvert.SerializeObject(_prioritizedSensors);

            foreach (var sensor in _prioritizedSensors)
                if (sensor.WiseName == sensorName)
                    return JsonConvert.SerializeObject(sensor);

            return string.Format("unknown sensor \"{0}\"!", sensorName);
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
                return CumulativeSensorsAreReady;
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
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                if (value == _connected)
                    return;

                _connected = WiseSite.och.Connected;

                if (_connected)
                    startSensors();
                else
                    stopSensors();
            }
        }

        public void stopSensors()
        {
            foreach (Sensor s in _cumulativeSensors)
                s.Enabled = false;
        }

        public void startSensors()
        {
            foreach (Sensor s in _cumulativeSensors)
                s.Restart(0);
        }

        public string DriverId
        {
            get
            {
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
                return "Implements Wise40 SafeToOperate. Version: " + DriverVersion;
            }
        }

        public static string DriverVersion
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        #endregion

        public string UnsafeReasons
        {
            get
            {
                return string.Join(Const.subFieldSeparator, UnsafeReasonsList);
            }
        }

        public List<string> UnsafeReasonsList
        {
            get
            {
                List<string> reasons = new List<string>();

                if (!_connected)
                {
                    reasons.Add("Not Connected");
                    return reasons;
                }

                if (_shuttingDown)
                    return reasons;     // when shutting down all sensors are ignored

                foreach (Sensor s in _prioritizedSensors)
                {
                    if (!s.HasAttribute(Sensor.SensorAttribute.AlwaysEnabled) && !s.StateIsSet(Sensor.SensorState.Enabled))
                        continue;   // not enabled

                    if (_bypassed && s.HasAttribute(Sensor.SensorAttribute.CanBeBypassed))
                        continue;   // bypassed

                    if (!s.isSafe)
                    {
                        string reason = null;

                        if (s.HasAttribute(Sensor.SensorAttribute.Immediate))
                            reason = s.reason();
                        else
                        {
                            if (!s.StateIsSet(Sensor.SensorState.Ready))
                            {
                                // cummulative and not ready
                                reason = String.Format("{0} - not ready (only {1} of {2} readings)",
                                    s.WiseName, s._nreadings, s._repeats);
                            }
                            else if (s.StateIsSet(Sensor.SensorState.Stabilizing))
                            {
                                // cummulative and stabilizing
                                string time = string.Empty;
                                TimeSpan ts = s.TimeToStable;

                                if (ts.TotalMinutes > 0)
                                    time += ((int)ts.TotalMinutes).ToString() + "m";
                                time += ts.Seconds.ToString() + "s";

                                reason = String.Format("{0} - stabilizing in {1}", s.WiseName, time);
                            }
                            else if (s.HasAttribute(Sensor.SensorAttribute.CanBeStale) && s.StateIsSet(Sensor.SensorState.Stale))
                                // cummulative and stale
                                reason = String.Format("{0} - {1} out of {2} readings are stale",
                                    s.WiseName, s._nstale, s._repeats);
                        }

                        if (reason != null)
                        {
                            // we have a reason for this sensor not being safe
                            reasons.Add(reason);
                            if (s.HasAttribute(Sensor.SensorAttribute.ForcesDecision))
                                break;      // don't bother with the remaining sensors
                        }
                    }
                }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "UnsafeReasons: {0}",
                    string.Join(Const.recordSeparator, reasons));
                #endregion
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

            {
                switch (command.ToLower())
                {
                    case "humidity": status = isSafeHumidity; break;
                    case "wind": status = isSafeWindSpeed; break;
                    case "sun": status = isSafeSunElevation; break;
                    case "clouds": status = isSafeCloudCover; break;
                    case "rain": status = isSafeRain; break;
                    default:
                        status = Const.TriStateStatus.Error;
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
                if (!cloudsSensor.StateIsSet(Sensor.SensorState.Ready))
                    return Const.TriStateStatus.Warning;
                return cloudsSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                if (!windSensor.StateIsSet(Sensor.SensorState.Ready))
                    return Const.TriStateStatus.Warning;
                return windSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                if (!humiditySensor.StateIsSet(Sensor.SensorState.Ready))
                    return Const.TriStateStatus.Warning;
                return humiditySensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                if (!rainSensor.StateIsSet(Sensor.SensorState.Ready))
                    return Const.TriStateStatus.Warning;
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
                {
                    ret = false;
                    goto Out;
                }

                //if (_shuttingDown)
                //{
                //    ret = true;
                //    goto Out;
                //}

                foreach (Sensor s in _prioritizedSensors)
                {
                    if (!s.HasAttribute(Sensor.SensorAttribute.AlwaysEnabled) && !s.StateIsSet(Sensor.SensorState.Enabled))
                        continue;

                    if (_bypassed && s.HasAttribute(Sensor.SensorAttribute.CanBeBypassed))
                        continue;

                    if (! s.isSafe)
                    {
                        ret = false;    // The first non-safe sensor forces NOT SAFE
                        goto Out;
                    }
                }

                Out:
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "IsSafe: {0}", ret);
                #endregion
                return ret;
            }
        }

        #endregion

        public bool CumulativeSensorsAreReady
        {
            get
            {
                foreach (Sensor s in _cumulativeSensors)
                {
                    if (! s.StateIsSet(Sensor.SensorState.Ready) || s.StateIsSet(Sensor.SensorState.Stabilizing))
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

            foreach (Sensor s in _prioritizedSensors)
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
            foreach (Sensor s in _prioritizedSensors)
                s.writeProfile();
        }
        #endregion
    }

    public class SafeToOperateDigest
    {
        public bool ComputerControlIsSafe;
        public bool PlatformIsSafe;
        public bool HumanInterventionIsSafe;
        public bool Bypassed;
        public bool Ready;
        public bool Safe;
        public List<string> UnsafeReasons;
        public Colors Colors;
        public double SunElevation;
    }

    public class Colors
    {
        public int SunElevationColorArgb;
        public int RainColorArgb;
        public int WindSpeedColorArgb;
        public int HumidityColorArgb;
        public int CloudCoverColorArgb;
    }
}
