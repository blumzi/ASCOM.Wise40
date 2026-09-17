using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.DriverAccess;
using Newtonsoft.Json;

namespace Dash
{
    public partial class RenishawForm : Form
    {
        ASCOM.DriverAccess.Telescope tele;

        //
        // True while the refresh timer is pushing digest values into the controls.
        //
        // Without it this form SILENTLY DEMOTED THE TELESCOPE TO THE OLD ENCODERS.
        //  Setting radioButtonXxx.Checked raises CheckedChanged, the handlers called
        //  Action("encoders", ...) unconditionally, and that write persists to the ASCOM
        //  Profile - so a value read out of the digest came straight back as if the
        //  operator had clicked it.
        //
        // Two ways that bit, and both are fixed here:
        //
        //  - EncodersInUseEnum.Old is 0, the default.  If Dash polled before the driver
        //     had filled in the digest - which is exactly what happens when Dash and the
        //     RemoteServer start together under the watcher - it read Old, checked the Old
        //     button, and wrote Old.  From then on the digest genuinely said Old, so it
        //     latched.  Observed twice on 2026-09-17, each time within seconds of the
        //     chain coming up, quietly undoing the encoder work and restoring 31 arcmin
        //     of pointing error with no message anywhere.
        //
        //  - CheckedChanged also fires on the button being UNchecked.  Checking "New"
        //     unchecks "Old", which called Action("encoders", "old").  Both handlers ran
        //     on every tick, and which one won was down to event ordering.
        //
        private bool _updatingFromDigest;

        public RenishawForm(ASCOM.DriverAccess.Telescope wiseTele)
        {
            InitializeComponent();
            tele = wiseTele;
            timerRefresh.Start();
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            TelescopeDigest telescopeDigest = JsonConvert.DeserializeObject<TelescopeDigest>(tele.Action("status", ""));
            RenishawDigest renishawDigest = telescopeDigest.Renishaw;

            //
            // Take the old encoders' readings from the digest, NOT from
            //  telescopeDigest.Current - that is whichever encoder is in use, so once
            //  EncodersInUse became New this form was comparing the Renishaw with
            //  itself and every delta showed zero.
            //
            double oldHA = renishawDigest.oldHA;
            double oldDec = renishawDigest.oldDec;
            double newHA = renishawDigest.HA;
            double newDec = renishawDigest.Dec;
            Angle lst = Angle.FromHours(telescopeDigest.LocalSiderealTime);

            labelLST.Text = $"{lst.ToNiceString()} [{lst.Hours}]";
            _updatingFromDigest = true;
            try
            {
                switch (telescopeDigest.EncodersInUse)
                {
                    case WiseTele.EncodersInUseEnum.Old:
                        radioButtonOldEncoders.Checked = true;
                        break;
                    case WiseTele.EncodersInUseEnum.New:
                        radioButtonNewEncoders.Checked = true;
                        break;
                }
            }
            finally
            {
                _updatingFromDigest = false;
            }

            labelEncoderHA.Text = renishawDigest.EncHA.ToString();
            labelEncoderDec.Text = renishawDigest.EncDEC.ToString();
            labelHA.Text = $"{Angle.FromHours(newHA).ToNiceString()} [{newHA}]";
            labelDec.Text = $"{Angle.FromDegrees(newDec).ToNiceString()} [{newDec}]";
            labelRadiansHA.Text = renishawDigest.radHA.ToString();
            labelRadiansDec.Text = renishawDigest.radDec.ToString();

            labelOriginalHA.Text = $"{Angle.FromHours(oldHA).ToNiceString()} [{oldHA}]";
            labelOriginalDec.Text = $"{Angle.FromDegrees(oldDec).ToNiceString()} [{oldDec}]";

            double deltaHA = Math.Abs(oldHA - newHA);
            double deltaDec = Math.Abs(oldDec - newDec);
            labelDeltaHA.Text = $"{Angle.FromHours(deltaHA).ToNiceString()} [{deltaHA}]";
            // Dec is in DEGREES - FromHours here showed it 15x too large.
            labelDeltaDec.Text = $"{Angle.FromDegrees(deltaDec).ToNiceString()} [{deltaDec}]";
        }

        //
        // Act only on a genuine click: not while the timer is writing the digest into the
        //  controls, and only for the button that has just become CHECKED - see the
        //  comment on _updatingFromDigest.  Which encoders fly the telescope is not
        //  something a refresh tick gets to decide.
        //
        private void radioButtonOldEncoders_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingFromDigest || !radioButtonOldEncoders.Checked)
                return;

            tele.Action("encoders", "old");
        }

        private void radioButtonNewEncoders_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingFromDigest || !radioButtonNewEncoders.Checked)
                return;

            tele.Action("encoders", "new");
        }
    }
}
