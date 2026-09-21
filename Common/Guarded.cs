using System;
using System.Threading;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    /// <summary>
    /// Wrappers that stop a callback's exception from killing the process, or vanishing.
    /// </summary>
    //
    // WHY THIS EXISTS
    //
    // An exception that escapes a callback thread terminates the process.  Not the callback, not
    //  the timer - the whole process.  This codebase has been bitten three times in two days:
    //
    //   . SafetyMonitorTimer.SafetyChecker - an ObjectDisposedException out of AbortSlew took down
    //     the ASCOM server, and with it the telescope, dome and focuser.  The safety monitor
    //     killing the thing it exists to protect.
    //
    //   . Watcher.OnExit - read Process.Id after the worker had Closed the object.  Windows
    //     recorded SEVEN of these.  One left the observatory unsupervised for 6.6 hours, through
    //     the end of a night, because a dead watcher restarts nothing and stops nothing.
    //
    //   . The slewer tasks - a different failure of the same family.  Task.Run does NOT kill the
    //     process when it faults; since .NET 4.5 the exception is simply discarded unless someone
    //     observes it.  Two slewers faulted 3 ms after starting and logged one line each saying
    //     "Faulted", with no hint as to why.  That cost a deploy cycle to diagnose.
    //
    // So there are two distinct hazards wearing the same clothes, and they want opposite fixes:
    //
    //   TIMERS, THREADS AND LIBRARY-RAISED EVENTS kill the process.  Guard them.
    //   TASKS fail SILENTLY.  Observe them.
    //
    // WHAT THIS DELIBERATELY DOES NOT DO
    //
    // It does not make the process survive arbitrary failures.  A callback that throws has failed
    //  and the caller is told so in the log; what it must not do is take eleven other things down
    //  with it.  The .NET Framework offers <legacyUnhandledExceptionPolicy enabled="1"/>, which
    //  would suppress the terminations wholesale, and it is the wrong tool: it leaves a process
    //  running in an unknown state after an arbitrary fault.  For a telescope that is worse than
    //  dying - a driver limping on past an unexpected exception can leave motors energised.
    //  Crashing is honest; running while broken is not.  Guard where we know what the failure
    //  means; do not blanket-suppress.
    //
    public static class Guarded
    {
        private static readonly Debugger debugger = Debugger.Instance;

        /// <summary>
        /// Runs an action, logging anything it throws instead of letting it escape.
        /// </summary>
        /// <param name="what">Names the call site, so the log says which callback failed.</param>
        public static void Run(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log(what, ex);
            }
        }

        /// <summary>
        /// Wraps a TimerCallback so a throw cannot terminate the process.
        /// </summary>
        /// <example>
        /// new System.Threading.Timer(Guarded.Timer(nameof(OnDomeTimer), OnDomeTimer));
        /// </example>
        public static TimerCallback Timer(string what, TimerCallback callback)
        {
            return state => Run(what, () => callback(state));
        }

        /// <summary>
        /// Wraps an EventHandler for events raised by library threads - Process.Exited and the
        /// like - where a throw would otherwise reach nobody and kill the process.
        /// </summary>
        public static EventHandler Event(string what, EventHandler handler)
        {
            return (sender, args) => Run(what, () => handler(sender, args));
        }

        /// <summary>
        /// Wraps a ThreadStart, for the same reason.
        /// </summary>
        public static ThreadStart Thread(string what, ThreadStart body)
        {
            return () => Run(what, () => body());
        }

        /// <summary>
        /// Starts a task and OBSERVES it, so a fault is logged rather than discarded.
        /// </summary>
        //
        // The opposite problem to the others: this one is already non-fatal and that is exactly
        //  what makes it dangerous.  A faulted Task.Run whose exception nobody reads is silence,
        //  and silence is what made the MissingMethodException in the slewers take a deploy cycle
        //  to find.  The continuation runs on fault only, and flattens, because a task that
        //  wrapped several will otherwise report a bare AggregateException.
        //
        public static Task Fire(string what, Action action)
        {
            return Observe(what, Task.Run(action));
        }

        public static Task Fire(string what, Action action, CancellationToken token)
        {
            return Observe(what, Task.Run(action, token));
        }

        /// <summary>
        /// Attaches fault logging to an existing task and returns THAT TASK, not the continuation.
        /// </summary>
        //
        // Returning the continuation would be a trap.  With OnlyOnFaulted it is CANCELLED whenever
        //  the work succeeds, so a caller who kept the result - "slewer.task = ..." - would hold a
        //  task that faults with TaskCanceledException on every successful run.  The callers in
        //  this repo do keep it, and wait on it.  So: observe on the side, hand back the original.
        //
        public static Task Observe(string what, Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                    foreach (Exception ex in t.Exception.Flatten().InnerExceptions)
                        Log(what, ex);
            }, TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        /// <summary>
        /// Last-resort logging for a process: what died, and where.
        /// </summary>
        //
        // AppDomain.UnhandledException CANNOT prevent the termination - by design, since .NET 2.0.
        //  It is a chance to say what happened before the process goes, and that is worth having:
        //  the only reason the watcher's seven deaths were ever explained is that Windows kept a
        //  .NET Runtime event with the stack trace.  Had the driver written its own line, it would
        //  have been found six crashes earlier.
        //
        // UnobservedTaskException catches the OTHER kind - a faulted task nobody read - at the
        //  point the GC finalises it, which is late but better than never.
        //
        private static int _handlersInstalled;

        public static void InstallProcessHandlers(string processName)
        {
            // Idempotent: several components may ask, and subscribing twice would double every
            //  line in a log that is already measured in gigabytes.
            if (Interlocked.CompareExchange(ref _handlersInstalled, 1, 0) != 0)
                return;

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    Exception ex = e.ExceptionObject as Exception;
                    debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                        $"FATAL [{processName}]: unhandled {(ex == null ? e.ExceptionObject?.ToString() : ex.GetType().Name)}: " +
                        $"{ex?.Message} terminating={e.IsTerminating} at\n{ex?.StackTrace}");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                try
                {
                    foreach (Exception ex in e.Exception.Flatten().InnerExceptions)
                        debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                            $"UNOBSERVED [{processName}]: {ex.GetType().Name}: {ex.Message} at\n{ex.StackTrace}");
                    e.SetObserved();
                }
                catch { }
            };
        }

        private static void Log(string what, Exception ex)
        {
            try
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"Guarded({what}): caught {ex.GetType().Name}: {ex.Message} at\n{ex.StackTrace}");
            }
            catch
            {
                // Logging must never be the thing that escapes.
            }
        }
    }
}
