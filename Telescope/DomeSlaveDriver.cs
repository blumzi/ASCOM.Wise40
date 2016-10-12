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
    public class DomeSlaveDriver: IConnectable
    {
        private static WiseDome wisedome = WiseDome.Instance;
        private static WiseTele wisetele = WiseTele.Instance;
        private bool _connected = false;
        private ASCOM.Astrometry.NOVAS.NOVAS31 novas31;
        private AstroUtils astroutils;
        AutoResetEvent _arrivedAtAz = new AutoResetEvent(false);
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
            init();
            wisedome.Connect(value);
            _connected = value;
        }

        public void SlewToAz(double az)
        {
            if (wisetele == null)
                wisetele = WiseTele.Instance;
            
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to SlewToAzimuth({0})", new Angle(az, Angle.Type.Az));
            #endregion
            try
            {
                wisedome.SlewToAzimuth(az);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Waiting for dome to arrive to target az");
                #endregion
                _arrivedAtAz.WaitOne();
            }
            catch (Exception ex)
            {
                wisedome.AbortSlew();
                throw ex;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome arrived to {0}", new Angle(az, Angle.Type.Az));
            #endregion
        }

        public void Park()
        {
            if (wisetele == null)
                wisetele = WiseTele.Instance;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to Park");
            #endregion
            try
            {
                wisedome.Park();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Waiting for dome to arrive to target az");
                #endregion
                _arrivedAtAz.WaitOne();
            }
            catch (Exception ex)
            {
                wisedome.AbortSlew();
                throw ex;
            }
            
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome is Parked");
            #endregion
        }

        public void SlewToAz(Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;

            wisesite.prepareRefractionData(WiseTele.Instance._calculateRefraction);
            instance.novas31.Equ2Hor(instance.astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            SlewToAz(az);
        }

        public bool Slewing
        {
            get
            {
                return wisetele.slewers.Active(Slewers.Type.Dome);
            }
        }

        public void AbortSlew()
        {
            wisedome.AbortSlew();
            wisetele.slewers.Delete(Slewers.Type.Dome, ref wisetele._driverInitiatedSlewing);
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

        public void StopShutter()
        {
            wisedome.ShutterStop();
        }

        public string ShutterStatus
        {
            get
            {
                if (!Connected)
                    return "Not connected";

                switch (wisedome.ShutterStatus)
                {
                    //case DeviceInterface.ShutterState.shutterClosed:
                    //    return "Closed";
                    case DeviceInterface.ShutterState.shutterClosing:
                        return "Closing";
                    //case DeviceInterface.ShutterState.shutterOpen:
                    //    return "Open";
                    case DeviceInterface.ShutterState.shutterOpening:
                        return "Opening";
                    default:
                        return null;
                }
            }
        }
    }
}
