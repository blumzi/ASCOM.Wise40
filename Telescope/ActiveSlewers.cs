using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class ActiveSlewers
    {
        private WiseTele wisetele;
        public enum SlewerType { SlewerNone, SlewerDome, SlewerRa, SlewerDec, SlewerFocus };
        private Object _lock = new object();
        private List<SlewerType> _active;

        public ActiveSlewers()
        {
            wisetele = WiseTele.Instance;
            _active = new List<SlewerType>();
        }

        public void Add(SlewerType slewer)
        {
            string before;

            lock (_lock)
            {
                before = ToString();
                _active.Add(slewer);
            }
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes,
                "ActiveSlewers: added {0}, \"{1}\" => \"{2}\"", slewer.ToString(), before, this.ToString());
            #endregion
        }

        public void Delete(SlewerType slewer, ref bool makeFalseIfEmpty)
        {
            string before;

            lock (_lock)
            {
                before = ToString();
                _active.Remove(slewer);
            }
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes,
                "ActiveSlewers: deleted {0}, \"{1}\" => \"{2}\"", slewer.ToString(), before, this.ToString());
            #endregion
            if (_active.Count() == 0)
            {
                #region debug
                wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, "ActiveSlewers: turning ref to false");
                #endregion
                makeFalseIfEmpty = false;
            }
        }

        public override string ToString()
        {
            List<string> s = new List<string>();
            lock (_lock)
            {
                foreach (var a in _active)
                    s.Add(a.ToString().Substring(6));
            }

            return string.Join(",", s.ToArray());
        }

        public bool Active(SlewerType slewer)
        {
            bool ret = false;

            lock (_lock)
                ret = _active.Contains(slewer);
            return ret;
        }

        public void Clear()
        {
            lock(_lock)
                _active.Clear();
        }
    }
}
