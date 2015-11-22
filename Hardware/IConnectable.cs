using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.WiseHardware
{
    public interface IConnectable
    {
        void Connect(bool connected);
    }
}
