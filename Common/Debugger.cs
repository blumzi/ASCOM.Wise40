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
        public enum DebugLevel
        {
            DebugEncoders = (1 << 0),
            DebugAxes = (1 << 1),
            DebugMotors = (1 << 2),
            DebugExceptions = (1 << 3),
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
            if ((_level & (uint)level) != 0)
            {
                DateTime now = DateTime.Now;

                Console.WriteLine(string.Format("[{0}] {1}/{2}/{3} {4}: {5}: {6}",
                    Thread.CurrentThread.ManagedThreadId,
                    now.Day, now.Month, now.Year, now.TimeOfDay,
                    level.ToString(),
                    string.Format(fmt, o)));
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
