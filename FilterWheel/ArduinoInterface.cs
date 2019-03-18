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

        static void onCommunicationComplete(object sender, CommunicationCompleteEventArgs e)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ArduinoInterface.onCommunicationComplete: reply = {0}", e.reply);
            #endregion
            if (e.reply.StartsWith("tag:"))
            {
                _tag = e.reply.Substring("tag:".Length);
            }
            else if (e.reply.StartsWith("error:"))
                _error = e.reply.Substring("error:".Length);
        }

        public class CommunicationCompleteEventArgs : EventArgs
        {
            public string reply { get; set; }
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
                if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.Close();

                _serialPort = new System.IO.Ports.SerialPort(_serialPortName, _serialPortSpeed);
                communicationCompleteHandler += onCommunicationComplete;

                if (value == _serialPort.IsOpen)
                    return;

                if (value)
                {
                    if (!_serialPort.IsOpen)
                    {
                        try
                        {
                            _status = ArduinoStatus.Connecting;
                            _serialPort.Open();
                            _serialPort.ReadExisting();  // flush
                            _status = ArduinoStatus.Idle;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(ex.Message);
                        }
                    }
                }
                else
                {
                    _serialPort.Close();
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
                    driverProfile.WriteValue(Const.wiseFilterWheelDriverID, "Port", _serialPortName);
                }
            }
        }

        private string mkPacket(string payload)
        {
            return stx + payload + cr + nl + etx;
            // TODO: checksum
        }

        private string getPacket()
        {
            if (!_serialPort.IsOpen)
                return string.Empty;
            string skipped = _serialPort.ReadTo(Stx);
            string msg = _serialPort.ReadTo(Etx).TrimEnd(etx);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "getPacket: skipped: [{0}], got: [{1}]", skipped, msg);
            #endregion
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
                string port = driverProfile.GetValue(Const.wiseFilterWheelDriverID, "Port", string.Empty,"COM7");
                if (System.IO.Ports.SerialPort.GetPortNames().Contains(port))
                {
                    _serialPortName = port;
                    _serialPort = new System.IO.Ports.SerialPort(_serialPortName, _serialPortSpeed);
                    communicationCompleteHandler += onCommunicationComplete;
                }
            }

            _initialized = true;
        }

        private static void communicationTimedOut(Object state)
        {
            communicatorTask.Dispose();
            communicationTimer.Dispose();
            _status = ArduinoStatus.Idle;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Arduino: Communicate(\"{0}\") ==> Timedout (after {1} millis)",
                _command, _timeoutMillis);
            #endregion
            throw new ArduinoCommunicationException(string.Format("Timedout after {0} millis.", _timeoutMillis));
        }


        private void SendCommand(string command,
            bool waitForReply = true,
            ArduinoStatus interimStatus = ArduinoStatus.Communicating,
            int timeoutMillis = 0)
        {
            if (_serialPort == null) {
                _error = "_serialPort is null";
                _status = ArduinoStatus.BadPort;
                return;
            } else if (! _serialPort.IsOpen)
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino status: {0}", _status));
            #endregion
            try
            {
                communicatorTask = Task.Run(() =>
                {
                    string packet = mkPacket(command);
                    string reply = null;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Arduino: Communicate: Sending \"{0}\" ...", _command);
                    #endregion
                    lock (_serialLock)
                    {
                        _serialPort.Write(packet);
                        if (waitForReply)
                        {
                            Thread.Sleep(1000);
                            reply = getPacket().TrimEnd(crnls);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Arduino: Communicate(\"{0}\") ==> \"{1}\"", _command, reply);
                            #endregion
                        }
                    }
                    CommunicationCompleteEventArgs e = new CommunicationCompleteEventArgs();
                    e.reply = reply;
                    _status = ArduinoStatus.Idle;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino status: {0}", _status));
                    #endregion
                    RaiseCommunicationComplete(e);
                });
            }
            catch (Exception ex)
            {
                communicatorTask.Dispose();
                _status = ArduinoStatus.Idle;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("arduino status: {0}, communication exception: {1}", _status, ex.Message));
                #endregion
            }
        }

        public void StartReadingTag()
        {
            SendCommand("get-tag", true);
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
                return _tag;
            }
        }
    }
}
