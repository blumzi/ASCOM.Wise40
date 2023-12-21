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
        private ASCOM.DriverAccess.ObservingConditions tessw;

        public CloudsSensor(WiseSafeToOperate instance) :
            base("Clouds",
                Attribute.Periodic |
                Attribute.CanBeStale |
                Attribute.CanBeBypassed /*|
                Attribute.AlwaysEnabled  */,
                "", "", "f0", "CloudCover",
                instance) {

            //
            // The only clouds sensor we currently have is the TessW (no more Boltwoods)
            // This sensor (CloudsSensor) cannot be enabled unless teh TessW is enabled
            //
            bool tessWEnabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "TessWRefresher", "Enabled", true.ToString()));
            if (tessWEnabled)
                Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "Clouds", "Enabled", true.ToString()));
            else
                Enabled = false;

            if (Enabled)
                tessw = new DriverAccess.ObservingConditions("ASCOM.Wise40.TessW.ObservingConditions")            {
                    Connected = true
                };
        }

        public override object Digest()
        {
            return new CloudsDigest()
            {
                Name = WiseName,
                IsSafe = IsSafe,
            };
        }

        public override void ReadSensorProfile()
        {
            const uint defaultMax = 0;

            MaxAsString = wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, WiseName, "Max", defaultMax.ToString());
        }

        public override void WriteSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, WiseName, MaxAsString, "Max");
        }

        public override Reading GetReading()
        {
            if (tessw == null || !Enabled)
                return null;

            double seconds = SecondsSinceLastUpdate;
            Reading r = new Reading
            {
                Stale = IsStale,
                secondsSinceLastUpdate = seconds,
                timeOfLastUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(seconds)),
                value = tessw.CloudCover,
            };

            if (r.Stale) {
                r.Safe = false;
                r.Usable = false;
                _status = "Stale data";
            }
            else
            {
                if (_max == 0)
                    r.Safe = r.value == 0.0;
                else
                    r.Safe = r.value <= _max;
                r.Usable = true;
                _status = $"Cloud cover {FormatVerbal(r.value)} (max: {FormatVerbal(_max)})";
            }

            return r;
        }

        public override string UnsafeReason()
        {
            return $"{_nbad} out of {_repeats} recent cloud cover readings were higher than {FormatVerbal(_max)}";
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
