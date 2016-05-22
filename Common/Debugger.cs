using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ASCOM.Wise40.Common
{
    public class Debugger
    {
        private uint _level;

        public Debugger(uint level = 0)
        {
            _level = level;
        }

        public bool Debugging(DebugLevel level)
        {
            return (_level & (uint)level) != 0;
        }

        public void WriteLine(DebugLevel level, string fmt, params object[] o)
        {
            if ((_level & (uint)level) != 0)
            {
                DateTime now = DateTime.Now;

                Console.WriteLine(string.Format("{0}: [{1}] {2}/{3}/{4} {5}: {6}", level.ToString(), Thread.CurrentThread.ManagedThreadId,
                    now.Day, now.Month, now.Year, now.TimeOfDay, String.Format(fmt, o)));
            }
        }

        public uint Level
        {
            get
            {
                return _level;
            }

            set
            {
                _level = value;
            }
        }

        public enum DebugLevel
        {
            DebugEncoders = (1 << 0),
            DebugAxes = (1 << 1),
            DebugMotors = (1 << 2),
            DebugExceptions = (1 << 3),
        };
    }
}
