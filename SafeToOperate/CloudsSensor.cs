using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class CloudsSensor : Sensor
    {
        private uint _max;

        public CloudsSensor(WiseSafeToOperate instance) :
            base("Clouds",
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed, instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", "0");
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = IsStale("CloudCover")
            };

            if (r.stale)
                r.safe = false;
            else
            {
                double cover = WiseSafeToOperate.och.CloudCover;

                if (_max == 0)
                    r.safe = cover == 0.0;
                else
                    r.safe = cover <= _max;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            #endregion
            return r;
        }

        public override string reason()
        {
            return string.Format("{0} out of {1} recent cloud cover readings were higher than {2}%",
                _nbad, _repeats, MaxAsString);
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
}
