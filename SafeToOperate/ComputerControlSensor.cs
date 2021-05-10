using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40SafeToOperate
{
    public class ComputerControlSensor : Sensor
    {
        public bool _wasSafe = false;
        private string _status = "";

        public ComputerControlSensor(WiseSafeToOperate instance) :
            base("ComputerControl",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.ForcesDecision |
                Attribute.Wise40Specific,
                "", "", "", "",
                instance)
        { }

        public override object Digest()
        {
            return new ComputerControlDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override string UnsafeReason()
        {
            return Const.computerControlAtMaintenance;
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
            if (Hardware.computerControlPin == null)
                return null;

            Reading r = new Reading
            {
                Stale = false,
                Safe = Hardware.ComputerHasControl,
                Usable = true,
                secondsSinceLastUpdate = 0,
                timeOfLastUpdate = DateTime.Now,
                value = Hardware.ComputerHasControl ? 1 : 0,
            };

            _status = r.Safe ? "Operational" : "Maintenance";
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

    public class ComputerControlDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
