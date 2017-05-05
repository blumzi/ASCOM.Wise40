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

using PID;

namespace ASCOM.Wise40
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        private static readonly WiseFocuser _instance = new WiseFocuser();
        private bool _initialized = false;
        private bool _connected = false;
        private enum FocuserStatus { Idle, MovingUp, MovingAllUp, MovingDown, MovingAllDown };
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

        private uint targetPos;

        private double _startPos; //, _stopPos, _endPos;

        private System.Threading.Timer movementTimer;   // Should be ON only when the focuser is moving
        private int movementTimeout = 50;               // millis between movement monitoring events

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseSite wisesite = WiseSite.Instance;

        internal static string driverID = "ASCOM.Wise40.Focuser";

        private static string driverDescription = string.Format("ASCOM Wise40.Focuser v{0}", version.ToString());

        public TimeProportionedPidController upPID, downPID;

        public WiseFocuser() { }

        public static WiseFocuser Instance
        {
            get
            {
                return _instance;
            }
        }

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

        public void init(bool multiTurn = false)
        {
            if (_initialized)
                return;

            Name = "WiseFocuser";
            _status = FocuserStatus.Idle;

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();
            //debugger.init(Debugger.DebugLevel.DebugLogic | Debugger.DebugLevel.DebugEncoders);
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

            pinUp.SetOff();
            pinDown.SetOff();
            movementTimer.Change(Timeout.Infinite, Timeout.Infinite);
            /*
                    _stopPos = (int)Position;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Started stopping at {0} ...", _stopPos);
                    #endregion
                    DateTime start = DateTime.Now;
                    while ((DateTime.Now - start).TotalMilliseconds < 1000)
                    {
                        //debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Stopping, now at {0}", Position);
                        Thread.Sleep(50);
                    }
                    _endPos = (double) Position;
                    double travel = Math.Abs(_stopPos - _startPos), stoppingDist = Math.Abs(_endPos - _stopPos);
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "stopping: travel: {0}, stopping distance: {1}, {2}%",
                        travel, stoppingDist, (stoppingDist / travel) * 100);
            */
            targetPos = 0;
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Starting Move({0}) at {1}",
                dir.ToString(), _startPos);
            switch (dir)
            {
                case Direction.Up:
                    targetPos = UpperLimit;
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingUp;
                    break;
                case Direction.Down:
                    targetPos = LowerLimit;
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingDown;
                    break;
                case Direction.AllUp:
                    targetPos = UpperLimit;
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingAllUp;
                    break;
                case Direction.AllDown:
                    targetPos = LowerLimit;
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingAllDown;
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

        public void Move(uint pos)
        {
            #region trace
            traceLogger.LogMessage("Move", Position.ToString());
            #endregion
            if (!Connected)
                throw new NotConnectedException("Not connected!");

            if (TempComp)
                throw new InvalidOperationException("Cannot Move while TempComp == true");

            uint currentPos = Position;

            if (currentPos == pos)
                return;

            targetPos = pos;
            /*
            if (targetPos > currentPos)
            {
                _status = FocuserStatus.MovingUp;     
                pinUp.SetOn();
            }
            else
            {
                _status = FocuserStatus.MovingDown;
                pinDown.SetOn();
            }
            
            movementTimer.Change(movementTimeout, movementTimeout);
            */
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

        public string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                #region trace
                traceLogger.LogMessage("DriverVersion Get", driverVersion);
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
                if (targetPos - Position <= motionParameters[Direction.Up].stoppingDistance)
                    Stop();
            }
            else if (pinDown.isOn)
            {
                if (Position - targetPos <= motionParameters[Direction.Down].stoppingDistance)
                    Stop();
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
                        ret = "Moving Down";
                        break;
                    case FocuserStatus.MovingDown:
                        ret = "Moving Up";
                        break;
                    case FocuserStatus.MovingAllUp:
                        ret = string.Format("Moving All Up to {0}", UpperLimit);
                        break;
                    case FocuserStatus.MovingAllDown:
                        ret = string.Format("Moving All Down to {0}", LowerLimit);
                        break;
                }
                return ret;
            }
        }

        public void SetZero()
        {
            encoder.SetZero();
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
