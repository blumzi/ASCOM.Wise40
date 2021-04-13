using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;

namespace ASCOM.Wise40
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class TelescopeSetupDialogForm : Form
    {
        public TelescopeSetupDialogForm()
        {
            InitializeComponent();

            WiseTele.ReadProfile();
            accuracyBox.SelectedItem = (WiseSite.astrometricAccuracy == Accuracy.Full) ? 0 : 1;
            checkBoxBypassSafety.Checked = WiseTele.BypassCoordinatesSafety;
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            WiseSite.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;
            WiseTele.BypassCoordinatesSafety = checkBoxBypassSafety.Checked;
            WiseTele.WriteProfile();
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {}
    }
}