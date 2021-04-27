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
    public class DomeSimulation : WiseObject
    {
        public System.Threading.Timer _timer;
        public readonly TimeSpan _fullTravelTimeSpan = new TimeSpan(0, 0, 5);
        public ShutterState _state = ShutterState.shutterClosed;
        public DateTime _startOfMovement;
        public double _precent;
        public int _range;
        public bool _isMoving = false;
        public ShutterState _prevState;
        private readonly WiseDomeShutter _wiseDomeShutter;

        public DomeSimulation(WiseDomeShutter wiseDomeShutter)
        {
            _wiseDomeShutter = wiseDomeShutter;
            _timer = new System.Threading.Timer(OnTimer, this, Timeout.Infinite, Timeout.Infinite);
        }

        public static void OnTimer(object state)
        {
            if (!Simulated)
                return;

            DomeSimulation simulation = WiseDomeShutter.Instance.simulation;

            simulation.StopTimer();

            switch (simulation._state)
            {
                case ShutterState.shutterClosing:
                    simulation._state = ShutterState.shutterClosed;
                    break;

                case ShutterState.shutterOpening:
                    simulation._state = ShutterState.shutterOpen;
                    break;
            }
        }

        public void StartTimer()
        {
            _timer.Change(Convert.ToInt32(_fullTravelTimeSpan.TotalMilliseconds), Timeout.Infinite);
            _isMoving = true;
            _startOfMovement = DateTime.Now;
        }

        public void CalculatePosition()
        {
            _precent = (DateTime.Now.Subtract(_startOfMovement).TotalMilliseconds / _fullTravelTimeSpan.TotalMilliseconds) * 100;
            _range = _wiseDomeShutter.lowestRange + (int)((_wiseDomeShutter.highestRange - _wiseDomeShutter.lowestRange) * ((1 + _precent) / 100));
        }

        public void StopTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _isMoving = false;
            CalculatePosition();
        }
    }

    public class WiseDomeShutter : WiseObject
    {
        private static bool _initialized = false;
        public static WisePin openPin, closePin;
        private static readonly Hardware.Hardware hw = Hardware.Hardware.Instance;
        private readonly Debugger debugger = Debugger.Instance;

        public string ipAddress;
        public int lowestRange, highestRange;
        public bool ShutterWebClientEnabled = false;
        public bool syncVentWithShutter = false;

        public DomeSimulation simulation;

        private ShutterState _state = ShutterState.shutterError;
        private string _stateReason;

        private List<WisePin> shutterPins;

        public PeriodicHttpFetcher periodicHttpFetcher;

        private readonly Dictionary<string, TimeSpan> pacingTimeSpans = new Dictionary<string, TimeSpan>() {
            { "slow", TimeSpan.FromSeconds(15) },
            { "fast", TimeSpan.FromSeconds(3)  },
        };

        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        public static System.Threading.Timer shutterMotionTimer = new System.Threading.Timer(OnShutterMotionTimer);
        private readonly TimeSpan shutterMotionTravelTime = TimeSpan.FromSeconds(22);
        private ShutterState shutterMotionEndState;     // the shutter state after 22 seconds, when we have no WiFi

        public WiseDomeShutter()
        {
            simulation = new DomeSimulation(this);
        }

        static WiseDomeShutter() { }

        public void Stop(string reason)
        {
            if (Simulated)
            {
                if (IsMoving)
                    simulation.StopTimer();
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

            periodicHttpFetcher.Period = pacingTimeSpans["slow"];
        }

        public static bool CloseEnough(int value, int limit)
        {
            return Math.Abs(value - limit) < 10;
        }

        public void StartClosing(string reason)
        {
            if (Simulated)
            {
                simulation._state = ShutterState.shutterClosing;
                simulation.StartTimer();
                return;
            }

            if (openPin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartClosing(reason: {reason}): ignored (openPin is ON)");
                #endregion debug
                return;
            }

            if (periodicHttpFetcher.Alive)
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

                periodicHttpFetcher.Period = pacingTimeSpans["fast"];
            }
        }

        public void StartOpening(string reason)
        {
            if (Simulated)
            {
                simulation._state = ShutterState.shutterOpening;
                simulation.StartTimer();
                return;
            }

            if (closePin.isOn)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"StartOpening(reason: {reason}): ignored (closePin is ON)");
                #endregion debug
                return;
            }

            if (periodicHttpFetcher.Alive)
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
                periodicHttpFetcher.Period = pacingTimeSpans["fast"];
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
                    return simulation._state;

                ShutterState ret;

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
                else if (periodicHttpFetcher.Alive)
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
                return _state;
            }

            set
            {
                _state = value;
            }
        }

        public void Init()
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
            if (ShutterWebClientEnabled && !string.IsNullOrEmpty(ipAddress))
            {
                periodicHttpFetcher = new PeriodicHttpFetcher(
                    "ShutterRange",
                    $"http://{ipAddress}/range",
                    pacingTimeSpans["slow"]
                );
            }
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

                lazy.Value.Init();
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
                ipAddress = driverProfile.GetValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, string.Empty, "").Trim();
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
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.DomeShutter_IPAddress, ipAddress);
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
                    return simulation._state == ShutterState.shutterOpening || simulation._state == ShutterState.shutterClosing;
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
                    return simulation._range;

                const string prefix = "<!DOCTYPE HTML>\r\n<html>";
                const string suffix = "</html>\r\n";

                if (periodicHttpFetcher.Alive)
                {
                    string result = periodicHttpFetcher.Result;

                    if (result.StartsWith(prefix) && result.EndsWith(suffix))
                    {
                        result = result.Remove(0, prefix.Length);
                        result = result.Remove(result.IndexOf(suffix[0]));

                        if (!string.IsNullOrEmpty(result))
                            return Convert.ToInt32(result);
                    }
                }

                return -1;
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

        public static void OnShutterMotionTimer(object state)
        {
            shutterMotionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Instance.Stop($"Motion period ({Instance.shutterMotionTravelTime.ToMinimalString()}) has ended");
        }
    }
}
