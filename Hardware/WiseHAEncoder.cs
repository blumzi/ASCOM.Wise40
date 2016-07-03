﻿using System;
using System.Collections.Generic;
using MccDaq;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    /// <summary>
    /// Implements the Wise40 HourAngle Encoder interface
    /// </summary>
    public class WiseHAEncoder: IEncoder
    {
        private WiseDaq wormDaqLow, wormDaqHigh;
        private WiseDaq axisDaqLow, axisDaqHigh;
        private List<WiseDaq> daqs;
        private bool _simulated = false;
        private bool _connected = false;
        private string _name;
        private uint _daqsValue;
        //private uint _value_at_fiducial_point = 1433418;

        private AtomicReader wormAtomicReader, axisAtomicReader;

        private Astrometry.NOVAS.NOVAS31 Novas31;
        private Astrometry.AstroUtils.AstroUtils astroutils;

        public Angle _angle;

        const double HaMultiplier = 2 * Math.PI / 720 / 4096;
        const double HaCorrection = -3.063571542;                   // 20081231: Shai Kaspi
        const uint _simulatedValueAtFiducialMark = 1432672;

        private Common.Debugger debugger = new Debugger();

        public WiseHAEncoder(string name)
        {
            Novas31 = new Astrometry.NOVAS.NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();

            List<WiseDaq> wormDaqs, axisDaqs, teleDaqs;

            teleDaqs = Hardware.Instance.teleboard.daqs;

            wormDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.ThirdPortA);
            wormDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortCL);
            axisDaqLow = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortB);
            axisDaqHigh = teleDaqs.Find(x => x.porttype == DigitalPortType.FourthPortA);

            wormDaqs = new List<WiseDaq>();
            wormDaqs.Add(wormDaqLow);
            wormDaqs.Add(wormDaqHigh);

            axisDaqs = new List<WiseDaq>();
            axisDaqs.Add(axisDaqLow);
            axisDaqs.Add(axisDaqHigh);

            daqs = new List<WiseDaq>();
            daqs.AddRange(wormDaqs);
            daqs.AddRange(axisDaqs);
            foreach (WiseDaq daq in daqs)
                daq.setDir(DigitalPortDirection.DigitalIn);

            wormAtomicReader = new AtomicReader(wormDaqs);
            axisAtomicReader = new AtomicReader(axisDaqs);

            simulated = false;
            foreach (WiseDaq d in daqs)
            {
                if (d.wiseBoard.type == WiseBoard.BoardType.Soft)
                {
                    simulated = true;
                    break;
                }
            }
            _name = name;

            _angle = simulated ? new Angle("00h00m00.0s") : Angle.FromRadians((Value * HaMultiplier) + HaCorrection, Angle.Type.RA);

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                //debugger.Level = Convert.ToUInt32(driverProfile.GetValue("ASCOM.Wise40.Telescope", "Debug Level", string.Empty, "0"));
                debugger.Level = (uint)Debugger.DebugLevel.DebugAll;
            }
        }

        /// <summary>
        /// Reads the axis and worm encoders
        /// </summary>
        /// <returns>Combined Daq values</returns>
        public uint Value
        {
            get {
                if (! simulated)
                {
                    List<uint> daqValues;
                    uint worm, axis;

                    daqValues = wormAtomicReader.Values;
                    worm = (daqValues[1] << 8) | daqValues[0];
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "[{0}] Value - HA worm: {1}", this.GetHashCode(), worm);

                    daqValues = axisAtomicReader.Values;
                    axis = (daqValues[0] >> 4) | (daqValues[1] << 4);
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "[{0}] Value - HA axis: {1}", this.GetHashCode(), axis);

                    _daqsValue = ((axis * 720 - worm) & 0xfff000) + worm;
                }
                return _daqsValue;
            }

            set
            {
                if (simulated)
                    _daqsValue = value;
            }
        }

        public Angle Angle
        {
            get
            {
                if (!simulated)
                    _angle.Radians = (Value * HaMultiplier) + HaCorrection;

                return _angle;
            }

            set
            {
                if (simulated)
                    _angle = value;
            }
        }

        public double Degrees
        {
            get
            {
                if (!simulated)
                    _angle.Radians = (_daqsValue * HaMultiplier) + HaCorrection;

                return Angle.Degrees;
            }

            set
            {
                Angle before = _angle;
                _angle.Degrees = value;
                Angle after = _angle;
                Angle delta = after - before;

                if (simulated)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugEncoders, "[{0}] {1}: {2} + {3} = {4}", this.GetHashCode(), name, before, delta, after);
                    _daqsValue = (uint)((_angle.Radians + HaCorrection) / HaMultiplier);
                }
            }
        }

        private double Hours
        {
            get
            {
                return Angle.Hours;
            }
        }

        /// <summary>
        /// Right Ascension in Hours
        /// </summary>
        public Angle RightAscension
        {
            get
            {
                Angle ret = WiseSite.Instance.LocalSiderealTime - Angle.FromHours(Hours, Angle.Type.RA);
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "[{0}] RightAscension: {1}", this.GetHashCode(), ret);

                return ret;
            }
        }

        public void Connect(bool connected)
        {
            if (connected)
            {
                wormDaqLow.setOwners("HAWormLow");
                wormDaqHigh.setOwners("HAWormHigh");
                axisDaqLow.setOwners("HAAxisLow");
                axisDaqHigh.setOwners("HAAxisHigh");
            } else
            {
                wormDaqLow.unsetOwners();
                wormDaqHigh.unsetOwners();
                axisDaqLow.unsetOwners();
                axisDaqHigh.unsetOwners();
            }
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
            foreach (WiseDaq daq in daqs)
                daq.unsetOwners();
        }

        public bool simulated
        {
            get
            {
                return _simulated; 
            }

            set
            {
                _simulated = value;
            }
        }

        public string name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }
    }
}