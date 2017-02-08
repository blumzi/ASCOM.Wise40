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

            WiseFilterWheel.ReadProfile();
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.knownWheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb;
                    ComboBox cb;

                    cb = (ComboBox)F.Controls.Find(string.Format("comboBox{0}{1}", w.positions.Length, i), true)[0];
                    if (cb.Text != string.Empty)
                        w.positions[i].filterName = cb.Text.Remove(cb.Text.IndexOf(':'));
                    else
                        w.positions[i].filterName = string.Empty;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    w.positions[i].tag = tb.Text ?? string.Empty;
                }
            }
            WiseFilterWheel.port = comboBoxPort.Text;

            WiseFilterWheel.WriteProfile();
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
            WiseFilterWheel.ReadProfile();

            foreach (var w in WiseFilterWheel.knownWheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    ComboBox cb;

                    cb = (ComboBox)F.Controls.Find(string.Format("comboBox{0}{1}", w.positions.Length, i), true)[0];
                    if (w.positions[i].filterName == string.Empty)
                        cb.Text = string.Empty;
                    else {
                        Filter f = WiseFilterWheel.filterInventory.Find((x) => x.Name == w.positions[i].filterName);
                        cb.Text = string.Format("{0}: {1}", f.Name, f.Description);
                    }
                    foreach (Filter f in WiseFilterWheel.filterInventory)
                        cb.Items.Add(string.Format("{0}: {1}", f.Name, f.Description));

                    TextBox tb;
                    tb = (TextBox)F.Controls.Find(string.Format("textBoxWheel{0}RFID{1}", w.positions.Length, i), true)[0];
                    tb.Text = w.positions[i].tag ?? string.Empty;
                    tb.Enabled = checkBoxEditableRFIDs.Checked;
                }
            }
            comboBoxPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (!String.IsNullOrEmpty(WiseFilterWheel.port) && comboBoxPort.Items.Contains(WiseFilterWheel.port))
                comboBoxPort.Text = WiseFilterWheel.port;
        }

        private void FilterWheelSetupDialogForm_Load(object sender, EventArgs e)
        {
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.knownWheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                }
            }
        }

        private void checkBoxEditableRFIDs_CheckedChanged(object sender, EventArgs e)
        {
            Form F = this.FindForm();
            CheckBox cb = sender as CheckBox;

            foreach (var w in WiseFilterWheel.knownWheels)
            {
                for (int i = 0; i < w.positions.Length; i++)
                {
                    TextBox tb;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.name, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                    tb.Enabled = cb.Checked;
                }
            }
        }
    }
}