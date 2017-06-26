using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.FilterWheel
{
    public class RFIDReader
    {
        private Debugger debugger = Debugger.Instance;

        private static readonly RFIDReader _instance = new RFIDReader();
        public static RFIDReader Instance
        {
            get
            {
                return _instance;
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
