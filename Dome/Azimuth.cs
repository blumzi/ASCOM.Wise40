using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASCOM.Astrometry.AstroUtils;

namespace ASCOM.Wise40
{
    public class Azimuth : AstroUtils
    {
        private double _value;

        public Azimuth(double az)
        {
            _value = Normalized(az);
        }

        public double Normalized(double az)
        {
            return Range(az, 0.0, true, 360.0, false);
        }

        public double Normalized()
        {
            return _value = Range(_value, 0.0, true, 360.0, false);
        }

        public double Inc(double delta)
        {
            return _value = Normalized(_value + delta);
        }

        public double Dec(double az, double delta)
        {
            return _value = Normalized(_value - delta);
        }

        public double minDelta(double az)
        {
            return Normalized(Math.Min(360.0 - Math.Abs(_value - az), Math.Abs(_value - az)));
        }

        /// <summary>
        /// Calculates the distance to az when moving clockwise (azimuth increases)
        /// </summary>
        /// <param name="az"></param>
        /// <returns>distance CW in degrees</returns>
        public double DeltaCW(double az)
        {
            az = Normalized(az);

            if (az == Normalized(_value))
                return 0;

            return Normalized((az > _value) ? az - _value : 360 - _value + az);
        }

        public double DeltaCCW(double az)
        {
            az = Normalized(az);

            if (az == Normalized(_value))
                return 0;

            return Normalized((az > _value) ? 360 - az + _value : _value - az); ;
        }
    }
}
