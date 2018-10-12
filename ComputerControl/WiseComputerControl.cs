using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40
{
    public class WiseComputerControl : WiseObject
    {
        private static readonly WiseComputerControl instance = new WiseComputerControl(); // Singleton
        private static bool _initialized = false;
        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        private WiseDomePlatform wisedomeplatform = WiseDomePlatform.Instance;

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
            
            _initialized = true;
            wisedomeplatform.init();
        }

        public bool IsSafe
        {
            get
            {
                if (Simulated)
                    return true;
                else
                    return !Maintenance && PlatformIsDown;
            }
        }

        public bool Maintenance
        {
            get
            {
                return Hardware.Hardware.computerControlPin.isOff;
            }
        }

        public bool PlatformIsDown
        {
            get
            {
                return wisedomeplatform.IsSafe;
            }
        }

        public List<string> UnsafeReasons()
        {
            List<string> reasons = new List<string>();

            if (Hardware.Hardware.computerControlPin.isOff)
                reasons.Add(Const.computerControlAtMaintenance);

            if (!wisedomeplatform.IsSafe)
                reasons.Add("Platform is RAISED");

            return reasons; ;
        }
    }
}
