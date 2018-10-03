using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40SafeToOperate
{
    public class DoorLockSensor : Sensor
    {
        WisePin doorLockPin;        // TBD
        WisePin doorLockBypassPin;  // TBD

        public DoorLockSensor(WiseSafeToOperate instance) :
            base("DoorLock",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance) { }

        public override string reason()
        {
            return "DoorLock is OPEN";
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
                safe = true // TBD: read doorLockPin and doorLockBypassPin
            };
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            #endregion
            return r;
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }
    }
}
