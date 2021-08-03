using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{
    public class Angle
    {
        internal static Astrometry.AstroUtils.AstroUtils astroutils = new Astrometry.AstroUtils.AstroUtils();
        internal static ASCOM.Utilities.Util ascomutils = new ASCOM.Utilities.Util();
        public enum AngleType {  Deg, RA, Dec, HA, Az, Alt, None };

        private double _radians;
        private readonly bool _periodic;
        private readonly double _highest;
        private readonly double _lowest;
        private readonly bool _highestIncluded;
        private readonly bool _lowestIncluded;
        private AngleType _type;
        private readonly bool _isHMS;
        private const double pi = Math.PI;

        public Angle(double val = double.NaN,
            AngleType type = AngleType.Deg,
            double highest = double.PositiveInfinity,
            bool highestIncluded = false,
            double lowest = double.NegativeInfinity,
            bool lowestIncluded = false)
        {
            this._type = type;
            switch (this._type)
            {
                case AngleType.Deg:
                    _periodic = true;
                    _highest = highest;
                    _lowest = lowest;
                    _lowestIncluded = lowestIncluded;
                    _highestIncluded = highestIncluded;
                    _isHMS = false;
                    break;

                case AngleType.RA:
                    _periodic = true;
                    _lowest = 0.0;
                    _lowestIncluded = true;
                    _highest = 24.0;
                    _highestIncluded = false;
                    _isHMS = true;
                    break;

                case AngleType.Dec:
                case AngleType.Alt:
                    _periodic = false;
                    _lowest = -(pi/2);
                    _lowestIncluded = true;
                    _highest = pi/2;
                    _highestIncluded = true;
                    _isHMS = false;
                    break;

                case AngleType.HA:
                    _periodic = false;
                    _lowest = -12.0;
                    _lowestIncluded = true;
                    _highest = 12.0;
                    _highestIncluded = true;
                    _isHMS = true;
                    break;

                case AngleType.Az:
                    _periodic = true;
                    _lowest = 0;
                    _lowestIncluded = true;
                    _highest = 2*pi;
                    _highestIncluded = false;
                    _isHMS = false;
                    break;
            }

            double rad = Deg2Rad(_isHMS ? Hours2Deg(val) : val);
            if (_periodic)
            {
                while (rad > _highest)
                    rad -= 2 * pi;
                while (rad < _lowest)
                    rad += 2 * pi;
            }

            //Radians = (_type == Type.Dec) ? Math.Acos(Math.Abs(Math.Cos(rad))) : rad;
            Radians = rad;
        }

        public Angle(int u, int m, double s, int sign = 1)
        {
            if (_isHMS)
            {
                Radians = Deg2Rad(sign * ascomutils.HMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s)));
            }
            else
            {
                Radians = Deg2Rad(sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s)));
            }
        }

        public Angle(string sexagesimal)
        {
            char[] hoursSeparators = { 'h', 'H' };

            if (sexagesimal.IndexOfAny(hoursSeparators) == -1)
            {
                //char[] delimiters = { ':', 'd', 'm', 's' };
                //double deg, min, sec;

                this._type = AngleType.Deg;
                this._isHMS = false;
                this._periodic = false;
                this._highest = double.PositiveInfinity;
                this._highestIncluded = false;
                this._lowest = double.NegativeInfinity;
                this._lowestIncluded = false;

                if (sexagesimal.EndsWith("s"))
                    sexagesimal = sexagesimal.TrimEnd(new char[] { 's' });
                //string[] words = sexagesimal.Split(delimiters);
                //switch (words.Length)
                //{
                //    case 1:
                //        sec = Convert.ToDouble(words[0]);
                //        break;
                //    case 2:
                //        min = Convert.ToDouble(words[0]);
                //        sec = Convert.ToDouble(words[1]);
                //        break;
                //    case 3:
                //        deg = Convert.ToDouble(words[0]);
                //        min = Convert.ToDouble(words[1]);
                //        sec = Convert.ToDouble(words[2]);
                //        break;
                //}

                this.Radians = Deg2Rad(ascomutils.DMSToDegrees(sexagesimal));
            }
            else
            {
                //char[] delimiters = { 'h', 'm', 's' };
                //double hr, min, sec;

                this._type = AngleType.RA;
                this._isHMS = true;
                this._periodic = true;
                this._highest = 24.0;
                this._highestIncluded = false;
                this._lowest = 0.0;
                this._lowestIncluded = true;

                if (sexagesimal.EndsWith("s"))
                    sexagesimal = sexagesimal.TrimEnd(new char[] {'s'});
                //string[] words = sexagesimal.Split(delimiters);
                //switch (words.Length)
                //{
                //    case 1:
                //        sec = Convert.ToDouble(words[0]);
                //        break;
                //    case 2:
                //        min = Convert.ToDouble(words[0]);
                //        sec = Convert.ToDouble(words[1]);
                //        break;
                //    case 3:
                //        hr = Convert.ToDouble(words[0]);
                //        min = Convert.ToDouble(words[1]);
                //        sec = Convert.ToDouble(words[2]);
                //        break;
                //}
                this.Radians = Deg2Rad(ascomutils.HMSToDegrees(sexagesimal));
            }
        }

        public AngleType Type
        {
            get
            {
                return _type;
            }
        }

        public static double Deg2Rad(double deg)
        {
            return (deg * pi) / 180.0;
        }

        public static double Rad2Deg(double rad)
        {
            return (rad * 180.0) / pi;
        }

        public static double Rad2Hours(double rad)
        {
            return rad * 24.0 / (2.0 * pi);
        }

        public static double Hours2Rad(double hours)
        {
            return hours * 2.0 * pi / 24.0;
        }

        public static Angle FromRadians(double rad, AngleType type = AngleType.Deg)
        {
            return new Angle(rad * 180.0 / Math.PI, type);
        }

        public static Angle RaFromRadians(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI, AngleType.RA);
        }

        public static Angle AzFromRadians(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI, AngleType.Az);
        }

        public static Angle FromDegrees(double deg, AngleType type = AngleType.Deg)
        {
            return new Angle(deg, type);
        }

        public static Angle DecFromRadians(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI, AngleType.Dec);
        }

        public static Angle HaFromRadians(double rad)
        {
            return new Angle(rad * 180.0 / Math.PI, AngleType.HA);
        }

        public static Angle FromHours(double hours, AngleType type = AngleType.RA)
        {
            return new Angle(hours, type);
        }

        public static Angle RaFromHours(double hours)
        {
            return FromHours(hours, AngleType.RA);
        }
        public static Angle HaFromHours(double hours)
        {
            return FromHours(hours, AngleType.HA);
        }

        public static Angle DecFromDegrees(double degrees)
        {
            return FromDegrees(degrees, AngleType.Dec);
        }

        public static Angle AltFromDegrees(double degrees)
        {
            return FromDegrees(degrees, AngleType.Alt);
        }

        public static Angle AzFromDegrees(double degrees)
        {
            return FromDegrees(degrees, AngleType.Az);
        }

        public static double Normalize(Angle a, double d)
        {
            if (a._periodic)
            {
                double abs = Math.Abs(d) % a._highest;
                int sign = Math.Sign(d);

                return (sign < 0) ? a._highest - abs : a._lowest + abs;
            }
            else if (Math.Abs(d) > a._highest && (a._type == AngleType.Dec || a._type == AngleType.Alt))
            {
                double abs = Math.Abs(d);
                int sign = Math.Sign(d);

                abs = a._highest - (abs % a._highest);
                return abs * sign;
            }
            return d;
        }

        /// <summary>
        /// Exposes the internal value, converted to degrees if _isHSM.
        /// </summary>
        public double Degrees
        {
            get
            {
                return Rad2Deg(Radians);
            }

            set
            {
                if (_periodic)
                    value = (value %= _highest) >= 0.0 ? value : (value + _highest);

                Radians = value;
            }
        }

        public double Hours
        {
            get
            {
                return Rad2Hours(Radians);
            }

            set
            {
                Radians = Hours2Rad(value);
            }
        }

        public double Radians
        {
            get
            {
                return _radians;
            }

            set
            {
                if (_periodic)
                {
                    if (!double.IsInfinity(value))
                    {
                        while (value > 2 * pi)
                            value -= 2 * pi;
                        while (value < -2 * pi)
                            value += 2 * pi;
                    }
                }
                _radians = value;
            }
        }

        public override string ToString()
        {
            return _isHMS ?
                ascomutils.DegreesToHMS(Degrees, "h", "m", "s", 1) :
                ascomutils.DegreesToDMS(Degrees, ":", ":", "", 1);
        }

        public string ToNiceString()
        {
            if (Double.IsNaN(Radians))
                return "Invalid";

            return _isHMS ?
                ascomutils.DegreesToHMS(Degrees, "h", "m", "s", 1) :
                ascomutils.DegreesToDMS(Degrees, "°", "'", "\"", 1);
        }

        public string ToShortNiceString()
        {
            if (Double.IsNaN(Radians))
                return "Invalid";

            return (_type == AngleType.Az || _type == AngleType.Alt) ?
                Degrees.ToString("0.0°") :
                ToNiceString();
        }

        //private static double NormalizeAltAndDec(Angle a, double d)
        //{
        //    if (d > a._highest)
        //        return a._highest - Math.Abs(a._highest - d);
        //    else if (d < a._lowest)
        //        return a._lowest - Math.Abs(a._lowest - d);
        //    else return d;
        //}

        public static Angle operator +(Angle a1, Angle a2)
        {
            if (a1 is null && a2 is null)
                return null;
            else if (a1 is null)
                return a2;
            else if (a2 is null)
                return a1;

            double radians = a1.Radians + a2.Radians;

            if (a1._periodic)
            {
                double max = a1._isHMS ? Hours2Rad(a1._highest) : a1._highest;
                radians %= max;
            }

            return a1._isHMS ? Angle.FromHours(Rad2Hours(radians), a1._type) : Angle.FromRadians(radians, a1._type);
        }

        public static Angle operator -(Angle a1, Angle a2)
        {
            if (a1 is null && a2 is null)
                return null;
            if (a1 is null)
                return a2;
            else if (a2 is null)
                return a1;

            double radians = a1.Radians - a2.Radians;
            if (a1._periodic)
            {
                double max = a1._isHMS ? Hours2Rad(a1._highest) : a1._highest;
                radians %= max;

                while (radians < -2 * pi)
                    radians += 2 * pi;
            }

            return a1._isHMS ? Angle.FromHours(Rad2Hours(radians), a1._type) : Angle.FromRadians(radians, a1._type);
        }

        public static bool operator >(Angle a1, Angle a2)
        {
            return a1.Radians > a2.Radians;
        }

        public static bool operator <(Angle a1, Angle a2)
        {
            return a1.Radians < a2.Radians;
        }

        public static bool operator ==(Angle a1, Angle a2)
        {
            if (System.Object.ReferenceEquals(a1, a2))
                return true;

            if (a1 is null || (a2 is null))
                return false;

            return a1.Radians == a2.Radians;
        }

        public static bool operator !=(Angle a1, Angle a2)
        {
            return !(a1 == a2);
        }

        public static bool operator <=(Angle a1, Angle a2)
        {
            return a1.Radians <= a2.Radians;
        }

        public static bool operator >=(Angle a1, Angle a2)
        {
            return a1.Radians >= a2.Radians;
        }

        public override int GetHashCode()
        {
            return _radians.GetHashCode() ^ _type.GetHashCode() ^ _isHMS.GetHashCode();
        }

        public static Angle Min(Angle a1, Angle a2)
        {
            if (a1 is null || (a2 is null))
                return null;

            var min = (a1.Radians <= a2.Radians) ? a1 : a2;
            return Angle.FromRadians(min.Radians, min._type);
        }

        public static Angle Max(Angle a1, Angle a2)
        {
            if (a1 is null || a2 is null)
                return null;

            var max = (a1.Radians >= a2.Radians) ? a1 : a2;
            return Angle.FromRadians(max.Radians, max._type);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Angle b = obj as Angle;
            return Math.Abs(b.Radians - Radians) <= EpsilonRad;
        }

        public bool Equals(Angle a)
        {
            if (a is null)
                return false;
            return Math.Abs(a.Radians - Radians) <= EpsilonRad;
        }

        public ShortestDistanceResult ShortestDistance(Angle other)
        {
            Angle incSide, decSide;
            ShortestDistanceResult result = new ShortestDistanceResult();

            Debugger debugger = Debugger.Instance;

            if (other == this)
                return new ShortestDistanceResult(new Angle(0.0, this._type), Const.AxisDirection.None);

            if (_periodic)
            {
                if (other > this)
                {
                    decSide = other - this;
                    incSide = this + ((_isHMS) ?
                        Angle.FromHours(_highest - other.Hours, this._type) :
                        Angle.FromRadians(_highest - other.Radians, this._type));
                }
                else
                {
                    decSide = other + ((_isHMS) ?
                        Angle.FromHours(_highest - this.Hours, this._type) :
                        Angle.FromRadians(_highest - this.Radians, this._type));
                    incSide = this - other;
                }

                if (incSide < decSide)
                {
                    result.angle = incSide;
                    result.direction = Const.AxisDirection.Decreasing;
                }
                else
                {
                    result.angle = decSide;
                    result.direction = Const.AxisDirection.Increasing;
                }
                result.angle._type = this._type;
            }
            else
            {
                result.angle = Angle.FromRadians(Math.Abs(this.Radians - other.Radians), this._type);
                result.direction = (this.Radians > other.Radians) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "ShortestDistance: {0} -> {1} ==> {2} {3}", this, other, result.angle, result.direction);
            return result;
        }

        public static double Deg2Hours(string s)
        {
            return new Angle(s).Hours;
        }

        public static double Deg2Hours(double deg)
        {
            return deg / 15.0;
        }

        public static double Hours2Deg(string s)
        {
            return new Angle(s).Degrees;
        }

        public static double Hours2Deg(double hours)
        {
            return hours * 15.0;
        }

        public static readonly Angle Zero = new Angle(0.0);
        public static readonly Angle Invalid = new Angle(double.NaN);
        public static readonly Angle InvalidAz = new Angle(double.NaN, AngleType.Az);
        public static double EpsilonRad = Deg2Rad(1.0 / 3600.0 / 1000000.0);    // 1 micro-second
    }
}