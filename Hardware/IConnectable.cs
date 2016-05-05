using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.Wise40.Hardware
{
    public interface IConnectable
    {
        void Connect(bool connected);
        bool Connected
        {
            get;
        }
    }
}
