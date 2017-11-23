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
        private double[] ra = new double[2];
        private double[] dec = new double[2];
        private double[] raEnc = new double[2];
        private double[] decEnc = new double[2];
        private Statuser statuser;

        private enum raTrackBar { West, None, East };
        private enum decTrackBar { South, None, North };

        public PulseGuideForm()
        {
            InitializeComponent();
            statuser = new Statuser(labelStatus);
            statuser.Show("");
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            Visible = false;
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            ra[0] = wisetele.RightAscension;
            dec[0] = wisetele.Declination;
            raEnc[0] = wisetele.HAEncoder.Value;
            decEnc[0] = wisetele.DecEncoder.Value;

            int[] millis = new int[2] { 0, 0 };
            GuideDirections[] dir = new GuideDirections[2];

            labelRaDeltaDeg.Text = labelDecDeltaDeg.Text = labelRaDeltaEnc.Text = labelDecDeltaEnc.Text = string.Empty;

            switch ((raTrackBar)trackBarRa.Value)
            {
                case raTrackBar.West:
                    dir[0] = GuideDirections.guideWest;
                    millis[0] = Convert.ToInt32(textBoxRaMillis.Text);
                    break;
                case raTrackBar.None:
                    millis[0] = 0;
                    break;
                case raTrackBar.East:
                    dir[0] = GuideDirections.guideEast;
                    millis[0] = Convert.ToInt32(textBoxRaMillis.Text);
                    break;
            }

            switch ((decTrackBar)trackBarDec.Value)
            {
                case decTrackBar.North:
                    dir[1] = GuideDirections.guideNorth;
                    millis[1] = Convert.ToInt32(textBoxDecMillis.Text);
                    break;
                case decTrackBar.None:
                    millis[1] = 0;
                    break;
                case decTrackBar.South:
                    dir[1] = GuideDirections.guideSouth;
                    millis[1] = Convert.ToInt32(textBoxDecMillis.Text);
                    break;
            }

            if (millis[0] == 0 && millis[1] == 0)
                return;

            statuser.Show("Guiding ...");

            if (millis[0] != 0)
            {
                try
                {
                    wisetele.PulseGuide(dir[0], millis[0]);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }


            if (millis[1] != 0)
            {
                try
                {
                    wisetele.PulseGuide(dir[1], millis[1]);
                }
                catch (Exception ex)
                {
                    statuser.Show(ex.Message, 5000, Statuser.Severity.Error);
                }
            }

            while (wisetele.IsPulseGuiding)
                Thread.Sleep(10);

            statuser.Show("Done.", 2000);
            ra[1] = wisetele.RightAscension;
            dec[1] = wisetele.Declination;
            raEnc[1] = wisetele.HAEncoder.Value;
            decEnc[1] = wisetele.DecEncoder.Value;

            if (millis[0] != 0)
            {
                labelRaDeltaDeg.Text = (new Angle(Math.Abs(ra[1] - ra[0]), Angle.Type.RA)).ToString();
                labelRaDeltaEnc.Text = (raEnc[1] - raEnc[0]).ToString("F0");
            }

            if (millis[1] != 0)
            {
                labelDecDeltaDeg.Text = (new Angle(Math.Abs(decEnc[1] - decEnc[0]), Angle.Type.Deg)).ToString();
                labelDecDeltaEnc.Text = (decEnc[1] - decEnc[0]).ToString("F0");
            }
        }
    }
}
