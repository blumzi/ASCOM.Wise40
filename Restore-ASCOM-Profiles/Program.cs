using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Utilities;
using ASCOM.Wise40.Common;
using System.Windows.Forms;

namespace Restore_ASCOM_Profiles
{
    public class Program
    {
        private static bool realMachine = Environment.MachineName.ToLower() == "dome-pc";
        public enum Mode { LCO, ACP, WISE, SKIP };
        public static Mode mode = Mode.WISE;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Mode m;
                if (Enum.TryParse<Mode>(args[0].ToUpper(), out m))
                    mode = m;
            }

            if (mode == Mode.SKIP)
                Environment.Exit(0);

            WriteCloudSensorProfile();
            WriteVantageProProfile();
            WriteSafeToOpenProfile();
            //WriteSafeToImageProfile();
            WriteDomeProfile();
            WriteTelescopeProfile();
            WriteOCHProfile();
            WriteFilterWheelProfile();
            WriteObservatoryMonitorProfile();

            string message = string.Format("The Wise40 ASCOM Profiles have been initializes to mode \"{0}\".", mode.ToString());
            Console.WriteLine(message);
            MessageBox.Show(message);

            Environment.Exit(0);
        }

        internal static void WriteCloudSensorProfile()
        {
            string driverID = "ASCOM.Wise40.Boltwood.ObservingConditions";
            string dataFileProfileName = "DataFile";
            string dataFile = realMachine ?
                "//WO-NEO/Temp/clarityII-data.txt" :
                "c:/temp/ClarityII-data.txt";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, dataFileProfileName, dataFile);
            }
        }

        internal static void WriteVantageProProfile()
        {
            string driverID = "ASCOM.Wise40.VantagePro.ObservingConditions";
            string reportFileProfileName = "DataFile";
            string reportFile = realMachine ? 
                "c:/Wise40/Weather/Davis VantagePro/Weather_Wise40_Vantage_Pro.htm" :
                "c:/temp/Weather_Wise40_Vantage_Pro.htm";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, reportFileProfileName, reportFile);
            }
        }

        internal static void WriteSafeToOpenProfile()
        {
            string driverID = "ASCOM.Wise40SafeToOpen.SafetyMonitor";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                //driverProfile.WriteValue(driverID, "Max", "cloudClear", "Clouds");
                //driverProfile.WriteValue(driverID, "Interval", "30", "Clouds");
                //driverProfile.WriteValue(driverID, "Repeats", "3", "Clouds");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Clouds");

                //driverProfile.WriteValue(driverID, "Max", "70", "Wind");
                //driverProfile.WriteValue(driverID, "Interval", "30", "Wind");
                //driverProfile.WriteValue(driverID, "Repeats", "3", "Wind");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Wind");

                //driverProfile.WriteValue(driverID, "Max", "0", "Rain");
                //driverProfile.WriteValue(driverID, "Interval", "30", "Rain");
                //driverProfile.WriteValue(driverID, "Repeats", "3", "Rain");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Rain");

                //driverProfile.WriteValue(driverID, "Max", "40", "Humidity");
                //driverProfile.WriteValue(driverID, "Interval", "30", "Humidity");
                //driverProfile.WriteValue(driverID, "Repeats", "4", "Humidity");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Humidity");

                //driverProfile.WriteValue(driverID, "Max", "dayDark", "Light");
                //driverProfile.WriteValue(driverID, "Interval", "30", "Light");
                //driverProfile.WriteValue(driverID, "Repeats", "4", "Light");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Light");

                //driverProfile.WriteValue(driverID, "Max", "-7", "Sun");
                //driverProfile.WriteValue(driverID, "Interval", "60", "Sun");
                //driverProfile.WriteValue(driverID, "Repeats", "2", "Sun");
                //driverProfile.WriteValue(driverID, "Enabled", "True", "Sun");

                driverProfile.WriteValue(driverID, "Clouds", "cloudClear", "Max");
                driverProfile.WriteValue(driverID, "Clouds", "30", "Interval");
                driverProfile.WriteValue(driverID, "Clouds", "3", "Repeats");
                driverProfile.WriteValue(driverID, "Clouds", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Wind", "70", "Max");
                driverProfile.WriteValue(driverID, "Wind", "30", "Interval");
                driverProfile.WriteValue(driverID, "Wind", "3", "Repeats");
                driverProfile.WriteValue(driverID, "Wind", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Rain", "0", "Max");
                driverProfile.WriteValue(driverID, "Rain", "30", "Interval");
                driverProfile.WriteValue(driverID, "Rain", "2", "Repeats");
                driverProfile.WriteValue(driverID, "Rain", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Humidity", "40", "Max");
                driverProfile.WriteValue(driverID, "Humidity", "30", "Interval");
                driverProfile.WriteValue(driverID, "Humidity", "4", "Repeats");
                driverProfile.WriteValue(driverID, "Humidity", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Light", "dayDark", "Max");
                driverProfile.WriteValue(driverID, "Light", "30", "Interval");
                driverProfile.WriteValue(driverID, "Light", "4", "Repeats");
                driverProfile.WriteValue(driverID, "Light", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Sun", "-7", "Max");
                driverProfile.WriteValue(driverID, "Sun", "60", "Interval");
                driverProfile.WriteValue(driverID, "Sun", "2", "Repeats");
                driverProfile.WriteValue(driverID, "Sun", "True", "Enabled");
            }
        }

        internal static void WriteSafeToImageProfile()
        {
            string driverID = "ASCOM.Wise40SafeToImage.SafetyMonitor";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, "Clouds", "cloudClear", "Max");
                driverProfile.WriteValue(driverID, "Clouds", "30", "Interval");
                driverProfile.WriteValue(driverID, "Clouds", "3", "Repeats");
                driverProfile.WriteValue(driverID, "Clouds", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Wind", "1000", "Max");
                driverProfile.WriteValue(driverID, "Wind", "30", "Interval");
                driverProfile.WriteValue(driverID, "Wind", "3", "Repeats");
                driverProfile.WriteValue(driverID, "Wind", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Rain", "0", "Max");
                driverProfile.WriteValue(driverID, "Rain", "30", "Interval");
                driverProfile.WriteValue(driverID, "Rain", "3", "Repeats");
                driverProfile.WriteValue(driverID, "Rain", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Humidity", "40", "Max");
                driverProfile.WriteValue(driverID, "Humidity", "30", "Interval");
                driverProfile.WriteValue(driverID, "Humidity", "4", "Repeats");
                driverProfile.WriteValue(driverID, "Humidity", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Light", "dayDark", "Max");
                driverProfile.WriteValue(driverID, "Light", "30", "Interval");
                driverProfile.WriteValue(driverID, "Light", "4", "Repeats");
                driverProfile.WriteValue(driverID, "Light", "True", "Enabled");

                driverProfile.WriteValue(driverID, "Sun", "-7", "Max");
                driverProfile.WriteValue(driverID, "Sun", "60", "Interval");
                driverProfile.WriteValue(driverID, "Sun", "2", "Repeats");
                driverProfile.WriteValue(driverID, "Sun", "True", "Enabled");
            }
        }

        internal static void WriteDomeProfile()
        {
            string driverID = "ASCOM.Wise40.Dome";
            string autoCalibrateProfileName = "AutoCalibrate";
            string bypassSafetyProfileName = "Bypass Safety";
            string syncVentWithShutterProfileName = "Sync Vent With Shutter";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, autoCalibrateProfileName, true.ToString());
                driverProfile.WriteValue(driverID, bypassSafetyProfileName, true.ToString());
                driverProfile.WriteValue(driverID, syncVentWithShutterProfileName, mode == Mode.WISE ? false.ToString() : true.ToString());
            }
        }

        internal static void WriteTelescopeProfile()
        {
            string driverID = "ASCOM.Wise40.Telescope";
            string enslaveDomeProfileName = "Enslave Dome";
            string traceStateProfileName = "Tracing";
            string debugLevelProfileName = "DebugLevel";
            string studyMotionProfileName = "StudyMotion";
            string debugFileProfileName = "DebugFile";
            string debugFile = Const.topWise40Directory + "Logs/debug.txt";
            string refractionProfileName = "Calculate refraction";
            string minimalDomeTrackingMovementProfileName = "Minimal Dome Tracking Movement";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, studyMotionProfileName, false.ToString());
                driverProfile.WriteValue(driverID, enslaveDomeProfileName, mode == Mode.ACP ? false.ToString() : true.ToString());
                driverProfile.WriteValue(driverID, debugLevelProfileName, (Debugger.DebugLevel.DebugAxes |
                    Debugger.DebugLevel.DebugExceptions |
                    Debugger.DebugLevel.DebugDevice |
                    Debugger.DebugLevel.DebugASCOM |
                    Debugger.DebugLevel.DebugLogic).ToString());
                driverProfile.WriteValue(driverID, debugFileProfileName, debugFile);
                driverProfile.WriteValue(driverID, refractionProfileName, mode == Mode.LCO ? false.ToString() : true.ToString());
                driverProfile.WriteValue(driverID, minimalDomeTrackingMovementProfileName, "2.0");
            }
        }

        internal static void WriteOCHProfile()
        {
            string driverID = "ASCOM.OCH.ObservingConditions";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "CloudCover");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.Boltwood.ObservingConditions", "CloudCover");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "CloudCover");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "DewPoint");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "DewPoint");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "DewPoint");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "Humidity");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "Humidity");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "Humidity");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "Pressure");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "Pressure");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "Pressure");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "RainRate");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "RainRate");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "RainRate");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "SkyTemperature");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.Boltwood.ObservingConditions", "SkyTemperature");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "SkyTemperature");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "Temperature");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "Temperature");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "Temperature");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "WindDirection");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "WindDirection");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "WindDirection");

                driverProfile.WriteValue(driverID, "Device Mode", "Real", "WindSpeed");
                driverProfile.WriteValue(driverID, "ProgID", "ASCOM.Wise40.VantagePro.ObservingConditions", "WindSpeed");
                driverProfile.WriteValue(driverID, "Switch Number", "0", "WindSpeed");
            }
        }

        internal static void WriteFilterWheelProfile()
        {
            string driverID = "ASCOM.Wise40.FilterWheel";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";

                driverProfile.WriteValue(driverID, "Port", realMachine ? "COM6" : "COM5");

                driverProfile.WriteValue(driverID, "RFID", "7F0007F75E", "Wheel8/Position1");
                driverProfile.WriteValue(driverID, "RFID", "7F000817F7", "Wheel8/Position2");
                driverProfile.WriteValue(driverID, "RFID", "7F000AEFC5", "Wheel8/Position3");
                driverProfile.WriteValue(driverID, "RFID", "7C00563E5A", "Wheel8/Position4");
                driverProfile.WriteValue(driverID, "RFID", "7F001B2B73", "Wheel8/Position5");
                driverProfile.WriteValue(driverID, "RFID", "7F000ACAD5", "Wheel8/Position6");
                driverProfile.WriteValue(driverID, "RFID", "7F001B4A83", "Wheel8/Position7");
                driverProfile.WriteValue(driverID, "RFID", "7F0007BC0E", "Wheel8/Position8");

                driverProfile.WriteValue(driverID, "RFID", "7F001B4C16", "Wheel4/Position1");
                driverProfile.WriteValue(driverID, "RFID", "7C0055F4EB", "Wheel4/Position2");
                driverProfile.WriteValue(driverID, "RFID", "7F0007F75E", "Wheel4/Position3");
                driverProfile.WriteValue(driverID, "RFID", "7F001B0573", "Wheel4/Position4");

                driverProfile.WriteValue(driverID, "Filter Name", "U", "Wheel4/Position1");
                driverProfile.WriteValue(driverID, "Filter Name", "U", "Wheel8/Position1");
            }
        }

        internal static void WriteObservatoryMonitorProfile()
        {
            string driverID = "ASCOM.Wise40.ObservatoryMonitor";

            string intervalProfileName = "Interval";
            string lightEventsProfileName = "LightEvents";
            string sunEventsProfileName = "SunEvents";
            string windEventsProfileName = "WindEvents";
            string rainEventsProfileName = "RainEvents";
            string cloudEventsProfileName = "CloudEvents";
            string humidityEventsProfileName = "HumidityEvents";

            int _defaultInterval = 30;
            int _defaultCloudEvents = 5;
            int _defaultLightEvents = 3;
            int _defaultSunEvents = 2;
            int _defaultWindEvents = 4;
            int _defaultRainEvents = 2;
            int _defaultHumidityEvents = 2;

            using (Profile driverProfile = new Profile())
            {
                if (!driverProfile.IsRegistered(driverID))
                    driverProfile.Register(driverID, string.Format("ASCOM Wise40.ObservatoryMonitor v0.2"));

                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, intervalProfileName, _defaultInterval.ToString());
                driverProfile.WriteValue(driverID, lightEventsProfileName, _defaultLightEvents.ToString());
                driverProfile.WriteValue(driverID, sunEventsProfileName, _defaultSunEvents.ToString());
                driverProfile.WriteValue(driverID, windEventsProfileName, _defaultWindEvents.ToString());
                driverProfile.WriteValue(driverID, rainEventsProfileName, _defaultRainEvents.ToString());
                driverProfile.WriteValue(driverID, humidityEventsProfileName, _defaultHumidityEvents.ToString());
                driverProfile.WriteValue(driverID, cloudEventsProfileName, _defaultCloudEvents.ToString());
            }
        }
    }
}
