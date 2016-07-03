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
        private Wise40.Dome _dome = null;
        private bool _connected = false;
        private ASCOM.Astrometry.NOVAS.NOVAS31 novas31;
        private AstroUtils astroutils;
        AutoResetEvent _arrived = new AutoResetEvent(false);
        private bool _slewStarted = false;
        private Debugger debugger;
        private uint _debugLevel;

        public DomeSlaveDriver(Debugger debugger)
        {
            try
            {
                _dome = new Dome();
            } catch (Exception e)
            {
                throw new Exception(string.Format("DomeSlaveDriver: Cannot get a Dome instance ({0}", e.Message));
            }
            novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new AstroUtils();
            _arrived = _dome.arrived;
            this.debugger = debugger;

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: constructed");
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Connect(bool connect)
        {
            if (connect == true)
            {
                try
                {
                    _dome.Connected = true;
                    _dome.Slaved = true;
                    _dome.debugLevel = _debugLevel;
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("DomeSlaveDriver: Cannot enslave dome ({0})", e.Message));
                }
            }
            else
            {
                if (_dome != null)
                {
                    if (_dome.Slaved)
                        _dome.Slaved = false;
                    _dome.Connected = false;
                }
            }
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: Connect({0}) done", connect);
        }

        public void SlewStartAsync(Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;

            WiseSite.Instance.prepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                WiseSite.Instance.astrometricAccuracy,
                0, 0,
                WiseSite.Instance.onSurface,
                ra.Hours, dec.Degrees,
                WiseSite.Instance.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            _slewStarted = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "DomeSlaveDriver: Asking dome to SlewToAzimuth({0})", new Angle(az));
            _dome.SlewToAzimuth(az);
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
            if (_dome.CanPark) {
                _dome.Slaved = false;   // dome cannot park while slaved
                _slewStarted = true;
                _dome.Park();
            }
        }

        public uint debugLevel
        {
            set
            {
                _debugLevel = value;
            }
        }
    }
}
