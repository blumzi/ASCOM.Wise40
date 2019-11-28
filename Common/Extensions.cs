using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public static class Extensions
    {
        public static string ToMinimalString(this TimeSpan ts)
        {
            string ret = null;
            string s;

            if (ts.Days != 0)
                ret += $"{ts.Days}d";

            s = $@"{ts.Hours:d2}h";
            if (ts.Hours == 0)
            {
                if (ret != null)
                    ret += s;
            }
            else
                ret += s;

            s = $@"{ts.Minutes:d2}m";
            if (ts.Minutes == 0)
            {
                if (ret != null)
                    ret += s;
            }
            else
                ret += s;

            if (ts.Seconds == 0 && ts.Milliseconds != 0)
            {
                if (ret == null)
                    ret = $@"0.{ts.Milliseconds:d3}s";
                else
                    ret += $@"00.{ts.Milliseconds:d3}s";
            }
            else if (ts.Seconds != 0 && ts.Milliseconds == 0)
            {
                if (ret == null)
                    ret = $@"{ts.Seconds}s";
                else
                    ret += $@"{ts.Seconds:d2}s";
            }
            else if (ts.Seconds != 0 && ts.Milliseconds != 0)
            {
                if (ret == null)
                    ret = $@"{ts.Seconds}.{ts.Milliseconds:d3}s";
                else
                    ret += $@"{ts.Seconds:d2}.{ts.Milliseconds:d3}s";
            }

            return ret.StartsWith("0.") ? ret : ret.TrimStart(new char[] { '0' });
        }

        public static string ToCSV(this List<string> list)
        {
            return string.Join(",", list);
        }

        public static string ToCSV<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict)
        {
            return string.Join(",", dict.Keys);
        }

        public static string ToMySqlDateTime(this DateTime dateTimeUTC)
        {
            return dateTimeUTC.ToString(@"yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
