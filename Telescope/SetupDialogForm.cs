using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


using ASCOM.Astrometry;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;


namespace ASCOM.Wise40
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class TelescopeSetupDialogForm : Form
    {
        Telescope _T;

        public TelescopeSetupDialogForm(Telescope telescope)
        {
            InitializeComponent();

            _T = telescope;
            _T.ReadProfile();
            traceBox.Checked = _T._trace;
            accuracyBox.SelectedItem = (WiseSite.Instance.astrometricAccuracy == Accuracy.Full) ? 0 : 1;
            checkBoxEnslaveDome.Checked = _T._enslaveDome;
            if (WiseTele.Instance.debugger.Level == 0)
            {
                checkBoxDebugging.Checked = false;

                checkBoxDebugDevice.AutoCheck = true;
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
                checkBoxDebugDevice.AutoCheck = true;

                Debugger debugger = WiseTele.Instance.debugger;
                checkBoxDebugEncoders.Checked = debugger.Debugging(Debugger.DebugLevel.DebugEncoders);
                checkBoxDebugAxes.Checked = debugger.Debugging(Debugger.DebugLevel.DebugAxes);
                checkBoxDebugExceptions.Checked = debugger.Debugging(Debugger.DebugLevel.DebugExceptions);
                checkBoxDebugMotors.Checked = debugger.Debugging(Debugger.DebugLevel.DebugMotors);
                checkBoxDebugDevice.Checked = debugger.Debugging(Debugger.DebugLevel.DebugDevice);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            _T._trace = traceBox.Checked;
            WiseSite.Instance.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;

            uint level = 0;
            if (checkBoxDebugging.Checked)
            {
                if (checkBoxDebugEncoders.Checked) level |= (uint) Debugger.DebugLevel.DebugEncoders;
                if (checkBoxDebugAxes.Checked) level |= (uint)Debugger.DebugLevel.DebugAxes;
                if (checkBoxDebugExceptions.Checked) level |= (uint)Debugger.DebugLevel.DebugExceptions;
                if (checkBoxDebugMotors.Checked) level |= (uint)Debugger.DebugLevel.DebugMotors;
                if (checkBoxDebugDevice.Checked) level |= (uint)Debugger.DebugLevel.DebugDevice;
            }
            WiseTele.Instance.debugger.Level = level;
            _T._enslaveDome = checkBoxEnslaveDome.Checked;

            _T.WriteProfile();
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
                checkBoxDebugDevice.AutoCheck = true;
            } else
            {
                checkBoxDebugExceptions.AutoCheck = false;
                checkBoxDebugEncoders.AutoCheck = false;
                checkBoxDebugAxes.AutoCheck = false;
                checkBoxDebugMotors.AutoCheck = false;
                checkBoxDebugDevice.AutoCheck = false;

                checkBoxDebugExceptions.Checked = false;
                checkBoxDebugEncoders.Checked = false;
                checkBoxDebugAxes.Checked = false;
                checkBoxDebugMotors.Checked = false;
                checkBoxDebugDevice.Checked = false;
            }
        }
    }
}