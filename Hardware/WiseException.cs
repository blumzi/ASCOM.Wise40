using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.WiseHardware
{
    class WiseException : Exception
    {
        public WiseException(string message) : base(message)
        {
            throw new DriverException(message);
        }
    }
}
