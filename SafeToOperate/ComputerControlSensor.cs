using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class ComputerControlSensor : Sensor
    {
        public ComputerControlSensor(WiseSafeToOperate instance) :
            base("ComputerControl",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance)
        { }

        public override string reason()
        {
            return "ComputerControl at MAINTENANCE";
        }

        public override string MaxAsString
        {
            set { }

            get { return 0.ToString(); }
        }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                safe = !WiseSafeToOperate.wisecomputercontrol.Maintenance
            };
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "ComputerControlSensor: getIsSafe: {0}", r.safe);
            #endregion
            return r;
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }
    }
}
