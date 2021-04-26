using System;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using Newtonsoft.Json;

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
            Init();
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
            bool stale = periodicHttpFetcher.Stale;
            double elevation = SunElevation;

            Reading r = new Reading
            {
                Stale = stale,
                Usable = !Double.IsNaN(elevation) && !stale,
                Safe = !Double.IsNaN(elevation) && elevation <= max,
                value = elevation,
                timeOfLastUpdate = periodicHttpFetcher.LastFetch,
                secondsSinceLastUpdate = periodicHttpFetcher.Age.TotalSeconds,
            };

            if (Double.IsNaN(elevation))
                _status = "Sun elevation is not available yet";
            else if (r.Stale)
                _status = $"Sun elevation is stale (older than {periodicHttpFetcher.MaxAge.ToMinimalString()})";
            else
                _status = $"Sun elevation is {FormatVerbal(SunElevation)} (max: {FormatVerbal(max)})";

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
            double currentElevation = SunElevation;
            double max = DateTime.Now.Hour < 12 ? _maxAtDawn : _maxAtDusk;

            if (Double.IsNaN(currentElevation))
                return "Sun elevation is not available yet";
            else if (periodicHttpFetcher.Stale)
                return $"Sun elevation is stale (older than {periodicHttpFetcher.MaxAge.ToMinimalString()})";

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

        public double SunElevation
        {
            get
            {
                if (!periodicHttpFetcher.Alive)
                    return Double.NaN;

                try
                {
                    IpGeolocationInfo info = JsonConvert.DeserializeObject<IpGeolocationInfo>(
                        periodicHttpFetcher.Result
                        .Replace("\"-:-\"", "\"00:00\""));

                    return info.sun_altitude;
                }
                catch (InvalidValueException)
                {
                    return Double.NaN;
                }
            }
        }

        private const string apiKey = "d6ce0c7ecb5c451ba2b462dfb5750364";
        private static readonly string URL = "https://api.ipgeolocation.io/astronomy?" +
            $"apiKey={apiKey}&" +
            $"lat={WiseSite.Instance.Latitude.Degrees}&" +
            $"long={WiseSite.Instance.Longitude.Degrees}";

        private bool _initialized = false;
        private static PeriodicHttpFetcher periodicHttpFetcher;

        public void Init()
        {
            if (_initialized)
                return;

            WiseName = "SunSensor";
            periodicHttpFetcher = new PeriodicHttpFetcher(
                WiseName,
                URL,
                TimeSpan.FromMinutes(2),
                tries: 3,
                maxAgeMillis: (int) TimeSpan.FromMinutes(5).TotalMilliseconds
            );

            _initialized = true;
        }
    }

    public class SunDigest
    {
        public string Name;
        public bool IsSafe;
    }

    public class IpGeolocationLocation
    {
        public double latitude;
        public double longitude;
    }

    public class IpGeolocationInfo
    {
        public IpGeolocationLocation location;
        public DateTime date;
        public DateTime current_time;
        public DateTime sunrise;
        public DateTime sunset;
        public string sun_status;
        public DateTime solar_noon;
        public TimeSpan day_length;
        public double sun_altitude;
        public double sun_distance;
        public double sun_azimuth;
        public DateTime moonrise;
        public DateTime moonset;
        public string moon_status;
        public double moon_altitude;
        public double moon_distance;
        public double moon_azimuth;
        public double moon_parallactic_angle;
    }
}