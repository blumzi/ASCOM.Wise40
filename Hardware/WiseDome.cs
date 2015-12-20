using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MccDaq;
using ASCOM.Utilities;
using ASCOM.Wise40.Properties;

namespace ASCOM.WiseHardware
{
    public class WiseDome : IConnectable, IDisposable {

        private WisePin leftPin, rightPin;
        private WisePin openPin, closePin;
        private WisePin caliPin, ventPin;
        private WiseDomeEncoder domeEncoder;
        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        private bool calibrating;
        private bool ventIsOpen;
        private bool isStuck;
        private enum DomeState { Idle, MovingCW, MovingCCW, AutoShutdown };
        public enum ShutterState { Idle, Opening, Open, Closing, Closed };
        private enum StuckPhase { NotStuck, FirstStop, GoBackward, SecondStop, ResumeForward };
        public enum Direction { CW, None, CCW };

        private DomeState state;
        private ShutterState _shutterState;

        private StuckPhase _stuckPhase;
        private int prevTicks;      // for Stuck checks
        private DateTime nextStuckEvent;

        private const double CallibrationPointAzimuth = 254.6;
        public const int TicksPerDomeRevolution = 1018;
        public const double DegreesPerTick = 360.0 / TicksPerDomeRevolution;
        public const double ticksPerDegree = TicksPerDomeRevolution / 360;
        public const double ParkAzimuth = 90.0;
        private const int simulatedEncoderTicksPerSecond = 6;     // As per Yftach's measurement

        private double targetAz;

        private System.Timers.Timer domeTimer;
        private System.Timers.Timer shutterTimer;
        private System.Timers.Timer movementTimer;
        private System.Timers.Timer stuckTimer;

        private bool simulated;

        private TraceLogger logger;

        public WiseDome(TraceLogger logger, bool simulated = true)
        {
            this.logger = logger;
            Hardware.Instance.init(simulated);

            try {
                connectables = new List<IConnectable>();
                disposables = new List<IDisposable>();

                openPin = new WisePin("DomeShutterOpen", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 0, DigitalPortDirection.DigitalOut);
                closePin = new WisePin("DomeShutterClose", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 1, DigitalPortDirection.DigitalOut);
                leftPin = new WisePin("DomeLeft", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 2, DigitalPortDirection.DigitalOut);
                rightPin = new WisePin("DomeRight", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 3, DigitalPortDirection.DigitalOut);

                caliPin = new WisePin("DomeCalibration", Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalIn, simulated ? true : false);
                if (simulated)
                    ventPin = new WisePin("DomeVent", Hardware.Instance.domeboard, DigitalPortType.FirstPortA, 7, DigitalPortDirection.DigitalOut);
                else
                    ventPin = new WisePin("DomeVent", Hardware.Instance.teleboard, DigitalPortType.ThirdPortCL, 0, DigitalPortDirection.DigitalOut);

                domeEncoder = new WiseDomeEncoder("DomeEncoder", logger, simulated);

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
                disposables.Add(domeEncoder);
            }
            catch (WiseException e)
            {
                log("WiseDome constructor caught: {0}.", e.Message);
            }

            openPin.SetOff();
            closePin.SetOff();
            leftPin.SetOff();
            rightPin.SetOff();

            calibrating = false;
            ventIsOpen = false;
            this.simulated = simulated;
            state = DomeState.Idle;
            _shutterState = ShutterState.Closed;

            domeTimer = new System.Timers.Timer(100);   // runs every 100 millis
            domeTimer.Elapsed += onDomeTimer;
            domeTimer.Enabled = true;

            shutterTimer = new System.Timers.Timer(25 * 1000); // runs every 25 (22 * 110%) seconds
            shutterTimer.Elapsed += onShutterTimer;
            shutterTimer.Enabled = false;

            movementTimer = new System.Timers.Timer(2000); // runs every 25 (22 * 110%) seconds
            movementTimer.Elapsed += onMovementTimer;
            movementTimer.Enabled = false;

            stuckTimer = new System.Timers.Timer(1000);  // runs every 1 second
            stuckTimer.Elapsed += onStuckTimer;
            stuckTimer.Enabled = false;

            log("WiseDome constructor done.");
        }

        public void Connect(bool connected)
        {
            foreach (var connectable in connectables)
            {
                connectable.Connect(connected);
            }
        }

        public void log(string fmt, params object[] o)
        {
            string msg = String.Format(fmt, o);

            logger.LogMessage("WiseDome", msg);
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
            if (((state == DomeState.MovingCCW) || (state == DomeState.MovingCW)) && (minAzDistance(az, Azimuth) <= inertiaDegrees(targetAz)))
                return true;
            return false;
        }

        private void onDomeTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            // log("onDomeTimer: targetAz: {0}, calibrating: {1}", targetAz.ToString(), calibrating.ToString());
            if (targetAz != -1 && arriving(targetAz))
            {
                Stop();
                targetAz = -1;
            }

            if (AtCaliPoint)
            {
                if (calibrating)
                {
                    Stop();
                    calibrating = false;
                }
                domeEncoder.Calibrate(CallibrationPointAzimuth);
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
            if (isStuck || ((state != DomeState.MovingCW) && (state != DomeState.MovingCCW)))
                return;

            deltaTicks = 0;
            currTicks  = domeEncoder.Value;

            if (currTicks == prevTicks)
                isStuck = true;
            else {
                switch (state) {
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

            forwardPin = (state == DomeState.MovingCCW) ? leftPin : rightPin;
            backwardPin = (state == DomeState.MovingCCW) ? rightPin : leftPin;

            switch (_stuckPhase) {
                case StuckPhase.NotStuck:              // Stop, let the wheels cool down
                    forwardPin.SetOff();
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.FirstStop;
                    nextStuckEvent = rightNow.AddMilliseconds(10000);
                    log("stuck: {0} deg, phase1: stopped moving, letting wheels cool for 10 seconds", Azimuth);
                    break;

                case StuckPhase.FirstStop:             // Go backward for two seconds
                    backwardPin.SetOn();
                    _stuckPhase = StuckPhase.GoBackward;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    log("stuck: {0} deg, phase2: going backwards for 2 seconds", Azimuth);
                    break;

                case StuckPhase.GoBackward:            // Stop again for two seconds
                    backwardPin.SetOff();
                    _stuckPhase = StuckPhase.SecondStop;
                    nextStuckEvent = rightNow.AddMilliseconds(2000);
                    log("stuck: {0} deg, phase3: stopping for 2 seconds", Azimuth);
                    break;

                case StuckPhase.SecondStop:            // Done, resume original movement
                    forwardPin.SetOn();
                    _stuckPhase = StuckPhase.NotStuck;
                    isStuck = false;
                    stuckTimer.Enabled = false;
                    nextStuckEvent = rightNow.AddYears(100);
                    log("stuck: {0} deg, phase4: resumed original motion", Azimuth);
                    break;
            }
        }


        public void MoveCW()
        {
            leftPin.SetOff();
            rightPin.SetOn();
            state = DomeState.MovingCW;
            domeEncoder.setMovement(Direction.CW);
            movementTimer.Enabled = true;
            log("WiseDome: Started moving CW");
        }

        public void MoveRight()
        {
            MoveCW();
        }

        public void MoveCCW()
        {
            rightPin.SetOff();
            leftPin.SetOn();
            state = DomeState.MovingCCW;
            domeEncoder.setMovement(Direction.CCW);
            movementTimer.Enabled = true;
            log("WiseDome: Started moving CCW");
        }

        public void MoveLeft()
        {
            MoveCCW();
        }

        public void Stop()
        {
            rightPin.SetOff();
            leftPin.SetOff();
            state = DomeState.Idle;
            movementTimer.Enabled = false;
            domeEncoder.setMovement(Direction.None);
            log("WiseDome: Stopped");
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
                return (simulated) ? domeEncoder.Value == 10 : caliPin.IsOff();
            }
        }

        public double Azimuth
        {
            get
            {
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
            calibrating = true;
            if (domeEncoder.calibrated)
            {
                switch(ShortestWayAz(Azimuth, CallibrationPointAzimuth)) {
                    case Direction.CCW: MoveCCW(); break ;
                    case Direction.CW:  MoveCW(); break;
                }
            } else
                MoveCCW();
        }

        public void MoveTo(double az)
        {
            Direction dir;

            if (!domeEncoder.calibrated)
                return;
            targetAz = az;

            dir = ShortestWayAz(domeEncoder.Azimuth, targetAz);
            switch (dir)
            {
                case Direction.CCW:
                    MoveCCW();
                    break;
                case Direction.CW:
                    MoveCW();
                    break;
            }
        }

        private double minAzDistance(double az1, double az2)
        {
            return Math.Floor(Math.Min(360 - Math.Abs(az1 - az2), Math.Abs(az1 - az2)));
        }

        public bool AtPark()
        {
            return (domeEncoder.calibrated) ? Math.Abs(Azimuth - ParkAzimuth) < 1.0 : false;
        }

        public ShutterState shutterState
        {
            get
            {
                return _shutterState;
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
            ventPin.SetOff();
        }

        public bool Slewing
        {
            get
            {
                return (state == DomeState.MovingCCW) || (state == DomeState.MovingCW);
            }
        }

        public bool ShutterIsActive
        {
            get
            {
                return ((_shutterState == ShutterState.Opening) || (_shutterState == ShutterState.Closing)) ? true : false;
            }
        }

    }
}
