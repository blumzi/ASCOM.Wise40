using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        private Dome _dome;

        public SetupDialogForm(Dome dome)
        {
            _dome = dome;
            InitializeComponent();

            _dome.ReadProfile();
            checkBoxTrace.Checked = _dome.traceState;
            checkBoxDebug.Checked = (_dome.debugger.Level != 0);
            checkBoxDebugDevice.Checked = _dome.debugger.Debugging(Debugger.DebugLevel.DebugDevice);
            checkBoxDebugEncoders.Checked = _dome.debugger.Debugging(Debugger.DebugLevel.DebugEncoders);
            checkBoxDebugExceptions.Checked = _dome.debugger.Debugging(Debugger.DebugLevel.DebugExceptions);
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            _dome.traceState = checkBoxTrace.Checked;

            uint level = 0;
            if (checkBoxDebug.Checked)
            {
                if (checkBoxDebugDevice.Checked) level |= (uint)Debugger.DebugLevel.DebugDevice;
                if (checkBoxDebugEncoders.Checked) level |= (uint)Debugger.DebugLevel.DebugEncoders;
                if (checkBoxDebugExceptions.Checked) level |= (uint)Debugger.DebugLevel.DebugExceptions;
            }
            _dome.debugger.Level = level;
            _dome.WriteProfile();
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void checkBoxDebug_CheckedChanged(object sender, EventArgs e)
        {
            List<CheckBox> cbs = new List<CheckBox>() { checkBoxDebugDevice, checkBoxDebugEncoders, checkBoxDebugExceptions };
            CheckBox master = sender as CheckBox;

            foreach (CheckBox cb in cbs)
                cb.AutoCheck = master.Checked;
        }
    }
}