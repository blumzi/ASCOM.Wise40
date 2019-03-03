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

        [Flags]
        public enum SensorAttribute
        {
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
        };

        [Flags]
        public enum SensorState
        {
            None = 0,
            EnoughReadings = (1 << 1),  // has enough readings to decide if safe or not
            Safe = (1 << 2),            // at least one reading was safe
            Stabilizing = (1 << 3),     // in transition from unsafe to safe
            Stale = (1 << 4),           // the data readings are too old
            Enabled = (1 << 5),         // It is not AlwaysEnabled and was enabled
        };

        public bool StateIsSet(SensorState s)
        {
            bool ret;

            lock (_lock)
            {
                ret = (_state & s) != 0;
            }
            return ret;
        }

        public bool StateIsNotSet(SensorState s)
        {
            return !StateIsSet(s);
        }

        public void SetState(SensorState s)
        {
            if (!StateIsSet(s))
                _state |= s;
        }

        public void UnsetState(SensorState s)
        {
            if (StateIsSet(s))
                _state &= ~s;
        }

        public SensorAttribute _attributes;
        public SensorState _state;
        public int _intervalMillis;
        public int _repeats;
        public int _nbad;
        public int _nstale;
        public bool _enabled;
        public int _nreadings;
        Event.SafetyEvent.SensorState sensorState = Event.SafetyEvent.SensorState.NotSet;

        public class Reading
        {
            public bool stale;
            public bool safe;
            public double value;
            public bool usable = false;

            public override string ToString()
            {
                return safe ? "safe" : "not-safe";
            }
        }

        protected FixedSizedQueue<Reading> _readings;
        protected static Debugger debugger = Debugger.Instance;

        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.Instance;

        protected Sensor(string name, SensorAttribute attributes, WiseSafeToOperate instance)
        {
            wisesafetooperate = instance;
            WiseName = name;
            _attributes = attributes;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            if (HasAttribute(SensorAttribute.AlwaysEnabled))
                Enabled = true;
            _state = SensorState.None;

            Restart(5000);
            activityMonitor.Event(new Event.SafetyEvent(
                sensor: WiseName,
                details: "Created",
                before: sensorState,
                after: Event.SafetyEvent.SensorState.Init));
            sensorState = Event.SafetyEvent.SensorState.Init;
        }

        public bool HasAttribute(SensorAttribute attr)
        {
            return (_attributes & attr) != 0;
        }

        public bool DoesNotHaveAttribute(SensorAttribute attr)
        {
            return !HasAttribute(attr);
        }

        public Reading LatestReading
        {
            get
            {
                if (_readings == null)
                    return new Reading
                    {
                        usable = false,
                    };

                var arr = _readings.ToArray();

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

            _intervalMillis = 1000 * Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, WiseName, "Interval", defaultInterval.ToString()));
            if (DoesNotHaveAttribute(SensorAttribute.Immediate))
                _repeats = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, WiseName, "Repeats", defaultRepeats.ToString()));

            if (DoesNotHaveAttribute(SensorAttribute.AlwaysEnabled))
                Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, WiseName, "Enabled", true.ToString()));

            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, WiseName, (_intervalMillis / 1000).ToString(), "Interval");

            if (DoesNotHaveAttribute(SensorAttribute.Immediate))
                wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, WiseName, _repeats.ToString(), "Repeats");

            if (DoesNotHaveAttribute(SensorAttribute.AlwaysEnabled))
                wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, WiseName, Enabled.ToString(), "Enabled");

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

        public bool IsStale(string propertyName)
        {
            if (DoesNotHaveAttribute(SensorAttribute.CanBeStale))
                return false;

            //if (!WiseSite.och.Connected)
            //    WiseSite.och.Connected = true;

            if (WiseSite.och.TimeSinceLastUpdate(propertyName) > WiseSafeToOperate.ageMaxSeconds)
            {
                SetState(SensorState.Stale);
                return true;
            }

            UnsetState(SensorState.Stale);
            return false;
        }

        public void Restart(int dueMillis)
        {
            _state = new SensorState();
            _nbad = 0;
            _nreadings = 0;
            _nstale = 0;

            readProfile();
            if (HasAttribute(SensorAttribute.Immediate) && _timer != null)
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
            if (!Enabled)
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
                if (!r.safe)
                    nbad++;
                if (r.stale)
                    nstale++;
            }
            SensorState savedState = _state;
            bool wassafe = StateIsSet(SensorState.Safe);
            bool wasready = StateIsSet(SensorState.EnoughReadings);

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
                if (!r.safe)
                    nbad++;
                if (r.stale)
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
                UnsetState(SensorState.Safe);

                if (HasAttribute(SensorAttribute.CanBeStale) && (_nstale > 0))
                {
                    ExtendUnsafety(string.Format("{0} stale reading{1}", _nstale, _nstale > 1 ? "s" : ""));
                    SetState(SensorState.Stale);
                    return;     // Remain unsafe - at least one stale reading
                }
                else
                    UnsetState(SensorState.Stale);

                if (_readings.ToArray().Count() != _repeats)
                {
                    UnsetState(SensorState.EnoughReadings);
                    return;     // Remain unsafe - not enough readings
                }
                else
                {
                    if (!StateIsSet(SensorState.EnoughReadings))
                    {
                        activityMonitor.Event(new Event.SafetyEvent(
                            sensor: WiseName,
                            details: "Became ready",
                            before: sensorState,
                            after: Event.SafetyEvent.SensorState.Ready));
                        sensorState = Event.SafetyEvent.SensorState.Ready;
                    }
                    SetState(SensorState.EnoughReadings);
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
                if (StateIsSet(SensorState.Stabilizing)) {
                    if (currentReading.stale || !currentReading.safe)
                    {
                        ExtendUnsafety("Unsafe reading while stabilizing");
                        return;
                    }

                    if (now.CompareTo(_endOfStabilization) <= 0)
                        return;
                    else {
                        UnsetState(SensorState.Stabilizing);
                        prolong = false;    // don't prolong if just ended stabilization
                    }
                }

                // If we got here the sensor is currently safe
                if (wasready && StateIsSet(SensorState.EnoughReadings) && !wassafe && prolong)
                {
                    ExtendUnsafety("Readings just turned safe");
                    return;     // Remain unsafe - just begun stabilizing
                }

                SetState(SensorState.Safe);
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
            SetState(SensorState.Stabilizing);
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
                if (HasAttribute(SensorAttribute.Immediate))
                    return TimeSpan.Zero;

                return StateIsSet(SensorState.Stabilizing) ?
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

                if (HasAttribute(SensorAttribute.Immediate))
                {
                    ret = getReading().safe;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        "Sensor ({0}), (immediate) isSafe: {1}",
                        WiseName, ret);
                    #endregion
                }
                else
                {

                    ret = StateIsSet(SensorState.Safe);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        "Sensor ({0}), (cumulative) isSafe: {1} (state: {2})",
                        WiseName, ret, _state.ToString());
                    #endregion
                }
                return ret;
            }
        }

        public bool Enabled
        {
            get
            {
                if (HasAttribute(SensorAttribute.AlwaysEnabled))
                    return true;

                return _enabled;
            }

            set
            {
                _enabled = value;
                if (_enabled)
                {
                    SetState(SensorState.Enabled);
                    _timer.Change(0, _intervalMillis);
                }
                else
                {
                    UnsetState(SensorState.Enabled);
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }
}
