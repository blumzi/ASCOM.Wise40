﻿using System;
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

namespace ASCOM.Wise40SafeToOpen
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

        public Profile _profile;

        public LightSensor lightSensor;
        public WindSensor windSensor;
        public CloudsSensor cloudsSensor;
        public RainSensor rainSensor;
        public HumiditySensor humiditySensor;
        public SunSensor sunSensor;
        public HumanInterventionSensor humanInterventionSensor;
        public List<Sensor> _sensors;
        
        internal static string ageMaxSecondsProfileName = "AgeMaxSeconds";
        internal static string stableAfterMinProfileName = "StableAfterMin";
        public int ageMaxSeconds;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected = false;

        private Wise40.Common.Debugger debugger = Wise40.Common.Debugger.Instance;
        private static TraceLogger tl = new TraceLogger("", "Wise40.SafeToOpen");

        public WiseBoltwood boltwood = WiseBoltwood.Instance;
        public WiseVantagePro vantagePro = WiseVantagePro.Instance;
        
        private static object syncObject = new object();

        private static volatile WiseSafeToOperate _instanceOpen;
        private static bool initialized = false;
        
        public TimeSpan _stabilizationPeriod;
        private int _defaultStabilizationPeriodMinutes = 15;

        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public double siteLatitude, siteLongitude, siteElevation;
        public Astrometry.OnSurface onSurface;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption = Astrometry.RefractionOption.NoRefraction;
        Object3 Sun = new Object3();

        object reasonsLock = new object();
        List<string> unsafeReasons = new List<string>();

        public static WiseSafeToOperate InstanceOpen
        {
            get
            {
                if (_instanceOpen == null)
                {
                    lock(syncObject)
                    {
                        if (_instanceOpen == null)
                            _instanceOpen = new WiseSafeToOperate(Type.Open);
                    }
                }
                _instanceOpen.init();
                return _instanceOpen;
            }
        }

        public WiseSafeToOperate(Type type)
        {
            _type = type;
            tl = new TraceLogger("", "Wise40.SafeToOpen");
        }

        public void init()
        {
            //string type = _type == Type.Open ? "Open" : "Image";

            if (initialized)
                return;

            name = "Wise40 SafeToOpen";
            driverID = Const.wiseSafeToOpenDriverID;
            driverDescription = string.Format("ASCOM Wise40.SafeToOpen v{0}", version.ToString());

            if (_profile == null)
            {
                _profile = new Profile() { DeviceType = "SafetyMonitor" };
            }

            lightSensor = new LightSensor(this);
            humiditySensor = new HumiditySensor(this);
            windSensor = new WindSensor(this);
            sunSensor = new SunSensor(this);
            cloudsSensor = new CloudsSensor(this);
            rainSensor = new RainSensor(this);
            humanInterventionSensor = new HumanInterventionSensor(this);
            _sensors = new List<Sensor>() {
                windSensor,
                cloudsSensor,
                rainSensor,
                lightSensor,
                humiditySensor,
                sunSensor,
                humanInterventionSensor };
            
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false;

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

            ReadProfile(); // Read device configuration from the ASCOM Profile store
            initialized = true;

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

            using (SafeToOperateSetupDialogForm F = new SafeToOperateSetupDialogForm())
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
            else
                return stringSafetyCommand(command);
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
                _connected = boltwood.Connected == true && vantagePro.Connected == true;

                if (_connected)
                    startSensors();
                else
                    stopSensors();
            }
        }

        public void stopSensors()
        {
            foreach (Sensor s in _sensors)
                s.Stop();
        }

        public void startSensors()
        {
            foreach (Sensor s in _sensors)
                s.Start();
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
                //tl.LogMessage("Description Get", driverDescription);
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
                lock (reasonsLock)
                {
                    unsafeReasons.Clear();
                    if (!humanInterventionSensor.isSafe)
                    {
                        AddReason(humanInterventionSensor.reason());
                    }
                    else
                    {
                        bool dummy;

                        dummy = _boltwoodIsValid;
                        dummy = _vantageProIsValid;
                        string reason;
                        foreach (Sensor s in _sensors)
                            if (!s.isSafe && (reason = s.reason()) != string.Empty)
                                AddReason(reason);
                    }
                }
                return unsafeReasons;
            }
        }

        void AddReason(string reason)
        {
            if (unsafeReasons != null)
                unsafeReasons.Add(reason);
        }

        #region Individual Property Implementations
        #region Boolean Properties (for ASCOM)
        public bool _boltwoodIsValid
        {
            get
            {
                if (boltwood == null)
                {
                    AddReason("No connection to the Boltwood station");
                    return false;
                }
                double timeSinceLastUpdate = boltwood.TimeSinceLastUpdate("");
                if (ageMaxSeconds > 0 &&  timeSinceLastUpdate > ageMaxSeconds)
                {
                    AddReason(string.Format("Boltwood data is too old ({0:g} > {1}sec)",
                        TimeSpan.FromSeconds((int)timeSinceLastUpdate).ToString(), ageMaxSeconds));
                    return false;
                }
                return true;
            }
        }

        public bool _vantageProIsValid
        {
            get
            {
                if (vantagePro == null)
                {
                    AddReason("No connection to the VantagePro station");
                    return false;
                }

                double timeSinceLastUpdate = vantagePro.TimeSinceLastUpdate("");
                if (ageMaxSeconds > 0 && timeSinceLastUpdate > ageMaxSeconds)
                {
                    AddReason(string.Format("VantagePro data is too old ({0:g} > {1}sec)",
                        TimeSpan.FromSeconds((int)timeSinceLastUpdate).ToString(), ageMaxSeconds));
                    return false;
                }
                return true;
            }
        }

        private string stringSafetyCommand(string command)
        {
            Const.TriStateStatus status = Const.TriStateStatus.Good;
            string ret = "unknown";

            lock (reasonsLock)
            {
                unsafeReasons.Clear();
                switch (command.ToLower())
                {
                    case "light": status = isSafeLight; break;
                    case "humidity": status = isSafeHumidity; break;
                    case "wind": status = isSafeWindSpeed; break;
                    case "sun": status = isSafeSunElevation; break;
                    case "clouds": status = isSafeCloudCover; break;
                    case "rain": status = isSafeRain; break;
                    default:
                        status = Const.TriStateStatus.Error;
                        unsafeReasons.Add(string.Format("invalid command \"{0}\"", command));
                        break;
                }
            }

            switch (status)
            {
                case Const.TriStateStatus.Normal:
                case Const.TriStateStatus.Good:
                    return "ok";
                case Const.TriStateStatus.Error:
                    return "error: " + unsafeReasons[0];
                case Const.TriStateStatus.Warning:
                    return "warning: " + unsafeReasons[0];
            }

            return ret;
        }

        #endregion

        #region TriState Properties (for object)
        public Const.TriStateStatus isSafeCloudCover
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    cloudsSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeLight
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    lightSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    windSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    humiditySensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    rainSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeSunElevation
        {
            get
            {
                return sunSensor.isSafe ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
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
                bool ret = true;

                if (!_connected)
                    ret = false;
                else if (!humanInterventionSensor.isSafe)
                    ret = false;
                else
                {
                    if (!_boltwoodIsValid || !_vantageProIsValid)
                        return false;

                    foreach (Sensor s in _sensors)
                    {
                        if (s.nReadings < s._repeats)
                        {
                            AddReason(string.Format("{0} - not enough readings ({1} < {2})", s.Name, s.nReadings, s._repeats));
                        }
                        if (!s.isSafe)
                        {
                            ret = false;
                            break;
                        }
                    }
                }

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
            ageMaxSeconds = Convert.ToInt32(_profile.GetValue(driverID, ageMaxSecondsProfileName, string.Empty, 180.ToString()));

            int minutes = Convert.ToInt32(_profile.GetValue(driverID, stableAfterMinProfileName, string.Empty, _defaultStabilizationPeriodMinutes.ToString()));
            _stabilizationPeriod = new TimeSpan(0, minutes, 0);

            foreach (Sensor s in _sensors)
                s.readProfile();

            using (Profile driverProfile = new Profile())
            {
                string telescopeDriverId = Const.wiseTelescopeDriverID;
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
            _profile.WriteValue(driverID, ageMaxSecondsProfileName, ageMaxSeconds.ToString());
            _profile.WriteValue(driverID, stableAfterMinProfileName, _stabilizationPeriod.Minutes.ToString());
            foreach (Sensor s in _sensors)
                s.writeProfile();
        }
        #endregion
    }
}
