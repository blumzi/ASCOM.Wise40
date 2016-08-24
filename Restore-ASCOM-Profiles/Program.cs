using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Utilities;

namespace Restore_ASCOM_Profiles
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteCloudSensorProfile();
            WriteVantageProProfile();
            WriteSafeToOpenProfile();
            WriteSafeToImageProfile();

            Environment.Exit(0);
        }

        internal static void WriteCloudSensorProfile()
        {
            string driverID = "ASCOM.CloudSensor.ObservingConditions";
            string dataFileProfileName = "Data File";
            string traceStateProfileName = "Trace Level";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, dataFileProfileName, "c:/temp/clarityII-data.txt");
            }
        }

        internal static void WriteVantageProProfile()
        {
            string driverID = "ASCOM.Vantage.ObservingConditions";
            string reportFileProfileName = "Report File";
            string traceStateProfileName = "Trace Level";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "ObservingConditions";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, reportFileProfileName, "c:/temp/Weather_Wise40_Vantage_Pro.htm");
            }
        }

        internal static void WriteSafeToOpenProfile()
        {
            string driverID = "ASCOM.Wise40.SafeToOpen.SafetyMonitor";
            string traceStateProfileName = "Trace Level";
            string cloudsMaxProfileName = "Clouds Max";
            string windMaxProfileName = "Wind Max";
            string rainMaxProfileName = "Rain Max";
            string lightMaxProfileName = "Light Max";
            string humidityMaxProfileName = "Humidity Max";
            string ageMaxSecondsProfileName = "Age Max";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, "cloudClear");
                driverProfile.WriteValue(driverID, windMaxProfileName, 100.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, 0.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, "darkLight");
                driverProfile.WriteValue(driverID, humidityMaxProfileName, 70.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, 0.ToString());
            }
        }

        internal static void WriteSafeToImageProfile()
        {
            string driverID = "ASCOM.Wise40.SafeToImage.SafetyMonitor";
            string traceStateProfileName = "Trace Level";
            string cloudsMaxProfileName = "Clouds Max";
            string windMaxProfileName = "Wind Max";
            string rainMaxProfileName = "Rain Max";
            string lightMaxProfileName = "Light Max";
            string humidityMaxProfileName = "Humidity Max";
            string ageMaxSecondsProfileName = "Age Max";

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, traceStateProfileName, false.ToString());
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, "cloudClear");
                driverProfile.WriteValue(driverID, windMaxProfileName, 100.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, 0.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, "darkLight");
                driverProfile.WriteValue(driverID, humidityMaxProfileName, 70.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, 0.ToString());
            }
        }
    }
}
