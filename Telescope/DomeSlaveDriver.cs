﻿using System;
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
    public class DomeSlaveDriver : IConnectable
    {
        private static readonly WiseDome wisedome = WiseDome.Instance;
        private bool _connected = false;
        //private ASCOM.Astrometry.NOVAS.NOVAS31 novas31 = new Astrometry.NOVAS.NOVAS31();
        private readonly SafeNovas31 safeNovas31 = new SafeNovas31();
        private readonly SafeAstroutils safeAstroutils = new SafeAstroutils();
        public AutoResetEvent _arrivedAtAz;
        private Debugger debugger;

        private static readonly WiseSite wisesite = WiseSite.Instance;

        private bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static DomeSlaveDriver() { }
        public DomeSlaveDriver() { }

        private static readonly Lazy<DomeSlaveDriver> lazy = new Lazy<DomeSlaveDriver>(() => new DomeSlaveDriver()); // Singleton

        public static DomeSlaveDriver Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (_initialized)
                return;

            try
            {
                debugger = Debugger.Instance;
                //novas31 = new Astrometry.NOVAS.NOVAS31();
                //astroutils = new AstroUtils();
                _arrivedAtAz = new AutoResetEvent(false);
                wisedome.SetArrivedAtAzEvent(_arrivedAtAz);

                _initialized = true;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: init() done.");
                #endregion
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"DomeSlaveDriver: init() Caught:\n{ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
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
            Init();
            wisedome.Connect(value);
            _connected = value;
        }

        public void SlewToAz(double az, string reason)
        {
            Angle azAngle = new Angle(az, Angle.AngleType.Az);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"DomeSlaveDriver: Asking dome to SlewToAzimuth({azAngle.ToShortNiceString()}, reason: {reason})");
            #endregion
            try
            {
                wisedome.SlewToAzimuth(az, reason);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    $"DomeSlaveDriver:SlewToAz Waiting for dome to arrive to target {Angle.AzFromDegrees(az).ToShortNiceString()}");
                #endregion
                _arrivedAtAz.WaitOne();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"DomeSlaveDriver:SlewToAz Dome arrived to target {azAngle.ToShortNiceString()}");
                #endregion
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    $"DomeSlaveDriver:SlewToAz got \"{ex.Message}\"\nat {ex.StackTrace}\nwhile slewing to {azAngle.ToShortNiceString()}, Aborting slew!");
                #endregion
                wisedome.AbortSlew($"from DomeSlaveDriver:SlewToAz({azAngle.ToShortNiceString()}, {reason})");
                //throw ex;
            }
        }

        public void Park()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to Park");
            #endregion
            try
            {
                wisedome.Park();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    $"DomeSlaveDriver: Waiting for dome to park at {Angle.AzFromDegrees(wisedome.ParkAzimuth).ToShortNiceString()}");
                #endregion
                _arrivedAtAz.WaitOne();
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    $"DomeSlaveDriver:  while waiting for dome to park at {Angle.AzFromDegrees(wisedome.ParkAzimuth).ToShortNiceString()} Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
                wisedome.AbortSlew("from Park");
                //throw ex;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome is Parked");
            #endregion
        }

        public void FindHome()
        {
             #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Asking dome to findHome");
            #endregion
            try
            {
                wisedome.StartFindingHome();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Waiting for dome to findHome");
                #endregion
            }
            catch (Exception ex)
            {
                wisedome.AbortSlew($"(FindHome): Caught: {ex.Message}");
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "DomeSlaveDriver: Dome found Home");
            #endregion
        }

        public void SlewToAz(Angle primaryAngle, Angle dec, string reason)
        {
            if (primaryAngle.Type == Angle.AngleType.HA)
                primaryAngle = wisesite.LocalSiderealTime - primaryAngle;   // HA => RA

            Angle newDomeAz = CalculateDomeAzimuth(primaryAngle, dec);

            if (! wisedome.FarEnoughToMove(newDomeAz))  // Silently ignore slew request
                return;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"DomeSlaveDriver:SlewToAz (for {reason}): ra: {primaryAngle}, dec: {dec} => {newDomeAz.ToShortNiceString()}");
            #endregion
            SlewToAz(newDomeAz.Degrees, reason);
        }

        public bool Slewing
        {
            get
            {
                return Slewers.Active(Slewers.Type.Dome);
            }
        }

        public static void AbortSlew()
        {
            wisedome.AbortSlew("from DomeSlaveDriver.AbortSlew()");
            WiseTele.Instance.slewers.Delete(Slewers.Type.Dome);
        }

        public string Azimuth
        {
            get
            {
                if (!wisedome.Connected)
                    return "Not connected";
                if (!wisedome.Calibrated)
                    return "Not calibrated";

                return wisedome.Azimuth.ToShortNiceString();
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

        public void OpenShutter(bool bypassSafety = false)
        {
            wisedome.OpenShutter("ASCOM.Wise40.Telescope:DomeSlaveDriver", bypassSafety);
        }

        public static void CloseShutter(string reason)
        {
            wisedome.CloseShutter($"ASCOM.Wise40.Telescope:DomeSlaveDriver (reason: {reason})");
        }

        public void StopShutter(string reason)
        {
            wisedome.wisedomeshutter.Stop(reason);
        }

        public ASCOM.DeviceInterface.ShutterState ShutterState
        {
            get
            {
                return wisedome.ShutterState;
            }
        }

        public string ShutterStatusString
        {
            get
            {
                if (!Connected)
                    return "Not connected";

                return wisedome.ShutterStatusString;
            }
        }

        public bool AtPark
        {
            get
            {
                return wisedome.AtPark;
            }
        }

        public static void Unpark()
        {
            wisedome.Unpark();
        }

        /// <summary>
        /// Calculates the dome azimuth for a given telescope position.
        /// NOTE: Code adapted from Time_Coord.pas in the RemoteCommander Delphi project
        /// </summary>
        /// <param name="ra">Angle, target RA</param>
        /// <param name="dec">Angle, target DEC</param>
        /// <returns>Angle, dome azimuth</returns>
        public Angle CalculateDomeAzimuth(Angle ra, Angle dec)
        {
            const double X0 = 0.0;  // meters
            const double Y0 = 0.0;  // meters
            const double Z0 = 0.0;  // meters
            const double R = 5.0;   // meters - dome radius
            const double L = 1.2;   // meters - optical axis offset from ra axis
            const double SiteNorthLat = 0.534024354440983; //+30:35:50.43;

            double Lx, Ly, Lz, Px, Py, Pz, PL, QA, QB, QC, A1, Rx1, Ry1, DomeAz;
            double rar = 0, decr = 0, targetHA, targetAlt, targetAz = 0, zd = 0;

            wisesite.PrepareRefractionData();
            safeNovas31.Equ2Hor(safeAstroutils.JulianDateUT1(0), 0,
                WiseSite.astrometricAccuracy,
                0, 0,
                wisesite._onSurface,
                ra.Hours, dec.Degrees,
                WiseSite.refractionOption,
                ref zd, ref targetAz, ref rar, ref decr);

            targetHA = (wisesite.LocalSiderealTime - ra).Radians;
            targetAz = Angle.AzFromDegrees(targetAz).Radians;
            targetAlt = Angle.AltFromDegrees(90.0 - zd).Radians;

            Lx = X0 - (Math.Sin(SiteNorthLat) * Math.Sin(-targetHA) * L);
            Ly = Y0 + (Math.Cos(-targetHA) * L);
            Lz = Z0 + (Math.Cos(SiteNorthLat) * Math.Sin(-targetHA) * L);

            Px = Math.Cos(-targetAz) * Math.Cos(targetAlt);
            Py = Math.Sin(-targetAz) * Math.Cos(targetAlt);
            Pz = Math.Sin(targetAlt);

            PL = (Px * Lx) + (Py * Ly) + (Pz * Lz);

            QA = 1;
            QB = 2 * PL;
            QC = (L * L) - (R * R);
            A1 = (-QB + Math.Sqrt((QB * QB) - (4 * QA * QC))) / (2 * QA);

            Rx1 = (A1 * Px) - Lx;
            Ry1 = (A1 * Py) - Ly;

            DomeAz = -Math.Atan2(Ry1, Rx1);

            if (DomeAz < 0)
               DomeAz  += 2 * Math.PI;

            return Angle.AzFromRadians(DomeAz);
        }

        public bool ShutterIsMoving
        {
            get
            {
                return wisedome.wisedomeshutter.IsMoving;
            }
        }
    }
}
