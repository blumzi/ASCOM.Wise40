
#define USE_HTTP_FETCHER

using System;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

#if USE_HTTP_FETCHER
using Newtonsoft.Json;
#else
using ASCOM.Astrometry;
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
            DateTime now = DateTime.Now;
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
#if USE_HTTP_FETCHER
            else if (r.Stale)
                _status = $"Sun elevation is stale (older than {sunElevation.MaxAge.ToMinimalString()})";
#endif
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

#if USE_HTTP_FETCHER
            else if (sunElevation.Stale)
                return $"Sun elevation is stale (older than {sunElevation.MaxAge.ToMinimalString()})";
#endif

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

#if USE_HTTP_FETCHER
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
#endif

    public class SunElevation
    {
        private TimeSpan _maxAge = TimeSpan.FromSeconds(10);
        private double _value = Double.NaN;
        private bool _initialized = false;

#if USE_HTTP_FETCHER
        private const string apiKey = "d6ce0c7ecb5c451ba2b462dfb5750364";
        private static readonly string URL = "https://api.ipgeolocation.io/astronomy?" +
            $"apiKey={apiKey}&" +
            $"lat={WiseSite.Latitude}&" +
            $"long={WiseSite.Longitude}";
        private static PeriodicHttpFetcher periodicHttpFetcher;
#else
        private DateTime _lastUpdate = DateTime.MinValue;
#endif

        public void Init()
        {
            if (_initialized)
                return;

#if USE_HTTP_FETCHER
            periodicHttpFetcher = new PeriodicHttpFetcher(
                "SunElevation",
                URL,
                TimeSpan.FromMinutes(2),
                tries: 3,
                maxAgeMillis: (int)TimeSpan.FromMinutes(5).TotalMilliseconds
            );
#endif
            _initialized = true;
        }

        public SunElevation() { }

        public double Value
        {
            get
            {
#if USE_HTTP_FETCHER
                if (!periodicHttpFetcher.Alive)
                    _value = Double.NaN;
                else {
                    try
                    {
                        IpGeolocationInfo info = JsonConvert.DeserializeObject<IpGeolocationInfo>(
                            periodicHttpFetcher.Result
                            .Replace("\"-:-\"", "\"00:00\""));

                        _value = info.sun_altitude;
                    }
                    catch (InvalidValueException)
                    {
                        _value = Double.NaN;
                    }
                }
#else
                if (!Stale)
                    return _value;

                WiseSite.InitOCH();

                ASCOM.Utilities.Util ascomUtil = new Utilities.Util();

                OnSurface onSurface = new OnSurface()
                {
                    Latitude = WiseSite.Latitude,
                    Longitude = WiseSite.Longitude,
                    Height = WiseSite.Elevation,
                    Pressure = WiseSite.och.Pressure,
                    Temperature = WiseSite.och.Temperature,
                };

                CatEntry3 catEntry = new CatEntry3()
                {
                    StarName = "Sun",
                };

                Object3 target = new Object3()
                {
                    Name = "Sun",
                    Number = Body.Sun,
                    Star = catEntry,
                    Type = ObjectType.MajorPlanetSunOrMoon,
                };

                double ra = 0.0, dec = 0.0, dis = 0.0;

                short ret = WiseSite.novas31.TopoPlanet(ascomUtil.JulianDate, target, WiseSite.astroutils.DeltaT(), onSurface, Accuracy.Full, ref ra, ref dec, ref dis);

                if (ret != 0)
                    Exceptor.Throw<InvalidOperationException>("SunElevation", $"Cannot calculate Sun position (novas31.TopoPlanet: ret: {ret})");

                double rar = 0, decr = 0, az = 0, zd = 0;

                WiseSite.novas31.Equ2Hor(WiseSite.astroutils.JulianDateUT1(0), 0,
                    WiseSite.astrometricAccuracy,
                    0, 0,
                    onSurface,
                    ra, dec,
                    WiseSite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                _value = 90.0 - zd;
                _lastUpdate = DateTime.Now;
#endif
                return _value;
            }
        }

        public DateTime LastUpdate {
            get
            {
#if USE_HTTP_FETCHER
                return periodicHttpFetcher.LastSuccess;
#else
                return _lastUpdate;
#endif
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
                return
#if USE_HTTP_FETCHER
                    periodicHttpFetcher.MaxAge
#else
                    _maxAge
#endif

                ;
            }
        }
    }
}