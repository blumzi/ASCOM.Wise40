using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40 //.Dome
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class DomeSetupDialogForm : Form
    {
        private readonly WiseDome wisedome = WiseDome.Instance;

        public DomeSetupDialogForm()
        {
            InitializeComponent();

            wisedome.ReadProfile();
            checkBoxAutoCalibrate.Checked = wisedome._autoCalibrate;
            checkBoxSyncVent.Checked = wisedome.SyncVentWithShutter;
            textBoxShutterIpAddress.Text = wisedome.wisedomeshutter.IpAddress;
            textBoxShutterHighestValue.Text = wisedome.wisedomeshutter.highestRange.ToString();
            textBoxShutterLowestValue.Text = wisedome.wisedomeshutter.lowestRange.ToString();
            checkBoxShutterUseWebClient.Checked = wisedome.wisedomeshutter.ShutterWebClientEnabled;

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                double minimalStep = Convert.ToDouble(driverProfile.GetValue(Const.WiseDriverID.Telescope,
                    Const.ProfileName.Dome_MinimalMovement, string.Empty, "2.0"));
                textBoxMinimalStep.Text = minimalStep.ToString();
            }
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            wisedome.wisedomeshutter.ShutterWebClientEnabled = checkBoxShutterUseWebClient.Checked;
            wisedome.wisedomeshutter.IpAddress = textBoxShutterIpAddress.Text.Trim();
            wisedome.wisedomeshutter.highestRange = Convert.ToInt32(textBoxShutterHighestValue.Text);
            wisedome.wisedomeshutter.lowestRange = Convert.ToInt32(textBoxShutterLowestValue.Text);
            wisedome._autoCalibrate = checkBoxAutoCalibrate.Checked;
            wisedome.SyncVentWithShutter = checkBoxSyncVent.Checked;

            wisedome.WriteProfile();

            using (ASCOM.Utilities.Profile driverProfile = new ASCOM.Utilities.Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(Const.WiseDriverID.Telescope,
                    Const.ProfileName.Dome_MinimalMovement, textBoxMinimalStep.Text);
            }
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

        private void checkBoxAutoCalibrate_CheckedChanged(object sender, EventArgs e)
        {
            wisedome._autoCalibrate = (sender as CheckBox)?.Checked ?? false;
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void DomeSetupDialogForm_Load(object sender, EventArgs e)
        {
        }
    }
}