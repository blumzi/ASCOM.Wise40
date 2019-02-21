using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class HumiditySensor : Sensor
    {
        private double _max;
        private string _status;

        public HumiditySensor(WiseSafeToOperate instance) :
            base("Humidity",
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed, instance) { }

        public override object Digest()
        {
            return new HumidityDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            const double defaultMax = 90.0;

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
                stale = IsStale("Humidity")
            };

            r.value = WiseSite.och.Humidity;
            if (r.stale)
            {
                r.safe = false;
                r.usable = false;
            }
            else
            {
                r.safe = (_max == 0.0) ? r.value == 0.0 : r.value < _max;
                r.usable = true;
            }

            _status = string.Format("Humidity is {0}% (max: {1}%)", r.value, _max);
            return r;
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }
        public override string reason()
        {
            return string.Format("{0} out of {1} recent humidity readings were higher than {2}%",
                _nbad, _repeats, MaxAsString);
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

    public class HumidityDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
