using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;
using MccDaq;
using System.Threading;

//#define MULTI_TURN_ENCODER

namespace ASCOM.Wise40
{
    public class WiseFocuserEnc : WiseEncoder
    {
#if MULTI_TURN_ENCODER
        private WisePin pinZero, pinLatch;
#endif
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;

        private readonly uint maxPos = (1 << 12);
        //private readonly uint maxTurns = (1 << 13);

        private uint _daqsValue;
        private bool _connected = false;

        List<IConnectable> connectables = new List<IConnectable>();
        List<IDisposable> disposables = new List<IDisposable>();
        
        public WiseFocuserEnc()
        {
            Name = "FocusEnc";
#if MULTI_TURN_ENCODER
            pinLatch = new WisePin("FocusLatch", hardware.miscboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);
            pinZero = new WisePin("FocusZero",   hardware.miscboard, DigitalPortType.FirstPortCH, 3, DigitalPortDirection.DigitalOut);
            base.init("FocusEnc",
                1 << 21,
                new List<WiseEncSpec>() {
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortCL, mask = 0xf },
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortB,  mask = 0xff },
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortA,  mask = 0xff },
                    }
            );
            connectables.AddRange(new List<IConnectable> { pinLatch, pinZero };
            disposables.AddRange(new List<IConnectable> { pinLatch, pinZero };
#else
            base.init("FocusEnc",
                1 << 7,
                new List<WiseEncSpec>() {
                        new WiseEncSpec() { brd = hardware.miscboard, port = DigitalPortType.FirstPortB,  mask = 0x7f },
                    },
                true
                );
#endif
        }

        public new uint Value
        {
            get
            {
                uint turns, pos;

                if (!Simulated)
                {
#if MULTI_TURN_ENCODER
                    pinLatch.SetOn();
#endif
                    _daqsValue = base.Value;
#if MULTI_TURN_ENCODER
                    pinLatch.SetOff();
#endif
                }
#if MULTI_TURN_ENCODER
                pos = _daqsValue & 0xfff;
                turns = (_daqsValue >> 12) & 0x1fff;
#else
                pos = _daqsValue;
                turns = 0;
#endif

                return (turns * maxPos) + pos;
            }

            set
            {
                if (Simulated)
                {
#if MULTI_TURN_ENCODER
                    uint pos = value % maxPos;
                    uint turns = value / maxPos;
                    _daqsValue = (turns << 12) | pos;
#else
                    _daqsValue = value % maxPos;
#endif
                }
            }
        }

#if MULTI_TURN_ENCODER
        public void SetZero()
        {
            pinZero.SetOn();
            Thread.Sleep(150);
            pinZero.SetOff();    
        }
#endif
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
