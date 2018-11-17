﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class Hardware: WiseObject
    {
        private List<WiseBoard> WiseBoards = new List<WiseBoard>();
        public WiseBoard domeboard, teleboard, miscboard;
        private static volatile Hardware _instance/* = new Hardware()*/;     // Singleton
        private static object _syncObject = new object();
        private bool _initialized = false;
        public float mccRevNum, mccVxdRevNum;
        public static WisePin computerControlPin;

        public static Hardware Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncObject)
                    {
                        if (_instance == null)
                            _instance = new Hardware();
                        _instance.init();
                    }
                }
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            int wantedBoards = 3;        // We always have three boards, either real or simulated
            int maxMccBoards;

            if (!Simulated)
            {
                MccService.GetRevision(out mccRevNum, out mccVxdRevNum);
                MccService.ErrHandling(MccDaq.ErrorReporting.DontPrint, MccDaq.ErrorHandling.DontStop);
                maxMccBoards = MccDaq.GlobalConfig.NumBoards;

                // get the real Mcc boards
                for (int i = 0; i < maxMccBoards; i++)
                {
                    int type;

                    MccDaq.MccBoard board = new MccDaq.MccBoard(i);
                    board.BoardConfig.GetBoardType(out type);
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
    }
}
