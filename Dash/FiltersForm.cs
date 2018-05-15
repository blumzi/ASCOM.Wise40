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
using ASCOM.Wise40.FilterWheel;

namespace Dash
{
    public partial class FiltersForm : Form
    {
        private WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;
        BindingList<Filter> boundFilters;
        BindingSource source;
        Debugger debugger = Debugger.Instance;
        private int _filterSize;

        //static public class Util
        //{
        //    static public T Find<T>(Control container) where T : Control
        //    {
        //        foreach (Control child in container.Controls)
        //            return (child is T ? (T)child : Find<T>(child));
        //        // Not found.
        //        return null;
        //    }
        //}

        public FiltersForm(int filterSize)
        {
            _filterSize = filterSize;

            boundFilters = new BindingList<Filter>(WiseFilterWheel.filterInventory[_filterSize]);
            ReadProfile();
            InitializeComponent();
            labelTitle.Text = string.Format("Wise40 {0}\" filters", _filterSize);
            source = new BindingSource(boundFilters, null);
            dataGridView.DataSource = source;
            dataGridView.AllowUserToAddRows = true;
            dataGridView.AllowUserToDeleteRows = true;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64);
            dataGridView.EnableHeadersVisualStyles = false;
        }

        void ReadProfile()
        {
            WiseFilterWheel.ReadProfile();
        }

        void WriteProfile()
        {
            WiseFilterWheel.WriteProfile();
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
                if (f.FilterName == string.Empty)
                    continue;
                names.Add(f.FilterName);
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

            WiseFilterWheel.filterInventory[_filterSize] = filters;
            WiseFilterWheel.SaveFiltersToCsvFile(_filterSize);
            WriteProfile();
            Close();
        }

        private void dgGrid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();
            Color color = Color.DarkOrange;
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
                DataGridView view = sender as DataGridView;
                List<string> columnNames = new List<string>() { "Name", "Description", "Focus offset" };

                MessageBox.Show(string.Format("Bad \"{0}\" for filter #{1}", columnNames[e.ColumnIndex], e.RowIndex + 1));
            }
            e.ThrowException = false;
        }
    }
}
