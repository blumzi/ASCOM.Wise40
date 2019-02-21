using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;


using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40SafeToOperate
{
    public class DoorLockSensor : Sensor
    {
        private static WisePin DoorLockPin;
        private static WisePin BypassPin;
        public static int _doorLockDelaySeconds, _defaultDoorLockDelaySeconds = 30;
        private Hardware hardware = Hardware.Instance;
        private static Timer _timer = new Timer(new System.Threading.TimerCallback(Check));
        private static bool _doorLockWasSafe = false, _bypassWasSafe = false;
        private static bool _isSafe = true;
        private static bool _debugging = false;
        private static int _doorLockCounter, _bypassCounter;
        private bool _wasSafe = false;
        private string _status;

        public DoorLockSensor(WiseSafeToOperate instance) :
            base("DoorLock",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.CanBeBypassed |
                SensorAttribute.ForcesDecision, instance)
        {
            DoorLockPin = new WisePin("DoorLock", hardware.domeboard, DigitalPortType.FirstPortCH, 3, DigitalPortDirection.DigitalIn);
            BypassPin = new WisePin("DoorBypass", hardware.domeboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalIn);

            _timer.Change(0, (int)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        public override object Digest()
        {
            return new DoorLockDigest
            {
                Name = WiseName,
                IsSafe = isSafe,
                DoorLockPin = DoorLockPin.isOn ? 1 : 0,
                DoorLockIsSafe = DoorLockIsSafe,
                DoorLockCounter = _doorLockCounter,
                BypassPin = BypassPin.isOn ? 1 : 0,
                BypassIsSafe = BypassIsSafe,
                BypassCounter = _bypassCounter,
                UnsafeReason = reason(),
            };
        }

        public override string reason()
        {
            return "Door unlocked and not bypassed";
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
                safe = _isSafe,
                usable = true,
            };

            _status = string.Format("Lock is {0}, bypass is {1}",
                                            DoorLockIsSafe ? "closed" : "open",
                                            BypassIsSafe ? "OFF" : "ON");
            if (r.safe != _wasSafe)
            {
                activityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.safe)));
            }
            _wasSafe = r.safe;
            return r;
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }

        public override void readSensorProfile() {
            _doorLockDelaySeconds = Convert.ToInt32(wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Const.ProfileName.SafeToOperate_DoorLockDelay, string.Empty, _defaultDoorLockDelaySeconds.ToString()));
        }

        public override void writeSensorProfile() {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Const.ProfileName.SafeToOperate_DoorLockDelay, _doorLockDelaySeconds.ToString());
        }

        private static bool DoorLockPinIsSafe
        {
            get
            {
                return DoorLockPin.isOff;
            }
        }

        private static bool BypassPinIsSafe
        {
            get
            {
                return BypassPin.isOff;
            }
        }

        private static bool DoorLockIsSafe
        {
            get
            {
                return DoorLockPinIsSafe || (_doorLockCounter > 0);
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
            bool doorLockIsSafe = DoorLockPinIsSafe;
            bool bypassIsSafe = BypassPinIsSafe;

            if (_doorLockWasSafe && !doorLockIsSafe)
                _doorLockCounter = _doorLockDelaySeconds;
            else if (!_doorLockWasSafe && doorLockIsSafe)
                _doorLockCounter = 0;
            else
                if (_doorLockCounter > 0)
                _doorLockCounter--;

            if (_bypassWasSafe && !bypassIsSafe)
                _bypassCounter = _doorLockDelaySeconds;
            else if (!_bypassWasSafe && bypassIsSafe)
                _bypassCounter = 0;
            else
                if (_bypassCounter > 0)
                _bypassCounter--;

            _doorLockWasSafe = doorLockIsSafe;
            _bypassWasSafe = bypassIsSafe;

            _isSafe = DoorLockIsSafe || BypassIsSafe;
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
    }

    public class DoorLockDigest
    {
        public string Name;
        public bool IsSafe;
        public int DoorLockPin;
        public bool DoorLockIsSafe;
        public int DoorLockCounter;
        public int BypassPin;
        public bool BypassIsSafe;
        public int BypassCounter;
        public string UnsafeReason;
    }
}
