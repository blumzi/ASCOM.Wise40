using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class PressureSensor : Sensor
    {
        double _max = 10000;
        private string _status = "";

        public PressureSensor(WiseSafeToOperate instance) :
            base("Pressure",
                SensorAttribute.ForInfoOnly |
                SensorAttribute.CanBeStale |
                SensorAttribute.CanBeBypassed |
                SensorAttribute.AlwaysEnabled, instance)
        { }

        public override object Digest()
        {
            return new PressureDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            const double defaultMax = 10000;
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
                stale = IsStale("Pressure")
            };

            r.value = WiseSite.och.Pressure;

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

            _status = r.stale ? "Stale data" : string.Format("Pressure is {0:f1} mBar (max: {1:f1} mBar)", r.value, _max);
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
            return string.Format("{0} out of {1} recent pressure readings were higher than {2} mBar.",
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

    public class PressureDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
