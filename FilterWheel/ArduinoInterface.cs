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
        private static ASCOM.Wise40.Common.Debugger debugger = Debugger.Instance;

        private bool _initialized = false;
        private string _serialPort;
        private static object _serialLock = new Object();

        private System.IO.Ports.SerialPort serial;
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

                lazy.Value.init(WiseFilterWheel.port);
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
        public enum ArduinoStatus { Idle, BadPort, Connecting, Communicating, Moving };
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
                if (_serialPort == null || !System.IO.Ports.SerialPort.GetPortNames().Contains(_serialPort) || serial == null)
                    return false;
                return serial.IsOpen;
            }

            set
            {
                if (_serialPort == null)
                    _serialPort = WiseFilterWheel.port;

                if (_serialPort == null || !System.IO.Ports.SerialPort.GetPortNames().Contains(_serialPort))
                    return;

                if (serial != null && serial.IsOpen)
                    serial.Close();

                serial = new System.IO.Ports.SerialPort(_serialPort, 57600);
                communicationCompleteHandler += onCommunicationComplete;

                if (value == serial.IsOpen)
                    return;

                if (value)
                {
                    if (!serial.IsOpen)
                    {
                        try
                        {
                            _status = ArduinoStatus.Connecting;
                            serial.Open();
                            serial.ReadExisting();  // flush
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
                    serial.Close();
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
            if (!serial.IsOpen)
                return string.Empty;
            string skipped = serial.ReadTo(Stx);
            string msg = serial.ReadTo(Etx).TrimEnd(etx);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "getPacket: skipped: [{0}], got: [{1}]", skipped, msg);
            #endregion
            return msg;
        }

        public ArduinoInterface()
        {
        }

        public void init(string port)
        {
            if (_initialized)
                return;

            if (! System.IO.Ports.SerialPort.GetPortNames().Contains(port))
            {
                _initialized = true;
                return;
            }

            _serialPort = port;
            serial = new System.IO.Ports.SerialPort(_serialPort, 57600);
            communicationCompleteHandler += onCommunicationComplete;
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
            string[] serialPorts = System.IO.Ports.SerialPort.GetPortNames();
            if (_serialPort == null) {
                _error = string.Format("_serialPort is null");
                _status = ArduinoStatus.BadPort;
                return;
            } else if (!serialPorts.Contains(_serialPort))
            {
                _error = string.Format("No such serial port \"{0}\"in {1}.", _serialPort, serialPorts.ToString());
                _status = ArduinoStatus.BadPort;
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
                        serial.Write(packet);
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

        public string Status
        {
            get
            {
                return string.Format("{0}{1}", _status.ToString(), (_error == null) ? "" : " (" + _error + ")");
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
