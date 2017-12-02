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

namespace ASCOM.Wise40.Focuser
{
    public class WiseFocuser : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        private bool _initialized = false;
        private bool _connected = false;
        private bool _needUpwardCompensation = false;
        private uint _upwardCompensation = 100;
        private enum FocuserStatus { Idle, MovingUp, MovingAllUp, MovingDown, MovingAllDown, Stopping };
        private FocuserStatus _status = FocuserStatus.Idle;

        private ASCOM.Utilities.TraceLogger tl;
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
        private uint _realTarget;
        private bool _movingToTarget = false;
        private uint _start;
        private uint _startStopping;
        private uint _endStopping;
        private uint _travel;

        private FixedSizedQueue<uint> recentPositions = new FixedSizedQueue<uint>(3);

        private System.Threading.Timer movementMonitoringTimer;   // Should be ON only when the focuser is moving
        private int movementMonitoringTimeout = 35;     // millis between movement monitoring events

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseSite wisesite = WiseSite.Instance;

        public static string driverID = "ASCOM.Wise40.Focuser";

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
            _status = FocuserStatus.Idle;

            encoder = WiseFocuserEnc.Instance;
            encoder.init(true);
            ReadProfile();
            debugger.init();
            tl = new TraceLogger("", "Focuser");
            tl.Enabled = debugger.Tracing;

            //tl.LogMessage("init", "Initializing ...");
            hardware.init();
            wisesite.init();

            pinDown = new WisePin("FocusDown", hardware.miscboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, direction: Const.Direction.Decreasing);
            pinUp = new WisePin("FocusUp", hardware.miscboard, DigitalPortType.FirstPortCH, 1, DigitalPortDirection.DigitalOut, direction: Const.Direction.Increasing);

            connectables.AddRange(new List<IConnectable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            disposables.AddRange(new List<IDisposable> { _instance.pinUp, _instance.pinDown, _instance.encoder });
            
            movementMonitoringTimer = new System.Threading.Timer(new TimerCallback(MonitorMovement));

            motionParameters = new Dictionary<Direction, MotionParameter>();
            motionParameters[Direction.Up] = new MotionParameter() { stoppingDistance = 10 };
            motionParameters[Direction.Down] = new MotionParameter() { stoppingDistance =  10 };
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
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Focuser";
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
            }
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
                double ret = 50000.0 / (encoder.UpperLimit - encoder.LowerLimit);
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
                bool ret = true;
                #region trace
                //tl.LogMessage("Absolute Get", ret.ToString());
                #endregion
                return ret; // This is an absolute focuser
            }
        }

        /// <summary>
        /// If all the recentPositions (enqueued by the movementMonitorTimer callback) are the
        ///  same, change status from Stopping to Idle and stop the timer.
        /// </summary>
        private bool FullyStopped
        {
            get
            {
                uint[] recent = recentPositions.ToArray();
                if (recent.Count() == 0)
                    return true;

                uint first = recent[0];

                if (first == 0)     // just initialized
                    return false;

                for (int i = 1; i < recent.Count(); i++)
                    if (recent[i] != first)
                        return false;

                movementMonitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _endStopping = first;
                _travel = (_endStopping > _start) ? _endStopping - _start : _start - _endStopping;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser:FullyStopped: _target: {0}, _start: {1}, _startStopping: {2}, _endStopping: {3}, _travel: {4}",
                    _targetPos, _start, _startStopping, _endStopping, _travel);
                #endregion
                _movingToTarget = false;
                //_targetPos = 0;
                _status = FocuserStatus.Idle;
                if (_needUpwardCompensation)
                {
                    Move(_realTarget);
                    _needUpwardCompensation = false;
                }
                return true;
            }
        }

        public void Stop()
        {
            if (Simulated)
                encoder.stopMoving();
            
            pinUp.SetOff();
            pinDown.SetOff();
            _startStopping = Position;
            _status = FocuserStatus.Stopping;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser:Stop Started stopping at {0} ...", _startStopping);
            #endregion
            while (!FullyStopped)
                Thread.Sleep(100);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser:Stop Stopped at {0} ...", Position);
            #endregion
        }

        public void Halt()
        {
            #region trace
            //tl.LogMessage("Halt", "");
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

                bool ret = pinUp.isOn || pinDown.isOn || !FullyStopped;
                #region trace
                //tl.LogMessage("Halt", ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool Link
        {
            get
            {
                #region trace
                //tl.LogMessage("Link Get", this.Connected.ToString());
                #endregion
                return this.Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
                #region trace
                //tl.LogMessage("Link Set", value.ToString());
                #endregion
                this.Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        public int MaxIncrement
        {
            get
            {
                #region trace
                //tl.LogMessage("MaxIncrement Get", UpperLimit.ToString());
                #endregion
                return (int)UpperLimit; // Maximum change in one move
            }
        }

        public int MaxStep
        {
            get
            {
                #region trace
                //tl.LogMessage("MaxStep Get", UpperLimit.ToString());
                #endregion
                return (int)UpperLimit; // Maximum extent of the focuser, so position range is 0 to 10,000
            }
        }

        public void Move(Direction dir)
        {
            _start = Position;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFocuser: Starting Move({0}) at {1}",
                dir.ToString(), _start);
            #endregion
            _movingToTarget = false;
            _startStopping = _endStopping = _travel = 0;
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
                    break;
                case Direction.AllDown:
                    pinDown.SetOn();
                    _status = FocuserStatus.MovingAllDown;
                    break;
            }

            movementMonitoringTimer.Change(movementMonitoringTimeout, movementMonitoringTimeout);

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
            //tl.LogMessage("Move", Position.ToString());
            #endregion
            if (!Connected)
                throw new NotConnectedException("Not connected!");

            if (TempComp)
                throw new InvalidOperationException("Cannot Move while TempComp == true");

            if (toPos > encoder.UpperLimit || toPos < encoder.LowerLimit)
                throw new DriverException(string.Format("Can only move between {0} and {1}!", encoder.LowerLimit, encoder.UpperLimit));

            uint currentPos = Position;

            if (currentPos == toPos)
                return;

            _targetPos = toPos;
            _movingToTarget = true;
            if (_targetPos > currentPos)
            {
                _status = FocuserStatus.MovingUp;
                pinUp.SetOn();
                if (Simulated)
                    encoder.startMoving(Const.Direction.Increasing);
            }
            else
            {
                _realTarget = _targetPos;
                _targetPos -= _upwardCompensation;
                _needUpwardCompensation = true;
                _status = FocuserStatus.MovingDown;
                pinDown.SetOn();
                if (Simulated)
                    encoder.startMoving(Const.Direction.Decreasing);
            }                

            movementMonitoringTimer.Change(movementMonitoringTimeout, movementMonitoringTimeout);

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
                //tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
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
                #region trace
                ////tl.LogMessage("Description Get", driverDescription);
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
                //tl.LogMessage("DriverInfo Get", driverInfo);
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
                //tl.LogMessage("DriverVersion Get", driverVersion);
                #endregion
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                short ret = 2;

                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "InterfaceVersion: tl: #{0}", tl.GetHashCode());
                #region trace
                //WiseFocuser.//tl.LogMessage("InterfaceVersion Get", ret.ToString());
                #endregion
                return ret;
            }
        }

        private void MonitorMovement(object StateObject)
        {
            uint currPos = Position;
            recentPositions.Enqueue(currPos);

            if (pinUp.isOn)
            {
                if (currPos >= UpperLimit)
                    Stop();

                if (_movingToTarget)
                {
                    if (currPos >= _targetPos) // overshoot
                        Stop();
                    if (_targetPos - currPos <= motionParameters[Direction.Up].stoppingDistance)
                        Stop();
                }
            }

            if (pinDown.isOn)
            {
                if (currPos <= LowerLimit)
                    Stop();

                if (_movingToTarget)
                {
                    if (currPos <= _targetPos) // overshoot
                        Stop();
                    if (currPos - _targetPos <= motionParameters[Direction.Down].stoppingDistance)
                        Stop();
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
        }

        public uint LowerLimit
        {
            get
            {
                return encoder.LowerLimit;
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