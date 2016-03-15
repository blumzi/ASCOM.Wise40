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
        private double alt, az;

        public AltAz(double alt, double az)
        {
            this.alt = alt;
            this.az = az;
        }

        public double Alt
        {
            get
            {
                return this.alt;
            }
            set
            {
                this.alt = value;
            }
        }

        public double Az
        {
            get
            {
                return this.az;
            }
            set
            {
                this.az = value;
            }
        }
    }

    public class RaDec
    {
        private double ra, dec;

        public RaDec(double ra, double dec)
        {
            this.ra = ra;
            this.dec = dec;
        }

        public double Ra
        {
            get
            {
                return this.ra;
            }
            set
            {
                this.ra = value;
            }
        }

        public double Dec
        {
            get
            {
                return this.dec;
            }
            set
            {
                this.dec = value;
            }
        }
    }

    public class LatLon
    {
        private double lat, lon;

        public LatLon(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public double Lat
        {
            get
            {
                return this.lat;
            }
            set
            {
                this.lat = value;
            }
        }

        public double Lon
        {
            get
            {
                return this.lon;
            }
            set
            {
                this.lon = value;
            }
        }
    }

    public class Coordinates
    {
        static public RaDec AltAz2RaDec(AltAz altAzm, LatLon location, DateTime time, double elevation)
        {
            var utils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
            var MJDdate = utils.CalendarToMJD(time.Day, time.Month, time.Year);
            MJDdate += time.TimeOfDay.TotalDays;

            var tfm = new ASCOM.Astrometry.Transform.Transform();
            tfm.JulianDateTT = MJDdate;
            tfm.SiteElevation = elevation * 1000;
            tfm.SiteLatitude = location.Lat;
            tfm.SiteLongitude = location.Lon;
            tfm.SiteTemperature = 0;
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

            var tfm = new ASCOM.Astrometry.Transform.Transform();
            tfm.JulianDateTT = MJDdate;
            tfm.SiteElevation = elevation * 1000;
            tfm.SiteLatitude = location.Lat;
            tfm.SiteLongitude = location.Lon;
            tfm.SiteTemperature = 0;
            tfm.SetJ2000(coord.Ra, coord.Dec);
            tfm.Refresh();

            var res = new AltAz(tfm.ElevationTopocentric, tfm.AzimuthTopocentric);
            return res;
        }
    }
}
