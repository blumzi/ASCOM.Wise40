using System;
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
            public double rightAscension, declination;
        }

        private class MeasuredMovementResult
        {
            public bool cancelled;
        }

        private MeasuredMovementResult result;

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
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackMotor;

        private bool _slewing = false;
        private bool _atPark;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Target target;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly Dictionary<double, string> rateName = new Dictionary<double, string> {
            { Const.rateStopped,  "rateStopped" },
            { Const.rateSlew,  "rateSlew" },
            { Const.rateSet,  "rateSet" },
            { Const.rateGuide,  "rateGuide" },
            { -Const.rateSlew,  "-rateSlew" },
            { -Const.rateSet,  "-rateSet" },
            { -Const.rateGuide,  "-rateGuide" },
            { Const. rateTrack, "rateTrack" },
        };
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
        private static AutoResetEvent backgroundWorkerDone = new AutoResetEvent(false);

        public double targetDeclination {
            get
            {
                if (target.Declination == null)
                    throw new ValueNotSetException("Target not set");

                traceLogger.LogMessage("TargetDeclination Get", target.Declination.ToFormattedString(Angle.Format.Dec));
                return target.Declination.Degrees;
            }

            set
            {
                if (value < -90 || value > 90)
                    throw new InvalidValueException("Must be between -90 and 90");

                target.Declination = new Angle(value);
                traceLogger.LogMessage("TargetDeclination Set", target.Declination.ToFormattedString(Angle.Format.Dec));
            }
        }

        public double targetRightAscension
        {
            get
            {
                if (target.RightAscension == null)
                    throw new ValueNotSetException("Target not set");

                traceLogger.LogMessage("TargetRightAscension Set", target.RightAscension.ToFormattedString(Angle.Format.RAhms));
                return target.RightAscension.Degrees;
            }

            set
            {
                if (value < 0 || value > 24)
                    throw new ASCOM.InvalidValueException("Must be between 0 to 24");

                target.RightAscension = new Angle(value);
                traceLogger.LogMessage("TargetRightAscension Set", target.RightAscension.ToFormattedString(Angle.Format.RAhms));
            }
        }

        public double mirrorDiam
        {
            get
            {
                return mainMirrorDiam;
            }
        }

        public double mirrorArea
        {
            get
            {
                return Math.PI * Math.Pow(mirrorDiam, 2);
            }
        }

        public bool doesRefraction {
            get
            {
                return false;
            }
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
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
                return _connected;
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
            traceLogger = T.tl;
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            Hardware.Hardware.Instance.init();
            target = new Target();
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

            instance.NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin, Const.AxisDirection.Increasing, new List<object> { instance.DecEncoder });
            instance.SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin, Const.AxisDirection.Decreasing, new List<object> { instance.DecEncoder });
            instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });
            instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });
            instance.TrackMotor = new WiseVirtualMotor("TrackMotor", TrackPin, null, null, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });

            instance.axisMotors = new Dictionary<TelescopeAxes, List<WiseVirtualMotor>>();
            instance.axisMotors[TelescopeAxes.axisPrimary] = new List<WiseVirtualMotor> { instance.EastMotor, instance.WestMotor };
            instance.axisMotors[TelescopeAxes.axisSecondary] = new List<WiseVirtualMotor> { instance.NorthMotor, instance.SouthMotor };

            instance.directionMotors = new List<WiseVirtualMotor>();
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisPrimary]);
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisSecondary]);

            instance.allMotors = new List<WiseVirtualMotor>();
            instance.allMotors.AddRange(instance.directionMotors);
            instance.allMotors.Add(TrackMotor);

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
                anglePerSecond = new Angle("02:00:0.0"), changeDirection = new Angle("00:00:30.0"),
                startMovement = new Angle("00:00:20.0"), stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 500.0,      // 2deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = new Angle("00:01:0.0"), changeDirection = new Angle("00:00:20.0"),
                startMovement = new Angle("00:00:10.0"), stopMovement = new Angle("00:00:10.0"),
                millisecondsPerDegree = 60000.0,    // 1min/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = new Angle("00:00:01.0"), changeDirection = new Angle("00:00:00.5"),
                startMovement = new Angle("00:00:00.5"),  stopMovement = new Angle("00:00:00.5"),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };


            instance.movementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                anglePerSecond = new Angle("02:00:00.0"), changeDirection = new Angle("00:00:30.0"),
                startMovement = new Angle("00:00:20.0"),  stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 500.0,      // 2 deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                anglePerSecond = new Angle("00:01:00.0"), changeDirection = new Angle("00:00:30.0"),
                startMovement = new Angle("00:00:20.0"), stopMovement = new Angle("00:00:20.0"),
                millisecondsPerDegree = 60000.0,    // 1 min/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                anglePerSecond = new Angle("00:00:01.0"), changeDirection = new Angle("00:00:00.5"),
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
            instance.connectables.Add(instance.TrackMotor);
            instance.connectables.Add(instance.HAEncoder);
            instance.connectables.Add(instance.DecEncoder);

            instance.disposables.Add(instance.NorthMotor);
            instance.disposables.Add(instance.EastMotor);
            instance.disposables.Add(instance.WestMotor);
            instance.disposables.Add(instance.SouthMotor);
            instance.disposables.Add(instance.TrackMotor);
            instance.disposables.Add(instance.HAEncoder);
            instance.disposables.Add(instance.DecEncoder);

            SlewPin.SetOff();
            instance.TrackMotor.SetOff();
            instance.NorthMotor.SetOff();
            instance.EastMotor.SetOff();
            instance.WestMotor.SetOff();
            instance.SouthMotor.SetOff();
        }

        public double focalLength
        {
            get
            {
                return 7.112;   // Las Campanas 40" (meters)
            }
        }

        public void abortSlew()
        {
            Stop();
        }

        public double RightAscension
        {
            get
            {
                return HAEncoder.RightAscension;
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
                return DecEncoder.Declination;
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

                return az;
            }
        }

        public double Altitude
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0;

                WiseSite.Instance.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    WiseSite.Instance.astrometricAccuracy,
                    0, 0,
                    WiseSite.Instance.onSurface,
                    RightAscension, Declination,
                    WiseSite.Instance.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                return (90.0 - zd);
            }
        }

        public bool Tracking
        {
            get
            {
                return TrackMotor.isOn; ;
            }

            set
            {
                if (value)
                {
                    TrackMotor.SetOn(Const.rateTrack);
                    if (!safetyMonitorTimer.isOn)
                        safetyMonitorTimer.SetOn();
                }
                else
                {
                    if (TrackMotor.isOn)
                        TrackMotor.SetOff();

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

        public DriveRates TrackingRates
        {
            get
            {
                return DriveRates.driveSidereal;
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
                return _slewing;
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
                return 0.0;
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate, Const.AxisDirection direction = Const.AxisDirection.Increasing, bool stopTracking = false)
        {
            MovementWorker mover = null;
            TelescopeAxes thisAxis = Axis;
            TelescopeAxes otherAxis = (thisAxis == TelescopeAxes.axisPrimary) ? TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;

            if (thisAxis == TelescopeAxes.axisTertiary)
                throw new InvalidValueException("Cannot move in axisTertiary");

            if (AtPark)
                throw new InvalidValueException("Cannot MoveAxis while AtPark");

            if (Rate == Const.rateStopped)
            {
                foreach (WiseVirtualMotor m in axisMotors[thisAxis])
                    if (m.isOn)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "MoveAxis: rateStopped: {0} was on, stopping it.", m.name);
                        m.SetOff();
                    }

                _slewing = false;

                if (wasTracking)
                    Tracking = true;

                WiseTele.Instance.currMovement[Axis].rate = Const.rateStopped;      // signals _slewToCoordinatesAsync_bgw_DoWork that this Axis is stopped
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "MoveAxis: {0}, rate: {1}", Axis, rateName[Rate]);
                return;
            }

            double absRate = Math.Abs(Rate);
            if (! ((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                throw new InvalidValueException(string.Format("Invalid rate {0}", Rate.ToString()));

            if (WiseTele.Instance.currMovement[otherAxis].rate != Const.rateStopped && absRate != WiseTele.Instance.currMovement[otherAxis].rate)
                throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) ({2}) [{3} is moving at {4}]",
                    Axis, rateName[Rate], axisDirectionName[Axis][direction], otherAxis, rateName[WiseTele.Instance.currMovement[otherAxis].rate]));

            try {
                mover = movementDict[new MovementSpecifier(Axis, direction)];
            } catch(Exception e) {
                throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) ({2}) [{3}]",
                    Axis, axisDirectionName[Axis][direction], rateName[Rate], e.Message));
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "MoveAxis: {0}: ({1}), at {2}, stopTracking: {3}", Axis, axisDirectionName[Axis][direction], rateName[Rate], stopTracking);
            wasTracking = Tracking;
            if (stopTracking)
                Tracking = false;
            _slewing = true;

            foreach (WiseVirtualMotor m in mover.motors) {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "MoveAxis: {0}: starting {1}", rateName[Rate], m.name);
                m.SetOn(Rate);
            }

            if (Moving && !safetyMonitorTimer.isOn)
                safetyMonitorTimer.SetOn();
        }

        public bool IsPulseGuiding
        {
            get
            {
                return false;   // TBD
            }
        }

        public void SlewToTargetAsync()
        {
            double deltaRa, deltaDec;

            if (target.RightAscension == null || target.Declination == null)
                throw new ValueNotSetException("Target not set");

            SafeAtCoordinates(target.RightAscension.Degrees, target.Declination.Degrees, false);

            deltaRa = targetRightAscension - RightAscension;
            deltaDec = targetDeclination - Declination;

            /*
             * TODO:
             *  - Split deltaRa and deltaDec into rateSlew, rateSet and rateGuide segments
             *  - Slewing = true
             *  - For each segment
             *      - Start the axis-motor
             *      - Start the axis-timer (frequency = ???)
             *      - The axis-timer handler:
             *          - if the next occurence will be less than (2 * the stoppingTime[currentRate]) away:
             *              - stops the axis-motor
             *              - sets handler to one that throws a new NotImplementedException (to prevent event after Enabled = false)
             *              - sets Enabled = false
             *              - if the other axis-timer is also done, Slewing = false
             */
            _slewToCoordinatesAsync(target.RightAscension.Degrees, target.Declination.Degrees);
        }

        /// <summary>
        /// Checks if we're safe at a given position
        ///  
        /// If (takeAction == true) then take the apropriate recovery action
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        /// <param name="takeAction">Take recovery actions or not</param>
        public bool SafeAtCoordinates(double ra, double dec, bool takeAction = false)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;
            Angle alt;

            WiseSite.Instance.prepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                WiseSite.Instance.astrometricAccuracy,
                0, 0,
                WiseSite.Instance.onSurface,          // TBD: set Pressure (mbar) and Temperature (C)
                ra, dec,
                WiseSite.Instance.refractionOption,   // TBD: do we want refraction?
                ref zd, ref az, ref rar, ref decr);

            alt = Angle.FromDeg(90.0 - zd);
            if (alt.Degrees < altLimit.Degrees)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "Not safe to move to ra: {0}, dec: {1} (alt: {2} is below altLimit: {3})",
                    Angle.FromDeg(ra).ToFormattedString(Angle.Format.RA),
                    Angle.FromDeg(dec).ToFormattedString(Angle.Format.Dec),
                    alt.ToFormattedString(Angle.Format.Deg),
                    altLimit.ToFormattedString(Angle.Format.Deg));

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
            double backoffDeg = 20 / 3600; // 20 seconds (TBD)
            double primaryBackoff = 0, secondaryBackoff = 0;
            double ra = RightAscension, ha = HourAngle, dec = Declination;

            primaryBackoff = (ha > 0) ? ra - backoffDeg : ra + backoffDeg;
            secondaryBackoff = (dec > 0) ? -backoffDeg : backoffDeg;
            SlewToCoordinatesSync(primaryBackoff, secondaryBackoff);
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
                return _atPark;
            }

            set
            {
                _atPark = value;
            }
        }

        public void Park()
        {
            if (AtPark)
                return;

            _slewToCoordinatesSync(WiseSite.Instance.LocalSiderealTime, WiseSite.Instance.Latitude);    // sync
            AtPark = true;
        }

        private void _slewToCoordinatesSync(double RightAscension, double Declination)  // sync
        {
            _slewToCoordinatesAsync(RightAscension, Declination);
            backgroundWorkerDone.WaitOne();
            _slewing = false;
        }

        private void _slewToCoordinatesAsync_bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
                result = e.Cancelled ? null : (MeasuredMovementResult)e.Result;
        }

        private void _slewToCoordinatesAsync_bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            MeasuredMovementArg arg = (MeasuredMovementArg)e.Argument;
            BackgroundWorker bgw = sender as BackgroundWorker;

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: arg: ra = {0}, dec = {1}",
                new Angle(arg.rightAscension), new Angle(arg.declination));

            _slewing = true;
            Movement cm;

            if (bgw.CancellationPending)
                goto workDone;

            //
            // Split the distance into segments in which the axes can either:
            //  1. both move at the same rate
            //  2. one stop and the other move at that rate
            //
            foreach (double rate in rates)
            {
                if (simulated && (rate == Const.rateSet || rate == Const.rateGuide))     // TODO: Major fuckup <=================================
                    break;
                //
                // Phase #1
                //  Prepare each axis' currMovement instance:
                //   - startingAngle:           The angle before starting the movement
                //   - longTermTargetAngle:     The angle to which we want to ultimatly get on this axis (ra or dec)
                //   - shortTermTargetAngle:    The angle we want to reach in the next sub-movement, at the same rate as the other axis
                //   - direction:               Which way the axis will move
                //
                if (bgw.CancellationPending)
                    goto workDone;

                foreach (TelescopeAxes axis in axes)
                {
                    MovementParameters mp = movementParameters[axis][rate];
                    WiseTele.Instance.currMovement[axis] = new Movement() { rate = Const.rateStopped };
                    cm = WiseTele.Instance.currMovement[axis];

                    if (axis == TelescopeAxes.axisPrimary)
                    {
                        cm.startingAngle = new Angle(RightAscension);
                        cm.longTermTargetAngle = new Angle(arg.rightAscension);
                    }
                    else
                    {
                        cm.startingAngle = new Angle(Declination);
                        cm.longTermTargetAngle = new Angle(arg.declination);
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

                    if (cm.deltaAngle < minimalMovementAngle) {
                        cm.rate = Const.rateStopped;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: Not moving at rate {1}, too short", axis, rateName[rate]);
                    } else
                    {
                        Angle movementDistance = cm.deltaAngle - movementParameters[axis][rate].anglePerSecond;
                        if (cm.direction == Const.AxisDirection.Increasing)
                            cm.shortTermTargetAngle = cm.startingAngle + movementDistance;
                        else
                            cm.shortTermTargetAngle = cm.startingAngle - movementDistance;

                        cm.rate = rate;
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: startingAngle: {1}, targetAngle: {2}, direction: {3}, rate: {4}",
                            axis, cm.startingAngle, cm.shortTermTargetAngle, cm.direction, rateName[cm.rate]);
                    }
                }

                if (currMovement[TelescopeAxes.axisPrimary].rate == Const.rateStopped && currMovement[TelescopeAxes.axisSecondary].rate == Const.rateStopped)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: Both axes will NOT to move at rate {0}, will try next rate.", rateName[rate]);
                    continue;   // cannot move at this rate, move to next one
                }

                if (bgw.CancellationPending)
                    goto workDone;

                // Phase #2:
                //  Calculate the minimal distance both axes can move at the same rate.
                //
                Angle commonDeltaAngle = Angle.Min(currMovement[TelescopeAxes.axisPrimary].deltaAngle, currMovement[TelescopeAxes.axisSecondary].deltaAngle);
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: commonDeltaAngle: {0}", commonDeltaAngle);

                //
                // Phase #3:
                //  Start moving on each axis which can move at this rate
                //
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

                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0}: (STARTING) Calling MoveAxis({0}, {1}, {4}) startingAngle: {2}, targetAngle: {3}, direction: {4}, rate: {5}", axis, rateName[rate],
                        cm.startingAngle, cm.shortTermTargetAngle, cm.direction, rateName[cm.rate]);

                    if (bgw.CancellationPending)
                        goto workDone;

                    MoveAxis(axis, rate, cm.direction, false);   // sets currentMovement[axis].rate != rateStopped
                }
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: Axe(s) in motion at {0}", rateName[rate]);

                //
                // Phase #4:
                //  Wait for both axes (or just the one that's moving) to reach their destination
                //
                while (true)
                {
                    if (bgw.CancellationPending) goto
                            workDone;

                    foreach (TelescopeAxes axis in axes)
                    {
                        cm = currMovement[axis];
                        if (cm.rate == Const.rateStopped) // movement on this axis was already stopped
                            continue;

                        double currentDegrees = (axis == TelescopeAxes.axisPrimary) ? RightAscension : Declination;
                        bool arrivedAtTarget = (cm.direction == Const.AxisDirection.Increasing) ?
                            currentDegrees >= cm.shortTermTargetAngle.Degrees :
                            currentDegrees <= cm.shortTermTargetAngle.Degrees;

                        if (arrivedAtTarget)
                        {
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} stopping at {5}, calling  MoveAxis({0}, {1}) startingAngle: {2}, targetAngle: {3}, direction {4}",
                                axis, rateName[Const.rateStopped], cm.startingAngle, cm.shortTermTargetAngle, cm.direction, new Angle(currentDegrees));

                            MoveAxis(axis, Const.rateStopped, Const.AxisDirection.None, false);  // this axis has moved within range of the target, stop it (sets currentMovement[axis].rate = rateStopped)
                        } else
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: {0} moving at {5}, (deg: {2}), {1} (inter: {3}, final: {4})",
                                axis, new Angle(currentDegrees), currentDegrees, cm.shortTermTargetAngle, cm.longTermTargetAngle, rateName[cm.rate]);
                    }

                    if (currMovement[TelescopeAxes.axisPrimary].rate == Const.rateStopped && currMovement[TelescopeAxes.axisSecondary].rate == Const.rateStopped)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: BOTH axes are STOPPED after moving at {0}", rateName[rate]);
                        break;
                    }
                    else
                        Thread.Sleep(5);    // TODO: make it configurable or constant
                }
            }

            //
            // Phase #5:
            //  Motion complete (or cancelled)
            //
            workDone:
            e.Result = new MeasuredMovementResult() { cancelled = false };
            if (bgw.CancellationPending)
            {
                ((MeasuredMovementResult)e.Result).cancelled = true;
                Stop();
            }
            foreach (TelescopeAxes axis in axes)
                prevMovement[axis] = currMovement[axis];    // save for changing-of-direction info.

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "bgw: ALL DONE, returning.");
            backgroundWorkerDone.Set();
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
        private void _slewToCoordinatesAsync(double RightAscension, double Declination)
        {
            _slewToCoordinatesAsync_bgw = new BackgroundWorker();

            _slewToCoordinatesAsync_bgw.WorkerSupportsCancellation = true;
            _slewToCoordinatesAsync_bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_slewToCoordinatesAsync_bgw_RunWorkerCompleted);
            _slewToCoordinatesAsync_bgw.DoWork += new DoWorkEventHandler(_slewToCoordinatesAsync_bgw_DoWork);

            _slewToCoordinatesAsync_bgw.RunWorkerAsync(new MeasuredMovementArg() { rightAscension = RightAscension, declination = Declination });
        }

        public void SlewToCoordinatesSync(double RightAscension, double Declination) // sync
        {
            try
            {
                _slewToCoordinatesSync(RightAscension, Declination);
            }
            catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinates: _slewToCoordinatesSync({0}, {1}) threw exception: {2}", new Angle(RightAscension), new Angle(Declination), e.Message);
                _slewing = false;
            }
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            try
            {
                _slewToCoordinatesAsync(RightAscension, Declination);
            } catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinatesAsync: _slewToCoordinatesAsync({0}, {1}) threw exception: {2}", new Angle(RightAscension), new Angle(Declination), e.Message);
                _slewing = false;
            }
        }

        private void DoCheckSafety(object StateObject)
        {
            SafeAtCoordinates(RightAscension, Declination, true);
        }
    }
}
