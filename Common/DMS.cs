namespace ASCOM.Wise40.Common
{
    using System.Threading;

    public class DMS
    {
        internal static string decimalSeparator = Thread.CurrentThread.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator;
        public int D { get; set; }
        public int M { get; set; }
        public decimal S { get; set; }
        public int Sign { get; set; }
        public bool isRA { get; set; }

        public static DMS FromDeg(double deg)
        {
            return new DMS(deg);
        }

        public decimal Deg
        {
            set
            {
                if (value < 0)
                {
                    this.Sign = -1;
                    value = value * -1;
                }
                else {
                    this.Sign = 1;
                }
                this.D = (int)value;
                value = (value - this.D) * 60;
                this.M = (int)value;
                this.S = (value - this.M) * 60;
            }
            get
            {
                return (this.D + (decimal)this.M / 60 + this.S / 3600) * this.Sign;
            }
        }

        public DMS(decimal deg, bool IsRA = false)
        {
            this.Deg = deg;
            this.isRA = IsRA;
        }

        public DMS(double deg, bool IsRA = false)
        {
            this.Deg = (decimal)deg;
            this.isRA = IsRA;
        }

        public DMS(int d, int m, decimal s, int sign = 0)
        {
            this.D = d;
            this.M = m;
            this.S = s;
            this.Sign = sign;
        }

        public static bool TryParse(string coordinates, out DMS value)
        {
            value = new DMS(-1000m);
            decimal val;

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
                    var dig = c.Length > 3 ? decimal.Parse("0" + decimalSeparator + c[3]) : 0;
                    value.S = int.Parse(c[2]) + dig;

                    return true;
                }
            }
            catch
            {
                return false;
            }
            if (decimal.TryParse(coordinates, out val))
            {
                value.Deg = val;
                return true;
            }
            return false;
        }

        override public string ToString()
        {
            return string.Format("{0:d2}°{1:d2}'{2:00.0}\"", this.D, this.M, this.S);
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
    public static class MyDMSExtension
    {
        public static DMS ToDMS(this double value, bool IsRA = false)
        {
            return new DMS(value, IsRA);
        }

        public static DMS ToDMS(this decimal value, bool IsRA = false)
        {
            return new DMS(value, IsRA);
        }
    }
}