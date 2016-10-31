using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public class Const
    {
        // public const double SiderealRate = 0.9972695664; //0.9972695677 (orig) 1⁄1,002737909350795

        private const double stellarDay = 86164.0905308329; //86164.098903691; //86164.09054 (wolfram alpha)
        private const double solarDay = 86400;
        private const double lunarDay = 89416.2793513594;

        public enum CardinalDirection { North = 0, East = 1, South = 2, West = 3 };
        public enum AxisDirection { None = 0, Increasing = 1, Decreasing = -1 };
        public enum TriStateStatus { Good = 1, Warning = 2, Error = 3 };

        public const double rateStopped = 0.0;
        public const double rateSlew = 2.0;                           // two degrees/sec
        public const double rateSet = 1.0 / 60;                       // one minute/sec
        public const double rateGuide = 1.0 / 3600;                   // one second/sec
        public const double rateTrack = 360.0 / stellarDay;           // sidereal rate
        public const double rateTrackLunar = 360.0 / lunarDay;        // lunar rate
        public const double rateTrackSolar = 360.0 / solarDay;        // solar rate

        public const double defaultReadTimeoutMillis = 2000.0;
        public const int defaultReadRetries = 20;

        public const string crnl = "\r\n";
        public const string checkmark = " ✓";
    }
}
