using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using System.Collections;
using System.Globalization;
using System.Threading;

using Newtonsoft.Json;

namespace ASCOM.Wise40 //.FilterWheel
{
    public class WiseFilterWheel : WiseObject, IDisposable
    {
        private static readonly Version version = new Version(0, 2);

        public Debugger debugger = Debugger.Instance;

        private static bool _initialized = false;
        private static bool _enabled = false;
        private bool disposed = false;

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = Const.WiseDriverID.FilterWheel;
        public ArduinoInterface arduino = ArduinoInterface.Instance;
        public static ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        public enum WheelType { Wheel8, Wheel4, Unknown};
        public enum FilterSize { TwoInch, ThreeInch };
        public static List<FilterSize> filterSizes = new List<FilterSize> { FilterSize.TwoInch, FilterSize.ThreeInch };

        public static Wheel wheel8 = new Wheel(WheelType.Wheel8);
        public static Wheel wheel4 = new Wheel(WheelType.Wheel4);
        public Wheel currentWheel;
        public static List<Wheel> wheels = new List<Wheel>() { wheel8, wheel4 };

        private readonly string _savedFile = "c://Wise40/FilterWheel/Config.txt";
        private static readonly System.Threading.Timer _updateTimer = new System.Threading.Timer(new TimerCallback(Updater));
        public static DateTime _lastDataReceived;
        public static readonly Exceptor Exceptor = new Exceptor(Debugger.DebugLevel.DebugFilterWheel);

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
            public short _position;
            public short _targetPosition;
            public int _nPositions;
            public FilterSize _filterSize;
            public List<Filter> _filterInventory;
            public string _filterCSVFile;
            public DateTime _lastReadFromCSV;

            public Wheel(WheelType type)
            {
                _type = type;
                switch (_type)
                {
                    case WheelType.Wheel4:
                        _nPositions = 4;
                        _filterSize = FilterSize.ThreeInch;
                        _filterCSVFile = Const.topWise40Directory + "/FilterWheel/threeInchFilters.csv";
                        break;

                    case WheelType.Wheel8:
                        _nPositions = 8;
                        _filterSize = FilterSize.TwoInch;
                        _filterCSVFile = Const.topWise40Directory + "/FilterWheel/twoInchFilters.csv";
                        break;
                }
                _positions = new FWPosition[_nPositions];

                _targetPosition = -1;
                WiseName = _type.ToString();

                for (int i = 0; i < _nPositions; i++)
                {
                    _positions[i].filterName = _positions[i].tag = string.Empty;
                }
                _position = -1;

                ReadFiltersFromCsvFile();
            }

            public class PositionDigest
            {
                public int Position;
                public string Name;
                public string Description;
                public int Offset;
                public string RFIDTag;
                public string Comment;
            }

            public class WheelDigest
            {
                public WheelType Type;
                public string Name;
                public string FilterSizeInch;
                public string FilterSizeString;
                public int Npositions;
                public PositionDigest CurrentPosition;
                public List<PositionDigest> Filters;
            }

            public List<PositionDigest> Positions
            {
                get
                {
                    List<PositionDigest> positions = new List<PositionDigest>();

                    for (int i = 0; i < _positions.Length; i++)
                    {
                        string filterName = _positions[i].filterName;
                        Filter filter = (string.IsNullOrEmpty(filterName)) ? null : _filterInventory.Find((x) => x.Name == filterName);

                        positions.Add(new PositionDigest
                        {
                            Position = i,
                            Name = filterName,
                            Description = filter == null ? "" : filter.Description,
                            Offset = filter?.Offset ?? 0,
                            RFIDTag = _positions[i].tag,
                            Comment = filter?.Comment ?? null,
                        });
                    }
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
                        Name = WiseName,
                        Npositions = _nPositions,
                        Filters = Positions,
                        CurrentPosition = Positions[_position],
                        FilterSizeInch = _filterSize == FilterSize.ThreeInch ? "3" : "2",
                        FilterSizeString = _filterSize.ToString(),
                    };
                }
            }

            //
            // There are two CSV filter inventory files (for two and three inch filters respectively)
            //
            private void ReadFiltersFromCsvFile()
            {
                if (_filterInventory == null)
                    _filterInventory = new List<Filter>();

                if (!System.IO.File.Exists(_filterCSVFile))
                    return;

                if (System.IO.File.GetLastWriteTime(_filterCSVFile).CompareTo(_lastReadFromCSV) <= 0)
                    return;

                using (var sr = new System.IO.StreamReader(_filterCSVFile))
                {
                    string line;
                    string[] fields;
                    int offset;
                    string comment = null;

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.TrimStart().TrimEnd();
                        if (line.Length == 0)           // skip empty lines
                            continue;
                        if (line.StartsWith("#"))       // skip comments
                            continue;
                        int idx = line.IndexOf('#');    // remember comment
                        if (idx != -1)
                        {
                            comment = line.Substring(idx + 1).Trim();
                            line.Remove(idx);
                            line = line.Trim();
                        }
                        fields = line.Split(CSVseparator[0]);
                        if (fields.Length != 3)         // skip bad lines
                            continue;

                        try
                        {
                            offset = Convert.ToInt32(fields[2]);
                        }
                        catch (FormatException)
                        {
                            offset = 0;
                        }
                        _filterInventory.Add(new Filter(fields[0], fields[1], offset, comment));
                    }
                }
                _lastReadFromCSV = DateTime.Now;
            }

            public List<Filter> GetKnownFilters
            {
                get
                {
                    return _filterInventory;
                }
            }
        }

        public static Wheel LookupWheel(string tag)
        {
            Wheel ret = null;

            foreach (var wheel in wheels)
            {
                for (short pos = 0; pos < wheel._nPositions; pos++)
                    if (tag == wheel._positions[pos].tag)
                    {
                        ret = wheel;
                        ret._position = pos;
                        #region debug
                        Instance.debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "lookupWheel({0}) ==> wheel: {1}, position: {2}",
                            tag, ret.WiseName, ret._position);
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
                return Const.WiseDriverID.FilterWheel;
            }
        }

        //void onCommunicationComplete(object sender, ArduinoInterface.CommunicationCompleteEventArgs e)
        //{
        //    string tag = arduino.Tag;
        //    string stat = arduino.StatusAsString;

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "onCommunicationComplete: tag: \"{0}\", status: {1}",
        //        tag, stat);
        //    #endregion
        //    if (stat == "No tag in range")
        //    {
        //        currentWheel._position = currentWheel._targetPosition;
        //        RaiseWheelOrPositionChanged();
        //    }
        //    if (tag != null) {
        //        currentWheel = lookupWheel(tag);
        //        RaiseWheelOrPositionChanged();
        //    }
        //}

        private static readonly Lazy<WiseFilterWheel> lazy = new Lazy<WiseFilterWheel>(() => new WiseFilterWheel()); // Singleton

        public static WiseFilterWheel Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (_initialized)
                return;

            ReadProfile();
            if (!Enabled)
                return;

            //foreach (var size in filterSizes)
            //    ReadFiltersFromCsvFile(size);

            RestoreCurrentWheelFromFile();

            if (!Simulated)
            {
                arduino.Init();
                //arduino.communicationCompleteHandler += onCommunicationComplete;
            }

            _initialized = true;
        }

        private static readonly String[] defaultWheel8RFIDs = {
            "7F0007F75E",   // 1
            "7F000817F7",   // 2
            "7F000AEFC5",   // 3
            "7C00563E5A",   // 4
            "7F001B2B73",   // 5
            "7F000ACAD5",   // 6
            "7F001B4A83",   // 7
            "7F0007BC0E",   // 8
        };

        private static readonly String[] defaultWheel4RFIDs = {
            "7F001B4C16",   // A
            "7C0055F4EB",   // B
            "7F001B0573",   // C
            "7F000AF9A0",   // D
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

                Enabled = (WiseSite.OperationalMode != WiseSite.OpMode.LCO) &&
                    Convert.ToBoolean(driverProfile.GetValue(driverID, "Enabled", string.Empty, "false"));
            }
        }

        public static Dictionary<FilterSize, int> filterSizeToIndex = new Dictionary<FilterSize, int>
        {
            {FilterSize.TwoInch, 0 },
            {FilterSize.ThreeInch, 1 },
        };

        public static void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "FilterWheel" })
            {
                string subKey;

                driverProfile.WriteValue(driverID, "Enabled", Enabled.ToString());
                foreach (Wheel w in wheels)
                {
                    string name = w.WiseName;
                    for (int pos = 0; pos < w._nPositions; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);

                        driverProfile.WriteValue(driverID, "Filter Name", w._positions[pos].filterName, subKey);
                        driverProfile.WriteValue(driverID, "RFID", w._positions[pos].tag, subKey);
                    }
                }
            }
        }

        public static void Updater(object StateObject)
        {
            Instance.arduino.StartReadingTag();
        }

        public bool Connected
        {
            get
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.LCO)
                    return false;

                return arduino.Connected;
            }

            set
            {
                if (WiseSite.OperationalMode == WiseSite.OpMode.LCO)
                    return;

                if (value == arduino.Connected)
                    return;
                try
                {
                    arduino.Connected = value;
                }
                catch /* (Exception ex) */
                {
                    throw;
                }

                ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.FilterWheel, value, line: ActivityMonitor.Tracer.filterwheel.Line));

                if (value)
                    _updateTimer.Change(2000, 30000);
                else
                    _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
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

                _enabled = value;
            }
        }

        public static string Description { get; } = $"{driverID} v{version}";

        public string DriverInfo
        {
            get
            {
                return "Wise40 FilterWheel. Version: " + DriverVersion;
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
                case "debug":
                    if (!String.IsNullOrEmpty(parameters))
                    {
                        Debugger.DebugLevel newDebugLevel;
                        try
                        {
                            Enum.TryParse<Debugger.DebugLevel>(parameters, out newDebugLevel);
                            Debugger.SetCurrentLevel(newDebugLevel);
                        }
                        catch
                        {
                            return $"Cannot parse DebugLevel \"{parameters}\"";
                        }
                    }
                    return $"{Debugger.Level}";

                case "enabled":
                    if (string.IsNullOrEmpty(parameters))
                    {
                        return Enabled.ToString();
                    }
                    else
                    {
                        Enabled = Convert.ToBoolean(parameters);
                        return "ok";
                    }

                case "status":
                    return JsonConvert.SerializeObject(new WiseFilterWheelDigest
                    {
                        Enabled = Enabled,
                        LastDataReceived = _lastDataReceived,
                        Arduino = new ArduinoDigest
                        {
                            Status = arduino.Status,
                            StatusString = arduino.StatusAsString,
                            Error = arduino.Error,
                            LastCommand = arduino.LastCommand,
                        },
                        Serial = new SerialPortDigest
                        {
                            Port = arduino.SerialPortName,
                            Speed = arduino.SerialPortSpeed.ToString(),
                            IsOpen = arduino.PortIsOpen,
                        },
                        Wheel = currentWheel?.Digest,
                    });

                case "current-wheel":
                    if (string.IsNullOrEmpty(parameters))
                    {
                        return JsonConvert.SerializeObject(currentWheel?.Digest);
                    }
                    else
                    {
                        CurrentWheelDigest = JsonConvert.DeserializeObject<Wheel.WheelDigest>(parameters);
                        return "ok";
                    }

                case "get-filter-inventory":
                        return JsonConvert.SerializeObject(Filters);

                case "set-filter-inventory":
                    SetFilterInventoryParam par = JsonConvert.DeserializeObject<SetFilterInventoryParam>(parameters);
                    SetFilterInventory(par);
                    return "ok";

                case "get-tag":
                    arduino.StartReadingTag();
                    return "ok";

                case "position":
                    if (string.IsNullOrEmpty(parameters))
                        return Position.ToString();

                    Position = Convert.ToInt16(parameters);
                    return "ok";

                default:
                    Exceptor.Throw<ASCOM.ActionNotImplementedException>($"Action(\"{action}\")", "Not implemented by this driver");
                    return string.Empty;
            }
        }

        public class FilterDigest
        {
            public string Name;
            public string Description;
            public int Offset;
            public string Comment;
        }

        public class WheelFilterDigest
        {
            public string Wheel;
            public string FilterSize;
            public List<FilterDigest> Filters;

            public WheelFilterDigest(FilterSize size)
            {
                Wheel wheel = size == WiseFilterWheel.FilterSize.TwoInch ? wheel8 : wheel4;
                Wheel = wheel.WiseName;
                FilterSize = size.ToString();
                Filters = new List<FilterDigest>();

                List<Filter> inventory = wheel._filterInventory;

                foreach (var filter in inventory)
                {
                    Filters.Add(new FilterDigest
                    {
                        Name = filter.Name,
                        Description = filter.Description,
                        Offset = filter.Offset,
                        Comment = filter.Comment,
                    });
                }
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

        public static void SetFilterInventory(SetFilterInventoryParam par)
        {
            Wheel wheel = par.FilterSize == FilterSize.TwoInch ? wheel8 : wheel4;

            wheel._filterInventory = new List<Filter>();
            foreach (var f in par.Filters)
                wheel._filterInventory.Add(f);

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
            Exceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw})", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw})", "Not implemented");
            return false;
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            Exceptor.Throw<MethodNotImplementedException>($"CommandString({command}, {raw})", "Not implemented");
            return string.Empty;
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (Connected)
            {
                Exceptor.Throw<NotConnectedException>("CheckConnected", message);
            }
        }

        public string Name
        {
            get
            {
                return Const.WiseDriverID.FilterWheel;
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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

#pragma warning disable CA1819 // Properties should not return arrays
        public int[] FocusOffsets
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                if (currentWheel == null)
                    Exceptor.Throw<FilterWheelNotDetectedException>("FocusOffsets", "No filter wheel detected");

                List<int> focusOffsets = new List<int>();

                foreach (FWPosition position in currentWheel._positions) // Write filter offsets to the log
                {
                    int offset = (string.IsNullOrEmpty(position.filterName)) ? 0 :
                                    currentWheel._filterInventory.Find((x) => x.Name == position.filterName).Offset;
                    focusOffsets.Add(offset);
                }
                return focusOffsets.ToArray();
            }
        }

#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Names
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                if (currentWheel == null)
                    Exceptor.Throw<FilterWheelNotDetectedException>("Names", "No filter wheel detected");

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
                    Exceptor.Throw<NotConnectedException>("Position", "Not connected");

                if (currentWheel == null)
                    Exceptor.Throw<FilterWheelNotDetectedException>("Position", "No filter wheel detected");

                short ret = currentWheel._position;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "Position:get: {0}", ret);
                #endregion
                return ret;
            }

            set
            {
                if (!Connected)
                    Exceptor.Throw<NotConnectedException>("Position", "Not connected");

                if (currentWheel == null)
                    Exceptor.Throw<FilterWheelNotDetectedException>("Position", "No filter wheel detected");

                int nPositions = currentWheel._nPositions;
                short targetPosition = value;

                if (targetPosition == currentWheel._position)
                    return;

                if ((targetPosition < 0) || (targetPosition >= nPositions))
                    Exceptor.Throw<InvalidValueException>("Position.set", $"Bad position ({targetPosition}): allowed: 0 to {nPositions - 1}");

                int cw, ccw;    // # of positions to move
                if (targetPosition > currentWheel._position)
                {
                    ccw = targetPosition - currentWheel._position;
                    cw = nPositions - ccw;
                } else
                {
                    cw = currentWheel._position - targetPosition;
                    ccw = nPositions - cw;
                }

                int slots = (cw < ccw) ? cw : ccw;
                ArduinoInterface.StepperDirection dir = (cw < ccw) ?    // NOTE: Wheel moves oposite to stepper
                    ArduinoInterface.StepperDirection.CW :
                    ArduinoInterface.StepperDirection.CCW;
                if (nPositions == 4)
                    slots *= 2;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel,
                    "Position:set: current: {0}, target: {1}, slots: {2}, cw: {3}, ccw: {4}, dir: {5}",
                    currentWheel._position, targetPosition, slots, cw, ccw, dir.ToString());
                #endregion

                StartActivity(
                    Activity.FilterWheel.Operation.Move,
                    currentWheel.WiseName,
                    currentWheel._position,
                    targetPosition);

                currentWheel._targetPosition = targetPosition;
                try
                {
                    arduino.StartMoving(dir, slots);
                } catch (Exception ex) {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "Position:set: communication exception {0}", ex.StackTrace);
                    #endregion
                }
            }
        }
        #endregion

        public int Positions
        {
            get
            {
                if (currentWheel == null)
                    Exceptor.Throw<FilterWheelNotDetectedException>("Positions", "No filter wheel detected");

                return currentWheel._nPositions;
            }
        }

        public event EventHandler WheelOrPositionChanged;

        public void RaiseWheelOrPositionChanged()
        {
            EventHandler handler = WheelOrPositionChanged;

            if (handler != null)
            {
                foreach (EventHandler singleCast in handler.GetInvocationList())
                {
                    try
                    {
                        if ((singleCast.Target is ISynchronizeInvoke syncInvoke) && (syncInvoke.InvokeRequired))
                            syncInvoke.Invoke(singleCast, new object[] { this, EventArgs.Empty });
                        else
                            singleCast(this, EventArgs.Empty);
                    }
                    catch
                    { }
                }
            }
        }

        public Wheel.WheelDigest CurrentWheelDigest
        {
            get
            {
                Wheel.WheelDigest ret = null;

                if (currentWheel != null)
                    ret = currentWheel.Digest;
                return ret;
            }

            set
            {
                switch (value.Type)
                {
                    case WheelType.Wheel4:
                        currentWheel = wheel4;
                        break;
                    case WheelType.Wheel8:
                        currentWheel = wheel8;
                        break;
                }
                currentWheel._position = (short) value.CurrentPosition.Position;

                SaveCurrentWheelToFile();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "CurrentWheel:set: wheel = {0}, position = {1}",
                    currentWheel.WiseName, currentWheel._position);
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
                sw.WriteLine(string.Format("Wheel: {0}", currentWheel._type.ToString()));
                sw.WriteLine(string.Format("Position: {0}", currentWheel._position));
            }
        }

        private void RestoreCurrentWheelFromFile()
        {
            string wheel = string.Empty;
            string pos = string.Empty;
            string[] parts;
            char[] spaces = { ' ', '\t' };
            WheelType wheelType = WheelType.Unknown;
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
                Enum.TryParse<WheelType>(wheel, out wheelType);
            }
            catch (FormatException) { }

            try
            {
                p = Convert.ToInt16(pos);
            }
            catch (FormatException) { }

            switch (wheelType)
            {
                case WheelType.Wheel4:
                    currentWheel = wheel4;
                    break;
                case WheelType.Wheel8:
                    currentWheel = wheel8;
                    break;
            }

            currentWheel._position = p;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugFilterWheel, "RestoreCurrentFromFile: wheel = {0}, position = {1}",
                currentWheel.WiseName, currentWheel._position);
            #endregion
        }

        private const string CSVseparator = ",";
        public static void SaveFiltersInventoryToCsvFile(FilterSize filterSize)
        {
            Wheel wheel = filterSize == FilterSize.TwoInch ? wheel8 : wheel4;
            string fileName = wheel._filterCSVFile;

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));

            using (var sw = new System.IO.StreamWriter(fileName, false))
            {
                sw.WriteLine("#");
                sw.WriteLine($"# Wise40 {filterSize}\" filter inventory");
                sw.WriteLine($"# Last saved on: {DateTime.Now}");
                sw.WriteLine("# Filter line format:");
                sw.WriteLine($"#  name{CSVseparator}decription{CSVseparator}offset");
                sw.WriteLine("# Name and description are free strings, offset must be a integer.");
                sw.WriteLine("# Empty lines and comments (starting with #) are ignored.");
                sw.WriteLine("#");
                sw.WriteLine("\n");

                foreach (var filter in wheel._filterInventory)
                {
                    string line = $"{filter.Name}{CSVseparator}{filter.Description}{CSVseparator}{filter.Offset}";
                    if (!string.IsNullOrEmpty(filter.Comment))
                        line += $" # {filter.Comment}";
                    sw.WriteLine(line);
                }
            }
        }

        [Serializable]
        public class FilterWheelNotDetectedException : Exception
        {
            public FilterWheelNotDetectedException()
                :base("Could not detect a filter wheel!")
            {
            }

            public FilterWheelNotDetectedException(string message)
                : base(message)
            {
            }

            public FilterWheelNotDetectedException(string message, Exception inner)
                : base(message, inner)
            {
            }

            protected FilterWheelNotDetectedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            {
                throw new System.NotImplementedException();
            }
        }

        public static void StartActivity(Activity.FilterWheel.Operation op, string startWheel, int startPos, int targetPos)
        {
            activityMonitor.NewActivity(new Activity.FilterWheel(new Activity.FilterWheel.StartParams
            {
                operation = op,
                startWheel = startWheel,
                startPosition = startPos,
                targetPosition = targetPos,
            }));
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        public static void EndActivity(Activity.FilterWheel.Operation op, string endWheel, int endPos, string endTag, Activity.State endState, string endReason)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
        {
            activityMonitor.EndActivity(ActivityMonitor.ActivityType.FilterWheel, new Activity.FilterWheel.EndParams()
            {
                endWheel = endWheel,
                endPosition = endPos,
                endTag = endTag,
                endState = endState,
                endReason = endReason,
            });
        }
    }

    public class ArduinoDigest
    {
        public ArduinoInterface.ArduinoStatus Status;
        public string StatusString;
        public string Error;
        public string LastCommand;
    }

    public class SerialPortDigest
    {
        public string Port;
        public string Speed;
        public bool IsOpen;
    }

    public class WiseFilterWheelDigest
    {
        public bool Enabled;
        public string Status;
        public DateTime LastDataReceived;
        public ArduinoDigest Arduino;
        public SerialPortDigest Serial;
        public WiseFilterWheel.Wheel.WheelDigest Wheel;
    }
}
