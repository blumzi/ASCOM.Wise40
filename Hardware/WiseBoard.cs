using System.Collections.Generic;
using System.Windows.Forms;
using ASCOM.Wise40.Common;

using MccDaq;

namespace ASCOM.Wise40.Hardware
{
    public class WiseBoard : WiseObject
    {
        public MccBoard mccBoard;
        public List<WiseDaq> daqs;
        public enum BoardType { Hard, Soft };
        public BoardType type;
        public int boardNum;
        public GroupBox gb;

        public WiseBoard(MccBoard mccBoard, int boardNum = 0)
        {
            int ndaqs;

            if (mccBoard == null)    // a simulated board
            {
                type = BoardType.Soft;
                this.mccBoard = new MccBoard(boardNum);
                this.boardNum = boardNum;
                WiseName = "Board" + boardNum.ToString();
                ndaqs = (boardNum == 1) ? 16 : 4;
            }
            else                // a real Mcc board
            {
                type = BoardType.Hard;
                this.mccBoard = mccBoard;
                this.boardNum = this.mccBoard.BoardNum;
                WiseName = "Board" + this.mccBoard.BoardNum.ToString();               
                this.mccBoard.BoardConfig.GetDiNumDevs(out ndaqs);
            }

            daqs = new List<WiseDaq>();
            for (int devno = 0; devno < ndaqs; devno++)
                daqs.Add(new WiseDaq(this, devno));
        }

        public string ownersToString()
        {
            string ret = WiseName + '\n';

            foreach (WiseDaq daq in daqs)
                ret += daq.ownersToString() + '\n';
            return ret;
        }
    }
}
