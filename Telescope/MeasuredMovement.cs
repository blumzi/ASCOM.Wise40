using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    /// <summary>
    /// Calculates the target value to be reached by the respective axis encoder
    /// </summary>
    class MeasuredMovement
    {
        System.Timers.Timer timer;      // used to check if the movement is complete

        /// <summary>
        /// Moves the telescope along an axis by a given angle at a given rate
        /// </summary>
        /// <param name="axis">The axis to move along</param>
        /// <param name="rate">The rate at which to move</param>
        /// <param name="angle">By how much to move.  Can be negative.</param>
        public MeasuredMovement(TelescopeAxes axis, double rate, Angle angle)
        {

        }
    }
}
