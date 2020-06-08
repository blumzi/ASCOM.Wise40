using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40; //.Telescope;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;
using System.Threading;

namespace Dash
{
    public partial class PulseGuideForm : Form
    {
        private static readonly WiseTele wisetele = WiseTele.Instance;
        private readonly Statuser statuser;

        private enum RaTrackBar { West, None, East };
        private enum DecTrackBar { South, None, North };

        private class Movement
        {
            public bool moving;
            public GuideDirections dir;
            public int millis;
            public double coord_before, coord_after;
            public double enc_before, enc_after;
        };

        private readonly Movement primaryMovement = new Movement() { moving = false };
        private readonly Movement secondaryMovement = new Movement() { moving = false };

        private readonly Dictionary<TelescopeAxes, Movement> CurrentMove = new Dictionary<TelescopeAxes, Movement>();

        public PulseGuideForm()
        {
            InitializeComponent();
            CurrentMove.Add(TelescopeAxes.axisPrimary, primaryMovement);
            CurrentMove.Add(TelescopeAxes.axisSecondary, secondaryMovement);
            statuser = new Statuser(labelStatus);
            statuser.Show("");
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            Visible = false;
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            labelRaDeltaDeg.Text = labelDecDeltaDeg.Text = labelRaDeltaEnc.Text = labelDecDeltaEnc.Text = string.Empty;
            CurrentMove[TelescopeAxes.axisPrimary].moving = CurrentMove[TelescopeAxes.axisSecondary].moving = false;

            if (radioButtonEast.Checked || radioButtonWest.Checked)
            {
                CurrentMove[TelescopeAxes.axisPrimary].moving = true;
                CurrentMove[TelescopeAxes.axisPrimary].millis = Convert.ToInt32(textBoxRaMillis.Text);
                CurrentMove[TelescopeAxes.axisPrimary].coord_before = wisetele.RightAscension;
                CurrentMove[TelescopeAxes.axisPrimary].enc_before = wisetele.HAEncoder.EncoderValue;
                CurrentMove[TelescopeAxes.axisPrimary].dir = radioButtonWest.Checked ?
                    GuideDirections.guideWest : GuideDirections.guideEast;
            }

            if (radioButtonNorth.Checked || radioButtonSouth.Checked)
            {
                CurrentMove[TelescopeAxes.axisSecondary].moving = true;
                CurrentMove[TelescopeAxes.axisSecondary].millis = Convert.ToInt32(textBoxDecMillis.Text);
                CurrentMove[TelescopeAxes.axisSecondary].coord_before = wisetele.Declination;
                CurrentMove[TelescopeAxes.axisSecondary].enc_before = wisetele.DecEncoder.EncoderValue;
                CurrentMove[TelescopeAxes.axisSecondary].dir = radioButtonNorth.Checked ?
                    GuideDirections.guideNorth : GuideDirections.guideSouth;
            }

            if (!CurrentMove[TelescopeAxes.axisPrimary].moving && !CurrentMove[TelescopeAxes.axisSecondary].moving)
                return;

            statuser.Show("Guiding ...");

            if (CurrentMove[TelescopeAxes.axisPrimary].moving)
            {
                try
                {
                    wisetele.PulseGuide(CurrentMove[TelescopeAxes.axisPrimary].dir, CurrentMove[TelescopeAxes.axisPrimary].millis);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }

            if (CurrentMove[TelescopeAxes.axisSecondary].moving)
            {
                try
                {
                    wisetele.PulseGuide(CurrentMove[TelescopeAxes.axisSecondary].dir, CurrentMove[TelescopeAxes.axisSecondary].millis);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }

            while (wisetele.IsPulseGuiding)
                Application.DoEvents();

            statuser.Show("Done.", 2000);

            if (CurrentMove[TelescopeAxes.axisPrimary].moving)
            {
                CurrentMove[TelescopeAxes.axisPrimary].coord_after = wisetele.RightAscension;
                CurrentMove[TelescopeAxes.axisPrimary].enc_after = wisetele.HAEncoder.EncoderValue;
                labelRaDeltaDeg.Text = (new Angle(Math.Abs(CurrentMove[TelescopeAxes.axisPrimary].coord_after - CurrentMove[TelescopeAxes.axisPrimary].coord_before), Angle.AngleType.RA)).ToString();
                labelRaDeltaEnc.Text = (Math.Abs(CurrentMove[TelescopeAxes.axisPrimary].enc_after - CurrentMove[TelescopeAxes.axisPrimary].enc_before)).ToString("F0");
            }

            if (CurrentMove[TelescopeAxes.axisSecondary].moving)
            {
                CurrentMove[TelescopeAxes.axisSecondary].coord_after = wisetele.Declination;
                CurrentMove[TelescopeAxes.axisSecondary].enc_after = wisetele.DecEncoder.EncoderValue;
                labelDecDeltaDeg.Text = (new Angle(Math.Abs(CurrentMove[TelescopeAxes.axisSecondary].coord_after - CurrentMove[TelescopeAxes.axisSecondary].coord_before), Angle.AngleType.Dec)).ToString();
                labelDecDeltaEnc.Text = (Math.Abs(CurrentMove[TelescopeAxes.axisSecondary].enc_after - CurrentMove[TelescopeAxes.axisSecondary].enc_before)).ToString("F0");
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            wisetele.pulsing.Abort();
        }
    }
}
