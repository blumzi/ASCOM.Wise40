using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using MccDaq;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public class WisePin : WiseObject, IConnectable, IDisposable, IOnOff
    {
        private int bit;
        private WiseDaq daq;
        private DigitalPortDirection dir;
        private bool inverse;
        private bool _connected = false;
        private bool _controlled;
        private Const.Direction _direction = Const.Direction.None;  // Generally speaking - does it increase or decrease an encoder value
        private Debugger debugger = Debugger.Instance;

        public WisePin(string name,
            WiseBoard brd,
            DigitalPortType port,
            int bit,
            DigitalPortDirection dir,
            bool inverse = false,
            Const.Direction direction = Const.Direction.None,
            bool controlled = false)
        {
            this.WiseName = name +
                "@Board" +
                (brd.type == WiseBoard.BoardType.Hard ? brd.mccBoard.BoardNum : brd.boardNum) +
                port.ToString() +
                "[" + bit.ToString() + "]";

            if ((daq = brd.daqs.Find(x => x.porttype == port)) == null)
                throw new WiseException(this.WiseName + ": Invalid Daq spec, no " + port + " on this board");
            this.dir = dir;
            this.bit = bit;
            this.inverse = inverse;
            this._direction = direction;
            this._controlled = controlled;
            daq.setDir(dir);
            if (daq.owners != null && daq.owners[bit].owner == null)
                daq.setOwner(name, bit);
        }

        public void SetOn()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;

            if (!Simulated && _controlled && Hardware.computerControlPin.isOff)
                throw new Hardware.MaintenanceModeException(Const.computerControlAtMaintenance);

            int i, maxTries = 10;
            lock (daq._lock)
            {
                ushort v, v1;
                daq.wiseBoard.mccBoard.DIn(daq.porttype, out v);
                v |= (ushort)(1 << bit);

                if (WiseName.StartsWith("Focus"))
                {
                    //
                    // Somehow the Focus pins (maybe this is specific to the DAQ board,
                    //  behave differently from the pins on the other boards.
                    // DON'T do the validation loop.
                    //
                    daq.wiseBoard.mccBoard.DOut(daq.porttype, v);
                    return;
                }

                for (i = 0; i < maxTries; i++)
                {
                    daq.wiseBoard.mccBoard.DOut(daq.porttype, v);

                    Thread.Sleep(100);
                    daq.wiseBoard.mccBoard.DIn(daq.porttype, out v1);
                    if (v1 == v)
                    {
                        if (i > 0)
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    string.Format("SetOn: pin {0} got On after {1} tries!",
                                    WiseName, i + 1));
                            #endregion
                        return;
                    }
                    Thread.Sleep(20);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    string.Format("SetOn: pin {0} does not get On after {1} tries!",
                    WiseName, maxTries));
            #endregion
        }

        public void SetOff()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;

            if (!Simulated && _controlled && Hardware.computerControlPin.isOff)
                throw new Hardware.MaintenanceModeException(Const.computerControlAtMaintenance);

            int i, maxTries = 10;
            lock (daq._lock)
            {
                ushort v, v1;
                daq.wiseBoard.mccBoard.DIn(daq.porttype, out v);

                v &= (ushort)~(1 << bit);

                if (WiseName.StartsWith("Focus"))
                {
                    //
                    // Somehow the Focus pins (maybe this is specific to the DAQ board,
                    //  behave differently from the pins on the other boards.
                    // DON'T do the validation loop.
                    //
                    daq.wiseBoard.mccBoard.DOut(daq.porttype, v);
                    return;
                }

                for (i = 0; i < maxTries; i++)
                {
                    daq.wiseBoard.mccBoard.DOut(daq.porttype, v);
                    Thread.Sleep(100);
                    daq.wiseBoard.mccBoard.DIn(daq.porttype, out v1);
                    if (v == v1)
                    {
                        if (i > 0)
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    string.Format("SetOff: pin {0} got Off after {1} tries!",
                                    WiseName, i + 1));
                            #endregion
                        return;
                    }
                    Thread.Sleep(20);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                string.Format("SetOff: pin {0} does not get Off after {1} tries!",
                    WiseName, maxTries));
            #endregion
        }

        public bool isOn
        {
            get
            {
                bool ret = (daq.Value & (ushort)(1 << bit)) != 0;

                return inverse ? !ret : ret;
            }
        }

        public bool isOff
        {
            get
            {
                return !isOn;
            }
        }

        public void Connect(bool connected)
        {
            if (connected)
                daq.setOwner(WiseName, bit);
            else
                daq.unsetOwner(bit);
            _connected = connected;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        public void Dispose()
        {
            SetOff();
            daq.unsetOwner(bit);
        }

        public Const.Direction Direction
        {
            get
            {
                return _direction;
            }
        }
    }
}