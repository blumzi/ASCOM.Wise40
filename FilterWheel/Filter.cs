using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40
{
    public class Filter
    {
        private string _name, _desc;
        private int _offset;

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public string Description
        {
            get
            {
                return _desc;
            }

            set
            {
                _desc = value;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                _offset = value;
            }
        }

        public Filter(string name, string desc, int offset)
        {
            Name = name;
            Description = desc;
            Offset = offset;
        }
    }
}
