using System;

namespace ExtendedDouble
{
    public static class Azimuth
    {
        public static double Normalized(this double az)
        {            
            return az % 360;
        }

        public static double Inc(this double az, double delta)
        {
            return  Normalized(az + delta);
        }

        public static double Dec(this double az, double delta)
        {
            return Normalized(az - delta);
        }

        public static double minDelta(this double az1, double az2)
        {
            return Normalized(Math.Min(360.0 - Math.Abs(az1 - az2), Math.Abs(az1 - az2)));
        }

        /// <summary>
        /// Calculates the distance to az when moving clockwise (azimuth increases)
        /// </summary>
        /// <param name="az"></param>
        /// <returns>distance CW in degrees</returns>
        public static double DeltaCW(this double az1, double az2)
        {
            if (Normalized(az2) == Normalized(az1))
                return 0;

            return Normalized((az2 > az1) ? az2 - az1 : 360 - az1 + az2);
        }

        public static double DeltaCCW(this double az1, double az2)
        {
            if (Normalized(az2) == Normalized(az1))
                return 0;

            return Normalized((az2 > az1) ? 360 - az2 + az1 : az1 - az2); ;
        }
    }
}
