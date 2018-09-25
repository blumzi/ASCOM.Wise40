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
        public static WisePin openPin, closePin;
        private static Hardware.Hardware hw = Hardware.Hardware.Instance;
        private Debugger debugger = Debugger.Instance;

        public string _ipAddress;
        public int _lowestValue, _highestValue;
        public bool _useShutterWebClient = false;

        private static WiseDomeShutter _instance; // Singleton
        private static object syncObject = new object();

        private System.Threading.Timer _timer;
        private TimeSpan _timeToFullShutterMovement;

        private ShutterState _state = ShutterState.shutterClosed; // till we know better ...
        
        private static WiseObject wiseobject = new WiseObject();
        private static TimeSpan _simulatedAge = new TimeSpan(0, 0, 3);

        List<WisePin> shutterPins;

        public WebClient webClient = null;

        private ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public class WebClient
        {
            private static System.Threading.Timer _timer;
            private static string _url;
            private static DateTime _lastReading = DateTime.MinValue;
            private static HttpClient _client;
            private static int _value;
            private static Debugger debugger = Debugger.Instance;
            private static TimeSpan _maxAge = new TimeSpan(0, 0, 60);
            private static object _lock = new object();

            public TimeSpan Age
            {
                get
                {
                    return DateTime.Now.Subtract(_lastReading);
                }
            }

            public bool Alive
            {
                get
                {
                    return Age <= _maxAge;
                }
            }

            public int Value
            {
                get
                {
                    if (! Alive)
                        return -1;
                    return _value;
                }
            }

            public WebClient(string address)
            {
                _client = new HttpClient();
                _url = String.Format("http://{0}/encoder", address);
                Task.Run(() =>
                {
                    _timer = new System.Threading.Timer(PeriodicallyReadShutterPosition);
                    _timer.Change(0, 5000);
                });
            }

            private static void PeriodicallyReadShutterPosition(object state)
            {
                lock (_lock)
                {
                    int res = GetShutterPosition().GetAwaiter().GetResult();
                    if (res != -1)
                    {
                        _lastReading = DateTime.Now;
                        _value = res;
                    }
                }
            }

            public static async Task<int> GetShutterPosition()
            {
                int ret = -7;
                
                try
                {
                    var result = await _client.GetAsync(_url);
                    if (result.IsSuccessStatusCode)
                    {
                        string prefix = "<!DOCTYPE HTML>\r\n<html>";
                        string suffix = "</html>\r\n";

                        string reply = await result.Content.ReadAsStringAsync();
                        if (reply.StartsWith(prefix) && reply.EndsWith(suffix))
                        {

                            reply = reply.Remove(0, prefix.Length);
                            reply = reply.Remove(reply.IndexOf(suffix[0]));
                            ret = Convert.ToInt32(reply);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "GetShutterPosition: got {0}", ret);
                            #endregion
                        }
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartClosing: started closing the shutter");
            #endregion debug
            activityMonitor.StartActivity(ActivityMonitor.Activity.Shutter);
            closePin.SetOn();
            _state = ShutterState.shutterClosing;
            _timer.Change((int) _timeToFullShutterMovement.TotalMilliseconds, Timeout.Infinite);
        }

        public void StartOpening()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "StartOpening: started opening the shutter");
            #endregion debug
            activityMonitor.StartActivity(ActivityMonitor.Activity.Shutter);
            openPin.SetOn();
            _state = ShutterState.shutterOpening;
            _timer.Change((int) _timeToFullShutterMovement.TotalMilliseconds, Timeout.Infinite);
        }

        public ShutterState State
        {
            get
            {
                if (openPin.isOn)
                    return ShutterState.shutterOpening;
                else if (closePin.isOn)
                    return ShutterState.shutterClosing;
                else if (!CanUseWebShutter)
                    return _state;

                // Both motors are OFF and we can use the webShutter
                int percentOpen = -1;
                if ((percentOpen = Percent) == -1)
                    return _state;
                else if (percentOpen <= 2)
                    return ShutterState.shutterClosed;
                else if (percentOpen >= 98)
                    return ShutterState.shutterOpen;
                else
                    return _state;
            }

            set
            {
                _state = value;
            }
        }

        /// <summary>
        /// Called after time-to-{open,close} passes.
        /// </summary>
        /// <param name="state"></param>
        private void onTimer(object sender)
        {
            ShutterState prev = State;
            if (IsMoving)
                Stop();
            if (prev == ShutterState.shutterClosing)
                State = ShutterState.shutterClosed;
            else if (prev == ShutterState.shutterOpening)
                State = ShutterState.shutterOpen;
        }

        public void init()
        {
            try
            {
                openPin = new WisePin("ShutterOpen", hw.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                closePin = new WisePin("ShutterClose", hw.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut);
            }
            catch (Exception ex)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, "WiseDomeShutter.init: Exception: {0}.", ex.Message);
            }
            //_state = State;
            _state = ShutterState.shutterClosed;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            _timeToFullShutterMovement = Simulated ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(25);
            openPin.SetOff();
            closePin.SetOff();

            if (_useShutterWebClient && _ipAddress != string.Empty)
                webClient = new WebClient(_ipAddress);
            shutterPins = new List<WisePin> { openPin, closePin };
        }

        public void Dispose()
        {
            openPin.SetOff();
            closePin.SetOff();
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
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                _useShutterWebClient = Convert.ToBoolean(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_UseWebClient, string.Empty, false.ToString()));
                _ipAddress = driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_IPAddress, string.Empty, "").Trim();
                _highestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_HighestValue, string.Empty, "-1"));
                _lowestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.DomeShutter_LowestValue, string.Empty, "-1"));
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
                return false;
                //return webClient != null && _lowestValue != -1 && _highestValue != -1;
            }
        }

        public TimeSpan Age
        {
            get
            {
                if (Simulated)
                    return _simulatedAge;

                if (CanUseWebShutter)
                    return webClient.Age;

                return TimeSpan.Zero;
            }
        }

        public int Percent
        {
            get
            {
                if (!CanUseWebShutter)
                    return -1;

                int lastReadValue = webClient.Value;
                if (lastReadValue == -1)
                    return -1;

                return (int)((lastReadValue - _lowestValue) * 100.0) / ((_highestValue - _lowestValue));
            }
        }
    }
}
