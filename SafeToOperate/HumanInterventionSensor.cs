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
        private HumanIntervention.HumanInterventionDetails details;

        public HumanInterventionSensor(WiseSafeToOperate instance) :
            base("HumanIntervention",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.ForcesDecision,
                "", "", "", "",
                instance) { }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                Stale = false,
                Safe = !HumanIntervention.IsSet(),
                Usable = true,
                secondsSinceLastUpdate = 0,
                timeOfLastUpdate = DateTime.Now,
            };

            r.value = r.Safe ? 1 : 0;
            if (r.Safe)
                details = null;
            else
            {
                details = HumanIntervention.Details;
                if (details.CampusGlobal)
                    UnsetAttributes(Attribute.Wise40Specific);
                else
                    SetAttributes(Attribute.Wise40Specific);
            }

            if (r.Safe != _wasSafe)
            {
                activityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: r.Safe ? null : details.ToString(),
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.Safe)));
            }
            _wasSafe = r.Safe;
            return r;
        }

        public override string Status
        {
            get
            {
                return details == null ? "Not set" : details.ToString();
            }
        }

        public override object Digest()
        {
            return new HumanInterventionDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
                Details = details,
            };
        }

        public override string reason()
        {
            return (details != null) ? $";Operator: {details.Operator};Reason: {details.Reason};Created: {details.Created} (LT)" : "";
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
        public HumanIntervention.HumanInterventionDetails Details;
    }
}
