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
    public class MovementCanceledException : Exception
    {
        public MovementCanceledException()
        {

        }

        public MovementCanceledException(string message)
            : base(message)
        {

        }
        public MovementCanceledException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }

    //public class WiseTele : IDisposable, IConnectable
    public class WiseTele : IDisposable, IConnectable, ISimulated
    {
        private class SafetyMonitorTimer
        {
            private System.Threading.Timer timer;
            private bool _isOn;
            private int dueTime, period;

            public SafetyMonitorTimer(System.Threading.TimerCallback callback, int dueTime, int period)
            {
                timer = new System.Threading.Timer(callback);
                this.dueTime = dueTime;
                this.period = period;
                _isOn = false;
            }

            public bool isOn {
                get
                {
                    return _isOn;
                }
            }

            public void SetOn()
            {
                timer.Change(dueTime, period);
                _isOn = true;
            }

            public void SetOff()
            {
                timer.Change(0, 0);
                _isOn = false;
            }
        }

        private class MeasuredMovementArg {
            public Angle rightAscension, declination;
        }

        private class MeasuredMovementResult
        {
            public bool cancelled;
        }

        private MeasuredMovementResult result;

        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public static string driverDescription = "Wise40 Telescope";

        private static NOVAS31 novas31;
        private static Util ascomutils;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        private TraceLogger traceLogger;
        public Debugger debugger = new Debugger((uint) Debugger.DebugLevel.DebugAll);

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

        //private Target target;
        private Angle _targetRightAscension;
        private Angle _targetDeclination;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly List<TelescopeAxes> axes = new List<TelescopeAxes> { TelescopeAxes.axisPrimary, TelescopeAxes.axisSecondary };


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

        public class MovementParameters
        {
            public Angle changeDirection;
            public Angle startMovement;
            public Angle stopMovement;
            public Angle anglePerSecond;
            public double millisecondsPerDegree;
        };

        public class Movement
        {
            public Const.AxisDirection direction;
            public double rate;
            public Angle startingAngle;
            public Angle longTermTargetAngle;       // Where we finally want to get
            public Angle shortTermTargetAngle;      // A smaller intermediate step towards the longTermTargetAngle
            public Angle deltaAngle;
        };

        public Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>> movementParameters;
        public Dictionary<TelescopeAxes, Movement> prevMovement;         // remembers data about the previous axes movement, specifically the direction
        public Dictionary<TelescopeAxes, Movement> currMovement;         // the current axes movement        

        private Angle altLimit = new Angle("0:14:0.0");                  // telescope must not go below this Altitude (14 min)

        public MovementDictionary movementDict;
        private bool wasTracking;

        //private System.Threading.Timer safetyMonitorTimer;
        private SafetyMonitorTimer safetyMonitorTimer;

        public BackgroundWorker _slewToCoordinatesAsync_bgw;
        private static AutoResetEvent _slewToCoordinatesAsync_bgwDone = new AutoResetEvent(false);

        public bool _enslaveDome = false;
        private DomeSlaveDriver domeSlaveDriver;

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
        /// This gives approx 2047.46 ticks per 1deg, or 3600/2047.46 seconds per encoder tick.
        /// 
        /// </summary>
        private const double closeEnough = 3600.0 / 2047.46;

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
                if (_enslaveDome)
                    domeSlaveDriver.Connect(value);
            }
        }

        public void log(string fmt, params object[] o)
        {
            string msg = String.Format(fmt, o);
            DateTime now = DateTime.Now;

            traceLogger.LogMessage("WiseTele", msg);
            Console.WriteLine(string.Format("[{0}] {1}/{2}/{3} {4}: {5}", Thread.CurrentThread.ManagedThreadId,
                now.Day, now.Month, now.Year, now.TimeOfDay, msg));
        }

        public static readonly WiseTele instance = new WiseTele(); // Singleton

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

        public void init(Telescope T)
        {
            WisePin SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pinsisInitialized = true;

            debugger = new Debugger();
            T.ReadProfile();
            traceLogger = new TraceLogger("", "Tele");
            traceLogger.Enabled = Telescope._trace;
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            Hardware.Hardware.Instance.init();
            
            List<ISimulated> hardware_elements = new List<ISimulated>();

            try
            {
                instance.connectables = new List<IConnectable>();
                instance.disposables = new List<IDisposable>();

                NorthPin = new WisePin("TeleNorth", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalOut);
                EastPin = new WisePin("TeleEast", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut);
                WestPin = new WisePin("TeleWest", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalOut);
                SouthPin = new WisePin("TeleSouth", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalOut);

                SlewPin = new WisePin("TeleSlew", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut);
                TrackPin = new WisePin("TeleTrack", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);

                NorthGuidePin = new WisePin("TeleNorthGuide", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortB, 0, DigitalPortDirection.DigitalOut);
                EastGuidePin = new WisePin("TeleEastGuide", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortB, 1, DigitalPortDirection.DigitalOut);
                WestGuidePin = new WisePin("TeleWestGuide", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortB, 2, DigitalPortDirection.DigitalOut);
                SouthGuidePin = new WisePin("TeleSouthGuide", Hardware.Hardware.Instance.teleboard, DigitalPortType.FirstPortB, 3, DigitalPortDirection.DigitalOut);

                instance.HAEncoder = new WiseHAEncoder("TeleHAEncoder");
                instance.DecEncoder = new WiseDecEncoder("TeleDecEncoder");
            }
            catch (WiseException e)
            {
               debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseTele constructor caught: {0}.", e.Message);
            }

            instance.NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin, TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing, new List<object> { instance.DecEncoder });
            instance.SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin, TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing, new List<object> { instance.DecEncoder });
            instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });
            instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin, TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });
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
                startMovement = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),

                //anglePerSecond = new Angle("02:00:0.0"),
                //changeDirection = new Angle("00:00:30.0"),
                //startMovement = new Angle("00:00:20.0"),
                //stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 500.0,      // 2deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = Angle.FromHours(Angle.Deg2Hours(Const.rateSet)),
                changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:10.0")),
                startMovement = Angle.FromHours(Angle.Deg2Hours("00:00:10.0")),
                millisecondsPerDegree = 60000.0,    // 1min/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = Angle.FromHours(Angle.Deg2Hours(Const.rateGuide)),
                changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                startMovement = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                stopMovement = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateSlew), changeDirection = new Angle("00:00:30.0"),
                startMovement = new Angle("00:00:20.0"),  stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 500.0,      // 2 deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateSet), changeDirection = new Angle("00:00:30.0"),
                startMovement = new Angle("00:00:20.0"), stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 60000.0,    // 1 min/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = new Angle(Const.rateGuide), changeDirection = new Angle("00:00:00.5"),
                startMovement = new Angle("00:00:00.5"), stopMovement = new Angle("00:00:00.5"),
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
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing)] = new MovementWorker(new WiseVirtualMotor[] { EastMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] = new MovementWorker(new WiseVirtualMotor[] { WestMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing)] = new MovementWorker(new WiseVirtualMotor[] { NorthMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing)] = new MovementWorker(new WiseVirtualMotor[] { SouthMotor });

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

            if (_enslaveDome)
            {
                domeSlaveDriver = new DomeSlaveDriver(debugger);
            }
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

            traceLogger.LogMessage("AbortSlew", "");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "AbortSlew");
            Stop();
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
                return astroutils.ConditionHA(HAEncoder.Degrees);
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

    /*
        void equ2hor (double jd_ut1, double delta_t, short int accuracy,
            double xp, double yp, on_surface *location, double ra,
            double dec, short int ref_option,
            double *zd, double *az, double *rar, double *decr)
        PURPOSE:
            This function transforms topocentric right ascension and
            declination to zenith distance and azimuth. It uses a method
            that properly accounts for polar motion, which is significant at
            the sub-arcsecond level. This function can also adjust
            coordinates for atmospheric refraction.
        REFERENCES:
            Kaplan, G. (2008). USNO/AA Technical Note of 28 Apr 2008, "Refraction as a Vector."
        INPUT ARGUMENTS:
            jd_ut1 (double) UT1 Julian date.
            delta_t (double) Difference TT-UT1 at 'jd_ut1', in seconds.
            accuracy (short int) Selection for method and accuracy
                = 0 ... full accuracy
                = 1 ... reduced accuracy
            xp (double) Conventionally-defined xp pole with respect to ITRS reference pole, in arcseconds. coordinate of celestial intermediate
            yp (double) Conventionally-defined yp pole with respect to ITRS reference pole, in arcseconds. coordinate of celestial intermediate
            *location (struct on_surface) Pointer to structure containing observer's location (defined in novas.h).
            ra (double) Topocentric right ascension of object of interest, in hours, referred to true equator and equinox of date.
            dec (double) Topocentric declination of object of interest, in degrees, referred to true equator and equinox of date.
            ref_option (short int)
                = 0 ... no refraction
                = 1 ... include refraction, using 'standard' atmospheric conditions.
                = 2 ... include refraction, using atmospheric parameters input in the 'location' structure.
        OUTPUT ARGUMENTS:
            *zd (double) Topocentric zenith distance in degrees, affected by refraction if 'ref_option' is non-zero.
            *az (double) Topocentric azimuth (measured east from north) in degrees.
            *rar (double) Topocentric right ascension of object of interest, in hours, referred to true equator and equinox of date, affected by refraction if 'ref_option' is non-zero.
            *decr (double) Topocentric declination of object of interest, in degrees referred to true equator and equinox of date, affected by refraction if 'ref_option' is non-zero.
        RETURNED VALUE:
            None.
     */
        public double Azimuth
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd= 0;

                WiseSite.Instance.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    WiseSite.Instance.astrometricAccuracy,
                    0, 0,
                    WiseSite.Instance.onSurface,
                    RightAscension, Declination,
                    WiseSite.Instance.refractionOption,
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

                WiseSite.Instance.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    WiseSite.Instance.astrometricAccuracy,
                    0, 0,
                    WiseSite.Instance.onSurface,
                    RightAscension, Declination,
                    WiseSite.Instance.refractionOption,
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
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Get - {0}", ret));
                return ret;
            }

            set
            {
                traceLogger.LogMessage("Tracking Set", value.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Set - {0}", value));

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
            if (_slewToCoordinatesAsync_bgw != null && _slewToCoordinatesAsync_bgw.IsBusy)
                _slewToCoordinatesAsync_bgw.CancelAsync();

            foreach (WiseVirtualMotor motor in directionMotors)
                if (motor.isOn)
                    motor.SetOff();

            safetyMonitorTimer.SetOff();

            Tracking = false;
            Slewing = false;
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
                bool ret = _slewing || (_enslaveDome && domeSlaveDriver.Slewing);

                traceLogger.LogMessage("Slewing Get", ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("Slewing Get - {0}", ret));

                if (_wasSlewing == true && ret == false)
                    _driverInitiatedSlewing = false;

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

        private void _moveAxis(TelescopeAxes Axis, double Rate, Const.AxisDirection direction = Const.AxisDirection.Increasing, bool stopTracking = false)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): called", Axis, RateName(Rate));

            MovementWorker mover = null;
            TelescopeAxes thisAxis = Axis;
            TelescopeAxes otherAxis = (thisAxis == TelescopeAxes.axisPrimary) ? TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;

            if (thisAxis == TelescopeAxes.axisTertiary)
                throw new InvalidValueException("Cannot move in axisTertiary");

            if (AtPark)
            {
                WiseTele.Instance.currMovement[Axis].rate = Const.rateStopped;
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

                WiseTele.Instance.currMovement[Axis].rate = Const.rateStopped;      // signals _slewToCoordinatesAsync_bgw_DoWork that this Axis is stopped
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): done.", Axis, RateName(Rate));

                _slewing = false;

                return;
            }

            double absRate = Math.Abs(Rate);
            if (! ((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                throw new InvalidValueException(string.Format("_moveAxis({0}, {1}): Invalid rate.", Axis, Rate));

            if (WiseTele.Instance.currMovement[otherAxis].rate != Const.rateStopped && absRate != WiseTele.Instance.currMovement[otherAxis].rate)
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
            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToTargetAsync", string.Format("Started: ra: {0}, dec: {1}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTargetAsync({0}, {1})", ra, dec));

            if (_targetRightAscension == null)
                throw new ValueNotSetException("Target RA not set");
            if (_targetDeclination == null)
                throw new ValueNotSetException("Target Dec not set");

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while NOT Tracking");


            if (! SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToTargetAsync({0}, {1})", ra, dec));

            _driverInitiatedSlewing = true;

            try
            {
                if (_enslaveDome)
                    domeSlaveDriver.SlewStartAsync(ra, dec);
                _slewToCoordinatesAsync(_targetRightAscension, _targetDeclination);
            }
            catch (Exception e)
            {
                _driverInitiatedSlewing = false;
                throw e;
            }
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

            WiseSite.Instance.prepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                WiseSite.Instance.astrometricAccuracy,
                0, 0,
                WiseSite.Instance.onSurface,          // TBD: set Pressure (mbar) and Temperature (C)
                ra.Hours, dec.Degrees,
                WiseSite.Instance.refractionOption,   // TBD: do we want refraction?
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

            if (_enslaveDome)
                domeSlaveDriver.SlewToParkStart();
            _slewToCoordinatesSync(WiseSite.Instance.LocalSiderealTime, WiseSite.Instance.Latitude);
            AtPark = true;
            if (_enslaveDome)
                domeSlaveDriver.SlewWait();
        }

        private void _slewToCoordinatesSync(Angle RightAscension, Angle Declination)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), called.", RightAscension, Declination);
            _slewToCoordinatesAsync(RightAscension, Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), waiting ...", RightAscension, Declination);

            while (Slewing)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), still Slewing ...", RightAscension, Declination);
                Thread.Sleep(500);
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), done.", RightAscension, Declination);
        }

        private void _slewToCoordinatesAsync_bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesAsync_bgw_RunWorkerCompleted: Called: e.Cancelled: {0}, e.Error: \"{1}\"",
                e.Cancelled, e.Error);

            if (!e.Cancelled && e.Error == null)
                result = e.Cancelled ? null : (MeasuredMovementResult)e.Result;

            Slewing = false;
        }

        private void _slewToCoordinatesAsync_bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            MeasuredMovementArg arg = (MeasuredMovementArg)e.Argument;
            BackgroundWorker bgw = sender as BackgroundWorker;
            MeasuredMovementResult result = e.Result as MeasuredMovementResult;

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: arg: ra = {0}, dec = {1}", arg.rightAscension, arg.declination);

            _slewing = true;
            Movement cm;

            result = new MeasuredMovementResult() { cancelled = false };
            e.Cancel = false;
            try
            {
                if (bgw.CancellationPending)
                    throw new MovementCanceledException();

                //
                // Split the distance into segments in which the axes can either:
                //  1. both move at the same rate
                //  2. one stop and the other move at that rate
                //
                foreach (double rate in rates)
                {
                    //
                    // Phase #1
                    //  Prepare each axis' currMovement instance:
                    //   - startingAngle:           The angle before starting the movement
                    //   - longTermTargetAngle:     The angle to which we want to ultimatly get on this axis (ra or dec)
                    //   - shortTermTargetAngle:    The angle we want to reach in the next sub-movement, at the same rate as the other axis
                    //   - direction:               Which way the axis will move
                    //
                    if (bgw.CancellationPending)
                        throw new MovementCanceledException();

                    foreach (TelescopeAxes axis in axes)
                    {
                        MovementParameters mp = movementParameters[axis][rate];
                        WiseTele.Instance.currMovement[axis] = new Movement() { rate = Const.rateStopped };
                        cm = WiseTele.Instance.currMovement[axis];

                        if (axis == TelescopeAxes.axisPrimary)
                        {
                            cm.startingAngle = Angle.FromHours(RightAscension, Angle.Type.RA);
                            cm.longTermTargetAngle = arg.rightAscension;
                        }
                        else
                        {
                            cm.startingAngle = Angle.FromDegrees(Declination, Angle.Type.Dec);
                            cm.longTermTargetAngle = arg.declination;
                        }

                        var shortest = cm.startingAngle.ShortestDistance(cm.longTermTargetAngle);
                        cm.deltaAngle = shortest.angle;
                        cm.direction = shortest.direction;

                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {3}: preliminary: startingAngle: {0}, deltangle: {1}, direction: {2}",
                            cm.startingAngle, cm.deltaAngle, cm.direction, axis);

                        //
                        // A minimal move on this axis, at this rate, is the sum of:
                        //  - the startMovement angle
                        //  - at least one anglePerSecond angle
                        //  - the stopMovement angle
                        //  - if we change direction, the changeDirection angle
                        //
                        // If the total movement needed is less than this total, don't move. Let
                        //  the next iteration, at a lower rate, take care of it.
                        //
                        Angle minimalMovementAngle = mp.startMovement + mp.anglePerSecond + mp.stopMovement;
                        if (prevMovement[axis].direction != Const.AxisDirection.None && prevMovement[axis].direction != cm.direction)
                            minimalMovementAngle += mp.changeDirection;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: minimalMovementAngle: {1}", axis, minimalMovementAngle);

                        if (cm.deltaAngle < minimalMovementAngle)
                        {
                            cm.rate = Const.rateStopped;
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: Not moving at rate {1}, too short", axis, RateName(rate));
                        }
                        else
                        {
                            Angle movementDistance = cm.deltaAngle - movementParameters[axis][rate].anglePerSecond;
                            if (cm.direction == Const.AxisDirection.Increasing)
                                cm.shortTermTargetAngle = cm.startingAngle + movementDistance;
                            else
                                cm.shortTermTargetAngle = cm.startingAngle - movementDistance;

                            cm.rate = rate;
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: startingAngle: {1}, targetAngle: {2}, direction: {3}, rate: {4}",
                                axis, cm.startingAngle, cm.shortTermTargetAngle, cm.direction, RateName(cm.rate));
                        }
                    }

                    if (currMovement[TelescopeAxes.axisPrimary].rate == Const.rateStopped && currMovement[TelescopeAxes.axisSecondary].rate == Const.rateStopped)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: Both axes will NOT to move at rate {0}, will try next rate.", RateName(rate));
                        continue;   // cannot move at this rate, move to next one
                    }

                    if (bgw.CancellationPending)
                        throw new MovementCanceledException();

                    // Phase #2:
                    //  Calculate the minimal distance both axes can move at the same rate.
                    //
                    //Angle commonDeltaAngle = Angle.Min(currMovement[TelescopeAxes.axisPrimary].deltaAngle, currMovement[TelescopeAxes.axisSecondary].deltaAngle);
                    Angle commonDeltaAngle = Angle.FromDegrees(
                        Math.Min(
                            currMovement[TelescopeAxes.axisPrimary].deltaAngle.Degrees,
                            currMovement[TelescopeAxes.axisSecondary].deltaAngle.Degrees));
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: commonDeltaAngle: {0}", commonDeltaAngle);

                    //
                    // Phase #3:
                    //  Start moving on each axis which can move at this rate
                    //
                    List<TelescopeAxes> activeAxes = new List<TelescopeAxes>();
                    foreach (TelescopeAxes axis in axes)
                    {
                        cm = currMovement[axis];

                        if (cm.rate == Const.rateStopped)
                        {
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} STOPPED", axis);
                            continue;
                        }

                        Angle thisMovementAngle = Angle.Max(commonDeltaAngle, cm.deltaAngle);
                        if (cm.direction == Const.AxisDirection.Increasing)
                            cm.shortTermTargetAngle = cm.startingAngle + thisMovementAngle;
                        else
                            cm.shortTermTargetAngle = cm.startingAngle - thisMovementAngle;

                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: Calling MoveAxis({0}, {1}, {2}) startingAngle: {3}, targetAngle: {4}",
                            axis, RateName(rate), cm.direction, cm.startingAngle, cm.shortTermTargetAngle);

                        if (bgw.CancellationPending)
                            throw new MovementCanceledException();

                        _moveAxis(axis, rate, cm.direction, false);   // sets currentMovement[axis].rate != rateStopped
                        activeAxes.Add(axis);
                    }
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} in motion at {1}", activeAxes.ToString(), RateName(rate));

                    //
                    // Phase #4:
                    //  Wait for both axes (or just the one that's moving) to reach their destination
                    //
                    while (true)
                    {
                        if (bgw.CancellationPending)
                            throw new MovementCanceledException();

                        foreach (TelescopeAxes axis in axes)
                        {
                            ShortestDistanceResult delta, prev_delta = new ShortestDistanceResult() { angle = Angle.invalid };
                            char mark = '?';

                            cm = currMovement[axis];
                            if (cm.rate == Const.rateStopped) // movement on this axis was already stopped
                                continue;

                            Angle currentAngle = (axis == TelescopeAxes.axisPrimary) ?
                                Angle.FromHours(RightAscension, Angle.Type.RA) :
                                Angle.FromDegrees(Declination, Angle.Type.Dec);

                            delta = currentAngle.ShortestDistance(cm.shortTermTargetAngle);
                            bool arrivedAtTarget = (delta.angle.Degrees <= closeEnough);

                            if (arrivedAtTarget)
                            {
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} stopping at {5}, calling  MoveAxis({0}, {1}) startingAngle: {2}, targetAngle: {3}, direction {4}",
                                    axis, RateName(Const.rateStopped), cm.startingAngle, cm.shortTermTargetAngle, cm.direction, currentAngle);

                                _moveAxis(axis, Const.rateStopped, Const.AxisDirection.None, false);  // this axis has moved within range of the target, stop it (sets currentMovement[axis].rate = rateStopped)
                            }
                            else
                            {
                                if (prev_delta.angle.Degrees != double.NaN)
                                    mark = (prev_delta == delta) ? '=' : (prev_delta.angle > delta.angle) ? 'v' : '^';

                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} still moving, {1}, at {2} ==> {3} ({6}), delta: {4}{5}",
                                    axis, RateName(cm.rate), currentAngle, cm.shortTermTargetAngle,
                                    mark,
                                    delta.angle, cm.longTermTargetAngle);
                                prev_delta = delta;
                            }
                        }

                        if (currMovement[TelescopeAxes.axisPrimary].rate == Const.rateStopped && currMovement[TelescopeAxes.axisSecondary].rate == Const.rateStopped)
                        {
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: BOTH axes are STOPPED after moving at {0}", RateName(rate));
                            break;
                        }
                        else
                        {
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "sleeping 5 ...");
                            Thread.Sleep(5);    // TODO: make it configurable or constant
                        }
                    }
                }
            } catch (MovementCanceledException)
            {
                result.cancelled = true;
                e.Cancel = true;
            } finally {

                //
                // Phase #5:
                //  Motion complete (or cancelled)
                //
                Stop();
                currMovement[TelescopeAxes.axisPrimary].rate = Const.rateStopped;
                currMovement[TelescopeAxes.axisSecondary].rate = Const.rateStopped;

                foreach (TelescopeAxes axis in axes)
                    prevMovement[axis] = currMovement[axis];    // save for change-of-direction info.

                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: ALL DONE, returning.");
            }
        }

        /// <summary>
        /// This is the common engine for moving the telescope to some known position.
        /// It is a measured-movement, i.e. we don't relly on the telescope getting there
        ///  after a period of time, we frequently monitor the encoders to decide if we're there yet.
        /// If we think we overshot we go back.
        /// 
        /// This is ASYNC.
        /// </summary>
        /// <param name="RightAscension"></param>
        /// <param name="Declination"></param>
        private void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "_slewToCoordinatesAsync({0}, {1}) - called",
                RightAscension, Declination);

            _slewToCoordinatesAsync_bgw = new BackgroundWorker();

            _slewToCoordinatesAsync_bgw.WorkerSupportsCancellation = true;
            _slewToCoordinatesAsync_bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_slewToCoordinatesAsync_bgw_RunWorkerCompleted);
            _slewToCoordinatesAsync_bgw.DoWork += new DoWorkEventHandler(_slewToCoordinatesAsync_bgw_DoWork);

            _slewToCoordinatesAsync_bgw.RunWorkerAsync(new MeasuredMovementArg() { rightAscension = RightAscension, declination = Declination });
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            Angle ra = Angle.FromHours(RightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(Declination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToCoordinates", string.Format("ra: {0}, dec: {0}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToCoordinates - {0}, {1}", ra, dec));

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
                if (_enslaveDome)
                    domeSlaveDriver.SlewStartAsync(ra, dec);
                _slewToCoordinatesSync(ra, dec);
            }
            catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinates: _slewToCoordinatesSync({0}, {1}) threw exception: {2}",
                    ra, dec, e.Message);
                _slewing = false;
            }
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.FromHours(RightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(Declination, Angle.Type.Dec);

            debugger.WriteLine(Common.Debugger.DebugLevel.DebugDevice, "SlewToCoordinatesAsync({0}, {1})", ra, dec);

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToCoordinatesAsync({0}, {1})", ra, dec));

            try
            {
                if (_enslaveDome)
                    domeSlaveDriver.SlewStartAsync(ra, dec);
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
                double ret = WiseSite.Instance.LocalSiderealTime.Hours;

                traceLogger.LogMessage("SiderealTime", "Get - " + ret.ToString());
                return ret;
            }
        }

        public double SiteElevation
        {
            get
            {
                double elevation = WiseSite.Instance.Elevation;

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
                double latitude = WiseSite.Instance.Latitude.Degrees;

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
                double longitude = WiseSite.Instance.Longitude.Degrees;

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
                if (_enslaveDome)
                    domeSlaveDriver.SlewStartAsync(ra, dec);

                SlewToCoordinates(TargetRightAscension, TargetDeclination); // sync

                if (_enslaveDome)
                    domeSlaveDriver.SlewWait();
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
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + 
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
