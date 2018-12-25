using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using MccDaq;

namespace ASCOM.Wise40SafeToOperate
{
    public class PlatformSensor : Sensor
    {
        private WisePin domePlatformIsDownPin;

        public PlatformSensor(WiseSafeToOperate instance) :
            base("Platform",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance)
        {
            domePlatformIsDownPin = new WisePin(Const.notsign + "PlatDown",
                Hardware.Instance.domeboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalIn);
        }

        public override object Digest()
        {
            return new PlatformDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

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
                safe = domePlatformIsDownPin.isOff,
            };
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", WiseName, r.safe);
            #endregion
            return r;
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }
    }

    public class PlatformDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
