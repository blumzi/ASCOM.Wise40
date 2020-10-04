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
        /// <para>An AxisMonitor supplies the following functionality:</para>
        /// <para>  1. Can tell whether its axis is moving or stationary.</para>
        /// <para>
        ///   2. Rejects spurious encoder readings.  We get this quite a lot, specially on the Dec axis.
        ///      The encoders have parallel outputs (i.e. one wire per bit).  The longer the cables are and
        ///      the more motors are in their vecinity, the more flipped bits occur.  The cables from the Dec
        ///      encoders are longer and pass near more motors within the telescopes body.
        /// </para>
        /// <para>  3. Provides the last-known-as-good coordinate(s) for its axis</para>
        ///
        /// </summary>
        public struct AxisPosition
        {
            public double radians;
            public bool predicted; // true: encoder reading was !Acceptable, false: encoder reading was accepted
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
        public const int nSamples = 500 / _samplingFrequency;  // a half second's worth of samples

        public FixedSizedQueue<AxisPosition> _samples = new FixedSizedQueue<AxisPosition>(nSamples);
        protected bool _ready = false;

        protected static double _maxDeltaRadiansAtSlewRate = 0.0021; // approx. Angle("5d00m00s").Radians / nSamples;

        /// <summary>
        /// A background Task that checks whether the telescope axis is moving
        ///  - primaryAxis: RightAscension should not change if Tracking
        ///  - secondaryAxis: Declination should not change.
        /// </summary>
        public Task movementCheckerTask;
        public static CancellationTokenSource movementCheckerCancellationTokenSource;
        public static CancellationToken movementCheckerCancellationToken;

        public System.Threading.Timer movementCheckerTimer;

        protected AxisMonitor(TelescopeAxes axis)
        {
            _axis = axis;
            WiseName = _axis.ToString() + "Monitor";
            astroutils = new Astrometry.AstroUtils.AstroUtils();
        }

        public abstract bool IsMoving { get; }

        public abstract bool IsReady { get; set; }

        public double DeltaT
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
            int last = arr.Length - 1;

            if (arr.Length < 3)
                return double.NaN;

            const double dT = (1000 / _samplingFrequency);
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
            {
                if (m.IsOn)
                    ret += m.WiseName + " (" + WiseTele.RateName(m.currentRate) + ") ";
            }
            return ret;
        }

        protected abstract void SampleAxisMovement(object StateObject);

        /// <summary>
        /// <para>
        /// Tests whether an encoder reading (transformed into radians) is acceptable.  This allows
        /// rejecting spurious encoder readings.
        /// </para>
        /// <para>
        /// It should be a  multi-tiered process:
        ///  1. Is it between the highest and lowest reading the respective axis can produce
        ///  2. Is it reasonably close to the previous reading (if one is available)
        ///     - must be less than the max delta at the current speed (or at least at Slew speed)
        /// </para>
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        protected abstract bool Acceptable(double rad);

        protected abstract double Predicted(double rad);

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
                movementCheckerTask = Task.Run(() => AxisMovementChecker(), movementCheckerCancellationToken);
            }
            catch (OperationCanceledException)
            {
                movementCheckerTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StopMovementChecker()
        {
            movementCheckerCancellationTokenSource?.Cancel();
            movementCheckerTimer?.Change(Timeout.Infinite, Timeout.Infinite);
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
        public const double raEpsilon = 2e-3;        // epsilon for primaryMonitor, while tracking
        public const double haEpsilon = 7.0;         // epsilon for primaryMonitor, while NOT tracking

        public static FixedSizedQueue<double> _raDeltas = new FixedSizedQueue<double>(nSamples);
        public static FixedSizedQueue<double> _haDeltas = new FixedSizedQueue<double>(nSamples);

        private double _rightAscension = double.NaN, _hourAngle = double.NaN;
        private double _prevRightAscension = double.NaN, _prevHourAngle = double.NaN;

        private readonly double[] x = new double[3] { 0.0, 0.0, 0.0 };
        private readonly double[] dx = new double[2] { 0.0, 0.0 };
        private double ddx = 0.0;

        public PrimaryAxisMonitor() : base(TelescopeAxes.axisPrimary) { }

        private readonly WiseHAEncoder _encoder = WiseTele.Instance.HAEncoder;

        public static void ResetRASamples()
        {
            _raDeltas = new FixedSizedQueue<double>(nSamples);
        }

        protected override void SampleAxisMovement(object StateObject)
        {
            double reading = _encoder.Angle.Radians;
            AxisPosition position = Acceptable(reading) ?
                new AxisPosition() {
                    radians = reading,
                    predicted = false,
                } :
                new AxisPosition() {
                    radians = Predicted(reading),
                    predicted = true,
                };

            _currPosition = position;

            x[0] = x[1];
            x[1] = x[2];
            x[2] = _currPosition.radians;
            dx[0] = dx[1];
            dx[1] = x[2] - x[1];
            ddx = dx[1] - dx[0];

            if (Double.IsNaN(_prevPosition.radians))
            {
                Angle currentAngle = Angle.FromRadians(_currPosition.radians);

                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevHourAngle = currentAngle.Hours;
                _prevRightAscension = (wisesite.LocalSiderealTime - currentAngle).Hours;
                return;
            }

            _hourAngle = Angle.FromRadians(_currPosition.radians).Hours;
            //_hourAngle = Angle.HaFromRadians(_currPosition.radians).Hours;
            _rightAscension = wisesite.LocalSiderealTime.Hours - _hourAngle;
            _samples.Enqueue(_currPosition);

            if (! IsReady && (_samples.ToArray().Length == _samples.MaxSize))
            {
                #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{WiseName}: became ready.");
                #endregion
                IsReady = true;
            }

            double raDelta = Math.Abs(_rightAscension - _prevRightAscension);
            double haDelta = Math.Abs(_hourAngle - _prevHourAngle);
            _raDeltas.Enqueue(raDelta);
            _haDeltas.Enqueue(haDelta);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"{WiseName}:SampleAxisMovement: _currPosition: {_currPosition.radians} ({(_currPosition.predicted ? "PREDICTED" : "REAL")}), " +
                $"_prevPosition: {_prevPosition.radians} ({(_prevPosition.predicted ? "PREDICTED" : "REAL")})," +
                $"raDelta: {raDelta}, haDelta: {haDelta}, active motors: {ActiveMotors(_axis)}");
            #endregion

            _prevPosition.radians = _currPosition.radians;
            _prevHourAngle = _hourAngle;
            _prevRightAscension = _rightAscension;
        }

        public Angle RightAscension
        {
            get
            {
                return Angle.RaFromHours(astroutils.ConditionRA(_rightAscension));
            }
        }

        public Angle HourAngle
        {
            get
            {
                return Angle.HaFromHours(astroutils.ConditionHA(_hourAngle));
            }
        }

        public override bool IsReady
        {
            get
            {
                return _ready;
            }

            set
            {
                _ready = value;
            }
        }

        public override bool IsMoving
        {
            get
            {
                if (!IsReady)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{WiseName}:IsMoving: No ready.)");
                    #endregion
                    return false;
                }

                double max = double.MinValue;
                bool tracking = wisetele.Tracking;
                double[] arr;
                double epsilon;

                if (tracking)
                {
                    arr = _raDeltas.ToArray();
                    epsilon = raEpsilon;
                }
                else
                {
                    arr = _haDeltas.ToArray();
                    epsilon = haEpsilon;
                }

                foreach (double d in arr)
                {
                    if (d > max)
                        max = d;
                }

                bool ret = max > epsilon;

                #region debug
                string deb = $"{WiseName}:IsMoving: max: {max:F15}, epsilon: {epsilon:F15}, ret: {ret}, active: {ActiveMotors(_axis)}" + "[";
                foreach (double d in arr)
                    deb += $" {d:F10}";
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, deb + " ]");
                #endregion
                return ret;
            }
        }

        public override double Velocity()
        {
            AxisPosition[] samples = _samples.ToArray();
            int last = samples.Length - 1;

            if (samples.Length < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.RaFromRadians(deltaRadians / DeltaT);

            return  a.Hours;
        }

        protected override bool Acceptable(double rad)
        {
            if (Double.IsNaN(_prevPosition.radians))
                return true;

            double delta = Math.Abs(rad - _prevPosition.radians);
            if (delta > _maxDeltaRadiansAtSlewRate)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:Acceptable({1}): Suspect (Abs({1} - {2}) = {3} > {4})",
                    WiseName, rad, _prevPosition.radians, delta, _maxDeltaRadiansAtSlewRate);
                #endregion
                return false;
            }

            return true;
        }

        /// <summary>
        /// <para>Predicted position (radians)</para>
        /// <para>
        /// The SampleAxisMovement function maintains:
        ///  . The last three acceptable positions are kept in x[0..2]
        ///  . The first differences are kept in dx[0..1]:
        ///   . dx[0] = x[1] - x[0]
        ///   . dx[1] = x[2] - x[1]
        ///  . The second difference is kept in ddx:
        ///   . ddx = dx[1] - dx[0]
        /// </para>
        ///
        /// </summary>
        protected override double Predicted(double reading)
        {
            double pred = x[2] + dx[1] + ddx;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:Predicted: r: {1}, p: {2}, r - p: {3}, (r - p)Angle: {4}",
                WiseName, reading,  pred, reading - pred, Angle.RaFromHours(reading - pred).ToNiceString());
            #endregion

            return reading; // pred;
        }
    }

    public class SecondaryAxisMonitor : AxisMonitor
    {
        public static FixedSizedQueue<double> _decDeltas = new FixedSizedQueue<double>(nSamples);

        private double _declination = double.NaN, _prevDeclination = double.NaN;
        public FixedSizedQueue<double> _decSamples = new FixedSizedQueue<double>(nSamples);

        public SecondaryAxisMonitor() : base(TelescopeAxes.axisSecondary) { }

        private readonly WiseDecEncoder _encoder = WiseTele.Instance.DecEncoder;

        private readonly double[] x = new double[3] { 0.0, 0.0, 0.0 };   // last three positions
        private readonly double[] dx = new double[2] { 0.0, 0.0 };       // first differences between last positions
        private double ddx = 0.0;                                        // second difference between first differences

        protected override void SampleAxisMovement(object StateObject)
        {
            double reading = _encoder.Angle.Radians;
            AxisPosition position = Acceptable(reading) ?
                new AxisPosition()
                {
                    radians = reading,
                    predicted = false,
                } :
                new AxisPosition()
                {
                    radians = Predicted(reading),
                    predicted = true,
                };

            _currPosition = position;

            x[0] = x[1];
            x[1] = x[2];
            x[2] = _currPosition.radians;
            dx[0] = dx[1];
            dx[1] = x[2] - x[1];
            ddx = dx[1] - dx[0];

            if (Double.IsNaN(_prevPosition.radians))
            {
                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevDeclination = Angle.DecFromRadians(_currPosition.radians).Degrees;
                return;
            }

            double rads = _currPosition.radians;

            if (rads > Const.onePI)
                rads -= Const.twoPI;

            _declination = Angle.DecFromRadians(rads).Degrees;
            _samples.Enqueue(_currPosition);

            if (!IsReady && (_samples.ToArray().Length == _samples.MaxSize))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{WiseName}: became ready.");
                #endregion
                IsReady = true;
            }

            double delta = Math.Abs(_declination - _prevDeclination);
            _decDeltas.Enqueue(delta);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:SampleAxisMovement: _currPosition: {1}, _prevPosition: {2}, delta: {3}, active motors: {4}",
                WiseName, _currPosition.radians, _prevPosition.radians, delta, ActiveMotors(_axis));
            #endregion

            _prevPosition.radians = _currPosition.radians;
            _prevDeclination = _declination;
        }

        public override bool IsReady
        {
            get
            {
                return _ready;
            }

            set
            {
                _ready = value;
            }
        }

        public override bool IsMoving
        {
            get
            {
                if (! IsReady)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{WiseName}:IsMoving: Not ready.");
                    #endregion
                    return false;    // not enough samples
                }

                double[] arr = _decDeltas.ToArray();

                foreach (double d in arr)
                    if (d != 0.0)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: true", WiseName);
                        #endregion
                        return true;
                    }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: false", WiseName);
                #endregion
                return false;
            }
        }

        public Angle Declination
        {
            get
            {
                double dec = _declination;

                return Angle.DecFromDegrees(dec);
            }
        }

        public override double Velocity()
        {
            AxisPosition[] samples = _samples.ToArray();
            int last = samples.Length - 1;

            if (samples.Length < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.FromRadians(deltaRadians / DeltaT);

            return a.Degrees;
        }

        protected override bool Acceptable(double rad)
        {
            if (Double.IsNaN(_prevPosition.radians))
                return true;

            double delta = Math.Abs(rad - _prevPosition.radians);
            if (delta > _maxDeltaRadiansAtSlewRate)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "{0}:Acceptable({1}): Suspect (Abs({2} - {3}) = {4} > {5})",
                        WiseName, rad, rad, _prevPosition.radians, delta, _maxDeltaRadiansAtSlewRate);
                #endregion
                return false;
            }

            return true;
        }

        protected override double Predicted(double reading)
        {
            double pred = x[2] + dx[1] + ddx;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:Predicted: r: {1}, p: {2}, r - p: {3}, (r - p)Angle: {4}",
                WiseName, reading, pred, reading - pred, Angle.DecFromDegrees(reading - pred).ToNiceString());
            #endregion

            return reading; // pred;
        }
    }
}
