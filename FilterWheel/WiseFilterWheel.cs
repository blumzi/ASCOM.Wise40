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
        private RFIDReader RFIDReader = RFIDReader.Instance;

        private bool _connected = false;
        
        private static bool _initialized = false;

        public WiseFilterWheel() { }
        static WiseFilterWheel() { }
        internal static string driverID = "ASCOM.Wise40.FilterWheel";
        private static string driverDescription = "ASCOM FilterWheel Driver for Wise40.";

        public struct FWPosition {
            public string name;
            public string uuid;
            public int offset;

            public FWPosition(string n, int o, string u)
            {
                name = n;
                offset = o;
                uuid = u;
            }
        };

        public struct Wheel
        {
            public string name;
            public FWPosition[] positions;

            public Wheel(string name, int npos)
            {
                this.name = name;
                this.positions = new FWPosition[npos];
                for (int i = 0; i < npos; i++)
                {
                    positions[i].name = positions[i].uuid = string.Empty;
                    positions[i].offset = 0;
                }
            }
        }

        private Wheel wheel;
        public Wheel wheel8 = new Wheel("Wheel8", 8);
        public Wheel wheel4 = new Wheel("Wheel4", 4);

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
            /*
             * Detect the filter wheel:
             * 1. Read the nearest filter RFID
             * 2. Lookup the RFID in the profile
             * 3. Deduce:
             *      - If this is the 8-position or the 4-position wheel
             *      - What's the current position (optional)
             */
            traceLogger.LogMessage("WiseFilterWheel", "Completed initialisation");
            wheel = wheel8;
        }

        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "FilterWheel";
                string subKey;

                foreach (Wheel w in new List<Wheel> { wheel8, wheel4 })
                {
                    for (int pos = 0; pos < w.positions.Length; pos++)
                    {
                        subKey = string.Format("{0}/{1}", w.name, pos + 1);

                        w.positions[pos].name = driverProfile.GetValue(driverID, "Name", subKey, string.Empty);
                        w.positions[pos].offset = Convert.ToInt32(driverProfile.GetValue(driverID, "Focus Offset", subKey, 0.ToString()));
                        w.positions[pos].uuid = driverProfile.GetValue(driverID, "RFID", subKey, string.Empty);
                    }
                }
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
                    for (int pos = 0; pos < w.positions.Length; pos++)
                    {
                        subKey = string.Format("{0}/{1}", w.name, pos + 1);

                        driverProfile.WriteValue(driverID, "Name", w.positions[pos].name, subKey);
                        driverProfile.WriteValue(driverID, "Focus Offset", w.positions[pos].offset.ToString(), subKey);
                        driverProfile.WriteValue(driverID, "RFID", w.positions[pos].uuid, subKey);
                    }
                }
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

                foreach (FWPosition position in wheel.positions) // Write filter offsets to the log
                {
                    focusOffsets.Add(position.offset);
                    traceLogger.LogMessage("FocusOffsets Get", position.offset.ToString());
                }

                return focusOffsets.ToArray();
            }
        }

        public string[] Names
        {
            get
            {
                List<string> names = new List<string>();
                foreach (FWPosition position in wheel.positions)
                {
                    traceLogger.LogMessage("Names Get", position.name);
                    names.Add(position.name);
                }

                return names.ToArray();
            }
        }
        
        public short Position
        {
            get
            {
                string uuid = RFIDReader.UUID;
                short ret = -1;

                for (short i = 0; i < wheel.positions.Length; i++)
                {
                    if (uuid == wheel.positions[i].uuid)
                    {
                        ret = i;
                        break;
                    }
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "GetCurrentPosition: {0}", ret);
                #endregion
                return ret;
            }

            set
            {
                int nPositions = wheel.positions.Length;

                traceLogger.LogMessage("Position Set", value.ToString());
                if ((value < 0) | (value > nPositions - 1))
                {
                    traceLogger.LogMessage("", "Throwing InvalidValueException - Position: " + value.ToString() + ", Range: 0 to " + (nPositions - 1).ToString());
                    throw new InvalidValueException("Position", value.ToString(), "0 to " + (nPositions - 1).ToString());
                }
                /*
                 * TODO: Tell the filter wheel to go to position <value>
                 */
                //fwPosition = value;
            }
        }
        #endregion
    }
}
