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
        TelescopeAxes axis;
        double rate;

        System.Threading.Timer timer;
        private static WiseTele wisetele = WiseTele.Instance;
        private int samplingIntervalMillis;
        DateTime start, motorStop, axisStop;

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
        private List<DataPoint> dataPoints;
        private DataPoint motorStopPoint, axisStopPoint;

        private void sampleMotion(object StateObject)
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
            TimerCallback TimerCallback = new TimerCallback(sampleMotion);
            timer = new Timer(TimerCallback, null, 0, samplingIntervalMillis);
        }

        public void Dispose()
        {
            motorStop = DateTime.Now;
            double startValue, motorStopValue, axisStopValue;
            DataPoint[] arr = dataPoints.ToArray();

            startValue = arr[0].value;
            motorStopValue = arr[arr.Length - 1].value;

            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            motorStopPoint = new DataPoint(motorStop.Subtract(start).TotalMilliseconds, motorStopValue);
            while (WiseTele.AxisIsMoving(axis))
            {
                Thread.Sleep(samplingIntervalMillis);
                sampleMotion(null);
            }
            axisStop = DateTime.Now;
            axisStopValue = arr[arr.Length - 1].value;
            axisStopPoint = new DataPoint(axisStop.Subtract(start).TotalMilliseconds, axisStopValue);

            generateDataFiles();
        }

        public void generateDataFiles()
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
