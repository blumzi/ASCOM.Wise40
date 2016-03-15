using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public class Target
    {
        public DMS Ra { get; set; }
        public DMS Dec { get; set; }
        public DMS Alt { get; set; }
        public DMS Azm { get; set; }
    }
}
