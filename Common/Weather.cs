using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public abstract class WeatherStation: WiseObject
    {
        public enum WeatherStationVendor { DavisInstruments, Boltwood, Stars4All };
        public enum WeatherStationModel { VantagePro2, CloudSensorII, TessW };
        public enum WeatherStationInputMethod
        {
            ClarityII,
            WeatherLink_HtmlReport,
            TessW,
            Weizmann_TBD,
            Korean_TBD
        };

        public int _unitId;
        protected WeatherLogger _weatherLogger;

        public abstract WeatherStationVendor Vendor
        {
            get;
        }

        public abstract WeatherStationModel Model
        {
            get;
        }

        //public abstract string RawData
        //{
        //    get;
        //}

        public abstract bool Enabled
        {
            get;
            set;
        }

        public abstract WeatherStationInputMethod InputMethod
        {
            get;
            set;
        }
    }
}
