using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40
{
    public class WiseDecEncoder : IEncoder
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;
        private bool _simulated;
        private string _name;
        private uint _daqsValue;
        
        private AtomicReader wormAtomicReader, axisAtomicReader;
        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        private bool _connected = false;

        public Angle _angle;

        const double decMultiplier = 2 * Math.PI / 600 / 4096;
        const double DecCorrection = 0.35613322;                //20081231 SK: ActualDec-Encoder Dec [rad]

        private Common.Debugger debugger = new Debugger();
        private Hardware.Hardware hw = Hardware.Hardware.Instance;

        public WiseDecEncoder(string name)
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            List<WiseDaq> wormDaqs, axisDaqs, teleDaqs;

            teleDaqs = hw.teleboard.daqs;

            wormDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.FirstPortA);
            wormDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.SecondPortCL);
            axisDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.SecondPortB);
            axisDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.SecondPortA);

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

            _angle = simulated ?
                Angle.FromDegrees(90.0, Angle.Type.Dec) - WiseSite.Instance.Latitude :
                Angle.FromRadians((Value * decMultiplier) + DecCorrection, Angle.Type.Dec);

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                debugger.Level = Convert.ToUInt32(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Debug Level", string.Empty, "0"));
                //debugger.Level = (uint)Debugger.DebugLevel.DebugAll;
            }
        }

        public double Declination
        {
            get
            {
                return Degrees;
            }
        }

        public void Connect(bool connected)
        {
            if (connected)
            {
                wormDaqLow.setOwners("DecWormLow");
                wormDaqHigh.setOwners("DecWormHigh");
                axisDaqLow.setOwners("DecAxisLow");
                axisDaqHigh.setOwners("DecAxisHigh");
            }
            else
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

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public uint Value
        {
            get
            {
                if (!simulated)
                {
                    List<uint> daqValues;
                    uint worm, axis;

                    daqValues = wormAtomicReader.Values;
                    worm = (daqValues[1] << 8) | daqValues[0];

                    daqValues = axisAtomicReader.Values;
                    axis = (daqValues[0] >> 4) | (daqValues[1] << 4);

                    _daqsValue = ((axis * 600 + worm) & 0xfff000) + worm;
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, 
                        "{0}: value: {1}, axis: {2}, worm: {3}", 
                        name, _daqsValue, axis, worm);
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
                if(! simulated)
                    _angle.Radians = (Value * decMultiplier) + DecCorrection;

                if (_angle.Radians > Math.PI)
                    _angle.Radians -= 2 * Math.PI;

                return _angle;
            }

            set
            {
                if (simulated)
                {
                    _angle.Degrees = value.Degrees;
                    Value = (uint) Math.Round((_angle.Radians - DecCorrection) / decMultiplier);
                }
            }
        }

        public double Degrees
        {
            get
            {
                if (!simulated)
                {
                    uint v = Value;
                    _angle.Radians = (v * decMultiplier) + DecCorrection;

                    while (_angle.Radians > Math.PI)
                        _angle.Radians -= 2 * Math.PI;

                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders,
                        "[{0}] {1} Degrees - Value: {2}, deg: {3}", this.GetHashCode(), name, v, _angle);
                }

                return _angle.Degrees;
            }

            set
            {
                _angle.Degrees = value;
                if (simulated)
                {
                    _daqsValue = (uint) ((_angle.Radians - DecCorrection) / decMultiplier);
                }
            }
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
