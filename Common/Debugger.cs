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
        private static readonly Debugger instance = new Debugger(); // Singleton
        private static bool _initialized = false;

        static Debugger()
        {
        }

        public Debugger()
        {
        }

        public static Debugger Instance
        {
            get
            {
                return instance;
            }
        }

        public enum DebugLevel
        {
            DebugASCOM = (1 << 0),
            DebugDevice = (1 << 1),
            DebugLogic = (1 << 2),
            DebugExceptions = (1 << 3),
            DebugAxes = (1 << 4),
            DebugMotors = (1 << 5),
            DebugEncoders = (1 << 6),

            DebugAll = DebugEncoders | DebugAxes | DebugMotors | DebugExceptions | DebugDevice | DebugASCOM | DebugLogic,
        };

        public static string[] indents = new string[(int)DebugLevel.DebugEncoders + 1];

        private static uint _level;

        public void init(uint level = 0)
        {
            if (_initialized)
                return;

            if (level != 0)
                _level = level;

            indents[(int)DebugLevel.DebugASCOM] = "      ";
            indents[(int)DebugLevel.DebugDevice] = ">     ";
            indents[(int)DebugLevel.DebugLogic] = ">>    ";
            indents[(int)DebugLevel.DebugExceptions] = ">>>   ";
            indents[(int)DebugLevel.DebugAxes] = ">>>>  ";
            indents[(int)DebugLevel.DebugMotors] = ">>>>> ";
            indents[(int)DebugLevel.DebugEncoders] = ">>>>>>";

            _initialized = true;
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

               System.Diagnostics.Debug.WriteLine(string.Format("{0,4} {1:4} {2}/{3}/{4} {5} {6,-25} {7}",
                    Thread.CurrentThread.ManagedThreadId.ToString(),
                    (Task.CurrentId.HasValue ? Task.CurrentId.Value : -1).ToString(),
                    now.Day, now.Month, now.Year, now.TimeOfDay,
                    indents[(int)level] + " " + level.ToString() + ":",
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
