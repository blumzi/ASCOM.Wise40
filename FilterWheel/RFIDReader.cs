using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40 //.FilterWheel
{
    public class RFIDReader
    {
        private Debugger debugger = Debugger.Instance;

        private static readonly Lazy<RFIDReader> lazy = new Lazy<RFIDReader>(() => new RFIDReader()); // Singleton

        public static RFIDReader Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                return null;
            }
        }

        public RFIDReader() { }
        static RFIDReader() { }

        private const string dummyID = "DEADBEEF83";

        public string UUID
        {
            get
            {
                string ret = string.Empty;

                ret = dummyID;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "RFID.ID Get: returning {0}", ret);
                #endregion
                return ret;
            }
        }
    }
}
