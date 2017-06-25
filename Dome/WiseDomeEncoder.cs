using System;
using ASCOM.Utilities;
using MccDaq;
using System.Collections.Generic;

using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40.Dome
{
    public class WiseDomeEncoder : WiseEncoder
    {
        private Angle azimuth;
        private bool _calibrated = false;

        // Simulation stuff
        private int simulatedValue = 0;                         // the simulated dome encoder value
        private Angle simulatedStuckAzimuth;
        private DateTime endSimulatedStuck;                     // time when the dome-stuck simulation should end
        private System.Threading.Timer simulationTimer;         // times simulated-dome events
        private const int _simulatedEncoderTicksPerSecond = 6;

        private uint _caliTicks;
        private Angle _caliAz = new Angle(254.6, Angle.Type.Az);
        private WiseDome.Direction _movingDirection;
        private const int _hwTicks = 1024;
        private bool _initialized = false;
        
        private Hardware.Hardware hw = Hardware.Hardware.Instance;

        private static readonly WiseDomeEncoder _instance = new WiseDomeEncoder(); // Singleton

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
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "DomeEnc";
            base.init("DomeEnc",
                _hwTicks,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.domeboard, port = DigitalPortType.FirstPortCH, mask = 0x03 },
                    new WiseEncSpec() { brd = hw.domeboard, port = DigitalPortType.FirstPortB,  mask = 0xff },
                },
                true);

            if (Simulated)
            {
                simulatedValue = 0;
                simulatedStuckAzimuth = Angle.invalidAz;
                int simulationTimeout = 1000 / _simulatedEncoderTicksPerSecond;
                simulationTimer = new System.Threading.Timer(new System.Threading.TimerCallback(onSimulationTimer));
                simulationTimer.Change(simulationTimeout, simulationTimeout);
            }
            _initialized = true;
        }

        public void setMovement(WiseDome.Direction dir)
        {
            _movingDirection = dir;
        }
        
        private void onSimulationTimer(object state)
        {
            if (! Simulated)
                return;

            DateTime rightNow = DateTime.Now;

            if (_instance.simulatedStuckAzimuth != Angle.invalidAz)                // A simulatedStuck is required
            {
                if (Math.Abs(_instance.Azimuth.Degrees - simulatedStuckAzimuth.Degrees) <= 1.0)       // we're in the vicinity of the simulatedStuckAzimuth
                {
                    if (endSimulatedStuck.Equals(DateTime.MinValue))        // endSimulatedStuck is not set
                        endSimulatedStuck = rightNow.AddSeconds(3);         // set it to (now + 3sec)

                    if (DateTime.Compare(rightNow, endSimulatedStuck) < 0)  // is it time to end simulatedStuck?
                        return;                                             // not yet - don't modify simulatedValue
                    else
                    {
                        simulatedStuckAzimuth = Angle.invalidAz;
                        endSimulatedStuck = DateTime.MinValue;
                    }
                }
            }

            switch (_instance._movingDirection)
            {
                case WiseDome.Direction.CCW:
                    _instance.simulatedValue++;
                    break;

                case WiseDome.Direction.CW:
                    _instance.simulatedValue--;
                    break;
            }

            if (_instance.simulatedValue < 0)
                _instance.simulatedValue = 1023;
            if (_instance.simulatedValue > 1023)
                _instance.simulatedValue = 0;
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "DomeEncoder: Calibrate({0}) at {1}", az.ToString(), _caliTicks);
            #endregion
        }

        public bool Calibrated
        {
            get
            {
                return _calibrated;
            }
        }

        public Angle Azimuth
        {
            get
            {
                uint currTicks;
                Angle az;

                if (!_calibrated)
                    return Angle.invalidAz;

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
                    Name, az.ToNiceString(), currTicks, _caliTicks, _caliAz.ToNiceString());
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
        public new uint Value
        {
            get
            {
                uint ret = Simulated ? (uint)simulatedValue : base.Value;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "WiseDome: encoder: ret {0}", ret);
                #endregion
                return ret;
            }

            set
            {
                if (!Simulated)
                    return;
                simulatedValue = (int) value;
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