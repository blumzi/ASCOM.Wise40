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
        private ASCOM.Wise40.Common.Debugger debugger = Debugger.Instance;
        private static readonly ArduinoInterface _instance = new ArduinoInterface();

        private bool _initialized = false;
        private string port;
        private Object serialLock = new Object();

        private System.IO.Ports.SerialPort serial;
        private const char stx = (char)2;
        private const char etx = (char)3;
        private const char cr = (char)13;
        private const char nl = (char)10;
        private string Stx = stx.ToString();
        private string Etx = etx.ToString();

        public enum StepperDirection { CW, CCW };
        public enum ArduinoStatus {  Idle, Connecting, Communicating, Moving };
        private ArduinoStatus _status = ArduinoStatus.Idle;

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
                } else
                {
                    serial.Close();
                }
            }
        }

        private string mkPacket(string payload)
        {
            return payload + cr;
            //return stx + payload + cr + nl + etx;
            // TODO: checksum
        }

        private string getPacket()
        {
            if (!serial.IsOpen)
                return string.Empty;

            string msg = serial.ReadLine();
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "getPacket: got: [{0}]", msg);
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
            this.port = port;
            _initialized = true;
        }

        private string Communicate(string command, bool waitForReply = true, ArduinoStatus interimStatus = ArduinoStatus.Communicating)
        {
            string reply = string.Empty;
            char[] crnls = { '\r', '\n' };
            ArduinoStatus prevStatus = _status;
            _status = interimStatus;
            string packet = mkPacket(command);
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Arduino: Communicate: Sending \"{0}\" ...", command);
            lock (serialLock)
            {
                serial.Write(packet);
                if (waitForReply)
                {
                    Thread.Sleep(1000);
                    reply = getPacket().TrimEnd(crnls);
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Arduino: Communicate(\"{0}\") ==> \"{1}\"", command, reply);
                }
            }
            _status = prevStatus;
            return reply;
        }

        public string getPosition()
        {
            string reply = Communicate("get-tag", true);

            if (reply == string.Empty || reply == "tag:no-tag" || reply.StartsWith("error:"))
                return "Unknown";
            return reply.Substring("tag:".Length);
        }

        public string move(StepperDirection dir, int nPos = 1)
        {
            string command = string.Format("move-{0}:{1}", (dir == StepperDirection.CW) ? "cw" : "ccw", nPos.ToString());
            string reply = Communicate(command, true, ArduinoStatus.Moving);


            if (reply == string.Empty || reply == "tag:no-tag" || reply.StartsWith("error:"))
                return "Unknown";
            return reply.Substring("tag:".Length);
        }

        public string Status
        {
            get
            {
                switch (_status)
                {
                    case ArduinoStatus.Communicating:
                        return "Communicating";
                    case ArduinoStatus.Moving:
                        return "Moving";
                    case ArduinoStatus.Idle:
                    default:
                        return "Idle";
                }
            }
        }
    }
}
