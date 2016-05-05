using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WisePin : WiseObject, IConnectable, IDisposable
    {
        private int bit;
        private WiseDaq daq;
        private DigitalPortDirection dir;
        private bool inverse;
        private bool _connected = false;

        public WisePin(string name, WiseBoard brd, DigitalPortType port, int bit, DigitalPortDirection dir, bool inverse = false)
        {
            this.name = name +
                "@Board" +
                (brd.type == WiseBoard.BoardType.Hard ? brd.mccBoard.BoardNum : brd.boardNum) +
                port.ToString() +
                "[" + bit.ToString() + "]";


            if ((daq = brd.daqs.Find(x => x.porttype == port)) == null)
                throw new WiseException(this.name + ": Invalid Daq spec, no " + port + " on this board");
            this.dir = dir;
            this.bit = bit;
            this.inverse = inverse;
            daq.setDir(dir);
        }

        public bool simulated
        {
            get
            {
                return daq.wiseBoard.type == WiseBoard.BoardType.Soft;
            }
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

        public bool isOn
        {
            get
            {
                bool ret = (daq.Value & (ushort)(1 << bit)) != 0;

                return inverse ? !ret : ret;
            }
        }

        public bool isOff
        {
            get
            {
                return isOn;
            }
        }

        public void Connect(bool connected)
        {
            if (connected)
                daq.setOwner(name, bit);
            else
                daq.unsetOwner(bit);
            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose()
        {
            SetOff();
            daq.unsetOwner(bit);
        }
    }
}