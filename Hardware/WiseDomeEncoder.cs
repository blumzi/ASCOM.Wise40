using System;
using ASCOM.Utilities;
using MccDaq;
using System.Collections.Generic;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDomeEncoder : IConnectable
    {
        private Angle azimuth;
        public bool calibrated;

        private bool _simulated = Environment.MachineName != "dome-ctlr"; 
        private int simulatedValue;                     // the simulated dome encoder value
        private Angle simulatedStuckAzimuth;
        private DateTime endSimulatedStuck;             // time when the dome-stuck simulation should end
        private System.Timers.Timer simulationTimer;    // times simulated-dome events

        private int caliTicks;
        private Angle caliAz = new Angle(254.6);
        private WiseDome.Direction moving;
        private const int simulatedEncoderTicksPerSecond = 6;
        private bool _connected = false;
        private const int hwTicks = 1024;
        private Angle NoSimulatedStuckAz = Angle.invalid;
        private Debugger debugger;

        private WiseDaq encDaqLow, encDaqHigh;
        private AtomicReader encAtomicReader;

        public WiseDomeEncoder(string name, Debugger debugger)
        {
            this.debugger = debugger;
            encDaqLow = Hardware.Instance.domeboard.daqs.Find(x => x.porttype == DigitalPortType.FirstPortB);
            encDaqHigh = Hardware.Instance.domeboard.daqs.Find(x => x.porttype == DigitalPortType.FirstPortCH);
            encAtomicReader = new AtomicReader(new List<WiseDaq>() { encDaqHigh, encDaqLow });

            if (_simulated)
            {
                simulatedValue = 0;
                simulatedStuckAzimuth = NoSimulatedStuckAz;
                simulationTimer = new System.Timers.Timer(1000 / 6);
                simulationTimer.Elapsed += onSimulationTimer;
                simulationTimer.Enabled = true;
            }
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
                if (Math.Abs(Azimuth.Degrees - simulatedStuckAzimuth.Degrees) <= 1.0)       // we're in the vicinity of the simulatedStuckAzimuth
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


        public void SetAzimuth(Angle az)
        {
            Calibrate(az);
            Azimuth = az;
        }

        public void SimulateStuckAt(Angle az)
        {
            simulatedStuckAzimuth = az;
        }

        public void Calibrate(Angle az)
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
                encDaqHigh.setOwner(encDaqHigh.name, 0);
                encDaqHigh.setOwner(encDaqHigh.name, 1);
                encDaqLow.setOwners("domeEncLow");
            }
            else
            {
                encDaqHigh.unsetOwner(0);
                encDaqHigh.unsetOwner(1);
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

        public Angle Azimuth
        {
            get
            {
                int currTicks;
                Angle az;

                if (!calibrated)
                    return new Angle(double.NaN);

                currTicks = Value;
                if (currTicks == caliTicks)
                {
                    az = caliAz;
                }
                else if (currTicks > caliTicks)
                {
                    az = caliAz - new Angle((currTicks - caliTicks) * WiseDome.DegreesPerTick);
                }
                else
                {
                    az = caliAz + new Angle((caliTicks - currTicks) * WiseDome.DegreesPerTick);
                }

                if (az.Degrees > 360)
                    az.Degrees -= 360;

                if (az.Degrees < 0)
                    az.Degrees += 360;

                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "Azimuth: {0}, currTicks: {1}, caliTicks: {2}, caliAz: {3}",
                    az, currTicks, caliTicks, caliAz);
                return az;
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

                if (_simulated)
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