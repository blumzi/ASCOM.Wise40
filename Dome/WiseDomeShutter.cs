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

        public static System.Threading.Timer _sim_Timer = new System.Threading.Timer(_sim_OnTimer);
        public static TimeSpan _sim_FullTravelTimeSpan = new TimeSpan(0, 0, 5);
        public static ShutterState _sim_State = ShutterState.shutterClosed;
        public static DateTime _sim_StartOfMovement;
        public static double _sim_Precent;
        public static int _sim_Range;
        public static bool _sim_IsMoving = false;

        private ShutterState _state = ShutterState.shutterError;
        private string _stateReason;

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
            public static int _timeoutMillis;

            public WebClient(string address, WiseDomeShutter wisedomeshutter)
            {
                _client = new HttpClient();
                _client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
                _client.DefaultRequestHeaders.ConnectionClose = false;
                _uri = $"http://{address}/range";

                _periodicWebReadTimer = new System.Threading.Timer(new TimerCallback(PeriodicReader));

                _client.Timeout = TimeSpan.FromSeconds(5);
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                        $"SetPacing: Changed  pacing to {_pacing} ({_timeoutMillis} millis)");
                    #endregion
                    _periodicWebReadTimer.Change(_timeoutMillis, 0);
                }
            }

            public int ShutterRange
            {
                get
                {
                    if (DateTime.Now.Subtract(_lastReadingTime) <= _maxAge)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"ShutterRange: returning: {_lastReading}");
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

                if (! Instance.webClient.WiFiIsWorking)
                {
                    _wisedomeshutter._state = ShutterState.shutterError;
                    _wisedomeshutter._stateReason = "PeriodicReader: WiFi is not working";
                    _prevState = ShutterState.shutterError;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                        $"PeriodicReader: " +
                        $"state: {_wisedomeshutter._state.ToString()} " +
                        $"reason: {_wisedomeshutter._stateReason.Replace("PeriodicReader: ", "")}");
                    #endregion

                    if (DateTime.Now.Subtract(Instance.webClient._startOfShutterMotion).TotalSeconds > maxTravelTimeSeconds)
                    {
                        _wisedomeshutter.Stop($"{maxTravelTimeSeconds} seconds passed from startOfMotion");
                    }
                    _periodicWebReadTimer.Change(WebClient._timeoutMillis, 0);
                    return;
                }

                _lastReadingTime = DateTime.Now;
                _lastReading = reading;

                if (_wisedomeshutter.CloseEnough(reading, _wisedomeshutter._lowestValue) && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterClosed;
                    _wisedomeshutter._stateReason =
                        $"PeriodicReader: Shutter at {reading} (close enough to lowest value {_wisedomeshutter._lowestValue}) and not moving";
                    if (_prevState != ShutterState.shutterClosed)
                        _wisedomeshutter.Stop("Shutter closed");
                }

                else if (_wisedomeshutter.CloseEnough(reading, _wisedomeshutter._highestValue) && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterOpen;
                    _wisedomeshutter._stateReason =
                        $"PeriodicReader: Shutter at {reading} (close enough to highest value {_wisedomeshutter._highestValue}) and not moving";
                    if (_prevState != ShutterState.shutterOpen)
                        _wisedomeshutter.Stop("Shutter opened");
                }
                else if (_prevReading == Int32.MinValue)
                {
                    _wisedomeshutter._state = ShutterState.shutterError;
                    _wisedomeshutter._stateReason = "PeriodicReader: _prevReading == Int32.MinValue";
                }
                else
                {
                    if (openPin.isOn && closePin.isOn)
                    {
                        _wisedomeshutter._state = ShutterState.shutterError;
                        _wisedomeshutter._stateReason = "PeriodicReader: Both openPin and closePin are ON!";
                    }
                    else if (openPin.isOn)
                    {
                        _wisedomeshutter._state = ShutterState.shutterOpening;
                        _wisedomeshutter._stateReason = "PeriodicReader: openPin is ON!";
                    }
                    else if (closePin.isOn)
                    {
                        _wisedomeshutter._state = ShutterState.shutterClosing;
                        _wisedomeshutter._stateReason = "PeriodicReader: closePin is ON!";
                    }
                }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, 
                    $"PeriodicReader: at {reading}, " +
                    $"prevReading: {_prevReading} " +
                    $"state: {_wisedomeshutter._state.ToString()} "+
                    $"reason: {_wisedomeshutter._stateReason.Replace("PeriodicReader: ", "")}");
                #endregion
                _prevReading = _lastReading;
                _prevState = _wisedomeshutter._state;
                _periodicWebReadTimer.Change(_timeoutMillis, 0);
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
                int ret = -1;
                int maxTries = 10, tryNo;

                _totalCommunicationAttempts++;
                CommunicationAttempt attempt = new CommunicationAttempt();

                DateTime start = DateTime.Now;
                TimeSpan duration = TimeSpan.Zero;

                HttpResponseMessage response = null;
                for (tryNo = 0; tryNo < maxTries; tryNo++)
                {
                    string preamble = "GetShutterPosition: " +
                        $"attempt: {attempt.Id}, " +
                        $"try#: {tryNo}, " +
                        $"Azimuth: {WiseDome.Instance.Azimuth.ToNiceString()} ";

                    try
                    {
                        response = await _client.GetAsync(_uri);
                        duration = DateTime.Now.Subtract(start);
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        duration = DateTime.Now.Subtract(start);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                            preamble + $"HttpRequestException = {ex.Message}, duration: {duration}");
                        #endregion
                        continue;
                    }
                    catch(TaskCanceledException ex)
                    {
                        duration = DateTime.Now.Subtract(start);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                            preamble + $"Timedout ({ex.Message}), duration: {duration}");
                        #endregion
                        continue;
                    }
                    catch (Exception ex)
                    {
                        duration = DateTime.Now.Subtract(start);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                            preamble + $"Exception = {ex.Message}, duration: {duration}");
                        #endregion
                        continue;
                    }
                }

                if (response != null)
                {

                    Instance.webClient.WiFiIsWorking = true;

                    if (response.IsSuccessStatusCode)
                    {
                        string prefix = "<!DOCTYPE HTML>\r\n<html>";
                        string suffix = "</html>\r\n";

                        _lastSuccessfullAttempt = attempt;
                        string content = await response.Content.ReadAsStringAsync();
                        if (content.StartsWith(prefix) && content.EndsWith(suffix))
                        {

                            content = content.Remove(0, prefix.Length);
                            content = content.Remove(content.IndexOf(suffix[0]));
                            ret = Convert.ToInt32(content);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: " + 
                                $"attempt: {attempt.Id}, " +
                                $"try#: {tryNo}, " +
                                $"Success = {ret}, " +
                                $"duration: {duration}");
                            #endregion
                        }
                    }
                    else
                    {
                        _failedCommunicationAttempts++;
                        _lastFailedAttempt = attempt;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: " +
                            $"attempt: {attempt.Id}, " +
                            $"try#: {tryNo}, " +
                            $"Azimuth: {WiseDome.Instance.Azimuth.ToNiceString()}, " +
                            $"HTTP failure: StatusCode: {response.StatusCode}, ReasonPhrase: {response.ReasonPhrase} " +
                            $"duration: {duration}");
                        #endregion
                    }
                }
                else
                {
                    Instance.webClient.WiFiIsWorking = false;
                    _failedCommunicationAttempts++;
                    _lastFailedAttempt = attempt;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: attempt: {0}, try#: {1}, Azimuth: {2}, HTTP response == null, duration: {3}",
                        attempt.Id, tryNo, WiseDome.Instance.Azimuth.ToNiceString(), DateTime.Now.Subtract(start));
                    #endregion
                }
                return ret;
            }
        }

        public WiseDomeShutter() { }

        static WiseDomeShutter() { }

        public void Stop(string reason)
        {
            if (Simulated)
            {
                if (IsMoving)
                    _sim_stopTimer();
                return;
            }

            ShutterState prev = State;
            bool openPinWasOn = openPin.isOn;
            bool closePinWasOn = closePin.isOn;

            if (openPinWasOn || closePinWasOn)
            {
                openPin.SetOff();
                closePin.SetOff();
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Shutter, new Activity.Shutter.EndParams()
                    {
                        endState = Activity.State.Succeeded,
                        endReason = reason,
                        percentOpen = PercentOpen,
                    });
                webClient.SetPacing(WebClient.Pacing.Slow);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                    $"Stop: was moving (openPin: {openPinWasOn}, closePin: {closePinWasOn})");
                #endregion
            }
            else
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "Stop: was NOT moving.");
                #endregion
            }
        }

        public bool CloseEnough(int value, int limit)
        {
            return Math.Abs(value - limit) < 10;
        }

        public void StartClosing()
        {
            if (Simulated)
            {
                _sim_State = ShutterState.shutterClosing;
                _sim_StartOfMovement = DateTime.Now;
                _sim_Timer.Change(Convert.ToInt32(_sim_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
                return;
            }

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
                activityMonitor.NewActivity(new Activity.Shutter(new Activity.Shutter.StartParams
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
            if (Simulated)
            {
                _sim_State = ShutterState.shutterOpening;
                _sim_StartOfMovement = DateTime.Now;
                _sim_Timer.Change(Convert.ToInt32(_sim_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
                return;
            }

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
                activityMonitor.NewActivity(new Activity.Shutter(new Activity.Shutter.StartParams
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

        public string StateReason
        {
            get
            {
                return _stateReason;
            }
        }

        public ShutterState State
        {
            get
            {
                if (Simulated)
                    return _sim_State;

                ShutterState ret = ShutterState.shutterError;

                if (openPin.isOn && closePin.isOn)
                {
                    ret = ShutterState.shutterError;
                    _stateReason = "Both openPin and closePin are ON";
                }
                else if (openPin.isOn)
                {
                    ret = ShutterState.shutterOpening;
                    _stateReason = "openPin is ON";
                }
                else if (closePin.isOn)
                {
                    ret = ShutterState.shutterClosing;
                    _stateReason = "closePin is ON";
                }
                else
                {
                    int rangeCm = RangeCm;

                    if (rangeCm == -1)
                    {
                        ret = ShutterState.shutterError;
                        _stateReason = Instance.webClient.WiFiIsWorking ? "rangeCM == -1 (unknown problem)" : "WiFi problem";
                    }
                    else if (CloseEnough(rangeCm, _lowestValue))
                    {
                        ret = ShutterState.shutterClosed;
                        _stateReason = $"range: {rangeCm}cm is close enough to lower limit: {_lowestValue}cm";
                    }
                    else if (CloseEnough(rangeCm, _highestValue))
                    {
                        ret = ShutterState.shutterOpen;
                        _stateReason = $"range: {rangeCm}cm is close enough to highest limit: {_highestValue}cm";
                    } else
                    {
                        ret = ShutterState.shutterOpen;
                        _stateReason = $"range: {rangeCm}cm is between lowest: {_lowestValue}cm and highest: {_highestValue}cm";
                    }
                }

                _state = ret;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"State: {_state} (reason: {_stateReason})");
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

            if (Simulated)
            {
                _initialized = true;
                return;
            }

            try
            {
                openPin = new WisePin("ShutterOpen", hw.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut, controlled: true);
                closePin = new WisePin("ShutterClose", hw.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut, controlled: true);
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"WiseDomeShutter.init: Caught: {ex.Message} at\n{ex.StackTrace}");
                #endregion
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
                ShutterWebClientEnabled = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_UseWebClient, string.Empty, false.ToString()));
                _ipAddress = driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, string.Empty, "").Trim();
                _highestValue = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_HighestValue, string.Empty, "-1"));
                _lowestValue = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_LowestValue, string.Empty, "-1"));
                _syncVentWithShutter = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.Dome_SyncVentWithShutter, string.Empty, defaultSyncVentWithShutter.ToString()));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_UseWebClient, ShutterWebClientEnabled.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, _ipAddress.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_HighestValue, _highestValue.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_LowestValue, _lowestValue.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.Dome_SyncVentWithShutter, _syncVentWithShutter.ToString());
            }
        }

        public bool IsMoving
        {
            get
            {
                if (Simulated)
                {
                    return _sim_State == ShutterState.shutterOpening || _sim_State == ShutterState.shutterClosing;
                }

                var ret = openPin.isOn || closePin.isOn;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"IsMoving: {ret}");
                #endregion
                return ret;
            }
        }

        public int RangeCm
        {
            get
            {
                if (Simulated)
                    return _sim_Range;

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

        public static void _sim_startTimer()
        {
            _sim_Timer.Change(Convert.ToInt32(_sim_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
            _sim_IsMoving = true;
            _sim_StartOfMovement = DateTime.Now;
        }

        public static void _sim_CalculatePosition()
        {
            _sim_Precent = (DateTime.Now.Subtract(_sim_StartOfMovement).TotalMilliseconds / _sim_FullTravelTimeSpan.TotalMilliseconds) * 100;
            _sim_Range = Instance._lowestValue + (int) ((Instance._highestValue - Instance._lowestValue) * (1 + _sim_Precent / 100));
        }

        public static void _sim_stopTimer()
        {
            _sim_Timer.Change(Timeout.Infinite, Timeout.Infinite);
            _sim_IsMoving = false;
            _sim_CalculatePosition();
        }

        public static void _sim_OnTimer(object state)
        {
            if (!Simulated)
                return;

            _sim_stopTimer(); ;

            switch(_sim_State)
            {
                case ShutterState.shutterClosing:
                    _sim_State = ShutterState.shutterClosed;
                    break;

                case ShutterState.shutterOpening:
                    _sim_State = ShutterState.shutterOpen;
                    break;
            }
        }
    }
}
