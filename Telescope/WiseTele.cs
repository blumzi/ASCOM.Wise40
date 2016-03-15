using System;
using System.Collections.Generic;
using ASCOM.Utilities;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.DeviceInterface;

using MccDaq;
using ASCOM.Wise40;
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
    public sealed class WiseTele : IDisposable, IConnectable
    {
        public static NOVAS31 Novas31;
        public static ASCOM.Utilities.Util ascomUtil;
        public Astrometry.Accuracy accuracy;
        public enum Speeds { Slew, Set, Guide };
        public enum Direction { North = 0, East = 1, South = 2, West = 3 };
        public enum MotorState { Off = 0, On = 1 };

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        private TraceLogger logger;

        private bool connected = false;
        private bool isInitialized = false;


        private List<WiseMotor> directionMotors;
        private List<WiseMotor> directionGuideMotors;
        private List<WiseMotor> primaryAxisMotors, secondaryAxisMotors;

        private WiseHAEncoder HAEncoder;
        private WiseDecEncoder DecEncoder;

        public WiseMotor TrackMotor, SlewMotor;
        public WiseMotor NorthMotor, SouthMotor, EastMotor, WestMotor;
        public WiseMotor NorthGuideMotor, SouthGuideMotor, EastGuideMotor, WestGuideMotor;

        private Speeds current_speed = Speeds.Guide;
        private bool tracking = false;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Target _target;

        public const double rateSlew = 2.0;            // two degrees/sec
        public const double rateSet = 1.0 / 60;        // one minute/sec
        public const double rateGuide = rateSet / 60;  // one second/sec
        public const double rateSlewSet = rateSlew + rateSet;
        public const double rateSlewGuide = rateSlew + rateGuide;
        public const double rateSetGuide = rateSet + rateGuide;
        public const double rateSlewSetGuide = rateSlew + rateSet + rateGuide;

        public MovementDictionary moveDict;
        private bool wasTracking;

        // TBD: Can we turn on/off motor combinations?


        public double targetDeclination {
            get
            {
                if (_target.Dec == null)
                    throw new ValueNotSetException("Target not set");

                logger.LogMessage("TargetDeclination Get", _target.Dec.ToString(":"));
                return (double) _target.Dec.Deg;
            }

            set
            {
                if (value < -90 || value > 90)
                    throw new InvalidValueException("Must be between -90 and 90");

                var val = new DMS(value);
                logger.LogMessage("TargetDeclination Set", val.Deg.ToString());
                _target.Dec = val;
            }
        }

        public double targetRightAscension
        {
            get
            {
                if (_target.Ra == null)
                    throw new ValueNotSetException("Target not set");

                logger.LogMessage("TargetRightAscension Set", _target.Ra.ToString(":"));
                return (double) _target.Ra.Deg;
            }

            set
            {
                if (value < 0 || value > 24)
                    throw new ASCOM.InvalidValueException("Must be between 0 to 24");

                var val = new DMS(value);
                logger.LogMessage("TargetRightAscension Set", val.Deg.ToString());
                _target.Ra = val;
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
            this.connected = connected;
        }

        public void log(string fmt, params object[] o)
        {
            string msg = String.Format(fmt, o);

            logger.LogMessage("WiseTele", msg);
        }

        private static readonly WiseTele instance = new WiseTele(); // Singleton

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseTele()
        {
        }

        private WiseTele()
        {
        }

        public static WiseTele Instance
        {
            get
            {
                return instance;
            }
        }

        public void init(TraceLogger tl, Astrometry.Accuracy acc)
        {
            if (isInitialized)
                return;

            WisePin TrackPin = null, SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pinsisInitialized = true;

            logger = tl;
            accuracy = acc;
            Novas31 = new NOVAS31();
            ascomUtil = new Util();
            Hardware.Hardware.Instance.init();
            _target = new Target();

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

                instance.HAEncoder = new WiseHAEncoder();
                instance.DecEncoder = new WiseDecEncoder();
            }
            catch (WiseException e)
            {
                instance.log("WiseTele constructor caught: {0}.", e.Message);
            }

            instance.NorthGuideMotor = new WiseMotor("NorthGuideMotor", NorthGuidePin, 0.0, new List<object> { instance.DecEncoder }, true);
            instance.SouthGuideMotor = new WiseMotor("SouthGuideMotor", SouthGuidePin, 0.0, new List<object> { instance.DecEncoder }, false);
            instance.EastGuideMotor = new WiseMotor("EastGuideMotor", EastGuidePin, 0.0, new List<object> { instance.HAEncoder }, true);
            instance.WestGuideMotor = new WiseMotor("WestGuideMotor", WestGuidePin, 0.0, new List<object> { instance.HAEncoder }, false);

            instance.NorthMotor = new WiseMotor("NorthMotor", NorthPin, 0.0, new List<object> { instance.DecEncoder }, true);
            instance.SouthMotor = new WiseMotor("SouthMotor", SouthPin, 0.0, new List<object> { instance.DecEncoder }, false);
            instance.EastMotor = new WiseMotor("EastMotor", EastPin, 0.0, new List<object> { instance.HAEncoder }, true);
            instance.WestMotor = new WiseMotor("WestMotor", WestPin, 0.0, new List<object> { instance.HAEncoder }, false);

            instance.TrackMotor = new WiseMotor("TrackMotor", TrackPin, 0.0, new List<object> { instance.HAEncoder, instance.DecEncoder }, true);
            instance.SlewMotor = new WiseMotor("SlewMotor", SlewPin, 0.0, new List<object> { instance.HAEncoder, instance.DecEncoder }, true);

            instance.directionMotors = new List<WiseMotor>(4) { instance.NorthMotor, instance.EastMotor, instance.SouthMotor, instance.WestMotor };
            instance.directionGuideMotors = new List<WiseMotor>(4) { instance.NorthGuideMotor, instance.EastGuideMotor, instance.SouthGuideMotor, instance.WestGuideMotor };

            instance.moveDict = new MovementDictionary();
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateSlew, 1)] = new MovementWorker(new WiseMotor[] { EastMotor }, true);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateSlew, -1)] = new MovementWorker(new WiseMotor[] { WestMotor }, true);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateSet, 1)] = new MovementWorker(new WiseMotor[] { EastMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateSet, -1)] = new MovementWorker(new WiseMotor[] { WestMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateGuide, 1)] = new MovementWorker(new WiseMotor[] { EastGuideMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisPrimary, rateGuide, -1)] = new MovementWorker(new WiseMotor[] { WestGuideMotor }, false);

            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateSlew, 1)] = new MovementWorker(new WiseMotor[] { NorthMotor }, true);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateSlew, -1)] = new MovementWorker(new WiseMotor[] { SouthMotor }, true);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateSet, 1)] = new MovementWorker(new WiseMotor[] { NorthMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateSet, -1)] = new MovementWorker(new WiseMotor[] { SouthMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateGuide, 1)] = new MovementWorker(new WiseMotor[] { NorthGuideMotor }, false);
            instance.moveDict[new MovementSpecifier(TelescopeAxes.axisSecondary, rateGuide, -1)] = new MovementWorker(new WiseMotor[] { SouthGuideMotor }, false);

            instance.primaryAxisMotors = new List<WiseMotor>() { EastMotor, EastGuideMotor, WestMotor, WestGuideMotor };
            instance.secondaryAxisMotors = new List<WiseMotor>() { NorthMotor, NorthGuideMotor, SouthMotor, SouthGuideMotor };

            instance.connectables.Add(instance.SlewMotor);
            instance.connectables.Add(instance.TrackMotor);
            instance.connectables.Add(instance.NorthMotor);
            instance.connectables.Add(instance.EastMotor);
            instance.connectables.Add(instance.WestMotor);
            instance.connectables.Add(instance.SouthMotor);
            instance.connectables.Add(instance.NorthGuideMotor);
            instance.connectables.Add(instance.EastGuideMotor);
            instance.connectables.Add(instance.WestGuideMotor);
            instance.connectables.Add(instance.SouthGuideMotor);
            instance.connectables.Add(instance.HAEncoder);
            instance.connectables.Add(instance.DecEncoder);

            instance.disposables.Add(instance.SlewMotor);
            instance.disposables.Add(instance.TrackMotor);
            instance.disposables.Add(instance.NorthMotor);
            instance.disposables.Add(instance.EastMotor);
            instance.disposables.Add(instance.WestMotor);
            instance.disposables.Add(instance.SouthMotor);
            instance.disposables.Add(instance.NorthGuideMotor);
            instance.disposables.Add(instance.EastGuideMotor);
            instance.disposables.Add(instance.WestGuideMotor);
            instance.disposables.Add(instance.SouthGuideMotor);
            instance.disposables.Add(instance.HAEncoder);
            instance.disposables.Add(instance.DecEncoder);

            instance.SlewMotor.SetOff();
            instance.TrackMotor.SetOff();
            instance.NorthMotor.SetOff();
            instance.EastMotor.SetOff();
            instance.WestMotor.SetOff();
            instance.SouthMotor.SetOff();
            instance.NorthGuideMotor.SetOff();
            instance.EastGuideMotor.SetOff();
            instance.WestGuideMotor.SetOff();
            instance.SouthGuideMotor.SetOff();
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


        public bool atHome
        {
            get
            {
                return false; // TBD
            }
        }

        public bool atPark
        {
            get
            {
                return false; // TBD
            }
        }

        public void findHome()
        {
            // TBD
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
                return HAEncoder.HourAngle;
            }
        }

        public double Declination
        {
            get
            {
                return DecEncoder.Declination;
            }
        }

        public double Azimuth
        {
            get
            {
                return 0.0; // TBD
            }
        }

        public double Altitude
        {
            get
            {
                return 0.0; // TBD
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
                return tracking;
            }
            set
            {
                tracking = value;
                if (tracking)
                    TrackMotor.SetOn();
                else
                    TrackMotor.SetOff();
            }
        }

        public void Stop()
        {
            foreach (WiseMotor motor in directionMotors)
                motor.SetOff();
            foreach (WiseMotor motor in directionGuideMotors)
                motor.SetOff();

            Tracking = false;
            SlewMotor.SetOff();
        }

        public bool Slewing
        {
            get
            {
                List<WiseMotor> motors = new List<WiseMotor>(directionMotors);
                motors.AddRange(directionGuideMotors);

                foreach (WiseMotor motor in motors)
                    if (motor.isOn)
                        return true;
                return false;
            }
        }

        public double DeclinationRate
        {
            get
            {
                return 0.0;
            }
        }

        public void MoveAxis(ASCOM.DeviceInterface.TelescopeAxes Axis, double Rate)
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

            double rate = Math.Abs(Rate);
            int sign = (Rate < 0) ? -1 : 1;

            try {
                //Console.WriteLine("MoveAxis({0}, {1}):\n{2}", Axis, Rate, moveDict);
                mover = moveDict[new MovementSpecifier(Axis, rate, sign)];
            } catch(Exception e) {
                throw new InvalidValueException(string.Format("Cannot MoveAxis({0}, {1}) [{2}]", Axis, Rate, e.Message));
            }

            wasTracking = Tracking;
            Tracking = false;

            foreach (WiseMotor m in mover.motors)
                m.SetOn();
            if (mover.slew)
                SlewMotor.SetOn();
            else
                SlewMotor.SetOff();
        }

        public bool IsPulseGuiding
        {
            get
            {
                return false;   // TBD
            }
        }
    }
}
