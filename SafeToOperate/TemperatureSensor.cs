using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class TemperatureSensor : Sensor
    {
        private double _max = 10000;
        private string _status = "";

        public TemperatureSensor(WiseSafeToOperate instance) :
            base("Temperature",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.CanBeStale |
                Attribute.CanBeBypassed |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "°C", " deg", "G3", "Temperature",
                instance)
        { }

        public override object Digest()
        {
            return new TemperatureDigest()
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
                value = WiseSite.och.Temperature,
                secondsSinceLastUpdate = seconds,
                timeOfLastUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(seconds)),
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

            _status = $"Temperature is {FormatVerbal(r.value)} (max: {FormatVerbal(_max)})";
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
            return $"{_nbad} out of {_repeats} recent temperature readings were higher than {FormatVerbal(_max)}.";
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
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"Sensor ({WiseName}) Max: {MaxAsString}");
                #endregion
            }
        }
    }

    public class TemperatureDigest
    {
        public string Name;
        public bool IsSafe;
    }
}
