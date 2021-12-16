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

            double oldHA = telescopeDigest.Current.HourAngle;
            double oldDec = telescopeDigest.Current.Declination;
            double newHA = renishawDigest.HA;
            double newDec = renishawDigest.Dec;
            Angle lst = Angle.FromHours(telescopeDigest.LocalSiderealTime);

            labelLST.Text = $"{lst.ToNiceString()} [{lst.Hours}]";
            switch (telescopeDigest.EncodersInUse)
            {
                case WiseTele.EncodersInUseEnum.Old:
                    radioButtonOldEncoders.Checked = true;
                    break;
                case WiseTele.EncodersInUseEnum.New:
                    radioButtonNewEncoders.Checked = true;
                    break;
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
            labelDeltaDec.Text = $"{Angle.FromHours(deltaDec).ToNiceString()} [{deltaDec}]";
        }

        private void radioButtonOldEncoders_CheckedChanged(object sender, EventArgs e)
        {
            tele.Action("encoders", "old");
        }

        private void radioButtonNewEncoders_CheckedChanged(object sender, EventArgs e)
        {
            tele.Action("encoders", "new");
        }
    }
}
