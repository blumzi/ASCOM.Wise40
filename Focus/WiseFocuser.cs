using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Collections;
using System.Globalization;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using MccDaq;

using ASCOM.Wise40SafeToOperate;

namespace ASCOM.Wise40
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        private static bool _initialized = false;
        private static bool _connected = false;
        private static readonly uint _upwardCompensation = 100;

        public class State
        {
            [Flags]
            public enum Flags
            {
                MovingUp = (1 << 0),                   // Movings without a specific target
                MovingDown = (1 << 1),                 //  ditto
                MovingToTarget = (1 << 2),             // Trying to reach a specified target
                MovingToIntermediateTarget = (1 << 3), // Moving to an intermediate target, below the specified target
                Stopping = (1 << 4),                   // The motor(s) have been stopped, maybe not yet fully stopped
                Testing = (1 << 5),
            };
            public Flags _flags;

            private const Flags motionFlags = 
                Flags.MovingUp | 
                Flags.MovingDown | 
                Flags.MovingToTarget | 
                Flags.MovingToIntermediateTarget |
                Flags.Stopping;

            public bool IsIdle()
            {
                return ((_flags & motionFlags) == 0);
            }

            public bool IsSet(Flags f)
            {
                return (_flags & f) != 0;
            }

            public void Set(Flags f)
            {
                _flags |= f;
            }

            public void Unset(Flags f)
            {
                _flags &= ~f;
            }

            public void BecomeIdle()
            {
                activityMonitor.EndActivity(ActivityMonitor.Activity.Focuser);
            }

            public bool Equals(Flags f)
            {
                return (_flags == f);
            }

            public override string ToString()
            {
                return _flags.ToString();
            }
        }

        private static State _state = new State();

        private static bool _encoderIsChanging = false;

        private ASCOM.Utilities.TraceLogger tl;
        public Debugger debugger = Debugger.Instance;

        private WisePin pinUp, pinDown;
        private WiseFocuserEnc encoder;

        private static WiseSafeToOperate safetooperate = WiseSafeToOperate.Instance;
        private static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public enum Direction { Up, Down, AllUp, AllDown };
        public class MotionParameter
        {
            public int stoppingDistance;   // number of encoder ticks to stop
        };

        Dictionary<Direction, MotionParameter> motionParameters;

        private static uint _targetPosition, _intermediatePosition;
        private static uint _mostRecentPosition;

        private const int nRecentPositions = 5;
        private FixedSizedQueue<uint> recentPositions = new FixedSizedQueue<uint>(nRecentPositions);

        private System.Threading.Timer movementMonitoringTimer;
        private static TimeSpan movementMonitoringTimeSpan = TimeSpan.FromMilliseconds(35);

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseSite wisesite = WiseSite.Instance;

        public static string driverID = Const.wiseFocusDriverID;

        private static string driverDescription = string.Format("ASCOM Wise40.Focuser v{0}", version.ToString());

        public WiseFocuser() { }
        static WiseFocuser() { }
        private static volatile WiseFocuser _instance; // Singleton
        private static object syncObject = new object();

        public static WiseFocuser Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseFocuser();
                        _instance.init();
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "WiseFocuser";

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();
            debugger.init();
            tl = new TraceLogger("", "Focuser");
            tl.Enabled = debugger.Tracing;

            wisesite.init();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, direction: Const.Direction.Decreasing, controlled: true);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut, direction: Const.Direction.Increasing, controlled: true);

            connectables.AddRange(new List<IConnectable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            disposables.AddRange(new List<IDisposable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            
            movementMonitoringTimer = new System.Threading.Timer(new TimerCallback(onTimer));
            movementMonitoringTimer.Change(0, (int) movementMonitoringTimeSpan.TotalMilliseconds);

            motionParameters = new Dictionary<Direction, MotionParameter>
            {
                [Direction.Up] = new MotionParameter() { stoppingDistance = 10 },
                [Direction.Down] = new MotionParameter() { stoppingDistance = 10 }
            };
            _initialized = true;
        }

        public void Connect(bool connected)
        {
            foreach (var connectable in connectables)
                connectable.Connect(connected);
        }

        public bool Connected
        {
            get
            {
                #region trace
                //tl.LogMessage("Connected Get", _connected.ToString());
                #endregion
                return _connected;
            }

            set
            {
                #region trace
                //tl.LogMessage("Connected Set", value.ToString());
                #endregion
                if (value == _connected)
                    return;

                foreach (var connectable in connectables)
                    connectable.Connect(value);

                _connected = value;
            }
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
            tl.Dispose();
        }

        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Focuser" })
            {}
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Focuser" })
            {}
        }

        public double Temperature
        {
            get
            {
                #region trace
                //tl.LogMessage("Temperature Get", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("Temperature", false);
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                bool ret = false;
                #region trace
                //tl.LogMessage("TempCompAvailable Get", ret.ToString());
                #endregion
                return ret; // Temperature compensation is not available in this driver
            }
        }

        public bool TempComp
        {
            get
            {
                #region trace
                //tl.LogMessage("TempComp Get", false.ToString());
                #endregion
                return false;
            }
            set
            {
                #region trace
                //tl.LogMessage("TempComp Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("TempComp", false);
            }
        }

        /// <summary>
        /// The full travel is 50mm
        /// </summary>
        public double StepSize
        {
            get
            {
                double ret = 50000.0 / (UpperLimit - LowerLimit);
                #region trace
                //tl.LogMessage("StepSize Get", string.Format("Get - {0}", ret));
                #endregion
                return ret;
            }
        }

        public uint position
        {
            get
            {
                uint pos = _mostRecentPosition;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "position: {0}", pos);
                #endregion
                return pos;
            }
        }

        public uint Position
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");

                return position;
            }
        }

        public bool Absolute
        {
            get
            {
                bool ret = true;
                return ret; // This is an absolute focuser
            }
        }

        public void StartStopping()
        {
            _state.Set(State.Flags.Stopping);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                "StartStopping: Started stopping at {0} (_state: {1})...", Position, _state);
            #endregion
            if (Simulated)
                encoder.stopMoving();
            
            pinUp.SetOff();
            pinDown.SetOff();
        }

        public void Halt()
        {
            if (!Connected)
                throw new NotConnectedException("");
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Halt");
            #endregion
            StartStopping();
        }

        public bool IsMoving
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");

                bool ret = !_state.IsIdle();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "IsMoving: {0}", ret);
                #endregion
                return ret;
            }
        }

        public bool Link
        {
            get
            {
                return this.Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
                this.Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        public int MaxIncrement
        {
            get
            {
                return (int)UpperLimit; // Maximum change in one move
            }
        }

        public int MaxStep
        {
            get
            {
                return (int)UpperLimit; // Maximum extent of the focuser, so position range is 0 to 10,000
            }
        }

        public void Move(Direction dir)
        {
            if (!safetooperate.IsSafe)
                throw new InvalidOperationException(string.Join(", ", safetooperate.UnsafeReasons));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Starting Move({0}) at {1}",
                dir.ToString(), Position);
            #endregion
            activityMonitor.StartActivity(ActivityMonitor.Activity.Focuser);
            switch (dir)
            {
                case Direction.Up:
                    _state.Set(State.Flags.MovingUp);
                    StartMoving(dir);
                    break;
                case Direction.Down:
                    _state.Set(State.Flags.MovingDown);
                    StartMoving(dir);
                    break;
                case Direction.AllUp:
                    _state.Set(State.Flags.MovingUp);
                    Move(targetPos: UpperLimit);
                    break;
                case Direction.AllDown:
                    _state.Set(State.Flags.MovingDown);
                    Move(targetPos: LowerLimit);
                    break;
            }
        }

        public void Move(uint targetPos)
        {
            if (!Connected)
                throw new NotConnectedException("Not connected!");

            if (IsMoving)
                throw new InvalidOperationException("Cannot Move while IsMoving == true");

            if (!safetooperate.IsSafe)
                throw new InvalidOperationException(string.Join(", ", safetooperate.UnsafeReasons));

            if (TempComp)
                throw new InvalidOperationException("Cannot Move while TempComp == true");

            if (targetPos > UpperLimit || targetPos < LowerLimit)
                throw new DriverException(string.Format("Can only move between {0} and {1}!", LowerLimit, UpperLimit));

            uint currentPosition = Position;

            if (currentPosition == targetPos)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Move({0}) - target same as current, not moving", targetPos);
                #endregion
                return;
            }

            if (targetPos > currentPosition && ((targetPos - currentPosition) < motionParameters[Direction.Up].stoppingDistance))
                throw new InvalidOperationException(string.Format("Too short. Move at least {0} positions up!",
                    motionParameters[Direction.Up].stoppingDistance));

            if (targetPos < currentPosition && ((currentPosition - targetPos) < motionParameters[Direction.Down].stoppingDistance))
                throw new InvalidOperationException(string.Format("Too short. Move at least {0} positions down!",
                    motionParameters[Direction.Down].stoppingDistance));

            activityMonitor.StartActivity(ActivityMonitor.Activity.Focuser);
            _targetPosition = targetPos;
            StartMovingToTarget();
        }

        private void StartMoving(Direction dir)
        {
            switch(dir)
            {
                case Direction.Up:
                    pinUp.SetOn();
                    if (Simulated)
                        encoder.startMoving(Const.Direction.Increasing);
                    break;

                case Direction.Down:
                    pinDown.SetOn();
                    if (Simulated)
                        encoder.startMoving(Const.Direction.Decreasing);
                    break;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                "StartMoving: _move({0}) - at {1} started moving {2} ", dir.ToString(), Position, dir.ToString());
            #endregion
        }

        private void StartMovingToTarget()
        {
            uint currentPos = Position;

            if (_targetPosition > currentPos)
            {
                _intermediatePosition = 0;
                _state.Set(State.Flags.MovingToTarget);
                StartMoving(Direction.Up);
            }
            else
            {
                _intermediatePosition = _targetPosition - _upwardCompensation;
                _state.Set(State.Flags.MovingToIntermediateTarget);
                StartMoving(Direction.Down);
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                "StartMovingToTarget: _target: {0}, _intermediateTarget: {1}, _state: {2}",
                _targetPosition, _intermediatePosition, _state);
            #endregion
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!Connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        private ArrayList supportedActions = new ArrayList() {
            "status",
            "start-testing",
            "end-testing"
        };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        #region testing
        Test runningTest = null;
        #endregion

        public string Action(string actionName, string actionParameters)
        {
            if (actionName == "status")
                return Status;
            else if (actionName == "start-testing")
            {
                //try
                //{
                //    runningTest = new Test(actionParameters);
                //}
                //catch (Exception ex)
                //{
                //    return ex.Message;
                //}
                _state.Set(State.Flags.Testing);
                return "ok";
            }
            else if (actionName == "end-testing")
            {
                _state.Unset(State.Flags.Testing);
                return "ok";
            }
            else
                throw new ASCOM.ActionNotImplementedException("Action " + actionName +
                    " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public static string DriverId
        {
            get
            {
                return driverID;
            }
        }

        public static string Description
        {
            get
            {
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "Wise40 Focuser. Version: " + DriverVersion;
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);

                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                short ret = 2;

                return ret;
            }
        }

        private void onTimer(object StateObject)
        {
            movementMonitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            const int maxDeltaBetweenReadings = 4;

            State oldState = _state;
            uint reading = encoder.Value;

            if (_mostRecentPosition != 0)
                do
                {
                    if ((int)Math.Abs(reading - _mostRecentPosition) < maxDeltaBetweenReadings)
                        break;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "onTimer: discarded reading: {0} ({1})", reading, _mostRecentPosition);
                    #endregion
                    reading = encoder.Value;
                } while (true);
            _mostRecentPosition = reading;

            if (_state.IsSet(State.Flags.Testing))
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "onTimer: _mostRecentPosition: {0}", _mostRecentPosition);

            recentPositions.Enqueue(_mostRecentPosition);

            #region Check if encoder is changing
            uint[] arr = recentPositions.ToArray();

            bool changing = false;
            if (arr.Count() < nRecentPositions)
                changing = true;      // not enough readings yet
            else
            {
                uint max = arr.Max();
                foreach (uint pos in arr)
                    if (pos != max)
                    {
                        changing = true;
                        break;
                    }
            }
            #endregion
            _encoderIsChanging = changing;

            if (
                ((pinUp.isOn || _state.IsSet(State.Flags.MovingUp)) &&
                        CloseEnough(_mostRecentPosition, UpperLimit, Direction.Up)) ||
                ((pinDown.isOn || _state.IsSet(State.Flags.MovingDown)) &&
                        CloseEnough(_mostRecentPosition, LowerLimit, Direction.Down))
               )
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "onTimer: at {0} Limit stop (state: {1})",
                    _mostRecentPosition, oldState);
                #endregion
                StartStopping();
                goto RestartTimer;
            }

            if (_state.IsSet(State.Flags.Stopping) && !_encoderIsChanging)
            {
                _state.Unset(State.Flags.Stopping);

                if (_state.IsSet(State.Flags.MovingDown) ||
                    _state.IsSet(State.Flags.MovingUp) ||
                    _state.IsSet(State.Flags.MovingToIntermediateTarget) ||
                    _state.IsSet(State.Flags.MovingToTarget))
                {
                    // Done moving
                    bool wasMovingToIntermediateTarget = _state.IsSet(State.Flags.MovingToIntermediateTarget);

                    _state.Unset(
                        State.Flags.MovingDown | 
                        State.Flags.MovingUp | 
                        State.Flags.MovingToIntermediateTarget |
                        State.Flags.MovingToTarget);

                    if (wasMovingToIntermediateTarget)
                    {
                        // Done moving to intermediate target, start moving to target
                        _state.Set(State.Flags.MovingToTarget);
                        StartMoving(Direction.Up);
                    }
                    else
                        _state.BecomeIdle();
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                        "onTimer: stopped moving: at {0} (_state: {1} => {2})",
                        _mostRecentPosition, oldState, _state);
                    #endregion
                }
                else if (_state.IsSet(State.Flags.Testing)) 
                    runningTest.NextIteration();
            }

            if (_state.IsSet(State.Flags.MovingToTarget) && 
                        CloseEnough(_mostRecentPosition, _targetPosition, Direction.Up))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    "onTimer: close to target: at {0} (_state: {1})", _mostRecentPosition, _state);
                #endregion
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping();
            }

            if (_state.IsSet(State.Flags.MovingToIntermediateTarget) &&
                        CloseEnough(_mostRecentPosition, _intermediatePosition, Direction.Down))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    "onTimer: close to intermediate target: at {0} (_state: {1})", _mostRecentPosition, _state);
                #endregion
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping();
            }

        RestartTimer:
            movementMonitoringTimer.Change(movementMonitoringTimeSpan, movementMonitoringTimeSpan);
        }

        private bool CloseEnough(uint current, uint target, Direction dir)
        {
            bool ret = false;

            if (dir == Direction.Up)
                ret = (target - current) <= motionParameters[Direction.Up].stoppingDistance;

            else if (dir == Direction.Down)
                ret = (current - target) <= motionParameters[Direction.Down].stoppingDistance;

            return ret;
        }

        public string Status
        {
            get
            {
                List<string> ret = new List<string>();

                if (_state.IsSet(State.Flags.MovingUp))
                {
                    ret.Add("moving-up");
                }

                if (_state.IsSet(State.Flags.MovingDown))
                {
                    ret.Add("moving-down");
                }

                if (_state.IsSet(State.Flags.MovingToTarget))
                {
                    ret.Add(string.Format("moving-to-{0}", _targetPosition));
                }

                if (_state.IsSet(State.Flags.MovingToIntermediateTarget))
                {
                    ret.Add(string.Format("moving-to-{0}-via-{1}", _targetPosition, _intermediatePosition));
                }

                if (_state.IsSet(State.Flags.Stopping))
                    ret.Add("stopping");

                if (_state.IsSet(State.Flags.Testing))
                    ret.Add("testing");

                string s = string.Join(",", ret);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Status: {0}", s);
                #endregion
                return s;
            }
        }

        public uint UpperLimit
        {
            get
            {
                return encoder.UpperLimit;
            }
        }

        public uint LowerLimit
        {
            get
            {
                return encoder.LowerLimit;
            }
        }

        #region testing
        public class Test
        {
            private static System.Threading.Timer _timer =
                new System.Threading.Timer(new TimerCallback(EndIteration));
            private int _nIterations, _millis, _iteration;
            private Direction _dir;

            public Test(string parameters)
            {
                string[] args = parameters.Split(',');
                string badArgs = "Bad args for StartTest <times,millis,up|down> (millis >= 50)";

                if (args.Count() >= 3)
                    args[2] = args[2].ToLower();

                if (args.Count() != 3 || !(args[2] == "up" || args[2] == "down"))
                    throw new Exception(badArgs);

                _nIterations = Convert.ToInt32(args[0]);
                _millis = Convert.ToInt32(args[1]);
                if (_nIterations <= 0 || _millis < 50)
                    throw new Exception(badArgs);

                _dir = (args[2] == "up") ? Direction.Up : Direction.Down;
                _iteration = 0;

                _state.Set(State.Flags.Testing);
                NextIteration();
            }

            public void NextIteration()
            {
                if (_iteration >= _nIterations)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    _state.Unset(State.Flags.Testing);
                    return;
                }

                _iteration++;
                _timer.Change(_millis, Timeout.Infinite);
                Instance.Move(_dir);
            }

            private static void EndIteration(object state)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                Instance.StartStopping(); ;
            }
        }
        #endregion
    }
}