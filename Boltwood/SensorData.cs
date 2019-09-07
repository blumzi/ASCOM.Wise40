using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Boltwood
{

    //    
    //    Excerpt from the ClarityII manual
    //            
    //        17.1.1 New Format
    //            This recommended format gives access to all of the data Cloud Sensor II can provide.The data is similar
    //            to the display fields in the Clarity II window.The format has been split across two lines to make it fit on
    //            this page:
    //
    //            Date       Time        T V SkyT  AmbT SenT Wind Hum DewPt Hea R W     Since Now() Day's c w r d C A
    //            2005-06-03 02:07:23.34 C K -28.5 18.7 22.5 45.3 75  10.3  30  0 00004 038506.08846      1 2 1 0 0 0
    //
    //            The header line is here just for illustration.It does not actually appear anywhere.
    //            The fields mean:
    //
    //
    //            Heading Col’s Meaning
    //
    //            Date        1-10    local date yyyy-mm-dd
    //            Time        12-22   local time hh:mm:ss.ss (24 hour clock)
    //            T           24      temperature units displayed and in this data, 'C' for Celsius or 'F' for Fahrenheit
    //            V           26      wind velocity units displayed and in this data, ‘K’ for km/hr or ‘M’ for mph or 'm' for m/s
    //            SkyT        28-33   sky-ambient temperature, 999. for saturated hot, -999. for saturated cold, or –998. for wet
    //            AmbT        35-40   ambient temperature
    //            SenT        41-47   sensor case temperature, 999. for saturated hot, -999. for saturated cold. Neither saturated condition 
    //                                 should ever occur.
    //            Wind        49-54   wind speed or:
    //                                  -1. if still heating up,
    //                                  -2. if wet,
    //                                  -3. if the A/D from the wind probe is bad (firmware<V56 only) ,
    //                                  -4. if the probe is not heating (a failure condition),
    //                                  -5. if the A/D from the wind probe is low (shorted, a failure condition) (firmware >=V56 only),
    //                                  -6. if the A/D from the wind probe is high (no probe plugged in or a failure) (firmware >=V56 only).
    //            Hum         56-58   relative humidity in %
    //            DewPt       60-65   dew point temperature
    //            Hea         67-69   heater setting in %
    //            R           71      rain flag, =0 for dry, =1 for rain in the last minute, =2 for rain right now
    //            W           73      wet flag, =0 for dry, =1 for wet in the last minute, =2 for wet right now
    //            Since       75-79   seconds since the last valid data
    //            Now() Day's 81-92   date/time given as the VB6 Now() function result (in days) when Clarity II last wrote this file
    //            c           94      cloud condition (see the Cloudcond enum in section 20)  Cloud Sensor II User’s Manual V0029 46
    //            w           96      wind condition(see the Windcond enum in section 20)
    //            r           98      rain condition(see the Raincond enum in section 20)
    //            d           100     daylight condition(see the Daycond enum in section 20)
    //            C           102     roof close, = 0 not requested, = 1 if roof close was requested on this cycle
    //            A           104     alert, = 0 when not alerting, = 1 when alerting
    //
    public class SensorData
    {
        public double age;
        public DateTime localTime;
        public enum TempUnits
        {
            tempCelsius = 0,
            tempFahrenheit = 1,
        }
        public TempUnits tempUnits;    // 'C' for Celsius, 'F' for Fahrenheit
        public enum WindUnits
        {
            windKmPerHour = 0,
            windMilesPerHour = 1,
            windMeterPerSecond = 2,
        }
        public WindUnits windUnits;
        public double skyAmbientTemp;  // 999 for saturated hot, -999 for saturated cold, -998 for wet
        public enum SpecialTempValue
        {
            specialTempSaturatedHot = 999,
            specialTempSaturatedLow = -999,
            specialTempWet = -998,
        }
        public double ambientTemp;  // tempUnits
        public double sensorTemp;   // tempUnits
        public double windSpeed;    // windUnits
        public enum SpecialWindSpeedValue
        {
            windHeatingUp = -1,
            windWet = -2,
            windBadProbe = -3,
            windProbeNotHeating = -4,
            windProbeShorted = -5,
            windProbeFailure = -6,
        }
        public int humidity;        // %
        public double dewPoint;     // degrees
        public int heaterSetting;   // %
        public enum WetFlagValue { wetDry = 0, wetLastMinute = 1, wetRightNow = 2 };
        public WetFlagValue rainFlag;
        public WetFlagValue wetFlag;
        public int secondsSinceLastValidData;
        public DateTime lastWriten;
        public enum CloudCondition
        {
            cloudUnknown = 0,
            cloudClear = 1,
            cloudCloudy = 2,
            cloudVeryCloudy = 3,
            cloudWet = 4,
        }
        public CloudCondition cloudCondition;
        private static Dictionary<CloudCondition, double> cloudCondition2CloudCover = new Dictionary<CloudCondition, double>() {
                    { CloudCondition.cloudUnknown, 0.0 },
                    { CloudCondition.cloudClear, 0.0 },
                    { CloudCondition.cloudCloudy, 50.0 },
                    { CloudCondition.cloudVeryCloudy, 90.0 },
                    { CloudCondition.cloudWet, 100.0 },
                };
        public enum WindCondition
        {
            windUnknow = 0,
            windwindCalm = 1,
            windWindy = 2,
            windVeryWindy = 3,
        }
        public WindCondition windCondition;
        public enum RainCondition
        {
            rainUnknown = 0,
            rainDry = 1,
            rainWet = 2,
            rainRain = 3,
        }
        public static Dictionary<RainCondition, int> intRainCondition = new Dictionary<RainCondition, int>()
        {
            {RainCondition.rainUnknown, 0 },
            {RainCondition.rainDry, 1 },
            {RainCondition.rainWet, 2 },
            {RainCondition.rainRain, 3 },
        };
        public RainCondition rainCondition;
        public enum DayCondition
        {
            dayUnknown = 0,
            dayDark = 1,
            dayLight = 2,
            dayVeryLight = 3,
        }
        public static Dictionary<DayCondition, int> intDayCondition = new Dictionary<DayCondition, int>()
        {
            {DayCondition.dayUnknown, 0 },
            {DayCondition.dayDark, 1 },
            {DayCondition.dayLight, 2 },
            {DayCondition.dayVeryLight, 3 },
        };
        public DayCondition dayCondition;
        public bool roofCloseRequested;
        public bool alerting;
        private ASCOM.Utilities.Util util = new Utilities.Util();

        public SensorData(string data, EnvironmentLogger env)
        {
            try
            {
                localTime = Convert.ToDateTime(data.Substring(0, 22));
                DateTime utcTime = localTime.ToUniversalTime();
                age = DateTime.Now.Subtract(localTime).TotalSeconds;

                switch (data.Substring(23, 1))
                {
                    case "C":
                        tempUnits = TempUnits.tempCelsius;
                        break;
                    case "F":
                        tempUnits = TempUnits.tempFahrenheit;
                        break;
                }
                skyAmbientTemp = Convert.ToDouble(data.Substring(27, 6));
                ambientTemp = Convert.ToDouble(data.Substring(34, 6));
                sensorTemp = Convert.ToDouble(data.Substring(40, 6));
                if (tempUnits != TempUnits.tempCelsius)
                {
                    skyAmbientTemp = util.ConvertUnits(skyAmbientTemp, Utilities.Units.degreesFarenheit, Utilities.Units.degreesCelsius);
                    ambientTemp = util.ConvertUnits(ambientTemp, Utilities.Units.degreesFarenheit, Utilities.Units.degreesCelsius);
                    sensorTemp = util.ConvertUnits(sensorTemp, Utilities.Units.degreesFarenheit, Utilities.Units.degreesCelsius);
                }
                windSpeed = Convert.ToDouble(data.Substring(48, 6));
                switch (data.Substring(25, 1))
                {
                    case "K":
                        windUnits = WindUnits.windKmPerHour;
                        break;
                    case "M":
                        windUnits = WindUnits.windMilesPerHour;
                        break;
                    case "m":
                        windUnits = WindUnits.windMeterPerSecond;
                        break;
                }
                humidity = Convert.ToInt32(data.Substring(55, 3));
                dewPoint = Convert.ToDouble(data.Substring(59, 6));
                heaterSetting = Convert.ToInt32(data.Substring(66, 3));
                rainFlag = (WetFlagValue)Convert.ToInt32(data.Substring(70, 1));
                wetFlag = (WetFlagValue)Convert.ToInt32(data.Substring(72, 1));
                secondsSinceLastValidData = Convert.ToInt32(data.Substring(74, 5));
                //lastWriten = Convert.ToDateTime(data.Substring(80, 12));
                cloudCondition = (CloudCondition)Convert.ToInt32(data.Substring(93, 1));
                windCondition = (WindCondition)Convert.ToInt32(data.Substring(95, 1));
                rainCondition = (RainCondition)Convert.ToInt32(data.Substring(97, 1));
                dayCondition = (DayCondition)Convert.ToInt32(data.Substring(99, 1));
                var x = Convert.ToInt32(data.Substring(101, 1));
                roofCloseRequested = (x == 1) ? true : false;

                switch (windUnits)
                {
                    case SensorData.WindUnits.windKmPerHour:
                        windSpeed = windSpeed * 1000 / 3600;
                        break;
                    case SensorData.WindUnits.windMilesPerHour:
                        windSpeed = util.ConvertUnits(windSpeed, Utilities.Units.milesPerHour, Utilities.Units.metresPerSecond);
                        break;
                    case SensorData.WindUnits.windMeterPerSecond:
                        break;
                }

                env.Log(new Dictionary<string, string>()
                {
                    ["SkyAmbientTemp"] = skyAmbientTemp.ToString(),
                    ["SensorTemp"] = sensorTemp.ToString(),
                    ["WindSpeed"] = (windSpeed * 3.6).ToString(),   // mps -> kmh
                    ["Humidity"] = humidity.ToString(),
                    ["DewPoint"] = dewPoint.ToString(),
                    ["CloudCover"] = cloudCondition2CloudCover[cloudCondition].ToString(),
                    ["RainRate"] = ((int)rainCondition).ToString(),
                }, utcTime);
            }
            catch (Exception e)
            {
                throw new InvalidValueException(string.Format("Could not parse sensor data, caught: {0}", e.Message));
            }
        }
    }
}
