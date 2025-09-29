using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseHardLimit : WiseObject
    {
        private static bool _initialized = false;
        private WisePin hardLimitIsDownPin;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseHardLimit() { }
        public WiseHardLimit() { }

        private static readonly Lazy<WiseHardLimit> lazy = new Lazy<WiseHardLimit>(() => new WiseHardLimit()); // Singleton

        public static WiseHardLimit Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            hardLimitIsDownPin = new WisePin(Const.notsign + "HardLimit",
                Hardware.Hardware.Instance.teleboard, DigitalPortType.SecondPortCH, 1, DigitalPortDirection.DigitalIn);
            _initialized = true;
        }

        public bool IsSafe
        {
            get
            {
                return Simulated ? true : hardLimitIsDownPin.isOff;
            }
        }
    }
}
