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
            Color errorColor = Color.Red;
            int i;

            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            i = Convert.ToInt32(textBoxAge.Text);
            if (i < 0)
            {
                textBoxAge.ForeColor = errorColor;
                valid = false;
            } else
            {
                SafetyMonitor.ageMaxSeconds = i;
            }

            wisesafetoopen.cloudsSensor.MaxAsString = (comboBoxCloud.SelectedIndex + 1).ToString();
            wisesafetoopen.rainSensor.MaxAsString = textBoxRain.Text;
            wisesafetoopen.lightSensor.MaxAsString = (comboBoxLight.SelectedIndex + 1).ToString();

            i = Convert.ToInt32(textBoxHumidity.Text);
            if (i >= 0 && i <= 100)
            {
                wisesafetoopen.humiditySensor.MaxAsString = i.ToString();
            }
            else
            {
                textBoxHumidity.ForeColor = errorColor;
                valid = false;
            }
            i = Convert.ToInt32(textBoxWind.Text);
            if (i >= 0)
            {
                wisesafetoopen.windSensor.MaxAsString = i.ToString();
            }
            else
            {
                textBoxWind.ForeColor = errorColor;
                valid = false;
            }

            double deg = Convert.ToDouble(textBoxSunElevation.Text);
            if (deg > 0 || deg < -20)
            {
                textBoxSunElevation.ForeColor = errorColor;
                valid = false;
            }
            else
                wisesafetoopen.sunSensor.MaxAsString = deg.ToString();

            if (!valid)
                return;

            foreach (Sensor s in wisesafetoopen._sensors)
                s.Stop();

            wisesafetoopen.lightSensor.Repeats = Convert.ToInt32(textBoxLightRepeats.Text);
            wisesafetoopen.cloudsSensor.Repeats = Convert.ToInt32(textBoxCloudRepeats.Text);
            wisesafetoopen.windSensor.Repeats = Convert.ToInt32(textBoxWindRepeats.Text);
            wisesafetoopen.humiditySensor.Repeats = Convert.ToInt32(textBoxHumidityRepeats.Text);
            wisesafetoopen.sunSensor.Repeats = Convert.ToInt32(textBoxSunRepeats.Text);
            wisesafetoopen.rainSensor.Repeats = Convert.ToInt32(textBoxRainRepeats.Text);

            wisesafetoopen.lightSensor.Interval = Convert.ToInt32(textBoxLightIntervalSeconds.Text);
            wisesafetoopen.cloudsSensor.Interval = Convert.ToInt32(textBoxCloudIntervalSeconds.Text);
            wisesafetoopen.windSensor.Interval = Convert.ToInt32(textBoxWindIntervalSeconds.Text);
            wisesafetoopen.humiditySensor.Interval = Convert.ToInt32(textBoxHumidityIntervalSeconds.Text);
            wisesafetoopen.sunSensor.Interval = Convert.ToInt32(textBoxSunIntervalSeconds.Text);
            wisesafetoopen.rainSensor.Interval = Convert.ToInt32(textBoxRainIntervalSeconds.Text);

            wisesafetoopen.lightSensor.Enabled = checkBoxLight.Checked;
            wisesafetoopen.cloudsSensor.Enabled = checkBoxCloud.Checked;
            wisesafetoopen.windSensor.Enabled = checkBoxWind.Checked;
            wisesafetoopen.humiditySensor.Enabled = checkBoxHumidity.Checked;
            wisesafetoopen.sunSensor.Enabled = checkBoxSun.Checked;
            wisesafetoopen.rainSensor.Enabled = checkBoxRain.Checked;
            
            wisesafetoopen.WriteProfile();

            foreach (Sensor s in wisesafetoopen._sensors)
                s.Start();
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
            wisesafetoopen.Connected = true;
            wisesafetoopen.ReadProfile();
            
            SensorData.CloudCondition cloudsMax = (SensorData.CloudCondition)
                Enum.Parse(typeof(SensorData.CloudCondition), wisesafetoopen.cloudsSensor.MaxAsString);
            comboBoxCloud.SelectedIndex = (int) cloudsMax - 1;
            textBoxRain.Text = wisesafetoopen.rainSensor.MaxAsString;
            SensorData.DayCondition lightMax = (SensorData.DayCondition)
                Enum.Parse(typeof(SensorData.DayCondition), wisesafetoopen.lightSensor.MaxAsString);
            comboBoxLight.SelectedIndex = (int) lightMax - 1;
            textBoxWind.Text = wisesafetoopen.windSensor.MaxAsString;
            textBoxHumidity.Text = wisesafetoopen.humiditySensor.MaxAsString;
            textBoxAge.Text = wisesafetoopen.ageMaxSeconds.ToString();
            textBoxSunElevation.Text = wisesafetoopen.sunSensor.MaxAsString;
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
