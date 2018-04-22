//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM FilterWheel driver for Wise40
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM FilterWheel interface version: <To be completed by driver developer>
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
#define FilterWheel

using System;
using System.Collections;
using System.Runtime.InteropServices;

using ASCOM.DeviceInterface;

namespace ASCOM.Wise40.FilterWheel
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.FilterWheel
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.FilterWheel
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40 from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM FilterWheel Driver for Wise40.
    /// </summary>
    [Guid("E971207E-4642-458F-80F5-7322A85E1649")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class FilterWheel : IFilterWheelV2
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        //internal static string driverID = "ASCOM.Wise40.FilterWheel";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>

        private WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public FilterWheel()
        {
            wisefilterwheel.init();
        }

        //
        // PUBLIC COM INTERFACE IFilterWheelV2 IMPLEMENTATION
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
            wisefilterwheel.SetupDialog();
        }

        public ArrayList SupportedActions
        {
            get
            {
                return wisefilterwheel.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return wisefilterwheel.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            wisefilterwheel.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return wisefilterwheel.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return wisefilterwheel.CommandString(command, raw);
        }

        public void Dispose()
        {
            wisefilterwheel.Dispose();
        }

        public bool Connected
        {
            get
            {
                return wisefilterwheel.Connected;
            }
            set
            {
                wisefilterwheel.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                return wisefilterwheel.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return wisefilterwheel.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return wisefilterwheel.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return wisefilterwheel.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return wisefilterwheel.Name;
            }
        }

        #endregion

        #region IFilerWheel Implementation

        public int[] FocusOffsets
        {
            get
            {
                return wisefilterwheel.FocusOffsets;
            }
        }

        public string[] Names
        {
            get
            {
                return wisefilterwheel.Names;
            }
        }

        public short Position
        {
            get
            {
                return wisefilterwheel.Position;
            }
            set
            {
                wisefilterwheel.Position = value;
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
                P.DeviceType = "FilterWheel";
                if (bRegister)
                {
                    P.Register(WiseFilterWheel.Instance.DriverID, WiseFilterWheel.Instance.Description);
                }
                else
                {
                    P.Unregister(WiseFilterWheel.Instance.DriverID);
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

        #endregion
    }
}
