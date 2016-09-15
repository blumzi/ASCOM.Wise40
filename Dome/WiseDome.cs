using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.SafeToOpen;

namespace ASCOM.Wise40
{
    public class WiseDome : IConnectable, IDisposable {

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
        private bool _ventIsOpen;
        private bool _isStuck;

        public enum DomeState { Idle, MovingCW, MovingCCW, AutoShutdown };
        public enum ShutterState { Idle, Opening, Open, Closing, Closed };
        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        public enum Direction { CW, None, CCW };

        private DomeState _state;
        private ShutterState _shutterState;

        private StuckPhase _stuckPhase;
        private uint _prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        private Angle _homePointAzimuth = new Angle(254.6, Angle.Type.Az);
        public const int TicksPerDomeRevolution = 1018;

        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        private const int simulatedEncoderTicksPerSecond = 6;   // As per Yftach's measurement

        public const double _parkAzimuth = 90.0;
        private Angle _simulatedStuckAz = new Angle(333.0);      // If targeted to this Az, we simulate dome-stuck (must be a valid az)
        private bool _parking = false;

        private Angle _targetAz = null;

        private System.Timers.Timer _domeTimer;
        private System.Timers.Timer _shutterTimer;
        private System.Timers.Timer _movementTimer;
        private System.Timers.Timer _stuckTimer;

        private bool _simulated = Environment.MachineName.ToLower() != "dome-ctlr";
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

        public void SetLogger(TraceLogger logger)
        {
            tl = logger;
        }

        public void init()
        {
            if (_initialized)
                return;

            tl = new TraceLogger();

            using (Profile profile = new Profile())
            {
                string driverID = "ASCOM.Wise40.Dome";

                profile.DeviceType = "Dome";
                debugger.Level = Convert.ToUInt32(profile.GetValue(driverID, "Debug Level", string.Empty, "0"));
                tl.Enabled = Convert.ToBoolean(profile.GetValue(driverID, "Trace Level", string.Empty, "false"));
                _autoCalibrate = Convert.ToBoolean(profile.GetValue(driverID, "AutoCalibrate", string.Empty, "false"));
            }

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
            _ventIsOpen = false;
            _state = DomeState.Idle;
            _shutterState = ShutterState.Closed;

            _domeTimer = new System.Timers.Timer(100);   // runs every 100 millis
            _domeTimer.Elapsed += onDomeTimer;
            _domeTimer.Enabled = true;

            if (_simulated)
                _shutterTimer = new System.Timers.Timer(2 * 1000);
            else
                _shutterTimer = new System.Timers.Timer(25 * 1000);
            _shutterTimer.Elapsed += onShutterTimer;
            _shutterTimer.Enabled = false;

            _movementTimer = new System.Timers.Timer(2000); // runs every two seconds
            _movementTimer.Elapsed += onMovementTimer;
            _movementTimer.Enabled = false;

            _stuckTimer = new System.Timers.Timer(1000);  // runs every 1 second
            _stuckTimer.Elapsed += onStuckTimer;
            _stuckTimer.Enabled = false;

            wisesite.init();

            _initialized = true;

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: init() done.");
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
            if ((_state != DomeState.MovingCCW) && (_state != DomeState.MovingCW))
                return false;

            ShortestDistanceResult shortest = instance.Azimuth.ShortestDistance(there);
            return shortest.angle <= inertiaAngle(there);
        }

        private void onDomeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_targetAz != null && arriving(_targetAz))
            {
                Stop();
                _targetAz = null;

                if (_parking)
                {
                    _parking = false;
                    AtPark = true;
                }

                _arrivedAtAzEvent.Set();
            }

            if (AtCaliPoint)
            {
                if (_calibrating)
                {
                    Stop();
                    _calibrating = false;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Setting _foundCalibration ...");
                    #endregion
                    _foundCalibration.Set();
                }
                domeEncoder.Calibrate(_homePointAzimuth);
            }
        }

        private void onShutterTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_shutterState == ShutterState.Opening || _shutterState == ShutterState.Closing)
            {
                ShutterStop();
                _shutterTimer.Enabled = false;
            }
        }

        private void onMovementTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            uint currTicks, deltaTicks;
            const int leastExpectedTicks = 2;  // least number of Ticks expected to change in two seconds
            
            // the movementTimer should not be Enabled unless the dome is moving
            if (_isStuck || ((_state != DomeState.MovingCW) && (_state != DomeState.MovingCCW)))
                return;

            deltaTicks = 0;
            currTicks  = domeEncoder.Value;

            if (currTicks == _prevTicks)
                _isStuck = true;
            else {
                switch (_state) {
                    case DomeState.MovingCW:        // encoder decreases
                        if (_prevTicks > currTicks)
                            deltaTicks = _prevTicks - currTicks;
                        else
                            deltaTicks = domeEncoder.Ticks - currTicks + _prevTicks;

                        if (deltaTicks < leastExpectedTicks)
                            _isStuck = true;
                        break;

                    case DomeState.MovingCCW:       // encoder increases
                        if (_prevTicks > currTicks)
                            deltaTicks = _prevTicks - currTicks;
                        else
                            deltaTicks = domeEncoder.Ticks - _prevTicks + currTicks;

                        if (deltaTicks < leastExpectedTicks)
                            _isStuck = true;
                        break;
                }
            }

            if (_isStuck) {
                _stuckPhase    = StuckPhase.NotStuck;
                nextStuckEvent = DateTime.Now;
                onStuckTimer(null, null);           // call first phase immediately
                _stuckTimer.Enabled = true;
            }

            _prevTicks = currTicks;
        }


        private void onStuckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime rightNow;
            WisePin backwardPin, forwardPin;

            rightNow = DateTime.Now;

            if (DateTime.Compare(rightNow, nextStuckEvent) < 0)
                return;

            forwardPin = (_state == DomeState.MovingCCW) ? leftPin : rightPin;
            backwardPin = (_state == DomeState.MovingCCW) ? rightPin : leftPin;

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
                    _stuckTimer.Enabled = false;
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
            _state = DomeState.MovingCW;
            domeEncoder.setMovement(Direction.CW);
            _movementTimer.Enabled = true;
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
            _state = DomeState.MovingCCW;
            domeEncoder.setMovement(Direction.CCW);
            _movementTimer.Enabled = true;
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
            _state = DomeState.Idle;
            _movementTimer.Enabled = false;
            domeEncoder.setMovement(Direction.None);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: Stopped");
            #endregion
        }

        public void StartOpeningShutter()
        {
            openPin.SetOn();
            _shutterState = ShutterState.Opening;
            _shutterTimer.Start();
        }

        public void StartClosingShutter()
        {
            closePin.SetOn();
            _shutterState = ShutterState.Closing;
            _shutterTimer.Start();
        }

        public void ShutterStop()
        {
            switch (_shutterState)
            {
                case ShutterState.Opening:
                    openPin.SetOff();
                    _shutterState = ShutterState.Open;
                    break;

                case ShutterState.Closing:
                    closePin.SetOff();
                    _shutterState = ShutterState.Closed;
                    break;
            }
        }

        public bool AtCaliPoint
        {
            get
            {
                return (_simulated) ? domeEncoder.Value == 10 : homePin.isOff;
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
                        return Angle.invalid;
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
            if (ShutterIsActive())
            {
                tl.LogMessage("Dome: FindHome", "Cannot FindHome, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot FindHome, shutter is active!");
            }

            if (wisesite.computerControl != null && !wisesite.computerControl.IsSafe)
                throw new ASCOM.InvalidOperationException("Wise40 computer control is OFF!");

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

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: FindHomePoint: waiting for _foundCalibration ...");
            #endregion
            _foundCalibration.WaitOne();
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

            if (ShutterIsActive())
            {
                tl.LogMessage("Dome: SlewToAzimuth", "Denied, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot move, shutter is active!");
            }

            if (wisesite.computerControl != null && !wisesite.computerControl.IsSafe)
                throw new ASCOM.InvalidOperationException("Wise40 computer control is OFF!");

            Angle toAng = new Angle(degrees);

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
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDome: SlewToAzimuth: {0}, not calibrated, _autoCalibrate == false, throwing InvalidOperationException", toAng);
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

            if (_simulated && _targetAz == _simulatedStuckAz)
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
                switch (_state)
                {
                    case DomeState.Idle:
                        return "Idle";
                    case DomeState.MovingCCW:
                        return Calibrated ? "Moving CCW" : "Calibrating CCW";
                    case DomeState.MovingCW:
                        return Calibrated ? "Moving CW" : "Calibrating CW";
                    default:
                        return "Unknown";
                }
            }
        }

        public bool ShutterIsActive()
        {
            return _shutterState == ShutterState.Opening || _shutterState == ShutterState.Closing;
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

                bool ret = (_state == DomeState.MovingCCW) || (_state == DomeState.MovingCW) ||
                    (_shutterState == ShutterState.Opening) || (_shutterState == ShutterState.Closing);

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

            if (!Calibrated)
                throw new InvalidOperationException("Cannot Park, dome is NOT calibrated");

            if (ShutterIsActive())
            {
                tl.LogMessage("Dome: Park", "Cannot Park, shutter is active.");
                throw new ASCOM.InvalidOperationException("Cannot Park, shutter is active!");
            }
            
            tl.LogMessage("Dome: Park", "");

            AtPark = false;
            _parking = true;
            SlewToAzimuth(_parkAzimuth);
        }

        public void OpenShutter()
        {
            if (Slewing)
                throw new ASCOM.InvalidOperationException("Cannot OpenShutter, dome is slewing!");

            if (wisesite.safeToOpen != null && !wisesite.safeToOpen.IsSafe)
                throw new ASCOM.InvalidOperationException("Not safe to open shutter!");

            tl.LogMessage("Dome: OpenShutter", "");

            ShutterStop();
            StartOpeningShutter();
        }

        public void CloseShutter()
        {
            if (Slewing)
                throw new ASCOM.InvalidOperationException("Cannot CloseShutter, dome is slewing!");

            tl.LogMessage("Dome: CloseShutter", "");

            ShutterStop();
            StartClosingShutter();
        }

        public void AbortSlew()
        {
            tl.LogMessage("Dome: AbortSlew", "");
            Stop();
        }

        public double Altitude
        {
            get
            {
                tl.LogMessage("Dome: Altitude Get", "Not implemented");
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
                tl.LogMessage("Dome: CanFindHome Get", true.ToString());
                return true;
            }
        }

        public bool CanPark
        {
            get
            {
                tl.LogMessage("Dome: CanPark Get", true.ToString());
                return true;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                tl.LogMessage("Dome: CanSetAltitude Get", false.ToString());
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

            tl.LogMessage("Dome: SyncToAzimuth", ang.ToString());
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

        public ASCOM.DeviceInterface.ShutterState ShutterStatus
        {
            get
            {
                ASCOM.DeviceInterface.ShutterState ret = DeviceInterface.ShutterState.shutterError;

                switch (_shutterState)
                {
                    case WiseDome.ShutterState.Closed:
                        ret = DeviceInterface.ShutterState.shutterClosed;
                        break;
                    case WiseDome.ShutterState.Closing:
                        ret = DeviceInterface.ShutterState.shutterClosing;
                        break;
                    case WiseDome.ShutterState.Open:
                        ret = DeviceInterface.ShutterState.shutterOpen;
                        break;
                    case WiseDome.ShutterState.Opening:
                        ret = DeviceInterface.ShutterState.shutterOpening;
                        break;
                }
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

        public string Name
        {
            get
            {
                string name = "Wise40 Dome";
                //tl.LogMessage("Name Get", name);
                return name;
            }
        }
    }
}
