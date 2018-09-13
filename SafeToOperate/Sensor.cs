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
    public abstract class Sensor: WiseObject
    {
        private System.Threading.Timer _timer;
        public bool _enabled;
        public bool _enabledByProfile;
        public int _interval;      // millis
        public int _repeats;
        public bool _ready;
        public int _nreadings, _nbad;
        protected FixedSizedQueue<bool> _isSafeQueue;
        private bool _running = false;
        protected string _maxValueProfileName;
        protected static Debugger debugger = Debugger.Instance;

        private bool _mustStabilize;
        private DateTime _startedStabilizing = DateTime.MinValue;
        
        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafetooperate;

        private ASCOM.Utilities.Util util = new Util();


        protected Sensor(string name, WiseSafeToOperate instance)
        {
            base.Name = name;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            wisesafetooperate = instance;
            _mustStabilize = name != "Sun";
            _ready = false;
            _nreadings = 0;
            _nbad = 0;
            readProfile();
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

            _interval = 1000 * Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Interval", defaultInterval.ToString()));
            _repeats = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Repeats", defaultRepeats.ToString()));
            _isSafeQueue = new FixedSizedQueue<bool>(_repeats);
            Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Enabled", true.ToString()));
            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, (_interval / 1000).ToString(), "Interval");
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, _repeats.ToString(), "Repeats");
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, _enabled.ToString(), "Enabled");
            writeSensorProfile();
        }
        #endregion

        //public int nBadReadings
        //{
        //    get
        //    {
        //        var values = _isSafeQueue.ToArray();
        //        int nbad = 0;

        //        foreach (var v in values)
        //            if (v == false)
        //                nbad++;
        //        return nbad;
        //    }
        //}

        //public int nReadings
        //{
        //    get
        //    {
        //        return _isSafeQueue.ToArray().Length;
        //    }
        //}
        
        protected bool isStabilizing
        {
            get
            {
                if (!_mustStabilize)
                    return false;

                if (_startedStabilizing == DateTime.MinValue)
                    return false;
                return DateTime.Now.Subtract(_startedStabilizing) < wisesafetooperate._stabilizationPeriod;
            }
        }

        public abstract string reason();
        public abstract void readSensorProfile();
        public abstract void writeSensorProfile();
        public abstract bool getIsSafe();
        public abstract string MaxAsString { get; set; }

        private void onTimer(object StateObject)
        {        
            if (!Enabled)
            {
                Stop();
                return;
            }
            
            bool wassafe = false;
            foreach (bool safe in _isSafeQueue.ToArray())
                if (safe)
                {
                    wassafe = true;
                    break;
                }

            bool reading = getIsSafe();
            _isSafeQueue.Enqueue(reading);
            _nreadings++;
            if (_nreadings > _repeats)
                _nreadings = _repeats;

            if (_nreadings == _repeats)
                _ready = true;

            bool[] arr = _isSafeQueue.ToArray();
            #region debug
            List<string> values = new List<string>();
            foreach (bool safe in arr)
                values.Add(safe.ToString());

            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) onTimer: added {1} [{2}]", Name, reading, String.Join(",", values));
            #endregion

            if (!_ready)
                return;

            _nbad = 0;
            foreach (bool safe in arr)
                if (!safe)
                    _nbad++;

            bool issafe = false;
            foreach (bool safe in arr)
                if (safe)
                {
                    issafe = true;
                    break;
                }

            #region debug
            if (wassafe != issafe)
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) isSafe changed from {1} to {2}", Name, wassafe, issafe);
            #endregion

            if (_mustStabilize && (issafe && !wassafe))
            {
                _startedStabilizing = DateTime.Now;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) started stabilizing", Name);
                #endregion
            }
            else
            {
                _startedStabilizing = DateTime.MinValue;
            }
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
            }
        }

        public void Start()
        {
            if (!Enabled || Running)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Start: not started: Enabled={1}, Running={2}", Name, Enabled, Running);
                #endregion
                return;
            }
            _isSafeQueue = new FixedSizedQueue<bool>(_repeats);
            _running = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Start: started", Name);
            #endregion
            _timer.Change(0, _interval);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _running = false;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Stop: stopped", Name);
            #endregion
        }

        public bool Running
        {
            get
            {
                return _running;
            }
        }

        public int Interval
        {
            get
            {
                return _interval / 1000;
            }

            set
            {
                if (Running)
                {
                    Stop();
                    _interval = value * 1000;
                    Start();
                }
                else
                    _interval = value * 1000;
            }
        }

        public int Repeats
        {
            get
            {
                return _repeats;
            }

            set
            {
                if (Running)
                {
                    Stop(); ;
                    _repeats = value;
                    Start();
                }
                else
                    _repeats = value;

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
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor {0}, isSafe: {1} (not enabled)", Name, ret);
                    #endregion
                    return ret;
                }

                if (Name == "HumanIntervention" || Name == "Sun")
                {  // One-shots
                    ret = getIsSafe();
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor {0}, isSafe: {1}", Name, ret);
                    #endregion
                    return ret;
                }

                if (!_ready || isStabilizing)
                {
                    ret = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor {0}, isSafe: {1} (ready: {2}, stabilizing: {3})", Name, ret, _ready, isStabilizing);
                    #endregion
                    return ret;
                }

                ret = _nbad != _nreadings;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor {0}, isSafe: {1} (nReadings: {2}, nBadReadings: {3})", Name, ret, _nreadings, _nbad);
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
            return (wisesafetooperate.och.WindSpeed * 3.6) < _max;
        }

        public override string reason()
        {
            if (! _ready)
                return string.Format("{0} - not enough readings ({1} < {2})", Name, _nreadings, _repeats);

            if (isStabilizing)
                return string.Format("{0} - stabilizing", Name);

            int nbad;

            if (_nbad < _repeats)
                return string.Empty;

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
            return wisesafetooperate.och.CloudCover <= _max;
        }

        public override string reason()
        {
            if (! _ready)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, _nreadings, _repeats);
            }

            if (_nbad < _repeats)
                return string.Empty;

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
            return wisesafetooperate.och.RainRate <= _max;
        }

        public override string reason()
        {
            if (! _ready)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, _nreadings, _repeats);
            }

            int nbad;

            if (_nbad < _repeats)
                return string.Empty;

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
            return wisesafetooperate.och.Humidity <= _max;
        }

        public override string reason()
        {
            if (! _ready)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, _nreadings, _repeats);
            }

            int nbad;

            if (_nbad < _repeats)
                return string.Empty;

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
