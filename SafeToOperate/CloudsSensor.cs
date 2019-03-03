using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class CloudsSensor : Sensor
    {
        private uint _max;
        private string _status = "";

        public CloudsSensor(WiseSafeToOperate instance) :
            base("Clouds",
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed, instance) { }

        public override object Digest()
        {
            return new CloudsDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            const uint defaultMax = 0;

            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, WiseName, "Max", defaultMax.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, WiseName, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            if (WiseSite.och == null)
                return null;

            Reading r = new Reading
            {
                stale = IsStale("CloudCover")
            };

            if (r.stale) {
                r.safe = false;
                r.usable = false;
                _status = "Stale data";
            }
            else
            {
                r.value = WiseSite.och.CloudCover;

                if (_max == 0)
                    r.safe = r.value == 0.0;
                else
                    r.safe = r.value <= _max;
                r.usable = true;
                _status = string.Format("Cloud cover {0:f1} (max: {1:f1})", r.value, _max);
            }

            return r;
        }

        public override string reason()
        {
            return string.Format("{0} out of {1} recent cloud cover readings were higher than {2}%",
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
                _max = Convert.ToUInt32(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }

    public class CloudsDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
