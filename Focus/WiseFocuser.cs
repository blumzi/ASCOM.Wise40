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
        private static readonly Version version = new Version(0, 2);
        private static bool _initialized = false;
        private static bool _connected = false;
        private const int _upwardCompensation = 100;

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

            public static void BecomeIdle()
            {
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Focuser, new Activity.Focuser.EndParams()
                    {
                        endState = Activity.State.Succeeded,
                        endReason = "Reached target",
                        end = (int) WiseFocuser.Instance.Position,
                    });
            }

            public bool Equals(Flags f)
            {
                return _flags == f;
            }

            public override string ToString()
            {
                return _flags.ToString();
            }
        }

        private static readonly State _state = new State();
        private int _startStoppingPosition;
        private const int _runawayPositions = 500;

        private static bool _encoderIsChanging = false;
        public Debugger debugger = Debugger.Instance;

        private WisePin pinUp, pinDown;
        private WiseFocuserEnc encoder;

        private static readonly WiseSafeToOperate safetooperate = WiseSafeToOperate.Instance;
        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public enum Direction { Up, Down, AllUp, AllDown };
        public class MotionParameter
        {
            public int stoppingDistance;   // number of encoder ticks to stop
        };

        private Dictionary<Direction, MotionParameter> motionParameters;

        private static int _targetPosition, _intermediatePosition;
        private static int _mostRecentPosition;

        private const int nRecentPositions = 5;
        private readonly FixedSizedQueue<uint> recentPositions = new FixedSizedQueue<uint>(nRecentPositions);

        private static System.Threading.Timer movementMonitoringTimer;
        private const int movementMonitoringMillis = 50;

        private readonly List<IConnectable> connectables = new List<IConnectable>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private readonly Hardware.Hardware hardware = Hardware.Hardware.Instance;

        public static string driverID = Const.WiseDriverID.Focus;

        private static readonly string driverDescription = $"{driverID} v{version}";

        public WiseFocuser() { }
        static WiseFocuser() { }

        private static DateTime _lastRead = DateTime.MinValue;
        private static bool _debugging;

        private static readonly Lazy<WiseFocuser> lazy = new Lazy<WiseFocuser>(() => new WiseFocuser()); // Singleton

        public static WiseFocuser Instance
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

            WiseName = "WiseFocuser";

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, direction: Const.Direction.Decreasing, controlled: true);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut, direction: Const.Direction.Increasing, controlled: true);

            connectables.AddRange(new List<IConnectable> { pinUp, pinDown, encoder });
            disposables.AddRange(new List<IDisposable> { pinUp, pinDown, encoder });

            movementMonitoringTimer = new System.Threading.Timer(new TimerCallback(OnTimer));
            movementMonitoringTimer.Change(movementMonitoringMillis, movementMonitoringMillis);

            motionParameters = new Dictionary<Direction, MotionParameter>
            {
                [Direction.Up] = new MotionParameter() { stoppingDistance = 10 },
                [Direction.Down] = new MotionParameter() { stoppingDistance = 10 }
            };

            _mostRecentPosition = (int)encoder.Value;
            _lastRead = DateTime.Now;
            recentPositions.Enqueue((uint)_mostRecentPosition);

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

                ActivityMonitor.Instance.Event(new Event.DriverConnectEvent(Const.WiseDriverID.Focus, _connected, line: ActivityMonitor.Tracer.focuser.Line));
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
        public static void WriteProfile()
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
                Exceptor.Throw<PropertyNotImplementedException>("Temperature", "Not implemented");
                return Double.NaN;
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                #region trace
                //tl.LogMessage("TempCompAvailable Get", false.ToString());
                #endregion
                return false; // Temperature compensation is not available in this driver
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
                Exceptor.Throw<PropertyNotImplementedException>("TempComp", "Not implemented", true);
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
                //tl.LogMessage($"StepSize Get", "Get - {ret}");
                #endregion
                return ret;
            }
        }

        public uint MostRecentPosition
        {
            get
            {
                int pos = _mostRecentPosition;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"position: {pos}");
                #endregion
                return (uint) pos;
            }
        }

        public uint Position
        {
            get
            {
                if (!Connected)
                    Exceptor.Throw<NotConnectedException>("Position", "Not connected");

                return MostRecentPosition;
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
                $"StartStopping: Started stopping at {Position} (new _state: {_state}, reason: {reason}) ...");
            #endregion
            if (Simulated)
                encoder.stopMoving();

            pinUp.SetOff();
            pinDown.SetOff();
            _startStoppingPosition = (int) Position;
            Thread.Sleep(50);
        }

        public void Halt(string reason = "Halt")
        {
            if (!Connected)
                Exceptor.Throw<NotConnectedException>("Halt", "Not connected");

            StartStopping(reason);
        }

        public bool IsMoving
        {
            get
            {
                if (!Connected)
                    Exceptor.Throw<NotConnectedException>("IsMoving", "Not connected");

                bool ret = _state.IsSet(State.Flags.AnyMoving) || pinUp.isOn || pinDown.isOn;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"IsMoving: {ret}");
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
            if (!safetooperate.IsSafeWithoutCheckingForShutdown())
                Exceptor.Throw<InvalidOperationException>("Move", string.Join(", ", safetooperate.UnsafeReasonsList()));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"Starting Move({dir}) at {Position}");
            #endregion
            activityMonitor.NewActivity(new Activity.Focuser(new Activity.Focuser.StartParams
            {
                start = (int)Position,
                direction = (dir == Direction.Up || dir == Direction.AllUp) ?
                    Activity.Focuser.Direction.Up :
                    Activity.Focuser.Direction.Down,
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
            uint currentPosition = Position;
            string op = $"Move({targetPos}) from: {currentPosition}";

            if (!Connected)
                Exceptor.Throw<NotConnectedException>(op, "Not connected!");

            if (IsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot move, already moving");

            if (!safetooperate.IsSafe)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", safetooperate.UnsafeReasonsList()));

            if (TempComp)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot Move while TempComp == true");

            if (targetPos > UpperLimit || targetPos < LowerLimit)
                Exceptor.Throw <DriverException>(op, $"Can only move between {LowerLimit} and {UpperLimit}!");

            if (currentPosition == targetPos)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op} - target same as current, not moving");
                #endregion
                return;
            }

            if (targetPos > currentPosition && ((targetPos - currentPosition) < motionParameters[Direction.Up].stoppingDistance))
                Exceptor.Throw<InvalidOperationException>(op, $"Too short. Move at least {motionParameters[Direction.Up].stoppingDistance} positions up!");

            if (targetPos < currentPosition && ((currentPosition - targetPos) < motionParameters[Direction.Down].stoppingDistance))
                Exceptor.Throw<InvalidOperationException>(op, $"Too short. Move at least {motionParameters[Direction.Down].stoppingDistance} positions down!");

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, op);
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
                $"StartMoving: _move({dir}) - at {Position} started moving {dir} ");
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

            activityMonitor.NewActivity(new Activity.Focuser(new Activity.Focuser.StartParams
            {
                start = (int)currentPos,
                target = _targetPosition,
                intermediateTarget = _intermediatePosition,
                direction = (dir == Direction.Up) ?
                    Activity.Focuser.Direction.Up :
                    Activity.Focuser.Direction.Down,
            }));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                $"StartMovingToTarget: at {currentPos}, _target: {_targetPosition}, _intermediateTarget: {_intermediatePosition}, _state: {_state}");
            #endregion
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!Connected)
                Exceptor.Throw<NotConnectedException>("CheckConnected", message);
        }

        public ArrayList SupportedActions { get; } = new ArrayList() {
            "status",
            "start-testing",
            "end-testing",
        };

        public string Action(string action, string parameter)
        {
            action = action.ToLower();
            parameter = parameter.ToLower();

            if (action == "status")
            {
                return Digest;
            }
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
                        uint target;

                        if (UInt32.TryParse(parameter, out target))
                            Move(target);
                        else
                            return $"Bad parameter \"{parameter}\" to Action(\"move\")";
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
            {
                Exceptor.Throw<ActionNotImplementedException>($"Action({action})", "Not implemented by this driver");
                return string.Empty;
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw}", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw}", "Not implemented");
            return false;
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            Exceptor.Throw<MethodNotImplementedException>($"CommandString({command}, {raw}", "Not implemented");
            return string.Empty;
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
                return 2;
            }
        }

        private void OnTimer(object StateObject)
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
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"OnTimer: reading: {reading}, _mostRecentPosition: {_mostRecentPosition}, delta: {delta}, millis: {millis}");
            }
            #endregion

            if (_mostRecentPosition != 0 &&  delta != 0 && delta > maxDelta) {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"OnTimer: suspect reading: {reading} _mostRecentPosition: {_mostRecentPosition}, (delta: {delta} > maxDelta: {maxDelta})");
                #endregion
            }
            _mostRecentPosition = reading;

            recentPositions.Enqueue((uint) _mostRecentPosition);

            #region Check if encoder is changing
            uint[] arr = recentPositions.ToArray();

            bool changing = false;
            if (arr.Length < nRecentPositions)
            {
                changing = true;      // not enough readings yet
            }
            else
            {
                uint max = arr.Max();
                foreach (uint pos in arr)
                {
                    if (pos != max)
                    {
                        changing = true;
                        break;
                    }
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
                StartStopping($"OnTimer: at {_mostRecentPosition} Limit stop (state: {oldState})");
                return;
            }

            if (_state.IsSet(State.Flags.Stopping) && Math.Abs(_mostRecentPosition - _startStoppingPosition) > _runawayPositions)
            {
                StartStopping($"OnTimer: runaway: Started stopping at {_startStoppingPosition} now at {_mostRecentPosition}");
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
                    {
                        State.BecomeIdle();
                    }
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                        $"OnTimer: stopped moving: at {_mostRecentPosition} (target: {_targetPosition}, intermediateTarget: {_intermediatePosition}, old state: {oldState}, new state: {_state})");
                    #endregion
                }
            }

            if (_state.IsSet(State.Flags.MovingToTarget) &&
                        CloseEnough(_mostRecentPosition, _targetPosition, Direction.Up))
            {
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping($"OnTimer: close to target: at {_mostRecentPosition} (_state: {_state})");
            }

            if (_state.IsSet(State.Flags.MovingToIntermediateTarget) &&
                        CloseEnough(_mostRecentPosition, _intermediatePosition, Direction.Down))
            {
                if (!_state.IsSet(State.Flags.Stopping))
                    StartStopping($"OnTimer: close to intermediate target: at {_mostRecentPosition} (_state: {_state})");
            }

            if (!_state.IsSet(State.Flags.AnyMoving) && _encoderIsChanging)
                StartStopping("OnTimer: Runaway");
        }

        private bool CloseEnough(int current, int target, Direction dir)
        {
            if (dir == Direction.Up)
            {
                return ((target - current) <= motionParameters[Direction.Up].stoppingDistance) ||
                    (current >= target);
            }
            else if (dir == Direction.Down)
            {
                return ((current - target) <= motionParameters[Direction.Down].stoppingDistance) ||
                   (current <= target);
            }

            return false;
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
                    ret.Add($"moving-to-{_targetPosition}");
                }

                if (_state.IsSet(State.Flags.MovingToIntermediateTarget))
                {
                    ret.Add($"moving-to-{_targetPosition}-via-{_intermediatePosition}");
                }

                if (_state.IsSet(State.Flags.Stopping))
                    ret.Add("stopping");

                string s = string.Join(",", ret);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"Status: {s}");
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