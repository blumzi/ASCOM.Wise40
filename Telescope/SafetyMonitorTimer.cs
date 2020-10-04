using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40 //.Telescope
{
    /// <summary>
    /// A timer that should be on whenever any of the directional or tracking motors
    ///  are on.  The callback checks if the telescope is safe at the current coordinates.
    /// </summary>
    public class SafetyMonitorTimer
    {
        private static readonly WiseTele wisetele = WiseTele.Instance;
        private readonly Timer _timer;
        private readonly int _dueTime, _period;
        private bool _enabled;
        public enum ActionWhenNotSafe {  None, StopMotors, Backoff };

        public ActionWhenNotSafe WhenNotSafe { get; set; } = ActionWhenNotSafe.None;

        private void SafetyChecker(object StateObject)
        {
            wisetele.safetyMonitorTimer.Enabled = false;

            string reason = wisetele.SafeAtCoordinates(
                Angle.RaFromHours(wisetele.RightAscension),
                Angle.DecFromDegrees(wisetele.Declination));

            if (string.IsNullOrEmpty(reason))
            {
                wisetele.safetyMonitorTimer.Enabled = true;
                return;
            }

            #region debug
            WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"SafetyChecker: activated (action: {WhenNotSafe}, reason: {reason})");
            #endregion

            switch (WhenNotSafe)
            {
                case ActionWhenNotSafe.None:
                    return;
                case ActionWhenNotSafe.StopMotors:
                    wisetele.Stop($"SafetyChecker: ({reason})");
                    break;
                case ActionWhenNotSafe.Backoff:
                    wisetele.Backoff(reason);
                    break;
            }
            wisetele.safetyMonitorTimer.Enabled = true;
        }

        public SafetyMonitorTimer(int dueTime = 1000, int period = 1000)
        {
            _timer = new Timer(new TimerCallback(SafetyChecker));
            this._dueTime = dueTime;
            this._period = period;
            _enabled = false;
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
                if (_enabled && !WiseTele.BypassCoordinatesSafety)
                    _timer.Change(_dueTime, _period);
                else
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void EnableIfNeeded(ActionWhenNotSafe action)
        {
            if ((wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.IsOn) && !Enabled)
            {
                WhenNotSafe = action;
                Enabled = true;
            }
        }

        public void DisableIfNotNeeded()
        {
            if (Enabled && !(wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.IsOn))
            {
                Enabled = false;
                WhenNotSafe = ActionWhenNotSafe.None;
            }
        }
    }
}
