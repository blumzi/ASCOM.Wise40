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
        private readonly int bit;
        private readonly WiseDaq daq;
        private readonly DigitalPortDirection dir;
        private readonly bool inverse;
        private bool _connected = false;
        private readonly bool _controlled;
        private readonly Debugger debugger = Debugger.Instance;

        public WisePin(string name,
            WiseBoard brd,
            DigitalPortType port,
            int bit,
            DigitalPortDirection dir,
            bool inverse = false,
            Const.Direction direction = Const.Direction.None,
            bool controlled = false)
        {
            int boardNumber = (brd.type == WiseBoard.BoardType.Hard) ? brd.mccBoard.BoardNum : brd.boardNum;
            WiseName = $"{name}@Board{boardNumber}{port}[{bit}]";

            if ((daq = brd.daqs.Find(x => x.porttype == port)) == null)
                Exceptor.Throw<WiseException>("WisePin", $"{WiseName}: Invalid Daq spec, no {port} on this board");
            this.dir = dir;
            this.bit = bit;
            this.inverse = inverse;
            this.Direction = direction;
            this._controlled = controlled;
            daq.SetDir(dir);
            if (daq.owners != null && daq.owners[bit].owner == null)
                daq.SetOwner(name, bit);
        }

        public void SetOn()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;

            if (!Simulated && _controlled && Hardware.computerControlPin.isOff)
            {
                //Exceptor.Throw<Hardware.MaintenanceModeException>("SetOn", Const.computerControlAtMaintenance);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WisePin:SetOff: Cannot set OFF - MAINTENANCE mode");
                #endregion
                return;
            }

            int i, maxTries = 10;
            lock (daq._lock)
            {
                daq.wiseBoard.mccBoard.DIn(daq.porttype, out ushort v);
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
                    daq.wiseBoard.mccBoard.DIn(daq.porttype, out ushort v1);
                    if (v1 == v)
                    {
                        if (i > 0)
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"SetOn: pin {WiseName} got On after {i + 1} tries!");
                            #endregion
                        return;
                    }
                    Thread.Sleep(20);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"SetOn: pin {WiseName} does not get On after {maxTries} tries!");
            #endregion
        }

        public void SetOff()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;

            if (!Simulated && _controlled && Hardware.computerControlPin.isOff)
            {
                //Exceptor.Throw<Hardware.MaintenanceModeException>("SetOff", Const.computerControlAtMaintenance);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WisePin:SetOff: Cannot set OFF - MAINTENANCE mode");
                #endregion
                return;
            }

            int i, maxTries = 10;
            lock (daq._lock)
            {
                daq.wiseBoard.mccBoard.DIn(daq.porttype, out ushort v);

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
                    daq.wiseBoard.mccBoard.DIn(daq.porttype, out ushort v1);
                    if (v == v1)
                    {
                        if (i > 0)
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    $"SetOff: pin {WiseName} got Off after {i + 1} tries!");
                            #endregion
                        return;
                    }
                    Thread.Sleep(20);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"SetOff: pin {WiseName} does not get Off after {maxTries} tries!");
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
                daq.SetOwner(WiseName, bit);
            else
                daq.UnsetOwner(bit);
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
            daq.UnsetOwner(bit);
        }

        public Const.Direction Direction { get; } = Const.Direction.None;
    }
}