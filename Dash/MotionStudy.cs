using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.SafeToOperate;

namespace Dash
{
    public class MotionStudy
    {
        TelescopeAxes axis;
        double rate;

        System.Threading.Timer timer;
        private static WiseTele wisetele = WiseTele.Instance;
        private static int samplingIntervalMillis = 100;
        private static int count = 0;
        DateTime start;

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
                return string.Format("{0}, {1}", millis, value.ToString());
            }
        };
        private List<DataPoint> dataPoints;


        private void sampleMotion(object StateObject)
        {
            double value = (axis == TelescopeAxes.axisPrimary) ?
                wisetele.HAEncoder.Angle.Hours :
                wisetele.DecEncoder.Angle.Degrees;

            dataPoints.Add(new DataPoint(DateTime.Now.Subtract(start).TotalMilliseconds, value));
        }

        public MotionStudy(TelescopeAxes axis, double rate)
        {
            this.axis = axis;
            this.rate = rate;
            start = DateTime.Now;
            dataPoints = new List<DataPoint>();
            TimerCallback TimerCallback = new TimerCallback(sampleMotion);
            timer = new Timer(TimerCallback, null, 0, samplingIntervalMillis);
            count++;
        }

        public void Dispose()
        {
            DateTime motorStop = DateTime.Now;
            DateTime axisStop;
            double startValue, motorStopValue, axisStopValue;
            DataPoint[] arr = dataPoints.ToArray();

            startValue = arr[0].value;
            motorStopValue = arr[arr.Length - 1].value;

            timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            string path = string.Format("c:/temp/MotionStudy-{0}_{1}_{2}_{3}.txt",
                axis.ToString().Substring(4),
                WiseTele.RateName(Math.Abs(rate)),
                rate < 0 ? "Decreasing" : "Increasing",
                motorStop.ToString("dd-MMM-yyyy_HH-mm"));
            dataPoints.Add(new DataPoint(motorStop.Subtract(start).TotalMilliseconds, 0.0));
            while (wisetele.AxisIsMoving(axis))
            {
                Thread.Sleep(samplingIntervalMillis);
                sampleMotion(null);
            }
            axisStop = DateTime.Now;
            axisStopValue = arr[arr.Length - 1].value;
            dataPoints.Add(new DataPoint(axisStop.Subtract(start).TotalMilliseconds, 0.0));

            System.IO.StreamWriter sw = new System.IO.StreamWriter(path);
            //sw.WriteLine(string.Format(";"));
            //sw.WriteLine(string.Format("; Start to motor stop: {0} millis", motorStop.Subtract(start).TotalMilliseconds));
            //sw.WriteLine(string.Format("; Start to motor stop: {0} delta", Math.Abs(motorStopValue - startValue)));
            //sw.WriteLine(string.Format("; Motor stop to axis stop: {0} millis", axisStop.Subtract(motorStop).TotalMilliseconds));
            //sw.WriteLine(string.Format("; Motor stop to axis stop: {0} millis", Math.Abs(axisStopValue - motorStopValue)));
            //sw.WriteLine(string.Format(";"));
            foreach (DataPoint point in dataPoints)
            {
                sw.WriteLine(point.ToString());
            }
            sw.Close();
        }
    }
}
