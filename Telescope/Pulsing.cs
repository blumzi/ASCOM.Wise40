using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.DeviceInterface;
using System.Collections.Concurrent;

namespace ASCOM.Wise40
{
    /// <summary>
    /// A PulserTask performs an Asynchronous PulseGuide on a WiseTelescope
    ///  axis.
    /// </summary>
    public class PulserTask
    {
        public TelescopeAxes _axis;
        public int _duration;
        public WiseVirtualMotor _motor;
        public Task task;
        private readonly Common.Debugger debugger;

        public PulserTask()
        {
            debugger = Common.Debugger.Instance;
        }

        public override string ToString()
        {
            return _axis.ToString() + "_PulseGuideTask";
        }

        public void Run()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            _motor.SetOn(Const.rateGuide);
            while (sw.ElapsedMilliseconds < _duration)
            {
                if (Pulsing.pulseGuideCT.IsCancellationRequested)
                {
                    #region debug
                    debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic,
                        "PulserTask on {0} aborted after {1} millis.", _axis.ToString(), sw.ElapsedMilliseconds);
                    #endregion
                    break;
                }
                Thread.Sleep(10);
            }
            _motor.SetOff();
            sw.Stop();
        }
    }

    public class Pulsing
    {
        public Common.Debugger debugger;
        private static volatile Pulsing _instance;
        private static readonly object syncObject = new object();
        private static readonly ConcurrentDictionary<TelescopeAxes, PulserTask> _active = new ConcurrentDictionary<TelescopeAxes, PulserTask>();
        private static WiseTele wisetele;
        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public static Dictionary<GuideDirections, TelescopeAxes> guideDirection2Axis = new Dictionary<GuideDirections, TelescopeAxes>
        {
            {GuideDirections.guideEast, TelescopeAxes.axisPrimary },
            {GuideDirections.guideWest, TelescopeAxes.axisPrimary },
            {GuideDirections.guideNorth, TelescopeAxes.axisSecondary },
            {GuideDirections.guideSouth, TelescopeAxes.axisSecondary },
        };

        private static Dictionary<GuideDirections, WiseVirtualMotor> guideDirection2Motor;

        public void Init()
        {
            debugger = Common.Debugger.Instance;
            wisetele = WiseTele.Instance;
            guideDirection2Motor = new Dictionary<GuideDirections, WiseVirtualMotor>() {
                { GuideDirections.guideEast, wisetele.EastMotor },
                { GuideDirections.guideWest, wisetele.WestMotor },
                { GuideDirections.guideNorth, wisetele.NorthMotor },
                { GuideDirections.guideSouth, wisetele.SouthMotor },
            };
        }

        public static CancellationTokenSource pulseGuideCTS = new CancellationTokenSource();
        public static CancellationToken pulseGuideCT;

        public Pulsing() { }

        static Pulsing() { }

        public static Pulsing Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new Pulsing();
                    }
                }
                return _instance;
            }
        }

        public void Abort(string reason)
        {
            pulseGuideCTS.Cancel();
            pulseGuideCTS = new CancellationTokenSource();
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, $"Aborted PulseGuiding (reason: {reason})");
            #endregion
        }

        public void Start(GuideDirections direction, int duration)
        {
            string op = $"Pulsing.Start(direction: {direction}, duration: {duration}) ";

            PulserTask pulserTask = new PulserTask() {
                _axis = guideDirection2Axis[direction],
                _duration = duration,
                _motor = guideDirection2Motor[direction],
                task = null,
            };
            pulseGuideCT = pulseGuideCTS.Token;

            Activate(pulserTask);
            pulserTask.task = Task.Run(() =>
            {
                try
                {
                    pulserTask.Run();
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, $"{op}: Caught: {ex.Message} at {ex.StackTrace}");
                    #endregion
                    Abort($"{op}: Caught: {ex.Message} at {ex.StackTrace}");
                    Deactivate(pulserTask, Activity.State.Aborted, $"Caught exception: {ex.Message}\n at {ex.StackTrace}\n");
                }
            }, pulseGuideCT).ContinueWith((t) =>
            {
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic,
                    $"pulser on {pulserTask._axis} completed with status: {t.Status}");
                #endregion
                Deactivate(pulserTask, Activity.State.Succeeded, $"pulsing on {pulserTask._axis} completed");
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void Activate(PulserTask t)
        {
            string before = ToString();

            if (! _active.TryAdd(t._axis, t))
            {
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActivePulsers:Activate {t._axis} already active [{_active}]");
                #endregion
                return;
            }

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActivePulsers:Activate added {t._axis} [{before}] => [{ToString()}]");
            #endregion
        }

        private void Deactivate(PulserTask t, Activity.State completionState, string completionReason)
        {
            string before = ToString();

            if (! _active.TryRemove(t._axis, out PulserTask _))
            {
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActivePulsers:Deactivate {t._axis} not active [{before}]");
                #endregion
                return;
            }

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugAxes, $"ActivePulsers:Deactivate removed {t._axis} [{before}] => [{ToString()}]");
            #endregion

            ActivityMonitor.ActivityType activityType = t._axis == TelescopeAxes.axisPrimary ?
                ActivityMonitor.ActivityType.PulsingRa :
                ActivityMonitor.ActivityType.PulsingDec;

            Activity activity = activityMonitor.LookupInProgress(activityType);
            if (activity != null) {
                Activity.PulsingEndParams endParams = new Activity.PulsingEndParams()
                {
                    endState = completionState,
                    endReason = completionReason,
                    _end = new Activity.TelescopeSlew.Coords
                    {
                        ra = WiseTele.Instance.RightAscension,
                        dec = WiseTele.Instance.Declination,
                    }
                };

                if (activityType == ActivityMonitor.ActivityType.PulsingRa)
                    (activity as Activity.PulsingRa)?.EndActivity(endParams);
                else
                    (activity as Activity.PulsingDec)?.EndActivity(endParams);
            }
        }

        public override string ToString()
        {
            return _active.ToCSV();
        }

        public static bool Active(TelescopeAxes axis)
        {
            return _active.ContainsKey(axis);
        }

        public static bool Active(GuideDirections direction)
        {
            return _active.ContainsKey(guideDirection2Axis[direction]);
        }

        public bool IsPulseGuiding
        {
            get
            {
                return _active.Count > 0;
            }
        }

        public string ReasonsForPulseGuiding
        {
            get
            {
                return IsPulseGuiding ? ToString() : "Telescope is not PulseGuiding";
            }
        }
    }
}