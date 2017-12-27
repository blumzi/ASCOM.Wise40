using System;
using System.IO;
using ASCOM.Utilities;

namespace ASCOM.Wise40.Common
{
    public class WiseObject
    {
        private string _name;
        //private bool _simulated = Environment.MachineName.ToLower() != "dome-ctlr";
        private bool _simulated = AreWeReallySimulated();

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public bool Simulated
        {
            get
            {
                return _simulated;
            }
            set
            {
                _simulated = value;
            }
        }

        private static bool AreWeReallySimulated()
        {
            return File.Exists(Const.topWise40Directory + "simulate") || 
                (Environment.MachineName.ToLower() != "dome-pc");
        }
    }
}