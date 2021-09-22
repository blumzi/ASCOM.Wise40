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
        private readonly static Version version = new Version(0, 2);

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
        public static ARDOSensor ardoSensor;

        public static DoorLockSensor doorLockSensor;
        public static ComputerControlSensor computerControlSensor;
        public static PlatformSensor platformSensor;

        public static List<Sensor> _cumulativeSensors, _prioritizedSensors;
        public static Dictionary<string, Sensor> _sensorHandlers = new Dictionary<string, Sensor>();
        private enum SafetyScope { Wise40, WiseWide };
        private const SafetyScope safetyScope = SafetyScope.Wise40;

        private static bool _bypassed = false;
        public static int ageMaxSeconds;

        public static Event.SafetyEvent.SafetyState _safetyState = Event.SafetyEvent.SafetyState.Unknown;
        public static ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        public static bool _unsafeBecauseNotReady = false;

        private static readonly ASCOM.DriverAccess.ObservingConditions tessw = new ASCOM.DriverAccess.ObservingConditions("ASCOM.Wise40.TessW.ObservingConditions");

        public static readonly Exceptor Exceptor = new Exceptor(Debugger.DebugLevel.DebugSafety);

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private static bool _connected = false;

        private readonly Wise40.Common.Debugger debugger = Debugger.Instance;

        public static WiseSite wisesite = WiseSite.Instance;

        private static bool initialized = false;

        public static TimeSpan _stabilizationPeriod;
        private const int _defaultStabilizationPeriodMinutes = 15;

        public Astrometry.Accuracy astrometricAccuracy;

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

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (initialized)
                return;

            name = "Wise40 SafeToOperate";
            driverDescription = $"{driverID} v{version}";

            if (_profile == null)
            {
                _profile = new Profile() { DeviceType = "SafetyMonitor" };
            }

            WiseSite.InitOCH();
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
            ardoSensor = new ARDOSensor(this);

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
                ardoSensor,
            };

            _cumulativeSensors = new List<Sensor>();
            foreach (Sensor s in _prioritizedSensors)
            {
                _sensorHandlers[s.WiseName.ToLower()] = s;
                if (!s.HasAttribute(Sensor.Attribute.SingleReading))
                    _cumulativeSensors.Add(s);
            }

            _connected = false;

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

        public ArrayList SupportedActions { get; } = new ArrayList() {
            "bypass",
            "status",
            "unsafereasons",
            "unsafereasons-json",
            "list-sensors",
            "sensor-is-safe",
            "raw-weather-data",
            "wise-list-sensors",
            "wise-sensor-is-safe",
            "issafe",
            "wise-issafe",
            "wise-unsafereasons",
            "debug",
        };

        public string Action(string actionName, string actionParameters)
        {
            string ret = "default-action-ret";
            List<string> sensors = new List<string>();
            string sensorName;
            List<string> parameters = new List<string>();

            if (!string.IsNullOrEmpty(actionParameters))
                parameters = actionParameters.ToLower().Split(',').ToList<string>();

            switch (actionName.ToLower())
            {
                case "debug":
                    if (!String.IsNullOrEmpty(actionParameters))
                    {
                        Debugger.DebugLevel newDebugLevel;
                        try
                        {
                            Enum.TryParse<Debugger.DebugLevel>(actionParameters, out newDebugLevel);
                            Debugger.SetCurrentLevel(newDebugLevel);
                        }
                        catch
                        {
                            return $"Cannot parse DebugLevel \"{actionParameters}\"";
                        }
                    }
                    return $"{Debugger.Level}";

                case "list-sensors":
                    foreach (Sensor s in _prioritizedSensors)
                    {
                        if (! s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                            sensors.Add(s.WiseName);
                    }

                    ret = JsonConvert.SerializeObject(sensors);
                    break;

                case "wise-list-sensors":
                    foreach (Sensor s in _prioritizedSensors)
                    {
                        if (! s.HasAttribute(Sensor.Attribute.Wise40Specific) && !s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                            sensors.Add(s.WiseName);
                    }

                    ret = JsonConvert.SerializeObject(sensors);
                    break;

                case "sensor-is-safe":
                    if (parameters.Count != 1)
                    {
                        ret = "Must specify sensor name";
                    }
                    else
                    {
                        sensorName = parameters[0];
                        if (_sensorHandlers.ContainsKey(sensorName))
                        {
                            ret = JsonConvert.SerializeObject(_sensorHandlers[sensorName].IsSafe);
                        }
                        else
                        {
                            Exceptor.Throw<InvalidValueException>("Action(sensor-is-safe)", $"Unknown sensor \"{sensorName}\"!");
                            ret = string.Empty;
                        }
                    }
                    break;

                case "wise-sensor-is-safe":
                    if (parameters.Count != 1)
                    {
                        ret = "Must specify sensor name";
                    }
                    else
                    {
                        sensorName = parameters[0];
                        if (!_sensorHandlers.ContainsKey(sensorName) || _sensorHandlers[sensorName].HasAttribute(Sensor.Attribute.Wise40Specific))
                            Exceptor.Throw<InvalidValueException>("Action(wise-sensor-is-safe)", $"Unknown Wise-wide sensor \"{sensorName}\"!");
                        ret = JsonConvert.SerializeObject(_sensorHandlers[sensorName].IsSafe);
                    }
                    break;

                case "bypass":
                    if (parameters.Count < 1)
                        ret = "Must specify parameter 'start', 'end' or 'status'";
                    else
                    {
                        switch (parameters[0])
                        {
                            case "start":
                                _bypassed = true;
                                if (!parameters.Contains("temporary"))
                                    _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"Started bypass (parameters: {string.Join(",", parameters.ToArray())})");
                                #endregion
                                ret = "ok";
                                break;

                            case "end":
                                _bypassed = false;
                                if (!parameters.Contains("temporary"))
                                    _profile.WriteValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, _bypassed.ToString());
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"Ended bypass (parameters: {string.Join(",", parameters.ToArray())})");
                                #endregion
                                ret = "ok";
                                break;

                            case "status":
                                ret = _bypassed.ToString();
                                break;
                        }
                    }
                    break;

                case "status":
                    ret = (parameters.Count == 0) ? Digest : DigestSensors(parameters[0]);
                    break;

                case "unsafereasons":
                    ret = string.Join(Const.recordSeparator, UnsafeReasonsList());
                    break;

                case "unsafereasons-json":
                    ret = JsonConvert.SerializeObject(UnsafeReasonsList());
                    break;

                case "wise-issafe":
                    ret = JsonConvert.SerializeObject(WiseIsSafe);
                    break;

                case "issafe":
                    ret = JsonConvert.SerializeObject(IsSafe);
                    break;

                case "wise-unsafereasons":
                    ret = JsonConvert.SerializeObject(UnsafeReasonsList(toBeIgnored: Sensor.Attribute.Wise40Specific));
                    break;

                case "raw-weather-data":
                    ret = RawWeatherData;
                    break;

                default:
                    Exceptor.Throw<ActionNotImplementedException>("Action", $"Action {actionName} is not implemented by this driver");
                    ret = string.Empty;
                    break;
            }
            return ret;
        }

        public string Digest
        {
            get
            {
                _bypassed = Convert.ToBoolean(_profile.GetValue(driverID, Const.ProfileName.SafeToOperate_Bypassed, string.Empty, false.ToString()));

                SafeToOperateDigest digest = new SafeToOperateDigest()
                {
                    Bypassed = _bypassed,
                    Ready = IsReady(),
                    Safe = IsSafe,
                    UnsafeReasons = UnsafeReasonsList(),
                    UnsafeBecauseNotReady = _unsafeBecauseNotReady,
                    ShuttingDown = activityMonitor.ShuttingDown,
                    ComputerControl = Sensor.SensorDigest.FromSensor(computerControlSensor),
                    SunElevation = Sensor.SensorDigest.FromSensor(sunSensor),
                    HumanIntervention = Sensor.SensorDigest.FromSensor(humanInterventionSensor),
                    Platform = Sensor.SensorDigest.FromSensor(platformSensor),
                    HumanInterventionCampusGlobal = humanInterventionSensor.CampusGlobal,

                    Temperature = Sensor.SensorDigest.FromSensor(temperatureSensor),
                    Pressure = Sensor.SensorDigest.FromSensor(pressureSensor),
                    Humidity = Sensor.SensorDigest.FromSensor(humiditySensor),
                    RainRate = Sensor.SensorDigest.FromSensor(rainSensor),
                    WindSpeed = Sensor.SensorDigest.FromSensor(windSensor),
                    CloudCover = Sensor.SensorDigest.FromSensor(cloudsSensor),

                    WindDirection = Sensor.SensorDigest.FromOCHProperty("WindDirection"),
                    DewPoint = Sensor.SensorDigest.FromOCHProperty("DewPoint"),
                    SkyTemperature = Sensor.SensorDigest.FromOCHProperty("SkyTemperature"),
                };

                return JsonConvert.SerializeObject(digest);
            }
        }

        public static string DigestSensors(string sensorName)
        {
            if (string.IsNullOrEmpty(sensorName) || sensorName == "all" || sensorName == "\"\"" || sensorName == "''")
                return JsonConvert.SerializeObject(_prioritizedSensors);

            foreach (var sensor in _prioritizedSensors)
            {
                if (!sensor.HasAttribute(Sensor.Attribute.ForInfoOnly) && sensor.WiseName == sensorName)
                {
                    return JsonConvert.SerializeObject(sensor);
                }
            }

            return $"unknown sensor \"{sensorName}\"!";
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw})", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");

            if (string.Equals(command, "ready", StringComparison.OrdinalIgnoreCase))
            {
                return IsReady(toBeIgnored: Sensor.Attribute.None);
            }
            else
            {
                Exceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw})", "Not implemented");
                return false;
            }
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");

            return string.Equals(command, "unsafereasons", StringComparison.OrdinalIgnoreCase)
                ? Action("unsafereasons", string.Empty)
                : SafetyCommandAsString(command, raw);
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
                    StartSensors();
                else
                    StopSensors();

                ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.WiseSafeToOperate, _connected, line: ActivityMonitor.Tracer.safety.Line));
            }
        }

        public static void StopSensors()
        {
            foreach (Sensor s in _prioritizedSensors)
            {
                s.Enabled = false;
                s.Connected = false;
            }
        }

        public static void StartSensors()
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
                return String.Format(CultureInfo.InvariantCulture, $"{version.Major}.{version.Minor}");
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

        public static string GenericUnsafeReason(Sensor s, Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
        {
            if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                return null;

            if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                return null;    // not enabled

            if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                return null;    // bypassed

            if ((toBeIgnored == Sensor.Attribute.Wise40Specific) && s.HasAttribute(toBeIgnored) && s.StateIsNotSet(Sensor.State.EnoughReadings))
                return null;    // the sensor is to be ignored from the unsafereasons

            string reason = $"{s.WiseName} - ";

            if (!s.IsSafe)
            {
                if (s.HasAttribute(Sensor.Attribute.SingleReading))
                {
                    reason += s.UnsafeReason();
                }
                else
                {
                    if (s.StateIsSet(Sensor.State.Stabilizing))
                    {
                        // cummulative and stabilizing
                        reason += $"({s.FormatSymbolic(s.LatestReading.value)}) stabilizing in {s.TimeToStable.ToMinimalString(showMillis: false)}";
                    }
                    else if (!s.StateIsSet(Sensor.State.EnoughReadings))
                    {
                        // cummulative and not ready
                        reason += $"not ready (only {s._nreadings} of {s._repeats} readings)";
                    }
                }

                if (s.HasAttribute(Sensor.Attribute.CanBeStale) && s.StateIsSet(Sensor.State.Stale))
                {
                    // cummulative and stale
                    reason += $" ({s._nstale} stale readings)";
                }
            }

            return reason;
        }

        /// <summary>
        /// Returns a list of reasons as to why it is not currently safe to operate
        /// </summary>
        /// <param name="scope">The scope of the question</param>
        /// <returns></returns>
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

                if (s.DoesNotHaveAttribute(Sensor.Attribute.SingleReading) &&
                    s.StateIsNotSet(Sensor.State.EnoughReadings) &&
                    ((toBeIgnored & Sensor.Attribute.Wise40Specific) != 0))
                {
                    continue;   // this is a wise-wise query and this sensor is not ready, ignore it
                }

                if (toBeIgnored != Sensor.Attribute.None && s.HasAttribute(toBeIgnored))
                    continue;

                if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                    continue;   // not enabled

                if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                    continue;   // bypassed

                if (!s.IsSafe)
                {
                    string reason = GenericUnsafeReason(s, toBeIgnored);

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

        private string SafetyCommandAsString(string command, bool raw)
        {
            Const.TriStateStatus status;
            string msg = string.Empty;

            switch (command.ToLower())
            {
                case "humidity": status = IsSafeHumidity; break;
                case "wind": status = IsSafeWindSpeed; break;
                case "sun": status = IsSafeSunElevation; break;
                case "clouds": status = IsSafeCloudCover; break;
                case "rain": status = IsSafeRain; break;
                default:
                    status = Const.TriStateStatus.Error;
                    msg = $"invalid command(\"{command}\", {raw})";
                    break;
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

            return "unknown";
        }

        #endregion

        #region TriState Properties (for object)
        public Const.TriStateStatus IsSafeCloudCover
        {
            get
            {
                if (!cloudsSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return cloudsSensor.IsSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus IsSafeWindSpeed
        {
            get
            {
                if (!windSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return windSensor.IsSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus IsSafeHumidity
        {
            get
            {
                if (!humiditySensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return humiditySensor.IsSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus IsSafeRain
        {
            get
            {
                if (!rainSensor.StateIsSet(Sensor.State.EnoughReadings))
                    return Const.TriStateStatus.Warning;
                return rainSensor.IsSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus IsSafeSunElevation
        {
            get
            {
                double max = DateTime.Now.Hour < 12 ? Convert.ToDouble(sunSensor.MaxAtDawnAsString) : Convert.ToDouble(sunSensor.MaxAtDuskAsString);

                if (sunSensor.IsStale)
                    return Const.TriStateStatus.Warning;

                double elevation = sunSensor.sunElevation.Value;
                if (Double.IsNaN(elevation))
                    return Const.TriStateStatus.Warning;

                return elevation <= max ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }
        #endregion
        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                if (activityMonitor.ShuttingDown)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "IsSafe: false # shutting down");
                    #endregion
                    return false;
                }

                bool ret = IsSafeWithoutCheckingForShutdown();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"IsSafe: {ret}");
                #endregion
                return ret;
            }
        }

        private bool WiseIsSafe
        {
            get
            {
                bool ret = IsSafeWithoutCheckingForShutdown(toBeIgnored: Sensor.Attribute.Wise40Specific);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"WiseIsSafe: {ret}");
                #endregion
                return ret;
            }
        }

        private readonly object _lock = new object();
        private Sensor unsafeSensor;

        public bool IsSafeWithoutCheckingForShutdown(Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
        {
            bool ret = true;
            _unsafeBecauseNotReady = false;

            Init();

            if (!_connected)
            {
                ret = false;
                goto Out;
            }

            unsafeSensor = null;
            lock (_lock)
            {
                foreach (Sensor s in _prioritizedSensors)
                {
                    if (s.HasAttribute(Sensor.Attribute.ForInfoOnly))
                        continue;

                    if (!s.HasAttribute(Sensor.Attribute.AlwaysEnabled) && !s.StateIsSet(Sensor.State.Enabled))
                        continue;

                    if (_bypassed && s.HasAttribute(Sensor.Attribute.CanBeBypassed))
                        continue;

                    if (toBeIgnored != Sensor.Attribute.None && s.HasAttribute(toBeIgnored))
                    {
                        if (s == humanInterventionSensor && !(s as HumanInterventionSensor).CampusGlobal)
                            continue;
                    }

                    if (!s.IsSafe)
                    {
                        if (!s.HasAttribute(Sensor.Attribute.SingleReading))
                        {
                            if (s.StateIsNotSet(Sensor.State.EnoughReadings))
                                _unsafeBecauseNotReady = true;

                            if (toBeIgnored == Sensor.Attribute.Wise40Specific && s.HasAttribute(Sensor.Attribute.Wise40Specific))
                            {
                                continue;
                            }
                        }

                        ret = false;    // The first non-safe cumulative sensor forces NOT SAFE
                        unsafeSensor = s;
                        goto Out;
                    }
                }
            }

        Out:
            //if (ret == true && unsafeSensor == null)    // ret was true at start but no unsafeSensor was found
            //    ret = false;

            Event.SafetyEvent.SafetyState currentSafetyState = (ret) ?
                Event.SafetyEvent.SafetyState.Safe :
                Event.SafetyEvent.SafetyState.Unsafe;

            if (currentSafetyState != _safetyState)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"changed from {_safetyState} to {currentSafetyState}");
                #endregion
                _safetyState = currentSafetyState;
                ActivityMonitor.Event(new Event.SafetyEvent(_safetyState,
                    currentSafetyState == Event.SafetyEvent.SafetyState.Unsafe ? UnsafeReasons.Replace(Const.recordSeparator, "\n") : ""));
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"IsSafeWithoutCheckingForShutdown: {ret}");
            #endregion
            return ret;
        }

        #endregion

        public static bool IsReady(Sensor.Attribute toBeIgnored = Sensor.Attribute.None)
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
                Exceptor.Throw<ASCOM.NotConnectedException>("CheckConnected", message);
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
                s.ReadProfile();

            using (Profile driverProfile = new Profile())
            {
                const string telescopeDriverId = Const.WiseDriverID.Telescope;

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
                s.WriteProfile();
        }
        #endregion

        private string RawWeatherData
        {
            get
            {
                RawWeatherData ret = new RawWeatherData
                {
                    Boltwood = JsonConvert.DeserializeObject<List<BoltwoodStation.RawData>>(WiseSite.och.Action("//Wise40.Boltwood:raw-data", "")),
                    VantagePro2 = JsonConvert.DeserializeObject<VantagePro2StationRawData>(WiseSite.och.Action("//Wise40.VantagePro2:raw-data", "")),
                    TessW = JsonConvert.DeserializeObject<TessWStationRawData>(tessw.Action("raw-data", "")),
                    OWL = WiseSafeToOperate.owlRefresher.Digest() as OWLRefresher.OWLDigest,
                    ARDO = ardoSensor.Digest() as ARDORawData,
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
        public bool HumanInterventionCampusGlobal;

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
        public VantagePro2StationRawData VantagePro2;
        public TessWStationRawData TessW;
        public OWLRefresher.OWLDigest OWL;
        public ARDORawData ARDO;
    }
}
