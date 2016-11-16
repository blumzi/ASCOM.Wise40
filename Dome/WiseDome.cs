using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.SafeToOperate;

namespace ASCOM.Wise40
{
    public class WiseDome : WiseObject, IConnectable, IDisposable {

        private static readonly WiseDome instance = new WiseDome(); // Singleton
        private static WiseSite wisesite = WiseSite.Instance;
        private static bool _initialized = false;

        private WisePin leftPin, rightPin;
        private WisePin openPin, closePin;
        private WisePin homePin, ventPin;
        private static WiseDomeEncoder domeEncoder = WiseDomeEncoder.Instance;
        private List<IConnectable> connectables;
        private List<IDisposable> disposables;
        private bool _connected = false;
        private bool _calibrating = false;
        public bool _autoCalibrate = false;
        private bool _isStuck;


        [FlagsAttribute] public enum DomeState {
            Idle = 0,
            MovingCW = (1 << 0),
            MovingCCW = (1 << 1),
            Calibrating = (1 << 2),
            Parking = (1 << 3),
            //AllMovements = MovingCCW|MovingCW|Parking|Calibrating,
        };
        private DomeState _state;        
        private ShutterState _shutterState;

        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        private StuckPhase _stuckPhase;

        public enum Direction { None, CW, CCW };

        private uint _prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        private Angle _homePointAzimuth = new Angle(254.6, Angle.Type.Az);
        public const int TicksPerDomeRevolution = 1018;

        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        private const int simulatedEncoderTicksPerSecond = 6;   // As per Yftach's measurement

        public const double _parkAzimuth = 90.0;
        private Angle _simulatedStuckAz = new Angle(333.0);      // If targeted to this Az, we simulate dome-stuck (must be a valid az)

        private Angle _targetAz = null;

        private System.Threading.Timer _domeTimer;
        private System.Threading.Timer _shutterTimer;
        private System.Threading.Timer _movementTimer;
        private System.Threading.Timer _stuckTimer;

        private int _shutterTimeout;
        private readonly int _movementTimeout = 2000;
        private readonly int _domeTimeout = 100;
        
        private bool _slaved = false;
        private bool _atPark = false;

        private Debugger debugger = Debugger.Instance;

        private static AutoResetEvent _arrivedAtAzEvent;
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
                return instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "Wise40 Dome";
            tl = new TraceLogger("", "Dome");
            tl.Enabled = debugger.Tracing;
            ReadProfile();
            hw.init();

            try {
                connectables = new List<IConnectable>();
                disposables = new List<IDisposable>();

                openPin = new WisePin("ShutterOpen", hw.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                closePin = new WisePin("ShutterClose", hw.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut);
                leftPin = new WisePin("DomeLeft", hw.domeboard, DigitalPortType.FirstPortA, 2, DigitalPortDirection.DigitalOut);
                rightPin = new WisePin("DomeRight", hw.domeboard, DigitalPortType.FirstPortA, 3, DigitalPortDirection.DigitalOut);

                homePin = new WisePin("DomeCalib", hw.domeboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalIn);
                ventPin = new WisePin("DomeVent", hw.teleboard, DigitalPortType.ThirdPortCL, 0, DigitalPortDirection.DigitalOut);

                domeEncoder = WiseDomeEncoder.Instance;
                domeEncoder.init();

                connectables.Add(openPin);
                connectables.Add(closePin);
                connectables.Add(leftPin);
                connectables.Add(rightPin);
                connectables.Add(homePin);
                connectables.Add(ventPin);
                connectables.Add(domeEncoder);

                disposables.Add(openPin);
                disposables.Add(closePin);
                disposables.Add(leftPin);
                disposables.Add(rightPin);
                disposables.Add(homePin);
                disposables.Add(ventPin);
            }
            catch (WiseException e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseDome: constructor caught: {0}.", e.Message);
            }

            openPin.SetOff();
            closePin.SetOff();
            leftPin.SetOff();
            rightPin.SetOff();

            _calibrating = false;
            _state = DomeState.Idle;
            _shutterState = ShutterState.shutterClosed;
            
            _domeTimer = new System.Threading.Timer(new TimerCallback(onDomeTimer));
            _domeTimer.Change(_domeTimeout, _domeTimeout);
            
            _shutterTimer = new System.Threading.Timer(new TimerCallback(onShutterTimer));
            _shutterTimeout = (Simulated ? 2 : 25 ) * 1000;
            
            _movementTimer = new System.Threading.Timer(new TimerCallback(onMovementTimer));
            
            _stuckTimer = new System.Threading.Timer(new TimerCallback(onStuckTimer));

            wisesite.init();

            RestoreCalibrationData();

            _initialized = true;

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: init() done.");
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
            _arrivedAtAzEvent = e;
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
            if (! DomeIsMoving)
                return false;

            ShortestDistanceResult shortest = instance.Azimuth.ShortestDistance(there);
            return shortest.angle <= inertiaAngle(there);
        }

        private bool DomeIsMoving
        {
            get
            {
                return DomeStateIsOn(DomeState.MovingCCW) | DomeStateIsOn(DomeState.MovingCW);
            }
        }
        
        private void onDomeTimer(object state)
        {
            if (AtCaliPoint)
            {
                if (_calibrating)
                {
                    Stop();
                    Thread.Sleep(2000);     // settle down
                    _calibrating = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Setting _foundCalibration ...");
                    #endregion
                    _foundCalibration.Set();
                }
                domeEncoder.Calibrate(_homePointAzimuth);
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

                _arrivedAtAzEvent.Set();
            }            
        }
        
        private bool ShutterIsMoving
        {
            get
            {
                return _shutterState == ShutterState.shutterOpening || _shutterState == ShutterState.shutterClosing;
            }
        }
        private void onShutterTimer(object state)
        {
            if (ShutterIsMoving)
            {
                _shutterTimer.Change(0, 0);
                ShutterStop();
            }
        }
        
        private void onMovementTimer(object state)
        {
            uint currTicks, deltaTicks;
            const int leastExpectedTicks = 2;  // least number of Ticks expected to change in two seconds
            
            // the movementTimer should not be Enabled unless the dome is moving
            if (_isStuck || ! DomeIsMoving)
                return;

            deltaTicks = 0;
            currTicks  = domeEncoder.Value;

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

            SaveCalibrationData();
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: stuck: {0}, phase1: stopped moving, letting wheels cool for 10 seconds", instance.Azimuth);
                    #endregion

                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: stuck: {0}, phase2: going backwards for 2 seconds", instance.Azimuth);
                    #endregion
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: stuck: {0}, phase3: stopping for 2 seconds", instance.Azimuth);
                    #endregion
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    _isStuck = false;
                    _stuckTimer.Change(0, 0);
                    nextStuckEvent = rightNow.AddYears(100);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: stuck: {0}, phase4: resumed original motion", instance.Azimuth);
                    #endregion
                    break;
            }
        }


        public void StartMovingCW()
        {
            AtPark = false;

            leftPin.SetOff();
            rightPin.SetOn();
            SetDomeState(DomeState.MovingCW);
            domeEncoder.setMovement(Direction.CW);
            _movementTimer.Change(0, _movementTimeout);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Started moving CW");
            #endregion
        }

        public void MoveRight()
        {
            StartMovingCW();
        }

        public void StartMovingCCW()
        {
            AtPark = false;

            rightPin.SetOff();
            leftPin.SetOn();
            SetDomeState(DomeState.MovingCCW);
            domeEncoder.setMovement(Direction.CCW);
            _movementTimer.Change(0, _movementTimeout);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Started moving CCW");
            #endregion
        }

        public void MoveLeft()
        {
            StartMovingCCW();
        }

        public void Stop()
        {
            rightPin.SetOff();
            leftPin.SetOff();
            UnsetDomeState(DomeState.MovingCCW|DomeState.MovingCW);
            _movementTimer.Change(0, 0);
            domeEncoder.setMovement(Direction.None);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Stopped");
            #endregion
        }

        public void StartOpeningShutter()
        {
            openPin.SetOn();
            _shutterState = ShutterState.shutterOpening;
            _shutterTimer.Change(_shutterTimeout, _shutterTimeout);
        }

        public void StartClosingShutter()
        {
            closePin.SetOn();
            _shutterState = ShutterState.shutterClosing;
            _shutterTimer.Change(_shutterTimeout, _shutterTimeout);
        }

        public void ShutterStop()
        {
            switch (_shutterState)
            {
                case ShutterState.shutterOpening:
                    openPin.SetOff();
                    _shutterState = ShutterState.shutterOpen;
                    break;

                case ShutterState.shutterClosing:
                    closePin.SetOff();
                    _shutterState = ShutterState.shutterClosed;
                    break;
            }
        }

        public bool AtCaliPoint
        {
            get
            {
                return (Simulated) ? domeEncoder.Value == 10 : homePin.isOff;
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
                        FindHome();
                    else
                        return Angle.FromDegrees(double.NaN, Angle.Type.Az);
                }

                ret = domeEncoder.Azimuth;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: [{0}] Azimuth: get => {1}", this.GetHashCode(), ret);
                #endregion
                #region trace
                tl.LogMessage("Dome: Azimuth Get", ret.ToString());
                #endregion
                return ret;
            }

            set
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Azimuth: set({0})", value);
                #endregion
                #region trace
                tl.LogMessage("Dome: Azimuth Set", value.ToString());
                #endregion
                domeEncoder.Calibrate(value);
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

        public void FindHome()
        {
            if (ShutterIsMoving)
            {
                tl.LogMessage("Dome: FindHome", "Cannot FindHome, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot FindHome, shutter is active!");
            }

            if (wisesite.computerControl != null && !wisesite.computerControl.IsSafe)
                throw new ASCOM.InvalidOperationException("Computer control is OFF!");

            AtPark = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: FindHomePoint: started");
            #endregion
            _calibrating = true;

            if (Calibrated)
            {
                ShortestDistanceResult shortest = instance.Azimuth.ShortestDistance(_homePointAzimuth);

                switch (shortest.direction) {
                    case Const.AxisDirection.Decreasing: StartMovingCCW(); break ;
                    case Const.AxisDirection.Increasing:  StartMovingCW(); break;
                }
            } else
                StartMovingCCW();
            SetDomeState(DomeState.Calibrating);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: FindHomePoint: waiting for _foundCalibration ...");
            #endregion
            _foundCalibration.WaitOne();
            UnsetDomeState(DomeState.Calibrating);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: FindHomePoint: _foundCalibration was Set()");
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

            if (wisesite.computerControl != null && !wisesite.computerControl.IsSafe)
                throw new ASCOM.InvalidOperationException("Wise40 computer control is OFF!");

            Angle toAng = new Angle(degrees, Angle.Type.Az);

            tl.LogMessage("Dome: SlewToAzimuth", toAng.ToString());

            if (!Calibrated)
            {
                if (_autoCalibrate)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: SlewToAzimuth: {0}, not calibrated, _autoCalibrate == true, calling FindHomePoint", toAng);
                    #endregion
                    FindHome();
                } else
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: SlewToAzimuth: {0}, not calibrated, _autoCalibrate == false, throwing InvalidOperationException", toAng.ToNiceString());
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
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: SlewToAzimuth: {0} => {1} (dist: {2}), moving {3}",
                instance.Azimuth, toAng, shortest.angle, shortest.direction);
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

        public void FullClose()
        {
            StartClosingShutter();
        }

        public void FullOpen()
        {
            StartOpeningShutter();
        }

        public void Dispose()
        {
            openPin.SetOff();
            closePin.SetOff();
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

            tl.LogMessage("Dome: Park", "");
            
            SetDomeState(DomeState.Parking);
            if (!Calibrated)
                FindHome();

            AtPark = false;
            SlewToAzimuth(_parkAzimuth);
        }

        public void OpenShutter()
        {
            string err = null;

            if (Slewing)
                err += "Cannot OpenShutter, dome is slewing!";

            if (wisesite.safeToOpen != null && !wisesite.safeToOpen.IsSafe)
                err += "Not safeToOpen: " + wisesite.safeToOpen.CommandString("unsafeReasons", false);

            if (err == null)
            {
                #region trace
                tl.LogMessage("Dome: OpenShutter", err);
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "WiseDome: OpenShutter: opening shutter: ");
                #endregion
                ShutterStop();
                StartOpeningShutter();
                Vent = true;
            } else
            {
                #region trace
                tl.LogMessage("Dome: OpenShutter", "");
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "WiseDome: OpenShutter: Cannot open shutter: " + err);
                #endregion
                _shutterState = ShutterState.shutterError;
            }
        }

        public void CloseShutter()
        {
            string err = null;

            if (Slewing)
                err = "Cannot CloseShutter, dome is slewing!";

            if (err == null)
            {
                #region trace
                tl.LogMessage("Dome: CloseShutter", "");
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "CloseShutter: started closing");
                #endregion
                ShutterStop();
                StartClosingShutter();
                Vent = false;
            } else
            {
                #region trace
                tl.LogMessage("Dome: CloseShutter", err);
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "CloseShutter: " + err);
                #endregion
                _shutterState = ShutterState.shutterError;
            }
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
                bool atHome = AtCaliPoint;

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
            instance.Azimuth = ang;
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

        public ASCOM.DeviceInterface.ShutterState ShutterState
        {
            get
            {
                ASCOM.DeviceInterface.ShutterState ret = _shutterState;
                tl.LogMessage("Dome: ShutterState get", ret.ToString());
                return ret;
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("Dome: SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
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
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "First draft, Version: " + string.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("Dome: DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
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

        private readonly string calibrationDataFilePath = "c:/temp/DomeCalibrationData.txt";

        private void SaveCalibrationData()
        {
            if (!Calibrated)
                return;

            List<string> lines = new List<string>();
            DateTime now = DateTime.Now;

            lines.Add("#");
            lines.Add(string.Format("# WiseDome calibration data, generated automatically, please don't change!"));
            lines.Add(string.Format("# Saved: {0} at {1}", now.ToLongDateString(), now.ToLongTimeString()));
            lines.Add("#");
            lines.Add(string.Format("Encoder: {0}", domeEncoder.Value));
            lines.Add(string.Format("Azimuth: {0}", Azimuth.Degrees.ToString()));

            System.IO.File.WriteAllLines(calibrationDataFilePath, lines);
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
                    domeEncoder.Calibrate(Angle.FromDegrees(savedAzimuth));
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Restored calibration data from \"{0}\", Azimuth: {1}",
                        calibrationDataFilePath, savedAzimuth);
                    #endregion
                }
                else if (savedEncoderValue == domeEncoder.Value)
                {
                    domeEncoder.Calibrate(Angle.FromDegrees(savedAzimuth));
                    #region trace
                    tl.LogMessage("Dome", string.Format("Restored calibration data from \"{0}\", Azimuth: {1}", calibrationDataFilePath, savedAzimuth));
                    #endregion
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Restored calibration data from \"{0}\", Azimuth: {1}",
                        calibrationDataFilePath, savedAzimuth);
                    #endregion
                }
            }

            try
            {
                System.IO.File.Delete(calibrationDataFilePath);
            } catch
            {

            }
        }
        #endregion

        #region Profile
        internal static string autoCalibrateProfileName = "AutoCalibrate";
        internal static string driverID = "ASCOM.Wise40.Dome";

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                _autoCalibrate = Convert.ToBoolean(driverProfile.GetValue(driverID, autoCalibrateProfileName, string.Empty, "false"));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, autoCalibrateProfileName, _autoCalibrate.ToString());
            }
        }
        #endregion
    }
}
