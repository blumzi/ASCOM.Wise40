using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Telescope;
using ASCOM.Wise40.Common;
using ASCOM.DeviceInterface;
using System.Threading;

namespace Dash
{
    public partial class PulseGuideForm : Form
    {
        private static WiseTele wisetele = WiseTele.Instance;
        //private double[] ra = new double[2];
        //private double[] dec = new double[2];
        //private double[] raEnc = new double[2];
        //private double[] decEnc = new double[2];
        private Statuser statuser;

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

        Movement primaryMovement = new Movement() { moving = false };
        Movement secondaryMovement = new Movement() { moving = false };

        private Dictionary<TelescopeAxes, Movement> move = new Dictionary<TelescopeAxes, Movement>();

        public PulseGuideForm()
        {
            InitializeComponent();
            move.Add(TelescopeAxes.axisPrimary, primaryMovement);
            move.Add(TelescopeAxes.axisSecondary, secondaryMovement);
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
            move[TelescopeAxes.axisPrimary].moving = move[TelescopeAxes.axisSecondary].moving = false;
            
            if (radioButtonEast.Checked || radioButtonWest.Checked)
            {
                move[TelescopeAxes.axisPrimary].moving = true;
                move[TelescopeAxes.axisPrimary].millis = Convert.ToInt32(textBoxRaMillis.Text);
                move[TelescopeAxes.axisPrimary].coord_before = wisetele.RightAscension;
                move[TelescopeAxes.axisPrimary].enc_before = wisetele.HAEncoder.Value;
                move[TelescopeAxes.axisPrimary].dir = radioButtonWest.Checked ? 
                    GuideDirections.guideWest : GuideDirections.guideEast;
            }
            
            if (radioButtonNorth.Checked || radioButtonSouth.Checked)
            {
                move[TelescopeAxes.axisSecondary].moving = true;
                move[TelescopeAxes.axisSecondary].millis = Convert.ToInt32(textBoxDecMillis.Text);
                move[TelescopeAxes.axisSecondary].coord_before = wisetele.Declination;
                move[TelescopeAxes.axisSecondary].enc_before = wisetele.DecEncoder.Value;
                move[TelescopeAxes.axisSecondary].dir = radioButtonNorth.Checked ?
                    GuideDirections.guideNorth : GuideDirections.guideSouth;
            }

            if (!move[TelescopeAxes.axisPrimary].moving && !move[TelescopeAxes.axisSecondary].moving)
                return;

            statuser.Show("Guiding ...");
            
            if (move[TelescopeAxes.axisPrimary].moving)
            {
                try
                {
                    wisetele.PulseGuide(move[TelescopeAxes.axisPrimary].dir, move[TelescopeAxes.axisPrimary].millis);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }
            
            if (move[TelescopeAxes.axisSecondary].moving)
            {
                try
                {
                    wisetele.PulseGuide(move[TelescopeAxes.axisSecondary].dir, move[TelescopeAxes.axisSecondary].millis);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }

            while (wisetele.IsPulseGuiding)
                Application.DoEvents();

            statuser.Show("Done.", 2000);
            
            if (move[TelescopeAxes.axisPrimary].moving)
            {
                move[TelescopeAxes.axisPrimary].coord_after = wisetele.RightAscension;
                move[TelescopeAxes.axisPrimary].enc_after = wisetele.HAEncoder.Value;
                labelRaDeltaDeg.Text = (new Angle(Math.Abs(move[TelescopeAxes.axisPrimary].coord_after - move[TelescopeAxes.axisPrimary].coord_before), Angle.Type.RA)).ToString();
                labelRaDeltaEnc.Text = (Math.Abs(move[TelescopeAxes.axisPrimary].enc_after - move[TelescopeAxes.axisPrimary].enc_before)).ToString("F0");
            }
            
            if (move[TelescopeAxes.axisSecondary].moving)
            {
                move[TelescopeAxes.axisSecondary].coord_after = wisetele.Declination;
                move[TelescopeAxes.axisSecondary].enc_after = wisetele.DecEncoder.Value;
                labelDecDeltaDeg.Text = (new Angle(Math.Abs(move[TelescopeAxes.axisSecondary].coord_after - move[TelescopeAxes.axisSecondary].coord_before), Angle.Type.Dec)).ToString();
                labelDecDeltaEnc.Text = (Math.Abs(move[TelescopeAxes.axisSecondary].enc_after - move[TelescopeAxes.axisSecondary].enc_before)).ToString("F0");
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            wisetele.pulsing.Abort();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
