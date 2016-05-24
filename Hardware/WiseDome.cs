using System;
using System.Threading;
using System.Collections.Generic;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDome : IConnectable, IDisposable {

        private WisePin leftPin, rightPin;
        private WisePin openPin, closePin;
        private WisePin caliPin, ventPin;
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

        private const double CallibrationPointAzimuth = 254.6;
        public const int TicksPerDomeRevolution = 1018;
        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        public const double ParkAzimuth = 90.0;
        private const int simulatedEncoderTicksPerSecond = 6;   // As per Yftach's measurement
        private const double simulatedStuckAz = 333.0;          // If targeted to this Az, we simulate dome-stuck (must be a valid az)
        private const double invalidAz = double.NaN;

        private double targetAz;

        private System.Timers.Timer domeTimer;
        private System.Timers.Timer shutterTimer;
        private System.Timers.Timer movementTimer;
        private System.Timers.Timer stuckTimer;

        private bool _simulated = Environment.MachineName != "dome-ctlr";
        private bool _slaved = false;
        private bool _atPark = false;

        private Debugger debugger;
        private static AutoResetEvent reachedCalibrationPoint = new AutoResetEvent(false);

        public WiseDome()
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

                caliPin = new WisePin("DomeCalibration", Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalIn);
                if (_simulated)
                    ventPin = new WisePin("DomeVent", Hardware.Instance.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut);
                else
                    ventPin = new WisePin("DomeVent", Hardware.Instance.teleboard, DigitalPortType.ThirdPortCL, 0, DigitalPortDirection.DigitalOut);

                domeEncoder = new WiseDomeEncoder("DomeEncoder");

                connectables.Add(openPin);
                connectables.Add(closePin);
                connectables.Add(leftPin);
                connectables.Add(rightPin);
                connectables.Add(caliPin);
                connectables.Add(ventPin);
                connectables.Add(domeEncoder);

                disposables.Add(openPin);
                disposables.Add(closePin);
                disposables.Add(leftPin);
                disposables.Add(rightPin);
                disposables.Add(caliPin);
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
        private double inertiaDegrees(double az)
        {
            return 2 * (360 / TicksPerDomeRevolution);
        }

        /// <summary>
        /// Checks if we're close enough to a given Azimuth
        /// </summary>
        /// <param name="az"></param>
        /// <returns></returns>
        private bool arriving(double az)
        {
            if (((_state == DomeState.MovingCCW) || (_state == DomeState.MovingCW)) && (minAzDistance(az, Azimuth) <= inertiaDegrees(targetAz)))
                return true;
            return false;
        }

        private void onDomeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (targetAz != invalidAz && arriving(targetAz))
            {
                Stop();
                targetAz = invalidAz;
            }

            if (AtCaliPoint)
            {
                if (_calibrating)
                {
                    Stop();
                    _calibrating = false;
                    reachedCalibrationPoint.Set();
                }
                domeEncoder.Calibrate(CallibrationPointAzimuth);
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0:###.##} deg, phase1: stopped moving, letting wheels cool for 10 seconds", Azimuth);
                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0:###.##} deg, phase2: going backwards for 2 seconds", Azimuth);
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0:###.##} deg, phase3: stopping for 2 seconds", Azimuth);
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    isStuck = false;
                    stuckTimer.Enabled = false;
                    nextStuckEvent = rightNow.AddYears(100);
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "stuck: {0:###.##} deg, phase4: resumed original motion", Azimuth);
                    break;
            }
        }


        public void MoveCW()
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
            MoveCW();
        }

        public void MoveCCW()
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
            MoveCCW();
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
                return (_simulated) ? domeEncoder.Value == 10 : caliPin.isOff;
            }
        }

        public double Azimuth
        {
            get
            {
                if (!domeEncoder.calibrated)
                    return invalidAz;
                 
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

        private Direction ShortestWayAz(double fromAz, double toAz)
        {
            double deltaCW, deltaCCW;

            if (Math.Abs(fromAz - toAz) <= DegreesPerTick)
                return Direction.None;

            if (fromAz < toAz)
            {
                deltaCW = toAz - fromAz;
                deltaCCW = fromAz + 360 - toAz;
            } else
            {
                deltaCW = 360 - fromAz + toAz;
                deltaCCW = fromAz - toAz;
            }

            return (deltaCCW < deltaCW) ? Direction.CCW : Direction.CW;
        }

        public void FindCalibrationPoint()
        {
            AtPark = false;

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindCalibrationPoint: started");
            _calibrating = true;
            if (domeEncoder.calibrated)
            {
                switch(ShortestWayAz(Azimuth, CallibrationPointAzimuth)) {
                    case Direction.CCW: MoveCCW(); break ;
                    case Direction.CW:  MoveCW(); break;
                }
            } else
                MoveCCW();

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindCalibrationPoint: waiting for reachedCalibrationPoin ...");
            reachedCalibrationPoint.WaitOne();
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "FindCalibrationPoint: reachedCalibrationPoin was Set()");
        }

        public void SlewToAzimuth(double az)
        {
            Direction dir;

            if (!Calibrated) {
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToAzimuth({0}), not calibrated, calling FindCalibrationPoint", az);
                FindCalibrationPoint();
            }

            targetAz = az;
            AtPark = false;

            dir = ShortestWayAz(domeEncoder.Azimuth, targetAz);
            switch (dir)
            {
                case Direction.CCW:
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToAzimuth({0}), at {1}, moving CCW", az, Azimuth.ToString());
                    MoveCCW();
                    break;
                case Direction.CW:
                    debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToAzimuth({0}), at {1}, moving CW", az, Azimuth.ToString());
                    MoveCW();
                    break;
            }

            if (_simulated && targetAz == simulatedStuckAz)
            {
                double stuckAtAz = (dir == Direction.CW) ? targetAz - 5.0 : targetAz + 5.0;

                domeEncoder.SimulateStuckAt(stuckAtAz);
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "Dome encoder will simulate stuck at {0}", stuckAtAz);
            }               
        }

        private double minAzDistance(double az1, double az2)
        {
            double dist = Math.Floor(Math.Min(360 - Math.Abs(az1 - az2), Math.Abs(az1 - az2)));

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "minAzDistance({0}, {1}) => {2}", az1, az2, dist);
            return dist;
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
