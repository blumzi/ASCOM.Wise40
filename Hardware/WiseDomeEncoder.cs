using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using ASCOM.Utilities;
using ASCOM.Wise40.Properties;
using MccDaq;

namespace ASCOM.WiseHardware
{
    public class WiseDomeEncoder : IConnectable, IDisposable 
    {
        private WiseEncoder hwEncoder;
        private double azimuth;
        public bool calibrated;
        private bool simulated;
        private int simulatedValue;
        private System.Timers.Timer simulationTimer;
        private TraceLogger logger;

        private int caliTicks;
        private double caliAz;
        private WiseDome.Direction moving;
        private const int simulatedEncoderTicksPerSecond = 6;
        private bool connected;

        private void log(string fmt, params object[] o)
        {
            string msg = String.Format(fmt, o);

            logger.LogMessage("WiseDomeEncoder", msg);
        }

        public WiseDomeEncoder(string name, TraceLogger logger, bool simulated = false)
        {
            WiseEncSpec[] encSpecs;

            encSpecs = new WiseEncSpec[] {
                new WiseEncSpec() { brd = Hardware.Instance.domeboard, port = DigitalPortType.FirstPortB },
                new WiseEncSpec() { brd = Hardware.Instance.domeboard, port = DigitalPortType.FirstPortCH, mask = 0x3 },
            };

            hwEncoder = new WiseEncoder(name, 1024, encSpecs, true, 100);
            this.simulated = simulated;
            if (this.simulated)
            {
                simulatedValue = 0;
                simulationTimer = new System.Timers.Timer(1000 / 6);
                simulationTimer.Elapsed += onSimulationTimer;
                simulationTimer.Enabled = true;
            }

            this.logger = logger;
            connected = false;
        }

        public void setMovement(WiseDome.Direction dir)
        {
            moving = dir;
        }

        private void onSimulationTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!connected)
                return;

            switch (moving)
            {
                case WiseDome.Direction.CCW:
                    simulatedValue++;
                    break;

                case WiseDome.Direction.CW:
                    simulatedValue--;
                    break;
            }

            if (simulatedValue < 0)
                simulatedValue = 1023;
            if (simulatedValue > 1023)
                simulatedValue = 0;
        }


        public void SetAzimuth(double az)
        {
            Calibrate(az);
            Azimuth = az;
        }

        public void Calibrate(double az)
        {
            caliTicks = Value;
            caliAz = az;
            calibrated = true;
        }

        public void Connect(bool connected)
        {
            this.connected = connected;
            hwEncoder.Connect(this.connected);
        }

        public void Dispose()
        {
            hwEncoder.Dispose();
        }

        public double Azimuth
        {
            get
            {
                int currTicks;
                double ret;

                if (!calibrated)
                    return -1.0;

                currTicks = Value;
                if (currTicks == caliTicks)
                {
                    ret = caliAz;
                }
                else if (currTicks > caliTicks)
                {
                    ret = caliAz - (currTicks - caliTicks) * WiseDome.DegreesPerTick;
                }
                else
                {
                    ret = caliAz + (caliTicks - currTicks) * WiseDome.DegreesPerTick;
                }

                if (ret > 360)
                    ret -= 360;

                if (ret < 0)
                    ret += 360;

                // log("Azimuth: currTicks: {0}, caliTicks: {1}, caliAz: {2}, ret: {3}", currTicks, caliTicks, caliAz, ret);
                return ret;
            }

            set
            {
                azimuth = value;
            }
        }

        public int Value
        {
            get
            {
                return (simulated) ? simulatedValue : hwEncoder.Value;
            }
        }
    }
}

