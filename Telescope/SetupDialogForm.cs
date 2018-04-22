using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;


namespace ASCOM.Wise40 //.Telescope
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class TelescopeSetupDialogForm : Form
    {
        private static WiseSite wisesite = WiseSite.Instance;
        private static WiseTele wisetele = WiseTele.Instance;

        public TelescopeSetupDialogForm(bool traceState, Debugger.DebugLevel debugLevel, Accuracy accuracy, bool enslaveDome)
        {
            InitializeComponent();
            wisetele.init();
            wisesite.init();
            
            accuracyBox.SelectedItem = (wisesite.astrometricAccuracy == Accuracy.Full) ? 0 : 1;
            checkBoxEnslaveDome.Checked = wisetele._enslaveDome;
            checkBoxCalculateRefraction.Checked = wisetele._calculateRefraction;
            checkBoxBypassSafety.Checked = wisetele.BypassSafety;
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            wisesite.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;
            wisetele._enslaveDome = checkBoxEnslaveDome.Checked;
            wisetele._calculateRefraction = checkBoxCalculateRefraction.Checked;
            wisetele.BypassSafety = checkBoxBypassSafety.Checked;
            wisetele.WriteProfile();
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
    }
}