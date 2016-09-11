using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.CloudSensor
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        private ObservingConditions _oc;

        public SetupDialogForm(ObservingConditions oc)
        {
            _oc = oc;

            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            _oc._dataFile = labelDataFileValue.Text;
            _oc._traceState = chkTrace.Checked;
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

        private void InitUI()
        {
            chkTrace.Checked = _oc._traceState;
            labelDataFileValue.Text = _oc._dataFile;          
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            labelDataFileValue.Text = ((OpenFileDialog)sender).FileName;
        }

        private void buttonChooseDataFile_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }
    }
}