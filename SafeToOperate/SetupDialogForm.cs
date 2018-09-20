using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
//using ASCOM.Wise40.SafeToOperate;
using ASCOM.Wise40.Boltwood;

namespace ASCOM.Wise40SafeToOperate
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SafeToOperateSetupDialogForm : Form
    {
        WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.Instance;

        public SafeToOperateSetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            
            InitUI();
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
                WiseSafeToOperate.ageMaxSeconds = i;
            }

            i = Convert.ToInt32(textBoxRestoreSafety.Text);
            if (i < 0)
            {
                textBoxRestoreSafety.ForeColor = errorColor;
                valid = false;
            }
            else
            {
                WiseSafeToOperate._stabilizationPeriod = new TimeSpan(0, Convert.ToInt32(textBoxRestoreSafety.Text), 0);
            }

            WiseSafeToOperate.cloudsSensor.MaxAsString = textBoxCloudCoverPercent.Text;
            WiseSafeToOperate.rainSensor.MaxAsString = textBoxRain.Text;

            i = Convert.ToInt32(textBoxHumidity.Text);
            if (i >= 0 && i <= 100)
            {
                WiseSafeToOperate.humiditySensor.MaxAsString = i.ToString();
            }
            else
            {
                textBoxHumidity.ForeColor = errorColor;
                valid = false;
            }

            i = Convert.ToInt32(textBoxWind.Text);
            if (i >= 0)
            {
                WiseSafeToOperate.windSensor.MaxAsString = i.ToString();
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
                WiseSafeToOperate.sunSensor.MaxAsString = deg.ToString();

            if (!valid)
                return;

            foreach (Sensor s in WiseSafeToOperate._sensors)
                s.Stop();

            WiseSafeToOperate.cloudsSensor._repeats = Convert.ToInt32(textBoxCloudRepeats.Text);
            WiseSafeToOperate.cloudsSensor._intervalMillis = Convert.ToInt32(textBoxCloudIntervalSeconds.Text);
            WiseSafeToOperate.cloudsSensor._enabled = checkBoxCloud.Checked;

            WiseSafeToOperate.windSensor._repeats = Convert.ToInt32(textBoxWindRepeats.Text);
            WiseSafeToOperate.windSensor._intervalMillis = Convert.ToInt32(textBoxWindIntervalSeconds.Text);
            WiseSafeToOperate.windSensor._enabled = checkBoxWind.Checked;

            WiseSafeToOperate.humiditySensor._repeats = Convert.ToInt32(textBoxHumidityRepeats.Text);
            WiseSafeToOperate.humiditySensor._intervalMillis = Convert.ToInt32(textBoxHumidityIntervalSeconds.Text);
            WiseSafeToOperate.humiditySensor._enabled = checkBoxHumidity.Checked;

            WiseSafeToOperate.sunSensor._repeats = Convert.ToInt32(textBoxSunRepeats.Text);
            WiseSafeToOperate.sunSensor._intervalMillis = Convert.ToInt32(textBoxSunIntervalSeconds.Text);
            WiseSafeToOperate.sunSensor._enabled = checkBoxSun.Checked;

            WiseSafeToOperate.rainSensor._repeats = Convert.ToInt32(textBoxRainRepeats.Text);
            WiseSafeToOperate.rainSensor._intervalMillis = Convert.ToInt32(textBoxRainIntervalSeconds.Text);
            WiseSafeToOperate.rainSensor._enabled = checkBoxRain.Checked;            
            
            wisesafetooperate.WriteProfile();

            foreach (Sensor s in WiseSafeToOperate._sensors)
                s.Restart(0);

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
            wisesafetooperate.Connected = true;

            textBoxCloudCoverPercent.Tag = textBoxCloudCoverPercent.Text = WiseSafeToOperate.cloudsSensor.MaxAsString;
            textBoxRain.Tag = textBoxRain.Text = WiseSafeToOperate.rainSensor.MaxAsString;
            textBoxWind.Tag = textBoxWind.Text = WiseSafeToOperate.windSensor.MaxAsString;
            textBoxHumidity.Tag = textBoxHumidity.Text = WiseSafeToOperate.humiditySensor.MaxAsString;
            textBoxSunElevation.Tag = textBoxSunElevation.Text = WiseSafeToOperate.sunSensor.MaxAsString;

            textBoxAge.Text = WiseSafeToOperate.ageMaxSeconds.ToString();
            textBoxRestoreSafety.Tag = textBoxRestoreSafety.Text = WiseSafeToOperate._stabilizationPeriod.Minutes.ToString();

            checkBoxCloud.Tag = checkBoxCloud.Checked = WiseSafeToOperate.cloudsSensor._enabled;
            checkBoxHumidity.Tag = checkBoxHumidity.Checked = WiseSafeToOperate.humiditySensor._enabled;
            checkBoxRain.Tag = checkBoxRain.Checked = WiseSafeToOperate.rainSensor._enabled;
            checkBoxSun.Tag = checkBoxSun.Checked = WiseSafeToOperate.sunSensor._enabled;
            checkBoxWind.Tag = checkBoxWind.Checked = WiseSafeToOperate.windSensor._enabled;

            textBoxCloudIntervalSeconds.Tag = textBoxCloudIntervalSeconds.Text = WiseSafeToOperate.cloudsSensor._intervalMillis.ToString();
            textBoxHumidityIntervalSeconds.Tag = textBoxHumidityIntervalSeconds.Text = WiseSafeToOperate.humiditySensor._intervalMillis.ToString();
            textBoxRainIntervalSeconds.Tag = textBoxRainIntervalSeconds.Text = WiseSafeToOperate.rainSensor._intervalMillis.ToString();
            textBoxSunIntervalSeconds.Tag = textBoxSunIntervalSeconds.Text = WiseSafeToOperate.sunSensor._intervalMillis.ToString();
            textBoxWindIntervalSeconds.Tag = textBoxWindIntervalSeconds.Text = WiseSafeToOperate.windSensor._intervalMillis.ToString();

            textBoxCloudRepeats.Tag = textBoxCloudRepeats.Text = WiseSafeToOperate.cloudsSensor._repeats.ToString();
            textBoxHumidityRepeats.Tag = textBoxHumidityRepeats.Text = WiseSafeToOperate.humiditySensor._repeats.ToString();
            textBoxRainRepeats.Tag = textBoxRainRepeats.Text = WiseSafeToOperate.rainSensor._repeats.ToString();
            textBoxSunRepeats.Tag = textBoxSunRepeats.Text = WiseSafeToOperate.sunSensor._repeats.ToString();
            textBoxWindRepeats.Tag = textBoxWindRepeats.Text = WiseSafeToOperate.windSensor._repeats.ToString();

            toolTip1.SetToolTip(textBoxHumidity, "0 to 100 (%)");
            toolTip1.SetToolTip(textBoxAge, "0 to use data of any age (sec)");
            toolTip1.SetToolTip(textBoxHumidity, "between 0 and 100 (%)");
            toolTip1.SetToolTip(textBoxWind, "greter than 0 (mps)");
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
