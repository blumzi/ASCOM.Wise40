﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class SunSensor : Sensor
    {
        private double _max;

        public SunSensor(WiseSafeToOperate instance) :
            base("Sun",
                SensorAttribute.Immediate |
                SensorAttribute.AlwaysEnabled |
                SensorAttribute.CanBeBypassed, instance) { }

        public override void readSensorProfile()
        {
            MaxAsString = wisesafetooperate._profile.GetValue(Const.wiseSafeToOperateDriverID, Name, "Max", 0.0.ToString());
        }

        public override void writeSensorProfile()
        {
            wisesafetooperate._profile.WriteValue(Const.wiseSafeToOperateDriverID, Name, MaxAsString, "Max");
        }

        public override Reading getReading()
        {
            Reading r = new Reading
            {
                stale = false,
                safe = wisesafetooperate.SunElevation <= _max
            };
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugSafety, "{0}: getIsSafe: {1}", Name, r.safe);
            #endregion
            return r;
        }

        public override string reason()
        {
            double currentElevation = wisesafetooperate.SunElevation;

            if (currentElevation <= _max)
                return string.Empty;

            return string.Format("The Sun elevation ({0:f1}deg) is higher than {1:f1}deg.",
                currentElevation, _max);
        }

        public override string MaxAsString
        {
            set
            {
                _max = Convert.ToDouble(value);
            }

            get
            {
                return _max.ToString();
            }
        }
    }
}