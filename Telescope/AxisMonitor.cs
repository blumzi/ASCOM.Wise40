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
    public class AxisMonitor : IConnectable
    {
        private const int nSamples = 5;
        private double _previousValue = double.NaN;
        private FixedSizedQueue<double> _deltas = new FixedSizedQueue<double>(nSamples);
        private TelescopeAxes _axis, _other_axis;
        private WiseTele wisetele = WiseTele.Instance;
        private bool _connected = false;
        private Debugger debugger = Debugger.Instance;
        private bool _whileTracking = false;
        private WiseVirtualMotor trackingMotor = WiseTele.Instance.TrackingMotor;
        
        /// <summary>
        /// The epsilon value contains the minimal encoder change (within the _samplingFrequency below)
        ///  that's considered as the axis being currently moving.
        /// </summary>
        private double epsilon;
        private static readonly int _samplingFrequency = 10;
        private const double secondaryDelta = 0.0001;
        private const double trackingDelta = 0;
        private const double primaryDelta = 7.0;
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

            if ((new WiseObject()).Simulated)
            {
                epsilon = simulatedDelta;
            }
            else if (_axis == TelescopeAxes.axisPrimary)
            {
                epsilon = primaryDelta; // To Be Reviewed
                _whileTracking = trackingMotor.isOn;
            }
            else if (_axis == TelescopeAxes.axisSecondary)
            {
                epsilon = secondaryDelta;   // Measured as the minimal change while the Dec axis is moving
            }

            _other_axis = (_axis == TelescopeAxes.axisPrimary) ?
                TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;
        }

        public bool IsMoving
        {
            get
            {
                var max = _deltas.ToArray().Max();
                bool ret =  max > epsilon;

                #region debug
                string deb = string.Format("AxisMonitor:IsMoving:{0}: max: {1:F15}, epsilon: {2:F15}, ret: {3}, active: {4}",
                    _axis, max, epsilon, ret, ActiveMotors(_axis)) + "[";
                foreach (var d in _deltas.ToArray())
                    deb += " " + d.ToString();
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, deb + "]");
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
                    ret += m.Name + " ";
            return ret;
        }

        private void SampleAxisMovement(object StateObject)
        {
            if (_axis == TelescopeAxes.axisPrimary)
            {
                if (trackingMotor.isOn != _whileTracking)
                {
                    _whileTracking = trackingMotor.isOn;
                    _deltas = new FixedSizedQueue<double>(nSamples);
                }
            }

            double value = (_axis == TelescopeAxes.axisPrimary) ?
                    (_whileTracking ? wisetele.RightAscension : wisetele.HAEncoder.Value) :
                    wisetele.Declination;

            if (_previousValue == double.NaN)
            {
                _previousValue = value;
                return;
            }

            double d = Math.Abs(value - _previousValue);
            if (d == double.NaN)
                return;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "AxisMonitor:SampleAxisMovement:{0}: value: {1}, _previousValue: {2}, enqueueing: {3:F15}, active: {4}",
                _axis, value, _previousValue, d, ActiveMotors(_axis));
            #endregion
            _deltas.Enqueue(d);
            _previousValue = value;
        }

        public void AxisMovementChecker()
        {
            TimerCallback axisMovementTimerCallback = new TimerCallback(SampleAxisMovement);
            movementCheckerTimer = new System.Threading.Timer(axisMovementTimerCallback);
            movementCheckerTimer.Change(100/_samplingFrequency, 1000/_samplingFrequency);
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

        public bool CanMoveAtRate(double rate)
        {
            double otherRate = Const.rateStopped;
            bool ret = false;

            foreach (WiseVirtualMotor m in wisetele.axisMotors[_other_axis])
                {
                    if (m.isOn)
                    {
                        otherRate = m.currentRate;
                        break;
                    }
                }

            if ((otherRate == Const.rateStopped) ||
                (otherRate == rate) ||
                ((rate == Const.rateSlew || rate == Const.rateSet) && otherRate == Const.rateGuide) ||
                (rate == Const.rateGuide && (otherRate == Const.rateSet || otherRate == Const.rateSlew)))
                ret = true;
            else
                ret = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "CanMoveAtRate: {0}, axis: {1} => {2}",
                WiseTele.RateName(rate), _axis, ret.ToString());
            #endregion

            return ret;
        }
    }
}
