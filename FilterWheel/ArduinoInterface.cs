using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public class ArduinoInterface
    {
        private static Debugger debugger = Debugger.Instance;

        private bool _initialized = false;
        private string _serialPortName;
        private int _serialPortSpeed = 57600;   // Fixed
        private static object _serialLock = new Object();

        private System.IO.Ports.SerialPort _serialPort;
        private const char stx = (char)2;
        private const char etx = (char)3;
        private const char cr = (char)13;
        private const char nl = (char)10;
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

        static void onCommunicationComplete(object sender, CommunicationCompleteEventArgs e)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ArduinoInterface.onCommunicationComplete: reply = {0}", hexdump(e.Reply));
            #endregion
            if (e.Reply.StartsWith("tag:"))
            {
                _tag = e.Reply.Substring("tag:".Length);
            }
            else if (e.Reply.StartsWith("error:"))
                _error = e.Reply.Substring("error:".Length);
        }

        public class CommunicationCompleteEventArgs : EventArgs
        {
            public string Reply { get; set; }
        }

        public event EventHandler<CommunicationCompleteEventArgs> communicationCompleteHandler;

        public virtual void RaiseCommunicationComplete(CommunicationCompleteEventArgs e)
        {
            EventHandler<CommunicationCompleteEventArgs> handler = communicationCompleteHandler;
            if (handler != null)
                handler(this, e);
        }

        static Task communicatorTask;
        static Timer communicationTimer = new Timer(communicationTimedOut);
        private static string _command;
        static int _timeoutMillis;
        private static string _tag;

        public enum StepperDirection { CW, CCW };
        public enum ArduinoStatus { Idle, BadPort, PortNotOpen, Connecting, Communicating, Moving };
        private static ArduinoStatus _status = ArduinoStatus.Idle;
        private string _lastCommandSent;
        private bool _waitingForReply;

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
                        _serialPort = new System.IO.Ports.SerialPort(_serialPortName, _serialPortSpeed);
                        _serialPort.Handshake = System.IO.Ports.Handshake.None;
                        _serialPort.DtrEnable = true;
                        _serialPort.RtsEnable = true;
                        communicationCompleteHandler += onCommunicationComplete;

                        _status = ArduinoStatus.Connecting;
                        _serialPort.Open();
                        string flushed = _serialPort.ReadExisting();  // flush
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ArduinoInterface:Connected: flushed: \"{0}\"", hexdump(flushed));
                        #endregion
                        _status = ArduinoStatus.Idle;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(ex.Message);
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
            //return stx + payload + cr + nl + etx;
            return payload + cr;
            // TODO: checksum
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
                //string skipped = _serialPort.ReadTo(Stx);
                //msg = _serialPort.ReadTo(Etx).TrimEnd(etx);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "getPacket: got: [{0}]", msg);
                #endregion
            } catch (Exception ex) {
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
                if (System.IO.Ports.SerialPort.GetPortNames().Contains(port))
                {
                    _serialPortName = port;
                    //communicationCompleteHandler += onCommunicationComplete;
                }
            }

            _initialized = true;
        }

        private static void communicationTimedOut(Object state)
        {
            //CTS.Cancel();
            //Thread.Sleep(100);

            //communicatorTask.Dispose();
            //communicationTimer.Dispose();
            _status = ArduinoStatus.Idle;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "arduino: Communicate(\"{0}\") ==> Timedout (after {1} millis)",
                _command, _timeoutMillis);
            #endregion
            throw new ArduinoCommunicationException(string.Format("Timedout after {0} millis.", _timeoutMillis));
        }


        private void SendCommand(string command,
            bool waitForReply = true,
            ArduinoStatus interimStatus = ArduinoStatus.Communicating,
            int timeoutMillis = 0)
        {
            _waitingForReply = false;
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

            char[] crnls = { '\r', '\n' };

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
            _error = null;
            CTS = new CancellationTokenSource();
            CT = CTS.Token;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino: status: {0}", _status));
            #endregion
            try
            {
                communicatorTask = Task.Run(() =>
                {
                    _lastCommandSent = command;
                    string packet = mkPacket(command);
                    string reply = null;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "arduino: Communicate: Sending \"{0}\" ...", hexdump(packet));
                    #endregion
                    lock (_serialLock)
                    {
                        _serialPort.Write(packet);
                        if (waitForReply)
                        {
                            _waitingForReply = true;
                            //Thread.Sleep(1000);
                            //reply = getPacket().TrimEnd(crnls);
                            _serialPort.ReadTimeout = 5000;
                            reply = _serialPort.ReadExisting();
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "arduino: Communicate(\"{0}\") ==> \"{1}\"", hexdump(packet), reply);
                            #endregion
                        }
                    }
                    CommunicationCompleteEventArgs e = new CommunicationCompleteEventArgs()
                    {
                        Reply = reply,
                    };
                    _status = ArduinoStatus.Idle;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino: status: {0}", _status));
                    #endregion
                    RaiseCommunicationComplete(e);
                }, CT);
            }
            catch (Exception ex)
            {
                communicatorTask.Dispose();
                _status = ArduinoStatus.Idle;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino: status: {0}, communication exception: {1}", _status, ex.Message));
                #endregion
            }
        }

        public void StartReadingTag()
        {
            SendCommand("get-tag", true, ArduinoStatus.Communicating/*, 5000*/);
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

        public bool WaitingForReply
        {
            get
            {
                return _waitingForReply;
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
                return _error;
            }
        }

        public string Tag
        {
            get
            {
                SendCommand("get-tag", waitForReply: true, interimStatus: ArduinoStatus.Communicating/*, timeoutMillis: 5000*/);
                return _tag;
            }
        }
    }
}
