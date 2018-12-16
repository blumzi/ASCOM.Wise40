using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class HumanInterventionSensor : Sensor
    {
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
                safe = !Wise40.HumanIntervention.IsSet()
            };
            //#region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            //#endregion
            return r;
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
