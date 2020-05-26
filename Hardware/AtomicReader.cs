using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class AtomicReader : WiseObject
    {
        public const double DefaultTimeoutMillis = 2000.0;
        public static readonly int DefaultMaxTries = 20;
        private readonly double _timeoutMillis;    // milliseconds between Daq reads
        private readonly int _maxTries;
        private readonly Stopwatch stopwatch;
        private readonly List<WiseDaq> daqs;
        private readonly object _lock = new object();

        private readonly Common.Debugger debugger = Common.Debugger.Instance;

        public AtomicReader(string name, List<WiseDaq> daqs,
            double timeoutMillis = Const.defaultReadTimeoutMillis,
            int maxTries = Const.defaultReadRetries)
        {
            WiseName = name;
            stopwatch = new Stopwatch();
            this.daqs = daqs;
            _timeoutMillis = timeoutMillis;
            _maxTries = maxTries;
        }

        public List<uint> Values
        {
            get
            {
                List<uint> results;
                List<double> elapsedMillis = new List<double>();
                List<long> elapsedTicks = new List<long>();
                int tries = _maxTries;

                do
                {
                    lock (_lock)
                    {
                        results = new List<uint>(daqs.Count);
                        foreach (var daq in daqs)
                        {
                            uint v;

                            if (daq.wiseBoard.type == WiseBoard.BoardType.Hard && daqs.IndexOf(daq) > 0)
                                stopwatch.Restart();

                            v = daq.Value;

                            if (daq.wiseBoard.type == WiseBoard.BoardType.Hard && daqs.IndexOf(daq) > 0)
                            {
                                stopwatch.Stop();
                                double millis = stopwatch.Elapsed.TotalMilliseconds;
                                elapsedMillis.Add(millis);
                                elapsedTicks.Add(stopwatch.ElapsedTicks);

                                if (millis > _timeoutMillis)
                                    break;
                            }
                            results.Add(v);
                        }
                    }

                    if (results.Count == daqs.Count)
                    {
                        #region debug
                        string s = $"[{GetHashCode()}] AtomicReader {WiseName}:Values: ";
                        foreach (var res in results)
                            s += $"{res} ";
                        s += "inter daqs (";
                        foreach (WiseDaq daq in daqs)
                            s += $"{daq.WiseName} ";
                        s += ") {elapsedMillis.Count} read times: ";
                        foreach (double m in elapsedMillis)
                            s += $"{m} ";

                        s += ", ticks: ";
                        foreach (long t in elapsedTicks)
                            s += $"{t} ";
                        debugger.WriteLine(Common.Debugger.DebugLevel.DebugDAQs, s);
                        #endregion
                        return results;
                    }
                } while (--tries > 0);

                #region debug
                string err = "AtomicReader " + WiseName + " Failed to read daqs: ";
                foreach (WiseDaq daq in daqs)
                    err += daq.WiseName + " ";
                err += ", within " + _timeoutMillis.ToString() + " milliSeconds, max: ";
                err += elapsedMillis.Max().ToString();
                err += " [ ";
                foreach (double m in elapsedMillis)
                    err += m.ToString() + " ";
                err += "]";

                debugger.WriteLine(Common.Debugger.DebugLevel.DebugDAQs, err);
                #endregion
                Exceptor.Throw<WiseException>("Values", err);
                return new List<uint>();
            }
        }
    }
}
