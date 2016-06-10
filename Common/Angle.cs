using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{

    public class ShortestDistanceResult
    {
        public Angle angle;
        public Const.AxisDirection direction;

        public ShortestDistanceResult(Angle a, Const.AxisDirection d)
        {
            angle = a;
            direction = d;
        }

        public ShortestDistanceResult()
        {
            angle = Angle.invalid;
            direction = Const.AxisDirection.None;
        }
    };

    public class Angle
    {
        internal static Astrometry.AstroUtils.AstroUtils astroutils = new Astrometry.AstroUtils.AstroUtils();
        internal static ASCOM.Utilities.Util ascomutils = new ASCOM.Utilities.Util();
        public enum Format { Deg, RA, Dec, HA, Alt, Az, Double, Rad, HAhms, RAhms, HMS };

        private double _degrees;

        private int Sign
        {
            get
            {
                return _degrees < 0 ? -1 : 1;
            }
        }

        public static Angle FromRadians(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI);
        }

        public static Angle FromDegrees(double deg)
        {
            return new Angle(deg);
        }

        public static Angle FromHours(double hours)
        {
            return new Angle(hours * 15.0);
        }

        public double Degrees
        {
            get
            {
                return astroutils.Range(_degrees, 0.0, true, 360.0, false);
            }

            set
            {
                _degrees = astroutils.Range(value, 0.0, true, 360.0, false);
            }
        }

        public double Hours
        {
            get
            {
                return astroutils.ConditionRA(_degrees / 15.0);
            }

            set
            {
                _degrees = astroutils.ConditionRA(value * 15.0);
            }
        }

        public double Declination
        {
            get
            {
                return _degrees;
            }

            set
            {
                if (value < -90.0 || value > 90.0)
                    throw new InvalidValueException(string.Format("Invalid value {0}, must be between -90.0 and 90.0", value));
                _degrees = value;
            }
        }

        public double Raw
        {
            get
            {
                return _degrees;
            }

            set
            {
                _degrees = value;
            }
        }

        public double Radians
        {
            get
            {
                return Degrees * Math.PI / 180.0;
            }

            set
            {
                Degrees = value * 180.0 / Math.PI;
            }
        }

        public double ToRA
        {
            get
            {
                return astroutils.ConditionRA(_degrees);
            }
        }

        public double ToHA
        {
            get
            {
                return astroutils.ConditionHA(_degrees);
            }
        }

        public Angle(double deg)
        {
            _degrees = deg;
        }

        public Angle(int d, int m, double s, int sign = 1)
        {
            Degrees = sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", d, m, s));
        }

        public Angle(string s)
        {
            Degrees = ascomutils.DMSToDegrees(s);
        }

        public static bool TryParse(string coordinates, out Angle value)
        {
            value = new Angle(-1000);
            double val;

            var c = coordinates.Split(new[] { ' ', '°', '\'', ':' });

            try
            {
                switch (c.Length)
                {
                    case 1:
                        value = new Angle(int.Parse(c[0]), 0, 0);
                        return true;
                    case 2:
                        value = new Angle(int.Parse(c[0]), int.Parse(c[1]), 0);
                        return true;
                    case 3:
                        value = new Angle(int.Parse(c[0]), int.Parse(c[1]), double.Parse(c[2]));
                        Console.WriteLine("TryParse: {0}, {1}, {2} => {3}", int.Parse(c[0]), int.Parse(c[1]), double.Parse(c[2]), value.ToFormattedString(Format.Deg));
                        return true;
                }
            }
            catch
            {
                return false;
            }

            if (double.TryParse(coordinates, out val))
            {
                value.Degrees = val;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);
        }

        public string ToFormattedString(Format format = Format.Deg)
        {
            switch (format)
            {

                case Format.RA:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);

                case Format.Deg:
                case Format.Dec:
                case Format.Az:
                case Format.Alt:
                    return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);

                case Format.Double:
                    return string.Format("{0:0.000000}", Degrees);

                case Format.Rad:
                    return string.Format("{0:0.000000}", Radians);

                case Format.RAhms:
                case Format.HAhms:
                case Format.HA:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);
                case Format.HMS:
                    return ascomutils.DegreesToHMS(Hours, "h", "m", "s", 1);
            }
            return "";
        }

        public static Angle operator +(Angle a1, Angle a2)
        {
            if ((object)a1 == null)
                return a2;
            else if ((object)a2 == null)
                return a1;

            return new Angle((a1.Degrees + a2.Degrees) % 360.0);
        }

        public static Angle operator -(Angle a1, Angle a2)
        {
            if ((object)a1 == null)
                return a2;
            else if ((object)a2 == null)
                return a1;

            return new Angle((a1.Degrees - a2.Degrees) % 360.0);
        }

        public static bool operator >(Angle a1, Angle a2)
        {
            return (a1.Degrees > a2.Degrees) ? true : false;
        }

        public static bool operator <(Angle a1, Angle a2)
        {
            return (a1.Degrees < a2.Degrees) ? true : false;
        }

        public static bool operator ==(Angle a1, Angle a2)
        {
            if (System.Object.ReferenceEquals(a1, a2))
                return true;

            if (((object)a1 == null || ((object)a2 == null)))
                return false;

            return a1.Degrees == a2.Degrees;
        }

        public static bool operator !=(Angle a1, Angle a2)
        {
            return !(a1 == a2);
        }

        public static bool operator <=(Angle a1, Angle a2)
        {
            return a1.Degrees <= a2.Degrees;
        }

        public static bool operator >=(Angle a1, Angle a2)
        {
            return a1.Degrees >= a2.Degrees;
        }

        public static Angle Min(Angle a1, Angle a2)
        {
            if ((object)a1 == null || ((object)a2 == null))
                return null;
            return new Angle((a1.Degrees < a2.Degrees) ? a1.Degrees : a2.Degrees);
        }

        public static Angle Max(Angle a1, Angle a2)
        {

            if ((object)a1 == null || ((object)a2 == null))
                return null;
            return new Angle((a1.Degrees > a2.Degrees) ? a1.Degrees : a2.Degrees);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Angle b = obj as Angle;
            return b.Degrees == Degrees;
        }

        public bool Equals(Angle a)
        {
            if ((object)a == null)
                return false;
            return a.Degrees == Degrees;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public ShortestDistanceResult ShortestDistance(Angle other)
        {
            Angle incSide, decSide;
            ShortestDistanceResult res = new ShortestDistanceResult();

            Debugger debugger = new Debugger();
            debugger.Level = 25;

            if (other == this)
                return new ShortestDistanceResult(Angle.zero, Const.AxisDirection.None);

            if (other > this)
            {
                decSide = other - this;
                incSide = this + (Angle.max - other);
            }
            else
            {
                decSide = other + (Angle.max - this);
                incSide = this - other;
            }

            if (incSide < decSide)
            {
                res.angle = incSide;
                res.direction = Const.AxisDirection.Decreasing;
            } else
            {
                res.angle = decSide;
                res.direction = Const.AxisDirection.Increasing;
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "ShortestDistance: {0} -> {1} ==> {2} {3}", this, other, res.angle, res.direction);
            return res;
        }

        public static readonly Angle zero = new Angle(0.0);
        public static readonly Angle invalid = new Angle(double.NaN);
        public static readonly Angle max = new Angle(360.0);
    }
}