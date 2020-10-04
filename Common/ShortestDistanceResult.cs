using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{

    public class ShortestDistanceResult
    {
        public Angle angle;
        public Const.AxisDirection direction;

        public ShortestDistanceResult(Angle a, Const.AxisDirection d)
        {
            angle = a;
            direction = d;
        }

        public ShortestDistanceResult()
        {
            angle = Angle.Invalid;
            direction = Const.AxisDirection.None;
        }
    }
}
