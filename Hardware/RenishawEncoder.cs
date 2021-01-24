using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCIe1711_NET;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
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

        private const double haConstant = 0.002547126296947 / 19482030;
        private const double decConstant = haConstant;  // TBD
        private enum BissMode { B = 0, C = 1 };
        private static readonly PCIe1711 Board = PCIe1711.OpenBoard(0);
        public enum Module { Ha = 0, Dec = 1 };
        private readonly byte _moduleNumber;
        private readonly Module _module;

        private const int jitterBits = 6;       // number of LSBs to discard

        private static Debugger debugger = Debugger.Instance;

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

        public UInt64 Position
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
                UInt32 position = (UInt32)((reading >> 2) & 0xffffffff);

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
                    return position >> jitterBits;
                }
            }
        }

        public double Radians
        {
            get
            {
                return Position * ((_module == Module.Ha) ? haConstant : decConstant);
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
}
