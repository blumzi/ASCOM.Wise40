using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;
using System.IO;

namespace ASCOM.Wise40 //.Telescope
{
    public class MotionStudy
    {
        private readonly TelescopeAxes axis;
        private readonly double rate;

        private readonly Timer timer;
        private static readonly WiseTele wisetele = WiseTele.Instance;
        private readonly int samplingIntervalMillis;
        private readonly DateTime start;
        private DateTime motorStop;

        private struct DataPoint
        {
            public double millis;
            public double value;

            public DataPoint(double millis, double value)
            {
                this.millis = millis;
                this.value = value;
            }

            public override string ToString()
            {
                return $"{millis}, {value}";
            }
        };
        private readonly List<DataPoint> dataPoints;

        private void SampleMotion(object StateObject)
        {
            double value = (axis == TelescopeAxes.axisPrimary) ?
                wisetele.HAEncoder.Angle.Radians :
                wisetele.DecEncoder.Angle.Radians;

            dataPoints.Add(new DataPoint(DateTime.Now.Subtract(start).TotalMilliseconds, value));
        }

        public MotionStudy(TelescopeAxes axis, double rate, int intervalMillis = 500)
        {
            this.axis = axis;
            this.rate = rate;
            samplingIntervalMillis = intervalMillis;
            start = DateTime.Now;
            dataPoints = new List<DataPoint>();
            TimerCallback TimerCallback = new TimerCallback(SampleMotion);
            timer = new Timer(TimerCallback, null, 0, samplingIntervalMillis);
        }

        public void Dispose()
        {
            motorStop = DateTime.Now;

            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            while (WiseTele.AxisIsMoving(axis))
            {
                Thread.Sleep(samplingIntervalMillis);
                SampleMotion(null);
            }
            //axisStop = DateTime.Now;

            GenerateDataFiles();
        }

        public void GenerateDataFiles()
        {
            string rateName = WiseTele.RateName(Math.Abs(rate));
            string directory = string.Format(Const.topWise40Directory +  "Telescope/MotionStudy/{0}/{1}/{2}",
                motorStop.ToString("yyyy-MMM-dd_HH-mm"),
                axis.ToString().Substring(4), rateName);

            System.IO.Directory.CreateDirectory(directory);

            string radiansFile = System.IO.Path.Combine(directory, "radians.dat");
            string velocitiesFile = System.IO.Path.Combine(directory, "velocities.dat");
            System.IO.StreamWriter radians = new System.IO.StreamWriter(radiansFile);
            System.IO.StreamWriter velocities = new System.IO.StreamWriter(velocitiesFile);
            //sw.WriteLine(string.Format(";"));
            //sw.WriteLine(string.Format("; Start to motor stop: {0} millis", motorStop.Subtract(start).TotalMilliseconds));
            //sw.WriteLine(string.Format("; Start to motor stop: {0} delta", Math.Abs(motorStopValue - startValue)));
            //sw.WriteLine(string.Format("; Motor stop to axis stop: {0} millis", axisStop.Subtract(motorStop).TotalMilliseconds));
            //sw.WriteLine(string.Format("; Motor stop to axis stop: {0} millis", Math.Abs(axisStopValue - motorStopValue)));
            //sw.WriteLine(string.Format(";"));
            for (int i = 0; i < dataPoints.Count; i++)
            {
                DataPoint dp = dataPoints[i];
                radians.WriteLine(dp.ToString());
                if (i > 0)
                {
                    DataPoint dpPrev = dataPoints[i - 1];
                    double v = Math.Abs(dp.value - dpPrev.value) / (dp.millis - dpPrev.millis);
                    velocities.WriteLine($"{dp.millis} {v:F10}");
                }
            }
            radians.Close();
            velocities.Close();

            string gnuplotFile = System.IO.Path.Combine(directory, rateName + ".pl");
            System.IO.StreamWriter gnuPlot = new System.IO.StreamWriter(gnuplotFile);

            gnuPlot.WriteLine("");
            gnuPlot.WriteLine($"plot '{radiansFile}' with lines title \"radians\"");
            gnuPlot.WriteLine($"plot '{velocitiesFile}' with lines title \"velocity\"");
            gnuPlot.Close();
        }
    }
}
