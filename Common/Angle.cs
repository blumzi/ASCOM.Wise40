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

        /// <summary>
        /// Whether this angle type is measured in hours rather than degrees, i.e. whether the
        ///  Angle constructor will read a bare value as HOURS.
        /// </summary>
        /// <remarks>
        /// The instance field _isHMS says the same thing, but it is only available once an Angle
        ///  exists - and the conversions below have to know BEFORE constructing one.
        /// </remarks>
        public static bool IsHms(AngleType type)
        {
            return type == AngleType.RA || type == AngleType.HA;
        }

        public static Angle FromRadians(double rad, AngleType type = AngleType.Deg)
        {
            //
            // Type-aware since 2026-09-19.  This used to be, unconditionally:
            //
            //      return new Angle(rad * 180.0 / Math.PI, type);
            //
            // i.e. it converted radians to DEGREES whatever the type was - and for an HMS type
            //  the constructor reads a bare value as HOURS.  Every radians-to-HMS conversion was
            //  therefore 15x too large, 15 being degrees per hour.  0.356675 rad is 20.436
            //  degrees, and that number went in as 20.436 hours.
            //
            // It surfaced through ShortestDistance's non-periodic branch, which is the only
            //  unguarded caller that can receive an HMS type.  AngleType.HA is the only type that
            //  is both non-periodic and HMS, which is why nothing else ever showed it: Dec takes
            //  the same branch but is degree-based, and RA and Az are periodic and take the other
            //  branch, which was already guarded by _isHMS.
            //
            // The consequence was not subtle.  An HA-targeted Park slew computed 5.3502 rad for a
            //  0.357 rad move, never approached arrival, and ran the primary axis about 80 degrees
            //  into a physical limit switch.  See TestAngleHa, which reproduces all of it in under
            //  a second without a telescope.
            //
            // Angle.Min, Angle.Max and RaFromRadians/HaFromRadians were wrong by the same 15x for
            //  the same reason; the first two are fixed by this change, the helpers below
            //  duplicated the expression and are fixed there.
            //
            return IsHms(type) ?
                new Angle(Rad2Hours(rad), type) :
                new Angle(rad * 180.0 / Math.PI, type);
        }

        public static Angle RaFromRadians(double rad)
        {
            //
            // Rad2Hours, not degrees - see FromRadians above.  This was 15x too large, and
            //  because RA is periodic over 0..24 the error WRAPPED rather than blowing up:
            //  1.701696 rad should be 6.5h and came out as 1.5h, since 97.5 mod 24 is 1.5.  A
            //  plausible wrong answer is worse than an obvious one.
            //
            return new Angle(Rad2Hours(rad), AngleType.RA);
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
            // Rad2Hours, not degrees - see FromRadians above.
            return new Angle(Rad2Hours(rad), AngleType.HA);
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

        /// <summary>
        /// ToNiceString() with leading all-zero groups dropped, for places where the full
        ///  form does not fit:
        ///
        ///     00h00m00.3s  ->  0.3s
        ///     00h12m34.5s  ->  12m34.5s
        ///     00°00'08.6"  ->  8.6"
        ///     00°26'37.3"  ->  26'37.3"
        ///     48°50'01.7"  ->  48°50'01.7"     (nothing to drop)
        ///
        /// Note this is NOT ToShortNiceString(), which only abbreviates Alt and Az and
        ///  falls through to ToNiceString() for everything else.
        ///
        /// Best kept for DIFFERENCES rather than positions.  Dropping the units off the
        ///  front makes a small angle much easier to read, but it also removes the cue that
        ///  says whether you are looking at hours or degrees - fine for "how far is left to
        ///  go", misleading for a coordinate.
        /// </summary>
        public string ToCompactString()
        {
            string s = ToNiceString();

            if (s == "Invalid")
                return s;

            string sign = "";
            if (s.StartsWith("-") || s.StartsWith("+"))
            {
                sign = s.Substring(0, 1);
                s = s.Substring(1);
            }

            // leading zero hours or degrees, then leading zero minutes
            s = System.Text.RegularExpressions.Regex.Replace(s, @"^0+[h°]", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"^0+[m']", "");

            // and any zero padding left on the front of the remaining number, keeping one digit
            s = System.Text.RegularExpressions.Regex.Replace(s, @"^0+(?=\d)", "");

            return sign + s;
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

        /// <summary>
        /// Converts an Increasing/Decreasing in a target's OWN coordinate into the mechanical
        ///  sense that the telescope's motors and coast figures are keyed by.
        /// </summary>
        //
        // ShortestDistance reports its direction in the coordinate it was given: "Increasing"
        //  means the number goes up, nothing more.  The driver's movementDict maps
        //  axisPrimary+Increasing to the EAST motor, and the per-direction coast figures in
        //  realMovementParameters were measured east and west.  Both are therefore in RIGHT
        //  ASCENSION sense, because right ascension increases EASTWARD.
        //
        // Hour angle increases WESTWARD - HA = LST - RA - so for an hour-angle target every
        //  direction out of ShortestDistance means the opposite motor and the opposite coast.
        //
        // Unconverted, that is precisely what happened on 2026-09-19.  A Park from HA
        //  -01h21m44.8s to HA 0 needs the hour angle to INCREASE, which is westward; the
        //  dictionary read Increasing and started the EAST motor.  The axis ran about 80 degrees
        //  the wrong way until a physical limit switch cut its power.  The 15x distance error
        //  fixed in FromRadians was the other half of that failure; this is the half that
        //  survived it, and the reason slew-to-ha-dec stayed disabled after the distance was
        //  correct again.
        //
        // Kept here rather than in the driver so TestAngleHa can check it without a telescope,
        //  and beside ShortestDistance because it is only ever applied to that method's output.
        //
        public static Const.AxisDirection MechanicalDirection(Const.AxisDirection coordinateDirection, AngleType type)
        {
            if (type != AngleType.HA)
                return coordinateDirection;

            if (coordinateDirection == Const.AxisDirection.Increasing)
                return Const.AxisDirection.Decreasing;

            if (coordinateDirection == Const.AxisDirection.Decreasing)
                return Const.AxisDirection.Increasing;

            return coordinateDirection;     // None stays None
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