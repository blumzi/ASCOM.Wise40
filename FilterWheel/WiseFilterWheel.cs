using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
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

        private bool _connected = false;

        private static bool _initialized = false;

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = "ASCOM.Wise40.FilterWheel";
        private static string driverDescription = "ASCOM FilterWheel Driver for Wise40.";
        private ArduinoInterface arduino;
        public string port;

        public enum WheelType { Invalid, Wheel8, Wheel4 };
        public static Wheel currentWheel;
        public static Wheel wheel8 = new Wheel(WheelType.Wheel8);
        public static Wheel wheel4 = new Wheel(WheelType.Wheel4);
        public static Wheel wheelInvalid = new Wheel(WheelType.Invalid);
        public static List<Wheel> wheels = new List<Wheel>() { wheel8, wheel4 };
        private int currentPosition;

        public struct FWPosition
        {
            public string filterName;
            public string tag;
            public int filterOffset;

            public FWPosition(string n, int o, string u)
            {
                filterName = n;
                filterOffset = o;
                tag = u;
            }
        };

        public struct Wheel
        {
            public WheelType type;
            public FWPosition[] positions;
            public string name;

            public Wheel(WheelType type)
            {
                this.type = type;
                if (type == WheelType.Wheel4)
                    this.positions = new FWPosition[4];
                else if (type == WheelType.Wheel8)
                    this.positions = new FWPosition[8];
                else
                    this.positions = new FWPosition[0];
                name = "Wheel" + positions.Length.ToString();

                for (int i = 0; i < this.positions.Length; i++)
                {
                    this.positions[i].filterName = this.positions[i].tag = string.Empty;
                    this.positions[i].filterOffset = 0;
                }
            }

        }

        public Tuple<Wheel, int> lookupWheelPosition(string tag)
        {
            foreach (var wheel in wheels)
            {
                for (int pos = 0; pos < wheel.positions.Length; pos++)
                    if (tag == wheel.positions[pos].tag)
                        return Tuple.Create(wheel, pos);
            }
            return Tuple.Create(wheelInvalid, -1);
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

        public void init()
        {
            if (_initialized)
                return;

            traceLogger.LogMessage("WiseFilterWheel", "Starting initialisation");
            Connected = false;
            ReadProfile();
            arduino = new ArduinoInterface(port);
            arduino.Connected = true;
            string currentTag = arduino.getPosition();

            Tuple<Wheel, int> t = lookupWheelPosition(currentTag);
            if (t.Item1.type != WheelType.Invalid)
            {
                currentWheel = t.Item1;
                currentPosition = t.Item2;
            }
            traceLogger.LogMessage("WiseFilterWheel", "Completed initialisation");
        }

        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";
                string subKey;

                foreach (Wheel w in wheels)
                {
                    string name = "Wheel" + ((w.type == WheelType.Wheel4) ? "4" : "8");
                    for (int pos = 0; pos < w.positions.Length; pos++)
                    {
                        subKey = string.Format("{0}/Position{1}", name, pos + 1);

                        w.positions[pos].filterName = driverProfile.GetValue(driverID, "Name", subKey, string.Empty);
                        w.positions[pos].filterOffset = Convert.ToInt32(driverProfile.GetValue(driverID, "Focus Offset", subKey, 0.ToString()));
                        w.positions[pos].tag = driverProfile.GetValue(driverID, "RFID", subKey, string.Empty);
                    }
                }
                port = driverProfile.GetValue(driverID, "Port", string.Empty, string.Empty);
            }
        }

        public void WriteProfile()
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

                        driverProfile.WriteValue(driverID, "Name", w.positions[pos].filterName, subKey);
                        driverProfile.WriteValue(driverID, "Focus Offset", w.positions[pos].filterOffset.ToString(), subKey);
                        driverProfile.WriteValue(driverID, "RFID", w.positions[pos].tag, subKey);
                    }
                }
                driverProfile.WriteValue(driverID, "Port", port);
            }
        }

        public bool Connected
        {
            get
            {
                traceLogger.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }

            set
            {
                traceLogger.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;
                _connected = value;
                arduino.Connected = value;
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
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        public new string Name
        {
            get
            {
                string name = "Wise40 FilterWheel";
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
            if (_connected)
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
                    focusOffsets.Add(position.filterOffset);
                    traceLogger.LogMessage("FocusOffsets Get", position.filterOffset.ToString());
                }

                return focusOffsets.ToArray();
            }
        }

        public string[] Names
        {
            get
            {
                List<string> names = new List<string>();
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
                short ret = -1;
                if (!Connected)
                    throw (new NotConnectedException("Not connected"));

                string tag = arduino.getPosition();
                Tuple<Wheel, int> t = lookupWheelPosition(tag);
                if (t.Item1.type != WheelType.Invalid)
                {
                    currentWheel = t.Item1;
                    currentPosition = t.Item2;
                    ret = (short)currentPosition;
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "GetCurrentPosition: {0}", ret);
                #endregion
                return ret;
            }

            set
            {
                if (!Connected)
                    throw (new NotConnectedException("Not connected"));

                int nPositions = currentWheel.positions.Length;
                short targetPosition = value;

                if (targetPosition == currentPosition)
                    return;

                traceLogger.LogMessage("Position Set", targetPosition.ToString());
                if ((targetPosition < 0) | (targetPosition > nPositions - 1))
                {
                    traceLogger.LogMessage("", "Throwing InvalidValueException - Position: " + targetPosition.ToString() + ", Range: 0 to " + (nPositions - 1).ToString());
                    throw new InvalidValueException("Position", targetPosition.ToString(), "0 to " + (nPositions - 1).ToString());
                }

                int cw, ccw;    // # of positions to move
                if (targetPosition > currentPosition)
                {
                    cw = targetPosition - currentPosition;
                    ccw = currentPosition + currentWheel.positions.Length - targetPosition;
                } else
                {
                    cw = currentPosition - targetPosition;
                    ccw = targetPosition + nPositions - currentPosition;
                }

                if (cw > ccw)
                    arduino.move(ArduinoInterface.Direction.CW, (nPositions == 4) ? 2 * cw : cw);
                else
                    arduino.move(ArduinoInterface.Direction.CCW, (nPositions == 4) ? 2 * ccw : ccw);
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
    }
}
