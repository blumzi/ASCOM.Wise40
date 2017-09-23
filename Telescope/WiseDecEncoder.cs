using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40.Telescope
{
    public class WiseDecEncoder : WiseObject, IConnectable, IDisposable, IEncoder
    {
        private uint _daqsValue;

        private WiseEncoder axisEncoder, wormEncoder;

        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        private bool _connected = false;

        public Angle _angle;
        private const double halfPI = Math.PI / 2.0;

        const double decMultiplier = 2 * Math.PI / 600 / 4096;
        const double DecCorrection = 0.35613322;                //20081231 SK: ActualDec-Encoder Dec [rad]

        private Common.Debugger debugger = Debugger.Instance;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;
        private static WiseSite wisesite = WiseSite.Instance;

        private Object _lock = new Object();

        public WiseDecEncoder(string name)
        {
            Name = "DecEncoder";
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            wisesite.init();

            axisEncoder = new WiseEncoder("DecAxis",
                1 << 16,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortA, mask = 0xff },
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortB, mask = 0xff },
                }
            );

            wormEncoder = new WiseEncoder("DecWorm",
                1 << 12,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortCL, mask = 0x0f },
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.FirstPortA,   mask = 0xff },
                }
            );

            Name = name;

            _angle = Simulated ?
                Angle.FromDegrees(90.0, Angle.Type.Dec) - wisesite.Latitude :
                Angle.FromRadians((Value * decMultiplier) + DecCorrection, Angle.Type.Dec);
        }

        public double Declination
        {
            get
            {
                return Degrees;
            }
        }

        public void Connect(bool connected)
        {
            axisEncoder.Connect(connected);
            wormEncoder.Connect(connected);

            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose() {
            axisEncoder.Dispose();
            wormEncoder.Dispose();
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public uint Value
        {
            get
            {
                if (!Simulated)
                {
                    uint worm, axis;

                    lock (_lock)
                    {
                        List<uint> wormValues = wormEncoder.RawValues;
                        List<uint> axisValues = axisEncoder.RawValues;

                        worm = ((wormValues[0] & 0xf) << 8) | (wormValues[1] & 0xff);                        
                        axis = (axisValues[1] >> 4) | (axisValues[0] << 4);

                        _daqsValue = ((axis * 600 + worm) & 0xfff000) - worm;
                    }
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders,
                        "{0}: _daqsValue: {1}, (0x{1:x}), axis: {2}, worm: {3}", 
                        Name, _daqsValue, axis, worm);
                    #endregion
                }
                return _daqsValue;
            }

            set
            {
                if (Simulated)
                    _daqsValue = value;
            }
        }

        public Angle Angle
        {
            get
            {

                if(! Simulated)
                    _angle.Radians = (Value * decMultiplier) + DecCorrection;

                Angle ret = _angle;

                if (FlippedOver90Degrees)
                    ret.Radians = Math.PI - ret.Radians;

                return ret;
            }

            set
            {
                if (Simulated)
                {
                    _angle.Radians = value.Radians;
                    Value = (uint) Math.Round((_angle.Radians - DecCorrection) / decMultiplier);
                }
            }
        }

        public bool FlippedOver90Degrees
        {
            get
            {
                double radians = (Value * decMultiplier) + DecCorrection;
                bool flipped = radians > halfPI;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "FlippedOver90Degrees: radians: {0}, ret: {1}", radians, flipped);
                #endregion
                return flipped;
            }
        }

        public double Degrees
        {
            get
            {
                Angle ret = _angle;

                if (!Simulated)
                {
                    uint v = Value;
                    _angle.Radians = (v * decMultiplier) + DecCorrection;

                    ret = _angle;
                    if (FlippedOver90Degrees)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "WiseDecEncoder: Flipped");
                        #endregion
                        ret.Radians = Math.PI - ret.Radians;
                    }

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders,
                        "[{0}] {1} Degrees - Value: {2}, deg: {3}", this.GetHashCode(), Name, v, ret);
                    #endregion
                }

                return ret.Degrees;
            }

            set
            {
                _angle.Degrees = value;
                if (Simulated)
                {
                    _daqsValue = (uint) ((_angle.Radians - DecCorrection) / decMultiplier);
                }
            }
        }
    }
}
