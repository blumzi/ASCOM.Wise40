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
using ASCOM.Wise40.Hardware;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class WiseDome : WiseObject, IConnectable, IDisposable {

        private Version version = new Version(0, 2);

        private static volatile WiseDome _instance; // Singleton
        private static object syncObject = new object();
        private static WiseSite wisesite = WiseSite.Instance;
        private static bool _initialized = false;

        private WisePin leftPin, rightPin;
        private WisePin[] caliPins = new WisePin[3];
        private WisePin ventPin;
        private WisePin projectorPin;
        private static WiseDomeEncoder domeEncoder = WiseDomeEncoder.Instance;
        private List<IConnectable> connectables;
        private List<IDisposable> disposables;
        private bool _connected = false;
        private bool _calibrating = false;
        public bool _autoCalibrate = false;
        private bool _isStuck;
        private static Object _caliWriteLock = new object();

        public WiseDomeShutter wisedomeshutter = WiseDomeShutter.Instance;
        private static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        [FlagsAttribute] public enum DomeState {
            Idle = 0,
            MovingCW = (1 << 0),
            MovingCCW = (1 << 1),
            Calibrating = (1 << 2),
            Parking = (1 << 3),
        };
        private DomeState _state;

        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        private StuckPhase _stuckPhase;

        public enum Direction { None, CW, CCW };

        private uint _prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        public bool _shutterIsAlive
        {
            get
            {
                return wisedomeshutter.webClient != null && wisedomeshutter.webClient.Alive;
            }
        }

        public int ShutterPercent
        {
            get
            {
                return wisedomeshutter.Percent;
            }
        }

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
        private List<CalibrationPoint> calibrationPoints = new List<CalibrationPoint>();
        public const int TicksPerDomeRevolution = 1018;

        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        private const int simulatedEncoderTicksPerSecond = 6;   // As per Yftach's measurement

        public const double _parkAzimuth = 90.0;
        private Angle _simulatedStuckAz = new Angle(333.0);      // If targeted to this Az, we simulate dome-stuck (must be a valid az)

        private Angle _targetAz = null;

        private System.Threading.Timer _domeTimer;
        private System.Threading.Timer _movementTimer;
        private System.Threading.Timer _stuckTimer;

        private readonly int _movementTimeout = 2000;
        private readonly int _domeTimeout = 50;

        private bool _slaved = false;
        private bool _atPark = false;

        private Debugger debugger = Debugger.Instance;

        private AutoResetEvent internalArrivedAtAzEvent = new AutoResetEvent(false);
        private List<AutoResetEvent> externalArrivedAtAzEvents = new List<AutoResetEvent>();
        private static AutoResetEvent _foundCalibration = new AutoResetEvent(false);
        private static Hardware.Hardware hw = Hardware.Hardware.Instance;

        private static TraceLogger tl;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseDome()
        {
        }

        public WiseDome()
        {
        }

        public static WiseDome Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseDome();
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "Wise40 Dome";
            debugger.init();
            tl = new TraceLogger("", "Dome");
            tl.Enabled = debugger.Tracing;
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

                calibrationPoints.Add(new CalibrationPoint(caliPins[0], new Angle(254.6, Angle.Type.Az), 10 + 2 * caliPointsSpacing));
                calibrationPoints.Add(new CalibrationPoint(caliPins[1], new Angle(133.0, Angle.Type.Az), 10 + 1 * caliPointsSpacing));
                calibrationPoints.Add(new CalibrationPoint(caliPins[2], new Angle(18.0, Angle.Type.Az), 10 + 0 * caliPointsSpacing));

                ventPin = new WisePin("DomeVent", hw.domeboard, DigitalPortType.FirstPortA, 5, DigitalPortDirection.DigitalOut);
                projectorPin = new WisePin("DomeProjector", hw.domeboard, DigitalPortType.FirstPortA, 4, DigitalPortDirection.DigitalOut);

                domeEncoder.init();
                wisedomeshutter.init();

                List<WisePin> domePins = new List<WisePin> { leftPin, rightPin, ventPin, projectorPin };
                domePins.AddRange(caliPins);
                domePins.AddRange(wisedomeshutter.pins());

                connectables.AddRange(domePins);
                disposables.AddRange(domePins);
            }
            catch (WiseException e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: constructor caught: {0}.", e.Message);
            }

            try
            {
                leftPin.SetOff();
                rightPin.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) { }

            _calibrating = false;
            _state = DomeState.Idle;

            _domeTimer = new System.Threading.Timer(new TimerCallback(onDomeTimer));
            _domeTimer.Change(_domeTimeout, _domeTimeout);

            _movementTimer = new System.Threading.Timer(new TimerCallback(onMovementTimer));

            _stuckTimer = new System.Threading.Timer(new TimerCallback(onStuckTimer));

            wisesite.init();

            _initialized = true;

            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: init() done.");
        }

        private bool DomeStateIsOn(DomeState flag)
        {
            return (_state & flag) != 0;
        }

        private bool StateIsOff(DomeState flag)
        {
            return !DomeStateIsOn(flag);
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
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Dome: Connected Get", _connected.ToString());
                return _connected;
            }

            set
            {
                tl.LogMessage("Dome: Connected Set", value.ToString());

                if (value == true)
                {
                    RestoreCalibrationData();

                    if (!Calibrated && _autoCalibrate)
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
        private Angle inertiaAngle(Angle az)
        {
            return new Angle(2 * (360.0 / TicksPerDomeRevolution));
        }

        /// <summary>
        /// Checks if we're close enough to a given Azimuth
        /// </summary>
        /// <param name="there"></param>
        /// <returns></returns>
        private bool arriving(Angle there)
        {
            string message = string.Format("WiseDome:arriving: at {0} target {1}: ", Azimuth, there);

            if (!DomeIsMoving)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message + "Dome is not moving => false");
                #endregion
                return false;
            }

            Angle az = Azimuth;
            ShortestDistanceResult shortest = _instance.Azimuth.ShortestDistance(there);
            Angle inertial = inertiaAngle(there);

            if (DomeStateIsOn(DomeState.MovingCW) && (shortest.direction == Const.AxisDirection.Decreasing))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, message + "direction changed CW to CCW => true", az, there);
                #endregion
                return true;
            }
            else if (DomeStateIsOn(DomeState.MovingCCW) && (shortest.direction == Const.AxisDirection.Increasing))
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
                    string.Format("shortest.Angle {0} <= inertiaAngle({1}) => true", shortest.angle, inertial));
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
                var ret = DomeStateIsOn(DomeState.MovingCCW) | DomeStateIsOn(DomeState.MovingCW);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "DomeIsMoving: {0}", ret);
                #endregion
                return ret;
            }
        }

        /// <summary>
        /// The dome timer is always enabled, at 100 millisec intervals.
        /// </summary>
        /// <param name="state"></param>
        private void onDomeTimer(object state)
        {
            CalibrationPoint cp;

            if ((cp = AtCaliPoint) != null)
            {
                domeEncoder.Calibrate(cp.az);
                if (_calibrating)
                {
                    _calibrating = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                        "WiseDome: Setting _foundCalibration[{0}] == {1} ...", calibrationPoints.IndexOf(cp), cp.az.ToNiceString());
                    #endregion
                    Stop();
                    Thread.Sleep(2000);     // settle down
                    _foundCalibration.Set();
                }
            }

            if (_targetAz != null && arriving(_targetAz))
            {
                Stop();
                _targetAz = null;

                if (DomeStateIsOn(DomeState.Parking))
                {
                    UnsetDomeState(DomeState.Parking);
                    AtPark = true;
                }

                var waiters = new List<AutoResetEvent>() { internalArrivedAtAzEvent };
                waiters.AddRange(externalArrivedAtAzEvents);
                foreach (var e in waiters)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome,
                        "WiseDome: Setting arrivedAtAzEvent (#{0})", e.GetHashCode());
                    #endregion
                    e.Set();
                }
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
        /// The movement timer is activated when the dome starts moving either CW or CCW, and 
        ///   gets disabled by Stop().
        ///   
        /// The handler decides whether the dome encoder has changed, or the dome seems to be stuck.
        /// </summary>
        /// <param name="state"></param>
        private void onMovementTimer(object state)
        {
            uint currTicks, deltaTicks;
            const int leastExpectedTicks = 2;  // least number of Ticks expected to change in two seconds

            SaveCalibrationData();

            // the movementTimer should not be Enabled unless the dome is moving
            if (_isStuck || !DomeIsMoving)
                return;

            deltaTicks = 0;
            currTicks = domeEncoder.Value;

            if (currTicks == _prevTicks)
                _isStuck = true;
            else
            {
                if (DomeStateIsOn(DomeState.MovingCW))
                {
                    if (_prevTicks > currTicks)
                        deltaTicks = _prevTicks - currTicks;
                    else
                        deltaTicks = domeEncoder.Ticks - currTicks + _prevTicks;

                    if (deltaTicks < leastExpectedTicks)
                        _isStuck = true;
                }
                else if (DomeStateIsOn(DomeState.MovingCCW))
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


        private void onStuckTimer(object state)
        {
            DateTime rightNow;
            WisePin backwardPin, forwardPin;

            rightNow = DateTime.Now;

            if (DateTime.Compare(rightNow, nextStuckEvent) < 0)
                return;

            forwardPin = DomeStateIsOn(DomeState.MovingCCW) ? leftPin : rightPin;
            backwardPin = DomeStateIsOn(DomeState.MovingCCW) ? rightPin : leftPin;

            switch (_stuckPhase) {
                case StuckPhase.NotStuck:              // Stop, let the wheels cool down
                    forwardPin.SetOff();
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.FirstStop;
                    nextStuckEvent = rightNow.AddMilliseconds(10000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: stuck: {0}, phase1: stopped moving, letting wheels cool for 10 seconds", _instance.Azimuth);
                    #endregion

                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: stuck: {0}, phase2: going backwards for 2 seconds", _instance.Azimuth);
                    #endregion
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: stuck: {0}, phase3: stopping for 2 seconds", _instance.Azimuth);
                    #endregion
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    _isStuck = false;
                    _stuckTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    nextStuckEvent = rightNow.AddYears(100);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: stuck: {0}, phase4: resumed original motion", _instance.Azimuth);
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

        public void MoveRight()
        {
            StartMovingCW();
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
                return;
            }

            SetDomeState(DomeState.MovingCCW);
            domeEncoder.setMovement(Direction.CCW);
            _movementTimer.Change(0, _movementTimeout);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: Started moving CCW");
            #endregion
        }

        public void MoveLeft()
        {
            StartMovingCCW();
        }

        public void Stop()
        {
            int tries;

            #region debug
            string dbg = string.Format("WiseDome:Stop Starting to stop (encoder: {0}) ", domeEncoder.Value);
            if (Calibrated)
                dbg += string.Format(", az: {0}", Azimuth);
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
            if (Calibrated)
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome:Stop Fully stopped at az: {0} (encoder: {1}) after {2} tries",
                    Azimuth, domeEncoder.Value, tries + 1);
            else
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome:Stop Fully stopped (not calibrated) (encoder: {0}) after {1} tries",
                    domeEncoder.Value, tries + 1);
            #endregion

            activityMonitor.EndActivity(ActivityMonitor.Activity.Dome);
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
                Angle ret;

                if (!Calibrated)
                {
                    if (_autoCalibrate)
                        StartFindingHome();
                    else
                        return Angle.FromDegrees(double.NaN, Angle.Type.Az);
                }

                ret = domeEncoder.Azimuth;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: Azimuth: get => {0}", ret.ToNiceString());
                #endregion
                #region trace
                tl.LogMessage("Dome: Azimuth Get", ret.ToString());
                #endregion
                return ret;
            }

            set
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: Azimuth: set({0})", value);
                #endregion
                #region trace
                tl.LogMessage("Dome: Azimuth Set", value.ToString());
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
                    projectorPin.SetOn();
                else
                    projectorPin.SetOff();
            }
        }

        public void StartFindingHome()
        {
            if (Calibrated)
            {
                //
                // Consider safety only AFTER calibration, otherwise we ca never produce
                //  an Azimuth reading
                //
                if (ShutterIsMoving)
                {
                    tl.LogMessage("WiseDome:StartFindingHome", "Cannot move, shutter is active.");
                    throw new ASCOM.InvalidOperationException("Cannot move, shutter is active!");
                }

                if (wisesite.safeToOperate != null && !wisesite.safeToOperate.IsSafe)
                    throw new ASCOM.InvalidOperationException(wisesite.safeToOperate.Action("unsafereasons", ""));
            }

            AtPark = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome:StartFindingHome: started");
            #endregion
            _calibrating = true;

            activityMonitor.StartActivity(ActivityMonitor.Activity.Dome);
            if (Calibrated)
            {
                List<ShortestDistanceResult> distanceToCaliPoints = new List<ShortestDistanceResult>();
                foreach (var cp in calibrationPoints)
                    distanceToCaliPoints.Add(_instance.Azimuth.ShortestDistance(cp.az));

                ShortestDistanceResult closest = new ShortestDistanceResult(new Angle(360.0, Angle.Type.Az), Const.AxisDirection.None);
                foreach (var res in distanceToCaliPoints)
                    if (res.angle < closest.angle)
                        closest = res;

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
            activityMonitor.EndActivity(ActivityMonitor.Activity.Dome);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: FindHomePoint: _foundCalibration was Set()");
            #endregion
        }

        public void SlewToAzimuth(double degrees)
        {
            if (Slaved)
                throw new InvalidOperationException("Cannot SlewToAzimuth, dome is Slaved");

            if (degrees < 0 || degrees >= 360)
                throw new InvalidValueException(string.Format("Invalid azimuth: {0}, must be >= 0 and < 360", degrees));

            if (ShutterIsMoving)
            {
                tl.LogMessage("Dome: SlewToAzimuth", "Denied, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot move, shutter is active!");
            }

            if (wisesite.safeToOperate != null && !wisesite.safeToOperate.IsSafe)
                throw new ASCOM.InvalidOperationException(wisesite.safeToOperate.Action("unsafereasons", ""));

            Angle toAng = new Angle(degrees, Angle.Type.Az);

            tl.LogMessage("Dome: SlewToAzimuth", toAng.ToString());

            activityMonitor.StartActivity(ActivityMonitor.Activity.Dome);

            if (!Calibrated)
            {
                if (_autoCalibrate)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: SlewToAzimuth: {0}, not calibrated, _autoCalibrate == true, calling FindHomePoint", toAng);
                    #endregion
                    StartFindingHome();
                } else
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: SlewToAzimuth: {0}, not calibrated, _autoCalibrate == false, throwing InvalidOperationException", toAng.ToNiceString());
                    #endregion
                    throw new ASCOM.InvalidOperationException("Not calibrated!");
                }
            }

            _targetAz = toAng;
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
            debugger.WriteLine(Debugger.DebugLevel.DebugDome, "WiseDome: SlewToAzimuth: {0} => {1} (dist: {2}), moving {3}",
                _instance.Azimuth, toAng, shortest.angle, shortest.direction);
            #endregion

            if (Simulated && _targetAz == _simulatedStuckAz)
            {
                Angle epsilon = new Angle(5.0);
                Angle stuckAtAz = (shortest.direction == Const.AxisDirection.Increasing) ? _targetAz - epsilon : _targetAz + epsilon;

                domeEncoder.SimulateStuckAt(stuckAtAz);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "WiseDome: Dome encoder will simulate stuck at {0}", stuckAtAz);
                #endregion
            }
        }

        public bool AtPark
        {
            get
            {
                tl.LogMessage("Dome: AtPark Get", _atPark.ToString());
                return _atPark;
            }

            set
            {
                tl.LogMessage("Dome: AtPark Set", value.ToString());
                _atPark = value;
            }
        }

        public string Status
        {
            get
            {
                string ret = string.Empty;

                if (!DomeIsMoving)
                    return ret;

                if (DomeStateIsOn(DomeState.MovingCW))
                    ret = "Moving CW";
                else if (DomeStateIsOn(DomeState.MovingCCW))
                    ret = "Moving CCW";

                if (_targetAz != null)
                    ret += string.Format(" to {0}", _targetAz.ToNiceString());

                if (DomeStateIsOn(DomeState.Calibrating))
                    ret += " (calibrating)";
                if (DomeStateIsOn(DomeState.Parking))
                    ret += " (parking)";

                return ret;
            }
        }

        public void Dispose()
        {
            wisedomeshutter.Dispose();
            leftPin.SetOff();
            rightPin.SetOff();

            Vent = false;
        }

        public bool Slewing
        {
            get
            {
                if (Slaved)
                    throw new InvalidOperationException("Cannot get Slewing while dome is Slaved");

                bool ret = DomeIsMoving || ShutterIsMoving;

                tl.LogMessage("Dome: Slewing Get", ret.ToString());
                return ret;
            }
        }

        public bool Slaved
        {
            get
            {
                tl.LogMessage("Dome: Slaved Get", _slaved.ToString());
                return _slaved;
            }

            set
            {
                tl.LogMessage("Dome: Slaved Set", value.ToString());
                _slaved = value;
            }
        }

        public void Park()
        {
            if (Slaved)
                throw new InvalidOperationException("Cannot Park, dome is Slaved");

            if (ShutterIsMoving)
            {
                tl.LogMessage("Dome: Park", "Cannot Park, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot Park, shutter is active!");
            }

            if (!Calibrated && !_autoCalibrate)
            {
                tl.LogMessage("Dome: Park", string.Format("Dome: Park", "Cannot Park, not calibrated and _autoCalibrate == {0}.", _autoCalibrate.ToString()));
                throw new ASCOM.InvalidOperationException("Cannot Park, not calibrated!");
            }

            tl.LogMessage("Dome: Park", "");

            SetDomeState(DomeState.Parking);

            AtPark = false;
            SlewToAzimuth(_parkAzimuth);
        }

        public void OpenShutter(bool bypassSafety = false)
        {
            if (Slewing)
                throw new InvalidOperationException("Dome is slewing!");

            if (!bypassSafety && (wisesite.safeToOperate != null && !wisesite.safeToOperate.IsSafe))
                throw new InvalidOperationException(wisesite.safeToOperate.CommandString("unsafeReasons", false));

            if (activityMonitor.Active(ActivityMonitor.Activity.ShuttingDown))
                throw new InvalidOperationException("Observatory is shutting down!");

            wisedomeshutter.Stop();
            wisedomeshutter.StartOpening();
        }

        public void CloseShutter()
        {
            if (Slewing)
                throw new InvalidOperationException("Dome is slewing!");
            
            wisedomeshutter.Stop();
            wisedomeshutter.StartClosing();
        }

        public void AbortSlew()
        {
            #region trace
            tl.LogMessage("Dome: AbortSlew", "");
            #endregion
            Stop();
        }

        public double Altitude
        {
            get
            {
                #region trace
                tl.LogMessage("Dome: Altitude Get", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("Altitude", false);
            }
        }

        public bool AtHome
        {
            get
            {
                bool atHome = (AtCaliPoint == calibrationPoints[0]);

                tl.LogMessage("Dome: AtHome Get", atHome.ToString());
                return atHome;
            }
        }

        public bool CanFindHome
        {
            get
            {
                #region trace
                tl.LogMessage("Dome: CanFindHome Get", true.ToString());
                #endregion
                return true;
            }
        }

        public bool CanPark
        {
            get
            {
                #region trace
                tl.LogMessage("Dome: CanPark Get", true.ToString());
                #endregion
                return true;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                #region trace
                tl.LogMessage("Dome: CanSetAltitude Get", false.ToString());
                #endregion
                return false;
            }
        }

        public bool CanSetAzimuth
        {
            get
            {
                tl.LogMessage("Dome: CanSetAzimuth Get", true.ToString());
                return true;
            }
        }

        public bool CanSetPark
        {
            get
            {
                tl.LogMessage("Dome: CanSetPark Get", false.ToString());
                return false;
            }
        }

        public bool CanSetShutter
        {
            get
            {
                tl.LogMessage("Dome: CanSetShutter Get", true.ToString());
                return true;
            }
        }

        public bool CanSlave
        {
            get
            {
                tl.LogMessage("Dome: CanSlave Get", true.ToString());
                return true;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                tl.LogMessage("Dome: CanSyncAzimuth Get", true.ToString());
                return true;
            }
        }

        public void SyncToAzimuth(double degrees)
        {
            Angle ang = new Angle(degrees, Angle.Type.Az);

            if (degrees < 0.0 || degrees >= 360.0)
                throw new InvalidValueException(string.Format("Cannot SyncToAzimuth({0}), must be >= 0 and < 360", ang));
            #region trace
            tl.LogMessage("Dome: SyncToAzimuth", ang.ToString());
            #endregion
            _instance.Azimuth = ang;
        }

        public void SlewToAltitude(double Altitude)
        {
            tl.LogMessage("Dome: SlewToAltitude", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SlewToAltitude");
        }

        public void SetPark()
        {
            tl.LogMessage("Dome: SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public ShutterState ShutterState
        {
            get
            {
                ShutterState ret = wisedomeshutter.State;
                #region trace
                tl.LogMessage("Dome: ShutterState get", ret.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDome, "ShutterState - get: {0}", ret.ToString());
                #endregion
                return ret;
            }
        }

        public string ShutterStatus
        {
            get
            {
                string ret = "unknown";
                int percent = wisedomeshutter.Percent;

                switch (ShutterState)
                {
                    case ShutterState.shutterClosed:
                        ret = "Shutter is closed";
                        break;
                    case ShutterState.shutterClosing:
                    case ShutterState.shutterOpening:
                    case ShutterState.shutterOpen:
                        ret = "Shutter is open";
                        break;
                    case ShutterState.shutterError:
                        ret = "Shutter is in error!";
                        break;
                }
                if (percent != -1)
                    ret += string.Format(" ({0}% open)", percent);
                return ret;
            }
        }


        private static ArrayList supportedActions = new ArrayList() {
            "dome:get-projector",
            "dome:set-projector",
            "dome:get-vent",
            "dome:set-vent",
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

            switch (actionName)
            {
                case "dome:get-projector":
                    return Projector.ToString().ToLower();

                case "dome:set-projector":
                    switch (param)
                    {
                        case "on":
                            Projector = true;
                            break;

                        case "off":
                            Projector = false;
                            break;

                        default:
                            return "bad parameter";
                    }
                    return "ok";

                case "dome:get-vent":
                    return Vent.ToString().ToLower();

                case "dome:set-vent":
                    switch (param)
                    {
                        case "on":
                            Vent = true;
                            break;

                        case "off":
                            Vent = false;
                            break;

                        default:
                            return "bad parameter";
                    }
                    return "ok";

                default:
                    throw new ASCOM.ActionNotImplementedException(
                        "Action " + actionName + " is not implemented by this driver");
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        private void CheckConnected(string message)
        {
            if (!Connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }


        public string Description
        {
            get
            {
                string description = "Wise40 Dome";

                tl.LogMessage("Dome: Description Get", description);
                return description;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "First draft, Version: " + DriverVersion;
                tl.LogMessage("Dome: DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("Dome: DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                tl.LogMessage("Dome: InterfaceVersion Get", "2");
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
            lines.Add(string.Format("# WiseDome calibration data, generated automatically, please don't change!"));
            lines.Add("#");
            lines.Add(string.Format("# Saved: {0}", now.ToLocalTime()));
            lines.Add("#");
            lines.Add(string.Format("Encoder: {0}", domeEncoder.Value));
            lines.Add(string.Format("Azimuth: {0}", Azimuth.Degrees.ToString()));

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
                    domeEncoder.Calibrate(Angle.FromDegrees(savedAzimuth, Angle.Type.Az));
                }
                else if (savedEncoderValue == domeEncoder.Value)
                {
                    domeEncoder.Calibrate(Angle.FromDegrees(savedAzimuth, Angle.Type.Az));
                }
                #region trace
                tl.LogMessage("Dome", string.Format("Restored calibration data from \"{0}\", Azimuth: {1}", calibrationDataFilePath, savedAzimuth));
                #endregion
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

        public bool MotorsAreActive
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
                return wisedomeshutter._syncVentWithShutter;
            }

            set
            {
                wisedomeshutter._syncVentWithShutter = value;
            }
        }

        #region Profile

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        public void ReadProfile()
        {
            bool defaultSyncVentWithShutter = (wisesite.OperationalMode == WiseSite.OpMode.WISE) ? false : true;

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
            {
                _autoCalibrate = Convert.ToBoolean(driverProfile.GetValue(Const.wiseDomeDriverID, Const.ProfileName.Dome_AutoCalibrate, string.Empty, true.ToString()));
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
                driverProfile.WriteValue(Const.wiseDomeDriverID, Const.ProfileName.Dome_AutoCalibrate, _autoCalibrate.ToString());
            }
            wisedomeshutter.WriteProfile();
        }
        #endregion
    }
}
