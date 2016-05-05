﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public interface IDegrees: IWiseObject
    {
        double Degrees
        {
            get; set;
        }
    }
}
