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
            labelReportFileValue.Text = vantagePro.DataFile;
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            vantagePro.DataFile = labelReportFileValue.Text;
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
    }
}