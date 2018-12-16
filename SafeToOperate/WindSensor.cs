using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class WindSensor : Sensor
    {
        double _max;

        public WindSensor(WiseSafeToOperate instance) :
            base("Wind",
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed, instance) { }

        public override object Digest()
        {
            return new WindDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            const double defaultMax = 40;
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
                stale = IsStale("WindSpeed")
            };

            if (r.stale)
                r.safe = false;
            else
            {
                double kmh = WiseSite.och.WindSpeed * 3.6;
                r.safe = (_max == 0.0) ? kmh == 0.0 : kmh < _max;
            }
            return r;
        }

        public override string reason()
        {
            return string.Format("{0} out of {1} recent wind speed readings were higher than {2}km/h.",
                _nbad, _repeats, _max);
        }

        public override string MaxAsString
        {
            get
            {
                return _max.ToString();
            }

            set
            {
                _max = Convert.ToDouble(value);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "Sensor ({0}) Max: {1}", WiseName, MaxAsString);
                #endregion
            }
        }
    }

    public class WindDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
