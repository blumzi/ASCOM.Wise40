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
        private bool _calibrated = false;

        private bool _simulated = Environment.MachineName.ToLower() != "dome-ctlr"; 
        private uint simulatedValue;                     // the simulated dome encoder value
        private Angle simulatedStuckAzimuth;
        private DateTime endSimulatedStuck;             // time when the dome-stuck simulation should end
        private System.Timers.Timer simulationTimer;    // times simulated-dome events

        private uint _caliTicks;
        private Angle _caliAz = new Angle(254.6, Angle.Type.Az);
        private WiseDome.Direction _movingDirection;
        private const int _simulatedEncoderTicksPerSecond = 6;
        private bool _connected = false;
        private const int _hwTicks = 1024;
        private Debugger debugger = Debugger.Instance;
        private string _name;
        private bool _initialized = false;

        private WiseDaq encDaqLow, encDaqHigh;
        private AtomicReader encAtomicReader;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;

        private static readonly WiseDomeEncoder instance = new WiseDomeEncoder(); // Singleton

        private object _lock = new object();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseDomeEncoder()
        {
        }

        public WiseDomeEncoder()
        {
        }

        public static WiseDomeEncoder Instance
        {
            get
            {
                return instance;
            }
        }

        public void init(string name)
        {
            if (_initialized)
                return;

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
            _initialized = true;
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
            _calibrated = true;
        }

        public bool Calibrated
        {
            get
            {
                return _calibrated;
            }
        }

        public void Connect(bool connected)
        {
            _connected = connected;
            if (connected)
            {
                encDaqHigh.setOwner("domeEncLow", 0);
                encDaqHigh.setOwner("domeEncLow", 1);
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
                uint currTicks;
                Angle az;

                if (!_calibrated)
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

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "WiseDome: {0}: Azimuth: {1}, currTicks: {2}, caliTicks: {3}, caliAz: {4}",
                    _name, az.ToNiceString(), currTicks, _caliTicks, _caliAz.ToNiceString());
                #endregion
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
        public uint Value
        {
            get
            {
                uint ret;

                if (_simulated)
                    return simulatedValue;

                List<uint> values;
                lock (_lock)
                {
                    values = encAtomicReader.Values;
                }
                ret = ((values[1] << 8) | values[0]) & 0x3ff;
                ret = WiseEncoder.GrayCode[ret];
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "WiseDome: encoder: ret {0}", ret);
                #endregion
                return ret;
            }
        }

        /// <summary>
        /// Gets the native number of ticks per turn of the dome-encoder
        /// </summary>
        public uint Ticks
        {
            get
            {
                return _hwTicks;
            }
        }
    }
}