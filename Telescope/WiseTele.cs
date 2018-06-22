using System;
using System.Collections;
using System.Collections.Generic;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.DeviceInterface;

using MccDaq;
using ASCOM.Wise40;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// From the Las Campanas web site (http://www.lco.cl/telescopes-information/henrietta-swope) for the Swope telescope,
///   an identical twin to the Wise40 telescope:
/// 
/// The Swope telescope was built by the Boller and Chivens Division of the Perkin-Elmer Corp
/// The optical characteristics are discussed in detail by Bowen and Vaughen (1973, Applied Optics, 12, 1430).
/// The optical design is an f/7 Ritchey-Chrétien in which the radii of curvature of the primary and secondary are equal,
/// thereby achieving a zero Petzval sum and a flat field. Astigmatism is eliminated with a Gascoigne corrector lens.
/// This design achieves a well-corrected field about 3 degrees in diameter. However, to do this it was necessary to 
///  use a secondary one-half the diameter of the primary, thereby intercepting 25% of the incident light.
/// 
/// An f/13.5 secondary used for infrared imaging is also available through a top-end "flip".
///  
/// 
/// 2. Optical Design
///  
/// The following table gives the optical specifications of the f/7 Cassegrain configuration.
/// 
///     Diameter primary:	                            1,016 mm
///     Focal length primary:	                        4,118 mm
///     Focal length Cassegrain:	                    7,112 mm
///     Diameter hole in primary:	                      386 mm
///     Diameter secondary:	                              508 mm
///     Diameter corrector plate:	                      386 mm
///     Distance between mirrors:	                    2,384 mm
///     Focal point distance behind surface of primary:	  610 mm
///     Radius of curvature of focal surface:	              infinity
///     Unvignetted/corrected field of view:	         1.92 degrees
///     Vignetting @ 88 arcmin field angle:	                5 %
///     Scale:	                                       0.0345 mm/arcsec
/// 
/// </summary>
namespace ASCOM.Wise40
{
    public class WiseTele : WiseObject, IDisposable, IConnectable
    {
        private static Version version = new Version(0, 2);
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public static string driverDescription = string.Format("Wise40 Telescope v{0}", version.ToString());

        private static NOVAS31 novas31;
        private static Util ascomutils;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        public TraceLogger traceLogger = new TraceLogger();
        public Debugger debugger = Debugger.Instance;

        private bool _connected = false;
        private bool _parking = false;

        public InactivityMonitor inactivityMonitor;

        #region TrackingRestoration
        /// <summary>
        /// Remembers the Tracking state when MoveAxis instance(s) are activated.
        /// When no more MoveAxis instance(s) are active, it restores the remembered Tracking stat.e
        /// </summary>
        private class TrackingRestorer
        {
            bool _wasTracking;
            bool _savedTrackingState = false;
            long _axisMovers;

            public TrackingRestorer()
            {
                Interlocked.Exchange(ref _axisMovers, 0);
            }

            public void AddMover()
            {
                long current = Interlocked.Increment(ref _axisMovers);
                #region debug
                string dbg = string.Format("TrackingRestorer:AddMover:  current: {0}", current);
                #endregion
                if (current == 1)
                {
                    _wasTracking = _instance.Tracking;
                    _savedTrackingState = true;
                    #region debug
                    dbg += string.Format(" remembering _wasTracking: {0}", _wasTracking);
                    #endregion
                }
                #region debug
                _instance.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
                #endregion
            }

            public void RemoveMover()
            {
                long current = Interlocked.Read(ref _axisMovers);
                #region debug
                string dbg = string.Format("TrackingRestorer:RemoveMover:  current: {0}", current);
                #endregion
                if (current > 0)
                {
                    current = Interlocked.Decrement(ref _axisMovers);
                    if (current == 0 && _savedTrackingState)
                    {
                        _instance.Tracking = _wasTracking;
                        #region debug
                        dbg += string.Format(" restored Tracking to {0}", _wasTracking);
                        #endregion
                    }
                }
                #region debug
                _instance.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
                #endregion
            }
        };
        TrackingRestorer _trackingRestorer;
        #endregion

        private List<WiseVirtualMotor> directionMotors, allMotors;
        public Dictionary<TelescopeAxes, List<WiseVirtualMotor>> axisMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        public WisePin TrackPin;
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackingMotor;

        private bool _bypassCoordinatesSafety = false;
        private bool _syncingDomePosition = false;

        private bool _atPark;
        private bool _movingToSafety = false;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Angle _targetRightAscension;
        private Angle _targetDeclination;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly List<TelescopeAxes> axes = new List<TelescopeAxes> { TelescopeAxes.axisPrimary, TelescopeAxes.axisSecondary };

        private object _primaryValuesLock = new Object(), _secondaryValuesLock = new Object();

        public object _primaryEncoderLock = new object(), _secondaryEncoderLock = new object();

        private static WiseSite wisesite = WiseSite.Instance;

        private ReadyToSlewFlags readyToSlew = ReadyToSlewFlags.Instance;

        System.Threading.Timer trackingTimer;
        const int trackingDomeAdjustmentInterval = 30 * 1000;   // half a minute

        /// <summary>
        /// Usually two or three tasks are used to perform a slew:
        /// - if the dome is slaved, a dome slewer
        /// - an axisPrimary slewer
        /// - an axisSecondary slewer
        /// 
        /// An asynchronous slew just fires the tasks.
        /// A synchronous slew waits on the whole list to complete.
        /// </summary>
        public struct SlewerTask
        {
            public Slewers.Type type;
            public Task task;

            public override string ToString()
            {
                return type.ToString();
            }
        }

        private CancellationTokenSource telescopeCTS = new CancellationTokenSource();
        private CancellationToken telescopeCT;

        private CancellationTokenSource domeCTS = new CancellationTokenSource();
        private CancellationToken domeCT;

        public Slewers slewers = Slewers.Instance;
        public Pulsing pulsing = Pulsing.Instance;

        private AxisMonitor primaryStatusMonitor, secondaryStatusMonitor;
        Dictionary<TelescopeAxes, AxisMonitor> axisStatusMonitors;

        public double _lastTrackingLST;

        public static Dictionary<Const.AxisDirection, string> axisPrimaryNames = new Dictionary<Const.AxisDirection, string>()
            {
                { Const.AxisDirection.Increasing, "East" },
                { Const.AxisDirection.Decreasing, "West" },
            }, axisSecondaryNames = new Dictionary<Const.AxisDirection, string>()
            {
                { Const.AxisDirection.Increasing, "North" },
                { Const.AxisDirection.Decreasing, "South" },
            };

        public static Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>> axisDirectionName = new Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>>()
            {
                { TelescopeAxes.axisPrimary, axisPrimaryNames },
                { TelescopeAxes.axisSecondary, axisSecondaryNames },
            };

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        internal static string driverID = Const.wiseTelescopeDriverID;
        internal static string astrometricAccuracyProfileName = "Astrometric accuracy";
        internal static string traceStateProfileName = "Tracing";
        internal static string bypassCoordinatesSafetyProfileName = "BypassCoordinatesSafety";

        public class MovementParameters
        {
            public Angle minimalMovement;
            public Angle stopMovement;
        };

        public class Movement
        {
            public Const.AxisDirection direction;
            public double rate;
            public Angle start;
            public Angle target;            // Where we finally want to get, through all the speed rates.
            public string taskName;
            public TelescopeAxes axis;
        };

        public Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>> movementParameters, realMovementParameters, simulatedMovementParameters;
        public Dictionary<TelescopeAxes, Movement> prevMovement;         // remembers data about the previous axes movement, specifically the direction
        public Dictionary<TelescopeAxes, Movement> currMovement;         // the current axes movement

        public MovementDictionary movementDict;

        public SafetyMonitorTimer safetyMonitorTimer;

        public bool _enslaveDome = false;
        public double _minimalDomeTrackingMovement;
        private DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;

        public bool _calculateRefraction = false;
        private string minimalDomeTrackingMovementProfileName = "Minimal Dome Tracking Movement";
        
        private static WiseComputerControl wisecomputercontrol = WiseComputerControl.Instance;

        public static string RateName(double rate)
        {
            Dictionary<double, string> names = new Dictionary<double, string> {
                { Const.rateStopped,  "rateStopped" },
                { Const.rateSlew,  "rateSlew" },
                { Const.rateSet,  "rateSet" },
                { Const.rateGuide,  "rateGuide" },
                { -Const.rateSlew,  "-rateSlew" },
                { -Const.rateSet,  "-rateSet" },
                { -Const.rateGuide,  "-rateGuide" },
                { Const. rateTrack, "rateTrack" },
            };

            if (names.ContainsKey(rate))
                return names[rate];
            return rate.ToString();
        }

        public void CheckCoordinateSanity(Angle.Type type, double value)
        {
            if (type == Angle.Type.Dec && (value < -90.0 || value > 90.0))
                throw new InvalidValueException(string.Format("Invalid Declination {0}. Must be between -90 and 90",
                    Angle.FromDegrees(value).ToNiceString()));

            if (type == Angle.Type.RA && (value < 0.0 || value > 24.0))
                throw new ASCOM.InvalidValueException(string.Format("Invalid RightAscension {0}. Must be between 0 to 24",
                    Angle.FromHours(value).ToNiceString()));
        }

        public double TargetDeclination
        {
            get
            {
                if (_targetDeclination == null)
                    throw new ValueNotSetException("Target not set");
                #region trace
                traceLogger.LogMessage("TargetDeclination Get", string.Format("{0}", _targetDeclination));
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Get - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
                #endregion debug
                return _targetDeclination.Degrees;
            }

            set
            {
                inactivityMonitor.Start("TargetDeclination was set");
                CheckCoordinateSanity(Angle.Type.Dec, value);

                _targetDeclination = Angle.FromDegrees(value, Angle.Type.Dec);
                #region trace
                traceLogger.LogMessage("TargetDeclination Set", string.Format("{0}", _targetDeclination));
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Set - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
                #endregion debug
            }
        }

        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == null)
                    throw new ValueNotSetException("Target RA not set");

                Angle ret = _targetRightAscension;

                #region trace
                traceLogger.LogMessage("TargetRightAscension Get", string.Format("{0}", _targetRightAscension));
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Get - {0} ({1})", ret, ret.Hours));
                #endregion debug

                return _targetRightAscension.Hours;
            }

            set
            {
                inactivityMonitor.Start("TargetRightAscension was set");
                CheckCoordinateSanity(Angle.Type.RA, value);
                _targetRightAscension = Angle.FromHours(value, Angle.Type.RA);
                #region trace
                traceLogger.LogMessage("TargetRightAscension Set", string.Format("{0}", _targetRightAscension));
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Set - {0} ({1})",
                    _targetRightAscension,
                    _targetRightAscension.Hours));
                #endregion debug
            }
        }

        public double ApertureDiameter
        {
            get
            {
                return mainMirrorDiam;
            }
        }

        public double ApertureArea
        {
            get
            {
                return Math.PI * Math.Pow(ApertureDiameter, 2);
            }
        }

        public bool doesRefraction
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("DoesRefraction Get", ret.ToString());
                #endregion
                return ret;
            }

            set
            {
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction");
            }
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
            _targetRightAscension = null;
            _targetDeclination = null;
        }

        public void Connect(bool connected)
        {
            foreach (var connectable in connectables)
            {
                connectable.Connect(connected);
            }
            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                #region trace
                traceLogger.LogMessage("Connected Get", _connected.ToString());
                #endregion
                return _connected;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("Connected Set", value.ToString());
                #endregion
                if (value == true)
                    inactivityMonitor.Start("Connected was set to true");

                if (value == _connected)
                    return;

                if (_enslaveDome && connectables.Find(x => x.Equals(domeSlaveDriver)) == null)
                    connectables.Add(domeSlaveDriver);

                foreach (var connectable in connectables)
                {
                    connectable.Connect(value);
                }
                _connected = value;
            }
        }

        private static volatile WiseTele _instance; // Singleton
        private static object syncObject = new object();
        private static bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseTele()
        {
        }

        public WiseTele()
        {
        }

        public static WiseTele Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseTele();
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "WiseTele";

            WisePin SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pins

            ReadProfile();
            debugger.init();
            traceLogger = new TraceLogger("", "Tele");
            traceLogger.Enabled = debugger.Tracing;
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            hardware.init();
            wisesite.init();
            wisecomputercontrol.init();

            _trackingRestorer = new TrackingRestorer();
            inactivityMonitor = new InactivityMonitor();

            switch (wisesite.OperationalMode)
            {
                case WiseSite.OpMode.LCO:
                    _enslaveDome = true;
                    _calculateRefraction = true;
                    break;

                case WiseSite.OpMode.WISE:
                    _enslaveDome = true;
                    _calculateRefraction = true;

                    break;
                case WiseSite.OpMode.ACP:
                    _enslaveDome = false;
                    _calculateRefraction = true;
                    break;
            }

            #region MotorDefinitions
            //
            // Define motors-related hardware (pins and encoders)
            //
            try
            {
                _instance.connectables = new List<IConnectable>();
                _instance.disposables = new List<IDisposable>();

                NorthPin = new WisePin("TeleNorth", hardware.teleboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalOut);
                EastPin = new WisePin("TeleEast", hardware.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut);
                WestPin = new WisePin("TeleWest", hardware.teleboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalOut);
                SouthPin = new WisePin("TeleSouth", hardware.teleboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalOut);

                SlewPin = new WisePin("TeleSlew", hardware.teleboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut);
                TrackPin = new WisePin("TeleTrack", hardware.teleboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);

                NorthGuidePin = new WisePin("TeleNorthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 0, DigitalPortDirection.DigitalOut);
                EastGuidePin = new WisePin("TeleEastGuide", hardware.teleboard, DigitalPortType.FirstPortB, 1, DigitalPortDirection.DigitalOut);
                WestGuidePin = new WisePin("TeleWestGuide", hardware.teleboard, DigitalPortType.FirstPortB, 2, DigitalPortDirection.DigitalOut);
                SouthGuidePin = new WisePin("TeleSouthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 3, DigitalPortDirection.DigitalOut);


                _instance.DecEncoder = new WiseDecEncoder("TeleDecEncoder");
                _instance.HAEncoder = new WiseHAEncoder("TeleHAEncoder", _instance.DecEncoder);
            }
            catch (WiseException e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseTele constructor caught: {0}.", e.Message);
                #endregion debug
            }

            //
            // Define motors-related software interfaces (WiseVirtualMotor)
            //
            _instance.NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing, new List<object> { _instance.DecEncoder });

            _instance.SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing, new List<object> { _instance.DecEncoder });

            _instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { _instance.HAEncoder });

            _instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { _instance.HAEncoder });

            _instance.TrackingMotor = new WiseVirtualMotor("TrackMotor", TrackPin, null, null,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { _instance.HAEncoder });
            if (TrackPin.isOn)
                _instance.TrackingMotor.SetOn(Const.rateTrack);
            else
                _instance.TrackingMotor.SetOff();

            //
            // Define motor groups
            //
            _instance.axisMotors = new Dictionary<TelescopeAxes, List<WiseVirtualMotor>>();
            _instance.axisMotors[TelescopeAxes.axisPrimary] = new List<WiseVirtualMotor> { _instance.EastMotor, _instance.WestMotor };
            _instance.axisMotors[TelescopeAxes.axisSecondary] = new List<WiseVirtualMotor> { _instance.NorthMotor, _instance.SouthMotor };

            _instance.directionMotors = new List<WiseVirtualMotor>();
            _instance.directionMotors.AddRange(_instance.axisMotors[TelescopeAxes.axisPrimary]);
            _instance.directionMotors.AddRange(_instance.axisMotors[TelescopeAxes.axisSecondary]);

            _instance.allMotors = new List<WiseVirtualMotor>();
            _instance.allMotors.AddRange(_instance.directionMotors);
            _instance.allMotors.Add(TrackingMotor);

            List<WiseObject> hardware_elements = new List<WiseObject>();
            hardware_elements.AddRange(_instance.allMotors);
            hardware_elements.Add(_instance.HAEncoder);
            hardware_elements.Add(_instance.DecEncoder);
            #endregion

            safetyMonitorTimer = new SafetyMonitorTimer();
            SyncDomePosition = false;

            #region realMovementParameters
            _instance.realMovementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>();

            _instance.realMovementParameters[TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>();
            _instance.realMovementParameters[TelescopeAxes.axisPrimary][Const.rateSlew] = new MovementParameters()
            {
                minimalMovement = new Angle("00h02m00.0s"),
                stopMovement = new Angle("00h16m00.0s"),
            };

            _instance.realMovementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:05.0")),
                stopMovement = new Angle("00h00m02.0s"),
            };

            _instance.realMovementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                stopMovement = new Angle("00h00m00.04s"),
            };

            _instance.realMovementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            _instance.realMovementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                minimalMovement = new Angle("00:30:00.0"),
                stopMovement = new Angle("04:00:00.0"),
            };

            _instance.realMovementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                minimalMovement = new Angle("00:01:00.0"),
                stopMovement = new Angle("00:00:15.0"),
            };

            _instance.realMovementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                minimalMovement = new Angle("00:00:01.0"),
                stopMovement = new Angle("00:00:00.5"),
            };
            #endregion

            #region simulatedMovementParameters
            _instance.simulatedMovementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>();

            _instance.simulatedMovementParameters[TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>();
            _instance.simulatedMovementParameters[TelescopeAxes.axisPrimary][Const.rateSlew] = new MovementParameters()
            {
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("01:00:00.0")),
                stopMovement = new Angle("00h01m00.0s"),
            };

            _instance.simulatedMovementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                stopMovement = new Angle("00h00m01.0s"),
            };

            _instance.simulatedMovementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                stopMovement = new Angle("00h00m01.0s"),
            };

            _instance.simulatedMovementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            _instance.simulatedMovementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                minimalMovement = new Angle("01:00:00.0"),
                stopMovement = new Angle("00:01:00.0"),
            };

            _instance.simulatedMovementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                minimalMovement = new Angle("00:00:01.0"),
                stopMovement = new Angle("00:00:01.0"),
            };

            _instance.simulatedMovementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                minimalMovement = new Angle("00:00:01.0"),
                stopMovement = new Angle("00:00:01.0"),
            };
            #endregion

            _instance.movementParameters = Simulated ?
                simulatedMovementParameters :
                realMovementParameters;

            // prevMovement remembers the previous movement, so we can detect change-of-direction
            _instance.prevMovement = new Dictionary<TelescopeAxes, Movement>();
            _instance.prevMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            _instance.prevMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };

            // currMovement contains the current telescope-movement parameters
            _instance.currMovement = new Dictionary<TelescopeAxes, Movement>();
            _instance.currMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            _instance.currMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };

            _instance.movementDict = new MovementDictionary();
            _instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { WestMotor });
            _instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { EastMotor });
            _instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { NorthMotor });
            _instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { SouthMotor });

            primaryStatusMonitor = new AxisMonitor(TelescopeAxes.axisPrimary);
            secondaryStatusMonitor = new AxisMonitor(TelescopeAxes.axisSecondary);
            axisStatusMonitors = new Dictionary<TelescopeAxes, AxisMonitor>()
            {
                { TelescopeAxes.axisPrimary, primaryStatusMonitor },
                { TelescopeAxes.axisSecondary, secondaryStatusMonitor },
            };

            _instance.connectables.Add(_instance.NorthMotor);
            _instance.connectables.Add(_instance.EastMotor);
            _instance.connectables.Add(_instance.WestMotor);
            _instance.connectables.Add(_instance.SouthMotor);
            _instance.connectables.Add(_instance.TrackingMotor);
            _instance.connectables.Add(_instance.HAEncoder);
            _instance.connectables.Add(_instance.DecEncoder);
            _instance.connectables.Add(_instance.primaryStatusMonitor);
            _instance.connectables.Add(_instance.secondaryStatusMonitor);

            _instance.disposables.Add(_instance.NorthMotor);
            _instance.disposables.Add(_instance.EastMotor);
            _instance.disposables.Add(_instance.WestMotor);
            _instance.disposables.Add(_instance.SouthMotor);
            _instance.disposables.Add(_instance.TrackingMotor);
            _instance.disposables.Add(_instance.HAEncoder);
            _instance.disposables.Add(_instance.DecEncoder);

            SlewPin.SetOff();
            _instance.TrackingMotor.SetOff();
            _instance.NorthMotor.SetOff();
            _instance.EastMotor.SetOff();
            _instance.WestMotor.SetOff();
            _instance.SouthMotor.SetOff();

            _instance.domeSlaveDriver.init();
            _instance.connectables.Add(_instance.domeSlaveDriver);

            pulsing.Init();

            _initialized = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele init() done.");
            #endregion debug
        }

        public double FocalLength
        {
            get
            {
                double ret = 7.112;  // from Las Campanas 40" (meters)

                #region trace
                traceLogger.LogMessage("FocalLength Get", ret.ToString());
                #endregion
                return ret;
            }
        }

        public void AbortSlew()
        {
            inactivityMonitor.Start("AbortSlew");
            if (AtPark)
            {
                //#region debug
                //debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Got AbortSlew while AtPark, not throwing InvalidOperationException !!!");
                //#endregion
                throw new InvalidOperationException("Cannot AbortSlew while AtPark");
            }

            Stop();
            inactivityMonitor.EndActivity(InactivityMonitor.Activity.Slewing);
            #region trace
            traceLogger.LogMessage("AbortSlew", "");
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "AbortSlew");
            #endregion debug
        }

        public double RightAscension
        {
            get
            {
                var ret = HAEncoder.RightAscension;

                #region trace
                traceLogger.LogMessage("RightAscension", string.Format("Get - {0} ({1})", ret, ret.Hours));
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("RightAscension Get - {0} ({1})", ret, ret.Hours));
                #endregion debug

                return ret.Hours;
            }
        }

        public double HourAngle
        {
            get
            {
                return astroutils.ConditionHA(HAEncoder.Angle.Hours);
            }
        }

        public double Declination
        {
            get
            {
                var ret = Angle.FromDegrees(DecEncoder.Declination);

                #region trace
                traceLogger.LogMessage("Declination", string.Format("Get - {0} ({1})", ret, ret.Degrees));
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Declination Get - {0} ({1})", ret, ret.Degrees));
                #endregion debug
                return ret.Degrees;
            }
        }

        public double Azimuth
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0;

                wisesite.prepareRefractionData(_calculateRefraction);
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                #region trace
                traceLogger.LogMessage("Azimuth Get", az.ToString());
                #endregion
                return az;
            }
        }

        public double Altitude
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0, alt;

                wisesite.prepareRefractionData(_calculateRefraction);
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                alt = 90.0 - zd;
                #region trace
                traceLogger.LogMessage("Altitude Get", alt.ToString());
                #endregion
                return alt;
            }
        }

        private bool SyncDomePosition
        {
            get
            {
                return _syncingDomePosition;
            }

            set
            {
                if (!_enslaveDome)
                    return;

                if (trackingTimer == null)
                    trackingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(AdjustDomePositionWhileTracking));

                if (value == true)
                {
                    trackingTimer.Change(trackingDomeAdjustmentInterval, trackingDomeAdjustmentInterval);
                }
                else
                {
                    trackingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                _syncingDomePosition = value;
            }
        }

        private void AdjustDomePositionWhileTracking(object StateObject)
        {
            if (!Tracking)
            {
                SyncDomePosition = false;
                return;
            }

            if (_enslaveDome && !slewers.Active(Slewers.Type.Dome))
                DomeSlewer(Angle.FromHours(RightAscension), Angle.FromDegrees(Declination));
        }

        public bool Tracking
        {
            get
            {
                bool ret = TrackingMotor.isOn;

                #region trace
                traceLogger.LogMessage("Tracking", "Get - " + ret.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Get - {0}", ret));
                #endregion
                return ret;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("Tracking Set", value.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Set - {0}", value));
                #endregion

                if (value)
                {
                    if (!wisecomputercontrol.IsSafe && !BypassCoordinatesSafety)
                        throw new ASCOM.InvalidOperationException(wisecomputercontrol.UnsafeReasons());

                    _lastTrackingLST = wisesite.LocalSiderealTime.Hours;

                    if (TrackingMotor.isOff)
                        TrackingMotor.SetOn(Const.rateTrack);
                    inactivityMonitor.StartActivity(InactivityMonitor.Activity.Tracking);
                }
                else
                {
                    if (TrackingMotor.isOn)
                        TrackingMotor.SetOff();
                    inactivityMonitor.EndActivity(InactivityMonitor.Activity.Tracking);
                }
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Backoff);

                SyncDomePosition = value;
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                var rates = DriveRates.driveSidereal;

                #region trace
                traceLogger.LogMessage("TrackingRate Get - ", rates.ToString());
                #endregion
                return rates;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("TrackingRate Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
            }
        }

        /// <summary>
        /// Stop all directional motors that are currently working.
        /// Does not affect tracking.
        /// </summary>
        public void Stop()
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - started");
            #endregion

            if (Slewing)
            {
                try
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Canceling telescopeCT: #{0}", telescopeCT.GetHashCode());
                    #endregion
                    telescopeCTS.Cancel();
                    telescopeCTS = new CancellationTokenSource();
                }
                catch (AggregateException ax)
                {
                    ax.Handle((Func<Exception, bool>)((ex) =>
                    {
                        #region debug
                        debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                            "Stop: telescope slewing cancellation got {0}", ex.Message);
                        #endregion debug
                        if (ex is ObjectDisposedException)
                            return true;
                        return false;
                    }));
                }

                if (_enslaveDome)
                {
                    try
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Calling DomeStopper");
                        #endregion
                        DomeStopper();
                    }
                    catch (AggregateException ax)
                    {
                        ax.Handle((Func<Exception, bool>)((ex) =>
                        {
                            #region debug
                            debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                                "Stop: dome slewing cancellation got {0}", ex.Message);
                            #endregion debug
                            if (ex is ObjectDisposedException)
                                return true;
                            return false;
                        }));
                    }
                }
            }

            foreach (WiseVirtualMotor motor in allMotors)
                if (motor.isOn)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Stopping {0}", motor.Name);
                    #endregion
                    motor.SetOff();
                }

            safetyMonitorTimer.DisableIfNotNeeded();
        }

        public void AbortPulseGuiding()
        {
            pulsing.Abort();
        }

        public void FullStop()
        {
            Stop();
            if (IsPulseGuiding)
                AbortPulseGuiding();
            Tracking = false;

            foreach (WiseVirtualMotor motor in allMotors)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:FullStop - Stopping {0}", motor.Name);
                #endregion
                motor.SetOff(); // ForceOff
            }
        }

        public bool AxisIsMoving(TelescopeAxes axis)
        {
            bool ret = false;

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    ret = primaryStatusMonitor.IsMoving;
                    break;
                case TelescopeAxes.axisSecondary:
                    ret = secondaryStatusMonitor.IsMoving;
                    break;
            }

            return ret;
        }

        public bool DirectionMotorsAreActive
        {
            get
            {
                foreach (WiseVirtualMotor m in _instance.directionMotors)
                    if (m.isOn)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Implements ITelescopeV3.Slewing Property.
        /// True ONLY during SlewXXX and MoveAxis methods.
        /// </summary>
        public bool Slewing
        {
            get
            {
                bool ret = slewers.Count > 0 ||
                    (!IsPulseGuiding && DirectionMotorsAreActive) ||
                    _movingToSafety;                // triggered by SafeAtCoordinates()

                #region trace
                traceLogger.LogMessage("Slewing Get", ret.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Slewing Get - {0}", ret));
                #endregion debug
                return ret;
            }
        }

        public double DeclinationRate
        {
            get
            {
                double decRate = 0.0;

                #region trace
                traceLogger.LogMessage("DeclinationRate", "Get - " + decRate.ToString());
                #endregion
                return decRate;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("DeclinationRate Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public void HandpadMoveAxis(TelescopeAxes Axis, double Rate)
        {
            #region trace
            traceLogger.LogMessage("HandpadMoveAxis", string.Format("HandpadMoveAxis({0}, {1})", Axis, Rate));
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("HandpadMoveAxis({0}, {1})", Axis, Rate));
            #endregion debug

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None :
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            inactivityMonitor.StartActivity(InactivityMonitor.Activity.Handpad);
            _moveAxis(Axis, Rate, direction, false);

            if (! BypassCoordinatesSafety)
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.StopMotors);
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            #region trace
            traceLogger.LogMessage("MoveAxis", string.Format("MoveAxis({0}, {1})", Axis, Rate));
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("MoveAxis({0}, {1})", Axis, Rate));
            #endregion debug

            if (!wisecomputercontrol.IsSafe && !BypassCoordinatesSafety)
                throw new ASCOM.InvalidOperationException(wisecomputercontrol.UnsafeReasons());

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None :
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            _moveAxis(Axis, Rate, direction, true);
        }

        private void StopAxis(TelescopeAxes axis)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): called", axis);
            #endregion debug

            // Stop any motors that may be On
            foreach (WiseVirtualMotor m in _instance.axisMotors[axis])
                if (m.isOn)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "StopAxis({0}):  {1} was on, stopping it.", axis, m.Name);
                    #endregion debug
                    m.SetOff();
                }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): done.", axis);
            #endregion debug
        }

        /// <summary>
        /// Tries to start/stop the relevant motors.
        /// </summary>
        /// <param name="Axis"></param>
        /// <param name="Rate"></param>
        /// <param name="direction"></param>
        /// <param name="stopTracking"></param>
        /// <returns>The required action was performed.</returns>
        private bool _moveAxis(
            TelescopeAxes Axis,
            double Rate,
            Const.AxisDirection direction = Const.AxisDirection.None,
            bool stopTracking = false)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): called", Axis, RateName(Rate));
            #endregion debug

            MovementWorker mover = null;
            TelescopeAxes thisAxis = Axis;
            TelescopeAxes otherAxis = (thisAxis == TelescopeAxes.axisPrimary) ? TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;

            if (thisAxis == TelescopeAxes.axisTertiary)
                throw new InvalidValueException("Cannot move in axisTertiary");

            if (AtPark)
            {
                Instance.currMovement[Axis].rate = Const.rateStopped;
                throw new InvalidValueException("Cannot MoveAxis while AtPark");
            }

            if (!wisecomputercontrol.IsSafe)
                throw new InvalidOperationException(wisecomputercontrol.UnsafeReasons());

            if (Rate == Const.rateStopped)
            {
                StopAxisAndWaitForHalt(thisAxis);
                safetyMonitorTimer.DisableIfNotNeeded();
                _trackingRestorer.RemoveMover();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): done.",
                    Axis, RateName(Rate));
                #endregion
                return true;
            }

            double absRate = Math.Abs(Rate);
            if (!((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                throw new InvalidValueException(string.Format("_moveAxis({0}, {1}): Invalid rate.", Axis, Rate));

            if (!axisStatusMonitors[Axis].CanMoveAtRate(absRate))
            {
                string msg = string.Format("Cannot _moveAxis({0}, {1}) ({2}) while {3} is moving at {4}",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], otherAxis, RateName(currMovement[otherAxis].rate));

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, msg);
                #endregion debug
                return false;
            }

            try
            {
                mover = movementDict[new MovementSpecifier(Axis, direction)];
            }
            catch (Exception e)
            {
                string msg = string.Format("Don't know how to _moveAxis({0}, {1}) (no mover) ({2}) [{3}]",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], e.Message);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, msg);
                #endregion debug
                return false;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): direction: {2}, stopTracking: {3}",
                Axis, RateName(Rate), axisDirectionName[Axis][direction], stopTracking);
            #endregion debug

            if (stopTracking)
            {
                _instance._trackingRestorer.AddMover();
                Tracking = false;
            }

            Angle currPosition = (Axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(_instance.RightAscension, Angle.Type.RA) :
                Angle.FromDegrees(_instance.Declination, Angle.Type.Dec);

            foreach (WiseVirtualMotor m in mover.motors)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "_moveAxis({0}, {1}): currPosition: {2}, starting {3}", Axis, RateName(Rate), currPosition, m.Name);
                #endregion debug
                m.SetOn(Rate);
            }

            if (! BypassCoordinatesSafety)
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Backoff);

            return true;
        }

        public bool IsPulseGuiding
        {
            get
            {
                bool ret = pulsing.Active();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "IsPulseGuiding: {0}", ret);
                #endregion
                return ret;
            }
        }

        public void SlewToTargetAsync()
        {
            if (_targetRightAscension == null)
                throw new ValueNotSetException("Target RA not set");
            if (_targetDeclination == null)
                throw new ValueNotSetException("Target Dec not set");

            Angle ra = Angle.FromHours(_instance.TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(_instance.TargetDeclination, Angle.Type.Dec);

            #region trace
            traceLogger.LogMessage("SlewToTargetAsync", string.Format("Started: ra: {0}, dec: {1}", ra, dec));
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTargetAsync({0}, {1})", ra, dec));
            #endregion debug

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while NOT Tracking");

            string notSafe = SafeAtCoordinates(ra, dec);
            if (notSafe != string.Empty)
                throw new InvalidOperationException(notSafe);

            if (!wisecomputercontrol.IsSafe)
                throw new InvalidOperationException(wisecomputercontrol.UnsafeReasons());
            
            _doSlewToCoordinatesAsync(_targetRightAscension, _targetDeclination);
        }

        /// <summary>
        /// Check whether it's safe at .5 degrees in the specified direction 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns>Safe or not-safe.</returns>
        public bool SafeToMove(List<Const.CardinalDirection> directions)
        {
            double ra = RightAscension;
            double dec = Declination;
            double delta = 0.5;

            foreach (var dir in directions) {
                switch (dir)
                {
                    case Const.CardinalDirection.North:
                        dec += delta;
                        break;
                    case Const.CardinalDirection.South:
                        dec -= delta;
                        break;
                    case Const.CardinalDirection.East:
                        ra += Angle.Deg2Hours(delta);
                        break;
                    case Const.CardinalDirection.West:
                        ra -= Angle.Deg2Hours(delta);
                        break;
                }
            }
            return SaferAtCoordinates(Angle.FromDegrees(ra, Angle.Type.RA), Angle.FromDegrees(dec, Angle.Type.Dec));
        }

        public bool SaferAtCoordinates(Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;

            wisesite.prepareRefractionData(_calculateRefraction);
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            return Math.Abs(Math.Cos(Angle.Deg2Rad(90.0 - zd))) < Math.Abs(Math.Cos(Angle.Deg2Rad(Altitude)));
        }

        /// <summary>
        /// Checks if we're safe at a given position:  Used:
        ///  - before slewing to check if the scope will be safe at the target coordinates
        ///  - by the safety timer to check that the scope is safe at the current coordinates.
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>

        public string SafeAtCoordinates(Angle ra, Angle dec)
        {
            if (BypassCoordinatesSafety)
                return string.Empty;

            Angle altLimit = new Angle(16.0, Angle.Type.Alt);
            Angle haLimit = Angle.FromHours(7.0, Angle.Type.HA);
            Angle lower_decLimit = Angle.FromDegrees(-35.0, Angle.Type.Dec);
            Angle upper_decLimit = Angle.FromDegrees(89.9, Angle.Type.Dec);

            double rar = 0, decr = 0, az = 0, zd = 0; 
            List<string> reasons = new List<string>();

            wisesite.prepareRefractionData(_calculateRefraction);
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            Angle alt = Angle.FromDegrees(90.0 - zd, Angle.Type.Alt);
            if (alt < altLimit)
                reasons.Add(string.Format("Altitude too low: {0} < {1}", alt.ToNiceString(), altLimit.ToNiceString()));

            if (dec > upper_decLimit)
                reasons.Add(string.Format("Declination too high: {0} > {1}", dec.ToNiceString(), upper_decLimit.ToNiceString()));
            if (dec < lower_decLimit)
                reasons.Add(string.Format("Declination too low: {0} < {1}", dec.ToNiceString(), lower_decLimit.ToNiceString()));

            //double ha = astroutils.ConditionHA((wisesite.LocalSiderealTime - ra).Hours);
            double ha = _instance.HourAngle;
            if (Math.Abs(ha) > haLimit.Hours)
                reasons.Add(string.Format("HourAngle too high: Abs({0}) > {1}", ha, haLimit.ToNiceString()));

            if (reasons.Count > 0)
            {
                string msg = string.Format("SafeAtCoordinates(ra: {0}, dec: {1}) - ",
                    ra.ToString(), dec.ToString()) + String.Join(", ", reasons.ToArray());
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, msg);
                #endregion
                return msg;
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Checks what motors are on and moves the scope away from danger.
        /// </summary>
        public void Backoff()
        {
            List<WiseVirtualMotor> wereActive = new List<WiseVirtualMotor>();

            // Remember which motors were active when we became unsafe
            foreach (var m in _instance.directionMotors)
                if (m.isOn)
                {
                    wereActive.Add(m);
                    m.SetOff();
                }

            if (_instance.TrackingMotor.isOn)
            {
                Tracking = false;
                if (wereActive.Find((x) => x.Name == "EastMotor") == null)
                    wereActive.Add(_instance.TrackingMotor);
            }
            Stop();

            _movingToSafety = true;
            foreach (var m in wereActive)
            {
                switch (m.Name)
                {
                    case "EastMotor":
                    case "TrackingMotor":
                        MoveAxis(TelescopeAxes.axisPrimary, -Const.rateSlew);
                        break;
                    case "WestMotor":
                        MoveAxis(TelescopeAxes.axisPrimary, Const.rateSlew);
                        break;
                    case "NorthMotor":
                        MoveAxis(TelescopeAxes.axisSecondary, -Const.rateSlew);
                        break;
                    case "SouthMotor":
                        MoveAxis(TelescopeAxes.axisSecondary, Const.rateSlew);
                        break;
                }
            }
            Thread.Sleep(1000);
            _movingToSafety = false;
        }

        public bool AtPark
        {
            get
            {
                bool ret = _atPark;

                #region trace
                traceLogger.LogMessage("AtPark", "Get - " + ret.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtPark Get - {0}", ret));
                #endregion debug

                return ret;
            }

            set
            {
                _atPark = value;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtPark Set - {0}", _atPark.ToString()));
                #endregion debug
            }
        }

        //
        // This is the Synchronous version, as mandated by ASCOM
        //
        public void Park()
        {
            #region trace
            traceLogger.LogMessage("Park", "");
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park");
            #endregion debug
            if (AtPark)
                return;

            Angle ra = wisesite.LocalSiderealTime;
            Angle dec = Angle.FromDegrees(66.0, Angle.Type.Dec);

            bool wasEnslavingDome = _enslaveDome;
            _parking = true;
            if (wasEnslavingDome)
            {
                _enslaveDome = false;
                DomeParker();
            }
            _instance.TargetRightAscension = ra.Hours;
            _instance.TargetDeclination = dec.Degrees;
            Tracking = true;
            _slewToCoordinatesSync(ra, dec);
            if (wasEnslavingDome)
            {
                while (!domeSlaveDriver.AtPark)
                    Thread.Sleep(2000);
            }
            AtPark = true;
            Tracking = false;
            _parking = false;
            _enslaveDome = wasEnslavingDome;
        }

        public void ParkFromGui(bool parkDome)
        {
            #region trace
            traceLogger.LogMessage("Park", "");
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park");
            #endregion debug
            if (AtPark)
                return;

            Angle ra = wisesite.LocalSiderealTime;
            Angle dec = Angle.FromDegrees(66.0, Angle.Type.Dec);

            if (parkDome)
                DomeParker();
            SlewToCoordinatesAsync(ra.Hours, dec.Degrees, false);
        }

        private void _slewToCoordinatesSync(Angle RightAscension, Angle Declination)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "_slewToCoordinatesSync: ({0}, {1}), called.", RightAscension, Declination);
            #endregion debug
            try
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_slewToCoordinatesSync: telescopeCT: #{0}", telescopeCT.GetHashCode());
                #endregion

                Task.Run(() =>
                {
                    _doSlewToCoordinatesAsync(RightAscension, Declination);
                }, telescopeCT);
            }
            catch (AggregateException ae)
            {
                ae.Handle((Func<Exception, bool>)((ex) =>
                {
                    #region debug
                    debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                        "_slewToCoordinatesSync: Caught \"{0}\"", ex.Message);
                    #endregion
                    return false;
                }));
            }

            Thread.Sleep(200);
            while (slewers.Count > 0)
                Thread.Sleep(200);
        }

        private enum ScopeSlewerStatus { Undefined, CloseEnough, ChangedDirection, Canceled };

        private void ScopeSlewer(TelescopeAxes axis, Angle targetAngle)
        {
            Slewers.Type otherSlewer = (axis == TelescopeAxes.axisPrimary) ? Slewers.Type.Dec : Slewers.Type.Ra;
            Grapher grapher = new Grapher(axis,
                axis == TelescopeAxes.axisPrimary ? RightAscension : Declination,
                targetAngle.Degrees);

            _instance.currMovement[axis] = new Movement()
            {
                rate = Const.rateStopped,
                direction = Const.AxisDirection.None
            };
            Movement cm = _instance.currMovement[axis];
            int waitForOtherAxisMillis = 500;

            cm.taskName = (axis == TelescopeAxes.axisPrimary) ? "primarySlewer" : "secondarySlewer";
            cm.target = targetAngle;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0} Slewer started.", cm.taskName);
            #endregion

            ScopeSlewerStatus status;

            foreach (var rate in rates)
            {
                bool done = false;

                readyToSlew.AxisIsReady(axis, rate);
                DateTime startWaiting = DateTime.Now;
                while (!readyToSlew.BothAxesAreReady(rate))
                {
                    //
                    // After 30 seconds, start checking that the other slewer is alive before
                    // waiting for it to be ready.  Just in case the other task dies.
                    // 
                    if (DateTime.Now.Subtract(startWaiting).TotalSeconds > 30 && !slewers.Active(otherSlewer))
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, string.Format("{0} at {1}: The other slewer is not actie.  Stopping !!!",
                            cm.taskName, RateName(rate)));
                        #endregion
                        Stop();
                        return;
                    }

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, string.Format("{0} at {1}: Waiting {2} millis for other axis ...",
                        cm.taskName, RateName(rate), waitForOtherAxisMillis));
                    #endregion
                    #region graph
                    grapher.Record((axis == TelescopeAxes.axisPrimary) ? RightAscension : Declination,
                        string.Format("at {0} waiting {1} millis for other axis", RateName(rate), waitForOtherAxisMillis));
                    #endregion
                    Thread.Sleep(waitForOtherAxisMillis);
                }

                do
                {
                    status = ScopeSlewCloser(axis, rate, grapher);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0} SlewCloser({1}, {2}) => {3}",
                        cm.taskName, axis, RateName(rate), status.ToString());
                    #endregion
                    switch (status)
                    {
                        case ScopeSlewerStatus.CloseEnough:
                        case ScopeSlewerStatus.Canceled:
                            done = true;
                            break;
                        case ScopeSlewerStatus.ChangedDirection:
                            break;  // NOTE: this breaks the switch, not the loop
                    }
                }
                while (!done);
            }
        }

        private void StopAxisAndWaitForHalt(TelescopeAxes axis)
        {
            Movement cm = _instance.currMovement[axis];
            StopAxis(axis);

            #region debug
            Angle a = (axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(_instance.RightAscension) :
                Angle.FromDegrees(_instance.Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0} at {1}: {2} waiting for {3} to stop moving ...",
                cm.taskName, RateName(cm.rate), a, axis);
            #endregion debug
            while (AxisIsMoving(axis))
            {
                Thread.Sleep(500);
            }
            #region debug
            a = (axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(_instance.RightAscension) :
                Angle.FromDegrees(_instance.Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0} at {1}: {2} {3} has stopped moving.",
                cm.taskName, RateName(cm.rate), a, axis);
            #endregion debug
        }

        private ScopeSlewerStatus ScopeSlewCloser(TelescopeAxes axis, double rate, Grapher grapher)
        {
            Movement cm = Instance.currMovement[axis];
            Angle currPosition = new Angle(0.0);
            MovementParameters mp;
            AxisMonitor axisStatus = (axis == TelescopeAxes.axisPrimary) ?
                primaryStatusMonitor : secondaryStatusMonitor;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0}: cm.finalTarget: {1}", cm.taskName, cm.target);
            #endregion debug

            try
            {
                mp = movementParameters[axis][rate];

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "ScopeSlewCloser: telescopeCT (#{0}), IsCancellationRequested: {1}",
                    telescopeCT.GetHashCode(), telescopeCT.IsCancellationRequested.ToString());
                #endregion
                telescopeCT.ThrowIfCancellationRequested();

                cm.start = (axis == TelescopeAxes.axisPrimary) ?
                    Angle.FromHours(_instance.RightAscension, Angle.Type.RA) :
                    Angle.FromDegrees(_instance.Declination, Angle.Type.Dec);

                var shortest = cm.start.ShortestDistance(cm.target);
                cm.direction = shortest.direction;

                Angle minimalMovementAngle = mp.minimalMovement + mp.stopMovement;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "{0} at {1}: minimalMovementAngle: {2}",
                    cm.taskName, RateName(rate), minimalMovementAngle);
                #endregion debug

                if (shortest.angle < minimalMovementAngle)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: Not moving, too short ({2} < {3}, {4} < {5})",
                        cm.taskName, RateName(rate),
                        shortest.angle, minimalMovementAngle,
                        shortest.angle.Degrees, minimalMovementAngle.Degrees);
                    #endregion debug
                    return ScopeSlewerStatus.CloseEnough;
                }

                // Set the axis in motion at the current rate ...
                cm.rate = rate;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "{0} at {1}: cm.start: {2}, cm.target: {3}, shortest.angle: {4}, cm.direction: {5}",
                    cm.taskName, RateName(cm.rate), cm.start, cm.target, shortest.angle, cm.direction);
                #endregion debug
                while (!_moveAxis(axis, cm.rate, cm.direction, false))
                {
                    const int syncMillis = 500;

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "ScopeSlewCloser:cancellation: token: (#{0}) IsCancellationRequested: {1}",
                        telescopeCT.GetHashCode(),
                        telescopeCT.IsCancellationRequested.ToString());
                    #endregion
                    telescopeCT.ThrowIfCancellationRequested();
                    Thread.Sleep(syncMillis);
                }

                // ... and wait for it to arrive at target
                ShortestDistanceResult remainingDistance = null;
                bool closestAtCurrentRate = false;

                while (!closestAtCurrentRate)
                {
                    const int waitMillis = 10;    // TODO: make it configurable or constant

                    #region debug
                    if (telescopeCT.IsCancellationRequested)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "ScopeSlewCloser:cancellation: (#{0}), IsCancellationRequested {1}",
                            telescopeCT.GetHashCode(),
                            telescopeCT.IsCancellationRequested.ToString());
                    }
                    #endregion

                    telescopeCT.ThrowIfCancellationRequested();

                    currPosition = (axis == TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(_instance.RightAscension, Angle.Type.RA) :
                        Angle.FromDegrees(_instance.Declination, Angle.Type.Dec);
                    remainingDistance = currPosition.ShortestDistance(cm.target);

                    if (cm.direction != remainingDistance.direction)
                    {
                        closestAtCurrentRate = true;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                            "{0} at {1}: at {2}, direction changed while moving {3} => {4}, returning ChangedDirection",
                            cm.taskName, RateName(cm.rate), currPosition,
                            cm.direction.ToString(), remainingDistance.direction.ToString()
                            );
                        #endregion
                        StopAxisAndWaitForHalt(axis);
                        //
                        // We have moved as close as possible in the current axis direction.
                        // Let the calling routine call us again to try all the cascading rates in the 
                        //  opposite axis direction.
                        //
                        cm.direction = remainingDistance.direction;
                        return ScopeSlewerStatus.ChangedDirection;
                    }
                    else if (remainingDistance.angle <= mp.stopMovement)
                    {
                        closestAtCurrentRate = true;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                            "{0} at {1}: stopping at {2}, ({3} from cm.target: {4}): closest at this rate: remainingDistance.angle {5} <= mp.stopMovement {6}",
                            cm.taskName, RateName(cm.rate), currPosition,
                            remainingDistance.angle, cm.target,
                            remainingDistance.angle, mp.stopMovement);
                        #endregion
                        #region graph
                        grapher.Record(currPosition.Degrees, string.Format("at {0} - before stopping", RateName(cm.rate)));
                        #endregion
                        StopAxisAndWaitForHalt(axis);

                        Angle angleAfterStopping = (axis == TelescopeAxes.axisPrimary) ?
                            Angle.FromHours(_instance.RightAscension, Angle.Type.RA) :
                            Angle.FromDegrees(_instance.Declination, Angle.Type.Dec);
                        #region graph
                        grapher.Record(angleAfterStopping.Degrees, string.Format("at {0} - after stopping", RateName(cm.rate)));
                        #endregion
                        ShortestDistanceResult statusAfterStopping = angleAfterStopping.ShortestDistance(cm.target);

                        if (statusAfterStopping.direction == cm.direction)
                            continue;   // cascade to the next lower rate
                        else
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                "{0} at {1}: at {2}, direction changed after stopping {3} => {4}, returning ChangedDirection",
                                cm.taskName, RateName(cm.rate), angleAfterStopping,
                                cm.direction.ToString(), statusAfterStopping.direction.ToString()
                                );
                            #endregion
                            cm.direction = statusAfterStopping.direction;
                            return ScopeSlewerStatus.ChangedDirection;
                        }
                    }
                    else
                    {
                        // Not there yet, continue waiting
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                "{0} at {1}: at {2}, moving ==> target: {3}, remaining (Angle: {4}, direction: {5}), stopMovement: ({6}, {7}), sleeping {8} millis, cancellation: (#{9}) {10} ...",
                                cm.taskName, RateName(cm.rate), currPosition,
                                cm.target,
                                remainingDistance.angle, remainingDistance.direction,
                                mp.stopMovement, mp.stopMovement.Radians,
                                waitMillis,
                                telescopeCT.GetHashCode(),
                                telescopeCT.IsCancellationRequested.ToString());
                        #endregion debug
                        #region graph
                        grapher.Record(currPosition.Degrees, string.Format("at {0}", RateName(cm.rate)));
                        #endregion
                        telescopeCT.ThrowIfCancellationRequested();
                        Thread.Sleep(waitMillis);
                        telescopeCT.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    "{0} at {1}: Slew cancelled at {2}", cm.taskName, RateName(cm.rate), currPosition);
                #endregion debug
                StopAxisAndWaitForHalt(axis);
                return ScopeSlewerStatus.Canceled;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: stopping at {2}, ({3} from cm.target: {4}): returning CloseEnough",
                        cm.taskName, RateName(cm.rate), currPosition,
                        currPosition.ShortestDistance(cm.target).angle, cm.target);
            #endregion
            return ScopeSlewerStatus.CloseEnough;
        }

        private static SlewerTask domeSlewer;

        private static void checkDomeActionCancelled(object StateObject)
        {
            if (_instance.domeCT.IsCancellationRequested)
            {
                domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _instance.SyncDomePosition = false;
                _instance.domeSlaveDriver.AbortSlew();
            }
        }

        private static System.Threading.Timer domeSlewTimer;

        private void _genericDomeSlewerTask(Action action)
        {
            domeSlewer = new SlewerTask() { type = Slewers.Type.Dome, task = null };
            domeCT = domeCTS.Token;
            domeSlewTimer = new System.Threading.Timer(new TimerCallback(checkDomeActionCancelled));

            slewers.Add(domeSlewer);
            domeSlewer.task = Task.Run(() =>
                {
                    try
                    {
                        domeSlewTimer.Change(100, 100);
                        action();
                    }
                    catch (OperationCanceledException)
                    {
                        domeSlaveDriver.AbortSlew();
                        domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        slewers.Delete(Slewers.Type.Dome);
                    }
                }, domeCT).ContinueWith((domeSlewerTask) =>
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        "slewer \"{0}\" completed with status: {1}", Slewers.Type.Dome.ToString(), domeSlewerTask.Status.ToString());
                    #endregion
                    domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    slewers.Delete(Slewers.Type.Dome);
                }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void DomeSlewer(Angle ra, Angle dec)
        {
            _genericDomeSlewerTask(() => domeSlaveDriver.SlewToAz(ra, dec));
        }

        public void DomeSlewer(double az)
        {
            _genericDomeSlewerTask(() => domeSlaveDriver.SlewToAz(az));
        }

        public void DomeParker()
        {
            _genericDomeSlewerTask(() => domeSlaveDriver.Park());
        }

        public void DomeCalibrator()
        {
            _genericDomeSlewerTask(() => domeSlaveDriver.FindHome());
        }

        public void DomeStopper()
        {
            domeCTS.Cancel();
            domeCTS = new CancellationTokenSource();
            SyncDomePosition = false;
        }
                      
        private void _doSlewToCoordinatesAsync(Angle RightAscension, Angle Declination)
        {
            CheckCoordinateSanity(Angle.Type.RA, RightAscension.Hours);
            CheckCoordinateSanity(Angle.Type.Dec, Declination.Degrees);

            slewers.Clear();
            readyToSlew.Reset();
            inactivityMonitor.StartActivity(InactivityMonitor.Activity.Slewing);
            try
            {
                if (_instance._enslaveDome)
                {
                    DomeSlewer(RightAscension, Declination);
                }

                telescopeCT = telescopeCTS.Token;
                foreach (Slewers.Type slewerType in new List<Slewers.Type>() { Slewers.Type.Ra, Slewers.Type.Dec })
                {
                    SlewerTask slewer = new SlewerTask() { type = slewerType, task = null };
                    try
                    {
                        TelescopeAxes axis;
                        Angle angle;
                        if (slewerType == Slewers.Type.Ra)
                        {
                            axis = TelescopeAxes.axisPrimary; angle = RightAscension;
                        }
                        else
                        {
                            axis = TelescopeAxes.axisSecondary; angle = Declination;
                        }

                        slewers.Add(slewer);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "Passing (#{0}) to slewer.task", telescopeCT.GetHashCode());
                        #endregion
                        slewer.task = Task.Run(() =>
                        {
                            ScopeSlewer(axis, angle);
                        }, telescopeCT).ContinueWith((slewerTask) =>
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                "slewer \"{0}\" completed with status: {1}", slewer.type.ToString(), slewerTask.Status.ToString());
                            #endregion
                            slewers.Delete(slewerType);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Failed to run slewer {0}: {1}", slewerType.ToString(), ex.Message);
                        #endregion
                        slewers.Delete(slewerType);
                    }
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((Func<Exception, bool>)((ex) =>
                {
                    #region debug
                    debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                        "_slewToCoordinatesAsync: Caught {0}", ex.Message);
                    #endregion
                    return false;
                }));
            }
        }

        public void SlewToCoordinates(double RightAscension, double Declination, bool noSafetyCheck = false)
        {
            _instance.TargetRightAscension = RightAscension;
            _instance.TargetDeclination = Declination;

            Angle ra = Angle.FromHours(_instance.TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(_instance.TargetDeclination, Angle.Type.Dec);

            #region trace
            traceLogger.LogMessage("SlewToCoordinates", string.Format("ra: {0}, dec: {0}", ra, dec));
            #endregion
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("SlewToCoordinates - {0}, {1}", ra, dec));
            #endregion debug

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!noSafetyCheck)
            {
                string notSafe = SafeAtCoordinates(ra, dec);
                if (notSafe != string.Empty)
                    throw new InvalidOperationException(notSafe);
            }

            if (!wisecomputercontrol.IsSafe)
                throw new InvalidOperationException(wisecomputercontrol.UnsafeReasons());

            try
            {
                _slewToCoordinatesSync(ra, dec);
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    "SlewToCoordinates: _slewToCoordinatesSync({0}, {1}) threw exception: {2}",
                    ra, dec, e.Message);
                #endregion
            }
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination, bool doChecks = true)
        {

            CheckCoordinateSanity(Angle.Type.RA, RightAscension);
            CheckCoordinateSanity(Angle.Type.Dec, Declination);

            _instance.TargetRightAscension = RightAscension;
            _instance.TargetDeclination = Declination;

            Angle ra = Angle.FromHours(_instance.TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(_instance.TargetDeclination, Angle.Type.Dec);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "SlewToCoordinatesAsync({0}, {1})", ra, dec);
            #endregion

            if (doChecks)
            {
                if (AtPark)
                    throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

                if (!Tracking)
                    throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

                string notSafe = SafeAtCoordinates(ra, dec);
                if (notSafe != string.Empty)
                    throw new InvalidOperationException(notSafe);
            }

            if (!wisecomputercontrol.IsSafe)
                throw new InvalidOperationException(wisecomputercontrol.UnsafeReasons());

            try
            {
                _doSlewToCoordinatesAsync(ra, dec);
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinatesAsync({0}, {1}) caught exception: {2}",
                    RightAscension, Declination, e.Message);
                #endregion
            }
        }

        public void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
        {
            //if (DecOver90Degrees)
            //{
            //    telescopeCT = telescopeCTS.Token;
            //    Task southScooter = Task.Run(() =>
            //    {
            //        ScootSouth();
            //    }, telescopeCT).ContinueWith((scooter) =>
            //    {
            //        #region debug
            //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
            //            "southScooter completed with status: {0}", scooter.Status.ToString());
            //        #endregion
            //        _doSlewToCoordinatesAsync(RightAscension, Declination);
            //    }, TaskContinuationOptions.ExecuteSynchronously);
            //}
            //else
                _doSlewToCoordinatesAsync(RightAscension, Declination);
        }

        //public void ScootSouth()
        //{
        //    if (!DecOver90Degrees)
        //        return;

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Scooting South from {0}, {1}",
        //        Angle.FromHours(_instance.RightAscension).ToNiceString(),
        //        Angle.FromDegrees(_instance.Declination).ToNiceString());
        //    #endregion
            
        //    double targetRadians = Angle.Deg2Rad(89.5);

        //    _movingToSafety = true;     // make Slewing true

        //    while (true)
        //    {
        //        double remainingRadians = DecEncoder._angle.Radians - targetRadians;
        //        double selectedRate = Const.rateStopped;

        //        // Select the rate at which to move
        //        foreach (var rate in rates)
        //            if (remainingRadians <= _instance.movementParameters[TelescopeAxes.axisSecondary][rate].minimalMovement.Radians) {
        //                selectedRate = rate;
        //                break;
        //            }
        //        if (selectedRate == Const.rateStopped)
        //        {
        //            // Couldn't find a rate at which to move
        //            _movingToSafety = false;
        //            return;
        //        }

        //        // The rate is selected, get moving
        //        _moveAxis(TelescopeAxes.axisSecondary, selectedRate, Const.AxisDirection.Decreasing, false);
        //        MovementParameters mp = _instance.movementParameters[TelescopeAxes.axisSecondary][selectedRate];
        //        while (true)
        //        {
        //            remainingRadians = DecEncoder._angle.Radians - targetRadians;
        //            if (telescopeCT.IsCancellationRequested || (remainingRadians <= 0 || remainingRadians <= mp.stopMovement.Radians))
        //            {
        //                StopAxisAndWaitForHalt(TelescopeAxes.axisSecondary);
        //                _movingToSafety = false;
        //                return;
        //            }
        //            #region debug
        //            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ScootSouth: at {0}, remainingRadians: {1}, sleeping 10 millis ...",
        //                selectedRate.ToString(), remainingRadians);
        //            #endregion
                    
        //            Thread.Sleep(10);
        //        }
        //    }
        //}

        public void Unpark()
        {
            if (AtPark)
                AtPark = false;

            #region trace
            traceLogger.LogMessage("Unpark", "Done");
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Unpark");
            #endregion debug
        }

        public DateTime UTCDate
        {
            get
            {
                DateTime utcDate = DateTime.UtcNow;
                #region trace
                traceLogger.LogMessage("UTCDate Get - ", utcDate.ToString());
                #endregion
                return utcDate;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("UTCDate Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                #region trace
                traceLogger.LogMessage("TrackingRates", "Get - ");
                #endregion
                foreach (DriveRates driveRate in trackingRates)
                {
                    #region trace
                    traceLogger.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                    #endregion
                }
                return trackingRates;
            }
        }

        public void SyncToTarget()
        {
            #region trace
            traceLogger.LogMessage("SyncToTarget", "Not implemented");
            #endregion
            throw new ASCOM.MethodNotImplementedException("SyncToTarget");
        }

        public string Description
        {
            get
            {
                var ret = driverDescription;
                #region trace
                traceLogger.LogMessage("Description Get", ret);
                #endregion
                return ret;
            }
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                AlignmentModes mode = AlignmentModes.algGermanPolar;

                #region trace
                traceLogger.LogMessage("AlignmentMode Get", mode.ToString());
                #endregion
                return mode;
            }
        }

        public double SiderealTime
        {
            get
            {
                double ret = wisesite.LocalSiderealTime.Hours;

                #region trace
                traceLogger.LogMessage("SiderealTime", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public double SiteElevation
        {
            get
            {
                double elevation = wisesite.Elevation;

                #region trace
                traceLogger.LogMessage("SiteElevation Get", elevation.ToString());
                #endregion
                return elevation;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("SiteElevation Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                double latitude = wisesite.Latitude.Degrees;

                #region trace
                traceLogger.LogMessage("SiteLatitude Get", latitude.ToString());
                #endregion
                return latitude;
            }
            set
            {
                #region trace
                traceLogger.LogMessage("SiteLatitude Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
            }
        }

        /// <summary>
        /// Site Longitude in degrees as per ASCOM.DriverAccess
        /// </summary>
        public double SiteLongitude
        {
            get
            {
                double longitude = wisesite.Longitude.Degrees;

                #region trace
                traceLogger.LogMessage("SiteLongitude Get", longitude.ToString());
                #endregion
                return longitude;
            }
            set
            {
                #region trace
                traceLogger.LogMessage("SiteLongitude Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
            }
        }

        public void SlewToTarget()
        {
            Angle ra = Angle.FromHours(_instance.TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(_instance.TargetDeclination, Angle.Type.Dec);

            #region trace
            traceLogger.LogMessage("SlewToTarget", string.Format("ra: {0}, dec: {0}", ra, dec));
            #endregion
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTarget - {0}, {1}", ra, dec));
            #endregion debug

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            string notSafe = SafeAtCoordinates(ra, dec);
            if (notSafe != string.Empty)
                throw new InvalidOperationException(notSafe);

            if (!wisecomputercontrol.IsSafe)
                throw new InvalidOperationException(wisecomputercontrol.UnsafeReasons());

            SlewToCoordinates(_instance.TargetRightAscension, _instance.TargetDeclination); // sync
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            #region trace
            traceLogger.LogMessage("SyncToAltAz", "Not implemented");
            #endregion
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            #region trace
            traceLogger.LogMessage("SyncToCoordinates", "Not implemented");
            #endregion
            throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            bool ret;

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary: ret = true; break;   // Right Ascension
                case TelescopeAxes.axisSecondary: ret = true; break;   // Declination
                case TelescopeAxes.axisTertiary: ret = false; break;  // Image Rotator/Derotator
                default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
            }
            #region trace
            traceLogger.LogMessage("CanMoveAxis", "Get - " + Axis.ToString() + ": " + ret.ToString());
            #endregion

            return ret;
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equJ2000;

                #region trace
                traceLogger.LogMessage("EquatorialSystem", "Get - " + equatorialSystem.ToString());
                #endregion
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            #region trace
            traceLogger.LogMessage("FindHome", "Not Implemented");
            #endregion
            throw new MethodNotImplementedException("FindHome");
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            PierSide ret = PierSide.pierEast;
            #region trace
            traceLogger.LogMessage("DestinationSideOfPier Get", ret.ToString());
            #endregion
            return ret;
        }

        public bool CanPark
        {
            get
            {
                #region trace
                traceLogger.LogMessage("CanPark", "Get - " + true.ToString());
                #endregion
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                bool ret = true;
                #region trace
                traceLogger.LogMessage("CanPulseGuide", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                #region trace
                traceLogger.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                #endregion
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                #region trace
                traceLogger.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                #endregion
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanSetPark", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                #region trace
                traceLogger.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                #endregion
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                #region trace
                traceLogger.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                #endregion
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                bool ret = true;

                #region trace
                traceLogger.LogMessage("CanSetTracking", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSlew
        {
            get
            {
                bool ret = true;

                #region trace
                traceLogger.LogMessage("CanSlew", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanSlewAltAz", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanSlewAltAzAsync", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                bool ret = true;

                #region trace
                traceLogger.LogMessage("CanSlewAsync", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSync
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanSync", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanSyncAltAz", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public bool CanUnpark
        {
            get
            {
                bool ret = true;

                #region trace
                traceLogger.LogMessage("CanUnpark", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                double rate = Const.rateGuide;
                #region trace
                traceLogger.LogMessage("GuideRateDeclination Get", rate.ToString());
                #endregion
                return rate;
            }
            set
            {
                #region trace
                traceLogger.LogMessage("GuideRateDeclination Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination - Set", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                double rate = Const.rateGuide;

                #region trace
                traceLogger.LogMessage("GuideRateRightAscension Get", rate.ToString());
                #endregion
                return rate;
            }
            set
            {
                #region trace
                traceLogger.LogMessage("GuideRateRightAscension Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension - Set", true);
            }
        }

        public bool AtHome
        {
            get
            {
                bool ret = false;       // Homing is not implemented

                #region trace
                traceLogger.LogMessage("AtHome", "Get - " + ret.ToString());
                #endregion
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtHome Get - {0}", ret));
                #endregion debug
                return ret;
            }
        }

        public bool CanFindHome
        {
            get
            {
                bool ret = false;

                #region trace
                traceLogger.LogMessage("CanFindHome", "Get - " + ret.ToString());
                #endregion
                return ret;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            IAxisRates rates = new AxisRates(Axis);

            #region trace
            traceLogger.LogMessage("AxisRates", "Get - " + rates.ToString());
            #endregion
            return rates;
        }

        public short SlewSettleTime
        {
            get
            {
                #region trace
                traceLogger.LogMessage("SlewSettleTime Get", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }

            set
            {
                #region trace
                traceLogger.LogMessage("SlewSettleTime Set", "Not implemented");
                #endregion
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            #region trace
            traceLogger.LogMessage("SlewToAltAz", String.Format("SlewToAltAz({0}, {1})", Azimuth, Altitude));
            #endregion
            throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            #region trace
            traceLogger.LogMessage("SlewToAltAzAsync", String.Format("SlewToAltAzAsync({0}, {1})", Azimuth, Altitude));
            #endregion
            throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                #region trace
                traceLogger.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                #endregion
                return rightAscensionRate;
            }

            set
            {
                #region trace
                traceLogger.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
                #endregion
            }
        }

        public void SetPark()
        {
            #region trace
            traceLogger.LogMessage("SetPark", "Not implemented");
            #endregion
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                PierSide side = PierSide.pierEast;

                #region trace
                traceLogger.LogMessage("SideOfPier Get", side.ToString());
                #endregion
                return side;
            }

            set
            {
                if (value != PierSide.pierEast)
                    throw new InvalidValueException("Only pierEast is valid!");
                #region trace
                traceLogger.LogMessage("SideOfPier Set", string.Format("Silently ignoring {0}", value.ToString()));
                #endregion
            }
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            #region trace
            traceLogger.LogMessage("PulseGuide", string.Format("Direction={0}, Duration={1}", Direction.ToString(), Duration.ToString()));
            #endregion
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "PulseGuide: Direction={0}, Duration={1}", Direction.ToString(), Duration.ToString());
            #endregion
            if (AtPark)
                throw new InvalidOperationException("Cannot PulseGuide while AtPark");

            if (Slewing)
                throw new InvalidOperationException("Cannot PulseGuide while Slewing");

            if (pulsing.Active(Direction))
            {
                throw new InvalidOperationException(string.Format(
                    "Already PulseGuiding on {0}", Pulsing.guideDirection2Axis[Direction].ToString()));
            }

            try
            {
                pulsing.Start(Direction, Duration);
                inactivityMonitor.StartActivity(InactivityMonitor.Activity.Pulsing);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("PulseGuide: Cannot Start({0}, {1}): {2}",
                    Direction.ToString(), Duration.ToString(), ex.Message));
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                #region trace
                traceLogger.LogMessage("SupportedActions Get", "Returning empty arraylist");
                #endregion
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        private void CheckConnected(string message)
        {
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            if (command == "active")
                inactivityMonitor.Start("CommandBlind(\"active\")");
            else
                throw new ASCOM.MethodNotImplementedException(string.Format("CommandBlind: {0}", command));
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            if (command == "active")
                return inactivityMonitor.ObservatoryIsActive();
            else
                throw new ASCOM.MethodNotImplementedException(string.Format("CommandBool {0}", command));
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");

            if (command == "opmode")
                return wisesite.OperationalMode.ToString();
            else
                throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = string.Format("ASCOM Wise40.Telescope v{0}", version.ToString());
                #region trace
                traceLogger.LogMessage("DriverInfo Get", driverInfo);
                #endregion
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {

                string driverVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                #region trace
                traceLogger.LogMessage("DriverVersion Get", driverVersion);
                #endregion
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                #region trace
                traceLogger.LogMessage("InterfaceVersion Get", "3");
                #endregion
                return Convert.ToInt16("3");
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                Accuracy acc;

                if (Enum.TryParse<Accuracy>(driverProfile.GetValue(driverID, astrometricAccuracyProfileName, string.Empty, "Full"), out acc))
                    wisesite.astrometricAccuracy = acc;
                else
                    wisesite.astrometricAccuracy = Accuracy.Full;
                _bypassCoordinatesSafety = Convert.ToBoolean(driverProfile.GetValue(driverID, bypassCoordinatesSafetyProfileName, string.Empty, false.ToString()));
            }

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
                _minimalDomeTrackingMovement = Convert.ToDouble(driverProfile.GetValue(Const.wiseDomeDriverID, minimalDomeTrackingMovementProfileName, string.Empty, "2.0"));
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                driverProfile.WriteValue(driverID, traceStateProfileName, traceLogger.Enabled.ToString());
                driverProfile.WriteValue(driverID, astrometricAccuracyProfileName, wisesite.astrometricAccuracy.ToString());
                driverProfile.WriteValue(driverID, bypassCoordinatesSafetyProfileName, _bypassCoordinatesSafety.ToString());
            }

            using (Profile driverProfile = new Profile() { DeviceType = "Dome" })
                driverProfile.WriteValue(Const.wiseDomeDriverID, minimalDomeTrackingMovementProfileName, _minimalDomeTrackingMovement.ToString());
        }

        public string Status
        {
            get
            {
                string ret = string.Empty;

                if (slewers.Active(Slewers.Type.Dec) || slewers.Active(Slewers.Type.Ra))
                {
                    Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
                    Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);
                    ret = _parking ? "Parking" : "Slewing" + string.Format(" to RA {0} DEC {1}", ra, dec);
                }
                else if (IsPulseGuiding)
                {
                    ret = "PulseGuiding in";
                    if (pulsing.Active(TelescopeAxes.axisPrimary))
                        ret += " RA";
                    if (pulsing.Active(TelescopeAxes.axisSecondary))
                        ret += " DEC";
                }
                return ret;
            }
        }

        public bool BypassCoordinatesSafety
        {
            get
            {
                return _bypassCoordinatesSafety;
            }

            set
            {
                _bypassCoordinatesSafety = value;
            }
        }

        //public bool DecOver90Degrees
        //{
        //    get
        //    {
        //        return DecEncoder.DecOver90Degrees;
        //    }
        //}
    }
}
