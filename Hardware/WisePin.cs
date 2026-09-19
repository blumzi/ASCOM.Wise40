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
        private bool disposed = false;
        public static readonly Exceptor Exceptor = new Exceptor(Common.Debugger.DebugLevel.DebugDAQs);

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
                Exceptor.Throw<Exception>("WisePin", $"{WiseName}: Invalid Daq spec, no {port} on this board");
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

            if (!Simulated && _controlled && Hardware.MaintenanceMode)
            {
                //Exceptor.Throw<Hardware.MaintenanceModeException>("SetOn", Const.computerControlAtMaintenance);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WisePin:SetOff: Cannot set OFF - MAINTENANCE mode");
                #endregion
                return;
            }

            DriveAndVerify(on: true, op: "SetOn");
        }

        //
        // How long to keep trying before declaring the pin dead, and how often to look.
        //
        // The budget is the same 1200ms the old loop spent (10 tries x (100 + 20)), kept
        //  deliberately: a genuine hardware refusal should still be reported after the same
        //  wait, and shortening it is a judgement about relays that the code has no evidence
        //  for.  What changed is that the readback is now CHECKED every 5ms instead of once
        //  every 120ms, so the ordinary case - which is every case that ever succeeds - costs
        //  a few milliseconds rather than a flat 100.
        //
        private const int readbackBudgetMillis = 1200;
        private const int readbackPollMillis = 5;
        private const int rewriteEveryMillis = 100;     // the old loop re-issued DOut each try

        /// <summary>
        /// Drives this pin and confirms it read back, then returns as soon as it has.
        ///
        /// Two things were wrong with doing this inline, twice, once per direction:
        ///
        ///  - It compared the WHOLE PORT.  v was the port as read, with our bit adjusted, and
        ///     success meant the entire port matched.  TeleNorth/East/West/South share
        ///     FirstPortCL bits 0-3 and the four guide pins share FirstPortB bits 0-3, so a
        ///     write by the other axis between our read and our readback made the comparison
        ///     fail on a bit that was never ours - and burned the whole retry budget doing it.
        ///     Only the bit being written is checked now.
        ///
        ///  - Every attempt cost a flat Thread.Sleep(100) while holding daq._lock, so the
        ///     other axis waited too.  A slew does 10-16 pin writes across both axes; that is
        ///     1.0-1.6 seconds of sleeping per slew.
        ///
        /// Worth recording what the logs say about the retry loop this replaces: across a
        ///  full night, retries succeeded ZERO times and gave up 8 times.  A pin either reads
        ///  back at once or it never does, so the repeated attempts bought nothing except the
        ///  1.2 seconds they took to conclude. The diagnostic on failure now says what was
        ///  actually read and whether other bits on the port moved, which distinguishes a
        ///  refusing relay from contention.
        /// </summary>
        private void DriveAndVerify(bool on, string op)
        {
            ushort mask = (ushort)(1 << bit);

            //
            // What our bit must read once the relay has responded.  Same comparison the old
            //  code made - it built a whole target port value and then masked it down to this.
            //  Note this is the RAW bit sense: inverse is applied by isOn, not here, which is
            //  how it always behaved.
            //
            ushort target = on ? mask : (ushort)0;
            ushort before, got;

            //
            // THE LOCK IS TAKEN PER DAQ ACCESS, NEVER ACROSS THE WAIT.
            //
            // The poll loop used to sit inside one lock (daq._lock) spanning the whole method,
            //  Thread.Sleep included, so a single pin could hold a shared BOARD lock for up to
            //  readbackBudgetMillis - 1200ms.  Everything else touching that board queued
            //  behind it, and there is a lot of it: WisePin.isOn is a live DIn, so StopAxis's
            //  m.IsOn, WiseVirtualMotor.ActiveMortorPins - called twice per SetOff - and
            //  AxisMonitor's ActiveMotors all block on it.
            //
            // Measured consequence, 2026-09-19: StopAxis took 1.19 SECONDS on the primary axis.
            //  At slew rate that is 2.2 degrees of travel after the decision to stop, which
            //  turned a 0.19 degree undershoot into a 2.0 degree overshoot and cost 139 seconds
            //  of set-rate crawling to undo.  Neither the "took Nms to read back" line nor the
            //  timeout line had fired, so no individual verify was slow - it was all queueing.
            //
            // Every other holder of this lock, in WiseDaq, wraps exactly one DIn or DOut.  This
            //  was the only one that slept, and in the motor-OFF path each millisecond of delay
            //  is 1.8 millidegrees of unwanted travel at slew rate.
            //
            lock (daq._lock)
            {
                daq.wiseBoard.mccBoard.DIn(daq.porttype, out before);
                ushort w = on ? (ushort)(before | mask) : (ushort)(before & ~mask);
                daq.wiseBoard.mccBoard.DOut(daq.porttype, w);
            }

            //
            // The relay is commanded from here on.  Everything below only CONFIRMS it, so it
            //  must not block anybody.
            //
            if (WiseName.StartsWith("Focus"))
            {
                //
                // Somehow the Focus pins (maybe this is specific to the DAQ board) behave
                //  differently from the pins on the other boards.  DON'T do the validation loop.
                //
                return;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            long nextWriteAt = rewriteEveryMillis;
            got = before;

            while (sw.ElapsedMilliseconds < readbackBudgetMillis)
            {
                Thread.Sleep(readbackPollMillis);           // lock NOT held

                lock (daq._lock)
                    daq.wiseBoard.mccBoard.DIn(daq.porttype, out got);

                if ((got & mask) == target)
                {
                    #region debug
                    if (sw.ElapsedMilliseconds > 50)
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            $"{op}: pin {WiseName} took {sw.ElapsedMilliseconds}ms to read back");
                    #endregion
                    return;
                }

                if (sw.ElapsedMilliseconds >= nextWriteAt)
                {
                    //
                    // Re-read and recompute rather than reusing a value from before the wait.
                    //  Splitting the lock means another pin on this port may legitimately have
                    //  moved in between, and rewriting a stale port image would clobber it.  The
                    //  old code sidestepped this only by never letting go of the lock.
                    //
                    lock (daq._lock)
                    {
                        daq.wiseBoard.mccBoard.DIn(daq.porttype, out ushort cur);
                        ushort w = on ? (ushort)(cur | mask) : (ushort)(cur & ~mask);
                        daq.wiseBoard.mccBoard.DOut(daq.porttype, w);
                    }
                    nextWriteAt = sw.ElapsedMilliseconds + rewriteEveryMillis;
                }
            }

            #region debug
            //
            // Say what was actually read, and whether anything ELSE on the port moved - that is
            //  what separates a relay that will not pick up from contention with another pin on
            //  the same port.
            //
            ushort otherBitsChanged = (ushort)((got ^ before) & ~mask);

            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"{op}: pin {WiseName} did not read back {(on ? "On" : "Off")} after " +
                $"{sw.ElapsedMilliseconds}ms: read 0x{got:X4}, our bit {bit} reads " +
                $"{((got & mask) != 0 ? 1 : 0)}" +
                (otherBitsChanged != 0 ? $", OTHER bits changed: 0x{otherBitsChanged:X4}" : ", no other bits moved"));
            #endregion
        }

        public void SetOff()
        {
            if (dir != DigitalPortDirection.DigitalOut)
                return;

            if (!Simulated && _controlled && Hardware.MaintenanceMode)
            {
                //Exceptor.Throw<Hardware.MaintenanceModeException>("SetOff", Const.computerControlAtMaintenance);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WisePin:SetOff: Cannot set OFF - MAINTENANCE mode");
                #endregion
                return;
            }

            DriveAndVerify(on: false, op: "SetOff");
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                SetOff();
                daq.UnsetOwner(bit);

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Const.Direction Direction { get; } = Const.Direction.None;
    }
}