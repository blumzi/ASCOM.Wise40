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

namespace ASCOM.Wise40 //.FilterWheel
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class FilterWheelSetupDialogForm : Form
    {
        private readonly WiseFilterWheel wisefilterwheel = WiseFilterWheel.Instance;

        public FilterWheelSetupDialogForm()
        {
            wisefilterwheel.Init();

            WiseFilterWheel.ReadProfile();
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Form F = this.FindForm();

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb;
                    ComboBox cb = (ComboBox)F.Controls.Find(string.Format("comboBox{0}{1}", w._nPositions, i), true)[0];
                    if (!string.IsNullOrEmpty(cb.Text))
                        w._positions[i].filterName = cb.Text.Remove(cb.Text.IndexOf(':'));
                    else
                        w._positions[i].filterName = string.Empty;

                    tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    w._positions[i].tag = tb.Text ?? string.Empty;
                }
            }
            WiseFilterWheel.Instance.arduino.SerialPortName = comboBoxPort.Text;
            WiseFilterWheel.Enabled = checkBoxEnabled.Checked;

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

            foreach (var w in WiseFilterWheel.wheels)
            {
                WiseFilterWheel.Instance.Init();
                for (int i = 0; i < w._nPositions; i++)
                {
                    ComboBox cb = (ComboBox)F.Controls.Find(string.Format("comboBox{0}{1}", w._nPositions, i), true)[0];
                    if (string.IsNullOrEmpty(w._positions[i].filterName))
                    {
                        cb.Text = string.Empty;
                    }
                    else {
                        Filter f = w._filterInventory.Find((x) => x.Name == w._positions[i].filterName);
                        cb.Text = (f == null) ? "<??>" : string.Format("{0}: {1}", f.Name, f.Description);
                    }
                    foreach (Filter f in w._filterInventory)
                        cb.Items.Add(string.Format("{0}: {1}", f.Name, f.Description));

                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBoxWheel{0}RFID{1}", w._nPositions, i), true)[0];
                    tb.Text = w._positions[i].tag ?? string.Empty;
                    tb.Enabled = checkBoxEditableRFIDs.Checked;
                }
            }

            string[] existingPorts = System.IO.Ports.SerialPort.GetPortNames();
            comboBoxPort.Items.AddRange(existingPorts);

            string port = WiseFilterWheel.Instance.arduino.SerialPortName;
            if (!String.IsNullOrEmpty(port))
            {
                foreach (var p in existingPorts)
                {
                    if (p == port)
                    {
                        comboBoxPort.Text = port;
                        break;
                    }
                }
            } else
            {
                comboBoxPort.Text = "";
            }

            labelOpModeValue.Text = WiseSite.OperationalMode.ToString();

            UpdateEditability();
        }

        private void UpdateEditability()
        {
            Form F = this.FindForm();
            bool editable;

            if (WiseFilterWheel.Enabled)
            {
                checkBoxEnabled.Checked = true;
                checkBoxEditableRFIDs.Checked = false;
                comboBoxPort.Enabled = true;
                editable = true;
            }
            else
            {
                checkBoxEnabled.Checked = false;
                checkBoxEnabled.AutoCheck = false;
                checkBoxEditableRFIDs.Checked = false;
                checkBoxEditableRFIDs.AutoCheck = false;
                comboBoxPort.Enabled = false;
                editable = false;
            }

            bool editableRFIDs = checkBoxEditableRFIDs.Checked;
            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    ComboBox cb = (ComboBox)F.Controls.Find(string.Format("comboBox{0}{1}", w._nPositions, i), true)[0];
                    cb.Enabled = editable;

                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBoxWheel{0}RFID{1}", w._nPositions, i), true)[0];
                    tb.Enabled = editableRFIDs;
                }
            }
        }

        private void FilterWheelSetupDialogForm_Load(object sender, EventArgs e)
        {
            Form F = this.FindForm();

            foreach (var w in new List<WiseFilterWheel.Wheel> { WiseFilterWheel.wheel4, WiseFilterWheel.wheel8})
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                }
            }
        }

        private void checkBoxEditableRFIDs_CheckedChanged(object sender, EventArgs e)
        {
            Form F = this.FindForm();
            CheckBox cb = sender as CheckBox;

            foreach (var w in WiseFilterWheel.wheels)
            {
                for (int i = 0; i < w._nPositions; i++)
                {
                    TextBox tb = (TextBox)F.Controls.Find(string.Format("textBox{0}RFID{1}", w.WiseName, i), true)[0];
                    CueProvider.SetCue(tb, "Missing");
                    tb.Enabled = cb.Checked;
                }
            }
        }
    }
}