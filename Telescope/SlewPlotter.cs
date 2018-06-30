using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;
using System.Diagnostics;

namespace ASCOM.Wise40 //.Telescope
{
    public class SlewPlotter
    {
        public SlewPlotter(TelescopeAxes axis,  double start, double target)
        {
            Angle.Type angleType = axis == TelescopeAxes.axisPrimary ? Angle.Type.RA : Angle.Type.Dec;
            DateTime utcNow = DateTime.UtcNow;
            string folder = Wise40.Common.Debugger.LogFolder() + 
                string.Format("/slew-started-at-{0:D2}h{1:D2}m{2:D2}s_UTC", utcNow.Hour, utcNow.Minute, utcNow.Second);

            Directory.CreateDirectory(folder);
            string filename = folder + string.Format("/{0}.dat",
                axis == TelescopeAxes.axisPrimary ? "RA" : "DEC"
                );
            try
            {
                _sw = new StreamWriter(filename);
                _sw.AutoFlush = true;
                _sw.WriteLine("#");
                _sw.WriteLine("# Started at: {0}", utcNow.ToShortDateString());
                _sw.WriteLine("# Start position: {0}", start.ToString());
                _sw.WriteLine("# Target position: {0}", target.ToString());
                _sw.WriteLine("# Axis: {0}", axis.ToString());
                _sw.WriteLine("#");
                _sw.WriteLine("# milliseconds position log10(abs(error))");
                _sw.WriteLine("#");
            } catch
            {
                _sw = null;
            }
            _start = start;
            _target = target;
            _stopWatch.Start();
        }

        public void Record(double coord, string comment = null)
        {
            if (_sw == null)
                return;

            string s = string.Format("{0} {1} {2}",
                _stopWatch.ElapsedMilliseconds,
                coord.ToString(),
                Math.Log10(Math.Abs(_target - coord)));
            if (comment != null)
                s += "  # " + comment;
            _sw.WriteLine(s);
        }

        private StreamWriter _sw = null;
        Stopwatch _stopWatch = new Stopwatch();
        private double _start, _target;
    }
}
