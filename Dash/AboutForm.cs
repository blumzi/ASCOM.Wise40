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

namespace Dash
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string appVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);

            labelAbout.Text = string.Format(
                "This dashboard controls the Wise 40 inch Observatory at" + Const.crnl +
                "Mizpe Ramon, Israel." + Const.crnl +
                Const.crnl +
                "The Wise Observatory is operated by the" + Const.crnl +
                Const.crnl +
                "Department of Astrophysics" + Const.crnl +
                "School of Physics and Astronomy" + Const.crnl +
                "Raymond and Beverly Sackler Faculty of Exact Sciences" + Const.crnl +
                Const.crnl +
                "Tel Aviv University" + Const.crnl +
                Const.crnl +
                "Author: Arie Blumenzweig <blumzi@013.net>" + Const.crnl +
                "October 2016" + Const.crnl +
                Const.crnl +
                "Version: {0}" + Const.crnl, appVersion);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
