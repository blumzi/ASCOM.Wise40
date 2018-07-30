using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.Wise40.VantagePro
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        WiseVantagePro vantagePro = WiseVantagePro.Instance;

        public SetupDialogForm()
        {
            InitializeComponent();
            vantagePro.ReadProfile();
            if (vantagePro._opMode == WiseVantagePro.OpMode.File)
            {
                radioButtonDataFile.Checked = true;
                opMode = WiseVantagePro.OpMode.File;
            }
            else
            {
                radioButtonSerialPort.Checked = true;
                opMode = WiseVantagePro.OpMode.Serial;
            }
            
            labelReportFileValue.Text = vantagePro.DataFile;
            comboBoxSerialPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (!String.IsNullOrEmpty(vantagePro._portName) && comboBoxSerialPort.Items.Contains(vantagePro._portName))
                comboBoxSerialPort.Text = vantagePro._portName;
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            vantagePro._opMode = radioButtonDataFile.Checked ? WiseVantagePro.OpMode.File : WiseVantagePro.OpMode.Serial;
            vantagePro.DataFile = labelReportFileValue.Text;
            vantagePro._portName = comboBoxSerialPort.Text;
            vantagePro.WriteProfile();
            Close();
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

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {

        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            labelReportFileValue.Text = openFileDialog.FileName;
        }

        private void buttonChoose_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private void radioButtonDataFile_Click(object sender, EventArgs e)
        {
            opMode = WiseVantagePro.OpMode.File;
        }

        private void radioButtonSerialPort_Click(object sender, EventArgs e)
        {
            opMode = WiseVantagePro.OpMode.Serial;
        }
    }
}