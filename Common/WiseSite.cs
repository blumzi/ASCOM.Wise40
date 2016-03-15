using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common.Properties;
using ASCOM.Utilities;
using ASCOM.Astrometry;

namespace ASCOM.Wise40
{
    public class WiseSite: IDisposable
    {
        private static WiseSite site;
        private static bool isInitialized;
        private Astrometry.NOVAS.NOVAS31 Novas31;
        private AstroUtils astroUtils;
        private Astrometry.Accuracy accuracy;

        Astrometry.SiteInfo siteInfo;
        public Astrometry.OnSurface onSurface;

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

                siteInfo.Height = 875;
                siteInfo.Latitude = 30.59583333333333;
                siteInfo.Longitude = 34.763333333333335;
                Novas31 = new Astrometry.NOVAS.NOVAS31();
                astroUtils = new AstroUtils();
                onSurface.Height = siteInfo.Height;
                onSurface.Latitude = siteInfo.Latitude;
                onSurface.Longitude = siteInfo.Longitude;

                using (Profile driverProfile = new Profile())
                {
                    driverProfile.DeviceType = "Telescope";
                    string acc = Convert.ToString(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Astrometric accuracy", string.Empty, "Full"));
                    this.accuracy = (acc == "Full") ? Accuracy.Full : Accuracy.Reduced;
                }
            }
        }

        public void Dispose()
        {
            Novas31.Dispose();
            astroUtils.Dispose();
        }

        public double Longitude
        {
            get
            {
                return siteInfo.Longitude;
            }
        }

        public double Latitude
        {
            get
            {
                return siteInfo.Latitude;
            }
        }

        public double Elevation
        {
            get
            {
                return siteInfo.Height;
            }
        }

        public double LocalSiderealTime
        {
            get
            {
                var nov = new NOVAS31();
                var ast = new AstroUtils();
                var currJD = ast.JulianDateUT1(0);
                double lstNow = 0;
                var res = nov.SiderealTime(
                    currJD, 0d, 0, GstType.GreenwichApparentSiderealTime, Method.EquinoxBased, accuracy, ref lstNow);

                if (res != 0)
                    throw new InvalidValueException("Error getting Local Apparent Sidereal time");
                return lstNow + Longitude / 15;
            }
        }

        public static double ToSiderealTime(DateTime dt)
        {
            var utilities = new Utilities.Util();
            double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateLocalToJulian(dt) - 2451545.0))
                                  % 24.0;
            return siderealTime;
        }


    }
}
