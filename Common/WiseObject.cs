using System;
using System.IO;
using ASCOM.Utilities;

namespace ASCOM.Wise40.Common
{
    public class WiseObject
    {
        private string _name;
        private static bool _simulated = Environment.MachineName.ToLower() != "dome-pc";

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
        }
    }
}