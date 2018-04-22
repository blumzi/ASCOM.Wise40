using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
//using ASCOM.Wise40.SafeToOperate;
using ASCOM.Utilities;
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.VantagePro;
using System.IO;

namespace ASCOM.Wise40SafeToOpen //.SafeToOperate
{
    public abstract class Sensor: WiseObject
    {
        private System.Threading.Timer _timer;
        public bool _enabled;
        public bool _enabledByProfile;
        public int _interval;      // millis
        public int _repeats;
        protected FixedSizedQueue<bool> _isSafeQueue;
        private bool _running = false;
        protected string _maxValueProfileName;
        protected static Debugger debugger = Debugger.Instance;

        private bool _mustStabilize;
        private DateTime _startedStabilizing = DateTime.MinValue;

        protected static string driverID = "ASCOM.Wise40SafeToOpen.SafetyMonitor";
        protected static string deviceType = "SafetyMonitor";

        protected static WiseSafeToOperate wisesafe;


        protected Sensor(string name, WiseSafeToOperate instance)
        {
            base.Name = name;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            wisesafe = instance;
            _mustStabilize = name != "Sun";
            readProfile();
        }

        public void readProfile()
        {
            _interval = 1000 * Convert.ToInt32(wisesafe._profile.GetValue(driverID, Name, "Interval", 0.ToString()));
            _repeats = Convert.ToInt32(wisesafe._profile.GetValue(driverID, Name, "Repeats", 0.ToString()));
            _isSafeQueue = new FixedSizedQueue<bool>(_repeats);
            Enabled = Convert.ToBoolean(wisesafe._profile.GetValue(driverID, Name, "Enabled", true.ToString()));
            readSensorProfile();
        }

        public void writeProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, (_interval / 1000).ToString(), "Interval");
            wisesafe._profile.WriteValue(driverID, Name, _repeats.ToString(), "Repeats");
            wisesafe._profile.WriteValue(driverID, Name, _enabled.ToString(), "Enabled");
            writeSensorProfile();
        }

        public int nBadReadings
        {
            get
            {
                var values = _isSafeQueue.ToArray();
                int nbad = 0;

                foreach (var v in values)
                    if (v == false)
                        nbad++;
                return nbad;
            }
        }

        public int nReadings
        {
            get
            {
                return _isSafeQueue.ToArray().Length;
            }
        }
        
        protected bool isStabilizing
        {
            get
            {
                if (!_mustStabilize)
                    return false;

                if (_startedStabilizing == DateTime.MinValue)
                    return false;
                return DateTime.Now.Subtract(_startedStabilizing) < wisesafe._stabilizationPeriod;
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
            bool reading = getIsSafe();
            #region debug
            bool wassafe = false;
            foreach (bool safe in _isSafeQueue.ToArray())
                if (safe)
                {
                    wassafe = true;
                    break;
                }
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) onTimer: enqueing {1}", Name, reading);
            #endregion
            _isSafeQueue.Enqueue(reading);
            
            bool issafe = false;
            foreach (bool safe in _isSafeQueue.ToArray())
                if (safe)
                {
                    issafe = true;
                    break;
                }

            #region debug
            if (wassafe != issafe)
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) isSafe changed from {1} to {2}", Name, wassafe, issafe);
            #endregion

            if (_mustStabilize && (issafe && !wassafe))
            {
                _startedStabilizing = DateTime.Now;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) started stabilizing", Name);
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
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) Start: not started: Enabled={1}, Running={2}", Name, Enabled, Running);
                #endregion
                return;
            }
            _isSafeQueue = new FixedSizedQueue<bool>(_repeats);  // memory leak, dispose old _values
            _running = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) Start: started", Name);
            #endregion
            _timer.Change(0, _interval);
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _running = false;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) Stop: stopped", Name);
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
                if (!Enabled)
                    return true;


                if (Name == "HumanIntervention")
                    return getIsSafe();

                if (isStabilizing)
                    return false;

                if (nReadings < _isSafeQueue.MaxSize)   // not enough readings yet
                    return false;

                return nBadReadings != nReadings;
            }
        }
    }

    public class WindSensor : Sensor
    {
        double _max;

        public WindSensor(WiseSafeToOperate instance) : base("Wind", instance) {}

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            return wisesafe.vantagePro.KMH(wisesafe.vantagePro.WindSpeedMps) < _max;
        }

        public override string reason()
        {
            if (nReadings < _repeats)
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);

            if (isStabilizing)
                return string.Format("{0} - stabilizing", Name);

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} wind speed readings from VantagePro were higher than {1} km/h.", nbad, _max);
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
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Sensor ({0}) Max: {1}", Name, MaxAsString);
                #endregion
            }
        }
    }

    public class HumanInterventionSensor : Sensor
    {
        private const string humanInterventionFilePath = Const.topWise40Directory + "Observatory/HumanIntervention.txt";

        public HumanInterventionSensor(WiseSafeToOperate instance) : base("HumanIntervention", instance) {
            Directory.CreateDirectory(Path.GetDirectoryName(humanInterventionFilePath));
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }

        public override bool getIsSafe()
        {
            bool ret = !File.Exists(humanInterventionFilePath);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "HumanInterventionSensor: getIsSafe: {0}", ret);
            #endregion
            return ret;
        }

        public override string reason()
        {
            if (!File.Exists(humanInterventionFilePath))
                return string.Empty;

            StreamReader sr = new StreamReader(humanInterventionFilePath);
            string line, reason = string.Empty;

            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("Operator:") || line.StartsWith("Created:") || line.StartsWith("Reason:"))
                   reason += line + "; ";
            }

            reason = "Human Intervention: " + ((reason == string.Empty) ? string.Format("File \"{0}\" exists.", humanInterventionFilePath) : reason);
            return reason;
        }

        public override string MaxAsString
        {
            set {}

            get { return 0.ToString(); }
        }
    }

    public class LightSensor : Sensor
    {
        private Dictionary<string, int> stringToDayConditions = new Dictionary<string, int>
                {
                    {"dayUnknown", 0 },
                    {"dayDark", 1 },
                    {"dayLight", 2 },
                    {"dayVeryLight", 3 },
                };

        private Dictionary<int, string> dayConditionsToString = new Dictionary<int, string>
                {
                    {0, "dayUnknown"},
                    {1, "dayDark"},
                    {2, "dayLight"},
                    {3, "dayVeryLight"},
                };
        public SensorData.DayCondition _max;

        public LightSensor(WiseSafeToOperate instance) : base("Light", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", dayConditionsToString[(int)SensorData.DayCondition.dayUnknown]);
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {

            SensorData.DayCondition light = (SensorData.DayCondition)Enum.Parse(typeof(SensorData.DayCondition),
                wisesafe.boltwood.CommandString("daylight", true));
                
            return (light != SensorData.DayCondition.dayUnknown) && ((int)light <= (int)_max);
        }

        public override string reason()
        {
            if (nReadings < _repeats)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);
            }

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} day condition readings from Boltwood were higher than \"{1}\".",
                nbad, MaxAsString.Replace("day", ""));
        }

        public override string MaxAsString
        {
            set
            {
                _max = (SensorData.DayCondition)Enum.Parse(typeof(SensorData.DayCondition), value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }

    public class CloudsSensor : Sensor
    {
        private SensorData.CloudCondition _max;

        public CloudsSensor(WiseSafeToOperate instance) : base("Clouds", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", "cloudUnknown");
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            SensorData.CloudCondition cover = wisesafe.boltwood.CloudCover_condition;

            return (cover != SensorData.CloudCondition.cloudUnknown) && ((int)cover <= (int)_max);
        }

        public override string reason()
        {
            if (nReadings < _repeats)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);
            }

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} cloud cover readings from Boltwood were higher than \"{1}\"",
                nbad, MaxAsString.Replace("cloud", ""));
        }

        public override string MaxAsString
        {
            set
            {
                _max = (SensorData.CloudCondition)Enum.Parse(typeof(SensorData.CloudCondition), value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }

    public class RainSensor : Sensor
    {
        private double _max;

        public RainSensor(WiseSafeToOperate instance) : base("Rain", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            return wisesafe.vantagePro.RainRate <= _max;
        }

        public override string reason()
        {
            if (nReadings < _repeats)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);
            }

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} rain rate readings from VantagePro were higher than {1}", nbad, MaxAsString);
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

    public class HumiditySensor : Sensor
    {
        private double _max;

        public HumiditySensor(WiseSafeToOperate instance) : base("Humidity", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            return wisesafe.vantagePro.Humidity <= _max;
        }

        public override string reason()
        {
            if (nReadings < _repeats)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);
            }

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} humidity readings from VantagePro were higher than {1}", nbad, MaxAsString);
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

    public class SunSensor : Sensor
    {
        private double _max;

        public SunSensor(WiseSafeToOperate instance) : base("Sun", instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafe._profile.GetValue(driverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafe._profile.WriteValue(driverID, Name, MaxAsString, "Max");
        }

        public override bool getIsSafe()
        {
            return wisesafe.SunElevation <= _max;
        }

        public override string reason()
        {
            if (nReadings < _repeats)
            {
                return string.Format("{0} - not enough readings ({1} < {2})", Name, nReadings, _repeats);
            }

            int nbad;

            if ((nbad = nBadReadings) == 0)
                return string.Empty;

            return string.Format("The last {0} Sun elevation calculation(s) were higher than {1:f1}°", nbad, _max);
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
}
