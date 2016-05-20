using System;
using ASCOM.Utilities;
using MccDaq;
using System.Collections.Generic;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDomeEncoder : IConnectable
    {
        //private WiseEncoder hwEncoder;
        private double azimuth;
        public bool calibrated;

        private bool simulated;                         // is the dome encoder simulated or real?
        private int simulatedValue;                     // the simulated dome encoder value
        private double simulatedStuckAzimuth;           // the azimuth where we simulate dome-stuck (simulated value does not change)?
        private DateTime endSimulatedStuck;             // time when the dome-stuck simulation should end
        private System.Timers.Timer simulationTimer;    // times simulated-dome events

        private TraceLogger logger;

        private int caliTicks;
        private double caliAz;
        private WiseDome.Direction moving;
        private const int simulatedEncoderTicksPerSecond = 6;
        private bool _connected = false;
        private const int hwTicks = 1024;
        private const double NoSimulatedStuckAz = -1.0;

        private WiseDaq encDaqLow, encDaqHigh;
        private AtomicReader encAtomicReader;

        private void log(string fmt, params object[] o)
        {
            string msg = String.Format(fmt, o);

            logger.LogMessage("WiseDomeEncoder", msg);
        }

        public WiseDomeEncoder(string name, TraceLogger logger, bool simulated = false)
        {
            encDaqLow = Hardware.Instance.domeboard.daqs.Find(x => x.porttype == DigitalPortType.FirstPortA);
            encDaqHigh = Hardware.Instance.domeboard.daqs.Find(x => x.porttype == DigitalPortType.SecondPortCL);
            encAtomicReader = new AtomicReader(new List<WiseDaq>() { encDaqLow, encDaqHigh });

            this.simulated = simulated;
            if (this.simulated)
            {
                simulatedValue = 0;
                simulatedStuckAzimuth = NoSimulatedStuckAz;
                simulationTimer = new System.Timers.Timer(1000 / 6);
                simulationTimer.Elapsed += onSimulationTimer;
                simulationTimer.Enabled = true;
            }

            this.logger = logger;
        }

        public void setMovement(WiseDome.Direction dir)
        {
            moving = dir;
        }

        private void onSimulationTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime rightNow = DateTime.Now;

            if (! Connected)
                return;

            if (simulatedStuckAzimuth != NoSimulatedStuckAz)                // A simulatedStuck is required
            {
                if (Math.Abs(Azimuth - simulatedStuckAzimuth) <= 1.0)       // we're in the vicinity of the simulatedStuckAzimuth
                {
                    if (endSimulatedStuck.Equals(DateTime.MinValue))        // endSimulatedStuck is not set
                        endSimulatedStuck = rightNow.AddSeconds(3);         // set it to (now + 3sec)

                    if (DateTime.Compare(rightNow, endSimulatedStuck) < 0)  // is it time to end simulatedStuck?
                        return;                                             // not yet - don't modify simulatedValue
                    else
                    {
                        simulatedStuckAzimuth = NoSimulatedStuckAz;
                        endSimulatedStuck = DateTime.MinValue;
                    }
                }
            }

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

        public void SimulateStuckAt(double az)
        {
            simulatedStuckAzimuth = az;
        }

        public void Calibrate(double az)
        {
            caliTicks = Value;
            caliAz = az;
            calibrated = true;
        }

        public void Connect(bool connected)
        {
            _connected = connected;
            if (connected)
            {
                encDaqHigh.setOwners("domeEncHigh");
                encDaqLow.setOwners("domeEncLow");
            }
            else
            {
                encDaqHigh.unsetOwners();
                encDaqLow.unsetOwners();
            }
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
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

        /// <summary>
        /// Gets the dome-encoder's value (either simulated or real)
        /// </summary>
        public int Value
        {
            get
            {

                if (simulated)
                    return simulatedValue;

                List<uint> values = encAtomicReader.Values;

                return (int) ((values[1] << 8) | values[0]);
            }
        }

        /// <summary>
        /// Gets the native number of ticks per turn of the dome-encoder
        /// </summary>
        public int Ticks
        {
            get
            {
                return hwTicks;
            }
        }
    }
}

