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
        public bool _useShutterWebClient = false;
        public bool _syncVentWithShutter = false;

        private static WiseDomeShutter _instance; // Singleton
        private static object syncObject = new object();

        private System.Threading.Timer _timer;
        private TimeSpan _timeToFullShutterMovement;

        private ShutterState _state = ShutterState.shutterClosed; // till we know better ...

        private static WiseObject wiseobject = new WiseObject();
        private static TimeSpan _simulatedAge = new TimeSpan(0, 0, 3);

        List<WisePin> shutterPins;

        public WebClient webClient = null;

        private static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        private DateTime _startOfMovement;

        public class WebClient
        {
            private static System.Threading.Timer _periodicWebReadTimer;
            private static string _uri;
            private static DateTime _lastReadingTime = DateTime.MinValue;
            private static HttpClient _client;
            private static int _lastReading;
            private static Debugger debugger = Debugger.Instance;
            private static TimeSpan _maxAge = new TimeSpan(0, 0, 30);
            private static object _lock = new object();

            public WebClient(string address)
            {
                _client = new HttpClient();
                _client.Timeout = TimeSpan.FromSeconds(30);
                _client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/html"));
                _client.DefaultRequestHeaders.ConnectionClose = true;
                _uri = String.Format("http://{0}/range", address);

                _periodicWebReadTimer = new System.Threading.Timer(new TimerCallback(PeriodicReader));
                _periodicWebReadTimer.Change(1000, 5000);
            }

            public int ShutterRange
            {
                get
                {
                    if (DateTime.Now.Subtract(_lastReadingTime).TotalSeconds <= _maxAge.TotalSeconds)
                        return _lastReading;
                    return -1;
                }
            }

            private static void PeriodicReader(object state)
            {
                int res = GetWebShutterPosition().GetAwaiter().GetResult();
                if (res > 0)
                {
                    _lastReadingTime = DateTime.Now;
                    _lastReading = res;
                }
            }

            public static async Task<int> GetWebShutterPosition()
            {
                int ret = -7;

                try
                {
                    var response = await _client.GetAsync(_uri);

                    //will throw an exception if not successful
                    response.EnsureSuccessStatusCode();

                    string prefix = "<!DOCTYPE HTML>\r\n<html>";
                    string suffix = "</html>\r\n";

                    string content = await response.Content.ReadAsStringAsync();
                    if (content.StartsWith(prefix) && content.EndsWith(suffix))
                    {

                        content = content.Remove(0, prefix.Length);
                        content = content.Remove(content.IndexOf(suffix[0]));
                        ret = Convert.ToInt32(content);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: got {0}", ret);
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: Exception: {0}", ex.Message);
                    #endregion
                }
                return ret;
            }
        }

        public WiseDomeShutter() { }

        static WiseDomeShutter() { }

        public void Stop()
        {
            ShutterState prev = State;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            activityMonitor.EndActivity(ActivityMonitor.Activity.Shutter);

            switch (State)
            {
                case ShutterState.shutterOpening:
                    openPin.SetOff();
                    if (!CanUseWebShutter)
                        State = ShutterState.shutterOpen;
                    break;

                case ShutterState.shutterClosing:
                    closePin.SetOff();
                    if (!CanUseWebShutter)
                        State = ShutterState.shutterClosed;
                    break;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "ShutterStop: _state was {0}, now is {1}", prev, _state);
            #endregion
        }

        public void StartClosing()
        {
            if (CanUseWebShutter)
            {
                int percent = PercentOpen;

                if (percent == 0)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: shutter at {0}%, doing nothing", percent);
                    #endregion debug
                    return;
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: started closing the shutter");
            #endregion debug
            activityMonitor.StartActivity(ActivityMonitor.Activity.Shutter);
            closePin.SetOn();
            _startOfMovement = DateTime.Now;
            _state = ShutterState.shutterClosing;
            _timer.Change(0, 1000);
        }

        public void StartOpening()
        {
            if (CanUseWebShutter)
            {
                int percent = PercentOpen;

                if (percent == 100)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: shutter at {0}%, doing nothing", percent);
                    #endregion debug
                    return;
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: started opening the shutter");
            #endregion debug
            activityMonitor.StartActivity(ActivityMonitor.Activity.Shutter);
            openPin.SetOn();
            _startOfMovement = DateTime.Now;
            _state = ShutterState.shutterOpening;
            _timer.Change(0, 1000);
        }

        public ShutterState State
        {
            get
            {
                if (openPin.isOn)
                    return ShutterState.shutterOpening;

                if (closePin.isOn)
                    return ShutterState.shutterClosing;

                if (!CanUseWebShutter)
                    return _state;

                // Both motors are OFF and we can use the webShutter
                int percentOpen;
                if ((percentOpen = PercentOpen) == -1)
                    return _state;

                return (percentOpen == 0) ? ShutterState.shutterClosed : ShutterState.shutterOpen;
            }

            set
            {
                _state = value;
            }
        }

        /// <summary>
        /// Called every second after StartOpening or StartClosing.
        /// Stops the timer if shutter is open/closed.
        /// </summary>
        /// <param name="state"></param>
        private void onTimer(object sender)
        {
            DateTime now = DateTime.Now;
            ShutterState prevState = State;
            bool done = false;
            int percent = -1;

            if (CanUseWebShutter)
                percent = PercentOpen;

            if (percent != -1)
            {
                if (prevState == ShutterState.shutterOpening && percent == 100)
                {
                    State = ShutterState.shutterOpen;
                    done = true;
                }

                if (prevState == ShutterState.shutterClosing && percent == 0)
                {
                    State = ShutterState.shutterClosed;
                    done = true;
                }
            }
            else
            {
                if (prevState == ShutterState.shutterClosing &&
                        (now.Subtract(_startOfMovement).TotalSeconds >= _timeToFullShutterMovement.TotalSeconds))
                {
                    State = ShutterState.shutterClosed;
                    done = true;
                }

                if (prevState == ShutterState.shutterOpening &&
                        (now.Subtract(_startOfMovement).TotalSeconds >= _timeToFullShutterMovement.TotalSeconds))
                {
                    State = ShutterState.shutterOpen;
                    done = true;
                }
            }

            if (done)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _startOfMovement = DateTime.MinValue;
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

            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            _timeToFullShutterMovement = Simulated ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(25);
            try
            {
                openPin.SetOff();
                closePin.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) { }

            ReadProfile();
            if (_useShutterWebClient && _ipAddress != string.Empty)
                webClient = new WebClient(_ipAddress);
            shutterPins = new List<WisePin> { openPin, closePin };

            _state = ShutterState.shutterClosed;

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

        public static WiseDomeShutter Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseDomeShutter();
                        _instance.init();
                    }
                }
                return _instance;
            }
        }

        public List<WisePin> pins()
        {
            return shutterPins;
        }

        public void ReadProfile()
        {
            bool defaultSyncVentWithShutter = (WiseSite.Instance.OperationalMode == WiseSite.OpMode.WISE) ? false : true;

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                _useShutterWebClient = Convert.ToBoolean(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_UseWebClient, string.Empty, false.ToString()));
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
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_UseWebClient, _useShutterWebClient.ToString());
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

        public bool CanUseWebShutter
        {
            get
            {
                return webClient != null && _lowestValue != -1 && _highestValue != -1;
            }
        }

        public int PercentOpen
        {
            get
            {
                if (!CanUseWebShutter)
                    return -1;

                int currentRange = webClient.ShutterRange;
                if (currentRange == -1)
                    return -1;

                return (int)((currentRange - _lowestValue) * 100.0) / ((_highestValue - _lowestValue));
            }
        }
    }
}
