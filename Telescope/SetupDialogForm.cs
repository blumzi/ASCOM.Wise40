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
        private static WiseTele wisetele = WiseTele.Instance;

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


            Debugger debugger = Debugger.Instance;
            checkBoxDebugEncoders.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugEncoders) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugAxes.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugAxes) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugExceptions.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugExceptions) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugMotors.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugMotors) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugDevice.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugDevice) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugASCOM.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugASCOM) ? CheckState.Checked : CheckState.Unchecked;
            checkBoxDebugLogic.CheckState = debugger.Debugging(Debugger.DebugLevel.DebugLogic) ? CheckState.Checked : CheckState.Unchecked;
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            wisetele.traceLogger.Enabled = traceBox.Checked;
            wisesite.astrometricAccuracy = accuracyBox.Text == "Full" ? Accuracy.Full : Accuracy.Reduced;

            uint level = 0;
            if (checkBoxDebugEncoders.Checked) level |= (uint)Debugger.DebugLevel.DebugEncoders;
            if (checkBoxDebugAxes.Checked) level |= (uint)Debugger.DebugLevel.DebugAxes;
            if (checkBoxDebugExceptions.Checked) level |= (uint)Debugger.DebugLevel.DebugExceptions;
            if (checkBoxDebugMotors.Checked) level |= (uint)Debugger.DebugLevel.DebugMotors;
            if (checkBoxDebugDevice.Checked) level |= (uint)Debugger.DebugLevel.DebugDevice;
            if (checkBoxDebugASCOM.Checked) level |= (uint)Debugger.DebugLevel.DebugASCOM;
            if (checkBoxDebugLogic.Checked) level |= (uint)Debugger.DebugLevel.DebugLogic;
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

        private void buttonSetAll_Click(object sender, EventArgs e)
        {
            checkBoxDebugEncoders.Checked = true;
            checkBoxDebugAxes.Checked = true;
            checkBoxDebugExceptions.Checked = true;
            checkBoxDebugMotors.Checked = true;
            checkBoxDebugDevice.Checked = true;
            checkBoxDebugLogic.Checked = true;
            checkBoxDebugMotors.Checked = true;
            checkBoxDebugASCOM.Checked = true;
        }

        private void buttonClearAll_Click(object sender, EventArgs e)
        {
            checkBoxDebugEncoders.Checked = false;
            checkBoxDebugAxes.Checked = false;
            checkBoxDebugExceptions.Checked = false;
            checkBoxDebugMotors.Checked = false;
            checkBoxDebugDevice.Checked = false;
            checkBoxDebugLogic.Checked = false;
            checkBoxDebugMotors.Checked = false;
            checkBoxDebugASCOM.Checked = false;
        }
    }
}