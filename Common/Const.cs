using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public static class Const
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
        public const string noValue = "***";

        public const string fieldSeparator = ",";
        public const string subFieldSeparator = "@";
        public const string recordSeparator = ";";

        public const string topWise40Directory = "c:/Wise40/";
        public const string topWiseDirectory = "c:/Wise/";
        public const string humanInterventionFilePath = Const.topWise40Directory + "Observatory/HumanIntervention.txt";

        public static class WiseDriverID
        {
            public const string Telescope = "ASCOM.Wise40.Telescope";
            public const string Dome = "ASCOM.Wise40.Dome";
            public const string VantagePro = "ASCOM.Wise40.VantagePro.ObservingConditions";
            public const string Boltwood = "ASCOM.Wise40.Boltwood.ObservingConditions";
            public const string SafeToOperate = "ASCOM.Wise40SafeToOperate.SafetyMonitor";
            public const string WiseSafeToOperate = "ASCOM.WiseSafeToOperate.SafetyMonitor";
            public const string Focus = "ASCOM.Wise40.Focuser";
            public const string FilterWheel = "ASCOM.Wise40.FilterWheel";
            public const string ObservatoryMonitor = "ASCOM.Wise40.ObservatoryMonitor.SafetyMonitor";
            public const string TessW = "ASCOM.Wise40.TessW.ObservingConditions";
        }

        public const string computerControlAtMaintenance = "ComputerControl at Maintenance";

        public const double twoPI = 2.0 * Math.PI;
        public const double halfPI = Math.PI / 2;
        public const double onePI = Math.PI;

        public const double noTarget = -500.0;  // An impossible angle (RightAscension, HourAngle or Declination)

        public static class ProfileName
        {
            public static string Telescope_AstrometricAccuracy = "AstrometricAccuracy";
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
            public static string Dome_MinimalMovement = "MinimalMovement";

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

            public static string TessW_IpAddress = "IPAddress";
            public static string TessW_Enabled = "Enabled";

            public static string Site_DebugLevel = "SiteDebugLevel";
        }

        public static class RESTServer
        {
            public static string top = "http://127.0.0.1:11111/server/v1/";
        }

        public class App
        {
            private const string SimulatedTopDir = "c:/Users/Blumzi/Documents/Visual Studio 2015/Projects/Wise40/";
            private const string RealTopDir = "c:/Users/mizpe/source/repos/ASCOM.Wise40/";

            public string appName;
            public string path;
            public bool locallyDeveloped;

            public string Path
            {
                get
                {
                    if (path == null)
                        return null;

                    if (locallyDeveloped)
                        return (WiseObject.Simulated ? SimulatedTopDir : RealTopDir) + path;
                    return path;
                }
            }
        }

        public enum Application { RESTServer, Dash, WeatherLink, ObservatoryMonitor, OCH, RemoteClientLocalServer, SafetyDash }

        public static Dictionary<Application, App> Apps = new Dictionary<Application, App>()
            {
                {
                    Application.RESTServer, new App
                    {
                        locallyDeveloped = false,
                        appName = "ASCOM.RemoteServer",
                        path = "c:/Program Files (x86)/ASCOM/Remote/ASCOM.RemoteServer.exe",
                    }
                },
                {
                    Application.Dash, new App {
                        locallyDeveloped = true,
                        appName = "Dash",
                        path = "Dash/bin/x86/Debug/Dash.exe",
                    }
                },
                {
                    Application.SafetyDash, new App {
                        locallyDeveloped = false,
                        appName = "SafetyDash",
                        path = "c:/Program Files (x86)/Wise/Wise Remote Safety Dashboard/RemoteSafetyDashboard.exe",
                    }
                },

                {
                    Application.WeatherLink, new App
                    {
                        locallyDeveloped = false,
                        path = "c:/WeatherLink/WeatherLink 6.0.2.exe",
                        appName = "WeatherLink 6.0.2",
                    }
                },

                {
                    Application.ObservatoryMonitor, new App
                    {
                        locallyDeveloped = true,
                        appName = "ObservatoryMonitor",
                        path = "ObservatoryMonitor/bin/Debug/ObservatoryMonitor.exe",
                    }
                },

                {
                    Application.OCH, new App
                    {
                        locallyDeveloped = false,
                        appName = "ASCOM.OCH.Server",
                    }
                },

                {
                    Application.RemoteClientLocalServer, new App
                    {
                        locallyDeveloped = false,
                        appName = "ASCOM.RemoteClientLocalServer",
                    }
                },
            };

        public static class UnsafeReasons
        {
            public static string ShuttingDown = "Wise40 is shutting down";
        }

        public static class MySql
        {
            public static class DatabaseConnectionString
            {
                public static string LCO_hibernate = "server=pubsubdb.tlv.lco.gtn;user=hibernate;database=hibernate;port=3306;password=hibernate";
                public static string Wise_weather = "server=localhost;user=root;database=wise;port=3306;password=@!ab4131!@";
                public static string Wise40_activities = "server=localhost;user=root;database=wise40;port=3306;password=@!ab4131!@";
            }
        }
    }
}
