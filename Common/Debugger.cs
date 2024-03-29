﻿using System;
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

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        [FlagsAttribute]
        public enum DebugLevel : int
        {
            DebugNone = 0,
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
            DebugWise = (1 << 13),
            DebugActivity = (1 << 14),
            DebugMoon = (1 << 15),
            DebugHTTP = (1 << 16),
            DebugWeather = (1 << 17),
            DebugTele = (1 << 18),

            DebugDefault = DebugAxes | DebugExceptions | DebugASCOM | DebugLogic | DebugShutter |
                DebugWise | DebugActivity | DebugMoon | DebugHTTP | DebugWeather | DebugTele | DebugSafety,

            DebugAll = DebugDefault | 
                DebugDevice | DebugMotors | DebugEncoders | DebugDome | DebugShutter | DebugDAQs | DebugFocuser | DebugFilterWheel,
        };

        private static DebugLevel _currentLevel;

        public void Init(DebugLevel level = 0)
        {
            if (_initialized)
                return;

            if (WiseSite.ObservatoryName == "wise40")
                ReadProfile();
            else
                _currentLevel = DebugLevel.DebugWise;

            if (level != 0)
                _currentLevel = level;

            _appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            Trace.AutoFlush = true;
            _initialized = true;

            lock (_lock)
            {
                WriteLine(DebugLevel.DebugLogic, "##");
                WriteLine(DebugLevel.DebugLogic, $"## ============= {_appName} started ====================");
                WriteLine(DebugLevel.DebugLogic, "##");
            }
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

        /// <summary>
        /// Checks if the specifies level is debugged.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static bool Debugging(DebugLevel l)
        {
            return (_currentLevel & l) != 0;
        }

        public static string LogDirectory()
        {
            DateTime now = DateTime.UtcNow;
            if (now.Hour < 12)
                now = now.AddDays(-1);

            string top = WiseSite.ObservatoryName == "wise40" ?
                Const.topWise40Directory :
                Const.topWiseDirectory;

            return $"{top}/Logs/{now.Year}-{now.Month:D2}-{now.Day:D2}";
        }

        public void WriteLine(DebugLevel level, string fmt, params object[] o)
        {
            if (!_initialized || !Debugging(level))
                return;

            string msg = null;

            DateTime utcNow = DateTime.UtcNow;
            try
            {
                msg = string.Format(fmt, o);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"exception: {ex.Message}\nfmt: \"{fmt.Normalize()}\", o.Length: {o.Length}");
            }
            string taskID = (Task.CurrentId == null) ? "-1" : (Task.CurrentId ?? -1).ToString();

            string line = string.Format("{0} UT {1,-18} {2,-16} {3}",
                utcNow.ToString(@"HH\:mm\:ss\.fff"),
                $"{Process.GetCurrentProcess().Id},{Thread.CurrentThread.ManagedThreadId},{taskID}",
                level.ToString(),
                msg);
            string currentLogFile = $"{LogDirectory()}/{_appName}.txt";

            lock (_lock)
            {
                if (currentLogFile != _logFile)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(currentLogFile));
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
                    _logFile = currentLogFile;
                    traceListener = new TextWriterTraceListener(_logFile);
                    Trace.Listeners.Add(traceListener);
                    Trace.WriteLine("\n##");
                    Trace.WriteLine($"## {DateTime.UtcNow:yyyy-MMM-dd HH:mm:ss.fff}");
                    Trace.WriteLine("##\n");
                }

                Trace.WriteLine(line.Normalize());
            }
        }

        public static DebugLevel Level
        {
            get
            {
                return _currentLevel;
            }
        }

        public static void StartDebugging(DebugLevel levels)
        {
            _currentLevel |= levels;
            #region debug
            Instance.WriteLine(DebugLevel.DebugLogic, $"StopDebugging: current: {_currentLevel}");
            #endregion
        }

        public static void StopDebugging(DebugLevel levels)
        {
            _currentLevel &= ~levels;
            #region debug
            Instance.WriteLine(DebugLevel.DebugLogic, $"StopDebugging: current: {_currentLevel}");
            #endregion
        }

        public static void SetCurrentLevel(DebugLevel levels)
        {
            _currentLevel = levels;
            #region debug
            Instance.WriteLine(DebugLevel.DebugLogic, $"SetCurrentLevel (from: {CodeLocation}): current: {_currentLevel}");
            #endregion
        }

        public static void WriteProfile()
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
                    if (Enum.TryParse<DebugLevel>(p.GetValue(Const.WiseDriverID.Telescope, Const.ProfileName.Site_DebugLevel, string.Empty, DebugLevel.DebugDefault.ToString()), out DebugLevel d))
                        _currentLevel = d;
                }
            }
        }

        public static string CodeLocation
        {
            get
            {
                System.Diagnostics.StackFrame sf = new System.Diagnostics.StackTrace(true).GetFrame(2);
                string fileName = sf.GetFileName();

                fileName = fileName.Remove(0, fileName.IndexOf("ASCOM.Wise40") + "ASCOM.Wise40".Length);
                return $"{sf.GetMethod().Name}@...{fileName}:{sf.GetFileLineNumber()}";
            }
        }
    }
}
