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
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.VantagePro;
using ASCOM.Wise40.TessW;    

namespace ASCOM.Wise40SafeToOperate
{
    public class WiseSafeToOperate
    {
        private static Version version = new Version(0, 2);

        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        public string driverID = Const.WiseDriverID.SafeToOperate;
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
        public static PressureSensor pressureSensor;
        public static TemperatureSensor temperatureSensor;

        public static TessWRefresher tessWRefresher;
        public static OWLRefresher owlRefresher;
        public static ARDORefresher ardoRefresher;

        public static DoorLockSensor doorLockSensor;
        public static ComputerControlSensor computerControlSensor;
        public static PlatformSensor platformSensor;

        public static List<Sensor> _cumulativeSensors, _prioritizedSensors;
        public static Dictionary<string, Sensor> _sensorHandlers = new Dictionary<string, Sensor>();
        private enum SafetyScope { Wise40, WiseWide };
        private static SafetyScope safetyScope = SafetyScope.Wise40;

        private static bool _bypassed = false;
        public static int ageMaxSeconds;

        public static Event.SafetyEvent.SafetyState _safetyState = Event.SafetyEvent.SafetyState.Unknown;
        public static ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        public static bool _unsafeBecauseNotReady = false;

        private static ASCOM.DriverAccess.ObservingConditions tessw = new ASCOM.DriverAccess.ObservingConditions("ASCOM.Wise40.TessW.ObservingConditions");

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private static bool _connected = false;

        private Wise40.Common.Debugger debugger = Debugger.Instance;

        public static WiseSite wisesite = WiseSite.Instance;

        private static bool initialized = false;

        public static TimeSpan _stabilizationPeriod;
        private static int _defaultStabilizationPeriodMinutes = 15;

        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public Astrometry.Accuracy astrometricAccuracy;
        Object3 Sun = new Object3();

        static WiseSafeToOperate() { }
        public WiseSafeToOperate() { }

        private static readonly Lazy<WiseSafeToOperate> lazy = new Lazy<WiseSafeToOperate>(() => new WiseSafeToOperate()); // Singleton

        public static WiseSafeToOperate Instance
        {
            get
            {
                if (lazy == null)
                    return null;

                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public void init()
        {
            if (initialized)
                return;

            name = "Wise40 SafeToOperate";
            driverDescription = string.Format("{0} v{1}", driverID, version.ToString());

            if (_profile == null)
            {
                _profile = new Profile() { DeviceType = "SafetyMonitor" };
            }

            WiseSite.initOCH();
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
            pressureSensor = new PressureSensor(this);
            temperatureSensor = new TemperatureSensor(this);

            tessWRefresher = new TessWRefresher(this);
            owlRefresher = new OWLRefresher(this);
            ardoRefresher = new ARDORefresher(this);

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
                humanInterventionSensor,    // Immediate sensors
                computerControlSensor,
                platformSensor,
                doorLockSensor,

                sunSensor,                  // Weather sensors - affecting isSafe
                windSensor,
                cloudsSensor,
                rainSensor,
                humiditySensor,

                pressureSensor,             // Weather sensors - NOT affecting isSafe
                temperatureSensor,

                tessWRefresher,             // Refreshers
                owlRefresher,
                ardoRefresher,
            };

            _cumulativeSensors = new List<Sensor>();
            foreach (Sensor s in _prioritizedSensors)
            {
                _sensorHandlers[s.WiseName.ToLower()] = s;
                if (!s.HasAttribute(Sensor.Attribute.SingleReading))
                    _cumulativeSensors.Add(s);
            }

            _connected = false;

            novas31 = new NOVAS31();
            astroutils = new AstroUtils();
            ascomutils = new Util();

            novas31.MakeObject(0, Convert.ToInt16(Body.Sun), "Sun", new CatEntry3(), ref Sun);

            ReadProfile(); // Read device configuration from the ASCOM Profile store
            _safetyState = Event.SafetyEvent.SafetyState.Unknown;
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

        private readonly ArrayList supportedActions = new ArrayList() {
            "start-bypass",
            "end-bypass",
            "status",
            "unsafereasons",
            "list-sensors",
            "sensor-is-safe",
            "raw-weather-data",
            "wise-list-sensors",
            "wise-sensor-is-safe",
            "wise-issafe",
            "wise-unsafereasons",
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
            List<string> sensors = new List<string>();
            string sensorName;

            switch (actionName.ToLower())
            {
                case "list-sensors":
                    foreach (Sensor s in _prioritizedSensors)
                        if (! s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                            sensors.Add(s.WiseName);
                    ret = JsonConvert.SerializeObject(sensors);
                    break;

                case "wise-list-sensors":
                    foreach (Sensor s in _prioritizedSensors)
                        if (! s.HasAttribute(Sensor.Attribute.Wise40Specific) && !s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                            sensors.Add(s.WiseName);
                    ret = JsonConvert.SerializeObject(sensors);
                    break;

                case "sensor-is-safe":
                    sensorName = actionParameters.ToLower();
                    if (_sensorHandlers.ContainsKey(sensorName))
                        ret = JsonConvert.SerializeObject(_sensorHandlers[sensorName].isSafe);
                    else
                        throw new ASCOM.InvalidValueException($"Unknown sensor \"{sensorName}\"!");
                    break;

                case "wise-sensor-is-safe":
                    sensorName = actionParameters.ToLower();
                    if (!_sensorHandlers.ContainsKey(sensorName) || _sensorHandlers[sensorName].HasAttribute(Sensor.Attribute.Wise40Specific))
                        throw new ASCOM.InvalidValueException($"Unknown Wise-wide sensor \"{sensorName}\"!");
                    ret = JsonConvert.SerializeObject(_sensorHandlers[sensorName].isSafe);
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
                    ret = string.Join(Const.recordSeparator, UnsafeReasonsList());
                    break;

                case "wise-issafe":
                    ret = Convert.ToString(WiseIsSafe);
                    break;

                case "wise-unsafereasons":
                    ret = string.Join(Const.recordSeparator, UnsafeReasonsList(toBeIgnored: Sensor.Attribute.Wise40Specific));
                    break;

                case "raw-weather-data":
                    return RawWeatherData;

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

                return JsonConvert.SerializeObject(new SafeToOperateDigest
                {
                    Bypassed = _bypassed,
                    Ready = isReady(toBeIgnored: Sensor.Attribute.None),
                    Safe = IsSafe,
                    UnsafeReasons = UnsafeReasonsList(),
                    UnsafeBecauseNotReady = _unsafeBecauseNotReady,
                    ShuttingDown = activityMonitor.ShuttingDown,
                    ComputerControl = Sensor.SensorDigest.FromSensor(computerControlSensor),
                    SunElevation = Sensor.SensorDigest.FromSensor(sunSensor),
                    HumanIntervention = Sensor.SensorDigest.FromSensor(humanInterventionSensor),
                    Platform = Sensor.SensorDigest.FromSensor(platformSensor),

                    Temperature = Sensor.SensorDigest.FromSensor(temperatureSensor),
                    Pressure = Sensor.SensorDigest.FromSensor(pressureSensor),
                    Humidity = Sensor.SensorDigest.FromSensor(humiditySensor),
                    RainRate = Sensor.SensorDigest.FromSensor(rainSensor),
                    WindSpeed = Sensor.SensorDigest.FromSensor(windSensor),
                    CloudCover = Sensor.SensorDigest.FromSensor(cloudsSensor),

                    WindDirection = Sensor.SensorDigest.FromOCHProperty("WindDirection"),
                    DewPoint = Sensor.SensorDigest.FromOCHProperty("DewPoint"),
                    SkyTemperature = Sensor.SensorDigest.FromOCHProperty("SkyTemperature"),
                }
                );
            }
        }

        public string DigestSensors(string sensorName)
        {
            if (sensorName == "all")
                return JsonConvert.SerializeObject(_prioritizedSensors);

            foreach (var sensor in _prioritizedSensors)
                if (!sensor.HasAttribute(Sensor.Attribute.ForInfoOnly) && sensor.WiseName == sensorName)
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
                return isReady(toBeIgnored: Sensor.Attribute.None);
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

                ActivityMonitor.Instance.Event(new Event.DriverConnectEvent(Const.WiseDriverID.WiseSafeToOperate, _connected, line: ActivityMonitor.Tracer.safety.Line));
            }
        }

        public void stopSensors()
        {
            foreach (Sensor s in _prioritizedSensors)
            {
                s.Enabled = false;
                s.Connected = false;
            }
        }

        public void startSensors()
        {
            if (_prioritizedSensors == null)
                return;

            foreach (Sensor s in _prioritizedSensors)
            {
                s.Connected = true;

                if (s.HasAttribute(Sensor.Attribute.Periodic))
                    s.Restart();
            }
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
                return string.Join(Const.subFieldSeparator, UnsafeReasonsList());
            }
        }

        public static string GenericUnsafeReason(Sensor s)
        {
            if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                return null;

            if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                return null;   // not enabled

            if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                return null;   // bypassed

            string reason = string.Format("{0} - ", s.WiseName);

            if (!s.isSafe)
            {
                if (s.HasAttribute(Sensor.Attribute.SingleReading))
                    reason += s.reason();
                else
                {
                    if (s.StateIsSet(Sensor.State.Stabilizing))
                    {
                        // cummulative and stabilizing
                        string time = string.Empty;
                        TimeSpan ts = s.TimeToStable;

                        if (ts.TotalMinutes > 0)
                            time += ((int)ts.TotalMinutes).ToString() + "m";
                        time += ts.Seconds.ToString() + "s";

                        reason += String.Format("stabilizing in {0}", time);
                    }
                    else if (!s.StateIsSet(Sensor.State.EnoughReadings))
                    {
                        // cummulative and not ready
                        reason += String.Format("not ready (only {0} of {1} readings)",
                            s._nreadings, s._repeats);
                    }
                }

                if (s.HasAttribute(Sensor.Attribute.CanBeStale) && s.StateIsSet(Sensor.State.Stale))
                    // cummulative and stale
                    reason += String.Format(" ({0} stale readings)", s._nstale);
            }

            return reason;
        }

        public List<string> UnsafeReasonsList(Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
        {
            List<string> reasons = new List<string>();

            if (!_connected)
            {
                reasons.Add("Not Connected");
                return reasons;
            }

            if ((toBeIgnored & Sensor.Attribute.Wise40Specific) == 0 && activityMonitor.ShuttingDown)
            {
                reasons.Add(Const.UnsafeReasons.ShuttingDown);
                return reasons;     // when shutting down all sensors are ignored
            }

            foreach (Sensor s in _prioritizedSensors)
            {
                if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                    continue;

                if (toBeIgnored != Sensor.Attribute.None && s.HasAttribute(toBeIgnored))
                    continue;

                if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                    continue;   // not enabled

                if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                    continue;   // bypassed

                if (!s.isSafe)
                {
                    string reason = GenericUnsafeReason(s);

                    if (reason != null)
                    {
                        // we have a reason for this sensor not being safe
                        reasons.Add(reason);
                        if (s.HasAttribute(Sensor.Attribute.ForcesDecision))
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
                if (!cloudsSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return cloudsSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                if (!windSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return windSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                if (!humiditySensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return humiditySensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                if (!rainSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return rainSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeSunElevation
        {
            get
            {
                double max = DateTime.Now.Hour < 12 ? Convert.ToDouble(sunSensor.MaxAtDawnAsString) : Convert.ToDouble(sunSensor.MaxAtDuskAsString);

                return SunElevation <= max ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }
        #endregion
        #endregion

        public double SunElevation
        {
            get
            {
                if (astroutils == null)
                    return 0.0;

                double ra = 0, dec = 0, dis = 0;
                double jdt = astroutils.JulianDateUT1(0);
                short res;

                res = novas31.LocalPlanet(
                    astroutils.JulianDateUT1(0),
                    Sun,
                    astroutils.DeltaT(),
                    WiseSite.Instance._onSurface,
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
                    WiseSite.Instance._onSurface,
                    ra, dec,
                    WiseSite.refractionOption,
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
                if (activityMonitor.ShuttingDown)
                    return false;

                return IsSafeWithoutCheckingForShutdown();
            }
        }

        private bool WiseIsSafe
        {
            get
            {
                if (activityMonitor.ShuttingDown)
                    return false;

                return IsSafeWithoutCheckingForShutdown(toBeIgnored: Sensor.Attribute.Wise40Specific);
            }
        }

        private List<string> WiseUnsafeReasonsList
        {
            get
            {
                return new List<string>();
            }
        }


        public bool IsSafeWithoutCheckingForShutdown(Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
        {
            bool ret = true;
            _unsafeBecauseNotReady = false;

            init();

            if (!_connected)
            {
                ret = false;
                goto Out;
            }

            foreach (Sensor s in _prioritizedSensors)
            {
                if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                    continue;

                if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                    continue;

                if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                    continue;

                if (toBeIgnored != Sensor.Attribute.None && s.HasAttribute(toBeIgnored))
                    continue;

                if (!s.isSafe)
                {
                    ret = false;    // The first non-safe sensor forces NOT SAFE
                    if (!s.HasAttribute(Sensor.Attribute.SingleReading))
                        _unsafeBecauseNotReady = true;
                    goto Out;
                }
            }

        Out:
            Event.SafetyEvent.SafetyState currentSafetyState = (ret == true) ?
                Event.SafetyEvent.SafetyState.Safe :
                Event.SafetyEvent.SafetyState.Unsafe;

            if (currentSafetyState != _safetyState)
            {
                _safetyState = currentSafetyState;
                ActivityMonitor.Instance.Event(new Event.SafetyEvent(_safetyState,
                    currentSafetyState == Event.SafetyEvent.SafetyState.Unsafe ? UnsafeReasons.Replace(Const.recordSeparator, "\n") : ""));
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "IsReallySafe: {0}", ret);
            #endregion
            return ret;
        }

        #endregion

        public bool isReady(Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
        {
            foreach (Sensor s in _cumulativeSensors)
            {
                if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                    continue;

                if (toBeIgnored != Sensor.Attribute.None && s.HasAttribute(toBeIgnored))
                    continue;

                if (s.HasAttribute(Sensor.Attribute.Wise40Specific) && safetyScope != SafetyScope.Wise40)
                    continue;

                if (!s.StateIsSet(Sensor.State.EnoughReadings))
                    return false;
            }

            return true;
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
                string telescopeDriverId = Const.WiseDriverID.Telescope;

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

        private string RawWeatherData
        {
            get
            {
                RawWeatherData ret = new RawWeatherData
                {
                    Boltwood = JsonConvert.DeserializeObject<List<BoltwoodStation.RawData>>(WiseSite.och.Action("//Wise40.Boltwood:raw-data", "")),
                    VantagePro2 = JsonConvert.DeserializeObject<WiseVantagePro.VantagePro2StationRawData>(WiseSite.och.Action("//Wise40.VantagePro2:raw-data", "")),
                    TessW = JsonConvert.DeserializeObject<WiseTessW.TessWStationRawData>(tessw.Action("raw-data", "")),
                    OWL = WiseSafeToOperate.owlRefresher.Digest() as OWLRefresher.OWLDigest,
                };

                return JsonConvert.SerializeObject(ret);
            }
        }
    }

    public class SafeToOperateDigest
    {
        public bool Bypassed;
        public bool Ready;
        public bool Safe;
        public bool UnsafeBecauseNotReady;
        public List<string> UnsafeReasons;
        public bool ShuttingDown;

        // global sensors
        public Sensor.SensorDigest ComputerControl;
        public Sensor.SensorDigest Platform;
        public Sensor.SensorDigest HumanIntervention;
        public Sensor.SensorDigest SunElevation;

        // weather sensors
        public Sensor.SensorDigest Temperature;
        public Sensor.SensorDigest Pressure;
        public Sensor.SensorDigest Humidity;
        public Sensor.SensorDigest RainRate;
        public Sensor.SensorDigest WindSpeed;
        public Sensor.SensorDigest WindDirection;
        public Sensor.SensorDigest CloudCover;
        public Sensor.SensorDigest DewPoint;
        public Sensor.SensorDigest SkyTemperature;
    }

    public class WeatherDigest
    {
        public Sensor.SensorDigest Temperature;
        public Sensor.SensorDigest Pressure;
        public Sensor.SensorDigest Humidity;
        public Sensor.SensorDigest RainRate;
        public Sensor.SensorDigest WindSpeed;
        public Sensor.SensorDigest WindDirection;
        public Sensor.SensorDigest CloudCover;
        public Sensor.SensorDigest DewPoint;
        public Sensor.SensorDigest SkyTemperature;
    }


    public class RawWeatherData
    {
        public List<BoltwoodStation.RawData> Boltwood;
        public WiseVantagePro.VantagePro2StationRawData VantagePro2;
        public WiseTessW.TessWStationRawData TessW;
        public OWLRefresher.OWLDigest OWL;
    }
}
