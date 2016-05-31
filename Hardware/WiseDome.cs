using System;
using System.Threading;
using System.Collections.Generic;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDome : IConnectable, IDisposable {

        private WisePin leftPin, rightPin;
        private WisePin openPin, closePin;
        private WisePin homePin, ventPin;
        private WiseDomeEncoder domeEncoder;
        private List<IConnectable> connectables;
        private List<IDisposable> disposables;
        private bool _connected = false;
        private bool _calibrated = false;
        private bool _calibrating = false;
        private bool ventIsOpen;
        private bool isStuck;

        private enum DomeState { Idle, MovingCW, MovingCCW, AutoShutdown };
        public enum ShutterState { Idle, Opening, Open, Closing, Closed };
        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        public enum Direction { CW, None, CCW };

        private DomeState _state;
        private ShutterState _shutterState;

        private StuckPhase _stuckPhase;
        private int prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        private Angle HomePointAzimuth = new Angle(254.6);
        public const int TicksPerDomeRevolution = 1018;

        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        private const int simulatedEncoderTicksPerSecond = 6;   // As per Yftach's measurement

        public const double ParkAzimuth = 90.0;
        private Angle simulatedStuckAz = new Angle(333.0);      // If targeted to this Az, we simulate dome-stuck (must be a valid az)

        private Angle targetAz = null;

        private System.Timers.Timer domeTimer;
        private System.Timers.Timer shutterTimer;
        private System.Timers.Timer movementTimer;
        private System.Timers.Timer stuckTimer;

        private bool _simulated = Environment.MachineName != "dome-ctlr";
        private bool _slaved = false;
        private bool _atPark = false;

        private Debugger debugger;
        private static AutoResetEvent reachedHomePoint = new AutoResetEvent(false);

        private static AutoResetEvent _arrivedEvent;

        public WiseDome(AutoResetEvent arrivedEvent)
        {
            debugger = new Debugger();
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";
                debugger.Level = Convert.ToUInt32(profile.GetValue("ASCOM.Wise40.Dome", "Debug Level", string.Empty, "0"));
            }
            Hardware.Instance.init();

            try {
                connectables = new List<IConnectable>();
                disposables = new List<IDisposable>();

                openPin = new WisePin("DomeShutterOpen", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                closePin = new WisePin("DomeShutterClose", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut);
                leftPin = new WisePin("DomeLeft", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 2, DigitalPortDirection.DigitalOut);
                rightPin = new WisePin("DomeRight", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 3, DigitalPortDirection.DigitalOut);

                homePin = new WisePin("DomeCalibration", Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalIn);
                ventPin = new WisePin("DomeVent", Hardware.Instance.teleboard, DigitalPortType.ThirdPortCL, 0, DigitalPortDirection.DigitalOut);

                domeEncoder = new WiseDomeEncoder("DomeEncoder", debugger);

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
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseDome constructor caught: {0}.", e.Message);
            }

            openPin.SetOff();
            closePin.SetOff();
            leftPin.SetOff();
            rightPin.SetOff();

            _calibrating = false;
            ventIsOpen = false;
            _state = DomeState.Idle;
            _shutterState = ShutterState.Closed;

            domeTimer = new System.Timers.Timer(100);   // runs every 100 millis
            domeTimer.Elapsed += onDomeTimer;
            domeTimer.Enabled = true;

            if (_simulated)
                shutterTimer = new System.Timers.Timer(2 * 1000);
            else
                shutterTimer = new System.Timers.Timer(25 * 1000);
            shutterTimer.Elapsed += onShutterTimer;
            shutterTimer.Enabled = false;

            movementTimer = new System.Timers.Timer(2000); // runs every two seconds
            movementTimer.Elapsed += onMovementTimer;
            movementTimer.Enabled = false;

            stuckTimer = new System.Timers.Timer(1000);  // runs every 1 second
            stuckTimer.Elapsed += onStuckTimer;
            stuckTimer.Enabled = false;

            _arrivedEvent = arrivedEvent;

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseDome constructor done.");
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
                return _connected;
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
            return new Angle(2 * (Angle.max.Degrees / TicksPerDomeRevolution));
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

            ShortestDistanceResult shortest = Azimuth.ShortestDistance(there);
            //debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "arriving here: {0}, there: {1}, dist: {3}, epsilon: {2}, ret: {4}",
            //    Azimuth, there, shortest.angle, inertiaAngle(there), shortest.angle <= inertiaAngle(there));

            return shortest.angle <= inertiaAngle(there);
        }

        private void onDomeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (targetAz != null && arriving(targetAz))
            {
                Stop();
                targetAz = null;
                if (Slaved)
                    _arrivedEvent.Set();
            }

            if (AtCaliPoint)
            {
                if (_calibrating)
                {
                    Stop();
                    _calibrating = false;
                    reachedHomePoint.Set();
                }
                domeEncoder.Calibrate(HomePointAzimuth);
                _calibrated = true;
            }
        }

        private void onShutterTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_shutterState == ShutterState.Opening || _shutterState == ShutterState.Closing)
            {
                ShutterStop();
                shutterTimer.Enabled = false;
            }
        }

        private void onMovementTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            int currTicks, deltaTicks;
            const int leastExpectedTicks = 2;  // least number of Ticks expected to change in two seconds
            
            // the movementTimer should not be Enabled unless the dome is moving
            if (isStuck || ((_state != DomeState.MovingCW) && (_state != DomeState.MovingCCW)))
                return;

            deltaTicks = 0;
            currTicks  = domeEncoder.Value;

            if (currTicks == prevTicks)
                isStuck = true;
            else {
                switch (_state) {
                    case DomeState.MovingCW:        // encoder decreases
                        if (prevTicks > currTicks)
                            deltaTicks = prevTicks - currTicks;
                        else
                            deltaTicks = domeEncoder.Ticks - currTicks + prevTicks;

                        if (deltaTicks < leastExpectedTicks)
                            isStuck = true;
                        break;

                    case DomeState.MovingCCW:       // encoder increases
                        if (prevTicks > currTicks)
                            deltaTicks = prevTicks - currTicks;
                        else
                            deltaTicks = domeEncoder.Ticks - prevTicks + currTicks;

                        if (deltaTicks < leastExpectedTicks)
                            isStuck = true;
                        break;
                }
            }

            if (isStuck) {
                _stuckPhase    = StuckPhase.NotStuck;
                nextStuckEvent = DateTime.Now;
                onStuckTimer(null, null);           // call first phase immediately
                stuckTimer.Enabled = true;
            }

            prevTicks = currTicks;
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0}, phase1: stopped moving, letting wheels cool for 10 seconds", Azimuth);
                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0}, phase2: going backwards for 2 seconds", Azimuth);
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0}, phase3: stopping for 2 seconds", Azimuth);
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    isStuck = false;
                    stuckTimer.Enabled = false;
                    nextStuckEvent = rightNow.AddYears(100);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0}, phase4: resumed original motion", Azimuth);
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
            movementTimer.Enabled = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseDome: Started moving CW");
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
            movementTimer.Enabled = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseDome: Started moving CCW");
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
            movementTimer.Enabled = false;
            domeEncoder.setMovement(Direction.None);
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseDome: Stopped");
        }

        public void StartOpeningShutter()
        {
            openPin.SetOn();
            _shutterState = ShutterState.Opening;
            shutterTimer.Start();
        }

        public void StartClosingShutter()
        {
            closePin.SetOn();
            _shutterState = ShutterState.Closing;
            shutterTimer.Start();
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
                if (!domeEncoder.calibrated)
                    return Angle.invalid;
                 
                return domeEncoder.Azimuth;
            }

            set
            {
                domeEncoder.Calibrate(value);
            }
        }

        public void OpenVent()
        {
            if (!ventIsOpen)
            {
                ventPin.SetOn();
                ventIsOpen = true;
            }
        }

        public void CloseVent()
        {
            if (ventIsOpen)
            {
                ventPin.SetOff();
                ventIsOpen = false;
            }
        }

        public void FindHome()
        {
            AtPark = false;

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindHomePoint: started");
            _calibrating = true;

            if (domeEncoder.calibrated)
            {
                ShortestDistanceResult shortest = Azimuth.ShortestDistance(HomePointAzimuth);

                switch (shortest.direction) {
                    case Const.AxisDirection.Decreasing: StartMovingCCW(); break ;
                    case Const.AxisDirection.Increasing:  StartMovingCW(); break;
                }
            } else
                StartMovingCCW();

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindHomePoint: waiting for reachedCalibrationPoint ...");
            reachedHomePoint.WaitOne();
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindHomePoint: reachedCalibrationPoint was Set()");
        }

        public void SlewToAzimuth(double degrees)
        {
            Angle toAng = new Angle(degrees);

            if (!Calibrated) {
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToAzimuth: {0}, not calibrated, calling FindHomePoint", toAng);
                FindHome();
            }

            targetAz = toAng;
            AtPark = false;

            ShortestDistanceResult shortest = domeEncoder.Azimuth.ShortestDistance(targetAz);
            switch (shortest.direction)
            {
                case Const.AxisDirection.Decreasing:
                    StartMovingCCW();
                    break;
                case Const.AxisDirection.Increasing:
                    StartMovingCW();
                    break;
            }
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToAzimuth: {0} => {1} (dist: {2}), moving {3}", Azimuth, toAng, shortest.angle, shortest.direction);

            if (_simulated && targetAz == simulatedStuckAz)
            {
                Angle epsilon = new Angle(5.0);
                Angle stuckAtAz = (shortest.direction == Const.AxisDirection.Increasing) ? targetAz - epsilon : targetAz + epsilon;

                domeEncoder.SimulateStuckAt(stuckAtAz);
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "Dome encoder will simulate stuck at {0}", stuckAtAz);
            }               
        }

        public bool AtPark
        {
            get
            {
                return _atPark;
            }

            set
            {
                _atPark = value;
            }
        }

        public ShutterState shutterState
        {
            get
            {
                return _shutterState;
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
            ventPin.SetOff();
        }

        public bool Slewing
        {
            get
            {
                return (_state == DomeState.MovingCCW) || (_state == DomeState.MovingCW) ||
                    (_shutterState == ShutterState.Opening) || (_shutterState == ShutterState.Closing);
            }
        }

        public bool Slaved
        {
            get
            {
                return _slaved;
            }

            set
            {
                _slaved = value;
            }
        }

        public bool Calibrated
        {
            get
            {
                return _calibrated;
            }
        }

        public void Park()
        {
            AtPark = false;
            SlewToAzimuth(90.0);
            AtPark = true;
        }

        public void OpenShutter()
        {
            ShutterStop();
            StartOpeningShutter();
        }

        public void CloseShutter()
        {
            ShutterStop();
            StartClosingShutter();
        }

        public void AbortSlew()
        {
            Stop();
        }
    }
}
