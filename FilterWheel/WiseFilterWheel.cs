using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using System.Collections;
using System.Globalization;

using Newtonsoft.Json;

namespace ASCOM.Wise40 //.FilterWheel
{
    public class WiseFilterWheel : WiseObject, IDisposable
    {
        private static Version version = new Version(0, 2);

        public Debugger debugger = Debugger.Instance;        

        private static bool _initialized = false;
        private static bool _connected = false;
        private static bool _enabled = false;

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = Const.wiseFilterWheelDriverID;
        private static string driverDescription = string.Format("{0} v{1}", driverID, version.ToString());
        private ArduinoInterface arduino = ArduinoInterface.Instance;
        public static string port;

        public enum WheelType { None, Wheel8, Wheel4};
        public enum FilterSize { TwoInch, ThreeInch};
        public static List<FilterSize> filterSizes = new List<FilterSize> { FilterSize.TwoInch, FilterSize.ThreeInch };

        public Wheel currentWheel;
        public static Wheel wheel8 = new Wheel(WheelType.Wheel8);
        public static Wheel wheel4 = new Wheel(WheelType.Wheel4);
        public static Wheel wheelNone = new Wheel(WheelType.None);
        public static List<Wheel> wheels = new List<Wheel>() { wheel8, wheel4 };

        public static List<Filter>[] _filterInventory;

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
            public FilterSize _filterSize;

            public Wheel(WheelType type)
            {
                _type = type;
                if (type == WheelType.Wheel4)
                {
                    _nPositions = 4;
                    _filterSize = FilterSize.ThreeInch;
                }
                else if (type == WheelType.Wheel8)
                {
                    _nPositions = 8;
                    _filterSize = FilterSize.TwoInch;
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

            public class PositionDigest
            {
                public string FilterName;
                public string RFIDTag;
            }

            public class WheelDigest
            {
                public WheelType Type;
                public string Name;
                public int Npositions;
                public List<PositionDigest> Positions;
                public short CurrentPosition;
                public FilterSize FilterSize;
            }

            public List<PositionDigest> Positions
            {
                get
                {
                    List<PositionDigest> positions = new List<PositionDigest>();

                    for (int i = 0; i < _positions.Length; i++)
                        positions.Add(new PositionDigest
                        {
                            FilterName = _positions[i].filterName,
                            RFIDTag = _positions[i].tag,
                        });
                    return positions;
                }
            }

            public WheelDigest Digest
            {
                get
                {
                    return new WheelDigest
                    {
                        Type = _type,
                        Name = _name,
                        Npositions = _nPositions,
                        Positions = Positions,
                        CurrentPosition = _position,
                        FilterSize = _filterSize,
                    };
                }
            }
        }

        Wheel lookupWheel(string tag)
        {
            Wheel ret = wheelNone;

            foreach (var wheel in wheels)
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

        public static string DriverID
        {
            get
            {
                return Const.wiseFilterWheelDriverID;
            }
        }

        void onCommunicationComplete(object sender, ArduinoInterface.CommunicationCompleteEventArgs e)
        {
            string tag = arduino.Tag;
            string stat = arduino.Status;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, string.Format("WiseFilterWheel.onCommunicationComplete: tag: \"{0}\", status: {1}",
                tag, stat));
            #endregion
            if (stat == "No tag in range")
            {
                currentWheel._position = currentWheel._targetPosition;
                RaiseWheelOrPositionChanged();
            }
            if (tag != null) {
                currentWheel = lookupWheel(tag);
                RaiseWheelOrPositionChanged();
            }
        }

        private static readonly Lazy<WiseFilterWheel> lazy = new Lazy<WiseFilterWheel>(() => new WiseFilterWheel()); // Singleton

        public static WiseFilterWheel Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public void init()
        {
            if (_initialized)
                return;
            
            Connected = false;
            ReadProfile();
            ReadFiltersFromCsvFile();
            RestoreCurrentWheelFromFile();

            if (!Simulated)
            {
                arduino.init(WiseFilterWheel.port);

                arduino.communicationCompleteHandler += onCommunicationComplete;
                arduino.StartReadingTag();
            }
            Connected = true;

            _initialized = true;
        }

        private static String[] defaultWheel8RFIDs = {
            "7F0007F75E",
            "7F000817F7",
            "7F000AEFC5",
            "7C00563E5A",
            "7F001B2B73",
            "7F000ACAD5",
            "7F001B4A83",
            "7F0007BC0E",
        };

        private static String[] defaultWheel4RFIDs = {
            "7F001B4C16",
            "7C0055F4EB",
            "7F0007F75E",
            "7F001B0573",
        };

        public static void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "FilterWheel" })
            {
                string subKey;

                foreach (Wheel w in wheels)
                {
                    string wheelName = "Wheel" + w._nPositions.ToString();
                    for (int pos = 0; pos < w._nPositions; pos++)
                    {
                        string defaultRFID = (w == wheel8) ? defaultWheel8RFIDs[pos] : defaultWheel4RFIDs[pos];

                        subKey = string.Format("{0}/Position{1}", wheelName, pos + 1);
                        w._positions[pos].filterName = driverProfile.GetValue(driverID, "Filter Name", subKey, string.Empty);
                        w._positions[pos].tag = driverProfile.GetValue(driverID, "RFID", subKey, defaultRFID);
                    }
                }
           
                port = driverProfile.GetValue(driverID, "Port", string.Empty, Simulated ? "COM5" : "COM6");
                Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, "Enabled", string.Empty, "false"));
            }
        }

        private static Dictionary<FilterSize, string> filterCsvFiles = new Dictionary<FilterSize, string>() {
                {FilterSize.TwoInch, Const.topWise40Directory + "/FilterWheel/twoInchFilters.csv" },
                {FilterSize.ThreeInch, Const.topWise40Directory + "/FilterWheel/threeInchFilters.csv" },
            };

        public static Dictionary<FilterSize, int> filterSizeToIndex = new Dictionary<FilterSize, int>
        {
            {FilterSize.TwoInch, 0 },
            {FilterSize.ThreeInch, 1 },
        };

        //
        // There are two filter inventory files (for two and three inch filters respectively)
        // These are ';' separated CSV files
        //
        void ReadFiltersFromCsvFile()
        {

            WiseFilterWheel._filterInventory = new List<Filter>[filterSizes.Count()];
            WiseFilterWheel._filterInventory[filterSizeToIndex[FilterSize.TwoInch]] = new List<Filter>();
            WiseFilterWheel._filterInventory[filterSizeToIndex[FilterSize.ThreeInch]] = new List<Filter>();

            foreach (var filterSize in filterSizes)
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
                        WiseFilterWheel._filterInventory[(int)filterSize].Add(new Filter(fields[0], fields[1], offset));
                    }
                }
            }
        }

        public static void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "FilterWheel" })
            {
                string subKey;

                driverProfile.WriteValue(driverID, "Enabled", WiseFilterWheel.Enabled.ToString());
                foreach (Wheel w in wheels)
                {
                    string name = "Wheel" + w._name.ToString();
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

                return connected;
            }

            set
            {
                if (Simulated)
                {
                    if (value == _connected)
                        return;
                    _connected = value;
                    currentWheel = wheel8;
                    return;
                }

                if (!Enabled)
                    return;

                if (value == arduino.Connected)
                    return;
                arduino.Connected = value;
                if (value == true)
                    currentWheel = wheelNone;

                ActivityMonitor.Instance.Event(new Event.GlobalEvent(
                    string.Format("{0} {1}", driverID, value ? "Connected" : "Disconnected")));
            }
        }

        public static bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                if (value == _enabled)
                    return;

                if (value == true)
                {
                    Instance.Connected = true;
                    if (Instance.Connected)
                        _enabled = true;
                } else
                {
                    Instance.Connected = false;
                    _enabled = false;
                }
            }
        }

        public static string Description
        {
            get
            {
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                return "First attempt. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public string DriverVersion
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                return new ArrayList();
            }
        }

        public string Action(string action, string parameters)
        {
            switch (action.ToLower())
            {
                case "enabled":
                    if (parameters == "")
                        return Enabled.ToString();
                    else
                    {
                        Enabled = Convert.ToBoolean(parameters);
                        return "ok";
                    }

                case "status":
                    return JsonConvert.SerializeObject(new FilterWheelDigest
                    {
                        Enabled = Enabled,
                        OperationalState = State,
                        Position = Enabled ? Position : (short)-1,
                        Status = Status,
                    });

                case "current-wheel":
                    if (parameters == "")
                        return JsonConvert.SerializeObject(currentWheel.Digest);
                    else
                    {
                        CurrentWheel = JsonConvert.DeserializeObject<Wheel.WheelDigest>(parameters);
                        return "ok";
                    }

                case "get-filter-inventory":
                        return JsonConvert.SerializeObject(Filters);

                case "set-filter-inventory":
                    SetFilterInventory(JsonConvert.DeserializeObject<SetFilterInventoryParam>(parameters));
                    return "ok";

                default:
                    throw new ASCOM.ActionNotImplementedException("Action " + action + " is not implemented by this driver");
            }
        }

        public class FilterDigest
        {
            public string Name;
            public string Description;
            public int Offset;
        }

        public class WheelFilterDigest
        {
            public string Wheel;
            public string FilterSize;
            public List<FilterDigest> Filters;

            public WheelFilterDigest(FilterSize size)
            {
                Wheel = size == WiseFilterWheel.FilterSize.TwoInch ? "Wheel8" : "Wheel4";
                FilterSize = size.ToString();
                Filters = new List<FilterDigest>();

                List<Filter> inventory = _filterInventory[filterSizeToIndex[size]];

                foreach (var filter in inventory)
                    Filters.Add(new FilterDigest
                    {
                        Name = filter.FilterName,
                        Description = filter.FilterDescription,
                        Offset = filter.FilterOffset,
                    });
            }
        }

        public class FiltersInventoryDigest
        {
            public WheelFilterDigest[] FilterInventory = new WheelFilterDigest[2];

            public FiltersInventoryDigest()
            {
                FilterInventory = new WheelFilterDigest[WiseFilterWheel.filterSizes.Count];

                foreach (var filterSize in WiseFilterWheel.filterSizes)
                {
                    FilterInventory[filterSizeToIndex[filterSize]] = new WheelFilterDigest(filterSize);
                }
            }
        }

        public FiltersInventoryDigest Filters
        {
            get
            {
               return new FiltersInventoryDigest();
            }
        }

        public class SetFilterInventoryParam
        {
            public FilterSize FilterSize;
            public Filter[] Filters;
        }

        public void SetFilterInventory(SetFilterInventoryParam par)
        {
            int index = filterSizeToIndex[par.FilterSize];
            for (var i = 0; i < par.Filters.Length; i++)
                _filterInventory[index][i] = par.Filters[i];

            SaveFiltersInventoryToCsvFile(par.FilterSize);
        }

        public string State
        {
            get
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.LCO)
                    return "not available in LCO mode";
                if (!Enabled)
                    return "disabled by Setup";
                return "operational";
            }
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

        public string Name
        {
            get
            {
                return Const.wiseFilterWheelDriverID;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
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
                    int inventoryIndex = WiseFilterWheel.filterSizeToIndex[currentWheel._filterSize];
                    int offset = (position.filterName == string.Empty) ? 0 :
                                    _filterInventory[inventoryIndex].Find((x) => x.FilterName == position.filterName).FilterOffset;
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

                if ((targetPosition < 0) | (targetPosition > nPositions - 1))
                {
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
                ArduinoInterface.StepperDirection dir = (cw < ccw) ?    // NOTE: Wheel moves oposite to stepper
                    ArduinoInterface.StepperDirection.CCW :
                    ArduinoInterface.StepperDirection.CW;
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

        public Wheel.WheelDigest CurrentWheel
        {
            get
            {
                return currentWheel.Digest;
            }

            set
            {
                currentWheel = value.Type == WheelType.None ? wheel4 : wheel8;
                currentWheel._position = value.CurrentPosition;

                SaveCurrentWheelToFile();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseFilterWheel:SetCurrent: wheel = {0}, position = {1}",
                    currentWheel._name, currentWheel._position);
                #endregion
            }
        }

        private void SaveCurrentWheelToFile()
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

        private void RestoreCurrentWheelFromFile()
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

        public static void SaveFiltersInventoryToCsvFile(FilterSize filterSize)
        {
            string fileName = filterCsvFiles[filterSize];

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));

            using (var sw = new System.IO.StreamWriter(fileName))
            {
                sw.WriteLine("#");
                sw.WriteLine(string.Format("# Wise40 {0}\" filter inventory", filterSize.ToString()));
                sw.WriteLine(string.Format("# Last saved on: {0}", DateTime.Now.ToString()));
                sw.WriteLine("# Filter line format:");
                sw.WriteLine("#  name;decription;offset");
                sw.WriteLine("# Name and description are free strings, offset must be a integer.");
                sw.WriteLine("# Empty lines and comments (starting with #) are ignored.");
                sw.WriteLine("#");

                foreach (var filter in _filterInventory[filterSizeToIndex[filterSize]])
                {
                    sw.WriteLine(string.Format("{0};{1};{2}", filter.FilterName, filter.FilterDescription, filter.FilterOffset.ToString()));
                }
            }
        }
    }

    public class FilterWheelDigest
    {
        public short Position;
        public string OperationalState;
        public string Status;
        public bool Enabled;
    }
}
