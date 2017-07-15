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
    public partial class AboutForm : Form
    {
        public AboutForm(ObsMon obsmon)
        {
            InitializeComponent();
            
            string appVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}",
                obsmon.Version.Major, obsmon.Version.Minor);

            labelAbout.Text = "This utility monitors the Wise 40 inch Observatory at" + Const.crnl +
                "Mizpe Ramon, Israel." + Const.crnl +
                Const.crnl +
                "The Observatory will be shut down if any of the monitored" + Const.crnl +
                "parameter thresholds (see the Settings menu) are exceeded." + Const.crnl +
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
