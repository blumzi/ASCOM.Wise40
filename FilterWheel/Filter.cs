using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class Filter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Offset { get; set; }
        public string Comment { get; set; }

        public Filter(string name, string desc, int offset, string comment = null)
        {
            Name = name;
            Description = desc;
            Offset = offset;
            Comment = comment;
        }
    }
}
