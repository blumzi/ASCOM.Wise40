using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDecEncoder : IEncoder
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;
        private bool _simulated;
        private string _name;
        private uint _value;
        
        private AtomicReader wormAtomicReader, axisAtomicReader;
        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        private bool _connected = false;

        public Angle _angle;

        const double decMultiplier = 2 * Math.PI / 600 / 4096;
        const double DecCorrection = 0.35613322;                //20081231 SK: ActualDec-Encoder Dec [rad]

        public WiseDecEncoder(string name)
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            List<WiseDaq> wormDaqs, axisDaqs, teleDaqs;

            teleDaqs = Hardware.Instance.teleboard.daqs;

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

            _angle = simulated ? Angle.FromDeg(90.0 - WiseSite.Instance.Latitude) : Angle.FromRad((Value * decMultiplier) + DecCorrection);
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
                    uint worm, axis, val;

                    daqValues = wormAtomicReader.Values;
                    worm = ((daqValues[1] & 0x000f) << 8) | daqValues[0];       // ((SecondPortCL & 0x000f) << 8) | FirstPortA

                    daqValues = axisAtomicReader.Values;
                    axis = ((daqValues[0] & 0xff) >> 4) | (daqValues[1] << 4);  // ((SecondPortB & 0xff) >> 4) | (SecondPortA << 4)

                    // DecEnc:= ((DecAxisEnc * 600 + DecWormEnc) AND $FFF000) -DecWormEnc; //MASK lower 12 bits of DecAxisDAC
                    // Dec_Enc:= DecEnc * 2.5566346464760687161968126491532e-6;  //2*pi/600/4096
                    val = ((axis * 600 + worm) & 0xfff000) - worm;
                    return (uint) (val * (2 * Math.PI / 600 / 4096));
                }
                return _value;
            }

            set
            {
                if (simulated)
                    _value = value;
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
                    _angle.Degrees = value.Degrees;
            }
        }

        public double Degrees
        {
            get
            {
                double radians = 0;

                if (!simulated)
                {
                    radians = (Value * decMultiplier) + DecCorrection;

                    if (radians > Math.PI)
                        radians -= 2 * Math.PI;
                }
                _angle.Degrees = radians * 180.0 / Math.PI;

                return _angle.Degrees;
            }

            set
            {
                _angle.Degrees = value;
                if (simulated)
                {
                    _value = (uint) ((_angle.Radians - DecCorrection) / decMultiplier);
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
