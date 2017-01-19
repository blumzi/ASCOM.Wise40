using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.Wise40;
using RavSoft;

namespace ASCOM.Wise40
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class FilterWheelSetupDialogForm : Form
    {
        WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;

        public FilterWheelSetupDialogForm()
        {
            wisefilterwheel.init();

            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb;

                    tb = (TextBox) F.Controls.Find(string.Format("textBox{0}Name{1}", w.name, i), true)[0];
                    w.positions[i].filterName = tb.Text ?? string.Empty;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}Offset{1}", w.name, i), true)[0];
                    w.positions[i].filterOffset = tb.Text == string.Empty ? 0 : Convert.ToInt32(tb.Text);

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    w.positions[i].tag = tb.Text ?? string.Empty;
                }
            }
            wisefilterwheel.port = comboBoxPort.Text;

            wisefilterwheel.WriteProfile();
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
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}Name{1}", w.name, i), true)[0];
                    tb.Text = w.positions[i].filterName ?? string.Empty;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}Offset{1}", w.name, i), true)[0];
                    tb.Text = w.positions[i].filterOffset.ToString();

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    tb.Text = w.positions[i].tag ?? string.Empty;
                }
            }
            comboBoxPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (!String.IsNullOrEmpty(wisefilterwheel.port) && comboBoxPort.Items.Contains(wisefilterwheel.port))
                comboBoxPort.Text = wisefilterwheel.port;
        }

        private void FilterWheelSetupDialogForm_Load(object sender, EventArgs e)
        {
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}Name{1}", w.name, i), true)[0];
                    CueProvider.SetCue(tb, "Clear");

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}Offset{1}", w.name, i), true)[0];
                    CueProvider.SetCue(tb, "0");

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                }
            }
        }
    }
}