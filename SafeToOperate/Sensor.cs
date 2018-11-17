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
        private System.Threading.Timer _timer;
        private DateTime _endOfStabilization;

        [Flags]
        public enum SensorAttribute {
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
        };

        [Flags]
        public enum SensorState
        {
            None = 0,
            Ready = (1 << 1),           // has enough readings to decide if safe or not
            Safe = (1 << 2),            // at least one reading was safe
            Stabilizing = (1 << 3),     // in transition from unsafe to safe
            Stale = (1 << 4),           // the data readings are too old
            Enabled = (1 << 5),         // It is not AlwaysEnabled and was enabled
        };

        public bool StateIsSet(SensorState s)
        {
            return (_state & s) != 0;
        }

        public bool StateIsNotSet(SensorState s)
        {
            return !StateIsSet(s);
        }

        public void SetState(SensorState s)
        {
            _state |= s;
        }

        public void UnsetState(SensorState s)
        {
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

        public class Reading
        {
            public bool stale;
            public bool safe;

            public override string ToString()
            {
                return string.Format("stale: {0}, safe: {1}", stale, safe);
            }
        }

        protected FixedSizedQueue<Reading> _readings;
        protected static Debugger debugger = Debugger.Instance;
        
        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafetooperate;

        protected Sensor(string name, SensorAttribute attributes, WiseSafeToOperate instance)
        {
            Name = name;
            _attributes = attributes;
            if (HasAttribute(SensorAttribute.AlwaysEnabled))
                Enabled = true;
            _state = SensorState.None;

            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            wisesafetooperate = instance;
            Restart(0);
        }

        public bool HasAttribute(SensorAttribute attr)
        {
            return (_attributes & attr) != 0;
        }

        public bool DoesNotHaveAttribute(SensorAttribute attr)
        {
            return !HasAttribute(attr);
        }

        #region ASCOM Profile
        public void readProfile()
        {
            int defaultInterval = 0, defaultRepeats = 0;

            switch (Name)
            {
                case "Wind": defaultInterval = 30; defaultRepeats = 3; break;
                case "Sun": defaultInterval = 60; defaultRepeats = 1; break;
                case "HumanIntervention": defaultInterval = 0; defaultRepeats = 1; break;
                case "Rain": defaultInterval = 30; defaultRepeats = 2; break;
                case "Clouds": defaultInterval = 30; defaultRepeats = 3; break;
                case "Humidity": defaultInterval = 30; defaultRepeats = 4; break;
            }

            _intervalMillis = 1000 * Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Interval", defaultInterval.ToString()));
            if (DoesNotHaveAttribute(SensorAttribute.Immediate))
                _repeats = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Repeats", defaultRepeats.ToString()));

            if (DoesNotHaveAttribute(SensorAttribute.AlwaysEnabled))
                Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Enabled", true.ToString()));

            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, (_intervalMillis / 1000).ToString(), "Interval");

            if (DoesNotHaveAttribute(SensorAttribute.Immediate))
                wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, _repeats.ToString(), "Repeats");

            if (DoesNotHaveAttribute(SensorAttribute.AlwaysEnabled))
                wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, Enabled.ToString(), "Enabled");

            writeSensorProfile();
        }
        #endregion

        public abstract string reason();
        public abstract void readSensorProfile();
        public abstract void writeSensorProfile();
        public abstract Reading getReading();
        public abstract string MaxAsString { get; set; }

        public bool IsStale(string propertyName)
        {
            if (DoesNotHaveAttribute(SensorAttribute.CanBeStale))
                return false;

            if (!WiseSafeToOperate.och.Connected)
                WiseSafeToOperate.och.Connected = true;

                if (WiseSafeToOperate.och.TimeSinceLastUpdate(propertyName) > WiseSafeToOperate.ageMaxSeconds)
            {
                SetState(SensorState.Stale);
                UnsetState(SensorState.Safe);
                return true;
            }

            UnsetState(SensorState.Stale);
            return false;
        }

        public void Restart(int due)
        {
            _state = new SensorState();
            _nbad = 0;
            _nreadings = 0;
            _nstale = 0;

            readProfile();
            if (HasAttribute(SensorAttribute.AlwaysEnabled) && _timer != null) {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                if (Enabled)
                {
                    if (_repeats > 1)
                    {
                        _readings = new FixedSizedQueue<Reading>(_repeats);
                        _timer.Change(due, _intervalMillis);
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
            {
                Stop();
                return;
            }
            
            if (StateIsSet(SensorState.Stabilizing))
            {
                // this timer event is at the end of the stabilization period
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) - Stabilization ended, restarting", Name);
                #endregion
                Restart(0);
                return;
            }

            bool wassafe = StateIsSet(SensorState.Safe);
            bool wasready = StateIsSet(SensorState.Ready);

            Reading currentReading = getReading();
            _readings.Enqueue(currentReading);
            if (_readings.ToArray().Count() == _repeats)
                SetState(SensorState.Ready);
            else
            {
                UnsetState(SensorState.Ready);
                UnsetState(SensorState.Safe);
            }

            Reading[] arr = _readings.ToArray();
            List<string> values = new List<string>();
            int nbad = 0, nstale = 0, nreadings = 0;
            foreach (Reading r in arr)
            {
                values.Add(r.safe.ToString());
                nreadings++;
                if (r.safe == false)
                    nbad++;
                if (r.stale == true)
                    nstale++;
            }
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) onTimer: added {1} [{2}]",
                Name, currentReading, String.Join(",", values));

            _nreadings = nreadings;
            _nbad = nbad;
            if (HasAttribute(SensorAttribute.CanBeStale))
            {
                _nstale = nstale;
                if (_nstale > 0)
                    SetState(SensorState.Stale);
                else
                    UnsetState(SensorState.Stale);
            }

            if (StateIsSet(SensorState.Ready))
            {
                if (_nbad == _repeats)
                    UnsetState(SensorState.Safe);
                else
                    SetState(SensorState.Safe);
            }

            if (!StateIsSet(SensorState.Ready))
                return;

            bool issafe = StateIsSet(SensorState.Safe);
            #region debug
            if (wassafe != issafe)
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) isSafe changed from {1} to {2}", Name, wassafe, issafe);
            #endregion

            if (wasready && (!wassafe && issafe))
            {
                // the sensor transited from unsafe to safe
                SetState(SensorState.Stabilizing);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) started stabilizing", Name);
                #endregion
                int millis = (int)WiseSafeToOperate._stabilizationPeriod.TotalMilliseconds;

                _timer.Change(millis, Timeout.Infinite);
                _endOfStabilization = DateTime.Now.AddMilliseconds(millis);
            }
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

        public void Stop()
        {
            if (DoesNotHaveAttribute(SensorAttribute.Immediate))
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Stop: stopped", Name);
                #endregion
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (not enabled)", Name, ret);
                    #endregion
                    return ret;
                }

                if (HasAttribute(SensorAttribute.Immediate))
                {
                    ret = getReading().safe;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1}", Name, ret);
                    #endregion
                    return ret;
                }

                if (DoesNotHaveAttribute(SensorAttribute.Immediate) && !StateIsSet(SensorState.Ready))
                {
                    ret = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (not ready)",
                        Name, ret);
                    #endregion
                    return ret;
                }

                if (DoesNotHaveAttribute(SensorAttribute.Immediate) && StateIsSet(SensorState.Stabilizing))
                {
                    ret = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (stabilizing)",
                        Name, ret);
                    #endregion
                    return ret;
                }

                ret = StateIsSet(SensorState.Safe);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} ({2} bad out of {3})",
                    Name, ret, _nbad, _repeats);
                #endregion
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
                if (HasAttribute(SensorAttribute.AlwaysEnabled))
                    return;

                _enabled = value;
                if (_enabled)
                    SetState(SensorState.Enabled);
                else
                    UnsetState(SensorState.Enabled);
            }
        }
    }
}
