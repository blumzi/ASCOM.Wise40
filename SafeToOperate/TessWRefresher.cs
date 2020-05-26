using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40SafeToOperate
{
    public class TessWRefresher: Sensor
    {
        private readonly DriverAccess.ObservingConditions tessw =
            new DriverAccess.ObservingConditions("ASCOM.Wise40.TessW.ObservingConditions");

        public TessWRefresher(WiseSafeToOperate instance) :
            base("TessWRefresher",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "", "", "", "",
                instance)
        {
            tessw.Connected = true;
        }

        public override string UnsafeReason()
        {
            return string.Empty;
        }

        public override Reading GetReading()
        {
            tessw.Refresh();
            return null;
        }

        public override object Digest()
        {
            return tessw.Action("status", "");
        }

        public override string MaxAsString
        {
            get { return ""; }
            set { }
        }

        public override void WriteSensorProfile() { }
        public override void ReadSensorProfile() { }

        public override string Status
        {
            get
            {
                return "";
            }
        }
    }
}
