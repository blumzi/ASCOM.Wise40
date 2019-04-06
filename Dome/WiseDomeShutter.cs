using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Net.Http;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class WiseDomeShutter : WiseObject
    {
        private static bool _initialized = false;
        public static WisePin openPin, closePin;
        private static Hardware.Hardware hw = Hardware.Hardware.Instance;
        private Debugger debugger = Debugger.Instance;

        public string _ipAddress;
        public int _lowestValue, _highestValue;
        public bool ShutterWebClientEnabled = false;
        public bool _syncVentWithShutter = false;

        private ShutterState _state = ShutterState.shutterError;

        private static TimeSpan _simulatedAge = new TimeSpan(0, 0, 3);

        List<WisePin> shutterPins;

        public WebClient webClient = null;

        private static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public class WebClient
        {

            public class CommunicationAttempt
            {
                public int Id;
                public DateTime Time;
                private static int nextId = 0;

                public CommunicationAttempt()
                {
                    Id = nextId++;
                    Time = DateTime.Now;
                }
            }
            public static int _totalCommunicationAttempts = 0, _failedCommunicationAttempts = 0;
            public static CommunicationAttempt _lastSuccessfullAttempt, _lastFailedAttempt;

            private static System.Threading.Timer _periodicWebReadTimer;
            private static string _uri;
            private static DateTime _lastReadingTime = DateTime.MinValue;
            private static HttpClient _client;
            private static int _lastReading, _prevReading = Int32.MinValue;
            private static Debugger debugger = Debugger.Instance;
            private static TimeSpan _maxAge = new TimeSpan(0, 0, 30);
            private static object _lock = new object();
            private static WiseDomeShutter _wisedomeshutter;
            private static ShutterState _prevState = ShutterState.shutterError;
            private bool _wifiIsWorking;
            public DateTime _startOfShutterMotion = DateTime.MinValue;

            public enum Pacing { None, Slow, Fast };
            private Pacing _pacing = Pacing.None;
            private Dictionary<Pacing, int> PacingToMillis = new Dictionary<Pacing, int>()
            {
                { Pacing.Slow, 15 * 1000 },
                { Pacing.Fast,  3 * 1000 },
            };
            private int _timeoutMillis;

            public WebClient(string address, WiseDomeShutter wisedomeshutter)
            {
                _client = new HttpClient();
                _client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
                _client.DefaultRequestHeaders.ConnectionClose = false;
                _uri = String.Format("http://{0}/range", address);

                _periodicWebReadTimer = new System.Threading.Timer(new TimerCallback(PeriodicReader));

                _client.Timeout = TimeSpan.FromSeconds(1.5);
                SetPacing(Pacing.Slow);
                _wisedomeshutter = wisedomeshutter;
            }

            public bool WiFiIsWorking
            {
                get
                {
                    return _wifiIsWorking;
                }

                set
                {
                    _wifiIsWorking = value;
                }
            }

            public void SetPacing(Pacing pacing)
            {
                if (_pacing != pacing)
                {
                    _pacing = pacing;
                    _timeoutMillis = PacingToMillis[_pacing] - 50;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "Changed  pacing to {0} ({1} millis)",
                        _pacing, _timeoutMillis);
                    #endregion
                    _periodicWebReadTimer.Change(0, _timeoutMillis);
                }
            }

            public int ShutterRange
            {
                get
                {
                    if (DateTime.Now.Subtract(_lastReadingTime) <= _maxAge)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugDome, "Returning: {0}", _lastReading);
                        #endregion
                        return _lastReading;
                    }
                    return -1;
                }
            }

            private static void PeriodicReader(object state)
            {
                int reading = GetWebShutterPosition().GetAwaiter().GetResult();
                const int maxTravelTimeSeconds = 22;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "PeriodicReader: cm: {0}", reading);
                #endregion
                if (! Instance.webClient.WiFiIsWorking)
                {
                    _wisedomeshutter._state = ShutterState.shutterError;
                    _prevState = ShutterState.shutterError;
                    
                    if (DateTime.Now.Subtract(Instance.webClient._startOfShutterMotion).TotalSeconds > maxTravelTimeSeconds)
                    {
                        _wisedomeshutter.Stop(string.Format("{0} seconds passed from startOfMotion", maxTravelTimeSeconds));
                    }
                    return;
                }

                _lastReadingTime = DateTime.Now;
                _lastReading = reading;

                if (Math.Abs(reading - _wisedomeshutter._lowestValue) <= 5 && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterClosed;
                    if (_prevState != ShutterState.shutterClosed)
                        _wisedomeshutter.Stop("Shutter closed");
                }
                else if (Math.Abs(reading - _wisedomeshutter._highestValue) <= 5 && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterOpen;
                    if (_prevState != ShutterState.shutterOpen)
                        _wisedomeshutter.Stop("Shutter opened");
                }
                else if (_prevReading == Int32.MinValue)
                {
                    _wisedomeshutter._state = ShutterState.shutterError;
                }
                else
                {
                    if (openPin.isOn && closePin.isOn)
                        _wisedomeshutter._state = ShutterState.shutterError;
                    else if (openPin.isOn)
                        _wisedomeshutter._state = ShutterState.shutterOpening;
                    else if (closePin.isOn)
                        _wisedomeshutter._state = ShutterState.shutterClosing;
                }

                _prevReading = _lastReading;
                _prevState = _wisedomeshutter._state;
            }

            public TimeSpan TimeSinceLastReading
            {
                get
                {
                    return DateTime.Now.Subtract(_lastReadingTime);
                }
            }

            public static async Task<int> GetWebShutterPosition()
            {
                int ret = -7;

                _totalCommunicationAttempts++;
                CommunicationAttempt communicationAttempt = new CommunicationAttempt();

                DateTime start = DateTime.Now;
                TimeSpan duration;
                try
                {
                    var response = await _client.GetAsync(_uri);
                    duration = DateTime.Now.Subtract(start);

                    Instance.webClient.WiFiIsWorking = true;

                    if (response.IsSuccessStatusCode)
                    {
                        string prefix = "<!DOCTYPE HTML>\r\n<html>";
                        string suffix = "</html>\r\n";

                        _lastSuccessfullAttempt = communicationAttempt;
                        string content = await response.Content.ReadAsStringAsync();
                        if (content.StartsWith(prefix) && content.EndsWith(suffix))
                        {

                            content = content.Remove(0, prefix.Length);
                            content = content.Remove(content.IndexOf(suffix[0]));
                            ret = Convert.ToInt32(content);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: Success: ret = {0}, duration: {1}", ret, duration);
                            #endregion
                        }
                    }
                    else
                    {
                        _failedCommunicationAttempts++;
                        _lastFailedAttempt = communicationAttempt;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: Azimuth: {0}, FAILED: StatusCode: {1}, ReasonPhrase: {2} duration: {3}",
                            WiseDome.Instance.Azimuth.ToNiceString(), response.StatusCode, response.ReasonPhrase, duration);
                        #endregion
                    }
                } catch (Exception ex)
                {
                    duration = DateTime.Now.Subtract(start);
                    Instance.webClient.WiFiIsWorking = false;
                    _failedCommunicationAttempts++;
                    _lastFailedAttempt = communicationAttempt;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: Azimuth: {0}, Exception: {1}, duration: {2}",
                        WiseDome.Instance.Azimuth.ToNiceString(), ex.Message, duration);
                    #endregion
                }
                return ret;
            }
        }

        public WiseDomeShutter() { }

        static WiseDomeShutter() { }

        public void Stop(string reason)
        {
            ShutterState prev = State;
            bool openPinWasOn = openPin.isOn;
            bool closePinWasOn = closePin.isOn;

            if (openPinWasOn || closePinWasOn)
            {
                openPin.SetOff();
                closePin.SetOff();
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Shutter, new Activity.ShutterActivity.EndParams()
                    {
                        endState = Activity.State.Succeeded,
                        endReason = reason,
                        percentOpen = PercentOpen,
                    });
                webClient.SetPacing(WebClient.Pacing.Slow);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                    "Stop: was moving (openPin: {0}, closePin: {1})",
                    openPinWasOn.ToString(), closePinWasOn.ToString());
                #endregion
            }
            else
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                    "Stop: was NOT moving.");
                #endregion
            }
        }

        public bool CloseEnough(int value, int limit)
        {
            return Math.Abs(value - limit) < 5;
        }

        public void StartClosing()
        {
            if (openPin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: ignored (openPin is ON)");
                #endregion debug
                return;
            }

            if (webClient.WiFiIsWorking)
            {
                int rangeCm = RangeCm;
                if (rangeCm != -1 && CloseEnough(rangeCm, _lowestValue))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: ignored (close enough to closed)");
                    #endregion debug
                    return;
                }
            }

            if (!closePin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: started closing the shutter");
                #endregion debug
                activityMonitor.NewActivity(new Activity.ShutterActivity(new Activity.ShutterActivity.StartParams
                {
                    operation = ShutterState.shutterClosing,
                    start = PercentOpen,
                    target = 0,
                }));
                webClient._startOfShutterMotion = DateTime.Now;
                closePin.SetOn();
                webClient.SetPacing(WebClient.Pacing.Fast);
            }
        }

        public void StartOpening()
        {
            if (closePin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: ignored (closePin is ON)");
                #endregion debug
                return;
            }

            if (webClient.WiFiIsWorking)
            {
                int rangeCm = RangeCm;
                if (rangeCm != -1 && CloseEnough(rangeCm, _highestValue))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: ignored (close enough to open)");
                    #endregion debug
                    return;
                }
            }

            if (!openPin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: started opening the shutter");
                #endregion debug
                activityMonitor.NewActivity(new Activity.ShutterActivity(new Activity.ShutterActivity.StartParams
                {
                    operation = ShutterState.shutterOpening,
                    start = PercentOpen,
                    target = 100,
                }));
                webClient._startOfShutterMotion = DateTime.Now;
                openPin.SetOn();
                webClient.SetPacing(WebClient.Pacing.Fast);
            }
        }

        public ShutterState State
        {
            get
            {
                ShutterState ret = ShutterState.shutterError;

                if (openPin.isOn && closePin.isOn)
                    ret = ShutterState.shutterError;
                else if (openPin.isOn)
                    return ShutterState.shutterOpening;
                else if (closePin.isOn)
                    return ShutterState.shutterClosing;
                else
                {
                    int rangeCm = RangeCm;

                    if (rangeCm == -1)
                        ret = ShutterState.shutterError;
                    else if (CloseEnough(rangeCm, _lowestValue))
                        ret = ShutterState.shutterClosed;
                    else if (CloseEnough(rangeCm, _highestValue))
                        ret = ShutterState.shutterOpen;
                }

                _state = ret;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "State: {0}", _state.ToString());
                #endregion
                return _state;
            }

            set
            {
                _state = value;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            try
            {
                openPin = new WisePin("ShutterOpen", hw.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut, controlled: true);
                closePin = new WisePin("ShutterClose", hw.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut, controlled: true);
            }
            catch (Exception ex)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "WiseDomeShutter.init: Exception: {0}.", ex.Message);
            }

            try
            {
                openPin.SetOff();
                closePin.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) { }

            ReadProfile();
            if (ShutterWebClientEnabled && _ipAddress != string.Empty)
                webClient = new WebClient(_ipAddress, this);
            shutterPins = new List<WisePin> { openPin, closePin };

            _state = ShutterState.shutterError;

            _initialized = true;
        }

        public void Dispose()
        {
            try
            {
                openPin.SetOff();
                closePin.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) { }
        }

        private static readonly Lazy<WiseDomeShutter> lazy = new Lazy<WiseDomeShutter>(() => new WiseDomeShutter()); // Singleton

        public static WiseDomeShutter Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public List<WisePin> pins()
        {
            return shutterPins;
        }

        public void ReadProfile()
        {
            bool defaultSyncVentWithShutter = (WiseSite.OperationalMode == WiseSite.OpMode.WISE) ? false : true;

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                ShutterWebClientEnabled = Convert.ToBoolean(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_UseWebClient, string.Empty, false.ToString()));
                _ipAddress = driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_IPAddress, string.Empty, "").Trim();
                _highestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_HighestValue, string.Empty, "-1"));
                _lowestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_LowestValue, string.Empty, "-1"));
                _syncVentWithShutter = Convert.ToBoolean(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.Dome_SyncVentWithShutter, string.Empty, defaultSyncVentWithShutter.ToString()));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_UseWebClient, ShutterWebClientEnabled.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_IPAddress, _ipAddress.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_HighestValue, _highestValue.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_LowestValue, _lowestValue.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.Dome_SyncVentWithShutter, _syncVentWithShutter.ToString());
            }
        }

        public bool IsMoving
        {
            get
            {
                var ret = openPin.isOn || closePin.isOn;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "ShutterIsMoving: {0}", ret.ToString());
                #endregion
                return ret;
            }
        }

        public int RangeCm
        {
            get
            {
                return webClient.ShutterRange;
            }
        }

        public int PercentOpen
        {
            get
            {
                int currentRange = RangeCm;

                if (currentRange == -1)
                    return -1;

                return (int)((currentRange - _lowestValue) * 100.0) / ((_highestValue - _lowestValue));
            }
        }
    }
}
