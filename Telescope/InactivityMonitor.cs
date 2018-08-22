using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;
using System.IO;

namespace ASCOM.Wise40
{
    public class InactivityMonitor : WiseObject
    {
        private Timer inactivityTimer;
        private int realMillisToInactivity = 15 * 60 * 1000;        // 15 minutes
        private int simulatedlMillisToInactivity = 3 * 60 * 1000;   //  3 minutes
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
            ShuttingDown = (1 << 7),
        };
        private Activity _activities = Activity.None;

        public void BecomeIdle(object StateObject)
        {
            EndActivity(Activity.GoingIdle);
        }

        public InactivityMonitor()
        {
            wisesite.init();
            inactivityTimer = new System.Threading.Timer(BecomeIdle);
            _activities = Activity.None;
            Start("init");
        }

        public void StartActivity(Activity act)
        {
            if (_shuttingDown)
                return;

            _activities |= act;
            if (act != Activity.GoingIdle)      // Any activity ends GoingIdle
                EndActivity(Activity.GoingIdle);
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, "InactivityMonitor:StartActivity: {0}", act.ToString());
            #endregion
            Stop();
        }

        public void EndActivity(Activity act)
        {
            if (_shuttingDown)
                return;

            _activities &= ~act;
            #region debug
            wisetele.debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, "InactivityMonitor:EndActivity: {0}", act.ToString());
            #endregion
            if (_activities == Activity.None)
                Start("No activities");
        }

        public bool Active(Activity a)
        {
            return (_activities & a) != 0;
        }

        public void Stop()
        {
            inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _due = DateTime.MinValue;
        }

        public void Start(string reason)
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
            inactivityTimer.Change(dueMillis, -1);
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ObservatoryIsActive: {0}", _activities.ToString());
            #endregion
            return _activities != Activity.None;
        }

        public string ObservatoryActivities
        {
            get
            {
                if (_activities == Activity.None)
                    return string.Empty;

                List<string> ret = new List<string>();
                if (Active(Activity.Tracking))
                    ret.Add("Tracking");
                if (Active(Activity.Slewing))
                    ret.Add("Slewing");
                if (Active(Activity.Pulsing))
                    ret.Add("Pulsing");
                if (Active(Activity.Parking))
                    ret.Add("Parking");
                if (Active(Activity.Handpad))
                    ret.Add("Handpad");
                if (Active(Activity.GoingIdle))
                    ret.Add("GoingIdle");

                return string.Join(", ", ret);
            }
        }
    }
}
