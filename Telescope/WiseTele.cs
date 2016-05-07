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
        private static NOVAS31 novas31;
        private static Util ascomutils;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        public enum Speeds { Slew, Set, Guide };
        public enum Direction { North = 0, East = 1, South = 2, West = 3 };
        public enum MotorState { Off = 0, On = 1 };

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        private TraceLogger logger;

        private bool _connected = false;
        private bool isInitialized = false;
        private bool _simulated = false;

        public Astrometry.SkyPos parkPos;

        private List<WiseMotor> directionMotors;
        private List<WiseMotor> primaryAxisMotors, secondaryAxisMotors, allMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        public WisePin TrackPin;
        public WiseMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackMotor;

        private Speeds current_speed = Speeds.Guide;
        private bool _tracking = false;
        private bool _slewing = false;
        private bool _atPark;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Target target;

        public const double rateSlew = 2.0;                           // two degrees/sec
        public const double rateSet = 1.0 / 60;                       // one minute/sec
        public const double rateGuide = rateSet / 60;                 // one second/sec
        public const double rateTrack = Const.TRACKRATE_SIDEREAL;     // sidereal rate
        public const double rateStopped = 0.0;
        public double primaryRate = rateStopped, secondaryRate = rateStopped;

        public Dictionary<double, int> stoppingTimes;                // how long (millis) it takes to stop when moving at a given rate

        private Angle altLimit = new Angle("0:14:0.0");              // telescope must not go below this Altitude (14 min)

        public MovementDictionary movementDict;
        private bool wasTracking;

        private System.Threading.Timer safetyMonitorTimer;

        public BackgroundWorker _slewToCoordinatesAsync_bgw;

        public double targetDeclination {
            get
            {
                if (target.Declination == null)
                    throw new ValueNotSetException("Target not set");

                logger.LogMessage("TargetDeclination Get", target.Declination.ToString(Angle.Format.Dec));
                return target.Declination.Degrees;
            }

            set
            {
                if (value < -90 || value > 90)
                    throw new InvalidValueException("Must be between -90 and 90");

                target.Declination = new Angle(value);
                logger.LogMessage("TargetDeclination Set", target.Declination.ToString(Angle.Format.Dec));
            }
        }

        public double targetRightAscension
        {
            get
            {
                if (target.RightAscension == null)
                    throw new ValueNotSetException("Target not set");

                logger.LogMessage("TargetRightAscension Set", target.RightAscension.ToString(Angle.Format.RAhms));
                return target.RightAscension.Degrees;
            }

            set
            {
                if (value < 0 || value > 24)
                    throw new ASCOM.InvalidValueException("Must be between 0 to 24");

                target.RightAscension = new Angle(value);
                logger.LogMessage("TargetRightAscension Set", target.RightAscension.ToString(Angle.Format.RAhms));
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
                return true;
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

            logger.LogMessage("WiseTele", msg);
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

        public void init(TraceLogger tl)
        {
            if (isInitialized)
                return;

            WisePin SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pinsisInitialized = true;

            logger = tl;
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            Hardware.Hardware.Instance.init();
            target = new Target();
            List<ISimulated> hardware_elements = new List<ISimulated>();
            const int Increase = 1, Decrease = -1;

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
                instance.log("WiseTele constructor caught: {0}.", e.Message);
            }

            instance.NorthMotor = new WiseMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin, new List<object> { instance.DecEncoder }, true);
            instance.SouthMotor = new WiseMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin, new List<object> { instance.DecEncoder }, false);
            instance.EastMotor = new WiseMotor("EastMotor", EastPin, EastGuidePin, SlewPin, new List<object> { instance.HAEncoder }, false);
            instance.WestMotor = new WiseMotor("WestMotor", WestPin, WestGuidePin, SlewPin, new List<object> { instance.HAEncoder }, true);
            instance.TrackMotor = new WiseMotor("TrackMotor", TrackPin, null, null, new List<object> { instance.HAEncoder }, true);

            instance.directionMotors = new List<WiseMotor>() { instance.NorthMotor, instance.EastMotor, instance.SouthMotor, instance.WestMotor };

            instance.allMotors = new List<WiseMotor>();
            instance.allMotors.AddRange(instance.directionMotors);


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

            parkPos = new Astrometry.SkyPos();
            parkPos.RA = ascomutils.DMSToDegrees("00:00:00.0");
            parkPos.Dec = WiseSite.Instance.Latitude;

            //safetyMonitorTimer = new System.Timers.Timer(100);       // safety check every 100 millis
            //safetyMonitorTimer.Elapsed += SafetyMonitorTimer_Elapsed;
            System.Threading.TimerCallback safetyMonitorTimerCallback = new System.Threading.TimerCallback(DoCheckSafety);
            safetyMonitorTimer = new System.Threading.Timer(safetyMonitorTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            instance.movementDict = new MovementDictionary();
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Decrease)] = new MovementWorker(new WiseMotor[] { EastMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Increase)] = new MovementWorker(new WiseMotor[] { WestMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Increase)] = new MovementWorker(new WiseMotor[] { NorthMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Decrease)] = new MovementWorker(new WiseMotor[] { SouthMotor });

            instance.primaryAxisMotors = new List<WiseMotor>() { instance.EastMotor, instance.WestMotor };
            instance.secondaryAxisMotors = new List<WiseMotor>() { instance.NorthMotor, instance.SouthMotor };

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

            stoppingTimes = new Dictionary<double, int>(3);
            if (simulated)
            {
                stoppingTimes[rateSlew] = 600;      // millis
                stoppingTimes[rateSet] = 60;
                stoppingTimes[rateGuide] = 5;
            } else
            {
                stoppingTimes[rateSlew] = 600;      // TBD: change to actual measured values
                stoppingTimes[rateSet] = 60;
                stoppingTimes[rateGuide] = 5;
            }
        }

        private void SafetyMonitorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckSafety(RightAscension, Declination, true);
        }

        public double focalLength
        {
            get
            {
                return 7.112;   // Las Cmpanas 40" (meters)
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

        public Speeds Speed
        {
            get
            {
                return current_speed;
            }
            set
            {
                current_speed = value;
            }
        } 

        public bool Tracking
        {
            get
            {
                return _tracking;
            }

            set
            {
                _tracking = value;
                if (_tracking)
                {
                    TrackMotor.SetOn(WiseTele.rateTrack);
                    safetyMonitorTimer.Change(100, 100);
                }
                else
                    if (TrackMotor.isOn)
                        TrackMotor.SetOff();
            }
        }

        public void Stop()
        {
            foreach (WiseMotor motor in allMotors)
                if (motor.isOn)
                    motor.SetOff();

            //safetyMonitorTimer.Enabled = false;
            safetyMonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Tracking = false;
            Slewing = false;
        }

        public bool Moving
        {
            get
            {
                foreach (WiseMotor m in allMotors)
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

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            MovementWorker mover = null;

            if (Axis == TelescopeAxes.axisTertiary)
                throw new InvalidValueException("Cannot move in axisTertiary");

            if (Rate == 0)
            {
                switch (Axis)
                {
                    case TelescopeAxes.axisPrimary:
                        foreach (WiseMotor m in primaryAxisMotors)
                            m.SetOff();
                        break;
                    case TelescopeAxes.axisSecondary:
                        foreach (WiseMotor m in secondaryAxisMotors)
                            m.SetOff();
                        break;
                }

                if (wasTracking)
                    Tracking = true;
                return;
            }

            if (AtPark)
                throw new InvalidValueException("Cannot MoveAxis while AtPark");

            double rate = Math.Abs(Rate);
            int sign = (Rate < 0) ? -1 : 1;

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary:
                    if (secondaryRate != 0.0 && Rate != secondaryRate)
                        throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) [secondaryAxis is moving at {2}]", Axis, Rate, secondaryRate));
                    break;

                case TelescopeAxes.axisSecondary:
                    if (primaryRate != 0.0 && Rate != primaryRate)
                        throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) [primaryAxis is moving at {2}]", Axis, Rate, primaryRate));
                    break;
            }

            try {
                mover = movementDict[new MovementSpecifier(Axis, sign)];
            } catch(Exception e) {
                throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) [{2}]", Axis, Rate, e.Message));
            }

            wasTracking = Tracking;
            Tracking = false;
            Slewing = true;

            foreach (WiseMotor m in mover.motors)
                m.SetOn(Rate);

            if (Moving)
                safetyMonitorTimer.Change(100, 100);
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
            //System.Timers.Timer primaryTimer, secondaryTimer;

            if (target.RightAscension == null || target.Declination == null)
                throw new ValueNotSetException("Target not set");

            CheckSafety(target.RightAscension.Degrees, target.Declination.Degrees, false);

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

            Slewing = true;
            _slewToCoordinatesAsync(target.RightAscension.Degrees, target.Declination.Degrees);
        }

        /// <summary>
        /// Checks if we're safe at a given position
        ///  
        /// If (doRecover == true) then take the apropriate recovery action
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        /// <param name="doRecover">Take recovery actions or not</param>
        public bool CheckSafety(double ra, double dec, bool doRecover)
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
                WiseTele.Instance.log("Not safe to move to ra: {0}, dec: {1} (alt: {2} is below altLimit: {3})",
                    Angle.FromDeg(ra).ToString(Angle.Format.RA),
                    Angle.FromDeg(dec).ToString(Angle.Format.Dec),
                    alt.ToString(Angle.Format.Deg),
                    altLimit.ToString(Angle.Format.Deg));

                if (doRecover)
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
            SlewToCoordinates(primaryBackoff, secondaryBackoff);
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

            _slewToCoordinates(parkPos.RA, parkPos.Dec);    // sync
            AtPark = true;
        }

        private void _slewToCoordinates(double RightAscension, double Declination)  // sync
        {
            // Can we sleep on a channel and be awaken by the async-completed event?

            _slewToCoordinatesAsync(RightAscension, Declination);
            while (_slewToCoordinatesAsync_bgw.IsBusy)
            {
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// This is the common engine for moving the telescope to some known position.
        /// It is a measured-movement, i.e. we don't relly on the telescope getting there
        ///  after a period of time, we permanently (or very frequently) monitor the encoders
        ///  to decide if we're there yet.
        /// If we think we overshot we go back.
        /// 
        /// This is ASYNC.
        /// </summary>
        /// <param name="RightAscension"></param>
        /// <param name="Declination"></param>
        private void _slewToCoordinatesAsync(double RightAscension, double Declination)
        {

        }

        public void SlewToCoordinates(double RightAscension, double Declination) // sync
        {
            Slewing = true;
            _slewToCoordinates(RightAscension, Declination);
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            Slewing = true;
            _slewToCoordinatesAsync(RightAscension, Declination);
        }

        private void DoCheckSafety(object StateObject)
        {
            //CheckSafety(RightAscension, Declination, true);
        }
    }
}
