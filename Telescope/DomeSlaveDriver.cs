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
        AutoResetEvent _arrived = new AutoResetEvent(false);
        private bool _slewStarted = false;
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
            _arrived = new AutoResetEvent(false);
            wisedome.init();
            wisedome.SetArrivedEvent(_arrived);
            wisedome.SetLogger(WiseTele.Instance.traceLogger);
            wisesite.init();

            _initialized = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: init() done.");
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
            _connected = value;
            wisedome.Connect(value);
        }

        public void SlewStartAsync(Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;

            wisesite.prepareRefractionData();
            instance.novas31.Equ2Hor(instance.astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            _slewStarted = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: Asking dome to SlewToAzimuth({0})", new Angle(az));
            wisedome.SlewToAzimuth(az);
        }

        public void SlewWait()
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: Waiting for dome to arrive to target az");
            _arrived.WaitOne();
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: Dome arrived to target az");
            _slewStarted = false;
        }

        public bool Slewing
        {
            get
            {
                return _slewStarted;
            }
        }

        public void SlewToParkStart()
        {
            if (wisedome.CanPark) {
                _slewStarted = true;
                wisedome.Park();
            }
        }

        public void AbortSlew()
        {
            wisedome.AbortSlew();
        }

        public string Azimuth
        {
            get
            {
                if (!Connected)
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
