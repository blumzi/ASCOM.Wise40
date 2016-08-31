using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using ASCOM.Astrometry.AstroUtils;
using System.Threading;

namespace ASCOM.Wise40
{
    class DomeSlaveDriver: IConnectable
    {
        private static WiseDome wisedome = WiseDome.Instance;
        private bool _connected = false;
        private ASCOM.Astrometry.NOVAS.NOVAS31 novas31;
        private AstroUtils astroutils;
        AutoResetEvent _arrivedAtAz = new AutoResetEvent(false);
        private bool _slewing = false;
        private Debugger debugger;

        public static readonly DomeSlaveDriver instance = new DomeSlaveDriver(); // Singleton
        private static WiseSite wisesite = WiseSite.Instance;

        private bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static DomeSlaveDriver()
        {
        }

        public DomeSlaveDriver()
        {
        }

        public static DomeSlaveDriver Instance
        {
            get
            {
                return instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            debugger = Debugger.Instance;
            instance.novas31 = new Astrometry.NOVAS.NOVAS31();
            instance.astroutils = new AstroUtils();
            _arrivedAtAz = new AutoResetEvent(false);
            wisedome.init();
            wisedome.SetArrivedAtAzEvent(_arrivedAtAz);
            wisedome.SetLogger(WiseTele.Instance.traceLogger);
            wisesite.init();

            _initialized = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: init() done.");
            #endregion
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Connect(bool value)
        {
            if (!_initialized)
                init();
            wisedome.Connect(value);
            _connected = value;
        }

        public void SlewToCoords(Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;

            if (WiseTele._driverInitiatedSlewing)
                WiseTele.activeSlewers.Add(ActiveSlewers.SlewerType.SlewerDome);

            wisesite.prepareRefractionData(WiseTele.Instance._calculateRefraction);
            instance.novas31.Equ2Hor(instance.astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            _slewing = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to SlewToAzimuth({0})", new Angle(az, Angle.Type.Az));
            #endregion
            wisedome.SlewToAzimuth(az);            
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Waiting for dome to arrive to target az");
            #endregion
            _arrivedAtAz.WaitOne();
            _slewing = false;

            if (WiseTele._driverInitiatedSlewing)
                WiseTele.activeSlewers.Delete(ActiveSlewers.SlewerType.SlewerDome, ref WiseTele._driverInitiatedSlewing);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome arrived to {0}", new Angle(az, Angle.Type.Az));
            #endregion
        }

        public bool Slewing
        {
            get
            {
                return _slewing;
            }
        }

        public void SlewToPark()
        {
            if (wisedome.CanPark) {
                _slewing = true;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to Park()");
                #endregion
                wisedome.Park();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Waiting for dome to arrive Park");
                #endregion
                _arrivedAtAz.WaitOne();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome arrived at Park.");
                #endregion
                _slewing = false;
            }
        }

        public void AbortSlew()
        {
            wisedome.AbortSlew();
            WiseTele.activeSlewers.Delete(ActiveSlewers.SlewerType.SlewerDome, ref WiseTele._driverInitiatedSlewing);
        }

        public string Azimuth
        {
            get
            {
                if (!wisedome.Connected)
                    return "Not connected";
                if (!wisedome.Calibrated)
                    return "Not calibrated";

                return wisedome.Azimuth.ToNiceString();
            }
        }

        public string Status
        {
            get
            {
                if (!Connected)
                    return "Not connected";

                return wisedome.Status;
            }
        }

        public void OpenShutter()
        {
            wisedome.OpenShutter();
        }

        public void CloseShutter()
        {
            wisedome.CloseShutter();
        }

        public string ShutterStatus
        {
            get
            {
                if (!Connected)
                    return "Not connected";

                switch (wisedome.ShutterStatus)
                {
                    case DeviceInterface.ShutterState.shutterClosed:
                        return "Closed";
                    case DeviceInterface.ShutterState.shutterClosing:
                        return "Closing";
                    case DeviceInterface.ShutterState.shutterOpen:
                        return "Open";
                    case DeviceInterface.ShutterState.shutterOpening:
                        return "Opening";
                    default:
                        return "Unknown";
                }
            }
        }
    }
}
