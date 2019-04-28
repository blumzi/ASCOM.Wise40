using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class RainSensor : Sensor
    {
        private double _max;
        private string _status;

        public RainSensor(WiseSafeToOperate instance) :
            base("Rain", 
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed, instance) { }
        
        public override object Digest()
        {
            return new RainDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            const double defaultMax = 0.0;
            MaxAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Max", defaultMax.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            if (WiseSite.och == null)
                return null;


            Reading r = new Reading
            {
                stale = IsStale("RainRate")
            };

            r.value = WiseSite.och.RainRate;
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

            _status = string.Format("RainRate is {0} (max: {1})", r.value, _max);
            return r;
        }

        public override string reason()
        {
            return string.Format("{0} out of {1} recent rain rate readings were higher than {2}",
                _nbad, _repeats, MaxAsString);
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

    public class RainDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
