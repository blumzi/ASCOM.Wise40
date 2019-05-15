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
                SensorAttribute.CanBeBypassed,
                "°", " deg", "f1", "",
                instance) { }

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
            MaxAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Max", defaultMax.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            if (wisesafetooperate == null)
                return null;

            Reading r = new Reading
            {
                Stale = false,
                Usable = true,
                Safe = wisesafetooperate.SunElevation <= _max,
                value = wisesafetooperate.SunElevation,
                timeOfLastUpdate = DateTime.Now,
                secondsSinceLastUpdate = 0,
            };

            _status = string.Format("Sun elevation is {0} (max: {1})", FormatVerbal(wisesafetooperate.SunElevation), FormatVerbal(_max));
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

        public override string reason()
        {
            double currentElevation = wisesafetooperate.SunElevation;

            if (currentElevation <= _max)
                return string.Empty;

            return string.Format("The Sun elevation ({0}) is higher than {1}.", FormatVerbal(currentElevation), FormatVerbal(_max));
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