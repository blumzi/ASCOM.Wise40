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
    public class WiseHAEncoder: IConnectable, IDisposable, IDegrees
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;

        private const string name = "TeleHAEncoder";
        private AtomicReader wormAtomicReader, axisAtomicReader;

        private Astrometry.NOVAS.NOVAS31 Novas31;

        public WiseHAEncoder()
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
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

            wormAtomicReader = new AtomicReader(wormDaqs);
            axisAtomicReader = new AtomicReader(axisDaqs);
        }

        private double HAcorrection()
        {
            return 0.0;
        }

        public double HourAngle
        {
            get
            {
                List<uint> daqValues;
                double enc;
                const double encMultiplier = 2 * Math.PI / 720 / 4096;
                uint worm, axis;

                daqValues = wormAtomicReader.Values;
                worm = (daqValues[1] << 8) | daqValues[0];

                daqValues = axisAtomicReader.Values;
                axis = (daqValues[0] >> 4) | (daqValues[1] << 4);

                enc = (((axis * 720 - worm) & 0xfff000) + worm) * encMultiplier;

                return enc + HAcorrection();
            }
        }

        public double RightAscension
        {
            get
            {

                return HourAngle - WiseSite.Instance.LocalSiderealTime;
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
        }

        public void Dispose()
        {
            foreach (WiseDaq daq in daqs)
                daq.unsetOwners();
        }

        public double Degrees
        {
            get
            {
                return 0.0; //TBD: transform encoder Value to Degrees
            }

            set
            {
                ; // TBD: transform value (in degrees) to encoder value
            }
        }
    }
}
