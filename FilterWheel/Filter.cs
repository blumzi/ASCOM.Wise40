using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40 //.FilterWheel
{
    public class Filter
    {
        private string _name, _desc;
        private int _offset;

        public string FilterName
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

        public string FilterDescription
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

        public int FilterOffset
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
            FilterName = name;
            FilterDescription = desc;
            FilterOffset = offset;
        }
    }
}
