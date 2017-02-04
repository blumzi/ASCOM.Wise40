using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class Filter
    {
        public string Name, Description;
        public int Offset;

        public Filter(string name, string desc, int offset)
        {
            Name = name;
            Description = desc;
            Offset = offset;
        }
    }
}
