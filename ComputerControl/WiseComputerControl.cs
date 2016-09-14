using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseComputerControl
    {
        private static readonly WiseComputerControl instance = new WiseComputerControl(); // Singleton
        private static bool _initialized = false;
        private WisePin computerControlPin;
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private static bool _simulated;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseComputerControl()
        {
        }

        public WiseComputerControl()
        {
        }

        public static WiseComputerControl Instance
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
            
            computerControlPin = new WisePin("CompControl", hardware.teleboard, DigitalPortType.SecondPortCH, 0, DigitalPortDirection.DigitalIn);
            _simulated = computerControlPin.Simulated;
            _initialized = true;
        }

        public bool IsSafe
        {
            get
            {
                return _simulated ? true : computerControlPin.isOn;
            }
        }
    }
}
