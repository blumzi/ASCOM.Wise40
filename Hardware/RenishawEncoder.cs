﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCIe1711_NET;
using ASCOM.Wise40.Common;
using ASCOM.Astrometry.AstroUtils;

namespace ASCOM.Wise40.Hardware
{
    public struct Solved
    {
        public int enc;
        public double coord;
    }

    /// <summary>
    /// <para>
    /// Hardware:
    ///  - one ADDI APCIe-1711 card
    ///  - two Renishaw Resolute encoders, model RL32BBT001B99A (BISS 32 bit)
    /// Functional modules 0 and 1 on the APCIe-1711 are configured for BISS protocol
    /// </para>
    /// <para>
    /// One encoder is slave #0 of the functional module #0.
    /// The other encoder is slave #0 of the functional module #1.
    /// </para>
    /// </summary>
    public class RenishawEncoder
    {
        private enum ChannelMode { BISS = 0, SSI = 1 };
        private const byte CRCPolynom = ((1 << 6) | (1 << 1) | (1 << 0));

        private const double haConstant = 0.604916 / 19237927;
        private const double decConstant = 36.007347 / 16047243;

        public static AstroUtils astroUtils = new AstroUtils();

        private enum BissMode { B = 0, C = 1 };
        private static readonly PCIe1711 Board = PCIe1711.OpenBoard(0);
        public enum Module { Ha = 0, Dec = 1 };
        private readonly byte _moduleNumber;
        private readonly Module _module;

        private const int jitterBits = 6;       // number of LSBs to discard

        private static readonly Debugger debugger = Debugger.Instance;
        public static readonly Exceptor Exceptor = new Exceptor(Common.Debugger.DebugLevel.DebugEncoders);

        private int prevPosition = Int32.MinValue;


        public RenishawEncoder(Module module)
        {
            int ret;

            byte[] channels = { 0 };
            byte[] options = { 0 };
            byte[] polynoms = { CRCPolynom };
            byte[] inverts = { 1 };
            byte[] dataLengths = { 35 };

            _moduleNumber = (byte)module;
            _module = module;

            ret = Board.BissMasterInitSingleCycle(moduleNbr: _moduleNumber,
                sensorDataFreqDivisor: 17,
                registerDataFreqDivisor: 0,
                channel0BISSSSIMode: (byte)ChannelMode.BISS,
                channel0BISSMode: (byte)BissMode.B,
                channel1BISSSSIMode: 0,
                channel1BISSMode: 0,
                nbrOfSlave: 1,
                channel: channels,
                dataLength: dataLengths,
                option: options,
                CRCPolynom: polynoms,
                CRCInvert: inverts);
            System.Threading.Thread.Sleep(500);

            if (ret != 0)
                throw new Exception($"BissMasterInitSingleCycle(moduleNumber: {_moduleNumber}) returned {ret}");
        }

        public int Position
        {
            get
            {
                string op = $"Renishaw({_module}).Position";
                int tries, maxTries = 5;
                UInt32 high = 0, low = 0;

                for (tries = 0; tries < maxTries; tries++)
                {
                    int ret = Board.BissMasterSingleCycleDataRead(
                        moduleNbr: _moduleNumber,
                        slaveIndex: 0,
                        out low,
                        out high);

                    if (ret == 0)
                        break;
                    else
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: BissMasterSingleCycleDataRead returned {ret}");
                        #endregion
                        System.Threading.Thread.Sleep(50);
                    }
                }
                if (tries >= maxTries)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: Cannot read Renishaw encoder {_module} after {tries} tries, returning 0");
                    #endregion
                    return 0;
                }

                UInt64 reading = ((UInt64)high << 32) | (UInt64)low;
                bool warning = (reading & (1 << 0)) == 0;
                bool error = (reading & (1 << 1)) == 0;
                int position = (int) ((reading >> 2) & 0xffffffff);

                if (warning)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: encoder or scale need cleaning");
                    #endregion
                }

                if (error)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: encoder read error, returning 0");
                    #endregion
                    return 0;
                }
                else
                {
                    if (tries > 0)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: needed {tries} tries to read encoder");
                        #endregion
                    }

                    position >>= jitterBits;
                    if (prevPosition != int.MinValue && (Math.Abs(prevPosition - position) == 1))
                    {
                        position = prevPosition;
                    }
                    prevPosition = position;
                    return position;
                }
            }
        }

        public double HourAngle
        {
            get
            {
                if (_module != Module.Ha)
                    Exceptor.Throw<InvalidOperationException>("HourAngle", $"Cannot get HourAngle from a {_module} type encoder");

                return Position * haConstant;
            }
        }

        public double Declination
        {
            get
            {
                if (_module != Module.Dec)
                    Exceptor.Throw<InvalidOperationException>("Declination", $"Cannot get Declination from a {_module} type encoder");

                return Position * decConstant;
            }
        }


        ~RenishawEncoder()
        {
            Board.BissMasterReleaseSingleCycle(moduleNbr: _moduleNumber);
        }
    }

    public class RenishawHAEncoder: RenishawEncoder
    {
        public RenishawHAEncoder() : base(Module.Ha)
        {
        }

        /*
         * 17604386 4.16946625561244
         * 18388977 2.44723569990817
         * 18668013 1.8355881253023
         * 19059563 0.976598420563999
         * 19113632 0.858408678139263
         * 19351104 0.336369248274927
         * 19520220 -0.035254653920525
         * 19620534 -0.255659491837299
         * 20144316 -1.40467935527617
         * 20180887 -1.48450384379839
         * 20363489 -1.88544873093344
         * 20388470 -1.94016159890154
         * 20548281 -2.29005671418188
         * 21299784 -3.938792458868
         */

        const double ENCmax = 21299784, HAmax = -3.938792458868,   RADmax = HAmax * 2.0 * Math.PI / 24.0;
        const double ENCmin = 17604386, HAmin =  4.16946625561244, RADmin = HAmin * 2.0 * Math.PI / 24.0;

        const double rad_per_tick = (RADmin - RADmax) / (ENCmax - ENCmin);

        public double Radians
        {
            get
            {

                double rad = RADmax + ((ENCmax - Position) * rad_per_tick);

                while (rad > 2 * Math.PI)
                    rad -= 2 * Math.PI;
                while (rad < -2 * Math.PI)
                    rad += 2 * Math.PI;

                return rad;
            }
        }

        public new double HourAngle
        {
            get
            {
                double rad = Radians;

                while (rad > 2 * Math.PI)
                    rad -= 2 * Math.PI;
                while (rad < -2 * Math.PI)
                    rad += 2 * Math.PI;

                return astroUtils.ConditionHA(Angle.Rad2Hours(rad));
            }
        }
    }


    public class RenishawDecEncoder: RenishawEncoder
    {
        public RenishawDecEncoder() : base(Module.Dec)
        {
        }

        /*
         * 11476719 -20.0711833033614
         * 12297287 -9.99880359441257
         * 12297813 -9.99912507505749
         * 13107123 -0.0601692329452953
         * 13107128 -0.0605717901098018
         * 13112607 -0.00278698826495569
         * 15552267 29.9508174662959
         * 15552372 29.9386467947406
         * 15552436 29.9303564166369
         * 16367685 39.9361051036646
         * 16845348 45.8007149196587
         * 17182764 49.9335460587729
         * 18001277 59.9869785909469
         * 18816442 69.9793498080761
         */

        const double ENCmax = 18816442, ENCmin = 11476719;
        const double DECmax = 69.9793498080761, DECmin = -20.0711833033614;
        const double deg_per_tick = (DECmax + -DECmin) / (ENCmax - ENCmin);

        const double RADmax = (DECmax * Math.PI) / 180.0, RADmin = (DECmin * Math.PI) / 180.0;
        const double rad_per_tick = (RADmax + -RADmin) / (ENCmax - ENCmin);

        public new double Declination
        {
            get
            {

                double rad = Radians;

                while (rad > Math.PI / 2)
                    rad -= Math.PI / 2;
                while (rad < -Math.PI / 2)
                    rad += Math.PI / 2;

                return Angle.Rad2Deg(rad);
            }
        }

        public bool Over90Deg
        {
            get
            {
                return Radians > Math.PI / 2;
            }
        }

        public double Radians
        {
            get
            {

                double rad = RADmax - ((ENCmax - Position) * rad_per_tick);

                while (rad > 2 * Math.PI)
                    rad -= 2 * Math.PI;
                while (rad < -2 * Math.PI)
                    rad += 2 * Math.PI;

                return rad;
            }
        }
    }
}
