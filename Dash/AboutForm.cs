using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                "This dashboard controls the Wise 40 inch Observatory at \r\n" +
                "Mizpe Ramon, Israel.\r\n" +
                "\r\n" +
                "The Wise Observatory is operated by the\r\n" +
                "\r\n" +
                "Department of Astrophysics\r\n" +
                "School of Physics and Astronomy\r\n" +
                "Raymond and Beverly Sackler Faculty of Exact Sciences\r\n" +
                "\r\n" +
                "Tel Aviv University\r\n" +
                "\r\n" +
                "Author: Arie Blumenzweig <blumzi@013.net>\r\n" +
                "October 2016\r\n" +
                "\r\n" + 
                "Version: {0}\r\n", appVersion);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
