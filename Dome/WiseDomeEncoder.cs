using System;
using ASCOM.Utilities;
using MccDaq;
using System.Collections.Generic;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40
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

        private int _caliTicks;
        private Angle _caliAz = new Angle(254.6, Angle.Type.Az);
        private WiseDome.Direction _movingDirection;
        private const int _simulatedEncoderTicksPerSecond = 6;
        private bool _connected = false;
        private const int _hwTicks = 1024;
        private Debugger debugger = Debugger.Instance;
        private string _name;

        private WiseDaq encDaqLow, encDaqHigh;
        private AtomicReader encAtomicReader;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;

        public WiseDomeEncoder(string name)
        {
            _name = name;
            encDaqLow = hw.domeboard.daqs.Find(x => x.porttype == DigitalPortType.FirstPortB);
            encDaqHigh = hw.domeboard.daqs.Find(x => x.porttype == DigitalPortType.FirstPortCH);
            encAtomicReader = new AtomicReader(new List<WiseDaq>() { encDaqHigh, encDaqLow });

            if (_simulated)
            {
                simulatedValue = 0;
                simulatedStuckAzimuth = Angle.invalid;
                simulationTimer = new System.Timers.Timer(1000 / 6);
                simulationTimer.Elapsed += onSimulationTimer;
                simulationTimer.Enabled = true;
            }
        }

        public void setMovement(WiseDome.Direction dir)
        {
            _movingDirection = dir;
        }

        private void onSimulationTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime rightNow = DateTime.Now;

            if (! Connected)
                return;

            if (simulatedStuckAzimuth != Angle.invalid)                // A simulatedStuck is required
            {
                if (Math.Abs(Azimuth.Degrees - simulatedStuckAzimuth.Degrees) <= 1.0)       // we're in the vicinity of the simulatedStuckAzimuth
                {
                    if (endSimulatedStuck.Equals(DateTime.MinValue))        // endSimulatedStuck is not set
                        endSimulatedStuck = rightNow.AddSeconds(3);         // set it to (now + 3sec)

                    if (DateTime.Compare(rightNow, endSimulatedStuck) < 0)  // is it time to end simulatedStuck?
                        return;                                             // not yet - don't modify simulatedValue
                    else
                    {
                        simulatedStuckAzimuth = Angle.invalid;
                        endSimulatedStuck = DateTime.MinValue;
                    }
                }
            }

            switch (_movingDirection)
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
            _caliTicks = Value;
            _caliAz = az;
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
                    return Angle.invalid;

                currTicks = Value;
                if (currTicks == _caliTicks)
                {
                    az = _caliAz;
                }
                else if (currTicks > _caliTicks)
                {
                    az = _caliAz - new Angle((currTicks - _caliTicks) * WiseDome.DegreesPerTick, Angle.Type.Az);
                }
                else
                {
                    az = _caliAz + new Angle((_caliTicks - currTicks) * WiseDome.DegreesPerTick, Angle.Type.Az);
                }

                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "{0}: Azimuth: {1}, currTicks: {2}, caliTicks: {3}, caliAz: {4}",
                    _name, az.ToNiceString(), currTicks, _caliTicks, _caliAz.ToNiceString());
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
                return _hwTicks;
            }
        }
    }
}