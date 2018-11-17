﻿using System;
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
        private WisePin computerControlPin;
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
            
            computerControlPin = new WisePin("CompControl", hardware.teleboard, DigitalPortType.SecondPortCH, 0, DigitalPortDirection.DigitalIn);
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
                if (Simulated)
                    return false;

                return !computerControlPin.isOn;
            }
        }

        public bool PlatformIsDown
        {
            get
            {
                if (Simulated)
                    return true;

                return wisedomeplatform.IsSafe;
            }
        }

        public List<string> UnsafeReasons()
        {
            List<string> reasons = new List<string>();

            if (!computerControlPin.isOn)
                reasons.Add("ComputerControl switch is on MAINTENANCE");
            if (!wisedomeplatform.IsSafe)
                reasons.Add("Platform is RAISED");

            return reasons; ;
        }
    }
}
