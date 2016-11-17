using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.Wise40.SafeToOperate;

namespace ASCOM.Wise40.SafeToImage
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        WiseSafeToOperate wisesafetoimage = WiseSafeToOperate.Instance(WiseSafeToOperate.Operation.Open);

        public SetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
            toolTip1.SetToolTip(textBoxAge, "0 to use data of any age");
            toolTip1.SetToolTip(textBoxHumidity, "between 0 and 100");
            toolTip1.SetToolTip(textBoxWind, "greter than 0");
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            bool valid = true;
            Color okColor = labelTitle.ForeColor;
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
                textBoxAge.ForeColor = okColor;
                wisesafetoimage.ageMaxSeconds = i;
            }
            wisesafetoimage.cloudsMaxEnum = (CloudSensor.SensorData.CloudCondition)comboBoxCloud.SelectedIndex;
            wisesafetoimage.cloudsMaxValue = CloudSensor.SensorData.doubleCloudCondition[wisesafetoimage.cloudsMaxEnum];
            wisesafetoimage.rainMax = comboBoxRain.SelectedIndex;
            wisesafetoimage.lightMaxEnum = (CloudSensor.SensorData.DayCondition)comboBoxLight.SelectedIndex;
            wisesafetoimage.lightMaxValue = (int)wisesafetoimage.lightMaxEnum;
            i = Convert.ToInt32(textBoxHumidity.Text);
            if (i >= 0 && i <= 100)
            {
                textBoxHumidity.ForeColor = okColor;
                wisesafetoimage.humidityMax = i;
            }
            else
            {
                textBoxHumidity.ForeColor = badColor;
                valid = false;
            }
            i = Convert.ToInt32(textBoxWind.Text);
            if (i >= 0)
            {
                textBoxWind.ForeColor = okColor;
                wisesafetoimage.windMax = i;
            }
            else
            {
                textBoxWind.ForeColor = badColor;
                valid = false;
            }

            if (valid)
                wisesafetoimage.WriteProfile();
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
            wisesafetoimage.ReadProfile();
            
            comboBoxCloud.SelectedIndex = (int)wisesafetoimage.cloudsMaxEnum;
            comboBoxRain.SelectedIndex = (int)wisesafetoimage.rainMax;
            comboBoxLight.SelectedIndex = (int)wisesafetoimage.lightMaxEnum;
            textBoxWind.Text = wisesafetoimage.windMax.ToString();
            textBoxHumidity.Text = wisesafetoimage.humidityMax.ToString();
            textBoxAge.Text = wisesafetoimage.ageMaxSeconds.ToString();
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
