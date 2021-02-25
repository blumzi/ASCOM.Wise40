using System;
using System.IO;
using ASCOM.Utilities;

namespace ASCOM.Wise40.Common
{
    public class WiseObject
    {
        public string WiseName { get; set; }

        public static bool Simulated { get; } = !string.Equals(Environment.MachineName, "dome-pc", StringComparison.OrdinalIgnoreCase);
    }
}