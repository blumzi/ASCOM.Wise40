using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class BitExtractor
    {
        public BitExtractor(int nbits, int lsb)
        {
            _nbits = nbits;
            _lsb = lsb;
            _mask = (uint) (1 << _nbits) - 1;
        }

        public uint Extract(uint from)
        {
            return (from >> _lsb) & _mask;
        }

        public uint MaxValue
        {
            get
            {
                return (uint)(1 << _nbits);
            }
        }

        private int _nbits;
        private int _lsb;
        private uint _mask;
    }

    public class WiseDaq : WiseObject
    {
        public WiseBoard wiseBoard;
        public DigitalPortType porttype;
        public DigitalPortDirection portdir;
        public int nbits;
        public WiseBitOwner[] owners;
        public GroupBox gb;

        private ushort _value;
        private ushort _mask;
        public object _lock = new object();

        private Debugger debugger = Debugger.Instance;

        /// <summary>
        /// The Daq's direction
        /// </summary>
        /// <param name="dir">Either DigitalIn or DigitalOut</param>
        public void setDir(DigitalPortDirection dir)
        {
            if (wiseBoard.type == WiseBoard.BoardType.Hard)
            {
                if (Hardware.Instance.mccRevNum == 5)
                {
                    try
                    {
                        lock (_lock)
                        {
                            wiseBoard.mccBoard.DConfigPort(porttype, dir);
                        }
                    } catch (Exception err)
                    {
                        throw new WiseException(WiseName + ": UL DConfigPort(" + porttype.ToString() + ", " + dir.ToString() + ") failed with " + err.Message);
                    }
                }
                else
                {
                    ErrorInfo err;

                    lock (_lock)
                    {
                        err = wiseBoard.mccBoard.DConfigPort(porttype, dir);
                    }
                    if (err.Value != 0)
                        throw new WiseException(WiseName + ": UL DConfigPort(" + porttype.ToString() + ", " + dir.ToString() + ") failed with " + err.Message);
                }

            }
            portdir = dir;
        }

        public WiseDaq(WiseBoard wiseBoard, int devno)
        {
            int porttype;

            this.wiseBoard = wiseBoard;
            if (wiseBoard.type == WiseBoard.BoardType.Soft)
            {
                porttype = (int) DigitalPortType.FirstPortA + devno;
                WiseName = "Board" + wiseBoard.boardNum.ToString() + "." + ((DigitalPortType)porttype).ToString();
                _value = 0;
                switch(devno % 4)
                {
                    case 0: nbits = 8; break;   // XXX-PortA
                    case 1: nbits = 8; break;   // XXX-PortB
                    case 2: nbits = 4; break;   // XXX-PortCL
                    case 3: nbits = 4; break;   // XXX-PortCH
                }
            }
            else
            {
                MccDaq.ErrorInfo err;

                err = wiseBoard.mccBoard.DioConfig.GetDevType(devno, out porttype);
                err = wiseBoard.mccBoard.DioConfig.GetNumBits(devno, out nbits);
                WiseName = "Board" + wiseBoard.mccBoard.BoardNum.ToString() + "." + ((DigitalPortType)porttype).ToString();
            }

            this.porttype = (DigitalPortType) porttype;
            _mask = (ushort) ((nbits == 8) ? 0xff : 0xf);
            owners = new WiseBitOwner[nbits];
            for (int i = 0; i < nbits; i++)
                owners[i] = new WiseBitOwner();
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

                if (wiseBoard.type == WiseBoard.BoardType.Hard)
                {

                    if (Hardware.Instance.mccRevNum == 5)
                    {
                        try
                        {
                            lock (_lock)
                            {
                                wiseBoard.mccBoard.DIn(porttype, out v);
                            }
                        }
                        catch (Exception err)
                        {
                            throw new WiseException(WiseName + ": UL DIn(" + porttype.ToString() + ") failed with " + err.Message);
                        }
                    }
                    else
                    {
                        ErrorInfo err;
                        lock (_lock)
                        {
                            err = wiseBoard.mccBoard.DIn(porttype, out v);
                        }
                        if (err.Value != ErrorInfo.ErrorCode.NoErrors)
                            throw new WiseException(WiseName + ": UL DIn(" + porttype.ToString() + ") failed with " + err.Message);
                    }
                }
                else
                    v = _value;

                return (ushort)(v & _mask);
            }

            set {
                ushort before = this.Value;

                if (wiseBoard.type == WiseBoard.BoardType.Hard)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugDAQs,
                        "daq.Value.set: board: {0}, port: {1}, value: 0x{2:x} => 0x{3:x}",
                        this.wiseBoard.WiseName,
                        this.porttype.ToString(),
                        before,
                        value);
                    if (portdir == DigitalPortDirection.DigitalOut)
                    {
                        if (Hardware.Instance.mccRevNum == 5)
                        {
                            try
                            {
                                lock (_lock)
                                {
                                    wiseBoard.mccBoard.DOut(porttype, value);
                                }
                            }
                            catch (Exception err)
                            {
                                throw new WiseException(WiseName + ": UL DOut(" + porttype.ToString() + ", " + value.ToString() + ") failed with :\"" + err.Message + "\"");
                            }
                        }
                        else
                        {
                            ErrorInfo err;
                            lock (_lock)
                            {
                                err = wiseBoard.mccBoard.DOut(porttype, value);
                            }
                            if (err.Value != ErrorInfo.ErrorCode.NoErrors)
                                throw new WiseException(WiseName + ": UL DOut(" + porttype.ToString() + ", " + value.ToString() + ") failed with :\"" + err.Message + "\"");
                        }

                        _value = value;
                    }
                }
                else
                    _value = value;
            }
        }

        /// <summary>
        ///  Remembers who owns the various bits of the Daq
        /// </summary>
       public void setOwner(string owner, int bit)
        {
            WiseBitOwner o = owners[bit];

            //if (o.owner != null && o.owner != owner )
            //    throw new WiseException(string.Format("Cannot set owner \"{0}\" for {1}[{2}]: Already owned by \"{3}\"!", owner, name, bit, o.owner));

            o.owner = owner;
            if (o.checkBox != null)
                o.checkBox.Text = owner;
        }

        public void unsetOwner(int bit)
        {
            owners[bit].owner = null;
            if (owners[bit].checkBox != null)
                owners[bit].checkBox.Text = "";
        }

        public void unsetOwners()
        {
            for (int i = 0; i < nbits; i++)
                unsetOwner(i);
        }

        public void setOwners(string owner)
        {
            for (int bit = 0; bit < nbits; bit++)
                setOwner(owner, bit);
        }

        public string ownersToString()
        {
            string ret = null;

            for (int bit = 0; bit < nbits; bit++)
                if (owners[bit].owner != null)
                    ret += WiseName + "[" + bit.ToString() + "]: " + owners[bit].owner + '\n';
            return ret;
        }
    }
}
