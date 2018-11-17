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
    public class AxisMonitor : WiseObject, IConnectable
    {
        public struct AxisPositionSample
        {
            public double delta;
        };

        private const int nSamples = 5;
        private double _previousValue = double.NaN;
        private double _previousEncoderValue = double.NaN;
        private FixedSizedQueue<AxisPositionSample> _samples = new FixedSizedQueue<AxisPositionSample>(nSamples);
        private TelescopeAxes _axis;
        private WiseTele wisetele = WiseTele.Instance;
        private bool _connected = false;
        private Debugger debugger = Debugger.Instance;
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
        private Task movementCheckerTask;
        private static CancellationTokenSource movementCheckerCancellationTokenSource;
        private static CancellationToken movementCheckerCancellationToken;

        private System.Threading.Timer movementCheckerTimer;

        public AxisMonitor(TelescopeAxes axis)
        {
            _axis = axis;
        }

        public bool IsMoving
        {
            get
            {
                var arr = _samples.ToArray();

                if (arr.Count() < _samples.MaxSize)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisMonitor:IsMoving:{0}: Not enough samples {1} < {2} = true",
                        _axis, arr.Count(), _samples.MaxSize);
                    #endregion
                    return true;    // not enough samples
                }

                // Check the array of delta-movements
                bool ret;
                #region debug
                string deb;
                #endregion
                if (_axis == TelescopeAxes.axisPrimary)
                {
                    double max = double.MinValue;

                    foreach (var sample in arr)
                        if (sample.delta > max)
                            max = sample.delta;

                    double epsilon = _whileTracking ? raEpsilon : haEpsilon;
                    ret = max > epsilon;
                    #region debug
                    deb = string.Format("AxisMonitor:IsMoving:{0}: max: {1:F15}, epsilon: {2:F15}, ret: {3}",
                        _axis, max, epsilon, ret);
                    #endregion
                }
                else
                {
                    ret = false;
                    foreach (var sample in arr)
                        if (sample.delta != 0.0)
                        {
                            ret = true;
                            break;
                        }
                    #region debug
                    deb = string.Format("AxisMonitor:IsMoving:{0}: ret: {1}", _axis,ret);
                    #endregion
                }

                #region debug
                deb += ", active motors: " + ActiveMotors(_axis);
                List<string> list = new List<string>();
                foreach (var sample in arr)
                    list.Add(sample.delta.ToString());
                deb += ", [" + string.Join(",", list) + "]";
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, deb);
                #endregion
                return ret;
            }
        }

        private string ActiveMotors(TelescopeAxes axis)
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

        private void SampleAxisMovement(object StateObject)
        {
            bool tracking = trackingMotor.isOn;

            if (_axis == TelescopeAxes.axisPrimary)
            {
                if (tracking != _whileTracking)
                {
                    // The tracking state has changed while we're sampling: discard previous
                    //  samples and start a new sequence
                    _whileTracking = tracking;
                    _samples = new FixedSizedQueue<AxisPositionSample>(nSamples);
                }
            }

            double currentValue = (_axis == TelescopeAxes.axisPrimary) ?
                    (tracking ? wisetele.RightAscension : wisetele.HAEncoder.Value) :
                    wisetele.DecEncoder.Angle.Radians;

            if (Double.IsNaN(_previousValue))
            {
                _previousValue = currentValue;
                return;
            }

            double delta = Math.Abs(currentValue - _previousValue);
            if (Double.IsNaN(delta))
                return;

            AxisPositionSample sample = new AxisPositionSample { delta = delta };
            double encoderValue = (_axis == TelescopeAxes.axisPrimary) ? wisetele.HAEncoder.Value : wisetele.DecEncoder.Value;
            double encoderDelta = double.NaN;
            if (!Double.IsNaN(_previousEncoderValue))
                encoderDelta = encoderValue - _previousEncoderValue;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "AxisMonitor:SampleAxisMovement:{0}: value: {1}, _previousValue: {2}, enqueueing: {3:F15}, active: {4}, encoder: {5}, encoderDelta: {6}",
                _axis, currentValue, _previousValue, delta, ActiveMotors(_axis), encoderValue, encoderDelta);
            #endregion
            _samples.Enqueue(sample);
            _previousValue = currentValue;
            _previousEncoderValue = encoderValue;
        }

        public void AxisMovementChecker()
        {
            TimerCallback axisMovementTimerCallback = new TimerCallback(SampleAxisMovement);
            movementCheckerTimer = new System.Threading.Timer(axisMovementTimerCallback);
            movementCheckerTimer.Change(0, 1000/_samplingFrequency);
        }

        private void StartMovementChecker()
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

        private void StopMovementChecker()
        {
            if (movementCheckerCancellationTokenSource != null)
                movementCheckerCancellationTokenSource.Cancel();

            if(movementCheckerTimer != null)
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

    public class PrimaryAxisMonitor: AxisMonitor
    {
        PrimaryAxisMonitor() : base(TelescopeAxes.axisPrimary) { }

        public double RightAscension
        {
            get
            {

            }
        }

        public double HourAngle
        {
            get
            {

            }
        }
    }

    public class SecondaryAxisMonitor: AxisMonitor
    {
        SecondaryAxisMonitor() : base(TelescopeAxes.axisSecondary) { }

        public double Declination
        {
            get
            {
                ;
            }
        }
    }
}
