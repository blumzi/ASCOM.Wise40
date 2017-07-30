using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
//using ASCOM.Wise40.Hardware;
using ASCOM.Utilities;
using System.Collections;
using System.Globalization;

namespace ASCOM.Wise40.FilterWheel
{
    public class WiseFilterWheel : WiseObject, IDisposable
    {
        private static Version version = new Version(0, 2);
        private static readonly WiseFilterWheel _instance = new WiseFilterWheel();

        public TraceLogger traceLogger = new TraceLogger();
        public Debugger debugger = Debugger.Instance;        

        private static bool _initialized = false;
        private static bool _connected = false;

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = "ASCOM.Wise40.FilterWheel";
        private static string driverDescription = string.Format("ASCOM Wise40.FilterWheel v{0}", version.ToString());
        private ArduinoInterface arduino = ArduinoInterface.Instance;
        public static string port;

        public enum WheelType { Unknown, Wheel8, Wheel4};
        public enum FilterSize { twoInch = 2, threeInch = 3};

        public Wheel currentWheel;
        public static Wheel wheel8 = new Wheel(WheelType.Wheel8);
        public static Wheel wheel4 = new Wheel(WheelType.Wheel4);
        public static Wheel wheelUnknown = new Wheel(WheelType.Unknown);
        public static List<Wheel> knownWheels = new List<Wheel>() { wheel8, wheel4 };

        public static List<Filter>[] filterInventory;

        private string _savedFile = "c://Wise40/FilterWheel/Config.txt";

        public struct FWPosition
        {
            public string filterName;
            public string tag;

            public FWPosition(string n, string u)
            {
                filterName = n;
                tag = u;
            }
        };

        public class Wheel: WiseObject
        {
            public WheelType _type;
            public FWPosition[] _positions;
            public string _name;
            public short _position;
            public short _targetPosition;
            public int _nPositions;
            public int _filterSize;

            public Wheel(WheelType type)
            {
                _type = type;
                if (type == WheelType.Wheel4)
                {
                    _nPositions = 4;
                    _filterSize = 3;
                }
                else if (type == WheelType.Wheel8)
                {
                    _nPositions = 8;
                    _filterSize = 2;
                }
                _positions = new FWPosition[_nPositions];

                _nPositions = _positions.Length;
                _targetPosition = -1;
                _name = "Wheel" + _nPositions.ToString();

                for (int i = 0; i < _nPositions; i++)
                {
                    _positions[i].filterName = _positions[i].tag = string.Empty;
                }
                _position = -1;
            }

        }

        Wheel lookupWheel(string tag)
        {
            Wheel ret = wheelUnknown;

            foreach (var wheel in knownWheels)
            {
                for (short pos = 0; pos < wheel._nPositions; pos++)
                    if (tag == wheel._positions[pos].tag)
                    {
                        ret = wheel;
                        ret._position = pos;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "lookupWheel({0}) ==> wheel: {1}, position: {2}",
                            tag, ret._name, ret._position);
                        #endregion
                        return ret;
                    }
            }
            return ret;
        }

        public string DriverID
        {
            get
            {
                return driverID;
            }
        }

        public static WiseFilterWheel Instance
        {
            get
            {
                return _instance;
            }
        }

        void onCommunicationComplete(object sender, ArduinoInterface.CommunicationCompleteEventArgs e)
        {
            string tag = arduino.Tag;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("WiseFilterWheel.onCommunicationComplete: tag: \"{0}\"", tag));
            #endregion
            if (tag != null) {
                currentWheel = lookupWheel(tag);
                RaiseWheelOrPositionChanged();
            }
        }

        public void init()
        {
            if (_initialized)
                return;
            
            traceLogger.LogMessage("WiseFilterWheel", "Starting initialisation");
            Connected = false;
            ReadProfile();
            ReadFiltersFromCsvFile();
            RestoreCurrentFromFile();

            if (!Simulated)
            {
                arduino.init(WiseFilterWheel.port);

                arduino.communicationCompleteHandler += onCommunicationComplete;
                arduino.StartReadingTag();
            }
            Connected = true;

            traceLogger.LogMessage("WiseFilterWheel", "Completed initialisation");
            _initialized = true;
        }

        public static void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";
                string subKey;

                foreach (Wheel w in knownWheels)
                {
                    string name = "Wheel" + w._nPositions.ToString();
                    for (int pos = 0; pos < w._nPositions; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);
                        w._positions[pos].filterName = driverProfile.GetValue(driverID, "Filter Name", subKey, string.Empty);
                        w._positions[pos].tag = driverProfile.GetValue(driverID, "RFID", subKey, string.Empty);
                    }
                }
                port = _instance.Simulated ? "NoPort" : driverProfile.GetValue(driverID, "Port", string.Empty, string.Empty);
            }
        }

        private static Dictionary<FilterSize, string> filterCsvFiles = new Dictionary<FilterSize, string>() {
                {FilterSize.twoInch, "c:/Wise40/FilterWheel/" +"twoInchFilters.csv" },
                {FilterSize.threeInch, "c:/Wise40/FilterWheel/" +"threeInchFilters.csv" },
            };

        //
        // There are two filter inventory files (for two and three inch filters respectively)
        // These are ';' separated CSV files
        //
        void ReadFiltersFromCsvFile()
        {

            WiseFilterWheel.filterInventory = new List<Filter>[4];
            WiseFilterWheel.filterInventory[(int)FilterSize.twoInch] = new List<Filter>();
            WiseFilterWheel.filterInventory[(int)FilterSize.threeInch] = new List<Filter>();

            foreach (var filterSize in new List<FilterSize> { FilterSize.twoInch, FilterSize.threeInch })
            {
                string csvFile = filterCsvFiles[filterSize];
                if (!System.IO.File.Exists(csvFile))
                    continue;

                using (var sr = new System.IO.StreamReader(csvFile))
                {
                    string line;
                    string[] fields;
                    char[] sep = { ';' };
                    int offset;

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.TrimStart().TrimEnd();
                        if (line.StartsWith("#"))       // skip comments
                            continue;
                        fields = line.Split(';');
                        if (fields.Length != 3)         // skip bad lines
                            continue;

                        try
                        {
                            offset = Convert.ToInt32(fields[2]);
                        } catch (FormatException)
                        {
                            offset = 0;
                        }
                        WiseFilterWheel.filterInventory[(int)filterSize].Add(new Filter(fields[0], fields[1], offset));
                    }
                }
            }

            //foreach (var sk in driverProfile.SubKeys(driverID))
            //{
            //    KeyValuePair kv = sk as KeyValuePair;
            //    if (!(kv.Key.StartsWith("2inch") || kv.Key.StartsWith("3inch")))
            //        continue;

            //    string name = driverProfile.GetValue(driverID, "Name", kv.Key, string.Empty);
            //    string description = driverProfile.GetValue(driverID, "Description", kv.Key, string.Empty);
            //    int offset = Convert.ToInt32(driverProfile.GetValue(driverID, "Offset", kv.Key, "0"));

            //    if (kv.Key.StartsWith("2inch/Filter"))
            //    {
            //        WiseFilterWheel.filterInventory[2].Add(new Filter(name, description, offset));
            //    }
            //    else if (kv.Key.StartsWith("3inch/Filter"))
            //    {
            //        WiseFilterWheel.filterInventory[3].Add(new Filter(name, description, offset));
            //    }
            //}
        }

        public static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";
                string subKey;

                foreach (Wheel w in new List<Wheel> { wheel8, wheel4 })
                {
                    string name = "Wheel" + ((w._type == WheelType.Wheel4) ? "4" : "8");
                    for (int pos = 0; pos < w._nPositions; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);

                        driverProfile.WriteValue(driverID, "Filter Name", w._positions[pos].filterName, subKey);
                        driverProfile.WriteValue(driverID, "RFID", w._positions[pos].tag, subKey);
                    }
                }
                driverProfile.WriteValue(driverID, "Port", WiseFilterWheel.port);
            }
        }

        public bool Connected
        {
            get
            {
                if (Simulated)
                    return _connected;

                bool connected = arduino.Connected;

                traceLogger.LogMessage("Connected Get", connected.ToString());
                return connected;
            }

            set
            {
                traceLogger.LogMessage("Connected Set", value.ToString());

                if (Simulated)
                {
                    if (value == _connected)
                        return;
                    _connected = value;
                    currentWheel = wheel8;
                    return;
                }

                if (value == arduino.Connected)
                    return;
                arduino.Connected = value;
                if (value == true)
                    currentWheel = wheelUnknown;
            }
        }

        public string Description
        {
            get
            {
                traceLogger.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "First attempt. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                traceLogger.LogMessage("SupportedActions Get", "Returning empty arraylist");
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
            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (Connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        public new string Name
        {
            get
            {
                string name = string.Format("{0} ({1} slots)", currentWheel._name, currentWheel._nPositions);
                traceLogger.LogMessage("Name Get", name);
                return name;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                traceLogger.LogMessage("InterfaceVersion Get", "2");
                return Convert.ToInt16("2");
            }
        }

        public void Dispose()
        {

        }

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
            if (Connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (FilterWheelSetupDialogForm F = new FilterWheelSetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

#region IFilerWheel Implementation

        public int[] FocusOffsets
        {
            get
            {
                List<int> focusOffsets = new List<int>();

                foreach (FWPosition position in currentWheel._positions) // Write filter offsets to the log
                {
                    int offset = (position.filterName == string.Empty) ? 0 :
                                    filterInventory[currentWheel._filterSize].Find((x) => x.Name == position.filterName).Offset;
                    focusOffsets.Add(offset);
                }
                return focusOffsets.ToArray();
            }
        }

        public string[] Names
        {
            get
            {
                List<string> names = new List<string>();
                if (currentWheel._positions == null)
                    return names.ToArray();
                foreach (FWPosition position in currentWheel._positions)
                {
                    traceLogger.LogMessage("Names Get", position.filterName);
                    names.Add(position.filterName);
                }

                return names.ToArray();
            }
        }

        public short Position
        {
            get
            {
                if (!Connected)
                    throw new NotConnectedException("Not connected");
#if RFID_IS_WORKING                
                string tag = arduino.Tag;
                if (tag == null)            // the arduino is busy
                    return -1;
                currentWheel = lookupWheel(tag);
                ret = currentWheel.position;
#else
                //
                // Since the RFID is NOT working the reader will reply with "No tag in range"
                if (arduino.isActive)
                    return -1;          // the arduino is busy

                string status = arduino.Status;
                if (status.StartsWith("error:No tag in range"))
                {
                    // The wheel reached its target position and stopped.
                    currentWheel._position = currentWheel._targetPosition;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "GetCurrentPosition: {0}", currentWheel._position);
                    #endregion
                    return currentWheel._position;
                }
#endif
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "GetCurrentPosition: {0}", currentWheel._position);
                #endregion
                return currentWheel._position;
            }

            set
            {
                if (!Connected)
                    throw (new NotConnectedException("Not connected"));

                int nPositions = currentWheel._nPositions;
                short targetPosition = value;

                if (targetPosition == currentWheel._position)
                    return;

                traceLogger.LogMessage("Position Set", targetPosition.ToString());
                if ((targetPosition < 0) | (targetPosition > nPositions - 1))
                {
                    traceLogger.LogMessage("", "Throwing InvalidValueException - Position: " + targetPosition.ToString() + ", Range: 0 to " + (nPositions - 1).ToString());
                    throw new InvalidValueException("Position", targetPosition.ToString(), "0 to " + (nPositions - 1).ToString());
                }

                if (Simulated)
                {
                    currentWheel._position = targetPosition;
                    return;
                }

                int cw, ccw;    // # of positions to move
                if (targetPosition > currentWheel._position)
                {
                    cw = targetPosition - currentWheel._position;
                    ccw = currentWheel._position + nPositions - targetPosition;
                } else
                {
                    cw = currentWheel._position - targetPosition;
                    ccw = targetPosition + nPositions - currentWheel._position;
                }

                int slots = (cw < ccw) ? cw : ccw;
                ArduinoInterface.StepperDirection dir = (cw < ccw) ?
                    ArduinoInterface.StepperDirection.CW :
                    ArduinoInterface.StepperDirection.CCW;
                if (nPositions == 4)
                    slots *= 2;

                currentWheel._targetPosition = targetPosition;
                try
                {
                    arduino.StartMoving(dir, slots);
                } catch (Exception ex) {
#region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "SetCurrentPosition: communication exception {0}", ex.Message);
#endregion
                }
            }
        }
#endregion

        public int Positions
        {
            get
            {
                return currentWheel._nPositions;
            }
        }

        public string Status
        {
            get
            {
                string status =  Simulated ? "Idle" : arduino.Status;
#region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("WiseFilterWheel status: {0}", status));
#endregion
                return status;
            }
        }
        
        public event EventHandler wheelOrPositionChanged;

        public void RaiseWheelOrPositionChanged()
        {
            EventHandler handler = wheelOrPositionChanged;

            if (null != handler)
            {
                foreach (EventHandler singleCast in handler.GetInvocationList())
                {
                    ISynchronizeInvoke syncInvoke = singleCast.Target as ISynchronizeInvoke;
                    try
                    {
                        if ((null != syncInvoke) && (syncInvoke.InvokeRequired))
                            syncInvoke.Invoke(singleCast, new object[] { this, EventArgs.Empty });
                        else
                            singleCast(this, EventArgs.Empty);
                    }
                    catch
                    { }
                }
            }
        }

        public void SetCurrent(WiseFilterWheel.Wheel wheel, short position)
        {
            currentWheel = wheel;
            currentWheel._position = position;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFilterWheel:SetCurrent: wheel = {0}, position = {1}",
                wheel._name, position);
            #endregion
            SaveCurrentToFile();
        }

        private void SaveCurrentToFile()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_savedFile));
            using (var sw = new System.IO.StreamWriter(_savedFile)) {
                sw.WriteLine("#");
                sw.WriteLine("# This file contains the last 'known-as-good' Wise40 Filter Wheel setup.");
                sw.WriteLine("# Saved at: {0}", DateTime.Now.ToString());
                sw.WriteLine("#");
                sw.WriteLine(string.Format("Wheel: {0}", currentWheel._nPositions));
                sw.WriteLine(string.Format("Position: {0}", currentWheel._position));
            }
        }

        private void RestoreCurrentFromFile()
        {
            string wheel = string.Empty;
            string pos = string.Empty;
            string[] parts;
            char[] spaces = { ' ', '\t' };
            int w = -1;
            short p = -1;

            if (!System.IO.File.Exists(_savedFile))
                return;

            using (var sr = new System.IO.StreamReader(_savedFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    if (line.StartsWith("Wheel: ")) {
                        parts = line.Split(':');
                        wheel = parts[1].TrimStart(spaces);
                    } else if (line.StartsWith("Position:"))
                    {
                        parts = line.Split(':');
                        pos = parts[1].TrimStart(spaces);
                    }
                }
            }

            try
            {
                w = Convert.ToInt32(wheel);
            }
            catch (FormatException) { }

            try
            {
                p = Convert.ToInt16(pos);
            }
            catch (FormatException) { }

            if (((w == 4) || (w == 8) && (p >= 0 && p <= w)))
            {
                currentWheel = (w == 4) ? wheel4 : wheel8;
                currentWheel._position = p;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    "WiseFilterWheel:RestoreCurrentFromFile: wheel = {0}, position = {1}",
                    currentWheel._name, currentWheel._position);
                #endregion
            }
        }

        public static void SaveFiltersToCsvFile(int filterSize)
        {
            string fileName = filterCsvFiles[(FilterSize) filterSize];

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));

            using (var sw = new System.IO.StreamWriter(fileName))
            {
                sw.WriteLine("#");
                sw.WriteLine(string.Format("# Wise40 {0} inch filter inventory", (int)filterSize));
                sw.WriteLine(string.Format("# Saved at: {0}", DateTime.Now.ToLongDateString()));
                sw.WriteLine("#");

                foreach (var filter in filterInventory[(int)filterSize])
                {
                    sw.WriteLine(string.Format("{0};{1};{2}", filter.Name, filter.Description, filter.Offset.ToString()));
                }
            }
        }
    }
}
