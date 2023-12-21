//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM SafetyMonitor driver for Wise40.SafeToOperate
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
using System.Globalization;
using System.Collections;

using ASCOM.DriverAccess;
using ASCOM.Wise40.Boltwood;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    //
    // Your driver's DeviceID is ASCOM.Wise40SafeToOperate.SafetyMonitor
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40SafeToOperate.SafetyMonitor
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40.SafeToOperate from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM SafetyMonitor Driver for Wise40.SafeToOperate.
    /// </summary>
    [Guid("67F3B23A-17E1-4F0B-A9E9-BDD750E3C289")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class SafetyMonitor : ISafetyMonitor
    {
        public static WiseSafeToOperate wisesafetooperate;
        private const string driverID = Const.WiseDriverID.SafeToOperate;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40.SafeToOperate"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public SafetyMonitor()
        {
            try
            {
                wisesafetooperate = WiseSafeToOperate.Instance;
            } catch (Exception ex) {
                ;
            }
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
            if (wisesafetooperate.Connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SafeToOperateSetupDialogForm F = new SafeToOperateSetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    wisesafetooperate.WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                return wisesafetooperate.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return wisesafetooperate.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            wisesafetooperate.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return wisesafetooperate.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return wisesafetooperate.CommandString(command, raw);
        }

        public void Dispose()
        {
            wisesafetooperate.Dispose();
        }

        public bool Connected
        {
            get
            {
                return wisesafetooperate.Connected;
            }
            set
            {
                wisesafetooperate.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                return wisesafetooperate.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return wisesafetooperate.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return WiseSafeToOperate.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return wisesafetooperate.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return wisesafetooperate.Name;
            }
        }

        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                return wisesafetooperate.IsSafe;
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
                    P.Register(driverID, "ASCOM Wise40.SafeToOperate v0.2");
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
        public static void RegisterASCOM(Type _)
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
        public static void UnregisterASCOM(Type _)
        {
            RegUnregASCOM(false);
        }

        #endregion

        #endregion
    }
}
