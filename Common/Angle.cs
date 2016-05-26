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
    };

    public class Angle
    {
        internal static Astrometry.AstroUtils.AstroUtils astroutils = new Astrometry.AstroUtils.AstroUtils();
        internal static ASCOM.Utilities.Util ascomutils = new ASCOM.Utilities.Util();
        public enum Format { Deg, RA, Dec, HA, Alt, Az, Double, Rad, HAhms, RAhms };

        private double _degrees;

        private int Sign
        {
            get
            {
                return _degrees < 0 ? -1 : 1;
            }
        }

        public int D
        {
            get
            {
                return (int) Math.Floor(_degrees);
            }
        }

        public int M
        {
            get
            {
                return (int) Math.Floor((_degrees - D) * 60);
            }
        }

        public double S
        {
            get
            {
                return (((_degrees - D) * 60) - M) * 60;
            }
        }

        public int H
        {
            get
            {
                return D / 15;
            }
        }

        public static Angle FromRad(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI);
        }

        public static Angle FromDeg(double deg)
        {
            return new Angle(deg);
        }

        public double Degrees
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
                return _degrees * Math.PI / 180.0;
            }

            set
            {
                _degrees = value * 180.0 * Math.PI;
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
            _degrees = sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", d, m, s));
        }

        public Angle(string s)
        {
            _degrees = ascomutils.DMSToDegrees(s);
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
                case Format.Deg:
                    return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);
                case Format.RA:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);
                case Format.Dec:
                    return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);
                case Format.HA:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);
                case Format.Az:
                    return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);
                case Format.Alt:
                    return ascomutils.DegreesToDMS(_degrees, "°", "'", "\"", 1);
                case Format.Double:
                    return string.Format("{0:0.000000}", Degrees);
                case Format.Rad:
                    return string.Format("{0:0.000000}", Radians);
                case Format.RAhms:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);
                case Format.HAhms:
                    return ascomutils.DegreesToHMS(_degrees, "h", "m", "s", 1);
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
            Angle incSide, decSide, smaller;
            Const.AxisDirection dir;

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
                smaller = incSide;
                dir = Const.AxisDirection.Decreasing;
            } else
            {
                smaller = decSide;
                dir = Const.AxisDirection.Increasing;
            }
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "ShortestDistance: from {4}, to {5}, ret: <{0}, {1}>, inc: {2}, dec: {3}", smaller, dir, incSide, decSide, this, other);
            return new ShortestDistanceResult(smaller, dir);
        }

        public static readonly Angle zero = new Angle(0.0);
        public static readonly Angle invalid = new Angle(double.NaN);
        public static readonly Angle max = new Angle(360.0);
    }
}