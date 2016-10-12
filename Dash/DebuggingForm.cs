using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Common;

namespace Dash
{
    public partial class DebuggingForm : Form
    {
        private Debugger debugger = Debugger.Instance;
        private bool _updateMessagesWindow = false;

        public DebuggingForm()
        {
            InitializeComponent();
            debugger.SetWindow(listBoxDebugMessages);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxDebugMessages.Items.Clear();
            listBoxDebugMessages.Update();
        }

        private void markToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "=============== Marker ===============");
        }

        private void followToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (_updateMessagesWindow)
            {
                debugger.AppendToWindow(false);
                _updateMessagesWindow = false;
                item.Text = "UpdateWindow";
            } else
            {
                debugger.AppendToWindow(true);
                _updateMessagesWindow = true;
                item.Text = "StopUpdating";
            }
        }

        private void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented  yet!");
        }
    }
}
