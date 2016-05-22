using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;


namespace ASCOM.Wise40
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        Telescope T;

        public SetupDialogForm(Telescope telescope)
        {
            InitializeComponent();

            T = telescope;
            T.ReadProfile();
            traceBox.Checked = Telescope._trace;
            accuracyBox.SelectedItem = (WiseSite.Instance.astrometricAccuracy == Accuracy.Full) ? 0 : 1;
            if (WiseTele.Instance.debugger.Level == 0)
            {
                checkBoxDebugging.Checked = false;

                checkBoxDebugExceptions.AutoCheck = true;
                checkBoxDebugEncoders.AutoCheck = false;
                checkBoxDebugAxes.AutoCheck = false;
                checkBoxDebugMotors.AutoCheck = false;
            }
            else
            {
                checkBoxDebugging.Checked = true;

                checkBoxDebugExceptions.Checked = true;
                checkBoxDebugEncoders.AutoCheck = true;
                checkBoxDebugAxes.AutoCheck = true;
                checkBoxDebugMotors.AutoCheck = true;

                Debugger debugger = WiseTele.Instance.debugger;
                checkBoxDebugEncoders.Checked = debugger.Debugging(Debugger.DebugLevel.DebugEncoders);
                checkBoxDebugAxes.Checked = debugger.Debugging(Debugger.DebugLevel.DebugAxes);
                checkBoxDebugExceptions.Checked = debugger.Debugging(Debugger.DebugLevel.DebugExceptions);
                checkBoxDebugMotors.Checked = debugger.Debugging(Debugger.DebugLevel.DebugMotors);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Telescope._trace = traceBox.Checked;
            WiseSite.Instance.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;

            uint level = 0;
            if (checkBoxDebugging.Checked)
            {
                if (checkBoxDebugEncoders.Checked) level |= (uint) Debugger.DebugLevel.DebugEncoders;
                if (checkBoxDebugAxes.Checked) level |= (uint)Debugger.DebugLevel.DebugAxes;
                if (checkBoxDebugExceptions.Checked) level |= (uint)Debugger.DebugLevel.DebugExceptions;
                if (checkBoxDebugMotors.Checked) level |= (uint)Debugger.DebugLevel.DebugMotors;
            }
            WiseTele.Instance.debugger.Level = level;

            T.WriteProfile();
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

        private void checkBoxDebugging_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDebugging.Checked)
            {
                checkBoxDebugExceptions.AutoCheck = true;
                checkBoxDebugEncoders.AutoCheck = true;
                checkBoxDebugAxes.AutoCheck = true;
                checkBoxDebugMotors.AutoCheck = true;
            } else
            {
                checkBoxDebugExceptions.AutoCheck = false;
                checkBoxDebugEncoders.AutoCheck = false;
                checkBoxDebugAxes.AutoCheck = false;
                checkBoxDebugMotors.AutoCheck = false;

                checkBoxDebugExceptions.Checked = false;
                checkBoxDebugEncoders.Checked = false;
                checkBoxDebugAxes.Checked = false;
                checkBoxDebugMotors.Checked = false;
            }
        }
    }
}