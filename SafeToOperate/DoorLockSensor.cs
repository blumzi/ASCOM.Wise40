using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40SafeToOperate
{
    public class DoorLockSensor : Sensor
    {
        private static WisePin DoorPin;
        private static WisePin BypassPin;
        public static int _doorLockDelaySeconds, _defaultDoorLockDelaySeconds = 30;
        private Hardware hardware = Hardware.Instance;
        private static Timer _timer = new Timer(new System.Threading.TimerCallback(Check));
        private static bool _doorWasSafe = false, _bypassWasSafe = false;
        private static bool _isSafe = true;
        private static bool _debugging = false;
        private static int _fakeDoorPin, _fakeBypassPin;
        private static int _doorCounter, _bypassCounter;

        public DoorLockSensor(WiseSafeToOperate instance) :
            base("DoorLock",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance)
        {
            DoorPin = new WisePin("DoorLock", hardware.domeboard, DigitalPortType.FirstPortCH, 3, DigitalPortDirection.DigitalIn);
            BypassPin = new WisePin("DoorBypass", hardware.domeboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalIn);

            _timer.Change(0, (int)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        public override string reason()
        {
            List<string> reasons = new List<string>();

            reasons.Add(string.Format("Door:Debugging-{0}", _debugging));
            reasons.Add(string.Format("Door:_isSafe-{0}", _isSafe));
            reasons.Add(string.Format("Lock:Pin-{0}", DoorPin.isOn ? 1 : 0));
            reasons.Add(string.Format("Lock:IsSafe-{0}", DoorIsSafe));
            reasons.Add(string.Format("Lock:Counter-{0}", _doorCounter));
            reasons.Add(string.Format("Bypass:Pin-{0}", BypassPin.isOn ? 1 : 0));
            reasons.Add(string.Format("Bypass:IsSafe-{0}", BypassIsSafe));
            reasons.Add(string.Format("Bypass:Counter-{0}", _bypassCounter));
            return string.Join(", ", reasons);

            //return "Door unlocked and not bypassed";
        }

        public override string MaxAsString
        {
            set {
                _doorLockDelaySeconds = Convert.ToInt32(value);
            }

            get {
                return _doorLockDelaySeconds.ToString();
            }
        }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                safe = true,    // _isSafe,
            };

            //#region debug
            //debugger.WriteLine(Wise40.Common.Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            //#endregion
            return r;
        }

        public override void readSensorProfile() {
            _doorLockDelaySeconds = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Const.ProfileName.SafeToOperate_DoorLockDelay, string.Empty, _defaultDoorLockDelaySeconds.ToString()));
        }

        public override void writeSensorProfile() {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Const.ProfileName.SafeToOperate_DoorLockDelay, _doorLockDelaySeconds.ToString());
        }

        private static bool DoorPinIsSafe
        {
            get
            {
                if (_debugging)
                    return (_fakeDoorPin == 0) ? true : false;
                else
                    return DoorPin.isOff;
            }
        }

        private static bool BypassPinIsSafe
        {
            get
            {
                if (_debugging)
                    return (_fakeBypassPin == 0) ? true : false;
                else
                    return BypassPin.isOff;
            }
        }

        private static bool DoorIsSafe
        {
            get
            {
                return DoorPinIsSafe || (_doorCounter > 0);
            }
        }

        private static bool BypassIsSafe
        {
            get
            {
                return BypassPinIsSafe || (_bypassCounter > 0);
            }
        }

        private static void Check(object o)
        {
            bool doorIsSafe = DoorPinIsSafe;
            bool bypassIsSafe = BypassPinIsSafe;

            if (_doorWasSafe && !doorIsSafe)
                _doorCounter = _doorLockDelaySeconds;
            else
                if (_doorCounter > 0)
                    _doorCounter--;

            if (_bypassWasSafe && !bypassIsSafe)
                _bypassCounter = _doorLockDelaySeconds;
            else
                if (_bypassCounter > 0)
                    _bypassCounter--;

            _doorWasSafe = doorIsSafe;
            _bypassWasSafe = bypassIsSafe;

            _isSafe = DoorIsSafe || BypassIsSafe;
        }

        public bool Debug
        {
            get
            {
                return _debugging;
            }

            set
            {
                _debugging = value;
            }
        }

        public int FakeDoorPin
        {
            get
            {
                return _fakeDoorPin;
            }

            set
            {
                _fakeDoorPin = value;
            }
        }

        public int FakeBypassPin
        {
            get
            {
                return _fakeBypassPin;
            }

            set
            {
                _fakeBypassPin = value;
            }
        }
    }
}
