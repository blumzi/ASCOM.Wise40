using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;

using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public abstract class AxisMonitor : WiseObject, IConnectable
    {
        /// <summary>
        /// An AxisMonitor supplies the following functionality:
        /// 
        ///   1. Can tell whether its axis is moving or stationary.
        ///   
        ///   2. Rejects spurious encoder readings.  We get this quite a lot, specially on the Dec axis.
        ///      The encoders have parallel outputs (i.e. one wire per bit).  The longer the cables are and
        ///      the more motors are in their vecinity, the more flipped bits occur.  The cables from the Dec 
        ///      encoders are longer and pass near more motors within the telescopes body.
        ///      
        ///   3. Provides the last-known-as-good coordinate(s) for its axis
        ///   
        /// </summary>
        public struct AxisPosition
        {
            public double radians;
        };

        public AxisPosition _prevPosition = new AxisPosition { radians = double.NaN };
        public AxisPosition _currPosition = new AxisPosition { radians = double.NaN };
        public static FixedSizedQueue<AxisPosition> _positions = new FixedSizedQueue<AxisPosition>(nSamples);
        public TelescopeAxes _axis;
        public WiseTele wisetele = WiseTele.Instance;
        public bool _connected = false;
        public Debugger debugger = Debugger.Instance;
        public WiseSite wisesite = WiseSite.Instance;
        public static Astrometry.AstroUtils.AstroUtils astroutils;

        public const int _samplingFrequency = 20;     // samples per second
        public const double simulatedDelta = 0.4;
        public const int nSamples = 1000 / _samplingFrequency;  // a second's worth of samples

        public FixedSizedQueue<AxisPosition> _samples = new FixedSizedQueue<AxisPosition>(nSamples);

        protected static double _maxDeltaRadiansAtSlewRate = new Angle("5d00m00s").Radians;

        /// <summary>
        /// A background Task that checks whether the telescope axis is moving
        ///  - primaryAxis: RightAscension should not change if Tracking
        ///  - secondaryAxis: Declination should not change.
        /// </summary>
        public Task movementCheckerTask;
        public static CancellationTokenSource movementCheckerCancellationTokenSource;
        public static CancellationToken movementCheckerCancellationToken;

        public System.Threading.Timer movementCheckerTimer;

        public AxisMonitor(TelescopeAxes axis)
        {
            _axis = axis;
            Name = _axis.ToString() + "Monitor";
            wisesite.init();
        }

        public abstract bool IsMoving { get; }

        public double deltaT
        {
            get
            {
                return 1000.0 / _samplingFrequency;     // milliseconds
            }
        }

        /// <summary>
        /// Calculates axis velocity based on the last two samples
        /// </summary>
        /// <returns>velocity in arcsec/sec </returns>
        public abstract double Velocity();

        /// <summary>
        /// Calculates axis acceleration based on the last three samples
        /// </summary>
        /// <returns>acceleration in arcsec/sec-squared </returns>
        public double Acceleration()
        {
            AxisPosition[] arr = _samples.ToArray();
            int last = arr.Count() - 1;

            if (arr.Count() < 3)
                return double.NaN;

            double dT = (1000 / _samplingFrequency);
            double dVLast = Math.Abs(arr[last].radians - arr[last - 1].radians) / dT;
            double dVPrev = Math.Abs(arr[last - 1].radians - arr[last - 2].radians) / dT;

            return (dVLast - dVPrev) / dT;
        }

        public string ActiveMotors(TelescopeAxes axis)
        {
            string ret = string.Empty;

            List<WiseVirtualMotor> motors = new List<WiseVirtualMotor>(wisetele.axisMotors[axis]);
            if (axis == TelescopeAxes.axisPrimary)
                motors.Add(wisetele.TrackingMotor);
            foreach (var m in motors)
                if (m.isOn)
                {
                    ret += m.Name + " (" + WiseTele.RateName(m.currentRate) + ") ";
                }
            return ret;
        }

        protected abstract void SampleAxisMovement(object StateObject);

        /// <summary>
        /// Tests whether an encoder reading (transformed into radians) is acceptable.  This allows 
        /// rejecting spurious encoder readings.
        /// 
        /// It should be a  multi-tiered process:
        ///  1. Is it between the highest and lowest reading the respective axis can produce
        ///  2. Is it reasonably close to the previous reading (if one is available)
        ///     - must be less than the max delta at the current speed (or at least at Slew speed)
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        protected abstract bool Acceptable(double rad);

        public void AxisMovementChecker()
        {
            TimerCallback axisMovementTimerCallback = new TimerCallback(SampleAxisMovement);
            movementCheckerTimer = new System.Threading.Timer(axisMovementTimerCallback);
            movementCheckerTimer.Change(0, 1000 / _samplingFrequency);
        }

        public void StartMovementChecker()
        {
            movementCheckerCancellationTokenSource = new CancellationTokenSource();
            movementCheckerCancellationToken = movementCheckerCancellationTokenSource.Token;

            try
            {
                movementCheckerTask = Task.Run(() =>
                {
                    AxisMovementChecker();
                }, movementCheckerCancellationToken);
            }
            catch (OperationCanceledException)
            {
                movementCheckerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StopMovementChecker()
        {
            if (movementCheckerCancellationTokenSource != null)
                movementCheckerCancellationTokenSource.Cancel();

            if (movementCheckerTimer != null)
                movementCheckerTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Connect(bool value)
        {
            if (value == _connected)
                return;

            if (value)
                StartMovementChecker();
            else
                StopMovementChecker();
            _connected = value;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (value == _connected)
                    return;

                if (value)
                    StartMovementChecker();
                else
                    StopMovementChecker();

                _connected = value;
            }
        }
    }

    public class PrimaryAxisMonitor : AxisMonitor
    {
        //public bool _whileTracking = false;
        //public WiseVirtualMotor trackingMotor = WiseTele.Instance.TrackingMotor;

        public const double raEpsilon = 1e-5;        // epsilon for primaryMonitor, while tracking
        public const double haEpsilon = 7.0;         // epsilon for primaryMonitor, while NOT tracking

        public static FixedSizedQueue<double> _raDeltas = new FixedSizedQueue<double>(nSamples);
        public static FixedSizedQueue<double> _haDeltas = new FixedSizedQueue<double>(nSamples);

        private double _rightAscension = double.NaN, _hourAngle = double.NaN;
        private double _prevRightAscension = double.NaN, _prevHourAngle = double.NaN;

        public PrimaryAxisMonitor() : base(TelescopeAxes.axisPrimary) { }

        private WiseHAEncoder _encoder = WiseTele.Instance.HAEncoder;

        protected override void SampleAxisMovement(object StateObject)
        {
            double reading;

            while (!Acceptable(reading = _encoder.Angle.Radians))
                ;

            _currPosition.radians = reading;

            if (Double.IsNaN(_prevPosition.radians))
            {
                Angle currentAngle = Angle.FromRadians(_currPosition.radians);

                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevHourAngle = currentAngle.Hours;
                _prevRightAscension = (wisesite.LocalSiderealTime - currentAngle).Hours;
                return;
            }

            _rightAscension = (wisesite.LocalSiderealTime - Angle.FromRadians(_currPosition.radians)).Hours;
            _hourAngle = Angle.FromRadians(_currPosition.radians).Hours;
            _samples.Enqueue(_currPosition);

            double raDelta = Math.Abs(_rightAscension - _prevRightAscension);
            double haDelta = Math.Abs(_hourAngle - _prevHourAngle);
            _raDeltas.Enqueue(raDelta);
            _haDeltas.Enqueue(haDelta);


            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:SampleAxisMovement: _currPosition: {1}, _prevPosition: {2}, raDelta: {3}, haDelta: {4}, active motors: {5}",
                Name, _currPosition.radians, _prevPosition.radians, raDelta, haDelta, ActiveMotors(_axis));
            #endregion

            _prevPosition.radians = _currPosition.radians;
            _prevHourAngle = _hourAngle;
            _prevRightAscension = _rightAscension;
        }

        public Angle RightAscension
        {
            get
            {
                //return astroutils.ConditionRA(_rightAscension);
                return Angle.FromHours(_rightAscension);
            }
        }

        public Angle HourAngle
        {
            get
            {
                //return astroutils.ConditionHA(_hourAngle);
                return Angle.FromHours(_hourAngle);
            }
        }

        public override bool IsMoving
        {
            get
            {
                double max = double.MinValue;
                bool tracking = wisetele.Tracking;
                double[] arr = (tracking) ? _raDeltas.ToArray() : _haDeltas.ToArray();
                double epsilon = double.NaN;

                if (tracking)
                {
                    arr = _raDeltas.ToArray();
                    if (arr.Count() < _raDeltas.MaxSize)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: Not enough samples {1} < {2} = true",
                            Name, arr.Count(), _raDeltas.MaxSize);
                        #endregion
                        return false;    // not enough samples
                    }
                    epsilon = raEpsilon;
                } else
                {
                    arr = _haDeltas.ToArray();
                    if (arr.Count() < _haDeltas.MaxSize)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: Not enough samples {1} < {2} = true",
                            Name, arr.Count(), _haDeltas.MaxSize);
                        #endregion
                        return false;    // not enough samples
                    }
                    epsilon = haEpsilon;
                }

                foreach (double d in arr)
                    if (d > max)
                        max = d;
                
                bool ret = max > epsilon;

                #region debug
                string deb = string.Format("{0}:IsMoving: max: {1:F15}, epsilon: {2:F15}, ret: {3}, active: {4}",
                    Name, max, epsilon, ret, ActiveMotors(_axis)) + "[";
                foreach (double d in arr)
                    deb += " " + d.ToString();
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, deb + " ]");
                #endregion
                return ret;
            }
        }

        public override double Velocity()
        {
            AxisPosition[] samples = _samples.ToArray();
            int last = samples.Count() - 1;

            if (samples.Count() < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.FromRadians(deltaRadians / deltaT, Angle.Type.RA);

            return  a.Hours;
        }

        protected override bool Acceptable(double rad)
        {
            if (!Double.IsNaN(_prevPosition.radians) && Math.Abs(_currPosition.radians - _prevPosition.radians) > _maxDeltaRadiansAtSlewRate)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:Acceptable({1}): Rejected ( Abs({1} - {2}) > {3}) )",
                    Name, rad, _prevPosition.radians, _maxDeltaRadiansAtSlewRate);
                #endregion
                return false;
            }

            return true;
        }
    }

    public class SecondaryAxisMonitor : AxisMonitor
    {
        public static FixedSizedQueue<double> _decDeltas = new FixedSizedQueue<double>(nSamples);

        private double _declination = double.NaN, _prevDeclination = double.NaN;
        public FixedSizedQueue<double> _decSamples = new FixedSizedQueue<double>(nSamples);

        public SecondaryAxisMonitor() : base(TelescopeAxes.axisSecondary) { }

        private WiseDecEncoder _encoder = WiseTele.Instance.DecEncoder;

        protected override void SampleAxisMovement(object StateObject)
        {
            double reading;

            while (!Acceptable(reading = _encoder.Angle.Radians))
                ;

            _currPosition.radians = reading;

            if (Double.IsNaN(_prevPosition.radians))
            {
                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevDeclination = Angle.FromRadians(_currPosition.radians).Degrees;
                return;
            }

            _declination = Angle.FromRadians(_currPosition.radians).Degrees;
            _samples.Enqueue(_currPosition);

            double delta = Math.Abs(_declination - _prevDeclination);
            _decDeltas.Enqueue(delta);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:SampleAxisMovement: _currPosition: {1}, _prevPosition: {2}, delta: {3}, active motors: {4}",
                Name, _currPosition.radians, _prevPosition.radians, delta, ActiveMotors(_axis));
            #endregion

            _prevPosition.radians = _currPosition.radians;
            _prevDeclination = _declination;
        }

        public override bool IsMoving
        {
            get
            {
                double[] arr = _decDeltas.ToArray();

                if (arr.Count() < _samples.MaxSize)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: Not enough samples: arr.Count() {1} < _samples.MaxSize: {2}",
                        Name, arr.Count(), _samples.MaxSize);
                    #endregion
                    return false;    // not enough samples
                }

                foreach (double d in arr)
                    if (d != 0.0)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: true", Name);
                        #endregion
                        return true;
                    }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: false", Name);
                #endregion
                return false;
            }
        }

        public Angle Declination
        {
            get
            {
                return Angle.FromDegrees(_declination);
            }
        }

        public override double Velocity()
        {
            AxisPosition[] samples = _samples.ToArray();
            int last = samples.Count() - 1;

            if (samples.Count() < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.FromRadians(deltaRadians / deltaT, Angle.Type.Dec);

            return a.Degrees;
        }

        protected override bool Acceptable(double rad)
        {
            if (!Double.IsNaN(_prevPosition.radians) && Math.Abs(_currPosition.radians - _prevPosition.radians) > _maxDeltaRadiansAtSlewRate)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:Acceptable({1}): Rejected (Abs({2} - {3}) > {4})",
                    Name, rad, rad, _prevPosition.radians, _maxDeltaRadiansAtSlewRate);
                #endregion
                return false;
            }

            return true;
        }
    }
}
