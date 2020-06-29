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
        private double _max = 10000;
        private string _status = "";

        public PressureSensor(WiseSafeToOperate instance) :
            base("Pressure",
                Attribute.ForInfoOnly |
                Attribute.CanBeStale |
                Attribute.CanBeBypassed |
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled,
                " mBar", " miiliBar", "f1", "Pressure",
                instance)
        { }

        public override object Digest()
        {
            return new PressureDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override void ReadSensorProfile()
        {
            const double defaultMax = 10000;
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
                value = WiseSite.och.Pressure,
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

            _status = string.Format("Pressure is {0} (max: {1})", FormatVerbal(r.value), FormatVerbal(_max));
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
            return string.Format("{0} out of {1} recent pressure readings were higher than {2}.", _nbad, _repeats, FormatVerbal(_max));
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
