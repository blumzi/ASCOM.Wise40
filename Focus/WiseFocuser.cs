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
        private readonly Exceptor FocusExceptor = new Exceptor(Debugger.DebugLevel.DebugFocuser);

        public class Reading
        {
            public int position;
            public DateTime time;
        }
        public class Motion
        {
            public int start;               // starting position
            public int target;              // target position
            public int intermediate;        // intermediate target
            public int startStopping;       // position where the stopping started
            public int stop;                // actual stopping position
            public Direction dirOrig;       // direction to target (not to intermediate)
            public Direction dirCurrent;    // current direction (either to target or to intermediate)

            private static readonly WiseFocuser focuser = WiseFocuser.Instance;
            private static readonly Debugger debugger = Debugger.Instance;

            public Motion(int targetPosition = noPosition, Direction direction = Direction.None)
            {
                dirCurrent = dirOrig = direction;
                target = targetPosition;
                State = new State()
                {
                    _flags = State.Flags.None,
                };
            }
            public void Start()
            {
                if (target == noPosition && dirOrig == Direction.None)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, "motion.Start(): no target and no direction");
                    #endregion
                    return;
                }

                if (target == noPosition)
                {
                    if (dirCurrent == Direction.Up)
                        State.Set(State.Flags.MovingUp);
                    else if (dirCurrent == Direction.Down)
                        State.Set(State.Flags.MovingDown);

                    StartMoving();      // dirCurrent is not Direction.None
                    return;
                }
                else if (target == focuser.UpperLimit)
                {
                    dirCurrent = Direction.Up;
                    State.Set(State.Flags.MovingUp);
                    StartMoving();
                    return;
                }
                else if (target == focuser.LowerLimit)
                {
                    dirCurrent = Direction.Down;
                    State.Set(State.Flags.MovingDown);
                    StartMoving();
                    return;
                }

                int distance = (int) Math.Abs(target - start);

                if (target > start)
                {
                    dirOrig = Direction.Up;
                    if (distance < focuser.motionParameters[dirOrig].stoppingDistance)
                    {
                        // To move UP less than 10 units we need to first move down
                        intermediate = target - focuser.motionParameters[dirOrig].compensation;
                        State.Set(State.Flags.MovingToIntermediateTarget);
                        dirCurrent = Direction.Down;
                        StartMoving();
                    }
                    else
                    {
                        // Moving UP more than 10 units just works
                        intermediate = noPosition;
                        State.Set(State.Flags.MovingToTarget);
                        dirCurrent = Direction.Up;
                        StartMoving();
                    }
                }
                else
                {
                    dirCurrent = dirOrig = Direction.Down;
                    intermediate = target - focuser.motionParameters[dirCurrent].compensation;
                    State.Set(State.Flags.MovingToIntermediateTarget);
                    StartMoving();
                }

                activityMonitor.NewActivity(new Activity.Focuser(new Activity.Focuser.StartParams
                {
                    start = start,
                    target = target,
                    intermediateTarget = intermediate,
                    direction = (dirOrig == Direction.Up) ?
                        Activity.Focuser.Direction.Up :
                        Activity.Focuser.Direction.Down,
                }));

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"motion.Start: start: {start}, target: {target}, intermediate: {intermediate}, state: [{State}]");
                #endregion
            }
            public void StartMoving()
            {
                WisePin pin = null;

                switch (dirCurrent)
                {
                    case Direction.Up:
                        pin = focuser.pinUp;
                        if (Simulated)
                            focuser.encoder.startMoving(Const.Direction.Increasing);
                        break;

                    case Direction.Down:
                        pin = focuser.pinDown;
                        if (Simulated)
                            focuser.encoder.startMoving(Const.Direction.Decreasing);
                        break;
                }

                if (pin != null)
                    pin.SetOn();
            }

            public void StartStopping(string reason)
            {
                if (State.IsSet(State.Flags.Stopping))
                    return;

                State.Set(State.Flags.Stopping);
                if (Simulated)
                    focuser.encoder.stopMoving();

                focuser.pinUp.SetOff();
                focuser.pinDown.SetOff();
                startStopping = (int) focuser.Position;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"motion.StartStopping: position: {startStopping}, state: [{State}], reason: ({reason}) ...");
                #endregion
                Thread.Sleep(50);
            }

            public bool Runaway
            {
                get
                {
                    int limit;
                    int position = (int) focuser.Position;
                    string op = $"motion.Runaway: at {position}";

                    limit = focuser.motionParameters[dirCurrent].runawayPositions;
                    if (dirCurrent == Direction.Up)
                    {
                        if (State.IsSet(State.Flags.MovingToTarget) && (position - target) > limit)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op}: overshoot target {target} by more than {limit} units");
                            #endregion
                            return true;
                        }
                        else if(State.IsSet(State.Flags.MovingToIntermediateTarget) && (position - intermediate) > limit)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op}: overshoot intermediate target {intermediate} by more than {limit} units");
                            #endregion
                            return true;
                        }
                    }
                    else
                    {
                        if (State.IsSet(State.Flags.MovingToTarget) && (target - position) > limit)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op}: undershoot target {target} by more than {limit} units");
                            #endregion
                            return true;
                        }
                        else if (State.IsSet(State.Flags.MovingToIntermediateTarget) && (intermediate - position) > limit)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op}: undershoot intermediate target {intermediate} by more than {limit} units");
                            #endregion
                            return true;
                        }
                    }
                    return false;
                }
            }

            public State State { get; }
        }
        public static Motion motion;
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
                Stuck = (1 << 5),
                Runaway = (1 << 6),
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

        private const int noPosition = -1;

        public Debugger debugger = Debugger.Instance;

        private WisePin pinUp, pinDown;
        private WiseFocuserEnc encoder;

        private static readonly WiseSafeToOperate safetooperate = WiseSafeToOperate.Instance;
        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public enum Direction { None, Up, Down, AllUp, AllDown };
        public class MotionParameters
        {
            public int stoppingDistance;   // number of encoder ticks to stop
            public int compensation;
            public int runawayPositions;
        };

        private Dictionary<Direction, MotionParameters> motionParameters;

        private static Reading _latestReading, _previousReading;

        private const int nRecentPositions = 5;
        private readonly FixedSizedQueue<Reading> readings = new FixedSizedQueue<Reading>(nRecentPositions);

        private static System.Threading.Timer movementMonitoringTimer;
        private const int movementMonitoringMillis = 50;

        private readonly List<IConnectable> connectables = new List<IConnectable>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        private readonly Hardware.Hardware hardware = Hardware.Hardware.Instance;

        public static string driverID = Const.WiseDriverID.Focus;

        public WiseFocuser() { }
        static WiseFocuser() { }

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
            movementMonitoringTimer.Change(movementMonitoringMillis, Timeout.Infinite);

            motionParameters = new Dictionary<Direction, MotionParameters>
            {
                [Direction.Up] = new MotionParameters() {
                    stoppingDistance = 10, compensation = 100, runawayPositions = 500,
                },
                [Direction.Down] = new MotionParameters() {
                    stoppingDistance = 10, compensation = 50, runawayPositions = 500,
                },
            };

            _latestReading = new Reading
            {
                position = (int)encoder.Value,
                time = DateTime.Now,
            };
            readings.Enqueue(_latestReading);

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

                ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.Focus, _connected, line: ActivityMonitor.Tracer.focuser.Line));
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var disposable in disposables)
                    disposable.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                FocusExceptor.Throw<PropertyNotImplementedException>("Temperature", "Not implemented");
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
                FocusExceptor.Throw<PropertyNotImplementedException>("TempComp", "Not implemented", true);
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
                int pos = _latestReading.position;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"MostRecentPosition - Get: {pos}");
                #endregion
                return (uint) pos;
            }
        }

        public uint Position
        {
            get
            {
                if (!Connected)
                    Connected = true;
                //FocusExceptor.Throw<NotConnectedException>("Position", "Not connected");

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

        public void Halt(string reason = "Halt")
        {
            if (!Connected)
                FocusExceptor.Throw<NotConnectedException>("Halt", "Not connected");

            if (motion != null)
                motion.StartStopping(reason);
        }

        public bool IsMoving
        {
            get
            {
                if (!Connected)
                    FocusExceptor.Throw<NotConnectedException>("IsMoving", "Not connected");

                if (motion == null)
                    return false;

                bool ret = motion.State.IsSet(State.Flags.AnyMoving) || pinUp.isOn || pinDown.isOn || EncoderIsChanging;
                #region debug
                string dbg = $"IsMoving: ret: {ret}, state: [{motion.State}]";
                if (pinUp.isOn)
                    dbg += ", pinUp: ON";
                if (pinDown.isOn)
                    dbg += ", pinDown: ON";
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,dbg);
                #endregion
                return ret;
            }
        }

        public bool Link
        {
            get
            {
                return Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }

            set
            {
                Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        public int MaxIncrement
        {
            get
            {
                return (int) UpperLimit; // Maximum change in one move
            }
        }

        public int MaxStep
        {
            get
            {
                return (int) UpperLimit; // Maximum extent of the focuser, so position range is 0 to 10,000
            }
        }

        public void Move(Direction dir)
        {
            string op = $"Move(dir: {dir}): ";

            if (!Connected)
                FocusExceptor.Throw<NotConnectedException>(op, "Not connected!");

            if (IsMoving)
                FocusExceptor.Throw<InvalidOperationException>(op, "Cannot move, already moving");

            if (!safetooperate.IsSafe)
                FocusExceptor.Throw<InvalidOperationException>(op, string.Join(", ", safetooperate.UnsafeReasonsList()));

            if (TempComp)
                FocusExceptor.Throw<InvalidOperationException>(op, "Cannot Move while TempComp == true");

            if (!safetooperate.IsSafeWithoutCheckingForShutdown())
                FocusExceptor.Throw<InvalidOperationException>(op, string.Join(", ", safetooperate.UnsafeReasonsList()));

            motion = new Motion()
            {
                target = noPosition,
                dirOrig = dir,
                dirCurrent = dir,
            };

            switch (dir)
            {
                case Direction.Up:
                case Direction.Down:
                    break;

                case Direction.AllUp:
                    motion.target = (int) UpperLimit;
                    break;
                case Direction.AllDown:
                    motion.target = (int)LowerLimit;
                    break;
            }
            motion.Start();
        }

        public void Move(uint targetPosition)
        {
            uint currentPosition = Position;
            string op = $"Move(from: {currentPosition} to {targetPosition})";

            if (!Connected)
                FocusExceptor.Throw<NotConnectedException>(op, "Not connected!");

            if (IsMoving)
                FocusExceptor.Throw<InvalidOperationException>(op, "Cannot move, already moving");

            if (!safetooperate.IsSafe)
                FocusExceptor.Throw<InvalidOperationException>(op, string.Join(", ", safetooperate.UnsafeReasonsList()));

            if (TempComp)
                FocusExceptor.Throw<InvalidOperationException>(op, "Cannot Move while TempComp == true");

            if (targetPosition > UpperLimit || targetPosition < LowerLimit)
                FocusExceptor.Throw <DriverException>(op, $"Can only move between {LowerLimit} and {UpperLimit}!");

            if (currentPosition == targetPosition)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"{op} - target same as current, not moving");
                #endregion
                return;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, op);
            #endregion

            motion = new Motion()
            {
                start = (int)currentPosition,
                target = (int)targetPosition,
            };

            motion.Start();
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!Connected)
                FocusExceptor.Throw<NotConnectedException>("CheckConnected", message);
        }

        public ArrayList SupportedActions { get; } = new ArrayList() {
            "status",
            "start-debugging",
            "end-debugging",
            "debug",
            "move",
            "halt",
            "stop",
            "limit",
        };

        public string Action(string action, string parameter)
        {
            action = action.ToLower();
            parameter = parameter.ToLower();

            if (action == "status")
            {
                return Digest;
            }
            else if (action == "start-debugging")
            {
                _debugging = true;
                return JsonConvert.SerializeObject(_debugging);
            }
            else if (action == "end-debugging")
            {
                _debugging = false;
                return JsonConvert.SerializeObject(_debugging);
            }
            else if (action == "debug")
            {
                if (!String.IsNullOrEmpty(parameter))
                {
                    Debugger.DebugLevel newDebugLevel;
                    try
                    {
                        Enum.TryParse<Debugger.DebugLevel>(parameter, out newDebugLevel);
                        Debugger.SetCurrentLevel(newDebugLevel);
                    }
                    catch
                    {
                        return $"Cannot parse DebugLevel \"{parameter}\"";
                    }
                }
                return $"{Debugger.Level}";
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
                FocusExceptor.Throw<ActionNotImplementedException>($"Action({action})", "Not implemented by this driver");
                return string.Empty;
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            FocusExceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw}", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            FocusExceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw}", "Not implemented");
            return false;
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            FocusExceptor.Throw<MethodNotImplementedException>($"CommandString({command}, {raw}", "Not implemented");
            return string.Empty;
        }

        public static string DriverId
        {
            get
            {
                return driverID;
            }
        }

        public static string Description { get; } = $"{driverID} v{version}";

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
            //
            // The typical encoder change rate was measured at less than 0.05 ticks/milli.
            //
            // Readings that produce a larger value are flagged (debug) to detect noise interferences on
            //  the encoder lines.
            //
            const double maxEncoderChangeRatePerMilli = 0.06;

            #region Get encoder reading
            _previousReading = _latestReading;
            _latestReading = new Reading
            {
                position = (int) encoder.Value,
                time = DateTime.Now,
            };
            double deltaMillis = _latestReading.time.Subtract(_previousReading.time).TotalMilliseconds;

            int deltaPositions = Math.Abs(_latestReading.position - _previousReading.position);
            double maxDeltaPosition = maxEncoderChangeRatePerMilli * deltaMillis;

            #region debug
            if (_debugging)
            {
                if (_latestReading.position != _previousReading.position)
                    debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                        $"OnTimer: _latestReading.position: {_latestReading.position}, delta: {deltaPositions}, deltaMillis: {deltaMillis}," +
                        $"rate: {deltaPositions/deltaMillis} positions/milli");
            }
            #endregion

            if (_latestReading.position != 0 && deltaPositions != 0 && deltaPositions > maxDeltaPosition)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"OnTimer: Too much movement: _latestReading.position: {_latestReading.position}, (delta: {deltaPositions} > maxDelta: {maxDeltaPosition})");
                #endregion
            }

            readings.Enqueue(_latestReading);
            #endregion

            #region Check if encoder is changing
            Reading[] arr = readings.ToArray();

            bool _encoderIsChanging = false;
            if (arr.Length > 1)
            {
                for (int i = 1; i < arr.Length; i++)
                {
                    if (arr[i].position != arr[0].position)
                    {
                        _encoderIsChanging = true;
                        break;
                    }
                }
            }

            EncoderIsChanging = _encoderIsChanging;
            #endregion

            if (motion == null)
                goto Done;

            State oldState = motion.State;

            #region Stuck?
            if ((pinDown.isOn || pinDown.isOn) && !EncoderIsChanging)
            {
                WisePin pin = pinUp.isOn ? pinUp : pinDown;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                    $"OnTimer: stuck: position: {Position}, {pin}.isOn, encoder not changing!");
                #endregion
                motion.State.Set(State.Flags.Stuck);
            }
            else
                motion.State.Unset(State.Flags.Stuck);
            #endregion

            #region Reached upper/lower limit
            if (
                ((pinUp.isOn || motion.State.IsSet(State.Flags.MovingUp)) &&
                        CloseEnough(_latestReading.position, (int) UpperLimit, Direction.Up)) ||
                ((pinDown.isOn || motion.State.IsSet(State.Flags.MovingDown)) &&
                        CloseEnough(_latestReading.position, (int) LowerLimit, Direction.Down))
               )
            {
                motion.StartStopping($"OnTimer: at {_latestReading.position} Limit stop (state: [{oldState}])");
                goto Done;
            }
            #endregion

            #region Runaway
            if (motion.Runaway)
            {
                motion.StartStopping("Runaway");
                goto Done;
            }
            #endregion

            #region Stopping
            if (motion.State.IsSet(State.Flags.Stopping) && !_encoderIsChanging)
            {
                motion.State.Unset(State.Flags.Stopping);

                if (motion.State.IsSet(State.Flags.AnyMoving))
                {
                    // Done moving
                    bool wasMovingToIntermediateTarget = motion.State.IsSet(State.Flags.MovingToIntermediateTarget);

                    motion.State.Unset(State.Flags.AnyMoving);

                    if (wasMovingToIntermediateTarget)
                    {
                        // Done moving to intermediate target, start moving to target
                        motion.State.Set(State.Flags.MovingToTarget);
                        motion.dirCurrent = Direction.Up;
                        motion.StartMoving();
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugFocuser,
                            $"OnTimer: position: {_latestReading.position}, stopped moving to intermediate: {motion.intermediate}, started moving to target: {motion.target}");
                        #endregion
                    }
                    else
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugFocuser, $"OnTimer: position: {Position}, target: {motion.target}, motion ended, became idle");
                        #endregion
                        motion = null;
                        State.BecomeIdle();
                    }
                    goto Done;
                }
            }
            #endregion

            #region Reached target while MovingToTarget
            if (motion.State.IsSet(State.Flags.MovingToTarget) &&
                        CloseEnough(_latestReading.position, motion.target, Direction.Up))
            {
                if (!motion.State.IsSet(State.Flags.Stopping))
                {
                    motion.StartStopping($"OnTimer: position: {_latestReading.position}, close to target: {motion.target}");
                    goto Done;
                }
            }
            #endregion

            #region Reached intermediate target
            if (motion.State.IsSet(State.Flags.MovingToIntermediateTarget) &&
                        CloseEnough(_latestReading.position, motion.intermediate, Direction.Down))
            {
                if (!motion.State.IsSet(State.Flags.Stopping))
                {
                    motion.StartStopping($"OnTimer: close to intermediate target: at {_latestReading.position} (state: [{motion.State}])");
                    goto Done;
                }
            }
            #endregion

            #region Not moving but encoder is changing
            if (!motion.State.IsSet(State.Flags.AnyMoving) && _encoderIsChanging)
            {
                string reason = $"OnTimer: Encoder is changing while focuser is not moving: state: [{motion.State}], readings: [ ";
                foreach (var reading in arr)
                    reason += $"{reading.position} ";
                reason += "]";
                motion.StartStopping(reason);
                goto Done;
            }
            #endregion

        Done:
            movementMonitoringTimer.Change(movementMonitoringMillis, Timeout.Infinite);
        }

        public bool EncoderIsChanging { get; set; }

        private bool CloseEnough(int currentPos, int targetPos, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return ((targetPos - currentPos) <= motionParameters[dir].stoppingDistance) || (currentPos >= targetPos);
                case Direction.Down:
                    return ((currentPos - targetPos) <= motionParameters[dir].stoppingDistance) || (currentPos <= targetPos);
                default:
                    return false;
            }
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

                if (motion == null)
                    return "";

                if (motion.State.IsSet(State.Flags.MovingUp))
                {
                    ret.Add("moving-up");
                }

                if (motion.State.IsSet(State.Flags.MovingDown))
                {
                    ret.Add("moving-down");
                }

                if (motion.State.IsSet(State.Flags.MovingToTarget))
                {
                    ret.Add($"moving-to-{motion.target}");
                }

                if (motion.State.IsSet(State.Flags.MovingToIntermediateTarget))
                {
                    ret.Add($"moving-to-{motion.target}-via-{motion.intermediate}");
                }

                if (motion.State.IsSet(State.Flags.Stopping))
                    ret.Add("stopping");

                if (motion.State.IsSet(State.Flags.Stuck))
                    ret.Add($"stuck at {Position}");

                if (motion.State.IsSet(State.Flags.Runaway))
                    ret.Add("runaway");

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