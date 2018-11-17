using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class PlatformSensor : Sensor
    {
        public PlatformSensor(WiseSafeToOperate instance) :
            base("Platform",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance)
        { }

        public override string reason()
        {
            return "Platform is RAISED";
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
                safe = WiseSafeToOperate.wisecomputercontrol.PlatformIsDown
            };
            //#region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            //#endregion
            return r;
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }
    }
}
