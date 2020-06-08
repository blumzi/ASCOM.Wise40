﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Common;
//using ASCOM.Wise40.Telescope;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40.Hardware
{
    /// <summary>
    /// <para>
    /// A WiseVirtualMotor implements the three moving rates available for each axis at the Wise40 telescope
    ///  by turning on (and off) the relevant hardware pins, as follows:
    ///   - rateSlew:  motorPin + slewPin
    ///   - rateSet:   motorPin
    ///   - rateGuide: guidePin
    /// </para>
    /// <para>When simulated the WiseVirtualMotor will also increase/decrease the value of the relevant (simulated) encoder.</para>
    /// </summary>
    public class WiseVirtualMotor : WiseObject, IConnectable, IDisposable //, ISimulated
    {
        private readonly WisePin motorPin, guideMotorPin, slewPin;
        private readonly List<object> encoders;
        private readonly System.Threading.Timer simulationTimer;
        public double currentRate;
        private readonly int simulationTimerFrequency;
        private int timer_counts;
        private DateTime prevTick;
        private readonly TelescopeAxes _axis, _otherAxis;
        private readonly Const.AxisDirection _direction;   // There are separate WiseMotor instances for North, South, East, West
        private bool _connected = false;
        private readonly Debugger debugger = Debugger.Instance;
        private readonly WiseTele wisetele = WiseTele.Instance;
        private readonly WiseSite wisesite = WiseSite.Instance;
        private readonly List<WisePin> allPins;

        public WiseVirtualMotor(
            string name,
            WisePin motorPin,
            WisePin guideMotorPin,
            WisePin slewPin,
            TelescopeAxes axis,
            Const.AxisDirection direction,
            List<object> encoders = null)
        {
            this.WiseName = name;

            this.motorPin = motorPin;
            this.guideMotorPin = guideMotorPin;
            this.slewPin = slewPin;
            this.allPins = new List<WisePin> { motorPin, slewPin, guideMotorPin };

            this.encoders = encoders;
            this._axis = axis;
            this._otherAxis = (_axis == TelescopeAxes.axisPrimary) ?
                TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;
            this._direction = direction;

            if (Simulated && (encoders == null || encoders.Count == 0))
                Exceptor.Throw<WiseException>("WiseVirtualMotor", $"{WiseName}: A simulated WiseVirtualMotor must have at least one encoder reference");

            if (Simulated)
            {
                simulationTimerFrequency = 30; // 15;
                TimerCallback TimerCallback = new TimerCallback(BumpEncoders);
                simulationTimer = new System.Threading.Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public string ActiveMortorPins()
        {
            if (wisetele.allMotors == null)
                return "";

            List<string> active = new List<string>();

            foreach (WiseVirtualMotor m in wisetele.allMotors)
            {
                foreach (WisePin pin in m.allPins)
                {
                    if (pin == null)
                        continue;

                    string shortName = pin.WiseName.Remove(pin.WiseName.IndexOf('@'));

                    if (pin.isOn && !active.Contains(shortName))
                        active.Add(shortName);
                }
            }

            return String.Join(", ", active);
        }

        public void SetOn(double rate)
        {
            rate = Math.Abs(rate);
            string activeBefore = ActiveMortorPins();

            if (motorPin?.isOn == true)
                motorPin.SetOff();
            if (guideMotorPin?.isOn == true)
                guideMotorPin.SetOff();
            if (slewPin?.isOn == true)
            {
                bool inUseByOtherAxis = false;
                foreach (WiseVirtualMotor m in wisetele.axisMotors[_otherAxis])
                {
                    if (m.currentRate == Const.rateSlew)
                    {
                        inUseByOtherAxis = true;
                        break;
                    }
                }

                if (!inUseByOtherAxis)
                    slewPin.SetOff();
            }

            currentRate = rate;
            if (rate == Const.rateSlew)
            {
                slewPin.SetOn();
                motorPin.SetOn();
            }
            else if (rate == Const.rateSet)
            {
                motorPin.SetOn();
            }
            else if (rate == Const.rateGuide)
            {
                guideMotorPin.SetOn();
            }
            else if (rate == Const.rateTrack)
            {
                motorPin.SetOn();
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}.SetOn at {1}: pins before: {2}, pins after: {3}",
                WiseName, WiseTele.RateName(rate), activeBefore, ActiveMortorPins());

            if (Simulated)
            {
                timer_counts = 0;
                prevTick = DateTime.Now;
                simulationTimer.Change(0, 1000 / simulationTimerFrequency);
            }
        }

        public void SetOff()
        {
            if (Simulated)
                simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);

            string activeBefore = ActiveMortorPins();
            if (guideMotorPin?.isOn == true)
                guideMotorPin.SetOff();

            if (motorPin?.isOn == true)
                motorPin.SetOff();

            currentRate = Const.rateStopped;
            if (slewPin?.isOn == true)
            {
                bool inUseByOtherAxis = false;
                foreach (WiseVirtualMotor m in wisetele.axisMotors[_otherAxis])
                {
                    if (m.currentRate == Const.rateSlew)
                    {
                        inUseByOtherAxis = true;
                        break;
                    }
                }
                if (!inUseByOtherAxis)
                    slewPin.SetOff();
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "{0}.SetOff: pins before: {1}, pins after: {2}",
                WiseName, activeBefore, ActiveMortorPins());
        }

        public bool IsOn
        {
            get
            {
                if (motorPin?.isOn == true)
                    return true;
                if (guideMotorPin?.isOn == true)
                    return true;
                return false;
            }
        }

        public bool IsOff
        {
            get
            {
                return !IsOn;
            }
        }

        /// <summary>
        /// <para>
        /// This is used only by simulated motors.
        /// It is called at a timer interval (simulationTimerFrequency) and increases/decreases
        ///  the attached encoder(s) according to the motor's currentRate.
        /// </para>
        /// <para>The TrackMotor is a special case.</para>
        /// </summary>
        /// <param name="StateObject"></param>
        private void BumpEncoders(object StateObject)
        {
            if (!Simulated)
                return;

            bool primary = _axis == TelescopeAxes.axisPrimary;
            Angle delta;

            //
            // Calculate the delta to be added/subtracted from the attached encoder(s)
            //
            if (WiseName == "TrackMotor")
            {
                //
                // To better simulate the tracking-motor, we use the actual LocalSiderealTime
                //  that passed since the last time we read it.  This neutralizes
                //  inaccuracies of the timer intervals.
                //
                // The wisetele._lastTrackingLST variable gets initialized each time
                //  Tracking is enabled.
                //
                double lstHoursNow = wisesite.LocalSiderealTime.Hours;
                delta = Angle.FromHours(lstHoursNow - wisetele._lastTrackingLST);
                wisetele._lastTrackingLST = lstHoursNow;
            }
            else
            {
                double degrees = currentRate / simulationTimerFrequency;
                double hours = Angle.Deg2Hours(currentRate) / simulationTimerFrequency;
                delta = primary ? Angle.FromHours(hours, Angle.AngleType.HA) : Angle.FromDegrees(degrees, Angle.AngleType.Dec);
            }

            foreach (IEncoder encoder in encoders)
            {
                Angle before, after;
                string op;

                lock (primary ? wisetele._primaryEncoderLock : wisetele._secondaryEncoderLock)
                {
                    before = primary ?
                        Angle.FromHours(wisetele.HourAngle, Angle.AngleType.HA) :
                        Angle.FromDegrees(wisetele.Declination, Angle.AngleType.Dec);

                    if (_direction == Const.AxisDirection.Increasing)
                    {
                        if (primary)
                        {
                            op = "-";
                            encoder.Angle -= delta;
                        }
                        else
                        {
                            op = "+";
                            encoder.Angle += delta;
                        }
                    }
                    else
                    {
                        if (primary)
                        {
                            op = "+";
                            encoder.Angle += delta;
                        }
                        else
                        {
                            op = "-";
                            encoder.Angle -= delta;
                        }
                    }

                    after = primary ?
                        Angle.FromHours(wisetele.HourAngle, Angle.AngleType.HA) :
                        Angle.FromDegrees(wisetele.Declination, Angle.AngleType.Dec);
                }

                debugger.WriteLine(Debugger.DebugLevel.DebugMotors,
                        "bumpEncoders: {0}: {1}: {13} {2}:  {3} {4} {5} = {6} ({7} {8} {9} = {10}) (#{11}, {12} ms)",
                        WiseName,                   // 0
                        encoder.WiseName,           // 1
                        primary ? "ha" : "dec", // 2
                        before,                 // 3
                        op,                     // 4
                        delta,                  // 5
                        after,                  // 6
                        before.Degrees,         // 7
                        op,                     // 8
                        delta.Degrees,          // 9
                        after.Degrees,          // 10
                        timer_counts++,         // 11
                        DateTime.Now.Subtract(prevTick).Milliseconds,   // 12
                        WiseTele.RateName(currentRate));                // 13
                prevTick = DateTime.Now;
            }
        }

        public void Connect(bool connected)
        {
            foreach (WisePin pin in new List<WisePin>() { motorPin, guideMotorPin })
            {
                pin?.Connect(connected);
            }

            if (connected && slewPin?.Connected == false)
                slewPin.Connect(connected);
            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose()
        {
            foreach (WisePin pin in new List<WisePin>() { motorPin, guideMotorPin })
            {
                pin?.Dispose();
            }
        }

        public override string ToString()
        {
            return WiseName;
        }

        public double RateFromPins
        {
            get
            {
                if (motorPin.isOn)
                    return slewPin.isOn ? Const.rateSlew : Const.rateSet;
                else if (guideMotorPin.isOn)
                    return Const.rateGuide;
                else
                    return Const.rateStopped;
            }
        }
    }
}