using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace ASCOM.Wise40.Common
{
    public class Debugger
    {
        public enum DebugLevel
        {
            DebugEncoders = (1 << 0),
            DebugAxes = (1 << 1),
            DebugMotors = (1 << 2),
            DebugExceptions = (1 << 3),
            DebugDevice = (1 << 4),
            DebugASCOM = (1 << 5),
            DebugLogic = (1 << 6),
        };

        private uint _level;

        public Debugger(uint level = 0) // may want to add optional file-name
        {
            _level = level;
        }

        /// <summary>
        /// Checks if the specifies level is debugged.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public bool Debugging(DebugLevel l)
        {
            return (_level & (uint)l) != 0;
        }

        public void WriteLine(DebugLevel level, string fmt, params object[] o)
        {
            if (Debugging(level))
            {
                DateTime now = DateTime.Now;
                string msg = string.Format(fmt, o);

               System.Diagnostics.Debug.WriteLine(string.Format("[{0}] {1}/{2}/{3} {4}: {5}: {6}",
                    Thread.CurrentThread.ManagedThreadId,
                    now.Day, now.Month, now.Year, now.TimeOfDay,
                    level.ToString(),
                    msg));
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
    }
}
