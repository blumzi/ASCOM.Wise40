using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace ASCOM.Wise40.Common
{
    public class Debugger
    {
        private static readonly Debugger instance = new Debugger(); // Singleton
        private ListBox listBox;
        private bool _appendToWindow = false;
        private static bool _initialized = false;
        private static bool _tracing = false;

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

        [FlagsAttribute]
        public enum DebugLevel : int
        {
            DebugASCOM = (1 << 0),
            DebugDevice = (1 << 1),
            DebugLogic = (1 << 2),
            DebugExceptions = (1 << 3),
            DebugAxes = (1 << 4),
            DebugMotors = (1 << 5),
            DebugEncoders = (1 << 6),

            DebugDefault = DebugAxes | DebugMotors | DebugExceptions | DebugASCOM | DebugLogic,

            DebugAll = DebugEncoders | DebugAxes | DebugMotors | DebugExceptions | DebugDevice | DebugASCOM | DebugLogic,
            DebugNone = 0,
        };

        public static string[] indents = new string[(int)DebugLevel.DebugEncoders + 1];

        private static DebugLevel _currentLevel;

        public void init(DebugLevel level = 0)
        {
            if (_initialized)
                return;

            ReadProfile();
            if (level != 0)
                _currentLevel = level;

            indents[(int)DebugLevel.DebugASCOM] = "      ";
            indents[(int)DebugLevel.DebugDevice] = ">     ";
            indents[(int)DebugLevel.DebugLogic] = ">>    ";
            indents[(int)DebugLevel.DebugExceptions] = ">>>   ";
            indents[(int)DebugLevel.DebugAxes] = ">>>>  ";
            indents[(int)DebugLevel.DebugMotors] = ">>>>> ";
            indents[(int)DebugLevel.DebugEncoders] = ">>>>>>";

            _initialized = true;
        }

        public void SetWindow(ListBox list, bool append = false)
        {
            listBox = list;
            _appendToWindow = append;
        }

        public void AppendToWindow(bool append)
        {
            _appendToWindow = append;
        }

        /// <summary>
        /// Checks if the specifies level is debugged.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public bool Debugging(DebugLevel l)
        {
            return (_currentLevel & l) != 0;
        }

        public void WriteLine(DebugLevel level, string fmt, params object[] o)
        {
            if (Debugging(level))
            {
                DateTime now = DateTime.Now;
                string msg = string.Format(fmt, o);
                string line = string.Format("{0,4} {1,4} {2}/{3}/{4} {5} {6,-25} {7}",
                    Thread.CurrentThread.ManagedThreadId.ToString(),
                    (Task.CurrentId.HasValue ? Task.CurrentId.Value : -1).ToString(),
                    now.Day, now.Month, now.Year, now.TimeOfDay,
                    indents[(int)level] + " " + level.ToString() + ":",
                    msg);

               System.Diagnostics.Debug.WriteLine(line);
                if (listBox != null && _appendToWindow)
                {
                    if (listBox.InvokeRequired)
                    {
                        listBox.Invoke(new Action(() =>
                        {
                            listBox.Items.Add(line);
                            listBox.Update();
                        }));
                    }
                    else
                    {
                        listBox.Items.Add(line);
                        listBox.Update();
                    }
                }
            }
        }

        public DebugLevel Level
        {
            get
            {
                return _currentLevel;
            }
        }

        public void StartDebugging(DebugLevel levels)
        {
            _currentLevel |= levels;
        }

        public void StopDebugging(DebugLevel levels)
        {
            _currentLevel &= ~levels;
        }

        internal static string driverID = "ASCOM.Wise40.Telescope";
        internal static string deviceType = "Telescope";

        public void WriteProfile()
        {
            using (ASCOM.Utilities.Profile p = new Utilities.Profile())
            {
                if (!p.IsRegistered(driverID))
                    p.Register(driverID, "Wise40 global settings");
                p.DeviceType = deviceType;
                p.WriteValue(driverID, "DebugLevel", Level.ToString());
                p.WriteValue(driverID, "Tracing", _tracing.ToString());
            }
        }

        public void ReadProfile()
        {
            using (ASCOM.Utilities.Profile p = new Utilities.Profile())
            {
                p.DeviceType = deviceType;
                if (p.IsRegistered(driverID))
                {
                    Debugger.DebugLevel defaultDebugLevel = Debugger.DebugLevel.DebugAxes | Debugger.DebugLevel.DebugMotors | Debugger.DebugLevel.DebugExceptions | Debugger.DebugLevel.DebugASCOM | Debugger.DebugLevel.DebugLogic;

                    //_currentLevel = (DebugLevel) Enum.Parse(typeof(DebugLevel), p.GetValue(driverID, "DebugLevel", string.Empty));
                    _currentLevel = DebugLevel.DebugDefault;
                    _tracing = Convert.ToBoolean(p.GetValue(driverID, "Tracing", string.Empty, "false"));
                }
            }
        }

        public bool Tracing
        {
            get
            {
                return _tracing;
            }

            set
            {
                _tracing = value;
            }
        }
    }
}
