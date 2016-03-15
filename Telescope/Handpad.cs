using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    public partial class HandpadForm : Form
    {
        public DaqsForm daqsForm;
        WiseTele T;

        public HandpadForm()
        {
            InitializeComponent();
            T = WiseTele.Instance;
            checkBoxTrack.Checked = T.Tracking;
            //daqsForm = new DaqsForm(this);
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButtonSlew_Click(object sender, EventArgs e)
        {
            T.Speed = WiseTele.Speeds.Slew;
        }

        private void radioButtonSet_Click(object sender, EventArgs e)
        {
            T.Speed = WiseTele.Speeds.Set;
        }

        private void radioButtonGuide_Click(object sender, EventArgs e)
        {
            T.Speed = WiseTele.Speeds.Guide;
        }

        private void checkBoxTrack_CheckedChanged(object sender, EventArgs e)
        {
            T.Tracking = ((CheckBox)sender).Checked;
        }

        private void buttonHardware_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            daqsForm = new DaqsForm(this);
            daqsForm.Visible = true;
        }

        private void buttonDome_Click(object sender, EventArgs e)
        {
            if (panelDome.Visible)
            {
                buttonDome.Text = "Show Dome";
                panelDome.Visible = false;
            }
            else {
                buttonDome.Text = "Hide Dome";
                panelDome.Visible = true;
            }
        }

        private void buttonFocuser_Click(object sender, EventArgs e)
        {
            if (panelFocuser.Visible)
            {
                buttonFocuser.Text = "Show Focuser";
                panelFocuser.Visible = false;
            }
            else
            {
                buttonFocuser.Text = "Hide Focuser";
                panelFocuser.Visible = true;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            T.Stop();
        }

        private void timerDisplayRefresh_Tick(object sender, EventArgs e)
        {
            if (!panelControls.Visible)
                return;

            RaDec radec = new RaDec(T.RightAscension, T.Declination);

            DateTime now = DateTime.Now;
            DateTime utc = now.ToUniversalTime();
            //AltAz altaz = Coordinates.RaDec2AltAz(radec, new LatLon(WiseSite.Instance.Latitude, WiseSite.Instance.Longitude), now, WiseSite.Instance.Elevation);
            AltAz altaz = new AltAz(0, 0);
            labelDate.Text = utc.ToLongDateString();
            labelLTValue.Text = now.TimeOfDay.ToString(@"hh\:mm\:ss\.f\ ");
            labelUTValue.Text = utc.TimeOfDay.ToString(@"hh\:mm\:ss\.f\ ");
            labelSiderealValue.Text = DMS.FromDeg(WiseSite.Instance.LocalSiderealTime).ToString(":", ".");

            labelRightAscensionValue.Text = DMS.FromDeg(radec.Ra).ToString(":");
            labelDeclinationValue.Text = DMS.FromDeg(radec.Dec).ToString();
            labelHourAngleValue.Text = DMS.FromDeg(T.HourAngle).ToString(":");
            
            labelAltitudeValue.Text = DMS.FromDeg(altaz.Alt).ToString();
            labelAzimuthValue.Text = DMS.FromDeg(altaz.Az).ToString();
        }

        private void HandpadForm_VisibleChanged(object sender, EventArgs e)
        {
            Form F = (Form)sender;
            if (F.Visible)
                timerDisplayRefresh.Enabled = true;
            else
                timerDisplayRefresh.Enabled = false;
        }

        private void buttonHandpad_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            panelControls.Visible = false;
        }

        private void HandpadForm_Load(object sender, EventArgs e)
        {

        }

        private void directionButton_MouseDown(object sender, MouseEventArgs e)
        {
            Button button = (Button)sender;
            List<WiseMotor> motors = new List<Hardware.WiseMotor>();
            bool guide = T.Speed == WiseTele.Speeds.Guide;

            if (button == buttonNorth)
                motors.Add(guide ? T.NorthGuideMotor : T.NorthMotor);
            else if (button == buttonEast)
                motors.Add(guide ? T.EastGuideMotor : T.EastMotor);
            else if (button == buttonSouth)
                motors.Add(guide ? T.SouthGuideMotor : T.SouthMotor);
            else if (button == buttonWest)
                motors.Add(guide ? T.WestGuideMotor : T.WestMotor);
            else if (button == buttonNE)
            {
                motors.Add(guide ? T.NorthGuideMotor : T.NorthMotor);
                motors.Add(guide ? T.EastGuideMotor : T.EastMotor);
            }
            else if (button == buttonSE)
            {
                motors.Add(guide ? T.SouthGuideMotor : T.SouthMotor);
                motors.Add(guide ? T.EastGuideMotor : T.EastMotor);
            }
            else if (button == buttonSW)
            {
                motors.Add(guide ? T.SouthGuideMotor : T.SouthMotor);
                motors.Add(guide ? T.WestGuideMotor : T.WestMotor);
            }
            else if (button == buttonNW)
            {
                motors.Add(guide ? T.NorthGuideMotor : T.NorthMotor);
                motors.Add(guide ? T.WestGuideMotor : T.WestMotor);
            }

            if (motors.Count() > 0)
            {
                if (T.Tracking)
                    T.TrackMotor.SetOn();
                if (T.Speed == WiseTele.Speeds.Slew)
                    T.SlewMotor.SetOn();
                foreach (WiseMotor motor in motors)
                    motor.SetOn();
            }

        }

        private void directionButton_MouseUp(object sender, MouseEventArgs e)
        {
            T.Stop();
        }
    }
}
