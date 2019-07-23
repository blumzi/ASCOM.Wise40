using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public abstract class Sensor : WiseObject
    {
        private System.Threading.Timer _timer;  // for this specific sensor instance
        private DateTime _endOfStabilization;
        private object _lock = new object();
        protected static ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        private string _propertyName;

        [Flags]
        public enum Attribute
        {
            None = 0,
            Immediate = (1 << 0),       // Decision is based on an immediate read of the sensor
                                        // Non-immediate sensors
                                        // - Are ready ONLY after _repeats readings have been accumulated
                                        // - Not Safe while not ready
                                        // - Once transited from unsafe to safe, must stabilize
                                        // - Readings may contain stale data
            AlwaysEnabled = (1 << 1),   // Cannot be disabled
            CanBeStale = (1 << 2),      // Reading the sensor may produce stale data
            CanBeBypassed = (1 << 3),   // By the Safety Bypass
            ForcesDecision = (1 << 4),  // If this sensor is not safe it forces SafeToOperate == false
            ForInfoOnly = (1 << 5),     // Will not affect the global isSafe state
            Wise40Specific = ( 1 << 6), // Relevant only to the Wise40 observatory safety, not to other Wise observatories
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
        public int _intervalMillis;
        public int _repeats;
        public int _nbad;
        public int _nstale;
        public bool _enabled;
        public bool _connected;
        public int _nreadings;
        public Units _units;
        public string _formatValue;
        Event.SafetyEvent.SensorState sensorState = Event.SafetyEvent.SensorState.NotSet;

        [FlagsAttribute] public enum Usability
        {
            None = 0,
            Usable = (1 << 0),
            Stale = (1 << 1),
            Safe = (1 << 2),
        }

        public class Units
        {
            public string symbolic;
            public string verbal;
        }

        public string FormatSymbolic(double value)
        {
            if (_formatValue == string.Empty)
                return string.Empty;

            string ret = LatestReading.value.ToString(_formatValue);
            if (_units.symbolic != string.Empty)
                ret += _units.symbolic;
            return ret;
        }

        public string FormatVerbal(double value)
        {
            if (_formatValue == string.Empty)
                return string.Empty;

            string ret = value.ToString(_formatValue);
            if (_units.verbal != string.Empty)
                ret += _units.verbal;
            return ret;
        }

        public class SensorDigest
        {
            public State State;
            public Reading LatestReading;
            public string ToolTip;
            private string _symbolic;
            private string _verbal;
            private bool _safe;
            private bool _stale;
            private bool _affectsSafety;
            public string Name;

            public SensorDigest() { }
            static SensorDigest() { }

            public static SensorDigest FromSensor(Sensor sensor)
            {
                SensorDigest digest = new SensorDigest();

                digest.Name = sensor.WiseName;
                digest.State = sensor._state;
                digest.LatestReading = sensor.LatestReading;
                digest.ToolTip = sensor.ToolTip;
                digest.Safe = sensor.isSafe;
                digest.Stale = sensor.IsStale;
                digest.Symbolic = sensor.FormatSymbolic(sensor.LatestReading.value);
                digest.Verbal = sensor.FormatVerbal(sensor.LatestReading.value);
                digest.AffectsSafety = !sensor.HasAttribute(Attribute.ForInfoOnly);

                return digest;
            }

            public static SensorDigest FromOCHProperty(string property)
            {
                SensorDigest digest = new SensorDigest();
                double v = 0, s = 0;

                digest.Safe = true;
                digest.Name = property;
                switch (property)
                {
                    case "WindDirection":
                        v = WiseSite.och.WindDirection;
                        s = WiseSite.och.TimeSinceLastUpdate(property);
                        digest.Symbolic = string.Format("{0}°", v);
                        digest.Verbal = string.Format("{0} deg", v);
                        digest.AffectsSafety = false;
                        digest.Stale = TooOld(s);
                        break;

                    case "DewPoint":
                        v = WiseSite.och.DewPoint;
                        s = WiseSite.och.TimeSinceLastUpdate(property);
                        digest.Symbolic = string.Format("{0}°C", v);
                        digest.Verbal = string.Format("{0} deg", v);
                        digest.AffectsSafety = false;
                        digest.Stale = TooOld(s);
                        break;

                    case "SkyTemperature":
                        v = WiseSite.och.SkyTemperature;
                        s = WiseSite.och.TimeSinceLastUpdate(property);
                        digest.Symbolic = string.Format("{0}°C", v);
                        digest.Verbal = string.Format("{0} deg", v);
                        digest.AffectsSafety = false;
                        digest.Stale = TooOld(s);
                        break;

                    default:
                        return null;
                }

                digest.ToolTip = "";

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

            public bool AffectsSafety
            {
                get
                {
                    return _affectsSafety;
                }

                set
                {
                    _affectsSafety = value;
                }
            }

            public string Symbolic
            {
                get
                {
                    return _symbolic;
                }

                set
                {
                    _symbolic = value;
                }
            }

            public string Verbal
            {
                get
                {
                    return _verbal;
                }

                set
                {
                    _verbal = value;
                }
            }

            public bool Stale
            {
                get
                {
                    return _stale;
                }

                set
                {
                    _stale = value;
                }
            }

            public System.Drawing.Color Color
            {
                get
                {
                    Const.TriStateStatus triState = Const.TriStateStatus.Normal;
                    if (Stale)
                        triState = Const.TriStateStatus.Warning;
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

            public bool Safe
            {
                get
                {
                    return _safe;
                }

                set
                {
                    _safe = value;
                }
            }
        }

        public string ToolTip
        {
            get
            {
                return isSafe ? "" : WiseSafeToOperate.GenericUnsafeReason(this);
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

        protected Sensor(string name, Attribute attributes, string symbolicUnits, string verbalUnits, string formatValue, string propertyName, WiseSafeToOperate instance)
        {
            wisesafetooperate = instance;
            WiseName = name;
            _attributes = attributes;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            if (HasAttribute(Attribute.AlwaysEnabled))
                Enabled = true;
            _state = State.None;
            _units = new Units { symbolic = symbolicUnits, verbal = verbalUnits };
            _formatValue = formatValue;
            _propertyName = propertyName;

            Restart(5000);
            activityMonitor.Event(new Event.SafetyEvent(
                sensor: WiseName,
                details: "Created",
                before: sensorState,
                after: Event.SafetyEvent.SensorState.Init));
            sensorState = Event.SafetyEvent.SensorState.Init;
        }

        public bool HasAttribute(Attribute attr)
        {
            return (_attributes & attr) != 0;
        }

        public bool DoesNotHaveAttribute(Attribute attr)
        {
            return !HasAttribute(attr);
        }

        public Reading LatestReading
        {
            get
            {
                if (HasAttribute(Attribute.Immediate))
                    return getReading();

                if (_readings == null)
                    return new Reading();

                var arr = _readings.ToArray();
                if (arr.Count() == 0)
                    return new Reading();

                return arr[arr.Count() - 1];
            }
        }

        #region ASCOM Profile
        public void readProfile()
        {
            int defaultInterval = 0, defaultRepeats = 0;

            switch (WiseName)
            {
                case "Wind": defaultInterval = 60; defaultRepeats = 3; break;
                case "Sun": defaultInterval = 60; defaultRepeats = 1; break;
                case "HumanIntervention": defaultInterval = 0; defaultRepeats = 1; break;
                case "Rain": defaultInterval = 60; defaultRepeats = 2; break;
                case "Clouds": defaultInterval = 60; defaultRepeats = 3; break;
                case "Humidity": defaultInterval = 60; defaultRepeats = 4; break;
                case "Pressure": defaultInterval = 60; defaultRepeats = 3; break;
                case "Temperature": defaultInterval = 60; defaultRepeats = 3; break;
            }

            _intervalMillis = 1000 * Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Interval", defaultInterval.ToString()));
            if (DoesNotHaveAttribute(Attribute.Immediate))
                _repeats = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Repeats", defaultRepeats.ToString()));

            if (DoesNotHaveAttribute(Attribute.AlwaysEnabled))
                Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Enabled", true.ToString()));

            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, (_intervalMillis / 1000).ToString(), "Interval");

            if (DoesNotHaveAttribute(Attribute.Immediate))
                wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, _repeats.ToString(), "Repeats");

            if (DoesNotHaveAttribute(Attribute.AlwaysEnabled))
                wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, Enabled.ToString(), "Enabled");

            writeSensorProfile();
        }
        #endregion

        public abstract string reason();
        public abstract void readSensorProfile();
        public abstract void writeSensorProfile();
        public abstract Reading getReading();
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

        public void Restart(int dueMillis)
        {
            _state = new State();
            _nbad = 0;
            _nreadings = 0;
            _nstale = 0;

            readProfile();
            if (HasAttribute(Attribute.Immediate) && _timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                if (Enabled)
                {
                    if (_repeats > 1)
                    {
                        _readings = new FixedSizedQueue<Reading>(_repeats);
                        _timer.Change(dueMillis, _intervalMillis);
                    }
                    else
                        _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        //
        // The timer is enabled ONLY on non-Immediate sensors
        //
        private void onTimer(object StateObject)
        {
            if (!Enabled || !Connected)
                return;

            DateTime now = DateTime.Now;
            Reading currentReading = getReading();
            if (currentReading == null)
                return;

            if (_readings == null)
                _readings = new FixedSizedQueue<Reading>(_nreadings);
            Reading[] arr = _readings.ToArray();
            List<string> values = new List<string>();
            int nbad = 0, nstale = 0, nreadings = 0;
            foreach (Reading r in arr)      // before current reading
            {
                values.Add(r.ToString());
                nreadings++;
                if (!r.Safe)
                    nbad++;
                if (r.Stale)
                    nstale++;
            }
            State savedState = _state;
            bool wassafe = StateIsSet(State.Safe);
            bool wasready = StateIsSet(State.EnoughReadings);

            _readings.Enqueue(currentReading);

            arr = _readings.ToArray();
            values = new List<string>();
            nbad = 0;
            nstale = 0;
            nreadings = 0;
            foreach (Reading r in arr)      // including current reading
            {
                values.Add(r.ToString());
                nreadings++;
                if (!r.Safe)
                    nbad++;
                if (r.Stale)
                    nstale++;
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                "onTimer: Sensor ({0}) readings: [{1}]",
                WiseName, String.Join(",", values));

            _nreadings = nreadings;
            _nbad = nbad;
            _nstale = nstale;

            lock (_lock)
            {
                UnsetState(State.Safe);

                if (HasAttribute(Attribute.CanBeStale) && (_nstale > 0))
                {
                    ExtendUnsafety(string.Format("{0} stale reading{1}", _nstale, _nstale > 1 ? "s" : ""));
                    SetState(State.Stale);
                    return;     // Remain unsafe - at least one stale reading
                }
                else
                    UnsetState(State.Stale);

                if (_readings.ToArray().Count() != _repeats)
                {
                    UnsetState(State.EnoughReadings);
                    return;     // Remain unsafe - not enough readings
                }
                else
                {
                    if (!StateIsSet(State.EnoughReadings))
                    {
                        activityMonitor.Event(new Event.SafetyEvent(
                            sensor: WiseName,
                            details: "Became ready",
                            before: sensorState,
                            after: Event.SafetyEvent.SensorState.Ready));
                        sensorState = Event.SafetyEvent.SensorState.Ready;
                    }
                    SetState(State.EnoughReadings);
                }

                if (_nbad == _repeats)
                {
                    ExtendUnsafety("All readings are unsafe");
                    if (wasready && wassafe)
                        activityMonitor.Event(new Event.SafetyEvent(
                            sensor: WiseName,
                            details: "Became unsafe",
                            before: sensorState,
                            after: Event.SafetyEvent.SensorState.NotSafe));
                    sensorState = Event.SafetyEvent.SensorState.NotSafe;
                    return;     // Remain unsafe - all readings are unsafe
                }

                bool prolong = true;
                if (StateIsSet(State.Stabilizing)) {
                    if (currentReading.Stale || !currentReading.Safe)
                    {
                        ExtendUnsafety("Unsafe reading while stabilizing");
                        return;
                    }

                    if (now.CompareTo(_endOfStabilization) <= 0)
                        return;
                    else {
                        UnsetState(State.Stabilizing);
                        prolong = false;    // don't prolong if just ended stabilization
                    }
                }

                // If we got here the sensor is currently safe
                if (wasready && StateIsSet(State.EnoughReadings) && !wassafe && prolong)
                {
                    ExtendUnsafety("Readings just turned safe");
                    return;     // Remain unsafe - just begun stabilizing
                }

                SetState(State.Safe);
                if (wasready && !wassafe)
                    activityMonitor.Event(new Event.SafetyEvent(
                        sensor: WiseName,
                        details: "Became safe",
                        before: sensorState,
                        after: Event.SafetyEvent.SensorState.Safe));
                sensorState = Event.SafetyEvent.SensorState.Safe;
            }
        }

        private void ExtendUnsafety(string reason)
        {
            SetState(State.Stabilizing);
            _endOfStabilization = DateTime.Now.AddMilliseconds((int)WiseSafeToOperate._stabilizationPeriod.TotalMilliseconds);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                "ExtendUnsafety: Sensor ({0}) will stabilize at {1} (reason: {2})",
                WiseName, _endOfStabilization.ToUniversalTime().ToShortTimeString(), reason);
            #endregion
        }

        public TimeSpan TimeToStable
        {
            get
            {
                if (HasAttribute(Attribute.Immediate))
                    return TimeSpan.Zero;

                return StateIsSet(State.Stabilizing) ?
                    _endOfStabilization - DateTime.Now :
                    TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Safe unless last _repeats were unsafe
        /// </summary>
        public bool isSafe
        {
            get
            {
                bool ret;

                if (!Enabled)
                {
                    ret = true;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (not enabled)", WiseName, ret);
                    #endregion
                    return ret;
                }

                if (HasAttribute(Attribute.Immediate))
                {
                    ret = getReading().Safe;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        "Sensor ({0}), (immediate) isSafe: {1}",
                        WiseName, ret);
                    #endregion
                }
                else
                {

                    ret = StateIsSet(State.Safe);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        "Sensor ({0}), (cumulative) isSafe: {1} (state: {2})",
                        WiseName, ret, _state.ToString());
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
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
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
                    _timer.Change(0, _intervalMillis);
                }
                else
                {
                    UnsetState(State.Enabled);
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }
}
