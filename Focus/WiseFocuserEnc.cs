using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using MccDaq;
using System.Threading;


namespace ASCOM.Wise40
{
    public class WiseFocuserEnc : WiseEncoder, IDisposable
    {
        private WisePin pinZero, pinLatch;
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;

        private readonly uint maxPos = (1 << 12);
        private readonly uint maxTurns = (1 << 13);

        private uint _daqsValue;
        private bool _connected = false;
        private bool _multiTurn = false;

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();
        
        public WiseFocuserEnc(bool multiTurn = false)
        {
            Name = "FocusEnc";
            this._multiTurn = multiTurn;
            if (this._multiTurn) {
                pinLatch = new WisePin("FocusLatch", hardware.miscboard, DigitalPortType.FirstPortCH, 3, DigitalPortDirection.DigitalOut);
                pinZero = new WisePin("FocusZero", hardware.miscboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);
                base.init("FocusEnc",
                    1 << 21,
                    new List<WiseEncSpec>() {
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortCL, mask = 0xf },
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortA,  mask = 0xff },
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortB,  mask = 0xff },
                        }
                );
                connectables.AddRange(new List<IConnectable>() { pinLatch, pinZero });
                disposables.AddRange(new List<IDisposable>() { pinLatch, pinZero });
            } else {
                base.init("FocusEnc",
                    1 << 7,
                    new List<WiseEncSpec>() {
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortB,  mask = 0x7f },
                        },
                    true
                    );
            }
        }

        public new uint Value
        {
            get
            {
                uint turns, pos;

                if (!Simulated)
                {
                    if (_multiTurn)
                    {
                        pinLatch.SetOn();
                        // delay??
                    }
                    _daqsValue = base.Value;
                    if (_multiTurn)
                        pinLatch.SetOff();
                }

                if (_multiTurn)
                {
                    pos = _daqsValue & 0xfff;
                    turns = (_daqsValue >> 12) & 0x1fff;
                }
                else
                {
                    pos = _daqsValue;
                    turns = 0;
                }

                return (turns * maxPos) + pos;
            }

            set
            {
                if (Simulated)
                {
                    if (_multiTurn) {
                        uint pos = value % maxPos;
                        uint turns = value / maxPos;
                        _daqsValue = (turns << 12) | pos;
                    } else
                        _daqsValue = value % maxPos;
                }
            }
        }
        
        public void SetZero()
        {
            if (!_multiTurn)
                return;
            pinZero.SetOn();
            Thread.Sleep(150);
            pinZero.SetOff();    
        }

        public new void Dispose()
        {
            foreach (var disposable in disposables)
                disposable.Dispose();

            base.Dispose();
        }

        public new bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (value == _connected)
                    return;

                foreach (var connectable in connectables)
                    connectable.Connect(value);
                base.Connect(value);

                _connected = value;
            }
        }
    }
}
