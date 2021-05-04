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
        private readonly int _period;
        private bool _enabled;
        public enum ActionWhenNotSafe {  None, Stop, Backoff };

        public ActionWhenNotSafe WhenNotSafe { get; set; } = ActionWhenNotSafe.None;

        private void SafetyChecker(object StateObject)
        {
            if (!Enabled || WhenNotSafe == ActionWhenNotSafe.None)
                return;

            string reason = wisetele.SafeAtCoordinates(
                Angle.RaFromHours(wisetele.RightAscension),
                Angle.DecFromDegrees(wisetele.Declination));

            if (string.IsNullOrEmpty(reason))
                return;

            string op = $"SafetyChecker: reason: {reason}";
            if (!Hardware.Hardware.ComputerHasControl)
            {
                #region debug
                WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: Skipped: No computer control!");
                #endregion
                return;
            }

            #region debug
            WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: activated (action: {WhenNotSafe})");
            #endregion

            wisetele.RecoveringSafety = true;

            if (WhenNotSafe == ActionWhenNotSafe.Stop || WhenNotSafe == ActionWhenNotSafe.Backoff)
            {
                if (wisetele.Slewing)
                    wisetele.AbortSlew(op);

                if (wisetele.IsPulseGuiding)
                    wisetele.AbortPulseGuiding(op);

                if (wisetele.Tracking)
                    wisetele.Tracking = false;
            }

            if (WhenNotSafe == ActionWhenNotSafe.Backoff)
                wisetele.Backoff(op);

            wisetele.RecoveringSafety = false;

            if (Enabled)
                _timer.Change(_period, Timeout.Infinite);
        }

        public SafetyMonitorTimer(int periodMillis = 1000)
        {
            _timer = new Timer(new TimerCallback(SafetyChecker));
            this._period = periodMillis;
            Enabled = false;
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
                    _timer.Change(_period, Timeout.Infinite);
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
