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
        public enum TriStateStatus { Normal = 0, Good = 1, Warning = 2, Error = 3 };
        public enum Direction { None, Increasing, Decreasing };

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
        public const string notsign = "\u00AC";

        public const string fieldSeparator = ",";
        public const string subFieldSeparator = "@";
        public const string recordSeparator = ";";

        public const string topWise40Directory = "c:/Wise40/";
        public const string humanInterventionFilePath = Const.topWise40Directory + "Observatory/HumanIntervention.txt";

        public const string wiseTelescopeDriverID = "ASCOM.Wise40.Telescope";
        public const string wiseDomeDriverID = "ASCOM.Wise40.Dome";
        public const string wiseVantageProDriverID = "ASCOM.Wise40.VantagePro.ObservingConditions";
        public const string wiseBoltwoodDriverID = "ASCOM.Wise40.Boltwood.ObservingConditions";
        public const string wiseSafeToOperateDriverID = "ASCOM.Wise40SafeToOperate.SafetyMonitor";
        public const string wiseFocusDriverID = "ASCOM.Wise40.Focuser";
        public const string wiseFilterWheelDriverID = "ASCOM.Wise40.FilterWheel";
        public const string wiseObservatoryMonitorDriverID = "ASCOM.Wise40.ObservatoryMonitor.SafetyMonitor";

        public const string wiseASCOMServerAppName = "ASCOM.RESTServer";
        public const string wiseASCOMServerPath = "c:/Program Files (x86)/ASCOM/Remote/ASCOM.RESTServer.exe";

        public const string wiseObservatoryMonitorAppName = "ObservatoryMonitor";
        public const string wiseASCOMOCHServerAppName = "ASCOM.OCH.Server";

        public const string wiseASCOMRemoteClientLocalServerAppName = "ASCOM.RemoteClientLocalServer";

        public const string wiseDashboardAppName = "Dash";
        public const string wiseSimulatedDashPath = "c:/Users/Blumzi/Documents/Visual Studio 2015/Projects/Wise40/Dash/bin/x86/Debug/Dash.exe";
        public const string wiseRealDashPath = "c:/Users/mizpe/source/repos/ASCOM.Wise40/Dash/bin/x86/Debug/Dash.exe";

        public const string wiseWeatherLinkAppPath = "c:/WeatherLink/WeatherLink 6.0.2.exe";
        public const string wiseWeatherLinkAppName = "WeatherLink 6.0.2";

        public const string computerControlAtMaintenance = "ComputerControl at Maintenance";

        public const double twoPI = 2.0 * Math.PI;
        public const double halfPI = Math.PI / 2;
        public const double onePI = Math.PI;

        public const double noTarget = -500.0;  // An impossible angle (RightAscension, HourAngle or Declination)

        public class ProfileName
        {
            public static string Telescope_AstrometricAccuracy = "AstrometricAccuracy";
            public static string Telescope_EnslaveDome = "EnslaveDome";
            public static string Telescope_CalculateRefraction = "CalculateRefraction";
            public static string Telescope_Tracing = "Tracing";
            public static string Telescope_BypassCoordinatesSafety = "BypassCoordinatesSafety";
            public static string Telescope_PlotSlews = "PlotSlews";
            public static string Telescope_MinutesToIdle = "MinutesToIdle";

            public static string SafeToOperate_AgeMaxSeconds = "AgeMaxSeconds";
            public static string SafeToOperate_StableAfterMin = "StableAfterMin";
            public static string SafeToOperate_Bypassed = "Bypassed";
            public static string SafeToOperate_DoorLockDelay = "DoorLockDelay";

            public static string Dome_AutoCalibrate = "AutoCalibrate";
            public static string Dome_SyncVentWithShutter = "SyncVentWithShutter";
            public static string Dome_MinimalTrackingMovement = "MinimalDomeTrackingMovement";

            public static string DomeShutter_IPAddress = "ShutterIPAddress";
            public static string DomeShutter_HighestValue = "ShutterHighestValue";
            public static string DomeShutter_LowestValue = "ShutterLowestValue";
            public static string DomeShutter_UseWebClient = "ShutterUseWebClient";

            public static string Boltwood_DataFile = "DataFile";
            public static string Boltwood_Enabled = "Enabled";
            public static string Boltwood_InputMethod = "InputMethod";
            public static string Boltwood_Name = "Name";

            public static string VantagePro_OpMode = "OperationMode";
            public static string VantagePro_DataFile = "DataFile";
            public static string VantagePro_SerialPort = "Port";
        }

        public class RESTServer
        {
            public static string top = "http://132.66.65.9:11111/server/v1/";
        }

        public class App
        {
            public string Name;
            public string RealPath;
            public string SimulatedPath;

        }

        public static Dictionary<string, App> Apps = new Dictionary<string, App>()
            {
                {
                    "Dash", new App {
                        Name = "Dash",
                        RealPath = "c:/Users/mizpe/source/repos/ASCOM.Wise40/Dash/bin/x86/Debug/Dash.exe",
                        SimulatedPath = "c:/Users/Blumzi/Documents/Visual Studio 2015/Projects/Wise40/Dash/bin/x86/Debug/Dash.exe"
                    }
                },
            };

    }
}
