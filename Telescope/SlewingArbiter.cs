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
    public class SlewingArbiter
    {
        private Debugger debugger = Debugger.Instance;
        Object _lock = new object();

        private const long StateStopped = 0;
        private const long StateSlew = (1 << 0);
        private const long StateSet = (1 << 1);
        private const long StateGuide = (1 << 2);

        private long primaryState = StateStopped;
        private long secondaryState = StateStopped;
        private Dictionary<double, long> rateToState = new Dictionary<double, long>
        {
            { Const.rateStopped, StateStopped },
            { Const.rateSlew, StateSlew },
            { Const.rateSet, StateSet },
            { Const.rateGuide, StateGuide },
        };

        private static volatile SlewingArbiter _instance; // Singleton

        private static object syncObject = new object();
        private static bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SlewingArbiter()
        {
        }

        public SlewingArbiter()
        {
        }

        public static SlewingArbiter Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new SlewingArbiter();
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
                primaryState = StateStopped;
                secondaryState = StateStopped;
            }
        }

        private Dictionary<long, string> stateName = new Dictionary<long, string>
        {
            { StateStopped, "Stopped" },
            { StateSlew, "Slew" },
            { StateSet, "Set" },
            { StateGuide, "Guide" },
        };

        public bool AxisTryToSetRate(TelescopeAxes axis, double wantedRate)
        {
            long wantedState = rateToState[wantedRate];
            bool ret = false;
            long otherCurrentState;

            lock (_lock)
            {
                if (axis == TelescopeAxes.axisPrimary)
                    otherCurrentState = Interlocked.Read(ref secondaryState);
                else
                    otherCurrentState = Interlocked.Read(ref primaryState);

                switch (wantedState) {
                    case StateStopped:       // An axis can stop anytime
                        ret = true;
                        break;

                    case StateSlew:
                        if (otherCurrentState == StateStopped || otherCurrentState == StateSlew || otherCurrentState == StateGuide)
                            ret = true;
                        break;

                    case StateSet:
                        if (otherCurrentState == StateStopped || otherCurrentState == StateSet || otherCurrentState == StateGuide)
                            ret = true;
                        break;

                    case StateGuide:
                        if (otherCurrentState == StateStopped || otherCurrentState == StateSlew || otherCurrentState == StateSet || otherCurrentState == StateGuide)
                            ret = true;
                        break;
                }

                if (ret == true)
                {
                    if (axis == TelescopeAxes.axisPrimary)
                        Interlocked.Exchange(ref primaryState, wantedState);
                    else
                        Interlocked.Exchange(ref secondaryState, wantedState);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "AxisTryToSetRate: {0} wants {1}, other at: {2} => {3}",
                axis, stateName[wantedState], stateName[otherCurrentState], ret ? "Granted" : "Denied");
            #endregion

            return ret;
        }
    };
}
