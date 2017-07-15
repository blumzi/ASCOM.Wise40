using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        ObsMon obsmon = ObsMon.Instance;

        public SetupDialogForm()
        {
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            ObsMon._interval = Convert.ToInt32(textBoxMonitoringFrequency.Text) * 1000;
            obsmon._cloudMaxEvents = Convert.ToInt32(textBoxCloudsEvents.Text);
            obsmon._humidityMaxEvents = Convert.ToInt32(textBoxHumidityEvents.Text);
            obsmon._windMaxEvents = Convert.ToInt32(textBoxWindEvents.Text);
            obsmon._rainMaxEvents = Convert.ToInt32(textBoxRainEvents.Text);
            obsmon._sunMaxEvents = Convert.ToInt32(textBoxSunEvents.Text);
            obsmon._lightMaxEvents = Convert.ToInt32(textBoxLightEvents.Text);
            obsmon.WriteProfile();

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
            textBoxCloudsEvents.Text = obsmon._cloudMaxEvents.ToString();
            textBoxHumidityEvents.Text = obsmon._humidityMaxEvents.ToString();
            textBoxLightEvents.Text = obsmon._lightMaxEvents.ToString();
            textBoxRainEvents.Text = obsmon._rainMaxEvents.ToString();
            textBoxSunEvents.Text = obsmon._sunMaxEvents.ToString();
            textBoxWindEvents.Text = obsmon._windMaxEvents.ToString();
            textBoxMonitoringFrequency.Text = (ObsMon._interval / 1000).ToString();
        }
    }
}
