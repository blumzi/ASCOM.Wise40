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
        private WiseBoltwood boltwood = WiseBoltwood.Instance;

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
                WeatherStation.WeatherStationInputMethod method;

                controls = Controls.Find("checkBox" + id.ToString(), true);
                WiseBoltwood.stations[id].Enabled = (controls[0] as CheckBox).Checked;
                WiseBoltwood.stations[id].Name = (controls[0] as CheckBox).Text;
                controls = Controls.Find("comboBoxMethod" + id.ToString(), true);
                if (Enum.TryParse<WeatherStation.WeatherStationInputMethod>((controls[0] as ComboBox).Text, out method))
                    WiseBoltwood.stations[id].InputMethod = method;
                if (method == WeatherStation.WeatherStationInputMethod.ClarityII)
                {
                    controls = Controls.Find("labelPath" + id.ToString(), true);
                    WiseBoltwood.stations[id].FilePath = (controls[0] as Label).Text;
                }
                WiseBoltwood.stations[id].WriteProfile();
            }
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

        private struct Station {
            public bool Enabled;
            public string Name;
            public string Path;
        };

        private void InitUI()
        {
            Control[] controls;
            Dictionary<int, Station> stations = new Dictionary<int, Station>(WiseBoltwood.nStations);
            stations[0] = new Station { Enabled = true, Name = "C18", Path = "//WO-NEO/Temp/clarityII-data.txt" };
            stations[1] = new Station { Enabled = true, Name = "C28", Path = "//C28-PC/Temp/ClarityII-data.txt" };

            for (int i = 2; i < WiseBoltwood.nStations; i++)
            {
                Station dummy = new Station { Enabled = false, Name = "", Path = "" };
                if (i == 2)
                    dummy.Name = "Weizmann";
                else
                    dummy.Name = "Korean" + (i - 2).ToString();
                stations[i] = dummy;

                controls = Controls.Find("checkBox" + i.ToString(), true);
                (controls[0] as CheckBox).Text = stations[i].Name;
                (controls[0] as CheckBox).Checked = stations[i].Enabled;
                controls = Controls.Find("labelPath" + i.ToString(), true);
                (controls[0] as Label).Text = stations[i].Path;
            }
        }

        private void openFileDialog0_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath0", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath1", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath2", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath3", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog4_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath4", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
        }

        private void openFileDialog5_FileOk(object sender, CancelEventArgs e)
        {
            Control[] controls;

            controls = Controls.Find("labelPath5", true);
            (controls[0] as Label).Text = ((OpenFileDialog)sender).FileName;
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