using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Utilities;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.SafeToOperate;
using ASCOM.Wise40.Telescope;
using ASCOM.Wise40.Dome;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public class ObsMon
    {
        private static Dictionary<Const.TriStateStatus, string> severities = new Dictionary<Const.TriStateStatus, string>()
        {
            { Const.TriStateStatus.Normal, "I" },
            { Const.TriStateStatus.Good, "I" },
            { Const.TriStateStatus.Warning, "W" },
            { Const.TriStateStatus.Error, "E" },
        };

        private class Event
        {
            private DateTime _time;
            public Event()
            {
                _time = DateTime.Now;
            }
        };

        public class Monitor
        {
            public Monitor(string name, int max, Func<Const.TriStateStatus> isSafe)
            {
                _name = name;
                _maxEvents = max;
                _queue = new FixedSizedQueue<Event>(_maxEvents);
                _isSafe = isSafe;
                monitors.Add(_name, this);
            }

            public bool triggered
            {
                get
                {
                    return (_queue.MaxSize != 0) && (_queue.ToArray().Length == _queue.MaxSize);
                }
            }

            public void Reset()
            {
                _queue = new FixedSizedQueue<Event>(_maxEvents);
            }

            public Const.TriStateStatus Check()
            {
                if (_maxEvents == 0)
                    return Const.TriStateStatus.Normal;

                Const.TriStateStatus status = _isSafe();
                string severity = severities[status];

                if (! (status == Const.TriStateStatus.Good || status == Const.TriStateStatus.Normal))
                {
                    _queue.Enqueue(new Event());
                    Log(string.Format("\"{0}\" monitor is not safe (event #{1} of max {2})",
                        _name, _queue.ToArray().Length, _queue.MaxSize), status);
                }

                return status;
            }

            public bool Enabled
            {
                get
                {
                    return _maxEvents != 0;
                }
            }

            public bool Disabled
            {
                get
                {
                    return !Enabled;
                }
            }

            private string _name;
            private int _maxEvents;
            private FixedSizedQueue<Event> _queue;
            private Func<Const.TriStateStatus> _isSafe;
        }

        public Const.TriStateStatus cloudsAreSafe() {
            return monitors["Clouds"].Enabled ? wisesafe.isSafeCloudCover : Const.TriStateStatus.Normal;
        }
        public Const.TriStateStatus humidityIsSafe()
        {
            return monitors["Humidity"].Enabled ? wisesafe.isSafeHumidity : Const.TriStateStatus.Normal;
        }
        public Const.TriStateStatus rainIsSafe()
        {
            return monitors["Rain"].Enabled ? wisesafe.isSafeRain : Const.TriStateStatus.Normal;
        }
        public Const.TriStateStatus windIsSafe() {
            return monitors["Wind"].Enabled ? wisesafe.isSafeWindSpeed : Const.TriStateStatus.Normal;
        }
        public Const.TriStateStatus lightIsSafe() {
            return monitors["Light"].Enabled ? wisesafe.isSafeLight : Const.TriStateStatus.Normal;
        }
        public Const.TriStateStatus sunIsSafe() {
            return monitors["Sun"].Enabled ? wisesafe.isSafeSunElevation : Const.TriStateStatus.Normal;
        }

        internal static string driverID = "ASCOM.Wise40.ObservatoryMonitor";
        public static int _interval;
        public int _sunMaxEvents, _rainMaxEvents, _windMaxEvents, _humidityMaxEvents, _lightMaxEvents, _cloudMaxEvents;
        private static bool _enabled = true;
        private static bool _shuttingDown = false;
        
        private static WiseSafeToOperate wisesafe = WiseSafeToOperate.InstanceOpen;

        private static System.Threading.Timer monitoringTimer = new System.Threading.Timer(new TimerCallback(onTimer));
        private DateTime nextCheck;

        private static Monitor _sunMonitor, _rainMonitors, _windMonitor, _humidityMonitor, _lightMonitor, _cloudMonitor;
        private static Dictionary<string, Monitor> monitors = new Dictionary<string, Monitor>();
        private ObsMainForm _form;

        private string intervalProfileName = "Interval";
        private string lightEventsProfileName = "LightEvents";
        private string sunEventsProfileName = "SunEvents";
        private string windEventsProfileName = "WindEvents";
        private string rainEventsProfileName = "RainEvents";
        private string humidityEventsProfileName = "HumidityEvents";
        private string cloudEventsProfileName = "CloudEvents";

        private int _defaultInterval = 30;
        private int _defaultLightEvents = 3;
        private int _defaultSunEvents = 2;
        private int _defaultWindEvents = 4;
        private int _defaultRainEvents = 2;
        private int _defaultHumidityEvents = 2;
        private int _defaultCloudEvents = 5;

        private static Version version = new Version(0, 2);

        private static volatile ObsMon _instance; // Singleton
        private static object syncObject = new object();
        private static Version _version = new System.Version(2, 0);

        static ObsMon() { }
        public ObsMon() { }

        public static ObsMon Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new ObsMon();
                    }
                }
                return _instance;
            }
        }

        public void init(ObsMainForm form)
        {
            ReadProfile();

            _form = form;
            _cloudMonitor = new Monitor("Clouds", _cloudMaxEvents, cloudsAreSafe);
            _rainMonitors = new Monitor("Rain", _rainMaxEvents, rainIsSafe);
            _windMonitor = new Monitor("Wind", _windMaxEvents, windIsSafe);
            _lightMonitor = new Monitor("Light", _lightMaxEvents, lightIsSafe);
            _humidityMonitor = new Monitor("Humidity", _humidityMaxEvents, humidityIsSafe);
            _sunMonitor = new Monitor("Sun", _sunMaxEvents, sunIsSafe);

            if (!wisesafe.Connected)
                wisesafe.Connected = true;
            
            if (Enabled && OnDuty)
                monitoringTimer.Change(0, _interval);
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                _interval = 1000 * Convert.ToInt32(driverProfile.GetValue(driverID, intervalProfileName, string.Empty, _defaultInterval.ToString()));
                _lightMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, lightEventsProfileName, string.Empty, _defaultLightEvents.ToString()));
                _sunMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, sunEventsProfileName, string.Empty, _defaultSunEvents.ToString()));
                _windMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, windEventsProfileName, string.Empty, _defaultWindEvents.ToString()));
                _rainMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, rainEventsProfileName, string.Empty, _defaultRainEvents.ToString()));
                _humidityMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, humidityEventsProfileName, string.Empty, _defaultHumidityEvents.ToString()));
                _cloudMaxEvents = Convert.ToInt32(driverProfile.GetValue(driverID, cloudEventsProfileName, string.Empty, _defaultCloudEvents.ToString()));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, intervalProfileName, (_interval / 1000).ToString());
                driverProfile.WriteValue(driverID, lightEventsProfileName, _lightMaxEvents.ToString());
                driverProfile.WriteValue(driverID, sunEventsProfileName, _sunMaxEvents.ToString());
                driverProfile.WriteValue(driverID, windEventsProfileName, _windMaxEvents.ToString());
                driverProfile.WriteValue(driverID, rainEventsProfileName, _rainMaxEvents.ToString());
                driverProfile.WriteValue(driverID, humidityEventsProfileName, _humidityMaxEvents.ToString());
            }
        }

        private static void onTimer(object StateObject)
        {
            if (!(_enabled && _instance.OnDuty))
                return;

            bool triggered = false;            

            foreach (var key in monitors.Keys)
                monitors[key].Check();

            foreach (var key in monitors.Keys)
            {
                if (monitors[key].triggered)
                {
                    triggered = true;
                    break;
                }
            }

            if (triggered)
            {
                monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _instance.ParkAndClose();
                foreach (var key in monitors.Keys)
                    monitors[key].Reset();
                monitoringTimer.Change(_interval, _interval);
            }
            else
                Log("Ok");
            _instance.nextCheck = DateTime.Now.AddMilliseconds(_interval);
        }

        private static void Log(string msg, Const.TriStateStatus status = Const.TriStateStatus.Normal)
        {
            _instance._form.Log(string.Format("{0:-10} {1}", severities[status], msg));
        }

        public void ParkAndClose()
        {
            _shuttingDown = true;

            WiseTele wisetele = WiseTele.Instance;
            WiseDome wisedome = null;

            Log("Starting the shut-down sequence ...");
            wisetele.init();
            Log("Connecting the telescope ...");
            wisetele.Connected = true;
            
            wisedome = WiseDome.Instance;
            wisedome.init();
            Log("Connecting the dome ...");
            wisedome.Connected = true;

            Task.Run(() =>
            {
                if (wisetele.Slewing)
                {
                    Log("Aborting the telescope slew ...");
                    wisetele.AbortSlew();
                }
                wisetele.Tracking = false;
                if (!wisetele.AtPark)
                {
                    Log("Parking the telescope ...");
                    wisetele.Park();
                }
                if (!wisetele._enslaveDome && !wisedome.AtPark)
                {
                    Log("Parking the dome ...");
                    wisedome.Park();
                }

                Log("Closing the dome shutter ...");
                wisedome.CloseShutter();// If we could sense if the dome is closed, we could avoid waisting time here!
            }).ContinueWith((contTask) =>
            {
                Log("Shut down sequence completed.");
                _shuttingDown = false;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public int SecondsToNextCheck
        {
            get
            {
                return (nextCheck - DateTime.Now).Seconds;
            }
        }

        public bool OnDuty
        {
            get
            {
                return true;
                //return sunIsSafe() == Const.TriStateStatus.Good;
            }
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                if (value == _enabled)
                    return;

                _enabled = value;
                if (_enabled)
                    monitoringTimer.Change(0, _interval);
                else
                {
                    monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    foreach (var key in monitors.Keys)
                        monitors[key].Reset();
                }
            }
        }

        public bool ShuttingDown
        {
            get
            {
                return _shuttingDown;
            }
        }

        public string Status
        {
            get
            {
                if (!Enabled)
                    return "Disabled";
                else if (!OnDuty)
                    return "OffDuty";
                else if (ShuttingDown)
                    return "ShuttingDown";
                else
                    return "Monitoring";
            }
        }

        public Version Version
        {
            get
            {
                return _version;
            }
        }
    }
}
