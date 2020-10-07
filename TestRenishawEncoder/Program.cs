using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCIe1711_NET;
using System.Diagnostics;

namespace TestRenishawEncoder
{
    public static class Program
    {
        private enum ChannelMode { BISS = 0, SSI = 1 };
        private const byte CRCPolynom = ((1 << 6) | (1 << 1) | (1 << 0));
        private enum BissMode { B = 0, C = 1 };

        public static void Main()
        {
            PCIe1711 Board = null;

            int ret;
            const int nEncoders = 1;
            const int nBits = 26;

            byte[] channels = { 0 };
            byte[] dataLengths = { nBits };
            byte[] options = { 0 };
            byte[] polynoms = { CRCPolynom };
            byte[] inverts = { 1 };
            const byte moduleNumber = 1;
            const int boardNo = 0;

            try
            {
                Board = PCIe1711.OpenBoard(boardNo);
            } catch (Exception ex)
            {
                Debug.WriteLine($"OpenBoard caught {ex.Message}");
                Environment.Exit(1);
            }

            ret = Board.BissMasterInitSingleCycle(moduleNbr: moduleNumber,
                sensorDataFreqDivisor: 0,
                registerDataFreqDivisor: 0,
                channel0BISSSSIMode: (byte)ChannelMode.BISS,
                channel0BISSMode: (byte)BissMode.C,
                channel1BISSSSIMode: 0,
                channel1BISSMode: 0,
                nbrOfSlave: nEncoders,
                channel: channels,
                dataLength: dataLengths,
                option: options,
                CRCPolynom: polynoms,
                CRCInvert: inverts);

            Debug.WriteLine($"BissMasterInitSingleCycle: ret == {ret}");
            if (ret != 0)
                Environment.Exit(1);

            ret = Board.BissMasterSingleCycleDataRead(moduleNbr: moduleNumber, slaveIndex: 0, out uint value, out _);
            Debug.WriteLine($"BissMasterSingleCycleDataRead: ret == {ret}");
            if (ret != 0)
                Environment.Exit(2);
            Debug.WriteLine($"value: {value}");

            ret = Board.BissMasterReleaseSingleCycle(moduleNbr: moduleNumber);
            Debug.WriteLine($"BissMasterReleaseSingleCycle: ret == {ret}");
            if (ret != 0)
                Environment.Exit(3);

            Debug.WriteLine("Done.");
        }
    }
}
