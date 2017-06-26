using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.FilterWheel
{
    public class ArduinoInterface
    {
        private static ASCOM.Wise40.Common.Debugger debugger = Debugger.Instance;
        private static readonly ArduinoInterface _instance = new ArduinoInterface();

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
        public enum ArduinoStatus { Idle, Connecting, Communicating, Moving };
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
                if (serial == null)
                    return false;
                return serial.IsOpen;
            }

            set
            {
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "getPacket: skipped: [{0}], got: [{1}]", skipped, msg);
            return msg;
        }

        public static ArduinoInterface Instance
        {
            get
            {
                return _instance;
            }
        }

        public ArduinoInterface()
        {
        }

        public void init(string port)
        {
            if (_initialized)
                return;
            serial = new System.IO.Ports.SerialPort(port, 57600);
            debugger.StartDebugging(Debugger.DebugLevel.DebugLogic);
            this._serialPort = port;
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
            SendCommand(command, true, ArduinoStatus.Moving);
        }

        public bool isActive
        {
            get
            {
                return _status != ArduinoStatus.Idle;
            }
        }

        public string Status
        {
            get
            {
                return (_error != null) ? _error : _status.ToString();
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
