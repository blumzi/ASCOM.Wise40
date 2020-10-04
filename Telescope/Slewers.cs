using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Collections.Concurrent;

namespace ASCOM.Wise40
{
    public class Slewers
    {
        private readonly Debugger debugger = Debugger.Instance;
        public enum Type { Dome, Ra, Dec, Ha };
        private static volatile Slewers _instance;
        private readonly static object syncObject = new object();
        private readonly static ConcurrentDictionary<Type, WiseTele.SlewerTask> _active = new ConcurrentDictionary<Type, WiseTele.SlewerTask>();
        private readonly static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public Slewers() { }
        static Slewers() { }

        public static Slewers Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new Slewers();
                    }
                }
                return _instance;
            }
        }

        public void Add(WiseTele.SlewerTask slewer)
        {
            string before = _active.ToCSV();
            if (!_active.TryAdd(slewer.type, slewer))
            {
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActiveSlewers: {slewer.type} already active [{before}]");
                #endregion
            }

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActiveSlewers: added {slewer}, [{before}] => [{_active.ToCSV()}]");
            #endregion
        }

        public bool Delete(Slewers.Type type)
        {
            string before = _active.ToCSV();
            if (!_active.TryRemove(type, out _))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"ActiveSlewers: ignored removal of {type}, [{before}]");
                #endregion
                return false;
            }

            if (_active.Count == 0)
            {
                if (WiseTele.endOfAsyncSlewEvent != null)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Slewers.Delete: generating endOfAsyncSlewEvent");
                    #endregion
                    WiseTele.endOfAsyncSlewEvent.Set();
                }

                activityMonitor.EndActivity(ActivityMonitor.ActivityType.TelescopeSlew, new Activity.TelescopeSlew.EndParams()
                {
                    endState = Activity.State.Succeeded,
                    endReason = "Reached target",
                    end = new Activity.TelescopeSlew.Coords
                    {
                        ra = WiseTele.Instance.RightAscension,
                        dec = WiseTele.Instance.Declination,
                    }
                });
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"ActiveSlewers: removed {type}, [{before}] => [{_active.ToCSV()}]");
            #endregion
            return true;
        }

        public int Count
        {
            get
            {
                int count = _active.Count;

                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActiveSlewers: Count {count} [{ToString()}]");
                #endregion
                return count;
            }
        }

        public override string ToString()
        {
            return _active.ToCSV();
        }

        public bool Active(WiseTele.SlewerTask slewer)
        {
            return _active.ContainsKey(slewer.type);
        }

        public bool Active(Slewers.Type type)
        {
            return _active.ContainsKey(type);
        }

        public static void Clear()
        {
            _active.Clear();
        }
    }
}
