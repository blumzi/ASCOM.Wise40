using System;
using System.Collections.Generic;
using MccDaq;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
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
        //private uint _value_at_fiducial_point = 1433418;

        private AtomicReader wormAtomicReader, axisAtomicReader;

        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        public Angle _angle;

        const double HaMultiplier = 2 * Math.PI / 720 / 4096;
        const double HaCorrection = -3.063571542;                   // 20081231: Shai Kaspi

        private Common.Debugger debugger = new Debugger();

        public WiseHAEncoder(string name)
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();

            List<WiseDaq> wormDaqs, axisDaqs, teleDaqs;

            teleDaqs = Hardware.Instance.teleboard.daqs;

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

            simulated = false;
            foreach (WiseDaq d in daqs)
            {
                if (d.wiseBoard.type == WiseBoard.BoardType.Soft)
                {
                    simulated = true;
                    break;
                }
            }
            _name = name;

            _angle = simulated ? new Angle("00:00:00.0") : Angle.FromRadians((Value * HaMultiplier) + HaCorrection);

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
                if (! simulated)
                {
                    List<uint> daqValues;
                    uint worm, axis;

                    daqValues = wormAtomicReader.Values;
                    worm = (daqValues[1] << 8) | daqValues[0];
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "HA worm: {0}", worm);

                    daqValues = axisAtomicReader.Values;
                    axis = (daqValues[0] >> 4) | (daqValues[1] << 4);
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "HA axis: {0}", axis);

                    _daqsValue = ((axis * 720 - worm) & 0xfff000) + worm;
                }
                return _daqsValue;
            }

            set
            {
                if (simulated)
                    _daqsValue = value;
            }
        }

        public Angle Angle
        {
            get
            {
                if (!simulated)
                    _angle.Radians = (Value * HaMultiplier) + HaCorrection;

                return _angle;
            }

            set
            {
                if (simulated)
                    _angle.Degrees = value.Degrees;
            }
        }

        public double Degrees
        {
            get
            {
                if (!simulated)
                    _angle.Radians = (_daqsValue * HaMultiplier) + HaCorrection;

                return Angle.Degrees;
            }

            set
            {
                double before = _angle.Degrees;
                _angle.Degrees = value;
                double after = _angle.Degrees;
                double delta = after - before;

                if (simulated)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "{0}: {1} + {2} = {3}", name, new Angle(before), new Angle(delta), new Angle(after));
                    _daqsValue = (uint)((_angle.Radians + HaCorrection) / HaMultiplier);
                }
            }
        }

        /// <summary>
        /// Right Ascension in Hours
        /// </summary>
        public Angle RightAscension
        {
            get
            {
                Angle ret = WiseSite.Instance.LocalSiderealTime - Angle.FromDegrees(Degrees);
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "RightAscension: {0}", ret);

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