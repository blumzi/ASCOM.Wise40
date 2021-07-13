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
using ASCOM.Wise40.Common;

using Newtonsoft.Json;

namespace Dash
{
    public partial class FiltersForm : Form
    {
        private readonly ASCOM.DriverAccess.FilterWheel _wiseFilterWheel;
        private BindingList<Filter> boundFilters;
        private BindingSource source;
        private readonly WiseFilterWheel.FilterSize _filterSize;

        public FiltersForm(ASCOM.DriverAccess.FilterWheel wiseFilterWheel, WiseFilterWheel.FilterSize filterSize)
        {
            _wiseFilterWheel = wiseFilterWheel;
            _filterSize = filterSize;

            WiseFilterWheel.Wheel wheel = filterSize == WiseFilterWheel.FilterSize.ThreeInch ? WiseFilterWheel.wheel4 : WiseFilterWheel.wheel8;
            boundFilters = new BindingList<Filter>(wheel.GetKnownFilters);
            InitializeComponent();
            labelTitle.Text = string.Format("Wise40 {0}\" filters", _filterSize == WiseFilterWheel.FilterSize.TwoInch ? 2 : 3);
            source = new BindingSource(boundFilters, null);
            dataGridView.DataSource = source;
            dataGridView.AllowUserToAddRows = true;
            dataGridView.AllowUserToDeleteRows = true;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64);
            dataGridView.EnableHeadersVisualStyles = false;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            source.Add(new Filter("", "", 0));
            dataGridView.CurrentCell = dataGridView.Rows[dataGridView.RowCount - 1].Cells[0];
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            var row = dataGridView.CurrentRow;
            if (row == null)    // nothing selected
                return;
            
            var answer = MessageBox.Show(
                string.Format("Do you really want to delete filter #{0}?", row.Index + 1),
                "Just making sure ...",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (answer == DialogResult.Yes)
                dataGridView.Rows.Remove(row);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            List<Filter> filters = new List<Filter>();
            List<string> names = new List<string>();

            foreach (Filter f in source.List)
            {
                if (string.IsNullOrEmpty(f.Name))
                    continue;
                names.Add(f.Name);
                filters.Add(f);
            }

            var dups = names.GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

            if (dups.Count > 0)
            {
                if (dups.Count > 1)
                    MessageBox.Show(string.Format("The names \"{0}\" are duplicated!", string.Join(", ", dups.ToArray())));
                else
                    MessageBox.Show(string.Format("The name \"{0}\" is duplicated!", dups[0]));
                return;
            }

            _wiseFilterWheel.Action("set-filter-inventory", JsonConvert.SerializeObject(new WiseFilterWheel.SetFilterInventoryParam
            {
                FilterSize = _filterSize,
                Filters = filters.ToArray(),
            }));
            Close();
        }

        private void dgGrid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();
            SolidBrush brush = new SolidBrush(Color.DarkOrange);

            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, brush, headerBounds, centerFormat);
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if ((e.Context & DataGridViewDataErrorContexts.Parsing) != 0)
            {
                List<string> columnNames = new List<string>() { "Name", "Description", "Focus offset" };

                MessageBox.Show(string.Format("Bad \"{0}\" for filter #{1}", columnNames[e.ColumnIndex], e.RowIndex + 1));
            }
            e.ThrowException = false;
        }
    }
}
