using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseSafetySwitch
    {
        private static readonly WiseSafetySwitch instance = new WiseSafetySwitch(); // Singleton
        private static bool _initialized = false;
        private WisePin safetyPin;
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseSafetySwitch()
        {
        }

        public WiseSafetySwitch()
        {
        }

        public static WiseSafetySwitch Instance
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
            
            safetyPin = new WisePin("SafetySwitch", hardware.teleboard, DigitalPortType.SecondPortCH, 0, DigitalPortDirection.DigitalIn);
            _initialized = true;
        }

        public bool IsSafe
        {
            get
            {
                return safetyPin.isOn;
            }
        }
    }
}
