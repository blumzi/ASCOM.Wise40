using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ASCOM.Wise40.Common;
using ASCOM.Wise40;

namespace ASCOM.Wise40.ObservatoryMonitor
{
    public partial class ObservatoryMonitorAboutForm : Form
    {
        public ObservatoryMonitorAboutForm(Version version)
        {
            InitializeComponent();
            
            string appVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}",
                version.Major, version.Minor);

            labelAbout.Text = "This utility monitors the Wise 40 inch Observatory at\n"  +
                "Mizpe Ramon, Israel.\n" +
                Const.crnl +
                "The Observatory will be shut down if:" + Const.crnl +
                "the operator intervened, or" + Const.crnl +
                "the telescope is idle, or" + Const.crnl +
                "weather conditions are not safe." + Const.crnl +
                Const.crnl +
                "Author: Arie Blumenzweig <blumzi@013.net> - since July 2017" + Const.crnl +
                Const.crnl +
                "Version: " + appVersion + Const.crnl;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
