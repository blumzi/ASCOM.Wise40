using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;

using ASCOM.Wise40;

namespace ASCOM.Wise40.Boltwood
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {      
        public SetupDialogForm()
        {
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Control[] controls;

            for (int id = 0; id < WiseBoltwood.nStations; id++)
            {
                controls = Controls.Find("checkBox" + id.ToString(), true);
                WiseBoltwood.stations[id].Enabled = (controls[0] as CheckBox)?.Checked ?? false;
                WiseBoltwood.stations[id].Name = (controls[0] as CheckBox)?.Text;
                controls = Controls.Find("comboBoxMethod" + id.ToString(), true);
                if (Enum.TryParse<WeatherStation.WeatherStationInputMethod>((controls[0] as ComboBox)?.Text, out WeatherStation.WeatherStationInputMethod method))
                    WiseBoltwood.stations[id].InputMethod = method;
                if (method == WeatherStation.WeatherStationInputMethod.ClarityII)
                {
                    controls = Controls.Find("labelPath" + id.ToString(), true);
                    WiseBoltwood.stations[id].FilePath = (controls[0] as Label)?.Text;
                }
                WiseBoltwood.stations[id].WriteProfile();
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

        private void InitUI()
        {
            Control[] controls;
            CheckBox cb;
            Label label;

            for (int i = 0; i < WiseBoltwood.nStations; i++)
            {
                controls = Controls.Find($"checkBox{i}", true);
                if (controls.Length < 1)
                    continue;

                cb = controls[0] as CheckBox;

                cb.Text = WiseBoltwood.stations[i].Name;
                cb.Checked = WiseBoltwood.stations[i].Enabled;

                if (!WiseBoltwood.stations[i].Enabled)
                    continue;

                controls = Controls.Find($"labelPath{i}", true);
                if (controls.Length < 1)
                    continue;
                label = controls[0] as Label;
                label.Text = WiseBoltwood.stations[i].FilePath;
            }
        }

        private void openFileDialog0_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath0", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath1", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath2", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath3", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog4_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath4", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog5_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls = Controls.Find("labelPath5", true);
            Label label = controls[0] as Label;

            label.Text = ((OpenFileDialog)sender).FileName;
        }

        private void buttonChooseDataFile0_Click(object sender, EventArgs e)
        {
            openFileDialog0.ShowDialog();
        }

        private void buttonChooseDataFile1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void buttonChooseDataFile2_Click(object sender, EventArgs e)
        {
            openFileDialog2.ShowDialog();
        }

        private void buttonChooseDataFile3_Click(object sender, EventArgs e)
        {
            openFileDialog3.ShowDialog();
        }

        private void buttonChooseDataFile4_Click(object sender, EventArgs e)
        {
            openFileDialog4.ShowDialog();
        }

        private void buttonChooseDataFile5_Click(object sender, EventArgs e)
        {
            openFileDialog5.ShowDialog();
        }
    }
}