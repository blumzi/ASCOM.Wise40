using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;

namespace ASCOM.Wise40SafeToOperate
{
    public class HumanInterventionSensor : Sensor
    {
        private bool _wasSafe = false;
        private string _status;

        public HumanInterventionSensor(WiseSafeToOperate instance) :
            base("HumanIntervention",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance) { }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                safe = !Wise40.HumanIntervention.IsSet(),
                usable = true,
            };

            _status = string.Format("{0}", r.safe ? "Not set" : HumanIntervention.Info);
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

        public override object Digest()
        {
            return new HumanInterventionDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override string reason()
        {
            return Wise40.HumanIntervention.Info;
        }

        public override string MaxAsString
        {
            set { }

            get { return 0.ToString(); }
        }
    }

    public class HumanInterventionDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
