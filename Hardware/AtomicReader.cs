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
        private const double timeoutMillis = 2000.0;    // milliseconds between Daq reads
        private Stopwatch stopwatch;
        private const int maxTries = 20;
        List<WiseDaq> daqs;

        Common.Debugger debugger = Common.Debugger.Instance;

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
                List<double> elapsedMillis = new List<double>();
                List<long> elapsedTicks = new List<long>();

                for (int tries = 0; tries < maxTries; tries++)
                {
                    for (i = 0; i < daqs.Count(); i++)
                    {
                        if (daqs[i].wiseBoard.type == WiseBoard.BoardType.Hard && i > 0)
                            stopwatch.Restart();

                        results.Add(daqs[i].Value);

                        if (daqs[i].wiseBoard.type == WiseBoard.BoardType.Hard && i > 0)
                        {
                            stopwatch.Stop();
                            double millis = stopwatch.Elapsed.TotalMilliseconds;
                            elapsedMillis.Add(millis);
                            elapsedTicks.Add(stopwatch.ElapsedTicks);
                            if (millis > timeoutMillis)
                                goto nexttry;
                        }
                    }

                    if (i == daqs.Count())
                    {
                        string s = "AtomicReader:Values: inter daqs (";
                        foreach (WiseDaq daq in daqs)
                            s += daq.name + " ";
                        s += ") " + elapsedMillis.Count + " read times: ";
                        foreach (double m in elapsedMillis)
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
                    err += daq.name + " ";
                err += ", within " + timeoutMillis.ToString() + " milliSeconds, max: ";
                err += elapsedMillis.Max().ToString();
                err += " [ ";
                foreach (double m in elapsedMillis)
                    err += m.ToString() + " ";
                err += "]";

                debugger.WriteLine(Common.Debugger.DebugLevel.DebugEncoders, err);
                throw new WiseException(err);
            }
        }
    }
}
