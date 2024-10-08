﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

using PID;

namespace ASCOM.Wise40.Hardware
{
    public class TimeProportionedPidController: PidController
    {
        private ulong _windowSize;          // Ticks
        private WisePin _pin;
        private ulong windowStartTime;
        private ulong _output = 0;
        private float _targetPosition;
        private Debugger debugger = Debugger.Instance;
        Func<int> _stopSimulatedProcess;

        public TimeProportionedPidController() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowSizeMillis">millis</param>
        /// <param name="pin"></param>
        /// <param name="samplingRate"></param>
        /// <param name="readProcess"></param>
        /// <param name="readOutput"></param>
        /// <param name="readSetPoint"></param>
        /// <param name="proportionalGain"></param>
        /// <param name="integralGain"></param>
        /// <param name="derivativeGain"></param>
        /// <param name="controllerDirection"></param>
        /// <param name="controllerMode"></param>
        public TimeProportionedPidController(string name,
            ulong windowSizeMillis, WisePin pin, TimeSpan samplingRate,
            Func<int> stopSimulatedProcess,
            Func<int> readProcess, Func<ulong> readOutput, Action<ulong> writeOutput, Func<int> readSetPoint,
            float proportionalGain, float integralGain, float derivativeGain,
            ControllerMode controllerMode = ControllerMode.Automatic): base(name, samplingRate, (float)0.0, (float)100.0,
                readProcess, readOutput, writeOutput, readSetPoint,
                proportionalGain, integralGain, derivativeGain,
                pin.Direction == Const.Direction.Increasing ? ControllerDirection.Direct : ControllerDirection.Reverse,
                controllerMode)
        {
            _windowSize = windowSizeMillis * TimeSpan.TicksPerMillisecond;
            base.SetOutputLimits(0, _windowSize);
            _pin = pin;
            if (readProcess == null)
                throw new ArgumentNullException(nameof(readProcess), "Read process must not be null.");
            if (readOutput == null)
                throw new ArgumentNullException(nameof(readOutput), "Read output must not be null.");
            _stopSimulatedProcess = stopSimulatedProcess;
            debugger.init();
        }

        protected override void Compute()
        {
            ulong now = (ulong) DateTime.Now.Ticks;

            base.Compute();

            if ((now - windowStartTime) > _windowSize)
            {
                windowStartTime += _windowSize;                
            }

            if (_output <= now - windowStartTime) {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    "{0}: Compute: _output: {1} <= {2} ({3} - {4}), Stopping",
                    Name, _output, now - windowStartTime, now, windowStartTime);
                #endregion
                base.Stop();
                _pin.SetOff();
                _stopSimulatedProcess();
            }
        }

        public void MoveTo(uint targetPosition)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "{0}: MoveTo({1})", Name, targetPosition);
            #endregion
            _targetPosition = targetPosition;
            windowStartTime = (ulong) DateTime.Now.Ticks;
            base.Run();
        }
    }
}
