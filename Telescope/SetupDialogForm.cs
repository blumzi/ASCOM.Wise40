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
        bool traceState;
        uint debugLevel;
        Accuracy accuracy;
        bool enslaveDome;
        private static WiseSite wisesite = WiseSite.Instance;

        public TelescopeSetupDialogForm(bool traceState, uint debugLevel, Accuracy accuracy, bool enslaveDome)
        {
            this.traceState = traceState;
            this.debugLevel = debugLevel;
            this.accuracy = accuracy;
            this.enslaveDome = enslaveDome;

            InitializeComponent();
            wisesite.init();
            
            traceBox.Checked = traceState;
            accuracyBox.SelectedItem = (accuracy == Accuracy.Full) ? 0 : 1;
            checkBoxEnslaveDome.Checked = enslaveDome;
            if (WiseTele.Instance.debugger.Level == 0)
            {
                checkBoxDebugging.Checked = false;

                checkBoxDebugDevice.AutoCheck = false;
                checkBoxDebugExceptions.AutoCheck = false;
                checkBoxDebugEncoders.AutoCheck = false;
                checkBoxDebugAxes.AutoCheck = false;
                checkBoxDebugMotors.AutoCheck = false;
            }
            else
            {
                checkBoxDebugging.Checked = true;

                checkBoxDebugExceptions.AutoCheck = true;
                checkBoxDebugEncoders.AutoCheck = true;
                checkBoxDebugAxes.AutoCheck = true;
                checkBoxDebugMotors.AutoCheck = true;
                checkBoxDebugDevice.AutoCheck = true;

                Debugger debugger = WiseTele.Instance.debugger;
                checkBoxDebugEncoders.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugEncoders) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugAxes.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugAxes) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugExceptions.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugExceptions) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugMotors.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugMotors) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugDevice.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugDevice) ? CheckState.Checked : CheckState.Unchecked;
            }
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Telescope._trace = traceBox.Checked;
            wisesite.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;

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
            WiseTele.Instance._enslaveDome = checkBoxEnslaveDome.Checked;
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

                Debugger debugger = WiseTele.Instance.debugger;
                checkBoxDebugExceptions.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugExceptions) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugEncoders.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugEncoders) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugAxes.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugAxes) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugMotors.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugMotors) ? CheckState.Checked : CheckState.Unchecked;
                checkBoxDebugDevice.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugDevice) ? CheckState.Checked : CheckState.Unchecked; 
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