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
        private static volatile WiseDomePlatform _instance = new WiseDomePlatform(); // Singleton
        private static object syncObject = new object();
        private static bool _initialized = false;
        private WisePin domePlatformIsDownPin;

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
                if (_instance == null)
                {
                    if (syncObject == null)
                        syncObject = new object();

                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseDomePlatform();
                        _instance.init();
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            domePlatformIsDownPin = new WisePin(Const.notsign + "PlatDown",
                Hardware.Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalIn);
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
