using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;

using ASCOM.Wise40.Common;
using ASCOM.Wise40;

namespace ASCOM.Wise40.Hardware
{
    public class Hardware: WiseObject
    {
        public List<WiseBoard> WiseBoards = new List<WiseBoard>();
        public WiseBoard domeboard, teleboard, miscboard;
        private bool _initialized = false;
        public float mccRevNum, mccVxdRevNum;
        public static WisePin computerControlPin;

        private static readonly Lazy<Hardware> lazy = new Lazy<Hardware>(() => new Hardware()); // Singleton

        public static Hardware Instance
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

            const int wantedBoards = 3;        // We always have three boards, either real or simulated
            int maxMccBoards;

            if (!Simulated)
            {
                MccService.GetRevision(out mccRevNum, out mccVxdRevNum);
                MccService.ErrHandling(MccDaq.ErrorReporting.DontPrint, MccDaq.ErrorHandling.DontStop);
                maxMccBoards = MccDaq.GlobalConfig.NumBoards;

                // get the real Mcc boards
                for (int i = 0; i < maxMccBoards; i++)
                {
                    MccBoard board = new MccBoard(i);
                    board.BoardConfig.GetBoardType(out int type);
                    if (type != 0)
                        WiseBoards.Add(new WiseBoard(board));
                }
            }

            // Add simulated boards, as needed
            for (int i = WiseBoards.Count; i < wantedBoards; i++)
            {
                WiseBoards.Add(new WiseBoard(null, i));
            }

            domeboard = WiseBoards.Find(x => x.mccBoard.BoardNum == 0);
            teleboard = WiseBoards.Find(x => x.mccBoard.BoardNum == 1);
            miscboard = WiseBoards.Find(x => x.mccBoard.BoardNum == 2);

            if (computerControlPin == null)
            {
                computerControlPin = new WisePin("CompControl", teleboard, DigitalPortType.SecondPortCH, 0, DigitalPortDirection.DigitalIn);
                computerControlPin.Connect(true);
            }

            _initialized = true;
        }

        public Hardware() { }
        static Hardware() { }

        public static bool ComputerHasControl
        {
            get
            {
                Instance.Init();
                return computerControlPin.isOn;
            }
        }

        public static bool MaintenanceMode
        {
            get
            {
                return !ComputerHasControl;
            }
        }

        [Serializable]
        public class MaintenanceModeException : Exception
        {
            public MaintenanceModeException()
            {
            }

            public MaintenanceModeException(string message)
                : base(message)
            {
            }

            public MaintenanceModeException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        [Serializable]
        public class DaqsException : Exception
        {
            public DaqsException()
            {
            }

            public DaqsException(string message)
                : base(message)
            {
            }

            public DaqsException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        [Serializable]
        public class BissMasterException : Exception
        {
            public BissMasterException()
            {
            }

            public BissMasterException(string message)
                : base(message)
            {
            }

            public BissMasterException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }

    public class HardwareMetaDigest
    {
        public List<BoardMetaDigest> Boards;

        public static HardwareMetaDigest FromHardware()
        {
            HardwareMetaDigest ret = new HardwareMetaDigest()
            {
                Boards = new List<BoardMetaDigest>()
            };

            foreach (WiseBoard board in Hardware.Instance.WiseBoards)
                ret.Boards.Add(BoardMetaDigest.FromHardware(board));

            return ret;
        }
    }

    public class HardwareDigest
    {
        public List<BoardDigest> Boards;

        public static HardwareDigest FromHardware()
        {
            HardwareDigest ret = new HardwareDigest() {
                Boards = new List<BoardDigest>(),
            };

            foreach (WiseBoard board in Hardware.Instance.WiseBoards)
                ret.Boards.Add(BoardDigest.FromHardware(board));

            return ret;
        }
    }

    public class WiseBitOwner
    {
        public string owner;
    }
}
