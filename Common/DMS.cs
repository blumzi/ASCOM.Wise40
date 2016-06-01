namespace ASCOM.Wise40.Common
{
    using System.Threading;
    using System;

    public class DMS
    {
        internal static string decimalSeparator = Thread.CurrentThread.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator;
        //private double _degrees;

        public double D { get; set; }
        public double M { get; set; }
        public double S { get; set; }
        public int Sign { get; set; }
        public bool isRA { get; set; }

        public static DMS FromRad(double rad)
        {
            return new DMS(rad * 180.0 / Math.PI);
        }

        public static DMS FromDeg(double deg, bool IsRA = false)
        {
            return new DMS(deg, IsRA);
        }

        public double Deg
        {
            set
            {
                if (value < 0)
                {
                    Sign = -1;
                    value = value * -1;
                }
                else {
                    Sign = 1;
                }
                D = Math.Floor(value);
                value = (value - D) * 60;
                M = Math.Floor(value);
                S = (value - M) * 60;
            }

            get
            {
                double ret = (D + M / 60 + S / 3600) * Sign;
                return ret;
            }
        }

        public double Rad
        {
            get
            {
                return Deg * Math.PI * 180.0;
            }

            set
            {
                Deg = value * 180.0 * Math.PI;
            }
        }

        public DMS(double deg, bool IsRA = false)
        {
            this.Deg = deg;
            this.isRA = IsRA;
        }

        public DMS(int d, int m, double s, int sign = 1)
        {
            this.D = d;
            this.M = m;
            this.S = s;
            this.Sign = sign;
        }

        public static bool TryParse(string coordinates, out DMS value)
        {
            value = new DMS(-1000);
            double val;

            //Regex r = new Regex(@"(\d+)[°\s]+(\d+)['\s]+(\d+)[\.\,]?(\d*)['\s]*");
            //var m = r.Match(coordinates);
            var c = coordinates.Split(new[] { ' ', '°', '\'', '.', ',', '"' });

            try
            {
                //if (m.Success)
                //{
                //    value.D = int.Parse(m.Groups[1].Value);
                //    value.M = int.Parse(m.Groups[2].Value);
                //    var dig = m.Groups[4].Length > 0 ? decimal.Parse("0" + Telescope.decimalSeparator + m.Groups[4].Value) : 0;
                //    value.S = int.Parse(m.Groups[3].Value) + dig;
                if (c.Length > 2)
                {
                    value.D = int.Parse(c[0]);
                    value.M = int.Parse(c[1]);
                    var dig = c.Length > 3 ? double.Parse("0" + decimalSeparator + c[3]) : 0;
                    value.S = int.Parse(c[2]) + dig;

                    return true;
                }
            }
            catch
            {
                return false;
            }

            if (double.TryParse(coordinates, out val))
            {
                value.Deg = val;
                return true;
            }
            return false;
        }

        override public string ToString()
        {
            return (isRA) ? string.Format("{0:d2}:{1:d2}:{2:00.0}", (int)(D/15), (int)M, S) :
                string.Format("{0:d2}°{1:d2}'{2:00.0}", (int)D, (int)M, S);
        }

        public string ToString(string del)
        {
            return string.Format("{0:d2}{3}{1:d2}{3}{2:00.0}", this.D, this.M, this.S, del);
        }

        public string ToString(string del1, string del2)
        {
            return string.Format("{0:d2}{3}{1:d2}{4}{2:00.0}", this.D, this.M, this.S, del1, del2);
        }
    }
}