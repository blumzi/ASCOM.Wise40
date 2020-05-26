using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ASCOM.DeviceInterface;
using System.Collections;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    #region Rate class
    //
    // The Rate class implements IRate, and is used to hold values
    // for AxisRates. You do not need to change this class.
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.Rate
    // The ClassInterface/None addribute prevents an empty interface called
    // _Rate from being created and used as the [default] interface
    //
    [Guid("3bfe88eb-d134-4212-ac5c-171e5e58cccb")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class Rate : ASCOM.DeviceInterface.IRate
    {
        //
        // Default constructor - Internal prevents public creation
        // of instances. These are values for AxisRates.
        //
        internal Rate(double minimum, double maximum)
        {
            this.Maximum = maximum;
            this.Minimum = minimum;
        }

        #region Implementation of IRate

        public void Dispose()
        {
            Exceptor.Throw<System.NotImplementedException>("Dispose", "Not implemented");
        }

        public double Maximum { get; set; } = 0;

        public double Minimum { get; set; } = 0;

        #endregion
    }
    #endregion

    #region AxisRates
    //
    // AxisRates is a strongly-typed collection that must be enumerable by
    // both COM and .NET. The IAxisRates and IEnumerable interfaces provide
    // this polymorphism. 
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.AxisRates
    // The ClassInterface/None addribute prevents an empty interface called
    // _AxisRates from being created and used as the [default] interface
    //
    [Guid("ca9b93c6-6c3d-4ab6-a128-02c9b5307c3b")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class AxisRates : IAxisRates, IEnumerable
    {
        private readonly Rate[] rates;

        //
        // Constructor - Internal prevents public creation
        // of instances. Returned by Telescope.AxisRates.
        //
        internal AxisRates(TelescopeAxes axis)
        {
            //
            // This collection must hold zero or more Rate objects describing the 
            // rates of motion ranges for the Telescope.MoveAxis() method
            // that are supported by your driver. It is OK to leave this 
            // array empty, indicating that MoveAxis() is not supported.
            //
            // Note that we are constructing a rate array for the axis passed
            // to the constructor. Thus we switch() below, and each case should 
            // initialize the array for the rate for the selected axis.
            //
            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    // TODO Initialize this array with any Primary axis rates that your driver may provide
                    // Example: m_Rates = new Rate[] { new Rate(10.5, 30.2), new Rate(54.0, 43.6) }
                    this.rates = new Rate[] {
                        new Rate(Const.rateGuide, Const.rateGuide),
                        new Rate(Const.rateSet, Const.rateSet),
                        new Rate(Const.rateSlew, Const.rateSlew),
                    };
                    break;

                case TelescopeAxes.axisSecondary:
                    // TODO Initialize this array with any Secondary axis rates that your driver may provide
                    this.rates = new Rate[] {
                        new Rate(Const.rateGuide, Const.rateGuide),
                        new Rate(Const.rateSet, Const.rateSet),
                        new Rate(Const.rateSlew, Const.rateSlew),
                     };
                    break;

                case TelescopeAxes.axisTertiary:
                    this.rates = Array.Empty<Rate>();
                    break;
            }
        }

        #region IAxisRates Members

        public int Count
        {
            get { return this.rates.Length; }
        }

        public void Dispose()
        {
            return;
        }

        public IEnumerator GetEnumerator()
        {
            return rates.GetEnumerator();
        }

        public IRate this[int index]
        {
            get { return this.rates[index - 1]; }	// 1-based
        }

        #endregion
    }
    #endregion

    #region TrackingRates
    //
    // TrackingRates is a strongly-typed collection that must be enumerable by
    // both COM and .NET. The ITrackingRates and IEnumerable interfaces provide
    // this polymorphism. 
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.TrackingRates
    // The ClassInterface/None addribute prevents an empty interface called
    // _TrackingRates from being created and used as the [default] interface
    //
    [Guid("39d23006-bf9d-4738-8d2c-0f6494486b93")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class TrackingRates : ITrackingRates, IEnumerable, IEnumerator
    {
        private readonly DriveRates[] trackingRates;
        private static int pos = -1;

        //
        // Default constructor - Internal prevents public creation
        // of instances. Returned by Telescope.AxisRates.
        //
        internal TrackingRates()
        {
            //
            // This array must hold ONE or more DriveRates values, indicating
            // the tracking rates supported by your telescope. The one value
            // (tracking rate) that MUST be supported is driveSidereal!
            //
            this.trackingRates = new[] { DriveRates.driveSidereal };
            // TODO Initialize this array with any additional tracking rates that your driver may provide
        }

        #region ITrackingRates Members

        public int Count
        {
            get { return this.trackingRates.Length; }
        }

        public IEnumerator GetEnumerator()
        {
            pos = -1;
            return this as IEnumerator;
        }

        public void Dispose()
        {
            Exceptor.Throw<System.NotImplementedException>("Dispose", "Not implemented");
        }

        public DriveRates this[int index]
        {
            get { return this.trackingRates[index - 1]; }   // 1-based
        }

        #endregion

        #region IEnumerable members

        public object Current
        {
            get
            {
                if (pos < 0 || pos >= trackingRates.Length)
                {
                    Exceptor.Throw<System.NotImplementedException>("Current", $"pos must be >= 0 and <= {trackingRates.Length}");
                }
                return trackingRates[pos];
            }
        }

        public bool MoveNext()
        {
            return ++pos < trackingRates.Length;
        }

        public void Reset()
        {
            pos = -1;
        }
        #endregion
    }
    #endregion
}
