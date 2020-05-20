using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace ASCOM.Wise40.Common
{
    public class Debugger
    {
        private ListBox listBox;
        private bool _appendToWindow = false;
        private static bool _initialized = false;
        private static string _appName = string.Empty;
        private static string _logFile = string.Empty;
        private static TextWriterTraceListener traceListener;
        private static readonly object _lock = new object();

        static Debugger()
        {
        }

        public Debugger()
        {
        }

        private static readonly Lazy<Debugger> lazy = new Lazy<Debugger>(() => new Debugger()); // Singleton

        public static Debugger Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
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
            DebugSafety = (1 << 7),
            DebugDome = (1 << 8),
            DebugShutter = (1 << 9),
            DebugDAQs = (1 << 10),
            DebugFocuser = (1 << 11),
            DebugFilterWheel = (1 << 12),

            DebugDefault = DebugAxes | DebugExceptions | DebugASCOM | DebugLogic | DebugShutter,

            DebugAll = DebugASCOM|DebugDevice|DebugLogic|DebugExceptions|DebugAxes|DebugMotors|DebugEncoders|DebugSafety|DebugDome|DebugShutter|DebugDAQs|DebugFocuser|DebugFilterWheel,
            DebugNone = 0,
        };

        private static DebugLevel _currentLevel;

        public void init(DebugLevel level = 0)
        {
            if (_initialized)
                return;

            ReadProfile();
            if (level != 0)
                _currentLevel = level;

            _appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            Trace.AutoFlush = true;
            _initialized = true;

            lock (_lock)
            {
                WriteLine(DebugLevel.DebugLogic, $"##");
                WriteLine(DebugLevel.DebugLogic, $"## ============= {_appName} started ====================");
                WriteLine(DebugLevel.DebugLogic, $"##");
            }
        }

        public void SetWindow(ListBox list, bool append = false)
        {
            listBox = list;
            _appendToWindow = append;
        }

        public bool Autoflush
        {
            get
            {
                return System.Diagnostics.Trace.AutoFlush;
            }

            set
            {
                System.Diagnostics.Trace.AutoFlush = value;
            }
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

        public static string LogDirectory()
        {
            DateTime now = DateTime.UtcNow;
            if (now.Hour < 12)
                now = now.AddDays(-1);
            return string.Format(Const.topWise40Directory + "Logs/{0}-{1:D2}-{2:D2}",
                    now.Year, now.Month, now.Day);
        }

        public void WriteLine(DebugLevel level, string fmt, params object[] o)
        {
            if (!_initialized || !Debugging(level))
                return;

            DateTime utcNow = DateTime.UtcNow;
            string msg = string.Format(fmt, o);
            string taskInfo = (Task.CurrentId == null) ?
                "-1" :
                (Task.CurrentId.HasValue ? Task.CurrentId.Value : -1).ToString();

            string line = string.Format("{0} UT {1,-18} {2,-16} {3}",
                utcNow.ToString(@"HH\:mm\:ss\.fff"),
                string.Format("{0},{1},{2}", Process.GetCurrentProcess().Id,
                    Thread.CurrentThread.ManagedThreadId.ToString(),
                    taskInfo),
                level.ToString(),
                msg);
            string currentLogPath = LogDirectory() + string.Format("/{0}.txt", _appName);
            lock (_lock)
            {
                if (currentLogPath != _logFile)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(currentLogPath));
                    if (traceListener != null)
                    {
                        try
                        {
                            traceListener.Flush();
                            traceListener.Close();
                            Trace.Listeners.Remove(traceListener);
                        }
                        catch { }
                    }
                    _logFile = currentLogPath;
                    traceListener = new TextWriterTraceListener(_logFile);
                    Trace.Listeners.Add(traceListener);
                    Trace.WriteLine("##");
                    Trace.WriteLine($"## {DateTime.Now:yyyy-MMM-dd HH:mm:ss.fff}");
                    Trace.WriteLine("##\n");
                }

                Trace.WriteLine(line);
            }

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

        public DebugLevel Level
        {
            get
            {
                return _currentLevel;
            }
        }

        public static void StartDebugging(DebugLevel levels)
        {
            _currentLevel |= levels;
        }

        public static void StopDebugging(DebugLevel levels)
        {
            _currentLevel &= ~levels;
        }

        public void WriteProfile()
        {
            using (ASCOM.Utilities.Profile p = new Utilities.Profile() { DeviceType = "Telescope" })
            {
                if (!p.IsRegistered(Const.WiseDriverID.Telescope))
                    p.Register(Const.WiseDriverID.Telescope, "Wise40 global settings");
                p.WriteValue(Const.WiseDriverID.Telescope, Const.ProfileName.Site_DebugLevel, Level.ToString());
            }
        }

        public static void ReadProfile()
        {
            using (ASCOM.Utilities.Profile p = new Utilities.Profile() { DeviceType = "Telescope" })
            {
                if (p.IsRegistered(Const.WiseDriverID.Telescope))
                {
                    DebugLevel d;
                    
                    if (Enum.TryParse<DebugLevel>(p.GetValue(Const.WiseDriverID.Telescope, Const.ProfileName.Site_DebugLevel, string.Empty, DebugLevel.DebugDefault.ToString()), out d))
                        _currentLevel = d;
                }
            }
        }
    }
}
