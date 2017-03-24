using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public interface IOnOff
    {
        bool isOff { get; }
        bool isOn { get; }
        void SetOn();
        void SetOff();
    }
}
