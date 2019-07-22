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
                Attribute.Immediate |
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
            if (Hardware.computerControlPin == null)
                return null;

            Reading r = new Reading
            {
                Stale = false,
                Safe = Hardware.computerControlPin.isOn,
                Usable = true,
                secondsSinceLastUpdate = 0,
                timeOfLastUpdate = DateTime.Now,
                value = Hardware.computerControlPin.isOn ? 1 : 0,
            };

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "ComputerControlSensor: getIsSafe: {0}", r.Safe);
            #endregion

            _status = r.Safe ? "Operational" : "Maintenance";
            if (r.Safe != _wasSafe)
            {
                activityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.Safe)));
            }
            _wasSafe = r.Safe;
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
