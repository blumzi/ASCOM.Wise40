using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PID;

namespace ASCOM.Wise40.Hardware
{
    public class TimeProportionedPidController: PidController
    {
        private float _windowSize;          // millis
        private WisePin _pin;
        private DateTime windowStartTime;
        private float _output;

        private float readOutput()
        {
            return _output;
        }

        private void writeOutput(float value)
        {
            _output = value;
        }

        TimeProportionedPidController(float windowSize, IOnOff relay, TimeSpan samplingRate,
            Func<float> readProcess, Func<float> readOutput, Func<float> readSetPoint, 
            float proportionalGain, float integralGain, float derivativeGain,
            ControllerDirection controllerDirection = ControllerDirection.Direct,
            ControllerMode controllerMode = ControllerMode.Automatic)
        {
            _windowSize = windowSize;
            _relay = relay;
            if (readProcess == null)
                throw new ArgumentNullException(nameof(readProcess), "Read process must not be null.");
            if (readOutput == null)
                throw new ArgumentNullException(nameof(readOutput), "Read output must not be null.");
            if (readSetPoint == null)
                throw new ArgumentNullException(nameof(readSetPoint), "Read set-point must not be null.");

            SamplingRate = samplingRate;
            SetOutputLimits(0, _windowSize);
            _readProcess = readProcess;
            _readOutput = readOutput;
            _writeOutput = writeOutput;
            _readSetPoint = readSetPoint;
            ProportionalGain = proportionalGain;
            IntegralGain = integralGain;
            DerivativeGain = derivativeGain;
            ControllerDirection = controllerDirection;
            ControllerMode = controllerMode;
        }

        protected override void Compute()
        {
            DateTime now = DateTime.Now;

            base.Compute();

            if ((now - windowStartTime).TotalMilliseconds > _windowSize)
            {
                windowStartTime.AddMilliseconds(_windowSize);
            }

            if (_output > (now - windowStartTime).TotalMilliseconds)
                _relay.SetOn();
            else
                _relay.SetOff();
        }
    }
}
