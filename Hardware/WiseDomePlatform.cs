using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseDomePlatform : WiseObject
    {
        private static readonly WiseDomePlatform instance = new WiseDomePlatform(); // Singleton
        private static bool _initialized = false;
        private WisePin domePlatformIsDownPin;
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseDomePlatform()
        {
        }

        public WiseDomePlatform()
        {
        }

        public static WiseDomePlatform Instance
        {
            get
            {
                return instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            domePlatformIsDownPin = new WisePin(Const.notsign + "PlatDown", hardware.domeboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalIn);
            _initialized = true;
        }

        public bool IsSafe
        {
            get
            {
                return Simulated ? true : domePlatformIsDownPin.isOff;
            }
        }
    }
}
