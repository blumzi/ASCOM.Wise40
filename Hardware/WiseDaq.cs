using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MccDaq;


namespace ASCOM.WiseHardware
{
    public class WiseDaq : WiseObject
    {
        private MccBoard board;
        public DigitalPortType porttype;
        public DigitalPortDirection portdir;
        public int nbits;
        public string[] owners;

        private ushort value;
        private ushort mask;

        /// <summary>
        /// The Daq's direction
        /// </summary>
        /// <param name="dir">Either DigitalIn or DigitalOut</param>
        public void setdir(DigitalPortDirection dir)
        {
            try {
                board.DConfigPort(porttype, dir);
            }
            catch (ULException e)
            {
                throw new WiseException(name + ": UL DConfigPort(" + porttype.ToString() + ", " + dir.ToString() + ") failed with " + e.Message);
            }
            portdir = dir;
        }

        public WiseDaq(MccBoard brd, int devno)
        {
            int type;
            MccDaq.ErrorInfo err;

            board = brd;

            err = board.DioConfig.GetDevType(devno, out type);
            porttype = (DigitalPortType) type;

            err = board.DioConfig.GetNumBits(devno, out nbits);
            mask = (ushort) ((nbits == 8) ? 0xff : 0xf);

            name = "Board" + board.BoardNum.ToString() + "." + porttype.ToString();
            owners = new string[nbits];
        }

        public void Dispose()
        {
            for (int i = 0; i < nbits; i++)
            {
                owners[i] = null;
            }
        }

        /// <summary>
        /// The software-maintained value of the Daq
        /// </summary>
        public ushort Value
        {
           get {
                ushort v;

                if (portdir == DigitalPortDirection.DigitalIn) 
                    try
                    {
                        board.DIn(porttype, out v);
                    }
                    catch (ULException e)
                    {
                        throw new WiseException(name + ": UL DIn(" + porttype.ToString() + ") failed with :\"" + e.Message + "\"");
                    }
                else
                    v = value;

                return (ushort)(v & mask);
            }

            set {
                if (portdir == MccDaq.DigitalPortDirection.DigitalOut)
                {
                    try
                    {
                        board.DOut(porttype, value);
                    }
                    catch (ULException e)
                    {
                        throw new WiseException(name + ": UL DOut(" + porttype.ToString() + ", " + value.ToString() + ") failed with :\"" + e.Message + "\"");
                    }
                    this.value = value;
                }
            }
        }

        /// <summary>
        ///  Remembers who owns the various bits of the Daq
        /// </summary>
       public void setowner(string owner, int bit)
        {
            if (owners[bit] != null)
                throw new WiseException(name + ": Already owned by \"" + owners[bit] + "\"");
            owners[bit] = owner;
        }

        public void unsetowner(int bit)
        {
            owners[bit] = null;
        }

        public string ownersToString()
        {
            string ret = null;

            for (int bit = 0; bit < nbits; bit++)
                if (owners[bit] != null)
                    ret += name + "[" + bit.ToString() + "]: " + owners[bit] + '\n';
            return ret;
        }
    }
}
