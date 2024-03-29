﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;
using System.IO;

namespace ASCOM.Wise40
{
    public class ActivityMonitor : WiseObject
    {
        private Timer inactivityTimer;
        private readonly int realMillisToInactivity = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;
        private readonly int simulatedlMillisToInactivity = (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
        private WiseTele wisetele = WiseTele.Instance;
        private WiseDome wisedome = WiseDome.Instance;
        private Debugger debugger = WiseTele.Instance.debugger;
        private bool _shuttingDown = false;
        private DateTime _due = DateTime.MinValue;                  // not set
        private WiseSite wisesite = WiseSite.Instance;

        [FlagsAttribute]
        public enum Activity
        {
            None = 0,
            Tracking = (1 << 0),
            Slewing = (1 << 1),
            Pulsing = (1 << 2),
            Dome = (1 << 3),
            Handpad = (1 << 4),
            GoingIdle = (1 << 5),
            Parking = (1 << 6),
            Shutter = (1 << 7),
            ShuttingDown = (1 << 8),

            MaxActivity = ShuttingDown << 1,
        };
        private Activity _currentlyActive = Activity.None;
        private List<Activity> _activities = new List<Activity> {
            Activity.Tracking,
            Activity.Slewing,
            Activity.Pulsing,
            Activity.Dome,
            Activity.Handpad,
            Activity.GoingIdle,
            Activity.Parking,
            Activity.Shutter,
            Activity.ShuttingDown,
        };

        public void BecomeIdle(object StateObject)
        {
            EndActivity(Activity.GoingIdle);
        }

        public ActivityMonitor()
        {
            wisesite.init();
            inactivityTimer = new System.Threading.Timer(BecomeIdle);
            _currentlyActive = Activity.None;
            StartTimer("init");
        }

        public void StartActivity(Activity act)
        {
            if (_shuttingDown)
                return;

            _currentlyActive |= act;
            if (act != Activity.GoingIdle)      // Any activity ends GoingIdle
                EndActivity(Activity.GoingIdle);
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, "InactivityMonitor:StartActivity: {0}", act.ToString());
            #endregion
            StopTimer();
        }

        public void EndActivity(Activity act)
        {
            if (_shuttingDown)
                return;

            _currentlyActive &= ~act;
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, "InactivityMonitor:EndActivity: {0}", act.ToString());
            #endregion
        }

        public bool Active(Activity a)
        {
            return (_currentlyActive & a) != 0;
        }

        public void StopTimer()
        {
            inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _due = DateTime.MinValue;
        }

        public void StartTimer(string reason)
        {
            if (_shuttingDown)
                return;

            // The file's creation time is used in case we crashed after starting to idle.
            string filename = Const.topWise40Directory + "Observatory/InactivityMonitorRestart";
            int dueMillis = Simulated ? simulatedlMillisToInactivity : realMillisToInactivity;

            if (reason == "init")
            {
                if (File.Exists(filename))
                {
                    double fileAgeMillis = DateTime.Now.Subtract(File.GetCreationTime(filename)).TotalMilliseconds;

                    if (fileAgeMillis < dueMillis)
                        dueMillis = Math.Min(dueMillis, (int)fileAgeMillis);
                    File.Delete(filename);
                }
                else
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, "InactivityMonitor:Restarted inactivity timer (reason = {0}, due = {1}).",
                reason, dueMillis);
            #endregion

            StartActivity(Activity.GoingIdle);
            inactivityTimer.Change(dueMillis, Timeout.Infinite);
            File.Create(filename).Close();

            DateTime now = DateTime.Now;
            _due = now.AddMilliseconds(dueMillis);
        }

        public TimeSpan RemainingTime
        {
            get
            {
                if (_due == DateTime.MinValue)
                    return TimeSpan.MaxValue;
                return _due.Subtract(DateTime.Now);
            }
        }

        public bool ObservatoryIsActive()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ObservatoryIsActive: {0}", _currentlyActive.ToString());
            #endregion
            return _currentlyActive != Activity.None;
        }

        public string ObservatoryActivities
        {
            get
            {
                List<string> ret = new List<string>();

                foreach (Activity a in _activities)
                    if (Active(a))
                        ret.Add(a.ToString());

                return string.Join(", ", ret);
            }
        }
    }
}
