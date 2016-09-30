//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Focuser driver for Wise40
//
// Description:	Drives the focusing mechanism of the Wise40 telescope.
//
// Implements:	ASCOM Focuser interface version: <To be completed by driver developer>
// Author:		(AB) Arie Blumenzweig <blumzi@013.net>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 26-Sep-2016	AB	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Focuser

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

namespace ASCOM.Wise40
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.Focuser
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.Focuser
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40 from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Focuser Driver for Wise40.
    /// </summary>
    [Guid("4cebb869-32ce-425e-8833-fbbc5054e274")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Focuser : IFocuserV2
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Wise40.Focuser";

        private WiseFocuser wisefocuser;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Focuser()
        {
            wisefocuser = WiseFocuser.Instance;
            wisefocuser.init();
            wisefocuser.traceLogger.LogMessage("Focuser", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE IFocuserV2 IMPLEMENTATION
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
            if (wisefocuser.Connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
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
                return wisefocuser.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return wisefocuser.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            wisefocuser.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return wisefocuser.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return wisefocuser.CommandString(command, raw);
        }

        public void Dispose()
        {
            wisefocuser.Dispose();
        }

        public bool Connected
        {
            get
            {
                return wisefocuser.Connected;
            }

            set
            {
                wisefocuser.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                return wisefocuser.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return wisefocuser.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return wisefocuser.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return wisefocuser.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return wisefocuser.Name;
            }
        }

        #endregion

        #region IFocuser Implementation


        public bool Absolute
        {
            get
            {
                return wisefocuser.Absolute;
            }
        }

        public void Halt()
        {
            wisefocuser.Halt();
        }

        public bool IsMoving
        {
            get
            {
                return wisefocuser.IsMoving;
            }
        }

        public bool Link
        {
            get
            {
                return wisefocuser.Link;
            }
            set
            {
                wisefocuser.Link = value;
            }
        }

        public int MaxIncrement
        {
            get
            {
                return wisefocuser.MaxIncrement;
            }
        }

        public int MaxStep
        {
            get
            {
                return wisefocuser.MaxStep;
            }
        }

        public void Move(int Position)
        {
            wisefocuser.Move(Position);
        }

        public int Position
        {
            get
            {
                return wisefocuser.Position; // Return the focuser position
            }
        }

        public double StepSize
        {
            get
            {
                return wisefocuser.StepSize;
            }
        }

        public bool TempComp
        {
            get
            {
                return wisefocuser.TempComp;
            }
            set
            {
                wisefocuser.TempComp = value;
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                return wisefocuser.TempCompAvailable;
            }
        }

        public double Temperature
        {
            get
            {
                return wisefocuser.Temperature;
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
                P.DeviceType = "Focuser";
                if (bRegister)
                {
                    P.Register(driverID, "ASCOM Wise40 Focuser");
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
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            wisefocuser.ReadProfile();
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            wisefocuser.WriteProfile();
        }

        #endregion

    }
}
