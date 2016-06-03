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
        private const uint timeoutMicros = 5000000;    // microseconds between Daq reads
        private Stopwatch stopwatch;
        private const int maxTries = 5;
        List<WiseDaq> daqs;

        Common.Debugger debugger = new Common.Debugger();

        public AtomicReader(List<WiseDaq> daqs)
        {
            stopwatch = new Stopwatch();
            this.daqs = daqs;

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                debugger.Level = Convert.ToUInt32(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Debug Level", string.Empty, "0"));
            }
        }

        public List<uint> Values
        {
            get
            {
                List<uint> results = new List<uint>();
                int i;
                List<long> elapsedMicros = new List<long>();
                List<long> elapsedTicks = new List<long>();

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
                            long micros = (stopwatch.ElapsedTicks / Stopwatch.Frequency) * 1000000L;
                            elapsedMicros.Add(micros);
                            elapsedTicks.Add(stopwatch.ElapsedTicks);
                            if (micros > timeoutMicros)
                                goto nexttry;
                        }
                    }

                    if (i == daqs.Count())
                    {
                        string s = "AtomicReader:Values: inter daqs (";
                        foreach (WiseDaq daq in daqs)
                            s += daq.name + " ";
                        s += ") " + elapsedMicros.Count + " read times: ";
                        foreach (long m in elapsedMicros)
                            s += m.ToString() + " ";

                        s += ", ticks: ";
                        foreach (long t in elapsedTicks)
                            s += t.ToString() + " ";
                        debugger.WriteLine(Common.Debugger.DebugLevel.DebugEncoders, s);

                        return results;
                    }

                    nexttry:;
                }
                
                string err = "Failed to read daqs: ";
                foreach (WiseDaq daq in daqs)
                    err += daq.name + ", ";
                err += "within " + timeoutMicros.ToString() + " microSeconds [";
                foreach (long m in elapsedMicros)
                    err += m.ToString() + ", ";
                err += "]";

                throw new WiseException(err);
            }
        }
    }
}
