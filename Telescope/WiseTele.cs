using System;
using System.Collections;
using System.Collections.Generic;
using ASCOM.Utilities;
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
    public class WiseTele : IDisposable, IConnectable, ISimulated
    {
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public static string driverDescription = "Wise40 Telescope";

        private static NOVAS31 novas31;
        private static Util ascomutils;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        public TraceLogger traceLogger;
        public Debugger debugger = Debugger.Instance;

        private bool _connected = false;
        private bool _simulated = false;

        private List<WiseVirtualMotor> directionMotors, allMotors;
        public Dictionary<TelescopeAxes, List<WiseVirtualMotor>> axisMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        public WisePin TrackPin;
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackingMotor;

        private bool _slewing = false;
        private bool _atPark;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Angle _targetRightAscension;
        private Angle _targetDeclination;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly List<TelescopeAxes> axes = new List<TelescopeAxes> { TelescopeAxes.axisPrimary, TelescopeAxes.axisSecondary };

        private long _primaryIsMoving = 0, _secondaryIsMoving = 0;
        private const int _nAxisValues = 5;
        private List<double> _primaryAxisValues = new List<double>(_nAxisValues);
        private List<uint> _secondaryAxisValues = new List<uint>(_nAxisValues);
        private object _primaryValuesLock = new Object(), _secondaryValuesLock = new Object();

        private static WiseSite wisesite = WiseSite.Instance;

        /// <summary>
        /// Usually two or three tasks are used to perform a slew:
        /// - if the dome is slaved, a dome slewer
        /// - an axisPrimary slewer
        /// - an axisSecondary slewer
        /// 
        /// An asynchronous slew just fires the tasks.
        /// A synchronous slew waits on the whole list to complete.
        /// </summary>
        private List<Task> slewers = new List<Task>();
        private static CancellationTokenSource slewingCancellationTokenSource;
        private static CancellationToken slewingCancellationToken;

        private Task movementCheckerTask;
        private static CancellationTokenSource movementCheckerCancellationTokenSource;
        private static CancellationToken movementCheckerCancellationToken;

        ReadyToSlewFlags readyToSlew = new ReadyToSlewFlags();

        public static Dictionary<Const.AxisDirection, string> axisPrimaryNames = new Dictionary<Const.AxisDirection, string>()
            {
                { Const.AxisDirection.Increasing, "West" },
                { Const.AxisDirection.Decreasing, "East" },
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

        private Hardware.Hardware hw = Hardware.Hardware.Instance;

        public class MovementParameters
        {
            public Angle changeDirection;
            //public Angle startMovement;
            public Angle minimalMovement;
            public Angle stopMovement;
            public Angle anglePerSecond;
            public double millisecondsPerDegree;
        };

        public class Movement
        {
            public Const.AxisDirection direction;
            public double rate;
            public Angle start;
            public Angle finalTarget;            // Where we finally want to get
            public Angle intermediateTarget;     // An intermediate step towards the finalAngle
            public Angle distanceToFinalTarget;
        };

        public Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>> movementParameters;
        public Dictionary<TelescopeAxes, Movement> prevMovement;         // remembers data about the previous axes movement, specifically the direction
        public Dictionary<TelescopeAxes, Movement> currMovement;         // the current axes movement        

        private Angle altLimit = new Angle("0:14:0.0");                  // telescope must not go below this Altitude (14 min)

        public MovementDictionary movementDict;
        private bool wasTracking;

        private SafetyMonitorTimer safetyMonitorTimer;

        public bool _enslaveDome = false;
        private DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;

        private bool _driverInitiatedSlewing = false;
        private bool _wasSlewing = false;

        /// <summary>
        /// From real-life measurements on axisPrimary, four samples, spaced at 5deg gave the following HA encoder values:
        ///     1.  5deg: 1422425
        ///     2. 10deg: 1412187
        ///     3. 15deg: 1401946
        ///     4. 20deg: 1391713
        ///     
        /// The respective deltas are:
        ///     1. d1: 10238
        ///     2. d2: 10241
        ///     3. d3: 10233
        ///     
        /// This gives approx 2047.46 ticks per 1deg, or 3600/2047.46 arcseconds per encoder tick.
        /// 
        /// </summary>

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

        public double TargetDeclination {
            get
            {
                if (_targetDeclination == null)
                    throw new ValueNotSetException("Target not set");

                traceLogger.LogMessage("TargetDeclination Get", string.Format("{0}", _targetDeclination));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Get - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
                return _targetDeclination.Degrees;
            }

            set
            {
                if (value < -90.0 || value > 90.0)
                    throw new InvalidValueException(string.Format("Invalid Declination {0}. Must be between -90 and 90", value));

                _targetDeclination = Angle.FromDegrees(value, Angle.Type.Dec);
                traceLogger.LogMessage("TargetDeclination Set", string.Format("{0}", _targetDeclination));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Set - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
            }
        }

        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == null)
                    throw new ValueNotSetException("Target RA not set");

                Angle ret = _targetRightAscension;

                traceLogger.LogMessage("TargetRightAscension Get", string.Format("{0}", _targetRightAscension));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Get - {0} ({1})", ret, ret.Hours));

                return _targetRightAscension.Hours;
            }

            set
            {
                if (value < 0.0 || value > 24.0)
                    throw new ASCOM.InvalidValueException(string.Format("Invalid RightAscension {0}. Must be between 0 to 24", value));

                _targetRightAscension = Angle.FromHours(value, Angle.Type.RA);
                traceLogger.LogMessage("TargetRightAscension Set", string.Format("{0}", _targetRightAscension));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Set - {0} ({1})",
                    _targetRightAscension,
                    _targetRightAscension.Hours));
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

        public bool doesRefraction {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("DoesRefraction Get", ret.ToString());
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
                traceLogger.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }

            set
            {
                traceLogger.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                foreach (var connectable in connectables)
                {
                    connectable.Connect(value);
                }
                _connected = value;
                if (instance._enslaveDome)
                    DomeSlaveDriver.Instance.Connect(value);

                if (_connected)
                {
                    movementCheckerCancellationTokenSource = new CancellationTokenSource();
                    movementCheckerCancellationToken = movementCheckerCancellationTokenSource.Token;

                    try
                    {
                        movementCheckerTask = Task.Run(() =>
                        {
                            Thread.CurrentThread.Name = "AxisMovementChecker";
                            AxisMovementChecker();
                        }, movementCheckerCancellationToken);
                    }
                    catch (OperationCanceledException) { }
                }
                else
                {
                    if (movementCheckerTask != null)
                    {
                        movementCheckerCancellationTokenSource.Cancel();
                    }
                }
            }
        }

        private static readonly WiseTele instance = new WiseTele(); // Singleton
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
                return instance;
            }
        }

        public void init(Telescope tele)
        {
            if (_initialized)
                return;

            WisePin SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pinsisInitialized = true;

            tele.ReadProfile();
            debugger.init();
            traceLogger = new TraceLogger("", "Tele");
            traceLogger.Enabled = Telescope._trace;
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            hw.init();
            wisesite.init();
            
            List<ISimulated> hardware_elements = new List<ISimulated>();

            try
            {
                instance.connectables = new List<IConnectable>();
                instance.disposables = new List<IDisposable>();

                NorthPin = new WisePin("TeleNorth", hw.teleboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalOut);
                EastPin = new WisePin("TeleEast", hw.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut);
                WestPin = new WisePin("TeleWest", hw.teleboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalOut);
                SouthPin = new WisePin("TeleSouth", hw.teleboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalOut);

                SlewPin = new WisePin("TeleSlew", hw.teleboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut);
                TrackPin = new WisePin("TeleTrack", hw.teleboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);

                NorthGuidePin = new WisePin("TeleNorthGuide", hw.teleboard, DigitalPortType.FirstPortB, 0, DigitalPortDirection.DigitalOut);
                EastGuidePin = new WisePin("TeleEastGuide", hw.teleboard, DigitalPortType.FirstPortB, 1, DigitalPortDirection.DigitalOut);
                WestGuidePin = new WisePin("TeleWestGuide", hw.teleboard, DigitalPortType.FirstPortB, 2, DigitalPortDirection.DigitalOut);
                SouthGuidePin = new WisePin("TeleSouthGuide", hw.teleboard, DigitalPortType.FirstPortB, 3, DigitalPortDirection.DigitalOut);

                instance.HAEncoder = new WiseHAEncoder("TeleHAEncoder");
                instance.DecEncoder = new WiseDecEncoder("TeleDecEncoder");
            }
            catch (WiseException e)
            {
               debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseTele constructor caught: {0}.", e.Message);
            }

            instance.NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin, TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing, new List<object> { instance.DecEncoder });
            instance.SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin, TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing, new List<object> { instance.DecEncoder });
            //instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });
            //instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });
            instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });
            instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });
            instance.TrackingMotor = new WiseVirtualMotor("TrackMotor", TrackPin, null, null, TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });

            instance.axisMotors = new Dictionary<TelescopeAxes, List<WiseVirtualMotor>>();
            instance.axisMotors[TelescopeAxes.axisPrimary] = new List<WiseVirtualMotor> { instance.EastMotor, instance.WestMotor };
            instance.axisMotors[TelescopeAxes.axisSecondary] = new List<WiseVirtualMotor> { instance.NorthMotor, instance.SouthMotor };

            instance.directionMotors = new List<WiseVirtualMotor>();
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisPrimary]);
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisSecondary]);

            instance.allMotors = new List<WiseVirtualMotor>();
            instance.allMotors.AddRange(instance.directionMotors);
            instance.allMotors.Add(TrackingMotor);

            hardware_elements.AddRange(instance.allMotors);
            hardware_elements.Add(instance.HAEncoder);
            hardware_elements.Add(instance.DecEncoder);
            foreach (ISimulated s in hardware_elements)
            {
                if (s.simulated)
                {
                    simulated = true;
                    break;
                }
            }

            TimerCallback safetyMonitorTimerCallback = new TimerCallback(DoCheckSafety);
            safetyMonitorTimer = new SafetyMonitorTimer(safetyMonitorTimerCallback, 100, 100);

            #region MovementParameters
            //
            // Initialize movement parameters.
            // These should be real-world, measured values.
            //
            instance.movementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>();

            instance.movementParameters[TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSlew] = new MovementParameters()
            {
                anglePerSecond = Angle.FromHours(Angle.Deg2Hours(Const.rateSlew)),
                changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:30.0")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("02:00:00.0")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                millisecondsPerDegree = 500.0,      // 2deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = Angle.FromHours(Angle.Deg2Hours(Const.rateSet)),
                changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:10.0")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                millisecondsPerDegree = 60000.0,    // 1min/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = Angle.FromHours(Angle.Deg2Hours(Const.rateGuide)),
                changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:10.0")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateSlew), changeDirection = new Angle("00:00:30.0"),
                minimalMovement = new Angle("02:00:00.0"),  stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 500.0,      // 2 deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateSet), changeDirection = new Angle("00:00:30.0"),
                minimalMovement = new Angle("00:10:00.0"), stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 60000.0,    // 1 min/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateGuide), changeDirection = new Angle("00:00:00.5"),
                minimalMovement = new Angle("00:00:10.0"), stopMovement = new Angle("00:00:00.5"),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };
            #endregion

            instance.prevMovement = new Dictionary<TelescopeAxes, Movement>();
            instance.prevMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            instance.prevMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };

            instance.currMovement = new Dictionary<TelescopeAxes, Movement>();
            instance.currMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            instance.currMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };


            instance.movementDict = new MovementDictionary();
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { EastMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { WestMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { NorthMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { SouthMotor });

            instance.connectables.Add(instance.NorthMotor);
            instance.connectables.Add(instance.EastMotor);
            instance.connectables.Add(instance.WestMotor);
            instance.connectables.Add(instance.SouthMotor);
            instance.connectables.Add(instance.TrackingMotor);
            instance.connectables.Add(instance.HAEncoder);
            instance.connectables.Add(instance.DecEncoder);

            instance.disposables.Add(instance.NorthMotor);
            instance.disposables.Add(instance.EastMotor);
            instance.disposables.Add(instance.WestMotor);
            instance.disposables.Add(instance.SouthMotor);
            instance.disposables.Add(instance.TrackingMotor);
            instance.disposables.Add(instance.HAEncoder);
            instance.disposables.Add(instance.DecEncoder);

            SlewPin.SetOff();
            instance.TrackingMotor.SetOff();
            instance.NorthMotor.SetOff();
            instance.EastMotor.SetOff();
            instance.WestMotor.SetOff();
            instance.SouthMotor.SetOff();

            if (instance._enslaveDome)
            {
                domeSlaveDriver = DomeSlaveDriver.Instance;
                domeSlaveDriver.init();
            }

            _initialized = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseTele init() done.");
        }

        public double FocalLength
        {
            get
            {
                double ret = 7.112;  // Las Campanas 40" (meters)

                traceLogger.LogMessage("FocalLength Get", ret.ToString());
                return ret;   
            }
        }

        public void AbortSlew()
        {
            if (AtPark)
                throw new InvalidOperationException("Cannot AbortSlew while AtPark");

            if (!_driverInitiatedSlewing)
                return;

            slewingCancellationTokenSource.Cancel();
            traceLogger.LogMessage("AbortSlew", "");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "AbortSlew");
        }

        public double RightAscension
        {
            get
            {
                var ret = HAEncoder.RightAscension;

                traceLogger.LogMessage("RightAscension", string.Format("Get - {0} ({1})", ret, ret.Hours));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("RightAscension Get - {0} ({1})", ret, ret.Hours));

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

                traceLogger.LogMessage("Declination", string.Format("Get - {0} ({1})", ret, ret.Degrees));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("Declination Get - {0} ({1})", ret, ret.Degrees));
                return ret.Degrees;
            }
        }

        public double Azimuth
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd= 0;

                wisesite.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                traceLogger.LogMessage("Azimuth Get", az.ToString());
                return az;
            }
        }

        public double Altitude
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0, alt;

                wisesite.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                alt = 90.0 - zd;
                traceLogger.LogMessage("Altitude Get", alt.ToString());
                return alt;
            }
        }

        public bool Tracking
        {
            get
            {
                bool ret = TrackingMotor.isOn;

                traceLogger.LogMessage("Tracking", "Get - " + ret.ToString());
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Get - {0}", ret));
                return ret;
            }

            set
            {
                traceLogger.LogMessage("Tracking Set", value.ToString());
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Set - {0}", value));

                if (value)
                {
                    if (TrackingMotor.isOff)
                        TrackingMotor.SetOn(Const.rateTrack);
                    if (!safetyMonitorTimer.isOn)
                        safetyMonitorTimer.SetOn();
                }
                else
                {
                    if (TrackingMotor.isOn)
                        TrackingMotor.SetOff();

                    bool active = false;
                    foreach (WiseVirtualMotor m in directionMotors)
                    {
                        if (m.isOn)
                        {
                            active = true;
                            break;
                        }
                        if (active && !safetyMonitorTimer.isOn)
                            safetyMonitorTimer.SetOn();
                    }
                }
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                var rates = DriveRates.driveSidereal;

                traceLogger.LogMessage("TrackingRate Get - ", rates.ToString());
                return rates;
            }

            set
            {
                traceLogger.LogMessage("TrackingRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
            }
        }

        public void Stop()
        {
            if (slewingCancellationTokenSource != null)
            {
                try
                {
                    slewingCancellationTokenSource.Cancel();
                } catch (AggregateException ax)
                {
                    ax.Handle((ex) =>
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            "Stop: got {0}", ex.Message);
                        if (ex is ObjectDisposedException)
                            return true;
                        return false;
                    });                        
                }
            }
                

            foreach (WiseVirtualMotor motor in directionMotors)
                if (motor.isOn)
                    motor.SetOff();

            safetyMonitorTimer.SetOff();

            Tracking = false;
            Slewing = false;
        }

        public bool AxisIsMoving(TelescopeAxes axis)
        {
            if (axis == TelescopeAxes.axisPrimary)
                return Interlocked.Read(ref _primaryIsMoving) == 1;
            else
                return Interlocked.Read(ref _secondaryIsMoving) == 1;
        }

        private void CheckAxisMovement(object StateObject)
        {
            foreach (TelescopeAxes axis in axes) {
                if (axis == TelescopeAxes.axisPrimary)
                {
                    var epsilon = new Angle("00:00:00.1").Degrees;
                    lock (_primaryValuesLock)
                    {
                        if (_primaryAxisValues.Count == _nAxisValues)
                            _primaryAxisValues.Remove(_primaryAxisValues.ElementAt(0));
                        _primaryAxisValues.Add(RightAscension);

                        if (_primaryAxisValues.Count == _nAxisValues)
                        {
                            var deltas = new List<double>();
                            for (var i = 1; i < _primaryAxisValues.Count; i++)
                                deltas.Add(Math.Abs(_primaryAxisValues[i] - _primaryAxisValues[i - 1]));

                            Interlocked.Exchange(ref _primaryIsMoving, (deltas.Max() <= epsilon) ? 0 : 1);
                        }
                    }
                }
                else
                {
                    uint epsilon = 1;
                    lock (_secondaryValuesLock)
                    {
                        if (_secondaryAxisValues.Count == _nAxisValues)
                            _secondaryAxisValues.Remove(_secondaryAxisValues.ElementAt(0));
                        _secondaryAxisValues.Add(DecEncoder.Value);

                        if (_secondaryAxisValues.Count == _nAxisValues)
                        {
                            var deltas = new List<uint>();
                            for (var i = 1; i < _secondaryAxisValues.Count; i++)
                                deltas.Add((uint)Math.Abs(_secondaryAxisValues[i] - _secondaryAxisValues[i - 1]));

                            Interlocked.Exchange(ref _secondaryIsMoving, (deltas.Max() <= epsilon) ? 0 : 1);
                        }
                    }
                }
            }
        }

        public void AxisMovementChecker()
        {
            TimerCallback axisMovementTimerCallback = new TimerCallback(CheckAxisMovement);
            System.Threading.Timer movementCheckerTimer = new System.Threading.Timer(axisMovementTimerCallback);
            movementCheckerTimer.Change(100, 100);
        }

        public bool Moving
        {
            get
            {
                foreach (WiseVirtualMotor m in directionMotors)
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
                bool ret = false;

                if (_slewing)
                    ret = true;
                else
                {
                    foreach (var slewer in slewers)
                        if (slewer.Status == TaskStatus.Created || 
                            slewer.Status == TaskStatus.WaitingToRun || 
                            slewer.Status == TaskStatus.Running)
                        {
                            ret = true;
                            break;
                        }
                }

                traceLogger.LogMessage("Slewing Get", ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("Slewing Get - {0}", ret));

                if (_wasSlewing == true && ret == false)
                {
                    //slewingCancellationTokenSource.Dispose();
                    slewingCancellationTokenSource = null;
                    _driverInitiatedSlewing = false;
                }

                _wasSlewing = ret;

                return ret;
            }

            set
            {
                _slewing = value;
            }

        }

        public double DeclinationRate
        {
            get
            {
                double decRate = 0.0;

                traceLogger.LogMessage("DeclinationRate", "Get - " + decRate.ToString());
                return decRate;
            }

            set
            {
                traceLogger.LogMessage("DeclinationRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            traceLogger.LogMessage("MoveAxis", string.Format("MoveAxis({0}, {1})", Axis, Rate));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("MoveAxis({0}, {1})", Axis, Rate));

            Const.AxisDirection direction = (Rate == 0.0) ? Const.AxisDirection.None : 
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            _driverInitiatedSlewing = true;

            try
            {
                _moveAxis(Axis, Rate, direction, true);
            }
            catch (Exception e)
            {
                _driverInitiatedSlewing = false;
                throw e;
            }
        }

        private void _moveAxis(
            TelescopeAxes Axis,
            double Rate,
            Const.AxisDirection direction = Const.AxisDirection.None,
            bool stopTracking = false)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): called", Axis, RateName(Rate));

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

            if (Rate == Const.rateStopped)
            {
                foreach (WiseVirtualMotor m in axisMotors[thisAxis])
                    if (m.isOn)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}):  {2} was on, stopping it.", Axis, RateName(Rate), m.name);
                        m.SetOff();
                    }

                if (wasTracking)
                    Tracking = true;

                Instance.currMovement[Axis].rate = Const.rateStopped;
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): done.", Axis, RateName(Rate));

                _slewing = false;

                return;
            }

            double absRate = Math.Abs(Rate);
            if (! ((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                throw new InvalidValueException(string.Format("_moveAxis({0}, {1}): Invalid rate.", Axis, Rate));

            if (Instance.currMovement[otherAxis].rate != Const.rateStopped && 
                absRate != Instance.currMovement[otherAxis].rate)
            {
                string msg = string.Format("Cannot _moveAxis({0}, {1}) ({2}) while {3} is moving at {4}",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], otherAxis, RateName(currMovement[otherAxis].rate));

                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, msg);
                throw new InvalidValueException(msg);
            }

            try {
                mover = movementDict[new MovementSpecifier(Axis, direction)];
            } catch(Exception e) {
                throw new InvalidValueException(string.Format("Don't know how to _moveAxis({0}, {1}) (no mover) ({2}) [{3}]",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], e.Message));
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): direction: {2}, stopTracking: {3}",
                Axis, RateName(Rate), axisDirectionName[Axis][direction], stopTracking);
            wasTracking = Tracking;
            if (stopTracking)
                Tracking = false;

            _slewing = true;

            foreach (WiseVirtualMotor m in mover.motors) {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): starting {2}", Axis, RateName(Rate), m.name);
                m.SetOn(Rate);
            }

            if (Moving && !safetyMonitorTimer.isOn)
                safetyMonitorTimer.SetOn();
        }

        public bool IsPulseGuiding
        {
            get
            {
                throw new PropertyNotImplementedException("IsPulseGuiding");
            }
        }

        public void SlewToTargetAsync()
        {
            if (_targetRightAscension == null)
                throw new ValueNotSetException("Target RA not set");
            if (_targetDeclination == null)
                throw new ValueNotSetException("Target Dec not set");

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToTargetAsync", string.Format("Started: ra: {0}, dec: {1}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTargetAsync({0}, {1})", ra, dec));


            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while NOT Tracking");


            if (! SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToTargetAsync({0}, {1})", ra, dec));

            _driverInitiatedSlewing = true;

            _slewToCoordinatesAsync(_targetRightAscension, _targetDeclination);
        }

        /// <summary>
        /// Checks if we're safe at a given position
        ///  
        /// If (takeAction == true) then take the apropriate recovery action
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        /// <param name="takeAction">Take recovery actions or not</param>
        public bool SafeAtCoordinates(Angle ra, Angle dec, bool takeAction = false)
        {
            return true;

            double rar = 0, decr = 0, az = 0, zd = 0;
            Angle alt;

            wisesite.prepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,          // TBD: set Pressure (mbar) and Temperature (C)
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,   // TBD: do we want refraction?
                ref zd, ref az, ref rar, ref decr);

            alt = Angle.FromDegrees(90.0 - zd);
            if (alt < altLimit)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    string.Format("Not safe to move to ra: {0}, dec: {1} (alt: {2} is below altLimit: {3})",
                    ra,
                    dec,
                    alt,
                    altLimit));

                if (takeAction)
                {
                    Stop();
                    BackToSafety();
                }
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Get away from an invalid position, by backing off 20sec on both axes.
        /// </summary>
        private void BackToSafety()
        {
            Angle backoff = new Angle("00:00:20.0");
            Angle primaryBackoff, secondaryBackoff;
            Angle ra = Angle.FromHours(RightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(Declination, Angle.Type.Dec);
            double ha = HourAngle;

            primaryBackoff = (ha > 0) ? ra - backoff : ra + backoff;
            secondaryBackoff = (dec > Angle.FromDegrees(0.0)) ? Angle.FromDegrees(-backoff.Degrees) : backoff;
            SlewToCoordinates(primaryBackoff.Hours, secondaryBackoff.Degrees);
        }

        public bool simulated
        {
            get
            {
                return _simulated;
            }
            set
            {
                _simulated = value;
            }
        }

        public bool AtPark
        {
            get
            {
                bool ret = _atPark;

                traceLogger.LogMessage("AtPark", "Get - " + ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtPark Get - {0}", ret));

                return ret;
            }

            set
            {
                _atPark = value;
            }
        }

        public void Park()
        {
            traceLogger.LogMessage("Park", "");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park");

            if (AtPark)
                return;

            _slewToCoordinatesSync(wisesite.LocalSiderealTime, wisesite.Latitude);
            AtPark = true;
        }

        private void _slewToCoordinatesSync(Angle RightAscension, Angle Declination)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), called.", RightAscension, Declination);
            _slewToCoordinatesAsync(RightAscension, Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), waiting ...", RightAscension, Declination);
            Task.WaitAll(slewers.ToArray());
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), done.", RightAscension, Declination);
        }

        private void Slewer(TelescopeAxes axis, Angle targetAngle)
        {
            string threadName = Thread.CurrentThread.Name;
            Movement cm = Instance.currMovement[axis];
            Angle currPosition = new Angle(0.0);
            MovementParameters mp;

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0}: targetAngle: {1}", threadName, targetAngle);

            try {
                cm.finalTarget = targetAngle;
                foreach (var rate in rates)
                {
                    mp = movementParameters[axis][rate];
                    Instance.currMovement[axis] = new Movement() { rate = Const.rateStopped };

                    slewingCancellationToken.ThrowIfCancellationRequested();
                    readyToSlew.Increment(rate);

                    //
                    // Wait till both axes can move at the current rate.
                    //
                    while (readyToSlew.Get(rate) != 2)
                    {
                        const int syncMillis = 500;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                           "{0} at {1}: waiting {2} millis for the other axis ...",
                           threadName, RateName(rate), syncMillis);

                        Thread.Sleep(syncMillis);
                        slewingCancellationToken.ThrowIfCancellationRequested();
                    }

                    //
                    // Both axes can now move at the current rate
                    //
                    cm.start = (axis == TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(RightAscension, Angle.Type.RA) :
                        Angle.FromDegrees(Declination, Angle.Type.Dec);

                    var shortest = cm.start.ShortestDistance(cm.finalTarget);
                    cm.distanceToFinalTarget = shortest.angle;
                    cm.direction = shortest.direction;

                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: start: {2}, distanceToFinalTarget: {3}, direction: {4}",
                        threadName, RateName(rate), cm.start, cm.distanceToFinalTarget, cm.direction);

                    Angle minimalMovementAngle = mp.minimalMovement + mp.stopMovement;
                    if (prevMovement[axis].direction != Const.AxisDirection.None && prevMovement[axis].direction != cm.direction)
                    {
                        minimalMovementAngle += mp.changeDirection;
                        while (AxisIsMoving(axis))
                            ;
                    }
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0} at {1}: minimalMovementAngle: {2}",
                        threadName, RateName(rate), minimalMovementAngle);

                    if (cm.distanceToFinalTarget < minimalMovementAngle)
                    {
                        cm.rate = Const.rateStopped;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0} at {1}: Not moving, too short ({2} < {3}, {4} < {5})",
                            threadName, RateName(rate),
                            cm.distanceToFinalTarget, minimalMovementAngle,
                            cm.distanceToFinalTarget.Degrees, minimalMovementAngle.Degrees);
                        continue;   // to next rate
                    }
                    else
                    {
                        Angle intermediateDistance = cm.distanceToFinalTarget - mp.stopMovement;

                        if (cm.direction == Const.AxisDirection.Increasing)
                            cm.intermediateTarget = cm.start + intermediateDistance;
                        else
                            cm.intermediateTarget = cm.start - intermediateDistance;

                        cm.rate = rate;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                            "{0} at {1}: start: {2}, intermediateTarget: {3}, intermediateDistance: {4}, direction: {5}",
                            threadName, RateName(cm.rate), cm.start, cm.intermediateTarget, intermediateDistance, cm.direction);

                        slewingCancellationToken.ThrowIfCancellationRequested();
                        _moveAxis(axis, rate, cm.direction, false);

                        //
                        // The axis is set in motion, wait for it to arrive at target
                        //

                        ShortestDistanceResult prevRemainingDistance = null;
                        while (true)
                        {
                            const int waitMillis = 5;    // TODO: make it configurable or constant
                            ShortestDistanceResult remainingDistance;
                            double allowedDrift = new Angle("0:0:40.0").Degrees;

                            slewingCancellationToken.ThrowIfCancellationRequested();

                            currPosition = (axis == TelescopeAxes.axisPrimary) ?
                                Angle.FromHours(RightAscension, Angle.Type.RA) :
                                Angle.FromDegrees(Declination, Angle.Type.Dec);

                            remainingDistance = currPosition.ShortestDistance(cm.intermediateTarget);
                            string arrivalReason = null;

                            if (remainingDistance.angle.Degrees <= mp.stopMovement.Degrees)
                            {
                                arrivalReason = string.Format("proximity: remainingDistance.angle ({0}, {1}) <= mp.stopMovement ({2}, {3})",
                                    remainingDistance.angle, remainingDistance.angle.Degrees,
                                    mp.stopMovement, mp.stopMovement.Degrees);
                            }
                            else if (cm.direction != remainingDistance.direction) {
                                arrivalReason = string.Format("movement direction changed: cm.direction: {0} != remainingDistance.direction: {1}",
                                    cm.direction.ToString(), remainingDistance.direction.ToString());
                            } else if (prevRemainingDistance != null) {
                                if (Math.Abs(remainingDistance.angle.Degrees - prevRemainingDistance.angle.Degrees) > allowedDrift)
                                {
                                    //arrivalReason = string.Format("remainingDistance has increased: {0} > {1}, more than {3}",
                                    //    remainingDistance.angle, prevRemainingDistance.angle, allowedDrift.ToString());
                                    arrivalReason = null;
                                }
                            }

                            if (arrivalReason != null)
                            {
                                //
                                // We're so close to the target that after stopping the
                                //  motor inertia will take us there.
                                //
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                    "{0} at {1}: stopping at {2}, ({3} from {4}), arrivalReason: {5}, calling  MoveAxis({6}, {7})",
                                    threadName, RateName(cm.rate), currPosition,
                                    remainingDistance.angle, cm.intermediateTarget,
                                    arrivalReason,
                                    axis, RateName(Const.rateStopped));

                                _moveAxis(axis, Const.rateStopped);
                                break;
                            }
                            else
                            {
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                    "{0} at {1}: still moving, currPosition: {2} ==> intermediateTarget: {3}, finalTarget: {4}, remaining (Angle: {5}, degrees: {6}, direction: {7}), stopMovement: ({8}, {9}), sleeping {10} millis ...",
                                    threadName, RateName(cm.rate), currPosition,
                                    cm.intermediateTarget, cm.finalTarget,
                                    remainingDistance.angle, remainingDistance.angle.Degrees, remainingDistance.direction,
                                    mp.stopMovement, mp.stopMovement.Degrees,
                                    waitMillis);

                                prevRemainingDistance = remainingDistance;
                                Thread.Sleep(waitMillis);
                            }
                        }
                    }
                }
            } catch (OperationCanceledException)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "{0} at {1}: Slew cancelled at {2}", threadName, RateName(cm.rate), currPosition);
                _moveAxis(axis, Const.rateStopped);
            }
        }

        private void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
        {
            slewers.Clear();
            readyToSlew.Reset();
            slewingCancellationTokenSource = new CancellationTokenSource();
            slewingCancellationToken = slewingCancellationTokenSource.Token;
            Slewing = true;

            try
            {
                if (instance._enslaveDome)
                    slewers.Add(Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = "domeSlewer";
                        try
                        {
                            DomeSlaveDriver.Instance.SlewStartAsync(RightAscension, Declination);
                        } catch (OperationCanceledException)
                        {
                            DomeSlaveDriver.Instance.AbortSlew();
                        }
                    }, slewingCancellationToken));

                slewers.Add(Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "primarySlewer";
                    Slewer(TelescopeAxes.axisPrimary, RightAscension);
                }, slewingCancellationToken));

                slewers.Add(Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "secondarySlewer";
                    Slewer(TelescopeAxes.axisSecondary, Declination);
                }, slewingCancellationToken));

            } catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        "_slewToCoordinatesAsync: Caught {0}", ex.Message);
                    return false;
                });
            }
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            Angle ra = Angle.FromHours(RightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(Declination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToCoordinates", string.Format("ra: {0}, dec: {0}", ra, dec));
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("SlewToCoordinates - {0}, {1}", ra, dec));

            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");


            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToCoordinates({0}, {1})", ra, dec));

            try
            {
                _slewToCoordinatesSync(ra, dec);
            }
            catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    "SlewToCoordinates: _slewToCoordinatesSync({0}, {1}) threw exception: {2}",
                    ra, dec, e.Message);
                _slewing = false;
            }
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToCoordinatesAsync({0}, {1})", ra, dec);

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToCoordinatesAsync({0}, {1})", ra, dec));

            try
            {
                _slewToCoordinatesAsync(ra, dec);
            } catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinatesAsync({0}, {1}) caught exception: {2}",
                    RightAscension, Declination, e.Message);
                _slewing = false;
            }
        }

        private void DoCheckSafety(object StateObject)
        {
            SafeAtCoordinates(Angle.FromHours(RightAscension, Angle.Type.RA), Angle.FromDegrees(Declination, Angle.Type.Dec), true);
        }

        public void Unpark()
        {
            if (AtPark)
                AtPark = false;

            traceLogger.LogMessage("Unpark", "Done");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Unpark");
        }

        public DateTime UTCDate
        {
            get
            {
                DateTime utcDate = DateTime.UtcNow;
                traceLogger.LogMessage("UTCDate Get - ", utcDate.ToString());
                return utcDate;
            }

            set
            {
                traceLogger.LogMessage("UTCDate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                traceLogger.LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    traceLogger.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        public void SyncToTarget()
        {
            traceLogger.LogMessage("SyncToTarget", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToTarget");
        }

        public string Description
        {
            get
            {
                var ret = "Wise40 Telescope";
                traceLogger.LogMessage("Description Get", ret);
                return ret;
            }
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                AlignmentModes mode = AlignmentModes.algPolar;

                traceLogger.LogMessage("AlignmentMode Get", mode.ToString());
                return mode;
            }
        }

        public double SiderealTime
        {
            get
            {
                double ret = wisesite.LocalSiderealTime.Hours;

                traceLogger.LogMessage("SiderealTime", "Get - " + ret.ToString());
                return ret;
            }
        }

        public double SiteElevation
        {
            get
            {
                double elevation = wisesite.Elevation;

                traceLogger.LogMessage("SiteElevation Get", elevation.ToString());
                return elevation;
            }

            set
            {
                traceLogger.LogMessage("SiteElevation Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                double latitude = wisesite.Latitude.Degrees;

                traceLogger.LogMessage("SiteLatitude Get", latitude.ToString());
                return latitude;
            }
            set
            {
                traceLogger.LogMessage("SiteLatitude Set", "Not implemented");
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

                traceLogger.LogMessage("SiteLongitude Get", longitude.ToString());
                return longitude;
            }
            set
            {
                traceLogger.LogMessage("SiteLongitude Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
            }
        }

        public void SlewToTarget()
        {
            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToTarget", string.Format("ra: {0}, dec: {0}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTarget - {0}, {1}", ra, dec));

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToTarget({0}, {1})", ra, dec));

            _driverInitiatedSlewing = true;

            try
            {
                SlewToCoordinates(TargetRightAscension, TargetDeclination); // sync
            }
            catch (Exception e)
            {
                _driverInitiatedSlewing = false;
                throw e;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SyncToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            traceLogger.LogMessage("SyncToCoordinates", "Not implemented");
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
            traceLogger.LogMessage("CanMoveAxis", "Get - " + Axis.ToString() + ": " + ret.ToString());

            return ret;
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equJ2000;

                traceLogger.LogMessage("EquatorialSystem", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            traceLogger.LogMessage("FindHome", "Not Implemented");
            throw new MethodNotImplementedException("FindHome");
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            traceLogger.LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
        }

        public bool CanPark
        {
            get
            {
                traceLogger.LogMessage("CanPark", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                traceLogger.LogMessage("CanPulseGuide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                traceLogger.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                traceLogger.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSetPark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                traceLogger.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                traceLogger.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                traceLogger.LogMessage("CanSetTracking", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanSlew", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSlewAltAz", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSlewAltAzAsync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanSlewAsync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSync
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSyncAltAz", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanUnpark
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanUnpark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                traceLogger.LogMessage("GuideRateDeclination Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
            }
            set
            {
                traceLogger.LogMessage("GuideRateDeclination Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                traceLogger.LogMessage("GuideRateRightAscension Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
            }
            set
            {
                traceLogger.LogMessage("GuideRateRightAscension Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
            }
        }

        public bool AtHome
        {
            get
            {
                bool ret = false;       // Homing is not implemented

                traceLogger.LogMessage("AtHome", "Get - " + ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtHome Get - {0}", ret));
                return ret;
            }
        }

        public bool CanFindHome
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanFindHome", "Get - " + ret.ToString());
                return ret;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            IAxisRates rates = new AxisRates(Axis);

            traceLogger.LogMessage("AxisRates", "Get - " + rates.ToString());
            return rates;
        }

        public short SlewSettleTime
        {
            get
            {
                traceLogger.LogMessage("SlewSettleTime Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }

            set
            {
                traceLogger.LogMessage("SlewSettleTime Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SlewToAltAz", String.Format("SlewToAltAz({0}, {1})", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SlewToAltAzAsync", String.Format("SlewToAltAzAsync({0}, {1})", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                traceLogger.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                return rightAscensionRate;
            }

            set
            {
                traceLogger.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            traceLogger.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                PierSide side = PierSide.pierEast;

                traceLogger.LogMessage("SideOfPier Get", side.ToString());
                return side;
            }

            set
            {
                traceLogger.LogMessage("SideOfPier Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            traceLogger.LogMessage("PulseGuide", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("PulseGuide");
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
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            //string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "First ASCOM driver for the Wise40 telescope. Version: " + 
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                traceLogger.LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                string name = "Wise40 Telescope";
                traceLogger.LogMessage("Name Get", name);
                return name;
            }
        }

    }
}
