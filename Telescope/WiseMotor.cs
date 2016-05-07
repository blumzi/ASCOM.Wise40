using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WiseMotor : WiseObject, IConnectable, IDisposable, ISimulated
    {
        private WisePin motorPin, guideMotorPin, slewPin;
        private List<WisePin> activePins;
        private List<object> encoders;
        private System.Threading.Timer simulationTimer;
        private double currentRate;
        private int times_per_sec;
        private int timer_counts;
        private DateTime prevTick;
        private bool increase;
        private bool _simulated = false;
        private bool _connected = false;

        public WiseMotor(string name, WisePin motorPin, WisePin guideMotorPin, WisePin slewPin, List<object> encoders = null, bool increase = true)
        {
            this.name = name;

            this.motorPin = motorPin;
            this.guideMotorPin = guideMotorPin;
            this.slewPin = slewPin;

            this.encoders = encoders;
            this.increase = increase;
            simulated = motorPin.simulated || (guideMotorPin != null && guideMotorPin.simulated);

            if (simulated && (encoders == null || encoders.Count() == 0))
                throw new WiseException(name + ": A simulated motor must have and encoder reference");

            if (simulated)
            {
                times_per_sec = 15;
                TimerCallback TimerCallback = new TimerCallback(bumpEncoders);
                simulationTimer = new System.Threading.Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void SetOn(double rate)
        {
            WiseTele.Instance.log("{0}: On, rate: {1}", name, rate);

            rate = Math.Abs(rate);
            if (rate == WiseTele.rateSlew)
                activePins = new List<WisePin>() { motorPin, slewPin };
            else if (rate == WiseTele.rateSet || rate == WiseTele.rateTrack)
                activePins = new List<WisePin>() { motorPin };
            else if (rate == WiseTele.rateGuide)
                activePins = new List<WisePin>() { guideMotorPin };
            currentRate = rate;

            foreach (WisePin pin in activePins)
                pin.SetOn();

            if (simulated)
            {
                timer_counts = 0;
                prevTick = DateTime.Now;
                simulationTimer.Change(0, 1000 / times_per_sec);
            }
        }

        public void SetOff()
        {
            WiseTele.Instance.log("{0}: Off", name);

            if (simulated)
                simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (activePins == null || activePins.Count == 0)
                return;

            foreach (WisePin pin in activePins)
                pin.SetOff();
            currentRate = WiseTele.rateStopped;
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

            double increment = ((increase) ? currentRate : -currentRate) / times_per_sec;

            foreach (IDegrees encoder in encoders)
            {
                Angle before = new Angle(encoder.Degrees), inc = new Angle(increment);
                encoder.Degrees += increment;
                Angle after = new Angle(encoder.Degrees);

                WiseTele.Instance.log("{0}: {1} += {2} ({3} + {4} = {5}) (#{6}, {7} dMillis)", name, encoder.name, increment,
                    before.ToString(Angle.Format.Deg), inc.ToString(Angle.Format.Deg), after.ToString(Angle.Format.Deg), timer_counts++, DateTime.Now.Subtract(prevTick).Milliseconds);
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