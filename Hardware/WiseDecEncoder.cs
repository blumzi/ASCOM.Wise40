using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;

namespace ASCOM.Wise40.Hardware
{
    public class WiseDecEncoder : IConnectable, IDisposable, IDegrees
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;

        private const string name = "TeleDecEncoder";
        private AtomicReader wormAtomicReader, axisAtomicReader;
        private ASCOM.Astrometry.NOVAS.NOVAS31 Novas31;

        public WiseDecEncoder()
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
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

            wormAtomicReader = new AtomicReader(wormDaqs);
            axisAtomicReader = new AtomicReader(axisDaqs);
        }

        public double Declination
        {
            get
            {
                return 1.234; // TBD
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
                return 0.0; //TBD
            }

            set
            {
                ; // TBD
            }
        }
    }
}
