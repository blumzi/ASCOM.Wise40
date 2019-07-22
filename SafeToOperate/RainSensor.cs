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
                Attribute.CanBeStale |
                Attribute.CanBeBypassed,
                "", "", "f0", "RainRate",
                instance) { }
        
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

            double seconds = SecondsSinceLastUpdate;
            Reading r = new Reading
            {
                Stale = IsStale,
                secondsSinceLastUpdate = seconds,
                timeOfLastUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(seconds)),
                value = WiseSite.och.RainRate,
            };

            if (r.Stale)
            {
                r.Safe = false;
                r.Usable = false;
            }
            else
            {
                r.Safe = (_max == 0.0) ? r.value == 0.0 : r.value < _max;
                r.Usable = true;
            }

            _status = string.Format("RainRate is {0} (max: {1})", FormatVerbal(r.value), FormatVerbal(_max));
            return r;
        }

        public override string reason()
        {
            return string.Format("{0} out of {1} recent rain rate readings were higher than {2}", _nbad, _repeats, FormatVerbal(_max));
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
