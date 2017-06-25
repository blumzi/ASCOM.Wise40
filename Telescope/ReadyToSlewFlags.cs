using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Threading;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40.Telescope
{
    public class ReadyToSlewFlags
    {
        private Debugger debugger = Debugger.Instance;
        Object _lock = new object();

        private long readyForSlew = 0;
        private long readyForSet = 0;
        private long readyForGuide = 0;

        private static volatile ReadyToSlewFlags _instance; // Singleton

        private static object syncObject = new object();
        private static bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static ReadyToSlewFlags()
        {
        }

        public ReadyToSlewFlags()
        {
        }

        public static ReadyToSlewFlags Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new ReadyToSlewFlags();
                    }
                }

                if (! _initialized)
                {
                    _instance.Reset();
                    _initialized = true;
                }
                return _instance;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                readyForSlew = 0;
                readyForSet = 0;
                readyForGuide = 0;
            }
        }

        public void AxisIsReady(TelescopeAxes axis, double rate)
        {
            long newValue = -1;

            lock (_lock)
            {
                if (rate == Const.rateSlew)
                    newValue = Interlocked.Increment(ref readyForSlew);
                else if (rate == Const.rateSet)
                    newValue = Interlocked.Increment(ref readyForSet);
                else if (rate == Const.rateGuide)
                    newValue = Interlocked.Increment(ref readyForGuide);
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisIsReady: {0} at {1} (newValue: {2})",
                axis.ToString(), WiseTele.RateName(rate), newValue);
            #endregion

            if (newValue > 2)
            {
                throw new InvalidValueException(
                    string.Format("AxisIsReady: axis: {0}, {1}, newValue: {2}",
                    axis.ToString(), WiseTele.RateName(rate), newValue));
            }
        }

        public int Get(double rate)
        {
            long ret = 0;

            lock (_lock)
            {
                if (rate == Const.rateSlew)
                    ret = Interlocked.Read(ref readyForSlew);
                else if (rate == Const.rateSet)
                    ret = Interlocked.Read(ref readyForSet);
                else if (rate == Const.rateGuide)
                    ret = Interlocked.Read(ref readyForGuide);
            }

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
