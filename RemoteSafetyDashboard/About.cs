using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteSafetyDashboard
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();

            labelAbout.Text = "This dashboard shows the current weather and safety\n" +
                              "conditions at the Wise compound.\n" +
                              "\n" +
                              "It does this by periodically contacting the\n" +
                              $"ASCOM Server on {RemoteSafetyDashboard.remoteHostIp}\n" +
                              "\n" +
                              $"Version: v{RemoteSafetyDashboard.version}\n" +
                              $"Build time: {ASCOM.Wise40.Common.WiseObject.GetLinkerTimestampUtc(System.Reflection.Assembly.GetExecutingAssembly()):r}";

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
