using System;
using System.Collections.Generic;
using MccDaq;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;

namespace ASCOM.Wise40 //.Telescope
{
    public class WiseHAEncoder : WiseObject, IConnectable, IDisposable, IEncoder
    {
        private bool _connected = false;
        private /*uint*/ double _daqsValue;
        private const uint _realValueAtFiducialMark = 1432779; // Arie - 02 July 2016
        
        private WiseEncoder axisEncoder, wormEncoder;
        private WiseDecEncoder _decEncoder;

        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        public Angle _angle;
        private const double twoPI = Math.PI * 2.0;

        const double HaMultiplier = twoPI / 720 / 4096;
        const double HaCorrection = -3.063571542;                   // 20081231: Shai Kaspi
        //const double HaCorrection = -6.899777777777778;  // 20160702: Arie
        const uint _simulatedValueAtFiducialMark = _realValueAtFiducialMark;

        private Common.Debugger debugger = Common.Debugger.Instance;
        private Hardware.Hardware hw = Hardware.Hardware.Instance;
        private static WiseSite wisesite = WiseSite.Instance;

        private Object _lock = new object();

        public WiseHAEncoder(string name, WiseDecEncoder decEncoder)
        {
            Name = "HAEncoder";
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            wisesite.init();
            _decEncoder = decEncoder;

            axisEncoder = new WiseEncoder("HAAxis",
                1 << 16,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.FourthPortA, mask = 0xff },
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.FourthPortB, mask = 0xff },
                }
            );

            wormEncoder = new WiseEncoder("HAWorm",
                1 << 12,
                new List<WiseEncSpec>() {
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.FourthPortCL, mask = 0x0f },
                    new WiseEncSpec() { brd = hw.teleboard, port = DigitalPortType.ThirdPortA,   mask = 0xff },
                }
            );

            Name = name;

            if (Simulated)
                _angle = new Angle("00h00m00.0s");
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public /*uint*/ double Value
        {
            get
            {
                #region Delphi
                // procedure GetCoord(var HA, RA, HA_Enc, Dec, Dec_Enc :extended [double];
                // var HA_last, RA_last, Dec_last : extended [double];
                // var BAP0, BAP1, BAP2, BAP3, BBP0, BBP1, BBP2, BBP3: integer [int];
                // var HAWormEnc, HAAxisEnc, DecWormEnc, DecAxisEnc: longint [int]);
                //
                // HAWormEnc:= (BAP1 AND $000F)*$100 + BAP0;
                // HAAxisEnc:= ((BAP2 AND $00FF) div $10) +($10 * BAP3);
                // HAEnc:= ((HAAxisEnc * 720 - HAWormEnc) AND $FFF000) +HAWormEnc; //MASK lower 12 bits of HAEnc to be determined by HAWormEnc
                // HA_Enc:= HAEnc * 2.1305288720633905968306772076277e-6;  //2*pi/720/4096
                // HA:= HA_Enc + HACorrection;
                #endregion
                int worm, axis;

                if (! Simulated)
                {
                    lock (_lock)
                    {
                        List<int> wormValues = wormEncoder.RawValuesInt;
                        List<int> axisValues = axisEncoder.RawValuesInt;

                        worm = (wormValues[0] << 8) | wormValues[1];
                        axis = (axisValues[1] >> 4) | (axisValues[0] << 4);

                        _daqsValue = ((axis * 720 - worm) & 0xfff000) + worm;
                    }
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0}: value: {1}, axis: {2}, worm: {3}",
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

        public uint AxisValue
        {
            get
            {
                if (Simulated)
                    return 0;
                else
                {
                    return axisEncoder.Value;
                }
            }
        }

        public uint WormValue
        {
            get
            {
                if (Simulated)
                    return 0;
                else
                {
                    return wormEncoder.Value;
                }
            }
        }

        public Angle Angle
        {
            get
            {
                if (!Simulated)
                {
                    double radians = (Value * HaMultiplier) + HaCorrection;
                    if (_angle == null)
                        _angle = Angle.FromRadians(radians);
                    else
                        _angle.Radians = radians;
                }

                return _angle;
            }

            set
            {
                if (Simulated)
                {
                    _angle = Angle.FromHours(value.Hours);
                    Value = (uint)Math.Round((_angle.Radians - HaCorrection) / HaMultiplier);
                }
            }
        }

        public double Degrees
        {
            get
            {
                return Angle.Degrees;
            }

            set
            {
                Angle before = _angle;
                _angle.Degrees = value;
                Angle after = _angle;
                Angle delta = after - before;

                if (Simulated)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "[{0}] {1}: {2} + {3} = {4}",
                        this.GetHashCode(), Name, before, delta, after);
                    #endregion
                    _daqsValue = /*(uint)*/((_angle.Radians + HaCorrection) / HaMultiplier);
                }
            }
        }

        private double Hours
        {
            get
            {
                return Angle.Hours;
            }
        }

        /// <summary>
        /// Right Ascension in Hours
        /// </summary>
        public Angle RightAscension
        {
            get
            {
                Angle ret = wisesite.LocalSiderealTime - Angle.FromHours(Hours, Angle.Type.RA);
                #region Delphi
                //   if dec_Corrected > pi / 2.0 then  // Has the telescope gone North of dec=90deg?
                //      begin // Adjust the dec and HA values accordingly.
                //          dec_Corrected:= pi - dec_Corrected;
                //          HA_Corrected:= HA_Corrected + pi; // Add 12hr to HA
                //          if HA_Corrected > 2 * pi then
                //                HA_Corrected := HA_Corrected - 2 * pi
                //      end;
                #endregion
                if (_decEncoder.FlippedOver90Degrees)
                {
                    ret.Radians += Math.PI;     // Add 12 hours
                }
                if (ret.Radians < 0)
                    ret.Radians += twoPI;
                if (ret.Radians > twoPI)
                    ret.Radians -= twoPI;
                #region debug
                //debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "[{0}] RightAscension: {1}", this.GetHashCode(), ret);
                #endregion
                return ret;
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

        public void Dispose()
        {
            axisEncoder.Dispose();
            wormEncoder.Dispose();
        }        
    }
}