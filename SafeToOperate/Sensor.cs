using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using System.IO;

namespace ASCOM.Wise40SafeToOperate
{
    public abstract class Sensor : WiseObject
    {
        private System.Threading.Timer _timer;
        private DateTime _endOfStabilization;

        public class SensorAttributes
        {
            public const uint None = 0;
            public const uint Ready = (1 << 1);           // has enough readings to decide if safe or not
            public const uint Safe = (1 << 2);            // at least one reading was safe
            public const uint Stabilizing = (1 << 3);     // in transition from unsafe to safe
            public const uint Accumulating = (1 << 4);    // uses more than one reading to decide if safe or not
            public const uint TimerIsRunning = (1 << 5);
            public const uint MustStabilize = (1 << 6);   // uses more than one reading and must stabilize when transitioning from unsafe to safe
            public const uint Stale = (1 << 7);           // te data reading is too old
            private uint _value;

            public SensorAttributes()
            {
                Reset();
            }

            public bool IsSet(uint a)
            {
                return (_value & a) != 0;
            }

            public void Set(uint a)
            {
                _value |= a;
            }

            public void Unset(uint a)
            {
                _value &= ~a;
            }

            public void Reset()
            {
                _value = None;
            }
        }

        public int _intervalMillis;
        public int _repeats;
        public int _nbad;
        public bool _enabled;
        public SensorAttributes _attr = new SensorAttributes();

        protected FixedSizedQueue<bool> _isSafeQueue;
        protected static Debugger debugger = Debugger.Instance;
        
        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafetooperate;

        protected Sensor(string name, WiseSafeToOperate instance)
        {
            Name = name;
            _attr = new SensorAttributes();

            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            wisesafetooperate = instance;
            Restart(0);
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
            _repeats = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Repeats", defaultRepeats.ToString()));
            _enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Enabled", true.ToString()));
            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, (_intervalMillis / 1000).ToString(), "Interval");
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, _repeats.ToString(), "Repeats");
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, _enabled.ToString(), "Enabled");
            writeSensorProfile();
        }
        #endregion

        public abstract string reason();
        public abstract void readSensorProfile();
        public abstract void writeSensorProfile();
        public abstract bool getIsSafe();
        public abstract string MaxAsString { get; set; }

        public bool IsStale(string propertyName)
        {
            if (WiseSafeToOperate.och.TimeSinceLastUpdate(propertyName) > WiseSafeToOperate.ageMaxSeconds)
            {
                _attr.Set(SensorAttributes.Stale);
                _attr.Unset(SensorAttributes.Safe);
                return true;
            }

            _attr.Unset(SensorAttributes.Stale);
            return false;
        }

        public void Restart(int due)
        {
            _attr = new SensorAttributes();
            _nbad = 0;

            readProfile();
            if (_enabled)
            {
                if (_repeats > 1)
                {
                    _isSafeQueue = new FixedSizedQueue<bool>(_repeats);
                    _attr.Set(SensorAttributes.MustStabilize);
                    _attr.Set(SensorAttributes.Accumulating);
                    _timer.Change(due, _intervalMillis);
                } else
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void onTimer(object StateObject)
        {        
            if (!_enabled)
            {
                Stop();
                return;
            }
            
            if (_attr.IsSet(SensorAttributes.Stabilizing))
            {
                // this timer event is at the end of the stabilization period
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) - Stabilization ended, restarting", Name);
                #endregion
                Restart(0);
                return;
            }

            bool wassafe = _attr.IsSet(SensorAttributes.Safe);
            bool wasready = _attr.IsSet(SensorAttributes.Ready);

            bool currentReading = getIsSafe();
            _isSafeQueue.Enqueue(currentReading);
            if (_isSafeQueue.ToArray().Count() == _isSafeQueue.MaxSize)
                _attr.Set(SensorAttributes.Ready);

            bool[] arr = _isSafeQueue.ToArray();
            List<string> values = new List<string>();
            int baddies = 0;
            foreach (bool safe in arr)
            {
                values.Add(safe.ToString());
                if (safe == false)
                    baddies++;
            }
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) onTimer: added {1} [{2}]",
                Name, currentReading, String.Join(",", values));

            _nbad = baddies;
            if (_attr.IsSet(SensorAttributes.Ready) && _nbad < _repeats)
                _attr.Set(SensorAttributes.Safe);

            if (!_attr.IsSet(SensorAttributes.Ready))
                return;

            bool issafe = _attr.IsSet(SensorAttributes.Safe);
            #region debug
            if (wassafe != issafe)
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) isSafe changed from {1} to {2}", Name, wassafe, issafe);
            #endregion

            if (wasready && _attr.IsSet(SensorAttributes.MustStabilize) && (!wassafe && issafe))
            {
                // the sensor transited from unsafe to safe
                _attr.Set(SensorAttributes.Stabilizing);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) started stabilizing", Name);
                #endregion
                _timer.Change((int)WiseSafeToOperate._stabilizationPeriod.TotalMilliseconds, Timeout.Infinite);
                _endOfStabilization = DateTime.Now.AddMilliseconds((int)WiseSafeToOperate._stabilizationPeriod.TotalMilliseconds);
            }
        }

        public TimeSpan TimeToStable
        {
            get
            {
                return _attr.IsSet(SensorAttributes.Stabilizing) ? _endOfStabilization - DateTime.Now : TimeSpan.FromSeconds(0);
            }
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _attr.Unset(SensorAttributes.TimerIsRunning);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Stop: stopped", Name);
            #endregion
        }

        /// <summary>
        /// Safe unless last _repeats were unsafe
        /// </summary>
        public bool isSafe
        {
            get
            {
                bool ret;

                if (!_enabled)
                {
                    ret = true;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (not enabled)", Name, ret);
                    #endregion
                    return ret;
                }

                if (Name == "HumanIntervention" || Name == "Sun")
                {  // One-shots
                    ret = getIsSafe();
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1}", Name, ret);
                    #endregion
                    return ret;
                }

                if (!_attr.IsSet(SensorAttributes.Ready) || _attr.IsSet(SensorAttributes.Stabilizing))
                {
                    ret = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} (ready: {2}, stabilizing: {3})",
                        Name, ret, _attr.IsSet(SensorAttributes.Ready), _attr.IsSet(SensorAttributes.Stabilizing));
                    #endregion
                    return ret;
                }

                ret = _attr.IsSet(SensorAttributes.Safe);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}), isSafe: {1} ({2} bad out of {3})",
                    Name, ret, _nbad, _repeats);
                #endregion
                return ret;
            }
        }
    }

    #region Wind
    public class WindSensor : Sensor
    {
        double _max;

        public WindSensor(WiseSafeToOperate instance) : base("Wind", instance) {}

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            if (IsStale("WindSpeed"))
                return false;
            return (WiseSafeToOperate.och.WindSpeed * 3.6) < _max;
        }

        public override string reason()
        {
            return string.Format("The last {0} wind speed readings were higher than {1} km/h.", _nbad, _max);
        }

        public override string MaxAsString
        {
            get
            {
                return _max.ToString();
            }

            set
            {
                _max = Convert.ToDouble(value);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Max: {1}", Name, MaxAsString);
                #endregion
            }
        }
    }
    #endregion
    #region HumanIntervention
    public class HumanInterventionSensor : Sensor
    {
        public HumanInterventionSensor(WiseSafeToOperate instance) : base("HumanIntervention", instance) { }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }

        public override bool getIsSafe()
        {
            bool ret = !Wise40.HumanIntervention.IsSet();
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "HumanInterventionSensor: getIsSafe: {0}", ret);
            #endregion
            return ret;
        }

        public override string reason()
        {
            return Wise40.HumanIntervention.Info;
        }

        public override string MaxAsString
        {
            set {}

            get { return 0.ToString(); }
        }
    }
    #endregion
    #region Clouds
    public class CloudsSensor : Sensor
    {
        private uint _max;

        public CloudsSensor(WiseSafeToOperate instance) : base("Clouds", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", "0");
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            if (IsStale("CloudCover"))
                return false;
            return WiseSafeToOperate.och.CloudCover <= _max;
        }

        public override string reason()
        {
            return string.Format("The last {0} cloud cover readings were higher than \"{1}\"", _nbad, MaxAsString);
        }

        public override string MaxAsString
        {
            set
            {
                _max = Convert.ToUInt32(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }
    #endregion
    #region Rain
    public class RainSensor : Sensor
    {
        private double _max;

        public RainSensor(WiseSafeToOperate instance) : base("Rain", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            if (IsStale("RainRate"))
                return false;
            return WiseSafeToOperate.och.RainRate <= _max;
        }

        public override string reason()
        {
            return string.Format("The last {0} rain rate readings were higher than {1}", _nbad, MaxAsString);
        }

        public override string MaxAsString
        {
            set
            {
                _max = Convert.ToDouble(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }
    #endregion
    #region Humidity
    public class HumiditySensor : Sensor
    {
        private double _max;

        public HumiditySensor(WiseSafeToOperate instance) : base("Humidity", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", "90");
            if (MaxAsString == "0")
                MaxAsString = "90.0"; // ???
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            if (IsStale("Humidity"))
                return false;
            return WiseSafeToOperate.och.Humidity <= _max;
        }

        public override string reason()
        {
            return string.Format("The last {0} humidity readings were higher than {1}", _nbad, MaxAsString);
        }

        public override string MaxAsString
        {
            set
            {
                _max = Convert.ToDouble(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }
    #endregion
    #region Sun
    public class SunSensor : Sensor
    {
        private double _max;

        public SunSensor(WiseSafeToOperate instance) : base("Sun", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            return wisesafetooperate.SunElevation <= _max;
        }

        public override string reason()
        {
            double currentElevation = wisesafetooperate.SunElevation;

            if (currentElevation <= _max)
                return string.Empty;

            return string.Format("The Sun elevation ({0:f1}deg) is higher than {1:f1}deg.",
                currentElevation, _max);
        }

        public override string MaxAsString
        {
            set
            {
                _max = Convert.ToDouble(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }
    #endregion
}
