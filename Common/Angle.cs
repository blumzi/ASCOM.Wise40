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
        public enum Type {  Deg, RA, Dec, HA, Az, Alt, None };

        //private double _degrees;
        private double _radians;
        private bool _periodic;
        private double _highest;
        private double _lowest;
        private bool _highestIncluded;
        private bool _lowestIncluded;
        private Type _type;
        private bool _isHMS;
        private readonly static double pi = Math.PI;

        public Angle(double val = double.NaN,
            Type type = Type.Deg,
            double highest = double.PositiveInfinity,
            bool highestIncluded = false,
            double lowest = double.NegativeInfinity,
            bool lowestIncluded = false)
        {
            this._type = type;
            switch (this._type)
            {
                case Type.Deg:
                    _periodic = true;
                    _highest = highest;
                    _lowest = lowest;
                    _lowestIncluded = lowestIncluded;
                    _highestIncluded = highestIncluded;
                    _isHMS = false;
                    break;

                case Type.RA:
                    _periodic = true;
                    _lowest = 0.0;
                    _lowestIncluded = true;
                    _highest = 24.0;
                    _highestIncluded = false;
                    _isHMS = true;
                    break;

                case Type.Dec:
                case Type.Alt:
                    _periodic = false;
                    _lowest = -(pi/2);
                    _lowestIncluded = true;
                    _highest = pi/2;
                    _highestIncluded = true;
                    _isHMS = false;
                    break;

                case Type.HA:
                    _periodic = false;
                    _lowest = -12.0;
                    _lowestIncluded = true;
                    _highest = 12.0;
                    _highestIncluded = true;
                    _isHMS = true;
                    break;

                case Type.Az:
                    _periodic = true;
                    _lowest = 0;
                    _lowestIncluded = true;
                    _highest = 2*pi;
                    _highestIncluded = false;
                    _isHMS = false;
                    break;
            }

            //if (_cyclic)
            //    val = normalize(this, val);

            //if (val != double.NaN)
            //{
            //    if (_lowestIncluded && val < _lowest)
            //        throw new InvalidValueException(string.Format("value: {0} < lowest: {1}", val, _lowest));
            //    if (!_lowestIncluded && val <= _lowest)
            //        throw new InvalidValueException(string.Format("value: {0} <= lowest: {1}", val, _lowest));
            //    if (_highestIncluded && val > _highest)
            //        throw new InvalidValueException(string.Format("value: {0} > highest: {1}", val, _highest));
            //    if (!_highestIncluded && val >= _highest)
            //        throw new InvalidValueException(string.Format("value: {0} >= highest: {1}", val, _highest));
            //}

            //if (_cyclic)
            //    val = normalize(this, val);
            //val = normalize(this, val);

            //_degrees = _isHMS ? val * 15.0 : val;
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
                //Degrees = sign * ascomutils.HMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s));
                Radians = Deg2Rad(sign * ascomutils.HMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s)));
            else
                //Degrees = sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s));
                Radians = Deg2Rad(sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s)));
        }

        public Angle(string sexagesimal)
        {
            char[] hoursSeparators = { 'h', 'H' };

            if (sexagesimal.IndexOfAny(hoursSeparators) == -1)
            {
                char[] delimiters = { ':', 'd', 'm', 's' };
                double deg = 0.0, min = 0.0, sec = 0.0;

                this._type = Type.Deg;
                this._isHMS = false;
                this._periodic = false;
                this._highest = double.PositiveInfinity;
                this._highestIncluded = false;
                this._lowest = double.NegativeInfinity;
                this._lowestIncluded = false;

                if (sexagesimal.EndsWith("s"))
                    sexagesimal = sexagesimal.TrimEnd(new char[] { 's' });
                string[] words = sexagesimal.Split(delimiters);
                switch (words.Count())
                {
                    case 1:
                        sec = Convert.ToDouble(words[0]);
                        break;
                    case 2:
                        min = Convert.ToDouble(words[0]);
                        sec = Convert.ToDouble(words[1]);
                        break;
                    case 3:
                        deg = Convert.ToDouble(words[0]);
                        min = Convert.ToDouble(words[1]);
                        sec = Convert.ToDouble(words[2]);
                        break;
                }
                //this._degrees = normalize(this, deg + (min / 60) + (sec / 3600));

                //this._degrees = ascomutils.DMSToDegrees(sexagesimal);
                this.Radians = Deg2Rad(ascomutils.DMSToDegrees(sexagesimal));
            }
            else
            {
                char[] delimiters = { 'h', 'm', 's' };
                double hr = 0.0, min = 0.0, sec = 0.0;

                this._type = Type.RA;
                this._isHMS = true;
                this._periodic = true;
                this._highest = 24.0;
                this._highestIncluded = false;
                this._lowest = 0.0;
                this._lowestIncluded = true;

                if (sexagesimal.EndsWith("s"))
                    sexagesimal = sexagesimal.TrimEnd(new char[] {'s'});
                string[] words = sexagesimal.Split(delimiters);
                switch (words.Count())
                {
                    case 1:
                        sec = Convert.ToDouble(words[0]);
                        break;
                    case 2:
                        min = Convert.ToDouble(words[0]);
                        sec = Convert.ToDouble(words[1]);
                        break;
                    case 3:
                        hr = Convert.ToDouble(words[0]);
                        min = Convert.ToDouble(words[1]);
                        sec = Convert.ToDouble(words[2]);
                        break;
                }
                //this._degrees = ascomutils.HMSToDegrees(sexagesimal);
                //this._degrees = normalize(this, hr + (min / 60) + (sec / 3600)) * 15.0;
                this.Radians = Deg2Rad(ascomutils.HMSToDegrees(sexagesimal));
            }
        }

        //private int Sign
        //{
        //    get
        //    {
        //        return _degrees < 0 ? -1 : 1;
        //    }
        //}

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

        public static Angle FromRadians(double rad, Type type = Type.Deg)
        {
            return new Angle(rad * 180.0 / Math.PI, type);
        }

        public static Angle FromDegrees(double deg, Type type = Type.Deg)
        {
            return new Angle(deg, type);
        }

        public static Angle FromHours(double hours, Type type = Type.RA)
        {
            return new Angle(hours, type);
        }        

        public static double normalize(Angle a, double d)
        {
            if (a._periodic)
            {

                double abs = Math.Abs(d) % a._highest;
                int sign = Math.Sign(d);

                d = (sign < 0) ? a._highest - abs : a._lowest + abs;
            }
            else if (Math.Abs(d) > a._highest && (a._type == Type.Dec || a._type == Type.Alt))
            {
                double abs = Math.Abs(d);
                int sign = Math.Sign(d);

                abs = a._highest - (abs % a._highest);
                d = abs * sign;
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
                    while (value > 2 * pi)
                        value -= 2 * pi;
                    while (value < -2 * pi)
                        value += 2 * pi;
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
            if (_isHMS)
                return ascomutils.DegreesToHMS(Degrees, "h", "m", "s", 1);
            else if (_type == Type.Az)
                return Degrees.ToString("0.0°");
            else
                return ascomutils.DegreesToDMS(Degrees, "°", "'", "\"", 1);
        }

        private static double normalizeAltAndDec(Angle a, double d)
        {
            if (d > a._highest)
                return a._highest - Math.Abs(a._highest - d);
            else if (d < a._lowest)
                return a._lowest - Math.Abs(a._lowest - d);
            else return d;
        }

        public static Angle operator +(Angle a1, Angle a2)
        {
            if ((object)a1 == null && (object)a2 == null)
                return null;
            else if ((object)a1 == null)
                return a2;
            else if ((object)a2 == null)
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
            if ((object)a1 == null && (object)a2 == null)
                return null;
            if ((object)a1 == null)
                return a2;
            else if ((object)a2 == null)
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
            return (a1.Radians > a2.Radians) ? true : false;
        }

        public static bool operator <(Angle a1, Angle a2)
        {
            return (a1.Radians < a2.Radians) ? true : false;
        }

        public static bool operator ==(Angle a1, Angle a2)
        {
            if (System.Object.ReferenceEquals(a1, a2))
                return true;

            if (((object)a1 == null || ((object)a2 == null)))
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

        public static Angle Min(Angle a1, Angle a2)
        {
            if ((object)a1 == null || ((object)a2 == null))
                return null;

            var min = (a1.Radians <= a2.Radians) ? a1 : a2;
            return Angle.FromRadians(min.Radians, min._type);
        }

        public static Angle Max(Angle a1, Angle a2)
        {

            if ((object)a1 == null || ((object)a2 == null))
                return null;

            var max = (a1.Radians >= a2.Radians) ? a1 : a2;
            return Angle.FromRadians(max.Radians, max._type);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Angle b = obj as Angle;
            return Math.Abs(b.Radians - Radians) <= epsilonRad;
        }

        public bool Equals(Angle a)
        {
            if ((object)a == null)
                return false;
            return Math.Abs(a.Radians - Radians) <= epsilonRad;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public ShortestDistanceResult ShortestDistance(Angle other)
        {
            Angle incSide, decSide;
            ShortestDistanceResult result = new ShortestDistanceResult();

            Debugger debugger = Debugger.Instance;

            if (other == this)
                return new ShortestDistanceResult(Angle.zero, Const.AxisDirection.None);

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

        public static readonly Angle zero = new Angle(0.0);
        public static readonly Angle invalid = new Angle(double.NaN);
        public static double epsilonRad = Deg2Rad((1.0 / 3600.0) / 1000000.0);    // 1 micro-second
    }
}