using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using Newtonsoft.Json;

namespace ASCOM.Wise40SafeToOperate
{
    public abstract class Sensor : WiseObject
    {
        private readonly System.Threading.Timer _timer;  // for this specific sensor instance
        private DateTime _endOfStabilization;
        private readonly object _lock = new object();
        protected static ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        private readonly string _propertyName;
        private bool _firstTime = true;

        [Flags]
        public enum Attribute
        {
            None = 0,
            SingleReading = (1 << 0),   // Decision is based on an immediate read of the sensor
                                        // Multiple-readings sensors
                                        // - Are ready ONLY after _repeats readings have been accumulated
                                        // - Not Safe while not ready
                                        // - Once transited from unsafe to safe, must stabilize
                                        // - Readings may contain stale data
            AlwaysEnabled = (1 << 1),   // Cannot be disabled
            CanBeStale = (1 << 2),      // Reading the sensor may produce stale data
            CanBeBypassed = (1 << 3),   // By the Safety Bypass
            ForcesDecision = (1 << 4),  // If this sensor is not safe it forces SafeToOperate == false
            ForInfoOnly = (1 << 5),     // Will not affect the global isSafe state
            Wise40Specific = (1 << 6),  // Relevant only to the Wise40 observatory safety, not to other Wise observatories
            Periodic = (1 << 7),        // Needs to be read periodically (may not accumulate readings)
        };

        [Flags]
        public enum State
        {
            None = 0,
            EnoughReadings = (1 << 1),  // has enough readings to decide if safe or not
            Safe = (1 << 2),            // at least one reading was safe
            Stabilizing = (1 << 3),     // in transition from unsafe to safe
            Stale = (1 << 4),           // the data readings are too old
            Enabled = (1 << 5),         // It is not AlwaysEnabled and was enabled
            Connected = (1 << 6),
        };

        public bool StateIsSet(State s)
        {
            bool ret;

            lock (_lock)
            {
                ret = (_state & s) != 0;
            }
            return ret;
        }

        public bool StateIsNotSet(State s)
        {
            return !StateIsSet(s);
        }

        public void SetState(State s)
        {
            if (!StateIsSet(s))
                _state |= s;
        }

        public void UnsetState(State s)
        {
            if (StateIsSet(s))
                _state &= ~s;
        }

        public Attribute _attributes;
        public State _state;
        public int _repeats;
        public int _nbad;
        public int _nstale;
        public bool _enabled;
        public bool _connected;
        public int _nreadings;
        public Units _units;
        public string _valueFormatterString;
        private Event.SafetyEvent.SensorState _sensorState = Event.SafetyEvent.SensorState.NotSet;
        private Reading _latestReading = null;

        private int _busy = 0;

        [FlagsAttribute] public enum Usability
        {
            None = 0,
            Usable = (1 << 0),
            Stale = (1 << 1),
            Safe = (1 << 2),
        }

        public string FormatSymbolic(double value)
        {
            if (Double.IsNaN(value))
                return Const.noValue;

            if (string.IsNullOrEmpty(_valueFormatterString))
                return string.Empty;

            string ret = value.ToString(_valueFormatterString);
            if (!string.IsNullOrEmpty(_units.symbolic))
                ret += _units.symbolic;
            return ret;
        }

        public string FormatVerbal(double value)
        {
            if (Double.IsNaN(value))
                return Const.noValue;

            if (string.IsNullOrEmpty(_valueFormatterString))
                return string.Empty;

            string ret = value.ToString(_valueFormatterString);
            if (!string.IsNullOrEmpty(_units.verbal))
                ret += _units.verbal;
            return ret;
        }

        public class SensorDigest
        {
            public State State;
            public Reading LatestReading;
            public string ToolTip;
            public string Name;

            public SensorDigest() { }
            static SensorDigest() { }

            public static SensorDigest FromSensor(Sensor sensor)
            {
                Reading latestReading = sensor.LatestReading;
                SensorDigest digest = new SensorDigest
                {
                    Name = sensor.WiseName,
                    State = sensor._state,
                    LatestReading = latestReading,
                    ToolTip = sensor.ToolTip,
                    Safe = sensor.IsSafe,
                    Stale = sensor.IsStale,
                    Symbolic = sensor.FormatSymbolic(latestReading.value),
                    Verbal = sensor.FormatVerbal(latestReading.value),
                    AffectsSafety = !sensor.HasAttribute(Attribute.ForInfoOnly)
                };

                return digest;
            }

            public static SensorDigest FromOCHProperty(string property)
            {
                SensorDigest digest = new SensorDigest();
                double v, s;

                digest.Safe = true;
                digest.Name = property;
                switch (property)
                {
                    case "WindDirection":
                        v = WiseSite.och.WindDirection;
                        digest.Symbolic = $"{v}°";
                        digest.Verbal = $"{v} deg";
                        break;

                    case "DewPoint":
                        v = WiseSite.och.DewPoint;
                        digest.Symbolic = $"{v}°C";
                        digest.Verbal = $"{v} deg";
                        break;

                    case "SkyTemperature":
                        v = WiseSite.och.SkyTemperature;
                        digest.Symbolic = $"{v}°C";
                        digest.Verbal = $"{v} deg";
                        break;

                    default:
                        return null;
                }

                s = WiseSite.och.TimeSinceLastUpdate(property);
                digest.Stale = TooOld(s);
                digest.AffectsSafety = false;
                digest.ToolTip = "Does not affect safety";

                digest.LatestReading = new Reading
                {
                    value = v,
                    Safe = true,
                    Stale = TooOld(s),
                    Usable = true,
                    secondsSinceLastUpdate = s,
                    timeOfLastUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(s)),
                };
                return digest;
            }

            public bool AffectsSafety { get; set; }

            public string Symbolic { get; set; }

            public string Verbal { get; set; }

            public bool Stale { get; set; }

            public System.Drawing.Color Color
            {
                get
                {
                    Const.TriStateStatus triState;

                    if (Stale)
                    {
                        triState = Const.TriStateStatus.Warning;
                    }
                    else
                    {
                        if (AffectsSafety)
                            triState = Safe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
                        else
                            triState = Const.TriStateStatus.Normal;
                    }
                    return Statuser.TriStateColor(triState);
                }
            }

            public bool Safe { get; set; }
        }

        public string ToolTip
        {
            get
            {
                if (IsSafe)
                {
                    return "";
                }
                else
                {
                    if (HasAttribute(Attribute.ForInfoOnly))
                        return "Does not affect safety";
                    else
                        return WiseSafeToOperate.GenericUnsafeReason(this);
                }
            }
        }

        public class Reading
        {
            public double value;
            public Usability usability;
            public double secondsSinceLastUpdate;
            public DateTime timeOfLastUpdate;

            public Reading()
            {
                value = double.NaN;
                usability = Usability.None;
            }

            public override string ToString()
            {
                if (Stale)
                    return "stale";
                else if (Safe)
                    return "safe";
                else
                    return "not-safe";
            }

            public bool Usable
            {
                get
                {
                    return (usability & Usability.Usable) != 0;
                }

                set
                {
                    if (value)
                        usability |= Usability.Usable;
                    else
                        usability &= ~Usability.Usable;
                }
            }

            public bool Stale
            {
                get
                {
                    return (usability & Usability.Stale) != 0;
                }

                set
                {
                    if (value)
                        usability |= Usability.Stale;
                    else
                        usability &= ~Usability.Stale;
                }
            }

            public bool Safe
            {
                get
                {
                    return (usability & Usability.Safe) != 0;
                }

                set
                {
                    if (value)
                        usability |= Usability.Safe;
                    else
                        usability &= ~Usability.Safe;
                }
            }
        }

        protected FixedSizedQueue<Reading> _readings;
        protected static Debugger debugger = Debugger.Instance;

        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.Instance;

        protected Sensor(
            string name,
            Attribute attributes,
            string symbolicUnits,
            string verbalUnits,
            string formatValue,
            string propertyName,
            WiseSafeToOperate instance)
        {
            wisesafetooperate = instance;
            WiseName = name;
            _attributes = attributes;

            if (HasAttribute(Attribute.Periodic))
                _timer = new System.Threading.Timer(new TimerCallback(OnTimer), this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (HasAttribute(Attribute.AlwaysEnabled))
                Enabled = true;

            _state = State.None;
            _units = new Units { symbolic = symbolicUnits, verbal = verbalUnits };
            _valueFormatterString = formatValue;
            _propertyName = propertyName;

            Restart();

            ActivityMonitor.Event(new Event.SafetyEvent(
                sensor: WiseName,
                details: "Created",
                before: _sensorState,
                after: Event.SafetyEvent.SensorState.Init));
            _sensorState = Event.SafetyEvent.SensorState.Init;
        }

        public TimeSpan Interval { get; set; }

        public bool HasAttribute(Attribute attr)
        {
            return (_attributes & attr) != 0;
        }

        public void SetAttributes(Attribute attrs)
        {
            _attributes |= attrs;
        }

        public void UnsetAttributes(Attribute attrs)
        {
            _attributes &= ~(attrs);
        }

        public bool DoesNotHaveAttribute(Attribute attr)
        {
            return !HasAttribute(attr);
        }

        public Reading LatestReading
        {
            get
            {
                if (HasAttribute(Attribute.SingleReading))
                    return _latestReading;

                if (_readings == null)
                    return new Reading();

                var arr = _readings.ToArray();
                if (arr.Length == 0)
                    return new Reading();

                return arr[arr.Length - 1];
            }
        }

        #region ASCOM Profile
        public void ReadProfile()
        {
            int defaultInterval = 60, defaultRepeats = -1;

            switch (WiseName)
            {
                case "Wind": defaultRepeats = 3; break;
                case "Sun": defaultRepeats = 1; break;
                case "HumanIntervention": defaultRepeats = 1; break;
                case "Rain": defaultRepeats = 2; break;
                case "Clouds": defaultRepeats = 3; break;
                case "Humidity": defaultRepeats = 4; break;
                case "Pressure": defaultRepeats = 3; break;
                case "Temperature": defaultRepeats = 3; break;
            }

            if (HasAttribute(Attribute.Periodic))
            {
                int fromProfile = Convert.ToInt32(wisesafetooperate._profile.GetValue(
                    Const.WiseDriverID.SafeToOperate, WiseName, "Interval", defaultInterval.ToString()));

                Interval = TimeSpan.FromSeconds(fromProfile);
            }

            _repeats = HasAttribute(Attribute.SingleReading)
                ? 1
                : Convert.ToInt32(wisesafetooperate._profile.GetValue(
                    Const.WiseDriverID.SafeToOperate, WiseName, "Repeats", defaultRepeats.ToString()));

            if (HasAttribute(Attribute.AlwaysEnabled))
                Enabled = true;
            else
                Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Enabled", true.ToString()));

            if (Enabled && !HasAttribute(Attribute.SingleReading) && _repeats > 0)
                _readings = new FixedSizedQueue<Reading>(_repeats);

            ReadSensorProfile();
        }

        public void WriteProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, $"{Interval.TotalSeconds}", "Interval");

            if (DoesNotHaveAttribute(Attribute.SingleReading))
                wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, _repeats.ToString(), "Repeats");

            if (DoesNotHaveAttribute(Attribute.AlwaysEnabled))
                wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, Enabled.ToString(), "Enabled");

            WriteSensorProfile();
        }
        #endregion

        public abstract string UnsafeReason();
        public abstract void ReadSensorProfile();
        public abstract void WriteSensorProfile();
        public abstract Reading GetReading();
        public abstract string MaxAsString { get; set; }
        public abstract object Digest();
        public abstract string Status { get; }

        public double SecondsSinceLastUpdate
        {
            get
            {
                if (DoesNotHaveAttribute(Attribute.CanBeStale))
                    return double.NaN;

                return WiseSite.och.TimeSinceLastUpdate(_propertyName);
            }
        }

        public static bool TooOld(double seconds)
        {
            return seconds > WiseSafeToOperate.ageMaxSeconds;
        }

        public bool IsStale
        {
            get
            {
                if (DoesNotHaveAttribute(Attribute.CanBeStale))
                    return false;

                if (StateIsSet(State.EnoughReadings) && TooOld(SecondsSinceLastUpdate))
                {
                    SetState(State.Stale);
                    return true;
                }

                UnsetState(State.Stale);
                return false;
            }
        }

        public void Restart()
        {
            _state = new State();
            _nbad = 0;
            _nreadings = 0;
            _nstale = 0;

            ReadProfile();
        }

        //
        // The timer is enabled ONLY on non-Immediate sensors
        //
        private void OnTimer(object StateObject)
        {
            Sensor thisSensor = StateObject as Sensor;

            if (!Enabled || !Connected)
                return;

            if (Interlocked.CompareExchange(ref _busy, 1, 0) == 1)
                return;

            if (_firstTime)
            {
                Restore();
                _firstTime = false;
            }

            #region debug
            string op = $"Sensor({WiseName}):onTimer: attrs: [{_attributes}]";
            
            if (HasAttribute(Attribute.Periodic))
                op += $", every: {Interval.ToMinimalString()}";

            string dbgDetails = null;
            #endregion

            DateTime now = DateTime.Now;
            try
            {
                _latestReading = GetReading();
            }
            catch (Exception ex)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, op + $"Caught {ex.Message} at\n{ex.StackTrace}");
                goto RestartTimer;
            }
            if (_latestReading == null)
                goto RestartTimer;

            bool wassafe = StateIsSet(State.Safe);

            if (HasAttribute(Attribute.SingleReading))
            {
                Event.SafetyEvent.SensorState prevState = _sensorState, newState;
                bool issafe;

                if (_latestReading.Safe)
                {
                    SetState(State.Safe);
                    newState = Event.SafetyEvent.SensorState.Safe;
                    issafe = true;
                }
                else
                {
                    UnsetState(State.Safe);
                    newState = Event.SafetyEvent.SensorState.NotSafe;
                    issafe = false;
                }

                if (wassafe != issafe)
                    ActivityMonitor.Event(new Event.SafetyEvent(
                        sensor: WiseName,
                        details: $"Became {(issafe ? "safe" : "unsafe")}",
                        before: prevState,
                        after: newState));

                _sensorState = newState;
                goto RestartTimer;
            }

            //
            // Multiple readings sensors
            //
            bool wasready = StateIsSet(State.EnoughReadings);
            if (_readings == null)
                _readings = new FixedSizedQueue<Reading>(_nreadings);

            #region debug
            Reading[] arr = _readings.ToArray();
            List<string> prevValues, newValues;
            int nbad, nstale, nreadings;

            prevValues = new List<string>();
            foreach (Reading r in arr)      // before current reading
                prevValues.Add($"{r}");
            #endregion

            _readings.Enqueue(_latestReading);

            arr = _readings.ToArray();
            #region debug
            newValues = new List<string>();
            #endregion
            nbad = 0; nstale = 0; nreadings = 0;
            foreach (Reading r in arr)      // including current reading
            {
                #region debug
                newValues.Add($"{r}");
                #endregion
                nreadings++;
                if (!r.Safe)
                    nbad++;
                if (r.Stale)
                    nstale++;
            }

            #region debug
            dbgDetails = $", readings before: [{String.Join(",", prevValues)}] => after: [{String.Join(",", newValues)}]";
            #endregion

            _nreadings = nreadings;
            _nbad = nbad;
            _nstale = nstale;

            UnsetState(State.Safe);

            if (HasAttribute(Attribute.CanBeStale) && (_nstale > 0))
            {
                ExtendUnsafety($"{_nstale} stale reading{(_nstale > 1 ? "s" : "")}");
                SetState(State.Stale);
                goto SaveAndRestartTimer;  // Remain unsafe - at least one stale reading
            }
            else
            {
                UnsetState(State.Stale);
            }

            if (_readings.ToArray().Length != _repeats)
            {
                UnsetState(State.EnoughReadings);
                goto SaveAndRestartTimer;  // Remain unsafe - not enough readings
            }
            else
            {
                if (!StateIsSet(State.EnoughReadings))
                {
                    ActivityMonitor.Event(new Event.SafetyEvent(
                        sensor: WiseName,
                        details: "Became ready",
                        before: _sensorState,
                        after: Event.SafetyEvent.SensorState.Ready));
                    _sensorState = Event.SafetyEvent.SensorState.Ready;
                }
                SetState(State.EnoughReadings);
            }

            if (_nbad == _repeats)
            {
                ExtendUnsafety("All readings are unsafe");
                if (wasready && wassafe)
                {
                    ActivityMonitor.Event(new Event.SafetyEvent(
                        sensor: WiseName,
                        details: "Became unsafe",
                        before: _sensorState,
                        after: Event.SafetyEvent.SensorState.NotSafe));
                }
                _sensorState = Event.SafetyEvent.SensorState.NotSafe;
                goto SaveAndRestartTimer; // Remain unsafe - all readings are unsafe
            }

            bool prolong = true;
            if (StateIsSet(State.Stabilizing)) {
                if (_latestReading.Stale || !_latestReading.Safe)
                {
                    ExtendUnsafety("Unsafe reading while stabilizing");
                    goto RestartTimer;
                }

                if (now.CompareTo(_endOfStabilization) <= 0)
                {
                    goto RestartTimer;
                }
                else
                {
                    UnsetState(State.Stabilizing);
                    _endOfStabilization = DateTime.MinValue;
                    prolong = false;    // don't prolong if just ended stabilization
                }
            }

            // If we got here the sensor is currently safe
            if (wasready && StateIsSet(State.EnoughReadings) && !wassafe && prolong)
            {
                ExtendUnsafety("Readings just turned safe");
                goto SaveAndRestartTimer;
            }

            SetState(State.Safe);
            if (wasready && !wassafe)
            {
                ActivityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: "Became safe",
                    before: _sensorState,
                    after: Event.SafetyEvent.SensorState.Safe));
            }

            _sensorState = Event.SafetyEvent.SensorState.Safe;

        SaveAndRestartTimer:
            Save();
        RestartTimer:
            #region debug

            if (HasAttribute(Attribute.SingleReading))
                dbgDetails += $", {(_latestReading == null ? "not-ready" : "ready")}";
            else
            {
                dbgDetails += $", {(StateIsSet(State.EnoughReadings) ? "ready" : "not-ready")}";
                dbgDetails += $", {(StateIsSet(State.Stabilizing) ? "stabilizing" : "not-stabilizing")}";
            }
            dbgDetails += $", {(StateIsSet(State.Safe) ? "safe" : "not-safe")}";

            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"{op}{dbgDetails}");
            #endregion
            if (HasAttribute(Attribute.Periodic))
                _timer.Change(Interval, Timeout.InfiniteTimeSpan);

            Interlocked.Exchange(ref _busy, 0);
        }

        private void ExtendUnsafety(string reason)
        {
            SetState(State.Stabilizing);
            _endOfStabilization = DateTime.Now + WiseSafeToOperate._stabilizationPeriod;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                $"ExtendUnsafety: Sensor ({WiseName}) will stabilize at {_endOfStabilization.ToUniversalTime().ToShortTimeString()} (reason: {reason})");
            #endregion
        }

        public TimeSpan TimeToStable
        {
            get
            {
                if (HasAttribute(Attribute.SingleReading))
                    return TimeSpan.Zero;

                return StateIsSet(State.Stabilizing) ?
                    _endOfStabilization - DateTime.Now :
                    TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Safe unless last _repeats were unsafe
        /// </summary>
        public bool IsSafe
        {
            get
            {
                bool ret;

                if (!Enabled)
                {
                    ret = true;
                    #region debug
                    //debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"Sensor ({WiseName}), isSafe: {ret} (not enabled)");
                    #endregion
                    return ret;
                }

                if (HasAttribute(Attribute.SingleReading))
                {
                    ret = _latestReading != null && _latestReading.Safe;
                    #region debug
                    //debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"Sensor ({WiseName}), (SingleReading) isSafe: {ret}");
                    #endregion
                }
                else
                {
                    ret = StateIsSet(State.Safe);
                    #region debug
                    //debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                    //    $"Sensor ({WiseName}), (cumulative) isSafe: {ret} (state: {_state})");
                    #endregion
                }
                return ret;
            }
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                _connected = value;
                if (_connected)
                {
                    SetState(State.Connected);
                } else
                {
                    UnsetState(State.Connected);
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
        }

        public bool Enabled
        {
            get
            {
                if (HasAttribute(Attribute.AlwaysEnabled))
                    return true;

                return _enabled;
            }

            set
            {
                _enabled = value;
                if (_enabled)
                {
                    SetState(State.Enabled);
                    _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    UnsetState(State.Enabled);
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
        }

        public void Save()
        {
            if (HasAttribute(Attribute.SingleReading))
                return;

            SavedSensorState saved = new SavedSensorState()
            {
                TimeOfSave = DateTime.Now,
                State = this._state,
                Readings = _readings.ToArray(),
                EndOfStabilization = _endOfStabilization,
            };

            if (!Directory.Exists(SavedSensorStateDir))
                Directory.CreateDirectory(SavedSensorStateDir);

            try
            {
                using (StreamWriter sw = File.CreateText(SavedSensorStateFile))
                {
                    JsonSerializer js = new JsonSerializer();
                    js.Serialize(sw, saved);
                }
            }
            catch { }
        }

        private void Restore()
        {
            if (HasAttribute(Attribute.SingleReading))
                return;

            SavedSensorState saved = null;
            string file = SavedSensorStateFile;
            string op = $"Restore({WiseName}) from \"{file}\": ";

            if (!File.Exists(file))
                return;

            using (StreamReader sr = new StreamReader(file))
            {
                JsonSerializer js = new JsonSerializer();
                try
                {
                    saved = (SavedSensorState)js.Deserialize(sr, typeof(SavedSensorState));
                }
                catch (Exception ex)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, op + $"Caught {ex.Message} at\n{ex.StackTrace}");
                }
            }

            DateTime now = DateTime.Now;
            TimeSpan age = now.Subtract(saved.TimeOfSave);
            if (age <= TimeSpan.FromMinutes(5))
            {
                _state = saved.State;
                _endOfStabilization = saved.EndOfStabilization;
                foreach (Reading r in saved.Readings.ToArray())
                {
                    r.timeOfLastUpdate += age;
                    r.secondsSinceLastUpdate = now.Subtract(r.timeOfLastUpdate).TotalSeconds;
                    _readings.Enqueue(r);
                    _nreadings++;
                    if (!r.Safe)
                        _nbad++;
                    if (r.Stale)
                        _nstale++;
                }
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, op +
                    $"OK (_state: {_state}, _nreadings: {_nreadings}, _nbad: {_nbad}, _nstale: {_nstale}, safe: {IsSafe}, stale: {IsStale}, unsafeReason: {UnsafeReason()})");
            }
            else
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, op + $"data older than {Interval.ToMinimalString()}, ignored");
            }
            File.Delete(file);
        }

        private string SavedSensorStateFile
        {
            get
            {
                return SavedSensorStateDir + "/" + WiseName + ".json";
            }
        }

        private string SavedSensorStateDir {
            get
            {
                return Const.topWise40Directory + "/Sensors";
            }
        }
    }

    public class SavedSensorState
    {
        public DateTime TimeOfSave;
        public Sensor.State State;
        public Sensor.Reading[] Readings;
        public DateTime EndOfStabilization;
    }

    public class Units
    {
        public string symbolic;
        public string verbal;
    }
}
