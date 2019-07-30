using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;
using System.IO.Ports;

namespace ASCOM.Wise40
{
    public class ArduinoInterface
    {
        private static Debugger debugger = Debugger.Instance;

        private bool _initialized = false;
        private string _serialPortName;
        private int _serialPortSpeed = 57600;   // Fixed
        private static object _serialLock = new Object();

        private SerialPort _serialPort;
        private const char stx = (char)2;
        private const char etx = (char)3;
        private const char cr = (char)13;
        private const char nl = (char)10;
        private static char[] crnls = { cr, nl };
        private string Stx = stx.ToString();
        private string Etx = etx.ToString();

        private static CancellationTokenSource CTS = new CancellationTokenSource();
        private static CancellationToken CT;

        private static string _error;

        private static readonly Lazy<ArduinoInterface> lazy = new Lazy<ArduinoInterface>(() => new ArduinoInterface()); // Singleton

        public static ArduinoInterface Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public static string hexdump(string s)
        {
            if (s == null || s == string.Empty)
                return s;

            string res = "hexdump: ";

            foreach (char b in s.ToCharArray())
                res += Char.IsControl(b) ? string.Format("x{0:X2} ", (byte) b) : string.Format("{0} ", b);

            return res.TrimEnd(' ');
        }


        static Dictionary<int, string> connectorName = new Dictionary<int, string>
        {
            [2] = "CD2",
            [3] = "CD3",
            [4] = "CD4",
        };

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string data = (sender as SerialPort).ReadLine().TrimEnd(crnls);
            WiseFilterWheel.Wheel wheel = null;
            Activity.FilterWheelActivity.Operation op = ArduinoInterface._lastCommandSent == "get-tag" ?
                        Activity.FilterWheelActivity.Operation.Detect :
                        Activity.FilterWheelActivity.Operation.Move;

            _status = ArduinoStatus.Idle;
            _tag = null;
            _error = null;

            WiseFilterWheel._lastDataReceived = DateTime.Now;
            if (data.StartsWith("tag:"))
            {
                _tag = data.Substring("tag:".Length);
                WiseFilterWheel.Instance.currentWheel = wheel = WiseFilterWheel.lookupWheel(_tag);

                if (op == Activity.FilterWheelActivity.Operation.Move)
                {
                    if (wheel == null)
                    {
                        WiseFilterWheel.EndActivity(
                            op: op,
                            endWheel: null,
                            endPos: Activity.FilterWheelActivity.UnknownPosition,
                            endTag: _tag,
                            endState: Activity.State.Failed,
                            endReason: string.Format("Unknown tag: {0}", _tag));
                    }
                    else
                    {
                        WiseFilterWheel.EndActivity(
                            op: op,
                            endWheel: wheel.WiseName,
                            endPos: WiseFilterWheel.Instance.currentWheel._position,
                            endTag: _tag,
                            endState: Activity.State.Succeeded,
                            endReason: string.Format("Detected tag: {0}", _tag));
                    }
                }
            }
            else if (data.StartsWith("error:"))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "ArduinoInterface:DataReceivedHandler: data = {0}", data.TrimEnd(crnls));
                #endregion
                _error = data.Substring("error:".Length);
                if (_error.StartsWith("connector:"))
                {
                    int[] nums = Array.ConvertAll(_error.Substring("connector:".Length).Split(' '), int.Parse);
                    if (nums.Length == 1)
                        _error = "Connector " + nums[0].ToString() + " is NOT CONNECTED!";
                    else
                        _error = "Connectors " + String.Join(" and ", nums) + " are NOT CONNECTED!";
                }

                if (op == Activity.FilterWheelActivity.Operation.Move)
                    WiseFilterWheel.EndActivity(
                        op: op,
                        endWheel: null,
                        endPos: Activity.FilterWheelActivity.UnknownPosition,
                        endTag: _tag,
                        endState: Activity.State.Failed,
                        endReason: _error);
            }
        }

        static Task communicatorTask;
        static Timer communicationTimer = new Timer(communicationTimedOut);
        private static string _command;
        static int _timeoutMillis;
        private static string _tag;

        public enum StepperDirection { CW, CCW };
        public enum ArduinoStatus { Idle, BadPort, PortNotOpen, Connecting, Communicating, Moving, WaitingForReply, TagReceived };
        private static ArduinoStatus _status = ArduinoStatus.Idle;
        public static string _lastCommandSent;

        public class ArduinoCommunicationException : Exception
        {
            public ArduinoCommunicationException(string message) : base(message) { }
            public ArduinoCommunicationException() { }
            public ArduinoCommunicationException(string message, Exception inner) : base(message, inner) { }
        }

        public bool Connected
        {
            get
            {
                if (_serialPort == null)
                    return false;
                return _serialPort.IsOpen;
            }

            set
            {
                if (_serialPortName == null)
                    return;

                if (value)
                {
                    try
                    {
                        _serialPort = new SerialPort(_serialPortName, _serialPortSpeed);
                        _serialPort.Handshake = Handshake.None;
                        _serialPort.DtrEnable = true;
                        _serialPort.RtsEnable = true;
                        _serialPort.ReadTimeout = 60 * 1000;
                        _serialPort.ReceivedBytesThreshold = 1;
                        _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                        _status = ArduinoStatus.Connecting;
                        _serialPort.Open();
                        while (!_serialPort.IsOpen)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "ArduinoInterface:Connected: waiting for port ({0}) to open", _serialPortName);
                            #endregion
                            Thread.Sleep(1000);
                        }
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "ArduinoInterface:Connected: port ({0}) opened", _serialPortName);
                        #endregion
                        _status = ArduinoStatus.Idle;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(ex.StackTrace);
                    }
                }
                else
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
            }
        }

        public int SerialPortSpeed
        {
            get
            {
                return _serialPortSpeed;
            }
        }

        public string SerialPortName
        {
            get
            {
                return _serialPortName;
            }

            set
            {
                _serialPortName = value;
                using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile() { DeviceType = "FilterWheel" })
                {
                    driverProfile.WriteValue(Const.WiseDriverID.FilterWheel, "Port", _serialPortName);
                }
            }
        }

        private string mkPacket(string payload)
        {
            return payload + cr;
        }

        private string getPacket()
        {
            if (!_serialPort.IsOpen)
                return string.Empty;
            string msg = null;

            try
            {
                while (_serialPort.BytesToRead == 0)
                    Thread.Sleep(100);

                msg = _serialPort.ReadExisting();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "getPacket: got: [{0}]", msg);
                #endregion
            } catch /* (Exception ex) */{
                ;
            }
            return msg;
        }

        public ArduinoInterface()
        {
        }

        public void init()
        {
            if (_initialized)
                return;

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile() { DeviceType = "FilterWheel" })
            {
                string port = driverProfile.GetValue(Const.WiseDriverID.FilterWheel, "Port", string.Empty, "COM7");
                if (SerialPort.GetPortNames().Contains(port))
                {
                    _serialPortName = port;
                }
            }

            _initialized = true;
        }

        private static void communicationTimedOut(Object state)
        {
            _status = ArduinoStatus.Idle;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "ArduinoInterface: communicationTimedOut(\"{0}\") ==> Timedout (after {1} millis)",
                _command, _timeoutMillis);
            #endregion
            throw new ArduinoCommunicationException(string.Format("Timedout after {0} millis.", _timeoutMillis));
        }


        private void SendCommand(string command,
            bool waitForReply = true,
            ArduinoStatus interimStatus = ArduinoStatus.Communicating,
            int timeoutMillis = 0)
        {
            _lastCommandSent = null;

            if (_serialPort == null) {
                _error = "_serialPort is null";
                _status = ArduinoStatus.BadPort;
                return;
            } else if (!_serialPort.IsOpen)
            {
                _error = string.Format("port \"{0}\" not open", _serialPortName);
                _status = ArduinoStatus.PortNotOpen;
                return;
            }

            if (timeoutMillis != 0)
            {
                _timeoutMillis = timeoutMillis;
                communicationTimer = new Timer(communicationTimedOut);
                communicationTimer.Change(_timeoutMillis, Timeout.Infinite);
            }
            else
                _timeoutMillis = 0;

            _command = command;
            _status = interimStatus;

            // The current _tag becomes irrelevant.  A new one will be sent by the Arduino
            if (_command == "get-tag" || _command.StartsWith("move-"))
            {
                _tag = null;
            }

            _error = null;
            CTS = new CancellationTokenSource();
            CT = CTS.Token;

            try
            {
                communicatorTask = Task.Run(() =>
                {
                    _lastCommandSent = command;
                    string packet = mkPacket(command);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "ArduinoInterface:SendCommand: Sending \"{0}\" ...",
                        packet.TrimEnd(crnls));
                    #endregion
                    lock (_serialLock)
                    {
                        _serialPort.Write(packet);
                        if (waitForReply)
                            _status = ArduinoStatus.WaitingForReply;
                    }
                }, CT);
            }
            catch (Exception ex)
            {
                communicatorTask.Dispose();
                _status = ArduinoStatus.Idle;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel,
                    string.Format("ArduinoInterface:SendCommand: status: {0}, communication exception: {1}", _status, ex.StackTrace));
                #endregion
            }
        }

        public void StartReadingTag()
        {
            SendCommand("get-tag", waitForReply: true, interimStatus: ArduinoStatus.Communicating);
        }

        public void StartMoving(StepperDirection dir, int nPos = 1)
        {
            string command = string.Format("move-{0}:{1}", (dir == StepperDirection.CW) ? "cw" : "ccw", nPos.ToString());
            SendCommand(command, waitForReply: true, interimStatus: ArduinoStatus.Moving);
        }

        public bool isActive
        {
            get
            {
                return _error == null && _status != ArduinoStatus.Idle;
            }
        }

        public bool PortIsOpen
        {
            get
            {
                return _serialPort.IsOpen;
            }
        }

        public string StatusAsString
        {
            get
            {
                if (Error != null)
                    return Error;
                return _status.ToString();
            }
        }

        public string LastCommand
        {
            get
            {
                return _lastCommandSent;
            }
        }

        public ArduinoStatus Status
        {
            get
            {
                return _status;
            }
        }

        public string Error
        {
            get
            {
                return (_error == null || _error == String.Empty) ? null : _error;
            }
        }

        public string Tag
        {
            get
            {
                return _tag;
            }
        }
    }
}
