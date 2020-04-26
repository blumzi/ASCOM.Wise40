using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class ObservatoryMonitorSetupDialogForm : Form
    {
        private ObsMainForm _mainForm;

        public ObservatoryMonitorSetupDialogForm(ObsMainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            _mainForm.MinutesBetweenChecks = Convert.ToInt32(textBoxMonitoringFrequency.Text);
            ObsMainForm.MinutesToIdle = Convert.ToInt32(textBoxIdleAfterMinutes.Text);
            _mainForm.WriteProfile();
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

        private void InitUI()
        {
            _mainForm.ReadProfile();
            textBoxMonitoringFrequency.Text = _mainForm.MinutesBetweenChecks.ToString();
            textBoxIdleAfterMinutes.Text = ObsMainForm.MinutesToIdle.ToString();
        }
    }
}
