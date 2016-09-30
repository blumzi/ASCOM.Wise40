using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Hardware
{
    public interface IEncoder : IConnectable, IDegrees, IDisposable, IWiseObject
    {
        uint Value { get; set; }        // from the Daqs or simulated
        Angle Angle { get; set; }
    }
}
