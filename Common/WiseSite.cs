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

namespace ASCOM.Wise40
{
    public class WiseSite: IDisposable
    {
        private static WiseSite site;
        private static bool isInitialized;
        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.OnSurface onSurface;
        public Astrometry.RefractionOption refractionOption = Astrometry.RefractionOption.NoRefraction;

        public double siteLatitude, siteLongitude, siteElevation;

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
            }
        }

        public void Dispose()
        {
            novas31.Dispose();
            astroutils.Dispose();
            ascomutils.Dispose();
        }

        public double Longitude
        {
            get
            {
                return onSurface.Longitude;
            }
        }

        public double Latitude
        {
            get
            {
                return onSurface.Latitude;
            }
        }

        public double Elevation
        {
            get
            {
                return onSurface.Height;
            }
        }

        public double LocalSiderealTime
        {
            get
            {
                double gstNow = 0;

                var res = novas31.SiderealTime(
                    astroutils.JulianDateUT1(0), 0d, astroutils.DeltaT(), GstType.GreenwichApparentSiderealTime, Method.EquinoxBased, astrometricAccuracy, ref gstNow);

                if (res != 0)
                    throw new InvalidValueException("Error getting Greenwich Apparent Sidereal time");

                return astroutils.Range(gstNow + (Longitude / 15.0), 0.0, true, 24.0, false);
            }
        }

        public static double ToSiderealTime(DateTime dt)
        {
            var utilities = new Utilities.Util();
            double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateLocalToJulian(dt) - 2451545.0))
                                  % 24.0;
            return siderealTime;
        }

        /// <summary> 
        // If we haven't checked in a long enough time (10 minutes ?!?)
        //  get temperature, humidity, pressure, air-mass, etc
        /// </summary>
        public void prepareRefractionData()
        {
            refractionOption = Astrometry.RefractionOption.NoRefraction;

            // NOTE: keep low frequency

            // if success in getting temp. and pres., change refractionOption to LocationRefraction
        }
    }
}
