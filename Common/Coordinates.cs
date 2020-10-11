using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Astrometry;

namespace ASCOM.Wise40.Common
{
    public class AltAz
    {
        public AltAz(double alt, double az)
        {
            this.Alt = alt;
            this.Az = az;
        }

        public double Alt { get; set; }

        public double Az { get; set; }
    }

    public class RaDec
    {
        public RaDec(double ra, double dec)
        {
            this.Ra = ra;
            this.Dec = dec;
        }

        public double Ra { get; set; }

        public double Dec { get; set; }
    }

    public class LatLon
    {
        public LatLon(double lat, double lon)
        {
            this.Lat = lat;
            this.Lon = lon;
        }

        public double Lat { get; set; }

        public double Lon { get; set; }
    }

    public static class Coordinates
    {
        static public RaDec AltAz2RaDec(AltAz altAzm, LatLon location, DateTime time, double elevation)
        {
            var utils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
            var MJDdate = utils.CalendarToMJD(time.Day, time.Month, time.Year);
            MJDdate += time.TimeOfDay.TotalDays;

            var tfm = new ASCOM.Astrometry.Transform.Transform
            {
                JulianDateTT = MJDdate,
                SiteElevation = elevation * 1000,
                SiteLatitude = location.Lat,
                SiteLongitude = location.Lon,
                SiteTemperature = 0
            };
            tfm.SetAzimuthElevation(altAzm.Az, altAzm.Alt);
            tfm.Refresh();
            var res = new RaDec(tfm.RAJ2000, tfm.DecJ2000);
            return res;
        }

        static public AltAz RaDec2AltAz(RaDec coord, LatLon location, DateTime time, double elevation)
        {
            var utils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
            var MJDdate = utils.CalendarToMJD(time.Day, time.Month, time.Year);
            MJDdate += time.TimeOfDay.TotalDays;

            var tfm = new ASCOM.Astrometry.Transform.Transform
            {
                JulianDateTT = MJDdate,
                SiteElevation = elevation * 1000,
                SiteLatitude = location.Lat,
                SiteLongitude = location.Lon,
                SiteTemperature = 0
            };
            tfm.SetJ2000(coord.Ra, coord.Dec);
            tfm.Refresh();

            var res = new AltAz(tfm.ElevationTopocentric, tfm.AzimuthTopocentric);
            return res;
        }
    }
}
