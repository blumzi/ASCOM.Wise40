using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Threading;

namespace ASCOM.Wise40
{
    public class ReadyToSlewFlags
    {
        private long readyForSlew = 0;
        private long readyForSet = 0;
        private long readyForGuide = 0;

        public ReadyToSlewFlags() { }

        public void Reset()
        {
            readyForSlew = 0;
            readyForSet = 0;
            readyForGuide = 0;
        }

        public void Increment(double rate)
        {
            if (rate == Const.rateSlew)
                Interlocked.Increment(ref readyForSlew);
            else if (rate == Const.rateSet)
                Interlocked.Increment(ref readyForSet);
            else if (rate == Const.rateGuide)
                Interlocked.Increment(ref readyForGuide);
        }

        public int Get(double rate)
        {
            long ret = 0;

            if (rate == Const.rateSlew)
                ret = Interlocked.Read(ref readyForSlew);
            else if (rate == Const.rateSet)
                ret = Interlocked.Read(ref readyForSet);
            else if (rate == Const.rateGuide)
                ret = Interlocked.Read(ref readyForGuide);

            return (int)ret;
        }

        public bool Ready(double rate)
        {
            return Get(rate) == 2;
        }
    };
}
