using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MccDaq;

namespace ASCOM.WiseHardware
{
    public class WiseBoard : WiseObject
    {
        public MccBoard board;
        public List<WiseDaq> daqs;

        public WiseBoard(MccBoard brd)
        {
            int ndaqs;

            board = brd;
            name = "Board" + board.BoardNum.ToString();

            daqs = new List<WiseDaq>();
            board.BoardConfig.GetDiNumDevs(out ndaqs);

            for (int devno = 0; devno < ndaqs; devno++)
                daqs.Add(new WiseDaq(board, devno));
        }

        public string ownersToString()
        {
            string ret = name + '\n';

            foreach (WiseDaq daq in daqs)
                ret += daq.ownersToString() + '\n';
            return ret;
        }
    }
}
