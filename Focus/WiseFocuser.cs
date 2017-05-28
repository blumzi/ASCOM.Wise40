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

#if WITH_PID
using PID;
#endif

namespace ASCOM.Wise40
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        private bool _initialized = false;
        private bool _connected = false;
        private enum FocuserStatus { Idle, MovingUp, MovingAllUp, MovingDown, MovingAllDown, Stopping };
        private FocuserStatus _status = FocuserStatus.Idle;

        public TraceLogger traceLogger = new TraceLogger();
        public Debugger debugger = Debugger.Instance;

        private WisePin pinUp, pinDown;
        private WiseFocuserEnc encoder;

        public enum Direction { None, Up, Down, AllUp, AllDown };
        public class MotionParameter
        {
            public int stoppingDistance;   // number of encoder ticks to stop
        };

        Dictionary<Direction, MotionParameter> motionParameters;

        private uint _targetPos;
        private bool _movingToTarget = false;

        private double _startPos, _lastPos;

        private System.Threading.Timer movementTimer;   // Should be ON only when the focuser is moving
        private int movementTimeout = 50;               // millis between movement monitoring events

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseSite wisesite = WiseSite.Instance;

        public static string driverID = "ASCOM.Wise40.Focuser";

        private static string driverDescription = string.Format("ASCOM Wise40.Focuser v{0}", version.ToString());
#if WITH_PID
        public TimeProportionedPidController upPID, downPID;
#endif

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
                    }
                }
                return _instance;
            }
        }

#if WITH_PID
        private int readEncoder()
        {
            return (int)encoder.Value;
        }

        private ulong readOutput()
        {
            return (ulong)DateTime.Now.Ticks;
        }

        private void writeOutput(ulong value)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: writeOutput: {0}", value);
        }

        private int readSetPoint()
        {
            return (int)targetPos;
        }
#endif

        public void init(bool multiTurn = false)
        {
            if (_initialized)
                return;

            Name = "WiseFocuser";
            _status = FocuserStatus.Idle;

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();
            debugger.init(Debugger.DebugLevel.DebugLogic);
            traceLogger = new TraceLogger("", "Focuser");
            traceLogger.Enabled = debugger.Tracing;
            hardware.init();
            wisesite.init();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, direction: Const.Direction.Decreasing);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut, direction: Const.Direction.Increasing);

            connectables.AddRange(new List<IConnectable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            disposables.AddRange(new List<IDisposable> { _instance.pinUp, _instance.pinDown, _instance.encoder });

            System.Threading.TimerCallback movementTimerCallback = new System.Threading.TimerCallback(MonitorMovement);
            movementTimer = new System.Threading.Timer(movementTimerCallback);

            motionParameters = new Dictionary<Direction, MotionParameter>();
            motionParameters[Direction.Up] = new MotionParameter() { stoppingDistance = 100 };
            motionParameters[Direction.Down] = new MotionParameter() { stoppingDistance = 100 };
#if WITH_PID
            TimeSpan pidSamplingRate = new TimeSpan(0, 0, 0, 100);  // 100 milliseconds
            upPID = new TimeProportionedPidController(
                name: "focusUpPID",
                windowSizeMillis: 5000,
                pin: pinUp,
                samplingRate: pidSamplingRate,
                stopSimulatedProcess: stopSimulation,
                readProcess: readEncoder,
                readOutput: readOutput,
                readSetPoint: readSetPoint,
                writeOutput: writeOutput,
                proportionalGain: 5,
                integralGain: 2,
                derivativeGain: 1
                );

            downPID = new TimeProportionedPidController(
                name: "focusDownPID",
                windowSizeMillis: 5000,
                pin: pinDown,
                samplingRate: pidSamplingRate,
                stopSimulatedProcess: stopSimulation,
                readProcess: readEncoder,
                readOutput: readOutput,
                readSetPoint: readSetPoint,
                writeOutput: writeOutput,
                proportionalGain: 5,
                integralGain: 2,
                derivativeGain: 1
                );
#endif

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
                traceLogger.LogMessage("Connected Get", _connected.ToString());
                #endregion
                return _connected;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("Connected Set", value.ToString());
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
            traceLogger.Dispose();
        }

        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Focuser";

                encoder.UpperLimit = Convert.ToUInt32(driverProfile.GetValue(driverID, "Upper Limit", string.Empty, encoder.UpperLimit.ToString()));
                encoder.LowerLimit = Convert.ToUInt32(driverProfile.GetValue(driverID, "Lower Limit", string.Empty, encoder.LowerLimit.ToString()));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Focuser";
                driverProfile.WriteValue(driverID, "Upper Limit", encoder.UpperLimit.ToString());
                driverProfile.WriteValue(driverID, "Lower Limit", encoder.LowerLimit.ToString());
            }
        }

        public double Temperature
        {
            get
            {
                #region trace
                traceLogger.LogMessage("Temperature Get", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("Temperature", false);
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");
                #region trace
                traceLogger.LogMessage("TempCompAvailable Get", false.ToString());
                #endregion
                return false; // Temperature compensation is not available in this driver
            }
        }

        public bool TempComp
        {
            get
            {
                #region trace
                traceLogger.LogMessage("TempComp Get", false.ToString());
                #endregion
                return false;
            }
            set
            {
#region trace
                traceLogger.LogMessage("TempComp Set", "Not implemented");
#endregion
                throw new ASCOM.PropertyNotImplementedException("TempComp", false);
            }
        }

        public double StepSize
        {
            get
            {
#region trace
                traceLogger.LogMessage("StepSize Get", "Not implemented");
#endregion
                throw new ASCOM.PropertyNotImplementedException("StepSize", false);
            }
        }

        public uint position
        {
            get
            {
                uint pos = encoder.Value;
#region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "WiseFocuser: position: {0}", pos);
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
                if (!Connected)
                    throw new NotConnectedException("");
#region trace
                traceLogger.LogMessage("Absolute Get", true.ToString());
#endregion
                return true; // This is an absolute focuser
            }
        }

        public void Stop()
        {
            if (Simulated)
                encoder.stopMoving();

            movementTimer.Change(Timeout.Infinite, Timeout.Infinite);
            pinUp.SetOff();
            pinDown.SetOff();
#if !WITH_PID
            int startStopping = (int)Position, currPosition, prevPosition = startStopping;
            int travel = (int) Math.Abs(startStopping - _startPos);
#region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser:Stop Started stopping at {0} ...", startStopping);
            #endregion
            _status = FocuserStatus.Stopping;
            do
            {
                prevPosition = (int)Position;
                Thread.Sleep(50);
                currPosition = (int)Position;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser:Stop Slept 50 millis now at {0} (delta: {1})",
                    currPosition, Math.Abs(currPosition - prevPosition));
                #endregion
            }
            while (currPosition != prevPosition);
            
            int stoppingDist = Math.Abs(currPosition - startStopping);
#region debug
            if (travel != 0)
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    "WiseFocuser:Stop: travel: {0}, stopping distance: {1}, percent: {2:f2}",
                    travel, stoppingDist, (stoppingDist * 100)/ travel);
#endregion
#endif
            _movingToTarget = false;
            _status = FocuserStatus.Idle;
        }

        public void Halt()
        {
#region trace
            traceLogger.LogMessage("Halt", "");
#endregion
            if (!Connected)
                throw new NotConnectedException("");
#region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Halt");
#endregion
            Stop();
        }

        public bool IsMoving
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");

                bool ret = pinUp.isOn || pinDown.isOn;
#region trace
                traceLogger.LogMessage("Halt", ret.ToString());
#endregion
                return ret;
            }
        }

        public bool Link
        {
            get
            {
#region trace
                traceLogger.LogMessage("Link Get", this.Connected.ToString());
#endregion
                return this.Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
#region trace
                traceLogger.LogMessage("Link Set", value.ToString());
#endregion
                this.Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        public int MaxIncrement
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");
#region trace
                traceLogger.LogMessage("MaxIncrement Get", UpperLimit.ToString());
#endregion
                return (int)UpperLimit; // Maximum change in one move
            }
        }

        public int MaxStep
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");
#region trace
                traceLogger.LogMessage("MaxStep Get", UpperLimit.ToString());
#endregion
                return (int)UpperLimit; // Maximum extent of the focuser, so position range is 0 to 10,000
            }
        }

        public void Move(Direction dir)
        {
            _startPos = Position;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Starting Move({0}) at {1}",
                dir.ToString(), _startPos);
            #endregion
            _movingToTarget = false;
            switch (dir)
            {
                case Direction.Up:
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingUp;
                    break;
                case Direction.Down:
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingDown;
                    break;
                case Direction.AllUp:
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingAllUp;
                    _lastPos = Position;
                    break;
                case Direction.AllDown:
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingAllDown;
                    _lastPos = Position;
                    break;
            }

            movementTimer.Change(movementTimeout, movementTimeout);

            if (Simulated)
            {
                switch (dir)
                {
                    case Direction.AllDown:
                    case Direction.Down:
                        encoder.startMoving(Const.Direction.Decreasing);
                        break;
                    case Direction.AllUp:
                    case Direction.Up:
                        encoder.startMoving(Const.Direction.Increasing);
                        break;
                }
            }
        }

        public void Move(uint toPos)
        {
#region trace
            traceLogger.LogMessage("Move", Position.ToString());
#endregion
            if (!Connected)
                throw new NotConnectedException("Not connected!");

            if (TempComp)
                throw new InvalidOperationException("Cannot Move while TempComp == true");

            uint currentPos = Position;

            if (currentPos == toPos)
                return;

            _targetPos = toPos;
            _movingToTarget = true;
#if WITH_PID
            if (targetPos > currentPos)
            {
#region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Starting upPID");
#endregion
                encoder.startMoving(Const.Direction.Increasing);
                upPID.MoveTo(targetPos);
            }
            else
            {
#region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Starting downPID");
#endregion
                encoder.startMoving(Const.Direction.Decreasing);
                downPID.MoveTo(targetPos);
            }
#else
            if (_targetPos > currentPos)
            {
                _status = FocuserStatus.MovingUp;
                pinUp.SetOn();
                if (Simulated)
                    encoder.startMoving(Const.Direction.Increasing);
            }
            else
            {
                _status = FocuserStatus.MovingDown;
                pinDown.SetOn();
                if (Simulated)
                    encoder.startMoving(Const.Direction.Decreasing);
            }                

            movementTimer.Change(movementTimeout, movementTimeout);
#endif

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

        public ArrayList SupportedActions
        {
            get
            {
#region trace
                traceLogger.LogMessage("SupportedActions Get", "Returning empty arraylist");
#endregion
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
                if (!_instance.Connected)
                    throw new NotConnectedException("");
#region trace
                _instance.traceLogger.LogMessage("Description Get", driverDescription);
#endregion
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "Wise40 Focuser. Version: " + DriverVersion;
#region trace
                traceLogger.LogMessage("DriverInfo Get", driverInfo);
#endregion
                return driverInfo;
            }
        }

        public static string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
#region trace
                _instance.traceLogger.LogMessage("DriverVersion Get", driverVersion);
#endregion
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
#region trace
                traceLogger.LogMessage("InterfaceVersion Get", "2");
#endregion
                return Convert.ToInt16("2");
            }
        }

        private void MonitorMovement(object StateObject)
        {
            if (pinUp.isOn)
            {
                if (_movingToTarget && (_targetPos - Position) <= motionParameters[Direction.Up].stoppingDistance)
                    Stop();

                if (_status == FocuserStatus.MovingAllUp)
                {
                    uint currPos = Position;

                    if (_lastPos == currPos)
                        Stop();
                    _lastPos = currPos;
                }
            }
            else if (pinDown.isOn)
            {
                if (Position - _targetPos <= motionParameters[Direction.Down].stoppingDistance)
                    Stop();

                if (_status == FocuserStatus.MovingAllDown)
                {
                    uint currPos = Position;

                    if (_lastPos == currPos)
                        Stop();
                    _lastPos = currPos;
                }
            }
        }

        public string Status
        {
            get
            {
                string ret = "Idle";

                switch (_status)
                {
                    case FocuserStatus.MovingUp:
                        ret = "Moving Up";
                        if (_movingToTarget)
                            ret += string.Format(" to {0}", _targetPos);
                        break;
                    case FocuserStatus.MovingDown:
                        ret = "Moving Down";
                        if (_movingToTarget)
                            ret += string.Format(" to {0}", _targetPos);
                        break;
                    case FocuserStatus.MovingAllUp:
                        ret = "Moving All Up";
                        break;
                    case FocuserStatus.MovingAllDown:
                        ret = "Moving All Down";
                        break;
                    case FocuserStatus.Stopping:
                        ret = "Stopping";
                        break;
                }
                return ret;
            }
        }

        public uint UpperLimit
        {
            get
            {
                return encoder.UpperLimit;
            }

            set
            {
                encoder.UpperLimit = value;
            }
        }

        public uint LowerLimit
        {
            get
            {
                return encoder.LowerLimit;
            }

            set
            {
                encoder.LowerLimit = value;
            }
        }

        public int stopSimulation()
        {
            if (Simulated)
                encoder.stopMoving();
            return 0;
        }
    }
}
