using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common.Properties;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using ASCOM.DriverAccess;

using System.IO;

namespace ASCOM.Wise40
{
    public class WiseSite : IDisposable
    {
        private static WiseSite _wisesite = new WiseSite();
        private static bool _initialized;
        private Astrometry.NOVAS.NOVAS31 novas31;
        private static AstroUtils astroutils;
        private static ASCOM.Utilities.Util ascomutils;
        public Astrometry.OnSurface onSurface;
        public Astrometry.Accuracy astrometricAccuracy;
        public Astrometry.RefractionOption refractionOption;
        public double siteLatitude, siteLongitude, siteElevation;
        public ObservingConditions och;
        public SafetyMonitor computerControl, safeToOperate;
        private DateTime lastOCFetch;
        private Debugger debugger = Debugger.Instance;
        private bool calculateRefraction;

        public enum OpMode { LCO, ACP, WISE, NONE };
        public OpMode _opMode = OpMode.WISE;

        //
        // From the VantagePro summary graphs for 2015
        //
        private static readonly double[] averageTemperatures = { 9.7, 10.7, 14.0, 15.0, 21.1, 21.3, 24.4, 25.9, 24.7, 20.8, 16.1, 10.1 };
        private static readonly double[] averagePressures = { 1021, 1012, 1017, 1013, 1008, 1008, 1006, 1007, 1008, 1013, 1015, 1022 };

        public static WiseSite Instance
        {
            get
            {
                return _wisesite;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            novas31 = new NOVAS31();
            astroutils = new AstroUtils();
            ascomutils = new Util();

            siteLatitude = ascomutils.DMSToDegrees("30:35:50.43");
            siteLongitude = ascomutils.DMSToDegrees("34:45:43.86");
            siteElevation = 882.9;
            novas31.MakeOnSurface(siteLatitude, siteLongitude, siteElevation, 0.0, 0.0, ref onSurface);

            //WriteOCHProfile();  // Prepare a Wise profile for the OCH
            try
            {
                och = new ObservingConditions("ASCOM.OCH.ObservingConditions");
                och.Connected = true;
                refractionOption = Astrometry.RefractionOption.LocationRefraction;
                lastOCFetch = DateTime.Now;
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Could not connect to OCH: {0}", ex.Message);
                #endregion
                och = null;
                refractionOption = Astrometry.RefractionOption.NoRefraction;
            }

            try
            {
                computerControl = new SafetyMonitor(Const.wiseComputerControlDriverID);
                computerControl.Connected = true;
            }
            catch
            {
                computerControl = null;
            }

            try
            {
                safeToOperate = new SafetyMonitor(Const.wiseSafeToOpenDriverID);
                safeToOperate.Connected = true;
            }
            catch
            {
                safeToOperate = null;
            }

            _initialized = true;
        }

        public void Dispose()
        {
            novas31.Dispose();
            astroutils.Dispose();
            ascomutils.Dispose();

            if (computerControl != null)
            {
                computerControl.Connected = false;
                computerControl.Dispose();
            }

            if (safeToOperate != null)
            {
                safeToOperate.Connected = false;
                safeToOperate.Dispose();
            }
        }

        public Angle Longitude
        {
            get
            {
                return Angle.FromHours(onSurface.Longitude / 15.0);
            }
        }

        public Angle Latitude
        {
            get
            {
                return Angle.FromDegrees(onSurface.Latitude, Angle.Type.Dec);
            }
        }

        public double Elevation
        {
            get
            {
                return onSurface.Height;
            }
        }

        public Angle LocalSiderealTime
        {
            get
            {
                double gstNow = 0;

                var res = novas31.SiderealTime(
                    astroutils.JulianDateUT1(0), 0d,
                    astroutils.DeltaT(),
                    GstType.GreenwichApparentSiderealTime,
                    Method.EquinoxBased,
                    astrometricAccuracy,
                    ref gstNow);

                if (res != 0)
                    throw new InvalidValueException("Error getting Greenwich Apparent Sidereal time");

                return Angle.FromHours(gstNow) + Longitude;
            }
        }

        /// <summary> 
        // If we haven't checked in a long enough time (10 minutes ?!?)
        //  get temperature and pressure.
        /// </summary>
        public void prepareRefractionData(bool calculateRefraction)
        {
            const int freqOCFetchMinutes = 10;

            if (!calculateRefraction)
            {
                refractionOption = RefractionOption.NoRefraction;
                return;
            }
            DateTime now = DateTime.Now;
            int month = now.Month - 1;

            onSurface.Temperature = averageTemperatures[month];
            onSurface.Pressure = averagePressures[month];

            if (och != null && DateTime.Now.Subtract(lastOCFetch).TotalMinutes > freqOCFetchMinutes)
            {
                try
                {
                    double timeSinceLastUpdate = och.TimeSinceLastUpdate("Temperature");

                    if (timeSinceLastUpdate > (freqOCFetchMinutes * 60))
                    {
                        onSurface.Temperature = och.Temperature;
                        onSurface.Pressure = och.Pressure;
                        refractionOption = RefractionOption.LocationRefraction;
                    }
                }
                catch { }
            }
        }
        
        public OpMode OperationalMode
        {
            get
            {
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    OpMode mode;

                    if (Enum.TryParse<OpMode>(driverProfile.GetValue(Const.wiseTelescopeDriverID, "SiteOperationMode", null, "WISE").ToUpper(), out mode))
                        _opMode = mode;
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OperationalMode:get => {0}", _opMode.ToString());
                #endregion
                return _opMode;
            }

            set
            {
                _opMode = value;
                using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
                {
                    driverProfile.WriteValue(Const.wiseTelescopeDriverID, "SiteOperationMode", _opMode.ToString());
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "OperationalMode:set {0}", _opMode.ToString());
                #endregion
            }
        }
    }

    public static class HumanIntervention
    {
        static DateTime _lastInfoRead = DateTime.MinValue;
        static string _info = null;

        static HumanIntervention() {}

        public static void Create(string oper, string reason)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Const.humanInterventionFilePath));
            using (StreamWriter sw = new StreamWriter(Const.humanInterventionFilePath))
            {
                sw.WriteLine("Operator: " + oper);
                sw.WriteLine("Reason: " + reason);
                sw.WriteLine("Created: " + DateTime.Now.ToString());
            }

            while (! File.Exists(Const.humanInterventionFilePath))
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        public static void Remove()
        {
            bool deleted = false;

            while (!deleted)
            {
                try
                {
                    File.Delete(Const.humanInterventionFilePath);
                    deleted = true;
                }
                catch (IOException) { }
            }
        }
        
        public static bool IsSet()
        {
            return System.IO.File.Exists(Const.humanInterventionFilePath);
        }

        public static string Info
        {
            get
            {
                if (!IsSet())
                    return string.Empty;

                if (File.GetLastWriteTime(Const.humanInterventionFilePath) > _lastInfoRead)
                {

                    StreamReader sr = new StreamReader(Const.humanInterventionFilePath);
                    string line, info = string.Empty;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("Operator:") || line.StartsWith("Created:") || line.StartsWith("Reason:"))
                            info += line + "; ";
                    }

                    info = "Human Intervention: " + ((info == string.Empty) ? string.Format("File \"{0}\" exists.",
                        Const.humanInterventionFilePath) : info);
                    _info = info;
                    _lastInfoRead = DateTime.Now;
                }
                return _info;
            }
        }
    }
}
