using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Threading;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40
{
    public class ReadyToSlewFlags
    {
        private Debugger debugger = Debugger.Instance;

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

        public void AxisIsReady(TelescopeAxes axis, double rate)
        {
            long newValue = -1;

            if (rate == Const.rateSlew)
                newValue = Interlocked.Increment(ref readyForSlew);
            else if (rate == Const.rateSet)
                newValue = Interlocked.Increment(ref readyForSet);
            else if (rate == Const.rateGuide)
                newValue = Interlocked.Increment(ref readyForGuide);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisIsReady: {0} at {1} (newValue: {2})",
                axis.ToString(), WiseTele.RateName(rate), newValue);
            #endregion
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

        public bool BothAxesAreReady(double rate)
        {
            int nReady = Get(rate);
            bool ret = nReady == 2;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "BothAxesAreReady: at {0} ({1}) => {2}",
                WiseTele.RateName(rate), nReady, ret);
            #endregion
            return ret;
        }
    };
}
