using System;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40SafeToOperate
{
    public class HardLimitSensor : Sensor
    {
        private readonly WisePin hardLimitPin;
        private bool _wasSafe = false;
        private string _status;

        public HardLimitSensor(WiseSafeToOperate instance) :
            base("HardLimit",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.ForcesDecision |
                Attribute.Wise40Specific,
                "", "", "", "",
                instance)
        {
            hardLimitPin = new WisePin(Const.notsign + "HardLimit",
                Hardware.Instance.teleboard, DigitalPortType.SecondPortCH, 1, DigitalPortDirection.DigitalIn);
        }

        public override object Digest()
        {
            return new hardLimitDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override string UnsafeReason()
        {
            return "HardLimit is triggered";
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }

        public override string MaxAsString
        {
            set { }

            get { return 0.ToString(); }
        }

        public override Reading GetReading()
        {
            if (hardLimitPin == null)
                return null;

            Reading r = new Reading
            {
                Stale = false,
                Safe = hardLimitPin.isOn,
                Usable = true,
                secondsSinceLastUpdate = 0,
                timeOfLastUpdate = DateTime.Now,
            };

            r.value = r.Safe ? 1 : 0;

            _status = $"HardLimit is {(r.Safe ? "not triggered" : "triggered")}";
            if (r.Safe != _wasSafe)
            {
                ActivityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.Safe)));
            }
            _wasSafe = r.Safe;
            return r;
        }

        public override void ReadSensorProfile() { }
        public override void WriteSensorProfile() { }
    }

    public class hardLimitDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
