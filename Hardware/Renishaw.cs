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
    public class Renishaw
    {
        private static readonly Lazy<Renishaw> lazy = new Lazy<Renishaw>(() => new Renishaw()); // Singleton
        private bool _initialized = false;
        private static readonly Common.Debugger debugger = Common.Debugger.Instance;
        private PCIe1711 Board;
        private const int BoardNumber = 0;
        private const int HourAngleModule = 0;
        private const int DeclinationModule = 1;

        public static Renishaw Instance
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

            int ret = 0;
            Board = PCIe1711.OpenBoard(BoardNumber);
            if (Board == null)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"Renishaw.Init(): OpenBoard({BoardNumber})");
                #endregion
                return;
            }

            //ret = Board.BissMasterInitSingleCycle(HourAngleModule);
            if (ret != 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"Renishaw.Init(): Could not initialize HourAngleModule ({HourAngleModule}) as Biss Master: ret = {ret}");
                #endregion
                return;
            }

            //ret = Board.BissMasterInitSingleCycle(DeclinationModule);
            if (ret != 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"Renishaw.Init(): Could not initialize DeclinationModule ({DeclinationModule}) as Biss Master: ret = {ret}");
                #endregion
                return;
            }

            _initialized = true;
        }

        public static ulong HourAngle
        {
            get
            {
                return 0;
            }
        }
        public static ulong Declination
        {
            get
            {
                return 0;
            }
        }
    }
}
