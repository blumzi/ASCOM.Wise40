using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCIe1711_NET;

namespace Hardware
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
        private enum BissMode { B = 0, C = 1 };
        private static readonly PCIe1711 Board = PCIe1711.OpenBoard(0);
        private readonly byte _moduleNumber;
        public RenishawEncoder(int encoderNumber)
        {
            int ret;

            byte[] channels = { 0 };
            byte[] options = { 0 };
            byte[] polynoms = { CRCPolynom };
            byte[] inverts = { 1 };
            byte[] dataLengths = { 35 };
            _moduleNumber = (byte)encoderNumber;

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
        public UInt32 Position
        {
            get
            {
                string op = $"Renishaw({_moduleNumber}).Position";
                int ret = Board.BissMasterSingleCycleDataRead(
                    moduleNbr: _moduleNumber,
                    slaveIndex: 0,
                    out UInt32 low,
                    out UInt32 high);

                if (ret != 0)
                    throw new Exception($"{op}: BissMasterSingleCycleDataRead returned {ret}");

                UInt64 reading = (high << 32) | low;
                bool warning = (reading & (1 << 0)) == 0;
                bool error = (reading & (1 << 1)) == 0;
                UInt32 position = (UInt32) ((reading >> 2) & 0xffffffff);

                if (warning)
                    throw new Exception($"{op}: encoder or scale need cleaning");
                if (error)
                    throw new Exception($"{op}: encoder read error");

                return position;
            }
        }
        ~RenishawEncoder()
        {
            Board.BissMasterReleaseSingleCycle(moduleNbr: _moduleNumber);
        }
    }
}
