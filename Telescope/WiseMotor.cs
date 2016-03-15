using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WiseMotor : WiseObject, IConnectable, IDisposable
    {
        private WisePin pin;
        private List<object> encoders;
        private Timer timer;
        private double rate;
        private bool increase;
        private bool simulated;

        public WiseMotor(string name, WisePin pin, double rate, List<object> encoders = null, bool increase = true)
        {
            this.name = name;
            this.pin = pin;
            this.rate = rate;
            this.encoders = encoders;
            this.increase = increase;
            simulated = pin.simulated;

            if (simulated && (encoders == null || encoders.Count() == 0))
                throw new WiseException(name + ": A simulated motor must have and encoder reference");

            timer = new Timer();
            timer.Interval = 1000;     // milliseconds
            timer.Tick += timer_Tick;
        }

        public void SetOn()
        {
            Console.WriteLine("{0}: On", name);
            pin.SetOn();
            timer.Enabled = true;
        }

        public void SetOff()
        {
            Console.WriteLine("{0}: Off", name);
            pin.SetOff();
            timer.Enabled = false;
        }

        public bool isOn
        {
            get
            {
                return pin.isOn;
            }
        }

        public bool isOff
        {
            get
            {
                return !isOn;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            double increment = (increase) ? rate : -rate;

            foreach (IDegrees encoder in encoders)
                encoder.Degrees += increment;
        }

        public void Connect(bool connected)
        {
            pin.Connect(connected);
        }

        public void Dispose()
        {
            pin.Dispose();
        }

        public override string ToString()
        {
            return name;
        }
    }
}