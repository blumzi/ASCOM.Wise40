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

        internal static string shutterIPAddressProfileName = "ShutterIPAddress";
        internal static string shutterHighestValueProfileName = "ShutterHighestValue";
        internal static string shutterLowestValueProfileName = "ShutterLowestValue";

        private static WiseDomeShutter _instance; // Singleton
        private static object syncObject = new object();

        private System.Threading.Timer _timer;
        private int _timeout;

        private ShutterState _state = ShutterState.shutterClosed; // till we know better ...
        
        private static WiseObject wiseobject = new WiseObject();
        private static TimeSpan _simulatedAge = new TimeSpan(0, 0, 3);

        List<WisePin> shutterPins;

        public WebClient webClient = null;

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
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "GetShutterPosition: got {0}", ret);
                            #endregion
                        }
                    }
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "GetShutterPosition: Exception: {0}", ex.Message);
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ShutterStop: _state was {0}, now is {1}", prev, _state);
            #endregion
        }

        public void StartClosing()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "StartClosing: started closing the shutter");
            #endregion debug
            closePin.SetOn();
            _state = ShutterState.shutterClosing;
            _timer.Change(_timeout, Timeout.Infinite);
        }

        public void StartOpening()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "StartOpening: started opening the shutter");
            #endregion debug
            openPin.SetOn();
            _state = ShutterState.shutterOpening;
            _timer.Change(_timeout, Timeout.Infinite);
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
        private void onTimer(object state)
        {
            if (IsMoving)
                Stop();
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
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseDomeShutter.init: Exception: {0}.", ex.Message);
            }
            _state = State;
            _timer = new System.Threading.Timer(new TimerCallback(onTimer));
            _timeout = (Simulated ? 10 : 25) * 1000;
            openPin.SetOff();
            closePin.SetOff();

            if (_ipAddress != string.Empty)
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
                _ipAddress = driverProfile.GetValue(Const.wiseDomeDriverID, shutterIPAddressProfileName, string.Empty, "").Trim();
                _highestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, shutterHighestValueProfileName, string.Empty, "-1"));
                _lowestValue = Convert.ToInt32(driverProfile.GetValue(Const.wiseDomeDriverID, shutterLowestValueProfileName, string.Empty, "-1"));
            }
        }

        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                driverProfile.WriteValue(Const.wiseDomeDriverID, shutterIPAddressProfileName, _ipAddress.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, shutterHighestValueProfileName, _highestValue.ToString());
                driverProfile.WriteValue(Const.wiseDomeDriverID, shutterLowestValueProfileName, _lowestValue.ToString());
            }
        }

        public bool IsMoving
        {
            get
            {
                var ret = openPin.isOn || closePin.isOn;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ShutterIsMoving: {0}", ret.ToString());
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
