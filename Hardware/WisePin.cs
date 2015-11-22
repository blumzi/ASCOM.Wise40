using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MccDaq;

namespace ASCOM.WiseHardware
{
    public class WisePin : WiseObject, IConnectable, IDisposable
    {
        public int bit;
        private WiseDaq daq;
        private DigitalPortDirection dir;
        private bool inverse;

        public WisePin(string name, WiseBoard brd, DigitalPortType port, int bit, DigitalPortDirection dir, bool inverse = false)
        {
            this.name = name + "@Board" + brd.board.BoardNum + port.ToString() + "[" + bit.ToString() + "]";
            if ((daq = brd.daqs.Find(x => x.porttype == port)) == null)
                throw new WiseException(this.name + ": Invalid Daq spec, no " + port + " on this board");
            this.dir = dir;
            this.bit = bit;
            this.inverse = inverse;
            daq.setdir(dir);
        }

        public void SetOn()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;
            daq.Value |= (ushort)(1 << bit);
        }

        public void SetOff()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;
            daq.Value &= (ushort)~(1 << bit);
        }

        public bool IsOn()
        {
            bool ret = (daq.Value & (ushort)(1 << bit)) != 0;

            return inverse ? !ret : ret;
        }

        public bool IsOff()
        {
            return !IsOn();
        }

        public void Connect(bool connected)
        {
            if (connected)
                daq.setowner(name, bit);
            else
                daq.unsetowner(bit);
        }

        public void Dispose()
        {
            SetOff();
            daq.unsetowner(bit);
        }
    }
}