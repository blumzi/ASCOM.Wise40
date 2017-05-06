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

namespace ASCOM.Wise40
{
    public class WiseSite : IDisposable
    {
        private static WiseSite _wisesite = new WiseSite();
        private static bool _initialized;
        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.OnSurface onSurface;
        public Astrometry.RefractionOption refractionOption;
        public double siteLatitude, siteLongitude, siteElevation;
        public ObservingConditions och;
        //public ObservingConditions vantagePro;
        public SafetyMonitor computerControl, safeToOpen, safeToImage;
        private DateTime lastOCFetch;
        private Debugger debugger = Debugger.Instance;

        //
        // From the VantagePro summary graphs for 2015
        //
        private static readonly double[] averageTemperatures = { 9.7, 10.7, 14.0, 15.0, 21.1, 21.3, 24.4, 25.9, 24.7, 20.8, 16.1, 10.1 };
        private static readonly double[] averagePressures = { 1021, 1012, 1017, 1013, 1008, 1008, 1006, 1007, 1008, 1013, 1015, 1022 };

        public static WiseSite Instance
        {
            get
            {
                return _wisesite;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            novas31 = new NOVAS31();
            astroutils = new AstroUtils();
            ascomutils = new Util();

            siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
            siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
            siteElevation = 882.9;
            novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);

            try
            {
                och = new ObservingConditions("ASCOM.OCH.ObservingConditions");
                och.Connected = true;
                //vantagePro = new ObservingConditions("ASCOM.Wise40.VantagePro.ObservingConditions");
                //vantagePro.Connected = true;
                refractionOption = Astrometry.RefractionOption.LocationRefraction;
                lastOCFetch = DateTime.Now;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Could not connect to OCH: {0}", ex.Message);
                #endregion
                och = null;
                //vantagePro = null;
                refractionOption = Astrometry.RefractionOption.NoRefraction;
            }

            try
            {
                computerControl = new SafetyMonitor("ASCOM.Wise40.ComputerControl.SafetyMonitor");
                computerControl.Connected = true;
            }
            catch
            {
                computerControl = null;
            }

            try
            {
                safeToOpen = new SafetyMonitor("ASCOM.Wise40.SafeToOpen.SafetyMonitor");
                safeToOpen.Connected = true;
            }
            catch
            {
                safeToOpen = null;
            }

            try
            {
                safeToImage = new SafetyMonitor("ASCOM.Wise40.SafeToImage.SafetyMonitor");
                safeToImage.Connected = true;
            }
            catch
            {
                safeToImage = null;
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

            if (safeToOpen != null)
            {
                safeToOpen.Connected = false;
                safeToOpen.Dispose();
            }

            if (safeToImage != null)
            {
                safeToImage.Connected = false;
                safeToImage.Dispose();
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
//            if (vantagePro != null && DateTime.Now.Subtract(lastOCFetch).TotalMinutes > freqOCFetchMinutes)
            {
                try
                {
                    double timeSinceLastUpdate = och.TimeSinceLastUpdate("Temperature");
//                    double timeSinceLastUpdate = vantagePro.TimeSinceLastUpdate("Temperature");

                    if (timeSinceLastUpdate > (freqOCFetchMinutes * 60))
                    {
                        onSurface.Temperature = och.Temperature;
                        onSurface.Pressure = och.Pressure;
                        //onSurface.Temperature = vantagePro.Temperature;
                        //onSurface.Pressure = vantagePro.Pressure;
                        refractionOption = RefractionOption.LocationRefraction;
                    }
                }
                catch { }
            }
        }
    }
}
