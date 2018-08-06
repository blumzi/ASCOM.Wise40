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
        Object _lock = new object();

        private long primaryReadyForSlew = 0;
        private long primaryReadyForSet = 0;
        private long primaryReadyForGuide = 0;
        private long secondaryReadyForSlew = 0;
        private long secondaryReadyForSet = 0;
        private long secondaryReadyForGuide = 0;

        private long nActiveAxes = 0;

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

                if (!_initialized)
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
                primaryReadyForSlew = 0;
                primaryReadyForSet = 0;
                primaryReadyForGuide = 0;

                secondaryReadyForSlew = 0;
                secondaryReadyForSet = 0;
                secondaryReadyForGuide = 0;

                nActiveAxes = 0;
            }
        }

        public void Activate(TelescopeAxes axis)
        {
            lock(_lock)
            {
                Interlocked.Increment(ref nActiveAxes);
            }
        }

        public void Deactivate(TelescopeAxes axis)
        {
            lock (_lock)
            {
                Interlocked.Decrement(ref nActiveAxes);
            }
        }

        public void AxisBecomesReadyToMoveAtRate(TelescopeAxes axis, double rate)
        {
            long newValue = -1;

            lock (_lock)
            {
                switch (axis)
                {
                    case TelescopeAxes.axisPrimary:
                        if (rate == Const.rateSlew)
                            newValue = Interlocked.Increment(ref primaryReadyForSlew);
                        else if (rate == Const.rateSet)
                            newValue = Interlocked.Increment(ref primaryReadyForSet);
                        else if (rate == Const.rateGuide)
                            newValue = Interlocked.Increment(ref primaryReadyForGuide);
                        break;

                    case TelescopeAxes.axisSecondary:
                        if (rate == Const.rateSlew)
                            newValue = Interlocked.Increment(ref secondaryReadyForSlew);
                        else if (rate == Const.rateSet)
                            newValue = Interlocked.Increment(ref secondaryReadyForSet);
                        else if (rate == Const.rateGuide)
                            newValue = Interlocked.Increment(ref secondaryReadyForGuide);
                        break;
                }
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisBecomesReadyToMoveAtRate: {0} at {1} (newValue: {2})",
                axis.ToString(), WiseTele.RateName(rate), newValue);
            #endregion
        }


        //public void AxisNoLongerReadyToMoveAtRate(TelescopeAxes axis, double rate)
        //{
        //    long newValue = -1;

        //    lock (_lock)
        //    {
        //        if (rate == Const.rateSlew)
        //            newValue = Interlocked.Decrement(ref readyForSlew);
        //        else if (rate == Const.rateSet)
        //            newValue = Interlocked.Decrement(ref readyForSet);
        //        else if (rate == Const.rateGuide)
        //            newValue = Interlocked.Decrement(ref readyForGuide);
        //    }
        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisNoLongerReadyToMoveAtRate: {0} at {1} (newValue: {2})",
        //        axis.ToString(), WiseTele.RateName(rate), newValue);
        //    #endregion

        //    if (newValue < 0)
        //    {
        //        throw new InvalidValueException(
        //            string.Format("AxisNoLongerReadyToMoveAtRate: axis: {0}, {1}, newValue: {2}",
        //            axis.ToString(), WiseTele.RateName(rate), newValue));
        //    }
        //}

        //public int Get(double rate)
        //{
        //    long ret = 0;

        //    lock (_lock)
        //    {
        //        if (rate == Const.rateSlew)
        //            ret = Interlocked.Read(ref readyForSlew);
        //        else if (rate == Const.rateSet)
        //            ret = Interlocked.Read(ref readyForSet);
        //        else if (rate == Const.rateGuide)
        //            ret = Interlocked.Read(ref readyForGuide);
        //    }

        //    return (int)ret;
        //}

        public bool AxisCanMoveAtRate(double rate)
        {
            bool ret = false;

            if (nActiveAxes == 1)
                return true;

            if (rate == Const.rateSlew)
                ret = (primaryReadyForSlew == secondaryReadyForSlew);
            else if (rate == Const.rateSet)
                ret = (primaryReadyForSet == secondaryReadyForSet);
            else if (rate == Const.rateGuide)
                ret = (primaryReadyForGuide == secondaryReadyForGuide);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisCanMoveAtRate: at {0} => {1}",
                WiseTele.RateName(rate), ret);
            #endregion
            return ret;
        }
    };
}