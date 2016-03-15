using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class Const
    {
        /// <summary>
        /// UTC seconds in sidereal second
        /// </summary>
        public const double SiderealRate = 0.9972695664; //0.9972695677 (orig) 1⁄1,002737909350795

        /// <summary>
        /// UTC seconds in UTC day
        /// </summary>
        private const double SecPerDay = 86400;

        /// <summary>
        /// Sidereal rate (deg per sec)
        /// </summary>
        private const double SiderealRateDegPerSec = 360 / (SecPerDay * SiderealRate);

        /// <summary>
        /// Maximum Drive Rate (deg per sec)
        /// </summary>
        public const double MaxAxisRate = TRACKRATE_SLEW;

        /// <summary>
        /// Slewing rate 2 deg/sec.
        /// </summary>
        public const double TRACKRATE_SLEW = 2.0;

        /// <summary>
        /// Set rate 1 min/sec.
        /// </summary>
        public const double TRACKRATE_SET = 1 / 60;

        /// <summary>
        /// GuideRate 1 sec/sec.
        /// </summary>
        public const double TRACKRATE_GUIDE = (1 / 60) / 60;

        private const double STELLAR_DAY = 86164.0905308329; //86164.098903691; //86164.09054 (wolfram alpha)
        /// <summary>
        /// Sidereal tracking rate (deg/sec)
        /// </summary>
        public const double TRACKRATE_SIDEREAL = ((360.0) / STELLAR_DAY);

        private const double SOLAR_DAY = 86400;
        /// <summary>
        /// Solar tracking rate (deg/sec)
        /// </summary>
        public const double TRACKRATE_SOLAR = ((360.0) / SOLAR_DAY);
        private const double LUNAR_DAY = 89416.2793513594;
        /// <summary>
        /// Lunar tracking rate (deg/sec)
        /// </summary>
        public const double TRACKRATE_LUNAR = ((360.0) / LUNAR_DAY);//14.511415;
                                                                    //      1 s   | 1.11836×10^-5 lunar days
                                                                    //      1 min | 6.71018×10^-4 lunar days
                                                                    //      1 h   | 0.0402611 lunar days
                                                                    //      1 day | 0.966267 lunar days
    }
}
