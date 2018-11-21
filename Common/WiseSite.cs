using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common.Properties;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using ASCOM.DriverAccess;

using System.IO;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class WiseSite : IDisposable
    {
        private static bool _initialized = false;
        private static Astrometry.NOVAS.NOVAS31 novas31 = new NOVAS31();
        private static AstroUtils astroutils = new AstroUtils();
        private static ASCOM.Utilities.Util ascomutils = new Util();
        public Astrometry.OnSurface onSurface;
        public Observer observer;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption;
        public double siteLatitude, siteLongitude, siteElevation;
        public static ObservingConditions och;
        public SafetyMonitor computerControl, safeToOperate;
        private DateTime lastOCFetch;
        private Debugger debugger = Debugger.Instance;

        public enum OpMode { LCO, ACP, WISE, NONE };
        public OpMode _opMode = OpMode.WISE;

        //
        // From the VantagePro summary graphs for 2015
        //
        private static readonly double[] averageTemperatures = { 9.7, 10.7, 14.0, 15.0, 21.1, 21.3, 24.4, 25.9, 24.7, 20.8, 16.1, 10.1 };
        private static readonly double[] averagePressures = { 1021, 1012, 1017, 1013, 1008, 1008, 1006, 1007, 1008, 1013, 1015, 1022 };

        public WiseSite() { }
        static WiseSite() { }
        private static volatile WiseSite _instance; // Singleton
        private static object syncObject = new object();

        public static WiseSite Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseSite();
                    }
                }
                _instance.init();
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
            siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
            siteElevation = 882.9;
            novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);

            try
            {
                och = new ObservingConditions("ASCOM.OCH.ObservingConditions");
                och.Connected = true;
                refractionOption = Astrometry.RefractionOption.LocationRefraction;
                lastOCFetch = DateTime.Now;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Could not connect to OCH: {0}", ex.Message);
                #endregion
                och = null;
                refractionOption = Astrometry.RefractionOption.NoRefraction;
            }

            try
            {
                computerControl = new SafetyMonitor(Const.wiseComputerControlDriverID);
                computerControl.Connected = true;
            }
            catch
            {
                computerControl = null;
            }

            try
            {
                safeToOperate = new SafetyMonitor(Const.wiseSafeToOperateDriverID);
                safeToOperate.Connected = true;
            }
            catch
            {
                safeToOperate = null;
            }

            _initialized = true;
        }

        public void Dispose()
        {
            novas31.Dispose();
            astroutils.Dispose();
            ascomutils.Dispose();

            if (computerControl != null)
            {
                computerControl.Connected = false;
                computerControl.Dispose();
            }

            if (safeToOperate != null)
            {
                safeToOperate.Connected = false;
                safeToOperate.Dispose();
            }
        }

        public Angle Longitude
        {
            get
            {
                return Angle.FromHours(onSurface.Longitude / 15.0);
            }
        }

        public Angle Latitude
        {
            get
            {
                return Angle.FromDegrees(onSurface.Latitude, Angle.Type.Dec);
            }
        }

        public double Elevation
        {
            get
            {
                return onSurface.Height;
            }
        }

        public Angle LocalSiderealTime
        {
            get
            {
                double gstNow = 0;

                var res = novas31.SiderealTime(
                    astroutils.JulianDateUT1(0), 0d,
                    astroutils.DeltaT(),
                    GstType.GreenwichApparentSiderealTime,
                    Method.EquinoxBased,
                    astrometricAccuracy,
                    ref gstNow);

                if (res != 0)
                    throw new InvalidValueException("Error getting Greenwich Apparent Sidereal time");

                return Angle.FromHours(gstNow) + Longitude;
            }
        }

        /// <summary> 
        // If we haven't checked in a long enough time (10 minutes ?!?)
        //  get temperature and pressure.
        /// </summary>
        public void prepareRefractionData(bool calculateRefraction)
        {
            const int freqOCFetchMinutes = 10;

            if (!calculateRefraction)
            {
                refractionOption = RefractionOption.NoRefraction;
                return;
            }
            DateTime now = DateTime.Now;
            int month = now.Month - 1;

            onSurface.Temperature = averageTemperatures[month];
            onSurface.Pressure = averagePressures[month];

            if (och != null && DateTime.Now.Subtract(lastOCFetch).TotalMinutes > freqOCFetchMinutes)
            {
                try
                {
                    double timeSinceLastUpdate = och.TimeSinceLastUpdate("Temperature");

                    if (timeSinceLastUpdate > (freqOCFetchMinutes * 60))
                    {
                        onSurface.Temperature = och.Temperature;
                        onSurface.Pressure = och.Pressure;
                        refractionOption = RefractionOption.LocationRefraction;
                    }
                }
                catch { }
            }
        }
        
        public OpMode OperationalMode
        {
            get
            {
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    OpMode mode;

                    if (Enum.TryParse<OpMode>(driverProfile.GetValue(Const.wiseTelescopeDriverID, "SiteOperationMode", null, "WISE").ToUpper(), out mode))
                        _opMode = mode;
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OperationalMode:get => {0}", _opMode.ToString());
                #endregion
                return _opMode;
            }

            set
            {
                _opMode = value;
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    driverProfile.WriteValue(Const.wiseTelescopeDriverID, "SiteOperationMode", _opMode.ToString());
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OperationalMode:set {0}", _opMode.ToString());
                #endregion
            }
        }

        public bool OperationalModeRequiresRESTServer
        {
            get
            {
                // Not sure about ACP yet!
                return (_opMode == OpMode.LCO || _opMode == OpMode.WISE);
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
        public double AirMass(double alt)
        {
            const double halfPI = Math.PI / 2;

            if ((halfPI - alt) > 1.466)
                return 9.9;

            double secz1 = (1 / Math.Cos(halfPI - alt)) - 1;   // Secant(x) = 1 / Cos(x)

            return secz1 + 1 - 0.0018167 * secz1 - 0.002875 * Math.Pow(secz1, 2) - 0.0008083 * Math.Pow(secz1, 3);
        }

    }

    public class Moon
    {
        private static AstroUtils astroutils = new AstroUtils();
        private static NOVAS31 novas31 = new NOVAS31();
        private static Object3 moon;
        private static CatEntry3 dummy_star;
        private static Observer observer;
        private static Debugger debugger = Debugger.Instance;

        // start Singleton
        private static readonly Lazy<Moon> lazy =
            new Lazy<Moon>(() => new Moon()); // Singleton

        public static Moon Instance
        {
            get
            {
                lazy.Value.init();
                return lazy.Value;
            }
        }

        private Moon() { }
        // end Singleton

        private void init()
        {
            double[] ScPos = { 0, 0, 0 };
            double[] ScVel = { 0, 0, 0 };
            InSpace ObsSpace = new InSpace();

            novas31.MakeInSpace(ScPos, ScVel, ref ObsSpace);
            novas31.MakeObserver(ObserverLocation.EarthSurface, WiseSite.Instance.onSurface, ObsSpace, ref observer);
            novas31.MakeCatEntry("DUMMY", "xxx", 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, ref dummy_star);
            novas31.MakeObject(ObjectType.MajorPlanetSunOrMoon, 11, "Moon", dummy_star, ref moon);
        }

        public double Illumination
        {
            get
            {
                return astroutils.MoonIllumination(astroutils.JulianDateUT1(0));
            }
        }

        public double Phase
        {
            get
            {
                return astroutils.MoonPhase(astroutils.JulianDateUT1(0));
            }
        }

        //public short TopoPlanet(
        //    double JdTt,
        //    Object3 SsBody,
        //    double DeltaT,
        //    OnSurface Position,
        //    Accuracy Accuracy,
        //    ref double Ra,
        //    ref double Dec,
        //    ref double Dis
        //)


        ////8-----------------------------------------------------------------------------
        //function SphereDist(Long1:extended; Lat1:extended; Long2:extended; Lat2:extended): extended;
        ////------------------------------------------------------------------------------
        //// calculate the spherical distance between two coordinates
        //// Input : Long1 in radians [extended]
        ////         Lat1  in radians [extended]
        ////         Long2 in radians [extended]
        ////         Lat2  in radians [extended]
        //// Output: distance in radians [extended]
        //// EO - May 2002
        //        begin
        //   Result := arccos(sin(Lat1)*sin(Lat2) + cos(Lat1)*cos(Lat2)*cos(Long1 - Long2));
        //end;

        public static double SphereDist(double long1, double lat1, double long2, double lat2)
        {
            return Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(long1 - long2));
        }

        public Angle Distance(double telescopeRA, double telescopeDec)
        {
            SkyPos moonPos = new SkyPos();
            short ret;

            ret = novas31.Place(astroutils.JulianDateUT1(0),
                moon,
                observer,
                0.0,
                CoordSys.Astrometric,
                Accuracy.Full,
                ref moonPos);

            if (ret != 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Moon.Distance: Cannot calculate Moon position (ret: {0})", ret);
                #endregion
                return Angle.FromDegrees(0);
            }

            double rad = SphereDist(telescopeRA, telescopeDec, moonPos.RA, moonPos.Dec);
            return Angle.FromRadians(rad, Angle.Type.Deg);
        }
    }

    public static class HumanIntervention
    {
        static DateTime _lastInfoRead = DateTime.MinValue;
        static string _info = null;

        static HumanIntervention() {}

        public static void Create(string oper, string reason)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Const.humanInterventionFilePath));
            using (StreamWriter sw = new StreamWriter(Const.humanInterventionFilePath))
            {
                sw.WriteLine("Operator: \"" + oper + "\"");
                sw.WriteLine("Reason: \"" + reason + "\"");
                sw.WriteLine("Created: " + DateTime.Now.ToString("MMM dd yyyy, hh:mm:ss tt"));
            }

            while (!File.Exists(Const.humanInterventionFilePath))
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        public static void Remove()
        {
            if (!File.Exists(Const.humanInterventionFilePath))
                return;

            bool deleted = false;
            while (!deleted)
            {
                try
                {
                    File.Delete(Const.humanInterventionFilePath);
                    deleted = true;
                }
                catch (System.IO.IOException ex)    // in use
                {
                    ;
                }
            }
        }

        public static bool IsSet()
        {
            return System.IO.File.Exists(Const.humanInterventionFilePath);
        }

        public static string Info
        {
            get
            {
                string info = string.Empty;

                if (!IsSet())
                    return string.Empty;

                if (File.GetLastWriteTime(Const.humanInterventionFilePath) > _lastInfoRead)
                {

                    StreamReader sr = new StreamReader(Const.humanInterventionFilePath);
                    string line = string.Empty;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("Operator:") || line.StartsWith("Created:") || line.StartsWith("Reason:"))
                            info += line + "; ";
                    }

                    info = "Human Intervention; " + ((info == string.Empty) ? string.Format("File \"{0}\" exists.",
                        Const.humanInterventionFilePath) : info);
                    _info = info.TrimEnd(';', ' '); ;
                    _lastInfoRead = DateTime.Now;
                }
                return _info;
            }
        }
    }
}
