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
        public static Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption;
        public double siteLatitude, siteLongitude, siteElevation;
        public static ObservingConditions och;
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
                        {
                            _instance = new WiseSite();
                            _instance.init();
                        }
                        }
                }
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

            _initialized = true;
        }

        public void Dispose()
        {
            novas31.Dispose();
            astroutils.Dispose();
            ascomutils.Dispose();
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
                return (_opMode == OpMode.LCO || _opMode == OpMode.ACP);
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
}
