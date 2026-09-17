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
        private double _daqsValue;
        //private const uint _realValueAtFiducialMark = 1432779; // Arie - 02 July 2016

        private readonly WiseEncoder axisEncoder, wormEncoder;
        private readonly WiseDecEncoder _decEncoder;

        public Angle _angle;

        private const double HaMultiplier = Const.twoPI / 720 / 4096;
        private const double HaCorrection = -3.063571542;                   // 20081231: Shai Kaspi
        //private const uint _simulatedValueAtFiducialMark = _realValueAtFiducialMark;

        private readonly Debugger debugger = Debugger.Instance;
        private readonly Hardware.Hardware hw = Hardware.Hardware.Instance;
        private static readonly WiseSite wisesite = WiseSite.Instance;
        private int prev_worm = int.MinValue, prev_axis = int.MinValue;

        private readonly Object _lock = new object();
        private bool disposed = false;

        public WiseHAEncoder(string name, WiseDecEncoder decEncoder)
        {
            WiseName = "HAEncoder";
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

            WiseName = name;
            //
            // No Renishaw encoder is built here.  There is one per BiSS module and
            //  WiseTele owns both - see WiseTele.renishawHaEncoder.  Building a
            //  second one on module 0 is now refused by RenishawEncoder itself.
            //

            if (Simulated)
                _angle = new Angle("00h00m00.0s");
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public double EncoderValue
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

                        //worm = (wormValues[0] << 8) | wormValues[1];
                        //axis = (axisValues[1] >> 4) | (axisValues[0] << 4);
                        worm = ((wormValues[0] & 0x0f) * 0x100) + wormValues[1];
                        axis = ((axisValues[1] & 0xff) / 0x10) + (axisValues[0] * 0x10);

                        _daqsValue = (((axis * 720) - worm) & 0xfff000) + worm;
                    }
                    #region debug
                    string dbg = $"{WiseName}: value: {_daqsValue}, axis: {axis} (0x{axis:x}), worm: {worm} (0x{worm:x})";
                    if (prev_worm != int.MinValue)
                    {
                        dbg += string.Format(" prev_axis: {0} (0x{0:x}), prev_worm: {1} (0x{1:x})",
                            prev_axis, prev_worm);
                        dbg += string.Format(" xor_axis: {0}, xor_worm: {1}",
                            Convert.ToString(axis ^ prev_axis, 2).PadLeft(16, '0'),
                            Convert.ToString(worm ^ prev_worm, 2).PadLeft(12, '0'));
                    }

                    //dbg += $"Renishaw: {RenishawHaEncoder.Position}";
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, dbg);
                    prev_worm = worm;
                    prev_axis = axis;
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
                return Simulated ? 0 : axisEncoder.Value;
            }
        }

        public uint WormValue
        {
            get
            {
                return Simulated ? 0 : wormEncoder.Value;
            }
        }

        public double Radians
        {
            get
            {
                //
                // Everything else here - Angle, Degrees, Hours, RightAscension -
                //  goes through this, so this is the only place the choice of
                //  encoder has to be made.  Same as WiseDecEncoder.Radians.
                //
                return (WiseTele.Instance.EncodersInUse == WiseTele.EncodersInUseEnum.Old) ?
                    OldRadians :
                    WiseTele.renishawHaEncoder.Radians;
            }
        }

        //
        // This encoder's OWN reading, whichever encoder is currently in use.
        //
        // Needed so the digest can report a genuine Renishaw-minus-old difference.
        //  While the old encoders were driving, deltaHA compared the Renishaw against
        //  "the hour angle", which was the old encoder - a real cross-check between two
        //  independent sensors.  Switching to New silently turned that into the
        //  Renishaw compared with itself, so it read zero by construction and the
        //  cross-check was lost exactly when it started to matter.
        //
        public double OldRadians
        {
            get
            {
                return (EncoderValue * HaMultiplier) + HaCorrection;
            }
        }

        public double OldHourAngle
        {
            get
            {
                //
                // Folded to +/-12h the same way RenishawHaEncoder.HourAngle folds it
                //  (via astroUtils.ConditionHA), so the two can be differenced.
                //
                // This matters: OldRadians runs from HaCorrection (-3.0636 rad) to
                //  HaCorrection + 2*PI, i.e. -11.70h to +12.30h.  Unfolded, a point
                //  just past +12h would sit at +12.30 while the Renishaw reported
                //  -11.70, and the difference would come out as 24 hours rather than
                //  the few arcseconds it really is.
                //
                double hours = Angle.Rad2Hours(OldRadians);

                if (hours > 12.0)
                    hours -= 24.0;
                else if (hours < -12.0)
                    hours += 24.0;

                return hours;
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
                    EncoderValue = (uint)Math.Round((_angle.Radians - HaCorrection) / HaMultiplier);
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "{0}: {1} + {2} = {3}", WiseName, before, delta, after);
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
                Angle ret = wisesite.LocalSiderealTime - Angle.FromHours(Hours, Angle.AngleType.RA);
                #region Delphi
                //   if dec_Corrected > pi / 2.0 then  // Has the telescope gone North of dec=90deg?
                //      begin // Adjust the dec and HA values accordingly.
                //          dec_Corrected:= pi - dec_Corrected;
                //          HA_Corrected:= HA_Corrected + pi; // Add 12hr to HA
                //          if HA_Corrected > 2 * pi then
                //                HA_Corrected := HA_Corrected - 2 * pi
                //      end;
                #endregion
                if (_decEncoder.Over90Degrees)
                {
                    ret.Radians += Const.onePI;     // Add 12 hours
                }
                if (ret.Radians < 0)
                    ret.Radians += Const.twoPI;
                if (ret.Radians > Const.twoPI)
                    ret.Radians -= Const.twoPI;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "{0}: RightAscension: {1}", WiseName, ret);
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
    }
}