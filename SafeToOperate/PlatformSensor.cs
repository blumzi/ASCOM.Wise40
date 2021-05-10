using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40SafeToOperate
{
    public class PlatformSensor : Sensor
    {
        private readonly WisePin domePlatformIsDownPin;
        private bool _wasSafe = false;
        private string _status;

        public PlatformSensor(WiseSafeToOperate instance) :
            base("Platform",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.ForcesDecision |
                Attribute.Wise40Specific,
                "", "", "", "",
                instance)
        {
            domePlatformIsDownPin = new WisePin(Const.notsign + "PlatDown",
                Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalIn);
        }

        public override object Digest()
        {
            return new PlatformDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override string UnsafeReason()
        {
            return "Platform is RAISED";
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
            if (domePlatformIsDownPin == null)
                return null;

            Reading r = new Reading
            {
                Stale = false,
                Safe = domePlatformIsDownPin.isOff,
                Usable = true,
                secondsSinceLastUpdate = 0,
                timeOfLastUpdate = DateTime.Now,
            };

            r.value = r.Safe ? 1 : 0;

            _status = $"Platform is {(r.Safe ? "lowered" : "raised")}";
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

    public class PlatformDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
