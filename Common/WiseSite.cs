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
using ASCOM.DriverAccess;

namespace ASCOM.Wise40
{
    public class WiseSite : IDisposable
    {
        private static WiseSite site;
        private static bool isInitialized;
        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.OnSurface onSurface;
        public Astrometry.RefractionOption refractionOption;
        public double siteLatitude, siteLongitude, siteElevation;
        private ObservingConditions observingConditions;
        private DateTime lastOCFetch;

        public static WiseSite Instance
        {
            get
            {
                if (site == null)
                    site = new WiseSite();
                site.init();
                return site;
            }
        }

        public void init()
        {
            if (!isInitialized)
            {
                isInitialized = true;

                novas31 = new NOVAS31();
                astroutils = new AstroUtils();
                ascomutils = new Util();

                siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
                siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
                siteElevation = 882.9;
                novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);

                using (Profile driverProfile = new Profile())
                {
                    driverProfile.DeviceType = "Telescope";
                    string acc = Convert.ToString(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Astrometric accuracy", string.Empty, "Full"));
                    astrometricAccuracy = (acc == "Full") ? Accuracy.Full : Accuracy.Reduced;
                }


                try
                {
                    observingConditions = new ObservingConditions("ASCOM.Vantage.ObservingConditions");
                    refractionOption = Astrometry.RefractionOption.LocationRefraction;
                    lastOCFetch = DateTime.Now;
                }
                catch
                {
                    refractionOption = Astrometry.RefractionOption.NoRefraction;
                }
            }
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
        public void prepareRefractionData()
        {
            const int freqOCFetchMinutes = 10;

            refractionOption = Astrometry.RefractionOption.NoRefraction;
            if (observingConditions != null && DateTime.Now.Subtract(lastOCFetch).TotalMinutes < freqOCFetchMinutes)
            {
                try
                {
                    double timeSinceLastUpdate = observingConditions.TimeSinceLastUpdate("");

                    if (timeSinceLastUpdate < (freqOCFetchMinutes * 60))
                    {
                        onSurface.Temperature = observingConditions.Temperature;
                        onSurface.Pressure = observingConditions.Pressure;
                        refractionOption = Astrometry.RefractionOption.LocationRefraction;
                    }
                }
                catch
                {
                }
            }
        }
    }
}
