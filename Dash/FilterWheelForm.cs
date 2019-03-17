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
    public partial class FilterWheelForm : Form
    {
        private ASCOM.DriverAccess.FilterWheel _wiseFilterWheel;
        private Statuser _filterWheelStatus;
        private WiseFilterWheel.Wheel.WheelDigest _currentWheel;

        private string FilterName(string s)
        {
            return string.IsNullOrEmpty(s) ? "Clear" : s;
        }

        private void RefreshWheelInfo()
        {
           _currentWheel = JsonConvert.DeserializeObject<WiseFilterWheel.Wheel.WheelDigest>(_wiseFilterWheel.Action("current-wheel", ""));

            labelCurrentWheelValue.Text = _currentWheel.Name;
            short position = _currentWheel.CurrentPosition;

            if (position == -1)
            {
                labelCurrentPositionValue.Text = "Unknown";
                tableLayoutPanelWheel8.Visible = false;
                tableLayoutPanelWheel4.Visible = false;
                return;
            }

            labelCurrentPositionValue.Text = (position + 1).ToString();

            int nFilters = _currentWheel.Npositions;
            TableLayoutPanel table = (nFilters == 8) ? tableLayoutPanelWheel8 : tableLayoutPanelWheel4;
            TableLayoutPanel otherTable = (table == tableLayoutPanelWheel8) ? tableLayoutPanelWheel4 : tableLayoutPanelWheel8;

            for (int i = 0; i < nFilters; i++)
            {
                Label label = (Label)table.Controls.Find(string.Format("label{0}Filter{1}", _currentWheel.Name, i), true)[0];
                label.Text = FilterName(_currentWheel.Filters[i].Name);
                label.ForeColor = (i == position) ? Color.DarkOrange : Color.FromArgb(176, 161, 142);
            }
            table.Visible = true;
            otherTable.Visible = false;
        }

        public FilterWheelForm(ASCOM.DriverAccess.FilterWheel wiseFilterWheel)
        {
            _wiseFilterWheel = wiseFilterWheel;
            _filterWheelStatus = new Statuser(labelFilterWheelStatus);
            InitializeComponent();
        }

        private void buttonIdentify_Click(object sender, EventArgs e)
        {

        }

        private void buttonGoTo_Click(object sender, EventArgs e)
        {
            short targetPosition = -1;
            try
            {
                targetPosition = (short)Convert.ToInt32(textBoxPositionValue);
            }
            catch
            {
                _filterWheelStatus.Show("Invalid position", 1000, Statuser.Severity.Error);
                return;
            }

            if (targetPosition < 1 || targetPosition > _currentWheel.Npositions)
            {
                _filterWheelStatus.Show(string.Format("Invalid position: {0}", targetPosition), 1000, Statuser.Severity.Error);
                return;
            }

            string filterName = FilterName(_currentWheel.Filters[targetPosition - 1].Name);
            _filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition, filterName));
            _wiseFilterWheel.Position = (short) (targetPosition - 1);
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            short currentPosition = _currentWheel.CurrentPosition;
            short targetPosition = (short) ((currentPosition == 0) ? _currentWheel.Npositions - 1 : currentPosition - 1);

            string filterName = FilterName(_currentWheel.Filters[targetPosition].Name);
            _filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition + 1, filterName));
            _wiseFilterWheel.Position = targetPosition;
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            short currentPosition = _currentWheel.CurrentPosition;
            short targetPosition = (short)((currentPosition == _currentWheel.Npositions - 1) ? 0 : currentPosition + 1);

            string filterName = FilterName(_currentWheel.Filters[targetPosition].Name);
            _filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition + 1, filterName));
            _wiseFilterWheel.Position = targetPosition;
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            RefreshWheelInfo();
        }

        private void FilterWheelForm_VisibleChanged(object sender, EventArgs e)
        {
            timerRefresh.Enabled = Visible ? true : false;
        }
    }
}
