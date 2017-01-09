using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.Globalization;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private Version version = new Version(0, 1);
        private static readonly WiseFocuser instance = new WiseFocuser();
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
        
#if MULTI_TURN_ENCODER
        private const int focuserSteps = 10000;     // TBD: actual value at max position
#else
        private const int focuserSteps = 128;
#endif
        private int targetPos;
        
        private System.Threading.Timer simulationTimer;
        private int simulatedMotionTimeout = 100;       // millis between simulated encoder bumping events

        private System.Threading.Timer movementTimer;   // Should be ON only when the focuser is moving
        private int movementTimeout = 50;               // millis between movement monitoring events

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseSite wisesite = WiseSite.Instance;

        internal static string driverID = "ASCOM.Wise40.Focuser";

        private static string driverDescription = "ASCOM Wise40 Focuser";

        public WiseFocuser() {}

        public static WiseFocuser Instance
        {
            get
            {
                return instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "WiseFocuser";
            _status = FocuserStatus.Idle;

            ReadProfile();
            debugger.init(Debugger.DebugLevel.DebugLogic | Debugger.DebugLevel.DebugEncoders);
            traceLogger = new TraceLogger("", "Focuser");
            traceLogger.Enabled = debugger.Tracing;
            hardware.init();
            wisesite.init();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut);
            encoder = new WiseFocuserEnc(/* true */);
            
            connectables.AddRange(new List<IConnectable> { instance.pinUp, instance.pinDown, instance.encoder });
            disposables.AddRange(new List<IDisposable> { instance.pinUp, instance.pinDown, instance.encoder });


            System.Threading.TimerCallback simulationTimerCallback = new System.Threading.TimerCallback(SimulateMovement);
            simulationTimer = new System.Threading.Timer(simulationTimerCallback);

            System.Threading.TimerCallback movementTimerCallback = new System.Threading.TimerCallback(MonitorMovement);
            movementTimer = new System.Threading.Timer(movementTimerCallback);

            motionParameters = new Dictionary<Direction, MotionParameter>();
            motionParameters[Direction.Up] = new MotionParameter() { stoppingDistance = 2 };
            motionParameters[Direction.Down] = new MotionParameter() { stoppingDistance = 3 };

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
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
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

        public int position
        {
            get
            {
                int pos = (int)encoder.Value;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "Focuser: position: {0}", pos);
                #endregion
                return pos;
            }
        }
        public int Position
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
            pinUp.SetOff();
            pinDown.SetOff();
            if (Simulated)
                simulationTimer.Change(0, 0);
            movementTimer.Change(0, 0);
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Focuser: Halt");
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
                traceLogger.LogMessage("MaxIncrement Get", focuserSteps.ToString());
                #endregion
                return focuserSteps; // Maximum change in one move
            }
        }

        public int MaxStep
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("");
                #region trace
                traceLogger.LogMessage("MaxStep Get", focuserSteps.ToString());
                #endregion
                return focuserSteps; // Maximum extent of the focuser, so position range is 0 to 10,000
            }
        }

        public void Move(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    targetPos = -1;
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingUp;
                    break;
                case Direction.Down:
                    targetPos = -1;
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingDown;
                    break;
                case Direction.AllUp:
                    targetPos = int.MaxValue;
                    pinUp.SetOn();
                    _status = FocuserStatus.MovingAllUp;
                    break;
                case Direction.AllDown:
                    targetPos = int.MinValue;
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingAllDown;
                    break;
            }

            if (Simulated)
                simulationTimer.Change(simulatedMotionTimeout, simulatedMotionTimeout);
            movementTimer.Change(movementTimeout, movementTimeout);
        }

        public void Move(int pos)
        {
            #region trace
            traceLogger.LogMessage("Move", Position.ToString());
            #endregion
            if (!Connected)
                throw new NotConnectedException("Not connected!");

            if (TempComp)
                throw new InvalidOperationException("Cannot Move while TempComp == true");

            int currentPos = Position;

            if (currentPos == pos)
                return;

            targetPos = pos;
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

            if (Simulated)
                simulationTimer.Change(simulatedMotionTimeout, simulatedMotionTimeout);
            movementTimer.Change(movementTimeout, movementTimeout);
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

        public string Description
        {
            // TODO customise this device description
            get
            {
                if (! Connected)
                    throw new NotConnectedException("");
                #region trace
                traceLogger.LogMessage("Description Get", driverDescription);
                #endregion
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + DriverVersion;
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

        private void SimulateMovement(object StateObject)
        {
            Direction dir = pinUp.isOn ? Direction.Up : (pinDown.isOn ? Direction.Down : Direction.None);

            switch (dir)
            {
                case Direction.Up:
                case Direction.AllUp:
                    if (encoder.Value < MaxStep)
                        encoder.Value++;
                    break;
                case Direction.Down:
                case Direction.AllDown:
                    if (encoder.Value > 0)
                        encoder.Value--;
                    break;
            }
        }

        private void MonitorMovement(object StateObject)
        {
            if (pinUp.isOn && targetPos != -1 && targetPos != int.MaxValue)
            {
                if (targetPos - Position <= (Simulated ? 0 : motionParameters[Direction.Up].stoppingDistance))
                    Stop();
            }
            else if (pinDown.isOn && targetPos != -1 && targetPos != int.MinValue)
            {
                if (Position - targetPos <= (Simulated ? 0 : motionParameters[Direction.Down].stoppingDistance))
                    Stop();
            }
        }

        public string Status
        {
            get
            {
                string ret = "";

                switch (_status)
                {
                    case FocuserStatus.MovingUp:
                        ret = "Moving Up";
                        if (targetPos != -1)
                            ret += string.Format(" to {0}", targetPos);
                        break;
                    case FocuserStatus.MovingDown:
                        ret = "Moving Down";
                        if (targetPos != -1)
                            ret += string.Format(" to {0}", targetPos);
                        break;
                    case FocuserStatus.MovingAllUp:
                        ret = "Moving All Up";
                        break;
                    case FocuserStatus.MovingAllDown:
                        ret = "Moving All Down";
                        break;
                }
                return ret;
            }
        }

        public void SetZero()
        {
            encoder.SetZero();
        }
    }
}
