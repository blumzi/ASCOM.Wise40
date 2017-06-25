using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Focuser;

namespace FocuserApplication
{
    public partial class FormFocus : Form
    {
        WiseFocuser wisefocuser = WiseFocuser.Instance;
        Statuser focuserStatus;

        public FormFocus()
        {
            InitializeComponent();
            wisefocuser.init();
            wisefocuser.Connected = true;
            focuserStatus = new Statuser(labelStatus);
            timerRefresh.Enabled = true;

#if WITH_PID
            textBoxPID_P.Text = wisefocuser.upPID.ProportionalGain.ToString();
            textBoxPID_I.Text = wisefocuser.upPID.IntegralGain.ToString();
            textBoxPID_D.Text = wisefocuser.upPID.DerivativeGain.ToString();
#endif
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            labelFocusCurrentValue.Text = wisefocuser.Position.ToString();
            focuserStatus.Show(wisefocuser.Status);
        }

#region FocuserControl
        private void buttonFocusIncrease_Click(object sender, EventArgs e)
        {
            uint newPos = wisefocuser.Position + Convert.ToUInt32(comboBoxFocusStep.Text);
            if (newPos > wisefocuser.UpperLimit)
                newPos = wisefocuser.UpperLimit;

            if (newPos != wisefocuser.Position)
                wisefocuser.Move(newPos);
        }

        private void buttonFocusDecrease_Click(object sender, EventArgs e)
        {
            uint newPos = wisefocuser.Position - Convert.ToUInt32(comboBoxFocusStep.Text);
            if (newPos < 0)
                newPos = 0;

            if (newPos != wisefocuser.Position)
                wisefocuser.Move(newPos);
        }

        private void focuserHalt(object sender, MouseEventArgs e)
        {
            wisefocuser.Halt();
        }

        private void buttonFocuserStop_Click(object sender, EventArgs e)
        {
            wisefocuser.Stop();
        }

        private void buttonFocusUp_MouseDown(object sender, MouseEventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.Up);
        }

        private void buttonFocusDown_MouseDown(object sender, MouseEventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.Down);
        }

        private void buttonFocusStop(object sender, MouseEventArgs e)
        {
            wisefocuser.Stop();
        }

        private void buttonFocusGoto_Click(object sender, EventArgs e)
        {
            if (textBoxFocusGotoPosition.Text == string.Empty)
                return;

            try
            {
                uint newPos = Convert.ToUInt32(textBoxFocusGotoPosition.Text);
                wisefocuser.Move(newPos);
            }
            catch (Exception ex)
            {
                focuserStatus.Show(ex.Message, 1000, Statuser.Severity.Error);
            }
        }

        private void textBoxFocusGotoPosition_Validated(object sender, EventArgs e)
        {
            TextBox box = (sender as TextBox);

            if (box.Text == string.Empty)
                return;

            int pos = Convert.ToInt32(box.Text);

            if (pos < 0 || pos >= wisefocuser.MaxStep)
            {
                box.Text = string.Empty;
                focuserStatus.Show(string.Format("Bad position, 0..{0}", wisefocuser.MaxStep), 1000, Statuser.Severity.Error);
            }
        }

        private void buttonFocusAllUp_Click(object sender, EventArgs e)
        {
            wisefocuser.Move(WiseFocuser.Direction.AllUp);
        }

        private void buttonFocusAllDown_Click(object sender, EventArgs e)
        {
            if (wisefocuser.Position > 0)
                wisefocuser.Move(WiseFocuser.Direction.AllDown);
        }
/*
        private void buttonDownMillis_Click(object sender, EventArgs e)
        {
            int millis = Convert.ToInt32(comboBoxMillis.Text);
            DateTime end = DateTime.Now.AddMilliseconds(millis);
            wisefocuser.Move(WiseFocuser.Direction.Down);
            while (DateTime.Now.CompareTo(end) <= 0)
                Thread.Sleep(10);
            wisefocuser.Stop();
        }

        private void buttonUpMillis_Click(object sender, EventArgs e)
        {
            int millis = Convert.ToInt32(comboBoxMillis.Text);
            DateTime end = DateTime.Now.AddMilliseconds(millis);
            wisefocuser.Move(WiseFocuser.Direction.Up);
            while (DateTime.Now.CompareTo(end) <= 0)
                Thread.Sleep(10);
            wisefocuser.Stop();
        }
*/
        #endregion
    }
}
