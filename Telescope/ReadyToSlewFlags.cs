﻿using System;
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
        private readonly Debugger debugger = Debugger.Instance;
        private readonly Object _lock = new object();

        private long primaryReadyForSlew = 0;
        private long primaryReadyForSet = 0;
        private long primaryReadyForGuide = 0;
        private long secondaryReadyForSlew = 0;
        private long secondaryReadyForSet = 0;
        private long secondaryReadyForGuide = 0;

        private static volatile ReadyToSlewFlags _instance; // Singleton
        private static readonly object syncObject = new object();
        private static bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static ReadyToSlewFlags() { }

        public ReadyToSlewFlags() { }

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
                Interlocked.Exchange(ref primaryReadyForSlew, 0);
                Interlocked.Exchange(ref primaryReadyForSet, 0);
                Interlocked.Exchange(ref primaryReadyForGuide, 0);

                Interlocked.Exchange(ref secondaryReadyForSlew, 0);
                Interlocked.Exchange(ref secondaryReadyForSet, 0);
                Interlocked.Exchange(ref secondaryReadyForGuide, 0);
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

        public bool AxisCanMoveAtRate(TelescopeAxes axis, double rate)
        {
            bool ret = false;
            Slewers.Type otherSlewer = (axis == TelescopeAxes.axisPrimary) ? Slewers.Type.Dec : Slewers.Type.Ra;

            if (! Slewers.Active(otherSlewer))
                return true;        // the other axis has finished its slew, this axis can use any rate

            if (rate == Const.rateSlew)
                ret = (Interlocked.Read(ref primaryReadyForSlew) == Interlocked.Read(ref secondaryReadyForSlew));
            else if (rate == Const.rateSet)
                ret = (Interlocked.Read(ref primaryReadyForSet) == Interlocked.Read(ref secondaryReadyForSet));
            else if (rate == Const.rateGuide)
                ret = (Interlocked.Read(ref primaryReadyForGuide) == Interlocked.Read(ref secondaryReadyForGuide));

            #region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisCanMoveAtRate: {0} at {1} => {2}",
            //    axis, WiseTele.RateName(rate), ret);
            #endregion
            return ret;
        }
    };
}