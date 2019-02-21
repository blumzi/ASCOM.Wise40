using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class SunSensor : Sensor
    {
        private double _max;
        private const double defaultMax = -7.0;
        private bool _wasSafe = false;
        private string _status;

        public SunSensor(WiseSafeToOperate instance) :
            base("Sun",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.CanBeBypassed, instance) { }

        public override object Digest()
        {
            return new SunDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, WiseName, "Max", defaultMax.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, WiseName, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                usable = true,
                safe = wisesafetooperate.SunElevation <= _max
            };

            _status = string.Format("Sun elevation is {0:f1}deg (max: {1:f1}deg)", wisesafetooperate.SunElevation, _max);
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

        public override string reason()
        {
            double currentElevation = wisesafetooperate.SunElevation;

            if (currentElevation <= _max)
                return string.Empty;

            return string.Format("The Sun elevation ({0:f1}deg) is higher than {1:f1}deg.",
                currentElevation, _max);
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
            set
            {
                _max = Convert.ToDouble(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }

    public class SunDigest
    {
        public string Name;
        public bool IsSafe;
    }
}