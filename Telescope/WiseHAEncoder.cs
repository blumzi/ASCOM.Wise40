using System;
using System.Collections.Generic;
using MccDaq;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40
{
    /// <summary>
    /// Implements the Wise40 HourAngle Encoder interface
    /// </summary>
    public class WiseHAEncoder: IEncoder
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;
        private bool _simulated = false;
        private bool _connected = false;
        private string _name;
        private uint _daqsValue;
        private const uint _realValueAtFiducialMark = 1432779; // Arie - 02 July 2016

        private AtomicReader wormAtomicReader, axisAtomicReader;

        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        public Angle _angle;

        const double HaMultiplier = 2 * Math.PI / 720 / 4096;
        const double HaCorrection = -3.063571542;                   // 20081231: Shai Kaspi
        //const double HaCorrection = -6.899777777777778;  // 20160702: Arie
        const uint _simulatedValueAtFiducialMark = _realValueAtFiducialMark;

        private Common.Debugger debugger = Common.Debugger.Instance;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;
        private static WiseSite wisesite = WiseSite.Instance;

        private static Object _lock = new object();

        public WiseHAEncoder(string name)
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            wisesite.init();

            List<WiseDaq> wormDaqs, axisDaqs, teleDaqs;

            teleDaqs = hw.teleboard.daqs;

            wormDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.ThirdPortA);
            wormDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortCL);
            axisDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortB);
            axisDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortA);

            wormDaqs = new List<WiseDaq>();
            wormDaqs.Add(wormDaqLow);
            wormDaqs.Add(wormDaqHigh);

            axisDaqs = new List<WiseDaq>();
            axisDaqs.Add(axisDaqLow);
            axisDaqs.Add(axisDaqHigh);

            daqs = new List<WiseDaq>();
            daqs.AddRange(wormDaqs);
            daqs.AddRange(axisDaqs);
            foreach (WiseDaq daq in daqs)
                daq.setDir(DigitalPortDirection.DigitalIn);

            wormAtomicReader = new AtomicReader(wormDaqs);
            axisAtomicReader = new AtomicReader(axisDaqs);

            Simulated = false;
            foreach (WiseDaq d in daqs)
            {
                if (d.wiseBoard.type == WiseBoard.BoardType.Soft)
                {
                    Simulated = true;
                    break;
                }
            }
            _name = name;

            if (Simulated)
                _angle = new Angle("00h00m00.0s");
            else
                _angle = Angle.FromRadians((Value * HaMultiplier) + HaCorrection);
 
            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                debugger.Level = Convert.ToUInt32(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Debug Level", string.Empty, "0"));
            }
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public uint Value
        {
            get {
                if (Simulated)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "{0}: value: {1}",
                        name, _daqsValue);
                }
                else
                {
                    uint worm, axis;
                    lock (_lock)
                    {
                        List<uint> daqValues;

                        daqValues = wormAtomicReader.Values;
                        worm = (daqValues[1] << 8) | daqValues[0];

                        daqValues = axisAtomicReader.Values;
                        axis = (daqValues[0] >> 4) | (daqValues[1] << 4);

                        _daqsValue = ((axis * 720 - worm) & 0xfff000) + worm;
                    }
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders,
                        "{0}: value: {1}, axis: {2}, worm: {3}",
                        name, _daqsValue, axis, worm);
                }

                return _daqsValue;
            }

            set
            {
                if (Simulated)
                    _daqsValue = value;
            }
        }

        public uint AxisValue
        {
            get
            {
                if (Simulated)
                    return 0;
                else
                {
                    List<uint> daqValues;
                    uint axis;

                    daqValues = axisAtomicReader.Values;
                    axis = (daqValues[0] >> 4) | (daqValues[1] << 4);

                    return axis;
                }
            }
        }

        public uint WormValue
        {
            get
            {
                if (Simulated)
                    return 0;
                else
                {
                    List<uint> daqValues;
                    uint worm;

                    daqValues = wormAtomicReader.Values;
                    worm = (daqValues[1] << 8) | daqValues[0];

                    return worm;
                }
            }
        }

        public Angle Angle
        {
            get
            {
                if (!Simulated)
                    _angle.Radians = (Value * HaMultiplier) + HaCorrection;

                return _angle;
            }

            set
            {
                if (Simulated)
                {
                    _angle = Angle.FromHours(value.Hours);
                    Value = (uint)Math.Round((_angle.Radians - HaCorrection) / HaMultiplier);
                }
            }
        }

        public double Degrees
        {
            get
            {
                if (!Simulated)
                    _angle.Radians = (_daqsValue * HaMultiplier) + HaCorrection;

                return Angle.Degrees;
            }

            set
            {
                Angle before = _angle;
                _angle.Degrees = value;
                Angle after = _angle;
                Angle delta = after - before;

                if (Simulated)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "[{0}] {1}: {2} + {3} = {4}", this.GetHashCode(), name, before, delta, after);
                    _daqsValue = (uint)((_angle.Radians + HaCorrection) / HaMultiplier);
                }
            }
        }

        private double Hours
        {
            get
            {
                return Angle.Hours;
            }
        }

        /// <summary>
        /// Right Ascension in Hours
        /// </summary>
        public Angle RightAscension
        {
            get
            {
                Angle ret = wisesite.LocalSiderealTime - Angle.FromHours(Hours, Angle.Type.RA);
                //debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "[{0}] RightAscension: {1}", this.GetHashCode(), ret);

                return ret;
            }
        }

        public void Connect(bool connected)
        {
            if (connected)
            {
                wormDaqLow.setOwners("HAWormLow");
                wormDaqHigh.setOwners("HAWormHigh");
                axisDaqLow.setOwners("HAAxisLow");
                axisDaqHigh.setOwners("HAAxisHigh");
            } else
            {
                wormDaqLow.unsetOwners();
                wormDaqHigh.unsetOwners();
                axisDaqLow.unsetOwners();
                axisDaqHigh.unsetOwners();
            }
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
            foreach (WiseDaq daq in daqs)
                daq.unsetOwners();
        }

        public bool Simulated
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

        public string name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }
    }
}