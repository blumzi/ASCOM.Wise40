using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class AtomicReader
    {
        private const uint timeoutMicros = 100;    // microseconds between Daq reads
        private Stopwatch stopwatch;
        private const int maxTries = 5;
        List<WiseDaq> daqs;

        public AtomicReader(List<WiseDaq> daqs)
        {
            stopwatch = new Stopwatch();
            this.daqs = daqs;
        }

        public List<uint> Values
        {
            get
            {
                List<uint> results = new List<uint>(daqs.Count());
                int i;

                for (int tries = 0; tries < maxTries; tries++)
                {
                    for (i = 0; i < daqs.Count(); i++)
                    {
                        if (daqs[i].wiseBoard.type == WiseBoard.BoardType.Hard && i > 0)
                            stopwatch.Start();

                        results.Add(daqs[i].Value);

                        if (daqs[i].wiseBoard.type == WiseBoard.BoardType.Hard && i > 0)
                        {
                            stopwatch.Stop();
                            if (stopwatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L)) > timeoutMicros)
                                goto nexttry;
                        }
                    }

                    if (i == daqs.Count())
                        return results;

                    nexttry:;
                }

                string err = "Failed to read daqs: ";
                foreach (WiseDaq daq in daqs)
                    err += daq.name + ", ";
                err += "within " + timeoutMicros.ToString() + " microSeconds";

                throw new WiseException(err);
            }
        }
    }
}
