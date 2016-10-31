using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public class Slewers
    {
        private Debugger debugger = Debugger.Instance;
        public enum Type { Dome, Ra, Dec, Focuser };
        private Object _lock = new object();
        private static readonly Slewers _instance = new Slewers();
        private static List<WiseTele.SlewerTask> _active = new List<WiseTele.SlewerTask>();

        public Slewers() { }

        static Slewers() { }

        public static Slewers Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Add(WiseTele.SlewerTask slewer)
        {
            string before;

            lock (_lock)
            {
                before = ToString();
                _active.Add(slewer);
            }
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes,
                "ActiveSlewers: added {0}, \"{1}\" => \"{2}\"", slewer.ToString(), before, this.ToString());
            #endregion
        }

        public void Delete(Slewers.Type type)
        {
            string before;
            WiseTele.SlewerTask slewerTask;

            lock (_lock)
            {
                before = ToString();

                slewerTask = _active.Find((s) => s.type == type);
                _active.Remove(slewerTask);
            }
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes,
                "ActiveSlewers: deleted {0}, \"{1}\" => \"{2}\"", type.ToString(), before, this.ToString());
            #endregion
        }

        public int Count
        {
            get {
                return _active.Count;
            }
        }

        public override string ToString()
        {
            List<string> s = new List<string>();
            lock (_lock)
            {
                foreach (var a in _active)
                    s.Add(a.type.ToString());
            }

            return string.Join(",", s.ToArray());
        }

        public bool Active(WiseTele.SlewerTask slewer)
        {
            bool ret = false;

            lock (_lock)
                foreach (var a in _active)
                {
                    if (a.type == slewer.type)
                    {
                        ret = true;
                        break;
                    }
                }
            return ret;
        }

        public bool Active(Slewers.Type type)
        {
            bool ret = false;

            lock (_lock)
                foreach (var a in _active)
                {
                    if (a.type == type)
                    {
                        ret = true;
                        break;
                    }
                }
            return ret;
        }

        public void Clear()
        {
            lock(_lock)
                _active.Clear();
        }

        public Task[] ToArray()
        {
            List<Task> tasks = new List<Task>();

            foreach (var a in _active)
                tasks.Add(a.task);
            return tasks.ToArray();
        }
    }
}
