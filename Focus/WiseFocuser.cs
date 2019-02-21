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
using Newtonsoft.Json;

namespace ASCOM.Wise40
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        private static bool _initialized = false;
        private static bool _connected = false;
        private static readonly int _upwardCompensation = 100;

        public class State
        {
            [Flags]
            public enum Flags
            {
                None = 0,
                MovingUp = (1 << 0),                   // Movings without a specific target
                MovingDown = (1 << 1),                 //  ditto
                MovingToTarget = (1 << 2),             // Trying to reach a specified target
                MovingToIntermediateTarget = (1 << 3), // Moving to an intermediate target, below the specified target
                Stopping = (1 << 4),                   // The motor(s) have been stopped, maybe not yet fully stopped

                AnyMoving = MovingUp | MovingDown | MovingToIntermediateTarget | MovingToTarget | Stopping,
            };
            public Flags _flags;

            private const Flags motionFlags = 
                Flags.MovingUp | 
                Flags.MovingDown | 
                Flags.MovingToTarget | 
                Flags.MovingToIntermediateTarget |
                Flags.Stopping;

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
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Focuser, new Activity.FocuserActivity.EndParams()
                    {
                        endState = Activity.State.Succeeded,
                        endReason = "Reached target",
                        end = (int) WiseFocuser.Instance.Position,
                    });
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

        private static int _targetPosition, _intermediatePosition;
        private static int _mostRecentPosition;

        private const int nRecentPositions = 5;
        private FixedSizedQueue<uint> recentPositions = new FixedSizedQueue<uint>(nRecentPositions);

        private static System.Threading.Timer movementMonitoringTimer;
        private static int movementMonitoringMillis = 50;

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

        private static DateTime _lastRead = DateTime.MinValue;
        private static bool _debugging;

        public static WiseFocuser Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new WiseFocuser();
                            _instance.init();
                        }
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            WiseName = "WiseFocuser";

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, direction: Const.Direction.Decreasing, controlled: true);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut, direction: Const.Direction.Increasing, controlled: true);

            connectables.AddRange(new List<IConnectable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            disposables.AddRange(new List<IDisposable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            
            movementMonitoringTimer = new System.Threading.Timer(new TimerCallback(onTimer));
            movementMonitoringTimer.Change(movementMonitoringMillis, movementMonitoringMillis);

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

                ActivityMonitor.Instance.Event(new Event.GlobalEvent(
                    string.Format("{0} {1}", Const.wiseFocusDriverID, value ? "Connected" : "Disconnected")));
            }
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();
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
                int pos = _mostRecentPosition;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "position: {0}", pos);
                #endregion
                return (uint) pos;
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
                return true; // This is an absolute focuser
            }
        }

        public void StartStopping(string reason)
        {
            if (_state.IsSet(State.Flags.Stopping))
                return;

            _state.Set(State.Flags.Stopping);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                "StartStopping: Started stopping at {0} (new _state: {1}, reason: {2}) ...",
                    Position, _state, reason);
            #endregion
            if (Simulated)
                encoder.stopMoving();
            
            pinUp.SetOff();
            pinDown.SetOff();
            Thread.Sleep(50);
        }

        public void Halt(string reason = "Halt")
        {
            if (!Connected)
                throw new NotConnectedException("");

            StartStopping(reason);
        }

        public bool IsMoving
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");

                bool ret = _state.IsSet(State.Flags.AnyMoving) || pinUp.isOn || pinDown.isOn;
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
            if (!safetooperate.IsSafe && !activityMonitor.InProgress(ActivityMonitor.ActivityType.ShuttingDown))
                throw new InvalidOperationException(string.Join(", ", safetooperate.UnsafeReasonsList));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Starting Move({0}) at {1}",
                dir.ToString(), Position);
            #endregion
            activityMonitor.NewActivity(new Activity.FocuserActivity(new Activity.FocuserActivity.StartParams
            {
                start = (int)Position,
                direction = (dir == Direction.Up || dir == Direction.AllUp) ?
                    Activity.FocuserActivity.Direction.Up :
                    Activity.FocuserActivity.Direction.Down,
                target = -1,
            }));
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

            if (!safetooperate.IsSafe && !activityMonitor.InProgress(ActivityMonitor.ActivityType.ShuttingDown))
                throw new InvalidOperationException(string.Join(", ", safetooperate.UnsafeReasonsList));

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

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "Move: at {0}, targetPos: {1}",
                currentPosition, targetPos);
            #endregion
            _targetPosition = (int) targetPos;
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
            Direction dir;

            if (_targetPosition > currentPos)
            {
                _intermediatePosition = 0;
                _state.Set(State.Flags.MovingToTarget);
                dir = Direction.Up;
                StartMoving(dir);
            }
            else
            {
                _intermediatePosition = _targetPosition - _upwardCompensation;
                _state.Set(State.Flags.MovingToIntermediateTarget);
                dir = Direction.Down;
                StartMoving(dir);
            }

            activityMonitor.NewActivity(new Activity.FocuserActivity(new Activity.FocuserActivity.StartParams
            {
                start = (int)currentPos,
                target = _targetPosition,
                intermediateTarget = _intermediatePosition,
                direction = (dir == Direction.Up) ?
                    Activity.FocuserActivity.Direction.Up :
                    Activity.FocuserActivity.Direction.Down,
            }));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                "StartMovingToTarget: at {0}, _target: {1}, _intermediateTarget: {2}, _state: {3}",
                currentPos, _targetPosition, _intermediatePosition, _state);
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
            "end-testing",
        };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        public string Action(string action, string parameter)
        {
            action = action.ToLower();
            parameter = parameter.ToLower();

            if (action == "status")
                return Digest;
            else if (action == "debug")
            {
                _debugging = Convert.ToBoolean(parameter);
                return JsonConvert.SerializeObject(_debugging);
            }
            else if (action == "move")
            {
                switch (parameter)
                {
                    case "up":
                        Move(Direction.Up);
                        break;

                    case "down":
                        Move(Direction.Down);
                        break;

                    case "all-up":
                        Move(Direction.AllUp);
                        break;

                    case "all-down":
                        Move(Direction.AllDown);
                        break;

                    default:
                        uint target = 0;

                        if (UInt32.TryParse(parameter, out target))
                            Move(target);
                        else
                            return string.Format("Bad parameter \"{0}\" to Action(\"move\")", parameter);
                        break;
                }
                return "ok";
            }
            else if (action == "halt" || action == "stop")
            {
                Halt(reason: parameter);
                return "ok";
            }
            else if (action == "limit")
            {
                switch (parameter)
                {
                    case "lower":
                        return LowerLimit.ToString();
                    case "upper":
                        return UpperLimit.ToString();
                }
                return "ok";
            }
            else
                throw new ASCOM.ActionNotImplementedException("Action " + action +
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
            //movementMonitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

            //
            // At the current timer interval (35 millis), the handler is actually called at about 140 millis
            // intervals (don't really know why).  The typical measured maximal encoder change per 1 millisecond is
            // 0.01 encoder ticks.
            //
            // Readings that produce a much larger value are discarded to eliminate noise interferences on
            //  the encoder lines.
            //
            const double maxEncoderChangeRatePerMilli = 0.010 * 100;

            State oldState = _state;
            int reading = (int) encoder.Value;
            DateTime now = DateTime.Now;
            double millis = now.Subtract(_lastRead).TotalMilliseconds;
            _lastRead = now;

            int delta = Math.Abs(reading - _mostRecentPosition);
            double maxDelta = maxEncoderChangeRatePerMilli * millis;

            #region debug
            if (_debugging)
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    "onTimer: reading: {0}, _mostRecentPosition: {1}, delta: {2}, millis: {3}",
                    reading, _mostRecentPosition, delta, millis);
            #endregion

            if (_mostRecentPosition != 0 &&  delta != 0 && delta > maxDelta) {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    "onTimer: suspect reading: {0} _mostRecentPosition: {1}, (delta: {2} > maxDelta: {3})",
                    reading, _mostRecentPosition, delta, maxDelta);
                #endregion
            }
            _mostRecentPosition = reading;

            recentPositions.Enqueue((uint) _mostRecentPosition);

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
                        CloseEnough(_mostRecentPosition, (int) UpperLimit, Direction.Up)) ||
                ((pinDown.isOn || _state.IsSet(State.Flags.MovingDown)) &&
                        CloseEnough(_mostRecentPosition, (int) LowerLimit, Direction.Down))
               )
            {
                StartStopping(string.Format("onTimer: at {0} Limit stop (state: {1})",
                    _mostRecentPosition, oldState));
                return;
            }

            if (_state.IsSet(State.Flags.Stopping) && !_encoderIsChanging)
            {
                _state.Unset(State.Flags.Stopping);

                if (_state.IsSet(State.Flags.AnyMoving))
                {
                    // Done moving
                    bool wasMovingToIntermediateTarget = _state.IsSet(State.Flags.MovingToIntermediateTarget);

                    _state.Unset(State.Flags.AnyMoving);

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
                        "onTimer: stopped moving: at {0} (target: {1}, intermediateTarget: {2}, old state: {3}, new state: {4})",
                        _mostRecentPosition, _targetPosition, _intermediatePosition, oldState, _state);
                    #endregion
                }
            }

            if (_state.IsSet(State.Flags.MovingToTarget) && 
                        CloseEnough(_mostRecentPosition, _targetPosition, Direction.Up))
            {
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping(string.Format("onTimer: close to target: at {0} (_state: {1})", _mostRecentPosition, _state));
            }

            if (_state.IsSet(State.Flags.MovingToIntermediateTarget) &&
                        CloseEnough(_mostRecentPosition, _intermediatePosition, Direction.Down))
            {
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping(string.Format("onTimer: close to intermediate target: at {0} (_state: {1})", _mostRecentPosition, _state));
            }

            if (!_state.IsSet(State.Flags.AnyMoving) && _encoderIsChanging)
                StartStopping("onTimer: Runaway");
        }

        private bool CloseEnough(int current, int target, Direction dir)
        {
            bool ret = false;

            if (dir == Direction.Up)
                ret = ((target - current) <= motionParameters[Direction.Up].stoppingDistance) ||
                    (current >= target);

            else if (dir == Direction.Down)
                ret = ((current - target) <= motionParameters[Direction.Down].stoppingDistance) ||
                    (current <= target);

            return ret;
        }

        public string Digest
        {
            get
            {
                return JsonConvert.SerializeObject(new FocuserDigest()
                    {
                        Position = Position,
                        StatusString = StatusString,
                        IsMoving = IsMoving,
                    });
            }
        }

        public string StatusString
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
    }

    public class FocuserDigest
    {
        public uint Position;
        public string StatusString;
        public bool IsMoving;
    }
}