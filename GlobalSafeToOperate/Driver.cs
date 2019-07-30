//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM SafetyMonitor driver for Wise.SafeToOperate
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM SafetyMonitor interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define SafetyMonitor

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System.Globalization;
using System.Collections;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise
{
    //
    // Your driver's DeviceID is ASCOM.Wise.SafeToOperate.SafetyMonitor
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise.SafeToOperate.SafetyMonitor
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise.SafeToOperate from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM SafetyMonitor Driver for Wise.SafeToOperate.
    /// </summary>
    [Guid("6674dcfd-677e-4c07-bcb9-05ec7c243c88")]
    [ClassInterface(ClassInterfaceType.None)]
    public class SafetyMonitor : ISafetyMonitor
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Wise.SafeToOperate.SafetyMonitor";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "ASCOM SafetyMonitor Driver for Wise.SafeToOperate.";
        
        private static Wise40.Common.Debugger debugger = Wise40.Common.Debugger.Instance;

        static DriverAccess.SafetyMonitor  wisesafetooperate = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise.SafeToOperate"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public SafetyMonitor()
        {
            wisesafetooperate = new DriverAccess.SafetyMonitor("ASCOM.Remote1.SafetyMonitor");
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
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            System.Windows.Forms.MessageBox.Show("No configurable settings, just press OK");
        }

        public ArrayList SupportedActions
        {
            get
            {
                return new ArrayList() { "isSafe" };
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            if (actionName == "isSafe")
                return wisesafetooperate.Action("global-safetooperate", "");
            else {
                string msg = "Action " + actionName + " is not implemented by this driver";
                #region debug
                debugger.WriteLine(Wise40.Common.Debugger.DebugLevel.DebugLogic, msg);
                #endregion
                throw new ASCOM.ActionNotImplementedException(msg);
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
        }

        public bool Connected
        {
            get
            {
                return wisesafetooperate.Connected;
            }

            set
            {
                if (value == wisesafetooperate.Connected)
                    return;

                wisesafetooperate.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                return Const.WiseDriverID.WiseSafeToOperate;
            }
        }

        public string DriverInfo
        {
            get
            {
                return Description + " v" + DriverVersion;
            }
        }

        public string DriverVersion
        {
            get
            {
                return wisesafetooperate.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                return "GlobalSafeToOperate";
            }
        }

        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                return Convert.ToBoolean(wisesafetooperate.Action("wise-issafe", ""));
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "SafetyMonitor";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                return Connected;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }
        #endregion
    }
}
