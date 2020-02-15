using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class SunSensor : Sensor
    {
        private double _maxAtDawn, _maxAtDusk;
        private const double defaultMaxAtDawn = -10.0;
        private const double defaultMaxAtDusk = -4.0;
        private bool _wasSafe = false;
        private string _status;

        public SunSensor(WiseSafeToOperate instance) :
            base("Sun",
                Attribute.SingleReading |
                Attribute.Periodic |
                Attribute.AlwaysEnabled |
                Attribute.CanBeBypassed,
                "°", " deg", "f1", "",
                instance) { }

        public override object Digest()
        {
            return new SunDigest()
            {
                Name = WiseName,
                IsSafe = isSafe,
            };
        }

        public override void readSensorProfile()
        {
            MaxAtDawnAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MaxAtDawn", defaultMaxAtDawn.ToString());
            MaxAtDuskAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "MaxAtDusk", defaultMaxAtDusk.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAtDawnAsString, "MaxAtDawn");
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAtDuskAsString, "MaxAtDusk");
        }

        public override Reading getReading()
        {
            if (wisesafetooperate == null)
                return null;

            double max = DateTime.Now.Hour < 12 ? _maxAtDawn : _maxAtDusk;

            Reading r = new Reading
            {
                Stale = false,
                Usable = true,
                Safe = wisesafetooperate.SunElevation <= max,
                value = wisesafetooperate.SunElevation,
                timeOfLastUpdate = DateTime.Now,
                secondsSinceLastUpdate = 0,
            };

            _status = string.Format("Sun elevation is {0} (max: {1})", FormatVerbal(wisesafetooperate.SunElevation), FormatVerbal(max));
            if (r.Safe != _wasSafe)
            {
                activityMonitor.Event(new Event.SafetyEvent(
                    sensor: WiseName,
                    details: _status,
                    before: Event.SafetyEvent.ToSensorSafety(_wasSafe),
                    after: Event.SafetyEvent.ToSensorSafety(r.Safe)));
            }
            _wasSafe = r.Safe;
            return r;
        }

        public override string reason()
        {
            double currentElevation = wisesafetooperate.SunElevation;
            double max = DateTime.Now.Hour < 12 ? _maxAtDawn : _maxAtDusk;

            return currentElevation <= max ? "" : string.Format("The Sun elevation ({0}) is higher than {1}",
                FormatVerbal(currentElevation), FormatVerbal(max));
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }

        public string MaxAtDuskAsString
        {
            set
            {
                _maxAtDusk = Convert.ToDouble(value);
            }

            get
            {
                return _maxAtDusk.ToString();
            }
        }

        public string MaxAtDawnAsString
        {
            set
            {
                _maxAtDawn = Convert.ToDouble(value);
            }

            get
            {
                return _maxAtDawn.ToString();
            }
        }

        public override string MaxAsString { get { return ""; } set { } }
    }

    public class SunDigest
    {
        public string Name;
        public bool IsSafe;
    }
}