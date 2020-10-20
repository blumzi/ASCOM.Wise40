using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Net;

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
            TimeSpan ts = DateTime.Now.Subtract(_lastDataRead);
            bool stale = ts > _maxTimeBetweenIpGeolocationReads;
            double elevation = SunElevation;

            Reading r = new Reading
            {
                Stale = stale,
                Usable = !Double.IsNaN(_elevation) && !stale,
                Safe = !Double.IsNaN(elevation) && elevation <= max,
                value = elevation,
                timeOfLastUpdate = _lastDataRead,
                secondsSinceLastUpdate = ts.TotalSeconds,
            };

            if (Double.IsNaN(elevation))
                _status = "Sun elevation is not available yet";
            else if (r.Stale)
                _status = $"Sun elevation is stale (older than {_maxTimeBetweenIpGeolocationReads.ToMinimalString()})";
            else
                _status = $"Sun elevation is {FormatVerbal(SunElevation)} (max: {FormatVerbal(max)})";

            if (r.Safe != _wasSafe)
            {
                activityMonitor.Event(new Event.SafetyEvent(
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
            else if (DateTime.Now.Subtract(_lastDataRead) > _maxTimeBetweenIpGeolocationReads)
                return $"Sun elevation is stale (older than {_maxTimeBetweenIpGeolocationReads.ToMinimalString()})";

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
                return _elevation;
            }
        }

        private void PeriodicReader(object state)
        {
            if (Interlocked.CompareExchange(ref _readingInterlock, 1, 0) == 0)
                return;
            GetIpGeolocationInfo().GetAwaiter().GetResult();
            Interlocked.Exchange(ref _readingInterlock, 0);
        }

        public async Task GetIpGeolocationInfo()
        {
            int maxTries = 10, tryNo;

            DateTime start = DateTime.Now;
            TimeSpan duration = TimeSpan.Zero;

            HttpResponseMessage response = null;
            for (tryNo = 0; tryNo < maxTries; tryNo++)
            {
                try
                {
                    response = await _client.GetAsync(URL).ConfigureAwait(false);
                    duration = DateTime.Now.Subtract(start);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    duration = DateTime.Now.Subtract(start);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetIpGeolocationInfo: try#: {tryNo}, HttpRequestException: {ex.Message} at {ex.StackTrace}, duration: {duration}");
                    #endregion
                    continue;
                }
                catch (TaskCanceledException)
                {
                    duration = DateTime.Now.Subtract(start);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetIpGeolocationInfo: try#: {tryNo}, timedout, duration: {duration}");
                    #endregion
                    continue;
                }
                catch (Exception ex)
                {
                    duration = DateTime.Now.Subtract(start);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetIpGeolocationInfo: try#: {tryNo}, Exception: {ex.Message} at {ex.StackTrace}, duration: {duration}");
                    #endregion
                    continue;
                }
            }

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    IpGeolocationInfo info;

                    try
                    {
                        info = JsonConvert.DeserializeObject<IpGeolocationInfo>(content);
                        _elevation = info.sun_altitude;
                        _lastDataRead = DateTime.Now;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                            "GetIpGeolocationInfo: success: got " +
                            $"elevation: {Angle.FromDegrees(_elevation, Angle.AngleType.Alt).ToShortNiceString()} " +
                            $"after {tryNo + 1} tries " +
                            $"in {duration.ToMinimalString()}");
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                            $"GetIpGeolocationInfo: caught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        return;
                    }
                    return;
                }
                else
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetIpGeolocationInfo: try#: {tryNo}, HTTP failure: StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase} duration: {duration}");
                    #endregion
                }
            }
            else
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                    $"GetIpGeolocationInfo: try#: {tryNo}, HTTP response == null, duration: {duration}");
                #endregion
            }
        }

        private const string apiKey = "d6ce0c7ecb5c451ba2b462dfb5750364";

        private static readonly string URL = "https://api.ipgeolocation.io/astronomy?" +
            $"apiKey={apiKey}&" +
            $"lat={WiseSite.Instance.Latitude.Degrees}&" +
            $"long={WiseSite.Instance.Longitude.Degrees}";

        private DateTime _lastDataRead = DateTime.MinValue;
        private TimeSpan _timeBetweenIpGeolocationReads = TimeSpan.FromMinutes(2);
        private TimeSpan _maxTimeBetweenIpGeolocationReads = TimeSpan.FromMinutes(5);
        public HttpClient _client;
        public Timer _periodicReadTimer;
        private bool _initialized = false;
        private int _readingInterlock = 0;
        private static double _elevation = Double.NaN;

        public void Init()
        {
            if (_initialized)
                return;

            WiseName = "SunSensor";

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.Timeout = TimeSpan.FromSeconds(10);

            _periodicReadTimer = new Timer(new TimerCallback(PeriodicReader));
            _periodicReadTimer.Change(0, (int) _timeBetweenIpGeolocationReads.TotalMilliseconds);

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