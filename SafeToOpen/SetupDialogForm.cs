using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.Wise40.SafeToOperate;
using ASCOM.Wise40.Boltwood;

namespace ASCOM.Wise40.SafeToOperate
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        WiseSafeToOperate wisesafetoopen = WiseSafeToOperate.InstanceOpen;

        public SetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
            toolTip1.SetToolTip(textBoxHumidity, "0 to 100 (%)");
            toolTip1.SetToolTip(textBoxAge, "0 to use data of any age (sec)");
            toolTip1.SetToolTip(textBoxHumidity, "between 0 and 100 (%)");
            toolTip1.SetToolTip(textBoxWind, "greter than 0 (mps)");
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            bool valid = true;
            Color badColor = Color.Red;
            int i;

            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            i = Convert.ToInt32(textBoxAge.Text);
            if (i < 0)
            {
                textBoxAge.ForeColor = badColor;
                valid = false;
            } else
            {
                SafetyMonitor.ageMaxSeconds = i;
            }
            wisesafetoopen.cloudsMaxEnum = (Boltwood.SensorData.CloudCondition)comboBoxCloud.SelectedIndex;
            wisesafetoopen.cloudsMaxValue = Boltwood.SensorData.doubleCloudCondition[wisesafetoopen.cloudsMaxEnum];

            wisesafetoopen.rainMax = comboBoxRain.SelectedIndex;

            wisesafetoopen.lightMaxEnum = (Boltwood.SensorData.DayCondition)comboBoxLight.SelectedIndex;
            wisesafetoopen.lightMaxValue = (int)wisesafetoopen.lightMaxEnum;

            i = Convert.ToInt32(textBoxHumidity.Text);
            if (i >= 0 && i <= 100)
            {
                SafetyMonitor.humidityMax = i;
            }
            else
            {
                textBoxHumidity.ForeColor = badColor;
                valid = false;
            }
            i = Convert.ToInt32(textBoxWind.Text);
            if (i >= 0)
            {
                SafetyMonitor.windMax = i;
            }
            else
            {
                textBoxWind.ForeColor = badColor;
                valid = false;
            }

            if (valid)
                wisesafetoopen.WriteProfile();
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
            wisesafetoopen.ReadProfile();
            
            comboBoxCloud.SelectedIndex = (int)wisesafetoopen.cloudsMaxEnum;
            comboBoxRain.SelectedIndex = (int)wisesafetoopen.rainMax;
            comboBoxLight.SelectedIndex = (int)wisesafetoopen.lightMaxEnum;
            textBoxWind.Text = wisesafetoopen.windMax.ToString();
            textBoxHumidity.Text = wisesafetoopen.humidityMax.ToString();
            textBoxAge.Text = wisesafetoopen.ageMaxSeconds.ToString();
        }

        private void textBoxWind_Validating(object sender, CancelEventArgs e)
        {
            if (int.Parse(((TextBox)sender).Text) < 0)
            {
                ((TextBox)sender).ForeColor = Color.Red;
            } else
            {
                ((TextBox)sender).ForeColor = Color.DarkOrange;
            }
            base.OnTextChanged(e);
        }

        private void textBoxHumidity_Validating(object sender, CancelEventArgs e)
        {
            int percent = int.Parse(((TextBox)sender).Text);
            if (percent < 0 || percent > 100)
            {
                ((TextBox)sender).ForeColor = Color.Red;
            }
            else
            {
                ((TextBox)sender).ForeColor = Color.DarkOrange;
            }
            base.OnTextChanged(e);
        }

        private void textBoxAge_Validating(object sender, CancelEventArgs e)
        {
            if (int.Parse(((TextBox)sender).Text) < 0)
            {
                ((TextBox)sender).ForeColor = Color.Red;
            }
            else
            {
                ((TextBox)sender).ForeColor = Color.DarkOrange;
            }
            base.OnTextChanged(e);
        }
    }
}
