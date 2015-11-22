using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.WiseHardware
{
    class HardwareTest
    {
        static void Main(string[] args)
        {
            WisePin bit0, bit7;
            WiseEncoder enc;
            WiseEncSpec[] encSpecs;

            Hardware hw = new Hardware();
            bit0 = new WisePin("bit0", Hardware.Instance.domeboard, MccDaq.DigitalPortType.FirstPortA, 0, MccDaq.DigitalPortDirection.DigitalOut);
            bit7 = new WisePin("bit7", Hardware.Instance.domeboard, MccDaq.DigitalPortType.FirstPortA, 7, MccDaq.DigitalPortDirection.DigitalOut);

            encSpecs = new WiseEncSpec[] {
                new WiseEncSpec() { brd = Hardware.Instance.domeboard, port = MccDaq.DigitalPortType.FirstPortA },
                new WiseEncSpec() { brd = Hardware.Instance.domeboard, port = MccDaq.DigitalPortType.FirstPortA, mask = 0x3  },
            };

            enc = new WiseEncoder("testEncoder", 1024, encSpecs, true, 100);
            bit0.SetOn();
            Console.WriteLine(Hardware.Instance.domeboard.ownersToString());
            bit0.SetOff();
            System.Threading.Thread.Sleep(60000);
        }
    }
}
