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
                Attribute.Periodic |
                Attribute.CanBeStale |
                Attribute.CanBeBypassed,
                "%", " percent", "G3", "Humidity",
                instance) { }

        public override object Digest()
        {
            return new HumidityDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override void ReadSensorProfile()
        {
            const double defaultMax = 90.0;

            MaxAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Max", defaultMax.ToString());
        }

        public override void WriteSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAsString, "Max");
        }

        public override Reading GetReading()
        {
            if (WiseSite.och == null)
                return null;

            double seconds = SecondsSinceLastUpdate;
            Reading r = new Reading
            {
                Stale = IsStale,
                secondsSinceLastUpdate = seconds,
                timeOfLastUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(seconds)),
                value = WiseSite.och.Humidity,
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

            _status = $"Humidity is {FormatVerbal(r.value)} (max: {FormatVerbal(_max)})";
            return r;
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }
        public override string UnsafeReason()
        {
            return $"{_nbad} out of {_repeats} recent humidity readings were higher than {FormatVerbal(_max)}";
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
