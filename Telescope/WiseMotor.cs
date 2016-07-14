using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40.Hardware
{
    /// <summary>
    /// A WiseVirtualMotor implements the three moving rates available for each axis at the Wise40 telescope
    ///  by turning on (and off) the relevant hardware pins, as follows:
    ///   - rateSlew:  motorPin + slewPin
    ///   - rateSet:   motorPin
    ///   - rateGuide: guidePin
    ///   
    /// When simulated the WiseVirtualMotor will also increase/decrease the value of the relevant (simulated) encoder.
    /// </summary>
    public class WiseVirtualMotor : WiseObject, IConnectable, IDisposable, ISimulated
    {
        private WisePin motorPin, guideMotorPin, slewPin;
        private List<WisePin> activePins;
        private List<object> encoders;
        private object encodersLock = new object();
        private System.Threading.Timer simulationTimer;
        private double currentRate;
        private int simulationTimerFrequency;
        private int timer_counts;
        private DateTime prevTick;
        private TelescopeAxes _axis;
        private Const.AxisDirection _direction;   // There are separate WiseMotor instances for North, South, East, West
        private bool _simulated = false;
        private bool _connected = false;

        public WiseVirtualMotor(
            string name,
            WisePin motorPin,
            WisePin guideMotorPin,
            WisePin slewPin,
            TelescopeAxes axis,
            Const.AxisDirection direction,
            List<object> encoders = null)
        {
            this.name = name;

            this.motorPin = motorPin;
            this.guideMotorPin = guideMotorPin;
            this.slewPin = slewPin;

            this.encoders = encoders;
            this._axis = axis;
            this._direction = direction;
            simulated = motorPin.Simulated || (guideMotorPin != null && guideMotorPin.Simulated);

            if (simulated && (encoders == null || encoders.Count() == 0))
                throw new WiseException(name + ": A simulated WiseVirtualMotor must have at least one encoder reference");

            if (simulated)
            {
                simulationTimerFrequency = 15;
                TimerCallback TimerCallback = new TimerCallback(bumpEncoders);
                simulationTimer = new System.Threading.Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void SetOn(double rate)
        {
            rate = Math.Abs(rate);
            WiseTele.Instance.debugger.WriteLine(Debugger.DebugLevel.DebugMotors, "{0}: On at {1}", name, WiseTele.RateName(rate));

            if (rate == Const.rateSlew)
                activePins = new List<WisePin>() { slewPin, motorPin };
            else if (rate == Const.rateSet || rate == Const.rateTrack)
                activePins = new List<WisePin>() { motorPin };
            else if (rate == Const.rateGuide)
                activePins = new List<WisePin>() { guideMotorPin };
            currentRate = rate;

            foreach (WisePin pin in activePins)
                pin.SetOn();

            if (simulated)
            {
                timer_counts = 0;
                prevTick = DateTime.Now;
                simulationTimer.Change(0, 1000 / simulationTimerFrequency);
            }
        }

        public void SetOff()
        {
            WiseTele.Instance.debugger.WriteLine(Debugger.DebugLevel.DebugMotors,
                "{0}: Off (was at {1})", name, WiseTele.RateName(currentRate));

            if (simulated)
                simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if ((activePins != null) && (activePins.Count > 0))
            {
                for (int i = activePins.Count - 1; i >= 0; i--)
                {
                    activePins[i].SetOff();
                    activePins.RemoveAt(i);
                }
            }
            currentRate = Const.rateStopped;
        }

        public bool isOn
        {
            get
            {
                return (activePins != null) && activePins.Count != 0;
            }
        }

        public bool isOff
        {
            get
            {
                return !isOn;
            }
        }

        private void bumpEncoders(object StateObject)
        {
            if (!simulated)
                return;

            double degrees = currentRate / simulationTimerFrequency;
            Angle angle = (_axis == TelescopeAxes.axisPrimary) ? Angle.FromHours(degrees/15.0) : Angle.FromDegrees(degrees);

            foreach (IEncoder encoder in encoders)
            {
                Angle before, after;
                string op;

                lock (encodersLock)
                {
                    before = _axis == TelescopeAxes.axisPrimary ?
                    Angle.FromHours(WiseTele.Instance.RightAscension) :
                    Angle.FromDegrees(WiseTele.Instance.Declination);

                    op = _direction == Const.AxisDirection.Increasing ? "+" : "-";
                    if (_direction == Const.AxisDirection.Increasing)
                        encoder.Angle += angle;
                    else
                        encoder.Angle -= angle;
                    after = _axis == TelescopeAxes.axisPrimary ? 
                        Angle.FromHours(WiseTele.Instance.RightAscension) : 
                        Angle.FromDegrees(WiseTele.Instance.Declination);
                }

                if (WiseTele.Instance.debugger.Debugging(Debugger.DebugLevel.DebugMotors))
                {
                    WiseTele.Instance.debugger.WriteLine(Debugger.DebugLevel.DebugMotors,
                        "bumpEncoders: {0}: {1}: {2}: (angle: {3}, op: {10}) {4} => {5} ({6} => {7}) (#{8}, {9} ms)",
                        name,
                        encoder.name,
                        _axis == TelescopeAxes.axisPrimary ? "ra" : "dec",
                        angle.Degrees,
                        before,
                        after,
                        before.Degrees,
                        after.Degrees,
                        timer_counts++,
                        DateTime.Now.Subtract(prevTick).Milliseconds,
                        op);
                }
                prevTick = DateTime.Now;
            }
        }

        public void Connect(bool connected)
        {
            foreach (WisePin pin in new List<WisePin>() { motorPin, guideMotorPin })
                if (pin != null)
                    pin.Connect(connected);
            if (connected && slewPin != null && !slewPin.Connected)
                slewPin.Connect(connected);
            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose()
        {
            foreach (WisePin pin in new List<WisePin>() { motorPin, guideMotorPin })
                if (pin != null)
                    pin.Dispose();
        }

        public override string ToString()
        {
            return name;
        }

        public bool simulated
        {
            get
            {
                return _simulated;
            }
            set
            {
                _simulated = value;
            }
        }
    }
}