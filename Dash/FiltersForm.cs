using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40;

namespace Dash
{
    public partial class FiltersForm : Form
    {
        private WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;

        public FiltersForm()
        {
            ReadProfile();
            InitializeComponent();
            var bindingList = new BindingList<Filter>(WiseFilterWheel.filterInventory);
            var source = new BindingSource(bindingList, null);
            dataGridView.DataSource = source;
        }

        void ReadProfile()
        {
            WiseFilterWheel.ReadProfile();
        }

        void WriteProfile()
        {
            WiseFilterWheel.WriteProfile();
        }
    }
}
