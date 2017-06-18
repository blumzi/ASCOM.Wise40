using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ASCOM;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.VantagePro;

namespace ASCOM.Wise40.SafeToOperate
{
    public class WiseSafeToOperate
    {
        private static Version version = new Version(0, 2);
        public enum Type { Open, Image };
        private Type _type;

        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        public string driverID;
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public string driverDescription;
        private string name;

        internal static string cloudsMaxProfileName = "Clouds Max";
        internal static string windMaxProfileName = "Wind Max";
        internal static string rainMaxProfileName = "Rain Max";
        internal static string lightMaxProfileName = "Light Max";
        internal static string humidityMaxProfileName = "Humidity Max";
        internal static string ageMaxSecondsProfileName = "Age Max";
        internal static string sunMaxProfileName = "Sun Elevation Max";

        public Boltwood.SensorData.CloudCondition cloudsMaxEnum;
        public Boltwood.SensorData.DayCondition lightMaxEnum;

        public double cloudsMaxValue;
        public double windMax;
        public double rainMax;
        public int lightMaxValue;
        public double humidityMax;
        public int ageMaxSeconds;
        public double sunElevationMax;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected = false;

        private Wise40.Common.Debugger debugger = Wise40.Common.Debugger.Instance;
        private static TraceLogger tl;

        WiseBoltwood boltwood = WiseBoltwood.Instance;
        WiseVantagePro vantagePro = WiseVantagePro.Instance;

        private static WiseSafeToOperate _instanceOpen = new WiseSafeToOperate(Type.Open);
        private static WiseSafeToOperate _instanceImage = new WiseSafeToOperate(Type.Image);

        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public double siteLatitude, siteLongitude, siteElevation;
        public Astrometry.OnSurface onSurface;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption = Astrometry.RefractionOption.NoRefraction;
        Object3 Sun = new Object3();

        List<string> unsafeReasons = null;

        public static WiseSafeToOperate InstanceOpen
        {
            get
            {
                _instanceOpen.init();
                return _instanceOpen;
            }
        }

        public static WiseSafeToOperate InstanceImage
        {
            get
            {
                _instanceImage.init();
                return _instanceImage;
            }
        }

        public WiseSafeToOperate(Type type)
        {
            _type = type;
        }

        public void init()
        {
            string type = _type == Type.Open ? "Open" : "Image";

            name = "Wise40 SafeTo" + type;
            driverID = "ASCOM.Wise40.SafeTo" + type + ".SafetyMonitor";
            driverDescription = string.Format("ASCOM Wise40.SafeTo{0} v{1}", type, version.ToString());
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Wise40.SafeTo" + type);
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false; // Initialise connected to false

            novas31 = new NOVAS31();
            astroutils = new AstroUtils();
            ascomutils = new Util();

            siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
            siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
            siteElevation = 882.9;
            novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);
            novas31.MakeObject(0, Convert.ToInt16(Body.Sun), "Sun", new CatEntry3(), ref Sun);

            try
            {
                boltwood.init();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Could not init boltwood: {0}", ex.Message));
            }

            try
            {
                vantagePro.init();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Could not init vantagePro: {0}", ex.Message));
            }

            tl.LogMessage("SafetyMonitor", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE ISafetyMonitor IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (_connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            if (command.ToLower() == "unsafereasons")
            {
                bool dummy = IsSafe;
                return string.Join(", ", UnsafeReasons);
            }

            return string.Empty;
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                if (boltwood != null)
                    boltwood.Connected = value;
                if (vantagePro != null)
                    vantagePro.Connected = value;
                _connected = value;
            }
        }

        public string DriverId
        {
            get
            {
                tl.LogMessage("DriverId Get", driverID);
                return driverID;
            }
        }

        public string Description
        {
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "Implements Wise40 weather max. values, wraps Boltwood CloudSensorII and Davis VantagePro. Version: " + DriverVersion;
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public static string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                tl.LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        public List<string> UnsafeReasons
        {
            get
            {
                List<string> ret;
                unsafeReasons = new List<string>();
                bool dummy;

                dummy = _boltwoodIsValid;
                dummy = IsSafeCloudCover;
                dummy = IsSafeLight;
                dummy = _vantageProIsValid;
                dummy = IsSafeWindSpeed;
                dummy = IsSafeHumidity;
                dummy = IsSafeRain;
                dummy = IsSafeSunElevation;

                ret = unsafeReasons;
                unsafeReasons = null;
                return ret;
            }
        }

        void AddReason(string reason)
        {
            if (unsafeReasons != null)
                unsafeReasons.Add(reason);
        }

        #region Individual Property Implementations
        #region Boolean Properties (for ASCOM)
        private bool _boltwoodIsValid
        {
            get
            {
                if (boltwood == null)
                {
                    AddReason("No connection to the Boltwood station");
                    return false;
                }
                if (ageMaxSeconds > 0 && boltwood.TimeSinceLastUpdate("") > ageMaxSeconds)
                {
                    AddReason(string.Format("Boltwood data is too old (age > {0})", ageMaxSeconds));
                    return false;
                }
                return true;
            }
        }

        private bool _vantageProIsValid
        {
            get
            {
                if (vantagePro == null)
                {
                    AddReason("No connection to the VantagePro station");
                    return false;
                }
                if (ageMaxSeconds > 0 && vantagePro.TimeSinceLastUpdate("") > ageMaxSeconds)
                {
                    AddReason(string.Format("Data from the VantagePro station is too old (age > {0})", ageMaxSeconds));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeLight
        {
            get
            {
                Dictionary<string, int> dayConditions = new Dictionary<string, int>
                {
                    {"dayUnknown", 0 },
                    {"dayDark", 1 },
                    {"dayLight", 2 },
                    {"dayVeryLight", 3 },
                };

                Dictionary<int, string> reversedDayConditions = new Dictionary<int, string>
                {
                    {0, "dayUnknown"},
                    {1, "dayDark"},
                    {2, "dayLight"},
                    {3, "dayVeryLight"},
                };

                int light = dayConditions[boltwood.CommandString("daylight", true)];
                if (light == 0)
                {
                    AddReason("Unknown day condition from Boltwood station");
                    return false;
                }
                if (light > lightMaxValue)
                {
                    AddReason(string.Format("Light condition from Boltwood ({0}) greater than max value ({1})",
                        reversedDayConditions[light], reversedDayConditions[lightMaxValue]));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeCloudCover
        {
            get
            {
                if (boltwood.CloudCover > cloudsMaxValue)
                {
                    AddReason(string.Format("Cloud cover from Boltwood ({0}) is greater than max value ({1})", boltwood.CloudCover, cloudsMaxValue));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeWindSpeed
        {
            get
            {
                if (vantagePro.WindSpeed > windMax)
                {
                    AddReason(string.Format("Wind speed from VantagePro ({0}) is greater than max value ({1})", vantagePro.WindSpeed, windMax));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeHumidity
        {
            get
            {
                if (vantagePro.Humidity > humidityMax)
                {
                    AddReason(string.Format("Humidity from VantagePro ({0}) is greater than max value ({1})", vantagePro.Humidity, humidityMax));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeRain
        {
            get
            {
                if (vantagePro.RainRate > rainMax)
                {
                    AddReason(string.Format("Rain rate from VantagePro ({0}) is greater than max value ({1})", vantagePro.RainRate, rainMax));
                    return false;
                }
                return true;
            }
        }

        private bool IsSafeSunElevation
        {
            get
            {
                var elev = SunElevation;
                if (elev > sunElevationMax)
                {
                    AddReason(string.Format("Sun elevation ({0:f1}°) is greater than max value ({1:f1}°)", Math.Floor(elev), sunElevationMax));
                    return false;
                }
                return true;
            }
        }
        #endregion

        #region TriState Properties (for object)
        public Const.TriStateStatus isSafeCloudCover
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    IsSafeCloudCover ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeLight
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    IsSafeLight ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeWindSpeed ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeHumidity ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeRain ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }
        #endregion
        #endregion

        public double SunElevation
        {
            get
            {
                double ra = 0, dec = 0, dis = 0;
                double jdt = astroutils.JulianDateUT1(0);
                short res;

                res = novas31.LocalPlanet(
                    astroutils.JulianDateUT1(0),
                    Sun,
                    astroutils.DeltaT(),
                    onSurface,
                    astrometricAccuracy,
                    ref ra, ref dec, ref dis);

                if (res != 0)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Failed to get LocalPlanet for the Sun (res: {0})", res);
                    return 0.0;
                }

                double rar = 0, decr = 0, zd = 0, az = 0;
                novas31.Equ2Hor(jdt, 0,
                    astrometricAccuracy,
                    0, 0,
                    onSurface,
                    ra, dec,
                    refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                if (res != 0)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Failed to convert equ2hor (res: {0})", res);
                    return 0.0;
                }
                double elev = 90.0 - zd;

                return elev;
            }
        }

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                bool ret;

                if (!_connected)
                    ret = false;
                else
                    ret =
                        _boltwoodIsValid &&
                        _vantageProIsValid &&
                        IsSafeLight &&
                        IsSafeCloudCover &&
                        IsSafeWindSpeed &&
                        IsSafeHumidity &&
                        IsSafeRain &&
                        IsSafeSunElevation;

                tl.LogMessage("IsSafe Get", ret.ToString());
                return ret;
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";

                cloudsMaxEnum = (Boltwood.SensorData.CloudCondition)
                    Enum.Parse(typeof(Boltwood.SensorData.CloudCondition),
                        driverProfile.GetValue(driverID, cloudsMaxProfileName, string.Empty, Boltwood.SensorData.CloudCondition.cloudClear.ToString()));
                cloudsMaxValue = Boltwood.SensorData.doubleCloudCondition[cloudsMaxEnum];

                windMax = Convert.ToDouble(driverProfile.GetValue(driverID, windMaxProfileName, string.Empty, 0.0.ToString()));
                rainMax = Convert.ToDouble(driverProfile.GetValue(driverID, rainMaxProfileName, string.Empty, 0.0.ToString()));
                humidityMax = Convert.ToDouble(driverProfile.GetValue(driverID, humidityMaxProfileName, string.Empty, 0.0.ToString()));
                ageMaxSeconds = Convert.ToInt32(driverProfile.GetValue(driverID, ageMaxSecondsProfileName, string.Empty, 0.ToString()));
                sunElevationMax = Convert.ToDouble(driverProfile.GetValue(driverID, sunMaxProfileName, string.Empty, 0.0.ToString()));

                lightMaxEnum = (Boltwood.SensorData.DayCondition)
                    Enum.Parse(typeof(Boltwood.SensorData.DayCondition),
                        driverProfile.GetValue(driverID, lightMaxProfileName, string.Empty, "dayUnknown"));
                lightMaxValue = (int)lightMaxEnum;
            }

            using (Profile driverProfile = new Profile())
            {
                string telescopeDriverId = "ASCOM.Wise40.Telescope";
                string astrometricAccuracyProfileName = "Astrometric accuracy";

                driverProfile.DeviceType = "Telescope";
                astrometricAccuracy =
                    driverProfile.GetValue(telescopeDriverId, astrometricAccuracyProfileName, string.Empty, "Full") == "Full" ?
                        Accuracy.Full :
                        Accuracy.Reduced;
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, cloudsMaxEnum.ToString());
                driverProfile.WriteValue(driverID, windMaxProfileName, windMax.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, rainMax.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, lightMaxEnum.ToString());
                driverProfile.WriteValue(driverID, humidityMaxProfileName, humidityMax.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, ageMaxSeconds.ToString());
                driverProfile.WriteValue(driverID, sunMaxProfileName, sunElevationMax.ToString());
            }
        }
        #endregion
    }
}
