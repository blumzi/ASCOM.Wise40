using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.WiseHardware
{
    public class Hardware
    {
        private List<WiseBoard> WiseBoards;
        public WiseBoard domeboard, teleboard, miscboard;
        private static Hardware hw;
        private bool isInitialized;
        public static string productionMachine = "dome-ctlr";
        public static Hardware Instance
        {
            get
            {
                if (hw == null)
                    hw = new Hardware();
                return hw;
            }
        }

        public void init(bool simulated)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                int maxboards = MccDaq.GlobalConfig.NumBoards;
                int type;

                MccDaq.MccService.ErrHandling(MccDaq.ErrorReporting.DontPrint, MccDaq.ErrorHandling.DontStop);

                WiseBoards = new List<WiseBoard>();
                for (int i = 0; i < maxboards; i++)
                {
                    MccDaq.MccBoard board = new MccDaq.MccBoard(i);
                    board.BoardConfig.GetBoardType(out type);
                    if (type != 0)
                        WiseBoards.Add(new WiseBoard(board));
                }

                if (simulated)
                {
                    miscboard = WiseBoards.Find(x => x.board.BoardNum == 0); ;
                    teleboard = miscboard;
                    domeboard = miscboard;
                }
                else
                {
                    miscboard = WiseBoards.Find(x => x.board.BoardNum == 0);
                    teleboard = WiseBoards.Find(x => x.board.BoardNum == 1);
                    domeboard = WiseBoards.Find(x => x.board.BoardNum == 2);
                }
            }
        }


        public Hardware()
        {
        }
    }
}
