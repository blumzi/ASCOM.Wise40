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
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.ForcesDecision, instance)
        { }

        public override object Digest()
        {
            return new ComputerControlDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override string reason()
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

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                safe = Hardware.computerControlPin.isOn,
                usable = true,
            };

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "ComputerControlSensor: getIsSafe: {0}", r.safe);
            #endregion

            if (r.safe != _wasSafe)
            {
                _status = r.safe ? "Operational" : "Maintenance";
                activityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.safe)));
            }
            _wasSafe = r.safe;
            return r;
        }

        public override void readSensorProfile() { }
        public override void writeSensorProfile() { }
    }

    public class ComputerControlDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
