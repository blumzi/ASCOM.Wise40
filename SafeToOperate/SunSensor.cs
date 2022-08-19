
#define USE_COORDINATE_SHARP

using System;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

#if USE_COORDINATE_SHARP
// NuGet package manager: Install-Package CoordinateSharp -Version 2.13.1.1
// Github:                https://github.com/Tronald/CoordinateSharp
// Web Site:              https://coordinatesharp.com/DeveloperGuide#solar-and-lunar-coordinates
using CoordinateSharp;
#endif

namespace ASCOM.Wise40SafeToOperate
{
    public class SunSensor : Sensor
    {
        private double _maxAtDawn, _maxAtDusk;
        private const double defaultMaxAtDawn = -10.0;
        private const double defaultMaxAtDusk = -4.0;
        public double MinSettableElevation { get; set; }
        public double MaxSettableElevation { get; set; }
        private const double DefaultMinSettableElevation = -20;
        private const double DefaultMaxSettableElevation = 5;
        private bool _wasSafe = false;
        private string _status;
        public SunElevation sunElevation = new SunElevation();

        public SunSensor(WiseSafeToOperate wiseSafeToOperate) :
            base("Sun",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.CanBeBypassed |
                Attribute.CanBeStale,
                "°", " deg", "f1", "",
                wiseSafeToOperate)
        {
            sunElevation.Init();
        }

        public override object Digest()
        {
            return new SunDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override void ReadSensorProfile()
        {
            MaxAtDawnAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MaxAtDawn", defaultMaxAtDawn.ToString());
            MaxAtDuskAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MaxAtDusk", defaultMaxAtDusk.ToString());
            MaxSettableElevation = Convert.ToDouble(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MaxSettableElevation", DefaultMaxSettableElevation.ToString()));
            MinSettableElevation = Convert.ToDouble(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MinSettableElevation", DefaultMinSettableElevation.ToString()));
        }

        public override void WriteSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAtDawnAsString, "MaxAtDawn");
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAtDuskAsString, "MaxAtDusk");
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxSettableElevation.ToString(), "MaxSettableElevation");
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MinSettableElevation.ToString(), "MinSettableElevation");
        }

        public override Reading GetReading()
        {
            if (wisesafetooperate == null)
                return null;

            double max = DateTime.Now.Hour < 12 ? _maxAtDawn : _maxAtDusk;
            bool stale = sunElevation.Stale;

            Reading r = new Reading
            {
                Stale = stale,
                Usable = !Double.IsNaN(sunElevation.Value) && !sunElevation.Stale,
                Safe = !Double.IsNaN(sunElevation.Value) && sunElevation.Value <= max,
                value = sunElevation.Value,
                timeOfLastUpdate = sunElevation.LastUpdate,
                secondsSinceLastUpdate = sunElevation.Age.TotalSeconds,
            };

            if (Double.IsNaN(sunElevation.Value))
                _status = "Sun elevation is not available yet";
            else
                _status = $"Sun elevation is {FormatVerbal(sunElevation.Value)} (max: {FormatVerbal(max)})";

            if (r.Safe != _wasSafe)
            {
                ActivityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.Safe)));
            }
            _wasSafe = r.Safe;
            return r;
        }

        public override string UnsafeReason()
        {
            double currentElevation = sunElevation.Value;
            double max = DateTime.Now.Hour < 12 ? _maxAtDawn : _maxAtDusk;

            if (Double.IsNaN(currentElevation))
                return "Sun elevation is not available yet";
            else if (sunElevation.Stale)
                return $"Sun elevation is stale (older than {sunElevation.MaxAge.ToMinimalString()})";

            return currentElevation <= max ? "" : $"The Sun elevation ({FormatVerbal(currentElevation)}) is higher than {FormatVerbal(max)}";
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }

        public string MaxAtDuskAsString
        {
            set
            {
                _maxAtDusk = Convert.ToDouble(value);
            }

            get
            {
                return _maxAtDusk.ToString();
            }
        }

        public string MaxAtDawnAsString
        {
            set
            {
                _maxAtDawn = Convert.ToDouble(value);
            }

            get
            {
                return _maxAtDawn.ToString();
            }
        }

        public override string MaxAsString { get { return ""; } set { } }

    }

    public class SunDigest
    {
        public string Name;
        public bool IsSafe;
    }

    public class SunElevation
    {
        private double _value = Double.NaN;
        private bool _initialized = false;

        private DateTime _lastUpdate = DateTime.MinValue;

        public void Init()
        {
            if (_initialized)
                return;

            _initialized = true;
        }

        public SunElevation() { }

        public double Value
        {
            get
            {
                Coordinate c = new Coordinate(WiseSite.Latitude, WiseSite.Longitude, DateTime.UtcNow);
                _value = c.CelestialInfo.SunAltitude;
                _lastUpdate = DateTime.Now;
                return _value;
            }
        }

        public DateTime LastUpdate {
            get
            {
                return _lastUpdate;
            }
        }

        public TimeSpan Age
        {
            get
            {
                return DateTime.Now - LastUpdate;
            }
        }

        public bool Stale
        {
            get
            {
                return Age > MaxAge;
            }
        }

        public TimeSpan MaxAge
        {
            get
            {
                return TimeSpan.FromMinutes(1);
            }
        }
    }
}