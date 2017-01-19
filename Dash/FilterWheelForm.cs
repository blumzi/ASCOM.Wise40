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

namespace Dash
{
    public partial class FilterWheelForm : Form
    {
        private WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;
        private Statuser filterWheelStatus;

        private string FilterName(string s)
        {
            return string.IsNullOrEmpty(s) ? "Clear" : s;
        }

        private void RefreshWheelInfo()
        {
            labelCurrentWheelValue.Text = string.IsNullOrEmpty(WiseFilterWheel.currentWheel.name) ? "Unknown" : WiseFilterWheel.currentWheel.name;
            short position = wisefilterwheel.Position;

            if (position == -1)
            {
                labelCurrentPositionValue.Text = "Unknown";
                tableLayoutPanelWheel8.Visible = false;
                tableLayoutPanelWheel4.Visible = false;
                return;
            }

            labelCurrentPositionValue.Text = (position + 1).ToString();

            int nFilters = wisefilterwheel.Positions;
            TableLayoutPanel table = (nFilters == 8) ? tableLayoutPanelWheel8 : tableLayoutPanelWheel4;
            TableLayoutPanel otherTable = (table == tableLayoutPanelWheel8) ? tableLayoutPanelWheel4 : tableLayoutPanelWheel8;

            for (int i = 0; i < nFilters; i++)
            {
                Label label = (Label)table.Controls.Find(string.Format("label{0}Filter{1}", WiseFilterWheel.currentWheel.name, i), true)[0];
                label.Text = FilterName(wisefilterwheel.Names[i]);
                label.ForeColor = (i == position) ? Color.DarkOrange : Color.FromArgb(176, 161, 142);
            }
            table.Visible = true;
            otherTable.Visible = false;
        }

        public FilterWheelForm()
        {
            InitializeComponent();
            filterWheelStatus = new Statuser(labelFilterWheelStatus);
            
            wisefilterwheel.init();

            if (!wisefilterwheel.Connected)
                wisefilterwheel.Connected = true;
        }

        private void buttonIdentify_Click(object sender, EventArgs e)
        {

        }

        private void buttonGoTo_Click(object sender, EventArgs e)
        {
            short targetPosition = -1;
            try
            {
                targetPosition = (short)Convert.ToInt32(textBoxPositionValue.Text);
            }
            catch
            {
                filterWheelStatus.Show("Invalid position", 1000, Statuser.Severity.Error);
                return;
            }

            if (targetPosition < 1 || targetPosition > wisefilterwheel.Positions)
            {
                filterWheelStatus.Show(string.Format("Invalid position: {0}", targetPosition), 1000, Statuser.Severity.Error);
                return;
            }

            string filterName = FilterName(wisefilterwheel.Names[targetPosition - 1]);
            filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition, filterName));
            wisefilterwheel.Position = (short) (targetPosition - 1);
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            short currentPosition = wisefilterwheel.Position;
            short targetPosition = (short) ((currentPosition == 0) ? wisefilterwheel.Positions - 1 : currentPosition - 1);

            string filterName = FilterName(wisefilterwheel.Names[targetPosition]);
            filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition + 1, filterName));
            wisefilterwheel.Position = targetPosition;
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            short currentPosition = wisefilterwheel.Position;
            short targetPosition = (short)((currentPosition == wisefilterwheel.Positions - 1) ? 0 : currentPosition + 1);

            string filterName = FilterName(wisefilterwheel.Names[targetPosition]);
            filterWheelStatus.Show(string.Format("Moving to position {0} ({1})", targetPosition + 1, filterName));
            wisefilterwheel.Position = targetPosition;
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
