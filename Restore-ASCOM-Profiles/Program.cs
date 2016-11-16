using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Utilities;
using ASCOM.Wise40.Common;

namespace Restore_ASCOM_Profiles
{
    class Program
    {
        private static bool realMachine = Environment.MachineName.ToLower() == "dome-ctlr";

        static void Main(string[] args)
        {
            WriteCloudSensorProfile();
            WriteVantageProProfile();
            WriteSafeToOpenProfile();
            WriteSafeToImageProfile();
            WriteDomeProfile();
            WriteTelescopeProfile();

            Environment.Exit(0);
        }

        internal static void WriteCloudSensorProfile()
        {
            string driverID = "ASCOM.CloudSensor.ObservingConditions";
            string dataFileProfileName = "Data File";
            string dataFile = realMachine ? 
                "z:/clarityII-data.txt" :
                "c:/temp/clarityII-data.txt";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, dataFileProfileName, dataFile);
            }
        }

        internal static void WriteVantageProProfile()
        {
            string driverID = "ASCOM.Vantage.ObservingConditions";
            string reportFileProfileName = "Report File";
            string reportFile = realMachine ?
                "y:/Weather_Wise40_Vantage_Pro.htm" :
                "c:/temp/Weather_Wise40_Vantage_Pro.htm";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, reportFileProfileName, reportFile);
            }
        }

        internal static void WriteSafeToOpenProfile()
        {
            string driverID = "ASCOM.Wise40.SafeToOpen.SafetyMonitor";
            string cloudsMaxProfileName = "Clouds Max";
            string windMaxProfileName = "Wind Max";
            string rainMaxProfileName = "Rain Max";
            string lightMaxProfileName = "Light Max";
            string humidityMaxProfileName = "Humidity Max";
            string ageMaxSecondsProfileName = "Age Max";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, "cloudClear");
                driverProfile.WriteValue(driverID, windMaxProfileName, 100.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, 0.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, "dayLight");
                driverProfile.WriteValue(driverID, humidityMaxProfileName, 70.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, 0.ToString());
            }
        }

        internal static void WriteSafeToImageProfile()
        {
            string driverID = "ASCOM.Wise40.SafeToImage.SafetyMonitor";
            string cloudsMaxProfileName = "Clouds Max";
            string windMaxProfileName = "Wind Max";
            string rainMaxProfileName = "Rain Max";
            string lightMaxProfileName = "Light Max";
            string humidityMaxProfileName = "Humidity Max";
            string ageMaxSecondsProfileName = "Age Max";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, "cloudClear");
                driverProfile.WriteValue(driverID, windMaxProfileName, 100.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, 0.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, "dayLight");
                driverProfile.WriteValue(driverID, humidityMaxProfileName, 70.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, 0.ToString());
            }
        }

        internal static void WriteDomeProfile()
        {
            string driverID = "ASCOM.Wise40.Dome";
            string bypassSafetyProfileName = "AutoCalibrate";
            string autoCalibrateProfileName = "Bypass Safety";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, autoCalibrateProfileName, true.ToString());
                driverProfile.WriteValue(driverID, bypassSafetyProfileName, false.ToString());
            }
        }

        internal static void WriteTelescopeProfile()
        {
            string driverID = "ASCOM.Wise40.Telescope";
            string enslaveDomeProfileName = "Enslave Dome";
            string traceStateProfileName = "Trace Level";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, enslaveDomeProfileName, true.ToString());
            }
        }
    }
}
