using ASCOM.Wise40.Common;
using PCIe1711_NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Hardware
{
    public class RenishawEncoder
    {
        private static readonly Lazy<RenishawEncoder> lazy = new Lazy<RenishawEncoder>(() => new RenishawEncoder()); // Singleton
        private bool _initialized = false;
        private static readonly Common.Debugger debugger = Common.Debugger.Instance;
        private static PCIe1711 Board;
        private const int BoardNumber = 0;
        private const byte CRCPolynom = ((1 << 6) | (1 << 1) | (1 << 0));
        private enum ChannelMode { BISS = 0, SSI = 1};
        private enum BissMode { B = 0, C = 1 };
        public enum EncoderType { HA, Dec };

        public static RenishawEncoder Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (_initialized)
                return;

            if (PCIe1711.GetBoardCount() == 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Renishaw.Init: no PCIe1171 boards");
                #endregion
                _initialized = true;
                return;
            }

            Board = PCIe1711.OpenBoard(BoardNumber);
            if (Board == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"Renishaw.Init(): OpenBoard({BoardNumber})");
                #endregion
                return;
            }
            _initialized = true;
        }

        public static uint HourAngle
        {
            get
            {
                return Read(EncoderType.HA);
            }
        }

        public static ulong Declination
        {
            get
            {
                return Read(EncoderType.Dec);
            }
        }

        public static uint Read(EncoderType enc)
        {
            {
                int ret;
                const int nEncoders = 1;
                const int nBits = 26;

                byte[] channels = { 0 };
                byte[] dataLengths = { nBits };
                byte[] options = { 0 };
                byte[] polynoms = { CRCPolynom };
                byte[] inverts = { 1 };
                byte moduleNumber = (byte)(enc == EncoderType.HA ? 0 : 1);

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

                if (ret != 0)
                {
                    Exceptor.Throw<Hardware.BissMasterException>("BissMasterInitSingleCycle",
                        $"Cannot initialize BISS for the {enc} encoder (ret: {ret})");
                }

                ret = Board.BissMasterSingleCycleDataRead(moduleNbr: moduleNumber, slaveIndex: 0, out uint value, out _);
                if (ret != 0)
                {
                    Exceptor.Throw<Hardware.BissMasterException>("BissMasterSingleCycleDataRead",
                        $"Cannot read BISS from the {enc} encoder (ret: {ret})");
                }

                ret = Board.BissMasterReleaseSingleCycle(moduleNbr: moduleNumber);
                {
                    Exceptor.Throw<Hardware.BissMasterException>("BissMasterReleaseSingleCycle",
                        $"Cannot release BISS for the {enc} encoder (ret: {ret})");
                }

                return (uint)(value & ~(1 << nBits));
            }
        }
    }
}
