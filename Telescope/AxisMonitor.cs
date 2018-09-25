﻿using System;
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
        public struct AxisPositionSample
        {
            public double radians;
        };

        protected const int nSamples = 5;
        //private double _previousValue = double.NaN;
        //private double _previousEncoderValue = double.NaN;
        protected FixedSizedQueue<AxisPositionSample> _samples = new FixedSizedQueue<AxisPositionSample>(nSamples);
        protected AxisPositionSample _currPosition, _prevPosition;
        protected TelescopeAxes _axis, _other_axis;
        protected WiseTele wisetele = WiseTele.Instance;
        protected WiseSite wisesite = WiseSite.Instance;
        private bool _connected = false;
        protected Debugger debugger = Debugger.Instance;
        private bool _whileTracking = false;
        private WiseVirtualMotor trackingMotor = WiseTele.Instance.TrackingMotor;
        
        private static readonly int _samplingFrequency = 5;
        private const double decEpsilon = 2e-6;       // epsilon for secondaryMonitor
        private const double raEpsilon = 1e-5;        // epsilon for primaryMonitor, while tracking
        private const double haEpsilon = 7.0;         // epsilon for primaryMonitor, while NOT tracking
        private const double simulatedDelta = 0.4;

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
            AxisPositionSample[] arr = _samples.ToArray();
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
        //private void SampleAxisMovement(object StateObject)
        //{
        //    bool tracking = trackingMotor.isOn;

        //    if (_axis == TelescopeAxes.axisPrimary)
        //    {
        //        if (tracking != _whileTracking)
        //        {
        //            // The tracking state has changed while we're sampling: discard previous
        //            //  samples and start a new sequence
        //            _whileTracking = tracking;
        //            _samples = new FixedSizedQueue<AxisPositionSample>(nSamples);
        //        }
        //    }

        //    double value = (_axis == TelescopeAxes.axisPrimary) ?
        //            (tracking ? wisetele.RightAscension : wisetele.HAEncoder.Value) :
        //            wisetele.DecEncoder.Angle.Radians;

        //    if (Double.IsNaN(_previousValue))
        //    {
        //        _previousValue = value;
        //        return;
        //    }

        //    double d = Math.Abs(value - _previousValue);
        //    if (Double.IsNaN(d))
        //        return;

        //    AxisPositionSample sample = new AxisPositionSample { value = d };
        //    double encoderValue = (_axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Value : wisetele.DecEncoder.Value;
        //    double encoderDelta = double.NaN;
        //    if (!Double.IsNaN(_previousEncoderValue))
        //        encoderDelta = encoderValue - _previousEncoderValue;

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
        //        "AxisMonitor:SampleAxisMovement:{0}: value: {1}, _previousValue: {2}, enqueueing: {3:F15}, active: {4}, encoder: {5}, encoderDelta: {6}",
        //        _axis, value, _previousValue, d, ActiveMotors(_axis), encoderValue, encoderDelta);
        //    #endregion
        //    _samples.Enqueue(sample);
        //    _previousValue = value;
        //    _previousEncoderValue = encoderValue;
        //}

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
        public const double raEpsilon = 1e-5;        // epsilon for primaryMonitor, while tracking
        public const double haEpsilon = 7.0;         // epsilon for primaryMonitor, while NOT tracking
        public const double _maxPrimaryDeltaRadiansAtSlewRate = 0.0;  // maximum difference between two consequtive encoder readings (radians)

        public static FixedSizedQueue<double> _raDeltas = new FixedSizedQueue<double>(nSamples);
        public static FixedSizedQueue<double> _haDeltas = new FixedSizedQueue<double>(nSamples);

        private double _rightAscension = double.NaN, _hourAngle = double.NaN;
        private double _prevRightAscension = double.NaN, _prevHourAngle = double.NaN;

        public PrimaryAxisMonitor() : base(TelescopeAxes.axisPrimary) { }

        private WiseHAEncoder _encoder = WiseTele.Instance.HAEncoder;

        protected override void SampleAxisMovement(object StateObject)
        {
            _currPosition.radians = _encoder.Angle.Radians;

            if (Double.IsNaN(_prevPosition.radians))
            {
                Angle currentAngle = Angle.FromRadians(_currPosition.radians);

                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevHourAngle = currentAngle.Hours;
                _prevRightAscension = (wisesite.LocalSiderealTime - currentAngle).Hours;
                return;
            }

            if (Math.Abs(_currPosition.radians - _prevPosition.radians) < _maxPrimaryDeltaRadiansAtSlewRate)
            {
                // Discard non-reasonable encoder readings
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}: Rejected encoder value {1} radians !!! ({2} - {3} > {4})",
                    Name, _currPosition.radians, _currPosition.radians, _prevPosition.radians, _maxPrimaryDeltaRadiansAtSlewRate);
                #endregion
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
            AxisPositionSample[] samples = _samples.ToArray();
            int last = samples.Count() - 1;

            if (samples.Count() < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.FromRadians(deltaRadians / deltaT, Angle.Type.RA);

            return  a.Hours;
        }
    }

    public class SecondaryAxisMonitor : AxisMonitor
    {
        public static FixedSizedQueue<double> _decDeltas = new FixedSizedQueue<double>(nSamples);

        private double _declination = double.NaN, _prevDeclination = double.NaN;
        public const double decEpsilon = 2e-6;       // epsilon for secondaryMonitor
        public FixedSizedQueue<double> _decSamples = new FixedSizedQueue<double>(nSamples);
        public const double _maxSecondaryDeltaRadiansAtSlewRate = 0.0;  // maximum difference between two consequtive encoder readings (radians)

        public SecondaryAxisMonitor() : base(TelescopeAxes.axisSecondary) { }

        private WiseDecEncoder _encoder = WiseTele.Instance.DecEncoder;

        protected override void SampleAxisMovement(object StateObject)
        {
            _currPosition.radians = _encoder.Angle.Radians;

            if (Double.IsNaN(_prevPosition.radians))
            {
                // We don't still have a _prevPosition to check against
                _prevPosition.radians = _currPosition.radians;
                _prevDeclination = Angle.FromRadians(_currPosition.radians).Degrees;
                return;
            }

            if (Math.Abs(_currPosition.radians - _prevPosition.radians) < _maxSecondaryDeltaRadiansAtSlewRate)
            {
                // Discard non-reasonable encoder readings
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}: Rejected encoder value {1} radians !!! ({2} - {3} > {4})",
                    Name, _currPosition.radians, _currPosition.radians, _prevPosition.radians, _maxSecondaryDeltaRadiansAtSlewRate);
                #endregion
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
                double max = double.MinValue;
                double[] arr = _decDeltas.ToArray();

                if (arr.Count() < _samples.MaxSize)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}:IsMoving: Not enough samples: arr.Count() {1} < _samples.MaxSize: {2}",
                        Name, arr.Count(), _samples.MaxSize);
                    #endregion
                    return true;    // not enough samples
                }

                foreach (double d in arr)
                    if (d > max)
                        max = d;

                double epsilon = decEpsilon;

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

        public Angle Declination
        {
            get
            {
                return Angle.FromDegrees(_declination);
            }
        }

        public override double Velocity()
        {
            AxisPositionSample[] samples = _samples.ToArray();
            int last = samples.Count() - 1;

            if (samples.Count() < 2)
                return double.NaN;

            double deltaRadians = Math.Abs(samples[last].radians - samples[last - 1].radians);
            Angle a = Angle.FromRadians(deltaRadians / deltaT, Angle.Type.Dec);

            return a.Degrees;
        }
    }
}
