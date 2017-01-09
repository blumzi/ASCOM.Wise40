using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class ArduinoInterface
    {
        private bool _connected = false;
        private string port;

        private System.IO.Ports.SerialPort serial;
        private const char stx = (char)2;
        private const char etx = (char)3;
        private string Stx = stx.ToString();
        private string Etx = etx.ToString();

        public enum Direction { CW, CCW };

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (value == _connected)
                    return;

                if (value)
                {
                    serial = new System.IO.Ports.SerialPort(port, 57600);
                    try
                    {
                        serial.Open();
                    }
                    catch (Exception ex)
                    {
                        //throw new InvalidOperationException(ex.Message);
                    }
                } else
                {
                    serial.Dispose();
                    serial = null;
                }
            }
        }

        private string mkPacket(string payload)
        {
            return stx + payload + etx;
            // TODO: checksum
        }

        private string getPacket()
        {
            if (!serial.IsOpen)
                return string.Empty;

            while (serial.ReadChar() != stx)
                ;
            return serial.ReadTo(Etx);
            // TODO: checksum
        }

        public ArduinoInterface(string port)
        {
            this.port = port;
        }

        public string getPosition(string reply = null)
        {
            if (!serial.IsOpen)
                return string.Empty;

            if (reply == null)
            {
                serial.Write(mkPacket("get-position"));
                reply = getPacket();
            }

            if (!reply.StartsWith("position:") || reply == "position:no-tag")
                return string.Empty;
            return reply.Substring("position:".Length);
        }

        public string move(Direction dir, int nPos = 1)
        {
            serial.Write(mkPacket("move" + ((dir == Direction.CW) ? "CW" : "CCW") + ":" + nPos.ToString()));
            return getPosition(getPacket());
        }
    }
}
