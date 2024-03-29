﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using ASCOM.DriverAccess;

using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASCOM.Wise40
{
    public class WiseSite : IDisposable
    {
        private static bool _initialized = false;
        public static SafeNovas31 novas31 = new SafeNovas31();
        public static SafeAstroutils astroutils = new SafeAstroutils();
        public static SafeAscomutil ascomutil = new SafeAscomutil();
        public readonly ASCOM.Utilities.Util ascomutils = new Util();
        public Astrometry.OnSurface _onSurface;
        public Observer _observer;
        public static Astrometry.Accuracy astrometricAccuracy;
        public static Astrometry.RefractionOption refractionOption = RefractionOption.LocationRefraction;
        public static ObservingConditions och;
        private static DateTime lastOCFetch;
        private static bool _och_initialized = false;
        private static readonly Debugger debugger = Debugger.Instance;
        private static readonly ASCOM.Astrometry.Transform.Transform _transform = new Astrometry.Transform.Transform();
        private static readonly TempFetcher _tempFetcher = new TempFetcher(10);
        private static string _processName;
        public static string _machine = Environment.MachineName.ToLower();
        private bool _disposed = false;

        public enum OpMode { LCO, ACP, WISE, NONE };

        //
        // From the VantagePro summary graphs for 2015
        //
        public static readonly double[] averageTemperatures = { 9.7, 10.7, 14.0, 15.0, 21.1, 21.3, 24.4, 25.9, 24.7, 20.8, 16.1, 10.1 };
        public static readonly double[] averagePressures = { 1021, 1012, 1017, 1013, 1008, 1008, 1006, 1007, 1008, 1013, 1015, 1022 };

        public WiseSite() { }
        static WiseSite() { }

        private static readonly Lazy<WiseSite> lazy = new Lazy<WiseSite>(() => new WiseSite()); // Singleton

        public static WiseSite Instance
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

            Latitude = Instance.ascomutils.DMSToDegrees("30:35:50.43");
            Longitude = Instance.ascomutils.DMSToDegrees("34:45:43.86");
            Elevation = 882.9;

            novas31.MakeOnSurface(Latitude, Longitude, Elevation, 0.0, 0.0, ref _onSurface);
            refractionOption = RefractionOption.LocationRefraction;

            _transform.SiteElevation = Elevation;
            _transform.SiteLatitude = Latitude;
            _transform.SiteLongitude = Longitude;
            _transform.Refraction = true;

            _processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            _initialized = true;
        }

        public static double Latitude { get; set; }
        public static double Longitude { get; set; }
        public static double Elevation { get; set; }

public static bool TransformApparentToJ2000(double apparentRA, double apparentDEC, ref double j2000RA, ref double j2000DEC)
        {
            _transform.SiteTemperature = _tempFetcher.Temperature;
            _transform.SetApparent(apparentRA, apparentDEC);

            try
            {
                j2000RA = _transform.RAJ2000;
                j2000DEC = _transform.DecJ2000;
                return true;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"TransformApparentToJ2000: Caught {ex.Message} at {ex.StackTrace}");
                #endregion
                return false;
            }
        }

        public static bool TransformJ2000ToApparent(double j2000RA, double j2000DEC , ref double apparentRA, ref double apparentDEC)
        {
            _transform.SiteTemperature = _tempFetcher.Temperature;
            _transform.SetJ2000(j2000RA, j2000DEC);

            try
            {
                apparentRA = _transform.RAApparent;
                apparentDEC = _transform.DECApparent;
                return true;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"TransformJ2000ToApparent: Caught {ex.Message} at {ex.StackTrace}");
                #endregion
                return false;
            }
        }

        public static OperationalProfile OperationalProfile { get; } = new OperationalProfile();

        public static void InitOCH()
        {
            if (_och_initialized)
                return;

            try
            {
                och = new ObservingConditions("ASCOM.OCH.ObservingConditions")
                {
                    Connected = true
                };
                lastOCFetch = DateTime.Now;
                _och_initialized = true;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Could not connect to OCH: {0}", ex.Message);
                #endregion
                och = null;
            }

        }

        public void Dispose()
        {
            // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    novas31.Dispose();
                    novas31 = null;
                }
                catch { }

                try {
                    ascomutils.Dispose();
                }
                catch { }

                try
                {
                    astroutils.Dispose();
                    astroutils = null;
                }
                catch { }

                _disposed = true;
            }
        }

        public Angle LocalSiderealTime
        {
            get
            {
                double gstNow = 0;
                short res = -1;
                const int maxTries = 5;

                for (int tries = 0; tries < maxTries; tries++)
                {
                    try
                    {
                        res = novas31.SiderealTime(
                            astroutils.JulianDateUT1(0), 0d,
                            astroutils.DeltaT(),
                            GstType.GreenwichApparentSiderealTime,
                            Method.EquinoxBased,
                            astrometricAccuracy,
                            ref gstNow);
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            $"LocalSiderealTime: try# {tries} of {maxTries} caught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                    }

                    if (res == 0)
                        return Angle.FromHours(gstNow) + Angle.FromHours(Longitude / 15);
                    else
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "LocalSiderealTime", $"try# {tries} of {maxTries}: Error getting novas31.SiderealTime, res: {res}");
                        #endregion
                    }
                }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "LocalSiderealTime: Giving up, returning 0.0");
                #endregion
                return Angle.FromHours(0.0, Angle.AngleType.RA);
            }
        }

        /// <summary>
        /// // If we haven't checked in a long enough time (10 minutes ?!?)
        //  get temperature and pressure.
        /// </summary>
        public void PrepareRefractionData()
        {
            const int freqOCFetchMinutes = 10;

            InitOCH();

            if (!OperationalProfile.CalculatesRefractionForHorizCoords)
            {
                refractionOption = RefractionOption.NoRefraction;
                return;
            }

            DateTime now = DateTime.Now;
            int month = now.Month - 1;

            _onSurface.Temperature = averageTemperatures[month];
            _onSurface.Pressure = averagePressures[month];

            if (och != null && now.Subtract(lastOCFetch).TotalMinutes > freqOCFetchMinutes)
            {
                try
                {
                    double timeSinceLastUpdate = och.TimeSinceLastUpdate("Temperature");

                    if (timeSinceLastUpdate > (freqOCFetchMinutes * 60))
                    {
                        _onSurface.Temperature = och.Temperature;
                        _onSurface.Pressure = och.Pressure;
                        refractionOption = RefractionOption.LocationRefraction;
                        lastOCFetch = now;
                    }
                }
                catch { }
            }
        }

        public static bool FilterWheelInUse
        {
            get
            {
                return OperationalMode != OpMode.LCO;
            }
        }

        public static OpMode OperationalMode
        {
            get
            {
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    if (Enum.TryParse<OpMode>(driverProfile.GetValue(Const.WiseDriverID.Telescope, "SiteOperationMode", null, "WISE").ToUpper(), out OpMode mode))
                        OperationalProfile.OpMode = mode;
                }
                //debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"WiseSite.OperationalMode: {OperationalProfile.OpMode}");
                return OperationalProfile.OpMode;
            }

            set
            {
                OperationalProfile.OpMode = value;
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    driverProfile.WriteValue(Const.WiseDriverID.Telescope, "SiteOperationMode", OperationalProfile.OpMode.ToString());
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OperationalMode:set {0}", OperationalProfile.OpMode.ToString());
                #endregion
            }
        }

        public bool OperationalModeRequiresRESTServer
        {
            get
            {
                return OperationalProfile.OpMode == OpMode.LCO || OperationalProfile.OpMode == OpMode.ACP;
            }
        }

        ////4-----------------------------------------------------------------------------
        //function CalcAM(alt:extended): extended;
        ////------------------------------------------------------------------------------
        ////Returns Air mass. Input: Zenith Distance [rad]
        ////Uses hardie formula. Valid to zenith-distance of about 87deg.
        ////By : Eran O. Ofek           January 1994
        //var
        //    secz1   : extended;
        //begin
        //  if (pi/2-alt)>1.466 then   //Calc up to ZD=1.466Radians=84 degrees
        //    Result:=9.9
        //  else begin
        //     secz1:= Sec(pi/2-alt)-1;
        //     Result := secz1+1 - 0.0018167*secz1 - 0.002875*sqr(secz1) - 0.0008083*power(secz1,3);
        //   end;
        //end;

        /// <summary>
        /// Air Mass at given altitude (radians)
        /// </summary>
        /// <param name="alt">Altitude in radians</param>
        /// <returns></returns>
        public static double AirMass(double alt)
        {
            const double halfPI = Math.PI / 2;

            if ((halfPI - alt) > 1.466)
                return 9.9;

            double secz1 = (1 / Math.Cos(halfPI - alt)) - 1;   // Secant(x) = 1 / Cos(x)

            return secz1 + 1 - (0.0018167 * secz1) - (0.002875 * Math.Pow(secz1, 2)) - (0.0008083 * Math.Pow(secz1, 3));
        }

        public static bool CurrentProcessIs(Const.Application app)
        {
            if (_processName == null)
                _processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return _processName == Const.Apps[app].appName;
        }

        public static string ObservatoryName
        {
            get
            {
                Dictionary<string, string> Host2Obs = new Dictionary<string, string>
                {
                    { "wo-neo", "c18" },
                    { "c28-pc", "c28" },
                    { "dome-pc", "wise40" },
                    { "h80-pc", "h80" },
                };

                string machine = Environment.MachineName.ToLower();

                if (! Host2Obs.Keys.Contains(machine))
                {
                    MessageBox.Show($"Unknown observatory name for machine: \"{machine}\"!\n" +
                        "\n" +
                        $"The application will now exit.", Application.ProductName);
                    Application.Exit();
                }

                return Host2Obs[machine];
            }
        }
    }

    public class TempFetcher
    {
        private TimeSpan _ts;
        private double _temp;
        private DateTime _lastFetch;

        public TempFetcher(int seconds)
        {
            _ts = new TimeSpan(0, 0, seconds);
            _lastFetch = DateTime.Now.Subtract(new TimeSpan(0, 0, (int) seconds + 1));
        }

        public double Temperature
        {
            get
            {
                if (WiseSite.och == null)
                    return WiseSite.averageTemperatures[DateTime.Now.Month - 1];

                DateTime now = DateTime.Now;
                if (now.Subtract(_lastFetch) > _ts)
                {
                    _temp = WiseSite.och.Temperature;
                    _lastFetch = now;
                }
                return _temp;
            }
        }
    }
}
