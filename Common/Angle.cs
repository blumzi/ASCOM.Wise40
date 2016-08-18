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
        //public enum Format { Deg, RA, Dec, HA, Alt, Az, Double, Rad, HAhms, RAhms, HMS };
        public enum Type {  Deg, RA, Dec, HA, Az, Alt, None };

        private double _degrees;
        private bool _cyclic;
        private double _highest;
        private double _lowest;
        private bool _highestIncluded;
        private bool _lowestIncluded;
        private Type _type;
        private bool _isHMS;

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
                    _cyclic = false;
                    _highest = highest;
                    _lowest = lowest;
                    _lowestIncluded = lowestIncluded;
                    _highestIncluded = highestIncluded;
                    _isHMS = false;
                    break;

                case Type.RA:
                    _cyclic = true;
                    _lowest = 0.0;
                    _lowestIncluded = true;
                    _highest = 24.0;
                    _highestIncluded = false;
                    _isHMS = true;
                    break;

                case Type.Dec:
                case Type.Alt:
                    _cyclic = false;
                    _lowest = -90.0;
                    _lowestIncluded = true;
                    _highest = 90.0;
                    _highestIncluded = true;
                    _isHMS = false;
                    break;

                case Type.HA:
                    _cyclic = false;
                    _lowest = -12.0;
                    _lowestIncluded = true;
                    _highest = 12.0;
                    _highestIncluded = true;
                    _isHMS = true;
                    break;

                case Type.Az:
                    _cyclic = true;
                    _lowest = 0;
                    _lowestIncluded = true;
                    _highest = 360.0;
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

            _degrees = _isHMS ? val * 15.0 : val;
        }

        public Angle(int u, int m, double s, int sign = 1)
        {
            if (_isHMS)
                Degrees = sign * ascomutils.HMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s));
            else
                Degrees = sign * ascomutils.DMSToDegrees(string.Format("{0}:{1}:{2}", u, m, s));
        }

        public Angle(string s)
        {
            char[] hoursSeparators = { 'h', 'H' };

            if (s.IndexOfAny(hoursSeparators) == -1)
            {
                char[] delimiters = { ':' };
                double deg = 0.0, min = 0.0, sec = 0.0;

                this._type = Type.Deg;
                this._isHMS = false;
                this._cyclic = false;
                this._highest = double.PositiveInfinity;
                this._highestIncluded = false;
                this._lowest = double.NegativeInfinity;
                this._lowestIncluded = false;
                string[] words = s.Split(delimiters);
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

                this._degrees = ascomutils.DMSToDegrees(s);
            }
            else
            {
                char[] delimiters = { 'h', 'm', 's' };
                double hr = 0.0, min = 0.0, sec = 0.0;

                this._type = Type.RA;
                this._isHMS = true;
                this._cyclic = true;
                this._highest = 24.0;
                this._highestIncluded = false;
                this._lowest = 0.0;
                this._lowestIncluded = true;

                if (s.EndsWith("s"))
                    s = s.TrimEnd(new char[] {'s'});
                string[] words = s.Split(delimiters);
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
                this._degrees = ascomutils.HMSToDegrees(s);
                //this._degrees = normalize(this, hr + (min / 60) + (sec / 3600)) * 15.0;
            }
        }

        private int Sign
        {
            get
            {
                return _degrees < 0 ? -1 : 1;
            }
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
            if (a._cyclic)
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
                return _degrees;
            }

            set
            {
                if (_cyclic)
                    value = (value %= _highest) >= 0.0 ? value : (value + _highest);

                //if (_highest != double.PositiveInfinity)
                //{
                //    if (_highestIncluded && value > _highest)
                //        throw new InvalidValueException(string.Format("value: {0} > highest: {1}", value, _highest));
                //    else if (!_highestIncluded && value >= _highest)
                //        throw new InvalidValueException(string.Format("value: {0} >= highest: {1}", value, _highest));
                //}

                //if (_lowest != double.NegativeInfinity)
                //{
                //    if (_lowestIncluded && value < _lowest)
                //        throw new InvalidValueException(string.Format("value: {0} < lowest: {1}", value, _lowest));
                //    else if (!_lowestIncluded && value <= _lowest)
                //        throw new InvalidValueException(string.Format("value: {0} <= lowest: {1}", value, _lowest));
                //}

                _degrees = value;
            }
        }

        public double Hours
        {
            get
            {
                return _degrees / 15.0;
            }

            set
            {
                Degrees = value * 15.0;
            }
        }

        //public double Declination
        //{
        //    get
        //    {
        //        return _value;
        //    }

        //    set
        //    {
        //        if (value < -90.0 || value > 90.0)
        //            throw new InvalidValueException(string.Format("Invalid value {0}, must be between -90.0 and 90.0", value));
        //        _value = value;
        //    }
        //}

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

        public override string ToString()
        {
            return _isHMS ? 
                ascomutils.DegreesToHMS(Degrees, "h", "m", "s", 1) : 
                ascomutils.DegreesToDMS(Degrees, ":", ":", "", 1);
        }

        public string ToNiceString()
        {
            return _isHMS ?
                ascomutils.DegreesToHMS(Degrees, "h", "m", "s", 1) :
                ascomutils.DegreesToDMS(Degrees, "°", "'", "\"", 1);
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

            double result = a1.Degrees + a2.Degrees;
            if (a1._cyclic)
            {
                double max = a1._isHMS ? a1._highest * 15.0 : a1._highest;
                result %= max;
                if (result < 0.0)
                    result += max;
            } else if (a1._type == Type.Alt || a1._type == Type.Dec)
                result = normalizeAltAndDec(a1, result);
            return a1._isHMS ? Angle.FromHours(result / 15.0, a1._type) : Angle.FromDegrees(result, a1._type);
        }

        public static Angle operator -(Angle a1, Angle a2)
        {
            if ((object)a1 == null && (object)a2 == null)
                return null;
            if ((object)a1 == null)
                return a2;
            else if ((object)a2 == null)
                return a1;

            double result = a1.Degrees - a2.Degrees;
            if (a1._cyclic)
            {
                double max = a1._isHMS ? a1._highest * 15.0 : a1._highest;
                result %= max;
                if (result < 0.0)
                    result += max;
            }
            else if (a1._type == Type.Alt || a1._type == Type.Dec)
                result = normalizeAltAndDec(a1, result);
            return a1._isHMS ? Angle.FromHours(result / 15.0, a1._type) : Angle.FromDegrees(result, a1._type);
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

            var min = (a1.Degrees <= a2.Degrees) ? a1 : a2;
            return new Angle(min.Degrees, min._type);
        }

        public static Angle Max(Angle a1, Angle a2)
        {

            if ((object)a1 == null || ((object)a2 == null))
                return null;

            var max = (a1.Degrees >= a2.Degrees) ? a1 : a2;
            return new Angle(max.Degrees, max._type);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Angle b = obj as Angle;
            return Math.Abs(b.Degrees - Degrees) <= epsilon;
        }

        public bool Equals(Angle a)
        {
            if ((object)a == null)
                return false;
            return Math.Abs(a.Degrees - Degrees) <= epsilon;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public ShortestDistanceResult ShortestDistance(Angle other)
        {
            Angle incSide, decSide;
            ShortestDistanceResult res = new ShortestDistanceResult();

            Debugger debugger = Debugger.Instance;

            if (other == this)
                return new ShortestDistanceResult(Angle.zero, Const.AxisDirection.None);

            if (_cyclic)
            {
                if (other > this)
                {
                    decSide = other - this;
                    incSide = this + ((_isHMS) ? 
                        Angle.FromHours(_highest - other.Hours, this._type) : 
                        Angle.FromDegrees(_highest - other.Degrees, this._type));
                }
                else
                {
                    decSide = other + ((_isHMS) ?
                        Angle.FromHours(_highest - this.Hours, this._type) :
                        Angle.FromDegrees(_highest - this.Degrees, this._type));
                    incSide = this - other;
                }

                if (incSide < decSide)
                {
                    res.angle = incSide;
                    res.direction = Const.AxisDirection.Decreasing;
                }
                else
                {
                    res.angle = decSide;
                    res.direction = Const.AxisDirection.Increasing;
                }
                res.angle._type = this._type;

            }
            else
            {
                res.angle = new Angle(Math.Abs(this.Degrees - other.Degrees), Type.Deg);
                res.direction = (this.Degrees > other.Degrees) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "ShortestDistance: {0} -> {1} ==> {2} {3}", this, other, res.angle, res.direction);
            return res;
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
        public static double epsilon = (1.0 / 3600.0) / 1000000.0;    // 1 micro-second
    }
}