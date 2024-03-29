using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
//using ASCOM.Wise40.SafeToOperate;
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.TessW;

namespace ASCOM.Wise40SafeToOperate
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SafeToOperateSetupDialogForm : Form
    {
        private readonly WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.Instance;

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
            int i = Convert.ToInt32(textBoxAge.Text);

            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
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

            double deg = Convert.ToDouble(textBoxSunElevationAtDawn.Text);
            if (deg > WiseSafeToOperate.sunSensor.MaxSettableElevation || deg < WiseSafeToOperate.sunSensor.MinSettableElevation)
            {
                textBoxSunElevationAtDawn.ForeColor = errorColor;
                valid = false;
            }
            else
            {
                WiseSafeToOperate.sunSensor.MaxAtDawnAsString = deg.ToString();
            }

            deg = Convert.ToDouble(textBoxSunElevationAtDusk.Text);
            if (deg > WiseSafeToOperate.sunSensor.MaxSettableElevation || deg < WiseSafeToOperate.sunSensor.MinSettableElevation)
            {
                textBoxSunElevationAtDusk.ForeColor = errorColor;
                valid = false;
            }
            else
            {
                WiseSafeToOperate.sunSensor.MaxAtDuskAsString = deg.ToString();
            }

            if (!valid)
                return;

            foreach (Sensor s in WiseSafeToOperate._cumulativeSensors)
                s.Enabled = false;

            bool chk = checkBoxTessWEnabled.Checked;
            var tessW = WiseTessW.Instance;
            tessW.Enabled = chk;
            tessW.WriteProfile();
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.TessW, Const.ProfileName.TessW_Enabled, chk.ToString());
            }

            chk = checkBoxOWLEnabled.Checked;
            var owlRefresher = WiseSafeToOperate.owlRefresher;
            owlRefresher.Enabled = chk;
            owlRefresher.WriteProfile();

            chk = checkBoxARDOEnabled.Checked;
            var ardoRefresher = WiseSafeToOperate.ardoSensor;
            ardoRefresher.Enabled = chk;
            ardoRefresher.WriteProfile();

            WiseSafeToOperate.cloudsSensor._repeats = Convert.ToInt32(textBoxCloudRepeats.Text);
            WiseSafeToOperate.cloudsSensor.Interval = TimeSpan.FromSeconds(Convert.ToInt32(textBoxCloudIntervalSeconds.Text));
            WiseSafeToOperate.cloudsSensor.Enabled = checkBoxCloud.Checked;

            WiseSafeToOperate.windSensor._repeats = Convert.ToInt32(textBoxWindRepeats.Text);
            WiseSafeToOperate.windSensor.Interval = TimeSpan.FromSeconds(Convert.ToInt32(textBoxWindIntervalSeconds.Text));
            WiseSafeToOperate.windSensor.Enabled = checkBoxWind.Checked;

            WiseSafeToOperate.humiditySensor._repeats = Convert.ToInt32(textBoxHumidityRepeats.Text);
            WiseSafeToOperate.humiditySensor.Interval = TimeSpan.FromSeconds(Convert.ToInt32(textBoxHumidityIntervalSeconds.Text));
            WiseSafeToOperate.humiditySensor.Enabled = checkBoxHumidity.Checked;

            WiseSafeToOperate.sunSensor.Interval = TimeSpan.FromSeconds(Convert.ToInt32(textBoxSunIntervalSeconds.Text));
            WiseSafeToOperate.sunSensor.Enabled = checkBoxSun.Checked;

            WiseSafeToOperate.rainSensor._repeats = Convert.ToInt32(textBoxRainRepeats.Text);
            WiseSafeToOperate.rainSensor.Interval = TimeSpan.FromSeconds(Convert.ToInt32(textBoxRainIntervalSeconds.Text));
            WiseSafeToOperate.rainSensor.Enabled = checkBoxRain.Checked;

            WiseSafeToOperate.doorLockSensor.MaxAsString = textBoxDoorLockDelay.Text;

            wisesafetooperate.WriteProfile();

            foreach (Sensor s in WiseSafeToOperate._cumulativeSensors)
                s.Restart();

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
            textBoxSunElevationAtDawn.Tag = textBoxSunElevationAtDawn.Text = WiseSafeToOperate.sunSensor.MaxAtDawnAsString;
            textBoxSunElevationAtDusk.Tag = textBoxSunElevationAtDusk.Text = WiseSafeToOperate.sunSensor.MaxAtDuskAsString;
            textBoxDoorLockDelay.Text = WiseSafeToOperate.doorLockSensor.MaxAsString;

            textBoxAge.Text = WiseSafeToOperate.ageMaxSeconds.ToString();
            textBoxRestoreSafety.Tag = textBoxRestoreSafety.Text = WiseSafeToOperate._stabilizationPeriod.Minutes.ToString();

            checkBoxCloud.Tag = checkBoxCloud.Checked = WiseSafeToOperate.cloudsSensor.Enabled;
            checkBoxHumidity.Tag = checkBoxHumidity.Checked = WiseSafeToOperate.humiditySensor.Enabled;
            checkBoxRain.Tag = checkBoxRain.Checked = WiseSafeToOperate.rainSensor.Enabled;
            checkBoxSun.Tag = checkBoxSun.Checked = WiseSafeToOperate.sunSensor.Enabled;
            checkBoxWind.Tag = checkBoxWind.Checked = WiseSafeToOperate.windSensor.Enabled;

            textBoxCloudIntervalSeconds.Tag = textBoxCloudIntervalSeconds.Text = (WiseSafeToOperate.cloudsSensor.Interval.TotalSeconds).ToString();
            textBoxHumidityIntervalSeconds.Tag = textBoxHumidityIntervalSeconds.Text = (WiseSafeToOperate.humiditySensor.Interval.TotalSeconds).ToString();
            textBoxRainIntervalSeconds.Tag = textBoxRainIntervalSeconds.Text = (WiseSafeToOperate.rainSensor.Interval.TotalSeconds).ToString();
            textBoxSunIntervalSeconds.Tag = textBoxSunIntervalSeconds.Text = (WiseSafeToOperate.sunSensor.Interval.TotalSeconds).ToString();
            textBoxWindIntervalSeconds.Tag = textBoxWindIntervalSeconds.Text = (WiseSafeToOperate.windSensor.Interval.TotalSeconds).ToString();

            textBoxCloudRepeats.Tag = textBoxCloudRepeats.Text = WiseSafeToOperate.cloudsSensor._repeats.ToString();
            textBoxHumidityRepeats.Tag = textBoxHumidityRepeats.Text = WiseSafeToOperate.humiditySensor._repeats.ToString();
            textBoxRainRepeats.Tag = textBoxRainRepeats.Text = WiseSafeToOperate.rainSensor._repeats.ToString();
            textBoxWindRepeats.Tag = textBoxWindRepeats.Text = WiseSafeToOperate.windSensor._repeats.ToString();

            toolTip1.SetToolTip(textBoxHumidity, "0 to 100 (%)");
            toolTip1.SetToolTip(textBoxAge, "0 to use data of any age (sec)");
            toolTip1.SetToolTip(textBoxHumidity, "Between 0 and 100 (%)");
            toolTip1.SetToolTip(textBoxWind, "Greater than 0 (mps)");

            string tip = $"Between {WiseSafeToOperate.sunSensor.MinSettableElevation} and {WiseSafeToOperate.sunSensor.MaxSettableElevation} (deg)";
            toolTip1.SetToolTip(textBoxSunElevationAtDawn, tip);
            toolTip1.SetToolTip(textBoxSunElevationAtDusk, tip);


            bool check = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "ARDO", "Enabled", true.ToString()));
            checkBoxARDOEnabled.Tag = check;
            checkBoxARDOEnabled.Checked = check;

            check = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "OWLRefresher", "Enabled", true.ToString()));
            checkBoxOWLEnabled.Tag = WiseSafeToOperate.owlRefresher.Enabled.ToString();
            checkBoxOWLEnabled.Checked= check;

            check = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "TessWRefresher", "Enabled", true.ToString()));
            checkBoxTessWEnabled.Tag = check;
            checkBoxTessWEnabled.Checked= check;
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

        private void checkBoxTessWEnabled_CheckedChanged(object sender, EventArgs e)
        {
            bool chk = checkBoxTessWEnabled.Checked;
            checkBoxCloud.Checked = chk;
        }

        private void checkBoxCloud_CheckedChanged(object sender, EventArgs e)
        {
            bool chk = checkBoxCloud.Checked;
            checkBoxTessWEnabled.Checked = chk;
        }
    }
}
