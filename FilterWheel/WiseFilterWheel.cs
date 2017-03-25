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

namespace ASCOM.Wise40
{
    public class WiseFilterWheel : WiseObject, IDisposable
    {
        private Version version = new Version(0, 1);
        private static readonly WiseFilterWheel _instance = new WiseFilterWheel();

        public TraceLogger traceLogger = new TraceLogger();
        public Debugger debugger = Debugger.Instance;        

        private static bool _initialized = false;
        private static bool _connected = false; // simulated

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = "ASCOM.Wise40.FilterWheel";
        private static string driverDescription = "ASCOM FilterWheel Driver for Wise40.";
        private ArduinoInterface arduino = ArduinoInterface.Instance;
        public static string port;

        public enum WheelType { Unknown, Wheel8, Wheel4, Simulated };
        public Wheel currentWheel;
        public static Wheel wheel8 = new Wheel(WheelType.Wheel8);
        public static Wheel wheel4 = new Wheel(WheelType.Wheel4);
        public static Wheel wheelUnknown = new Wheel(WheelType.Unknown);
        public static Wheel wheelSimulated = new Wheel(WheelType.Simulated);
        public static List<Wheel> knownWheels = new List<Wheel>() { wheel8, wheel4, wheelSimulated };

        public static List<Filter> filterInventory;

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

        public struct Wheel
        {
            public WheelType type;
            public FWPosition[] positions;
            public string name;
            public short position;

            public Wheel(WheelType type)
            {
                this.type = type;
                if (type == WheelType.Wheel4)
                    this.positions = new FWPosition[4];
                else if (type == WheelType.Wheel8)
                    this.positions = new FWPosition[8];
                else if (type == WheelType.Simulated)
                    this.positions = new FWPosition[8];
                else
                    this.positions = new FWPosition[0];
                name = "Wheel" + ((type == WheelType.Simulated) ? "Simulated" : positions.Length.ToString());

                for (int i = 0; i < this.positions.Length; i++)
                {
                    if (type == WheelType.Simulated)
                    {
                        this.positions[i].filterName = string.Format("SimulatedFilter#{0}", i);
                        this.positions[i].tag = string.Format("SimulatedTag#{0}", i);
                    } else
                        this.positions[i].filterName = this.positions[i].tag = string.Empty;
                }
                position = (short) ((type == WheelType.Simulated) ? 0 : -1);
            }

        }

        Wheel lookupWheel(string tag)
        {
            Wheel ret = wheelUnknown;

            foreach (var wheel in knownWheels)
            {
                for (short pos = 0; pos < wheel.positions.Length; pos++)
                    if (tag == wheel.positions[pos].tag)
                    {
                        ret = wheel;
                        ret.position = pos;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "lookupWheel({0}) ==> wheel: {1}, position: {2}",
                            tag, ret.name, ret.position);
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

            Simulated = true;   // Force
            traceLogger.LogMessage("WiseFilterWheel", "Starting initialisation");
            Connected = false;
            ReadProfile();
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
                    string name = "Wheel" + ((w.type == WheelType.Simulated) ? "Simulated" : w.positions.Length.ToString());
                    for (int pos = 0; pos < w.positions.Length; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);
                        if (w.type == WheelType.Simulated)
                        {
                            w.positions[pos].filterName = string.Format("SimulatedFilter#{0}", pos);
                            w.positions[pos].tag = string.Format("SimulatedTag#{0}", pos);
                        }
                        else
                        {
                            w.positions[pos].filterName = driverProfile.GetValue(driverID, "Filter Name", subKey, string.Empty);
                            w.positions[pos].tag = driverProfile.GetValue(driverID, "RFID", subKey, string.Empty);
                        }
                    }
                }
                port = _instance.Simulated ? "NoPort" : driverProfile.GetValue(driverID, "Port", string.Empty, string.Empty);

                WiseFilterWheel.filterInventory = new List<Filter>();
                foreach (var sk in driverProfile.SubKeys(driverID))
                {
                    KeyValuePair kv = sk as KeyValuePair;
                    if (kv.Key.StartsWith("Filter"))
                    {
                        string name = driverProfile.GetValue(driverID, "Name", kv.Key, string.Empty);
                        string description = driverProfile.GetValue(driverID, "Description", kv.Key, string.Empty);
                        int offset = Convert.ToInt32(driverProfile.GetValue(driverID, "Offset", kv.Key, string.Empty));

                        WiseFilterWheel.filterInventory.Add(new Filter(name, description, offset));
                    }
                }
            }
        }

        public static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";
                string subKey;

                foreach (Wheel w in new List<Wheel> { wheel8, wheel4 })
                {
                    string name = "Wheel" + ((w.type == WheelType.Wheel4) ? "4" : "8");
                    for (int pos = 0; pos < w.positions.Length; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);

                        driverProfile.WriteValue(driverID, "Filter Name", w.positions[pos].filterName, subKey);
                        driverProfile.WriteValue(driverID, "RFID", w.positions[pos].tag, subKey);
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
                    currentWheel = lookupWheel("SimulatedTag#0");
                    return;
                }

                if (value == arduino.Connected)
                    return;
                arduino.Connected = value;
                if (value == true)                
                        currentWheel = Simulated ? wheelSimulated : wheelUnknown;
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
                string name = string.Format("{0} ({1} slots)", currentWheel.name, currentWheel.positions.Length);
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

                foreach (FWPosition position in currentWheel.positions) // Write filter offsets to the log
                {
                    int offset = (position.filterName == string.Empty) ? 0 :
                                    filterInventory.Find((x) => x.Name == position.filterName).Offset;
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
                if (currentWheel.positions == null)
                    return names.ToArray();
                foreach (FWPosition position in currentWheel.positions)
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
                short ret;

                if (!Connected)
                    throw new NotConnectedException("Not connected");

                if (currentWheel.type == WheelType.Simulated)
                    return currentWheel.position;

                string tag = arduino.Tag;
                if (tag == null)            // the arduino is busy
                    return -1;
                
                currentWheel = lookupWheel(tag);
                ret = currentWheel.position;
                
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "GetCurrentPosition: {0}", ret);
                #endregion
                return ret;
            }

            set
            {
                if (!Connected)
                    throw (new NotConnectedException("Not connected"));

                int nPositions = currentWheel.positions.Length;
                short targetPosition = value;

                if (targetPosition == currentWheel.position)
                    return;

                traceLogger.LogMessage("Position Set", targetPosition.ToString());
                if ((targetPosition < 0) | (targetPosition > nPositions - 1))
                {
                    traceLogger.LogMessage("", "Throwing InvalidValueException - Position: " + targetPosition.ToString() + ", Range: 0 to " + (nPositions - 1).ToString());
                    throw new InvalidValueException("Position", targetPosition.ToString(), "0 to " + (nPositions - 1).ToString());
                }

                if (currentWheel.type == WheelType.Simulated)
                {
                    currentWheel.position = targetPosition;
                    return;
                }

                int cw, ccw;    // # of positions to move
                if (targetPosition > currentWheel.position)
                {
                    cw = targetPosition - currentWheel.position;
                    ccw = currentWheel.position + nPositions - targetPosition;
                } else
                {
                    cw = currentWheel.position - targetPosition;
                    ccw = targetPosition + nPositions - currentWheel.position;
                }

                int slots = (cw < ccw) ? cw : ccw;
                ArduinoInterface.StepperDirection dir = (cw < ccw) ?
                    ArduinoInterface.StepperDirection.CW :
                    ArduinoInterface.StepperDirection.CCW;
                if (nPositions == 4)
                    slots *= 2;

                try
                {
                    arduino.StartMoving(dir, slots);
                } catch (Exception ex) { }
            }
        }
        #endregion

        public int Positions
        {
            get
            {
                return currentWheel.positions.Length;
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
    }
}
