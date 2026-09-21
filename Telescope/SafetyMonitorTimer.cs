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

        //
        // Re-entrancy guard.  The callback can block for seconds - Backoff sleeps 3000ms per
        //  axis - so without this a check could start on top of a recovery still in progress.
        //
        private int _inCallback;

        //
        // Consecutive checks that found the telescope unsafe.  Used only to throttle the log:
        //  a condition we cannot recover from would otherwise write a line every second for
        //  as long as it lasts, and a night's log is already measured in gigabytes.
        //
        private int _consecutiveUnsafe;

        //
        // Consecutive passes that found nothing moving and nothing unsafe.  The monitor keeps
        //  re-arming through a few of these so that arming need not coincide with the motors
        //  starting - see the re-arm block in SafetyChecker.
        //
        private int _idlePasses;
        private const int idlePassesBeforeGivingUp = 3;

        //
        // Ordered weakest to strongest: EnableIfNeeded compares them, so a later, weaker request
        //  cannot displace a stronger one already in force.
        //
        // Stop is currently NEVER the effective action.  Its only caller is HandpadMoveAxis, and
        //  by the time that asks, InternalMoveAxis has already asked for Backoff - see the
        //  comment at that call site.  Do not read a Stop request as a promise of stopping.
        //
        public enum ActionWhenNotSafe {  None, Stop, Backoff };

        public ActionWhenNotSafe WhenNotSafe { get; set; } = ActionWhenNotSafe.None;

        private void SafetyChecker(object StateObject)
        {
            if (Interlocked.CompareExchange(ref _inCallback, 1, 0) != 0)
                return;      // a previous check is still running; it will re-arm us

            bool armedAtEntry = _enabled;
            ActionWhenNotSafe actionAtEntry = WhenNotSafe;
            bool wasUnsafe = false;

            try
            {
                if (!armedAtEntry || actionAtEntry == ActionWhenNotSafe.None)
                    return;

                // The violations are carried through to Backoff so it undoes the breach that
                //  happened rather than every breach that might have.  See WiseTele.Backoff.
                string reason = wisetele.SafeAtCoordinates(
                    Angle.RaFromHours(wisetele.RightAscension),
                    Angle.DecFromDegrees(wisetele.Declination),
                    out SafetyViolation violations);

                if (string.IsNullOrEmpty(reason))
                {
                    _consecutiveUnsafe = 0;
                    return;
                }

                wasUnsafe = true;
                _consecutiveUnsafe++;
                bool logThisPass = (_consecutiveUnsafe == 1) || (_consecutiveUnsafe % 60 == 0);

                string op = $"SafetyChecker: reason: {reason}";
                if (!Hardware.Hardware.ComputerHasControl)
                {
                    #region debug
                    if (logThisPass)
                        WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: Skipped: No computer control!");
                    #endregion
                    return;
                }

                #region debug
                if (logThisPass)
                    WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: activated (action: {actionAtEntry})");
                #endregion

                //
                // try/finally around RecoveringSafety.  Backoff calls MoveAxis, which throws
                //  if wisesafetooperate reports unsafe - entirely plausible when a coordinates
                //  violation coincides with a weather shutdown.  Without this the flag stayed
                //  true forever, which adds "Recovering safety" to the unsafe reasons and
                //  makes Tracking.set throw "Safety recovery is active" until the chain is
                //  restarted.
                //
                wisetele.RecoveringSafety = true;
                try
                {
                    if (wisetele.Slewing)
                        wisetele.AbortSlew(op);

                    if (wisetele.IsPulseGuiding)
                        wisetele.AbortPulseGuiding(op);

                    if (wisetele.Tracking)
                        wisetele.Tracking = false;

                    if (actionAtEntry == ActionWhenNotSafe.Backoff)
                        wisetele.Backoff(op, violations);
                }
                finally
                {
                    wisetele.RecoveringSafety = false;
                }
            }
            catch (Exception ex)
            {
                //
                // A TIMER CALLBACK THAT THROWS TERMINATES THE PROCESS.
                //
                // There was no catch here until 2026-09-20, when an ObjectDisposedException out
                //  of AbortSlew took the whole ASCOM server down - the safety monitor killing the
                //  thing it exists to protect, and with it the telescope, dome and focuser.
                //
                // Everything this method calls can throw: AbortSlew, Tracking.set and Backoff all
                //  reach hardware, and Backoff calls MoveAxis, which throws outright when
                //  wisesafetooperate reports unsafe.  So the callback swallows and logs instead.
                //  A check that fails is a check we retry in a second; a check that throws is an
                //  observatory that stops answering.
                //
                #region debug
                WiseTele.debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"SafetyChecker: caught {ex.GetType().Name}: {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
            finally
            {
                //
                // Re-arm.
                //
                // This used to be the last statement of the method, guarded by "if (Enabled)".
                //  Two things went wrong with that.  The ordinary SAFE path returns early, so
                //  it never reached the re-arm at all and the "periodic" monitor ran exactly
                //  once per enable - a 60 second slew got one check, about a second in, while
                //  the telescope was still next to where it started.  And on the unsafe path
                //  it did reach the line but Enabled was already false, because Backoff
                //  disables us: its closing MoveAxis(axis, rateStopped) reaches
                //  DisableIfNotNeeded, and Tracking has just been turned off, so no motors are
                //  active.  WhenNotSafe was reset to None by the same path.  Both are restored
                //  here.
                //
                // Terminating condition: keep checking while the motors are running, or while
                //  the position is still unsafe.  Once the telescope is both idle and safe we
                //  stop re-arming, which is what DisableIfNotNeeded intends.
                //
                bool motorsActive = wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.IsOn;

                //
                // A GRACE PERIOD, so that arming and the motors starting need not be simultaneous.
                //
                // Without it, removing the guard from EnableIfNeeded would only move the race one
                //  second later: arm, tick before the motors are up, find nothing moving and
                //  nothing unsafe, stop re-arming - and the move that follows is unmonitored, the
                //  very failure this is meant to end.
                //
                // Three idle passes, so a motor that takes up to about three seconds to report
                //  active is still covered.  Anything genuinely idle costs three extra
                //  SafeAtCoordinates calls and then stops, which is what DisableIfNotNeeded
                //  intends anyway.
                //
                if (motorsActive || wasUnsafe)
                    _idlePasses = 0;
                else
                    _idlePasses++;

                if (armedAtEntry && !WiseTele.BypassCoordinatesSafety &&
                    (motorsActive || wasUnsafe || _idlePasses < idlePassesBeforeGivingUp))
                {
                    _enabled = true;
                    WhenNotSafe = actionAtEntry;
                    _timer.Change(_period, Timeout.Infinite);
                }

                Interlocked.Exchange(ref _inCallback, 0);
            }
        }

        public SafetyMonitorTimer(int periodMillis = 1000)
        {
            _timer = new Timer(Guarded.Timer(nameof(SafetyChecker), SafetyChecker));
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
            //
            // NO "are the motors running yet?" TEST HERE.
            //
            // There used to be one, and it armed the monitor only if the motors happened to be
            //  active at the instant of the call:
            //
            //      if (!(wisetele.DirectionMotorsAreActive || wisetele.TrackingMotor.IsOn))
            //          return;
            //
            // Both callers ask BEFORE or WHILE the motors spin up - MoveAxis at WiseTele.cs:1438
            //  and the motor-start path at :1626 - so whether coordinate safety was watching at
            //  all came down to timing.  Measured 2026-09-20: four MoveAxis creeps past a test
            //  limit, three caught within a second, the fourth not caught at all.  The mount sat
            //  0.012h past the limit for twelve seconds and nothing happened, and there was no
            //  log line to say so, because the thing that would have written one was not running.
            //
            // Arming when nothing turns out to be moving is harmless: the re-arm test at the end
            //  of SafetyChecker is the real "should I keep going?" decision, and it now allows a
            //  few idle passes before giving up so that this arm-then-spin-up order is covered.
            //  The cost of a wasted pass is one SafeAtCoordinates; the cost of not arming is an
            //  unmonitored move.
            //
            //
            // Upgrade the action even when already armed.  This assignment used to sit inside
            //  the "!Enabled" test, so whichever caller armed the timer first decided what it
            //  would do for the rest of the episode: Tracking.set and InternalMoveAxis ask for
            //  Backoff, HandpadMoveAxis asks for Stop, and a handpad move that got there
            //  first left the monitor unable to back away from a limit.
            //
            // The enum is declared weakest to strongest - None, Stop, Backoff - so a plain
            //  comparison picks the stronger of the two.
            //
            if (action > WhenNotSafe)
                WhenNotSafe = action;

            // A fresh request gets the full grace, whether or not we were already armed.
            _idlePasses = 0;

            if (!Enabled)
                Enabled = true;
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
