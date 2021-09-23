using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Net.Http;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;
using ASCOM.Wise40SafeToOperate;
using ASCOM.Wise40.Hardware;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ASCOM.Wise40
{
    public class WiseDome : WiseObject, IConnectable, IDisposable {
        private static readonly Version version = new Version(0, 2);

        private static readonly WiseSafeToOperate wiseSafeToOperate = WiseSafeToOperate.Instance;
        private static bool _initialized = false;

        private WisePin leftPin, rightPin, ventPin, projectorPin;
        private static readonly WiseDomeEncoder domeEncoder = WiseDomeEncoder.Instance;
        private List<IConnectable> connectables;
        private List<IDisposable> disposables;
        private bool _connected = false;
        private Angle _minimalMove = Angle.AzFromDegrees(2.0);
        private bool _isStuck;

        private static readonly Object _caliWriteLock = new object();
        private readonly WisePin[] caliPins = new WisePin[3];
        private bool Calibrating { get; set; } = false;
        public bool _autoCalibrate = false;

        public WiseDomeShutter wisedomeshutter = WiseDomeShutter.Instance;
        public static ActivityMonitor activityMonitor = ActivityMonitor.Instance;
        public static readonly Exceptor Exceptor = new Exceptor(Common.Debugger.DebugLevel.DebugDome);

        [Flags] public enum DomeState {
            Idle = 0,
            MovingCW = (1 << 0),
            MovingCCW = (1 << 1),
            Calibrating = (1 << 2),
            Parking = (1 << 3),
            Stopping = (1 << 4),
        };
        private DomeState _state;

        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        private StuckPhase _stuckPhase;

        public enum Direction { None, CW, CCW };

        private uint _prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        public class CalibrationPoint {
            public uint simulatedEncoderValue;
            public Angle simulatedAz;
            public WisePin pin;
            public Angle az;

            public CalibrationPoint(WisePin _pin, Angle _az, uint _simEnc)
            {
                pin = _pin;
                az = _az;
                simulatedEncoderValue = _simEnc;
            }
        };
        private readonly List<CalibrationPoint> calibrationPoints = new List<CalibrationPoint>();
        public const int TicksPerDomeRevolution = 1018;

        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;

        public const double _parkAzimuth = 90.0;
        private readonly Angle _simulatedStuckAz = new Angle(333.0);      // If targeted to this Az, we simulate dome-stuck (must be a valid az)

        private Angle _targetAz = null;

        private System.Threading.Timer _domeTimer;
        private System.Threading.Timer _movementTimer;
        private System.Threading.Timer _stuckTimer;

        private readonly int _movementTimeout = 2000;
        private readonly int _domeTimeout = 50;

        private readonly Debugger debugger = Debugger.Instance;

        private readonly AutoResetEvent internalArrivedAtAzEvent = new AutoResetEvent(false);
        private readonly List<AutoResetEvent> externalArrivedAtAzEvents = new List<AutoResetEvent>();
        private readonly static AutoResetEvent _foundCalibration = new AutoResetEvent(false);
        private readonly static Hardware.Hardware hw = Hardware.Hardware.Instance;

        public static bool _adjustingForTracking = false;
        private static bool Stopping { get; set; }

        static WiseDome() { }
        public WiseDome() { }

        private static readonly Lazy<WiseDome> lazy = new Lazy<WiseDome>(() => new WiseDome()); // Singleton

        public static WiseDome Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (_initialized)
                return;

            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "Starting init()");
            WiseName = "Wise40 Dome";
            ReadProfile();

            try {
                uint caliPointsSpacing = domeEncoder.Ticks / 3;

                connectables = new List<IConnectable>();
                disposables = new List<IDisposable>();

                leftPin = new WisePin("DomeLeft", hw.domeboard, DigitalPortType.FirstPortA, 2, DigitalPortDirection.DigitalOut, controlled: true);
                rightPin = new WisePin("DomeRight", hw.domeboard, DigitalPortType.FirstPortA, 3, DigitalPortDirection.DigitalOut, controlled: true);

                caliPins[0] = new WisePin(Const.notsign + "DomeCali0", hw.domeboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalIn);
                caliPins[1] = new WisePin(Const.notsign + "DomeCali1", hw.domeboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalIn);
                caliPins[2] = new WisePin(Const.notsign + "DomeCali2", hw.domeboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalIn);

                calibrationPoints.Add(new CalibrationPoint(caliPins[0], new Angle(254.6, Angle.AngleType.Az), 10 + (2 * caliPointsSpacing)));
                calibrationPoints.Add(new CalibrationPoint(caliPins[1], new Angle(133.0, Angle.AngleType.Az), 10 + (1 * caliPointsSpacing)));
                calibrationPoints.Add(new CalibrationPoint(caliPins[2], new Angle( 18.0, Angle.AngleType.Az), 10 + (0 * caliPointsSpacing)));

                ventPin = new WisePin("DomeVent", hw.domeboard, DigitalPortType.FirstPortA, 5, DigitalPortDirection.DigitalOut);
                projectorPin = new WisePin("DomeProjector", hw.domeboard, DigitalPortType.FirstPortA, 4, DigitalPortDirection.DigitalOut);

                List<WisePin> domePins = new List<WisePin> { leftPin, rightPin, ventPin, projectorPin };
                domePins.AddRange(caliPins);
                domePins.AddRange(wisedomeshutter.Pins());

                connectables.AddRange(domePins);
                disposables.AddRange(domePins);
            }
            catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: constructor caught: {e.Message} at {e.StackTrace}.");
            }

            try
            {
                leftPin.SetOff();
                rightPin.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) { }

            Calibrating = false;
            _state = DomeState.Idle;

            _domeTimer = new System.Threading.Timer(new TimerCallback(OnDomeTimer));
            _domeTimer.Change(_domeTimeout, _domeTimeout);

            _movementTimer = new System.Threading.Timer(new TimerCallback(OnMovementTimer));

            _stuckTimer = new System.Threading.Timer(new TimerCallback(OnStuckTimer));

            _initialized = true;

            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: init() done.");
        }

        private bool StateIsOn(DomeState flag)
        {
            return (_state & flag) != 0;
        }

        private void SetDomeState(DomeState flags)
        {
            _state |= flags;
        }

        private void UnsetDomeState(DomeState flags)
        {
            _state &= ~flags;
        }

        public void SetArrivedAtAzEvent(AutoResetEvent e)
        {
            //_arrivedAtAzEvent = e;
            //#region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugDome,
            //    "WiseDome: SetArrivedAtAzEvent(#{0})", _arrivedAtAzEvent.GetHashCode());
            //#endregion
            externalArrivedAtAzEvents.Add(e);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                "WiseDome:SetArrivedAtAzEvent Added: (#{0})", e.GetHashCode());
            #endregion
        }

        public void Connect(bool connected)
        {
            foreach (var connectable in connectables)
            {
                connectable.Connect(connected);
            }
            _connected = connected;

            ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.Dome, _connected, line: ActivityMonitor.Tracer.dome.Line));
            ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.Dome, _connected, line: ActivityMonitor.Tracer.shutter.Line));
            ActivityMonitor.Event(new Event.ProjectorEvent(Projector));
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (value)
                {
                    RestoreCalibrationData();

                    if (!Calibrated && _autoCalibrate && Hardware.Hardware.ComputerHasControl)
                        Task.Run(() => StartFindingHome());
                }

                if (value == _connected)
                    return;

                _connected = value;
            }
        }

        public bool Calibrated
        {
            get
            {
                return domeEncoder.Calibrated;
            }
        }

        /// <summary>
        /// Calculates how many degrees it will take the dome to stop.
        /// Right now it's about 0.7 degrees (two ticks).  In the future it may be derived
        ///  from the Azimuth and maybe from the direction of travel.
        /// </summary>
        /// <param name="az"></param>
        /// <returns></returns>
        private Angle InertiaAngle()
        {
            return new Angle(2 * (360.0 / TicksPerDomeRevolution));
        }

        /// <summary>
        /// Checks if we're close enough to a given Azimuth
        /// </summary>
        /// <param name="there"></param>
        /// <returns></returns>
        private bool Arriving(Angle there)
        {
            if (!DomeIsMoving)
                return false;

            string message = $"WiseDome:arriving: at {Azimuth.ToShortNiceString()} target {there.ToShortNiceString()}: ";

            ShortestDistanceResult shortest = Azimuth.ShortestDistance(there);
            Angle inertial = InertiaAngle();

            if (StateIsOn(DomeState.MovingCW) && (shortest.direction == Const.AxisDirection.Decreasing))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message + "direction changed CW to CCW => true");
                #endregion
                return true;
            }
            else if (StateIsOn(DomeState.MovingCCW) && (shortest.direction == Const.AxisDirection.Increasing))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message + "direction changed CCW to CW => true");
                #endregion
                return true;
            }
            else if (shortest.angle <= inertial)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message +
                    $"shortest.Angle {shortest.angle} <= inertiaAngle({inertial}) => true");
                #endregion
                return true;
            }
            else
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message + "=> false");
                #endregion
                return false;
            }
        }

        private bool DomeIsMoving
        {
            get
            {
                var ret = StateIsOn(DomeState.MovingCCW) || StateIsOn(DomeState.MovingCW);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"DomeIsMoving: {ret}");
                #endregion
                return ret;
            }
        }

        /// <summary>
        /// The dome timer is always enabled, at 100 millisec intervals.
        /// </summary>
        /// <param name="state"></param>
        private void OnDomeTimer(object state)
        {
            CalibrationPoint cp;

            if ((cp = AtCaliPoint) != null)
            {
                domeEncoder.Calibrate(cp.az);
                if (Calibrating)
                {
                    Calibrating = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                        $"WiseDome: Setting _foundCalibration[{calibrationPoints.IndexOf(cp)}] == {cp.az.ToShortNiceString()} ...");
                    #endregion
                    Stop($"Arrived at calibration point {cp.az.ToShortNiceString()}");
                    Thread.Sleep(2000);     // settle down
                    _foundCalibration.Set();
                }
            }

            if (_targetAz != null && Arriving(_targetAz) && !StateIsOn(DomeState.Stopping))
            {
                SetDomeState(DomeState.Stopping);   // prevent onTimer from re-stopping
                Stop("Reached target");
                UnsetDomeState(DomeState.Stopping);

                _targetAz = null;

                if (StateIsOn(DomeState.Parking))
                {
                    UnsetDomeState(DomeState.Parking);
                    if (WiseSite.OperationalMode != WiseSite.OpMode.WISE)
                        AtPark = true;
                }

                GenerateArrivalEvent();
            }
        }

        private bool ShutterIsMoving
        {
            get
            {
                return wisedomeshutter.IsMoving;
            }
        }

        /// <summary>
        /// <para>
        /// The movement timer is activated when the dome starts moving either CW or CCW, and
        ///   gets disabled by Stop().
        /// </para>
        /// <para>The handler decides whether the dome encoder has changed, or the dome seems to be stuck.</para>
        /// </summary>
        /// <param name="state"></param>
        private void OnMovementTimer(object state)
        {
            uint currTicks, deltaTicks;
            const int leastExpectedTicks = 2;  // least number of Ticks expected to change in two seconds

            SaveCalibrationData();

            // the movementTimer should not be Enabled unless the dome is moving
            if (_isStuck || !DomeIsMoving)
                return;

            currTicks = domeEncoder.Value;
            if (currTicks == _prevTicks)
            {
                _isStuck = true;
            }
            else
            {
                if (StateIsOn(DomeState.MovingCW))
                {
                    if (_prevTicks > currTicks)
                        deltaTicks = _prevTicks - currTicks;
                    else
                        deltaTicks = domeEncoder.Ticks - currTicks + _prevTicks;

                    if (deltaTicks < leastExpectedTicks)
                        _isStuck = true;
                }
                else if (StateIsOn(DomeState.MovingCCW))
                {
                    if (_prevTicks > currTicks)
                        deltaTicks = _prevTicks - currTicks;
                    else
                        deltaTicks = domeEncoder.Ticks - _prevTicks + currTicks;

                    if (deltaTicks < leastExpectedTicks)
                        _isStuck = true;
                }
            }

            //if (_isStuck)
            //{
            //    _stuckPhase = StuckPhase.NotStuck;
            //    nextStuckEvent = DateTime.Now;
            //    _stuckTimer.Change(0, 1000);
            //}

            _prevTicks = currTicks;
        }

        private void OnStuckTimer(object state)
        {
            DateTime rightNow;
            WisePin backwardPin, forwardPin;

            rightNow = DateTime.Now;

            if (DateTime.Compare(rightNow, nextStuckEvent) < 0)
                return;

            forwardPin = StateIsOn(DomeState.MovingCCW) ? leftPin : rightPin;
            backwardPin = StateIsOn(DomeState.MovingCCW) ? rightPin : leftPin;

            string pre = $"WiseDome: stuck: {Azimuth}, ";
            switch (_stuckPhase) {
                case StuckPhase.NotStuck:              // Stop, let the wheels cool down
                    forwardPin.SetOff();
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.FirstStop;
                    nextStuckEvent = rightNow.AddMilliseconds(10000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, pre + "phase1: stopped moving, letting wheels cool for 10 seconds");
                    #endregion

                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, pre + "phase2: going backwards for 2 seconds");
                    #endregion
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, pre + "phase3: stopping for 2 seconds");
                    #endregion
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    _isStuck = false;
                    _stuckTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    nextStuckEvent = rightNow.AddYears(100);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, pre + "phase4: resumed original motion");
                    #endregion
                    break;
            }
        }

        public void StartMovingCW()
        {
            AtPark = false;

            try
            {
                leftPin.SetOff();
                rightPin.SetOn();
            } catch (Hardware.Hardware.MaintenanceModeException)
            {
                return;
            }

            SetDomeState(DomeState.MovingCW);
            domeEncoder.setMovement(Direction.CW);
            _movementTimer.Change(0, _movementTimeout);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: Started moving CW");
            #endregion
        }

        public void StartMovingCCW()
        {
            AtPark = false;
            try
            {
                rightPin.SetOff();
                leftPin.SetOn();
            } catch (Hardware.Hardware.MaintenanceModeException)
            {
                throw;
            }

            SetDomeState(DomeState.MovingCCW);
            domeEncoder.setMovement(Direction.CCW);
            _movementTimer.Change(0, _movementTimeout);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: Started moving CCW");
            #endregion
        }

        public void Stop(string reason)
        {
            if (Stopping)
                return;

            int tries;

            Stopping = true;
            #region debug
            string dbg = $"WiseDome:Stop({reason}) Starting to stop (encoder: {domeEncoder.Value}) ";
            if (Calibrated)
                dbg += $", az: {Azimuth.ToShortNiceString()}";
            else
                dbg += ", not calibrated";
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, dbg);
            #endregion
            _movementTimer.Change(Timeout.Infinite, Timeout.Infinite);
            rightPin.SetOff();
            leftPin.SetOff();
            UnsetDomeState(DomeState.MovingCCW | DomeState.MovingCW);
            domeEncoder.setMovement(Direction.None);

            for (tries = 0; tries < 10; tries++)
            {
                uint prev = domeEncoder.Value;
                Thread.Sleep(500);
                uint curr = domeEncoder.Value;
                if (prev == curr)
                    break;
            }

            if (Calibrated)
                SaveCalibrationData();
            #region debug
            try
            {
                dbg = $"WiseDome:Stop({reason}) Fully stopped ";
                if (Calibrated)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, dbg +
                        $"at az: {Azimuth.ToShortNiceString()}, " +
                        $"target: {((_targetAz == null) ? "none" : _targetAz.ToShortNiceString())} ",
                        $"encoder: {domeEncoder.Value}, " +
                        $"after {tries + 1} tries");
                }
                else
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, dbg + $"(not calibrated), encoder: {domeEncoder.Value}, after {tries + 1} tries");
                }
            } catch(Exception ex) {
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"Caught \"{ex.Message}\" at\n{ex.StackTrace}");
            }
            #endregion

            if (_adjustingForTracking)
            {
                _adjustingForTracking = false;
            }
            else
            {
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.DomeSlew, new Activity.DomeSlew.EndParams()
                {
                    endState = Activity.State.Succeeded,
                    endReason = reason,
                    endAz = Azimuth.Degrees,
                });
            }

            Stopping = false;
        }

        public CalibrationPoint AtCaliPoint
        {
            get
            {
                foreach (var cp in calibrationPoints)
                {
                    if (Simulated)
                    {
                        if (domeEncoder.Value == cp.simulatedEncoderValue)
                            return cp;
                    }
                    else
                    {
                        if (cp.pin.isOff)
                            return cp;
                    }
                }
                return null;
            }
        }

        public Angle Azimuth
        {
            get
            {
                Angle ret = Angle.AzFromDegrees(double.NaN);

                if (!Calibrated)
                {
                    if (_autoCalibrate)
                    {
                        try
                        {
                            StartFindingHome();
                        }
                        catch
                        {
                            return ret;
                        }
                    }
                    else
                        return ret;
                }

                ret = domeEncoder.Azimuth;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: Azimuth: get => {ret.ToShortNiceString()}");
                #endregion
                return ret;
            }

            set
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: Azimuth: set({value})");
                #endregion
                domeEncoder.Calibrate(value);
                SaveCalibrationData();
            }
        }

        public bool Vent
        {
            get
            {
                return ventPin.isOn;
            }

            set
            {
                if (value)
                    ventPin.SetOn();
                else
                    ventPin.SetOff();
            }
        }

        public bool Projector
        {
            get
            {
                return projectorPin.isOn;
            }

            set
            {
                if (value == projectorPin.isOn)
                    return;

                if (value)
                {
                    projectorPin.SetOn();
                    ActivityMonitor.Event(new Event.ProjectorEvent(true));
                }
                else
                {
                    projectorPin.SetOff();
                    ActivityMonitor.Event(new Event.ProjectorEvent(false));
                }
            }
        }

        public void StartFindingHome()
        {
            if (Hardware.Hardware.MaintenanceMode)
                Exceptor.Throw<InvalidOperationException>("StartFindingHome", "Cannot move, MAINTENANCE MODE is active!");

            if (Calibrated)
            {
                //
                // Consider safety only AFTER calibration, otherwise we can never produce
                //  an Azimuth reading
                //
                if (ShutterIsMoving)
                    Exceptor.Throw<InvalidOperationException>("StartFindingHome", "Cannot move, shutter is active!");

                if (!wiseSafeToOperate.IsSafeWithoutCheckingForShutdown())
                    Exceptor.Throw<InvalidOperationException>("StartFindingHome", wiseSafeToOperate.Action("unsafereasons", ""));
            }

            AtPark = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome:StartFindingHome: started");
            #endregion
            Calibrating = true;

            activityMonitor.NewActivity(new Activity.DomeSlew(new Activity.DomeSlew.StartParams()
            {
                type = Activity.DomeSlew.DomeEventType.FindHome,
            }));

            if (Calibrated)
            {
                List<ShortestDistanceResult> distanceToCaliPoints = new List<ShortestDistanceResult>();
                foreach (var cp in calibrationPoints)
                    distanceToCaliPoints.Add(Azimuth.ShortestDistance(cp.az));

                ShortestDistanceResult closest = new ShortestDistanceResult(new Angle(360.0, Angle.AngleType.Az), Const.AxisDirection.None);
                foreach (var res in distanceToCaliPoints)
                {
                    if (res.angle < closest.angle)
                    {
                        closest = res;
                    }
                }

                switch (closest.direction)
                {
                    case Const.AxisDirection.Decreasing: StartMovingCCW(); break;
                    case Const.AxisDirection.Increasing: StartMovingCW(); break;
                }
            }
            else
            {
                SetDomeState(DomeState.Calibrating);
                StartMovingCCW();
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome:StartFindingHome: waiting for _foundCalibration ...");
            #endregion
            _foundCalibration.WaitOne();
            UnsetDomeState(DomeState.Calibrating);
            activityMonitor.EndActivity(ActivityMonitor.ActivityType.DomeSlew, new Activity.DomeSlew.EndParams()
            {
                endState = Activity.State.Succeeded,
                endReason = $"Found calibration point at {Azimuth.ToShortNiceString()}",
                endAz = Azimuth.Degrees,
            });
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: FindHomePoint: _foundCalibration was Set()");
            #endregion
        }

        public void GenerateArrivalEvent()
        {
            var waiters = new List<AutoResetEvent>() { internalArrivedAtAzEvent };
            waiters.AddRange(externalArrivedAtAzEvents);

            foreach (var e in waiters)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                    $"GenerateArrivalEvent: Setting arrivedAtAzEvent (#{e.GetHashCode()})");
                #endregion
                e.Set();
            }
        }

        public void SlewToAzimuth(double degrees, string reason)
        {
            Angle targetAng = new Angle(degrees, Angle.AngleType.Az);
            string op = $"SlewToAzimuth({targetAng.ToShortNiceString()}, reason: {reason})";

            if (Hardware.Hardware.MaintenanceMode)
                Exceptor.Throw<InvalidOperationException>(op, "MAINTENENCE MODE is active");

            if (Slaved)
                Exceptor.Throw<InvalidOperationException>(op, "Dome is Slaved");

            if (degrees < 0 || degrees >= 360)
                Exceptor.Throw<InvalidOperationException>(op, $"Invalid azimuth: {targetAng.ToNiceString()}, must be >= 0 and < 360");

            if (ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot move, shutter is active!");

            if ((!StateIsOn(DomeState.Parking)) && !wiseSafeToOperate.IsSafeWithoutCheckingForShutdown())
                Exceptor.Throw<InvalidOperationException>(op, wiseSafeToOperate.Action("unsafereasons", ""));

            if (!Calibrated)
            {
                if (_autoCalibrate)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: {op}, not calibrated, _autoCalibrate == true, calling FindHomePoint");
                    #endregion
                    StartFindingHome();
                } else
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: {op}, not calibrated, _autoCalibrate == false, throwing InvalidOperationException");
                    #endregion
                    Exceptor.Throw<InvalidOperationException>(op, "Not calibrated!");
                }
            }

            if (!FarEnoughToMove(targetAng))
            {
                if (StateIsOn(DomeState.Parking))
                {
                    AtPark = true;
                }

                GenerateArrivalEvent();

                return;
            }

            // At this point we're commited to slewing
            if (!_adjustingForTracking)
            {
                // Log only real slew requests
                activityMonitor.NewActivity(new Activity.DomeSlew(new Activity.DomeSlew.StartParams
                {
                    type = Activity.DomeSlew.DomeEventType.Slew,
                    startAz = Azimuth.Degrees,
                    targetAz = degrees,
                    reason = reason,
                }));
            }

            _targetAz = targetAng;
            AtPark = false;

            ShortestDistanceResult shortest = domeEncoder.Azimuth.ShortestDistance(_targetAz);
            switch (shortest.direction)
            {
                case Const.AxisDirection.Decreasing:
                    StartMovingCCW();
                    break;
                case Const.AxisDirection.Increasing:
                    StartMovingCW();
                    break;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, $"WiseDome: {op} => at: {Azimuth.ToShortNiceString()}, dist: {shortest.angle.ToShortNiceString()}), moving {shortest.direction}");
            #endregion

            if (Simulated && _targetAz == _simulatedStuckAz)
            {
                Angle epsilon = new Angle(5.0);
                Angle stuckAtAz = (shortest.direction == Const.AxisDirection.Increasing) ? _targetAz - epsilon : _targetAz + epsilon;

                domeEncoder.SimulateStuckAt(stuckAtAz);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, $"WiseDome: Dome encoder will simulate stuck at {stuckAtAz}");
                #endregion
            }
        }

        public void Unpark()
        {
            AtPark = false;
        }

        public bool AtPark { get; set; }

        public string Digest
        {
            get
            {
                TimeSpan _sinceLastSuccess = TimeSpan.MaxValue, _sinceLastFailure = TimeSpan.MaxValue;
                DateTime now = DateTime.Now;
                PeriodicHttpFetcher fetcher = wisedomeshutter.periodicHttpFetcher;

                if (fetcher != null)
                {
                    if (fetcher.LastSuccess != DateTime.MinValue)
                        _sinceLastSuccess = now.Subtract(fetcher.LastSuccess);
                    if (fetcher.LastFailure != null)
                        _sinceLastFailure = now.Subtract(fetcher.LastFailure);
                }

                ShutterDigest shutterDigest = new ShutterDigest()
                    {
                        Status = ShutterStatusString,
                        State = ShutterState,
                        Reason = ShutterStateReason,
                        RangeCm = wisedomeshutter.RangeCm,
                        PercentOpen = wisedomeshutter.PercentOpen,
                        TimeSinceLastReading = (fetcher == null) ? TimeSpan.MaxValue : fetcher.Age,
                        WiFiIsWorking = (fetcher == null) ? false : fetcher.Alive,
                        TotalCommunicationAttempts = (fetcher == null) ? 0 : fetcher.Successes + fetcher.Failures,
                        FailedCommunicationAttempts = (fetcher == null) ? 0 : fetcher.Failures,
                        TimeSinceLastSuccessfullReading = _sinceLastSuccess,
                        TimeSinceLastFailedReading = _sinceLastFailure,
                        Tip = wisedomeshutter.ReasonsForIsMoving,
                    };

                double azimuth = Azimuth.Degrees;
                double targetAzimuth = _targetAz == null ? Const.noTarget : _targetAz.Degrees;
                bool calibrated = Calibrated;
                string status = Status;
                bool projector = Projector;
                bool vent = Vent;
                bool atPark = AtPark;
                bool slewing = Slewing;
                bool directionMotorsAreActive = DirectionMotorsAreActive;

                DomeDigest domeDigest = new DomeDigest()
                {
                    Azimuth = azimuth,
                    TargetAzimuth = targetAzimuth,
                    Calibrated = calibrated,
                    Status = status,
                    Vent = vent,
                    Projector = projector,
                    AtPark = atPark,
                    Slewing = slewing,
                    DirectionMotorsAreActive = directionMotorsAreActive,
                    Shutter = shutterDigest,
                    Tip = ReasonsForSlewing,
                };

                return JsonConvert.SerializeObject(domeDigest);
            }
        }

        public string Status
        {
            get
            {
                if (!DomeIsMoving)
                    return string.Empty;

                string ret = "Moving ";

                if (StateIsOn(DomeState.MovingCW))
                    ret += "CW";
                else if (StateIsOn(DomeState.MovingCCW))
                    ret += "CCW";

                if (_targetAz != null)
                    ret += $" to {_targetAz.ToShortNiceString()}";

                if (StateIsOn(DomeState.Calibrating))
                    ret += " (calibrating)";
                if (StateIsOn(DomeState.Parking))
                    ret += " (parking)";

                return ret;
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                wisedomeshutter.Dispose();
                leftPin.SetOff();
                rightPin.SetOff();

                Vent = false;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool Slewing
        {
            get
            {
                if (Slaved)
                    Exceptor.Throw<InvalidOperationException>("Slewing", "Dome is Slaved");

                return DomeIsMoving || ShutterIsMoving;
            }
        }

        public string ReasonsForSlewing
        {
            get
            {
                return DomeIsMoving ? $"Moving {(StateIsOn(DomeState.MovingCCW) ? "CCW" : "CW")}" : "Dome is not moving";
            }
        }

        public bool Slaved { get; set; } = false;

        public void Park()
        {
            const string op = "Park";

            if (Slaved)
                Exceptor.Throw<InvalidOperationException>(op, "Dome is Slaved");

            if (ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Shutter is active!");

            if (!Calibrated && !_autoCalibrate)
                Exceptor.Throw<InvalidOperationException>(op, "Not calibrated!");

            SetDomeState(DomeState.Parking);

            AtPark = false;
            SlewToAzimuth(_parkAzimuth, op);
        }

        public void OpenShutter(string reason, bool bypassSafety = false)
        {
            string op = $"WiseDome.OpenShutter(reason: {reason})";

            if (activityMonitor.ShuttingDown)
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.ACP)
                {
                    return;
                }
                else
                {
                    Exceptor.Throw<DriverException>(op, Const.UnsafeReasons.ShuttingDown);
                }
            }

            if (DirectionMotorsAreActive)
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.ACP)
                {
                    return;
                }
                else
                {
                    Exceptor.Throw<DriverException>(op, "Dome is slewing!");
                }
            }

            if (!bypassSafety && !wiseSafeToOperate.IsSafeWithoutCheckingForShutdown())
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.ACP)
                {
                    return;
                }
                else
                {
                    Exceptor.Throw<DriverException>(op, wiseSafeToOperate.CommandString("unsafeReasons", false));
                }
            }

            int percentOpen = wisedomeshutter.PercentOpen;
            if (percentOpen != -1 && percentOpen > 98)
                return;

            if (wisedomeshutter.State == ShutterState.shutterOpening)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"{op}: Already opening");
                return;
            }

            wisedomeshutter.Stop($"{op}: Stopped before StartOpening");
            wisedomeshutter.StartOpening(op);
            if (wisedomeshutter.syncVentWithShutter)
                Vent = true;
        }

        public void CloseShutter(string reason)
        {
            string op = $"CloseShutter(reason: {reason})";

            if (DirectionMotorsAreActive)
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.ACP)
                {
                    return;
                }
                else
                {
                    Exceptor.Throw<InvalidOperationException>($"{op}", "Cannot close shutter while dome is slewing!");
                }
            }

            int percentOpen = wisedomeshutter.PercentOpen;
            if (percentOpen != -1 && percentOpen < 1)
                return;

            if (wisedomeshutter.State == ShutterState.shutterClosing)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugShutter, $"{op}: Shutter is already closing");
                return;
            }

            wisedomeshutter.Stop($"{op}: Stopped before StartClosing");
            wisedomeshutter.StartClosing($"{op}");
            if (wisedomeshutter.syncVentWithShutter)
                Vent = false;
        }

        public void AbortSlew(string reason)
        {
            Stop($"From WiseDome.AbortSlew({reason})");
        }

        public double Altitude
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("Altitude", "Not Implemented");
                return Double.NaN;
            }
        }

        public bool AtHome
        {
            get
            {
                return AtCaliPoint == calibrationPoints[0];
            }
        }

        public bool CanFindHome
        {
            get
            {
                return true;
            }
        }

        public bool CanPark
        {
            get
            {
                return true;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                return false;
            }
        }

        public bool CanSetAzimuth
        {
            get
            {
                return true;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return false;
            }
        }

        public bool CanSetShutter
        {
            get
            {
                return true;
            }
        }

        public bool CanSlave
        {
            get
            {
                return true;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                return true;
            }
        }

        public void SyncToAzimuth(double degrees)
        {
            Angle ang = new Angle(degrees, Angle.AngleType.Az);

            if (degrees < 0.0 || degrees >= 360.0)
                Exceptor.Throw <InvalidValueException>($"SyncToAzimuth({ang})", "Angle must be >= 0 and < 360");
            Azimuth = ang;
        }

        public static void SlewToAltitude(double Altitude)
        {
            Exceptor.Throw<MethodNotImplementedException>($"SlewToAltitude({Altitude})", "Not implemented");
        }

        public static void SetPark()
        {
            Exceptor.Throw<MethodNotImplementedException>("SetPark", "Not implemented");
        }

        public ShutterState ShutterState
        {
            get
            {
                return wisedomeshutter.State;
            }
        }

        public string ShutterStateReason
        {
            get
            {
                return wisedomeshutter.StateReason;
            }
        }

        public string ShutterStatusString
        {
            get
            {
                string ret = ShutterState.ToString().ToLower().Remove(0, "shutter".Length);

                if (wisedomeshutter.periodicHttpFetcher != null && wisedomeshutter.periodicHttpFetcher.Alive)
                {
                    int percent = wisedomeshutter.PercentOpen;

                    if (percent != -1)
                        ret += $" ({percent}% open)";
                }
                else
                {
                    ret += " (error:No WiFi connection!)";
                }

                return ret;
            }
        }

        private readonly static ArrayList supportedActions = new ArrayList() {
            "projector",
            "vent",
            "digest",
        };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            string param = actionParameters.ToLower();
            string ret = "ok";

            switch (actionName)
            {
                case "debug":
                    if (!String.IsNullOrEmpty(actionParameters))
                    {
                        Debugger.DebugLevel newDebugLevel;
                        try
                        {
                            Enum.TryParse<Debugger.DebugLevel>(actionParameters, out newDebugLevel);
                            Debugger.SetCurrentLevel(newDebugLevel);
                        }
                        catch
                        {
                            return $"Cannot parse DebugLevel \"{actionParameters}\"";
                        }
                    }
                    return $"{Debugger.Level}";

                case "projector":
                    if (!string.IsNullOrEmpty(param))
                        Projector = Convert.ToBoolean(param);

                    return JsonConvert.SerializeObject(Projector);

                case "vent":
                    if (!string.IsNullOrEmpty(param))
                        Vent = Convert.ToBoolean(param);

                    return JsonConvert.SerializeObject(Vent);

                case "status":
                    return Digest;

                case "halt":
                case "stop":
                    Stop($"Action: \"{actionName}\"");
                    return "ok";

                case "unpark":
                    Unpark();
                    return "ok";

                case "set-zimuth":
                    Azimuth = Angle.AzFromDegrees(Convert.ToDouble(actionParameters));
                    return "ok";

                case "start-moving":
                    if (!wiseSafeToOperate.IsSafeWithoutCheckingForShutdown() && !activityMonitor.ShuttingDown)
                        Exceptor.Throw<InvalidOperationException>("Action(\"start-moving\")", wiseSafeToOperate.Action("unsafereasons", ""));

                    switch (param)
                    {
                        case "cw":
                            StartMovingCW();
                            break;
                        case "ccw":
                            StartMovingCCW();
                            break;
                        default:
                            ret = $"Bad parameter \"{param}\" for \"start-moving\".  Can be either \"cw\" or \"ccw\"";
                            break;
                    }
                    return ret;

                case "shutter":
                    switch(param)
                    {
                        case "halt":
                            wisedomeshutter.Stop($"Action \"{actionName}\".");
                            break;
                    }
                    return ret;

                case "sync-vent-with-shutter":
                    switch (param)
                    {
                        case "":
                            ret = SyncVentWithShutter.ToString();
                            break;

                        default:
                            bool onOff;

                            if (bool.TryParse(param, out onOff))
                            {
                                SyncVentWithShutter = onOff;
                            }
                            else
                            {
                                ret = $"Bad parameter \"{param}\" to \"sync-vent-with-shutter\"";
                            }
                            break;
                    }
                    return ret;

                case "auto-calibrate":
                    switch (param)
                    {
                        case "":
                            ret = _autoCalibrate.ToString();
                            break;

                        default:
                            bool calibrate;

                            if (bool.TryParse(param, out calibrate))
                            {
                                _autoCalibrate = calibrate;
                            }
                            else
                            {
                                ret = $"Bad parameter \"{param}\" to \"auto-calibrate\"";
                            }
                            break;
                    }
                    return ret;

                case "calibrate":
                    if (! Calibrated)
                        StartFindingHome();
                    return ret;

                default:
                    Exceptor.Throw<ActionNotImplementedException>($"Action(\"{actionName}\")",
                        "Not implemented by this driver");
                    return string.Empty;
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw})", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw})", "Not implemented");
            return false;
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            Exceptor.Throw<MethodNotImplementedException>($"CommandString({command}, {raw})", "Not implemented");
            return string.Empty;
        }

        private void CheckConnected(string message)
        {
            if (!Connected)
                Exceptor.Throw<NotConnectedException>("CheckConnected", message);
        }

        public string Description
        {
            get
            {
                return "Wise40 Dome";
            }
        }

        public string DriverInfo
        {
            get
            {
                return "First draft, Version: " + DriverVersion;
            }
        }

        public string DriverVersion
        {
            get
            {
                return $"{version.Major}.{version.Minor}";
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return Convert.ToInt16("2");
            }
        }

        #region SaveRestoreCalibrationData

        private readonly string calibrationDataFilePath = Const.topWise40Directory + "Dome/CalibrationData.txt";

        private void SaveCalibrationData()
        {
            if (!Calibrated)
                return;

            List<string> lines = new List<string>();
            DateTime now = DateTime.Now;

            lines.Add("#");
            lines.Add("# WiseDome calibration data, generated automatically, please don't change!");
            lines.Add("#");
            lines.Add($"# Saved: {now.ToLocalTime()}");
            lines.Add("#");
            lines.Add($"Encoder: {domeEncoder.Value}");
            lines.Add($"Azimuth: {Azimuth.Degrees}");

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(calibrationDataFilePath));

            lock (_caliWriteLock)
            {
                System.IO.File.WriteAllLines(calibrationDataFilePath, lines);
            }
        }

        private void RestoreCalibrationData()
        {
            uint savedEncoderValue = uint.MaxValue;
            double savedAzimuth = double.NaN;

            if (!System.IO.File.Exists(calibrationDataFilePath))
                return;

            bool valid = true;
            foreach (string line in System.IO.File.ReadLines(calibrationDataFilePath))
            {
                if (line.StartsWith("Encoder: "))
                {
                    if (!UInt32.TryParse(line.Substring("Encoder: ".Length), out savedEncoderValue))
                        valid = false;
                }
                else if (line.StartsWith("Azimuth: "))
                {
                    string val = line.Substring("Azimuth: ".Length);
                    if (val == "NaN" || !Double.TryParse(val, out savedAzimuth))
                        valid = false;
                }
            }

            if (valid) {
                if (Simulated)
                {
                    domeEncoder.Value = savedEncoderValue;
                    domeEncoder.Calibrate(Angle.AzFromDegrees(savedAzimuth));
                }
                else if (savedEncoderValue == domeEncoder.Value)
                {
                    domeEncoder.Calibrate(Angle.AzFromDegrees(savedAzimuth));
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "Restored calibration data from \"{0}\", Azimuth: {1}",
                    calibrationDataFilePath, savedAzimuth);
                #endregion
            }
        }
        #endregion

        public double ParkAzimuth
        {
            get
            {
                return _parkAzimuth;
            }
        }

        public bool DirectionMotorsAreActive
        {
            get
            {
                return leftPin.isOn || rightPin.isOn;
            }
        }

        public bool SyncVentWithShutter
        {
            get
            {
                return wisedomeshutter.syncVentWithShutter;
            }

            set
            {
                wisedomeshutter.syncVentWithShutter = value;
            }
        }

        public bool FarEnoughToMove(Angle targetAz)
        {
            Angle currentAz = Azimuth;
            Angle distance = currentAz.ShortestDistance(targetAz).angle;
            bool ret = distance > _minimalMove;

            if (!ret)
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                    $"Too short: distance: {distance.ToShortNiceString()} < minimal: {_minimalMove.ToShortNiceString()}");
                #endregion
            return ret;
        }

        #region Profile

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                _autoCalibrate = Convert.ToBoolean(driverProfile.GetValue(
                    Const.WiseDriverID.Dome, Const.ProfileName.Dome_AutoCalibrate, string.Empty, true.ToString()));

                _minimalMove = Angle.AzFromDegrees(Convert.ToDouble(driverProfile.GetValue(
                    Const.WiseDriverID.Dome, Const.ProfileName.Dome_MinimalMovement, string.Empty, "2.0")));
            }

            wisedomeshutter.ReadProfile();
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.Dome, Const.ProfileName.Dome_AutoCalibrate, _autoCalibrate.ToString());
            }
            wisedomeshutter.WriteProfile();
        }
        #endregion
    }

    public class ConnectionDigest
    {
        public enum ConnectionState { Working, Dead };

        private ConnectionState _state;
        public string Reason;

        public bool Working
        {
            get
            {
                return _state == ConnectionState.Working;
            }

            set
            {
                _state = value ? ConnectionState.Working : ConnectionState.Dead;
            }
        }
    }

    public class ShutterDigest
    {
        public ShutterState State;
        public string Reason;
        public string Status;
        public int PercentOpen;
        public int RangeCm;
        public TimeSpan TimeSinceLastReading;
        public bool WiFiIsWorking;
        public TimeSpan TimeSinceLastSuccessfullReading;
        public TimeSpan TimeSinceLastFailedReading;
        public int TotalCommunicationAttempts, FailedCommunicationAttempts;
        //public ConnectionDigest Connection;
        public string Tip;
    }

    public class DomeDigest
    {
        public double Azimuth;
        public double TargetAzimuth;
        public bool Calibrated;
        public string Status;
        public bool Vent;
        public bool Projector;
        public bool AtPark;
        public bool Slewing;
        public bool DirectionMotorsAreActive;
        public ShutterDigest Shutter;
        public string Tip;
    }
}
