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
    public class AxisStatusMonitor : IConnectable
    {
        private double _previousValue = double.NaN;
        private FixedSizedQueue<double> _deltas = new FixedSizedQueue<double>(5);
        private TelescopeAxes _axis;
        private WiseTele wisetele = WiseTele.Instance;
        private bool _connected = false;
        private Debugger debugger;

        private static double epsilon;
        private static readonly int _samplingFrequency = 10;

        /// <summary>
        /// A background Task that checks whether the telescope axis is moving
        ///  - primaryAxis: RightAscension should not change if Tracking
        ///  - secondaryAxis: Declination should not change.
        /// </summary>
        private Task movementCheckerTask;
        private static CancellationTokenSource movementCheckerCancellationTokenSource;
        private static CancellationToken movementCheckerCancellationToken;

        private System.Threading.Timer movementCheckerTimer;

        public AxisStatusMonitor(TelescopeAxes axis)
        {
            debugger = new Debugger();
            debugger.Level = (uint) Debugger.DebugLevel.DebugAxes;

            _axis = axis;
            epsilon = (_axis == TelescopeAxes.axisPrimary) ? .00001D : 0.0;
        }

        public bool IsMoving
        {
            get
            {
                bool ret;
                double _max = double.MinValue;
                double epsilon = 0.00002;       // TODO: Check in real life
                foreach (double d in _deltas.ToArray())
                    if (d > _max)
                        _max = d;

                if (_axis == TelescopeAxes.axisPrimary)
                    ret = _max > epsilon;
                else
                    ret = _max > 0.0;

                #region debug
                //debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}: _max: {1:F15}, epsilon: {2:F15}, ret: {3}, tracking: {4}",
                //    _axis, _max, epsilon, ret, wisetele.Tracking);
                #endregion
                return ret;
            }
        }

        private void SampleAxisMovement(object StateObject)
        {
            double value = (_axis == TelescopeAxes.axisPrimary) ? wisetele.RightAscension : wisetele.Declination;
            if (_previousValue == double.NaN)
            {
                _previousValue = value;
                return;
            }

            double d = Math.Abs(value - _previousValue);
            if (d == double.NaN)
                return;

            #region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}: value: {1}, _previousValue: {2}, enqueueing: {3:F15}",
            //    _axis, value, _previousValue, d);
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
                    Thread.CurrentThread.Name = _axis.ToString() + "MovementChecker";
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
            movementCheckerCancellationTokenSource.Cancel();
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
}
