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

        //
        // Half a second's worth of samples.  This was 500 / _samplingFrequency = 25, which at
        //  20 Hz is 1.25 seconds - the arithmetic was upside down, and the window was 2.5x
        //  what the comment claimed.  Every stop had to wait it out.
        //
        public const int nSamples = 500 * _samplingFrequency / 1000;

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
        //
        // Below this per-sample change the axis counts as stopped.  HOURS, because that is
        //  what _raDeltas and _haDeltas hold.
        //
        // 0.40 arcsec per 50ms sample is 8 arcsec/sec, about 3.4 HA encoder counts (0.1187
        //  arcsec each) - clear of the 1-2 counts of dither a stationary axis shows, and well
        //  below rateSet at 52.3 arcsec/sec.
        //
        // ONE value now, used whether or not the telescope is tracking.  What changes with
        //  tracking is WHICH series to watch - RightAscension should hold still while
        //  tracking, HourAngle while not - and that choice is made below.  The threshold has
        //  no business differing, and the two it replaced were both wrong:
        //
        //    raEpsilon = 2e-3 hours per sample = 108 arcsec per sample = 0.6 deg/sec.  Only
        //      the slew rate ever exceeded that; set and guide never registered.
        //
        //    haEpsilon = 7.0 HOURS per sample, against a maximum possible value of 24.
        //      Unreachable, so while NOT tracking this axis could never report motion at all.
        //
        // Note guide rate cannot be detected per-sample on this axis by any threshold:
        //  0.86 arcsec/sec is 0.043 arcsec per sample, a third of one encoder count.  It is
        //  below quantisation, not below this epsilon.
        //
        public const double primaryEpsilon = 0.40 / (3600.0 * 15.0);

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

            //
            // NOTE: RenishawHAEncoder.HourAngle already returns HOURS, not radians.
            //  Passing it through Angle.FromRadians().Hours converted it a second
            //  time, inflating it by 12/pi (~3.82) and making every discrepancy
            //  logged here useless.  The Dec monitor below never had this problem,
            //  it uses .Declination directly.
            //
            double renishawHa = WiseTele.renishawHaEncoder.HourAngle;
            double renishawRa = wisesite.LocalSiderealTime.Hours - renishawHa;
            double discrepancy = Math.Abs(_hourAngle - renishawHa);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"{WiseName}:SampleAxisMovement: _currPosition(rad): {_currPosition.radians:F15} ({(_currPosition.predicted ? "PREDICTED" : "REAL")}), " +
                $"_prevPosition(rad): {_prevPosition.radians:F15} ({(_prevPosition.predicted ? "PREDICTED" : "REAL")})," +
                $"raDelta: {raDelta:F15}, haDelta: {haDelta:F15}, active motors: {ActiveMotors(_axis)}" +
                $"enc: {_encoder.AxisValue}, renishaw: {WiseTele.renishawHaEncoder.Position}, renishaw.Radians: {WiseTele.renishawHaEncoder.Radians:F15}, " +
                $"Ha: {_hourAngle}, renishawHa: {renishawHa}, Ra: {_rightAscension}, renishawRa: {renishawRa}, discrepancy: {discrepancy}"
                );
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
                    epsilon = primaryEpsilon;
                }
                else
                {
                    arr = _haDeltas.ToArray();
                    epsilon = primaryEpsilon;
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

    /// <summary>
    /// Watches the declination axis.
    ///
    /// The "has it stopped" test used to be "is EVERY delta in the window exactly zero",
    ///  with no tolerance at all.  Dec resolves 0.0442 arcsec per count and the reading
    ///  dithers between adjacent counts when the axis is standing still, so that test could
    ///  only pass when 25 consecutive samples happened to land on the same count - waiting
    ///  for a coincidence.
    ///
    /// Measured on 2026-09-17, after a 20 degree slew: the axis was physically stationary
    ///  4.6 seconds after the motor was cut, and StopAxisAndWaitForHalt did not agree for a
    ///  further 21.1 seconds.  The logged positions across that time are four ADJACENT
    ///  encoder counts recurring, with no decay - noise at the last bit, not ringing.  That
    ///  cost was paid on the slew-to-set handover of every slew.
    /// </summary>
    public class SecondaryAxisMonitor : AxisMonitor
    {
        public static FixedSizedQueue<double> _decDeltas = new FixedSizedQueue<double>(nSamples);

        //
        // Below this per-sample change the axis counts as stopped.  Degrees, since _decDeltas
        //  holds degrees.
        //
        // 0.15 arcsec per 50ms sample is 3 arcsec/sec, and about 3.4 Renishaw counts - well
        //  clear of the 1 to 2 counts of dither seen on a stationary axis, and far below any
        //  rate the telescope can actually be driven at.  The slowest, rateGuide, is
        //  0.86 arcsec/sec, which is 0.043 arcsec per sample; a real guide-rate motion still
        //  registers because it accumulates, whereas dither does not.
        //
        // This is not a precision the mount can use: its own repeatability is ~30 arcsec, and
        //  after this handover the next leg drives at 49.6 arcsec/sec. Insisting on stillness
        //  to 0.044 arcsec bought nothing and cost 21 seconds a slew.
        //
        private const double decEpsilon = 0.15 / 3600.0;

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

            double renishawDec = WiseTele.renishawDecEncoder.Declination;
            double discrepancy = Math.Abs(_declination - renishawDec);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"{WiseName}:SampleAxisMovement: _currPosition(rad): {_currPosition.radians:F15}, _prevPosition(rad): {_prevPosition.radians:F15}, " +
                $"delta: {delta:F15}, active motors: {ActiveMotors(_axis)}" +
                $"enc: {_encoder.EncoderValue}, renishaw: {WiseTele.renishawDecEncoder.Position}, renishaw.Radians: {WiseTele.renishawDecEncoder.Radians:F15}, " +
                $"dec: {_declination}, renishawDec: {renishawDec}, discrepancy: {discrepancy}"
                );
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
                    if (d > decEpsilon)
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
