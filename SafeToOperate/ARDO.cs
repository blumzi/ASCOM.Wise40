using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class ARDO : WiseObject
    {
        private const string _address = "2.55.90.188";
        private const string _port = "10500";
        private const string _uri = "cgi-bin/cgiLastData";

        private readonly string URL = $"http://{_address}:{_port}/{_uri}";
        private DateTime _lastDataRead = DateTime.MinValue;
        private readonly Dictionary<string, string> sensorData = new Dictionary<string, string>();
        public HttpClient _client;
        public System.Threading.Timer _periodicWebReadTimer;
        public TimeSpan _interval = TimeSpan.FromMinutes(1);
        private bool _initialized = false;
        private int _reading = 0;
        private readonly Debugger debugger = Debugger.Instance;
        public DateTime updatedAtUT;
        public WeatherLogger _weatherLogger;

        private static readonly Lazy<ARDO> lazy = new Lazy<ARDO>(() => new ARDO()); // Singleton

        public static ARDO Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public void Refresh()
        {
            if (_lastDataRead != DateTime.MinValue && DateTime.Now.Subtract(_lastDataRead) < _interval)
                return;

            PeriodicReader(new object());
        }

        private void PeriodicReader(object state)
        {
            if (Interlocked.CompareExchange(ref _reading, 1, 0) == 0)
                return;

            bool succeeded = GetARDOInfo().GetAwaiter().GetResult();
            if (succeeded)
                Instance._lastDataRead = DateTime.Now;

            Interlocked.Exchange(ref _reading, 0);
        }

        public static async Task<bool> GetARDOInfo()
        {
            bool succeeded = false;
            int maxTries = 10, tryNo;

            DateTime start = DateTime.Now;
            TimeSpan duration = TimeSpan.Zero;

            HttpResponseMessage response = null;
            for (tryNo = 0; tryNo < maxTries; tryNo++)
            {
                try
                {
                    response = await Instance._client.GetAsync(Instance.URL).ConfigureAwait(false);
                    duration = DateTime.Now.Subtract(start);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    duration = DateTime.Now.Subtract(start);
                    #region debug
                    Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetARDOInfo: try#: {tryNo}, HttpRequestException: {ex.Message} at {ex.StackTrace}, duration: {duration}");
                    #endregion
                    continue;
                }
                catch (Exception ex)
                {
                    duration = DateTime.Now.Subtract(start);
                    #region debug
                    Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetARDOInfo: try#: {tryNo}, Exception: {ex.Message} at {ex.StackTrace}, duration: {duration}");
                    #endregion
                    continue;
                }
            }

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    foreach (string line in content.Split('\n').ToList())
                    {
                        List<string> words = line.Split('=').ToList();
                        if (words.Count == 2)
                            Instance.sensorData[words[0]] = words[1];
                    }
                    DateTime.TryParse(Instance.sensorData["dataGMTTime"] + "Z", out Instance.updatedAtUT);

                    Instance._weatherLogger?.Log(new Dictionary<string, string>()
                        {
                            ["Temperature"] = Instance.sensorData["temp"],
                            ["SkyAmbientTemp"] = Instance.sensorData["clouds"],
                            ["Humidity"] = Instance.sensorData["hum"],
                            ["DewPoint"] = Instance.sensorData["dewp"],
                            ["WindSpeed"] = Instance.sensorData["wind"],
                        }, Instance.updatedAtUT.ToLocalTime());
                    #region debug
                    Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetARDOInfo: try#: {tryNo}, Success, content: [{content}], duration: {duration}");
                    #endregion
                }
                else
                {
                    #region debug
                    Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"GetARDOInfo: try#: {tryNo}, HTTP failure: StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase} duration: {duration}");
                    #endregion
                }
            }
            else
            {
                #region debug
                Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                    $"GetARDOInfo: try#: {tryNo}, HTTP response == null, duration: {DateTime.Now.Subtract(start)}");
                #endregion
            }
            return succeeded;
        }

        public void init()
        {
            if (_initialized)
                return;

            WiseName = "ARDO";
            _periodicWebReadTimer = new System.Threading.Timer(PeriodicReader);

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
            _client.Timeout = TimeSpan.FromSeconds(10);

            _periodicWebReadTimer = new System.Threading.Timer(new TimerCallback(PeriodicReader));

            Refresh();
            _weatherLogger = new WeatherLogger("ARDO");

            _initialized = true;
        }

        public ARDORawData Digest
        {
            get
            {
                return new ARDORawData()
                {
                    UpdatedAtUT = Instance.updatedAtUT,
                    AgeInSeconds = DateTime.Now.Subtract(updatedAtUT).TotalSeconds,
                    SensorData = Instance.sensorData,
                };
            }
        }
    }

    public class ARDORawData
    {
        public string Name = "ARDO";
        public string Vendor = "Lunatico Astronomia";
        public string Model = "AAG Cloudwatcher";
        public DateTime UpdatedAtUT;
        public double AgeInSeconds;
        public Dictionary<string, string> SensorData;
    }

    public class ARDORefresher : Sensor
    {
        private readonly ARDO ardo = ARDO.Instance;

        public ARDORefresher(WiseSafeToOperate instance) :
            base("ARDORefresher",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "", "", "", "",
                instance)
        {
        }

        public override string UnsafeReason()
        {
            return string.Empty;
        }

        public override Reading GetReading()
        {
            ardo.Refresh();
            return null;
        }

        public override object Digest()
        {
            return ardo.Digest;
        }

        public override string MaxAsString
        {
            get { return ""; }
            set { }
        }

        public override void WriteSensorProfile() { }
        public override void ReadSensorProfile() { }

        public override string Status
        {
            get
            {
                return "";
            }
        }
    }
}
