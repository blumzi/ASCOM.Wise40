using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ASCOM.Wise40.Hardware
{
    public class WiseBitOwner
    {
        public string owner;
        public System.Windows.Forms.CheckBox checkBox;

        public WiseBitOwner(string o = null, CheckBox cb = null)
        {
            owner = o;
            checkBox = cb;
        }
    }
}
