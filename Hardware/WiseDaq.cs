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

        private readonly int _nbits;
        private readonly int _lsb;
        private readonly uint _mask;
    }

    public class WiseDaq : WiseObject
    {
        public WiseBoard wiseBoard;
        public DigitalPortType porttype;
        public DigitalPortDirection portdir;
        public int nbits;
        public WiseBitOwner[] owners;

        private ushort _value;
        private readonly ushort _mask;
        public object _lock = new object();

        private readonly Debugger debugger = Debugger.Instance;

        /// <summary>
        /// The Daq's direction
        /// </summary>
        /// <param name="dir">Either DigitalIn or DigitalOut</param>
        public void SetDir(DigitalPortDirection dir)
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
                        Exceptor.Throw<WiseException>("SetDir", $"{WiseName}: UL DConfigPort({porttype}, {dir}) failed with {err.Message}");
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
                        Exceptor.Throw<WiseException>("SetDir", $"{WiseName}: UL DConfigPort({porttype}, {dir}) failed with {err.Message}");
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
                WiseName = $"Board{wiseBoard.boardNum}.{(DigitalPortType)porttype}";
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
                wiseBoard.mccBoard.DioConfig.GetDevType(devno, out porttype);
                wiseBoard.mccBoard.DioConfig.GetNumBits(devno, out nbits);
                WiseName = $"Board{wiseBoard.mccBoard.BoardNum}.{(DigitalPortType)porttype}";
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
                ushort v = ushort.MinValue;

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
                            Exceptor.Throw<WiseException>("Value", $"{WiseName}: UL DIn({porttype}) failed with {err.Message}");
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
                            Exceptor.Throw<WiseException>("Value", $"{WiseName}: UL DIn({porttype}) failed with {err.Message}");
                    }
                }
                else
                {
                    v = _value;
                }

                return (ushort)(v & _mask);
            }

            set {
                ushort before = this.Value;

                if (wiseBoard.type == WiseBoard.BoardType.Hard)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugDAQs,
                        $"daq.Value.set: board: {wiseBoard.WiseName}, port: {porttype}, value: 0x{before:x} => 0x{value:x}");
                    #endregion
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
                                Exceptor.Throw<WiseException>("Value.set", $"{WiseName}: UL DOut({porttype}, 0x{value:x}) failed with :\"{err.Message}\"");
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
                                Exceptor.Throw<WiseException>("Value.set", $"{WiseName}: UL DOut({porttype}, 0x{value:x}) failed with :\"{err.Message}\"");
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
       public void SetOwner(string owner, int bit)
        {
            owners[bit].owner = owner;
        }

        public void UnsetOwner(int bit)
        {
            owners[bit].owner = null;
        }

        public void UnsetOwners()
        {
            for (int i = 0; i < nbits; i++)
                UnsetOwner(i);
        }

        public void SetOwners(string owner)
        {
            for (int bit = 0; bit < nbits; bit++)
                SetOwner(owner, bit);
        }

        public string OwnersToString()
        {
            string ret = null;

            for (int bit = 0; bit < nbits; bit++)
            {
                if (owners[bit].owner != null)
                {
                    ret += $"{WiseName}[{bit}]: {owners[bit].owner}\n";
                }
            }

            return ret;
        }
    }

    public class DaqMetaDigest
    {
        public DigitalPortType Porttype;
        public DigitalPortDirection Portdir;
        public int Nbits;
        public List<string> Owners;

        public static DaqMetaDigest FromHardware(WiseDaq daq)
        {
            DaqMetaDigest ret = new DaqMetaDigest()
            {
                Porttype = daq.porttype,
                Portdir = daq.portdir,
                Nbits = daq.nbits,
                Owners = new List<string>(),
            };

            foreach (WiseBitOwner owner in daq.owners)
                ret.Owners.Add(owner.owner);

            return ret;
        }
    }

    public class DaqDigest
    {
        public ushort Value;

        public static DaqDigest FromHardware(WiseDaq daq)
        {
            DaqDigest ret = new DaqDigest
            {
                Value = daq.Value
            };

            return ret;
        }
    }
}
