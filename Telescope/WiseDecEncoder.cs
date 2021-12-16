using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40
{
    public class WiseDecEncoder : WiseObject, IConnectable, IDisposable, IEncoder
    {
        private double _daqsValue;

        private readonly WiseEncoder axisEncoder, wormEncoder;
        private readonly RenishawDecEncoder renishawDecEncoder = new RenishawDecEncoder();

        private bool _connected = false;

        public Angle _angle = new Angle(0.0, Angle.AngleType.Dec);

        private const double DecMultiplier = Const.twoPI / 600 / 4096;
        private const double DecCorrection = 0.35613322;                //20081231 SK: ActualDec-Encoder Dec [rad]

        private readonly Debugger debugger = Debugger.Instance;
        private readonly Hardware.Hardware hw = Hardware.Hardware.Instance;
        private int prev_worm, prev_axis;

        private readonly Object _lock = new Object();
        private bool disposed = false;

        public WiseDecEncoder(string name)
        {
            WiseName = "DecEncoder";

            axisEncoder = new WiseEncoder("DecAxis",
                1 << 16,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortA, mask = 0xff }, // [0]
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortB, mask = 0xff }, // [1]
                }
            );

            wormEncoder = new WiseEncoder("DecWorm",
                1 << 12,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.SecondPortCL, mask = 0x0f }, // [0]
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.FirstPortA,   mask = 0xff }, // [1]
                }
            );

            WiseName = name;

            Angle = Simulated ?
                Angle.DecFromDegrees(85) :
                Angle.DecFromRadians(Radians);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                axisEncoder.Dispose();
                wormEncoder.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public double EncoderValue
        {
            get
            {
                if (!Simulated)
                {
                    #region Delphi
                    ////DecWormDAC is 12bit: lower nibble of boardAport0+ boardAport1
                    //
                    // procedure GetCoord(var HA, RA, HA_Enc, Dec, Dec_Enc :extended [double];
                    // var HA_last, RA_last, Dec_last : extended [double];
                    // var BAP0, BAP1, BAP2, BAP3, BBP0, BBP1, BBP2, BBP3: integer [int];
                    // var HAWormEnc, HAAxisEnc, DecWormEnc, DecAxisEnc: longint [int]);
                    //
                    // DecWormEnc:= (BBP1 AND $000F)*$100 + BBP0;
                    // DecAxisEnc:= ((BBP2 AND $00FF) div $10) +($10 * BBP3); //missing 4140 div 16
                    // DecEnc:= ((DecAxisEnc * 600 + DecWormEnc) AND $FFF000) -DecWormEnc; //MASK lower 12 bits of DecAxisDAC
                    // Dec_Enc:= DecEnc * 2.5566346464760687161968126491532e-6;  //2*pi/600/4096
                    // Dec:= Dec_Enc + DecCorrection;
                    // if (Dec > pi) then
                    //   Dec := Dec - pi2;
                    #endregion

                    int worm, axis;

                    lock (_lock)
                    {
                        List<int> wormValues = wormEncoder.RawValuesInt;
                        List<int> axisValues = axisEncoder.RawValuesInt;

                        worm = ((wormValues[0] & 0x0f) * 0x100) + (wormValues[1] & 0xff);
                        axis = ((axisValues[1] & 0xff) / 0x10) + (axisValues[0] * 0x10);

                        _daqsValue = (((axis * 600) + worm) & 0xfff000) - worm;
                    }
                    #region debug
                    string dbg = $"{WiseName}: value: {_daqsValue}, axis: {axis} (0x{axis:x}), worm: {worm} (0x{worm:x})";
                    if (prev_worm != int.MinValue)
                    {
                        dbg += $" prev_axis: {prev_axis} (0x{prev_axis:x}), prev_worm: {prev_worm} (0x{prev_worm:x})";
                        dbg += string.Format(" change_axis: {0}, change_worm: {1}",
                            Convert.ToString(axis ^ prev_axis, 2).PadLeft(16, '0'),
                            Convert.ToString(worm ^ prev_worm).PadLeft(12, '0'));
                    }
                    //dbg += $"RenishawDec: {RenishawDecEncoder.Position}";
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, dbg);
                    prev_axis = axis;
                    prev_worm = worm;
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
                if (!Simulated)
                {
                    double radians = Radians;
                    if (_angle == null)
                        _angle = Angle.DecFromRadians(radians);
                    else
                        _angle.Radians = radians;
                }

                Angle ret = _angle;

                //if (DecOver90Degrees)
                //    ret.Radians = halfPI - (ret.Radians - halfPI);

                return ret;
            }

            set
            {
                if (Simulated)
                {
                    if (_angle == null)
                        _angle = new Angle();
                    _angle.Radians = value.Radians;
                    EncoderValue = (uint) Math.Round((_angle.Radians - DecCorrection) / DecMultiplier);
                }
            }
        }

        public bool Over90Degrees
        {
            get
            {
                return Radians > Const.halfPI;
            }
        }

        public double Radians
        {
            get
            {
                return (WiseTele.Instance.EncodersInUse == WiseTele.EncodersInUseEnum.Old) ?
                    (EncoderValue * DecMultiplier) + DecCorrection :
                    renishawDecEncoder.Radians;
            }
        }

        public double Degrees
        {
            get
            {
                double rad = Radians;

                while (rad > Const.twoPI)
                    rad -= Const.twoPI;
                while (rad < Const.twoPI)
                    rad += Const.twoPI;

                if (Over90Degrees)
                    rad = Const.onePI - rad;

                return Angle.Rad2Deg(rad);
            }

            set
            {
                _angle.Degrees = value;
                if (Simulated)
                {
                    _daqsValue = (_angle.Radians - DecCorrection) / DecMultiplier;
                }
            }
        }
    }
}
