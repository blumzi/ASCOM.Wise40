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
        private static readonly Hardware.Hardware hw = Hardware.Hardware.Instance;
        private readonly Debugger debugger = Debugger.Instance;

        public string IpAddress;
        public int lowestRange, highestRange;
        public bool ShutterWebClientEnabled = false;
        public bool syncVentWithShutter = false;

        public static System.Threading.Timer Simulation_Timer = new System.Threading.Timer(Simulation_OnTimer);
        public static TimeSpan Simulation_FullTravelTimeSpan = new TimeSpan(0, 0, 5);
        public static ShutterState Simulation_State = ShutterState.shutterClosed;
        public static DateTime Simulation_StartOfMovement;
        public static double Simulation_Precent;
        public static int Simulation_Range;
        public static bool Simulation_IsMoving = false;
        private static ShutterState prevState;

        private ShutterState _state = ShutterState.shutterError;
        private string _stateReason;

        private List<WisePin> shutterPins;

        public WebClient webClient = null;

        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        public static System.Threading.Timer shutterMotionTimer = new System.Threading.Timer(OnShutterMotionTimer);
        private readonly TimeSpan shutterMotionTravelTime = TimeSpan.FromSeconds(22);
        private ShutterState shutterMotionEndState;     // the shutter state after 22 seconds, when we have no WiFi

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
            private static readonly Debugger debugger = Debugger.Instance;
            private static TimeSpan _maxAge = new TimeSpan(0, 0, 30);
            private static WiseDomeShutter _wisedomeshutter;

            public enum Pacing { None, Slow, Fast };
            private Pacing _pacing = Pacing.None;
            private readonly Dictionary<Pacing, int> PacingToMillis = new Dictionary<Pacing, int>()
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

            public bool WiFiIsWorking { get; set; }

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

                if (! Instance.webClient.WiFiIsWorking)
                {
                    _wisedomeshutter._state = ShutterState.shutterError;
                    _wisedomeshutter._stateReason = "PeriodicReader: WiFi is not working";
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                        $"PeriodicReader: " +
                        $"state: {_wisedomeshutter._state} " +
                        $"reason: {_wisedomeshutter._stateReason.Replace("PeriodicReader: ", "")}");
                    #endregion

                    _periodicWebReadTimer.Change(WebClient._timeoutMillis, 0);
                    return;
                }

                _lastReadingTime = DateTime.Now;
                _lastReading = reading;

                if (CloseEnough(reading, _wisedomeshutter.lowestRange) && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterClosed;
                    _wisedomeshutter._stateReason =
                        $"PeriodicReader: Shutter at {reading} (close enough to lowest value {_wisedomeshutter.lowestRange}) and not moving";
                    if (_wisedomeshutter.IsMoving)
                        _wisedomeshutter.Stop("Shutter closed");
                }
                else if (CloseEnough(reading, _wisedomeshutter.highestRange) && reading == _prevReading)
                {
                    _wisedomeshutter._state = ShutterState.shutterOpen;
                    _wisedomeshutter._stateReason =
                        $"PeriodicReader: Shutter at {reading} (close enough to highest value {_wisedomeshutter.highestRange}) and not moving";
                    if (_wisedomeshutter.IsMoving)
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
                    $"state: {_wisedomeshutter._state} "+
                    $"reason: {_wisedomeshutter._stateReason.Replace("PeriodicReader: ", "")}");
                #endregion
                _prevReading = _lastReading;
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
                        const string prefix = "<!DOCTYPE HTML>\r\n<html>";
                        const string suffix = "</html>\r\n";

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
                    Simulation_StopTimer();
                return;
            }

            if (! (openPin.isOn || closePin.isOn))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"Stop({reason}): both openPin and closePin were OFF.");
                #endregion
                return;
            }

            if (openPin.isOn)
            {
                openPin.SetOff();
                shutterMotionEndState = ShutterState.shutterOpen;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "Stop: stopped opening");
                #endregion
            }

            if (closePin.isOn)
            {
                closePin.SetOff();
                shutterMotionEndState = ShutterState.shutterClosed;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "Stop: stopped closing");
                #endregion
            }

            activityMonitor.EndActivity(ActivityMonitor.ActivityType.Shutter, new Activity.Shutter.EndParams()
            {
                endState = Activity.State.Succeeded,
                endReason = reason,
                percentOpen = PercentOpen,
            });
            webClient.SetPacing(WebClient.Pacing.Slow);
        }

        public static bool CloseEnough(int value, int limit)
        {
            return Math.Abs(value - limit) < 10;
        }

        public void StartClosing(string reason)
        {
            if (Simulated)
            {
                Simulation_State = ShutterState.shutterClosing;
                Simulation_StartOfMovement = DateTime.Now;
                Simulation_Timer.Change(Convert.ToInt32(Simulation_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
                return;
            }

            if (openPin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartClosing(reason: {reason}): ignored (openPin is ON)");
                #endregion debug
                return;
            }

            if (webClient.WiFiIsWorking)
            {
                int rangeCm = RangeCm;
                if (rangeCm != -1 && CloseEnough(rangeCm, lowestRange))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartClosing(reason: {reason}): ignored (close enough to closed)");
                    #endregion debug
                    return;
                }
            }

            if (!closePin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartClosing(reason: {reason}): started closing the shutter");
                #endregion debug
                activityMonitor.NewActivity(new Activity.Shutter(new Activity.Shutter.StartParams
                {
                    operation = ShutterState.shutterClosing,
                    start = PercentOpen,
                    target = 0,
                }));
                closePin.SetOn();
                shutterMotionTimer.Change((int) shutterMotionTravelTime.TotalMilliseconds, Timeout.Infinite);
                webClient.SetPacing(WebClient.Pacing.Fast);
            }
        }

        public void StartOpening(string reason)
        {
            if (Simulated)
            {
                Simulation_State = ShutterState.shutterOpening;
                Simulation_StartOfMovement = DateTime.Now;
                Simulation_Timer.Change(Convert.ToInt32(Simulation_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
                return;
            }

            if (closePin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartOpening(reason: {reason}): ignored (closePin is ON)");
                #endregion debug
                return;
            }

            if (webClient.WiFiIsWorking)
            {
                int rangeCm = RangeCm;
                if (rangeCm != -1 && CloseEnough(rangeCm, highestRange))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter,
                        $"StartOpening(reason: {reason}): ignored ({rangeCm} is close enough to {highestRange})");
                    #endregion debug
                    return;
                }
            }

            if (!openPin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartOpening(reason: {reason}): started opening the shutter");
                #endregion debug
                activityMonitor.NewActivity(new Activity.Shutter(new Activity.Shutter.StartParams
                {
                    operation = ShutterState.shutterOpening,
                    start = PercentOpen,
                    target = 100,
                }));
                openPin.SetOn();
                shutterMotionTimer.Change((int)shutterMotionTravelTime.TotalMilliseconds, Timeout.Infinite);
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
                    return Simulation_State;

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
                else if (Instance.webClient.WiFiIsWorking)
                {
                    int rangeCm = RangeCm;

                    if (rangeCm == -1)
                    {
                        ret = ShutterState.shutterError;
                        _stateReason = "WiFiIsWorking but rangeCM == -1 (unknown problem)";
                    }
                    else if (CloseEnough(rangeCm, lowestRange))
                    {
                        ret = ShutterState.shutterClosed;
                        _stateReason = $"range: {rangeCm}cm is close enough to lower limit: {lowestRange}cm";
                    }
                    else if (CloseEnough(rangeCm, highestRange))
                    {
                        ret = ShutterState.shutterOpen;
                        _stateReason = $"range: {rangeCm}cm is close enough to highest limit: {highestRange}cm";
                    }
                    else
                    {
                        ret = ShutterState.shutterOpen;
                        _stateReason = $"range: {rangeCm}cm is between lowest: {lowestRange}cm and highest: {highestRange}cm";
                    }
                }
                else
                {
                    ret = shutterMotionEndState;
                    _stateReason = "motion timer has expired";
                }

                _state = ret;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"State: {_state} (reason: {_stateReason})");
                #endregion
                prevState = ret;
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
            if (ShutterWebClientEnabled && ! string.IsNullOrEmpty(IpAddress))
                webClient = new WebClient(IpAddress, this);
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

        public List<WisePin> Pins()
        {
            return shutterPins;
        }

        public void ReadProfile()
        {
            bool defaultSyncVentWithShutter = WiseSite.OperationalMode == WiseSite.OpMode.WISE;

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                ShutterWebClientEnabled = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_UseWebClient, string.Empty, false.ToString()));
                IpAddress = driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, string.Empty, "").Trim();
                highestRange = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_HighestValue, string.Empty, "-1"));
                lowestRange = Convert.ToInt32(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_LowestValue, string.Empty, "-1"));
                syncVentWithShutter = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.Dome_SyncVentWithShutter, string.Empty, defaultSyncVentWithShutter.ToString()));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_UseWebClient, ShutterWebClientEnabled.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, IpAddress);
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_HighestValue, highestRange.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_LowestValue, lowestRange.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.Dome_SyncVentWithShutter, syncVentWithShutter.ToString());
            }
        }

        public bool IsMoving
        {
            get
            {
                if (Simulated)
                {
                    return Simulation_State == ShutterState.shutterOpening || Simulation_State == ShutterState.shutterClosing;
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
                    return Simulation_Range;

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

                return (int)((currentRange - lowestRange) * 100.0) / (highestRange - lowestRange);
            }
        }

        public static void Simulation_StartTimer()
        {
            Simulation_Timer.Change(Convert.ToInt32(Simulation_FullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
            Simulation_IsMoving = true;
            Simulation_StartOfMovement = DateTime.Now;
        }

        public static void Simulation_CalculatePosition()
        {
            Simulation_Precent = (DateTime.Now.Subtract(Simulation_StartOfMovement).TotalMilliseconds / Simulation_FullTravelTimeSpan.TotalMilliseconds) * 100;
            Simulation_Range = Instance.lowestRange + (int) ((Instance.highestRange - Instance.lowestRange) * ((1 + Simulation_Precent) / 100));
        }

        public static void Simulation_StopTimer()
        {
            Simulation_Timer.Change(Timeout.Infinite, Timeout.Infinite);
            Simulation_IsMoving = false;
            Simulation_CalculatePosition();
        }

        public static void OnShutterMotionTimer(object state)
        {
            shutterMotionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Instance.Stop($"Motion period ({Instance.shutterMotionTravelTime}) has ended");
        }

        public static void Simulation_OnTimer(object state)
        {
            if (!Simulated)
                return;

            Simulation_StopTimer();

            switch(Simulation_State)
            {
                case ShutterState.shutterClosing:
                    Simulation_State = ShutterState.shutterClosed;
                    break;

                case ShutterState.shutterOpening:
                    Simulation_State = ShutterState.shutterOpen;
                    break;
            }
        }
    }
}
